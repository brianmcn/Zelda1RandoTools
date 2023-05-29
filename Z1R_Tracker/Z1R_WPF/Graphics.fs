module Graphics

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let mutable theWindow : System.Windows.Window = null

type POINT = struct
    val x:int
    val y:int
    new(_x, _y) = {x=_x; y=_y}
end

// send input stuff
[<System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)>]
type MOUSEINPUT = struct
    val dx: int32
    val dy:int32
    val mouseData:uint32
    val dwFlags: uint32
    val time: uint32
    val dwExtraInfo: UIntPtr
    new(_dx, _dy, _mouseData, _dwFlags, _time, _dwExtraInfo) = {dx=_dx; dy=_dy; mouseData=_mouseData; dwFlags=_dwFlags; time=_time; dwExtraInfo=_dwExtraInfo}
end

[<System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)>]
type KEYBDINPUT = struct
    val wVk: uint16
    val wScan: uint16
    val dwFlags: uint32
    val time: uint32
    val dwExtraInfo: UIntPtr
    new(_wVk, _wScan, _dwFlags, _time, _dwExtraInfo) = {wVk =_wVk; wScan = _wScan; dwFlags = _dwFlags; time = _time; dwExtraInfo = _dwExtraInfo}
end

[<System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)>]
type HARDWAREINPUT = struct
    val uMsg: uint32
    val wParamL: uint16
    val wParamH: uint16
    new(_uMsg, _wParamL, _wParamH) = {uMsg = _uMsg; wParamL = _wParamL; wParamH = _wParamH}
end

[<System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)>]
type InputUnion = struct
    [<System.Runtime.InteropServices.FieldOffset(0)>]
    val mutable mi : MOUSEINPUT
    [<System.Runtime.InteropServices.FieldOffset(0)>]
    val mutable ki : KEYBDINPUT
    [<System.Runtime.InteropServices.FieldOffset(0)>]
    val mutable hi : HARDWAREINPUT 
end

[<System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)>]
type LPINPUT  = struct
    val mutable ``type``: int // 1 is keyboard
    val mutable u: InputUnion
end

type Win32() =
    [<System.Runtime.InteropServices.DllImport("User32.dll")>]
    static extern bool SetCursorPos(int X, int Y)

    //[<System.Runtime.InteropServices.DllImport("User32.dll")>]
    //static extern bool GetCursorPos(POINT *p)

    [<System.Runtime.InteropServices.DllImport("User32.dll")>]
    static extern void mouse_event(uint32 dwFlags, int dx, int dy, uint32 cButtons, int dwExtraInfo)
    
    static let MOUSEEVENTF_LEFTDOWN     = 0x02u
    static let MOUSEEVENTF_LEFTUP       = 0x04u
    static let MOUSEEVENTF_RIGHTDOWN    = 0x08u
    static let MOUSEEVENTF_RIGHTUP      = 0x10u
    static let MOUSEEVENTF_MIDDLEDOWN   = 0x20u
    static let MOUSEEVENTF_MIDDLEUP     = 0x40u
    static let MOUSEEVENTF_WHEEL        = 0x800u
    static let WHEEL_DELTA = 120
    // ||| these in if want absolute coords to send event, otherwise is relative
    //static let MOUSEEVENTF_ABSOLUTE     = 0x8000
    //static let MOUSEEVENTF_MOVE         = 0x0001

    // mouse sonar
    static let SPI_SETMOUSESONAR = 0x101Du
    static let SPIF_UPDATEINIFILE = 0x01u
    static let SPIF_SENDCHANGE = 0x02u
    [<System.Runtime.InteropServices.DllImport("user32.dll")>]
    static extern int SystemParametersInfo(uint32 uAction, uint32 uParam, bool lpvParam, uint32 fuWinIni)
    
    [<System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)>]
    static extern uint32 SendInput(int numberOfInputs, LPINPUT[] inputs, int sizeOfInputStructure)

    /////////////////////////////////////////////////////////////////////////////

    //This simulates a left mouse click
    static member LeftMouseClick() =
        //let mutable p = POINT(0,0)
        //GetCursorPos(&&p) |> ignore
        //System.Threading.ThreadPool.QueueUserWorkItem(System.Threading.WaitCallback(fun _ -> 
            //mouse_event(MOUSEEVENTF_LEFTDOWN, p.x, p.y, 0, 0)
            //mouse_event(MOUSEEVENTF_LEFTUP, p.x, p.y, 0, 0)
            //mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0u, 0)
            //System.Threading.Thread.Sleep(50)
            //mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0u, 0)
            //)) |> ignore
        // this seems to work fine, above code would perhaps be more real-ish timing-based
        mouse_event(MOUSEEVENTF_LEFTDOWN ||| MOUSEEVENTF_LEFTUP, 0, 0, 0u, 0)
    static member RightMouseClick() =
        mouse_event(MOUSEEVENTF_RIGHTDOWN ||| MOUSEEVENTF_RIGHTUP, 0, 0, 0u, 0)
    static member MiddleMouseClick() =
        mouse_event(MOUSEEVENTF_MIDDLEDOWN ||| MOUSEEVENTF_MIDDLEUP, 0, 0, 0u, 0)
    static member ScrollWheelRotateUp() =
        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, uint32 WHEEL_DELTA, 0)
    static member ScrollWheelRotateDown() =
        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, uint32 (-WHEEL_DELTA), 0)
    
    static member SetCursor(x,y) = 
        let transformedPoint = 
            if TrackerModelOptions.SmallerAppWindow.Value then 
                Point(x*TrackerModelOptions.SmallerAppWindowScaleFactor,y*TrackerModelOptions.SmallerAppWindowScaleFactor) 
            else Point(x,y)
        let pos = theWindow.PointToScreen(transformedPoint)
        SetCursorPos(int pos.X, int pos.Y) |> ignore

    static member SetSonar(enable) =
        SystemParametersInfo(SPI_SETMOUSESONAR, 0u, enable, SPIF_UPDATEINIFILE ||| SPIF_SENDCHANGE)

    static member DoSendInput(numberOfInputs, inputs, sizeOfInputStructure) =
        SendInput(numberOfInputs, inputs, sizeOfInputStructure)

let volumeChanged = new Event<int>()
let soundPlayer = new MediaPlayer()
soundPlayer.Volume <- float TrackerModelOptions.Volume / 300.
soundPlayer.Open(new Uri("confirm_speech.wav", UriKind.Relative))
let PlaySoundForSpeechRecognizedAndUsedToMark() =
    soundPlayer.Position <- TimeSpan(0L)
    soundPlayer.Play()
let soundPlayer2 = new MediaPlayer()
soundPlayer2.Volume <- float TrackerModelOptions.Volume / 300.
soundPlayer2.Open(new Uri("reminder_clink.wav", UriKind.Relative))
let PlaySoundForReminder() =
    soundPlayer2.Position <- TimeSpan(0L)
    soundPlayer2.Play()
