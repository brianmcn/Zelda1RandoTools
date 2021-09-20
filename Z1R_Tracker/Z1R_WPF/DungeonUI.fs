module DungeonUI

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let canvasAdd = Graphics.canvasAdd

////////////////////////

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
    let d = new DockPanel(Height=h, LastChildFill=true, Background=Brushes.Black, Opacity=0.6)
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
    let b:FrameworkElement = upcast new Border(Background=Brushes.Black, BorderThickness=Thickness(ST), BorderBrush=Brushes.DimGray, Child=d)
    b

////////////////////////

let makeDungeonTabs(appMainCanvas, TH, contentCanvasMouseEnterFunc, contentCanvasMouseLeaveFunc, fixedDungeon1Outlines:ResizeArray<Shapes.Line>, fixedDungeon2Outlines:ResizeArray<Shapes.Line>) =
    let grabHelper = new Dungeon.GrabHelper()
    let grabModeTextBlock = 
        new Border(BorderThickness=Thickness(2.), BorderBrush=Brushes.LightGray, 
                    Child=new TextBlock(TextWrapping=TextWrapping.Wrap, FontSize=16., Foreground=Brushes.Black, Background=Brushes.Gray, IsHitTestVisible=false,
                                        Text="You are now in 'grab mode', which can be used to move an entire segment of dungeon rooms and doors at once.\n\nTo abort grab mode, click again on 'GRAB' in the upper right of the dungeon tracker.\n\nTo move a segment, first click any marked room, to pick up that room and all contiguous rooms.  Then click again on a new location to 'drop' the segment you grabbed.  After grabbing, hovering the mouse shows a preview of where you would drop.  This behaves like 'cut and paste', and adjacent doors will come along for the ride.\n\nUpon completion, you will be prompted to keep changes or undo them, so you can experiment.")
        )
    let mutable popupState = Dungeon.DelayedPopupState.NONE  // key to an interlock that enables a fast double-click to bypass the popup
    let dungeonTabs = new TabControl(FontSize=12., Background=Brushes.Black)
    for level = 1 to 9 do
        let levelTab = new TabItem(Background=Brushes.Black, Foreground=Brushes.Black)
        let labelChar = if level = 9 then '9' else if TrackerModel.IsHiddenDungeonNumbers() then (char(int 'A' - 1 + level)) else (char(int '0' + level))
        let header = new TextBox(Width=22., Background=Brushes.Black, Foreground=Brushes.White, Text=sprintf "%c" labelChar, IsHitTestVisible=false, 
                                    HorizontalContentAlignment=HorizontalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), Padding=Thickness(0.))
        levelTab.Header <- header
        TrackerModel.GetDungeon(level-1).HiddenDungeonColorOrLabelChanged.Add(fun (color,_) -> 
            header.Background <- new SolidColorBrush(Graphics.makeColor(color))
            header.Foreground <- if Graphics.isBlackGoodContrast(color) then Brushes.Black else Brushes.White
            )
        let contentCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7), Background=Brushes.Black)
        contentCanvas.MouseEnter.Add(fun _ -> 
                contentCanvasMouseEnterFunc(level)
//              let i,j = TrackerModel.mapStateSummary.DungeonLocations.[level-1]
//              if (i,j) <> TrackerModel.NOTFOUND then
//                // when mouse in a dungeon map, show its location...
//                showLocatorExactLocation(TrackerModel.mapStateSummary.DungeonLocations.[level-1])
//                // ...and behave like we are moused there
//                drawRoutesTo(None, routeDrawingCanvas, Point(), i, j, TrackerModel.Options.Overworld.DrawRoutes.Value, if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxYGH else 0)
            )
        contentCanvas.MouseLeave.Add(fun ea -> 
            // only hide the locator if we actually left
            // MouseLeave fires (erroneously?) when we e.g. click a room, and redraw() causes the element we are Entered to be removed from the tree and replaced by another,
            // even though we never left the bounds of the enclosing canvas
            let pos = ea.GetPosition(contentCanvas)
            if (pos.X < 0. || pos.X > contentCanvas.ActualWidth) || (pos.Y < 0. || pos.Y > contentCanvas.ActualHeight) then
                contentCanvasMouseLeaveFunc(level)
//                hideLocator()
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
        let backgroundColorCanvas = new Canvas(Width=float(51*6+12), Height=float(TH))
        canvasAdd(dungeonCanvas, backgroundColorCanvas, 0., 0.)
        TrackerModel.GetDungeon(level-1).HiddenDungeonColorOrLabelChanged.Add(fun (color,_) ->
            backgroundColorCanvas.Background <- new SolidColorBrush(Graphics.makeColor(color))
            )
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
                if TrackerModel.IsHiddenDungeonNumbers() then
                    let TEXT = "LEVEL-9"
                    if i <> 6 || labelChar = '9' then
                        let fg = if Graphics.isBlackGoodContrast(TrackerModel.GetDungeon(level-1).Color) then Brushes.Black else Brushes.White
                        let tb = new TextBox(Width=float(13*3), Height=float(TH), FontSize=float(TH-4), Foreground=fg, Background=Brushes.Transparent, IsReadOnly=true, IsHitTestVisible=false,
                                                Text=TEXT.Substring(i,1), BorderThickness=Thickness(0.), FontFamily=new FontFamily("Courier New"), FontWeight=FontWeights.Bold)
                        canvasAdd(dungeonCanvas, tb, float(i*51)+12., 0.)
                        TrackerModel.GetDungeon(level-1).HiddenDungeonColorOrLabelChanged.Add(fun (color,_) ->
                            tb.Foreground <- if Graphics.isBlackGoodContrast(color) then Brushes.Black else Brushes.White
                            )
                    else
                        let gsc = new GradientStopCollection()
                        gsc.Add(new GradientStop(Colors.Red, 0.))
                        gsc.Add(new GradientStop(Colors.Orange, 0.2))
                        gsc.Add(new GradientStop(Colors.Yellow, 0.4))
                        gsc.Add(new GradientStop(Colors.LightGreen, 0.6))
                        gsc.Add(new GradientStop(Colors.LightBlue, 0.8))
                        gsc.Add(new GradientStop(Colors.MediumPurple, 1.))
                        let rainbowBrush = new LinearGradientBrush(gsc, 90.)
                        let tb = new TextBox(Width=float(13*3-16), Height=float(TH-4), FontSize=float(TH-4), Foreground=Brushes.Black, Background=rainbowBrush, IsReadOnly=true, IsHitTestVisible=false,
                                                Text="?", BorderThickness=Thickness(0.), FontFamily=new FontFamily("Courier New"), FontWeight=FontWeights.Bold,
                                                HorizontalContentAlignment=HorizontalAlignment.Center)
                        let button = new Button(Height=float(TH), Content=tb, BorderThickness=Thickness(2.), Margin=Thickness(0.), Padding=Thickness(0.), BorderBrush=Brushes.White)
                        canvasAdd(dungeonCanvas, button, float(i*51)+6., 0.)
                        let mutable popupIsActive = false
                        button.Click.Add(fun _ ->
                            if not popupIsActive then
                                let pos = tb.TranslatePoint(Point(tb.Width/2., tb.Height/2.), appMainCanvas)
                                Dungeon.HiddenDungeonColorChooserPopup(appMainCanvas, 75., 310., 110., 110., TrackerModel.GetDungeon(level-1).Color, level-1, 
                                    (fun () -> 
                                        Graphics.WarpMouseCursorTo(pos)
                                        popupIsActive <- false)) |> ignore
                            )
                else
                    let TEXT = sprintf "LEVEL-%d " level
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
                        let tileCanvas = new Canvas(Width=13.*3., Height=9.*3., Background=Brushes.Black)
                        let originalStateIndex = if roomStates.[i,j] < 10 then roomStates.[i,j] else roomStates.[i,j] - 1
                        let gridElementsSelectablesAndIDs : (FrameworkElement*bool*int)[] = Array.init (ROOMS-1) (fun n ->
                            let tweak(im:Image) = im.Opacity <- 0.5; im
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
                                let tile = roomBMPpairs(currentState) |> (fun (u,c) -> u) |> Graphics.BMPtoImage
                                tile.Opacity <- 0.6
                                canvasAdd(tileCanvas, tile, 0., 0.)),
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
                            (new CustomComboBoxes.ModalGridSelectBrushes(Brushes.Lime, Brushes.Lime, Brushes.Red, Brushes.Gray)).Dim(0.6), true)
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
                                        // plain left click
                                        if roomStates.[i,j] = 0 then
                                            // ad hoc useful gesture for clicking unknown room - it moves it to explored & completed state
                                            roomStates.[i,j] <- ROOMS-1
                                            roomCompleted.[i,j] <- true
                                        if popupState=Dungeon.DelayedPopupState.SOON then
                                            //printfn "click canceling"
                                            popupState <- Dungeon.DelayedPopupState.NONE // we clicked again before it activated, cancel it
                                            roomCompleted.[i,j] <- true  // interpret the double-left-click as completion
                                            redraw()
                                        else
                                            redraw()
                                            delayedActivatePopup(0)
                        elif ea.ChangedButton = Input.MouseButton.Right then
                            if not grabHelper.IsGrabMode then  // cannot right click rooms in grab mode
                                if isInterior then
                                    if roomStates.[i,j] = 0 then
                                        // ad hoc useful gesture for right-clicking unknown room - it moves it to explored & uncompleted state
                                        roomStates.[i,j] <- ROOMS-1
                                        roomCompleted.[i,j] <- false
                                    if popupState=Dungeon.DelayedPopupState.SOON then
                                        popupState <- Dungeon.DelayedPopupState.NONE // we clicked again before it activated, cancel it
                                        roomCompleted.[i,j] <- false  // interpret the double-right-click as uncompletion
                                        redraw()
                                    else
                                        redraw()
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
    dungeonTabs, grabModeTextBlock
