module HUDs

open OverworldItemGridUI

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let MakeHUDs(cm:CustomComboBoxes.CanvasManager, trackerDungeonMoused:Event<_>, trackerLocationMoused:Event<_>) =
    let appMainCanvas = cm.AppMainCanvas
    // near-mouse HUD
    let mutable nearMouseHUDChromeWindow = null : Window
    let makeOverlayWindow(_isRightClick) = // todo: save location/size/position/stayfade, use right click to reset defaults
        if nearMouseHUDChromeWindow <> null then
            nearMouseHUDChromeWindow.Close() // only one at a time
        nearMouseHUDChromeWindow <- new Window(Title="Z-Tracker near-mouse HUD controls", ResizeMode=ResizeMode.CanMinimize, SizeToContent=SizeToContent.WidthAndHeight, 
                                                WindowStartupLocation=WindowStartupLocation.CenterOwner,
                                                Owner=Application.Current.MainWindow, Background=Brushes.Black)
        let mutable lastMouse = DateTime.Now
        let mutable maxOpacity = 1.0
        let mutable oW, oH, oStay, oFade = ref 250., ref 250., ref 1000., ref 1300.

        // controls layout
        let mkTxt(txt) = new TextBox(FontSize=16., Foreground=Brushes.Lime, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), Text=txt)
        let mutable doUpdate = fun () -> ()
        let edit(tb:TextBox, label:TextBox, least, update:float ref) = 
            tb.BorderThickness <- Thickness(1.)
            tb.IsReadOnly <- false
            tb.IsHitTestVisible <- true
            tb.Width <- 80.
            tb.TextChanged.Add(fun _ ->
                try
                    let r = float tb.Text
                    if r >= least then
                        label.Foreground <- Brushes.Lime
                        update := r
                        doUpdate()
                    else
                        label.Foreground <- Brushes.Red
                with _ -> 
                    label.Foreground <- Brushes.Red
                )
        let sp = new StackPanel(Orientation=Orientation.Vertical)
        let topButtons = new DockPanel(LastChildFill=false)
        let topLeftButton = new Button(Content=mkTxt("Put HUD above"))
        topButtons.Children.Add(topLeftButton) |> ignore
        DockPanel.SetDock(topLeftButton, Dock.Left)
        let topRightButton = new Button(Content=mkTxt("Put HUD above"))
        topButtons.Children.Add(topRightButton) |> ignore
        DockPanel.SetDock(topRightButton, Dock.Right)
        sp.Children.Add(topButtons) |> ignore
        let spacer() = sp.Children.Add(new DockPanel(Height=10.)) |> ignore
        spacer()
        sp.Children.Add(mkTxt("Move this control window to position the HUD.\n(Click corner buttons if needed.)\nYou can minimize this control window once\nthe HUD is positioned where you want it.")) |> ignore
        spacer()
        let spWH = new StackPanel(Orientation=Orientation.Horizontal)
        let size = mkTxt("HUD Size -- ")
        let widthLabel = mkTxt("Width:")
        let widthInput = mkTxt((!oW).ToString())
        let heightLabel = mkTxt("   Height:")
        let heightInput = mkTxt((!oH).ToString())
        edit(widthInput, widthLabel, 20., oW)
        edit(heightInput, heightLabel, 20., oH)
        spWH.Children.Add(size) |> ignore
        spWH.Children.Add(widthLabel) |> ignore
        spWH.Children.Add(widthInput) |> ignore
        spWH.Children.Add(heightLabel) |> ignore
        spWH.Children.Add(heightInput) |> ignore
        sp.Children.Add(spWH) |> ignore
        spacer()
        let opacityLabel = mkTxt("Max Opacity:")
        sp.Children.Add(opacityLabel) |> ignore
        let slider = new Slider(Orientation=Orientation.Horizontal, Maximum=100., TickFrequency=10., TickPlacement=Primitives.TickPlacement.Both, IsSnapToTickEnabled=false, Width=400.)
        slider.Value <- maxOpacity * slider.Maximum
        slider.ValueChanged.Add(fun _ -> lastMouse <- DateTime.Now; maxOpacity <- slider.Value / 100.)
        sp.Children.Add(slider) |> ignore
        spacer()
        sp.Children.Add(mkTxt("After each mouse move in the tracker,\nHUD will stay at Max Opacity for 'Stay'ms, then\nfade out completely after 'Fade'ms.\nFor 'always on' mode, set 'Fade' to 0.")) |> ignore
        spacer()
        let spSF = new StackPanel(Orientation=Orientation.Horizontal)
        let stayLabel = mkTxt("Stay(ms):")
        let stayInput = mkTxt((!oStay).ToString())
        let fadeLabel = mkTxt("   Fade(ms):")
        let fadeInput = mkTxt((!oFade).ToString())
        edit(stayInput, stayLabel, 0., oStay)
        edit(fadeInput, fadeLabel, 0., oFade)
        spSF.Children.Add(stayLabel) |> ignore
        spSF.Children.Add(stayInput) |> ignore
        spSF.Children.Add(fadeLabel) |> ignore
        spSF.Children.Add(fadeInput) |> ignore
        sp.Children.Add(spSF) |> ignore
        spacer()
        let bottomButtons = new DockPanel(LastChildFill=false)
        let bottomLeftButton = new Button(Content=mkTxt("Put HUD below"))
        (bottomLeftButton.Content :?> TextBox).Background <- Brushes.Green
        bottomButtons.Children.Add(bottomLeftButton) |> ignore
        DockPanel.SetDock(bottomLeftButton, Dock.Left)
        let bottomRightButton = new Button(Content=mkTxt("Put HUD below"))
        bottomButtons.Children.Add(bottomRightButton) |> ignore
        DockPanel.SetDock(bottomRightButton, Dock.Right)
        sp.Children.Add(bottomButtons) |> ignore

        nearMouseHUDChromeWindow.Content <- new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(2.), Child=sp)
        nearMouseHUDChromeWindow.Show()
        
        let W = appMainCanvas.Width
        let nearMouseHUDWindow = new Window(Title="Z-Tracker near-mouse HUD", ResizeMode=ResizeMode.NoResize, SizeToContent=SizeToContent.Manual, Owner=Application.Current.MainWindow, 
                                        Background=Brushes.Black, WindowStyle=WindowStyle.None, AllowsTransparency=true, Opacity=maxOpacity, Topmost=true)
        nearMouseHUDWindow.WindowStartupLocation <- WindowStartupLocation.Manual
        nearMouseHUDWindow.Left <- 0.0
        nearMouseHUDWindow.Top <- 0.0

        let makeViewRect(upperLeft:Point, lowerRight:Point) =
            let vb = new VisualBrush(cm.RootCanvas)
            vb.ViewboxUnits <- BrushMappingMode.Absolute
            vb.Viewbox <- Rect(upperLeft, lowerRight)
            vb.Stretch <- Stretch.None
            let bwRect = new Shapes.Rectangle(Width=vb.Viewbox.Width, Height=vb.Viewbox.Height)
            bwRect.Fill <- vb
            bwRect

        let c = new Canvas()
        let wholeView = makeViewRect(Point(0.,0.), Point(W,appMainCanvas.Height))
        canvasAdd(c, wholeView, 0., 0.)

        let addFakeMouse(c:Canvas) =
            let fakeMouse = new Shapes.Polygon(Fill=Brushes.White)
            fakeMouse.Points <- new PointCollection([Point(0.,0.); Point(12.,6.); Point(6.,12.)])
            c.Children.Add(fakeMouse) |> ignore
            fakeMouse
        let fakeMouse = addFakeMouse(c)
        doUpdate <- fun () ->
            nearMouseHUDWindow.Width <- !oW
            nearMouseHUDWindow.Height <- !oH
            Canvas.SetLeft(fakeMouse, !oW / 2.)
            Canvas.SetTop(fakeMouse, !oH / 2.)
        doUpdate()

        let dp = new DockPanel(Width=W)
        dp.UseLayoutRounding <- true
        dp.Children.Add(c) |> ignore
        let borderForLocationMoveSetup = new Border(BorderBrush=Brushes.Transparent, BorderThickness=Thickness(1.), Child=dp)
        nearMouseHUDWindow.Content <- borderForLocationMoveSetup
    
        cm.RootCanvas.MouseMove.Add(fun ea ->   // we need RootCanvas to see mouse moving in popups
            let mousePos = ea.GetPosition(appMainCanvas)
            Canvas.SetLeft(wholeView, (!oW / 2.) - mousePos.X)
            Canvas.SetTop(wholeView, (!oH / 2.) - mousePos.Y)
            lastMouse <- DateTime.Now
            )

        let mutable which = 0  // 0 = bottom left, 1 = bottom right, 2 = top left, 3 = top right
        let moveWindow() =
            if which = 0 then
                nearMouseHUDWindow.Left <- nearMouseHUDChromeWindow.Left + 8.
                nearMouseHUDWindow.Top <- nearMouseHUDChromeWindow.Top + nearMouseHUDChromeWindow.Height
            elif which = 1 then
                nearMouseHUDWindow.Left <- nearMouseHUDChromeWindow.Left - 8. + nearMouseHUDChromeWindow.Width - nearMouseHUDWindow.Width
                nearMouseHUDWindow.Top <- nearMouseHUDChromeWindow.Top + nearMouseHUDChromeWindow.Height
            elif which = 2 then
                nearMouseHUDWindow.Left <- nearMouseHUDChromeWindow.Left + 8.
                nearMouseHUDWindow.Top <- nearMouseHUDChromeWindow.Top - 8. - nearMouseHUDWindow.Height
            else
                nearMouseHUDWindow.Left <- nearMouseHUDChromeWindow.Left - 8. + nearMouseHUDChromeWindow.Width - nearMouseHUDWindow.Width
                nearMouseHUDWindow.Top <- nearMouseHUDChromeWindow.Top - 8. - nearMouseHUDWindow.Height
            borderForLocationMoveSetup.BorderBrush <- Brushes.Gray
            lastMouse <- DateTime.Now
        let all = [bottomLeftButton; bottomRightButton; topLeftButton; topRightButton]
        for n,b in [0,bottomLeftButton; 1,bottomRightButton; 2,topLeftButton; 3,topRightButton] do
            b.Click.Add(fun _ ->
                which <- n
                for x in all do 
                    (x.Content :?> TextBox).Background <- Brushes.Black
                (b.Content :?> TextBox).Background <- Brushes.Green
                moveWindow()
                )
        nearMouseHUDChromeWindow.LocationChanged.Add(fun _ea ->
            //if overlayChromeWindow.WindowState = WindowState.Normal then  // dont update when Minimized
            if nearMouseHUDChromeWindow.Left <> -32000. then  // dont update when Minimized
                moveWindow()
            )
        nearMouseHUDChromeWindow.Closed.Add(fun _ -> nearMouseHUDWindow.Close())

        let timer = new System.Windows.Threading.DispatcherTimer()
        timer.Interval <- TimeSpan.FromMilliseconds(100.0)
        timer.Tick.Add(fun _ -> 
            if !oFade = 0.0 then
                nearMouseHUDWindow.Opacity <- maxOpacity
            else
                let diffms = (DateTime.Now - lastMouse).TotalMilliseconds |> float
                if diffms < !oStay then
                    nearMouseHUDWindow.Opacity <- maxOpacity
                elif diffms < !oFade then
                    let pct = (diffms - !oStay) / (!oFade - !oStay)
                    nearMouseHUDWindow.Opacity <- maxOpacity * (1.0 - pct)
                else
                    nearMouseHUDWindow.Opacity <- 0.
            borderForLocationMoveSetup.BorderBrush <- Brushes.Transparent
            )
        timer.Start()

        nearMouseHUDWindow.Show()
        moveWindow()
