module UI

open System
open Avalonia
open Avalonia.Controls
open Avalonia.Media

type MapStateProxy(state) =
    let U = Graphics.uniqueMapIconBMPs.Length 
    let NU = Graphics.nonUniqueMapIconBMPs.Length
    member this.State = state
    member this.IsX = state = U+NU-1
    member this.IsUnique = state >= 0 && state < U
    member this.IsDungeon = state >= 0 && state < 9
    member this.IsWarp = state >= 9 && state < 13
    member this.IsSword3 = state=13
    member this.IsSword2 = state=14
    member this.IsThreeItemShop = state >=16 && state <=22
    member this.HasTransparency = state >= 0 && state < 13 || state >= U+1 && state < U+8   // dungeons, warps, swords, and item-shops
    member this.IsInteresting = not(state = -1 || this.IsX)
    member this.Current() =
        if state = -1 then
            null
        elif state < U then
            Graphics.uniqueMapIconBMPs.[state]
        else
            Graphics.nonUniqueMapIconBMPs.[state-U]

let canvasAdd(c:Canvas, item, left, top) =
    if item <> null then
        c.Children.Add(item) |> ignore
        Canvas.SetTop(item, top)
        Canvas.SetLeft(item, left)
let gridAdd(g:Grid, x, c, r) =
    g.Children.Add(x) |> ignore
    Grid.SetColumn(x, c)
    Grid.SetRow(x, r)
let makeGrid(nc, nr, cw, rh) =
    let grid = new Grid()
    for i = 0 to nc-1 do
        grid.ColumnDefinitions.Add(new ColumnDefinition(Width=GridLength(float cw)))
    for i = 0 to nr-1 do
        grid.RowDefinitions.Add(new RowDefinition(Height=GridLength(float rh)))
    grid

let triforceInnerCanvases = Array.zeroCreate 8
let mainTrackerCanvases : Canvas[,] = Array2D.zeroCreate 8 4
let mainTrackerCanvasShaders : Canvas[,] = Array2D.init 8 4 (fun _ _ -> new Canvas(Width=30., Height=30., Background=Brushes.Black, Opacity=0.4))
let currentHeartsTextBox = new TextBox(Width=200., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "Current Hearts: %d" TrackerModel.playerComputedStateSummary.PlayerHearts)
let owRemainingScreensCheckBox = new CheckBox(Content = new TextBox(Width=150., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "OW spots left: %d" TrackerModel.mapStateSummary.OwSpotsRemain))

type TimelineItem(displayBMP, isHeart, isDone:unit->bool) =
    member this.DisplayBMP() = displayBMP()
    member this.IsHeart() = isHeart()
    member this.IsDone() = isDone()

