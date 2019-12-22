open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let voice = new System.Speech.Synthesis.SpeechSynthesizer()

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

type ItemState(whichItems:Image[]) =
    let mutable state = -1
    member this.Current() =
        if state = -1 then
            null
        else
            whichItems.[state]
    member private this.Impl(forward) = 
        if forward then 
            state <- state + 1
        else
            state <- state - 1
        if state < -1 then
            state <- whichItems.Length-1
        if state >= whichItems.Length then
            state <- -1
        if state <> -1 && whichItems.[state].Parent <> null then
            if forward then this.Next() else this.Prev()
        elif state = -1 then
            null
        else
            whichItems.[state]
    member this.Next() = this.Impl(true)
    member this.Prev() = this.Impl(false)

type MapState() =
    let mutable state = -1
    let U = Graphics.uniqueMapIcons.Length 
    let NU = Graphics.nonUniqueMapIconBMPs.Length
    member this.State = state
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

let mutable recordering = fun() -> ()
let mutable haveRecorder = false
let mutable haveLadder = false
let mutable haveCoastItem = false
let mutable playerHearts = 3  // start with 3
let triforces = Array.zeroCreate 8
let owCurrentState = Array2D.create 16 8 -1
let dungeonRemains = [| 4; 3; 3; 3; 3; 3; 3; 4 |]
let mainTrackerCanvases : Canvas[,] = Array2D.zeroCreate 8 4
let currentHeartsTextBox = new TextBox(Width=200., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "Current Hearts: %d" playerHearts)
let updateTotalHearts(x) = 
    playerHearts <- playerHearts + x
    //printfn "curent hearts = %d" playerHearts
    currentHeartsTextBox.Text <- sprintf "Current Hearts: %d" playerHearts
let updateDungeon(dungeonIndex, itemDiff) =
    if dungeonIndex >= 0 && dungeonIndex < 8 then
        dungeonRemains.[dungeonIndex] <- dungeonRemains.[dungeonIndex] + itemDiff
        if dungeonRemains.[dungeonIndex] = 0 then
            async { voice.Speak(sprintf "Dungeon %d is complete" (dungeonIndex+1)) } |> Async.Start
            for j = 0 to 3 do
                //mainTrackerCanvases.[dungeonIndex,j].Background <- System.Windows.Media.Brushes.Green 
                // TODO dont love this, is it ok? also gets darker if un-complete and re-complete, hm
                mainTrackerCanvases.[dungeonIndex,j].Children.Add(new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black, Opacity=0.4)) |> ignore
let debug() =
    for j = 0 to 7 do
        for i = 0 to 15 do
            printf "%3d " owCurrentState.[i,j]
        printfn ""
    printfn ""

