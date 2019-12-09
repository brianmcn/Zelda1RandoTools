open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media


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
    for i = 0 to nc do
        grid.ColumnDefinitions.Add(new ColumnDefinition(Width=GridLength(float cw)))
    for i = 0 to nr do
        grid.RowDefinitions.Add(new RowDefinition(Height=GridLength(float rh)))
    grid

type ItemState() =
    let mutable state = -1
    member this.Current() =
        if state = -1 then
            null
        else
            Graphics.allItems.[state]
    member private this.Impl(forward) = 
        if forward then 
            state <- state + 1
        else
            state <- state - 1
        if state < -1 then
            state <- Graphics.allItems.Length-1
        if state >= Graphics.allItems.Length then
            state <- -1
        if state <> -1 && Graphics.allItems.[state].Parent <> null then
            if forward then this.Next() else this.Prev()
        elif state = -1 then
            null
        else
            Graphics.allItems.[state]
    member this.Next() = this.Impl(true)
    member this.Prev() = this.Impl(false)

type MapState() =
    let mutable state = -1
    let U = Graphics.uniqueMapIcons.Length 
    let NU = Graphics.nonUniqueMapIconBMPs.Length
    member this.IsUnique = state >= 0 && state < U
    member this.IsDungeon = state >= 0 && state < 9
    member this.IsWarp = state >= 9 && state < 13
    member this.Current() =
        if state = -1 then
            null
        elif state < U then
            Graphics.uniqueMapIcons.[state]
        else
            Graphics.BMPtoImage Graphics.nonUniqueMapIconBMPs.[state-U]
    member private this.Impl(forward) = 
        if forward then 
            state <- state + 1
        else
            state <- state - 1
        if state < -1 then
            state <- U+NU-1
        if state >= U+NU then
            state <- -1
        if state >=0 && state < U && Graphics.uniqueMapIcons.[state].Parent <> null then
            if forward then this.Next() else this.Prev()
        else this.Current()
    member this.Next() = this.Impl(true)
    member this.Prev() = this.Impl(false)

let H = 30
let makeAll() =
    let c = new Canvas()
    c.Height <- float(30*4 + 11*3*8)
    c.Width <- float(16*16*3)

    c.Background <- System.Windows.Media.Brushes.Black 

    let mainTracker = makeGrid(9, 4, H, H)
    canvasAdd(c, mainTracker, 0., 0.)

    // triforce
    for i = 0 to 7 do
        let image = Graphics.emptyTriforces.[i]
        let c = new Canvas(Width=30., Height=30.)
        canvasAdd(c, image, 0., 0.)
        c.MouseDown.Add(fun _ -> 
            if c.Children.Contains(Graphics.emptyTriforces.[i]) then 
                c.Children.Clear(); c.Children.Add(Graphics.fullTriforces.[i]) |> ignore else 
                c.Children.Clear(); c.Children.Add(Graphics.emptyTriforces.[i]) |> ignore)
        gridAdd(mainTracker, c, i, 0)
    // floor hearts
    for i = 0 to 7 do
        let image = Graphics.emptyHearts.[i]
        let c = new Canvas(Width=30., Height=30.)
        canvasAdd(c, image, 0., 0.)
        c.MouseDown.Add(fun _ -> 
            if c.Children.Contains(Graphics.emptyHearts.[i]) then 
                c.Children.Clear(); c.Children.Add(Graphics.fullHearts.[i]) |> ignore else 
                c.Children.Clear(); c.Children.Add(Graphics.emptyHearts.[i]) |> ignore)
        gridAdd(mainTracker, c, i, 1)

    let boxItem() = 
        let c = new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black)
        let no = System.Windows.Media.Brushes.DarkRed
        let yes = System.Windows.Media.Brushes.LimeGreen 
        let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., Stroke=no)
        rect.StrokeThickness <- 3.0
        c.Children.Add(rect) |> ignore
        let is = new ItemState()
        c.MouseLeftButtonDown.Add(fun _ ->
            if obj.Equals(rect.Stroke, no) then
                rect.Stroke <- yes
            else
                rect.Stroke <- no)
        // item
        c.MouseWheel.Add(fun x -> 
            c.Children.Remove(is.Current())
            canvasAdd(c, (if x.Delta<0 then is.Next() else is.Prev()), 4., 4.)
            )
        c
    // items
    for i = 0 to 8 do
        for j = 0 to 1 do
            if j=0 || (i=0 || i=7 || i=8) then
                gridAdd(mainTracker, boxItem(), i, j+2)

    let OFFSET = 400.
    // ow hearts
    let owHeartGrid = makeGrid(4, 1, 30, 30)
    for i = 0 to 3 do
        let c = new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black)
        let image = Graphics.owHeartsEmpty.[i]
        canvasAdd(c, image, 0., 0.)
        c.MouseWheel.Add(fun x -> 
            let cur = 
                if c.Children.Contains(Graphics.owHeartsEmpty.[i]) then 0
                elif c.Children.Contains(Graphics.owHeartsFull.[i]) then 1
                else 2
            c.Children.Clear()
            let next = (cur + (if x.Delta<0 then 1 else -1) + 3) % 3
            canvasAdd(c, (if next = 0 then Graphics.owHeartsEmpty.[i] elif next = 1 then Graphics.owHeartsFull.[i] else Graphics.owHeartsSkipped.[i]), 0., 0.)
            )
        gridAdd(owHeartGrid, c, i, 0)
    canvasAdd(c, owHeartGrid, OFFSET, 0.)
    // ladder, armos, white sword items
    let owItemGrid = makeGrid(2, 3, 30, 30)
    gridAdd(owItemGrid, Graphics.ow_key_ladder, 0, 0)
    gridAdd(owItemGrid, Graphics.ow_key_armos, 0, 1)
    gridAdd(owItemGrid, Graphics.ow_key_white_sword, 0, 2)
    gridAdd(owItemGrid, boxItem(), 1, 0)
    gridAdd(owItemGrid, boxItem(), 1, 1)
    gridAdd(owItemGrid, boxItem(), 1, 2)
    canvasAdd(c, owItemGrid, OFFSET, 30.)

