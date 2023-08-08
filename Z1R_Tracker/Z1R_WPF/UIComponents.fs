module UIComponents

open OverworldItemGridUI
open DungeonUI.AhhGlobalVariables
open HotKeys.MyKey
open CustomComboBoxes.GlobalFlag

open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let UNICODE_UP = "\U0001F845"
let UNICODE_LEFT = "\U0001F844"
let UNICODE_DOWN = "\U0001F847"
let UNICODE_RIGHT = "\U0001F846"
let arrowColor = Graphics.freeze(new SolidColorBrush(Color.FromArgb(255uy,0uy,180uy,250uy)))
let bgColor = Graphics.freeze(new SolidColorBrush(Color.FromArgb(220uy,0uy,0uy,0uy)))

let MakeMagnifier(mirrorOverworldFEs:ResizeArray<FrameworkElement>, owMapNum, owMapBMPs:System.Drawing.Bitmap[,]) =
    // nearby ow tiles magnified overlay
    let ENLARGE = 8.
    let POP = 1  // width of entrance border
    let BT = 2.  // border thickness of the interior 3x3 grid of tiles
    let dungeonTabsOverlay = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(5.), Background=Brushes.Black, Opacity=0., IsHitTestVisible=false)
    let DTOCW,DTOCH = 3.*16.*ENLARGE + 4.*BT, 3.*11.*ENLARGE + 4.*BT
    let dungeonTabsOverlayContent = new Canvas(Width=DTOCW, Height=DTOCH)
    if owMapNum=4 then  // disable magnifier on blank/custom map
        let onMouseForMagnifier(_,_) = ()
        onMouseForMagnifier, dungeonTabsOverlay, dungeonTabsOverlayContent
    else
    mirrorOverworldFEs.Add(dungeonTabsOverlayContent)
    let dtocPlusLegend = new StackPanel(Orientation=Orientation.Vertical)
    dtocPlusLegend.Children.Add(dungeonTabsOverlayContent) |> ignore
    let dtocLegend = new StackPanel(Orientation=Orientation.Horizontal, Background=Graphics.almostBlack)
    for outer,inner,desc in [Brushes.Cyan, Brushes.Black, "open cave"
                             Brushes.Black, Brushes.Cyan, "bomb spot"
                             Brushes.Black, Brushes.Red, "burn spot"
                             Brushes.Black, Brushes.Yellow, "recorder spot"
                             Brushes.Black, Brushes.Magenta, "pushable spot"] do
        let black = new Canvas(Width=ENLARGE + 2.*(float POP + 1.), Height=ENLARGE + 2.*(float POP + 1.), Background=Brushes.Black)
        let outer = new Canvas(Width=ENLARGE + 2.*(float POP), Height=ENLARGE + 2.*(float POP), Background=outer)
        let inner = new Canvas(Width=ENLARGE, Height=ENLARGE, Background=inner)
        canvasAdd(black, outer, 1., 1.)
        canvasAdd(black, inner, 1.+float POP, 1.+float POP)
        dtocLegend.Children.Add(black) |> ignore
        let text = new TextBox(Text=desc, Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                                    FontSize=12., HorizontalContentAlignment=HorizontalAlignment.Center)
        dtocLegend.Children.Add(text) |> ignore
    dtocPlusLegend.Children.Add(dtocLegend) |> ignore
    dungeonTabsOverlay.Child <- dtocPlusLegend
    let overlayTiles = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let bmp = 
                let magnifierFilename = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, sprintf """Magnifier\quest.%d.ow.%2d.%2d.bmp""" owMapNum i j)
                Graphics.readCacheFileOrCreateBmp(magnifierFilename, fun () ->
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
                                            elif owMapNum=1 && i=11 && j=0 then // second quest has fairy here, borrow 2,4
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
                    // make the entrances 'pop'
                    // No 'entrance pixels' are on the edge of a tile, and we would be drawing outside bitmap array bounds if they were, so only iterate over interior pixels:
                    for x = 1 to 14 do
                        for y = 1 to 9 do
                            let c = owMapBMPs.[i,j].GetPixel(x*3, y*3)
                            let border = 
                                if c.ToArgb() = System.Drawing.Color.Black.ToArgb() then    // black open cave
                                    let c2 = owMapBMPs.[i,j].GetPixel((x-1)*3, y*3)
                                    if c2.ToArgb() = System.Drawing.Color.Black.ToArgb() then    // also black to the left, this is vanilla 6 two-wide entrance, only show one
                                        None
                                    else
                                        Some(System.Drawing.Color.FromArgb(0xFF,0x00,0xCC,0xCC))
                                elif c.ToArgb() = System.Drawing.Color.FromArgb(0xFF,0x00,0xFF,0xFF).ToArgb() then  // cyan bomb spot
                                    Some(System.Drawing.Color.FromArgb(0xFF,0x00,0x00,0x00))
                                elif c.ToArgb() = System.Drawing.Color.FromArgb(0xFF,0xFF,0xFF,0x00).ToArgb() then  // yellow recorder spot
                                    Some(System.Drawing.Color.FromArgb(0xFF,0x00,0x00,0x00))
                                elif c.ToArgb() = System.Drawing.Color.FromArgb(0xFF,0xFF,0x00,0x00).ToArgb() then  // red burn spot
                                    Some(System.Drawing.Color.FromArgb(0xFF,0x00,0x00,0x00))
                                elif c.ToArgb() = System.Drawing.Color.FromArgb(0xFF,0xFF,0x00,0xFF).ToArgb() then  // magenta pushblock spot
                                    Some(System.Drawing.Color.FromArgb(0xFF,0x00,0x00,0x00))
                                else
                                    None
                            match border with
                            | Some bc -> 
                                // thin black outline
                                for px = x*int ENLARGE - POP - 1 to (x+1)*int ENLARGE - 1 + POP + 1 do
                                    for py = y*int ENLARGE - POP - 1 to (y+1)*int ENLARGE - 1 + POP + 1 do
                                        bmp.SetPixel(px, py, System.Drawing.Color.Black)
                                // border color
                                for px = x*int ENLARGE - POP to (x+1)*int ENLARGE - 1 + POP do
                                    for py = y*int ENLARGE - POP to (y+1)*int ENLARGE - 1 + POP do
                                        bmp.SetPixel(px, py, bc)
                                // inner actual pixel
                                for px = x*int ENLARGE to (x+1)*int ENLARGE - 1 do
                                    for py = y*int ENLARGE to (y+1)*int ENLARGE - 1 do
                                        bmp.SetPixel(px, py, c)
                            | None -> ()
                    bmp)
            overlayTiles.[i,j] <- Graphics.BMPtoImage bmp
    let makeArrow(text) = 
        let tb = new TextBox(Text=text, FontSize=20., Foreground=arrowColor, Background=bgColor, IsReadOnly=true, BorderThickness=Thickness(0.))
        tb.Clip <- new RectangleGeometry(Rect(0., 6., 30., 18.))
        tb
    let onMouseForMagnifier(i,j) = 
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
                let dx = BT+float x*(16.*ENLARGE+BT)
                let dy = BT+float y*(11.*ENLARGE+BT)
                canvasAdd(dungeonTabsOverlayContent, overlayTiles.[xmin+x,ymin+y], dx, dy)
                if xmin+x = 1 && ymin+y = 6 then // Lost Woods
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_UP),   dx+3., dy-2.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_LEFT), dx+3., dy+18.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_DOWN), dx+3., dy+38.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_LEFT), dx+3., dy+58.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_RIGHT),dx+101., dy+28.)
                if xmin+x = 11 && ymin+y = 1 then // Lost Hills
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_UP),   dx+20., dy-2.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_UP),   dx+40., dy-2.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_UP),   dx+60., dy-2.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_UP),   dx+80., dy-2.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_LEFT), dx+3.,  dy+28.)
        if TrackerModelOptions.Overworld.ShowMagnifier.Value then 
            dungeonTabsOverlay.Opacity <- 1.0

    onMouseForMagnifier, dungeonTabsOverlay, dungeonTabsOverlayContent

let recorderEllipseNewDungeonsColor = Graphics.freeze(new SolidColorBrush(Color.FromRgb(220uy,220uy,220uy)))
let recorderEllipseVanillaColor = Brushes.White
let RecorderEllipseColor() = 
    if TrackerModel.recorderToNewDungeons then
        recorderEllipseNewDungeonsColor
    else
        recorderEllipseVanillaColor