volumeChanged.Publish.Add(fun v ->
    soundPlayer.Volume <- float v / 300.
    soundPlayer2.Volume <- float v / 300.
    )


let GetResourceStream(name) = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name)

let unparent(fe:FrameworkElement) =
    if fe.Parent<> null then
        (fe.Parent :?> Panel).Children.Remove(fe)

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
        let w = if cw = -1 then GridLength.Auto else GridLength(float cw)
        grid.ColumnDefinitions.Add(new ColumnDefinition(Width=w))
    for i = 0 to nr-1 do
        let h = if rh = -1 then GridLength.Auto else GridLength(float rh)
        grid.RowDefinitions.Add(new RowDefinition(Height=h))
    grid
let center(e, w, h) =
    let g = makeGrid(1, 1, w, h)
    gridAdd(g, e, 0, 0)
    g
let dock(e, dock) =
    let d = new DockPanel(LastChildFill=false)
    DockPanel.SetDock(e, dock)
    d.Children.Add(e) |> ignore
    d
let makeArrow(targetX, targetY, sourceX, sourceY, brush) =
    let tx,ty = targetX, targetY
    let sx,sy = sourceX, sourceY
    // line from source to target
    let line = new Shapes.Line(X1=sx, Y1=sy, X2=tx, Y2=ty, Stroke=brush, StrokeThickness=3.)
    line.StrokeDashArray <- new DoubleCollection(seq[5.;4.])
    let sq(x) = x*x
    let pct = 1. - 15./sqrt(sq(tx-sx)+sq(ty-sy))   // arrowhead base ideally 15 pixels down the line
    let pct = max pct 0.93                         // but at most 93% towards the target, for small lines
    let ax,ay = (tx-sx)*pct+sx, (ty-sy)*pct+sy
    // differential between target and arrowhead base
    let dx,dy = tx-ax, ty-ay
    // points orthogonal to the line from the base
    let p1x,p1y = ax+dy/2., ay-dx/2.
    let p2x,p2y = ax-dy/2., ay+dx/2.
    // triangle to make arrowhead
    let triangle = new Shapes.Polygon(Fill=brush)
    triangle.Points <- new PointCollection([Point(tx,ty); Point(p1x,p1y); Point(p2x,p2y)])
    line, triangle
let scaleUpCheckBoxBox(cb:CheckBox, scale) =
    cb.LayoutTransform <- new ScaleTransform(scale, scale)
    (cb.Content :?> FrameworkElement).LayoutTransform <- new ScaleTransform(1.0/scale, 1.0/scale)

let almostBlack = new SolidColorBrush(Color.FromRgb(30uy, 30uy, 30uy))
let makeButton(text, fontSizeOpt, fgOpt) =
    let tb = new TextBox(Text=text, IsReadOnly=true, IsHitTestVisible=false, TextAlignment=TextAlignment.Center, BorderThickness=Thickness(0.), Background=almostBlack)
    match fontSizeOpt with | None -> () | Some x -> tb.FontSize <- x
    match fgOpt with | None -> () | Some x -> tb.Foreground <- x
    let button = new Button(Content=tb, HorizontalContentAlignment=HorizontalAlignment.Stretch, VerticalContentAlignment=VerticalAlignment.Stretch)
    button

let makeColor(rgb) =
    let r = (rgb &&& 0xFF0000) / 0x10000
    let g = (rgb &&& 0x00FF00) / 0x100
    let b = (rgb &&& 0x0000FF) / 0x1
    Color.FromRgb(byte r, byte g, byte b)

let isBlackGoodContrast(rgb) =
    // https://gamedev.stackexchange.com/questions/38536/given-a-rgb-color-x-how-to-find-the-most-contrasting-color-y
    let r = (rgb &&& 0xFF0000) / 0x10000
    let g = (rgb &&& 0x00FF00) / 0x100
    let b = (rgb &&& 0x0000FF) / 0x1
    let r = float r / 255.
    let g = float g / 255.
    let b = float b / 255.
    let l = 0.2126 * r*r + 0.7152 * g*g + 0.0722 * b*b
    let use_black = l > 0.5*0.5
    use_black  // else use white

let transformColor(bmp:System.Drawing.Bitmap, f) =
    let r = new System.Drawing.Bitmap(bmp.Width,bmp.Height)
    for px = 0 to bmp.Width-1 do
        for py = 0 to bmp.Height-1 do
            let c = bmp.GetPixel(px,py)
            r.SetPixel(px, py, f c)
    r

let greyscale(bmp:System.Drawing.Bitmap) =
    transformColor(bmp, fun c -> 
        if c.ToArgb() = System.Drawing.Color.Transparent.ToArgb() then
            c
        else
            let avg = (int c.R + int c.G + int c.B) / 5  // not just average, but overall darker
            let avg = if avg = 0 then 0 else avg + 60    // never too dark
            System.Drawing.Color.FromArgb(avg, avg, avg)
        )
let desaturateColor(c:System.Drawing.Color, pct) =
    let f = pct   // 0.60 // desaturate by 60%
    let L = 0.3*float c.R + 0.6*float c.G + 0.1*float c.B
    let newR = float c.R + f * (L - float c.R)
    let newG = float c.G + f * (L - float c.G)
    let newB = float c.B + f * (L - float c.B)
    System.Drawing.Color.FromArgb(int newR, int newG, int newB)
let desaturate(bmp:System.Drawing.Bitmap, pct) = transformColor(bmp, (fun c -> desaturateColor(c,pct)))
let darkenImpl pct (bmp:System.Drawing.Bitmap) =
    let r = new System.Drawing.Bitmap(bmp.Width,bmp.Height)
    for px = 0 to bmp.Width-1 do
        for py = 0 to bmp.Height-1 do
            let c = bmp.GetPixel(px,py)
            let c = System.Drawing.Color.FromArgb(int(float c.R * pct), int(float c.G * pct), int(float c.B * pct))
            r.SetPixel(px, py, c)
    r
let darken(bmp:System.Drawing.Bitmap) =
    darkenImpl 0.5 bmp
let mediaColor(c:System.Drawing.Color) =
    Media.Color.FromArgb(c.A, c.R, c.G, c.B)

let BMPtoImage(bmp:System.Drawing.Bitmap) =
    let ms = new System.IO.MemoryStream()
    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png)  // must be png (not bmp) to save transparency info
    ms.Seek(0L, System.IO.SeekOrigin.Begin) |> ignore
    let bmimage = new System.Windows.Media.Imaging.BitmapImage()
    bmimage.BeginInit()
    
    // this is slower in practice
    //bmimage.CacheOption <- System.Windows.Media.Imaging.BitmapCacheOption.OnLoad    // can 'use' ms with this
    
    // this is faster in practice
    bmimage.CreateOptions <- System.Windows.Media.Imaging.BitmapCreateOptions.DelayCreation
    bmimage.CacheOption <- System.Windows.Media.Imaging.BitmapCacheOption.OnDemand    // must 'let' ms with this
    
    bmimage.StreamSource <- ms
    bmimage.EndInit()
    let i = new Image()
    i.Source <- bmimage
    i.Height <- float bmp.Height 
    i.Width <- float bmp.Width 
    i

