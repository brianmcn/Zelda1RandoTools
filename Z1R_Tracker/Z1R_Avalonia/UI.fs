module UI

open System
open Avalonia
open Avalonia.Controls
open Avalonia.Media
open Avalonia.Layout

let canvasAdd = Graphics.canvasAdd

type MapStateProxy(state) =
    static let U = Graphics.uniqueMapIconBMPs.Length 
    static let NU = Graphics.nonUniqueMapIconBMPs.Length
    static member NumStates = U + NU
    member this.State = state
    member this.IsX = state = U+NU-1
    member this.IsUnique = state >= 0 && state < U
    member this.IsDungeon = state >= 0 && state < 9
    member this.IsWarp = state >= 9 && state < 13
    member this.IsSword3 = state=13
    member this.IsSword2 = state=14
    member this.IsThreeItemShop = TrackerModel.MapSquareChoiceDomainHelper.IsItem(state)
    member this.IsInteresting = not(state = -1 || this.IsX)
    member this.CurrentBMP() =
        if state = -1 then
            null
        elif state < U then
            Graphics.uniqueMapIconBMPs.[state]
        else
            Graphics.nonUniqueMapIconBMPs.[state-U]
    member this.CurrentInteriorBMP() =
        if state = -1 then
            null
        else
            Graphics.mapIconInteriorBMPs.[state]

let gridAdd = Graphics.gridAdd
let makeGrid = Graphics.makeGrid

let triforceInnerCanvases = Array.zeroCreate 8
let mainTrackerCanvases : Canvas[,] = Array2D.zeroCreate 9 5
let mainTrackerCanvasShaders : Canvas[,] = Array2D.init 8 5 (fun _ _ -> new Canvas(Width=30., Height=30., Background=Brushes.Black, Opacity=0.4, IsHitTestVisible=false))
let currentHeartsTextBox = new TextBox(Width=200., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "Current Hearts: %d" TrackerModel.playerComputedStateSummary.PlayerHearts, Padding=Thickness(0.))
let owRemainingScreensTextBox = new TextBox(Width=150., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "%d OW spots left" TrackerModel.mapStateSummary.OwSpotsRemain, Padding=Thickness(0.))
let owGettableScreensTextBox = new TextBox(Width=150., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "Show %d gettable" TrackerModel.mapStateSummary.OwGettableLocations.Count, Padding=Thickness(0.))
let owGettableScreensCheckBox = new CheckBox(Content = owGettableScreensTextBox)

let drawRoutesTo(routeDrawingCanvas, point, i, j, drawRouteMarks, maxYellowGreenHighlights) =
    let maxYellowGreenHighlights = if owGettableScreensCheckBox.IsChecked.HasValue && owGettableScreensCheckBox.IsChecked.Value then 128 else maxYellowGreenHighlights
    OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, 
                                        TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), point, i, j, drawRouteMarks, maxYellowGreenHighlights)

let mutable f5WasRecentlyPressed = false
let mutable currentlyMousedOWX, currentlyMousedOWY = -1, -1
let mutable notesTextBox = null : TextBox
let mutable timeTextBox = null : TextBox
let H = 30
let RIGHT_COL = 440.
let TCH = 123  // timeline height
let TH = 24 // text height
let OMTW = OverworldRouteDrawing.OMTW  // overworld map tile width - at normal aspect ratio, is 48 (16*3)
let resizeMapTileImage(image:Image) =
    image.Width <- OMTW
    image.Height <- float(11*3)
    image.Stretch <- Stretch.Fill
    image.StretchDirection <- StretchDirection.Both
    image
let trimNumeralBmpToImage(iconBMP:System.Drawing.Bitmap) =
    let trimmedBMP = new System.Drawing.Bitmap(int OMTW, iconBMP.Height)
    let offset = int((48.-OMTW)/2.)
    for x = 0 to int OMTW-1 do
        for y = 0 to iconBMP.Height-1 do
            trimmedBMP.SetPixel(x,y,iconBMP.GetPixel(x+offset,y))
    Graphics.BMPtoImage trimmedBMP