let MakeLegend(cm:CustomComboBoxes.CanvasManager, doUIUpdateEvent:Event<unit>) =
    let makeStartIcon() = 
        let back = new Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=Brushes.DarkViolet, StrokeThickness=3.0, IsHitTestVisible=false)
        //back.StrokeDashArray <- new DoubleCollection([5.; 4.8; 9.7; 4.8; 5.])
        //let back = new Shapes.Rectangle(Width=23., Height=23., Stroke=Brushes.DarkViolet, StrokeThickness=3.0, IsHitTestVisible=false, RenderTransform=DungeonUI.fortyFiveDegrees)
        back.Effect <- new Effects.BlurEffect(Radius=7.0, KernelType=Effects.KernelType.Gaussian)
        let front = new Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=Brushes.Lime, StrokeThickness=3.0, IsHitTestVisible=false)
        //front.StrokeDashArray <- new DoubleCollection([5.; 4.8; 9.7; 4.8; 5.])
        //let front = new Shapes.Rectangle(Width=23., Height=23., Stroke=Brushes.Lime, StrokeThickness=3.0, IsHitTestVisible=false, RenderTransform=DungeonUI.fortyFiveDegrees)
        let c = new Canvas(Width=front.Width, Height=front.Height)
        if Graphics.canUseEffectsWithoutDestroyingPerformance then
            canvasAdd(c, back, 0., 0.)
            //canvasAdd(c, back, 15., 0.)
        canvasAdd(c, front, 0., 0.)
        //canvasAdd(c, front, 15., 0.)
        c
    let startIcon = makeStartIcon()
    let makeCustomWaypointIcon, theCustomWaypointIcon =
        let L = 6.0
        let makeIcon() = 
            let bg = new Shapes.Ellipse(Width=float(11*3)-2.+3.0*L, Height=float(11*3)-2.+2.*L, Stroke=Brushes.Black, StrokeThickness=3.0, IsHitTestVisible=false)
            bg.Effect <- new Effects.BlurEffect(Radius=5.0, KernelType=Effects.KernelType.Gaussian)
            let fg = new Shapes.Ellipse(Width=float(11*3)-2.+3.0*L, Height=float(11*3)-2.+2.*L, Stroke=Brushes.Orange, StrokeThickness=3.0, IsHitTestVisible=false)
            let c = new Canvas()
            if Graphics.canUseEffectsWithoutDestroyingPerformance then
                canvasAdd(c, bg, 1., 0.)
            canvasAdd(c, fg, 1., 0.)
            c
        let theCustomWaypointIcon = makeIcon()
        let animBrush = new LinearGradientBrush(new GradientStopCollection([new GradientStop(Colors.Orange, 0.0); new GradientStop(Colors.Orange, 0.6); new GradientStop(Colors.White, 1.0)]), 
                                                Point(0.5,0.5), Point(1.,0.))
        (theCustomWaypointIcon.Children.Item(theCustomWaypointIcon.Children.Count-1) :?> Shapes.Ellipse).Stroke <- animBrush
        let anim = new Animation.PointAnimationUsingKeyFrames()
        anim.KeyFrames <- new Animation.PointKeyFrameCollection()
        anim.KeyFrames.Add(new Animation.LinearPointKeyFrame(Value=Point(1.0,0.5), KeyTime=Animation.KeyTime.FromPercent(0.0)))   |> ignore
        anim.KeyFrames.Add(new Animation.LinearPointKeyFrame(Value=Point(0.8,0.8), KeyTime=Animation.KeyTime.FromPercent(0.125)))   |> ignore
        anim.KeyFrames.Add(new Animation.LinearPointKeyFrame(Value=Point(0.5,1.0), KeyTime=Animation.KeyTime.FromPercent(0.25)))   |> ignore
        anim.KeyFrames.Add(new Animation.LinearPointKeyFrame(Value=Point(0.2,0.8), KeyTime=Animation.KeyTime.FromPercent(0.375)))   |> ignore
        anim.KeyFrames.Add(new Animation.LinearPointKeyFrame(Value=Point(0.0,0.5), KeyTime=Animation.KeyTime.FromPercent(0.5)))    |> ignore
        anim.KeyFrames.Add(new Animation.LinearPointKeyFrame(Value=Point(0.2,0.2), KeyTime=Animation.KeyTime.FromPercent(0.625)))   |> ignore
        anim.KeyFrames.Add(new Animation.LinearPointKeyFrame(Value=Point(0.5,0.0), KeyTime=Animation.KeyTime.FromPercent(0.75)))  |> ignore
        anim.KeyFrames.Add(new Animation.LinearPointKeyFrame(Value=Point(0.8,0.2), KeyTime=Animation.KeyTime.FromPercent(0.875)))   |> ignore
        anim.KeyFrames.Add(new Animation.LinearPointKeyFrame(Value=Point(1.0,0.5), KeyTime=Animation.KeyTime.FromPercent(1.)))    |> ignore
        anim.Duration <- new Duration(System.TimeSpan.FromSeconds(4.))
        anim.RepeatBehavior <- Animation.RepeatBehavior.Forever
        anim.AutoReverse <- false
        animBrush.BeginAnimation(LinearGradientBrush.EndPointProperty, anim)
        makeIcon, theCustomWaypointIcon

    // map legend
    let BG = Graphics.freeze(new SolidColorBrush(Color.FromArgb(255uy,0uy,0uy,90uy)))
    let legendCanvas = new Canvas(Width=586., Height=float(11*3), Background=BG)
    let dungeonLegendIconCanvas = new Canvas(Width=float(16*3), Height=float(11*3))
    let dungeonLegendIconArea = new Canvas(Width=15., Height=float(11*3), Background=Brushes.White, Opacity=0.0001)   // to respond to mouse hover
    canvasAdd(legendCanvas, dungeonLegendIconCanvas, 162., 0.)
    let recorderDestinationButtonCanvas = new Canvas(Width=OMTW, Height=float(11*3), Background=BG, ClipToBounds=true)
    let recorderDestinationMouseHoverHighlight = new Shapes.Rectangle(Width=OMTW, Height=float(11*3), Stroke=Brushes.DarkCyan, StrokeThickness=1., Opacity=0.)
    //let recorderEllipse = new Shapes.Ellipse(Width=float(11*3)-2.+12.0, Height=float(11*3)-2.+6., Stroke=RecorderEllipseColor(), StrokeThickness=3.0, IsHitTestVisible=false)
    let recorderEllipse = new Shapes.Rectangle(Width=35., Height=35., Stroke=RecorderEllipseColor(), StrokeThickness=3.0, IsHitTestVisible=false, RenderTransform=DungeonUI.fortyFiveDegrees)

    let legendTB = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=BG, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), Text="The LEGEND\nof Z-Tracker:")
    canvasAdd(legendCanvas, legendTB, 0., 0.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=BG, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), Text="Dungeon\n(Completed)")
    canvasAdd(legendCanvas, tb, 197., 0.)
    canvasAdd(legendCanvas, recorderDestinationButtonCanvas, 282., 0.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=BG, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), Text="Recorder\nDestination")
    canvasAdd(legendCanvas, tb, 330., 0.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=BG, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), Text="...")
    let recorderDestinationSettingsButton = new Button(Content=tb)
    canvasAdd(legendCanvas, recorderDestinationSettingsButton, 382., 0.)
    let updateCurrentRecorderDestinationNumeral() =
        dungeonLegendIconCanvas.Children.Clear()
        recorderDestinationButtonCanvas.Children.Clear()
        canvasAdd(recorderDestinationButtonCanvas, recorderEllipse, 23., -8.)
        let yellowDungeonBMP = Graphics.theFullTileBmpTable.[currentRecorderDestinationIndex].[0]
        canvasAdd(dungeonLegendIconCanvas, Graphics.BMPtoImage yellowDungeonBMP, 0., 0.)
        recorderEllipse.Stroke <- RecorderEllipseColor()
        if TrackerModel.recorderToNewDungeons then
            let recorderDestinationLegendIcon = Graphics.BMPtoImage yellowDungeonBMP
            canvasAdd(recorderDestinationButtonCanvas, recorderDestinationLegendIcon, 0., 0.)
        else
            let tb = new TextBox(Text=sprintf "%c" (char currentRecorderDestinationIndex + char '1'), FontSize=12., FontWeight=FontWeights.Bold, Foreground=Brushes.White, Background=Brushes.Black, 
                                    IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.))
            recorderDestinationButtonCanvas.Children.Add(tb) |> ignore
            Canvas.SetLeft(tb, 0.)
            Canvas.SetBottom(tb, 0.)
        recorderDestinationButtonCanvas.Children.Add(recorderDestinationMouseHoverHighlight) |> ignore
        let halfShade = new System.Windows.Shapes.Rectangle(Width=15., Height=14.0, StrokeThickness=0., Fill=System.Windows.Media.Brushes.Black, Opacity=0.4, IsHitTestVisible=false)
        canvasAdd(dungeonLegendIconCanvas, halfShade, 15., 16.)
        canvasAdd(dungeonLegendIconCanvas, dungeonLegendIconArea, 15., 0.)
    updateCurrentRecorderDestinationNumeral()
    recorderDestinationSettingsButton.Click.Add(fun _ ->
        if not popupIsActive then
            popupIsActive <- true
            async {
                let sp = new StackPanel(Orientation=Orientation.Vertical, Margin=Thickness(3.))
                let tb = new TextBox(Text="Recorder settings",IsReadOnly=true,IsHitTestVisible=false,FontSize=16.,BorderThickness=Thickness(0.),Foreground=Brushes.Orange,Background=Brushes.Black)
                sp.Children.Add(tb) |> ignore
                sp.Children.Add(new DockPanel(Background=Brushes.Orange, Margin=Thickness(0.,5.,0.,5.), Height=3.)) |> ignore
                let cb = new CheckBox(Content=new TextBox(Text="Recorder to new dungeons",IsReadOnly=true,IsHitTestVisible=false,FontSize=16.,BorderThickness=Thickness(0.),
                                                            Foreground=Brushes.Orange,Background=Brushes.Black), Height=22.)
                ToolTipService.SetToolTip(cb, "If unchecked, the recorder goes to vanilla first quest dungeon locations")
                cb.IsChecked <- System.Nullable.op_Implicit TrackerModel.recorderToNewDungeons
                cb.Checked.Add(fun _ -> TrackerModel.recorderToNewDungeons <- true; updateCurrentRecorderDestinationNumeral(); doUIUpdateEvent.Trigger())
                cb.Unchecked.Add(fun _ -> TrackerModel.recorderToNewDungeons <- false; updateCurrentRecorderDestinationNumeral(); doUIUpdateEvent.Trigger())
                sp.Children.Add(cb) |> ignore
                let cb = new CheckBox(Content=new TextBox(Text="Recorder to unbeaten dungeons",IsReadOnly=true,IsHitTestVisible=false,FontSize=16.,BorderThickness=Thickness(0.),
                                                            Foreground=Brushes.Orange,Background=Brushes.Black), Height=22.)
                ToolTipService.SetToolTip(cb, "If checked, the recorder goes to dungeons where you do NOT yet have the triforce")
                cb.IsChecked <- System.Nullable.op_Implicit TrackerModel.recorderToUnbeatenDungeons
                cb.Checked.Add(fun _ -> TrackerModel.recorderToUnbeatenDungeons <- true; doUIUpdateEvent.Trigger())
                cb.Unchecked.Add(fun _ -> TrackerModel.recorderToUnbeatenDungeons <- false; doUIUpdateEvent.Trigger())
                sp.Children.Add(cb) |> ignore
                let wh = new System.Threading.ManualResetEvent(false)
                do! CustomComboBoxes.DoModal(cm, wh, 200., 200., new Border(Child=sp, Background=Brushes.Black, BorderThickness=Thickness(6.), BorderBrush=Brushes.Gray))
                popupIsActive <- false
            } |> Async.StartImmediate
        )
    recorderDestinationButtonCanvas.MouseEnter.Add(fun _ -> recorderDestinationMouseHoverHighlight.Opacity <- 1.)
    recorderDestinationButtonCanvas.MouseLeave.Add(fun _ -> recorderDestinationMouseHoverHighlight.Opacity <- 0.)
    recorderDestinationButtonCanvas.MouseDown.Add(fun ea ->
        let delta = 
            if ea.ChangedButton = Input.MouseButton.Left then
                1
            elif ea.ChangedButton = Input.MouseButton.Right then
                -1
            else 
                0
        currentRecorderDestinationIndex <- (currentRecorderDestinationIndex + 8 + delta) % 8
        updateCurrentRecorderDestinationNumeral()
        ea.Handled <- true
        )
    (*   // This screwed me up multiple times, and the fact that it's not user-rebindable is bad.
    recorderDestinationButtonCanvas.MyKeyAdd(fun ea ->
        let _skm,k = ea.Key
        if k >= Input.Key.D1 && k <= Input.Key.D8 then
            currentRecorderDestinationIndex <- (int k) - (int Input.Key.D1)
            updateCurrentRecorderDestinationNumeral()
            ea.Handled <- true
        if k >= Input.Key.NumPad1 && k <= Input.Key.NumPad8 then
            currentRecorderDestinationIndex <- (int k) - (int Input.Key.NumPad1)
            updateCurrentRecorderDestinationNumeral()
            ea.Handled <- true
        )
    *)

    let anyRoadLegendIcon = Graphics.BMPtoImage(Graphics.theFullTileBmpTable.[9].[0])
    canvasAdd(legendCanvas, anyRoadLegendIcon, 69., 0.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=BG, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Any Road\n(Warp)")
    canvasAdd(legendCanvas, tb, 102., 0.)

    let legendStartIconButtonCanvas = new Canvas(Background=BG, Width=OMTW*1.45, Height=11.*3.)
    let legendStartIcon = makeStartIcon()
    canvasAdd(legendStartIconButtonCanvas, legendStartIcon, 0.+4.*OMTW/48., 0.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=BG, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Start\nSpot", IsHitTestVisible=false)
    canvasAdd(legendStartIconButtonCanvas, tb, 0.8*OMTW, 0.)
    let legendStartIconButton = new Button(Content=legendStartIconButtonCanvas)
    canvasAdd(legendCanvas, legendStartIconButton, 514., 0.)
    let makeIconButtonBehavior(prefix, name, makeIcon, iconOffsetX, iconOffsetY, setIconXY) = (fun () ->
        if not popupIsActive then
            popupIsActive <- true
            let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, FontSize=16., Padding=Thickness(3.),
                                    Text=sprintf "%sLeft-Click an overworld map tile to move the %s icon there, or\nRight-click to remove it from the map, or\nClick anywhere outside the map to cancel and make no changes" prefix name)
            let element = new Canvas(Width=OMTW*16., Height=float(8*11*3), Background=Brushes.Transparent, IsHitTestVisible=true)
            element.Children.Add(tb) |> ignore
            Canvas.SetBottom(tb, element.Height)
            let hoverIcon = makeIcon()
            element.MouseLeave.Add(fun _ -> element.Children.Remove(hoverIcon))
            element.MouseMove.Add(fun ea ->
                let mousePos = ea.GetPosition(element)
                let i = int(mousePos.X / OMTW)
                let j = int(mousePos.Y / (11.*3.))
                element.Children.Remove(hoverIcon)
                canvasAdd(element, hoverIcon, float i*OMTW + iconOffsetX, float(j*11*3) + iconOffsetY)
                )
            let wh = new System.Threading.ManualResetEvent(false)
            element.MouseDown.Add(fun ea ->
                if ea.ButtonState = Input.MouseButtonState.Pressed && (ea.ChangedButton = Input.MouseButton.Right) then
                    setIconXY(-1, -1)
                    doUIUpdateEvent.Trigger()
                    wh.Set() |> ignore
                else
                    let mousePos = ea.GetPosition(element)
                    let i = int(mousePos.X / OMTW)
                    let j = int(mousePos.Y / (11.*3.))
                    if i>=0 && i<=15 && j>=0 && j<=7 then
                        setIconXY((if displayIsCurrentlyMirrored then (15-i) else i), j)
                        doUIUpdateEvent.Trigger()
                        wh.Set() |> ignore
                )
            element.MyKeyAdd(fun ea ->
                let mousePos = Input.Mouse.GetPosition(element)
                let i = int(mousePos.X / OMTW)
                let j = int(mousePos.Y / (11.*3.))
                if i>=0 && i<=15 && j>=0 && j<=7 then
                    match HotKeys.GlobalHotKeyProcessor.TryGetValue(ea.Key) with
                    | Some(HotKeys.GlobalHotkeyTargets.MoveCursorRight) -> 
                        ea.Handled <- true
                        if i<15 then Graphics.NavigationallyWarpMouseCursorTo(element.TranslatePoint(Point(float(i+1)*OMTW+OMTW/2., float(j*11*3+11*3/2)), cm.AppMainCanvas))
                    | Some(HotKeys.GlobalHotkeyTargets.MoveCursorLeft) -> 
                        ea.Handled <- true
                        if i>0 then Graphics.NavigationallyWarpMouseCursorTo(element.TranslatePoint(Point(float(i-1)*OMTW+OMTW/2., float(j*11*3+11*3/2)), cm.AppMainCanvas))
                    | Some(HotKeys.GlobalHotkeyTargets.MoveCursorUp) -> 
                        ea.Handled <- true
                        if j>0 then Graphics.NavigationallyWarpMouseCursorTo(element.TranslatePoint(Point(float(i)*OMTW+OMTW/2., float((j-1)*11*3+11*3/2)), cm.AppMainCanvas))
                    | Some(HotKeys.GlobalHotkeyTargets.MoveCursorDown) -> 
                        ea.Handled <- true
                        if j<7 then Graphics.NavigationallyWarpMouseCursorTo(element.TranslatePoint(Point(float(i)*OMTW+OMTW/2., float((j+1)*11*3+11*3/2)), cm.AppMainCanvas))
                    | Some(HotKeys.GlobalHotkeyTargets.LeftClick) -> Graphics.Win32.LeftMouseClick()
                    | Some(HotKeys.GlobalHotkeyTargets.MiddleClick) -> Graphics.Win32.MiddleMouseClick()
                    | Some(HotKeys.GlobalHotkeyTargets.RightClick) -> Graphics.Win32.RightMouseClick()
                    | _ -> ()
                )
            async {
                Graphics.WarpMouseCursorTo(Point(OMTW*8.5, 150. + float(11*3*4+11*3/2)))  // warp mouse to center of map
                do! CustomComboBoxes.DoModal(cm, wh, 0., 150., element)
                popupIsActive <- false
                } |> Async.StartImmediate
        )
    let setStartIconXY(i,j) =
        TrackerModel.startIconX <- i
        TrackerModel.startIconY <- j
    legendStartIconButtonBehavior <- makeIconButtonBehavior("Mark where you first spawned at the start of the game\n", "Start Spot", makeStartIcon, 8.5*OMTW/48., 0., setStartIconXY)
    legendStartIconButton.Click.Add(fun _ -> legendStartIconButtonBehavior())

    let setCustomWaypointXY(i,j) =
        TrackerModel.customWaypointX <- i
        TrackerModel.customWaypointY <- j
    let customWaypointButtonBehavior = makeIconButtonBehavior("(You can use the Custom Waypoint marker for whatever reason you like, to mark a single overworld tile)\n",
                                                                "Custom Waypoint", makeCustomWaypointIcon, -2., -5., setCustomWaypointXY)
    let customWaypointButtonCanvas = new Canvas(Background=BG, Width=OMTW*1.8, Height=11.*3.-4.)
    let customWaypointIcon = makeCustomWaypointIcon()
    let scaleTrans = new ScaleTransform(0.6666,0.6666)
    if scaleTrans.CanFreeze then
        scaleTrans.Freeze()
    customWaypointIcon.RenderTransform <- scaleTrans
    canvasAdd(customWaypointButtonCanvas, customWaypointIcon, 4., 0.)
    let tb = new TextBox(FontSize=10., Foreground=Brushes.Orange, Background=BG, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Custom\nWaypoint", IsHitTestVisible=false)
    canvasAdd(customWaypointButtonCanvas, tb, 0.8*OMTW, 0.)
    let customWaypointButton = new Button(Content=customWaypointButtonCanvas)
    canvasAdd(legendCanvas, customWaypointButton, 416., 0.)
    customWaypointButton.Click.Add(fun _ -> customWaypointButtonBehavior())

    recorderDestinationButtonCanvas, anyRoadLegendIcon, dungeonLegendIconArea, updateCurrentRecorderDestinationNumeral, legendCanvas, startIcon, theCustomWaypointIcon

let MakeItemProgressBar(owInstance:OverworldData.OverworldInstance) =
    // item progress
    let itemProgressCanvas = new Canvas(Width=16.*OMTW, Height=30.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, 
                            BorderBrush=Brushes.Gray, BorderThickness=Thickness(1.), Text="Item Progress", IsHitTestVisible=false)
    itemProgressCanvas.MouseMove.Add(fun ea ->
        let pos = ea.GetPosition(itemProgressCanvas)
        let x = pos.X - ITEM_PROGRESS_FIRST_ITEM
        if x >  30. && x <  60. then
            showLocatorInstanceFunc(owInstance.Burnable)
        if x > 240. && x < 270. then
            showLocatorInstanceFunc(owInstance.Ladderable)
        if x > 270. && x < 300. then
            showLocatorInstanceFunc(owInstance.Whistleable)
        if x > 300. && x < 330. then
            showLocatorInstanceFunc(owInstance.PowerBraceletable)
        if x > 330. && x < 360. then
            showLocatorInstanceFunc(owInstance.Raftable)
        )
    itemProgressCanvas.MouseLeave.Add(fun _ -> hideLocator())

    // make item progress bar images only once, then swap e.g. grey/blue/red candle on redraw; re-creating the images each time is expensive as measure in profiling
    let mutable x, y = ITEM_PROGRESS_FIRST_ITEM, 3.
    let DX = 30.
    let swordsByLevel = Array.init 4 (fun level -> 
        let sword = Graphics.BMPtoImage(Graphics.swordLevelToBmp(level))
        sword.Opacity <- 0.
        canvasAdd(itemProgressCanvas, sword, x, y)
        sword
        )
    x <- x + DX
    let grey_candle = Graphics.BMPtoImage(Graphics.greyscale Graphics.red_candle_bmp)
    let blue_candle = Graphics.BMPtoImage Graphics.blue_candle_bmp
    let red_candle = Graphics.BMPtoImage Graphics.red_candle_bmp
    for candle in [grey_candle; blue_candle; red_candle] do
        candle.Opacity <- 0.
        canvasAdd(itemProgressCanvas, candle, x, y)
    x <- x + DX
    let ringsByLevel = Array.init 3 (fun level -> 
        let ring = Graphics.BMPtoImage(Graphics.ringLevelToBmp(level))
        ring.Opacity <- 0.
        canvasAdd(itemProgressCanvas, ring, x, y)
        ring
        )
    x <- x + DX
    let have_bow = Graphics.BMPtoImage Graphics.bow_bmp
    let grey_bow = Graphics.BMPtoImage(Graphics.greyscale Graphics.bow_bmp)
    have_bow.Opacity <- 0.
    grey_bow.Opacity <- 0.
    canvasAdd(itemProgressCanvas, have_bow, x, y)
    canvasAdd(itemProgressCanvas, grey_bow, x, y)
    x <- x + DX
    let grey_arrow = Graphics.BMPtoImage(Graphics.greyscale Graphics.silver_arrow_bmp)
    let wood_arrow = Graphics.BMPtoImage Graphics.wood_arrow_bmp
    let silver_arrow = Graphics.BMPtoImage Graphics.silver_arrow_bmp
    for arrow in [grey_arrow; wood_arrow; silver_arrow] do
        arrow.Opacity <- 0.
        canvasAdd(itemProgressCanvas, arrow, x, y)
    x <- x + DX
    let have_wand = Graphics.BMPtoImage Graphics.wand_bmp
    let grey_wand = Graphics.BMPtoImage(Graphics.greyscale Graphics.wand_bmp)
    have_wand.Opacity <- 0.
    grey_wand.Opacity <- 0.
    canvasAdd(itemProgressCanvas, have_wand, x, y)
    canvasAdd(itemProgressCanvas, grey_wand, x, y)
    x <- x + DX
    let have_book = Graphics.BMPtoImage Graphics.book_bmp
    let grey_book = Graphics.BMPtoImage(Graphics.greyscale Graphics.book_bmp)
    let have_boom_book = Graphics.BMPtoImage Graphics.boom_book_bmp
    let grey_boom_book = Graphics.BMPtoImage(Graphics.greyscale Graphics.boom_book_bmp)
    for book in [have_book; grey_book; have_boom_book; grey_boom_book] do
        book.Opacity <- 0.
        canvasAdd(itemProgressCanvas, book, x, y)
    x <- x + DX
    let grey_boomer = Graphics.BMPtoImage(Graphics.greyscale Graphics.magic_boomerang_bmp)
    let wood_boomer = Graphics.BMPtoImage Graphics.boomerang_bmp
    let magi_boomer = Graphics.BMPtoImage Graphics.magic_boomerang_bmp
    for boo in [grey_boomer; wood_boomer; magi_boomer] do
        boo.Opacity <- 0.
        canvasAdd(itemProgressCanvas, boo, x, y)
    x <- x + DX
    let grey_ladder = Graphics.BMPtoImage(Graphics.greyscale Graphics.ladder_bmp)
    let have_ladder = Graphics.BMPtoImage Graphics.ladder_bmp
    grey_ladder.Opacity <- 0.
    have_ladder.Opacity <- 0.
    canvasAdd(itemProgressCanvas, grey_ladder, x, y)
    canvasAdd(itemProgressCanvas, have_ladder, x, y)
    x <- x + DX
    let grey_recorder = Graphics.BMPtoImage(Graphics.greyscale Graphics.recorder_bmp)
    let have_recorder = Graphics.BMPtoImage Graphics.recorder_bmp
    grey_recorder.Opacity <- 0.
    have_recorder.Opacity <- 0.
    canvasAdd(itemProgressCanvas, grey_recorder, x, y)
    canvasAdd(itemProgressCanvas, have_recorder, x, y)
    x <- x + DX
    let grey_power_bracelet = Graphics.BMPtoImage(Graphics.greyscale Graphics.power_bracelet_bmp)
    let have_power_bracelet = Graphics.BMPtoImage Graphics.power_bracelet_bmp
    grey_power_bracelet.Opacity <- 0.
    have_power_bracelet.Opacity <- 0.
    canvasAdd(itemProgressCanvas, grey_power_bracelet, x, y)
    canvasAdd(itemProgressCanvas, have_power_bracelet, x, y)
    x <- x + DX
    let grey_raft = Graphics.BMPtoImage(Graphics.greyscale Graphics.raft_bmp)
    let have_raft = Graphics.BMPtoImage Graphics.raft_bmp
    grey_raft.Opacity <- 0.
    have_raft.Opacity <- 0.
    canvasAdd(itemProgressCanvas, grey_raft, x, y)
    canvasAdd(itemProgressCanvas, have_raft, x, y)
    x <- x + DX
    let grey_key = Graphics.BMPtoImage(Graphics.greyscale Graphics.key_bmp)
    let have_key = Graphics.BMPtoImage Graphics.key_bmp
    grey_key.Opacity <- 0.
    have_key.Opacity <- 0.
    canvasAdd(itemProgressCanvas, grey_key, x, y)
    canvasAdd(itemProgressCanvas, have_key, x, y)

    let redrawItemProgressBar() = 
        swordsByLevel |> Array.iteri (fun i sword -> if i = TrackerModel.playerComputedStateSummary.SwordLevel then sword.Opacity <- 1. else sword.Opacity <- 0.)
        match TrackerModel.playerComputedStateSummary.CandleLevel with
        | 0 -> grey_candle.Opacity <- 1.; blue_candle.Opacity <- 0.; red_candle.Opacity <- 0.
        | 1 -> grey_candle.Opacity <- 0.; blue_candle.Opacity <- 1.; red_candle.Opacity <- 0.
        | 2 -> grey_candle.Opacity <- 0.; blue_candle.Opacity <- 0.; red_candle.Opacity <- 1.
        | _ -> failwith "bad CandleLevel"
        ringsByLevel |> Array.iteri (fun i ring -> if i = TrackerModel.playerComputedStateSummary.RingLevel then ring.Opacity <- 1. else ring.Opacity <- 0.)
        if TrackerModel.playerComputedStateSummary.HaveBow then
            have_bow.Opacity <- 1.; grey_bow.Opacity <- 0.
        else
            have_bow.Opacity <- 0.; grey_bow.Opacity <- 1.
        match TrackerModel.playerComputedStateSummary.ArrowLevel with
        | 0 -> grey_arrow.Opacity <- 1.; wood_arrow.Opacity <- 0.; silver_arrow.Opacity <- 0.
        | 1 -> grey_arrow.Opacity <- 0.; wood_arrow.Opacity <- 1.; silver_arrow.Opacity <- 0.
        | 2 -> grey_arrow.Opacity <- 0.; wood_arrow.Opacity <- 0.; silver_arrow.Opacity <- 1.
        | _ -> failwith "bad ArrowLevel"
        if TrackerModel.playerComputedStateSummary.HaveWand then
            have_wand.Opacity <- 1.; grey_wand.Opacity <- 0.
        else
            have_wand.Opacity <- 0.; grey_wand.Opacity <- 1.
        if TrackerModel.IsCurrentlyBook() then
            // book seed
            if TrackerModel.playerComputedStateSummary.HaveBookOrShield then
                have_book.Opacity <- 1.; grey_book.Opacity <- 0.; have_boom_book.Opacity <- 0.; grey_boom_book.Opacity <- 0.
            else
                have_book.Opacity <- 0.; grey_book.Opacity <- 1.; have_boom_book.Opacity <- 0.; grey_boom_book.Opacity <- 0.
        else
            // boomstick seed
            if TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Value() then
                have_book.Opacity <- 0.; grey_book.Opacity <- 0.; have_boom_book.Opacity <- 1.; grey_boom_book.Opacity <- 0.
            else
                have_book.Opacity <- 0.; grey_book.Opacity <- 0.; have_boom_book.Opacity <- 0.; grey_boom_book.Opacity <- 1.
        match TrackerModel.playerComputedStateSummary.BoomerangLevel with
        | 0 -> grey_boomer.Opacity <- 1.; wood_boomer.Opacity <- 0.; magi_boomer.Opacity <- 0.
        | 1 -> grey_boomer.Opacity <- 0.; wood_boomer.Opacity <- 1.; magi_boomer.Opacity <- 0.
        | 2 -> grey_boomer.Opacity <- 0.; wood_boomer.Opacity <- 0.; magi_boomer.Opacity <- 1.
        | _ -> failwith "bad BoomerangLevel"
        if TrackerModel.playerComputedStateSummary.HaveLadder then
            have_ladder.Opacity <- 1.; grey_ladder.Opacity <- 0.
        else
            have_ladder.Opacity <- 0.; grey_ladder.Opacity <- 1.
        if TrackerModel.playerComputedStateSummary.HaveRecorder then
            have_recorder.Opacity <- 1.; grey_recorder.Opacity <- 0.
        else
            have_recorder.Opacity <- 0.; grey_recorder.Opacity <- 1.
        if TrackerModel.playerComputedStateSummary.HavePowerBracelet then
            have_power_bracelet.Opacity <- 1.; grey_power_bracelet.Opacity <- 0.
        else
            have_power_bracelet.Opacity <- 0.; grey_power_bracelet.Opacity <- 1.
        if TrackerModel.playerComputedStateSummary.HaveRaft then
            have_raft.Opacity <- 1.; grey_raft.Opacity <- 0.
        else
            have_raft.Opacity <- 0.; grey_raft.Opacity <- 1.
        if TrackerModel.playerComputedStateSummary.HaveAnyKey then
            have_key.Opacity <- 1.; grey_key.Opacity <- 0.
        else
            have_key.Opacity <- 0.; grey_key.Opacity <- 1.
    redrawItemProgressBar, itemProgressCanvas, tb

let hintExplainer =
    let img = Graphics.BMPtoImage Graphics.z1rSampleHintBMP
    let showHotKeysWidthToRightEdge = THRU_MAIN_MAP_AND_ITEM_PROGRESS_H - THRU_MAIN_MAP_H + 115. + 20.  // +20 is a kludge to make this wider
    let w = showHotKeysWidthToRightEdge - 24.
    img.Height <- img.Height / img.Width * w
    img.Width <- w
    let sp = new StackPanel(Orientation=Orientation.Vertical)
    let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), FontSize=20., Margin=Thickness(3.,0.,0.,0.)) 
    tb.Text <- "Z1R Hints Explained!"
    sp.Children.Add(tb) |> ignore
    sp.Children.Add(new DockPanel(Height=3., Background=Brushes.Orange, Margin=Thickness(6.))) |> ignore
    let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), FontSize=12., Margin=Thickness(3.,0.,0.,0.)) 
    tb.Text <- "Each z1r hint takes the form\n\n(encoded dungeon/cave name)\n(map region)\n\nor\n\n" +
                "(encoded dungeon/cave name)\nwith (some item)\n\nUse the table at the left\nto decode the locations and\nto record any map regions\n\nSample Hint:"
    sp.Children.Add(new DockPanel(Height=6.)) |> ignore
    sp.Children.Add(tb) |> ignore
    sp.Children.Add(new Border(BorderBrush=Brushes.DarkSlateBlue, BorderThickness=Thickness(3.), Child=img, Width=showHotKeysWidthToRightEdge-18., Margin=Thickness(6.))) |> ignore
    let b = new Border(BorderBrush=Brushes.White, BorderThickness=Thickness(3.), Background=Brushes.Black, Child=sp, Width=showHotKeysWidthToRightEdge)
    b.MouseDown.Add(fun ea -> ea.Handled <- true)
    b