let OMTW = 48.  // overworld map tile width - at normal aspect ratio, is 48 (16*3)
let green = new SolidColorBrush(mediaColor(desaturateColor(System.Drawing.Color.Lime, 0.50)))
let yellow = new SolidColorBrush(mediaColor(desaturateColor(System.Drawing.Color.Yellow, 0.50)))
let red = new SolidColorBrush(mediaColor(desaturateColor(System.Drawing.Color.Red, 0.50)))
let palegreen = new SolidColorBrush(mediaColor(desaturateColor(System.Drawing.Color.Lime, 0.65)))
let paleyellow = new SolidColorBrush(mediaColor(desaturateColor(System.Drawing.Color.Yellow, 0.65)))
let palered = new SolidColorBrush(mediaColor(desaturateColor(System.Drawing.Color.Red, 0.65)))
type TileHighlightRectangle() as this =
    let s = new Shapes.Rectangle(Width=OMTW,Height=11.*3.,Stroke=Brushes.Lime,StrokeThickness=3.,Opacity=1.0,IsHitTestVisible=false)
    let Draw(isPale) =
        s.Opacity <- 1.0
        s.StrokeThickness <- if isPale then 2.0 else 4.0
    do
        this.MakeGreen()
    member _this.MakeRed() = s.Stroke <- red; Draw(false)
    member _this.MakeYellow() = s.Stroke <- yellow; Draw(false)
    member _this.MakeBoldGreen() = s.Stroke <- green; Draw(false); s.StrokeThickness <- 6.0
    member _this.MakeGreen() = s.Stroke <- green; Draw(false)
    member _this.MakePaleRed() = s.Stroke <- palered; Draw(true)
    member _this.MakePaleYellow() = s.Stroke <- paleyellow; Draw(true)
    member _this.MakePaleGreen() = s.Stroke <- palegreen; Draw(true)
    member _this.Hide() = s.Opacity <- 0.0
    member _this.MakeGreenWithBriefAnimation() = 
        if TrackerModelOptions.AnimateShopHighlights.Value then
            Draw(false)
            let da = new System.Windows.Media.Animation.DoubleAnimation(12.0, 4.0, new Duration(System.TimeSpan.FromSeconds(0.5)))
            s.BeginAnimation(Shapes.Rectangle.StrokeThicknessProperty, da)
            let ca = new System.Windows.Media.Animation.ColorAnimation(From=Colors.Cyan, To=Colors.Lime, Duration=new Duration(System.TimeSpan.FromSeconds(0.5)))
            let brush = new SolidColorBrush()
            s.Stroke <- brush
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ca)
        else
            this.MakeGreen()
    member _this.MakeYellowWithBriefAnimation() = 
        if TrackerModelOptions.AnimateShopHighlights.Value then
            Draw(false)
            let da = new System.Windows.Media.Animation.DoubleAnimation(12.0, 4.0, new Duration(System.TimeSpan.FromSeconds(0.5)))
            s.BeginAnimation(Shapes.Rectangle.StrokeThicknessProperty, da)
            let ca = new System.Windows.Media.Animation.ColorAnimation(From=Colors.Cyan, To=Colors.Yellow, Duration=new Duration(System.TimeSpan.FromSeconds(0.5)))
            let brush = new SolidColorBrush()
            s.Stroke <- brush
            brush.BeginAnimation(SolidColorBrush.ColorProperty, ca)
        else
            this.MakeGreen()
    member _this.Shape = s


// see also
// https://stackoverflow.com/questions/63184765/wpf-left-click-and-drag
// https://stackoverflow.com/questions/12802122/wpf-handle-drag-and-drop-as-well-as-left-click
type DragDropSurface<'T>(surface:FrameworkElement, onStartDrag : _ * ('T -> unit) -> unit ) = // surface is the entire element we can drag across that encompasses all the multiple items and sees all mouse interactions in the area
    let mutable isDragging = false
    let mutable startPoint : Point option = None
    let mutable clickFunc = None
    let mutable initiateDragFunc = None
    do
        surface.PreviewMouseMove.Add(fun ea ->
            if (ea.LeftButton = System.Windows.Input.MouseButtonState.Pressed ||
                ea.MiddleButton = System.Windows.Input.MouseButtonState.Pressed ||
                ea.RightButton = System.Windows.Input.MouseButtonState.Pressed)
                    && not isDragging && startPoint.IsSome then
                let pos = ea.GetPosition(surface)
                // This is the windows setting for click v drag
                //     if Math.Abs(pos.X - startPoint.Value.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(pos.Y - startPoint.Value.Y) > SystemParameters.MinimumVerticalDragDistance then
                // However it defaults to just 4 pixels, and some folks were inadvertantly dragging when they wanted to click.
                // The only place setupClickVersusDrag() is called in for 'painting rooms' in dungeon UI.  You would always need to go across the distance of 1 door (12.0) to paint multiple rooms.
                // So make size that the threshold, for less accidental-drag behavior.
                if Math.Abs(pos.X - startPoint.Value.X) > 12.0 || Math.Abs(pos.Y - startPoint.Value.Y) > 12.0 then
                    //printfn "%A  -  %A" startPoint pos
                    isDragging <- true
                    onStartDrag(ea, initiateDragFunc.Value)
                    isDragging <- false
                    startPoint <- None
                    clickFunc <- None
                    initiateDragFunc <- None
            )
        surface.MouseUp.Add(fun ea ->
            //printfn "MouseUp"
            if not isDragging then
                if clickFunc.IsSome then
                    clickFunc.Value(ea)
                // else e.g. dragged into and released
            startPoint <- None
            clickFunc <- None
            initiateDragFunc <- None
            )
    member this.RegisterClickable(e:FrameworkElement, onClick, onInitiateDrag) =     // each individual element says how it should respond to a click, or to being an initiator of a drag
        e.PreviewMouseDown.Add(fun ea -> 
            //printfn "PrevMD %A" (Input.Mouse.GetPosition(surface))
            startPoint <- Some(ea.GetPosition(surface))
            clickFunc <- Some(onClick)
            initiateDragFunc <- Some(onInitiateDrag)
            )

////////////////////////////////////////////////////

let alphaNumBmp =
    let imageStream = GetResourceStream("alphanumerics3x5.png")
    new System.Drawing.Bitmap(imageStream)