(*
    // common animations
    let da = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames()
    da.Duration <- new Duration(System.TimeSpan.FromSeconds(1.0))
    da.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(0.0, System.Windows.Media.Animation.KeyTime.FromTimeSpan(System.TimeSpan.FromSeconds(0.2)))) |> ignore
    da.KeyFrames.Add(new System.Windows.Media.Animation.DiscreteDoubleKeyFrame(0.0, System.Windows.Media.Animation.KeyTime.FromTimeSpan(System.TimeSpan.FromSeconds(0.5)))) |> ignore
    da.KeyFrames.Add(new System.Windows.Media.Animation.LinearDoubleKeyFrame(1.0, System.Windows.Media.Animation.KeyTime.FromTimeSpan(System.TimeSpan.FromSeconds(0.7)))) |> ignore
    da.KeyFrames.Add(new System.Windows.Media.Animation.DiscreteDoubleKeyFrame(1.0, System.Windows.Media.Animation.KeyTime.FromTimeSpan(System.TimeSpan.FromSeconds(1.0)))) |> ignore
    da.AutoReverse <- true
    da.RepeatBehavior <- System.Windows.Media.Animation.RepeatBehavior.Forever
    //let da = new System.Windows.Media.Animation.DoubleAnimation(From=System.Nullable(1.0), To=System.Nullable(0.0), Duration=new Duration(System.TimeSpan.FromSeconds(0.5)), 
    //            AutoReverse=true, RepeatBehavior=System.Windows.Media.Animation.RepeatBehavior.Forever)
    let animateds = ResizeArray()
    let removeAnimated(x) =
        animateds.Remove(x) |> ignore
    let addAnimated(x:UIElement) =
        animateds.Add(x)
        for x in animateds do
            x.BeginAnimation(Image.OpacityProperty, da)
*)

    // ow map
    let owMapGrid = makeGrid(16, 8, 16*3, 11*3)
    for i = 0 to 15 do
        for j = 0 to 7 do
            let image = Graphics.BMPtoImage(Graphics.overworldMapBMPs.[i,j])
            let c = new Canvas(Width=float(16*3), Height=float(11*3))
            canvasAdd(c, image, 0., 0.)
            gridAdd(owMapGrid, c, i, j)
            // shading between map tiles
            let OPA = 0.25
            let bottomShade = new Canvas(Width=float(16*3), Height=float(3), Background=System.Windows.Media.Brushes.Black, Opacity=OPA)
            canvasAdd(c, bottomShade, 0., float(10*3))
            let rightShade  = new Canvas(Width=float(3), Height=float(11*3), Background=System.Windows.Media.Brushes.Black, Opacity=OPA)
            canvasAdd(c, rightShade, float(15*3), 0.)
            // highlight mouse
            let rect = new System.Windows.Shapes.Rectangle(Width=float(16*3), Height=float(11*3), Stroke=System.Windows.Media.Brushes.White)
            c.MouseEnter.Add(fun _ -> c.Children.Add(rect) |> ignore)
            c.MouseLeave.Add(fun _ -> c.Children.Remove(rect) |> ignore)
            // icon
            let ms = new MapState()
            if Graphics.owMapSquaresAlwaysEmpty.[j].Chars(i) = 'X' then
                let icon = ms.Prev()
                icon.Opacity <- 0.5
                canvasAdd(c, icon, 0., 0.)
            else
                c.MouseWheel.Add(fun x -> 
                    //for x in c.Children do
                    //    removeAnimated(x)
                    c.Children.Clear()  // cant remove-by-identity because of non-uniques; remake whole canvas
                    canvasAdd(c, image, 0., 0.)
                    canvasAdd(c, bottomShade, 0., float(10*3))
                    canvasAdd(c, rightShade, float(15*3), 0.)
                    let icon = if x.Delta<0 then ms.Next() else ms.Prev()
                    if icon <> null then 
                        if ms.IsUnique then
                            icon.Opacity <- 0.6
                            //icon.BeginAnimation(Image.OpacityProperty, da)
                            //addAnimated(icon)
                        else
                            icon.Opacity <- 0.5
                    canvasAdd(c, icon, 0., 0.)
                    if ms.IsDungeon then
                        let rect = new System.Windows.Shapes.Rectangle(Width=float(16*3), Height=float(11*3), Stroke=System.Windows.Media.Brushes.Yellow, StrokeThickness = 3.)
                        c.Children.Add(rect) |> ignore
                    if ms.IsWarp then
                        let rect = new System.Windows.Shapes.Rectangle(Width=float(16*3), Height=float(11*3), Stroke=System.Windows.Media.Brushes.Aqua, StrokeThickness = 3.)
                        c.Children.Add(rect) |> ignore
                )
    canvasAdd(c, owMapGrid, 0., 120.)