let MakeHintDecoderUI(cm:CustomComboBoxes.CanvasManager) =
    let HINTGRID_W, HINTGRID_H = 180., 36.
    let hintGrid = makeGrid(3,OverworldData.hintMeanings.Length,int HINTGRID_W,int HINTGRID_H)
    let mutable row=0 
    let updateViewFunctions = Array.create 11 (fun _ -> ())
    let mkTxt(text) = 
        new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                    Width=HINTGRID_W-6.-4., Height=HINTGRID_H-6.-4., BorderThickness=Thickness(0.), VerticalAlignment=VerticalAlignment.Center, Text=text)
    for a,b in OverworldData.hintMeanings do
        let thisRow = row
        gridAdd(hintGrid, new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(1.), Text=a), 0, row)
        let tb = new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(1.), Text=b)
        let dp = new DockPanel(LastChildFill=true)
        let bmp = 
            if row < 8 then
                Graphics.emptyUnfoundNumberedTriforce_bmps.[row]
            elif row = 8 then
                Graphics.unfoundL9_bmp
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
        let button = new Button()
        let border = new Border(Child=button,Padding=Thickness(3.),BorderBrush=Brushes.LightGray,BorderThickness=Thickness(1.))
        gridAdd(hintGrid, border, 2, row)
        let updateView() =
            let hintZone = TrackerModel.GetLevelHint(thisRow)
            if hintZone.ToIndex() = 0 then
                b.Background <- Brushes.Black
            else
                b.Background <- Views.hintHighlightBrush
            let tb = 
                if hintZone = TrackerModel.HintZone.UNKNOWN then
                    let tb = mkTxt("Click to select")
                    tb.Foreground <- Brushes.DeepSkyBlue
                    tb
                else
                    mkTxt(hintZone.ToString())
            tb.Background <- Brushes.Transparent
            let dp = new DockPanel(LastChildFill=true, Background=Graphics.almostBlack)
            dp.MouseEnter.Add(fun _ -> dp.Background <- Graphics.almostBlackHoverFeedback)
            dp.MouseLeave.Add(fun _ -> dp.Background <- Graphics.almostBlack)
            let chevron = new TextBox(FontSize=16., Foreground=Brushes.Gray, Background=Brushes.Transparent, IsReadOnly=true, IsHitTestVisible=false, 
                                        BorderThickness=Thickness(0.), VerticalAlignment=VerticalAlignment.Center, Text="\U000025BC")
            dp.Children.Add(chevron) |> ignore
            DockPanel.SetDock(chevron, Dock.Right)
            dp.Children.Add(tb) |> ignore
            button.Content <- dp
        updateViewFunctions.[thisRow] <- updateView
        let mutable popupIsActive = false  // second level of popup, need local copy
        let activatePopup(activationDelta) =
            popupIsActive <- true
            let tileX, tileY = (let p = border.TranslatePoint(Point(),cm.AppMainCanvas) in p.X+3., p.Y+3.)
            let tileCanvas = new Canvas(Width=HINTGRID_W-6., Height=HINTGRID_H-6., Background=Brushes.Black)
            let redrawTile(i) =
                tileCanvas.Children.Clear()
                canvasAdd(tileCanvas, mkTxt(TrackerModel.HintZone.FromIndex(i).ToString()), 3., 3.)
            let gridElementsSelectablesAndIDs = [|
                for i = 0 to 10 do
                    yield mkTxt(TrackerModel.HintZone.FromIndex(i).ToString()) :> FrameworkElement, true, i
                |]
            let originalStateIndex = TrackerModel.GetLevelHint(thisRow).ToIndex()
            let (gnc, gnr, gcw, grh) = 1, 11, int HINTGRID_W-6, int HINTGRID_H-6
            let gx,gy = HINTGRID_W-3., -HINTGRID_H*float(thisRow)-9.
            let onClick(_ea, i) = CustomComboBoxes.DismissPopupWithResult(i)
            let extraDecorations = []
            let brushes = CustomComboBoxes.ModalGridSelectBrushes.Defaults()
            async {
                let! r = CustomComboBoxes.DoModalGridSelect(cm, tileX, tileY, tileCanvas, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (gnc, gnr, gcw, grh),
                                                float gcw/2., float grh/2., gx, gy, redrawTile, onClick, extraDecorations, brushes, CustomComboBoxes.NoWarp, None, "HintDecoder", None)
                match r with
                | Some(i) ->
                    TrackerModel.SetLevelHint(thisRow, TrackerModel.HintZone.FromIndex(i))
                    TrackerModel.forceUpdate()
                    updateView()
                | None -> ()
                popupIsActive <- false
                } |> Async.StartImmediate
        button.Click.Add(fun _ -> if not popupIsActive then activatePopup(0))
        button.MouseWheel.Add(fun x -> if not popupIsActive then activatePopup(if x.Delta>0 then -1 else 1))
        row <- row + 1
    let hintDescriptionTextBox = 
        new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.,0.,0.,4.), 
                    Text="Each hinted-but-not-yet-found location will cause a 'halo' to appear on\n"+
                         "the triforce/sword icon in the upper portion of the tracker, and hovering the\n"+
                         "halo will show the possible locations for that dungeon or sword cave.")
    let hintSP = new StackPanel(Orientation=Orientation.Vertical)
    hintSP.Children.Add(hintDescriptionTextBox) |> ignore
    hintSP.Children.Add(hintGrid) |> ignore
    let makeHintText(txt) = new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, Text=txt)
    let otherChoices = new DockPanel(LastChildFill=true)
    let otherTB = makeHintText("There are a few other types of hints. To see them, click here:")
    let otherButton = new Button(Content=new Label(FontSize=16., Content="Other hints"))
    DockPanel.SetDock(otherButton, Dock.Right)
    otherChoices.Children.Add(otherTB)|> ignore
    otherChoices.Children.Add(otherButton)|> ignore
    hintSP.Children.Add(otherChoices) |> ignore
    let hintBorder = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(8.), Background=Brushes.Black, Child=hintSP)
    hintBorder.MouseDown.Add(fun ea -> ea.Handled <- true)
    let tb = Graphics.makeButton("Hint Decoder", Some(12.), Some(Brushes.Orange))
    tb.MouseEnter.Add(fun _ -> showHintShopLocator())
    tb.MouseLeave.Add(fun _ -> hideLocator())
    tb.Click.Add(fun _ -> 
        if not popupIsActive then
            popupIsActive <- true
            for i = 0 to 10 do
                updateViewFunctions.[i]()
            let wh = new System.Threading.ManualResetEvent(false)
            let mutable otherButtonWasClicked = false
            otherButton.Click.Add(fun _ ->
                otherButtonWasClicked <- true
                wh.Set() |> ignore
                )
            async {
                let all = new Canvas()
                canvasAdd(all, hintBorder, 0., 0.)
                canvasAdd(all, hintExplainer, 560., 0.)
                do! CustomComboBoxes.DoModal(cm, wh, 0., 65., all)
                all.Children.Clear()
                if otherButtonWasClicked then
                    wh.Reset() |> ignore
                    let otherSP = new StackPanel(Orientation=Orientation.Vertical)
                    let otherTopTB = makeHintText("Here are the meanings of some hints, which you need to track on your own:")
                    otherTopTB.BorderThickness <- Thickness(0.,0.,0.,4.)
                    otherSP.Children.Add(otherTopTB) |> ignore
                    for desc,mean in 
                        [|
                        "A feat of strength will lead to...", "Either push a gravestone, or push\nan overworld rock requiring Power Bracelet"
                        "Sail across the water...", "Raft required to reach a place"
                        "Play a melody...", "Either an overworld recorder spot, or a\nDigdogger in a dungeon logically blocks..."
                        "Fire the arrow...", "In a dungeon, Gohma logically blocks..."
                        "Step over the water...", "Ladder required to obtain... (coast item,\noverworld river, or dungeon moat)"
                        |] do
                        let dp = new DockPanel(LastChildFill=true)
                        let d = makeHintText(desc)
                        d.Width <- 240.
                        dp.Children.Add(d) |> ignore
                        let m = makeHintText(mean)
                        DockPanel.SetDock(m, Dock.Right)
                        dp.Children.Add(m) |> ignore
                        otherSP.Children.Add(dp) |> ignore
                    let otherBottomTB = makeHintText("\nHere are the meanings of a couple final hints, which the tracker can help with\nby darkening the overworld spots you can logically ignore\n(click the checkbox to darken corresponding spots on the overworld)")
                    otherBottomTB.BorderThickness <- Thickness(0.,4.,0.,4.)
                    otherSP.Children.Add(otherBottomTB) |> ignore
                    let featsCheckBox  = new CheckBox(Content=makeHintText("No feat of strength... (Power Bracelet / pushing graves not required)"))
                    featsCheckBox.IsChecked <- System.Nullable.op_Implicit TrackerModel.NoFeatOfStrengthHintWasGiven
                    featsCheckBox.Checked.Add(fun _ -> TrackerModel.NoFeatOfStrengthHintWasGiven <- true; hideFeatsOfStrength true)
                    featsCheckBox.Unchecked.Add(fun _ -> TrackerModel.NoFeatOfStrengthHintWasGiven <- false; hideFeatsOfStrength false)
                    otherSP.Children.Add(featsCheckBox) |> ignore
                    let raftsCheckBox  = new CheckBox(Content=makeHintText("Sail not... (Raft not required)"))
                    raftsCheckBox.IsChecked <- System.Nullable.op_Implicit TrackerModel.SailNotHintWasGiven
                    raftsCheckBox.Checked.Add(fun _ -> TrackerModel.SailNotHintWasGiven <- true; hideRaftSpots true)
                    raftsCheckBox.Unchecked.Add(fun _ -> TrackerModel.SailNotHintWasGiven <- false; hideRaftSpots false)
                    otherSP.Children.Add(raftsCheckBox) |> ignore
                    let otherHintBorder = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(8.), Background=Brushes.Black, Child=otherSP)
                    do! CustomComboBoxes.DoModal(cm, wh, 0., 65., otherHintBorder)
                popupIsActive <- false
                } |> Async.StartImmediate
        )
    tb