let paintAlphanumerics3x5(ch, color, bmp:System.Drawing.Bitmap, x, y) =  // x and y are 1-pixel coordinates, even though bmp is blown up 3x
    let index =
        match ch with
        | '1' -> 0
        | '2' -> 1
        | '3' -> 2
        | '4' -> 3
        | '5' -> 4
        | '6' -> 5
        | '7' -> 6
        | '8' -> 7
        | '9' -> 8
        | '?' -> 9
        | 'A' -> 10
        | 'B' -> 11
        | 'C' -> 12
        | 'D' -> 13
        | 'E' -> 14
        | 'F' -> 15
        | 'G' -> 16
        | 'H' -> 17
        | 'S' -> 18 // sword
        | 'L' -> 19 // ladder
        | 'R' -> 20 // robot armos
        | 'T' -> 21 // T for take any
        | _ -> failwith "bad alphanumeric character to paint"
    for i = 0 to 2 do
        for j = 0 to 4 do
            if alphaNumBmp.GetPixel(index*3 + i, j).ToArgb() = System.Drawing.Color.Black.ToArgb() then
                // blow it up 3x
                for dx = 0 to 2 do
                    for dy = 0 to 2 do
                        bmp.SetPixel(3*(x+i)+dx, 3*(y+j)+dy, color)

let alphaNumOnTransparentBmp(ch, color, w:int, h:int, x, y) =
    let r = new System.Drawing.Bitmap(w,h)
    for px = 0 to w-1 do
        for py = 0 to h-1 do
            r.SetPixel(px, py, System.Drawing.Color.Transparent)
    paintAlphanumerics3x5(ch, color, r, x, y)
    r

