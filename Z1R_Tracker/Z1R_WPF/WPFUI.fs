module WPFUI

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let canvasAdd = Graphics.canvasAdd


let soundPlayer = new MediaPlayer()
soundPlayer.Volume <- 0.1
soundPlayer.Open(new Uri("confirm_speech.wav", UriKind.Relative))
let PlaySoundForSpeechRecognizedAndUsedToMark() =
    soundPlayer.Position <- TimeSpan(0L)
    soundPlayer.Play()

let voice = OptionsMenu.voice
let speechRecognizer = new System.Speech.Recognition.SpeechRecognitionEngine()
let wakePhrase = "tracker set"
let mapStatePhrases = dict [|
        "level one"         , 0
        "level two"         , 1
        "level three"       , 2
        "level four"        , 3
        "level five"        , 4
        "level six"         , 5
        "level seven"       , 6
        "level eight"       , 7
        "level nine"        , 8
        "any road"          , 12  // 9 10 11 12
        "sword three"       , 13
        "sword two"         , 14
        "hint shop"         , 15
        "arrow shop"        , 16
        "bomb shop"         , 17
        "book shop"         , 18
        "candle shop"       , 19
        "blue ring shop"    , 20
        "meat shop"         , 21
        "key shop"          , 22
        "shield shop"       , 23
        "take any"          , 24
        "potion shop"       , 25
        "money"             , 26
        |]
speechRecognizer.LoadGrammar(new System.Speech.Recognition.Grammar(let gb = new System.Speech.Recognition.GrammarBuilder(wakePhrase) in gb.Append(new System.Speech.Recognition.Choices(mapStatePhrases.Keys |> Seq.toArray)); gb))
let convertSpokenPhraseToMapCell(phrase:string) =
    let phrase = phrase.Substring(wakePhrase.Length+1)
    let newState = mapStatePhrases.[phrase]
    if newState = 12 then // any road
        if   TrackerModel.mapSquareChoiceDomain.CanAddUse( 9) then
            Some 9
        elif TrackerModel.mapSquareChoiceDomain.CanAddUse(10) then
            Some 10
        elif TrackerModel.mapSquareChoiceDomain.CanAddUse(11) then
            Some 11
        elif TrackerModel.mapSquareChoiceDomain.CanAddUse(12) then
            Some 12
        else
            None
    else
        if TrackerModel.mapSquareChoiceDomain.CanAddUse(newState) then
            Some newState
        else
            None

type MapStateProxy(state) =
    static let U = Graphics.uniqueMapIcons.Length 
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
    member this.Current() =
        if state = -1 then
            null
        elif state < U then
            Graphics.uniqueMapIcons.[state]
        else
            Graphics.BMPtoImage Graphics.nonUniqueMapIconBMPs.[state-U]
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
let currentHeartsTextBox = new TextBox(Width=200., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "Current Hearts: %d" TrackerModel.playerComputedStateSummary.PlayerHearts)
let owRemainingScreensTextBox = new TextBox(Width=200., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "%d OW spots left" TrackerModel.mapStateSummary.OwSpotsRemain)
let owGettableScreensTextBox = new TextBox(Width=200., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "Show %d gettable" TrackerModel.mapStateSummary.OwGettableLocations.Count)
let owGettableScreensCheckBox = new CheckBox(Content = owGettableScreensTextBox)

let drawRoutesTo(targetItemStateOption, routeDrawingCanvas, point, i, j, drawRouteMarks, maxYellowGreenHighlights) =
    let maxYellowGreenHighlights = if owGettableScreensCheckBox.IsChecked.HasValue && owGettableScreensCheckBox.IsChecked.Value then 128 else maxYellowGreenHighlights
    match targetItemStateOption with
    | Some(targetItemState) ->
        let owTargetworthySpots = Array2D.zeroCreate 16 8
        for x = 0 to 15 do
            for y = 0 to 7 do
                let msp = MapStateProxy(TrackerModel.overworldMapMarks.[x,y].Current())
                if msp.State = targetItemState || (msp.IsThreeItemShop && TrackerModel.getOverworldMapExtraData(x,y) = TrackerModel.MapSquareChoiceDomainHelper.ToItem(targetItemState)) then
                    owTargetworthySpots.[x,y] <- true
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, owTargetworthySpots, 
                                            Array2D.zeroCreate 16 8, point, i, j, drawRouteMarks, maxYellowGreenHighlights)
    | None ->
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, 
                                            TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), point, i, j, drawRouteMarks, maxYellowGreenHighlights)