open HotKeys.MyKey

let MakeBlockers(cm:CustomComboBoxes.CanvasManager, blockerQueries:ResizeArray<_>, levelTabSelected:Event<int>, blockersHoverEvent:Event<bool>, blockerDungeonSunglasses:FrameworkElement[],
                        contentCanvasMouseEnterFunc, contentCanvasMouseLeaveFunc) =
    // blockers
    let blockerBoxes : Canvas[,] = Array2D.zeroCreate 8 TrackerModel.DungeonBlockersContainer.MAX_BLOCKERS_PER_DUNGEON
    let makeBlockerBox(dungeonIndex, blockerIndex) =
        let c = Views.MakeBlockerView(dungeonIndex, blockerIndex)
        let mutable current = TrackerModel.DungeonBlockersContainer.GetDungeonBlocker(dungeonIndex, blockerIndex)
        TrackerModel.DungeonBlockersContainer.AnyBlockerChanged.Add(fun _ ->
            current <- TrackerModel.DungeonBlockersContainer.GetDungeonBlocker(dungeonIndex, blockerIndex)
            )
        // hovering a blocker box with a sellable item will highlight the corresponding shops (currently only way to mouse-hover to see key/meat shops)
        c.MouseEnter.Add(fun _ -> 
            match current.HardCanonical() with
            | TrackerModel.DungeonBlocker.BAIT ->           showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.MEAT)
            | TrackerModel.DungeonBlocker.KEY ->            showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.KEY)
            | TrackerModel.DungeonBlocker.BOMB ->           showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BOMB)
            | TrackerModel.DungeonBlocker.BOW_AND_ARROW ->  showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.ARROW)
            | TrackerModel.DungeonBlocker.MONEY ->          showLocatorRupees()
            | _ -> contentCanvasMouseEnterFunc(dungeonIndex+1)   // if nothing specific for this box to show, default to parent's hover behavior
            )
        c.MouseLeave.Add(fun _ -> hideLocator())
        Views.appMainCanvasGlobalBoxMouseOverHighlight.ApplyBehavior(c)
        blockerQueries.Add(fun () ->
            let pos = c.TranslatePoint(Point(), cm.AppMainCanvas)
            match current.HardCanonical() with
            | TrackerModel.DungeonBlocker.BAIT ->           Some(TrackerModel.MapSquareChoiceDomainHelper.MEAT, (pos.X, pos.Y))
            | TrackerModel.DungeonBlocker.KEY ->            Some(TrackerModel.MapSquareChoiceDomainHelper.KEY, (pos.X, pos.Y))
            | TrackerModel.DungeonBlocker.BOMB ->           Some(TrackerModel.MapSquareChoiceDomainHelper.BOMB, (pos.X, pos.Y))
            | TrackerModel.DungeonBlocker.BOW_AND_ARROW ->  Some(TrackerModel.MapSquareChoiceDomainHelper.ARROW, (pos.X, pos.Y))
            | _ -> None
            )
        let SetNewValue(db) = TrackerModel.DungeonBlockersContainer.SetDungeonBlocker(dungeonIndex, blockerIndex, db)
        let activate(activationDelta) =
            popupIsActive <- true
            let pc, predraw = Views.MakeBlockerCore()
            let popupRedraw(n) =
                let innerc = predraw(n)
                let s = HotKeys.BlockerHotKeyProcessor.AppendHotKeyToDescription(n.DisplayDescription(), n)
                let text = new TextBox(Text=s, Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                                            FontSize=16., HorizontalContentAlignment=HorizontalAlignment.Center)
                let textBorder = new Border(BorderThickness=Thickness(3.), Child=text, Background=Brushes.Black, BorderBrush=Brushes.Gray)
                let dp = new DockPanel(LastChildFill=false)
                DockPanel.SetDock(textBorder, Dock.Right)
                dp.Children.Add(textBorder) |> ignore
                Canvas.SetTop(dp, 30.)
                Canvas.SetRight(dp, 120.)
                innerc.Children.Add(dp) |> ignore
            let pos = c.TranslatePoint(Point(), cm.AppMainCanvas)
            async {
                let! r = CustomComboBoxes.DoModalGridSelect(cm, pos.X, pos.Y, pc, TrackerModel.DungeonBlocker.All |> Array.map (fun db ->
                                (if db=TrackerModel.DungeonBlocker.NOTHING then upcast Canvas() else upcast Graphics.blockerCurrentDisplay(db)), db.PlayerCouldBeBlockedByThis(), db), 
                                System.Array.IndexOf(TrackerModel.DungeonBlocker.All, current), activationDelta, (4, 4, 24, 24), 12., 12., -90., 30., popupRedraw,
                                (fun (_ea,db) -> CustomComboBoxes.DismissPopupWithResult(db)), [], CustomComboBoxes.ModalGridSelectBrushes.Defaults(), CustomComboBoxes.WarpToCenter, None, "Blocker", None)
                match r with
                | Some(db) -> SetNewValue(db)
                | None -> () 
                popupIsActive <- false
                } |> Async.StartImmediate
        let doPanel(pos:Point) =
            popupIsActive <- true
            let border = new Border(BorderBrush=Brushes.LightGray, BorderThickness=Thickness(3.), Background=Brushes.Black, Width=110.)
            let style = new Style(typeof<TextBox>)
            style.Setters.Add(new Setter(TextBox.BorderThicknessProperty, Thickness(0.)))
            style.Setters.Add(new Setter(TextBox.FontSizeProperty, 16.))
            style.Setters.Add(new Setter(TextBox.ForegroundProperty, Brushes.Orange))
            style.Setters.Add(new Setter(TextBox.BackgroundProperty, Brushes.Black))
            style.Setters.Add(new Setter(TextBox.IsHitTestVisibleProperty, false))
            style.Setters.Add(new Setter(TextBox.IsReadOnlyProperty, true))
            border.Resources.Add(typeof<TextBox>, style)
            let style = new Style(typeof<CheckBox>)
            style.Setters.Add(new Setter(CheckBox.HeightProperty, 22.))
            border.Resources.Add(typeof<CheckBox>, style)
            let panel = new StackPanel(Orientation=Orientation.Vertical)
            let hPanel = new System.Windows.Controls.Primitives.UniformGrid(Rows=1, Columns=2)
            let allButton = Graphics.makeButton("All", Some(16.), Some(Brushes.Orange))
            let noneButton = Graphics.makeButton("None", Some(16.), Some(Brushes.Orange))
            hPanel.Children.Add(allButton) |> ignore
            hPanel.Children.Add(noneButton) |> ignore
            let decorationCanvas = new Canvas()
            canvasAdd(decorationCanvas, new Border(BorderBrush=Brushes.LightGray, BorderThickness=Thickness(3.), Width=30., Height=30.), 77., -33.)
            panel.Children.Add(decorationCanvas) |> ignore
            panel.Children.Add(new TextBox(Text="Applies to...")) |> ignore
            panel.Children.Add(hPanel) |> ignore
            let cbs = ResizeArray()
            for name,i in ["Map",0; "Compass",1; "Triforce",2; "Item Box 1",3; "Item Box 2",4; "Item Box 3",5] do
                if i < 5 || TrackerModel.GetDungeon(dungeonIndex).Boxes.Length > 2 then 
                    let cb = new CheckBox(Content=new TextBox(Text=name))
                    cb.IsChecked <- TrackerModel.DungeonBlockersContainer.GetDungeonBlockerAppliesTo(dungeonIndex, blockerIndex, i)
                    cb.Checked.Add(fun _ -> TrackerModel.DungeonBlockersContainer.SetDungeonBlockerAppliesTo(dungeonIndex, blockerIndex, i, true))
                    cb.Unchecked.Add(fun _ -> TrackerModel.DungeonBlockersContainer.SetDungeonBlockerAppliesTo(dungeonIndex, blockerIndex, i, false))
                    panel.Children.Add(cb) |> ignore
                    cbs.Add(cb)
            allButton.Click.Add(fun _ -> for cb in cbs do cb.IsChecked <- true)
            noneButton.Click.Add(fun _ -> for cb in cbs do cb.IsChecked <- false)
            border.Child <- panel
            let wh = new System.Threading.ManualResetEvent(false)
            async {
                do! CustomComboBoxes.DoModal(cm, wh, pos.X-80., pos.Y+30., border)
                popupIsActive <- false
            } |> Async.StartImmediate
        c.MouseWheel.Add(fun x -> if not popupIsActive then activate(if x.Delta<0 then 1 else -1))
        c.MouseDown.Add(fun ea ->
            if not popupIsActive then
                if ea.ChangedButton = Input.MouseButton.Middle || 
                        (ea.ChangedButton = Input.MouseButton.Left && (Input.Keyboard.IsKeyDown(Input.Key.LeftShift) || Input.Keyboard.IsKeyDown(Input.Key.RightShift))) then
                    // middle-click or shift-left-click activates the checkbox panel
                    let pos = c.TranslatePoint(Point(), cm.AppMainCanvas)
                    doPanel(pos)
                else
                    activate(0)
            )
        c.MyKeyAdd(fun ea -> 
            if not popupIsActive then
                match HotKeys.GlobalHotKeyProcessor.TryGetValue(ea.Key) with
                | Some(HotKeys.GlobalHotkeyTargets.MoveCursorRight) -> 
                    ea.Handled <- true
                    if blockerIndex<TrackerModel.DungeonBlockersContainer.MAX_BLOCKERS_PER_DUNGEON-1 then
                        Graphics.NavigationallyWarpMouseCursorTo(blockerBoxes.[dungeonIndex,blockerIndex+1].TranslatePoint(Point(15.,15.),cm.AppMainCanvas))
                    elif dungeonIndex<>1 && dungeonIndex<>4 && dungeonIndex<>7 then
                        Graphics.NavigationallyWarpMouseCursorTo(blockerBoxes.[dungeonIndex+1,0].TranslatePoint(Point(15.,15.),cm.AppMainCanvas))
                | Some(HotKeys.GlobalHotkeyTargets.MoveCursorLeft) -> 
                    if blockerIndex>0 then
                        ea.Handled <- true
                        Graphics.NavigationallyWarpMouseCursorTo(blockerBoxes.[dungeonIndex,blockerIndex-1].TranslatePoint(Point(15.,15.),cm.AppMainCanvas))
                    elif dungeonIndex<>0 && dungeonIndex<>2 && dungeonIndex<>5 then
                        ea.Handled <- true
                        Graphics.NavigationallyWarpMouseCursorTo(blockerBoxes.[dungeonIndex-1,TrackerModel.DungeonBlockersContainer.MAX_BLOCKERS_PER_DUNGEON-1].TranslatePoint(Point(15.,15.),cm.AppMainCanvas))
                    // else unhandled, so app global handler will nudge mouse left
                | Some(HotKeys.GlobalHotkeyTargets.MoveCursorDown) -> 
                    ea.Handled <- true
                    if dungeonIndex<5 then
                        Graphics.NavigationallyWarpMouseCursorTo(blockerBoxes.[dungeonIndex+3,blockerIndex].TranslatePoint(Point(15.,15.),cm.AppMainCanvas))
                | Some(HotKeys.GlobalHotkeyTargets.MoveCursorUp) -> 
                    if dungeonIndex>2 then
                        ea.Handled <- true
                        Graphics.NavigationallyWarpMouseCursorTo(blockerBoxes.[dungeonIndex-3,blockerIndex].TranslatePoint(Point(15.,15.),cm.AppMainCanvas))
                    // else unhandled, so app global handler will nudge mouse up
                | _ -> ()
                match HotKeys.BlockerHotKeyProcessor.TryGetValue(ea.Key) with
                | Some(db) -> 
                    ea.Handled <- true
                    if current = db then
                        SetNewValue(TrackerModel.DungeonBlocker.NOTHING)    // idempotent hotkeys behave as a toggle
                    else
                        SetNewValue(db)
                | None -> ()
            )
        blockerBoxes.[dungeonIndex, blockerIndex] <- c
        c

    //let blockerColumnWidth = int((cm.AppMainCanvas.Width-BLOCKERS_AND_NOTES_OFFSET)/3.)   // would be 106
    let blockerColumnWidth = 108 // multiple of 9, to make 2/3 size avoid looking blurry
    let blockerGrid = makeGrid(3, 3, blockerColumnWidth, 36)
    let blockerHighlightBrush = Graphics.freeze(new SolidColorBrush(Color.FromRgb(50uy, 70uy, 50uy)))
    blockerGrid.Height <- float(36*3)
    for i = 0 to 2 do
        for j = 0 to 2 do
            if i=0 && j=0 then
                let d = new DockPanel(LastChildFill=false, Background=Brushes.Black)
                let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text="BLOCKERS", Width=float blockerColumnWidth, IsHitTestVisible=false,
                                        VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), TextAlignment=TextAlignment.Center)
                d.ToolTip <- "The icons you set in this area can remind you of what blocked you in a dungeon.\nFor example, a ladder represents being ladder blocked, or a sword means you need better weapons.\nSome reminders will trigger when you get the item that may unblock you."
                ToolTipService.SetPlacement(d, Primitives.PlacementMode.Top)
                d.Children.Add(tb) |> ignore
                let mutable cooldownTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Normal)
                cooldownTimer.Interval <- System.TimeSpan.FromMilliseconds(1000.)
                cooldownTimer.Tick.Add(fun _ ->
                    blockersHoverEvent.Trigger(true)
                    )
                d.MouseEnter.Add(fun _ -> 
                    cooldownTimer.Start()
                    )
                d.MouseLeave.Add(fun _ -> 
                    cooldownTimer.Stop()
                    blockersHoverEvent.Trigger(false)
                    )
                gridAdd(blockerGrid, d, i, j)
            else
                let dungeonIndex = (3*j+i)-1
                let labelChar = if TrackerModel.IsHiddenDungeonNumbers() then "ABCDEFGH".[dungeonIndex] else "12345678".[dungeonIndex]
                let sp = new StackPanel(Orientation=Orientation.Horizontal, Background=Brushes.Black)
                levelTabSelected.Publish.Add(fun level -> if level=dungeonIndex+1 then sp.Background <- blockerHighlightBrush else sp.Background <- Brushes.Black)
                let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text=labelChar.ToString(), Width=10., Height=14., IsHitTestVisible=false,
                                        VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.),
                                        TextAlignment=TextAlignment.Center, Margin=Thickness(2.,0.,0.,0.))
                let updateLabelColor() = tb.Foreground <- if TrackerModel.GetDungeon(dungeonIndex).HasBeenLocated() then Brushes.White else Brushes.Orange
                TrackerModel.GetDungeon(dungeonIndex).HasBeenLocatedChanged.Add(updateLabelColor)
                sp.Children.Add(tb) |> ignore
                for i = 0 to TrackerModel.DungeonBlockersContainer.MAX_BLOCKERS_PER_DUNGEON-1 do
                    sp.Children.Add(makeBlockerBox(dungeonIndex, i)) |> ignore
                gridAdd(blockerGrid, sp, i, j)
                sp.MouseEnter.Add(fun _ -> contentCanvasMouseEnterFunc(dungeonIndex+1))
                sp.MouseLeave.Add(fun _ -> contentCanvasMouseLeaveFunc(dungeonIndex+1))
                blockerDungeonSunglasses.[dungeonIndex] <- upcast sp // just reduce its opacity
    blockerGrid