let openCaveIconBmp = 
    let imageStream = GetResourceStream("open_cave20x20.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    bmp

let (boomerang_bmp, bow_bmp, magic_boomerang_bmp, raft_bmp, ladder_bmp, recorder_bmp, wand_bmp, red_candle_bmp, book_bmp, key_bmp, 
        silver_arrow_bmp, wood_arrow_bmp, red_ring_bmp, magic_shield_bmp, boom_book_bmp, 
        heart_container_bmp, power_bracelet_bmp, white_sword_bmp, ow_key_armos_bmp,
        brown_sword_bmp, magical_sword_bmp, blue_candle_bmp, blue_ring_bmp,
        ganon_bmp, zelda_bmp, bomb_bmp, bow_and_arrow_bmp, bait_bmp, question_marks_bmp, rupee_bmp, basement_stair_bmp) =
    let imageStream = GetResourceStream("icons7x7.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let a = [|  
        for i = 0 to bmp.Width/7 - 1 do
            let r = new System.Drawing.Bitmap(7*3,7*3)
            for px = 0 to 7*3-1 do
                for py = 0 to 7*3-1 do
                    let color = bmp.GetPixel(px/3 + i*7, py/3)
                    let color = if color.ToArgb() = System.Drawing.Color.Black.ToArgb() then System.Drawing.Color.Transparent else color
                    r.SetPixel(px, py, color)
            yield r
    |]
    (a.[0], a.[1], a.[2], a.[3], a.[4], a.[5], a.[6], a.[7], a.[8], a.[9],
        a.[10], a.[11], a.[12], a.[13], a.[14], a.[15], a.[16], a.[17], a.[18], a.[19],
        a.[20], a.[21], a.[22], a.[23], a.[24], a.[25], a.[26], a.[27], a.[28], a.[29], a.[30])

let bg16x16 = System.Drawing.Color.FromArgb(45, 50, 00)
let (digdogger_bmp, gleeok_bmp, gohma_bmp, manhandla_bmp, wizzrobe_bmp, patra_bmp, dodongo_bmp, red_bubble_bmp, blue_bubble_bmp, blue_darknut_bmp, other_monster_bmp, old_man_bmp) =
    let imageStream = GetResourceStream("zelda_bosses16x16.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let a = [|  
        for i = 0 to bmp.Width/16 - 1 do
            let r = new System.Drawing.Bitmap(18,18)  // border around it
            for px = 0 to 17 do
                for py = 0 to 17 do
                    if px=0 || px=17 || py=0 || py=17 then
                        r.SetPixel(px, py, bg16x16)
                    else
                        r.SetPixel(px, py, System.Drawing.Color.Black)
            for px = 0 to 15 do
                for py = 0 to 15 do
                    let color = bmp.GetPixel(px + i*16, py)
                    if color.ToArgb() = System.Drawing.Color.Black.ToArgb() then () else r.SetPixel(px+1, py+1, color)
            yield r
    |]
    (a.[0], a.[1], a.[2], a.[3], a.[4], a.[5], a.[6], a.[7], a.[8], a.[9], a.[10], a.[11])

let (zi_triforce_bmp, zi_heart_bmp, zi_bomb_bmp, zi_key_bmp, zi_fiver_bmp, zi_map_bmp, zi_compass_bmp, zi_other_item_bmp, zi_alt_bomb_bmp, zi_rock, zi_tree) =
    let imageStream = GetResourceStream("zelda_items16x16.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let a = [|  
        for i = 0 to bmp.Width/16 - 1 do
            let r = new System.Drawing.Bitmap(18,18)  // border around it
            for px = 0 to 17 do
                for py = 0 to 17 do
                    if px=0 || px=17 || py=0 || py=17 then
                        r.SetPixel(px, py, bg16x16)
                    else
                        r.SetPixel(px, py, System.Drawing.Color.Black)
            for px = 0 to 15 do
                for py = 0 to 15 do
                    let color = bmp.GetPixel(px + i*16, py)
                    if color.ToArgb() = System.Drawing.Color.Black.ToArgb() then () else r.SetPixel(px+1, py+1, color)
            yield r
    |]
    (a.[0], a.[1], a.[2], a.[3], a.[4], a.[5], a.[6], a.[7], a.[8], a.[9], a.[10])

let _brightTriforce_bmp, fullOrangeTriforce_bmp, _dullOrangeTriforce_bmp, greyTriforce_bmp, owHeartSkipped_bmp, owHeartEmpty_bmp, owHeartFull_bmp, iconRightArrow_bmp, iconCheckMark_bmp, iconExtras_bmp, iconDisk_bmp, owHeartTallFull_bmp = 
    let imageStream = GetResourceStream("icons10x10.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let all = [|
        for i = 0 to bmp.Width/10 - 1 do
            let r = new System.Drawing.Bitmap(10*3,10*3)
            for px = 0 to 10*3-1 do
                for py = 0 to 10*3-1 do
                    let color = bmp.GetPixel(px/3 + i*10, py/3)
                    let color = if color.ToArgb() = System.Drawing.Color.White.ToArgb() then System.Drawing.Color.Transparent else color
                    r.SetPixel(px, py, color)
            yield r
        |]
    transformColor(all.[1], (fun c -> if c.ToArgb() <> System.Drawing.Color.Transparent.ToArgb() then System.Drawing.Color.LightGray else c)), 
        all.[1],
        transformColor(all.[1], (fun c -> if c.ToArgb() <> System.Drawing.Color.Transparent.ToArgb() then desaturateColor(c, 0.25) else c)), 
        all.[0], all.[2], all.[3], all.[4], all.[5], all.[6], all.[7], all.[8], all.[9]
let UNFOUND_NUMERAL_COLOR = System.Drawing.Color.FromArgb(0x77,0x77,0x99)
let FOUND_NUMERAL_COLOR = System.Drawing.Color.White
let heartFromTakeAny_bmp, heartFromWhiteSwordCave_bmp, heartFromCoast_bmp, heartFromArmos_bmp, heartFromNumberedDungeon_bmps, heartFromLetteredDungeon_bmps =
    let LABEL = System.Drawing.Color.FromArgb(0xFF,0xFF,0xFF)
    let paint(ch) = 
        let bmp = owHeartTallFull_bmp.Clone() :?> System.Drawing.Bitmap
        paintAlphanumerics3x5(ch, LABEL, bmp, 4, 2)
        bmp
    let a = [|
        for ch in ["123456789"; "ABCDEFGH9"] do
            yield [|
            for i = 0 to 8 do
                yield paint(ch.Chars(i))
            |] |]
    paint('T'), paint('S'), paint('L'), paint('R'), a.[0], a.[1]
let emptyUnfoundNumberedTriforce_bmps, emptyUnfoundLetteredTriforce_bmps = 
    let a = [|
        for ch in ['1'; 'A'] do
            yield [|
            for i = 0 to 7 do
                let bmp = greyTriforce_bmp.Clone() :?> System.Drawing.Bitmap
                paintAlphanumerics3x5(char(int ch + i), UNFOUND_NUMERAL_COLOR, bmp, 4, 4)
                yield bmp
            |] |]
    a.[0], a.[1]
let emptyFoundNumberedTriforce_bmps, emptyFoundLetteredTriforce_bmps = 
    let a = [|
        for ch in ['1'; 'A'] do
            yield [|
            for i = 0 to 7 do
                let bmp = greyTriforce_bmp.Clone() :?> System.Drawing.Bitmap
                paintAlphanumerics3x5(char(int ch + i), FOUND_NUMERAL_COLOR, bmp, 4, 4)
                yield bmp
            |] |]
    a.[0], a.[1]
let fullNumberedUnfoundTriforce_bmps, fullNumberedFoundTriforce_bmps, fullLetteredUnfoundTriforce_bmps, fullLetteredFoundTriforce_bmps =
    let a = [|
        for ch in ['1'; 'A'] do
            yield [|
            for i = 0 to 7 do
                let bmp = fullOrangeTriforce_bmp.Clone() :?> System.Drawing.Bitmap
                paintAlphanumerics3x5(char(int ch + i), UNFOUND_NUMERAL_COLOR, bmp, 4, 4)
                yield bmp
            |]
            yield [|
            for i = 0 to 7 do
                let bmp = fullOrangeTriforce_bmp.Clone() :?> System.Drawing.Bitmap
                paintAlphanumerics3x5(char(int ch + i), FOUND_NUMERAL_COLOR, bmp, 4, 4)
                yield bmp
            |]
        |]
    a.[0], a.[1], a.[2], a.[3]
let unfoundL9_bmp,foundL9_bmp =
    let unfoundTriforceColor = emptyUnfoundNumberedTriforce_bmps.[0].GetPixel(15, 28)
    let u = new System.Drawing.Bitmap(10*3,10*3)
    let f = new System.Drawing.Bitmap(10*3,10*3)
    // make a rectangle 'shield' to draw the 9 on, so that a hint halo does not wash out the '9'
    for i = 3*3 to 8*3-1 do
        for j = 3*3 to 10*3-1 do
            u.SetPixel(i,j,unfoundTriforceColor)
            f.SetPixel(i,j,unfoundTriforceColor)
    paintAlphanumerics3x5('9', UNFOUND_NUMERAL_COLOR, u, 4, 4)
    paintAlphanumerics3x5('9', FOUND_NUMERAL_COLOR, f, 4, 4)
    u, f

let fairy_bmp =
    let imageStream = GetResourceStream("icons8x16.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let r = new System.Drawing.Bitmap(16,32)
    for x = 0 to 15 do
        for y = 0 to 31 do
            let c = bmp.GetPixel(x/2, y/2)
            if c.ToArgb() <> System.Drawing.Color.Black.ToArgb() then
                r.SetPixel(x, y, c)
    r

let dungeonRoomBmpPairs =
    let imageStream = GetResourceStream("new_icons13x9.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let a = [|  
        for i = 0 to bmp.Width/13 - 1 do
            let cr = new System.Drawing.Bitmap(13*3,9*3)
            let ur = new System.Drawing.Bitmap(13*3,9*3)
            for px = 0 to 13*3-1 do
                for py = 0 to 9*3-1 do
                    ur.SetPixel(px, py, bmp.GetPixel(px/3 + i*13,   py/3))
                    cr.SetPixel(px, py, bmp.GetPixel(px/3 + i*13, 9+py/3))
            yield (ur,cr)
    |]
    a
(*
let dungeonRoomFloorDrops, dungeonRoomMonsters =
    let imageStream = GetResourceStream("icons3x3.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let a = [|  
        for i = 0 to bmp.Width/3 - 1 do
            let r = new System.Drawing.Bitmap(5*3,5*3)
            let b = new System.Drawing.Bitmap(8*3,8*3)
            // black border around them
            for x = 0 to 14 do
                for y = 0 to 14 do
                    r.SetPixel(x,y,System.Drawing.Color.Black)
            for x = 0 to 23 do
                for y = 0 to 23 do
                    b.SetPixel(x,y,System.Drawing.Color.Black)
            // the icon in the middle
            for px = 0 to 3*3-1 do
                for py = 0 to 3*3-1 do
                    r.SetPixel(px+3, py+3, bmp.GetPixel(px/3 + i*3,   py/3))
                    b.SetPixel(2*px+3, 2*py+3, bmp.GetPixel(px/3 + i*3,   py/3))
                    b.SetPixel(2*px+1+3, 2*py+3, bmp.GetPixel(px/3 + i*3,   py/3))
                    b.SetPixel(2*px+3, 2*py+1+3, bmp.GetPixel(px/3 + i*3,   py/3))
                    b.SetPixel(2*px+1+3, 2*py+1+3, bmp.GetPixel(px/3 + i*3,   py/3))
            yield r, b
    |]
    a.[0..7], a.[8..]
*)

let overworldImage =
    let files = [|
//        "s_map_overworld_strip8.png"
        "s_map_overworld_vanilla_strip8.png"
//        "s_map_overworld_zones_strip8.png"
        |]
    let file = files.[(new System.Random()).Next(files.Length)]
//    printfn "selecting overworld file %s" file
    let imageStream = GetResourceStream(file)
    new System.Drawing.Bitmap(imageStream)
let zhMapIcons =
    let imageStream = GetResourceStream("s_icon_overworld_strip39.png")
    new System.Drawing.Bitmap(imageStream)
let zhDungeonIcons =
    let imageStream = GetResourceStream("s_btn_tr_dungeon_cell_strip3.png")
    new System.Drawing.Bitmap(imageStream)
let zhDungeonNums =
    let imageStream = GetResourceStream("s_btn_tr_dungeon_num_strip18.png")
    new System.Drawing.Bitmap(imageStream)


let allItemBMPs = [| book_bmp; boomerang_bmp; bow_bmp; power_bracelet_bmp; ladder_bmp; magic_boomerang_bmp; key_bmp; raft_bmp; recorder_bmp; red_candle_bmp; red_ring_bmp; silver_arrow_bmp; wand_bmp; white_sword_bmp |]
let allItemBMPsWithHeartShuffle = [| yield! allItemBMPs; for _i = 0 to 8 do yield heart_container_bmp |]

let readCacheFileOrCreateBmp(filename, createF : unit -> System.Drawing.Bitmap) =
    if System.IO.File.Exists(filename) then
        System.Drawing.Bitmap.FromFile(filename) :?> System.Drawing.Bitmap
    else
        let bmp = createF()
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filename)) |> ignore
        bmp.Save(filename)
        bmp