let mutable f5WasRecentlyPressed = false
let mutable currentlyMousedOWX, currentlyMousedOWY = -1, -1
let mutable notesTextBox = null : TextBox
let mutable timeTextBox = null : TextBox
let H = 30
let RIGHT_COL = 560.
let TCH = 123  // timeline height
let TH = 24 // text height
let OMTW = OverworldRouteDrawing.OMTW  // overworld map tile width - at normal aspect ratio, is 48 (16*3)
let resizeMapTileImage(image:Image) =
    image.Width <- OMTW
    image.Height <- float(11*3)
    image.Stretch <- Stretch.Fill
    image.StretchDirection <- StretchDirection.Both
    image
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
    let whichItems = Graphics.allItemsWithHeartShuffle 
    let bookOrMagicalShieldVB = whichItems.[0].Fill :?> VisualBrush
    let isCurrentlyBook = ref true
    let toggleBookMagicalShield() =
        if !isCurrentlyBook then
            bookOrMagicalShieldVB.Visual <- Graphics.BMPtoImage Graphics.magic_shield_bmp
        else
            bookOrMagicalShieldVB.Visual <- Graphics.BMPtoImage Graphics.book_bmp
        isCurrentlyBook := not !isCurrentlyBook
        TrackerModel.forceUpdate()

    let mutable routeTargetLastClickedTime = DateTime.Now - TimeSpan.FromMinutes(10.)
    let mutable routeTargetState = 0
    let currentRouteTarget() =
        if DateTime.Now - routeTargetLastClickedTime < TimeSpan.FromSeconds(10.) then
            Some(routeTargetState)
        else
            None
    let changeCurrentRouteTarget(newTargetState) =
        routeTargetLastClickedTime <- DateTime.Now
        routeTargetState <- newTargetState
    
    let mutable showLocatorExactLocation = fun(_x:int,_y:int) -> ()
    let mutable showLocatorHintedZone = fun(_hz:TrackerModel.HintZone) -> ()
    let mutable showLocatorInstanceFunc = fun(f:int*int->bool) -> ()
    let mutable showShopLocatorInstanceFunc = fun(_item:int) -> ()
    let mutable showLocator = fun(_l:int) -> ()
    let mutable hideLocator = fun() -> ()

    let appMainCanvas = new Canvas(Width=16.*OMTW, Background=Brushes.Black)

    let mainTracker = makeGrid(9, 5, H, H)
    canvasAdd(appMainCanvas, mainTracker, 0., 0.)

    let hintHighlightBrush = new LinearGradientBrush(Colors.Yellow, Colors.DarkGreen, 45.)
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
        c.MouseDown.Add(fun _ -> 
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
        let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., Stroke=CustomComboBoxes.no, StrokeThickness=3.0)
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
            let mutable i = box.CellCurrent()
            // find unique heart FrameworkElement to display
            while i>=14 && whichItems.[i].Parent<>null do
                i <- i + 1
            let fe = if i = -1 then null else whichItems.[i]
            canvasAdd(innerc, fe, 4., 4.)
        box.Changed.Add(fun _ -> redrawBoxOutline(); redrawInner(); if requiresForceUpdate then TrackerModel.forceUpdate())
        let mutable popupIsActive = false
        let activateComboBox(activationDelta) =
            popupIsActive <- true
            let pos = c.TranslatePoint(Point(),appMainCanvas)
            CustomComboBoxes.DisplayItemComboBox(appMainCanvas, pos.X, pos.Y, box.CellCurrent(), activationDelta, isCurrentlyBook, (fun (newBoxCellValue, newPlayerHas) ->
                box.Set(newBoxCellValue, newPlayerHas)
                popupIsActive <- false
                ), (fun () -> popupIsActive <- false))
        c.MouseDown.Add(fun ea ->
            if not popupIsActive then
                if ea.ButtonState = Input.MouseButtonState.Pressed &&
                        (ea.ChangedButton = Input.MouseButton.Left || ea.ChangedButton = Input.MouseButton.Middle || ea.ChangedButton = Input.MouseButton.Right) then
                    if box.CellCurrent() = -1 then
                        activateComboBox(0)
                    else
                        box.SetPlayerHas(CustomComboBoxes.MouseButtonEventArgsToPlayerHas ea)
            )
        // item
        c.MouseWheel.Add(fun x -> if not popupIsActive then activateComboBox(if x.Delta<0 then 1 else -1))
        c.MouseEnter.Add(fun _ -> 
            if not popupIsActive then
                match box.CellCurrent() with
                | 3 -> showLocatorInstanceFunc(owInstance.PowerBraceletable)
                | 4 -> showLocatorInstanceFunc(owInstance.Ladderable)
                | 7 -> showLocatorInstanceFunc(owInstance.Raftable)
                | 8 -> showLocatorInstanceFunc(owInstance.Whistleable)
                | 9 -> showLocatorInstanceFunc(owInstance.Burnable)
                | _ -> ()
            )
        c.MouseLeave.Add(fun _ -> if not popupIsActive then hideLocator())
        redrawBoxOutline()
        redrawInner()
        timelineItems.Add(new Timeline.TimelineItem(fun()->if obj.Equals(rect.Stroke,CustomComboBoxes.yes) then Some(boxCurrentBMP(true)) else None))
        c
    // items
    let finalCanvasOf1Or4 = boxItemImpl(TrackerModel.FinalBoxOf1Or4, false)
    for i = 0 to 8 do
        for j = 0 to 2 do
            let c = new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black)
            gridAdd(mainTracker, c, i, j+2)
            if j=0 || j=1 || i=7 then
                canvasAdd(c, boxItemImpl(TrackerModel.dungeons.[i].Boxes.[j], false), 0., 0.)
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
            mainTrackerCanvases.[i,j].MouseEnter.Add(fun _ -> showLocator(i))
            mainTrackerCanvases.[i,j].MouseLeave.Add(fun _ -> hideLocator())

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

    let hideFirstQuestCheckBox  = new CheckBox(Content=new TextBox(Text="HFQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    hideFirstQuestCheckBox.ToolTip <- "Hide First Quest\nIn a mixed quest overworld tracker, shade out the first-quest-only spots.\nUseful if you're unsure if randomizer flags are mixed quest or second quest.\nCan't be used if you've marked a first-quest-only spot as having something."
    ToolTipService.SetShowDuration(hideFirstQuestCheckBox, 12000)
    let hideSecondQuestCheckBox = new CheckBox(Content=new TextBox(Text="HSQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    hideSecondQuestCheckBox.ToolTip <- "Hide Second Quest\nIn a mixed quest overworld tracker, shade out the second-quest-only spots.\nUseful if you're unsure if randomizer flags are mixed quest or first quest.\nCan't be used if you've marked a second-quest-only spot as having something."
    ToolTipService.SetShowDuration(hideSecondQuestCheckBox, 12000)

    hideFirstQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
    hideFirstQuestCheckBox.Checked.Add(fun _ -> 
        if thereAreMarks(firstQuestOnlyInterestingMarks) then
            System.Media.SystemSounds.Asterisk.Play()
            hideFirstQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        else
            hideFirstQuestFromMixed false
        hideSecondQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        )
    hideFirstQuestCheckBox.Unchecked.Add(fun _ -> hideFirstQuestFromMixed true)
    if isMixed then
        canvasAdd(appMainCanvas, hideFirstQuestCheckBox, 35., 130.) 

    hideSecondQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
    hideSecondQuestCheckBox.Checked.Add(fun _ -> 
        if thereAreMarks(secondQuestOnlyInterestingMarks) then
            System.Media.SystemSounds.Asterisk.Play()
            hideSecondQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        else
            hideSecondQuestFromMixed false
        hideFirstQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        )
    hideSecondQuestCheckBox.Unchecked.Add(fun _ -> hideSecondQuestFromMixed true)
    if isMixed then
        canvasAdd(appMainCanvas, hideSecondQuestCheckBox, 160., 130.) 

    // WANT!
    let kitty = new Image()
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("CroppedBrianKitty.png")
    kitty.Source <- System.Windows.Media.Imaging.BitmapFrame.Create(imageStream)
    canvasAdd(appMainCanvas, kitty, 285., 30.)

    let OFFSET = 400.
    // ow 'take any' hearts
    let owHeartGrid = makeGrid(4, 1, 30, 30)
    for i = 0 to 3 do
        let c = new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black)
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
        c.MouseLeftButtonDown.Add(fun _ -> f true)
        c.MouseRightButtonDown.Add(fun _ -> f false)
        c.MouseWheel.Add(fun x -> f (x.Delta<0))
        gridAdd(owHeartGrid, c, i, 0)
        timelineItems.Add(new Timeline.TimelineItem(fun()->if TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(i)=1 then Some(Graphics.owHeartFull_bmp) else None))
    canvasAdd(appMainCanvas, owHeartGrid, OFFSET, 30.)
    // ladder, armos, white sword items
    let owItemGrid = makeGrid(2, 3, 30, 30)
    gridAdd(owItemGrid, Graphics.BMPtoImage Graphics.ladder_bmp, 0, 0)
    let armos = Graphics.ow_key_armos
    armos.MouseEnter.Add(fun _ -> showLocatorInstanceFunc(owInstance.HasArmos))
    armos.MouseLeave.Add(fun _ -> hideLocator())
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
    white_sword_canvas.MouseEnter.Add(fun _ -> showLocator(9))
    white_sword_canvas.MouseLeave.Add(fun _ -> hideLocator())

    let OW_ITEM_GRID_OFFSET_X,OW_ITEM_GRID_OFFSET_Y = OFFSET,60.
    canvasAdd(appMainCanvas, owItemGrid, OW_ITEM_GRID_OFFSET_X, OW_ITEM_GRID_OFFSET_Y)
    // brown sword, blue candle, blue ring, magical sword
    let owItemGrid2 = makeGrid(3, 3, 30, 30)
    let veryBasicBoxImpl(bmp:System.Drawing.Bitmap, startOn, isTimeline, changedFunc, rightClick) =
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let no = System.Windows.Media.Brushes.DarkRed
        let yes = System.Windows.Media.Brushes.LimeGreen 
        let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., Stroke=(if startOn then yes else no), StrokeThickness=3.0)
        c.Children.Add(rect) |> ignore
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has item drawn on it, not the box
        c.Children.Add(innerc) |> ignore
        c.MouseLeftButtonDown.Add(fun _ ->
            if obj.Equals(rect.Stroke, no) then
                rect.Stroke <- yes
            else
                rect.Stroke <- no
            changedFunc(obj.Equals(rect.Stroke, yes))
        )
        c.MouseRightButtonDown.Add(fun _ -> rightClick())
        canvasAdd(innerc, Graphics.BMPtoImage bmp, 4., 4.)
        if isTimeline then
            timelineItems.Add(new Timeline.TimelineItem(fun()->if obj.Equals(rect.Stroke,yes) then Some(bmp) else None))
        c
    let basicBoxImpl(tts, img, changedFunc, rightClick) =
        let c = veryBasicBoxImpl(img, false, true, changedFunc, rightClick)
        c.ToolTip <- tts
        c
    gridAdd(owItemGrid2, basicBoxImpl("Acquired wood sword (mark timeline)",    Graphics.brown_sword_bmp  , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword.Toggle()),    (fun () -> ())), 1, 0)
    let wood_arrow_box = basicBoxImpl("Acquired wood arrow (mark timeline)",    Graphics.wood_arrow_bmp   , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodArrow.Toggle()),    (fun () -> changeCurrentRouteTarget(TrackerModel.MapSquareChoiceDomainHelper.ARROW)))
    wood_arrow_box.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.ARROW))
    wood_arrow_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAdd(owItemGrid2, wood_arrow_box, 2, 1)
    let blue_candle_box = basicBoxImpl("Acquired blue candle (mark timeline, affects routing)",   Graphics.blue_candle_bmp  , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle.Toggle()),   (fun () -> changeCurrentRouteTarget(TrackerModel.MapSquareChoiceDomainHelper.BLUE_CANDLE)))
    blue_candle_box.MouseEnter.Add(fun _ -> if TrackerModel.playerComputedStateSummary.CandleLevel=0 then showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BLUE_CANDLE) else showLocatorInstanceFunc(owInstance.Burnable))
    blue_candle_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAdd(owItemGrid2, blue_candle_box, 1, 1)
    let blue_ring_box = basicBoxImpl("Acquired blue ring (mark timeline)",     Graphics.blue_ring_bmp    , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueRing.Toggle()),     (fun () -> changeCurrentRouteTarget(TrackerModel.MapSquareChoiceDomainHelper.BLUE_RING)))
    blue_ring_box.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BLUE_RING))
    blue_ring_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAdd(owItemGrid2, blue_ring_box, 2, 0)
    let mags_box = basicBoxImpl("Acquired magical sword (mark timeline)", Graphics.magical_sword_bmp, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Toggle()), (fun () -> ()))
    ToolTipService.SetPlacement(mags_box, System.Windows.Controls.Primitives.PlacementMode.Top)
    let magsHintHighlight = makeHintHighlight(30.)
    let redrawMagicalSwordCanvas() =
        mags_box.Children.Remove(magsHintHighlight)
        if not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) &&   // dont have sword
                TrackerModel.mapStateSummary.Sword3Location=TrackerModel.NOTFOUND &&           // not yet located cave
                TrackerModel.levelHints.[10]<>TrackerModel.HintZone.UNKNOWN then               // have a hint
            mags_box.Children.Insert(0, magsHintHighlight)
    redrawMagicalSwordCanvas()
    gridAdd(owItemGrid2, mags_box, 0, 2)
    mags_box.MouseEnter.Add(fun _ -> showLocator(10))
    mags_box.MouseLeave.Add(fun _ -> hideLocator())
    canvasAdd(appMainCanvas, owItemGrid2, OFFSET+60., 60.)
    // boomstick book, to mark when purchase in boomstick seed (normal book will become shield found in dungeon)
    let boom_book_box = basicBoxImpl("Purchased boomstick book (mark timeline)", Graphics.boom_book_bmp, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Toggle()), (fun () -> changeCurrentRouteTarget(TrackerModel.MapSquareChoiceDomainHelper.BOOK)))
    boom_book_box.MouseEnter.Add(fun _ -> showLocatorExactLocation(TrackerModel.mapStateSummary.BoomBookShopLocation))
    boom_book_box.MouseLeave.Add(fun _ -> hideLocator())
    canvasAdd(appMainCanvas, boom_book_box, OFFSET+120., 30.)
    // mark the dungeon wins on timeline via ganon/zelda boxes
    gridAdd(owItemGrid2, basicBoxImpl("Killed Ganon (mark timeline)",  Graphics.ganon_bmp, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasDefeatedGanon.Toggle()), (fun () -> ())), 1, 2)
    gridAdd(owItemGrid2, basicBoxImpl("Rescued Zelda (mark timeline)", Graphics.zelda_bmp, (fun b -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Toggle(); if b then notesTextBox.Text <- notesTextBox.Text + "\n" + timeTextBox.Text), (fun () -> ())), 2, 2)
    // mark whether player currently has bombs, for overworld routing
    let bombIcon = veryBasicBoxImpl(Graphics.bomb_bmp, false, false, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBombs.Toggle()), (fun () -> changeCurrentRouteTarget(TrackerModel.MapSquareChoiceDomainHelper.BOMB)))
    bombIcon.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BOMB))
    bombIcon.MouseLeave.Add(fun _ -> hideLocator())
    bombIcon.ToolTip <- "Player currently has bombs (affects routing)"
    canvasAdd(appMainCanvas, bombIcon, OFFSET+160., 60.)

    // shield versus book icon (for boomstick flags/seeds)
    let toggleBookShieldCheckBox  = new CheckBox(Content=new TextBox(Text="S/B",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    toggleBookShieldCheckBox.ToolTip <- "Shield item icon instead of book item icon"
    toggleBookShieldCheckBox.IsChecked <- System.Nullable.op_Implicit false
    toggleBookShieldCheckBox.Checked.Add(fun _ -> toggleBookMagicalShield())
    toggleBookShieldCheckBox.Unchecked.Add(fun _ -> toggleBookMagicalShield())
    canvasAdd(appMainCanvas, toggleBookShieldCheckBox, OFFSET+150., 30.)

    // overworld map grouping, as main point of support for mirroring
    let mirrorOverworldFEs = ResizeArray<FrameworkElement>()   // overworldCanvas (on which all map is drawn) is here, as well as individual tiny textual/icon elements that need to be re-flipped
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
            let image = resizeMapTileImage(Graphics.BMPtoImage(owMapBMPs.[i,j]))
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
        let rect = new System.Windows.Shapes.Rectangle(Width=7.0*OMTW/48., Height=float(11*3)-1.5, Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
        canvasAdd(owHidingMapGridCanvases.[x,y], rect, 7.*OMTW/48., 0.)
        let rect = new System.Windows.Shapes.Rectangle(Width=7.0*OMTW/48., Height=float(11*3)-1.5, Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
        canvasAdd(owHidingMapGridCanvases.[x,y], rect, 19.*OMTW/48., 0.)
        let rect = new System.Windows.Shapes.Rectangle(Width=7.0*OMTW/48., Height=float(11*3)-1.5, Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
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
    let ENLARGE = 8.
    let BT = 2.  // border thickness of the interior 3x3 grid of tiles
    let dungeonTabsOverlay = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(5.), Background=Brushes.Black, Opacity=0., IsHitTestVisible=false)
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
        let s = new System.Windows.Shapes.Line(X1=L1, X2=L2, Y1=T1+1.5, Y2=T1+1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=L1+1.5, X2=L1+1.5, Y1=T1, Y2=T2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=L1, X2=L2, Y1=B2-1.5, Y2=B2-1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=L1+1.5, X2=L1+1.5, Y1=B1, Y2=B2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R1, X2=R2, Y1=T1+1.5, Y2=T1+1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R2-1.5, X2=R2-1.5, Y1=T1, Y2=T2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R1, X2=R2, Y1=B2-1.5, Y2=B2-1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R2-1.5, X2=R2-1.5, Y1=B1, Y2=B2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
    let drawDungeonHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,System.Windows.Media.Brushes.Yellow)
    let drawCompletedIconHighlight(c,x,y) =
        let rect = new System.Windows.Shapes.Rectangle(Width=15.0*OMTW/48., Height=27.0, Stroke=System.Windows.Media.Brushes.Black, StrokeThickness = 3.,
                                                        Fill=System.Windows.Media.Brushes.Black, Opacity=0.4)
        let diff = if displayIsCurrentlyMirrored then 18.0*OMTW/48. else 15.0*OMTW/48.
        canvasAdd(c, rect, x*OMTW+diff, float(y*11*3)+3.0)
    let drawCompletedDungeonHighlight(c,x,y) =
        // darkened rectangle corners
        let yellow = System.Windows.Media.Brushes.Yellow.Color
        let darkYellow = Color.FromRgb(yellow.R/2uy, yellow.G/2uy, yellow.B/2uy)
        drawRectangleCornersHighlight(c,x,y,new SolidColorBrush(darkYellow))
        // darken the number
        drawCompletedIconHighlight(c,x,y)
    let drawWarpHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,System.Windows.Media.Brushes.Orchid)
    let drawDarkening(c,x,y) =
        let rect = new System.Windows.Shapes.Rectangle(Width=OMTW, Height=float(11*3), Stroke=System.Windows.Media.Brushes.Black, StrokeThickness = 3.,
                                                        Fill=System.Windows.Media.Brushes.Black, Opacity=X_OPACITY)
        canvasAdd(c, rect, x*OMTW, float(y*11*3))
    let drawDungeonRecorderWarpHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,System.Windows.Media.Brushes.Lime)
    let mutable mostRecentMouseEnterTime = DateTime.Now 
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            gridAdd(owMapGrid, c, i, j)
            // we need a dummy image to make the canvas absorb the mouse interactions, so just re-draw the map at 0 opacity
            let image = resizeMapTileImage <| Graphics.BMPtoImage(owMapBMPs.[i,j])
            image.Opacity <- 0.0
            canvasAdd(c, image, 0., 0.)
            // highlight mouse, do mouse-sensitive stuff
            let rect = new System.Windows.Shapes.Rectangle(Width=OMTW-4., Height=float(11*3)-4., Stroke=System.Windows.Media.Brushes.White)
            c.MouseEnter.Add(fun ea ->  canvasAdd(c, rect, 2., 2.)
                                        // draw routes
                                        let mousePos = ea.GetPosition(c)
                                        let mousePos = if displayIsCurrentlyMirrored then Point(OMTW - mousePos.X, mousePos.Y) else mousePos
                                        drawRoutesTo(currentRouteTarget(), routeDrawingCanvas, mousePos, i, j, TrackerModel.Options.Overworld.DrawRoutes.Value, if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxYGH else 0)
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
                                            dungeonTabsOverlay.Opacity <- 1.0
                                        // track current location for F5 & speech recognition purposes
                                        currentlyMousedOWX <- i
                                        currentlyMousedOWY <- j
                                        mostRecentMouseEnterTime <- DateTime.Now)
            c.MouseLeave.Add(fun _ -> c.Children.Remove(rect) |> ignore
                                      dungeonTabsOverlayContent.Children.Clear()
                                      dungeonTabsOverlay.Opacity <- 0.
                                      routeDrawingCanvas.Children.Clear())
            // icon
            if owInstance.AlwaysEmpty(i,j) then
                // already set up as permanent opaque layer, in code above, so nothing else to do
                // except...
                if i=15 && j=5 then // ladder spot
                    let coastBoxOnOwGridRect = new Shapes.Rectangle(Width=30., Height=30., Stroke=Brushes.Red, StrokeThickness=3., Fill=Graphics.overworldCommonestFloorColorBrush)
                    canvasAdd(c, coastBoxOnOwGridRect, OMTW-30., 1.)
                    TrackerModel.ladderBox.Changed.Add(fun _ ->  
                        if TrackerModel.ladderBox.PlayerHas() = TrackerModel.PlayerHas.NO && TrackerModel.ladderBox.CellCurrent() = -1 then
                            coastBoxOnOwGridRect.Opacity <- 1.
                            coastBoxOnOwGridRect.IsHitTestVisible <- true
                        else
                            coastBoxOnOwGridRect.Opacity <- 0.
                            coastBoxOnOwGridRect.IsHitTestVisible <- false
                        )
                    let mutable popupIsActive = false
                    let activateLadderSpotPopup(activationDelta) =
                        popupIsActive <- true
                        let pos = 
                            if displayIsCurrentlyMirrored then
                                c.TranslatePoint(Point(OMTW,4.),appMainCanvas)
                            else
                                c.TranslatePoint(Point(OMTW-30.,4.),appMainCanvas)
                        // in appMainCanvas coordinates:
                        // ladderBox position in main canvas
                        let lx,ly = OW_ITEM_GRID_OFFSET_X + 30., OW_ITEM_GRID_OFFSET_Y
                        // bottom middle of the box, as an arrow target
                        let tx,ty = lx+15., ly+30.+3.   // +3 so arrowhead does not touch the target box
                        // top middle of the box we are drawing on the coast, as an arrow source
                        let sx,sy = pos.X+15., pos.Y-3. // -3 so the line base does not touch the target box
                        // line from source to target
                        let line = new Shapes.Line(X1=sx, Y1=sy, X2=tx, Y2=ty, Stroke=Brushes.Yellow, StrokeThickness=3.)
                        line.StrokeDashArray <- new DoubleCollection(seq[5.;4.])
                        // 93% along the line towards the target, for an arrowhead base
                        let ax,ay = (tx-sx)*0.93+sx, (ty-sy)*0.93+sy
                        // differential between target and arrowhead base
                        let dx,dy = tx-ax, ty-ay
                        // points orthogonal to the line from the base
                        let p1x,p1y = ax+dy/2., ay-dx/2.
                        let p2x,p2y = ax-dy/2., ay+dx/2.
                        // triangle to make arrowhead
                        let triangle = new Shapes.Polygon(Fill=Brushes.Yellow)
                        triangle.Points <- new PointCollection([Point(tx,ty); Point(p1x,p1y); Point(p2x,p2y)])
                        // rectangle for remote box highlight
                        let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=Brushes.Yellow, StrokeThickness=3.)
                        // TODO mirror overworld - maybe TP() relative to owCanvas?
                        let gridX,gridY = if displayIsCurrentlyMirrored then 27., -3. else -117., -3. 
                        let decoX,decoY = if displayIsCurrentlyMirrored then 27., 108. else -152., 108.
                        let extraDecorations = [|
                            CustomComboBoxes.itemBoxMouseButtonExplainerDecoration, decoX, decoY
                            upcast line, -pos.X-3., -pos.Y-3.
                            upcast triangle, -pos.X-3., -pos.Y-3.
                            upcast rect, lx-pos.X-3., ly-pos.Y-3.
                            |]
                        CustomComboBoxes.DisplayRemoteItemComboBox(appMainCanvas, pos.X, pos.Y, -1, activationDelta, isCurrentlyBook, 
                            gridX, gridY, (fun (newBoxCellValue, newPlayerHas) ->
                                TrackerModel.ladderBox.Set(newBoxCellValue, newPlayerHas)
                                TrackerModel.forceUpdate()
                                popupIsActive <- false
                                ), 
                            (fun () -> popupIsActive <- false), extraDecorations)
                    coastBoxOnOwGridRect.MouseDown.Add(fun _ -> if not popupIsActive then activateLadderSpotPopup(0))
                    coastBoxOnOwGridRect.MouseWheel.Add(fun ea -> if not popupIsActive then activateLadderSpotPopup(if ea.Delta<0 then 1 else -1))
                    //if (TrackerModel.ladderBox.PlayerHas()=TrackerModel.PlayerHas.NO) && TrackerModel.ladderBox.CellCurrent() = -1 then  // dont have, unknown
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
                    let icon = 
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
                            Graphics.BMPtoImage tile
                        else
                            ms.Current()
                    // be sure to draw in appropriate layer
                    if icon <> null then 
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
                    // figure out what new state we just interacted-to
                    if delta = 777 then 
                        let curState = TrackerModel.overworldMapMarks.[i,j].Current()
                        if curState = -1 then
                            // if unmarked, use voice to set new state
                            match convertSpokenPhraseToMapCell(phrase) with
                            | Some newState -> 
                                if TrackerModel.overworldMapMarks.[i,j].AttemptToSet(newState) then
                                    PlaySoundForSpeechRecognizedAndUsedToMark()
                            | None -> ()
                        elif MapStateProxy(curState).IsThreeItemShop && TrackerModel.getOverworldMapExtraData(i,j)=0 then
                            // if item shop with only one item marked, use voice to set other item
                            match convertSpokenPhraseToMapCell(phrase) with
                            | Some newState -> 
                                if TrackerModel.MapSquareChoiceDomainHelper.IsItem(newState) then
                                    TrackerModel.setOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.ToItem(newState))
                                    PlaySoundForSpeechRecognizedAndUsedToMark()
                            | None -> ()
                    elif delta = 1 then
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
                let mutable popupIsActive = false
                c.MouseLeftButtonDown.Add(fun _ -> 
                    if not popupIsActive then
                        popupIsActive <- true
                        // left button activates the popup selector
                        let ST = CustomComboBoxes.borderThickness
                        let tileImage = resizeMapTileImage <| Graphics.BMPtoImage(owMapBMPs.[i,j])
                        let tileCanvas = new Canvas(Width=OMTW, Height=11.*3.)
                        let originalState = TrackerModel.overworldMapMarks.[i,j].Current()
                        let originalStateIndex = if originalState = -1 then MapStateProxy.NumStates else originalState
                        let activationDelta = if originalState = -1 then -1 else 0  // accelerator so 'click' means 'X'
                        let leftAligned = if displayIsCurrentlyMirrored then not(i<12) else i<12
                        let gridxPosition = if leftAligned then -ST else OMTW - float(8*(5*3+2*int ST)+int ST)
                        let gridElementsSelectablesAndIDs : (FrameworkElement*bool*int)[] = Array.init (MapStateProxy.NumStates+1) (fun n ->
                            if MapStateProxy(n).IsX then
                                upcast new Canvas(Width=5.*3., Height=9.*3., Background=Graphics.overworldCommonestFloorColorBrush, Opacity=X_OPACITY), true, n
                            elif n = MapStateProxy.NumStates then
                                upcast new Canvas(Width=5.*3., Height=9.*3., Background=Graphics.overworldCommonestFloorColorBrush), true, -1
                            else
                                upcast Graphics.BMPtoImage(MapStateProxy(n).CurrentInteriorBMP()), (n = originalState) || TrackerModel.mapSquareChoiceDomain.CanAddUse(n), n
                            )
                        let pos = c.TranslatePoint(Point(), appMainCanvas)
                        CustomComboBoxes.DoModalGridSelect(appMainCanvas, pos.X, pos.Y, tileCanvas,
                            gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (8, 4, 5*3, 9*3), gridxPosition, 11.*3.+ST,
                            (fun (currentState) -> 
                                tileCanvas.Children.Clear()
                                canvasAdd(tileCanvas, tileImage, 0., 0.)
                                let bmp = MapStateProxy(currentState).CurrentBMP()
                                if bmp <> null then
                                    let icon = bmp |> Graphics.BMPtoImage |> resizeMapTileImage
                                    if MapStateProxy(currentState).IsX then
                                        icon.Opacity <- X_OPACITY
                                    canvasAdd(tileCanvas, icon, 0., 0.)),
                            (fun (dismissPopup, _ea, currentState) ->
                                TrackerModel.overworldMapMarks.[i,j].Set(currentState)
                                redrawGridSpot()
                                dismissPopup()
                                if originalState = -1 && currentState <> -1 then TrackerModel.forceUpdate()  // immediate update to dismiss green/yellow highlight from current tile
                                popupIsActive <- false),
                            (fun () -> popupIsActive <- false),
                            [], CustomComboBoxes.ModalGridSelectBrushes.Defaults())
                    )
                c.MouseRightButtonDown.Add(fun _ -> 
                    if not popupIsActive then
                        // right click is the 'special interaction'
                        let msp = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                        if msp.State = -1 then
                            // right click empty tile changes to 'X'
                            updateGridSpot -1 ""
                        elif msp.IsThreeItemShop then
                            // right click a shop cycles down the second item
                            let MODULO = TrackerModel.MapSquareChoiceDomainHelper.NUM_ITEMS+1
                            // next item
                            let e = (TrackerModel.getOverworldMapExtraData(i,j) - 1 + MODULO) % MODULO
                            // skip past duplicates
                            let item1 = msp.State - 15  // 1-based
                            let e = if e = item1 then (e - 1 + MODULO) % MODULO else e
                            TrackerModel.setOverworldMapExtraData(i,j,e)
                            // redraw
                            redrawGridSpot()
                    )
                c.MouseWheel.Add(fun x -> if not popupIsActive then updateGridSpot (if x.Delta<0 then 1 else -1) "")
    speechRecognizer.SpeechRecognized.Add(fun r ->
        if TrackerModel.Options.ListenForSpeech.Value then 
            if not(TrackerModel.Options.RequirePTTForSpeech.Value) ||                                               // if PTT not required, or
                    Gamepad.IsLeftShoulderButtonDown() ||                                                           // if PTT is currently held, or
                    (DateTime.Now - Gamepad.LeftShoulderButtonMostRecentRelease) < TimeSpan.FromSeconds(1.0) then   // if PTT was just recently released
                //printfn "conf: %3.3f  %s" r.Result.Confidence r.Result.Text                                         // then we want to use speech
                let threshold =
                    if TrackerModel.Options.RequirePTTForSpeech.Value then
                        0.90f   // empirical tests suggest this confidence threshold is good enough to avoid false positives with PTT
                    else
                        0.94f   // empirical tests suggest this confidence threshold is good to avoid false positives
                if r.Result.Confidence > threshold then  
                        appMainCanvas.Dispatcher.Invoke(fun () -> 
                                if currentlyMousedOWX >= 0 then // can hear speech before we have moused over any (uninitialized location)
                                    let c = owCanvases.[currentlyMousedOWX,currentlyMousedOWY]
                                    if c <> null && c.IsMouseOver then  // canvas can be null for always-empty grid places
                                        owUpdateFunctions.[currentlyMousedOWX,currentlyMousedOWY] 777 r.Result.Text
                            )
        )
    canvasAdd(overworldCanvas, owMapGrid, 0., 0.)
    owMapGrid.MouseLeave.Add(fun _ ->
        if owGettableScreensCheckBox.IsChecked.HasValue && owGettableScreensCheckBox.IsChecked.Value then
            OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, 
                        TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), Point(0.,0.), 0, 0, false, 128)
        )

    let recorderingCanvas = new Canvas(Width=16.*OMTW, Height=float(8*11*3))  // really the 'extra top layer' canvas for adding final marks to overworld map
    recorderingCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(overworldCanvas, recorderingCanvas, 0., 0.)
    let startIcon = new System.Windows.Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=System.Windows.Media.Brushes.Lime, StrokeThickness=3.0)

    let THRU_MAIN_MAP_H = float(150 + 8*11*3)

    // map legend
    let LEFT_OFFSET = 78.0
    let legendCanvas = new Canvas()
    canvasAdd(appMainCanvas, legendCanvas, LEFT_OFFSET, THRU_MAIN_MAP_H)

    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="The LEGEND\nof Z-Tracker")
    canvasAdd(appMainCanvas, tb, 0., THRU_MAIN_MAP_H)

    let shrink(bmp) = resizeMapTileImage <| Graphics.BMPtoImage bmp
    canvasAdd(legendCanvas, shrink Graphics.d1bmp, 0., 0.)
    drawDungeonHighlight(legendCanvas,0.,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Active\nDungeon")
    canvasAdd(legendCanvas, tb, OMTW, 0.)

    canvasAdd(legendCanvas, shrink Graphics.d1bmp, 2.5*OMTW, 0.)
    drawDungeonHighlight(legendCanvas,2.5,0)
    drawCompletedDungeonHighlight(legendCanvas,2.5,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Completed\nDungeon")
    canvasAdd(legendCanvas, tb, 3.5*OMTW, 0.)

    canvasAdd(legendCanvas, shrink Graphics.d1bmp, 5.*OMTW, 0.)
    drawDungeonHighlight(legendCanvas,5.,0)
    drawDungeonRecorderWarpHighlight(legendCanvas,5.,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Recorder\nDestination")
    canvasAdd(legendCanvas, tb, 6.*OMTW, 0.)

    canvasAdd(legendCanvas, shrink Graphics.w1bmp, 7.5*OMTW, 0.)
    drawWarpHighlight(legendCanvas,7.5,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Any Road\n(Warp)")
    canvasAdd(legendCanvas, tb, 8.5*OMTW, 0.)

    let legendStartIcon = new System.Windows.Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=System.Windows.Media.Brushes.Lime, StrokeThickness=3.0)
    canvasAdd(legendCanvas, legendStartIcon, 12.5*OMTW+8.5*OMTW/48., 0.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Start\nSpot")
    canvasAdd(legendCanvas, tb, 13.5*OMTW, 0.)

    let THRU_MAP_AND_LEGEND_H = THRU_MAIN_MAP_H + float(11*3)

    // item progress
    let itemProgressCanvas = new Canvas(Width=16.*OMTW, Height=30.)
    itemProgressCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(appMainCanvas, itemProgressCanvas, 0., THRU_MAP_AND_LEGEND_H)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Item Progress")
    canvasAdd(appMainCanvas, tb, 120., THRU_MAP_AND_LEGEND_H + 4.)

    // Version
    let vb = new Button(Content=new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, BorderThickness=Thickness(0.), 
                            Text=sprintf "v%s" OverworldData.VersionString, IsReadOnly=true, IsHitTestVisible=false),
                        BorderThickness=Thickness(1.), Margin=Thickness(0.), Padding=Thickness(0.))
    canvasAdd(appMainCanvas, vb, 0., THRU_MAP_AND_LEGEND_H + 4.)
    vb.Click.Add(fun _ ->
        let cmb = new CustomMessageBox.CustomMessageBox(OverworldData.AboutHeader, System.Drawing.SystemIcons.Information, OverworldData.AboutBody, ["Go to website"; "Ok"])
        cmb.Owner <- Window.GetWindow(appMainCanvas)
        cmb.ShowDialog() |> ignore
        if cmb.MessageBoxResult = "Go to website" then
            System.Diagnostics.Process.Start(OverworldData.Website) |> ignore
        )

    let hintGrid = makeGrid(3,OverworldData.hintMeanings.Length,180,36)
    let mutable row=0 
    for a,b in OverworldData.hintMeanings do
        let thisRow = row
        gridAdd(hintGrid, new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(1.), Text=a), 0, row)
        let tb = new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(1.), Text=b)
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
        let comboBox = new ComboBox(FontSize=16., IsEditable=false, IsReadOnly=true, Foreground=Brushes.Black)
        comboBox.Resources.Add(SystemColors.WindowBrushKey, Brushes.YellowGreen)
        comboBox.ItemsSource <- [| for i = 0 to 10 do yield TrackerModel.HintZone.FromIndex(i).ToString() |]
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
    let hintBorder = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(8.), Background=Brushes.Black)
    hintBorder.Child <- hintGrid
    let tb = new Button(Content=new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), Text="Hint Decoder"))
    canvasAdd(appMainCanvas, tb, 680., THRU_MAP_AND_LEGEND_H + 6.)
    tb.Click.Add(fun _ -> CustomComboBoxes.DoModal(appMainCanvas, 0., THRU_MAP_AND_LEGEND_H + 6., hintBorder, fun()->()) |> ignore)

    let THRU_MAIN_MAP_AND_ITEM_PROGRESS_H = THRU_MAP_AND_LEGEND_H + 30.

    let blockerDungeonSunglasses : FrameworkElement[] = Array.zeroCreate 8
    let doUIUpdate() =
        if displayIsCurrentlyMirrored <> TrackerModel.Options.MirrorOverworld.Value then
            // model changed, align the view
            displayIsCurrentlyMirrored <- not displayIsCurrentlyMirrored
            if displayIsCurrentlyMirrored then
                for fe in mirrorOverworldFEs do
                    fe.RenderTransform <- new ScaleTransform(-1., 1., fe.ActualWidth/2., fe.ActualHeight/2.)
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
        let mutable x, y = 200., 3.
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
            member _this.AnnounceConsiderSword2() = 
                if TrackerModel.Options.VoiceReminders.SwordHearts.Value then 
                    let n = TrackerModel.sword2Box.CellCurrent()
                    if n = -1 then
                        async { voice.Speak("Consider getting the white sword item") } |> Async.Start
                    else
                        async { voice.Speak(sprintf "Consider getting the %s from the white sword cave" (TrackerModel.ITEMS.AsPronounceString(n, !isCurrentlyBook))) } |> Async.Start
            member _this.AnnounceConsiderSword3() = if TrackerModel.Options.VoiceReminders.SwordHearts.Value then async { voice.Speak("Consider the magical sword") } |> Async.Start
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
                if (TrackerModel.sword2Box.PlayerHas() = TrackerModel.PlayerHas.NO) && (TrackerModel.sword2Box.CellCurrent() <> -1) then
                    // display known-but-ungotten item on the map
                    let itemImage = Graphics.BMPtoImage Graphics.allItemBMPsWithHeartShuffle.[TrackerModel.sword2Box.CellCurrent()]
                    if displayIsCurrentlyMirrored then 
                        itemImage.RenderTransform <- new ScaleTransform(-1., 1., OMTW/4., 30.)
                    itemImage.Width <- OMTW/2.
                    itemImage.Opacity <- 1.0
                    let color = Brushes.Black
                    let border = new Border(BorderThickness=Thickness(1.), BorderBrush=color, Background=color, Child=itemImage, Opacity=0.5)
                    let diff = if displayIsCurrentlyMirrored then 0. else OMTW - 24.
                    canvasAdd(recorderingCanvas, border, OMTW*float(x)+diff, float(y*11*3)+4.)
                if TrackerModel.sword2Box.PlayerHas() <> TrackerModel.PlayerHas.NO then
                    drawCompletedIconHighlight(recorderingCanvas,float x,y)  // darken a gotten white sword item cave icon
            member _this.CoastItem() =
                if TrackerModel.ladderBox.PlayerHas()=TrackerModel.PlayerHas.NO then
                    let x,y = 15,5
                    if TrackerModel.ladderBox.CellCurrent() <> -1 then
                        // display known-but-ungotten item on the map
                        let itemImage = Graphics.BMPtoImage Graphics.allItemBMPsWithHeartShuffle.[TrackerModel.ladderBox.CellCurrent()]
                        if displayIsCurrentlyMirrored then 
                            itemImage.RenderTransform <- new ScaleTransform(-1., 1., OMTW/4., 30.)
                        itemImage.Opacity <- 1.0
                        let color = Brushes.Black
                        let border = new Border(BorderThickness=Thickness(3.), BorderBrush=color, Background=color, Child=itemImage, Opacity=0.6)
                        canvasAdd(recorderingCanvas, border, OMTW*float(x)+OMTW - 24., float(y*11*3)+1.)
            member _this.RoutingInfo(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations,owRouteworthySpots) = 
                // clear and redraw routing
                routeDrawingCanvas.Children.Clear()
                OverworldRouting.repopulate(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations)
                let pos = System.Windows.Input.Mouse.GetPosition(routeDrawingCanvas)
                let i,j = int(Math.Floor(pos.X / OMTW)), int(Math.Floor(pos.Y / (11.*3.)))
                if i>=0 && i<16 && j>=0 && j<8 then
                    drawRoutesTo(currentRouteTarget(), routeDrawingCanvas,System.Windows.Point(0.,0.), i, j, TrackerModel.Options.Overworld.DrawRoutes.Value, if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxYGH else 0)
                elif owGettableScreensCheckBox.IsChecked.HasValue && owGettableScreensCheckBox.IsChecked.Value then
                    // unexplored but gettable spots highlight
                    OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, 
                                TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), Point(0.,0.), 0, 0, false, 128)
            member _this.AnnounceCompletedDungeon(i) = if TrackerModel.Options.VoiceReminders.DungeonFeedback.Value then async { voice.Speak(sprintf "Dungeon %d is complete" (i+1)) } |> Async.Start
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
            member _this.AnnounceFoundDungeonCount(n) = 
                if TrackerModel.Options.VoiceReminders.DungeonFeedback.Value then 
                    async {
                        if n = 1 then
                            voice.Speak("You have located one dungeon") 
                        elif n = 9 then
                            voice.Speak("Congratulations, you have located all 9 dungeons")
                        else
                            voice.Speak(sprintf "You have located %d dungeons" n) 
                    } |> Async.Start
            member _this.AnnounceTriforceCount(n) = 
                if TrackerModel.Options.VoiceReminders.DungeonFeedback.Value then 
                    if n = 1 then
                        async { voice.Speak("You now have one triforce") } |> Async.Start
                    else
                        async { voice.Speak(sprintf "You now have %d triforces" n) } |> Async.Start
                    if n = 8 && not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) then
                        async { voice.Speak("Consider the magical sword before dungeon nine") } |> Async.Start
            member _this.AnnounceTriforceAndGo(triforces, tagLevel) = 
                if TrackerModel.Options.VoiceReminders.DungeonFeedback.Value then 
                    let go = if triforces=8 then "go time" else "triforce and go"
                    match tagLevel with
                    | 1 -> async { voice.Speak("You might be "+go) } |> Async.Start
                    | 2 -> async { voice.Speak("You are probably "+go) } |> Async.Start
                    | 3 -> async { voice.Speak("You are "+go) } |> Async.Start
                    | _ -> ()
            member _this.RemindUnblock(blockerType, dungeons, detail) =
                let sentence = 
                    "Now that you have" + 
                        match blockerType with
                        | TrackerModel.DungeonBlocker.COMBAT ->
                            let words = ResizeArray()
                            for d in detail do
                                match d with
                                | TrackerModel.CombatUnblockerDetail.BETTER_SWORD -> words.Add(" a better sword,")
                                | TrackerModel.CombatUnblockerDetail.BETTER_ARMOR -> words.Add(" better armor,")
                                | TrackerModel.CombatUnblockerDetail.WAND -> words.Add(" the wand,")
                            System.String.Concat words
                        | TrackerModel.DungeonBlocker.BOW_AND_ARROW -> " a beau and arrow,"
                        | TrackerModel.DungeonBlocker.RECORDER -> " the recorder,"
                        | TrackerModel.DungeonBlocker.LADDER -> " the ladder,"
                        | TrackerModel.DungeonBlocker.KEY -> " the any key,"
                        | _ -> " "
                        + " consider dungeon" + (if Seq.length dungeons > 1 then "s " else " ") + (1 + Seq.head dungeons).ToString() +
                        (let mutable s = ""
                         for d in Seq.tail dungeons do
                            s <- s + " and " + (1+d).ToString()
                         s
                         )
                async { voice.Speak(sentence) } |> Async.Start
            member _this.RemindShortly(itemId) = 
                let f, g, text =
                    if itemId = TrackerModel.ITEMS.KEY then
                        (fun() -> TrackerModel.playerComputedStateSummary.HaveAnyKey), (fun() -> TrackerModel.remindedAnyKey <- false), "Don't forget that you have the any key"
                    elif itemId = TrackerModel.ITEMS.LADDER then
                        (fun() -> TrackerModel.playerComputedStateSummary.HaveLadder), (fun() -> TrackerModel.remindedLadder <- false), "Don't forget that you have the ladder"
                    else
                        failwith "bad reminder"
                let cxt = System.Threading.SynchronizationContext.Current 
                async { 
                    do! Async.Sleep(60000)  // 60s
                    do! Async.SwitchToContext(cxt)
                    if f() then
                        if TrackerModel.Options.VoiceReminders.HaveKeyLadder.Value then 
                            do! Async.SwitchToThreadPool()
                            voice.Speak(text) 
                    else
                        g()
                } |> Async.Start
            })
    let threshold = TimeSpan.FromMilliseconds(500.0)
    let mutable ladderTime, recorderTime, powerBraceletTime = DateTime.Now, DateTime.Now, DateTime.Now
    let mutable owPreviouslyAnnouncedWhistleSpotsRemain, owPreviouslyAnnouncedPowerBraceletSpotsRemain = 0, 0
    let timer = new System.Windows.Threading.DispatcherTimer()
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
        // remind ladder
        if (DateTime.Now - ladderTime).Minutes > 2 then  // every 3 mins
            if TrackerModel.playerComputedStateSummary.HaveLadder then
                if not(TrackerModel.playerComputedStateSummary.HaveCoastItem) then
                    if TrackerModel.Options.VoiceReminders.CoastItem.Value then 
                        let n = TrackerModel.ladderBox.CellCurrent()
                        if n = -1 then
                            async { voice.Speak("Get the coast item with the ladder") } |> Async.Start
                        else
                            async { voice.Speak(sprintf "Get the %s off the coast" (TrackerModel.ITEMS.AsPronounceString(n, !isCurrentlyBook))) } |> Async.Start
                    ladderTime <- DateTime.Now
        // remind whistle spots
        if (DateTime.Now - recorderTime).Minutes > 2 then  // every 3 mins
            if TrackerModel.playerComputedStateSummary.HaveRecorder then
                let owWhistleSpotsRemain = TrackerModel.mapStateSummary.OwWhistleSpotsRemain.Count
                if owWhistleSpotsRemain >= owPreviouslyAnnouncedWhistleSpotsRemain && owWhistleSpotsRemain > 0 then
                    if TrackerModel.Options.VoiceReminders.RecorderPBSpots.Value then 
                        if owWhistleSpotsRemain = 1 then
                            async { voice.Speak("There is one recorder spot") } |> Async.Start
                        else
                            async { voice.Speak(sprintf "There are %d recorder spots" owWhistleSpotsRemain) } |> Async.Start
                recorderTime <- DateTime.Now
                owPreviouslyAnnouncedWhistleSpotsRemain <- owWhistleSpotsRemain
        // remind power bracelet spots
        if (DateTime.Now - powerBraceletTime).Minutes > 2 then  // every 3 mins
            if TrackerModel.playerComputedStateSummary.HavePowerBracelet then
                if TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain >= owPreviouslyAnnouncedPowerBraceletSpotsRemain && TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain > 0 then
                    if TrackerModel.Options.VoiceReminders.RecorderPBSpots.Value then 
                        if TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain = 1 then
                            async { voice.Speak("There is one power bracelet spot") } |> Async.Start
                        else
                            async { voice.Speak(sprintf "There are %d power bracelet spots" TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain) } |> Async.Start
                powerBraceletTime <- DateTime.Now
                owPreviouslyAnnouncedPowerBraceletSpotsRemain <- TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain
        )
    timer.Start()

    // Dungeon level trackers
    let START_DUNGEON_AND_NOTES_AREA_H = THRU_MAIN_MAP_AND_ITEM_PROGRESS_H
    let fixedDungeon1Outlines = ResizeArray()
    let fixedDungeon2Outlines = ResizeArray()

    let ROOMS = 26 // how many types
    let roomBMPpairs(n) =
        match n with
        | 0  -> (fst Graphics.cdungeonUnexploredRoomBMP), (fst Graphics.cdungeonUnexploredRoomBMP)
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
    let dungeonRoomMouseButtonExplainerDecoration =
        let ST = CustomComboBoxes.borderThickness
        let h = 9.*3.*2.+ST*4.
        let d = new DockPanel(Height=h, LastChildFill=true, Background=Brushes.Black)
        let mouseBMP = Graphics.mouseIconButtonColors2BMP
        let mouse = Graphics.BMPtoImage mouseBMP
        mouse.Height <- h
        mouse.Width <- float(mouseBMP.Width) * h / float(mouseBMP.Height)
        mouse.Stretch <- Stretch.Uniform
        let mouse = new Border(BorderThickness=Thickness(0.,0.,ST,0.), BorderBrush=Brushes.Gray, Child=mouse)
        d.Children.Add(mouse) |> ignore
        DockPanel.SetDock(mouse,Dock.Left)
        let sp = new StackPanel(Orientation=Orientation.Vertical, VerticalAlignment=VerticalAlignment.Bottom)
        d.Children.Add(sp) |> ignore
        for color, text, b in [Brushes.DarkMagenta,"Completed room",true; Brushes.DarkCyan,"Uncompleted room",false] do
            let p = new StackPanel(Orientation=Orientation.Horizontal, Margin=Thickness(ST))
            let pict = Graphics.BMPtoImage((if b then snd else fst)(roomBMPpairs(ROOMS-1)))
            pict.Margin <- Thickness(ST,0.,2.*ST,0.)
            p.Children.Add(pict) |> ignore
            let tb = new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                                    Text=text, VerticalAlignment=VerticalAlignment.Center, BorderThickness=Thickness(ST), BorderBrush=color)
            p.Children.Add(tb) |> ignore
            sp.Children.Add(p) |> ignore
        let b:FrameworkElement = upcast new Border(BorderThickness=Thickness(ST), BorderBrush=Brushes.Gray, Child=d)
        b

    let grabHelper = new Dungeon.GrabHelper()
    let grabModeTextBlock = 
        new Border(BorderThickness=Thickness(2.), BorderBrush=Brushes.LightGray, 
                    Child=new TextBlock(TextWrapping=TextWrapping.Wrap, FontSize=16., Foreground=Brushes.Black, Background=Brushes.Gray, IsHitTestVisible=false,
                                        Text="You are now in 'grab mode', which can be used to move an entire segment of dungeon rooms and doors at once.\n\nTo abort grab mode, click again on 'GRAB' in the upper right of the dungeon tracker.\n\nTo move a segment, first click any marked room, to pick up that room and all contiguous rooms.  Then click again on a new location to 'drop' the segment you grabbed.  After grabbing, hovering the mouse shows a preview of where you would drop.  This behaves like 'cut and paste', and adjacent doors will come along for the ride.\n\nUpon completion, you will be prompted to keep changes or undo them, so you can experiment.")
        )
    let mutable popupState = Dungeon.DelayedPopupState.NONE  // key to an interlock that enables a fast double-click to bypass the popup
    let dungeonTabs = new TabControl(FontSize=12.)
    dungeonTabs.Background <- System.Windows.Media.Brushes.Black 
    canvasAdd(appMainCanvas, dungeonTabs, 0., START_DUNGEON_AND_NOTES_AREA_H)
    for level = 1 to 9 do
        let levelTab = new TabItem(Background=System.Windows.Media.Brushes.SlateGray)
        levelTab.Header <- sprintf "  %d  " level
        let contentCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7), Background=Brushes.Black)
        contentCanvas.MouseEnter.Add(fun _ -> 
            let i,j = TrackerModel.mapStateSummary.DungeonLocations.[level-1]
            if (i,j) <> TrackerModel.NOTFOUND then
                // when mouse in a dungeon map, show its location...
                showLocatorExactLocation(TrackerModel.mapStateSummary.DungeonLocations.[level-1])
                // ...and behave like we are moused there
                drawRoutesTo(None, routeDrawingCanvas, Point(), i, j, TrackerModel.Options.Overworld.DrawRoutes.Value, if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxYGH else 0)
            )
        contentCanvas.MouseLeave.Add(fun ea -> 
            // only hide the locator if we actually left
            // MouseLeave fires (erroneously?) when we e.g. click a room, and redraw() causes the element we are Entered to be removed from the tree and replaced by another,
            // even though we never left the bounds of the enclosing canvas
            let pos = ea.GetPosition(contentCanvas)
            if (pos.X < 0. || pos.X > contentCanvas.ActualWidth) || (pos.Y < 0. || pos.Y > contentCanvas.ActualHeight) then
                hideLocator()
            )
        let dungeonCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7))  // draw e.g. rooms here
        let dungeonSourceHighlightCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7))  // draw grab-source highlights here
        let dungeonHighlightCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7))  // draw grab highlights here
        canvasAdd(contentCanvas, dungeonCanvas, 0., 0.)
        canvasAdd(contentCanvas, dungeonSourceHighlightCanvas, 0., 0.)
        canvasAdd(contentCanvas, dungeonHighlightCanvas, 0., 0.)

        levelTab.Content <- contentCanvas
        dungeonTabs.Height <- dungeonCanvas.Height + 30.   // ok to set this 9 times
        dungeonTabs.Items.Add(levelTab) |> ignore

        let TEXT = sprintf "LEVEL-%d " level
        // horizontal doors
        let unknown = Dungeon.unknown
        let no = Dungeon.no
        let yes = Dungeon.yes
        let blackedOut = Dungeon.blackedOut
        let horizontalDoors = Array2D.zeroCreate 7 8
        for i = 0 to 6 do
            for j = 0 to 7 do
                let d = new Canvas(Width=12., Height=16., Background=Brushes.Black)
                let rect = new Shapes.Rectangle(Width=12., Height=16., Stroke=unknown, StrokeThickness=2., Fill=unknown)
                let line = new Shapes.Line(X1 = 6., Y1 = -11., X2 = 6., Y2 = 27., StrokeThickness=2., Stroke=no, Opacity=0.)
                d.Children.Add(rect) |> ignore
                d.Children.Add(line) |> ignore
                let door = new Dungeon.Door(Dungeon.DoorState.UNKNOWN, (function 
                    | Dungeon.DoorState.YES        -> rect.Stroke <- yes; rect.Fill <- yes; rect.Opacity <- 1.; line.Opacity <- 0.
                    | Dungeon.DoorState.NO         -> rect.Opacity <- 0.; line.Opacity <- 1.
                    | Dungeon.DoorState.BLACKEDOUT -> rect.Stroke <- blackedOut; rect.Fill <- blackedOut; rect.Opacity <- 1.; line.Opacity <- 0.
                    | Dungeon.DoorState.UNKNOWN    -> rect.Stroke <- unknown; rect.Fill <- unknown; rect.Opacity <- 1.; line.Opacity <- 0.))
                horizontalDoors.[i,j] <- door
                canvasAdd(dungeonCanvas, d, float(i*(39+12)+39), float(TH+j*(27+12)+6))
                let left _ =        
                    if not grabHelper.IsGrabMode then  // cannot interact with doors in grab mode
                        if door.State <> Dungeon.DoorState.YES then
                            door.State <- Dungeon.DoorState.YES
                        else
                            door.State <- Dungeon.DoorState.UNKNOWN
                d.MouseLeftButtonDown.Add(left)
                let right _ = 
                    if not grabHelper.IsGrabMode then  // cannot interact with doors in grab mode
                        if door.State <> Dungeon.DoorState.NO then
                            door.State <- Dungeon.DoorState.NO
                        else
                            door.State <- Dungeon.DoorState.UNKNOWN
                d.MouseRightButtonDown.Add(right)
        // vertical doors
        let verticalDoors = Array2D.zeroCreate 8 7
        for i = 0 to 7 do
            for j = 0 to 6 do
                let d = new Canvas(Width=24., Height=12., Background=Brushes.Black)
                let rect = new Shapes.Rectangle(Width=24., Height=12., Stroke=unknown, StrokeThickness=2., Fill=unknown)
                let line = new Shapes.Line(X1 = -13., Y1 = 6., X2 = 37., Y2 = 6., StrokeThickness=2., Stroke=no, Opacity=0.)
                d.Children.Add(rect) |> ignore
                d.Children.Add(line) |> ignore
                let door = new Dungeon.Door(Dungeon.DoorState.UNKNOWN, (function 
                    | Dungeon.DoorState.YES        -> rect.Stroke <- yes; rect.Fill <- yes; rect.Opacity <- 1.; line.Opacity <- 0.
                    | Dungeon.DoorState.NO         -> rect.Opacity <- 0.; line.Opacity <- 1.
                    | Dungeon.DoorState.BLACKEDOUT -> rect.Stroke <- blackedOut; rect.Fill <- blackedOut; rect.Opacity <- 1.; line.Opacity <- 0.
                    | Dungeon.DoorState.UNKNOWN    -> rect.Stroke <- unknown; rect.Fill <- unknown; rect.Opacity <- 1.; line.Opacity <- 0.))
                verticalDoors.[i,j] <- door
                canvasAdd(dungeonCanvas, d, float(i*(39+12)+8), float(TH+j*(27+12)+27))
                let left _ =
                    if not grabHelper.IsGrabMode then  // cannot interact with doors in grab mode
                        if door.State <> Dungeon.DoorState.YES then
                            door.State <- Dungeon.DoorState.YES
                        else
                            door.State <- Dungeon.DoorState.UNKNOWN
                d.MouseLeftButtonDown.Add(left)
                let right _ = 
                    if not grabHelper.IsGrabMode then  // cannot interact with doors in grab mode
                        if door.State <> Dungeon.DoorState.NO then
                            door.State <- Dungeon.DoorState.NO
                        else
                            door.State <- Dungeon.DoorState.UNKNOWN
                d.MouseRightButtonDown.Add(right)
        // rooms
        let roomCanvases = Array2D.zeroCreate 8 8 
        let roomStates = Array2D.zeroCreate 8 8 // 1-9 = transports, see roomBMPpairs() below for rest
        let roomIsCircled = Array2D.zeroCreate 8 8
        let roomCompleted = Array2D.zeroCreate 8 8 
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
                        dungeonTabs.Cursor <- System.Windows.Input.Cursors.Hand
                        grabModeTextBlock.Opacity <- 1.
                    else
                        grabHelper.Abort()
                        dungeonHighlightCanvas.Children.Clear()
                        dungeonSourceHighlightCanvas.Children.Clear()
                        tb.Foreground <- Brushes.Gray
                        tb.Background <- Brushes.Black
                        dungeonTabs.Cursor <- null
                        grabModeTextBlock.Opacity <- 0.
                    )
                tb.PreviewMouseLeftButtonDown.Add(fun _ ->
                    grabHelper.ToggleGrabMode()
                    grabRedraw()
                    )
            else
                // LEVEL-9        
                let tb = new TextBox(Width=float(13*3), Height=float(TH), FontSize=float(TH-4), Foreground=Brushes.White, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false,
                                        Text=TEXT.Substring(i,1), BorderThickness=Thickness(0.), FontFamily=new FontFamily("Courier New"), FontWeight=FontWeights.Bold)
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
                        roomBMPpairs(roomStates.[i,j])
                        |> (fun (u,c) -> if roomCompleted.[i,j] then c else u)
                        |> Graphics.BMPtoImage 
                    canvasAdd(c, image, 0., 0.)
                    if roomIsCircled.[i,j] then
                        let ellipse = new Shapes.Ellipse(Width=float(13*3+12), Height=float(9*3+12), Stroke=Brushes.Yellow, StrokeThickness=3.)
                        ellipse.StrokeDashArray <- new DoubleCollection( seq[0.;2.5;6.;5.;6.;5.;6.;5.;6.;5.] )
                        canvasAdd(c, ellipse, -6., -6.)
                roomRedrawFuncs.Add(fun () -> redraw())
                let usedTransportsRemoveState(roomState) =
                    // track transport being changed away from
                    if [1..9] |> List.contains roomState then
                        usedTransports.[roomState] <- usedTransports.[roomState] - 1
                let usedTransportsAddState(roomState) =
                    // note any new transports
                    if [1..9] |> List.contains roomState then
                        usedTransports.[roomState] <- usedTransports.[roomState] + 1
                let immediateActivatePopup, delayedActivatePopup =
                    let activatePopup(activationDelta) =
                        if not(popupState=Dungeon.DelayedPopupState.SOON) then () (*printfn "witness self canceled"*) else   // if we are cancelled, do nothing
                        popupState <- Dungeon.DelayedPopupState.ACTIVE_NOW
                        //printfn "activating"
                        let ST = CustomComboBoxes.borderThickness
                        let tileCanvas = new Canvas(Width=13.*3., Height=9.*3.)
                        let originalStateIndex = if roomStates.[i,j] < 10 then roomStates.[i,j] else roomStates.[i,j] - 1
                        let gridElementsSelectablesAndIDs : (FrameworkElement*bool*int)[] = Array.init (ROOMS-1) (fun n ->
                            let tweak(im:Image) = im.Opacity <- 0.8; im
                            if n < 10 then
                                upcast tweak(Graphics.BMPtoImage(fst(roomBMPpairs(n)))), not(usedTransports.[n]=2) || n=originalStateIndex, n
                            else
                                upcast tweak(Graphics.BMPtoImage(fst(roomBMPpairs(n+1)))), true, n+1
                            )
                        let roomPos = c.TranslatePoint(Point(), appMainCanvas)
                        let gridxPosition = 13.*3. + ST
                        let gridYPosition = 0.-5.*9.*3.-ST
                        let h = 9.*3.*2.+ST*4.
                        CustomComboBoxes.DoModalGridSelect(appMainCanvas, roomPos.X, roomPos.Y, tileCanvas,
                            gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (5, 5, 13*3, 9*3), gridxPosition, gridYPosition,
                            (fun (currentState) -> 
                                tileCanvas.Children.Clear()
                                let tileBMP = roomBMPpairs(currentState) |> (fun (u,c) -> u)
                                canvasAdd(tileCanvas, Graphics.BMPtoImage tileBMP, 0., 0.)),
                            (fun (dismissPopup, ea, currentState) ->
                                if (ea.ChangedButton = Input.MouseButton.Left || ea.ChangedButton = Input.MouseButton.Right) && ea.ButtonState = Input.MouseButtonState.Pressed then
                                    usedTransportsRemoveState(roomStates.[i,j])
                                    roomStates.[i,j] <- currentState
                                    usedTransportsAddState(roomStates.[i,j])
                                    roomCompleted.[i,j] <- ea.ChangedButton = Input.MouseButton.Left
                                    redraw()
                                    dismissPopup()
                                    popupState <- Dungeon.DelayedPopupState.NONE
                                ),
                            (fun () -> popupState <- Dungeon.DelayedPopupState.NONE),   // onClose
                            [dungeonRoomMouseButtonExplainerDecoration, gridxPosition, gridYPosition-h-ST],
                            CustomComboBoxes.ModalGridSelectBrushes.Defaults())
                    let now(ad) =
                        if not(popupState=Dungeon.DelayedPopupState.ACTIVE_NOW) then
                            popupState <- Dungeon.DelayedPopupState.SOON
                            activatePopup(ad)
                    let soon(ad) =
                        if popupState=Dungeon.DelayedPopupState.NONE then
                            //printfn "soon scheduling"
                            popupState <- Dungeon.DelayedPopupState.SOON
                            async {
                                do! Async.Sleep(250)
                                appMainCanvas.Dispatcher.Invoke(fun _ -> activatePopup(ad))
                            } |> Async.Start
                    now, soon
                let BUFFER = 2.
                let highlightImpl(canvas,contiguous:_[,], brush) =
                    for x = 0 to 7 do
                        for y = 0 to 7 do
                            if contiguous.[x,y] then
                                let r = new Shapes.Rectangle(Width=float(13*3 + 12), Height=float(9*3 + 12), Fill=brush, Opacity=0.4, IsHitTestVisible=false)  // TODO creating lots of garbage
                                canvasAdd(canvas, r, float(x*51 - 6), float(TH+y*39 - 6))
                let highlight(contiguous:_[,], brush) = highlightImpl(dungeonHighlightCanvas,contiguous,brush)
                c.MouseEnter.Add(fun _ ->
                    if popupState <> Dungeon.DelayedPopupState.ACTIVE_NOW then
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
                c.MouseLeave.Add(fun _ ->
                    if popupState <> Dungeon.DelayedPopupState.ACTIVE_NOW then
                        if grabHelper.IsGrabMode then
                            dungeonHighlightCanvas.Children.Clear() // clear old preview
                    )
                c.MouseWheel.Add(fun x -> 
                    if popupState <> Dungeon.DelayedPopupState.ACTIVE_NOW then
                        if not grabHelper.IsGrabMode then  // cannot scroll rooms in grab mode
                            // scroll wheel activates the popup selector
                            let activationDelta = if x.Delta<0 then 1 else -1
                            immediateActivatePopup(activationDelta)
                        )
                Graphics.setupClickVersusDrag(c, (fun ea ->
                    if popupState <> Dungeon.DelayedPopupState.ACTIVE_NOW then
                        let pos = ea.GetPosition(c)
                        // I often accidentally click room when trying to target doors with mouse, only do certain actions when isInterior
                        let isInterior = not(pos.X < BUFFER || pos.X > c.Width-BUFFER || pos.Y < BUFFER || pos.Y > c.Height-BUFFER)
                        if ea.ChangedButton = Input.MouseButton.Left then
                            if grabHelper.IsGrabMode then
                                if not grabHelper.HasGrab then
                                    if roomStates.[i,j] <> 0 && roomStates.[i,j] <> 10 then
                                        dungeonHighlightCanvas.Children.Clear() // clear preview
                                        let contiguous = grabHelper.StartGrab(i,j,roomStates,roomIsCircled,roomCompleted,horizontalDoors,verticalDoors)
                                        highlightImpl(dungeonSourceHighlightCanvas, contiguous, Brushes.Pink)  // this highlight stays around until completed/aborted
                                        highlight(contiguous, Brushes.Lime)
                                else
                                    let backupRoomStates = roomStates.Clone() :?> int[,]
                                    let backupRoomIsCircled = roomIsCircled.Clone() :?> bool[,]
                                    let backupRoomCompleted = roomCompleted.Clone() :?> bool[,]
                                    let backupHorizontalDoors = horizontalDoors |> Array2D.map (fun c -> c.State)
                                    let backupVerticalDoors = verticalDoors |> Array2D.map (fun c -> c.State)
                                    grabHelper.DoDrop(i,j,roomStates,roomIsCircled,roomCompleted,horizontalDoors,verticalDoors)
                                    redrawAllRooms()  // make updated changes visual
                                    let cmb = new CustomMessageBox.CustomMessageBox("Verify changes", System.Drawing.SystemIcons.Question, "You moved a dungeon segment. Keep this change?", ["Keep changes"; "Undo"])
                                    cmb.Owner <- Window.GetWindow(c)
                                    cmb.ShowDialog() |> ignore
                                    grabRedraw()  // DoDrop completes the grab, neeed to update the visual
                                    if cmb.MessageBoxResult = null || cmb.MessageBoxResult = "Undo" then
                                        // copy back from old state
                                        backupRoomStates |> Array2D.iteri (fun x y v -> roomStates.[x,y] <- v)
                                        backupRoomIsCircled |> Array2D.iteri (fun x y v -> roomIsCircled.[x,y] <- v)
                                        backupRoomCompleted |> Array2D.iteri (fun x y v -> roomCompleted.[x,y] <- v)
                                        redrawAllRooms()  // make reverted changes visual
                                        horizontalDoors |> Array2D.iteri (fun x y c -> c.State <- backupHorizontalDoors.[x,y])
                                        verticalDoors |> Array2D.iteri (fun x y c -> c.State <- backupVerticalDoors.[x,y])
                            else
                                if isInterior then
                                    if System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift) then
                                        // shift click an unexplored room to mark not-on-map rooms (by "blackedOut"ing all the connections)
                                        if roomStates.[i,j] = 0 then
                                            if i > 0 then
                                                horizontalDoors.[i-1,j].State <- Dungeon.DoorState.BLACKEDOUT
                                            if i < 7 then
                                                horizontalDoors.[i,j].State <- Dungeon.DoorState.BLACKEDOUT
                                            if j > 0 then
                                                verticalDoors.[i,j-1].State <- Dungeon.DoorState.BLACKEDOUT
                                            if j < 7 then
                                                verticalDoors.[i,j].State <- Dungeon.DoorState.BLACKEDOUT
                                            roomStates.[i,j] <- 10
                                            roomCompleted.[i,j] <- true
                                            redraw()
                                        // shift click a blackedOut room to undo it back to unknown
                                        elif roomStates.[i,j] = 10 then
                                            if i > 0 && horizontalDoors.[i-1,j].State = Dungeon.DoorState.BLACKEDOUT then
                                                horizontalDoors.[i-1,j].State <- Dungeon.DoorState.UNKNOWN
                                            if i < 7 && horizontalDoors.[i,j].State = Dungeon.DoorState.BLACKEDOUT then
                                                horizontalDoors.[i,j].State <- Dungeon.DoorState.UNKNOWN
                                            if j > 0 && verticalDoors.[i,j-1].State = Dungeon.DoorState.BLACKEDOUT then
                                                verticalDoors.[i,j-1].State <- Dungeon.DoorState.UNKNOWN
                                            if j < 7 && verticalDoors.[i,j].State = Dungeon.DoorState.BLACKEDOUT then
                                                verticalDoors.[i,j].State <- Dungeon.DoorState.UNKNOWN
                                            roomStates.[i,j] <- 0
                                            roomCompleted.[i,j] <- false
                                            redraw()
                                    else
                                        if roomStates.[i,j] = 0 then
                                            // ad hoc useful gesture for clicking unknown room - it moves it to explored & completed state
                                            roomStates.[i,j] <- ROOMS-1
                                            roomCompleted.[i,j] <- true
                                        redraw()
                                        if popupState=Dungeon.DelayedPopupState.SOON then
                                            //printfn "click canceling"
                                            popupState <- Dungeon.DelayedPopupState.NONE // we clicked again before it activated, cancel it
                                        else
                                            delayedActivatePopup(0)
                        elif ea.ChangedButton = Input.MouseButton.Right then
                            if not grabHelper.IsGrabMode then  // cannot right click rooms in grab mode
                                if isInterior then
                                    if roomStates.[i,j] = 0 then
                                        // ad hoc useful gesture for right-clicking unknown room - it moves it to explored & uncompleted state
                                        roomStates.[i,j] <- ROOMS-1
                                        roomCompleted.[i,j] <- false
                                    redraw()
                                    if popupState=Dungeon.DelayedPopupState.SOON then
                                        popupState <- Dungeon.DelayedPopupState.NONE // we clicked again before it activated, cancel it
                                    else
                                        delayedActivatePopup(0)
                        elif ea.ChangedButton = Input.MouseButton.Middle then
                            if not grabHelper.IsGrabMode then  // cannot middle click rooms in grab mode
                                if isInterior then
                                    roomIsCircled.[i,j] <- not roomIsCircled.[i,j]
                                    redraw()
                    ), (fun ea ->
                    if popupState <> Dungeon.DelayedPopupState.ACTIVE_NOW then
                        // drag and drop to quickly 'paint' rooms
                        if not grabHelper.IsGrabMode then  // cannot initiate a drag in grab mode
                            if ea.LeftButton = System.Windows.Input.MouseButtonState.Pressed then
                                DragDrop.DoDragDrop(c, "L", DragDropEffects.Link) |> ignore
                            elif ea.RightButton = System.Windows.Input.MouseButtonState.Pressed then
                                DragDrop.DoDragDrop(c, "R", DragDropEffects.Link) |> ignore
                    ))
                c.DragOver.Add(fun ea ->
                    if popupState <> Dungeon.DelayedPopupState.ACTIVE_NOW then
                        if roomStates.[i,j] = 0 then
                            if ea.Data.GetData(DataFormats.StringFormat) :?> string = "L" then
                                roomStates.[i,j] <- ROOMS-1
                                roomCompleted.[i,j] <- true
                            else
                                roomStates.[i,j] <- ROOMS-1
                                roomCompleted.[i,j] <- false
                            redraw()
                    )
                c.AllowDrop <- true
        for quest,outlines in [| (DungeonData.firstQuest.[level-1], fixedDungeon1Outlines); (DungeonData.secondQuest.[level-1], fixedDungeon2Outlines) |] do
            // fixed dungeon drawing outlines - vertical segments
            for i = 0 to 6 do
                for j = 0 to 7 do
                    if quest.[j].Chars(i) <> quest.[j].Chars(i+1) then
                        let s = new System.Windows.Shapes.Line(X1=float(i*(39+12)+39+12/2), X2=float(i*(39+12)+39+12/2), Y1=float(TH+j*(27+12)-12/2), Y2=float(TH+j*(27+12)+27+12/2), 
                                        Stroke=Brushes.Red, StrokeThickness=3., IsHitTestVisible=false, Opacity=0.0)
                        canvasAdd(dungeonCanvas, s, 0., 0.)
                        outlines.Add(s)
            // fixed dungeon drawing outlines - horizontal segments
            for i = 0 to 7 do
                for j = 0 to 6 do
                    if quest.[j].Chars(i) <> quest.[j+1].Chars(i) then
                        let s = new System.Windows.Shapes.Line(X1=float(i*(39+12)-12/2), X2=float(i*(39+12)+39+12/2), Y1=float(TH+(j+1)*(27+12)-12/2), Y2=float(TH+(j+1)*(27+12)-12/2), 
                                        Stroke=Brushes.Red, StrokeThickness=3., IsHitTestVisible=false, Opacity=0.0)
                        canvasAdd(dungeonCanvas, s, 0., 0.)
                        outlines.Add(s)
        // "sunglasses"
        let darkenRect = new Shapes.Rectangle(Width=dungeonCanvas.Width, Height=dungeonCanvas.Height, StrokeThickness = 0., Fill=Brushes.Black, Opacity=0.15, IsHitTestVisible=false)
        canvasAdd(dungeonCanvas, darkenRect, 0., 0.)
    dungeonTabs.SelectedIndex <- 8

    let fqcb = new CheckBox(Content=new TextBox(Text="FQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    fqcb.ToolTip <- "Show vanilla first quest dungeon outlines"
    let sqcb = new CheckBox(Content=new TextBox(Text="SQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    sqcb.ToolTip <- "Show vanilla second quest dungeon outlines"

    fqcb.IsChecked <- System.Nullable.op_Implicit false
    fqcb.Checked.Add(fun _ -> fixedDungeon1Outlines |> Seq.iter (fun s -> s.Opacity <- 1.0); sqcb.IsChecked <- System.Nullable.op_Implicit false)
    fqcb.Unchecked.Add(fun _ -> fixedDungeon1Outlines |> Seq.iter (fun s -> s.Opacity <- 0.0))
    canvasAdd(appMainCanvas, fqcb, 310., START_DUNGEON_AND_NOTES_AREA_H) 

    sqcb.IsChecked <- System.Nullable.op_Implicit false
    sqcb.Checked.Add(fun _ -> fixedDungeon2Outlines |> Seq.iter (fun s -> s.Opacity <- 1.0); fqcb.IsChecked <- System.Nullable.op_Implicit false)
    sqcb.Unchecked.Add(fun _ -> fixedDungeon2Outlines |> Seq.iter (fun s -> s.Opacity <- 0.0))
    canvasAdd(appMainCanvas, sqcb, 360., START_DUNGEON_AND_NOTES_AREA_H) 

    canvasAdd(appMainCanvas, dungeonTabsOverlay, 0., START_DUNGEON_AND_NOTES_AREA_H)

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
        let make() =
            let c = new Canvas(Width=30., Height=30., Background=Brushes.Black, IsHitTestVisible=true)
            let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=Brushes.Gray, StrokeThickness=3.0, IsHitTestVisible=false)
            c.Children.Add(rect) |> ignore
            let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent, IsHitTestVisible=false)  // just has item drawn on it, not the box
            c.Children.Add(innerc) |> ignore
            let redraw(n) =
                innerc.Children.Clear()
                let bmp = blockerCurrentBMP(n)
                if bmp <> null then
                    let image = Graphics.BMPtoImage(bmp)
                    image.IsHitTestVisible <- false
                    canvasAdd(innerc, image, 4., 4.)
            c, redraw
        let c,redraw = make()
        let mutable current = TrackerModel.DungeonBlocker.NOTHING
        redraw(current)
        let mutable popupIsActive = false
        let activate(activationDelta) =
            popupIsActive <- true
            let pc, predraw = make()
            let pos = c.TranslatePoint(Point(), appMainCanvas)
            CustomComboBoxes.DoModalGridSelect(appMainCanvas, pos.X, pos.Y, pc, TrackerModel.DungeonBlocker.All |> Array.map (fun db ->
                    (if db=TrackerModel.DungeonBlocker.NOTHING then upcast Canvas() else upcast Graphics.BMPtoImage(blockerCurrentBMP(db))), true, db), 
                    Array.IndexOf(TrackerModel.DungeonBlocker.All, current), activationDelta, (3, 3, 21, 21), -60., 30., predraw,
                    (fun (dismissPopup,ea,db) -> 
                        current <- db
                        redraw(db)
                        dismissPopup()
                        popupIsActive <- false), (fun()-> popupIsActive <- false), [], CustomComboBoxes.ModalGridSelectBrushes.Defaults())
        c.MouseWheel.Add(fun x -> if not popupIsActive then activate(if x.Delta<0 then 1 else -1))
        c.MouseDown.Add(fun x -> if not popupIsActive then activate(0))
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
                tb.ToolTip <- "The icons you set in this area can remind you of what blocked you in a dungeon.\nFor example, a ladder represents being ladder blocked, or a sword means you need better weapons.\nSome voice reminders will trigger when you get the item that may unblock you."
                d.Children.Add(tb) |> ignore
                gridAdd(blockerGrid, d, i, j)
            else
                let dungeonNumeral = (3*j+i)
                let d = new DockPanel(LastChildFill=false)
                let sp = new StackPanel(Orientation=Orientation.Horizontal)
                let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text=sprintf "%d" dungeonNumeral, Width=30., 
                                        VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), 
                                        TextAlignment=TextAlignment.Right, Margin=Thickness(0.,0.,6.,0.))
                sp.Children.Add(tb) |> ignore
                sp.Children.Add(makeBlockerBox(dungeonNumeral-1, 0)) |> ignore
                sp.Children.Add(makeBlockerBox(dungeonNumeral-1, 1)) |> ignore
                d.Children.Add(sp) |> ignore
                gridAdd(blockerGrid, d, i, j)
                blockerDungeonSunglasses.[dungeonNumeral-1] <- upcast sp // just reduce its opacity
    canvasAdd(appMainCanvas, blockerGrid, 402., START_DUNGEON_AND_NOTES_AREA_H) 

    // notes    
    let tb = new TextBox(Width=appMainCanvas.Width-402., Height=dungeonTabs.Height - blockerGrid.Height)
    notesTextBox <- tb
    tb.FontSize <- 24.
    tb.Foreground <- System.Windows.Media.Brushes.LimeGreen 
    tb.Background <- System.Windows.Media.Brushes.Black 
    tb.Text <- "Notes\n"
    tb.AcceptsReturn <- true
    canvasAdd(appMainCanvas, tb, 402., START_DUNGEON_AND_NOTES_AREA_H + blockerGrid.Height) 

    grabModeTextBlock.Opacity <- 0.
    grabModeTextBlock.Width <- tb.Width
    canvasAdd(appMainCanvas, grabModeTextBlock, 402., START_DUNGEON_AND_NOTES_AREA_H) 

    let THRU_DUNGEON_AND_NOTES_AREA_H = START_DUNGEON_AND_NOTES_AREA_H + float(TH + 30 + 27*8 + 12*7 + 3)  // 3 is for a little blank space after this but before timeline

    // remaining OW spots
    canvasAdd(appMainCanvas, owRemainingScreensTextBox, RIGHT_COL, 90.)
    canvasAdd(appMainCanvas, owGettableScreensCheckBox, RIGHT_COL, 110.)
    owGettableScreensCheckBox.Checked.Add(fun _ -> TrackerModel.forceUpdate()) 
    owGettableScreensCheckBox.Unchecked.Add(fun _ -> TrackerModel.forceUpdate())
    owGettableScreensTextBox.MouseEnter.Add(fun _ -> 
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, 
                                            TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), Point(0.,0.), 0, 0, false, 128)
        )
    owGettableScreensTextBox.MouseLeave.Add(fun _ -> 
        if not(owGettableScreensCheckBox.IsChecked.HasValue) || not(owGettableScreensCheckBox.IsChecked.Value) then 
            routeDrawingCanvas.Children.Clear()
        )
    // current hearts
    canvasAdd(appMainCanvas, currentHeartsTextBox, RIGHT_COL, 130.)
    // coordinate grid
    let owCoordsGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owCoordsTBs = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let tb = new TextBox(Text=sprintf "%c  %d" (char (int 'A' + j)) (i+1),  // may change with OMTW and overall layout
                                    Foreground=Brushes.White, Background=Brushes.Transparent, BorderThickness=Thickness(0.0), 
                                    FontFamily=FontFamily("Consolas"), FontSize=16.0, FontWeight=FontWeights.Bold)
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
    showCoords.MouseEnter.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.85))
    showCoords.MouseLeave.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.0))
    canvasAdd(appMainCanvas, cb, RIGHT_COL + 140., 130.)

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
        let line = new System.Windows.Shapes.Line(X1=OMTW*float(x1), X2=OMTW*float(x2), Y1=float(y1*11*3), Y2=float(y2*11*3), Stroke=Brushes.White, StrokeThickness=3.)
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
        let tb = new TextBox(Text=name,FontSize=16.,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(2.),IsReadOnly=true)
        mirrorOverworldFEs.Add(tb)
        canvasAdd(overworldCanvas, tb, x*OMTW, y*11.*3.)
        tb.Opacity <- 0.
        tb.TextAlignment <- TextAlignment.Center
        tb.FontWeight <- FontWeights.Bold
        tb.IsHitTestVisible <- false
        zoneNames.Add(hz,tb)

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
    let zone_checkbox = new CheckBox(Content=new TextBox(Text="Show zones",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    zone_checkbox.IsChecked <- System.Nullable.op_Implicit false
    zone_checkbox.Checked.Add(fun _ -> changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,true))
    zone_checkbox.Unchecked.Add(fun _ -> changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,false))
    zone_checkbox.MouseEnter.Add(fun _ -> if not zone_checkbox.IsChecked.HasValue || not zone_checkbox.IsChecked.Value then changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,true))
    zone_checkbox.MouseLeave.Add(fun _ -> if not zone_checkbox.IsChecked.HasValue || not zone_checkbox.IsChecked.Value then changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,false))
    canvasAdd(appMainCanvas, zone_checkbox, 285., 130.)

    let owLocatorGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owLocatorTilesRowColumn = Array2D.zeroCreate 16 8
    let owLocatorTilesZone = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let rc = new Shapes.Rectangle(Width=OMTW, Height=float(11*3), Stroke=Brushes.Transparent, StrokeThickness=0., Fill=Brushes.White, Opacity=0., IsHitTestVisible=false)
            let z  = new Shapes.Rectangle(Width=OMTW, Height=float(11*3), Stroke=Brushes.Transparent, StrokeThickness=12., Fill=Brushes.Lime, Opacity=0., IsHitTestVisible=false)
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

    // timeline & options menu
    let START_TIMELINE_H = THRU_DUNGEON_AND_NOTES_AREA_H

    let moreOptionsLabel = new TextBox(Text="Options...", Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Margin=Thickness(0.), Padding=Thickness(0.), BorderThickness=Thickness(0.), IsReadOnly=true, IsHitTestVisible=false)
    let moreOptionsButton = new Button(MaxHeight=25., Content=moreOptionsLabel, BorderThickness=Thickness(1.), Margin=Thickness(0.), Padding=Thickness(0.))
    moreOptionsButton.Measure(new Size(System.Double.PositiveInfinity, 25.))

    let optionsCanvas = OptionsMenu.makeOptionsCanvas(appMainCanvas.Width, moreOptionsButton.DesiredSize.Height)
    optionsCanvas.Opacity <- 1.
    optionsCanvas.IsHitTestVisible <- true

    let theTimeline1 = new Timeline.Timeline(21., 4, 60, 5, appMainCanvas.Width-48., 1, "0h", "30m", "1h", moreOptionsButton.DesiredSize.Width-24.)
    let theTimeline2 = new Timeline.Timeline(21., 4, 60, 5, appMainCanvas.Width-48., 2, "0h", "1h", "2h", moreOptionsButton.DesiredSize.Width-24.)
    let theTimeline3 = new Timeline.Timeline(21., 4, 60, 5, appMainCanvas.Width-48., 3, "0h", "1.5h", "3h", moreOptionsButton.DesiredSize.Width-24.)
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
    canvasAdd(appMainCanvas, theTimeline1.Canvas, 24., START_TIMELINE_H)
    canvasAdd(appMainCanvas, theTimeline2.Canvas, 24., START_TIMELINE_H)
    canvasAdd(appMainCanvas, theTimeline3.Canvas, 24., START_TIMELINE_H)

    canvasAdd(appMainCanvas, moreOptionsButton, 0., START_TIMELINE_H)
    moreOptionsButton.Click.Add(fun _ -> CustomComboBoxes.DoModalDocked(appMainCanvas, Dock.Bottom, optionsCanvas, (fun() -> TrackerModel.Options.writeSettings())) |> ignore)

    let THRU_TIMELINE_H = START_TIMELINE_H + float TCH + 6.

    //                items  ow map  prog  dungeon tabs                timeline
    appMainCanvas.Height <- float(30*5 + 11*3*9 + 30 + TH + 30 + 27*8 + 12*7 + 3 + TCH + 6)


    //Dungeon.drawDungeonColorGrid(appMainCanvas)

    TrackerModel.forceUpdate()
    appMainCanvas, updateTimeline