let MakeZoneOverlay(overworldCanvas:Canvas, ensurePlaceholderFinished, mirrorOverworldFEs:ResizeArray<FrameworkElement>) =
    // zone overlay
    let owMapZoneColorCanvases, owMapZoneBlackCanvases =
        let avg(c1:System.Drawing.Color, c2:System.Drawing.Color) = System.Drawing.Color.FromArgb((int c1.R + int c2.R)/2, (int c1.G + int c2.G)/2, (int c1.B + int c2.B)/2)
        let toBrush(c:System.Drawing.Color) = Graphics.freeze(new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B)))
        let colors = 
            dict [
                'M', avg(System.Drawing.Color.Pink, System.Drawing.Color.Crimson) |> toBrush
                'L', System.Drawing.Color.BlueViolet |> toBrush
                'R', System.Drawing.Color.LightSeaGreen |> toBrush
                'H', System.Drawing.Color.Gray |> toBrush
                'C', System.Drawing.Color.LightBlue |> toBrush
                'G', avg(System.Drawing.Color.LightSteelBlue, System.Drawing.Color.SteelBlue) |> toBrush
                'D', System.Drawing.Color.Orange |> toBrush
                'F', System.Drawing.Color.LightGreen |> toBrush
                'S', System.Drawing.Color.DarkGray |> toBrush
                'W', System.Drawing.Color.Brown |> toBrush
            ]
        let imgs,darks = Array2D.zeroCreate 16 8, Array2D.zeroCreate 16 8
        for x = 0 to 15 do
            for y = 0 to 7 do
                imgs.[x,y] <- new Canvas(Width=OMTW, Height=float(11*3), Background=colors.Item(OverworldData.owMapZone.[y].[x]), IsHitTestVisible=false)
                darks.[x,y] <- new Canvas(Width=OMTW, Height=float(11*3), Background=Brushes.Black, IsHitTestVisible=false)
        imgs, darks
    let owMapZoneGrid = makeGrid(16, 8, int OMTW, 11*3)
    let allOwMapZoneColorCanvases,allOwMapZoneBlackCanvases = Array2D.zeroCreate 16 8, Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let zcc,zbc = owMapZoneColorCanvases.[i,j], owMapZoneBlackCanvases.[i,j]
            zcc.Opacity <- 0.0
            zbc.Opacity <- 0.0
            allOwMapZoneColorCanvases.[i,j] <- zcc
            allOwMapZoneBlackCanvases.[i,j] <- zbc
            gridAdd(owMapZoneGrid, zcc, i, j)
            gridAdd(owMapZoneGrid, zbc, i, j)
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

    let mutable isCurrentlyShown = false
    let changeZoneOpacity(hintZone,show) =
        ensurePlaceholderFinished()
        let noZone = hintZone=TrackerModel.HintZone.UNKNOWN
        if show then
            isCurrentlyShown <- true
            if noZone then 
                allOwMapZoneColorCanvases |> Array2D.iteri (fun _x _y zcc -> zcc.Opacity <- 0.3)
            owMapZoneBoundaries |> Seq.iter (fun x -> x.Opacity <- 0.9)
            zoneNames |> Seq.iter (fun (hz,textbox) -> if noZone || hz=hintZone then textbox.Opacity <- 0.6)
        else
            if isCurrentlyShown then   // performance - don't constantly re-do all this idempotent work
                allOwMapZoneColorCanvases |> Array2D.iteri (fun _x _y zcc -> zcc.Opacity <- 0.0)
                owMapZoneBoundaries |> Seq.iter (fun x -> x.Opacity <- 0.0)
                zoneNames |> Seq.iter (fun (_hz,textbox) -> textbox.Opacity <- 0.0)
                isCurrentlyShown <- false
    let zone_checkbox = new CheckBox(Content=new TextBox(Text="Zones",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false))
    zone_checkbox.IsChecked <- System.Nullable.op_Implicit TrackerModelOptions.Overworld.Zones.Value
    zone_checkbox.Checked.Add(fun _ -> changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,true); TrackerModelOptions.Overworld.Zones.Value <- true; TrackerModelOptions.writeSettings())
    zone_checkbox.Unchecked.Add(fun _ -> changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,false); TrackerModelOptions.Overworld.Zones.Value <- false; TrackerModelOptions.writeSettings())
    zone_checkbox.MouseEnter.Add(fun _ -> if not zone_checkbox.IsChecked.HasValue || not zone_checkbox.IsChecked.Value then changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,true))
    zone_checkbox.MouseLeave.Add(fun _ -> if not zone_checkbox.IsChecked.HasValue || not zone_checkbox.IsChecked.Value then changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,false))

    zone_checkbox, addZoneName, changeZoneOpacity, allOwMapZoneBlackCanvases

