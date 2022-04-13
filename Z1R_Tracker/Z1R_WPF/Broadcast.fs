module Broadcast

open OverworldItemGridUI

open OverworldMapTileCustomization

open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let allWithinOneScreen(w:Window) =
    let resHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height  // e.g. 1440 pixels
    let actualHeight = SystemParameters.PrimaryScreenHeight  // e.g. 960 units
    let scale = float resHeight / actualHeight  // e.g.   1.5 

    //printfn "Broadcast LTWHS: %f %f %f %f %f" w.Left w.Top w.Width w.Height scale
    // for whatever reason, this FUDGE is needed to be accurate
    let W_FUDGE = 8
    let H_FUDGE = 10
    let rect = new System.Drawing.Rectangle(int(w.Left * scale), int(w.Top * scale), int(w.Width * scale) - W_FUDGE, int(w.Height * scale) - H_FUDGE)

    let screens = System.Windows.Forms.Screen.AllScreens
    let mutable ok = false
    for s in screens do
        //printfn "Screen LTWH: %d %d %d %d" s.WorkingArea.Left s.WorkingArea.Top s.WorkingArea.Width s.WorkingArea.Height
        if s.WorkingArea.Contains(rect) then
            //printfn "ok on that screen"
            ok <- true
        else
            //printfn "not ok on that screen"
            ()
    ok