let H = 30
let makeAll(isHeartShuffle) =
    let whichItems = 
        if isHeartShuffle then
            Graphics.allItemsWithHeartShuffle 
        else
            Graphics.allItems
    
    let TH = 24 // text height
    let c = new Canvas()
    c.Height <- float(30*4 + 11*3*8 + TH + 27*8 + 12*7)
    c.Width <- float(16*16*3)

    c.Background <- System.Windows.Media.Brushes.Black 

    let mainTracker = makeGrid(9, 4, H, H)
    canvasAdd(c, mainTracker, 0., 0.)

    // triforce
    for i = 0 to 7 do
        let image = Graphics.emptyTriforces.[i]
        let c = new Canvas(Width=30., Height=30.)
        mainTrackerCanvases.[i,0] <- c
        canvasAdd(c, image, 0., 0.)
        c.MouseDown.Add(fun _ -> 
            if not triforces.[i] then 
                c.Children.Clear()
                c.Children.Add(Graphics.fullTriforces.[i]) |> ignore 
                triforces.[i] <- true
                updateDungeon(i, -1)
                recordering()
            else 
                c.Children.Clear()
                c.Children.Add(Graphics.emptyTriforces.[i]) |> ignore
                triforces.[i] <- false
                updateDungeon(i, +1)
                recordering()
        )
        gridAdd(mainTracker, c, i, 0)
    let hearts = whichItems.[14..]
    let boxItemImpl(dungeonIndex, isCoastItem) = 
        let c = new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black)
        let no = System.Windows.Media.Brushes.DarkRed
        let yes = System.Windows.Media.Brushes.LimeGreen 
        let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., Stroke=no)
        rect.StrokeThickness <- 3.0
        c.Children.Add(rect) |> ignore
        let is = new ItemState(whichItems)
        c.MouseLeftButtonDown.Add(fun _ ->
            if obj.Equals(rect.Stroke, no) then
                rect.Stroke <- yes
                updateDungeon(dungeonIndex, -1)
                if obj.Equals(is.Current(), Graphics.recorder) then
                    haveRecorder <- true
                    recordering()
                if obj.Equals(is.Current(), Graphics.ladder) then
                    haveLadder <- true
                if hearts |> Array.exists(fun x -> obj.Equals(is.Current(), x)) then
                    updateTotalHearts(1)
                if isCoastItem then
                    haveCoastItem <- true
            else
                rect.Stroke <- no
                updateDungeon(dungeonIndex, +1)
                if obj.Equals(is.Current(), Graphics.recorder) then
                    haveRecorder <- false
                    recordering()
                if obj.Equals(is.Current(), Graphics.ladder) then
                    haveLadder <- false
                if hearts |> Array.exists(fun x -> obj.Equals(is.Current(), x)) then
                    updateTotalHearts(-1)
                if isCoastItem then
                    haveCoastItem <- false
        )
        // item
        c.MouseWheel.Add(fun x -> 
            if obj.Equals(is.Current(), Graphics.recorder) && haveRecorder then
                haveRecorder <- false
                recordering()
            if obj.Equals(is.Current(), Graphics.ladder) && haveLadder then
                haveLadder <- false
            if hearts |> Array.exists(fun x -> obj.Equals(is.Current(), x)) && obj.Equals(rect.Stroke, yes) then
                updateTotalHearts(-1)
            c.Children.Remove(is.Current())
            canvasAdd(c, (if x.Delta<0 then is.Next() else is.Prev()), 4., 4.)
            if obj.Equals(is.Current(), Graphics.recorder) && obj.Equals(rect.Stroke,yes) then
                haveRecorder <- true
                recordering()
            if obj.Equals(is.Current(), Graphics.ladder) && obj.Equals(rect.Stroke,yes) then
                haveLadder <- true
            if hearts |> Array.exists(fun x -> obj.Equals(is.Current(), x)) && obj.Equals(rect.Stroke, yes) then
                updateTotalHearts(1)
        )
        c
    let boxItem(dungeonIndex) = 
        boxItemImpl(dungeonIndex,false)
    // floor hearts
    if isHeartShuffle then
        for i = 0 to 7 do
            let c = boxItem(i)
            mainTrackerCanvases.[i,1] <- c
            gridAdd(mainTracker, c, i, 1)
    else
        for i = 0 to 7 do
            let image = Graphics.emptyHearts.[i]
            let c = new Canvas(Width=30., Height=30.)
            mainTrackerCanvases.[i,1] <- c
            canvasAdd(c, image, 0., 0.)
            c.MouseDown.Add(fun _ -> 
                if c.Children.Contains(Graphics.emptyHearts.[i]) then 
                    c.Children.Clear()
                    c.Children.Add(Graphics.fullHearts.[i]) |> ignore 
                    updateTotalHearts(+1)
                    updateDungeon(i, -1)
                else 
                    c.Children.Clear()
                    c.Children.Add(Graphics.emptyHearts.[i]) |> ignore
                    updateTotalHearts(-1)
                    updateDungeon(i, +1)
            )
            gridAdd(mainTracker, c, i, 1)

    // items
    for i = 0 to 8 do
        for j = 0 to 1 do
            let mutable c = new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black)
            if j=0 || (i=0 || i=7 || i=8) then
                c <- boxItem(i)
                gridAdd(mainTracker, c, i, j+2)
            if i < 8 then
                mainTrackerCanvases.[i,j+2] <- c

    let kitty = new Image()
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("CroppedBrianKitty.png")
    kitty.Source <- System.Windows.Media.Imaging.BitmapFrame.Create(imageStream)
    canvasAdd(c, kitty, 285., 0.)

    let OFFSET = 400.
    // ow hearts
    let owHeartGrid = makeGrid(4, 1, 30, 30)
    for i = 0 to 3 do
        let c = new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black)
        let image = Graphics.owHeartsEmpty.[i]
        canvasAdd(c, image, 0., 0.)
        let f b =
            let cur = 
                if c.Children.Contains(Graphics.owHeartsEmpty.[i]) then 0
                elif c.Children.Contains(Graphics.owHeartsFull.[i]) then 1
                else 2
            c.Children.Clear()
            let next = (cur + (if b then 1 else -1) + 3) % 3
            canvasAdd(c, (  if next = 0 then 
                                updateTotalHearts(0-(if cur=1 then 1 else 0))
                                Graphics.owHeartsEmpty.[i] 
                            elif next = 1 then 
                                updateTotalHearts(1-(if cur=1 then 1 else 0))
                                Graphics.owHeartsFull.[i] 
                            else 
                                updateTotalHearts(0-(if cur=1 then 1 else 0))
                                Graphics.owHeartsSkipped.[i]), 0., 0.)
        c.MouseLeftButtonDown.Add(fun _ -> f true)
        c.MouseRightButtonDown.Add(fun _ -> f false)
        c.MouseWheel.Add(fun x -> f (x.Delta<0))
        gridAdd(owHeartGrid, c, i, 0)
    canvasAdd(c, owHeartGrid, OFFSET, 0.)
    // ladder, armos, white sword items
    let owItemGrid = makeGrid(2, 3, 30, 30)
    gridAdd(owItemGrid, Graphics.ow_key_ladder, 0, 0)
    gridAdd(owItemGrid, Graphics.ow_key_armos, 0, 1)
    gridAdd(owItemGrid, Graphics.ow_key_white_sword, 0, 2)
    gridAdd(owItemGrid, boxItemImpl(-1,true), 1, 0)
    gridAdd(owItemGrid, boxItem(-1), 1, 1)
    gridAdd(owItemGrid, boxItem(-1), 1, 2)
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
            let rect = new System.Windows.Shapes.Rectangle(Width=float(16*3)-4., Height=float(11*3)-4., Stroke=System.Windows.Media.Brushes.White)
            c.MouseEnter.Add(fun _ -> canvasAdd(c, rect, 2., 2.))
            c.MouseLeave.Add(fun _ -> c.Children.Remove(rect) |> ignore)
            // icon
            let ms = new MapState()
            if Graphics.owMapSquaresAlwaysEmpty.[j].Chars(i) = 'X' then
                let icon = ms.Prev()
                owCurrentState.[i,j] <- ms.State 
                icon.Opacity <- 0.5
                canvasAdd(c, icon, 0., 0.)
            else
                let f b =
                    //for x in c.Children do
                    //    removeAnimated(x)
                    let mutable needRecordering = false
                    if ms.IsDungeon then
                        needRecordering <- true
                    c.Children.Clear()  // cant remove-by-identity because of non-uniques; remake whole canvas
                    canvasAdd(c, image, 0., 0.)
                    canvasAdd(c, bottomShade, 0., float(10*3))
                    canvasAdd(c, rightShade, float(15*3), 0.)
                    let icon = if b then ms.Next() else ms.Prev()
                    owCurrentState.[i,j] <- ms.State 
                    //debug()
                    if icon <> null then 
                        if ms.IsUnique then
                            icon.Opacity <- 0.6
                            //icon.BeginAnimation(Image.OpacityProperty, da)
                            //addAnimated(icon)
                        else
                            icon.Opacity <- 0.5
                    canvasAdd(c, icon, 0., 0.)
                    if ms.IsDungeon then
                        let rect = new System.Windows.Shapes.Rectangle(Width=float(16*3)-4., Height=float(11*3)-4., Stroke=System.Windows.Media.Brushes.Yellow, StrokeThickness = 3.)
                        canvasAdd(c, rect, 2., 2.)
                        needRecordering <- true
                    if ms.IsWarp then
                        let rect = new System.Windows.Shapes.Rectangle(Width=float(16*3)-4., Height=float(11*3)-4., Stroke=System.Windows.Media.Brushes.Aqua, StrokeThickness = 3.)
                        canvasAdd(c, rect, 2., 2.)
                    if needRecordering then
                        recordering()
                c.MouseLeftButtonDown.Add(fun _ -> f true)
                c.MouseRightButtonDown.Add(fun _ -> f false)
                c.MouseWheel.Add(fun x -> f (x.Delta<0))
    canvasAdd(c, owMapGrid, 0., 120.)

    // map barriers
    let makeLine(x1, x2, y1, y2) = new System.Windows.Shapes.Line(X1=float(x1*16*3), X2=float(x2*16*3), Y1=float(y1*11*3), Y2=float(y2*11*3), Stroke=Brushes.Red, StrokeThickness=3.)
    canvasAdd(c, makeLine(0,4,2,2), 0., 120.)
    canvasAdd(c, makeLine(2,2,1,3), 0., 120.)
    canvasAdd(c, makeLine(4,4,0,1), 0., 120.)
    canvasAdd(c, makeLine(4,7,1,1), 0., 120.)
    canvasAdd(c, makeLine(8,10,1,1), 0., 120.)
    canvasAdd(c, makeLine(10,10,0,1), 0., 120.)
    canvasAdd(c, makeLine(11,11,0,1), 0., 120.)
    canvasAdd(c, makeLine(12,12,0,1), 0., 120.)
    canvasAdd(c, makeLine(14,14,0,1), 0., 120.)
    canvasAdd(c, makeLine(15,15,0,1), 0., 120.)
    canvasAdd(c, makeLine(14,16,2,2), 0., 120.)
    canvasAdd(c, makeLine(6,7,2,2), 0., 120.)
    canvasAdd(c, makeLine(8,12,2,2), 0., 120.)
    canvasAdd(c, makeLine(4,5,3,3), 0., 120.)
    canvasAdd(c, makeLine(7,8,3,3), 0., 120.)
    canvasAdd(c, makeLine(9,10,3,3), 0., 120.)
    canvasAdd(c, makeLine(12,13,3,3), 0., 120.)
    canvasAdd(c, makeLine(2,4,4,4), 0., 120.)
    canvasAdd(c, makeLine(5,8,4,4), 0., 120.)
    canvasAdd(c, makeLine(14,15,4,4), 0., 120.)
    canvasAdd(c, makeLine(1,2,5,5), 0., 120.)
    canvasAdd(c, makeLine(7,8,5,5), 0., 120.)
    canvasAdd(c, makeLine(10,11,5,5), 0., 120.)
    canvasAdd(c, makeLine(12,13,5,5), 0., 120.)
    canvasAdd(c, makeLine(14,15,5,5), 0., 120.)
    canvasAdd(c, makeLine(6,8,6,6), 0., 120.)
    canvasAdd(c, makeLine(14,15,6,6), 0., 120.)
    canvasAdd(c, makeLine(0,1,7,7), 0., 120.)
    canvasAdd(c, makeLine(4,5,7,7), 0., 120.)
    canvasAdd(c, makeLine(9,11,7,7), 0., 120.)
    canvasAdd(c, makeLine(12,15,7,7), 0., 120.)
    canvasAdd(c, makeLine(1,1,5,6), 0., 120.)
    canvasAdd(c, makeLine(2,2,4,5), 0., 120.)
    canvasAdd(c, makeLine(3,3,2,3), 0., 120.)
    canvasAdd(c, makeLine(3,3,4,5), 0., 120.)
    canvasAdd(c, makeLine(4,4,3,5), 0., 120.)
    canvasAdd(c, makeLine(5,5,3,5), 0., 120.)
    canvasAdd(c, makeLine(5,5,7,8), 0., 120.)
    canvasAdd(c, makeLine(6,6,2,3), 0., 120.)
    canvasAdd(c, makeLine(6,6,4,5), 0., 120.)
    canvasAdd(c, makeLine(7,7,3,4), 0., 120.)
    canvasAdd(c, makeLine(9,9,3,5), 0., 120.)
    canvasAdd(c, makeLine(10,10,3,4), 0., 120.)
    canvasAdd(c, makeLine(12,12,3,5), 0., 120.)
    canvasAdd(c, makeLine(13,13,3,4), 0., 120.)
    canvasAdd(c, makeLine(14,14,3,4), 0., 120.)
    canvasAdd(c, makeLine(15,15,2,3), 0., 120.)
    canvasAdd(c, makeLine(15,15,4,6), 0., 120.)

    let dungeonCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7))
    canvasAdd(c, dungeonCanvas, 0., float(120+8*11*3))

    // quadrants
    (*
    let QW = dungeonCanvas.Width/2.
    let QH = (dungeonCanvas.Height-float TH)/2.
    let QBG = new SolidColorBrush(Color.FromRgb(0uy, 40uy, 50uy))
    let topRight = new Canvas(Width=QW, Height=QH, Background=QBG)
    canvasAdd(dungeonCanvas, topRight, topRight.Width, float TH)
    let botLeft = new Canvas(Width=QW, Height=QH, Background=QBG)
    canvasAdd(dungeonCanvas, botLeft, 0., float TH + QH)
    *)
    (*
    let vert = new System.Windows.Shapes.Line(Stroke=Brushes.White, StrokeThickness=1., X1=QW, X2=QW, Y1=float TH, Y2=float TH+QH+QH)
    canvasAdd(dungeonCanvas, vert, 0., 0.)
    let hori = new System.Windows.Shapes.Line(Stroke=Brushes.White, StrokeThickness=1., X1=0., X2=QW+QW, Y1=float TH+QH, Y2=float TH+QH)
    canvasAdd(dungeonCanvas, hori, 0., 0.)
    *)

    let TEXT = "LEVEL-9 "
    // rooms
    let roomCanvases = Array2D.zeroCreate 8 8 
    let roomStates = Array2D.zeroCreate 8 8 // 0 = unexplored, 1-9 = transports, 10=tri, 11=heart, 12=vchute, 13=hchute, 14=tee, 15=explored empty
    let ROOMS = 16 // how many types
    let usedTransports = Array.zeroCreate 10 // slot 0 unused
    for i = 0 to 7 do
        // LEVEL-9        
        let tb = new TextBox(Width=float(13*3), Height=float(TH), FontSize=float(TH-4), Foreground=Brushes.White, Background=Brushes.Black, IsReadOnly=true,
                                Text=TEXT.Substring(i,1), BorderThickness=Thickness(0.), FontFamily=new FontFamily("Courier New"), FontWeight=FontWeights.Bold)
        canvasAdd(dungeonCanvas, tb, float(i*51)+12., 0.)
        // room map
        for j = 0 to 7 do
            let c = new Canvas(Width=float(13*3), Height=float(9*3))
            canvasAdd(dungeonCanvas, c, float(i*51), float(TH+j*39))
            let image = Graphics.BMPtoImage Graphics.dungeonUnexploredRoomBMP 
            canvasAdd(c, image, 0., 0.)
            roomCanvases.[i,j] <- c
            roomStates.[i,j] <- 0
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
                // update UI
                c.Children.Clear()
                let image =
                    match roomStates.[i,j] with
                    | 0  -> Graphics.dungeonUnexploredRoomBMP 
                    | 10 -> Graphics.dungeonTriforceBMP 
                    | 11 -> Graphics.dungeonPrincessBMP 
                    | 12 -> Graphics.dungeonVChuteBMP
                    | 13 -> Graphics.dungeonHChuteBMP
                    | 14 -> Graphics.dungeonTeeBMP
                    | 15 -> Graphics.dungeonExploredRoomBMP 
                    | n  -> Graphics.dungeonNumberBMPs.[n-1]
                    |> Graphics.BMPtoImage 
                canvasAdd(c, image, 0., 0.)
            // not allowing mouse clicks makes less likely to accidentally click room when trying to target doors with mouse
            //c.MouseLeftButtonDown.Add(fun _ -> f true)
            //c.MouseRightButtonDown.Add(fun _ -> f false)
            c.MouseWheel.Add(fun x -> f (x.Delta<0))
            // initial values
            if i=6 && (j=6 || j=7) then
                f false
    // horizontal doors
    let unknown = new SolidColorBrush(Color.FromRgb(55uy, 55uy, 55uy)) 
    let no = System.Windows.Media.Brushes.DarkRed
    let yes = System.Windows.Media.Brushes.Lime
    for i = 0 to 6 do
        for j = 0 to 7 do
            let d = new Canvas(Height=12., Width=12., Background=unknown)
            canvasAdd(dungeonCanvas, d, float(i*(39+12)+39), float(TH+j*(27+12)+8))
            let left _ =        
                if not(obj.Equals(d.Background, yes)) then
                    d.Background <- yes
                else
                    d.Background <- unknown
            d.MouseLeftButtonDown.Add(left)
            let right _ = 
                if not(obj.Equals(d.Background, no)) then
                    d.Background <- no
                else
                    d.Background <- unknown
            d.MouseRightButtonDown.Add(right)
            // initial values
            if (i=5 || i=6) && j=7 then
                right()
    // vertical doors
    for i = 0 to 7 do
        for j = 0 to 6 do
            let d = new Canvas(Height=12., Width=12., Background=unknown)
            canvasAdd(dungeonCanvas, d, float(i*(39+12)+14), float(TH+j*(27+12)+27))
            let left _ =
                if not(obj.Equals(d.Background, yes)) then
                    d.Background <- yes
                else
                    d.Background <- unknown
            d.MouseLeftButtonDown.Add(left)
            let right _ = 
                if not(obj.Equals(d.Background, no)) then
                    d.Background <- no
                else
                    d.Background <- unknown
            d.MouseRightButtonDown.Add(right)
            // initial values
            if i=6 && j=6 then
                left()
    // notes    
    let tb = new TextBox(Width=c.Width-400., Height=dungeonCanvas.Height)
    tb.FontSize <- 24.
    tb.Foreground <- System.Windows.Media.Brushes.LimeGreen 
    tb.Background <- System.Windows.Media.Brushes.Black 
    tb.Text <- "Notes"
    tb.AcceptsReturn <- true
    canvasAdd(c, tb, 400., float(120+8*11*3)) 
    // audio reminders    
    let cb = new CheckBox(Content=new TextBox(Text="Audio reminders",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    cb.IsChecked <- System.Nullable.op_Implicit true
    voice.Volume <- 30
    cb.Checked.Add(fun _ -> voice.Volume <- 30)
    cb.Unchecked.Add(fun _ -> voice.Volume <- 0)
    canvasAdd(c, cb, 600., 60.)
    // current hearts
    canvasAdd(c, currentHeartsTextBox, 600., 90.)

    c


// TODO
// free form text for seed flags?
// voice reminders:
//  - what else?
// TRIFORCE time splits ?
// ...2nd quest etc...

open System.Runtime.InteropServices 
module Winterop = 
    [<DllImport("User32.dll")>]
    extern bool RegisterHotKey(IntPtr hWnd,int id,uint32 fsModifiers,uint32 vk)

    [<DllImport("User32.dll")>]
    extern bool UnregisterHotKey(IntPtr hWnd,int id)

    let HOTKEY_ID = 9000

type MyWindowBase() as this = 
    inherit Window()
    let mutable source = null
    let VK_F10 = 0x79
    let MOD_NONE = 0u
    let mutable startTime = DateTime.Now
    do
        // full window
        let timer = new System.Windows.Threading.DispatcherTimer()
        timer.Interval <- TimeSpan.FromSeconds(1.0)
        timer.Tick.Add(fun _ -> this.Update())
        timer.Start()
    member this.StartTime = startTime
    abstract member Update : unit -> unit
    default this.Update() = ()
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

type MyWindow(isHeartSuffle) as this = 
    inherit MyWindowBase()
    let canvas = makeAll(isHeartSuffle)
    let hmsTimeTextBox = new TextBox(Text="timer",FontSize=42.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    let mutable ladderTime = DateTime.Now
    let da = new System.Windows.Media.Animation.DoubleAnimation(From=System.Nullable(1.0), To=System.Nullable(0.0), Duration=new Duration(System.TimeSpan.FromSeconds(0.5)), 
                AutoReverse=true, RepeatBehavior=System.Windows.Media.Animation.RepeatBehavior.Forever)
    do
        // full window
        this.Title <- "Zelda 1 Randomizer"
        this.Content <- canvas
        canvasAdd(canvas, hmsTimeTextBox, 600., 0.)
        this.SizeToContent <- SizeToContent.WidthAndHeight 
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        this.Left <- 0.0
        this.Top <- 0.0
        let recorderingCanvas = new Canvas(Width=float(16*16*3), Height=float(8*11*3))
        canvasAdd(canvas, recorderingCanvas, 0., 120.)
        recordering <- (fun () ->
            recorderingCanvas.Children.Clear()
            if haveRecorder then
                // highlight any triforce dungeons as recorder warp destinations
                for i = 0 to 7 do
                    if triforces.[i] then
                        for x = 0 to 15 do
                            for y = 0 to 7 do
                                if owCurrentState.[x,y] = i then
                                    let rect = new System.Windows.Shapes.Rectangle(Width=float(14*3), Height=float(9*3), Stroke=System.Windows.Media.Brushes.White, StrokeThickness = 5.)
                                    rect.BeginAnimation(UIElement.OpacityProperty, da)
                                    canvasAdd(recorderingCanvas, rect, float(x*16*3)+1., float(y*11*3)+3.)
            // highlight 9 after get all triforce
            if Array.forall id triforces then
                for x = 0 to 15 do
                    for y = 0 to 7 do
                        if owCurrentState.[x,y] = 8 then
                            let rect = new Canvas(Width=float(16*3), Height=float(11*3), Background=System.Windows.Media.Brushes.Pink)
                            rect.BeginAnimation(UIElement.OpacityProperty, da)
                            canvasAdd(recorderingCanvas, rect, float(x*16*3), float(y*11*3))
        )
    override this.Update() =
        base.Update()
        // update time
        let ts = DateTime.Now - this.StartTime
        let h,m,s = ts.Hours, ts.Minutes, ts.Seconds
        hmsTimeTextBox.Text <- sprintf "%02d:%02d:%02d" h m s
        // remind ladder
        if (DateTime.Now - ladderTime).Minutes > 1 then  // every 2 mins
            if haveLadder then
                if not haveCoastItem then
                    async { voice.Speak("Get the coast item with the ladder") } |> Async.Start
                    ladderTime <- DateTime.Now

type TimerOnlyWindow() as this = 
    inherit MyWindowBase()
    let hmsTimeTextBox = new TextBox(Text="timer",FontSize=42.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    let canvas = new Canvas(Width=180., Height=50., Background=System.Windows.Media.Brushes.Black)
    do
        // full window
        this.Title <- "Timer"
        this.Content <- canvas
        canvasAdd(canvas, hmsTimeTextBox, 0., 0.)
        this.SizeToContent <- SizeToContent.WidthAndHeight 
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        this.Left <- 0.0
        this.Top <- 0.0
    override this.Update() =
        base.Update()
        // update time
        let ts = DateTime.Now - this.StartTime
        let h,m,s = ts.Hours, ts.Minutes, ts.Seconds
        hmsTimeTextBox.Text <- sprintf "%02d:%02d:%02d" h m s

type TerrariaTimerOnlyWindow() as this = 
    inherit MyWindowBase()
    let FONT = 24.
    let hmsTimeTextBox = new TextBox(Text="timer",FontSize=FONT,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    let dayTextBox = new TextBox(Text="day",FontSize=FONT,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    let timeTextBox = new TextBox(Text="time",FontSize=FONT,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    let canvas = new Canvas(Width=170.*FONT/35., Height=FONT*16./4., Background=System.Windows.Media.Brushes.Black)
    do
        // full window
        this.Title <- "Timer"
        this.Content <- canvas
        canvasAdd(canvas, hmsTimeTextBox, 0., 0.)
        canvasAdd(canvas, dayTextBox, 0., FONT*5./4.)
        canvasAdd(canvas, timeTextBox, 0., FONT*10./4.)
        this.SizeToContent <- SizeToContent.WidthAndHeight 
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        this.Left <- 0.0
        this.Top <- 0.0
        this.BorderBrush <- Brushes.LightGreen
        this.BorderThickness <- Thickness(2.)
    override this.Update() =
        base.Update()
        // update hms time
        let mutable ts = DateTime.Now - this.StartTime
        let h,m,s = ts.Hours, ts.Minutes, ts.Seconds
        hmsTimeTextBox.Text <- sprintf "%02d:%02d:%02d" h m s
        // update terraria time
        let mutable day = 1
        while ts >= TimeSpan.FromMinutes(20.25) do
            ts <- ts - TimeSpan.FromMinutes(24.)
            day <- day + 1
        let mutable ttime = ts + TimeSpan.FromMinutes(8.25)
        if ttime >= TimeSpan.FromMinutes(24.) then
            ttime <- ttime - TimeSpan.FromMinutes(24.)
        let m,s = ttime.Minutes, ttime.Seconds
        let m,am = if m < 12 then m,"am" else m-12,"pm"
        let m = if m=0 then 12 else m
        timeTextBox.Text <- sprintf "%02d:%02d%s" m s am
        if ts < TimeSpan.FromMinutes(11.25) then   // 11.25 is 7:30pm, 20.25 is 4:30am
            dayTextBox.Text <- sprintf "Day %d" day
        else
            dayTextBox.Text <- sprintf "Night %d" day

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
        if argv.Length > 0 && argv.[0] = "timeronly" then
            app.Run(TimerOnlyWindow()) |> ignore
        elif argv.Length > 0 && argv.[0] = "terraria" then
            app.Run(TerrariaTimerOnlyWindow()) |> ignore
        elif argv.Length > 0 && argv.[0] = "heartShuffle" then
            app.Run(MyWindow(true)) |> ignore
        else
            app.Run(MyWindow(false)) |> ignore
#if DEBUG
#else
    with e ->
        printfn "crashed with exception"
        printfn "%s" (e.ToString())
        printfn "press enter to end"
        System.Console.ReadLine() |> ignore
#endif

    0