//    for i = 0 to Graphics.uniqueMapIcons.Length-1 do
//        canvasAdd(c, Graphics.uniqueMapIcons.[i], float(16*3*i), 120.)
//    for i = 0 to Graphics.nonUniqueMapIconBMPs.Length-1 do
//        canvasAdd(c, Graphics.BMPtoImage Graphics.nonUniqueMapIconBMPs.[i], float(16*3*i), 120.)
    c


open System.Runtime.InteropServices 
module Winterop = 
    [<DllImport("User32.dll")>]
    extern bool RegisterHotKey(IntPtr hWnd,int id,uint32 fsModifiers,uint32 vk)

    [<DllImport("User32.dll")>]
    extern bool UnregisterHotKey(IntPtr hWnd,int id)

    let HOTKEY_ID = 9000

type MyWindow() as this = 
    inherit Window()
    let mutable source = null
    let canvas = makeAll()
    let hmsTimeTextBox = new TextBox(Text="timer",FontSize=42.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    let mutable startTime = DateTime.Now
    let VK_F10 = 0x79
    let MOD_NONE = 0u
    let update() =
        // update time
        let ts = DateTime.Now - startTime
        let h,m,s = ts.Hours, ts.Minutes, ts.Seconds
        hmsTimeTextBox.Text <- sprintf "%02d:%02d:%02d" h m s
    do
        // full window
        this.Title <- "Zelda 1 Randomizer"
        this.Content <- canvas
        canvasAdd(canvas, hmsTimeTextBox, 600., 0.)
        this.SizeToContent <- SizeToContent.WidthAndHeight 
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        this.Left <- 1100.0
        this.Top <- 20.0
        let timer = new System.Windows.Threading.DispatcherTimer()
        timer.Interval <- TimeSpan.FromSeconds(1.0)
        timer.Tick.Add(fun _ -> update())
        timer.Start()
    override this.OnSourceInitialized(e) =
        base.OnSourceInitialized(e)
        let helper = new System.Windows.Interop.WindowInteropHelper(this)
        source <- System.Windows.Interop.HwndSource.FromHwnd(helper.Handle)
        source.AddHook(System.Windows.Interop.HwndSourceHook(fun a b c d e -> this.HwndHook(a,b,c,d,&e)))
        this.RegisterHotKey()
    override this.OnClosed(e) =
        source.RemoveHook(System.Windows.Interop.HwndSourceHook(fun a b c d e -> this.HwndHook(a,b,c,d,&e)))
        source <- null
        this.UnregisterHotKey()
        base.OnClosed(e)
    member this.RegisterHotKey() =
        let helper = new System.Windows.Interop.WindowInteropHelper(this);
        if(not(Winterop.RegisterHotKey(helper.Handle, Winterop.HOTKEY_ID, MOD_NONE, uint32 VK_F10))) then
            // handle error
            ()
    member this.UnregisterHotKey() =
        let helper = new System.Windows.Interop.WindowInteropHelper(this)
        Winterop.UnregisterHotKey(helper.Handle, Winterop.HOTKEY_ID) |> ignore
    member this.HwndHook(hwnd:IntPtr, msg:int, wParam:IntPtr, lParam:IntPtr, handled:byref<bool>) : IntPtr =
        let WM_HOTKEY = 0x0312
        if msg = WM_HOTKEY then
            if wParam.ToInt32() = Winterop.HOTKEY_ID then
                //let ctrl_bits = lParam.ToInt32() &&& 0xF  // see WM_HOTKEY docs
                let key = lParam.ToInt32() >>> 16
                if key = VK_F10 then
                    startTime <- DateTime.Now
        IntPtr.Zero
        

[<STAThread>]
[<EntryPoint>]
let main argv = 
    printfn "test %A" argv

    let app = new Application()
#if DEBUG
    do
#else
    try
#endif
        app.Run(MyWindow()) |> ignore
#if DEBUG
#else
    with e ->
        printfn "crashed with exception"
        printfn "%s" (e.ToString())
        printfn "press enter to end"
        System.Console.ReadLine() |> ignore
#endif

    0