let mutable alternativeOverworldMapFilename, shouldInitiallyHideOverworldMap = "", false   // startup screen can set these
let blankTileBmp =
    let fullTileBmp = new System.Drawing.Bitmap(16*3,11*3)
    let main = System.Drawing.Color.DarkSlateGray
    let alt = System.Drawing.Color.FromArgb(int main.R + 48, int main.G + 48, int main.B + 48)
    for px = 0 to 16*3-1 do
        for py = 0 to 11*3-1 do
            if px >= 15*3 || py >= 10*3 then
                fullTileBmp.SetPixel(px, py, System.Drawing.Color.Black)
            else
                fullTileBmp.SetPixel(px, py, (if (px/3+py/3)%2=0 then main else alt))
    fullTileBmp
let overworldMapBMPs(n) =
    let m = overworldImage
    let tiles = Array2D.zeroCreate 16 8
    if n=4 && not(System.String.IsNullOrEmpty(alternativeOverworldMapFilename)) then
        let image = new System.Drawing.Bitmap(alternativeOverworldMapFilename) :> System.Drawing.Image
        let bitmap = new System.Drawing.Bitmap( image, new System.Drawing.Size( 256*3, 88*3 ) )
        for x = 0 to 15 do
            for y = 0 to 7 do
                let tile = new System.Drawing.Bitmap(16*3,11*3)
                for px = 0 to 16*3-1 do
                    for py = 0 to 11*3-1 do
                        tile.SetPixel(px, py, bitmap.GetPixel((x*16*3 + px), (y*11*3 + py)))
                tiles.[x,y] <- tile
    else
        for x = 0 to 15 do
            for y = 0 to 7 do
                let tile = 
                    let filename = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, sprintf """Overworld\quest.%d.ow.%2d.%2d.bmp""" n x y)
                    readCacheFileOrCreateBmp(filename, fun () ->
                        let tile = new System.Drawing.Bitmap(16*3,11*3)
                        for px = 0 to 16*3-1 do
                            for py = 0 to 11*3-1 do
                                tile.SetPixel(px, py, m.GetPixel(256*n + (x*16*3 + px)/3, (y*11*3 + py)/3))
                        tile)
                tiles.[x,y] <- tile
    tiles

let TRANS_BG = System.Drawing.Color.FromArgb(1, System.Drawing.Color.Black)  // transparent background (will be darkened in program layer)
let itemBackgroundColor = System.Drawing.Color.FromArgb(0xEF,0x83,0)
let itemsBMP = 
    let imageStream = GetResourceStream("icons3x7.png")
    new System.Drawing.Bitmap(imageStream)

let genericDungeonInterior_bmp =
    let bmp = new System.Drawing.Bitmap(5*3,9*3)
    for px = 0 to 5*3-1 do
        for py = 0 to 9*3-1 do
            bmp.SetPixel(px, py, System.Drawing.Color.Yellow)
    paintAlphanumerics3x5('?', System.Drawing.Color.Black, bmp, 1, 2)
    bmp

// each overworld map tile may have multiple icons that can represent it (e.g. dungeon 1 versus dungeon A)
// we store a table, where the array index is the mapSquareChoiceDomain index of the general entry type, and the value there is a list of all possible icons
// MapStateProxy will eventually be responsible for 'decoding' the current tracker state into the appropriate icon
let theInteriorBmpTable = Array.init (TrackerModel.dummyOverworldTiles.Length) (fun _ -> ResizeArray())
do
    let imageStream = GetResourceStream("ow_icons5x9.png")
    let interiorIconStrip = new System.Drawing.Bitmap(imageStream)
    let darkxbmp = new System.Drawing.Bitmap(5*3,9*3)
    for px = 0 to 5*3-1 do
        for py = 0 to 9*3-1 do
            darkxbmp.SetPixel(px, py, System.Drawing.Color.Black)
    let getInteriorIconFromStrip(i) = 
        let bmp = new System.Drawing.Bitmap(5*3,9*3)
        for px = 0 to 5*3-1 do
            for py = 0 to 9*3-1 do
                bmp.SetPixel(px, py, interiorIconStrip.GetPixel(i*5+px/3, py/3))
        bmp
    // 0-8  dungeons: 4 varieties (numbered yellow, numbered green, lettered yellow, lettered green)
    for labels in ["123456789";"ABCDEFGH9"] do
        for color in [System.Drawing.Color.Yellow; System.Drawing.Color.Lime] do
            labels |> Seq.iteri (fun i ch ->
                let bmp = new System.Drawing.Bitmap(5*3,9*3)
                for px = 0 to 5*3-1 do
                    for py = 0 to 9*3-1 do
                        bmp.SetPixel(px, py, color)
                paintAlphanumerics3x5(ch, System.Drawing.Color.Black, bmp, 1, 2)
                theInteriorBmpTable.[i].Add(bmp)
                )
    // 9-12  any roads
    "1234" |> Seq.iteri (fun i ch ->
        let bmp = new System.Drawing.Bitmap(5*3,9*3)
        for px = 0 to 5*3-1 do
            for py = 0 to 9*3-1 do
                bmp.SetPixel(px, py, System.Drawing.Color.Orchid)
        paintAlphanumerics3x5(ch, System.Drawing.Color.Black, bmp, 1, 2)
        theInteriorBmpTable.[i+9].Add(bmp)
        )
    // 13  sword3
    theInteriorBmpTable.[13].Add(getInteriorIconFromStrip(0))
    theInteriorBmpTable.[13].Add(getInteriorIconFromStrip(0) |> darkenImpl 0.6)
    // 14  sword2
    theInteriorBmpTable.[14].Add(getInteriorIconFromStrip(1))
    theInteriorBmpTable.[14].Add(getInteriorIconFromStrip(1) |> darkenImpl 0.6)
    // 15  sword1
    theInteriorBmpTable.[15].Add(getInteriorIconFromStrip(2))
    theInteriorBmpTable.[15].Add(getInteriorIconFromStrip(2) |> darkenImpl 0.6)
    // 16-23  item shops (as single-item icons)
    for i = 0 to TrackerModel.MapSquareChoiceDomainHelper.NUM_ITEMS-1 do
        let bmp = new System.Drawing.Bitmap(5*3,9*3)
        for px = 0 to 5*3-1 do
            for py = 0 to 9*3-1 do
                bmp.SetPixel(px, py, itemBackgroundColor)
                if px/3 >= 1 && px/3 <= 3 && py/3 >= 1 && py/3 <= 7 then
                    let c = itemsBMP.GetPixel(i*3 + px/3-1, py/3-1)
                    if c.ToArgb() <> System.Drawing.Color.Black.ToArgb() then
                        bmp.SetPixel(px, py, c)
        theInteriorBmpTable.[i+16].Add(bmp)
    // 24  unknown money secret
    theInteriorBmpTable.[24].Add(getInteriorIconFromStrip(11))
    // 25  large secret
    theInteriorBmpTable.[25].Add(getInteriorIconFromStrip(8))
    theInteriorBmpTable.[25].Add(getInteriorIconFromStrip(8) |> darken)
    // 26  medium secret
    theInteriorBmpTable.[26].Add(getInteriorIconFromStrip(6))
    theInteriorBmpTable.[26].Add(getInteriorIconFromStrip(6) |> darken)
    // 27  small secret
    theInteriorBmpTable.[27].Add(getInteriorIconFromStrip(9))
    theInteriorBmpTable.[27].Add(getInteriorIconFromStrip(9) |> darken)
    // 28  door repair charge
    theInteriorBmpTable.[28].Add(getInteriorIconFromStrip(12))
    theInteriorBmpTable.[28].Add(getInteriorIconFromStrip(12) |> darkenImpl 0.7)
    // 29  money making game
    theInteriorBmpTable.[29].Add(getInteriorIconFromStrip(10))
    // 30  letter
    theInteriorBmpTable.[30].Add(getInteriorIconFromStrip(13))
    theInteriorBmpTable.[30].Add(getInteriorIconFromStrip(13) |> darkenImpl 0.7)
    // 31  armos
    theInteriorBmpTable.[31].Add(getInteriorIconFromStrip(7))
    theInteriorBmpTable.[31].Add(getInteriorIconFromStrip(7) |> darken)
    // 32  hint shop
    theInteriorBmpTable.[32].Add(getInteriorIconFromStrip(3))
    theInteriorBmpTable.[32].Add(getInteriorIconFromStrip(3) |> darkenImpl 0.7)
    // 33  take any
    theInteriorBmpTable.[33].Add(getInteriorIconFromStrip(4))
    theInteriorBmpTable.[33].Add(getInteriorIconFromStrip(4) |> darken)
    // 34  potion shop
    theInteriorBmpTable.[34].Add(getInteriorIconFromStrip(5))
    // 35  'X'
    theInteriorBmpTable.[35].Add(darkxbmp)