let mutable f5WasRecentlyPressed = false
let mutable currentlyMousedOWX, currentlyMousedOWY = -1, -1
let mutable notesTextBox = null : TextBox
let mutable timeTextBox = null : TextBox
let H = 30
let RIGHT_COL = 440.
let TLH = (1+9+5+9)*3  // timeline height
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
    
    let c = new Canvas()
    c.Width <- 16. * OMTW

    c.Background <- Brushes.Black 

    let mainTracker = makeGrid(9, 4, H, H)
    canvasAdd(c, mainTracker, 0., 0.)

    // triforce
    let updateEmptyTriforceDisplay(i) =
        let innerc : Canvas = triforceInnerCanvases.[i]
        innerc.Children.Clear()
        innerc.Children.Add(if TrackerModel.mapStateSummary.DungeonLocations.[i]=TrackerModel.NOTFOUND then Graphics.emptyUnfoundTriforces.[i] else Graphics.emptyFoundTriforces.[i]) |> ignore
    for i = 0 to 7 do
        let image = Graphics.emptyUnfoundTriforces.[i]
        let c = new Canvas(Width=30., Height=30.)
        mainTrackerCanvases.[i,0] <- c
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has triforce drawn on it, not the eventual shading of updateDungeon()
        triforceInnerCanvases.[i] <- innerc
        c.Children.Add(innerc) |> ignore
        canvasAdd(innerc, image, 0., 0.)
        c.PointerPressed.Add(fun _ -> 
            if not(TrackerModel.dungeons.[i].PlayerHasTriforce()) then 
                innerc.Children.Clear()
                innerc.Children.Add(Graphics.fullTriforces.[i]) |> ignore 
            else 
                updateEmptyTriforceDisplay(i)
            TrackerModel.dungeons.[i].ToggleTriforce()
        )
        gridAdd(mainTracker, c, i, 0)
        timelineItems.Add(new TimelineItem((fun()->Graphics.fullTriforce_bmps.[i]), (fun()->false), (fun()->TrackerModel.dungeons.[i].PlayerHasTriforce())))
    let boxItemImpl(box:TrackerModel.Box) = 
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let no = Brushes.DarkRed
        let yes = Brushes.LimeGreen 
        let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=no, StrokeThickness=3.0)
        c.Children.Add(rect) |> ignore
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has item drawn on it, not the box
        c.Children.Add(innerc) |> ignore
        c.PointerPressed.Add(fun ea -> if ea.GetCurrentPoint(c).Properties.IsLeftButtonPressed then (
            if obj.Equals(rect.Stroke, no) then
                rect.Stroke <- yes
            else
                rect.Stroke <- no
            box.TogglePlayerHas()
        ))
        let boxCurrentBMP() =
            match box.CellCurrent() with
            | -1 -> null
            |  0 -> (if !isCurrentlyBook then Graphics.book_bmp else Graphics.magic_shield_bmp)
            |  1 -> Graphics.boomerang_bmp
            |  2 -> Graphics.bow_bmp
            |  3 -> Graphics.power_bracelet_bmp
            |  4 -> Graphics.ladder_bmp
            |  5 -> Graphics.magic_boomerang_bmp
            |  6 -> Graphics.key_bmp
            |  7 -> Graphics.raft_bmp
            |  8 -> Graphics.recorder_bmp
            |  9 -> Graphics.red_candle_bmp
            | 10 -> Graphics.red_ring_bmp
            | 11 -> Graphics.silver_arrow_bmp
            | 12 -> Graphics.wand_bmp
            | 13 -> Graphics.white_sword_bmp
            |  _ -> Graphics.heart_container_bmp
        let redraw() =
            innerc.Children.Clear()
            let bmp = boxCurrentBMP()
            if bmp <> null then
                canvasAdd(innerc, Graphics.BMPtoImage(bmp), 4., 4.)
        // item
        c.PointerWheelChanged.Add(fun x -> 
            if x.Delta.Y<0. then
                box.CellNext()
            else
                box.CellPrev()
            redraw()
        )
        redrawBoxes.Add(fun() -> redraw())
        timelineItems.Add(new TimelineItem((fun()->boxCurrentBMP()), (fun()->box.CellCurrent()>13), (fun()->obj.Equals(rect.Stroke,yes))))
        c
    // items
    for i = 0 to 8 do
        for j = 0 to 2 do
            let mutable c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
            if j=0 || j=1 || (i=0 || i=7) then
                c <- boxItemImpl(TrackerModel.dungeons.[i].Boxes.[j])
                gridAdd(mainTracker, c, i, j+1)
            if i < 8 then
                mainTrackerCanvases.[i,j+1] <- c

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
    ToolTip.SetTip(hideFirstQuestCheckBox, "Hide First Quest\nIn a mixed quest overworld tracker, shade out the first-quest-only spots.\nUseful if you're unsure if randomizer flags are mixed quest or second quest.\nCan't be used if you've marked a first-quest-only spot as having something.")
    let hideSecondQuestCheckBox = new CheckBox(Content=new TextBox(Text="HSQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
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
        canvasAdd(c, hideFirstQuestCheckBox, 35., 90.) 

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
        canvasAdd(c, hideSecondQuestCheckBox, 140., 90.) 

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
        timelineItems.Add(new TimelineItem((fun()->null), (fun()->true), (fun()->TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(i)=1)))
    canvasAdd(c, owHeartGrid, OFFSET, 0.)
    // ladder, armos, white sword items
    let owItemGrid = makeGrid(2, 3, 30, 30)
    gridAdd(owItemGrid, Graphics.BMPtoImage Graphics.ladder_bmp, 0, 0)
    gridAdd(owItemGrid, Graphics.BMPtoImage Graphics.ow_key_armos_bmp, 0, 1)
    gridAdd(owItemGrid, Graphics.BMPtoImage Graphics.white_sword_bmp, 0, 2)
    gridAdd(owItemGrid, boxItemImpl(TrackerModel.ladderBox), 1, 0)
    gridAdd(owItemGrid, boxItemImpl(TrackerModel.armosBox), 1, 1)
    gridAdd(owItemGrid, boxItemImpl(TrackerModel.sword2Box), 1, 2)
    canvasAdd(c, owItemGrid, OFFSET, 30.)
    // brown sword, blue candle, blue ring, magical sword
    let owItemGrid = makeGrid(3, 2, 30, 30)
    let veryBasicBoxImpl(bmp:System.Drawing.Bitmap, startOn, isTimeline, changedFunc) =
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let no = Brushes.DarkRed
        let yes = Brushes.LimeGreen 
        let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=(if startOn then yes else no), StrokeThickness=3.0)
        c.Children.Add(rect) |> ignore
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has item drawn on it, not the box
        c.Children.Add(innerc) |> ignore
        c.PointerPressed.Add(fun ea -> if ea.GetCurrentPoint(c).Properties.IsLeftButtonPressed then (
            if obj.Equals(rect.Stroke, no) then
                rect.Stroke <- yes
            else
                rect.Stroke <- no
            changedFunc(obj.Equals(rect.Stroke, yes))
        ))
        canvasAdd(innerc, Graphics.BMPtoImage bmp, 4., 4.)
        if isTimeline then
            timelineItems.Add(new TimelineItem((fun()->bmp), (fun()->false), fun()->obj.Equals(rect.Stroke,yes)))
        c
    let basicBoxImpl(tts, img, changedFunc) =
        let c = veryBasicBoxImpl(img, false, true, changedFunc)
        ToolTip.SetTip(c, tts)
        c
    gridAdd(owItemGrid, basicBoxImpl("Acquired wood sword (mark timeline)",    Graphics.brown_sword_bmp  , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword.Toggle())), 1, 0)
    gridAdd(owItemGrid, basicBoxImpl("Acquired wood arrow (mark timeline)",    Graphics.wood_arrow_bmp   , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodArrow.Toggle())), 0, 1)
    gridAdd(owItemGrid, basicBoxImpl("Acquired blue candle (mark timeline)",   Graphics.blue_candle_bmp  , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle.Toggle())), 1, 1)
    gridAdd(owItemGrid, basicBoxImpl("Acquired blue ring (mark timeline)",     Graphics.blue_ring_bmp    , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueRing.Toggle())), 2, 0)
    gridAdd(owItemGrid, basicBoxImpl("Acquired magical sword (mark timeline)", Graphics.magical_sword_bmp, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Toggle())), 2, 1)
    canvasAdd(c, owItemGrid, OFFSET+60., 30.)
    // boomstick book, to mark when purchase in boomstick seed (normal book will become shield found in dungeon)
    canvasAdd(c, basicBoxImpl("Purchased boomstick book (mark timeline)", Graphics.boom_book_bmp, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Toggle())), OFFSET+120., 0.)
    // mark the dungeon wins on timeline via ganon/zelda boxes
    canvasAdd(c, basicBoxImpl("Killed Ganon (mark timeline)",  Graphics.ganon_bmp, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasDefeatedGanon.Toggle())), OFFSET+90., 90.)
    canvasAdd(c, basicBoxImpl("Rescued Zelda (mark timeline)", Graphics.zelda_bmp, (fun b -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Toggle(); if b then notesTextBox.Text <- notesTextBox.Text + "\n" + timeTextBox.Text)), OFFSET+120., 90.)
    // mark whether player currently has bombs, for overworld routing
    let bombIcon = veryBasicBoxImpl(Graphics.bomb_bmp, false, false, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBombs.Toggle()))
    ToolTip.SetTip(bombIcon, "Player currently has bombs")
    canvasAdd(c, bombIcon, OFFSET+160., 30.)

    // shield versus book icon (for boomstick flags/seeds)
    let toggleBookShieldCheckBox  = new CheckBox(Content=new TextBox(Text="S/B",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    ToolTip.SetTip(toggleBookShieldCheckBox, "Shield item icon instead of book item icon")
    toggleBookShieldCheckBox.IsChecked <- System.Nullable.op_Implicit false
    toggleBookShieldCheckBox.Checked.Add(fun _ -> toggleBookMagicalShield())
    toggleBookShieldCheckBox.Unchecked.Add(fun _ -> toggleBookMagicalShield())
    canvasAdd(c, toggleBookShieldCheckBox, OFFSET+150., 0.)

    // ow map animation layer
    // I can't figure out how to code Animations in Avalonia, so here's a simple kludge that uses DispatcherTime Interval Tick
    let canvasesToSlowBlink = ResizeArray()
    let canvasesToFastBlink = ResizeArray()
    let owRemainSpotHighlighters = Array2D.init 16 8 (fun i j ->
        let rect = new Canvas(Width=OMTW, Height=float(11*3), Background=Brushes.Lime)
        canvasesToSlowBlink.Add(rect)
        rect
        )

    // ow map opaque fixed bottom layer
    let X_OPACITY = 0.4
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
    canvasAdd(c, owOpaqueMapGrid, 0., 120.)

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
    canvasAdd(c, owDarkeningMapGrid, 0., 120.)

    // layer to place 'hiding' icons - dynamic darkening icons that are below route-drawing but above the previous layers
    let owHidingMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owHidingMapGridCanvases = Array2D.zeroCreate 16 8
    owHidingMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            gridAdd(owHidingMapGrid, c, i, j)
            owHidingMapGridCanvases.[i,j] <- c
    canvasAdd(c, owHidingMapGrid, 0., 120.)
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
    canvasAdd(c, routeDrawingCanvas, 0., 120.)

    // single ow tile magnified overlay
    let ENLARGE = 24. // make it this x bigger
    let dungeonTabsOverlay = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(8.), Background=Brushes.Black, IsVisible=false)
    let dungeonTabsOverlayContent = new StackPanel(Orientation=Layout.Orientation.Vertical)
    dungeonTabsOverlay.Child <- dungeonTabsOverlayContent
    let overlayTiles = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let bmp = new System.Drawing.Bitmap(16*int ENLARGE, 11*int ENLARGE)
            for x = 0 to bmp.Width-1 do
                for y = 0 to bmp.Height-1 do
                    let c = owMapBMPs.[i,j].GetPixel(int(float x*3./ENLARGE), int(float y*3./ENLARGE))
                    let c = 
                        if (x+1) % int ENLARGE = 0 || (y+1) % int ENLARGE = 0 then
                            System.Drawing.Color.FromArgb(int c.R / 2, int c.G / 2, int c.B / 2)
                        else
                            c
                    bmp.SetPixel(x,y,c)
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
    let drawCompletedDungeonHighlight(c,x,y) =
        // darkened rectangle corners
        let yellow = Brushes.Yellow.Color
        let darkYellow = Color.FromRgb(yellow.R/2uy, yellow.G/2uy, yellow.B/2uy)
        drawRectangleCornersHighlight(c,x,y,new SolidColorBrush(darkYellow))
        // darken the number
        let rect = new Shapes.Rectangle(Width=20.0*OMTW/48., Height=22.0, Stroke=Brushes.Black, StrokeThickness = 3.,
                                                        Fill=Brushes.Black, Opacity=0.4)
        canvasAdd(c, rect, x*OMTW+12.0*OMTW/48., float(y*11*3)+5.0)
    let drawWarpHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,Brushes.Aqua)
    let drawDarkening(c,x,y) =
        let rect = new Shapes.Rectangle(Width=OMTW-1.5, Height=float(11*3)-1.5, Stroke=Brushes.Black, StrokeThickness = 3.,
                                                        Fill=Brushes.Black, Opacity=0.4)
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
                                        // show enlarged version of current room
                                        dungeonTabsOverlayContent.Children.Add(overlayTiles.[i,j]) |> ignore
                                        dungeonTabsOverlay.IsVisible <- true
                                        // track current location for F5 & speech recognition purposes
                                        currentlyMousedOWX <- i
                                        currentlyMousedOWY <- j
                                        mostRecentMouseEnterTime <- DateTime.Now)
            c.PointerMoved.Add(fun ea ->
                if pointerEnteredButNotDrawnRoutingYet then
                    // draw routes
                    let pos = ea.GetPosition(c)
                    OverworldRouteDrawing.drawPaths(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, 
                                                    TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), pos, i, j)
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
                let updateGridSpot delta phrase =
                    // cant remove-by-identity because of non-uniques; remake whole canvas
                    owDarkeningMapGridCanvases.[i,j].Children.Clear()
                    c.Children.Clear()
                    // we need a dummy image to make the canvas absorb the mouse interactions, so just re-draw the map at 0 opacity
                    let image = resizeMapTileImage <| Graphics.BMPtoImage(owMapBMPs.[i,j])
                    image.Opacity <- 0.0
                    canvasAdd(c, image, 0., 0.)
                    if delta = 1 then
                        TrackerModel.overworldMapMarks.[i,j].Next()
                    elif delta = -1 then 
                        TrackerModel.overworldMapMarks.[i,j].Prev() 
                    elif delta = 0 then 
                        ()
                    else failwith "bad delta"
                    let ms = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                    let iconBMP = 
                        if ms.IsThreeItemShop && TrackerModel.overworldMapExtraData.[i,j] <> 0 then
                            let item1 = ms.State - 16  // 0-based
                            let item2 = TrackerModel.overworldMapExtraData.[i,j] - 1   // 0-based
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
                            ms.Current()
                    // be sure to draw in appropriate layer
                    let canvasToDrawOn =
                        if ms.HasTransparency && not ms.IsSword3 && not ms.IsSword2 then
                            if not ms.IsDungeon || (ms.State < 8 && TrackerModel.dungeons.[ms.State].IsComplete) then
                                drawDarkening(owDarkeningMapGridCanvases.[i,j], 0., 0)  // completed dungeons, warps, and shops get a darkened background in layer below routing
                            c
                        else
                            owDarkeningMapGridCanvases.[i,j]
                    if iconBMP <> null then 
                        let icon =
                            if ms.IsDungeon || ms.IsWarp then
                                trimNumeralBmpToImage(iconBMP)
                            else
                                resizeMapTileImage(Graphics.BMPtoImage iconBMP)
                        if ms.HasTransparency then
                            icon.Opacity <- 0.9
                        else
                            if ms.IsUnique then
                                icon.Opacity <- 0.6
                            elif ms.IsX then
                                icon.Opacity <- X_OPACITY
                            else
                                icon.Opacity <- 0.5
                        canvasAdd(canvasToDrawOn, icon, 0., 0.)
                    if ms.IsDungeon then
                        drawDungeonHighlight(canvasToDrawOn,0.,0)
                    if ms.IsWarp then
                        drawWarpHighlight(canvasToDrawOn,0.,0)
                    if OverworldData.owMapSquaresSecondQuestOnly.[j].Chars(i) = 'X' then
                        secondQuestOnlyInterestingMarks.[i,j] <- ms.IsInteresting 
                    if OverworldData.owMapSquaresFirstQuestOnly.[j].Chars(i) = 'X' then
                        firstQuestOnlyInterestingMarks.[i,j] <- ms.IsInteresting 
                owUpdateFunctions.[i,j] <- updateGridSpot 
                owCanvases.[i,j] <- c
                c.PointerPressed.Add(fun ea -> 
                    let msp = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                    if ea.GetCurrentPoint(c).Properties.IsLeftButtonPressed then 
                        if msp.State = -1 then
                            // left click empty tile changes to 'X'
                            updateGridSpot -1 ""
                        else
                            // left click a shop cycles up the second item
                            if msp.IsThreeItemShop then
                                // next item
                                let e = (TrackerModel.overworldMapExtraData.[i,j] + 1) % 8
                                // skip past duplicates
                                let item1 = msp.State - 15  // 1-based
                                let e = if e = item1 then (e + 1) % 8 else e
                                TrackerModel.overworldMapExtraData.[i,j] <- e
                                // redraw
                                updateGridSpot 0 ""
                    elif ea.GetCurrentPoint(c).Properties.IsRightButtonPressed then 
                        // right click a shop cycles down the second item
                        if msp.IsThreeItemShop then
                            // next item
                            let e = (TrackerModel.overworldMapExtraData.[i,j] - 1 + 8) % 8
                            // skip past duplicates
                            let item1 = msp.State - 15  // 1-based
                            let e = if e = item1 then (e - 1 + 8) % 8 else e
                            TrackerModel.overworldMapExtraData.[i,j] <- e
                            // redraw
                            updateGridSpot 0 ""
                    )
                c.PointerWheelChanged.Add(fun x -> updateGridSpot (if x.Delta.Y<0. then 1 else -1) "")
    canvasAdd(c, owMapGrid, 0., 120.)

    let mutable mapMostRecentMousePos = Point(-1., -1.)
    owMapGrid.PointerLeave.Add(fun _ -> mapMostRecentMousePos <- Point(-1., -1.))
    owMapGrid.PointerMoved.Add(fun ea -> mapMostRecentMousePos <- ea.GetPosition(owMapGrid))

    let recorderingCanvas = new Canvas(Width=16.*OMTW, Height=float(8*11*3))  // really the 'extra top layer' canvas for adding final marks to overworld map
    recorderingCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(c, recorderingCanvas, 0., 120.)
    let startIcon = new Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=Brushes.Lime, StrokeThickness=3.0)

    let THRU_MAIN_MAP_H = float(120 + 8*11*3)

    // map legend
    let LEFT_OFFSET = 78.0
    let legendCanvas = new Canvas()
    canvasAdd(c, legendCanvas, LEFT_OFFSET, THRU_MAIN_MAP_H)

    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="The LEGEND\nof Z-Tracker")
    canvasAdd(c, tb, 0., THRU_MAIN_MAP_H)

    canvasAdd(legendCanvas, trimNumeralBmpToImage Graphics.uniqueMapIconBMPs.[0], 0., 0.)
    drawDungeonHighlight(legendCanvas,0.,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Active\nDungeon")
    canvasAdd(legendCanvas, tb, OMTW, 0.)

    canvasAdd(legendCanvas, trimNumeralBmpToImage Graphics.uniqueMapIconBMPs.[0], 2.5*OMTW, 0.)
    drawDungeonHighlight(legendCanvas,2.5,0)
    drawCompletedDungeonHighlight(legendCanvas,2.5,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Completed\nDungeon")
    canvasAdd(legendCanvas, tb, 3.5*OMTW, 0.)

    canvasAdd(legendCanvas, trimNumeralBmpToImage Graphics.uniqueMapIconBMPs.[0], 5.*OMTW, 0.)
    drawDungeonHighlight(legendCanvas,5.,0)
    drawDungeonRecorderWarpHighlight(legendCanvas,5.,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Recorder\nDestination")
    canvasAdd(legendCanvas, tb, 6.*OMTW, 0.)

    canvasAdd(legendCanvas, trimNumeralBmpToImage Graphics.uniqueMapIconBMPs.[9], 7.5*OMTW, 0.)
    drawWarpHighlight(legendCanvas,7.5,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Any Road\n(Warp)")
    canvasAdd(legendCanvas, tb, 8.5*OMTW, 0.)

    let legendStartIcon = new Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=Brushes.Lime, StrokeThickness=3.0)
    canvasAdd(legendCanvas, legendStartIcon, 10.*OMTW+8.5*OMTW/48., 0.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Start\nSpot")
    canvasAdd(legendCanvas, tb, 11.*OMTW, 0.)

    let THRU_MAP_AND_LEGEND_H = THRU_MAIN_MAP_H + float(11*3)

    // item progress
    let itemProgressCanvas = new Canvas(Width=16.*OMTW, Height=30.)
    itemProgressCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(c, itemProgressCanvas, 0., THRU_MAP_AND_LEGEND_H)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Item Progress")
    canvasAdd(c, tb, 10., THRU_MAP_AND_LEGEND_H + 6.)
    
    let hintGrid = makeGrid(2,OverworldData.hintMeanings.Length,140,26)
    let mutable row=0 
    for a,b in OverworldData.hintMeanings do
        gridAdd(hintGrid, new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(1.), Text=a), 0, row)
        gridAdd(hintGrid, new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(1.), Text=b), 1, row)
        row <- row + 1
    let hintBorder = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(8.), Background=Brushes.Black)
    hintBorder.Child <- hintGrid
    hintBorder.Opacity <- 0.
    hintBorder.IsHitTestVisible <- false
    canvasAdd(c, hintBorder, 30., 120.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(1.), Text="Hint Decoder")
    canvasAdd(c, tb, 480., THRU_MAP_AND_LEGEND_H + 6.)
    tb.PointerEnter.Add(fun _ -> hintBorder.Opacity <- 1.0)
    tb.PointerLeave.Add(fun _ -> hintBorder.Opacity <- 0.0)

    let THRU_MAP_H = THRU_MAP_AND_LEGEND_H + 30.
    printfn "H thru item prog = %d" (int THRU_MAP_H)

    // WANT!
    let kitty = new Image()
    let imageStream = Graphics.GetResourceStream("CroppedBrianKitty.png")
    kitty.Source <- new Avalonia.Media.Imaging.Bitmap(imageStream)
    kitty.Width <- THRU_MAP_H - THRU_MAIN_MAP_H
    kitty.Height <- THRU_MAP_H - THRU_MAIN_MAP_H
    canvasAdd(c, kitty, 14.*OMTW, THRU_MAIN_MAP_H)

    let doUIUpdate() =
        // TODO found/not-found may need an update, only have event for found, hmm... for now just force redraw these on each update
        for i = 0 to 7 do
            if not(TrackerModel.dungeons.[i].PlayerHasTriforce()) then
                updateEmptyTriforceDisplay(i)
        recorderingCanvas.Children.Clear()
        // TODO event for redraw item progress? does any of this event interface make sense? hmmm
        itemProgressCanvas.Children.Clear()
        let mutable x, y = 90., 3.
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
            member _this.OverworldSpotsRemaining(n) = (owRemainingScreensCheckBox.Content :?> TextBox).Text <- sprintf "OW spots left: %d" n 
            member _this.DungeonLocation(i,x,y,hasTri,isCompleted) =
                if isCompleted then
                    drawCompletedDungeonHighlight(recorderingCanvas,float x,y)
                // highlight any triforce dungeons as recorder warp destinations
                if TrackerModel.playerComputedStateSummary.HaveRecorder && hasTri then
                    drawDungeonRecorderWarpHighlight(recorderingCanvas,float x,y)
                // highlight 9 after get all triforce
                if i = 8 && TrackerModel.dungeons.[0..7] |> Array.forall (fun d -> d.PlayerHasTriforce()) then
                    let rect = new Canvas(Width=OMTW, Height=float(11*3), Background=Brushes.Pink)
                    canvasesToFastBlink.Add(rect)
                    canvasAdd(recorderingCanvas, rect, OMTW*float(x), float(y*11*3))
            member _this.AnyRoadLocation(i,x,y) = ()
            member _this.WhistleableLocation(x,y) = ()
            member _this.Sword3(x,y) = 
                if not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) && TrackerModel.playerComputedStateSummary.PlayerHearts>=10 then
                    let rect = new Canvas(Width=OMTW, Height=float(11*3), Background=Brushes.Pink)
                    canvasesToFastBlink.Add(rect)
                    canvasAdd(recorderingCanvas, rect, OMTW*float(x), float(y*11*3))
            member _this.Sword2(x,y) = ()
            member _this.RoutingInfo(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations,owRouteworthySpots) = 
                // clear and redraw routing
                routeDrawingCanvas.Children.Clear()
                OverworldRouting.repopulate(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations)
                let pos = mapMostRecentMousePos
                let i,j = int(Math.Floor(pos.X / OMTW)), int(Math.Floor(pos.Y / (11.*3.)))
                if i>=0 && i<16 && j>=0 && j<8 then
                    OverworldRouteDrawing.drawPaths(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, 
                                                    TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), Point(0.,0.), i, j)
                // unexplored but gettable spots highlight
                if owRemainingScreensCheckBox.IsChecked.HasValue && owRemainingScreensCheckBox.IsChecked.Value then
                    for x = 0 to 15 do
                        for y = 0 to 7 do
                            if owRouteworthySpots.[x,y] && TrackerModel.overworldMapMarks.[x,y].Current() = -1 then
                                canvasAdd(recorderingCanvas, owRemainSpotHighlighters.[x,y], OMTW*float(x), float(y*11*3))
            member _this.AnnounceCompletedDungeon(i) = ()
            member _this.CompletedDungeons(a) =
                for i = 0 to 7 do
                    for j = 0 to 3 do
                        mainTrackerCanvases.[i,j].Children.Remove(mainTrackerCanvasShaders.[i,j]) |> ignore
                    if a.[i] then
                        for j = 0 to 3 do
                            mainTrackerCanvases.[i,j].Children.Add(mainTrackerCanvasShaders.[i,j]) |> ignore
            member _this.AnnounceFoundDungeonCount(n) = ()
            member _this.AnnounceTriforceCount(n) = ()
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
        for c in canvasesToSlowBlink do
            c.Opacity <- if c.Opacity = 0.2 then 0.5 else 0.2
        for c in canvasesToFastBlink do
            c.Opacity <- if c.Opacity = 0.0 then 0.6 else 0.0
        )
    timer.Start()

    // timeline
    let TLC = Brushes.SandyBrown   // timeline color
    let MW = (c.Width-48.)/60. // minute width
    let makeTimeline(leftText, rightText) = 
        let timelineCanvas = new Canvas(Height=float TLH, Width=c.Width)
        let tb1 = new TextBox(Text=leftText,FontSize=14.0,Background=Brushes.Black,Foreground=TLC,BorderThickness=Thickness(0.0),IsReadOnly=true)
        canvasAdd(timelineCanvas, tb1, 0., 30.)
        let tb2 = new TextBox(Text=rightText,FontSize=14.0,Background=Brushes.Black,Foreground=TLC,BorderThickness=Thickness(0.0),IsReadOnly=true)
        canvasAdd(timelineCanvas, tb2, c.Width-20., 30.)
        let line1 = new Shapes.Line(StartPoint=Point(24.,float(13*3)), EndPoint=Point(c.Width-24.,float(13*3)), Stroke=TLC, StrokeThickness=3.)
        canvasAdd(timelineCanvas, line1, 0., 0.)
        for i = 0 to 12 do
            let d = if i%2=1 then 3 else 0
            let line = new Shapes.Line(StartPoint=Point(24.+float(i)*MW*5.,float(11*3+d)), EndPoint=Point(24.+float(i)*MW*5.,float(15*3-d)), Stroke=TLC, StrokeThickness=3.)
            canvasAdd(timelineCanvas, line, 0., 0.)
        timelineCanvas 
    let timeline1Canvas = makeTimeline("0h","1h")
    let curTime = new Shapes.Line(StartPoint=Point(float(24),float(11*3)), EndPoint=Point(float(24),float(15*3)), Stroke=Brushes.White, StrokeThickness=3.)
    canvasAdd(timeline1Canvas, curTime, 0., 0.)

    let timeline2Canvas = makeTimeline("1h","2h")

    let top = ref true
    let updateTimeline(minute) =
        if minute < 0 || minute > 180 then
            ()
        else
            let tlc,minute = 
                if minute <= 60 then 
                    timeline1Canvas, minute 
                else
                    timeline2Canvas, minute-60
            let items = ResizeArray()
            let hearts = ResizeArray()
            for x in timelineItems do
                if x.IsDone() then
                    if x.IsHeart() then
                        hearts.Add(x)
                    else
                        items.Add(x)
            for x in items do
                timelineItems.Remove(x) |> ignore
            for x in hearts do
                timelineItems.Remove(x) |> ignore
            // post items
            for x in items do
                let bmp = x.DisplayBMP()
                let bmp =
                    if bmp.Width = 21 then  // item bmps are 21x21, make a 30x30 with it in middle
                        let newBMP = new System.Drawing.Bitmap(30,30)
                        for i = 0 to 20 do
                            for j = 0 to 20 do
                                newBMP.SetPixel(i+4, j+4, bmp.GetPixel(i,j))
                        newBMP
                    else
                        bmp
                let img = Graphics.BMPtoImage(bmp)
                img.Width <- 30.
                img.Height <- 30.
                canvasAdd(tlc, img, 24.+float(minute)*MW-15.-1., 3.+(if !top then 0. else 42.))
                let line = new Shapes.Line(StartPoint=Point(0.,float(12*3)), EndPoint=Point(0.,float(13*3)), Stroke=Brushes.LightBlue, StrokeThickness=2.)
                canvasAdd(tlc, line, 24.+float(minute)*MW-1., (if !top then 0. else 3.))
                top := not !top
            // post hearts
            if hearts.Count > 0 then
                let img = Graphics.BMPtoImage(Graphics.timelineHeart_bmp)
                img.Width <- 13.
                img.Height <- 13.
                canvasAdd(tlc, img, 24.+float(minute)*MW-3.-1.-2., 36. - 2.)
            // post current time
            curTime.StartPoint <- Point(24.+float(minute)*MW, curTime.StartPoint.Y)
            curTime.EndPoint <- Point(24.+float(minute)*MW, curTime.EndPoint.Y)
            timeline1Canvas.Children.Remove(curTime) |> ignore // have it be last
            timeline2Canvas.Children.Remove(curTime) |> ignore // have it be last
            canvasAdd(tlc, curTime, 0., 0.)
    canvasAdd(c, timeline1Canvas, 0., THRU_MAP_H)
    canvasAdd(c, timeline2Canvas, 0., THRU_MAP_H + timeline1Canvas.Height)

    let THRU_TIMELINE_H = THRU_MAP_H + timeline1Canvas.Height + timeline2Canvas.Height + 3.

    // Level trackers
    let fixedDungeon1Outlines = ResizeArray()
    let fixedDungeon2Outlines = ResizeArray()

    let dungeonTabs = new TabControl()
    dungeonTabs.Background <- Brushes.Black 
    canvasAdd(c, dungeonTabs , 0., THRU_TIMELINE_H)
    let tabItems = ResizeArray()
    for level = 1 to 9 do
        let levelTab = new TabItem(Background=Brushes.SlateGray, Foreground=Brushes.Black, Height=float(TH))
        levelTab.FontSize <- 16.
        levelTab.FontWeight <- FontWeight.Bold
        levelTab.VerticalContentAlignment <- Layout.VerticalAlignment.Center
        levelTab.Margin <- Thickness(1., 0.)
        levelTab.Padding <- Thickness(0.)
        levelTab.Header <- sprintf "  %d  " level
        let dungeonCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7))

        levelTab.Content <- dungeonCanvas 
        dungeonTabs.Height <- dungeonCanvas.Height + 30.   // ok to set this 9 times
        tabItems.Add(levelTab)

        let TEXT = sprintf "LEVEL-%d " level
        // horizontal doors
        let unknown = new SolidColorBrush(Color.FromRgb(55uy, 55uy, 55uy)) 
        let no = Brushes.DarkRed
        let yes = Brushes.Green
        let empty = Brushes.Black
        let horizontalDoorCanvases = Array2D.zeroCreate 7 8
        for i = 0 to 6 do
            for j = 0 to 7 do
                let d = new Canvas(Height=12., Width=12., Background=unknown)
                horizontalDoorCanvases.[i,j] <- d
                canvasAdd(dungeonCanvas, d, float(i*(39+12)+39), float(TH+j*(27+12)+8))
                let left _ =        
                    if not(obj.Equals(d.Background, yes)) then
                        d.Background <- yes
                    else
                        d.Background <- unknown
                let right _ = 
                    if not(obj.Equals(d.Background, no)) then
                        d.Background <- no
                    else
                        d.Background <- unknown
                d.PointerPressed.Add(fun ea -> 
                    if ea.GetCurrentPoint(c).Properties.IsLeftButtonPressed then (left())
                    elif ea.GetCurrentPoint(c).Properties.IsRightButtonPressed then (right()))
        // vertical doors
        let verticalDoorCanvases = Array2D.zeroCreate 8 7
        for i = 0 to 7 do
            for j = 0 to 6 do
                let d = new Canvas(Height=12., Width=12., Background=unknown)
                verticalDoorCanvases.[i,j] <- d
                canvasAdd(dungeonCanvas, d, float(i*(39+12)+14), float(TH+j*(27+12)+27))
                let left _ =
                    if not(obj.Equals(d.Background, yes)) then
                        d.Background <- yes
                    else
                        d.Background <- unknown
                let right _ = 
                    if not(obj.Equals(d.Background, no)) then
                        d.Background <- no
                    else
                        d.Background <- unknown
                d.PointerPressed.Add(fun ea -> 
                    if ea.GetCurrentPoint(c).Properties.IsLeftButtonPressed then (left())
                    elif ea.GetCurrentPoint(c).Properties.IsRightButtonPressed then (right()))
        // rooms
        let roomCanvases = Array2D.zeroCreate 8 8 
        let roomStates = Array2D.zeroCreate 8 8 // 0 = unexplored, 1-9 = transports, 10=vchute, 11=hchute, 12=tee, 13=tri, 14=heart, 15=start, 16=explored empty
        let roomCleared = Array2D.zeroCreate 8 8 // boolean
        let ROOMS = 17 // how many types
        let usedTransports = Array.zeroCreate 10 // slot 0 unused
        for i = 0 to 7 do
            // LEVEL-9        
            let tb = new TextBox(Width=float(13*3), Height=float(TH), FontSize=float(TH-12), Foreground=Brushes.White, Background=Brushes.Black, IsReadOnly=true,
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
                roomCleared.[i,j] <- false
                let updateUI () =
                    // update UI
                    c.Children.Clear()
                    let image =
                        match roomStates.[i,j] with
                        | 0  -> Graphics.cdungeonUnexploredRoomBMP
                        | 10 -> Graphics.cdungeonVChuteBMP
                        | 11 -> Graphics.cdungeonHChuteBMP
                        | 12 -> Graphics.cdungeonTeeBMP
                        | 13 -> Graphics.cdungeonTriforceBMP 
                        | 14 -> Graphics.cdungeonPrincessBMP 
                        | 15 -> Graphics.cdungeonStartBMP 
                        | 16 -> Graphics.cdungeonExploredRoomBMP 
                        | n  -> Graphics.cdungeonNumberBMPs.[n-1]
                        |> (fun (u,c) -> if roomCleared.[i,j] then c else u)
                        |> Graphics.BMPtoImage
                    canvasAdd(c, image, 0., 0.)

                let f b =
                    // track transport being changed away from
                    if [1..9] |> List.contains roomStates.[i,j] then
                        usedTransports.[roomStates.[i,j]] <- usedTransports.[roomStates.[i,j]] - 1
                    // go to next state
                    roomStates.[i,j] <- ((roomStates.[i,j] + (if b then 1 else -1)) + ROOMS) % ROOMS
                    // skip transport if already used both
                    while [1..9] |> List.contains roomStates.[i,j] && usedTransports.[roomStates.[i,j]] = 2 do
                        roomStates.[i,j] <- ((roomStates.[i,j] + (if b then 1 else -1)) + ROOMS) % ROOMS
                    // note any new transports
                    if [1..9] |> List.contains roomStates.[i,j] then
                        usedTransports.[roomStates.[i,j]] <- usedTransports.[roomStates.[i,j]] + 1
                    updateUI ()
                c.PointerPressed.Add(fun ea -> if ea.GetCurrentPoint(c).Properties.IsLeftButtonPressed then (
                    if ea.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift) then
                        // shift click to mark not-on-map rooms
                        // don't break transport count
                        if [1..9] |> List.contains roomStates.[i,j] then
                            usedTransports.[roomStates.[i,j]] <- usedTransports.[roomStates.[i,j]] - 1

                        // (unexplored,cleared) means "empty"
                        let makeEmpty = not (roomStates.[i,j] = 0 && roomCleared.[i,j])
                        roomStates.[i,j] <- 0
                        roomCleared.[i,j] <- makeEmpty
                        let door = if makeEmpty then empty else unknown :> ISolidColorBrush
                        if i > 0 then
                            horizontalDoorCanvases.[i-1,j].Background <- door
                        if i < 7 then
                            horizontalDoorCanvases.[i,j].Background <- door
                        if j > 0 then
                            verticalDoorCanvases.[i,j-1].Background <- door
                        if j < 7 then
                            verticalDoorCanvases.[i,j].Background <- door
                    else
                        // click to mark cleared room
                        roomCleared.[i,j] <- not roomCleared.[i,j]
                        if roomStates.[i,j] = 0 then
                            roomStates.[i,j] <- 16
                    updateUI ()
                    ))
                c.PointerWheelChanged.Add(fun x -> f (x.Delta.Y<0.))
                // drag and drop to quickly 'paint' rooms
                //let startAsPlainTask (work : Async<unit>) = Threading.Tasks.Task.Factory.StartNew(fun () -> work |> Async.RunSynchronously)
                c.PointerPressed.Add(fun ea ->
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
                            roomStates.[i,j] <- 16
                            roomCleared.[i,j] <- true
                        else
                            roomStates.[i,j] <- 16
                            roomCleared.[i,j] <- false
                        updateUI()
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
    canvasAdd(c, fqcb, 310., THRU_TIMELINE_H) 

    sqcb.IsChecked <- System.Nullable.op_Implicit false
    sqcb.Checked.Add(fun _ -> fixedDungeon2Outlines |> Seq.iter (fun s -> s.Opacity <- 1.0); fqcb.IsChecked <- System.Nullable.op_Implicit false)
    sqcb.Unchecked.Add(fun _ -> fixedDungeon2Outlines |> Seq.iter (fun s -> s.Opacity <- 0.0))
    canvasAdd(c, sqcb, 360., THRU_TIMELINE_H) 

    canvasAdd(c, dungeonTabsOverlay, 0., THRU_TIMELINE_H)

    // notes    
    let tb = new TextBox(Width=c.Width-402., Height=dungeonTabs.Height)
    notesTextBox <- tb
    tb.FontSize <- 24.
    tb.Foreground <- Brushes.LimeGreen 
    tb.Background <- Brushes.Black 
    tb.CaretBrush <- Brushes.LimeGreen 
    tb.Text <- "Notes\n"
    tb.AcceptsReturn <- true
    canvasAdd(c, tb, 402., THRU_TIMELINE_H) 

    // remaining OW spots
    canvasAdd(c, owRemainingScreensCheckBox, RIGHT_COL, 60.)
    owRemainingScreensCheckBox.Checked.Add(fun _ -> TrackerModel.forceUpdate()) 
    owRemainingScreensCheckBox.Unchecked.Add(fun _ -> TrackerModel.forceUpdate())
    // current hearts
    canvasAdd(c, currentHeartsTextBox, RIGHT_COL, 90.)
    // coordinate grid
    let owCoordsGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owCoordsTBs = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let tb = new TextBox(Text=sprintf "%c%d" (char (int 'A' + j)) (i+1), Foreground=Brushes.White, Background=Brushes.Transparent, BorderThickness=Thickness(0.0), 
                                    FontFamily=FontFamily("Consolas"), FontSize=16.0, FontWeight=FontWeight.Bold)
            tb.Opacity <- 0.0
            tb.IsHitTestVisible <- false // transparent to mouse
            owCoordsTBs.[i,j] <- tb
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            canvasAdd(c, tb, 2., 6.)
            gridAdd(owCoordsGrid, c, i, j) 
    canvasAdd(c, owCoordsGrid, 0., 120.)
    let showCoords = new TextBox(Text="Coords",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true)
    let cb = new CheckBox(Content=showCoords)
    cb.IsChecked <- System.Nullable.op_Implicit false
    cb.Checked.Add(fun _ -> owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.85))
    cb.Unchecked.Add(fun _ -> owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.0))
    showCoords.PointerEnter.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.85))
    showCoords.PointerLeave.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.0))
    canvasAdd(c, cb, RIGHT_COL + 140., 90.)

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
    let allOwMapZoneImages = ResizeArray()
    for i = 0 to 15 do
        for j = 0 to 7 do
            let image = Graphics.BMPtoImage owMapZoneBmps.[i,j]
            image.Opacity <- 0.0
            image.IsHitTestVisible <- false // transparent to mouse
            allOwMapZoneImages.Add(image)
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            canvasAdd(c, image, 0., 0.)
            gridAdd(owMapZoneGrid, c, i, j)
    canvasAdd(c, owMapZoneGrid, 0., 120.)

    let owMapZoneBoundaries = ResizeArray()
    let makeLine(x1, x2, y1, y2) = 
        let line = new Shapes.Line(StartPoint=Point(OMTW*float(x1),float(y1*11*3)), EndPoint=Point(OMTW*float(x2),float(y2*11*3)), Stroke=Brushes.White, StrokeThickness=3.)
        line.IsHitTestVisible <- false // transparent to mouse
        line
    let addLine(x1,x2,y1,y2) = 
        let line = makeLine(x1,x2,y1,y2)
        line.Opacity <- 0.0
        owMapZoneBoundaries.Add(line)
        canvasAdd(c, line, 0., 120.)
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

    let zoneNames = ResizeArray()
    let addZoneName(name, x, y) =
        let tb = new TextBox(Text=name,FontSize=12.,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(2.),IsReadOnly=true)
        canvasAdd(c, tb, 0. + x*OMTW, 120.+y*11.*3.)
        tb.Opacity <- 0.
        tb.TextAlignment <- TextAlignment.Center
        tb.FontWeight <- FontWeight.Bold
        tb.IsHitTestVisible <- false
        zoneNames.Add(tb)
    addZoneName("DEATH\nMOUNTAIN", 3.0, 0.3)
    addZoneName("GRAVE", 1.8, 2.5)
    addZoneName("DEAD\nWOODS", 1.8, 6.0)
    addZoneName("LAKE 1", 10.0, 0.1)
    addZoneName("LAKE 2", 5.0, 4.0)
    addZoneName("LAKE 3", 9.5, 5.5)
    addZoneName("RIVER 1", 8.3, 1.1)
    addZoneName("RIV\nER2", 5.1, 6.2)
    addZoneName("START", 7.3, 6.2)
    addZoneName("DESERT", 10.3, 3.1)
    addZoneName("FOREST", 12.3, 5.1)
    addZoneName("LOST\nHILLS", 12.3, 0.3)
    addZoneName("COAST", 14.3, 2.6)

    let changeZoneOpacity(show) =
        if show then
            allOwMapZoneImages |> Seq.iter (fun i -> i.Opacity <- 0.3)
            owMapZoneBoundaries |> Seq.iter (fun x -> x.Opacity <- 0.9)
            zoneNames |> Seq.iter (fun x -> x.Opacity <- 0.6)
        else
            allOwMapZoneImages |> Seq.iter (fun i -> i.Opacity <- 0.0)
            owMapZoneBoundaries |> Seq.iter (fun x -> x.Opacity <- 0.0)
            zoneNames |> Seq.iter (fun x -> x.Opacity <- 0.0)
    let showZones = new TextBox(Text="Zones",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true)
    let cb = new CheckBox(Content=showZones)
    cb.IsChecked <- System.Nullable.op_Implicit false
    cb.Checked.Add(fun _ -> changeZoneOpacity true)
    cb.Unchecked.Add(fun _ -> changeZoneOpacity false)
    showZones.PointerEnter.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then changeZoneOpacity true)
    showZones.PointerLeave.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then changeZoneOpacity false)
    canvasAdd(c, cb, RIGHT_COL + 140., 66.)


    //                items  ow map  prog  timeline    dungeon tabs                
    c.Height <- float(30*4 + 11*3*9 + 30 + 2*TLH + 3 + TH + TH + 27*8 + 12*7 + 30)
    TrackerModel.forceUpdate()
    c, updateTimeline


