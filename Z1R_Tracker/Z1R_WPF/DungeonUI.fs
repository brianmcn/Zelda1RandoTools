module DungeonUI

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let canvasAdd = Graphics.canvasAdd

////////////////////////

let TH = 24 // text height

open HotKeys.MyKey

let MakeLocalTrackerPanel(cm:CustomComboBoxes.CanvasManager, pos:Point, sunglasses, level, ghostBuster) =
    let dungeonIndex = level-1
    let linkCanvas = new Canvas(Width=30., Height=30.)
    let link1 = Graphics.BMPtoImage Graphics.linkFaceForward_bmp
    link1.Width <- 30.
    link1.Height <- 30.
    link1.Stretch <- Stretch.UniformToFill
    let link2 = Graphics.BMPtoImage Graphics.linkGotTheThing_bmp
    link2.Width <- 30.
    link2.Height <- 30.
    link2.Stretch <- Stretch.UniformToFill
    link2.Opacity <- 0.
    linkCanvas.Children.Add(link1) |> ignore
    linkCanvas.Children.Add(link2) |> ignore
    let yellow = new SolidColorBrush(Color.FromArgb(byte(sunglasses*255.), Colors.Yellow.R, Colors.Yellow.G, Colors.Yellow.B))
    // draw triforce (or label if 9) and N boxes, populated as now
    let sp = new StackPanel(Orientation=Orientation.Vertical, Opacity=sunglasses)
    if TrackerModel.IsHiddenDungeonNumbers() then
        let colorCanvas = new Canvas(Width=28., Height=28., Background=Brushes.Black)
        let d = TrackerModel.GetDungeon(dungeonIndex)
        let redraw(color,labelChar) =
            colorCanvas.Background <- new SolidColorBrush(Graphics.makeColor(color))
            colorCanvas.Children.Clear()
            let color = if Graphics.isBlackGoodContrast(color) then System.Drawing.Color.Black else System.Drawing.Color.White
            if d.LabelChar <> '?' then
                colorCanvas.Children.Add(Graphics.BMPtoImage(Graphics.alphaNumOnTransparentBmp(labelChar, color, 28, 28, 3, 2))) |> ignore
        redraw(d.Color, d.LabelChar)
        d.HiddenDungeonColorOrLabelChanged.Add(redraw)
        sp.Children.Add(colorCanvas) |> ignore
    let dungeonView = if dungeonIndex < 8 then Views.MakeTriforceDisplayView(cm, dungeonIndex, None, true) else Views.MakeLevel9View(None)
    sp.Children.Add(dungeonView) |> ignore
    let d = TrackerModel.GetDungeon(dungeonIndex)
    for box in d.Boxes do
        let c = new Canvas(Width=30., Height=30.)
        let view = Views.MakeBoxItem(cm, box)
        canvasAdd(c, view, 0., 0.)
        sp.Children.Add(c) |> ignore
        if level <> 9 && TrackerModel.IsHiddenDungeonNumbers() && sp.Children.Count = 5 then
            let r = new Shapes.Rectangle(Width=30., Height=30., Fill=new VisualBrush(ghostBuster), IsHitTestVisible=false)
            canvasAdd(c, r, 0., 0.)
    sp.Children.Add(linkCanvas) |> ignore
    sp.Margin <- Thickness(3.)
    let border = new Border(Child=sp, BorderThickness=Thickness(3.), BorderBrush=Brushes.DimGray, Background=Brushes.Black)
    // dynamic highlight
    let line,triangle = Graphics.makeArrow(30.*float dungeonIndex+15., 36.+float(d.Boxes.Length+1)*30., pos.X+21., pos.Y-3., yellow)
    let rect = new Shapes.Rectangle(Width=36., Height=6.+float(d.Boxes.Length+1+if TrackerModel.IsHiddenDungeonNumbers()then 1 else 0)*30., Stroke=yellow, StrokeThickness=3.)
    line.Opacity <- 0.
    triangle.Opacity <- 0.
    rect.Opacity <- 0.
    let c = new Canvas()
    canvasAdd(c, line, 0., 0.)
    canvasAdd(c, triangle, 0., 0.)
    canvasAdd(c, rect, 30.*float dungeonIndex-3., 27. - if TrackerModel.IsHiddenDungeonNumbers() then 30. else 0.)
    let highlight() =
        if c.Parent = null then
            canvasAdd(cm.AppMainCanvas, c, 0., 0.)
        link1.Opacity <- 0.
        link2.Opacity <- 1.
        line.Opacity <- 1.
        triangle.Opacity <- 1.
        rect.Opacity <- 1.
        border.BorderBrush <- yellow
    let unhighlight() = 
        cm.AppMainCanvas.Children.Remove(c)
        link1.Opacity <- 1.
        link2.Opacity <- 0.
        line.Opacity <- 0.
        triangle.Opacity <- 0.
        rect.Opacity <- 0.
        border.BorderBrush <- Brushes.DimGray
    sp.MouseEnter.Add(fun _ -> highlight())
    sp.MouseLeave.Add(fun _ -> unhighlight())
    border, unhighlight

let makeOutlineShapesImpl(quest:string[]) =
    let outlines = ResizeArray()
    // fixed dungeon drawing outlines - vertical segments
    for i = 0 to 6 do
        for j = 0 to 7 do
            if quest.[j].Chars(i) <> quest.[j].Chars(i+1) then
                let s = new Shapes.Line(X1=float(i*(39+12)+39+12/2), Y1=float(TH+j*(27+12)-12/2), X2=float(i*(39+12)+39+12/2), Y2=float(TH+j*(27+12)+27+12/2), 
                                Stroke=Brushes.Red, StrokeThickness=3., IsHitTestVisible=false)
                outlines.Add(s)
    // fixed dungeon drawing outlines - horizontal segments
    for i = 0 to 7 do
        for j = 0 to 6 do
            if quest.[j].Chars(i) <> quest.[j+1].Chars(i) then
                let s = new Shapes.Line(X1=float(i*(39+12)-12/2), Y1=float(TH+(j+1)*(27+12)-12/2), X2=float(i*(39+12)+39+12/2), Y2=float(TH+(j+1)*(27+12)-12/2), 
                                Stroke=Brushes.Red, StrokeThickness=3., IsHitTestVisible=false)
                outlines.Add(s)
    outlines
let makeFirstQuestOutlineShapes(dungeonNumber) = makeOutlineShapesImpl(DungeonData.firstQuest.[dungeonNumber])
let makeSecondQuestOutlineShapes(dungeonNumber) = makeOutlineShapesImpl(DungeonData.secondQuest.[dungeonNumber])

////////////////////////

type TrackerLocation =
    | OVERWORLD
    | DUNGEON

let mutable theDungeonTabControl = null : TabControl