// full tiles just have interior bmp in the center and transparent pixels all around (except for the final 'X' one)
let theFullTileBmpTable = Array.init theInteriorBmpTable.Length (fun _ -> ResizeArray())
do
    for i = 0 to theInteriorBmpTable.Length-1 do
        for interiorBmp in theInteriorBmpTable.[i] do
            let fullTileBmp = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    if px>=5*3 && px<10*3 && py>=1*3 && py<10*3 then 
                        fullTileBmp.SetPixel(px, py, interiorBmp.GetPixel(px-5*3, py-1*3))
                    else
                        fullTileBmp.SetPixel(px, py, if i=theInteriorBmpTable.Length-1 then System.Drawing.Color.Black else TRANS_BG)
            theFullTileBmpTable.[i].Add(fullTileBmp)
            
let linkFaceForward_bmp,linkRunRight_bmp,linkFaceRight_bmp,linkGotTheThing_bmp =
    let imageStream = GetResourceStream("link_icons.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let a = [|
        for i = 0 to 3 do
            let r = new System.Drawing.Bitmap(16, 16)
            for x = 0 to 15 do
                for y = 0 to 15 do
                    let color = bmp.GetPixel(16*i+x, y)
                    let color = if color.ToArgb() = System.Drawing.Color.Black.ToArgb() then System.Drawing.Color.Transparent else color
                    r.SetPixel(x, y, color)
            yield r
        |]
    a.[0], a.[1], a.[2], a.[3]
    
let loadBMP(filename) = 
    let imageStream = GetResourceStream(filename)
    let bmp = new System.Drawing.Bitmap(imageStream)
    bmp
let mouseIconButtonColorsBMP = loadBMP("mouse-icon-button-colors.png")
let mouseIconButtonColors2BMP = loadBMP("mouse-icon-button-colors-2.png")


let takeAnyPotionBMP = loadBMP("take-any-potion.png")
let takeAnyCandleBMP = loadBMP("take-any-candle.png")
let takeAnyHeartBMP = loadBMP("take-any-heart.png")
let takeAnyLeaveBMP = loadBMP("take-any-leave.png")
let takeThisWoodSwordBMP = loadBMP("take-this-wood-sword.png")
let takeThisCandleBMP = loadBMP("take-this-candle.png")
let takeThisLeaveBMP = loadBMP("take-this-leave.png")

let firstQuestItemReferenceBMP = loadBMP("first-quest-item-reference.png")
let secondQuestItemReferenceBMP = loadBMP("second-quest-item-reference.png")
let mirrorOverworldBMP = loadBMP("mirror-overworld.png")

(*
code for clipping screenshots to same size

let clipTakeAny(bmp:System.Drawing.Bitmap) =
    let r = new System.Drawing.Bitmap(672, 448)
    for x = 0 to 671 do
        for y = 0 to 447 do
            r.SetPixel(x,y,bmp.GetPixel(x+48, y+45))
    r
let takeAnyPotionBMP = loadBMP("take-any-potion.png") |> clipTakeAny
let takeAnyCandleBMP = loadBMP("take-any-candle.png") |> clipTakeAny
let takeAnyHeartBMP = loadBMP("take-any-heart.png") |> clipTakeAny
let takeAnyLeaveBMP = loadBMP("take-any-leave.png") |> clipTakeAny
let takeThisWoodSwordBMP = loadBMP("take-this-wood-sword.png") |> clipTakeAny
let takeThisCandleBMP = loadBMP("take-this-candle.png") |> clipTakeAny
let takeThisLeaveBMP = loadBMP("take-this-leave.png") |> clipTakeAny

do
    takeAnyPotionBMP.Save("takeAnyPotionBMP.png")
    takeAnyCandleBMP.Save("takeAnyCandleBMP.png")
    takeAnyHeartBMP.Save("takeAnyHeartBMP.png")
    takeAnyLeaveBMP.Save("takeAnyLeaveBMP.png")
    takeThisWoodSwordBMP.Save("takeThisWoodSwordBMP.png")
    takeThisCandleBMP.Save("takeThisCandleBMP.png")
    takeThisLeaveBMP.Save("takeThisLeaveBMP.png")
*)