#if NOT_RACE_LEGAL
    nearMouseHUD <- fun (isRightClick) -> makeOverlayWindow(isRightClick)
#endif

    let mutable minimapOverlayWindow = null : Window
    let makeMinimapOverlay(isRightClick) =
        if minimapOverlayWindow <> null then
            minimapOverlayWindow.Close() // only one at a time
        minimapOverlayWindow <- new Window(Title="Z-Tracker minimap overlay", ResizeMode=ResizeMode.NoResize, SizeToContent=SizeToContent.Manual,
                                                WindowStartupLocation=WindowStartupLocation.Manual, Owner=Application.Current.MainWindow,
                                                Background=Brushes.Transparent, WindowStyle=WindowStyle.None, AllowsTransparency=true,
                                                Opacity=1.0, Topmost=true)
        let init() =
            let W, H = minimapOverlayWindow.Width, minimapOverlayWindow.Height
            let entireCanvas = new Canvas(Width=W, Height=H)
            let minimapCanvas = new Canvas(Width=W, Height=H)
            let GRIDCOLOR = Brushes.Gray
            let vs = Array.init 9 (fun i ->
                let x = (48.+24.*float(i)) * W / 768.
                new Shapes.Line(Stroke=GRIDCOLOR, StrokeThickness=1.0, X1=x, X2=x, Y1=48.*H/672., Y2=(48.+12.*8.)*H/672.)
                )
            let hs = Array.init 9 (fun j ->
                let y = (48.+12.*float(j)) * H / 672.
                new Shapes.Line(Stroke=GRIDCOLOR, StrokeThickness=1.0, X1=48.*W/768., X2=(48.+24.*8.)*W/768., Y1=y, Y2=y)
                )
            let drect = 
                let c = new Canvas(Width=24.*W/768. - 2., Height=12.*H/672. - 2.)
                let b = new Border(BorderBrush=Brushes.White, BorderThickness=Thickness(3.), Child=c, Opacity=0.0)
                b
            let orect = 
                let c = new Canvas(Width=12.*W/768. - 2., Height=12.*H/672. - 2.)
                let b = new Border(BorderBrush=Brushes.White, BorderThickness=Thickness(3.), Child=c, Opacity=0.0)
                b
            let oLegendRect = 
                let c = new Canvas(Width=12.*W/768. - 2., Height=12.*H/672. - 2.)
                let b = new Border(BorderBrush=Brushes.White, BorderThickness=Thickness(3.), Child=c, Opacity=1.0, Height=12.*H/672. - 2. + 6.)
                b
            let mkTxt(txt) = new TextBox(FontSize=oLegendRect.Height, Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), Text=txt)
            let oText1 = mkTxt("Z-Tracker mouse ")
            let oText2 = mkTxt(" at H16")
            let oCaption = new StackPanel(Orientation=Orientation.Horizontal, Opacity=0.0)
            oCaption.Children.Add(oText1) |> ignore
            oCaption.Children.Add(oLegendRect) |> ignore
            oCaption.Children.Add(oText2) |> ignore
            canvasAdd(minimapCanvas, oCaption, 48.*W/768., 144.*H/672. + 2.)
            for i = 0 to 8 do
                canvasAdd(minimapCanvas, vs.[i], 0., 0.)
                canvasAdd(minimapCanvas, hs.[i], 0., 0.)
            canvasAdd(minimapCanvas, drect, 0., 0.)
            canvasAdd(minimapCanvas, orect, 0., 0.)
            trackerLocationMoused.Publish.Add(fun (tl,i,j) ->
                if i = -1 then
                    minimapCanvas.Opacity <- 0.0
                    drect.Opacity <- 0.0
                    orect.Opacity <- 0.0
                    oCaption.Opacity <- 0.0
                else
                    match tl with
                    | DungeonUI.TrackerLocation.DUNGEON ->
                        minimapCanvas.Opacity <- 1.0
                        drect.Opacity <- 1.0
                        orect.Opacity <- 0.0
                        oCaption.Opacity <- 0.0
                        Canvas.SetLeft(drect, (48.+24.*float(i)) * W / 768. - 2.)
                        Canvas.SetTop(drect, (48.+12.*float(j)) * H / 672. - 2.)
                    | DungeonUI.TrackerLocation.OVERWORLD ->
                        minimapCanvas.Opacity <- 1.0
                        drect.Opacity <- 0.0
                        orect.Opacity <- 1.0
                        oCaption.Opacity <- 1.0
                        let i = if displayIsCurrentlyMirrored then 16-i else i+1
                        oText2.Text <- sprintf " at %c%d" ("ABCDEFGH".[j]) i
                        Canvas.SetLeft(orect, (48.+12.*float(i-1)) * W / 768. - 2.)
                        Canvas.SetTop(orect, (48.+12.*float(j)) * H / 672. - 2.)
                )
            let pauseScreenMapCanvas = new Canvas(Width=W, Height=H, Opacity=0.0)
            let left = 384. * W / 768.
            let top = 285. * H / 672.
            let width = 8. * 24. * W / 768.
            let height = ((8. * 24.) - 6.) * H / 672.
            let heightWithHeader = height * float(TH + 27*8 + 12*7) / float(27*8 + 12*7)
            let topWithHeader = top - (heightWithHeader - height)
            let rect = new Shapes.Rectangle(Width=width, Height=heightWithHeader, Stroke=Brushes.Gray, StrokeThickness=1.)
            canvasAdd(pauseScreenMapCanvas, rect, left, topWithHeader)
            trackerDungeonMoused.Publish.Add(fun (vb:VisualBrush) ->
                if vb = null then
                    pauseScreenMapCanvas.Opacity <- 0.0
                else
                    rect.Fill <- vb
                    pauseScreenMapCanvas.Opacity <- 0.6
                )
            //c.UseLayoutRounding <- true
            //let outerBorder = new Border(BorderBrush=Brushes.Lime, BorderThickness=Thickness(1.), Child=c, Opacity=1.0)  // for help debugging
            //overlayLocatorWindow.Content <- outerBorder
            canvasAdd(entireCanvas, minimapCanvas, 0., 0.)
            canvasAdd(entireCanvas, pauseScreenMapCanvas, 0., 0.)
            minimapOverlayWindow.Content <- entireCanvas
        // 768   48 72 ...
        // 672   48 60 ...

        let sizerWindow = new Window()
        sizerWindow.Title <- "Z-Tracker sizer"
        sizerWindow.ResizeMode <- ResizeMode.CanResize
        sizerWindow.SizeToContent <- SizeToContent.Manual
        let save() = 
            TrackerModelOptions.OverlayLocatorWindowLTWH <- sprintf "%d,%d,%d,%d" (int sizerWindow.Left) (int sizerWindow.Top) (int sizerWindow.Width) (int sizerWindow.Height)
            TrackerModelOptions.writeSettings()
        let leftTopWidthHeight = TrackerModelOptions.OverlayLocatorWindowLTWH
        let matches = System.Text.RegularExpressions.Regex.Match(leftTopWidthHeight, """^(-?\d+),(-?\d+),(\d+),(\d+)$""")
        if not isRightClick && matches.Success then
            sizerWindow.Left <- float matches.Groups.[1].Value
            sizerWindow.Top <- float matches.Groups.[2].Value
            sizerWindow.Width <- float matches.Groups.[3].Value
            sizerWindow.Height <- float matches.Groups.[4].Value
            sizerWindow.WindowStartupLocation <- WindowStartupLocation.Manual
        else
            sizerWindow.Width <- 500.
            sizerWindow.Height <- 500.
            sizerWindow.WindowStartupLocation <- WindowStartupLocation.CenterOwner
        sizerWindow.Owner <- Application.Current.MainWindow
        sizerWindow.Background <- Brushes.Black
        sizerWindow.WindowStyle <- WindowStyle.SingleBorderWindow
        let dp = new DockPanel(Opacity=0.0, LastChildFill=true)
        let sp = new StackPanel(Orientation=Orientation.Vertical)
        sp.Children.Add(new TextBox(Text="Resize this window so that it exactly covers\nthe NES game screen, then click the button below", TextAlignment=TextAlignment.Center)) |> ignore
        let button = new Button(Content=new TextBox(Text="Click here after sizing"), Width=300., HorizontalAlignment=HorizontalAlignment.Center)
        button.Click.Add(fun _ -> 
            minimapOverlayWindow.Left <- sizerWindow.Left + 8.
            minimapOverlayWindow.Top<- sizerWindow.Top
            minimapOverlayWindow.Width <- sizerWindow.Width - 16.
            minimapOverlayWindow.Height <- sizerWindow.Height - 8.
            init()
            minimapOverlayWindow.Show()
            dp.Opacity <- 1.0
            save()
            sizerWindow.Close()
            )
        sp.Children.Add(button) |> ignore
        sp.Children.Add(new TextBox(Text="You can make gross adjustments to window size\nby grabbing the window corner, like any other window.", FontSize=12., TextAlignment=TextAlignment.Center)) |> ignore
        sp.Children.Add(new TextBox(Text="To fine-tune the window size, you can use the buttons\nbelow to adjust one pixel at a time.", FontSize=12., TextAlignment=TextAlignment.Center)) |> ignore
        let nudgeCanvas = new Canvas(Width=260., Height=260., HorizontalAlignment=HorizontalAlignment.Center)
        let r = new Shapes.Rectangle(Width=150., Height=150., Stroke=Brushes.White, StrokeThickness=3.)
        canvasAdd(nudgeCanvas, r, 55., 55.)
        let leftLarger = new Button(Content=new TextBox(Text="◄"), Width=40., Height=40.)
        canvasAdd(nudgeCanvas, leftLarger, 10., 110.)
        let leftSmaller = new Button(Content=new TextBox(Text="►"), Width=40., Height=40.)
        canvasAdd(nudgeCanvas, leftSmaller, 60., 110.)
        let rightSmaller = new Button(Content=new TextBox(Text="◄"), Width=40., Height=40.)
        canvasAdd(nudgeCanvas, rightSmaller, 160., 110.)
        let rightLarger = new Button(Content=new TextBox(Text="►"), Width=40., Height=40.)
        canvasAdd(nudgeCanvas, rightLarger, 210., 110.)
        let topLarger = new Button(Content=new TextBox(Text="▲"), Width=40., Height=40.)
        canvasAdd(nudgeCanvas, topLarger, 110., 10.)
        let topSmaller = new Button(Content=new TextBox(Text="▼"), Width=40., Height=40.)
        canvasAdd(nudgeCanvas, topSmaller, 110., 60.)
        let bottomSmaller = new Button(Content=new TextBox(Text="▲"), Width=40., Height=40.)
        canvasAdd(nudgeCanvas, bottomSmaller, 110., 160.)
        let bottomLarger = new Button(Content=new TextBox(Text="▼"), Width=40., Height=40.)
        canvasAdd(nudgeCanvas, bottomLarger, 110., 210.)
        let left(delta) = sizerWindow.Left <- sizerWindow.Left + delta
        let width(delta) = sizerWindow.Width <- sizerWindow.Width + delta
        let top(delta) = sizerWindow.Top <- sizerWindow.Top + delta
        let height(delta) = sizerWindow.Height <- sizerWindow.Height + delta
        leftLarger.Click.Add(fun _ -> left(-1.); width(1.))
        leftSmaller.Click.Add(fun _ -> left(1.); width(-1.))
        rightSmaller.Click.Add(fun _ -> width(-1.))
        rightLarger.Click.Add(fun _ -> width(1.))
        topLarger.Click.Add(fun _ -> top(-1.); height(1.))
        topSmaller.Click.Add(fun _ -> top(1.); height(-1.))
        bottomSmaller.Click.Add(fun _ -> height(-1.))
        bottomLarger.Click.Add(fun _ -> height(1.))
        sp.Children.Add(nudgeCanvas) |> ignore
        let outerBorder = new Border(BorderBrush=Brushes.White, BorderThickness=Thickness(2.,0.,2.,2.), Child=sp, Opacity=1.0)
        let style = new Style(typeof<TextBox>)
        style.Setters.Add(new Setter(TextBox.FontSizeProperty, 16.))
        style.Setters.Add(new Setter(TextBox.IsReadOnlyProperty, true))
        style.Setters.Add(new Setter(TextBox.IsHitTestVisibleProperty, false))
        outerBorder.Resources.Add(typeof<TextBox>, style)
        sizerWindow.Content <- outerBorder
        sizerWindow.Show()
#if NOT_RACE_LEGAL
    minimapOverlay <- fun (isRightClick) -> makeMinimapOverlay(isRightClick)
#endif    

    ignore makeMinimapOverlay
    ignore makeOverlayWindow
    ()