let MakeBroadcastWindow(cm:CustomComboBoxes.CanvasManager, blockerGrid:Grid, dungeonTabsOverlayContent:Canvas, refocusMainWindow) =
    let appMainCanvas = cm.AppMainCanvas
    let makeBroadcastWindow(size, showOverworldMagnifier) =
        let W = 768.
        let scaleSize = if size=1 then 256. elif size=2 then 512. else 768.
        let broadcastWindow = new Window()
        broadcastWindow.Title <- "Z-Tracker broadcast"
        broadcastWindow.ResizeMode <- ResizeMode.NoResize
        broadcastWindow.SizeToContent <- SizeToContent.Manual
        broadcastWindow.WindowStartupLocation <- WindowStartupLocation.Manual

        let topBar = new StackPanel(Orientation=Orientation.Vertical, HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center, 
                                        Background=new SolidColorBrush(Color.FromRgb(120uy, 30uy, 30uy)), Width=scaleSize, Opacity=0.0)
        let tb = new TextBox(Text="The broadcast window is at least partially off-screen.  Streaming software like OBS cannot capture display updates to off-screen windows.  " +
                                    "Re-position this window so that it is entirely within the bounds of a screen.  It is ok for this window to be BEHIND another window, " +
                                    "such as behind the main Z-Tracker application window.  See the Z-Tracker documentation for details.",
                                    Foreground=Brushes.White, Background=Brushes.Transparent, 
                                    IsReadOnly=true, BorderThickness=Thickness(0.), FontSize=14., VerticalAlignment=VerticalAlignment.Center, TextWrapping=TextWrapping.Wrap)
        topBar.Children.Add(tb) |> ignore
        let updateTopBar() =
            if allWithinOneScreen(broadcastWindow) then
                topBar.Opacity <- 0.0
            else
                topBar.Opacity <- 1.0

        let leftTop = TrackerModel.Options.BroadcastWindowLT
        let matches = System.Text.RegularExpressions.Regex.Match(leftTop, """^(-?\d+),(-?\d+)$""")
        if matches.Success then
            broadcastWindow.Left <- float matches.Groups.[1].Value
            broadcastWindow.Top <- float matches.Groups.[2].Value

        broadcastWindow.Loaded.Add(fun _ -> updateTopBar())
        broadcastWindow.LocationChanged.Add(fun _ ->
            updateTopBar()
            TrackerModel.Options.BroadcastWindowLT <- sprintf "%d,%d" (int broadcastWindow.Left) (int broadcastWindow.Top)
            TrackerModel.Options.writeSettings()
            )
        broadcastWindow.Width <- scaleSize + 16.
        broadcastWindow.Owner <- Application.Current.MainWindow
        broadcastWindow.Background <- Brushes.Black

        let makeViewRect(upperLeft:Point, lowerRight:Point) =
            let vb = new VisualBrush(appMainCanvas)
            vb.ViewboxUnits <- BrushMappingMode.Absolute
            vb.Viewbox <- Rect(upperLeft, lowerRight)
            vb.Stretch <- Stretch.None
            let bwRect = new Shapes.Rectangle(Width=vb.Viewbox.Width, Height=vb.Viewbox.Height)
            bwRect.Fill <- vb
            bwRect
        let dealWithPopups(topViewboxRelativeToApp, topViewboxRelativeToThisBroadcast, c) =
            let popups = new System.Collections.Generic.Stack<_>()
            let popupCanvasArea = new Canvas()
            canvasAdd(c, popupCanvasArea, 0., 0.)
            cm.AfterCreatePopupCanvas.Add(fun pc ->
                let vb = new VisualBrush(pc)
                vb.ViewboxUnits <- BrushMappingMode.Absolute
                vb.Viewbox <- Rect(Point(0.,0.), Point(appMainCanvas.Width,appMainCanvas.Height+CustomComboBoxes.BROADCAST_KLUDGE))
                vb.Stretch <- Stretch.None
                let bwRect = new Shapes.Rectangle(Width=vb.Viewbox.Width, Height=vb.Viewbox.Height)
                bwRect.Fill <- vb
                popups.Push(bwRect)
                canvasAdd(popupCanvasArea, bwRect, 0., topViewboxRelativeToThisBroadcast - topViewboxRelativeToApp)
                )
            cm.BeforeDismissPopupCanvas.Add(fun _pc ->
                if popups.Count > 0 then   // we can underflow if the user turns on the broadcast window mid-game, as the options window was a popup when the broadcast began
                    let bwRect = popups.Pop()
                    popupCanvasArea.Children.Remove(bwRect)
                )

        let timeline = makeViewRect(Point(0.,START_TIMELINE_H), Point(W,THRU_TIMELINE_H))
    
        // construct the top broadcast canvas (topc)
        let notesY = START_DUNGEON_AND_NOTES_AREA_H + blockerGrid.Height
        let top = makeViewRect(Point(0.,0.), Point(W,notesY))
        let H = top.Height + (THRU_TIMELINE_H - START_TIMELINE_H)  // the top one is the larger of the two, so always have window that size
        let topc = new Canvas(Width=W, Height=H)
        canvasAdd(topc, top, 0., 0.)
        let notes = makeViewRect(Point(BLOCKERS_AND_NOTES_OFFSET,notesY), Point(W,notesY+blockerGrid.Height))
        canvasAdd(topc, notes, 0., START_DUNGEON_AND_NOTES_AREA_H)
        let blackArea = new Canvas(Width=BLOCKERS_AND_NOTES_OFFSET*2.-W, Height=blockerGrid.Height, Background=Brushes.Black)
        canvasAdd(topc, blackArea, W-BLOCKERS_AND_NOTES_OFFSET, START_DUNGEON_AND_NOTES_AREA_H)
        if showOverworldMagnifier then
            let magnifierView = new Canvas(Width=dungeonTabsOverlayContent.Width, Height=dungeonTabsOverlayContent.Height, Background=new VisualBrush(dungeonTabsOverlayContent))
            Canvas.SetLeft(magnifierView, 0.)
            Canvas.SetBottom(magnifierView, 0.)
            topc.Children.Add(magnifierView) |> ignore
        dealWithPopups(0., 0., topc)

        // construct the bottom broadcast canvas (bottomc)
        let dun = makeViewRect(Point(0.,THRU_MAIN_MAP_AND_ITEM_PROGRESS_H), Point(W,START_TIMELINE_H))
        let tri = makeViewRect(Point(0.,0.), Point(W,150.))
        let pro = makeViewRect(Point(ITEM_PROGRESS_FIRST_ITEM,THRU_MAP_AND_LEGEND_H), 
                                Point(ITEM_PROGRESS_FIRST_ITEM + 13.*30.-11.,THRU_MAIN_MAP_AND_ITEM_PROGRESS_H))  // -11. because 'Hint decoder' button infringes into empty space by Any Key 
        pro.HorizontalAlignment <- HorizontalAlignment.Left
        pro.Margin <- Thickness(20.,0.,0.,0.)
        let owm = makeViewRect(Point(0.,150.), Point(W,THRU_MAIN_MAP_H))
        let sp = new StackPanel(Orientation=Orientation.Vertical)
        sp.Children.Add(tri) |> ignore
        sp.Children.Add(pro) |> ignore
        sp.Children.Add(dun) |> ignore
        let bottomc = new Canvas(Width=W, Height=H)
        canvasAdd(bottomc, sp, 0., 0.)
        let afterSoldItemBoxesX = OW_ITEM_GRID_LOCATIONS.OFFSET + 120.
        let scaleW = (W - afterSoldItemBoxesX) / W
        let scaleH = (180.-45.)/(THRU_MAIN_MAP_H-150.)
        owm.RenderTransform <- new ScaleTransform(scaleW,scaleH)
        canvasAdd(bottomc, owm, afterSoldItemBoxesX, 45.)
        // WANT!
        let kitty = new Image()
        let imageStream = Graphics.GetResourceStream("CroppedBrianKitty.png")
        kitty.Source <- System.Windows.Media.Imaging.BitmapFrame.Create(imageStream)
        kitty.Width <- 45.
        kitty.Height <- 45.
        canvasAdd(bottomc, kitty, afterSoldItemBoxesX+120. + 20., 0.)
        let ztlogo = new Image()
        let imageStream = Graphics.GetResourceStream("ZTlogo64x64.png")
        ztlogo.Source <- System.Windows.Media.Imaging.BitmapFrame.Create(imageStream)
        ztlogo.Width <- 30.
        ztlogo.Height <- 30.
        let logoBorder = new Border(BorderThickness=Thickness(1.), BorderBrush=Brushes.Gray, Child=ztlogo)
        canvasAdd(bottomc, logoBorder, afterSoldItemBoxesX+120. + 20. + 25., 11.)

        dealWithPopups(THRU_MAIN_MAP_AND_ITEM_PROGRESS_H, 180., bottomc)

        // draw fake mice on top level 
        let addFakeMouse(c:Canvas) =
            let fakeMouse = new Shapes.Polygon(Fill=Brushes.White)
            fakeMouse.Points <- new PointCollection([Point(0.,0.); Point(12.,6.); Point(6.,12.)])
            c.Children.Add(fakeMouse) |> ignore
            fakeMouse
        let fakeTopMouse = addFakeMouse(topc)
        let fakeBottomMouse = addFakeMouse(bottomc)

        // set up the main broadcast window
        broadcastWindow.Height <- H + 40.
        let dp = new DockPanel(Width=W, Height=H)
        dp.UseLayoutRounding <- true
        DockPanel.SetDock(timeline, Dock.Bottom)
        dp.Children.Add(timeline) |> ignore
        dp.Children.Add(topc) |> ignore
        
        let mutable timerX = 600.
        let factor = if size=1 then 0.333333 elif size=2 then 0.666666 else 1.0
        if size=1 || size=2 then
            let trans = new ScaleTransform(factor, factor)
            dp.LayoutTransform <- trans
            OverworldItemGridUI.broadcastTimeTextBox.LayoutTransform <- trans
            timerX <- timerX * factor
            broadcastWindow.Height <- H*factor + 40.
        else
            OverworldItemGridUI.broadcastTimeTextBox.LayoutTransform <- null
        let c = new Canvas(Width=W, Height=H)
        c.Children.Add(dp) |> ignore
        OverworldItemGridUI.broadcastTimeTextBox.Parent :?> Canvas |> (fun c -> if c <> null then c.Children.Remove(OverworldItemGridUI.broadcastTimeTextBox))  // deparent from prior window
        canvasAdd(c, OverworldItemGridUI.broadcastTimeTextBox, timerX, -10. * factor)
        c.Children.Add(topBar) |> ignore
        broadcastWindow.Content <- c
        
        let mutable isUpper = true
        cm.RootCanvas.MouseMove.Add(fun ea ->   // we need RootCanvas to see mouse moving in popups
            let mousePos = ea.GetPosition(appMainCanvas)
            if mousePos.Y < THRU_MAIN_MAP_AND_ITEM_PROGRESS_H then
                if not isUpper then
                    if cm.PopupCanvasStack.Count=0 then  // don't switch panes if a popup is active
                        isUpper <- true
                        dp.Children.RemoveAt(1)
                        dp.Children.Add(topc) |> ignore
                Canvas.SetLeft(fakeTopMouse, mousePos.X)
                Canvas.SetTop(fakeTopMouse, mousePos.Y)
            else
                if isUpper then
                    if cm.PopupCanvasStack.Count=0 then  // don't switch panes if a popup is active
                        isUpper <- false
                        dp.Children.RemoveAt(1)
                        dp.Children.Add(bottomc) |> ignore
                Canvas.SetLeft(fakeBottomMouse, mousePos.X)
                if mousePos.Y > START_TIMELINE_H && cm.PopupCanvasStack.Count=0 then
                    // The timeline is docked to the bottom in both the upper and lower views.
                    // There is 'dead space' below the dungeons area and above the timeline in the broadcast window.
                    // The fakeMouse should 'jump over' this dead space so that mouse-gestures in the timeline show in the right spot on the timeline.
                    // However this means that certain areas of the options-pane popup won't be fakeMouse-displayed correctly, 
                    // so we only do this offset when no popup is active.
                    let yDistanceMouseToBottom = appMainCanvas.Height - mousePos.Y
                    Canvas.SetTop(fakeBottomMouse, H - yDistanceMouseToBottom)
                else
                    Canvas.SetTop(fakeBottomMouse, mousePos.Y - THRU_MAIN_MAP_AND_ITEM_PROGRESS_H + 180.)
            )
        broadcastWindow
    let mutable broadcastWindow = null
    if TrackerModel.Options.ShowBroadcastWindow.Value then
        broadcastWindow <- makeBroadcastWindow(TrackerModel.Options.BroadcastWindowSize, TrackerModel.Options.BroadcastWindowIncludesOverworldMagnifier.Value)
        broadcastWindow.Show()
        refocusMainWindow()
    OptionsMenu.broadcastWindowOptionChanged.Publish.Add(fun () ->
        // close existing
        if broadcastWindow<>null then
            broadcastWindow.Close()
            broadcastWindow <- null
        // maybe restart
        if TrackerModel.Options.ShowBroadcastWindow.Value then
            broadcastWindow <- makeBroadcastWindow(TrackerModel.Options.BroadcastWindowSize, TrackerModel.Options.BroadcastWindowIncludesOverworldMagnifier.Value)
            broadcastWindow.Show()
            refocusMainWindow()
        )