open OverworldMapTileCustomization

let MakeMouseHoverExplainer(appMainCanvas:Canvas) =
    let mouseHoverExplainerIcon = new Button(Content=(Graphics.greyscale(Graphics.question_marks_bmp) |> Graphics.BMPtoImage))
    let c = new Canvas(Width=appMainCanvas.Width, Height=THRU_MAIN_MAP_AND_ITEM_PROGRESS_H, Opacity=0., IsHitTestVisible=false)
    let darkenTop = new Canvas(Width=OMTW*16., Height=150., Background=Brushes.Black, Opacity=0.40)
    canvasAdd(c, darkenTop, 0., 0.)
    let darkenOW = new Canvas(Width=OMTW*16., Height=11.*3.*8., Background=Brushes.Black, Opacity=0.85)
    canvasAdd(c, darkenOW, 0., 150.)
    let darkenBottom = new Canvas(Width=OMTW*16., Height=THRU_MAIN_MAP_AND_ITEM_PROGRESS_H - THRU_MAIN_MAP_H, Background=Brushes.Black, Opacity=0.40)
    canvasAdd(c, darkenBottom, 0., 150.+11.*3.*8.)

    let descMHE = new TextBox(Text="Mouse Hover Explainer",FontSize=30.0,Background=Brushes.Transparent,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true)

    let delayedDescriptions = ResizeArray()
    let mkTxt(text) = new TextBox(Text=text,FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(1.0),IsReadOnly=true)
    let addLabel(poly:Shapes.Polyline, text, x, y) =
        poly.Points.Add(Point(x,y))
        canvasAdd(c, poly, 0., 0.)
        delayedDescriptions.Add(c, mkTxt(text), x, y)

    let ST = 2.0
    let COL = Brushes.Green
    let triforces = 
        if TrackerModel.IsHiddenDungeonNumbers() then
            new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,58.; 268.,58.; 268.,32.; 528.,32.; 528.,2.; 2.,2.; 2.,58. ] |> Seq.map Point ))
        else
            new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,58.; 268.,58.; 268.,32.; 2.,32.; 2.,58. ] |> Seq.map Point ))
    addLabel(triforces, "Show location of dungeon, if known or hinted", 10., 300.)

    let COL = Brushes.MediumVioletRed
    let dx,dy = 573.,0.
    let eyeball = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,28.; 28.,28.; 28.,2.; 2.,2.; 2.,28. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(eyeball, "Hide all overworld icons", 300., 4.)

    let dx,dy = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.WHITE_SWORD_ICON)
    let whiteSword = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,28.; 28.,28.; 28.,2.; 2.,2.; 2.,28. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(whiteSword, "Show location of white sword cave, if known or hinted", 30., 270.)

    let dx,dy = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.ZELDA_BOX)
    let shopping = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 28.,28.; 28.,2.; 2.,2.; 2.,28.; 28.,28. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(shopping, "Show icons you opted to hide", 410., 150.)

    let COL = Brushes.CornflowerBlue
    let dx,dy = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.ARMOS_ICON)
    let armos = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,28.; 28.,28.; 28.,2.; 2.,2.; 2.,28. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(armos, "Show locations of any unmarked armos", 120., 240.)

    let dx,dy = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.WOOD_ARROW_BOX)
    let shopping = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,28.; 88.,28.; 88.,-28.; 32.,-28.; 32.,2.; 2.,2.; 2.,28. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(shopping, "Show locations of shops containing each item (or blocker", 400., 240.)
    let dx,dy = BLOCKERS_AND_NOTES_OFFSET+72., START_DUNGEON_AND_NOTES_AREA_H
    let blockers = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 10.,0.; -70.,0.; -70.,36.; 36.,36.; 36.,0.; 10.,0. ] |> Seq.map (fun (x,y) -> Point(210.+dx+x,dy+y))))
    addLabel(blockers, ")", 757., 240.)

    let COL = Brushes.MediumVioletRed
    let dx,dy = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.BLUE_CANDLE_BOX)
    let shopping = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,28.; 28.,28.; 28.,2.; 2.,2.; 2.,28. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(shopping, "If have no candle, show locations of shops with blue candle\nElse show unmarked burnable bush locations", 380., 270.)

    let COL = Brushes.Green
    let dx,dy = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.MAGS_BOX)
    let magsAndWoodSword = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,28.; 58.,28.; 58.,2.; 2.,2.; 2.,28. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(magsAndWoodSword, "Show locations of magical/wood sword caves, if known or hinted", 300., 210.)

    let dx,dy = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.HEARTS)
    let hearts = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,28.; 118.,28.; 118.,2.; 2.,2.; 2.,28. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    hearts.Points.Add(Point(270.,170.))
    canvasAdd(c, hearts, 0., 0.)
    let desc = mkTxt("Show locations of potion shops\nand un-taken Take Anys")
    desc.TextAlignment <- TextAlignment.Right
    Canvas.SetRight(desc, c.Width-270.)
    Canvas.SetTop(desc, 170.)
    c.Children.Add(desc) |> ignore

    let COL = Brushes.CornflowerBlue
    let dx,dy = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.HEARTS)
    let maxhearts = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 124.,28.; 124.,10.; 227.,10.; 227.,28.; 124.,28. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    maxhearts.Points.Add(Point(390.,160.))
    canvasAdd(c, maxhearts, 0., 0.)
    let desc = mkTxt("Show inventory\n& max hearts")
    desc.TextAlignment <- TextAlignment.Right
    Canvas.SetRight(desc, c.Width-390.)
    Canvas.SetTop(desc, 160.)
    c.Children.Add(desc) |> ignore

    let COL = Brushes.Green
    let openCaves = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 531.,138.; 547.,138.; 547.,122.; 531.,122.; 531.,138. ] |> Seq.map Point))
    addLabel(openCaves, "Show locations of unmarked open caves", 430., 180.)

    let COL = Brushes.MediumVioletRed
    let zonesEtAl = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 550.,50.; 475.,50.; 475.,92.; 406.,92.; 406.,130.; 505.,130.; 505.,110.; 550.,110.; 550.,50. ] |> Seq.map Point))
    addLabel(zonesEtAl, "As described", 630., 150.)

    let spotSummary = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 614.,115.; 725.,115.; 725.,90.; 614.,90.; 614.,115.; 630.,150. ] |> Seq.map Point))
    canvasAdd(c, spotSummary, 0., 0.)

    let COL = Brushes.Green
    let dx,dy = 85., THRU_MAIN_MAP_H + 3.
    let anyRoad = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ -2.,2.; -2.,25.; 13.,25.; 13.,2.; -2.,2. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(anyRoad, "Show Any Roads", 40., 340.)
    let COL = Brushes.MediumVioletRed
    let dx,dy = ITEM_PROGRESS_FIRST_ITEM+25., THRU_MAP_AND_LEGEND_H
    let candle = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,2.; 2.,28.; 28.,28.; 28.,2.; 2.,2. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    candle.Points.Add(Point(120.,410.))
    canvasAdd(c, candle, 0., 0.)
    let desc = mkTxt("Show Burnables")
    Canvas.SetRight(desc, c.Width-120.)
    Canvas.SetTop(desc, 390.)
    c.Children.Add(desc) |> ignore
    let COL = Brushes.CornflowerBlue
    let dx,dy = ITEM_PROGRESS_FIRST_ITEM+25.+7.*30., THRU_MAP_AND_LEGEND_H-2.
    let others = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,2.; 2.,28.; 118.,28.; 118.,2.; 2.,2. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    others.Points.Add(Point(330.,405.))
    canvasAdd(c, others, 0., 0.)
    let desc = mkTxt("Show Ladderable/Recorderable/\nPowerBraceletable/Raftable")
    desc.TextAlignment <- TextAlignment.Right
    Canvas.SetRight(desc, c.Width-330.)
    Canvas.SetTop(desc, 370.)
    c.Children.Add(desc) |> ignore
    let COL = Brushes.MediumVioletRed
    let dx,dy = LEFT_OFFSET + 4.8*OMTW + 15., THRU_MAIN_MAP_H + 3.
    let recorderDest = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 13.,2.; -39.,2.; -39.,25.; 13.,25.; 13.,2. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    recorderDest.Points.Add(Point(330.,340.))
    canvasAdd(c, recorderDest, 0., 0.)
    let desc = mkTxt("Show recorder destinations")
    Canvas.SetRight(desc, c.Width-330.)
    Canvas.SetTop(desc, 340.)
    c.Children.Add(desc) |> ignore
    
    let COL = Brushes.CornflowerBlue
    let dx,dy = LEFT_OFFSET + 7.8*OMTW + 56., THRU_MAIN_MAP_H + 36.
    let hintDecoderButton = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,2.; 2.,22.; 79.,22.; 79.,2.; 2.,2. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(hintDecoderButton, "Show hint shops", 442., 348.)
    
    let COL = Brushes.MediumVioletRed
    let dx,dy = BLOCKERS_AND_NOTES_OFFSET+70., START_DUNGEON_AND_NOTES_AREA_H
    let blockers = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 0.,0.; -70.,0.; -70.,36.; 38.,36.; 38.,0.; 0.,0. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(blockers, "Highlight potential\ndungeon continuations", 570., 320.)
    
    let COL = Brushes.Green
    let dx,dy = BLOCKERS_AND_NOTES_OFFSET-82., START_DUNGEON_AND_NOTES_AREA_H+2.
    let blockers = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ -4.,0.; -4.,20.; 25.,20.; 25.,0.; -4.,0.; -176.,-34.; -191.,-34.; -191.,-64.; -176.,-64.; -176.,-34.; -4.,0. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(blockers, "Show\ndungeon\nlocations", 339., 320.)

    for dd in delayedDescriptions do   // ensure these draw atop all the PolyLines
        canvasAdd(dd)
    // put MHE text atop all
    canvasAdd(c, descMHE, 450., 370.)

    mouseHoverExplainerIcon.MouseEnter.Add(fun _ -> 
        c.Opacity <- 1.0
        )
    mouseHoverExplainerIcon.MouseLeave.Add(fun _ -> 
        c.Opacity <- 0.0
        )

    mouseHoverExplainerIcon, c