let makeDungeonTabs(cm:CustomComboBoxes.CanvasManager, posY, selectDungeonTabEvent:Event<int>, trackerLocationMoused:Event<_>, trackerDungeonMoused:Event<_>, TH, rightwardCanvas:Canvas, levelTabSelected:Event<_>, 
                    mainTrackerGhostbusters:Canvas[], showProgress, contentCanvasMouseEnterFunc, contentCanvasMouseLeaveFunc) = async {
    let dungeonTabsWholeCanvas = new Canvas(Height=float(2*TH + 3 + 27*8 + 12*7 + 3))  // need to set height, as caller uses it
    let outlineDrawingCanvases = Array.zeroCreate 9  // where we draw non-shapes-dungeons overlays
    let grabHelper = new Dungeon.GrabHelper()
    let grabModeTextBlock = 
        new Border(BorderThickness=Thickness(2.), BorderBrush=Brushes.LightGray, 
                    Child=new TextBlock(TextWrapping=TextWrapping.Wrap, FontSize=16., Foreground=Brushes.Black, Background=Brushes.Gray, IsHitTestVisible=false,
                                        Text="You are now in 'grab mode', which can be used to move an entire segment of dungeon rooms and doors at once.\n\nTo abort grab mode, click again on 'GRAB' in the upper right of the dungeon tracker.\n\nTo move a segment, first click any marked room, to pick up that room and all contiguous rooms.  Then click again on a new location to 'drop' the segment you grabbed.  After grabbing, hovering the mouse shows a preview of where you would drop.  This behaves like 'cut and paste', and adjacent doors will come along for the ride.\n\nUpon completion, you will be prompted to keep changes or undo them, so you can experiment.")
        )
    // TrackerModel.Options.BigIconsInDungeons  // whether user has checked they prefer big icons
    let mutable bigIconsTemp = false            // whether we are currently in the mouse hover to show big icons
    let mutable popupIsActive = false
    let dungeonTabs = new TabControl(FontSize=12., Background=Brushes.Black)
    theDungeonTabControl <- dungeonTabs
    let masterRoomStates = Array.init 9 (fun _ -> Array2D.init 8 8 (fun _ _ -> new DungeonRoomState.DungeonRoomState()))
    let levelTabs = Array.zeroCreate 9
    let contentCanvases = Array.zeroCreate 9
    let dummyCanvas = new Canvas(Opacity=0.0001, IsHitTestVisible=false)  // a kludge to help work around TabControl unloading tabs when not selected
    let localDungeonTrackerPanelWidth = 42.
    let exportFunctions = Array.create 9 (fun () -> new DungeonSaveAndLoad.DungeonModel())
    let importFunctions = Array.create 9 (fun _ -> ())
    for level = 1 to 9 do
        let levelTab = new TabItem(Background=Brushes.Black, Foreground=Brushes.Black)
        levelTabs.[level-1] <- levelTab
        let labelChar = if level = 9 then '9' else if TrackerModel.IsHiddenDungeonNumbers() then (char(int 'A' - 1 + level)) else (char(int '0' + level))
        let header = new TextBox(Width=22., Background=Brushes.Black, Foreground=Brushes.White, Text=sprintf "%c" labelChar, IsReadOnly=true, IsHitTestVisible=false, 
                                    HorizontalContentAlignment=HorizontalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), Padding=Thickness(0.))
        levelTab.Header <- header
        TrackerModel.GetDungeon(level-1).HiddenDungeonColorOrLabelChanged.Add(fun (color,_) -> 
            header.Background <- new SolidColorBrush(Graphics.makeColor(color))
            header.Foreground <- if Graphics.isBlackGoodContrast(color) then Brushes.Black else Brushes.White
            )
        let tileSunglasses = 0.75
        let blockerGridHeight = float(36*3)  // brittle, but that's the current constant
        let contentCanvas = new Canvas(Height=float(TH + 3 + 27*8 + 12*7 + 3), Width=float(3 + 39*8 + 12*7 + 3)+localDungeonTrackerPanelWidth, Background=Brushes.Black)
        // rupee/blank/key/bomb row highlighter
        let highlightRow =
            let bmp = Dungeon.MakeLoZMinimapDisplayBmp(Array2D.zeroCreate 8 8, '?') 
            let rupeeKeyBomb = new System.Drawing.Bitmap(8, 32)
            for i = 0 to 7 do
                for j = 0 to 31 do
                    rupeeKeyBomb.SetPixel(i, j, bmp.GetPixel(72+i, 8+j))
            let i = Graphics.BMPtoImage rupeeKeyBomb
            i.Width <- 16.
            i.Height <- 64.
            i.Stretch <- Stretch.UniformToFill
            RenderOptions.SetBitmapScalingMode(i, BitmapScalingMode.NearestNeighbor)
            canvasAdd(contentCanvas, i, contentCanvas.Width-20., 3.)
            let rowHighlightGrid = Graphics.makeGrid(1,8,20,8)
            let rowHighlighter = new Canvas(Width=16., Height=8., Background=Brushes.Gray, Opacity=0.)
            Graphics.gridAdd(rowHighlightGrid, rowHighlighter, 0, 0)
            canvasAdd(contentCanvas, rowHighlightGrid, contentCanvas.Width-40., 3.)
            let highlightRow(rowOpt) =
                match rowOpt with
                | None -> rowHighlighter.Opacity <- 0.
                | Some r -> rowHighlighter.Opacity <- 1.0; Grid.SetRow(rowHighlighter, r)
            highlightRow
        // local dungeon tracker
        let LD_X, LD_Y = contentCanvas.Width-localDungeonTrackerPanelWidth, blockerGridHeight - float(TH)
        let pos = Point(0. + LD_X, posY + LD_Y)  // appMainCanvas coords where the local tracker panel will be placed
        let mutable localDungeonTrackerPanel = null
        do
            let PopulateLocalDungeonTrackerPanel() =
                let ldtp,unhighlight = MakeLocalTrackerPanel(cm, pos, tileSunglasses, level, if level=9 then null else mainTrackerGhostbusters.[level-1])
                if localDungeonTrackerPanel<> null then
                    contentCanvas.Children.Remove(localDungeonTrackerPanel)  // remove old one
                localDungeonTrackerPanel <- ldtp
                canvasAdd(contentCanvas, localDungeonTrackerPanel, LD_X, LD_Y) // add new one
                // don't remove the old 'unhighlight' event listener - this is technically a leak, but unless you toggle '2nd quest dungeons' button a million times in one session, it won't matter
                dungeonTabs.SelectionChanged.Add(fun _ -> unhighlight())  // an extra safeguard, since the highlight is not a popup, but just a crazy mark over the main canvas
            PopulateLocalDungeonTrackerPanel()
            OptionsMenu.secondQuestDungeonsOptionChanged.Publish.Add(fun _ -> PopulateLocalDungeonTrackerPanel())
        // main dungeon content
        contentCanvas.MouseEnter.Add(fun _ -> contentCanvasMouseEnterFunc(level))
        contentCanvas.MouseLeave.Add(fun ea -> 
            // only hide the locator if we actually left
            // MouseLeave fires (erroneously?) when we e.g. click a room, and redraw() causes the element we are Entered to be removed from the tree and replaced by another,
            // even though we never left the bounds of the enclosing canvas
            let pos = ea.GetPosition(contentCanvas)
            if (pos.X < 0. || pos.X > contentCanvas.ActualWidth) || (pos.Y < 0. || pos.Y > contentCanvas.ActualHeight) then
                contentCanvasMouseLeaveFunc(level)
            )
        let dungeonCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7), Background=Brushes.Black)  
        let dungeonHeaderCanvas = new Canvas(Height=float(TH), Width=float(39*8 + 12*7))         // draw e.g. BOARD-5 here
        dungeonHeaderCanvas.ClipToBounds <- true
        canvasAdd(dungeonCanvas, dungeonHeaderCanvas, 0., 0.)
        let dungeonBodyCanvas = new Canvas(Height=float(27*8 + 12*7), Width=float(39*8 + 12*7))  // draw e.g. rooms here
        dungeonBodyCanvas.ClipToBounds <- true
        canvasAdd(dungeonCanvas, dungeonBodyCanvas, 0., float TH)
        let mutable isFirstTimeClickingAnyRoomInThisDungeonTab = true
        let numeral = new TextBox(Foreground=Brushes.Magenta, Background=Brushes.Transparent, Text=sprintf "%c" labelChar, IsReadOnly=true, IsHitTestVisible=false, FontSize=200., Opacity=0.25,
                            Width=dungeonBodyCanvas.Width, Height=dungeonBodyCanvas.Height, VerticalAlignment=VerticalAlignment.Center, FontWeight=FontWeights.Bold,
                            HorizontalContentAlignment=HorizontalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), Padding=Thickness(0.))
        let showNumeral() =
            numeral.Opacity <- 0.7
            let ctxt = Threading.SynchronizationContext.Current
            async { 
                do! Async.Sleep(1000)
                do! Async.SwitchToContext(ctxt)
                numeral.Opacity <- if isFirstTimeClickingAnyRoomInThisDungeonTab then 0.25 else 0.0 
            } |> Async.Start
        let dungeonSourceHighlightCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7))  // draw grab-source highlights here
        let dungeonHighlightCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7))  // draw grab highlights here
        canvasAdd(contentCanvas, dungeonCanvas, 3., 3.)
        canvasAdd(contentCanvas, dungeonSourceHighlightCanvas, 3., 3.)
        canvasAdd(contentCanvas, dungeonHighlightCanvas, 3., 3.)

        levelTab.Content <- contentCanvas
        contentCanvases.[level-1] <- contentCanvas
        dungeonTabs.Height <- contentCanvas.Height + 30.   // ok to set this 9 times
        dungeonTabs.Items.Add(levelTab) |> ignore

        let installDoorBehavior(door:Dungeon.Door, doorCanvas:Canvas) =
            doorCanvas.MouseDown.Add(fun ea ->
                if not grabHelper.IsGrabMode then  // cannot interact with doors in grab mode
                    if ea.ChangedButton = Input.MouseButton.Left then
                        if door.State <> Dungeon.DoorState.YES then
                            door.State <- Dungeon.DoorState.YES
                        else
                            door.State <- Dungeon.DoorState.UNKNOWN
                    elif ea.ChangedButton = Input.MouseButton.Right then
                        if door.State <> Dungeon.DoorState.NO then
                            door.State <- Dungeon.DoorState.NO
                        else
                            door.State <- Dungeon.DoorState.UNKNOWN
                    elif ea.ChangedButton = Input.MouseButton.Middle then
                        if door.State <> Dungeon.DoorState.LOCKED then
                            door.State <- Dungeon.DoorState.LOCKED
                        else
                            door.State <- Dungeon.DoorState.UNKNOWN
                )
        
        // horizontal doors
        let highlight = Dungeon.highlight
        let unknown = Dungeon.unknown
        let no = Dungeon.no
        let yes = Dungeon.yes
        let locked = Dungeon.locked
        let horizontalDoors = Array2D.zeroCreate 7 8
        for i = 0 to 6 do
            for j = 0 to 7 do
                let d = new Canvas(Width=12., Height=16., Background=Brushes.Black)
                let rect = new Shapes.Rectangle(Width=12., Height=16., Stroke=unknown, StrokeThickness=2., Fill=unknown)
                let line = new Shapes.Line(X1 = 6., Y1 = -12., X2 = 6., Y2 = 28., StrokeThickness=3., Stroke=no, Opacity=0.)
                let highlightOutline = new Shapes.Rectangle(Width=14., Height=18., Stroke=highlight, StrokeThickness=2., Fill=Brushes.Transparent, IsHitTestVisible=false, Opacity=0.)
                d.Children.Add(rect) |> ignore
                d.Children.Add(line) |> ignore
                canvasAdd(d, highlightOutline, -1., -1.)
                let door = new Dungeon.Door(Dungeon.DoorState.UNKNOWN, (function 
                    | Dungeon.DoorState.YES        -> rect.Stroke <- yes; rect.Fill <- yes; rect.Opacity <- 1.; line.Opacity <- 0.
                    | Dungeon.DoorState.NO         -> rect.Opacity <- 0.; line.Opacity <- 1.
                    | Dungeon.DoorState.LOCKED     -> rect.Stroke <- locked; rect.Fill <- locked; rect.Opacity <- 1.; line.Opacity <- 0.
                    | Dungeon.DoorState.UNKNOWN    -> rect.Stroke <- unknown; rect.Fill <- unknown; rect.Opacity <- 1.; line.Opacity <- 0.))
                horizontalDoors.[i,j] <- door
                canvasAdd(dungeonBodyCanvas, d, float(i*(39+12)+39), float(j*(27+12)+6))
                installDoorBehavior(door, d)
                d.MouseEnter.Add(fun _ -> if not popupIsActive && not grabHelper.IsGrabMode then highlightOutline.Opacity <- 0.6)
                d.MouseLeave.Add(fun _ -> highlightOutline.Opacity <- 0.0)
        // vertical doors
        let verticalDoors = Array2D.zeroCreate 8 7
        for i = 0 to 7 do
            for j = 0 to 6 do
                let d = new Canvas(Width=24., Height=12., Background=Brushes.Black)
                let rect = new Shapes.Rectangle(Width=24., Height=12., Stroke=unknown, StrokeThickness=2., Fill=unknown)
                let line = new Shapes.Line(X1 = -14., Y1 = 6., X2 = 38., Y2 = 6., StrokeThickness=3., Stroke=no, Opacity=0.)
                let highlightOutline = new Shapes.Rectangle(Width=26., Height=14., Stroke=highlight, StrokeThickness=2., Fill=Brushes.Transparent, IsHitTestVisible=false, Opacity=0.)
                d.Children.Add(rect) |> ignore
                d.Children.Add(line) |> ignore
                canvasAdd(d, highlightOutline, -1., -1.)
                let door = new Dungeon.Door(Dungeon.DoorState.UNKNOWN, (function 
                    | Dungeon.DoorState.YES        -> rect.Stroke <- yes; rect.Fill <- yes; rect.Opacity <- 1.; line.Opacity <- 0.
                    | Dungeon.DoorState.NO         -> rect.Opacity <- 0.; line.Opacity <- 1.
                    | Dungeon.DoorState.LOCKED     -> rect.Stroke <- locked; rect.Fill <- locked; rect.Opacity <- 1.; line.Opacity <- 0.
                    | Dungeon.DoorState.UNKNOWN    -> rect.Stroke <- unknown; rect.Fill <- unknown; rect.Opacity <- 1.; line.Opacity <- 0.))
                verticalDoors.[i,j] <- door
                canvasAdd(dungeonBodyCanvas, d, float(i*(39+12)+8), float(j*(27+12)+27))
                installDoorBehavior(door, d)
                d.MouseEnter.Add(fun _ -> if not popupIsActive && not grabHelper.IsGrabMode then highlightOutline.Opacity <- 0.6)
                d.MouseLeave.Add(fun _ -> highlightOutline.Opacity <- 0.0)
        // rooms
        let roomCanvases = Array2D.zeroCreate 8 8 
        let roomStates = masterRoomStates.[level-1]
        let roomIsCircled = Array2D.zeroCreate 8 8
        let usedTransports = Array.zeroCreate 9 // slot 0 unused
        let roomRedrawFuncs = ResizeArray()
        let redrawAllRooms() =
            for f in roomRedrawFuncs do
                f()
        // minimap-draw-er
        let hoverCanvas = new Canvas(Width=28., Height=28., Background=Brushes.Black, IsHitTestVisible=true)
        let minimini = Dungeon.MakeMiniMiniMapBmp() |> Graphics.BMPtoImage
        minimini.Width <- 24.
        minimini.Height <- 24.
        minimini.Stretch <- Stretch.UniformToFill
        minimini.Margin <- Thickness(2.)
        let miniBorder = new Border(Child=minimini, BorderThickness=Thickness(1.), BorderBrush=Brushes.Gray)
        canvasAdd(hoverCanvas, miniBorder, 0., 0.)
        canvasAdd(contentCanvas, hoverCanvas, LD_X+8., LD_Y+190.)
        hoverCanvas.MouseEnter.Add(fun _ ->
            let markedRooms = roomStates |> Array2D.map (fun s -> not(s.IsEmpty))
            let bmp = Dungeon.MakeLoZMinimapDisplayBmp(markedRooms, if TrackerModel.IsHiddenDungeonNumbers() then '?' else char(level+int '0')) 
            let i = Graphics.BMPtoImage bmp
            i.Width <- 240.
            i.Height <- 120.
            i.Stretch <- Stretch.UniformToFill
            RenderOptions.SetBitmapScalingMode(i, BitmapScalingMode.NearestNeighbor)
            let b = new Border(Child=new Border(Child=i, BorderThickness=Thickness(8.), BorderBrush=Brushes.Black), BorderThickness=Thickness(2.), BorderBrush=Brushes.Gray)
            Canvas.SetBottom(b, 0.)
            Canvas.SetLeft(b, -260.)
            hoverCanvas.Children.Add(b) |> ignore
            let vb = new VisualBrush(dungeonCanvas)
            trackerDungeonMoused.Trigger(vb)
            )
        hoverCanvas.MouseLeave.Add(fun _ -> 
            hoverCanvas.Children.Clear()
            canvasAdd(hoverCanvas, miniBorder, 0., 0.)
            trackerDungeonMoused.Trigger(null)
            )
        // big icons for monsters & floor drops
        let bigIconsCB = new CheckBox(Content=DungeonRoomState.mkTxt("I"))
        bigIconsCB.IsChecked <- System.Nullable.op_Implicit TrackerModel.Options.BigIconsInDungeons
        dungeonTabs.SelectionChanged.Add(fun _ -> bigIconsCB.IsChecked <- System.Nullable.op_Implicit TrackerModel.Options.BigIconsInDungeons)
        bigIconsCB.Checked.Add(fun _ -> TrackerModel.Options.BigIconsInDungeons <- true; TrackerModel.Options.writeSettings(); redrawAllRooms())
        bigIconsCB.Unchecked.Add(fun _ -> TrackerModel.Options.BigIconsInDungeons <- false; bigIconsTemp <- false; TrackerModel.Options.writeSettings(); redrawAllRooms())
        bigIconsCB.ToolTip <- "Toggle whether larger or smaller corner icons are shown on dungeon rooms"
        let bigIconsPanel = new DockPanel(Width=28., Height=21., Background=Graphics.almostBlack, IsHitTestVisible=true)
        bigIconsPanel.Children.Add(bigIconsCB) |> ignore
        let bigIconsBorder = new Border(Child=bigIconsPanel, BorderThickness=Thickness(1.), BorderBrush=Brushes.Gray)
        canvasAdd(contentCanvas, bigIconsBorder, LD_X+8., LD_Y+222.)
        bigIconsBorder.MouseEnter.Add(fun _ ->
            bigIconsTemp <- true
            redrawAllRooms()
            let mds, fds = new System.Collections.Generic.HashSet<_>(), new System.Collections.Generic.HashSet<_>()
            for i = 0 to 7 do
                for j = 0 to 7 do
                    match roomStates.[i,j].MonsterDetail with
                    | DungeonRoomState.MonsterDetail.Unmarked -> ()
                    | x -> mds.Add(x) |> ignore
                    match roomStates.[i,j].FloorDropDetail with
                    | DungeonRoomState.FloorDropDetail.Unmarked -> ()
                    | x -> fds.Add(x) |> ignore
            let columns = new StackPanel(Orientation=Orientation.Horizontal)
            let ms = new StackPanel(Orientation=Orientation.Vertical)
            mds |> Seq.iter (fun x -> ms.Children.Add(x.LegendIcon()) |> ignore)
            columns.Children.Add(ms) |> ignore
            let fs = new StackPanel(Orientation=Orientation.Vertical)
            fds |> Seq.iter (fun x -> fs.Children.Add(x.LegendIcon()) |> ignore)
            columns.Children.Add(fs) |> ignore
            let all = new StackPanel(Orientation=Orientation.Vertical)
            all.Children.Add(DungeonRoomState.mkTxt("All monster & floor drop icons marked:")) |> ignore
            all.Children.Add(columns) |> ignore
            if mds.Count=0 && fds.Count=0 then
                all.Children.Add(DungeonRoomState.mkTxt("(none)")) |> ignore
            let border = new Border(BorderBrush=Brushes.Gray, Background=Brushes.Black, BorderThickness=Thickness(3.), Child=all)
            Canvas.SetLeft(border, 0.)
            Canvas.SetBottom(border, 0.)
            rightwardCanvas.Height <- dungeonTabs.ActualHeight
            rightwardCanvas.Children.Add(border) |> ignore
            )
        bigIconsBorder.MouseLeave.Add(fun _ ->
            bigIconsTemp <- false
            redrawAllRooms()
            rightwardCanvas.Children.Clear()
            )
        // grab button for this tab
        let grabTB = new TextBox(FontSize=float(TH-12), Foreground=Brushes.Gray, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false,
                                Text="GRAB", BorderThickness=Thickness(0.), Margin=Thickness(0.), Padding=Thickness(0.),
                                HorizontalContentAlignment=HorizontalAlignment.Center, VerticalContentAlignment=VerticalAlignment.Center)
        let grabButton = new Button(Width=float(13*3), Height=float(TH-3), Content=grabTB, Background=Brushes.Black, BorderThickness=Thickness(2.), 
                                    Margin=Thickness(0.), Padding=Thickness(0.), Focusable=false,
                                    HorizontalContentAlignment=HorizontalAlignment.Stretch, VerticalContentAlignment=VerticalAlignment.Stretch)
        let grabRedraw() =
            if grabHelper.IsGrabMode then
                grabTB.Foreground <- Brushes.White
                grabTB.Background <- Brushes.Red
                dungeonTabs.Cursor <- System.Windows.Input.Cursors.Hand
                grabModeTextBlock.Opacity <- 1.
            else
                grabHelper.Abort()
                dungeonHighlightCanvas.Children.Clear()
                dungeonSourceHighlightCanvas.Children.Clear()
                grabTB.Foreground <- Brushes.Gray
                grabTB.Background <- Brushes.Black
                dungeonTabs.Cursor <- null
                grabModeTextBlock.Opacity <- 0.
        canvasAdd(dungeonCanvas, grabButton, float(7*51)+1., 0.)
        grabButton.Click.Add(fun _ ->
            grabHelper.ToggleGrabMode()
            grabRedraw()
            )
        dungeonTabs.SelectionChanged.Add(fun ea -> 
            try
                let x = ea.AddedItems.[0]
                if obj.ReferenceEquals(x,levelTab) then
                    dummyCanvas.Children.Clear()
                    levelTab.Content <- contentCanvas   // re-parent the content which may have been deparented by summary tab
                    levelTabSelected.Trigger(level)
                    showNumeral()
            with _ -> ()
            // the tab has already changed, kill the current grab
            if grabHelper.IsGrabMode then 
                grabHelper.Abort()
            // and always draw (even if changing to), as it was unable to repaint when disabled earlier
            grabRedraw()
            )

        let setNewValueFunctions = Array2D.create 8 8 (fun _ -> ())
        let backgroundColorCanvas = new Canvas(Width=float(51*6+12), Height=float(TH))
        canvasAdd(dungeonHeaderCanvas, backgroundColorCanvas, 0., 0.)
        TrackerModel.GetDungeon(level-1).HiddenDungeonColorOrLabelChanged.Add(fun (color,_) ->
            backgroundColorCanvas.Background <- new SolidColorBrush(Graphics.makeColor(color))
            )
        let mutable animateDungeonRoomTile = fun _ -> ()
        for i = 0 to 7 do
            let HFF = new FontFamily("Courier New")
            if i<>7 then
                let makeLetter(bmpFunc) =
                    let bmp = bmpFunc() 
                    let img = Graphics.BMPtoImage bmp
                    img.Width <- float TH
                    img.Height <- float TH
                    img.Stretch <- Stretch.UniformToFill
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor)
                    img
                if TrackerModel.IsHiddenDungeonNumbers() then
                    if i <> 6 || labelChar = '9' then
                        let mutable img = null
                        let update() =
                            dungeonHeaderCanvas.Children.Remove(img)
                            img <- makeLetter(fun() -> Dungeon.MakeLetterBmpInZeldaFont(((if TrackerModel.Options.BOARDInsteadOfLEVEL.Value then "BOARD" else "LEVEL")+"-9").Substring(i,1).[0], 
                                                                                                Graphics.isBlackGoodContrast(TrackerModel.GetDungeon(level-1).Color)))
                            canvasAdd(dungeonHeaderCanvas, img, float(i*51)+9., 0.)
                        update()
                        OptionsMenu.BOARDInsteadOfLEVELOptionChanged.Publish.Add(fun _ -> update())
                        TrackerModel.GetDungeon(level-1).HiddenDungeonColorOrLabelChanged.Add(fun _ -> update())
                    else
                        let gsc = new GradientStopCollection()
                        gsc.Add(new GradientStop(Colors.Red, 0.))
                        gsc.Add(new GradientStop(Colors.Orange, 0.2))
                        gsc.Add(new GradientStop(Colors.Yellow, 0.4))
                        gsc.Add(new GradientStop(Colors.LightGreen, 0.6))
                        gsc.Add(new GradientStop(Colors.LightBlue, 0.8))
                        gsc.Add(new GradientStop(Colors.MediumPurple, 1.))
                        let rainbowBrush = new LinearGradientBrush(gsc, 90.)
                        let tb = new TextBox(Width=float(13*3-16), Height=float(TH+8), FontSize=float(TH+8), Foreground=Brushes.Black, Background=rainbowBrush, IsReadOnly=true, IsHitTestVisible=false,
                                                Text=sprintf"%c"labelChar, BorderThickness=Thickness(0.), FontFamily=HFF, FontWeight=FontWeights.Bold,
                                                HorizontalContentAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
                        let tbc = new Canvas(Width=tb.Width, Height=float(TH-4), ClipToBounds=true)
                        canvasAdd(tbc, tb, 0., -8.)
                        let button = new Button(Height=float(TH), Content=tbc, BorderThickness=Thickness(2.), Margin=Thickness(0.), Padding=Thickness(0.), BorderBrush=Brushes.White)
                        canvasAdd(dungeonCanvas, button, float(i*51)+6., 0.)
                        let mutable popupIsActive = false
                        button.Click.Add(fun _ ->
                            if not popupIsActive then
                                let pos = tb.TranslatePoint(Point(tb.Width/2., tb.Height/2.), cm.AppMainCanvas)
                                async {
                                    do! Dungeon.HiddenDungeonColorChooserPopup(cm, 75., 310., 110., 110., TrackerModel.GetDungeon(level-1).Color, level-1)
                                    Graphics.WarpMouseCursorTo(pos)
                                    popupIsActive <- false
                                    } |> Async.StartImmediate
                            )
                else
                    let bmpFunc() = Dungeon.MakeLetterBmpInZeldaFont((sprintf "%s-%d " (if TrackerModel.Options.BOARDInsteadOfLEVEL.Value then "BOARD" else "LEVEL") level).Substring(i,1).[0], false)
                    let mutable img = makeLetter(bmpFunc)
                    canvasAdd(dungeonHeaderCanvas, img, float(i*51)+9., 0.)
                    OptionsMenu.BOARDInsteadOfLEVELOptionChanged.Publish.Add(fun _ -> 
                        dungeonHeaderCanvas.Children.Remove(img)
                        img <- makeLetter(bmpFunc)
                        canvasAdd(dungeonHeaderCanvas, img, float(i*51)+9., 0.)
                        )
            OptionsMenu.BOARDInsteadOfLEVELOptionChanged.Trigger() // to populate tb.Text the first time
            // room map
            for j = 0 to 7 do
                let BUFFER = 2.  // I often accidentally click room when trying to target doors with mouse, make canvas smaller and draw outside it, so clicks on very edge not seen
                let c = new Canvas(Width=float(13*3)-2.*BUFFER, Height=float(9*3)-2.*BUFFER, Background=Brushes.Black, IsHitTestVisible=true)
                canvasAdd(dungeonBodyCanvas, c, float(i*51)+BUFFER, float(j*39)+BUFFER)
                let highlightOutline = new Shapes.Rectangle(Width=float(13*3)+2., Height=float(9*3)+2., Stroke=highlight, StrokeThickness=2., Fill=Brushes.Transparent, IsHitTestVisible=false, Opacity=0.)
                roomCanvases.[i,j] <- c
                roomIsCircled.[i,j] <- false
                let redraw() =
                    c.Children.Clear()
                    let image = roomStates.[i,j].CurrentDisplay(TrackerModel.Options.BigIconsInDungeons || bigIconsTemp)
                    image.IsHitTestVisible <- false
                    canvasAdd(c, image, -BUFFER, -BUFFER)
                    canvasAdd(c, highlightOutline, -1.-BUFFER, -1.-BUFFER)
                    if roomIsCircled.[i,j] then
                        let ellipse = new Shapes.Ellipse(Width=float(13*3+12), Height=float(9*3+12), Stroke=Brushes.Yellow, StrokeThickness=3., IsHitTestVisible=false)
                        //ellipse.StrokeDashArray <- new DoubleCollection( seq[0.;2.5;6.;5.;6.;5.;6.;5.;6.;5.] )
                        ellipse.StrokeDashArray <- new DoubleCollection( seq[0.;12.5;8.;15.;8.;15.;] )
                        canvasAdd(c, ellipse, -6.-BUFFER, -6.-BUFFER)
                redraw()
                roomRedrawFuncs.Add(fun () -> redraw())
                let usedTransportsRemoveState(roomState:DungeonRoomState.DungeonRoomState) =
                    // track transport being changed away from
                    match roomState.RoomType.KnownTransportNumber with
                    | None -> ()
                    | Some n -> usedTransports.[n] <- usedTransports.[n] - 1
                let usedTransportsAddState(roomState:DungeonRoomState.DungeonRoomState) =
                    // note any new transports
                    match roomState.RoomType.KnownTransportNumber with
                    | None -> ()
                    | Some n -> usedTransports.[n] <- usedTransports.[n] + 1
                let SetNewValue(newState:DungeonRoomState.DungeonRoomState) =
                    let originalState = roomStates.[i,j]
                    let originallyWasNotMarked = originalState.RoomType.IsNotMarked
                    let isLegal = newState.RoomType = originalState.RoomType || 
                                    (match newState.RoomType.KnownTransportNumber with
                                        | None -> true
                                        | Some n -> usedTransports.[n]<>2)
                    if isLegal then
                        usedTransportsRemoveState(roomStates.[i,j])
                        roomStates.[i,j] <- newState
                        usedTransportsAddState(roomStates.[i,j])
                        // conservative door inference
                        if TrackerModel.Options.DoDoorInference.Value && originallyWasNotMarked && not newState.IsEmpty && newState.RoomType.KnownTransportNumber.IsNone then
                            // they appear to have walked into this room from an adjacent room
                            let possibleEntries = ResizeArray()
                            if i > 0 && not(roomStates.[i-1,j].IsEmpty) then
                                possibleEntries.Add(horizontalDoors.[i-1,j])
                            if i < 7 && not(roomStates.[i+1,j].IsEmpty) then
                                possibleEntries.Add(horizontalDoors.[i,j])
                            if j > 0 && not(roomStates.[i,j-1].IsEmpty) then
                                possibleEntries.Add(verticalDoors.[i,j-1])
                            if j < 7 && not(roomStates.[i,j+1].IsEmpty) then
                                possibleEntries.Add(verticalDoors.[i,j])
                            if possibleEntries.Count = 1 then
                                let door = possibleEntries.[0]
                                if door.State = Dungeon.DoorState.UNKNOWN then
                                    door.State <- Dungeon.DoorState.YES
                        redraw()
                        animateDungeonRoomTile(i,j)
                    else
                        System.Media.SystemSounds.Asterisk.Play()  // e.g. they tried to set this room to transport4, but two transport4s already exist
                setNewValueFunctions.[i,j] <- SetNewValue
                let activatePopup(positionAtEntranceRoomIcons) = async {
                    popupIsActive <- true
                    let roomPos = c.TranslatePoint(Point(), cm.AppMainCanvas)
                    let dashCanvas = new Canvas()
                    canvasAdd(c, dashCanvas, -BUFFER, -BUFFER)
                    CustomComboBoxes.MakePrettyDashes(dashCanvas, Brushes.Lime, 13.*3., 9.*3., 3., 2., 1.2)
                    let pos = Point(roomPos.X+13.*3./2., roomPos.Y+9.*3./2.)
                    do! DungeonRoomStateUI.DoModalDungeonRoomSelectAndDecorate(cm, roomStates.[i,j], usedTransports, SetNewValue, positionAtEntranceRoomIcons) 
                    c.Children.Remove(dashCanvas)
                    Graphics.WarpMouseCursorTo(pos)
                    redraw()
                    popupIsActive <- false
                    }
                let highlightImpl(canvas,contiguous:_[,], brush) =
                    for x = 0 to 7 do
                        for y = 0 to 7 do
                            if contiguous.[x,y] then
                                let r = new Shapes.Rectangle(Width=float(13*3 + 12), Height=float(9*3 + 12), Fill=brush, Opacity=0.4, IsHitTestVisible=false)
                                canvasAdd(canvas, r, float(x*51 - 6), float(TH+y*39 - 6))
                let highlight(contiguous:_[,], brush) = highlightImpl(dungeonHighlightCanvas,contiguous,brush)
                c.MyKeyAdd(fun ea ->
                    if not popupIsActive then
                        if not grabHelper.IsGrabMode then
                            isFirstTimeClickingAnyRoomInThisDungeonTab <- false  // hotkey cancels first-time click accelerator, so not to interfere with all-hotkey folks
                            numeral.Opacity <- 0.0
                            // idempotent action on marked part toggles to Unmarked; user can left click to toggle completed-ness
                            match HotKeys.DungeonRoomHotKeyProcessor.TryGetValue(ea.Key) with
                            | Some(Choice1Of3(roomType)) -> 
                                ea.Handled <- true
                                let workingCopy = roomStates.[i,j].Clone()
                                if workingCopy.RoomType = roomType then
                                    workingCopy.RoomType <- DungeonRoomState.RoomType.Unmarked
                                else
                                    workingCopy.RoomType <- roomType
                                SetNewValue(workingCopy)
                            | Some(Choice2Of3(monsterDetail)) -> 
                                ea.Handled <- true
                                let workingCopy = roomStates.[i,j].Clone()
                                if workingCopy.MonsterDetail = monsterDetail then
                                    workingCopy.MonsterDetail <- DungeonRoomState.MonsterDetail.Unmarked
                                else
                                    workingCopy.MonsterDetail <- monsterDetail
                                SetNewValue(workingCopy)
                            | Some(Choice3Of3(floorDropDetail)) -> 
                                ea.Handled <- true
                                let workingCopy = roomStates.[i,j].Clone()
                                if workingCopy.FloorDropDetail = floorDropDetail then
                                    workingCopy.FloorDropDetail <- DungeonRoomState.FloorDropDetail.Unmarked
                                else
                                    workingCopy.FloorDropDetail <- floorDropDetail
                                SetNewValue(workingCopy)
                            | None -> ()
                    )
                c.MouseEnter.Add(fun _ ->
                    if not popupIsActive then
                        if grabHelper.IsGrabMode then
                            if not grabHelper.HasGrab then
                                if not(roomStates.[i,j].IsEmpty) then
                                    dungeonHighlightCanvas.Children.Clear() // clear old preview
                                    let contiguous = grabHelper.PreviewGrab(i,j,roomStates)
                                    highlight(contiguous, Brushes.Lime)
                            else
                                dungeonHighlightCanvas.Children.Clear() // clear old preview
                                let ok,warn = grabHelper.PreviewDrop(i,j)
                                highlight(ok, Brushes.Lime)
                                highlight(warn, Brushes.Yellow)
                        else
                            highlightRow(Some j)
                            highlightOutline.Opacity <- 0.6
                            trackerLocationMoused.Trigger(TrackerLocation.DUNGEON,i,j)
                    )
                c.MouseLeave.Add(fun _ ->
                    if not popupIsActive then
                        if grabHelper.IsGrabMode then
                            dungeonHighlightCanvas.Children.Clear() // clear old preview
                    highlightRow(None)
                    highlightOutline.Opacity <- 0.0
                    trackerLocationMoused.Trigger(TrackerLocation.DUNGEON,-1,-1)
                    )
                c.MouseWheel.Add(fun _ -> 
                    if not popupIsActive then
                        if not grabHelper.IsGrabMode then  // cannot scroll rooms in grab mode
                            // scroll wheel activates the popup selector
                            activatePopup(false) |> Async.StartImmediate
                    )
                Graphics.setupClickVersusDrag(c, (fun ea ->
                    if not popupIsActive then
                        async {
                            if ea.ChangedButton = Input.MouseButton.Left then
                                if grabHelper.IsGrabMode then
                                    if not grabHelper.HasGrab then
                                        if not(roomStates.[i,j].IsEmpty) then
                                            dungeonHighlightCanvas.Children.Clear() // clear preview
                                            let contiguous = grabHelper.StartGrab(i,j,roomStates,roomIsCircled,horizontalDoors,verticalDoors)
                                            highlightImpl(dungeonSourceHighlightCanvas, contiguous, Brushes.Pink)  // this highlight stays around until completed/aborted
                                            highlight(contiguous, Brushes.Lime)
                                    else
                                        let backupRoomStates = roomStates |> Array2D.map (fun x -> x.Clone())
                                        let backupRoomIsCircled = roomIsCircled.Clone() :?> bool[,]
                                        let backupHorizontalDoors = horizontalDoors |> Array2D.map (fun c -> c.State)
                                        let backupVerticalDoors = verticalDoors |> Array2D.map (fun c -> c.State)
                                        grabHelper.DoDrop(i,j,roomStates,roomIsCircled,horizontalDoors,verticalDoors)
                                        redrawAllRooms()  // make updated changes visual
                                        let cmb = new CustomMessageBox.CustomMessageBox("Verify changes", System.Drawing.SystemIcons.Question, "You moved a dungeon segment. Keep this change?", ["Keep changes"; "Undo"])
                                        cmb.Owner <- Window.GetWindow(c)
                                        cmb.ShowDialog() |> ignore
                                        grabRedraw()  // DoDrop completes the grab, neeed to update the visual
                                        if cmb.MessageBoxResult = null || cmb.MessageBoxResult = "Undo" then
                                            // copy back from old state
                                            backupRoomStates |> Array2D.iteri (fun x y v -> roomStates.[x,y] <- v)
                                            backupRoomIsCircled |> Array2D.iteri (fun x y v -> roomIsCircled.[x,y] <- v)
                                            redrawAllRooms()  // make reverted changes visual
                                            horizontalDoors |> Array2D.iteri (fun x y c -> c.State <- backupHorizontalDoors.[x,y])
                                            verticalDoors |> Array2D.iteri (fun x y c -> c.State <- backupVerticalDoors.[x,y])
                                else
                                    // plain left click
                                    let workingCopy = roomStates.[i,j].Clone()
                                    if not(isFirstTimeClickingAnyRoomInThisDungeonTab) && roomStates.[i,j].RoomType.IsNotMarked then
                                        // ad hoc useful gesture for clicking unknown room - it moves it to explored & completed state
                                        workingCopy.RoomType <- DungeonRoomState.RoomType.MaybePushBlock
                                        workingCopy.IsComplete <- true
                                        SetNewValue(workingCopy)
                                    else
                                        if isFirstTimeClickingAnyRoomInThisDungeonTab then
                                            workingCopy.RoomType <- DungeonRoomState.RoomType.StartEnterFromS
                                            workingCopy.IsComplete <- true
                                            SetNewValue(workingCopy)
                                            isFirstTimeClickingAnyRoomInThisDungeonTab <- false
                                            numeral.Opacity <- 0.0
                                        else
                                            match roomStates.[i,j].RoomType.NextEntranceRoom() with
                                            | Some(next) -> roomStates.[i,j].RoomType <- next  // cycle the entrance arrow around cardinal positions
                                            | None ->
                                                // toggle completedness
                                                roomStates.[i,j].IsComplete <- not roomStates.[i,j].IsComplete
                                    redraw()
                            elif ea.ChangedButton = Input.MouseButton.Right then
                                if not grabHelper.IsGrabMode then  // cannot right click rooms in grab mode
                                    // plain right click
                                    do! activatePopup(isFirstTimeClickingAnyRoomInThisDungeonTab)
                                    isFirstTimeClickingAnyRoomInThisDungeonTab <- false
                                    numeral.Opacity <- 0.0
                                    redraw()
                            elif ea.ChangedButton = Input.MouseButton.Middle then
                                if not grabHelper.IsGrabMode then  // cannot middle click rooms in grab mode
                                    // middle click toggles floor drops, or if none, toggle circles
                                    if roomStates.[i,j].FloorDropDetail.IsNotMarked then
                                        roomIsCircled.[i,j] <- not roomIsCircled.[i,j]
                                    else
                                        roomStates.[i,j].ToggleFloorDropBrightness()
                                    redraw()
                        } |> Async.StartImmediate
                    ), (fun ea ->
                    if not popupIsActive then
                        // drag and drop to quickly 'paint' rooms
                        if not grabHelper.IsGrabMode then  // cannot initiate a drag in grab mode
                            if ea.LeftButton = System.Windows.Input.MouseButtonState.Pressed then
                                DragDrop.DoDragDrop(c, "L", DragDropEffects.Link) |> ignore
                            elif ea.RightButton = System.Windows.Input.MouseButtonState.Pressed then
                                DragDrop.DoDragDrop(c, "R", DragDropEffects.Link) |> ignore
                    ))
                c.DragOver.Add(fun ea ->
                    if not popupIsActive then
                        if roomStates.[i,j].RoomType.IsNotMarked then
                            isFirstTimeClickingAnyRoomInThisDungeonTab <- false  // originally painting cancels the first time accelerator (for 'play half dungeon, then start maybe-marking' scenario)
                            numeral.Opacity <- 0.0
                            if ea.Data.GetData(DataFormats.StringFormat) :?> string = "L" then
                                roomStates.[i,j].RoomType <- DungeonRoomState.RoomType.MaybePushBlock
                                roomStates.[i,j].IsComplete <- true
                            else
                                roomStates.[i,j].RoomType <- DungeonRoomState.RoomType.MaybePushBlock
                                roomStates.[i,j].IsComplete <- false
                            redraw()
                    )
                c.AllowDrop <- true
        let outlineDrawingCanvas = new Canvas()  // where we draw non-shapes-dungeons overlays
        outlineDrawingCanvases.[level-1] <- outlineDrawingCanvas
        canvasAdd(dungeonCanvas, outlineDrawingCanvas, 0., 0.)
        // animation
        do
            let c(t) = Color.FromArgb(t,255uy,165uy,0uy)
            let scb = new SolidColorBrush(c(0uy))
            let ca = new Animation.ColorAnimation(From=Nullable<_>(c(0uy)), To=Nullable<_>(c(180uy)), Duration=new Duration(TimeSpan.FromSeconds(1.0)), AutoReverse=true)
            let roomHighlightTile = new Shapes.Rectangle(Width=float(13*3)+6., Height=float(9*3)+6., StrokeThickness=3., Stroke=scb, Opacity=1.0, IsHitTestVisible=false)
            canvasAdd(dungeonBodyCanvas, roomHighlightTile, 0., 0.)
            let animateRoomTile(x,y) = 
                if TrackerModel.Options.AnimateTileChanges.Value then
                    Canvas.SetLeft(roomHighlightTile, float(x*51)-3.)
                    Canvas.SetTop(roomHighlightTile, float(y*39)-3.)
                    scb.BeginAnimation(SolidColorBrush.ColorProperty, ca)
            animateDungeonRoomTile <- animateRoomTile
        // "sunglasses"
        let darkenRect = new Shapes.Rectangle(Width=dungeonCanvas.Width, Height=dungeonCanvas.Height, StrokeThickness = 0., Fill=Brushes.Black, Opacity=0.15, IsHitTestVisible=false)
        canvasAdd(dungeonCanvas, darkenRect, 0., 0.)
        canvasAdd(dungeonBodyCanvas, numeral, 0., 0.)  // so numeral displays atop all else
        exportFunctions.[level-1] <- (fun () ->
            let r = new DungeonSaveAndLoad.DungeonModel()
            r.HorizontalDoors <- Array.init 7 (fun i -> Array.init 8 (fun j -> horizontalDoors.[i,j].State.AsInt()))
            r.VerticalDoors <-   Array.init 8 (fun i -> Array.init 7 (fun j -> verticalDoors.[i,j].State.AsInt()))
            r.RoomIsCircled <-   Array.init 8 (fun i -> Array.init 8 (fun j -> roomIsCircled.[i,j]))
            r.RoomStates <-      Array.init 8 (fun i -> Array.init 8 (fun j -> roomStates.[i,j] |> DungeonSaveAndLoad.DungeonRoomStateAsModel))
            r
            )
        importFunctions.[level-1] <- (fun (dm:DungeonSaveAndLoad.DungeonModel) ->
            for i = 0 to 6 do
                for j = 0 to 7 do
                    horizontalDoors.[i,j].State <- Dungeon.DoorState.FromInt dm.HorizontalDoors.[j].[i]
            for i = 0 to 7 do
                for j = 0 to 6 do
                    verticalDoors.[i,j].State <- Dungeon.DoorState.FromInt dm.VerticalDoors.[j].[i]
            for i = 0 to 7 do
                for j = 0 to 7 do
                    roomIsCircled.[i,j] <- dm.RoomIsCircled.[j].[i]
                    let jsonModel = dm.RoomStates.[j].[i]
                    if jsonModel <> null then
                        let rs = jsonModel.AsDungeonRoomState()
                        if rs.RoomType <> DungeonRoomState.RoomType.Unmarked then
                            isFirstTimeClickingAnyRoomInThisDungeonTab <- false
                            numeral.Opacity <- 0.0 
                        setNewValueFunctions.[i,j](rs)
            )
        do! showProgress()
    // end -- for level in 1 to 9 do
    do
        // summary tab
        let levelTab = new TabItem(Background=Brushes.Black, Foreground=Brushes.Black)
        let labelChar = 'S'
        let header = new TextBox(Width=22., Background=Brushes.Black, Foreground=Brushes.White, Text=sprintf "%c" labelChar, IsReadOnly=true, IsHitTestVisible=false, 
                                 HorizontalContentAlignment=HorizontalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), Padding=Thickness(0.))
        levelTab.Header <- header
        let contentCanvas = new Canvas(Height=float(TH + 3 + 27*8 + 12*7 + 3), Width=float(3 + 39*8 + 12*7 + 3)+localDungeonTrackerPanelWidth, Background=Brushes.Black)
        contentCanvas.Children.Add(dummyCanvas) |> ignore
        dungeonTabs.SelectionChanged.Add(fun ea -> 
            try
                let x = ea.AddedItems.[0]
                if obj.ReferenceEquals(x,levelTab) then
                    dummyCanvas.Children.Clear()
                    for i = 0 to 7 do
                        // deparent content canvases
                        levelTabs.[i].Content <- null
                        // make them visually here (but hidden by the dummy's lack of opacity), so that updates (like BOARD<->LEVEL) get visually drawn to be picked up by VisualBrush
                        dummyCanvas.Children.Add(contentCanvases.[i]) |> ignore
                    levelTabSelected.Trigger(10)
            with _ -> ()
            )
        // grid
        let w, h = int contentCanvas.Width / 3, int contentCanvas.Height / 3
        let g = Graphics.makeGrid(3, 3, w, h)
        let make(i) =
            let mini = new Shapes.Rectangle(Width=float w, Height=float h, Fill=new VisualBrush(contentCanvases.[i]))
            let midi = new Shapes.Rectangle(Width=2.* float w, Height=2.* float h, Fill=new VisualBrush(contentCanvases.[i]))
            let overlay = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(5.), Background=Brushes.Black, IsHitTestVisible=false, Child=midi)
            Canvas.SetLeft(overlay, 0.)
            Canvas.SetBottom(overlay, 0.)
            mini.MouseEnter.Add(fun _ ->
                rightwardCanvas.Height <- dungeonTabs.ActualHeight
                rightwardCanvas.Children.Clear()
                rightwardCanvas.Children.Add(overlay) |> ignore
                levelTabSelected.Trigger(i+1)
                )
            mini.MouseLeave.Add(fun _ ->
                rightwardCanvas.Children.Clear()
                if dungeonTabs.SelectedIndex=9 then  // we may have clicked on D4, and MouseLeave fires after tab is switched
                    levelTabSelected.Trigger(10)
                )
            mini.MouseDown.Add(fun _ ->
                rightwardCanvas.Children.Clear()
                System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new System.Action(fun () -> 
                    dungeonTabs.SelectedIndex <- i
                    levelTabSelected.Trigger(i+1)
                    )) |> ignore
                )
            mini
        let text = new TextBox(Text="\nDungeon Summary\n\nHover to preview\n\nClick to switch tab\n", Margin=Thickness(0.,0.,8.,0.),
                               Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                               FontSize=12., HorizontalContentAlignment=HorizontalAlignment.Center)
        Graphics.gridAdd(g, text, 0, 0)
        Graphics.gridAdd(g, make(0), 1, 0)
        Graphics.gridAdd(g, make(1), 2, 0)
        Graphics.gridAdd(g, make(2), 0, 1)
        Graphics.gridAdd(g, make(3), 1, 1)
        Graphics.gridAdd(g, make(4), 2, 1)
        Graphics.gridAdd(g, make(5), 0, 2)
        Graphics.gridAdd(g, make(6), 1, 2)
        Graphics.gridAdd(g, make(7), 2, 2)
        canvasAdd(contentCanvas, g, 0., 0.)
        levelTab.Content <- contentCanvas
        dungeonTabs.Items.Add(levelTab) |> ignore
    dungeonTabs.SelectedIndex <- 9
    selectDungeonTabEvent.Publish.Add(fun i -> dungeonTabs.SelectedIndex <- i)

    // make the whole canvas
    canvasAdd(dungeonTabsWholeCanvas, dungeonTabs, 0., 0.) 

    if TrackerModel.IsHiddenDungeonNumbers() then
        let button = new Button(Content=new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, BorderThickness=Thickness(0.), 
                                                    Text="FQ/SQ", IsReadOnly=true, IsHitTestVisible=false),
                                        BorderThickness=Thickness(1.), Margin=Thickness(0.), Padding=Thickness(0.))
        canvasAdd(dungeonTabsWholeCanvas, button, 360., 0.)
        
        let currentDisplayState = Array.zeroCreate 9   // 0=nothing, 1-9 = FQ, 10-18 = SQ

        let mkTxt(txt,ok) =
            new TextBox(Width=50., Height=30., FontSize=15., Foreground=(if ok then Brushes.Lime else Brushes.Red), Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                        BorderThickness=Thickness(0.), Text=txt, VerticalContentAlignment=VerticalAlignment.Center, HorizontalContentAlignment=HorizontalAlignment.Center,
                        VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center)

        let mutable popupIsActive = false
        button.Click.Add(fun _ ->
            if not popupIsActive && not(dungeonTabs.SelectedIndex=9) then  // no behavior on summary tab
                popupIsActive <- true
                
                let ST = CustomComboBoxes.borderThickness
                let doRedraw(canvasToRedraw:Canvas, state) =
                    canvasToRedraw.Children.Clear() |> ignore
                    if state>=1 && state<=9 then
                        for s in makeFirstQuestOutlineShapes(state-1) do
                            canvasAdd(canvasToRedraw, s, 0., 0.)
                    if state>=10 && state<=18 then
                        for s in makeSecondQuestOutlineShapes(state-10) do
                            canvasAdd(canvasToRedraw, s, 0., 0.)
                
                let SI = dungeonTabs.SelectedIndex
                let roomStates = masterRoomStates.[SI]
                let isCompatible(q,l) =
                    let quest = if q=1 then DungeonData.firstQuest else DungeonData.secondQuest
                    let mutable ok = true
                    for x = 0 to 7 do
                        for y = 0 to 7 do
                            if not(roomStates.[x,y].IsEmpty) && quest.[l-1].[y].Chars(x)<>'X' then
                                ok <- false
                    ok
                let pos = outlineDrawingCanvases.[SI].TranslatePoint(Point(), cm.AppMainCanvas)
                let tileCanvas = new Canvas(Width=float(39*8 + 12*7), Height=float(TH + 27*8 + 12*7))
                let gridElementsSelectablesAndIDs = [|
                    yield (upcast mkTxt("none",true):FrameworkElement), true, 0
                    for l = 1 to 9 do
                        yield (upcast mkTxt(sprintf "1Q%d" l, isCompatible(1,l)):FrameworkElement), true, l
                    yield (upcast mkTxt("none",true):FrameworkElement), true, 0    // two 'none's just to make the grid look nicer
                    for l = 1 to 9 do
                        yield (upcast mkTxt(sprintf "2Q%d" l, isCompatible(2,l)):FrameworkElement), true, l+9
                    |]
                let originalStateIndex = 
                    if currentDisplayState.[SI] >= 10 then 
                        1+currentDisplayState.[SI]   // skip over the extra 'none'
                    else 
                        currentDisplayState.[SI]
                let activationDelta = 0
                let (gnc, gnr, gcw, grh) = (5, 4, 50, 30)
                let gx, gy = tileCanvas.Width + ST, 0.
                let redrawTile(state) = 
                    doRedraw(tileCanvas, state)
                let onClick(_ea, state) = CustomComboBoxes.DismissPopupWithResult(state)
                let extraDecorations = [|
                    (upcast new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Child=
                        new TextBox(FontSize=15., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), Margin=Thickness(3.),
                                    Text="Choose a vanilla dungeon outline to\ndraw on this dungeon map tab.\n\n"+
                                            "Green selections are compatible with\nyour currently marked rooms.\n\nChoose 'none' to remove outline.")
                        ):FrameworkElement), float gx, float gnr*(2.*ST+float grh)+2.*ST
                    |]
                let brushes = CustomComboBoxes.ModalGridSelectBrushes.Defaults()
                let gridClickDismissalDoesMouseWarpBackToTileCenter = false
                outlineDrawingCanvases.[SI].Children.Clear()  // remove current outline; the tileCanvas is transparent, and seeing the old one is bad. restored later
                async {
                    let! r = CustomComboBoxes.DoModalGridSelect(cm, pos.X, pos.Y, tileCanvas, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (gnc, gnr, gcw, grh),
                                    gx, gy, redrawTile, onClick, extraDecorations, brushes, gridClickDismissalDoesMouseWarpBackToTileCenter, None)
                    match r with
                    | Some(state) -> currentDisplayState.[SI] <- state
                    | None -> ()
                    doRedraw(outlineDrawingCanvases.[SI], currentDisplayState.[SI])
                    popupIsActive <- false
                    } |> Async.StartImmediate
            )
    else
        let fqcb = new CheckBox(Content=new TextBox(Text="FQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
        fqcb.ToolTip <- "Show vanilla first quest dungeon outlines"
        let sqcb = new CheckBox(Content=new TextBox(Text="SQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
        sqcb.ToolTip <- "Show vanilla second quest dungeon outlines"

        fqcb.IsChecked <- System.Nullable.op_Implicit false
        fqcb.Checked.Add(fun _ -> 
            sqcb.IsChecked <- System.Nullable.op_Implicit false
            for i = 0 to 8 do
                outlineDrawingCanvases.[i].Children.Clear() |> ignore
                for s in makeFirstQuestOutlineShapes(i) do
                    canvasAdd(outlineDrawingCanvases.[i], s, 0., 0.)
            )
        fqcb.Unchecked.Add(fun _ -> outlineDrawingCanvases |> Seq.iter (fun odc -> odc.Children.Clear()))
        canvasAdd(dungeonTabsWholeCanvas, fqcb, 350., 0.) 

        sqcb.IsChecked <- System.Nullable.op_Implicit false
        sqcb.Checked.Add(fun _ -> 
            fqcb.IsChecked <- System.Nullable.op_Implicit false
            for i = 0 to 8 do
                outlineDrawingCanvases.[i].Children.Clear() |> ignore
                for s in makeSecondQuestOutlineShapes(i) do
                    canvasAdd(outlineDrawingCanvases.[i], s, 0., 0.)
            )
        sqcb.Unchecked.Add(fun _ -> outlineDrawingCanvases |> Seq.iter (fun odc -> odc.Children.Clear()))
        canvasAdd(dungeonTabsWholeCanvas, sqcb, 400., 0.) 

    let exportDungeonModelsJsonLines() = DungeonSaveAndLoad.SaveAllDungeons [| for f in exportFunctions do yield f() |]
    let importDungeonModels(dma : DungeonSaveAndLoad.DungeonModel[]) =
        for i = 0 to 8 do
            importFunctions.[i](dma.[i])
    return dungeonTabsWholeCanvas, grabModeTextBlock, exportDungeonModelsJsonLines, importDungeonModels
    }