let makeAll(owMapNum) =
    let timelineItems = ResizeArray()
    let stringReverse (s:string) = new string(s.ToCharArray() |> Array.rev)
    let owMapBMPs, isMixed, owInstance =
        match owMapNum with
        | 0 -> Graphics.overworldMapBMPs(0), false, new OverworldData.OverworldInstance(OverworldData.FIRST)
        | 1 -> Graphics.overworldMapBMPs(1), false, new OverworldData.OverworldInstance(OverworldData.SECOND)
        | 2 -> Graphics.overworldMapBMPs(2), true,  new OverworldData.OverworldInstance(OverworldData.MIXED_FIRST)
        | 3 -> Graphics.overworldMapBMPs(3), true,  new OverworldData.OverworldInstance(OverworldData.MIXED_SECOND)
        | _ -> failwith "bad/unsupported owMapNum"
    TrackerModel.initializeAll(owInstance)
    let isCurrentlyBook = ref true
    let redrawBoxes = ResizeArray()
    let toggleBookMagicalShield() =
        isCurrentlyBook := not !isCurrentlyBook
        TrackerModel.forceUpdate()
        for f in redrawBoxes do
            f()
    
    let mutable showLocatorExactLocation = fun(_x:int,_y:int) -> ()
    let mutable showLocatorHintedZone = fun(_hz:TrackerModel.HintZone) -> ()
    let mutable showLocatorInstanceFunc = fun(f:int*int->bool) -> ()
    let mutable showShopLocatorInstanceFunc = fun(_item:int) -> ()
    let mutable showLocator = fun(_l:int) -> ()
    let mutable hideLocator = fun() -> ()

    let appMainCanvas = new Canvas(Width=16.*OMTW, Background=Brushes.Black)

    let mainTracker = makeGrid(9, 5, H, H)
    canvasAdd(appMainCanvas, mainTracker, 0., 0.)

    let hintHighlightBrush = new LinearGradientBrush(StartPoint=RelativePoint(0.,0.,RelativeUnit.Relative),EndPoint=RelativePoint(1.,1.,RelativeUnit.Relative))
    hintHighlightBrush.GradientStops.Add(new GradientStop(Colors.Yellow, 0.))
    hintHighlightBrush.GradientStops.Add(new GradientStop(Colors.DarkGreen, 1.))
    let makeHintHighlight(size) = new Shapes.Rectangle(Width=size, Height=size, StrokeThickness=0., Fill=hintHighlightBrush)
    // triforce
    let updateTriforceDisplay(i) =
        let innerc : Canvas = triforceInnerCanvases.[i]
        innerc.Children.Clear()
        let found = TrackerModel.mapStateSummary.DungeonLocations.[i]<>TrackerModel.NOTFOUND 
        if not(TrackerModel.dungeons.[i].IsComplete) &&  // dungeon could be complete without finding, in case of starting items and helpful hints
                not(found) && TrackerModel.levelHints.[i]<>TrackerModel.HintZone.UNKNOWN then
            innerc.Children.Add(makeHintHighlight(30.)) |> ignore
        if not(TrackerModel.dungeons.[i].PlayerHasTriforce()) then 
            innerc.Children.Add(if not(found) then Graphics.emptyUnfoundTriforces.[i] else Graphics.emptyFoundTriforces.[i]) |> ignore
        else
            innerc.Children.Add(Graphics.fullTriforces.[i]) |> ignore 
    for i = 0 to 7 do
        let image = Graphics.emptyUnfoundTriforces.[i]
        // triforce dungeon color
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        mainTrackerCanvases.[i,0] <- c
        gridAdd(mainTracker, c, i, 0)
        // triforce itself and label
        let c = new Canvas(Width=30., Height=30.)
        mainTrackerCanvases.[i,1] <- c
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has triforce drawn on it, not the eventual shading of updateDungeon()
        triforceInnerCanvases.[i] <- innerc
        c.Children.Add(innerc) |> ignore
        canvasAdd(innerc, image, 0., 0.)
        c.PointerPressed.Add(fun _ -> 
            TrackerModel.dungeons.[i].ToggleTriforce()
            updateTriforceDisplay(i)
        )
        gridAdd(mainTracker, c, i, 1)
        timelineItems.Add(new Timeline.TimelineItem(fun()->if TrackerModel.dungeons.[i].PlayerHasTriforce() then Some(Graphics.fullTriforce_bmps.[i]) else None))
    let level9ColorCanvas = new Canvas(Width=30., Height=30., Background=Brushes.Black)       // dungeon 9 doesn't need a color, but we don't want to special case nulls
    gridAdd(mainTracker, level9ColorCanvas, 8, 0) 
    mainTrackerCanvases.[8,0] <- level9ColorCanvas
    let level9NumeralCanvas = new Canvas(Width=30., Height=30.)     // dungeon 9 doesn't have triforce, but does have grey/white numeral display
    gridAdd(mainTracker, level9NumeralCanvas, 8, 1) 
    mainTrackerCanvases.[8,1] <- level9NumeralCanvas
    let boxItemImpl(box:TrackerModel.Box, requiresForceUpdate) = 
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=CustomComboBoxes.no, StrokeThickness=3.0)
        c.Children.Add(rect) |> ignore
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has item drawn on it, not the box
        c.Children.Add(innerc) |> ignore
        let boxCurrentBMP(isForTimeline) = CustomComboBoxes.boxCurrentBMP(isCurrentlyBook, box.CellCurrent(), isForTimeline)
        let redrawBoxOutline() =
            match box.PlayerHas() with
            | TrackerModel.PlayerHas.YES -> rect.Stroke <- CustomComboBoxes.yes
            | TrackerModel.PlayerHas.NO -> rect.Stroke <- CustomComboBoxes.no
            | TrackerModel.PlayerHas.SKIPPED -> rect.Stroke <- CustomComboBoxes.skipped
        let redrawInner() =
            innerc.Children.Clear()
            let bmp = boxCurrentBMP(false)
            if bmp <> null then
                canvasAdd(innerc, Graphics.BMPtoImage(bmp), 4., 4.)
        let activateComboBox(initBCC) =
            let pos = c.TranslatePoint(Point(),appMainCanvas)
            CustomComboBoxes.DisplayItemComboBox(appMainCanvas, pos.Value.X, pos.Value.Y, box.CellCurrent(), initBCC, isCurrentlyBook, (fun (newBoxCellValue, newPlayerHas) ->
                // update model
                box.Set(newBoxCellValue, newPlayerHas)
                // update view
                redrawBoxOutline()
                redrawInner()
                if requiresForceUpdate then
                    TrackerModel.forceUpdate()
                ))
        c.PointerPressed.Add(fun ea -> 
            let pp = ea.GetCurrentPoint(c)
            if pp.Properties.IsLeftButtonPressed || pp.Properties.IsMiddleButtonPressed || pp.Properties.IsRightButtonPressed then 
                if box.CellCurrent() = -1 then
                    activateComboBox(-1)
                else
                    box.SetPlayerHas(CustomComboBoxes.MouseButtonEventArgsToPlayerHas pp)
                    redrawBoxOutline()
                    if requiresForceUpdate then
                        TrackerModel.forceUpdate()
            )
        // item
        c.PointerWheelChanged.Add(fun x -> 
            let initBCC =
                if x.Delta.Y<0. then
                    box.CellNextFreeKey()
                else
                    box.CellPrevFreeKey()
            activateComboBox(initBCC)
            )
        c.PointerEnter.Add(fun _ ->
            match box.CellCurrent() with
            | 3 -> showLocatorInstanceFunc(owInstance.PowerBraceletable)
            | 4 -> showLocatorInstanceFunc(owInstance.Ladderable)
            | 7 -> showLocatorInstanceFunc(owInstance.Raftable)
            | 8 -> showLocatorInstanceFunc(owInstance.Whistleable)
            | 9 -> showLocatorInstanceFunc(owInstance.Burnable)
            | _ -> ()
            )
        c.PointerLeave.Add(fun _ ->
            hideLocator()
            )
        redrawBoxes.Add(fun() -> redrawInner())
        redrawBoxOutline()
        redrawInner()
        timelineItems.Add(new Timeline.TimelineItem(fun()->if obj.Equals(rect.Stroke,CustomComboBoxes.yes) then Some(boxCurrentBMP(true)) else None))
        c
    // items
    let finalCanvasOf1Or4 = boxItemImpl(TrackerModel.FinalBoxOf1Or4, false)
    for i = 0 to 8 do
        for j = 0 to 2 do
            let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
            gridAdd(mainTracker, c, i, j+2)
            if j=0 || j=1 || i=7 then
                canvasAdd(c, boxItemImpl(TrackerModel.dungeons.[i].Boxes.[j], false), 0., 0.)
            if i < 8 then
                mainTrackerCanvases.[i,j+2] <- c
    let RedrawForSecondQuestDungeonToggle() =
        mainTrackerCanvases.[0,4].Children.Remove(finalCanvasOf1Or4) |> ignore
        mainTrackerCanvases.[3,4].Children.Remove(finalCanvasOf1Or4) |> ignore
        if TrackerModel.Options.IsSecondQuestDungeons.Value then
            canvasAdd(mainTrackerCanvases.[3,4], finalCanvasOf1Or4, 0., 0.)
        else
            canvasAdd(mainTrackerCanvases.[0,4], finalCanvasOf1Or4, 0., 0.)
    RedrawForSecondQuestDungeonToggle()

    for i = 0 to 8 do
        for j = 0 to 1 do  // only hovering colors/triforces will show it
            mainTrackerCanvases.[i,j].PointerEnter.Add(fun _ -> showLocator(i))
            mainTrackerCanvases.[i,j].PointerLeave.Add(fun _ -> hideLocator())

    // in mixed quest, buttons to hide first/second quest
    let mutable firstQuestOnlyInterestingMarks = Array2D.zeroCreate 16 8
    let mutable secondQuestOnlyInterestingMarks = Array2D.zeroCreate 16 8
    let thereAreMarks(questOnlyInterestingMarks:_[,]) =
        let mutable r = false
        for x = 0 to 15 do 
            for y = 0 to 7 do
                if questOnlyInterestingMarks.[x,y] then
                    r <- true
        r
    let mutable hideFirstQuestFromMixed = fun b -> ()
    let mutable hideSecondQuestFromMixed = fun b -> ()

    let hideFirstQuestCheckBox  = new CheckBox(Content=new TextBox(Text="HFQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true, Padding=Thickness(0.)))
    ToolTip.SetTip(hideFirstQuestCheckBox, "Hide First Quest\nIn a mixed quest overworld tracker, shade out the first-quest-only spots.\nUseful if you're unsure if randomizer flags are mixed quest or second quest.\nCan't be used if you've marked a first-quest-only spot as having something.")
    let hideSecondQuestCheckBox = new CheckBox(Content=new TextBox(Text="HSQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true, Padding=Thickness(0.)))
    ToolTip.SetTip(hideSecondQuestCheckBox, "Hide Second Quest\nIn a mixed quest overworld tracker, shade out the second-quest-only spots.\nUseful if you're unsure if randomizer flags are mixed quest or first quest.\nCan't be used if you've marked a second-quest-only spot as having something.")

    hideFirstQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
    hideFirstQuestCheckBox.Checked.Add(fun _ -> 
        if thereAreMarks(firstQuestOnlyInterestingMarks) then
// TODO            System.Media.SystemSounds.Asterisk.Play()
            hideFirstQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        else
            hideFirstQuestFromMixed false
        hideSecondQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        )
    hideFirstQuestCheckBox.Unchecked.Add(fun _ -> hideFirstQuestFromMixed true)
    if isMixed then
        canvasAdd(appMainCanvas, hideFirstQuestCheckBox, 35., 120.) 

    hideSecondQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
    hideSecondQuestCheckBox.Checked.Add(fun _ -> 
        if thereAreMarks(secondQuestOnlyInterestingMarks) then
// TODO            System.Media.SystemSounds.Asterisk.Play()
            hideSecondQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        else
            hideSecondQuestFromMixed false
        hideFirstQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        )
    hideSecondQuestCheckBox.Unchecked.Add(fun _ -> hideSecondQuestFromMixed true)
    if isMixed then
        canvasAdd(appMainCanvas, hideSecondQuestCheckBox, 140., 120.) 

    let OFFSET = 280.
    // ow 'take any' hearts
    let owHeartGrid = makeGrid(4, 1, 30, 30)
    for i = 0 to 3 do
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        canvasAdd(c, Graphics.owHeartsEmpty.[i], 0., 0.)
        let f b =
            let cur = 
                if c.Children.Contains(Graphics.owHeartsEmpty.[i]) then 0
                elif c.Children.Contains(Graphics.owHeartsFull.[i]) then 1
                else 2
            c.Children.Clear()
            let next = (cur + (if b then 1 else -1) + 3) % 3
            canvasAdd(c, (  if next = 0 then 
                                Graphics.owHeartsEmpty.[i] 
                            elif next = 1 then 
                                Graphics.owHeartsFull.[i] 
                            else 
                                Graphics.owHeartsSkipped.[i]), 0., 0.)
            TrackerModel.playerProgressAndTakeAnyHearts.SetTakeAnyHeart(i,next)
        c.PointerPressed.Add(fun ea -> f(ea.GetCurrentPoint(c).Properties.IsLeftButtonPressed))
        c.PointerWheelChanged.Add(fun x -> f (x.Delta.Y<0.))
        gridAdd(owHeartGrid, c, i, 0)
        timelineItems.Add(new Timeline.TimelineItem(fun()->if TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(i)=1 then Some(Graphics.owHeartFull_bmp) else None))
    canvasAdd(appMainCanvas, owHeartGrid, OFFSET, 30.)
    // ladder, armos, white sword items
    let owItemGrid = makeGrid(2, 3, 30, 30)
    gridAdd(owItemGrid, Graphics.BMPtoImage Graphics.ladder_bmp, 0, 0)
    let armos = Graphics.BMPtoImage Graphics.ow_key_armos_bmp
    armos.PointerEnter.Add(fun _ -> showLocatorInstanceFunc(owInstance.HasArmos))
    armos.PointerLeave.Add(fun _ -> hideLocator())
    gridAdd(owItemGrid, armos, 0, 1)
    let white_sword_image = Graphics.BMPtoImage Graphics.white_sword_bmp
    let white_sword_canvas = new Canvas(Width=21., Height=21.)
    let redrawWhiteSwordCanvas() =
        white_sword_canvas.Children.Clear()
        if not(TrackerModel.playerComputedStateSummary.HaveWhiteSwordItem) &&           // don't have it yet
                TrackerModel.mapStateSummary.Sword2Location=TrackerModel.NOTFOUND &&    // have not found cave
                TrackerModel.levelHints.[9]<>TrackerModel.HintZone.UNKNOWN then         // have a hint
            white_sword_canvas.Children.Add(makeHintHighlight(21.)) |> ignore
        white_sword_canvas.Children.Add(white_sword_image) |> ignore
    redrawWhiteSwordCanvas()
    gridAdd(owItemGrid, white_sword_canvas, 0, 2)
    gridAdd(owItemGrid, boxItemImpl(TrackerModel.ladderBox, true), 1, 0)
    gridAdd(owItemGrid, boxItemImpl(TrackerModel.armosBox, false), 1, 1)
    gridAdd(owItemGrid, boxItemImpl(TrackerModel.sword2Box, true), 1, 2)
    white_sword_canvas.PointerEnter.Add(fun _ -> showLocator(9))
    white_sword_canvas.PointerLeave.Add(fun _ -> hideLocator())
    canvasAdd(appMainCanvas, owItemGrid, OFFSET, 60.)
    // brown sword, blue candle, blue ring, magical sword
    let owItemGrid = makeGrid(3, 3, 30, 30)
    let veryBasicBoxImpl(bmp:System.Drawing.Bitmap, startOn, isTimeline, changedFunc) =
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let no = Brushes.DarkRed
        let yes = Brushes.LimeGreen 
        let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=(if startOn then yes else no), StrokeThickness=3.0)
        c.Children.Add(rect) |> ignore
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has item drawn on it, not the box
        c.Children.Add(innerc) |> ignore
        c.PointerPressed.Add(fun ea -> 
            if ea.GetCurrentPoint(c).Properties.IsLeftButtonPressed then 
                if obj.Equals(rect.Stroke, no) then
                    rect.Stroke <- yes
                else
                    rect.Stroke <- no
                changedFunc(obj.Equals(rect.Stroke, yes))
        )
        canvasAdd(innerc, Graphics.BMPtoImage bmp, 4., 4.)
        if isTimeline then
            timelineItems.Add(new Timeline.TimelineItem(fun()->if obj.Equals(rect.Stroke,yes) then Some(bmp) else None))
        c
    let basicBoxImpl(tts, img, changedFunc) =
        let c = veryBasicBoxImpl(img, false, true, changedFunc)
        ToolTip.SetTip(c, tts)
        c
    let wood_sword_box = basicBoxImpl("Acquired wood sword (mark timeline)",    Graphics.brown_sword_bmp  , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword.Toggle()))
    gridAdd(owItemGrid, wood_sword_box, 1, 0)
    let wood_arrow_box = basicBoxImpl("Acquired wood arrow (mark timeline)",    Graphics.wood_arrow_bmp   , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodArrow.Toggle()))
    wood_arrow_box.PointerEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.ARROW))
    wood_arrow_box.PointerLeave.Add(fun _ -> hideLocator())
    gridAdd(owItemGrid, wood_arrow_box, 2, 1)
    let blue_candle_box = basicBoxImpl("Acquired blue candle (mark timeline, affects routing)",   Graphics.blue_candle_bmp  , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle.Toggle()))
    blue_candle_box.PointerEnter.Add(fun _ -> if TrackerModel.playerComputedStateSummary.CandleLevel=0 then showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BLUE_CANDLE) else showLocatorInstanceFunc(owInstance.Burnable))
    blue_candle_box.PointerLeave.Add(fun _ -> hideLocator())
    gridAdd(owItemGrid, blue_candle_box, 1, 1)
    let blue_ring_box = basicBoxImpl("Acquired blue ring (mark timeline)",     Graphics.blue_ring_bmp    , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueRing.Toggle()))
    blue_ring_box.PointerEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BLUE_RING))
    blue_ring_box.PointerLeave.Add(fun _ -> hideLocator())
    gridAdd(owItemGrid, blue_ring_box, 2, 0)
    let mags_box = basicBoxImpl("Acquired magical sword (mark timeline)", Graphics.magical_sword_bmp, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Toggle()))
    ToolTip.SetPlacement(mags_box, PlacementMode.Left)  // Avalonia's tip placement seems awful, at least on Windows
    let magsHintHighlight = makeHintHighlight(30.)
    let redrawMagicalSwordCanvas() =
        mags_box.Children.Remove(magsHintHighlight) |> ignore
        if not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) &&   // dont have sword
                TrackerModel.mapStateSummary.Sword3Location=TrackerModel.NOTFOUND &&           // not yet located cave
                TrackerModel.levelHints.[10]<>TrackerModel.HintZone.UNKNOWN then               // have a hint
            mags_box.Children.Insert(0, magsHintHighlight)
    redrawMagicalSwordCanvas()
    gridAdd(owItemGrid, mags_box, 0, 2)
    mags_box.PointerEnter.Add(fun _ -> showLocator(10))
    mags_box.PointerLeave.Add(fun _ -> hideLocator())
    canvasAdd(appMainCanvas, owItemGrid, OFFSET+60., 60.)
    // boomstick book, to mark when purchase in boomstick seed (normal book will become shield found in dungeon)
    let boom_book_box = basicBoxImpl("Purchased boomstick book (mark timeline)", Graphics.boom_book_bmp, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Toggle()))
    boom_book_box.PointerEnter.Add(fun _ -> showLocatorExactLocation(TrackerModel.mapStateSummary.BoomBookShopLocation))
    boom_book_box.PointerLeave.Add(fun _ -> hideLocator())
    canvasAdd(appMainCanvas, boom_book_box, OFFSET+120., 30.)
    // mark the dungeon wins on timeline via ganon/zelda boxes
    gridAdd(owItemGrid, basicBoxImpl("Killed Ganon (mark timeline)",  Graphics.ganon_bmp, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasDefeatedGanon.Toggle())), 1, 2)
    gridAdd(owItemGrid, basicBoxImpl("Rescued Zelda (mark timeline)", Graphics.zelda_bmp, (fun b -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Toggle(); if b then notesTextBox.Text <- notesTextBox.Text + "\n" + timeTextBox.Text)), 2, 2)
    // mark whether player currently has bombs, for overworld routing
    let bombIcon = veryBasicBoxImpl(Graphics.bomb_bmp, false, false, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBombs.Toggle()))
    bombIcon.PointerEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BOMB))
    bombIcon.PointerLeave.Add(fun _ -> hideLocator())
    ToolTip.SetTip(bombIcon, "Player currently has bombs (affects routing)")
    canvasAdd(appMainCanvas, bombIcon, OFFSET+160., 60.)

    // shield versus book icon (for boomstick flags/seeds)
    let toggleBookShieldCheckBox  = new CheckBox(Content=new TextBox(Text="S/B",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true, Padding=Thickness(0.)))
    ToolTip.SetTip(toggleBookShieldCheckBox, "Shield item icon instead of book item icon")
    toggleBookShieldCheckBox.IsChecked <- System.Nullable.op_Implicit false
    toggleBookShieldCheckBox.Checked.Add(fun _ -> toggleBookMagicalShield())
    toggleBookShieldCheckBox.Unchecked.Add(fun _ -> toggleBookMagicalShield())
    canvasAdd(appMainCanvas, toggleBookShieldCheckBox, OFFSET+150., 30.)

    // overworld map grouping, as main point of support for mirroring
    let mirrorOverworldFEs = ResizeArray<Visual>()   // overworldCanvas (on which all map is drawn) is here, as well as individual tiny textual/icon elements that need to be re-flipped
    let mutable displayIsCurrentlyMirrored = false
    let overworldCanvas = new Canvas(Width=OMTW*16., Height=11.*3.*8.)
    canvasAdd(appMainCanvas, overworldCanvas, 0., 150.)
    mirrorOverworldFEs.Add(overworldCanvas)

    // ow map opaque fixed bottom layer
    let X_OPACITY = 0.55
    let owOpaqueMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    owOpaqueMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    for i = 0 to 15 do
        for j = 0 to 7 do
            let image = resizeMapTileImage <| Graphics.BMPtoImage(owMapBMPs.[i,j])
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            canvasAdd(c, image, 0., 0.)
            gridAdd(owOpaqueMapGrid, c, i, j)
            // shading between map tiles
            let OPA = 0.25
            let bottomShade = new Canvas(Width=OMTW, Height=float(3), Background=Brushes.Black, Opacity=OPA)
            canvasAdd(c, bottomShade, 0., float(10*3))
            let rightShade  = new Canvas(Width=float(3), Height=float(11*3), Background=Brushes.Black, Opacity=OPA)
            canvasAdd(c, rightShade, OMTW-3., 0.)
            // permanent icons
            if owInstance.AlwaysEmpty(i,j) then
                let icon = resizeMapTileImage <| Graphics.BMPtoImage Graphics.nonUniqueMapIconBMPs.[Graphics.nonUniqueMapIconBMPs.Length-1] // "X"
                icon.Opacity <- X_OPACITY
                canvasAdd(c, icon, 0., 0.)
    canvasAdd(overworldCanvas, owOpaqueMapGrid, 0., 0.)

    // layer to place darkening icons - dynamic icons that are below route-drawing but above the fixed base layer
    // this layer is also used to draw map icons that get drawn below routing, such as potion shops
    let owDarkeningMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owDarkeningMapGridCanvases = Array2D.zeroCreate 16 8
    owDarkeningMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            gridAdd(owDarkeningMapGrid, c, i, j)
            owDarkeningMapGridCanvases.[i,j] <- c
    canvasAdd(overworldCanvas, owDarkeningMapGrid, 0., 0.)

    // layer to place 'hiding' icons - dynamic darkening icons that are below route-drawing but above the previous layers
    let owHidingMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owHidingMapGridCanvases = Array2D.zeroCreate 16 8
    owHidingMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            gridAdd(owHidingMapGrid, c, i, j)
            owHidingMapGridCanvases.[i,j] <- c
    canvasAdd(overworldCanvas, owHidingMapGrid, 0., 0.)
    let hide(x,y) =
        let hideColor = Brushes.DarkSlateGray // Brushes.Black
        let hideOpacity = 0.6 // 0.4
        let rect = new Shapes.Rectangle(Width=7.0*OMTW/48., Height=float(11*3)-1.5, Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
        canvasAdd(owHidingMapGridCanvases.[x,y], rect, 7.*OMTW/48., 0.)
        let rect = new Shapes.Rectangle(Width=7.0*OMTW/48., Height=float(11*3)-1.5, Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
        canvasAdd(owHidingMapGridCanvases.[x,y], rect, 19.*OMTW/48., 0.)
        let rect = new Shapes.Rectangle(Width=7.0*OMTW/48., Height=float(11*3)-1.5, Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
        canvasAdd(owHidingMapGridCanvases.[x,y], rect, 32.*OMTW/48., 0.)
    hideSecondQuestFromMixed <- 
        (fun unhide ->  // make mixed appear reduced to 1st quest
            for x = 0 to 15 do
                for y = 0 to 7 do
                    if OverworldData.owMapSquaresSecondQuestOnly.[y].Chars(x) = 'X' then
                        if unhide then
                            owHidingMapGridCanvases.[x,y].Children.Clear()
                        else
                            hide(x,y)
        )
    hideFirstQuestFromMixed <-
        (fun unhide ->   // make mixed appear reduced to 2nd quest
            for x = 0 to 15 do
                for y = 0 to 7 do
                    if OverworldData.owMapSquaresFirstQuestOnly.[y].Chars(x) = 'X' then
                        if unhide then
                            owHidingMapGridCanvases.[x,y].Children.Clear()
                        else
                            hide(x,y)
        )

    // ow route drawing layer
    let routeDrawingCanvas = new Canvas(Width=16.*OMTW, Height=float(8*11*3))
    routeDrawingCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(overworldCanvas, routeDrawingCanvas, 0., 0.)

    // nearby ow tiles magnified overlay
    let ENLARGE = 8. // make it this x bigger
    let BT = 2.  // border thickness of the interior 3x3 grid of tiles
    let dungeonTabsOverlay = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(5.), Background=Brushes.Black, IsVisible=false, IsHitTestVisible=false)
    let dungeonTabsOverlayContent = new Canvas(Width=3.*16.*ENLARGE + 4.*BT, Height=3.*11.*ENLARGE + 4.*BT)
    mirrorOverworldFEs.Add(dungeonTabsOverlayContent)
    dungeonTabsOverlay.Child <- dungeonTabsOverlayContent
    let overlayTiles = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let bmp = new System.Drawing.Bitmap(16*int ENLARGE, 11*int ENLARGE)
            for x = 0 to 15 do
                for y = 0 to 10 do
                    let c = owMapBMPs.[i,j].GetPixel(x*3, y*3)
                    for px = 0 to int ENLARGE - 1 do
                        for py = 0 to int ENLARGE - 1 do
                            // diagonal rocks
                            let c = 
                                // The diagonal rock data is based on the first quest map. A few screens are different in 2nd/mixed quest.
                                // So we apply a kludge to load the correct diagonal data.
                                let i,j = 
                                    if owMapNum=1 && i=4 && j=7 then // second quest has a cave like 14,5 here
                                        14,5
                                    elif owMapNum=1 && i=11 && j=0 then // second quest has dead fairy here, borrow 2,4
                                        2,4
                                    elif owMapNum<>0 && i=12 && j=3 then // non-first quest has a whistle lake here, borrow 2,4
                                        2,4
                                    else
                                        i,j
                                if OverworldData.owNEupperRock.[i,j].[x,y] then
                                    if px+py > int ENLARGE - 1 then 
                                        owMapBMPs.[i,j].GetPixel(x*3, (y+1)*3)
                                    else 
                                        c
                                elif OverworldData.owSEupperRock.[i,j].[x,y] then
                                    if px < py then 
                                        owMapBMPs.[i,j].GetPixel(x*3, (y+1)*3)
                                    else 
                                        c
                                elif OverworldData.owNElowerRock.[i,j].[x,y] then
                                    if px+py < int ENLARGE - 1 then 
                                        owMapBMPs.[i,j].GetPixel(x*3, (y-1)*3)
                                    else 
                                        c
                                elif OverworldData.owSElowerRock.[i,j].[x,y] then
                                    if px > py then 
                                        owMapBMPs.[i,j].GetPixel(x*3, (y-1)*3)
                                    else 
                                        c
                                else 
                                    c
                            // edges of squares
                            let c = 
                                if (px+1) % int ENLARGE = 0 || (py+1) % int ENLARGE = 0 then
                                    System.Drawing.Color.FromArgb(int c.R / 2, int c.G / 2, int c.B / 2)
                                else
                                    c
                            bmp.SetPixel(x*int ENLARGE + px, y*int ENLARGE + py, c)
            overlayTiles.[i,j] <- Graphics.BMPtoImage bmp
    // ow map
    let owMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owCanvases = Array2D.zeroCreate 16 8
    let owUpdateFunctions = Array2D.create 16 8 (fun _ _ -> ())
    let drawRectangleCornersHighlight(c,x,y,color) =
        // full rectangles badly obscure routing paths, so we just draw corners
        let L1,L2,R1,R2 = 0.0, (OMTW-4.)/2.-6., (OMTW-4.)/2.+6., OMTW-4.
        let T1,T2,B1,B2 = 0.0, 10.0, 19.0, 29.0
        let s = new Shapes.Line(StartPoint=Point(L1,T1+1.5), EndPoint=Point(L2,T1+1.5), Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new Shapes.Line(StartPoint=Point(L1+1.5,T1), EndPoint=Point(L1+1.5,T2), Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new Shapes.Line(StartPoint=Point(L1,B2-1.5), EndPoint=Point(L2,B2-1.5), Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new Shapes.Line(StartPoint=Point(L1+1.5,B1), EndPoint=Point(L1+1.5,B2), Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new Shapes.Line(StartPoint=Point(R1,T1+1.5), EndPoint=Point(R2,T1+1.5), Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new Shapes.Line(StartPoint=Point(R2-1.5,T1), EndPoint=Point(R2-1.5,T2), Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new Shapes.Line(StartPoint=Point(R1,B2-1.5), EndPoint=Point(R2,B2-1.5), Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new Shapes.Line(StartPoint=Point(R2-1.5,B1), EndPoint=Point(R2-1.5,B2), Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
    let drawDungeonHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,Brushes.Yellow)
    let drawCompletedIconHighlight(c,x,y) =
        let rect = new Shapes.Rectangle(Width=20.0*OMTW/48., Height=27.0, Stroke=Brushes.Black, StrokeThickness = 3.,
                                                        Fill=Brushes.Black, Opacity=0.4)
        let diff = if displayIsCurrentlyMirrored then 16.0*OMTW/48. else 12.0*OMTW/48.
        canvasAdd(c, rect, x*OMTW+diff, float(y*11*3)+3.0)
    let drawCompletedDungeonHighlight(c,x,y) =
        // darkened rectangle corners
        let yellow = Brushes.Yellow.Color
        let darkYellow = Color.FromRgb(yellow.R/2uy, yellow.G/2uy, yellow.B/2uy)
        drawRectangleCornersHighlight(c,x,y,new SolidColorBrush(darkYellow))
        // darken the number
        drawCompletedIconHighlight(c,x,y)
    let drawWarpHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,Brushes.Orchid)
    let drawDarkening(c,x,y) =
        let rect = new Shapes.Rectangle(Width=OMTW, Height=float(11*3), Stroke=Brushes.Black, StrokeThickness = 3.,
                                                        Fill=Brushes.Black, Opacity=X_OPACITY)
        canvasAdd(c, rect, x*OMTW, float(y*11*3))
    let drawDungeonRecorderWarpHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,Brushes.Lime)
    let mutable mostRecentMouseEnterTime = DateTime.Now 
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            let mutable pointerEnteredButNotDrawnRoutingYet = false  // PointerEnter does not correctly report mouse position, but PointerMoved does
            gridAdd(owMapGrid, c, i, j)
            // we need a dummy image to make the canvas absorb the mouse interactions, so just re-draw the map at 0 opacity
            let image = resizeMapTileImage <| Graphics.BMPtoImage(owMapBMPs.[i,j])
            image.Opacity <- 0.0
            canvasAdd(c, image, 0., 0.)
            // highlight mouse, do mouse-sensitive stuff
            let rect = new Shapes.Rectangle(Width=OMTW-4., Height=float(11*3)-4., Stroke=Brushes.White, StrokeThickness = 2.)
            c.PointerEnter.Add(fun ea ->canvasAdd(c, rect, 2., 2.)
                                        pointerEnteredButNotDrawnRoutingYet <- true
                                        // show enlarged version of current & nearby rooms
                                        dungeonTabsOverlayContent.Children.Clear()
                                        // fill whole canvas black, so elements behind don't show through
                                        canvasAdd(dungeonTabsOverlayContent, new Shapes.Rectangle(Width=dungeonTabsOverlayContent.Width, Height=dungeonTabsOverlayContent.Height, Fill=Brushes.Black), 0., 0.)
                                        let xmin = min (max (i-1) 0) 13
                                        let ymin = min (max (j-1) 0) 5
                                        // draw a white highlight rectangle behind the tile where mouse is
                                        let rect = new Shapes.Rectangle(Width=16.*ENLARGE + 2.*BT, Height=11.*ENLARGE + 2.*BT, Fill=Brushes.White)
                                        canvasAdd(dungeonTabsOverlayContent, rect, float (i-xmin)*(16.*ENLARGE+BT), float (j-ymin)*(11.*ENLARGE+BT))
                                        // draw the 3x3 tiles
                                        for x = 0 to 2 do
                                            for y = 0 to 2 do
                                                canvasAdd(dungeonTabsOverlayContent, overlayTiles.[xmin+x,ymin+y], BT+float x*(16.*ENLARGE+BT), BT+float y*(11.*ENLARGE+BT))
                                        if TrackerModel.Options.Overworld.ShowMagnifier.Value then 
                                            dungeonTabsOverlay.IsVisible <- true
                                        // track current location for F5 & speech recognition purposes
                                        currentlyMousedOWX <- i
                                        currentlyMousedOWY <- j
                                        mostRecentMouseEnterTime <- DateTime.Now)
            c.PointerMoved.Add(fun ea ->
                if pointerEnteredButNotDrawnRoutingYet then
                    // draw routes
                    let mousePos = ea.GetPosition(c)
                    let mousePos = if displayIsCurrentlyMirrored then Point(OMTW - mousePos.X, mousePos.Y) else mousePos
                    drawRoutesTo(routeDrawingCanvas, mousePos, i, j, 
                                    TrackerModel.Options.Overworld.DrawRoutes.Value, if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxYGH else 0)
                    pointerEnteredButNotDrawnRoutingYet <- false)
            c.PointerLeave.Add(fun _ -> c.Children.Remove(rect) |> ignore
                                        dungeonTabsOverlayContent.Children.Clear()
                                        dungeonTabsOverlay.IsVisible <- false
                                        pointerEnteredButNotDrawnRoutingYet <- false
                                        routeDrawingCanvas.Children.Clear())
            // icon
            if owInstance.AlwaysEmpty(i,j) then
                () // already set up as permanent opaque layer, in code above
            else
                let redrawGridSpot() =
                    // cant remove-by-identity because of non-uniques; remake whole canvas
                    owDarkeningMapGridCanvases.[i,j].Children.Clear()
                    c.Children.Clear()
                    // we need a dummy image to make the canvas absorb the mouse interactions, so just re-draw the map at 0 opacity
                    let image = resizeMapTileImage <| Graphics.BMPtoImage(owMapBMPs.[i,j])
                    image.Opacity <- 0.0
                    canvasAdd(c, image, 0., 0.)
                    let ms = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                    let iconBMP = 
                        if ms.IsThreeItemShop && TrackerModel.getOverworldMapExtraData(i,j) <> 0 then
                            let item1 = ms.State - 16  // 0-based
                            let item2 = TrackerModel.getOverworldMapExtraData(i,j) - 1   // 0-based
                            // cons up a two-item shop image
                            let tile = new System.Drawing.Bitmap(16*3,11*3)
                            for px = 0 to 16*3-1 do
                                for py = 0 to 11*3-1 do
                                    // two-icon area
                                    if px/3 >= 3 && px/3 <= 11 && py/3 >= 1 && py/3 <= 9 then
                                        tile.SetPixel(px, py, Graphics.itemBackgroundColor)
                                    else
                                        tile.SetPixel(px, py, Graphics.TRANS_BG)
                                    // icon 1
                                    if px/3 >= 4 && px/3 <= 6 && py/3 >= 2 && py/3 <= 8 then
                                        let c = Graphics.itemsBMP.GetPixel(item1*3 + px/3-4, py/3-2)
                                        if c.ToArgb() <> System.Drawing.Color.Black.ToArgb() then
                                            tile.SetPixel(px, py, c)
                                    // icon 2
                                    if px/3 >= 8 && px/3 <= 10 && py/3 >= 2 && py/3 <= 8 then
                                        let c = Graphics.itemsBMP.GetPixel(item2*3 + px/3-8, py/3-2)
                                        if c.ToArgb() <> System.Drawing.Color.Black.ToArgb() then
                                            tile.SetPixel(px, py, c)
                            tile
                        else
                            ms.CurrentBMP()
                    // be sure to draw in appropriate layer
                    if iconBMP <> null then 
                        let icon = resizeMapTileImage(Graphics.BMPtoImage iconBMP)
                        if ms.IsX then
                            icon.Opacity <- X_OPACITY
                            resizeMapTileImage icon |> ignore
                            canvasAdd(owDarkeningMapGridCanvases.[i,j], icon, 0., 0.)  // the icon 'is' the darkening
                        else
                            icon.Opacity <- 1.0
                            drawDarkening(owDarkeningMapGridCanvases.[i,j], 0., 0)     // darken below icon and routing marks
                            resizeMapTileImage icon |> ignore
                            canvasAdd(c, icon, 0., 0.)                                 // add icon above routing
                    if ms.IsDungeon then
                        drawDungeonHighlight(c,0.,0)
                    if ms.IsWarp then
                        drawWarpHighlight(c,0.,0)
                let updateGridSpot delta phrase =
                    if delta = 1 then
                        TrackerModel.overworldMapMarks.[i,j].Next()
                    elif delta = -1 then 
                        TrackerModel.overworldMapMarks.[i,j].Prev() 
                    elif delta = 0 then 
                        ()
                    else failwith "bad delta"
                    let ms = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                    if OverworldData.owMapSquaresSecondQuestOnly.[j].Chars(i) = 'X' then
                        secondQuestOnlyInterestingMarks.[i,j] <- ms.IsInteresting 
                    if OverworldData.owMapSquaresFirstQuestOnly.[j].Chars(i) = 'X' then
                        firstQuestOnlyInterestingMarks.[i,j] <- ms.IsInteresting 
                    redrawGridSpot()
                owUpdateFunctions.[i,j] <- updateGridSpot 
                owCanvases.[i,j] <- c
                mirrorOverworldFEs.Add(c)
                mirrorOverworldFEs.Add(owDarkeningMapGridCanvases.[i,j])
                c.PointerPressed.Add(fun ea -> 
                    if ea.GetCurrentPoint(c).Properties.IsLeftButtonPressed then 
                        // left click activates the popup selector
                        let ST = 3.
                        let tileImage = resizeMapTileImage <| Graphics.BMPtoImage(owMapBMPs.[i,j])
                        let tileCanvas = new Canvas(Width=OMTW, Height=11.*3.)
                        let originalState = TrackerModel.overworldMapMarks.[i,j].Current()
                        let originalStateIndex = if originalState = -1 then MapStateProxy.NumStates else originalState
                        let gridxPosition = if i < 12 then -ST else OMTW - float(8*(5*3+2*int ST)+int ST)
                        let gridElementsSelectablesAndIDs : (Control*bool*int)[] = Array.init (MapStateProxy.NumStates+1) (fun n ->
                            if MapStateProxy(n).IsX then
                                upcast new Canvas(Width=5.*3., Height=9.*3., Background=new SolidColorBrush(Color.FromRgb(204uy,176uy,136uy)), Opacity=X_OPACITY), true, n
                            elif n = MapStateProxy.NumStates then
                                upcast new Canvas(Width=5.*3., Height=9.*3., Background=new SolidColorBrush(Color.FromRgb(204uy,176uy,136uy))), true, -1
                            else
                                upcast Graphics.BMPtoImage(MapStateProxy(n).CurrentInteriorBMP()), (n = originalState) || TrackerModel.mapSquareChoiceDomain.CanAddUse(n), n
                            )
                        CustomComboBoxes.DoModalGridSelect(appMainCanvas, 0.+OMTW*float i, 150.+11.*3.*float j, tileCanvas,
                            originalStateIndex, ST, gridElementsSelectablesAndIDs, 8, 4, 5*3, 9*3, gridxPosition, 11.*3.+ST,
                            (fun (dismissPopup, _ea, currentState) ->
                                TrackerModel.overworldMapMarks.[i,j].Set(currentState)
                                redrawGridSpot()
                                dismissPopup()),
                            (fun (currentState) -> 
                                tileCanvas.Children.Clear()
                                canvasAdd(tileCanvas, tileImage, 0., 0.)
                                let bmp = MapStateProxy(currentState).CurrentBMP()
                                if bmp <> null then
                                    let icon = bmp |> Graphics.BMPtoImage |> resizeMapTileImage
                                    if MapStateProxy(currentState).IsX then
                                        icon.Opacity <- X_OPACITY
                                    canvasAdd(tileCanvas, icon, 0., 0.)))
                    elif ea.GetCurrentPoint(c).Properties.IsRightButtonPressed then 
                        // right click is the 'special interaction'
                        let MODULO = TrackerModel.MapSquareChoiceDomainHelper.NUM_ITEMS+1
                        let msp = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                        if msp.State = -1 then
                            // right click empty tile changes to 'X'
                            updateGridSpot -1 ""
                        if msp.IsThreeItemShop then
                            // right click a shop cycles down the second item
                            // next item
                            let e = (TrackerModel.getOverworldMapExtraData(i,j) - 1 + MODULO) % MODULO
                            // skip past duplicates
                            let item1 = msp.State - 15  // 1-based
                            let e = if e = item1 then (e - 1 + MODULO) % MODULO else e
                            TrackerModel.setOverworldMapExtraData(i,j,e)
                            // redraw
                            redrawGridSpot()
                    )
                c.PointerWheelChanged.Add(fun x -> updateGridSpot (if x.Delta.Y<0. then 1 else -1) "")
    canvasAdd(overworldCanvas, owMapGrid, 0., 0.)
    owMapGrid.PointerLeave.Add(fun _ ->
        if owGettableScreensCheckBox.IsChecked.HasValue && owGettableScreensCheckBox.IsChecked.Value then
            OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, 
                        TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), Point(0.,0.), 0, 0, false, 128)
        )

    let mutable mapMostRecentMousePos = Point(-1., -1.)
    owMapGrid.PointerLeave.Add(fun _ -> mapMostRecentMousePos <- Point(-1., -1.))
    owMapGrid.PointerMoved.Add(fun ea -> mapMostRecentMousePos <- ea.GetPosition(owMapGrid))

    let recorderingCanvas = new Canvas(Width=16.*OMTW, Height=float(8*11*3))  // really the 'extra top layer' canvas for adding final marks to overworld map
    recorderingCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(overworldCanvas, recorderingCanvas, 0., 0.)
    let startIcon = new Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=Brushes.Lime, StrokeThickness=3.0)

    let THRU_MAIN_MAP_H = float(150 + 8*11*3)

    // map legend
    let LEFT_OFFSET = 78.0
    let legendCanvas = new Canvas()
    canvasAdd(appMainCanvas, legendCanvas, LEFT_OFFSET, THRU_MAIN_MAP_H)

    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="The LEGEND\nof Z-Tracker", Padding=Thickness(0.))
    canvasAdd(appMainCanvas, tb, 0., THRU_MAIN_MAP_H)

    canvasAdd(legendCanvas, trimNumeralBmpToImage Graphics.uniqueMapIconBMPs.[0], 0., 0.)
    drawDungeonHighlight(legendCanvas,0.,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Active\nDungeon", Padding=Thickness(0.))
    canvasAdd(legendCanvas, tb, OMTW, 0.)

    canvasAdd(legendCanvas, trimNumeralBmpToImage Graphics.uniqueMapIconBMPs.[0], 2.5*OMTW, 0.)
    drawDungeonHighlight(legendCanvas,2.5,0)
    drawCompletedDungeonHighlight(legendCanvas,2.5,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Completed\nDungeon", Padding=Thickness(0.))
    canvasAdd(legendCanvas, tb, 3.5*OMTW, 0.)

    canvasAdd(legendCanvas, trimNumeralBmpToImage Graphics.uniqueMapIconBMPs.[0], 5.*OMTW, 0.)
    drawDungeonHighlight(legendCanvas,5.,0)
    drawDungeonRecorderWarpHighlight(legendCanvas,5.,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Recorder\nDestination", Padding=Thickness(0.))
    canvasAdd(legendCanvas, tb, 6.*OMTW, 0.)

    canvasAdd(legendCanvas, trimNumeralBmpToImage Graphics.uniqueMapIconBMPs.[9], 7.5*OMTW, 0.)
    drawWarpHighlight(legendCanvas,7.5,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Any Road\n(Warp)", Padding=Thickness(0.))
    canvasAdd(legendCanvas, tb, 8.5*OMTW, 0.)

    let legendStartIcon = new Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=Brushes.Lime, StrokeThickness=3.0)
    canvasAdd(legendCanvas, legendStartIcon, 10.*OMTW+8.5*OMTW/48., 0.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Start\nSpot", Padding=Thickness(0.))
    canvasAdd(legendCanvas, tb, 11.*OMTW, 0.)

    let THRU_MAP_AND_LEGEND_H = THRU_MAIN_MAP_H + float(11*3)

    // item progress
    let itemProgressCanvas = new Canvas(Width=16.*OMTW, Height=30.)
    itemProgressCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(appMainCanvas, itemProgressCanvas, 0., THRU_MAP_AND_LEGEND_H)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Item Progress", Padding=Thickness(0.))
    canvasAdd(appMainCanvas, tb, 38., THRU_MAP_AND_LEGEND_H + 4.)

    // Version
    let vb = new Button(Content=new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, BorderThickness=Thickness(0.), 
                            Text=sprintf "v%s" OverworldData.VersionString, IsReadOnly=true, IsHitTestVisible=false, Padding=Thickness(0.)),
                        BorderThickness=Thickness(1.), Margin=Thickness(0.), Padding=Thickness(0.))
    canvasAdd(appMainCanvas, vb, 0., THRU_MAP_AND_LEGEND_H + 4.)
    vb.Click.Add(fun _ ->
        let cmb = new CustomMessageBox.CustomMessageBox(OverworldData.AboutHeader, System.Drawing.SystemIcons.Information, OverworldData.AboutBody, ["Go to website"; "Ok"])
        async {
            let task = cmb.ShowDialog((Application.Current.ApplicationLifetime :?> ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime).MainWindow)
            do! Async.AwaitTask task
            if cmb.MessageBoxResult = "Go to website" then
                let cmd = (sprintf "xdg-open %s" OverworldData.Website).Replace("\"", "\\\"")
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(
                        FileName = "/bin/sh",
                        Arguments = sprintf "-c \"%s\"" cmd,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                    )) |> ignore
        } |> Async.StartImmediate
        )
    
    let hintGrid = makeGrid(3,OverworldData.hintMeanings.Length,160,36)
    let mutable row=0 
    for a,b in OverworldData.hintMeanings do
        let thisRow = row
        gridAdd(hintGrid, new TextBox(FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(1.), Text=a), 0, row)
        let tb = new TextBox(FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(1.), Text=b)
        let dp = new DockPanel(LastChildFill=true)
        let bmp = 
            if row < 8 then
                Graphics.emptyFoundTriforce_bmps.[row]
            elif row = 8 then
                Graphics.foundL9_bmp
            elif row = 9 then
                Graphics.white_sword_bmp
            else
                Graphics.magical_sword_bmp
        let image = Graphics.BMPtoImage bmp
        image.Width <- 32.
        image.Stretch <- Stretch.None
        let b = new Border(Child=image, BorderThickness=Thickness(1.), BorderBrush=Brushes.LightGray, Background=Brushes.Black)
        DockPanel.SetDock(b, Dock.Left)
        dp.Children.Add(b) |> ignore
        dp.Children.Add(tb) |> ignore
        gridAdd(hintGrid, dp, 1, row)
        let comboBox = new ComboBox(FontSize=14., MaxDropDownHeight=256.)
        comboBox.Items <- [| for i = 0 to 10 do yield TrackerModel.HintZone.FromIndex(i).ToString() |]
        comboBox.SelectedIndex <- 0
        comboBox.SelectionChanged.Add(fun _ -> 
            TrackerModel.levelHints.[thisRow] <- TrackerModel.HintZone.FromIndex(comboBox.SelectedIndex)
            if comboBox.SelectedIndex = 0 then
                b.Background <- Brushes.Black
            else
                b.Background <- hintHighlightBrush
            TrackerModel.forceUpdate()
            )
        gridAdd(hintGrid, comboBox, 2, row)
        row <- row + 1
    let hintBorder = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(4.), Background=Brushes.Black)
    hintBorder.Child <- hintGrid
    let tb = new Button(Content=new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), Text="Decode Hint"), 
                        BorderThickness=Thickness(1.), BorderBrush=Brushes.Gray, Padding=Thickness(0.))
    canvasAdd(appMainCanvas, tb, 496., THRU_MAP_AND_LEGEND_H + 4.)
    tb.Click.Add(fun _ -> CustomComboBoxes.DoModal(appMainCanvas, 0., THRU_MAP_AND_LEGEND_H + 4., hintBorder, fun()->()) |> ignore)

    let THRU_MAP_H = THRU_MAP_AND_LEGEND_H + 30.
    printfn "H thru item prog = %d" (int THRU_MAP_H)

    // WANT!
    let kitty = new Image()
    let imageStream = Graphics.GetResourceStream("CroppedBrianKitty.png")
    kitty.Source <- new Avalonia.Media.Imaging.Bitmap(imageStream)
    kitty.Width <- THRU_MAP_H - THRU_MAIN_MAP_H
    kitty.Height <- THRU_MAP_H - THRU_MAIN_MAP_H
    canvasAdd(appMainCanvas, kitty, 16.*OMTW - kitty.Width, THRU_MAIN_MAP_H)

    let blockerDungeonSunglasses : Visual[] = Array.zeroCreate 8
    let doUIUpdate() =
        if displayIsCurrentlyMirrored <> TrackerModel.Options.MirrorOverworld.Value then
            // model changed, align the view
            displayIsCurrentlyMirrored <- not displayIsCurrentlyMirrored
            if displayIsCurrentlyMirrored then
                for fe in mirrorOverworldFEs do
                    fe.RenderTransform <- new ScaleTransform(-1., 1.)
            else
                for fe in mirrorOverworldFEs do
                    fe.RenderTransform <- null
        // redraw triforce display (some may have located/unlocated/hinted)
        for i = 0 to 7 do
            updateTriforceDisplay(i)
        level9NumeralCanvas.Children.Clear()
        let l9found = TrackerModel.mapStateSummary.DungeonLocations.[8]<>TrackerModel.NOTFOUND 
        let img = Graphics.BMPtoImage(if not(l9found) then Graphics.unfoundL9_bmp else Graphics.foundL9_bmp)
        if not(l9found) && TrackerModel.levelHints.[8]<>TrackerModel.HintZone.UNKNOWN then
            canvasAdd(level9NumeralCanvas, makeHintHighlight(30.), 0., 0.)
        canvasAdd(level9NumeralCanvas, img, 0., 0.)
        // redraw white/magical swords (may have located/unlocated/hinted)
        redrawWhiteSwordCanvas()
        redrawMagicalSwordCanvas()

        recorderingCanvas.Children.Clear()
        RedrawForSecondQuestDungeonToggle()
        // TODO event for redraw item progress? does any of this event interface make sense? hmmm
        itemProgressCanvas.Children.Clear()
        let mutable x, y = 116., 3.
        let DX = 30.
        match TrackerModel.playerComputedStateSummary.SwordLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage (Graphics.greyscale Graphics.magical_sword_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.brown_sword_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.white_sword_bmp, x, y)
        | 3 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.magical_sword_bmp, x, y)
        | _ -> failwith "bad SwordLevel"
        x <- x + DX
        match TrackerModel.playerComputedStateSummary.CandleLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.red_candle_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.blue_candle_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.red_candle_bmp, x, y)
        | _ -> failwith "bad CandleLevel"
        x <- x + DX
        match TrackerModel.playerComputedStateSummary.RingLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.red_ring_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.blue_ring_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.red_ring_bmp, x, y)
        | _ -> failwith "bad RingLevel"
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveBow then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.bow_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.bow_bmp), x, y)
        x <- x + DX
        match TrackerModel.playerComputedStateSummary.ArrowLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.silver_arrow_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.wood_arrow_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.silver_arrow_bmp, x, y)
        | _ -> failwith "bad ArrowLevel"
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveWand then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.wand_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.wand_bmp), x, y)
        x <- x + DX
        if !isCurrentlyBook then
            // book seed
            if TrackerModel.playerComputedStateSummary.HaveBookOrShield then
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.book_bmp, x, y)
            else
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.book_bmp), x, y)
        else
            // boomstick seed
            if TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Value() then
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.boom_book_bmp, x, y)
            else
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.boom_book_bmp), x, y)
        x <- x + DX
        match TrackerModel.playerComputedStateSummary.BoomerangLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.magic_boomerang_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.boomerang_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.magic_boomerang_bmp, x, y)
        | _ -> failwith "bad BoomerangLevel"
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveLadder then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.ladder_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.ladder_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveRecorder then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.recorder_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.recorder_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HavePowerBracelet then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.power_bracelet_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.power_bracelet_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveRaft then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.raft_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.raft_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveAnyKey then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.key_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.key_bmp), x, y)
        // place start icon in top layer
        if TrackerModel.startIconX <> -1 then
            canvasAdd(recorderingCanvas, startIcon, 11.5*OMTW/48.-3.+OMTW*float(TrackerModel.startIconX), float(TrackerModel.startIconY*11*3))
        TrackerModel.allUIEventingLogic( {new TrackerModel.ITrackerEvents with
            member _this.CurrentHearts(h) = currentHeartsTextBox.Text <- sprintf "Current Hearts: %d" h
            member _this.AnnounceConsiderSword2() = ()
            member _this.AnnounceConsiderSword3() = ()
            member _this.OverworldSpotsRemaining(remain,gettable) =
                owRemainingScreensTextBox.Text <- sprintf "%d OW spots left" remain
                owGettableScreensTextBox.Text <- sprintf "Show %d gettable" gettable
            member _this.DungeonLocation(i,x,y,hasTri,isCompleted) =
                if isCompleted then
                    drawCompletedDungeonHighlight(recorderingCanvas,float x,y)
                // highlight any triforce dungeons as recorder warp destinations
                if TrackerModel.playerComputedStateSummary.HaveRecorder && hasTri then
                    drawDungeonRecorderWarpHighlight(recorderingCanvas,float x,y)
            member _this.AnyRoadLocation(i,x,y) = ()
            member _this.WhistleableLocation(x,y) = ()
            member _this.Sword3(x,y) =
                if TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value() then
                    drawCompletedIconHighlight(recorderingCanvas,float x,y)  // darken a gotten magic sword cave icon
            member _this.Sword2(x,y) =
                if (TrackerModel.sword2Box.PlayerHas()=TrackerModel.PlayerHas.NO) && TrackerModel.sword2Box.CellCurrent() <> -1 then
                    // display known-but-ungotten item on the map
                    let itemImage = Graphics.BMPtoImage Graphics.allItemBMPsWithHeartShuffle.[TrackerModel.sword2Box.CellCurrent()]
                    if displayIsCurrentlyMirrored then 
                        itemImage.RenderTransform <- new ScaleTransform(-1., 1.)
                    itemImage.Opacity <- 1.0
                    itemImage.Width <- OMTW/2.
                    let color = Brushes.Black
                    let border = new Border(BorderThickness=Thickness(1.), BorderBrush=color, Background=color, Child=itemImage, Opacity=0.5)
                    let diff = if displayIsCurrentlyMirrored then 0. else OMTW/2.
                    canvasAdd(recorderingCanvas, border, OMTW*float(x)+diff, float(y*11*3)+4.)
                if TrackerModel.sword2Box.PlayerHas() <> TrackerModel.PlayerHas.NO then
                    drawCompletedIconHighlight(recorderingCanvas,float x,y)  // darken a gotten white sword item cave icon
            member _this.CoastItem() =
                if (TrackerModel.ladderBox.PlayerHas()=TrackerModel.PlayerHas.NO) && TrackerModel.ladderBox.CellCurrent() <> -1 then
                    // display known-but-ungotten item on the map
                    let x,y = 15,5
                    let itemImage = Graphics.BMPtoImage Graphics.allItemBMPsWithHeartShuffle.[TrackerModel.ladderBox.CellCurrent()]
                    itemImage.Opacity <- 1.0
                    itemImage.Width <- OMTW/2.2
                    let color = Brushes.Black
                    let border = new Border(BorderThickness=Thickness(3.), BorderBrush=color, Background=color, Child=itemImage, Opacity=0.6)
                    canvasAdd(recorderingCanvas, border, OMTW*float(x)+OMTW/2., float(y*11*3)+1.)
            member _this.RoutingInfo(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations,owRouteworthySpots) = 
                // clear and redraw routing
                routeDrawingCanvas.Children.Clear()
                OverworldRouting.repopulate(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations)
                let pos = mapMostRecentMousePos
                let i,j = int(Math.Floor(pos.X / OMTW)), int(Math.Floor(pos.Y / (11.*3.)))
                if i>=0 && i<16 && j>=0 && j<8 then
                    drawRoutesTo(routeDrawingCanvas, Point(0.,0.), i, j, 
                                 TrackerModel.Options.Overworld.DrawRoutes.Value, if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxYGH else 0)
                elif owGettableScreensCheckBox.IsChecked.HasValue && owGettableScreensCheckBox.IsChecked.Value then
                    // unexplored but gettable spots highlight
                    OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, 
                                                        TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), Point(0.,0.), 0, 0, false, 128)
            member _this.AnnounceCompletedDungeon(i) = ()
            member _this.CompletedDungeons(a) =
                for i = 0 to 7 do
                    // top ui
                    for j = 0 to 4 do
                        mainTrackerCanvases.[i,j].Children.Remove(mainTrackerCanvasShaders.[i,j]) |> ignore
                    if a.[i] then
                        for j = 0 to 4 do
                            mainTrackerCanvases.[i,j].Children.Add(mainTrackerCanvasShaders.[i,j]) |> ignore
                    // blockers ui
                    if a.[i] then
                        blockerDungeonSunglasses.[i].Opacity <- 0.5
                    else
                        blockerDungeonSunglasses.[i].Opacity <- 1.
            member _this.AnnounceFoundDungeonCount(n) = ()
            member _this.AnnounceTriforceCount(n) = ()
            member _this.AnnounceTriforceAndGo(triforces, tagLevel) = ()
            member _this.RemindUnblock(blockerType, dungeons, detail) = ()
            member _this.RemindShortly(itemId) = ()
            })
    let threshold = TimeSpan.FromMilliseconds(500.0)
    let mutable ladderTime, recorderTime, powerBraceletTime = DateTime.Now, DateTime.Now, DateTime.Now
    let mutable owPreviouslyAnnouncedWhistleSpotsRemain, owPreviouslyAnnouncedPowerBraceletSpotsRemain = 0, 0
    let timer = new Threading.DispatcherTimer()
    timer.Interval <- TimeSpan.FromSeconds(1.0)
    timer.Tick.Add(fun _ -> 
        let hasUISettledDown = 
            DateTime.Now - TrackerModel.playerProgressLastChangedTime > threshold &&
            DateTime.Now - TrackerModel.dungeonsAndBoxesLastChangedTime > threshold &&
            DateTime.Now - TrackerModel.mapLastChangedTime > threshold
        if hasUISettledDown then
            let hasTheModelChanged = TrackerModel.recomputeWhatIsNeeded()  
            if hasTheModelChanged then
                doUIUpdate()
        )
    timer.Start()

    // timeline & options menu
    let moreOptionsLabel = new TextBox(Text="Options...", Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Margin=Thickness(0.), Padding=Thickness(0.), BorderThickness=Thickness(0.), IsReadOnly=true, IsHitTestVisible=false)
    let moreOptionsButton = new Button(MaxHeight=25., Content=moreOptionsLabel, BorderThickness=Thickness(1.), Margin=Thickness(0.), Padding=Thickness(0.))
    let optionsCanvas = new Border(Child=OptionsMenu.makeOptionsCanvas(25.),
                                   Background=SolidColorBrush(0xFF282828u), BorderBrush=Brushes.Gray, BorderThickness=Thickness(2.),
                                   ZIndex=111, IsVisible=true)
    moreOptionsButton.ZIndex <- optionsCanvas.ZIndex+1

    let theTimeline1 = new Timeline.Timeline(21., 4, 60, 5, appMainCanvas.Width-10., 1, "0h", "30m", "1h", 53.)
    let theTimeline2 = new Timeline.Timeline(21., 4, 60, 5, appMainCanvas.Width-10., 2, "0h", "1h", "2h", 53.)
    let theTimeline3 = new Timeline.Timeline(21., 4, 60, 5, appMainCanvas.Width-10., 3, "0h", "1.5h", "3h", 53.)
    theTimeline1.Canvas.Opacity <- 1.
    theTimeline2.Canvas.Opacity <- 0.
    theTimeline3.Canvas.Opacity <- 0.
    let updateTimeline(minute) =
        if minute <= 60 then
            theTimeline1.Canvas.Opacity <- 1.
            theTimeline2.Canvas.Opacity <- 0.
            theTimeline3.Canvas.Opacity <- 0.
            theTimeline1.Update(minute, timelineItems)
        elif minute <= 120 then
            theTimeline1.Canvas.Opacity <- 0.
            theTimeline2.Canvas.Opacity <- 1.
            theTimeline3.Canvas.Opacity <- 0.
            theTimeline2.Update(minute, timelineItems)
        else
            theTimeline1.Canvas.Opacity <- 0.
            theTimeline2.Canvas.Opacity <- 0.
            theTimeline3.Canvas.Opacity <- 1.
            theTimeline3.Update(minute, timelineItems)
    canvasAdd(appMainCanvas, theTimeline1.Canvas, 5., THRU_MAP_H)
    canvasAdd(appMainCanvas, theTimeline2.Canvas, 5., THRU_MAP_H)
    canvasAdd(appMainCanvas, theTimeline3.Canvas, 5., THRU_MAP_H)

    canvasAdd(appMainCanvas, moreOptionsButton, 0., THRU_MAP_H)
    moreOptionsButton.Click.Add(fun _ -> CustomComboBoxes.DoModal(appMainCanvas, 0., THRU_MAP_H, optionsCanvas, (fun () -> TrackerModel.Options.writeSettings())) |> ignore)

    let THRU_TIMELINE_H = THRU_MAP_H + float TCH + 6.

    // Dungeon level trackers
    let fixedDungeon1Outlines = ResizeArray()
    let fixedDungeon2Outlines = ResizeArray()

    let grabHelper = new Dungeon.GrabHelper()
    let grabModeTextBlock = 
        new Border(BorderThickness=Thickness(2.), BorderBrush=Brushes.LightGray, 
                   Child=new TextBlock(TextWrapping=TextWrapping.Wrap, FontSize=12., Foreground=Brushes.Black, Background=Brushes.Gray, IsHitTestVisible=false,
                                        Text="You are now in 'grab mode', which can be used to move an entire segment of dungeon rooms and doors at once.\n\nTo abort grab mode, click again on 'GRAB' in the upper right of the dungeon tracker.\n\nTo move a segment, first click any marked room, to pick up that room and all contiguous rooms.  Then click again on a new location to 'drop' the segment you grabbed.  After grabbing, hovering the mouse shows a preview of where you would drop.  This behaves like 'cut and paste', and adjacent doors will come along for the ride.\n\nUpon completion, you will be prompted to keep changes or undo them, so you can experiment.")
        )
    let dungeonTabs = new TabControl()
    dungeonTabs.Background <- Brushes.Black 
    canvasAdd(appMainCanvas, dungeonTabs , 0., THRU_TIMELINE_H)
    let tabItems = ResizeArray()
    for level = 1 to 9 do
        let levelTab = new TabItem(Background=Brushes.SlateGray, Foreground=Brushes.Black, Height=float(TH))
        levelTab.FontSize <- 16.
        levelTab.FontWeight <- FontWeight.Bold
        levelTab.VerticalContentAlignment <- Layout.VerticalAlignment.Center
        levelTab.Margin <- Thickness(1., 0.)
        levelTab.Padding <- Thickness(0.)
        levelTab.Header <- sprintf "  %d  " level
        let contentCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7), Background=Brushes.Black)
        contentCanvas.PointerEnter.Add(fun _ -> 
            let i,j = TrackerModel.mapStateSummary.DungeonLocations.[level-1]
            if (i,j) <> TrackerModel.NOTFOUND then
                // when mouse in a dungeon map, show its location...
                showLocatorExactLocation(TrackerModel.mapStateSummary.DungeonLocations.[level-1])
                // ...and behave like we are moused there
                drawRoutesTo(routeDrawingCanvas, Point(), i, j, TrackerModel.Options.Overworld.DrawRoutes.Value, if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxYGH else 0)
            )
        contentCanvas.PointerLeave.Add(fun ea -> hideLocator())
        let dungeonCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7))  // draw e.g. rooms here
        let dungeonSourceHighlightCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7))  // draw grab-source highlights here
        let dungeonHighlightCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7))  // draw grab highlights here
        canvasAdd(contentCanvas, dungeonCanvas, 0., 0.)
        canvasAdd(contentCanvas, dungeonSourceHighlightCanvas, 0., 0.)
        canvasAdd(contentCanvas, dungeonHighlightCanvas, 0., 0.)

        levelTab.Content <- contentCanvas
        dungeonTabs.Height <- dungeonCanvas.Height + 30.   // ok to set this 9 times
        tabItems.Add(levelTab)

        let TEXT = sprintf "LEVEL-%d " level
        // horizontal doors
        let unknown = Dungeon.unknown
        let no = Dungeon.no
        let yes = Dungeon.yes
        let blackedOut = Dungeon.blackedOut
        let horizontalDoorCanvases = Array2D.zeroCreate 7 8
        for i = 0 to 6 do
            for j = 0 to 7 do
                let d = new Canvas(Height=12., Width=12., Background=unknown)
                horizontalDoorCanvases.[i,j] <- d
                canvasAdd(dungeonCanvas, d, float(i*(39+12)+39), float(TH+j*(27+12)+8))
                let left _ =        
                    if not grabHelper.IsGrabMode then  // cannot interact with doors in grab mode
                        if not(obj.Equals(d.Background, yes)) then
                            d.Background <- yes
                        else
                            d.Background <- unknown
                let right _ = 
                    if not grabHelper.IsGrabMode then  // cannot interact with doors in grab mode
                        if not(obj.Equals(d.Background, no)) then
                            d.Background <- no
                        else
                            d.Background <- unknown
                d.PointerPressed.Add(fun ea -> 
                    if ea.GetCurrentPoint(appMainCanvas).Properties.IsLeftButtonPressed then (left())
                    elif ea.GetCurrentPoint(appMainCanvas).Properties.IsRightButtonPressed then (right()))
        // vertical doors
        let verticalDoorCanvases = Array2D.zeroCreate 8 7
        for i = 0 to 7 do
            for j = 0 to 6 do
                let d = new Canvas(Height=12., Width=12., Background=unknown)
                verticalDoorCanvases.[i,j] <- d
                canvasAdd(dungeonCanvas, d, float(i*(39+12)+14), float(TH+j*(27+12)+27))
                let left _ =
                    if not grabHelper.IsGrabMode then  // cannot interact with doors in grab mode
                        if not(obj.Equals(d.Background, yes)) then
                            d.Background <- yes
                        else
                            d.Background <- unknown
                let right _ = 
                    if not grabHelper.IsGrabMode then  // cannot interact with doors in grab mode
                        if not(obj.Equals(d.Background, no)) then
                            d.Background <- no
                        else
                            d.Background <- unknown
                d.PointerPressed.Add(fun ea -> 
                    if ea.GetCurrentPoint(appMainCanvas).Properties.IsLeftButtonPressed then (left())
                    elif ea.GetCurrentPoint(appMainCanvas).Properties.IsRightButtonPressed then (right()))
        // rooms
        let roomCanvases = Array2D.zeroCreate 8 8 
        let roomStates = Array2D.zeroCreate 8 8 // 1-9 = transports, see redraw() below for rest
        let roomIsCircled = Array2D.zeroCreate 8 8
        let roomCompleted = Array2D.zeroCreate 8 8 
        let ROOMS = 26 // how many types
        let usedTransports = Array.zeroCreate 10 // slot 0 unused
        let roomRedrawFuncs = ResizeArray()
        let redrawAllRooms() =
            for f in roomRedrawFuncs do
                f()
        let mutable grabRedraw = fun () -> ()
        for i = 0 to 7 do
            if i=7 then
                let tb = new TextBox(Width=float(13*3), Height=float(TH), FontSize=float(TH-12), Foreground=Brushes.Gray, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=true,
                                        Text="GRAB", BorderThickness=Thickness(0.))
                canvasAdd(dungeonCanvas, tb, float(i*51)+1., 0.)
                grabRedraw <- (fun () ->
                    if grabHelper.IsGrabMode then
                        tb.Foreground <- Brushes.White
                        tb.Background <- Brushes.Red
                        //dungeonTabs.Cursor <- Avalonia.Input.Cursor.Default // TODO other cursor?
                        grabModeTextBlock.Opacity <- 1.
                    else
                        grabHelper.Abort()
                        dungeonHighlightCanvas.Children.Clear()
                        dungeonSourceHighlightCanvas.Children.Clear()
                        tb.Foreground <- Brushes.Gray
                        tb.Background <- Brushes.Black
                        //dungeonTabs.Cursor <- null
                        grabModeTextBlock.Opacity <- 0.
                    )
                tb.AddHandler<_>(Avalonia.Input.InputElement.PointerPressedEvent, new EventHandler<_>(fun _ _ ->
                        grabHelper.ToggleGrabMode()
                        grabRedraw()
                    ), Avalonia.Interactivity.RoutingStrategies.Tunnel)
            else
                // LEVEL-9        
                let tb = new TextBox(Width=float(13*3), Height=float(TH), FontSize=float(TH-12), Foreground=Brushes.White, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false,
                                        Text=TEXT.Substring(i,1), BorderThickness=Thickness(0.), FontFamily=new FontFamily("Courier New"), FontWeight=FontWeight.Bold)
                canvasAdd(dungeonCanvas, tb, float(i*51)+12., 0.)
            // room map
            for j = 0 to 7 do
                let c = new Canvas(Width=float(13*3), Height=float(9*3))
                canvasAdd(dungeonCanvas, c, float(i*51), float(TH+j*39))
                let image = Graphics.BMPtoImage (fst Graphics.cdungeonUnexploredRoomBMP)
                canvasAdd(c, image, 0., 0.)
                roomCanvases.[i,j] <- c
                roomStates.[i,j] <- 0
                roomIsCircled.[i,j] <- false
                let redraw() =
                    c.Children.Clear()
                    let image =
                        match roomStates.[i,j] with
                        | 0  -> Graphics.cdungeonUnexploredRoomBMP 
                        | 10 -> (snd Graphics.cdungeonUnexploredRoomBMP), (snd Graphics.cdungeonUnexploredRoomBMP)
                        | 11 -> Graphics.cdungeonDoubleMoatBMP
                        | 12 -> Graphics.cdungeonChevyBMP
                        | 13 -> Graphics.cdungeonVMoatBMP
                        | 14 -> Graphics.cdungeonHMoatBMP
                        | 15 -> Graphics.cdungeonVChuteBMP
                        | 16 -> Graphics.cdungeonHChuteBMP
                        | 17 -> Graphics.cdungeonTeeBMP
                        | 18 -> Graphics.cdungeonNeedWand
                        | 19 -> Graphics.cdungeonBlueBubble
                        | 20 -> Graphics.cdungeonNeedRecorder
                        | 21 -> Graphics.cdungeonNeedBow
                        | 22 -> Graphics.cdungeonTriforceBMP 
                        | 23 -> Graphics.cdungeonPrincessBMP 
                        | 24 -> Graphics.cdungeonStartBMP 
                        | 25 -> Graphics.cdungeonExploredRoomBMP 
                        | n  -> Graphics.cdungeonNumberBMPs.[n-1]
                        |> (fun (u,c) -> if roomStates.[i,j] = 0 then u elif roomCompleted.[i,j] then c else u)
                        |> Graphics.BMPtoImage 
                    canvasAdd(c, image, 0., 0.)
                    if roomIsCircled.[i,j] then
                        let ellipse = new Shapes.Ellipse(Width=float(13*3+12), Height=float(9*3+12), Stroke=Brushes.Yellow, StrokeThickness=3.)
                        canvasAdd(c, ellipse, -6., -6.)
                roomRedrawFuncs.Add(fun () -> redraw())
                let f b =
                    // track transport being changed away from
                    if [1..9] |> List.contains roomStates.[i,j] then
                        usedTransports.[roomStates.[i,j]] <- usedTransports.[roomStates.[i,j]] - 1
                    // go to next state
                    roomStates.[i,j] <- ((roomStates.[i,j] + (if b then 1 else -1)) + ROOMS) % ROOMS
                    // skip transport if already used both; also skip state 10 (blackedOut)
                    while [1..9] |> List.contains roomStates.[i,j] && usedTransports.[roomStates.[i,j]] = 2 || roomStates.[i,j]=10 do
                        roomStates.[i,j] <- ((roomStates.[i,j] + (if b then 1 else -1)) + ROOMS) % ROOMS
                    // note any new transports
                    if [1..9] |> List.contains roomStates.[i,j] then
                        usedTransports.[roomStates.[i,j]] <- usedTransports.[roomStates.[i,j]] + 1
                    redraw()
                let BUFFER = 2.
                let highlightImpl(canvas,contiguous:_[,], brush) =
                    for x = 0 to 7 do
                        for y = 0 to 7 do
                            if contiguous.[x,y] then
                                let r = new Shapes.Rectangle(Width=float(13*3 + 12), Height=float(9*3 + 12), Fill=brush, Opacity=0.4, IsHitTestVisible=false)  // TODO creating lots of garbage
                                canvasAdd(canvas, r, float(x*51 - 6), float(TH+y*39 - 6))
                let highlight(contiguous:_[,], brush) = highlightImpl(dungeonHighlightCanvas,contiguous,brush)
                c.PointerEnter.Add(fun _ ->
                    if grabHelper.IsGrabMode then
                        if not grabHelper.HasGrab then
                            if roomStates.[i,j] <> 0 && roomStates.[i,j] <> 10 then
                                dungeonHighlightCanvas.Children.Clear() // clear old preview
                                let contiguous = grabHelper.PreviewGrab(i,j,roomStates)
                                highlight(contiguous, Brushes.Lime)
                        else
                            dungeonHighlightCanvas.Children.Clear() // clear old preview
                            let ok,warn = grabHelper.PreviewDrop(i,j,roomStates)
                            highlight(ok, Brushes.Lime)
                            highlight(warn, Brushes.Yellow)
                    )
                c.PointerLeave.Add(fun _ ->
                    if grabHelper.IsGrabMode then
                        dungeonHighlightCanvas.Children.Clear() // clear old preview
                    )
                c.PointerPressed.Add(fun ea -> 
                    if ea.GetCurrentPoint(c).Properties.IsLeftButtonPressed then
                        if grabHelper.IsGrabMode then
                            if not grabHelper.HasGrab then
                                if roomStates.[i,j] <> 0 && roomStates.[i,j] <> 10 then
                                    dungeonHighlightCanvas.Children.Clear() // clear preview
                                    let contiguous = grabHelper.StartGrab(i,j,roomStates,roomIsCircled,roomCompleted,horizontalDoorCanvases,verticalDoorCanvases)
                                    highlightImpl(dungeonSourceHighlightCanvas, contiguous, Brushes.Pink)  // this highlight stays around until completed/aborted
                                    highlight(contiguous, Brushes.Lime)
                            else
                                let backupRoomStates = roomStates.Clone() :?> int[,]
                                let backupRoomIsCircled = roomIsCircled.Clone() :?> bool[,]
                                let backupRoomCompleted = roomCompleted.Clone() :?> bool[,]
                                let backupHorizontalDoors = horizontalDoorCanvases |> Array2D.map (fun c -> c.Background)
                                let backupVerticalDoors = verticalDoorCanvases |> Array2D.map (fun c -> c.Background)
                                grabHelper.DoDrop(i,j,roomStates,roomIsCircled,roomCompleted,horizontalDoorCanvases,verticalDoorCanvases)
                                redrawAllRooms()  // make updated changes visual
                                let cmb = new CustomMessageBox.CustomMessageBox("Verify changes", System.Drawing.SystemIcons.Question, "You moved a dungeon segment. Keep this change?", ["Keep changes"; "Undo"])
                                async {
                                    let task = cmb.ShowDialog((Application.Current.ApplicationLifetime :?> ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime).MainWindow)
                                    do! Async.AwaitTask task
                                    grabRedraw()  // DoDrop completes the grab, neeed to update the visual
                                    if cmb.MessageBoxResult = null || cmb.MessageBoxResult = "Undo" then
                                        // copy back from old state
                                        backupRoomStates |> Array2D.iteri (fun x y v -> roomStates.[x,y] <- v)
                                        backupRoomIsCircled |> Array2D.iteri (fun x y v -> roomIsCircled.[x,y] <- v)
                                        backupRoomCompleted |> Array2D.iteri (fun x y v -> roomCompleted.[x,y] <- v)
                                        redrawAllRooms()  // make reverted changes visual
                                        horizontalDoorCanvases |> Array2D.iteri (fun x y c -> c.Background <- backupHorizontalDoors.[x,y])
                                        verticalDoorCanvases |> Array2D.iteri (fun x y c -> c.Background <- backupVerticalDoors.[x,y])
                                } |> Async.StartImmediate
                        else
                            let pos = ea.GetPosition(c)
                            if pos.X < BUFFER || pos.X > c.Width-BUFFER || pos.Y < BUFFER || pos.Y > c.Height-BUFFER then
                                () // do nothing, as I often accidentally click room when trying to target doors with mouse
                            else
                                if ea.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift) then
                                    // shift click an unexplored room to mark not-on-map rooms (by "blackedOut"ing all the connections)
                                    if roomStates.[i,j] = 0 then
                                        if i > 0 then
                                            horizontalDoorCanvases.[i-1,j].Background <- blackedOut
                                        if i < 7 then
                                            horizontalDoorCanvases.[i,j].Background <- blackedOut
                                        if j > 0 then
                                            verticalDoorCanvases.[i,j-1].Background <- blackedOut
                                        if j < 7 then
                                            verticalDoorCanvases.[i,j].Background <- blackedOut
                                        roomStates.[i,j] <- 10
                                        roomCompleted.[i,j] <- true
                                        redraw()
                                    // shift click a blackedOut room to undo it back to unknown
                                    elif roomStates.[i,j] = 10 then
                                        if i > 0 && obj.Equals(horizontalDoorCanvases.[i-1,j].Background,blackedOut) then
                                            horizontalDoorCanvases.[i-1,j].Background <- unknown
                                        if i < 7 && obj.Equals(horizontalDoorCanvases.[i,j].Background,blackedOut) then
                                            horizontalDoorCanvases.[i,j].Background <- unknown
                                        if j > 0 && obj.Equals(verticalDoorCanvases.[i,j-1].Background,blackedOut) then
                                            verticalDoorCanvases.[i,j-1].Background <- unknown
                                        if j < 7 && obj.Equals(verticalDoorCanvases.[i,j].Background,blackedOut) then
                                            verticalDoorCanvases.[i,j].Background <- unknown
                                        roomStates.[i,j] <- 0
                                        roomCompleted.[i,j] <- false
                                        redraw()
                                else
                                    if roomStates.[i,j] <> 0 then
                                        roomCompleted.[i,j] <- not roomCompleted.[i,j]
                                    else
                                        // ad hoc useful gesture for clicking unknown room - it moves it to explored & completed state in a single click
                                        roomStates.[i,j] <- ROOMS-1
                                        roomCompleted.[i,j] <- true
                                    redraw()
                    elif ea.GetCurrentPoint(c).Properties.IsRightButtonPressed then
                        if not grabHelper.IsGrabMode then  // cannot right click rooms in grab mode
                            let pos = ea.GetPosition(c)
                            if pos.X < BUFFER || pos.X > c.Width-BUFFER || pos.Y < BUFFER || pos.Y > c.Height-BUFFER then
                                () // do nothing, as I often accidentally click room when trying to target doors with mouse
                            else
                                if roomStates.[i,j] = 0 then
                                    // ad hoc useful gesture for right-clicking unknown room - it moves it to explored & uncompleted state in a single click
                                    roomStates.[i,j] <- ROOMS-1
                                    roomCompleted.[i,j] <- false
                                    redraw()
                    elif ea.GetCurrentPoint(c).Properties.IsMiddleButtonPressed then
                        // middle click toggles roomIsCircled
                        if not grabHelper.IsGrabMode then  // cannot middle click rooms in grab mode
                            let pos = ea.GetPosition(c)
                            if pos.X < BUFFER || pos.X > c.Width-BUFFER || pos.Y < BUFFER || pos.Y > c.Height-BUFFER then
                                () // do nothing, as I often accidentally click room when trying to target doors with mouse
                            else
                                roomIsCircled.[i,j] <- not roomIsCircled.[i,j]
                                redraw()
                    )
                c.PointerWheelChanged.Add(fun x -> 
                    if not grabHelper.IsGrabMode then  // cannot scroll rooms in grab mode
                        f (x.Delta.Y<0.))
                // drag and drop to quickly 'paint' rooms
                c.PointerPressed.Add(fun ea ->
                    if not grabHelper.IsGrabMode then  // cannot initiate a drag in grab mode
                        if ea.GetCurrentPoint(c).Properties.IsLeftButtonPressed then
                            let o = new Avalonia.Input.DataObject()
                            o.Set(Avalonia.Input.DataFormats.Text,"L")
                            Avalonia.Input.DragDrop.DoDragDrop(ea, o, Avalonia.Input.DragDropEffects.Link) |> ignore
                        elif ea.GetCurrentPoint(c).Properties.IsRightButtonPressed then
                            let o = new Avalonia.Input.DataObject()
                            o.Set(Avalonia.Input.DataFormats.Text,"R")
                            Avalonia.Input.DragDrop.DoDragDrop(ea, o, Avalonia.Input.DragDropEffects.Link) |> ignore
                    )
                c.AddHandler<_>(Avalonia.Input.DragDrop.DropEvent, new EventHandler<_>(fun o ea -> ()))
                c.AddHandler<_>(Avalonia.Input.DragDrop.DragOverEvent, new EventHandler<_>(fun o ea ->
                    if roomStates.[i,j] = 0 then
                        if ea.Data.GetText() = "L" then
                            roomStates.[i,j] <- ROOMS-1
                            roomCompleted.[i,j] <- true
                        else
                            roomStates.[i,j] <- ROOMS-1
                            roomCompleted.[i,j] <- false
                        redraw()
                    ))
                Avalonia.Input.DragDrop.SetAllowDrop(c, true)
        for quest,outlines in [| (DungeonData.firstQuest.[level-1], fixedDungeon1Outlines); (DungeonData.secondQuest.[level-1], fixedDungeon2Outlines) |] do
            // fixed dungeon drawing outlines - vertical segments
            for i = 0 to 6 do
                for j = 0 to 7 do
                    if quest.[j].Chars(i) <> quest.[j].Chars(i+1) then
                        let s = new Shapes.Line(StartPoint=Point(float(i*(39+12)+39+12/2), float(TH+j*(27+12)-12/2)), EndPoint=Point(float(i*(39+12)+39+12/2), float(TH+j*(27+12)+27+12/2)), 
                                        Stroke=Brushes.Red, StrokeThickness=3., IsHitTestVisible=false, Opacity=0.0)
                        canvasAdd(dungeonCanvas, s, 0., 0.)
                        outlines.Add(s)
            // fixed dungeon drawing outlines - horizontal segments
            for i = 0 to 7 do
                for j = 0 to 6 do
                    if quest.[j].Chars(i) <> quest.[j+1].Chars(i) then
                        let s = new Shapes.Line(StartPoint=Point(float(i*(39+12)-12/2), float(TH+(j+1)*(27+12)-12/2)), EndPoint=Point(float(i*(39+12)+39+12/2), float(TH+(j+1)*(27+12)-12/2)), 
                                        Stroke=Brushes.Red, StrokeThickness=3., IsHitTestVisible=false, Opacity=0.0)
                        canvasAdd(dungeonCanvas, s, 0., 0.)
                        outlines.Add(s)
    dungeonTabs.Items <- tabItems
    dungeonTabs.SelectionChanged.Add(fun _ ->
        for i = 0 to 8 do
            if dungeonTabs.SelectedIndex = i then
                tabItems.[i].Background <- Brushes.DarkSlateGray
            else
                tabItems.[i].Background <- Brushes.SlateGray
        )
    dungeonTabs.SelectedIndex <- 8

    let fqcb = new CheckBox(Content=new TextBox(Text="FQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    ToolTip.SetTip(fqcb, "Show vanilla first quest dungeon outlines")
    let sqcb = new CheckBox(Content=new TextBox(Text="SQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    ToolTip.SetTip(sqcb, "Show vanilla second quest dungeon outlines")

    fqcb.IsChecked <- System.Nullable.op_Implicit false
    fqcb.Checked.Add(fun _ -> fixedDungeon1Outlines |> Seq.iter (fun s -> s.Opacity <- 1.0); sqcb.IsChecked <- System.Nullable.op_Implicit false)
    fqcb.Unchecked.Add(fun _ -> fixedDungeon1Outlines |> Seq.iter (fun s -> s.Opacity <- 0.0))
    canvasAdd(appMainCanvas, fqcb, 310., THRU_TIMELINE_H) 

    sqcb.IsChecked <- System.Nullable.op_Implicit false
    sqcb.Checked.Add(fun _ -> fixedDungeon2Outlines |> Seq.iter (fun s -> s.Opacity <- 1.0); fqcb.IsChecked <- System.Nullable.op_Implicit false)
    sqcb.Unchecked.Add(fun _ -> fixedDungeon2Outlines |> Seq.iter (fun s -> s.Opacity <- 0.0))
    canvasAdd(appMainCanvas, sqcb, 360., THRU_TIMELINE_H) 

    canvasAdd(appMainCanvas, dungeonTabsOverlay, 0., THRU_TIMELINE_H)

    // blockers
    let blockerCurrentBMP(current) =
        match current with
        | TrackerModel.DungeonBlocker.COMBAT -> Graphics.white_sword_bmp
        | TrackerModel.DungeonBlocker.BOW_AND_ARROW -> Graphics.bow_and_arrow_bmp
        | TrackerModel.DungeonBlocker.RECORDER -> Graphics.recorder_bmp
        | TrackerModel.DungeonBlocker.LADDER -> Graphics.ladder_bmp
        | TrackerModel.DungeonBlocker.BAIT -> Graphics.bait_bmp
        | TrackerModel.DungeonBlocker.KEY -> Graphics.key_bmp
        | TrackerModel.DungeonBlocker.BOMB -> Graphics.bomb_bmp
        | TrackerModel.DungeonBlocker.NOTHING -> null

    let makeBlockerBox(dungeonNumber, blockerIndex) =
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black, IsHitTestVisible=true)
        let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=Brushes.Gray, StrokeThickness=3.0, IsHitTestVisible=false)
        c.Children.Add(rect) |> ignore
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent, IsHitTestVisible=false)  // just has item drawn on it, not the box
        c.Children.Add(innerc) |> ignore
        let mutable current = TrackerModel.DungeonBlocker.NOTHING
        let redraw(n) =
            innerc.Children.Clear()
            let bmp = blockerCurrentBMP(n)
            if bmp <> null then
                let image = Graphics.BMPtoImage(bmp)
                image.IsHitTestVisible <- false
                canvasAdd(innerc, image, 4., 4.)
        redraw(current)
        c.PointerWheelChanged.Add(fun x -> 
            if x.Delta.Y<0. then
                current <- current.Next()
            else
                current <- current.Prev()
            redraw(current)
            TrackerModel.dungeonBlockers.[dungeonNumber, blockerIndex] <- current
        )
        c

    let blockerColumnWidth = int((appMainCanvas.Width-402.)/3.)
    let blockerGrid = makeGrid(3, 3, blockerColumnWidth, 36)
    blockerGrid.Height <- float(36*3)
    for i = 0 to 2 do
        for j = 0 to 2 do
            if i=0 && j=0 then
                let d = new DockPanel(LastChildFill=false)
                let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text="BLOCKERS", Width=float blockerColumnWidth,
                                        VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), TextAlignment=TextAlignment.Center)
                ToolTip.SetTip(tb, "The icons you set in this area can remind you of what blocked you in a dungeon.\nFor example, a ladder represents being ladder blocked, or a sword means you need better weapons.")
                d.Children.Add(tb) |> ignore
                gridAdd(blockerGrid, d, i, j)
            else
                let dungeonNumeral = (3*j+i)
                let d = new DockPanel(LastChildFill=false)
                let sp = new StackPanel(Orientation=Orientation.Horizontal)
                let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text=sprintf "%d" dungeonNumeral, Width=18., 
                                        VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), TextAlignment=TextAlignment.Right)
                sp.Children.Add(tb) |> ignore
                sp.Children.Add(makeBlockerBox(dungeonNumeral-1, 0)) |> ignore
                sp.Children.Add(makeBlockerBox(dungeonNumeral-1, 1)) |> ignore
                d.Children.Add(sp) |> ignore
                gridAdd(blockerGrid, d, i, j)
                blockerDungeonSunglasses.[dungeonNumeral-1] <- upcast sp // just reduce its opacity
    canvasAdd(appMainCanvas, blockerGrid, 402., THRU_TIMELINE_H) 

    // notes    
    let tb = new TextBox(Width=appMainCanvas.Width-402., Height=dungeonTabs.Height - blockerGrid.Height)
    notesTextBox <- tb
    tb.FontSize <- 24.
    tb.Foreground <- Brushes.LimeGreen 
    tb.Background <- Brushes.Black 
    tb.CaretBrush <- Brushes.LimeGreen 
    tb.Text <- "Notes\n"
    tb.AcceptsReturn <- true
    canvasAdd(appMainCanvas, tb, 402., THRU_TIMELINE_H + blockerGrid.Height) 

    grabModeTextBlock.Opacity <- 0.
    grabModeTextBlock.Width <- tb.Width
    canvasAdd(appMainCanvas, grabModeTextBlock, 402., THRU_TIMELINE_H) 

    // remaining OW spots
    canvasAdd(appMainCanvas, owRemainingScreensTextBox, RIGHT_COL+30., 76.)
    canvasAdd(appMainCanvas, owGettableScreensCheckBox, RIGHT_COL, 98.)
    owGettableScreensCheckBox.Checked.Add(fun _ -> TrackerModel.forceUpdate()) 
    owGettableScreensCheckBox.Unchecked.Add(fun _ -> TrackerModel.forceUpdate())
    owGettableScreensTextBox.PointerEnter.Add(fun _ -> 
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, 
                                            TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), Point(0.,0.), 0, 0, false, 128)
        )
    owGettableScreensTextBox.PointerLeave.Add(fun _ -> 
        if not(owGettableScreensCheckBox.IsChecked.HasValue) || not(owGettableScreensCheckBox.IsChecked.Value) then 
            routeDrawingCanvas.Children.Clear()
        )
    // current hearts
    canvasAdd(appMainCanvas, currentHeartsTextBox, RIGHT_COL, 120.)
    // coordinate grid
    let owCoordsGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owCoordsTBs = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let tb = new TextBox(Text=sprintf "%c%d" (char (int 'A' + j)) (i+1), Foreground=Brushes.White, Background=Brushes.Transparent, BorderThickness=Thickness(0.0), 
                                    FontFamily=FontFamily("Consolas"), FontSize=16.0, FontWeight=FontWeight.Bold)
            mirrorOverworldFEs.Add(tb)
            tb.Opacity <- 0.0
            tb.IsHitTestVisible <- false // transparent to mouse
            owCoordsTBs.[i,j] <- tb
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            canvasAdd(c, tb, 2., 6.)
            gridAdd(owCoordsGrid, c, i, j) 
    canvasAdd(overworldCanvas, owCoordsGrid, 0., 0.)
    let showCoords = new TextBox(Text="Coords",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true)
    let cb = new CheckBox(Content=showCoords)
    cb.IsChecked <- System.Nullable.op_Implicit false
    cb.Checked.Add(fun _ -> owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.85))
    cb.Unchecked.Add(fun _ -> owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.0))
    showCoords.PointerEnter.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.85))
    showCoords.PointerLeave.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.0))
    canvasAdd(appMainCanvas, cb, RIGHT_COL + 140., 120.)

    // zone overlay
    let owMapZoneBmps =
        let avg(c1:System.Drawing.Color, c2:System.Drawing.Color) = System.Drawing.Color.FromArgb((int c1.R + int c2.R)/2, (int c1.G + int c2.G)/2, (int c1.B + int c2.B)/2)
        let colors = 
            dict [
                'M', avg(System.Drawing.Color.Pink, System.Drawing.Color.Crimson)
                'L', System.Drawing.Color.BlueViolet 
                'R', System.Drawing.Color.LightSeaGreen 
                'H', System.Drawing.Color.Gray
                'C', System.Drawing.Color.LightBlue 
                'G', avg(System.Drawing.Color.LightSteelBlue, System.Drawing.Color.SteelBlue)
                'D', System.Drawing.Color.Orange 
                'F', System.Drawing.Color.LightGreen 
                'S', System.Drawing.Color.DarkGray 
                'W', System.Drawing.Color.Brown
            ]
        let imgs = Array2D.zeroCreate 16 8
        for x = 0 to 15 do
            for y = 0 to 7 do
                let tile = new System.Drawing.Bitmap(int OMTW,11*3)
                for px = 0 to int OMTW-1 do
                    for py = 0 to 11*3-1 do
                        tile.SetPixel(px, py, colors.Item(OverworldData.owMapZone.[y].[x]))
                imgs.[x,y] <- tile
        imgs

    let owMapZoneGrid = makeGrid(16, 8, int OMTW, 11*3)
    let allOwMapZoneImages = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let image = Graphics.BMPtoImage owMapZoneBmps.[i,j]
            image.Opacity <- 0.0
            image.IsHitTestVisible <- false // transparent to mouse
            allOwMapZoneImages.[i,j] <- image
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            canvasAdd(c, image, 0., 0.)
            gridAdd(owMapZoneGrid, c, i, j)
    canvasAdd(overworldCanvas, owMapZoneGrid, 0., 0.)

    let owMapZoneBoundaries = ResizeArray()
    let makeLine(x1, x2, y1, y2) = 
        let line = new Shapes.Line(StartPoint=Point(OMTW*float(x1),float(y1*11*3)), EndPoint=Point(OMTW*float(x2),float(y2*11*3)), Stroke=Brushes.White, StrokeThickness=3.)
        line.IsHitTestVisible <- false // transparent to mouse
        line
    let addLine(x1,x2,y1,y2) = 
        let line = makeLine(x1,x2,y1,y2)
        line.Opacity <- 0.0
        owMapZoneBoundaries.Add(line)
        canvasAdd(overworldCanvas, line, 0., 0.)
    addLine(0,7,2,2)
    addLine(7,11,1,1)
    addLine(7,7,1,2)
    addLine(10,10,0,1)
    addLine(11,11,0,2)
    addLine(8,14,2,2)
    addLine(14,14,0,2)
    addLine(6,6,2,3)
    addLine(4,4,3,4)
    addLine(2,2,4,5)
    addLine(1,1,5,7)
    addLine(0,1,7,7)
    addLine(1,4,5,5)
    addLine(2,4,4,4)
    addLine(4,6,3,3)
    addLine(4,7,6,6)
    addLine(7,12,5,5)
    addLine(9,10,4,4)
    addLine(7,10,3,3)
    addLine(7,7,2,3)
    addLine(10,10,3,4)
    addLine(9,9,4,7)
    addLine(7,7,5,6)
    addLine(4,4,5,6)
    addLine(5,5,6,8)
    addLine(6,6,6,8)
    addLine(11,11,5,8)
    addLine(9,15,7,7)
    addLine(12,12,3,5)
    addLine(13,13,2,3)
    addLine(8,8,2,3)
    addLine(12,14,3,3)
    addLine(14,15,4,4)
    addLine(15,15,4,7)
    addLine(14,14,3,4)

    let zoneNames = ResizeArray()  // added later, to be top of z-order
    let addZoneName(hz, name, x, y) =
        let tb = new TextBox(Text=name,FontSize=12.,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(2.),IsReadOnly=true)
        mirrorOverworldFEs.Add(tb)
        canvasAdd(overworldCanvas, tb, x*OMTW, y*11.*3.)
        tb.Opacity <- 0.
        tb.TextAlignment <- TextAlignment.Center
        tb.FontWeight <- FontWeight.Bold
        tb.IsHitTestVisible <- false
        zoneNames.Add(hz, tb)

    let changeZoneOpacity(hintZone,show) =
        let noZone = hintZone=TrackerModel.HintZone.UNKNOWN
        if show then
            if noZone then 
                allOwMapZoneImages |> Array2D.iteri (fun x y image -> image.Opacity <- 0.3)
            owMapZoneBoundaries |> Seq.iter (fun x -> x.Opacity <- 0.9)
            zoneNames |> Seq.iter (fun (hz,textbox) -> if noZone || hz=hintZone then textbox.Opacity <- 0.6)
        else
            allOwMapZoneImages |> Array2D.iteri (fun x y image -> image.Opacity <- 0.0)
            owMapZoneBoundaries |> Seq.iter (fun x -> x.Opacity <- 0.0)
            zoneNames |> Seq.iter (fun (hz,textbox) -> textbox.Opacity <- 0.0)
    let zone_checkbox = new CheckBox(Content=new TextBox(Text="Zones",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    zone_checkbox.IsChecked <- System.Nullable.op_Implicit false
    zone_checkbox.Checked.Add(fun _ -> changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,true))
    zone_checkbox.Unchecked.Add(fun _ -> changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,false))
    zone_checkbox.PointerEnter.Add(fun _ -> if not zone_checkbox.IsChecked.HasValue || not zone_checkbox.IsChecked.Value then changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,true))
    zone_checkbox.PointerLeave.Add(fun _ -> if not zone_checkbox.IsChecked.HasValue || not zone_checkbox.IsChecked.Value then changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,false))
    canvasAdd(appMainCanvas, zone_checkbox, RIGHT_COL + 140., 96.)

    let owLocatorGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owLocatorTilesRowColumn = Array2D.zeroCreate 16 8
    let owLocatorTilesZone = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let rc = new Shapes.Rectangle(Width=OMTW, Height=float(11*3), Stroke=Brushes.Transparent, StrokeThickness=0., Fill=Brushes.White, Opacity=0., IsHitTestVisible=false)
            let z  = new Shapes.Rectangle(Width=OMTW, Height=float(11*3), Stroke=Brushes.Transparent, StrokeThickness=10., Fill=Brushes.Lime,  Opacity=0., IsHitTestVisible=false)
            owLocatorTilesRowColumn.[i,j] <- rc
            owLocatorTilesZone.[i,j] <- z
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            gridAdd(owLocatorGrid, rc, i, j)
            gridAdd(owLocatorGrid, z, i, j)
    canvasAdd(overworldCanvas, owLocatorGrid, 0., 0.)

    showLocatorExactLocation <- (fun (x,y) ->
        if (x,y) <> TrackerModel.NOTFOUND then
            // show exact location
            for i = 0 to 15 do
                owLocatorTilesRowColumn.[i,y].Opacity <- 0.35
            for j = 0 to 7 do
                owLocatorTilesRowColumn.[x,j].Opacity <- 0.35
        )
    showLocatorHintedZone <- (fun hinted_zone ->
        if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
            // have hint, so draw that zone...
            if not zone_checkbox.IsChecked.HasValue || not zone_checkbox.IsChecked.Value then changeZoneOpacity(hinted_zone,true)
            for i = 0 to 15 do
                for j = 0 to 7 do
                    // ... and highlight all undiscovered tiles
                    if OverworldData.owMapZone.[j].[i] = hinted_zone.AsDataChar() then
                        if TrackerModel.overworldMapMarks.[i,j].Current() = -1 then
                            owLocatorTilesZone.[i,j].Opacity <- 0.4
        )
    showLocatorInstanceFunc <- (fun f ->
        for i = 0 to 15 do
            for j = 0 to 7 do
                if f(i,j) && TrackerModel.overworldMapMarks.[i,j].Current() = -1 then
                    owLocatorTilesZone.[i,j].Opacity <- 0.4
        )
    showShopLocatorInstanceFunc <- (fun item ->
        for i = 0 to 15 do
            for j = 0 to 7 do
                let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                if cur = item || (TrackerModel.getOverworldMapExtraData(i,j) = TrackerModel.MapSquareChoiceDomainHelper.ToItem(item)) then
                    owLocatorTilesZone.[i,j].Opacity <- 0.4
        )
    showLocator <- (fun level ->
        let loc = 
            if level < 9 then
                TrackerModel.mapStateSummary.DungeonLocations.[level]
            elif level = 9 then
                TrackerModel.mapStateSummary.Sword2Location
            elif level = 10 then
                TrackerModel.mapStateSummary.Sword3Location
            else
                failwith "bad showLocator(level)"
        if loc <> TrackerModel.NOTFOUND then
            showLocatorExactLocation(loc)
        else
            let hinted_zone = TrackerModel.levelHints.[level]
            if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                showLocatorHintedZone(hinted_zone)
        )
    hideLocator <- (fun () ->
        if not zone_checkbox.IsChecked.HasValue || not zone_checkbox.IsChecked.Value then changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,false)
        for i = 0 to 15 do
            for j = 0 to 7 do
                owLocatorTilesRowColumn.[i,j].Opacity <- 0.0
                owLocatorTilesZone.[i,j].Opacity <- 0.0
        )

    addZoneName(TrackerModel.HintZone.DEATH_MOUNTAIN, "DEATH\nMOUNTAIN", 2.5, 0.3)
    addZoneName(TrackerModel.HintZone.GRAVE,          "GRAVE", 1.5, 2.8)
    addZoneName(TrackerModel.HintZone.DEAD_WOODS,     "DEAD\nWOODS", 1.4, 5.3)
    addZoneName(TrackerModel.HintZone.LAKE,           "LAKE 1", 10.2, 0.1)
    addZoneName(TrackerModel.HintZone.LAKE,           "LAKE 2", 5.5, 3.5)
    addZoneName(TrackerModel.HintZone.LAKE,           "LAKE 3", 9.4, 5.5)
    addZoneName(TrackerModel.HintZone.RIVER,          "RIVER 1", 7.3, 1.1)
    addZoneName(TrackerModel.HintZone.RIVER,          "RIV\nER2", 5.1, 6.2)
    addZoneName(TrackerModel.HintZone.NEAR_START,     "START", 7.3, 6.2)
    addZoneName(TrackerModel.HintZone.DESERT,         "DESERT", 10.3, 3.1)
    addZoneName(TrackerModel.HintZone.FOREST,         "FOREST", 12.3, 5.1)
    addZoneName(TrackerModel.HintZone.LOST_HILLS,     "LOST\nHILLS", 12.4, 0.3)
    addZoneName(TrackerModel.HintZone.COAST,          "COAST", 14.3, 2.7)




    //                            items  ow map  prog  timeline  dungeon tabs                
    appMainCanvas.Height <- float(30*5 + 11*3*9 + 30 + TCH + 6 + TH + TH + 27*8 + 12*7 + 30)

    CustomComboBoxes.InitializeItemComboBox(appMainCanvas)  // very very top
    
    TrackerModel.forceUpdate()
    appMainCanvas, updateTimeline