let overworldCommonestFloorColorBrush = new SolidColorBrush(Color.FromRgb(204uy,176uy,136uy))
(*
do
    let imageStream = GetResourceStream("icons3x7.png")
    let bmp37 = new System.Drawing.Bitmap(imageStream)
    let imageStream = GetResourceStream("icons7x7.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let newBmp = new System.Drawing.Bitmap(bmp.Width+7, bmp.Height)
    for i = 0 to bmp.Width-1 do
        for j = 0 to bmp.Height-1 do
            newBmp.SetPixel(i,j,bmp.GetPixel(i,j))
    for i = 0 to 6 do
        for j = 0 to 6 do
            newBmp.SetPixel(bmp.Width+i, j, Drawing.Color.Transparent)
    for i = 0 to 2 do
        for j = 0 to 6 do
            newBmp.SetPixel(bmp.Width+i+2, j, bmp37.GetPixel(3*5+i,j))
    newBmp.Save("""C:\Users\Admin1\Source\Repos\Zelda1RandoTools\Z1R_Tracker\Z1R_WPF\tmp.png""")

do
    let imageStream = GetResourceStream("icons7x7.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let newBmp = new System.Drawing.Bitmap(bmp.Width, bmp.Height)
    for i = 0 to bmp.Width-1 do
        for j = 0 to bmp.Height-1 do
            let c = bmp.GetPixel(i,j)
            if c.A <> 0xFFuy then
                newBmp.SetPixel(i,j,System.Drawing.Color.Black)
            else
                newBmp.SetPixel(i,j,c)
    newBmp.Save("""C:\Users\Admin1\Source\Repos\Zelda1RandoTools\Z1R_Tracker\Z1R_WPF\tmp.png""")
*)

let swordLevelToBmp(swordLevel) =
    match swordLevel with
    | 0 -> greyscale magical_sword_bmp
    | 1 -> brown_sword_bmp
    | 2 -> white_sword_bmp
    | 3 -> magical_sword_bmp
    | _ -> failwith "bad SwordLevel"
let ringLevelToBmp(ringLevel) =
    match ringLevel with
    | 0 -> greyscale red_ring_bmp
    | 1 -> blue_ring_bmp
    | 2 -> red_ring_bmp
    | _ -> failwith "bad RingLevel"

let blockers_gsc = new GradientStopCollection([new GradientStop(Color.FromArgb(255uy, 0uy, 180uy, 0uy), 0.)
                                               new GradientStop(Color.FromArgb(255uy, 40uy, 40uy, 40uy), 0.3)
                                               new GradientStop(Color.FromArgb(255uy, 40uy, 40uy, 40uy), 0.7)
                                               new GradientStop(Color.FromArgb(255uy, 180uy, 0uy, 0uy), 1.0)
                                               ])
let blockers_maybeBG = new LinearGradientBrush(blockers_gsc, Point(0.,0.), Point(1.,1.))
let blockerHardCanonicalBMP(current) = 
    match current with
    | TrackerModel.DungeonBlocker.COMBAT -> white_sword_bmp
    | TrackerModel.DungeonBlocker.BOW_AND_ARROW | TrackerModel.DungeonBlocker.MAYBE_BOW_AND_ARROW -> bow_and_arrow_bmp
    | TrackerModel.DungeonBlocker.RECORDER | TrackerModel.DungeonBlocker.MAYBE_RECORDER -> recorder_bmp
    | TrackerModel.DungeonBlocker.LADDER | TrackerModel.DungeonBlocker.MAYBE_LADDER -> ladder_bmp
    | TrackerModel.DungeonBlocker.BAIT | TrackerModel.DungeonBlocker.MAYBE_BAIT -> bait_bmp
    | TrackerModel.DungeonBlocker.KEY | TrackerModel.DungeonBlocker.MAYBE_KEY -> key_bmp
    | TrackerModel.DungeonBlocker.BOMB | TrackerModel.DungeonBlocker.MAYBE_BOMB -> bomb_bmp
    | TrackerModel.DungeonBlocker.MONEY | TrackerModel.DungeonBlocker.MAYBE_MONEY -> rupee_bmp
    | TrackerModel.DungeonBlocker.NOTHING -> null
let blockerCurrentDisplay(current) =
    let innerc = new Canvas(Width=24., Height=24., Background=Brushes.Transparent, IsHitTestVisible=false)  // just has item drawn on it, not the box
    innerc.Children.Clear()
    innerc.Background <- match current with
                            | TrackerModel.DungeonBlocker.MAYBE_LADDER
                            | TrackerModel.DungeonBlocker.MAYBE_BAIT
                            | TrackerModel.DungeonBlocker.MAYBE_BOMB
                            | TrackerModel.DungeonBlocker.MAYBE_BOW_AND_ARROW
                            | TrackerModel.DungeonBlocker.MAYBE_KEY
                            | TrackerModel.DungeonBlocker.MAYBE_MONEY
                            | TrackerModel.DungeonBlocker.MAYBE_RECORDER                            
                                -> blockers_maybeBG :> Brush
                            | _ -> Brushes.Black :> Brush
    let bmp = blockerHardCanonicalBMP(current)
    if bmp <> null then
        let image = BMPtoImage(bmp)
        image.IsHitTestVisible <- false
        canvasAdd(innerc, image, 1., 1.)
    innerc

let skipped = Brushes.MediumPurple
let placeSkippedItemXDecorationImpl(innerc:Canvas, size) =
    innerc.Children.Add(new Shapes.Line(Stroke=skipped, StrokeThickness=3., X1=0., Y1=0., X2=size, Y2=size)) |> ignore
    innerc.Children.Add(new Shapes.Line(Stroke=skipped, StrokeThickness=3., X1=size, Y1=0., X2=0., Y2=size)) |> ignore
let placeSkippedItemXDecoration(innerc) = placeSkippedItemXDecorationImpl(innerc, 30.)

let WarpMouseCursorTo(pos:Point) =
    Win32.SetCursor(pos.X, pos.Y)
    PlaySoundForSpeechRecognizedAndUsedToMark()
let SilentlyWarpMouseCursorTo(pos:Point) =
    Win32.SetCursor(pos.X, pos.Y)
let NavigationallyWarpMouseCursorTo(pos:Point) =   // can abstract over whether keyboard 'arrow' keys play the sound or not
    SilentlyWarpMouseCursorTo(pos)
    (*
    Win32.SetSonar(true) |> ignore
    do
        // Press and release Ctrl
        let VK_CONTROL = 0x11us
        let KEYEVENTF_KEYDOWN = 0x0u
        let KEYEVENTF_KEYUP = 0x2u
        let ipa = Array.create 1 (LPINPUT())
        ipa.[0].``type`` <- 1 // INPUT_KEYBOARD
        ipa.[0].u.ki <- KEYBDINPUT(VK_CONTROL, 0us, KEYEVENTF_KEYDOWN, 0u, UIntPtr(0u))
        Win32.DoSendInput(1, ipa, sizeof<LPINPUT>) |> ignore
        ipa.[0].u.ki <- KEYBDINPUT(VK_CONTROL, 0us, KEYEVENTF_KEYUP, 0u, UIntPtr(0u))
        Win32.DoSendInput(1, ipa, sizeof<LPINPUT>) |> ignore
    Win32.SetSonar(false) |> ignore
    *)
