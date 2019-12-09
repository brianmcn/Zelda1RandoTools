open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media


let canvasAdd(c:Canvas, item, left, top) =
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
        let image = Graphics.fullTriforces.[i]
        gridAdd(mainTracker, image, i, 0)
    // floor hearts
    for i = 0 to 7 do
        let image = Graphics.fullHearts.[i]
        gridAdd(mainTracker, image, i, 1)

    let boxItem(item) = 
        let box = new Border()
        box.BorderThickness <- new Thickness(3.0)
        box.BorderBrush <- System.Windows.Media.Brushes.Gray 
        // item
        box.Child <- item
        box
    // items
    let items = Array2D.zeroCreate 9 2
    items.[0,0] <- Graphics.boomerang
    items.[0,1] <- Graphics.bow
    items.[1,0] <- Graphics.magic_boomerang
    items.[2,0] <- Graphics.raft
    items.[3,0] <- Graphics.ladder
    items.[4,0] <- Graphics.recorder
    items.[5,0] <- Graphics.wand
    items.[6,0] <- Graphics.red_candle 
    items.[7,0] <- Graphics.key
    items.[7,1] <- Graphics.book
    items.[8,0] <- Graphics.red_ring 
    items.[8,1] <- Graphics.silver_arrow
    for i = 0 to 8 do
        for j = 0 to 1 do
            if j=0 || (i=0 || i=7 || i=8) then
                gridAdd(mainTracker, boxItem(items.[i,j]), i, j+2)

    let OFFSET = 400.
    // ow hearts
    let owHeartGrid = makeGrid(4, 1, 30, 30)
    for i = 0 to 3 do
        let image = if i=0 then Graphics.owHeartsSkipped.[i] elif i=3 then Graphics.owHeartsEmpty.[i] else Graphics.owHeartsFull.[i]
        gridAdd(owHeartGrid, image, i, 0)
    canvasAdd(c, owHeartGrid, OFFSET, 0.)
    // ladder, armos, white sword items
    let owItemGrid = makeGrid(2, 3, 30, 30)
    gridAdd(owItemGrid, Graphics.ow_key_ladder, 0, 0)
    gridAdd(owItemGrid, Graphics.ow_key_armos, 0, 1)
    gridAdd(owItemGrid, Graphics.ow_key_white_sword, 0, 2)
    gridAdd(owItemGrid, boxItem(Graphics.heart_container), 1, 0)
    gridAdd(owItemGrid, boxItem(Graphics.power_bracelet), 1, 1)
    gridAdd(owItemGrid, boxItem(Graphics.white_sword), 1, 2)
    canvasAdd(c, owItemGrid, OFFSET, 30.)

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
            // click shade
            let shade = new Canvas(Width=float(16*3), Height=float(11*3), Background=System.Windows.Media.Brushes.Black, Opacity=0.5)
            c.MouseWheel.Add(fun x -> if x.Delta > 0 then if c.Children.Contains(shade) then c.Children.Remove(shade) else c.Children.Add(shade) |> ignore)
    canvasAdd(c, owMapGrid, 0., 120.)
    c


type MyWindow() as this = 
    inherit Window()
    let all = makeAll()
    let content = all
    do
        // full window
        this.Title <- "Zelda 1 Randomizer"
        this.Content <- content
        this.SizeToContent <- SizeToContent.WidthAndHeight 
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        this.Left <- 1100.0
        this.Top <- 20.0

(*
        let timer = new System.Windows.Threading.DispatcherTimer()
        timer.Interval <- TimeSpan.FromSeconds(0.5)  // TODO decide time
        timer.Tick.Add(fun _ -> update())
        timer.Start()
*)
        

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
