module Graphics

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let mutable theWindow : System.Windows.Window = null
type Win32() =
    [<System.Runtime.InteropServices.DllImport("User32.dll")>]
    static extern bool SetCursorPos(int X, int Y)
    
    static member SetCursor(x,y) = 
        let pos = theWindow.PointToScreen(new Point(x,y))
        SetCursorPos(int pos.X, int pos.Y) |> ignore

let soundPlayer = new MediaPlayer()
soundPlayer.Volume <- 0.1
soundPlayer.Open(new Uri("confirm_speech.wav", UriKind.Relative))
let PlaySoundForSpeechRecognizedAndUsedToMark() =
    soundPlayer.Position <- TimeSpan(0L)
    soundPlayer.Play()




let GetResourceStream(name) = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name)

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

let BMPtoImage(bmp:System.Drawing.Bitmap) =
    let ms = new System.IO.MemoryStream()
    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png)  // must be png (not bmp) to save transparency info
    let bmimage = new System.Windows.Media.Imaging.BitmapImage()
    bmimage.BeginInit()
    ms.Seek(0L, System.IO.SeekOrigin.Begin) |> ignore
    bmimage.StreamSource <- ms
    bmimage.EndInit()
    let i = new Image()
    i.Source <- bmimage
    i.Height <- float bmp.Height 
    i.Width <- float bmp.Width 
    i

// see also
// https://stackoverflow.com/questions/63184765/wpf-left-click-and-drag
// https://stackoverflow.com/questions/12802122/wpf-handle-drag-and-drop-as-well-as-left-click
let setupClickVersusDrag(e:FrameworkElement, onClick, onStartDrag) =  // will only call onClick on 'clicks', will tell you when drag starts, you can call DoDragDrop or not
    let mutable isDragging = false
    let mutable startPoint = None
    e.PreviewMouseDown.Add(fun ea -> 
        startPoint <- Some(ea.GetPosition(null))
        )
    e.PreviewMouseMove.Add(fun ea ->
        if (ea.LeftButton = System.Windows.Input.MouseButtonState.Pressed ||
            ea.MiddleButton = System.Windows.Input.MouseButtonState.Pressed ||
            ea.RightButton = System.Windows.Input.MouseButtonState.Pressed)
                && not isDragging && startPoint.IsSome then
            let pos = ea.GetPosition(null)
            if Math.Abs(pos.X - startPoint.Value.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(pos.Y - startPoint.Value.Y) > SystemParameters.MinimumVerticalDragDistance then
                isDragging <- true
                onStartDrag(ea)
                isDragging <- false
        )
    e.MouseUp.Add(fun ea ->
        if not isDragging then
            if startPoint.IsSome then
                onClick(ea)
            // else e.g. dragged into and released
        startPoint <- None
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

let [| boomerang_bmp; bow_bmp; magic_boomerang_bmp; raft_bmp; ladder_bmp; recorder_bmp; wand_bmp; red_candle_bmp; book_bmp; key_bmp; 
        silver_arrow_bmp; wood_arrow_bmp; red_ring_bmp; magic_shield_bmp; boom_book_bmp; 
        heart_container_bmp; power_bracelet_bmp; white_sword_bmp; ow_key_armos_bmp;
        brown_sword_bmp; magical_sword_bmp; blue_candle_bmp; blue_ring_bmp;
        ganon_bmp; zelda_bmp; bomb_bmp; bow_and_arrow_bmp; bait_bmp |] =
    let imageStream = GetResourceStream("icons7x7.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    [|  for i = 0 to bmp.Width/7 - 1 do
            let r = new System.Drawing.Bitmap(7*3,7*3)
            for px = 0 to 7*3-1 do
                for py = 0 to 7*3-1 do
                    r.SetPixel(px, py, bmp.GetPixel(px/3 + i*7, py/3))
            yield r
    |]

let emptyTriforce_bmp, fullTriforce_bmp, owHeartSkipped_bmp, owHeartEmpty_bmp, owHeartFull_bmp = 
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
    all.[0], all.[1], all.[2], all.[3], all.[4]
let UNFOUND_NUMERAL_COLOR = System.Drawing.Color.FromArgb(0x88,0x88,0x88)
let emptyUnfoundNumberedTriforce_bmps, emptyUnfoundLetteredTriforce_bmps = 
    let a = [|
        for ch in ['1'; 'A'] do
            yield [|
            for i = 0 to 7 do
                let bmp = emptyTriforce_bmp.Clone() :?> System.Drawing.Bitmap
                paintAlphanumerics3x5(char(int ch + i), UNFOUND_NUMERAL_COLOR, bmp, 4, 4)
                yield bmp
            |] |]
    a.[0], a.[1]
let emptyFoundNumberedTriforce_bmps, emptyFoundLetteredTriforce_bmps = 
    let a = [|
        for ch in ['1'; 'A'] do
            yield [|
            for i = 0 to 7 do
                let bmp = emptyTriforce_bmp.Clone() :?> System.Drawing.Bitmap
                paintAlphanumerics3x5(char(int ch + i), System.Drawing.Color.White, bmp, 4, 4)
                yield bmp
            |] |]
    a.[0], a.[1]
let fullNumberedTriforce_bmps, fullLetteredTriforce_bmps =
    let a = [|
        for ch in ['1'; 'A'] do
            yield [|
            for i = 0 to 7 do
                let bmp = fullTriforce_bmp.Clone() :?> System.Drawing.Bitmap
                paintAlphanumerics3x5(char(int ch + i), System.Drawing.Color.White, bmp, 4, 4)
                yield bmp
            |] |]
    a.[0], a.[1]
let unfoundL9_bmp,foundL9_bmp =
    let u = new System.Drawing.Bitmap(10*3,10*3)
    let f = new System.Drawing.Bitmap(10*3,10*3)
    paintAlphanumerics3x5('9', UNFOUND_NUMERAL_COLOR, u, 4, 4)
    paintAlphanumerics3x5('9', System.Drawing.Color.White, f, 4, 4)
    u, f

let [| cdungeonUnexploredRoomBMP; cdungeonExploredRoomBMP; cdungeonDoubleMoatBMP; cdungeonChevyBMP; cdungeonVMoatBMP; cdungeonHMoatBMP; 
        cdungeonVChuteBMP; cdungeonHChuteBMP; cdungeonTeeBMP; cdungeonNeedWand; cdungeonBlueBubble; cdungeonNeedRecorder; cdungeonNeedBow; cdungeonTriforceBMP; cdungeonPrincessBMP; cdungeonStartBMP;
        cdn1bmp; cdn2bmp; cdn3bmp; cdn4bmp; cdn5bmp; cdn6bmp; cdn7bmp; cdn8bmp; cdn9bmp |] =
    let imageStream = GetResourceStream("icons13x9.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    [|  for i = 0 to bmp.Width/13 - 1 do
            let cr = new System.Drawing.Bitmap(13*3,9*3)
            let ur = new System.Drawing.Bitmap(13*3,9*3)
            for px = 0 to 13*3-1 do
                for py = 0 to 9*3-1 do
                    ur.SetPixel(px, py, bmp.GetPixel(px/3 + i*13,   py/3))
                    cr.SetPixel(px, py, bmp.GetPixel(px/3 + i*13, 9+py/3))
            yield (ur,cr)
    |]
let cdungeonNumberBMPs = [| cdn1bmp; cdn2bmp; cdn3bmp; cdn4bmp; cdn5bmp; cdn6bmp; cdn7bmp; cdn8bmp; cdn9bmp |]

let greyscale(bmp:System.Drawing.Bitmap) =
    let r = new System.Drawing.Bitmap(7*3,7*3)
    for px = 0 to 7*3-1 do
        for py = 0 to 7*3-1 do
            let c = bmp.GetPixel(px,py)
            let avg = (int c.R + int c.G + int c.B) / 5  // not just average, but overall darker
            let avg = if avg = 0 then 0 else avg + 20    // never too dark
            let c = System.Drawing.Color.FromArgb(avg, avg, avg)
            r.SetPixel(px, py, c)
    r

let overworldImage =
    let files = [|
//        "s_map_overworld_strip8.png"
        "s_map_overworld_vanilla_strip8.png"
//        "s_map_overworld_zones_strip8.png"
        |]
    let file = files.[(new System.Random()).Next(files.Length)]
    printfn "selecting overworld file %s" file
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


let makeVBrect(image) =
    // we need a point of indirection to swap the book and magical_shield icons, so a VisualBrush where we can poke the Visual works
    let vb = new VisualBrush(Visual=image, Opacity=1.0)
    let rect = new System.Windows.Shapes.Rectangle(Height=21., Width=21., Fill=vb)
    rect
let makeObject(bmp) = makeVBrect(BMPtoImage bmp)
// most of these need object identity for logic checks TODO hearts do, others? fix this - see what did in avalonia impl.
let boomerang, bow, magic_boomerang, raft, ladder, recorder, wand, red_candle, book, key, silver_arrow, red_ring, boom_book = 
    makeObject boomerang_bmp, makeObject bow_bmp, makeObject magic_boomerang_bmp, makeObject raft_bmp, makeObject ladder_bmp, makeObject recorder_bmp, makeObject wand_bmp, 
    makeObject red_candle_bmp, makeObject book_bmp, makeObject key_bmp, makeObject silver_arrow_bmp, makeObject red_ring_bmp, makeObject boom_book_bmp

let power_bracelet, white_sword, ow_key_armos = 
    makeObject power_bracelet_bmp, makeObject white_sword_bmp, makeObject ow_key_armos_bmp
let copyHeartContainer() =
    let bmp = new System.Drawing.Bitmap(7*3,7*3)
    for i = 0 to 20 do
        for j = 0 to 20 do
            bmp.SetPixel(i, j, heart_container_bmp.GetPixel(i,j))
    BMPtoImage bmp

let allItemBMPs = [| book_bmp; boomerang_bmp; bow_bmp; power_bracelet_bmp; ladder_bmp; magic_boomerang_bmp; key_bmp; raft_bmp; recorder_bmp; red_candle_bmp; red_ring_bmp; silver_arrow_bmp; wand_bmp; white_sword_bmp |]
let allItemBMPsWithHeartShuffle = [| yield! allItemBMPs; for _i = 0 to 8 do yield heart_container_bmp |]
let allItems = [| book; boomerang; bow; power_bracelet; ladder; magic_boomerang; key; raft; recorder; red_candle; red_ring; silver_arrow; wand; white_sword |]
let allItemsWithHeartShuffle = 
    [| yield! allItems; for _i = 0 to 8 do yield makeVBrect(copyHeartContainer()) |]

let owHeartsSkipped, owHeartsEmpty, owHeartsFull = Array.init 4 (fun _ -> BMPtoImage owHeartSkipped_bmp), Array.init 4 (fun _ -> BMPtoImage owHeartEmpty_bmp), Array.init 4 (fun _ -> BMPtoImage owHeartFull_bmp)

let overworldMapBMPs(n) =
    let m = overworldImage
    let tiles = Array2D.zeroCreate 16 8
    for x = 0 to 15 do
        for y = 0 to 7 do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    tile.SetPixel(px, py, m.GetPixel(256*n + (x*16*3 + px)/3, (y*11*3 + py)/3))
            tiles.[x,y] <- tile
    tiles

let TRANS_BG = System.Drawing.Color.FromArgb(1, System.Drawing.Color.Black)  // transparent background (will be darkened in program layer)
let uniqueNumberedMapIconBMPs, uniqueLetteredMapIconBMPs =
    let f labels =
        let imageStream = GetResourceStream("ow_icons5x9.png")
        let bmp = new System.Drawing.Bitmap(imageStream)
        let tiles = [|  
            for i in labels do
                let b = new System.Drawing.Bitmap(16*3,11*3)
                for px = 0 to 16*3-1 do
                    for py = 0 to 11*3-1 do
                        if px>=5*3 && px<10*3 && py>=1*3 && py<10*3 then 
                            b.SetPixel(px, py, System.Drawing.Color.Yellow)
                        else
                            b.SetPixel(px, py, TRANS_BG)
                paintAlphanumerics3x5(i, System.Drawing.Color.Black, b, 6, 3)
                yield b
            for i = 1 to 4 do  // warps 1-4
                let b = new System.Drawing.Bitmap(16*3,11*3)
                for px = 0 to 16*3-1 do
                    for py = 0 to 11*3-1 do
                        if px>=5*3 && px<10*3 && py>=1*3 && py<10*3 then 
                            b.SetPixel(px, py, System.Drawing.Color.Orchid)
                        else
                            b.SetPixel(px, py, TRANS_BG)
                paintAlphanumerics3x5(i.ToString().[0], System.Drawing.Color.Black, b, 6, 3)
                yield b
            for i = 0 to 1 do  // sword 3, sword 2
                let b = new System.Drawing.Bitmap(16*3,11*3)
                for px = 0 to 16*3-1 do
                    for py = 0 to 11*3-1 do
                        if px>=5*3 && px<10*3 && py>=1*3 && py<10*3 then 
                            b.SetPixel(px, py, bmp.GetPixel(i*5+(px-5*3)/3, (py-1*3)/3))
                        else
                            b.SetPixel(px, py, TRANS_BG)
                yield b
        |]
        tiles
    f "123456789", f "ABCDEFGH9"

let itemBackgroundColor = System.Drawing.Color.FromArgb(0xEF,0x83,0)
let itemsBMP = 
    let imageStream = GetResourceStream("icons3x7.png")
    new System.Drawing.Bitmap(imageStream)
let nonUniqueMapIconBMPs = 
    let imageStream = GetResourceStream("ow_icons5x9.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let tiles = [|  
        for i = 2 to 2 do  // hint shop
            let b = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    if px>=5*3 && px<10*3 && py>=1*3 && py<10*3 then 
                        b.SetPixel(px, py, bmp.GetPixel(i*5+(px-5*3)/3, (py-1*3)/3))
                    else
                        b.SetPixel(px, py, TRANS_BG)
            yield b
        // 3-item shops
        for i = 0 to 7 do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    // one-icon area
                    if px/3 >= 5 && px/3 <= 9 && py/3 >= 1 && py/3 <= 9 then
                        tile.SetPixel(px, py, itemBackgroundColor)
                    else
                        tile.SetPixel(px, py, TRANS_BG)
                    // icon
                    if px/3 >= 6 && px/3 <= 8 && py/3 >= 2 && py/3 <= 8 then
                        let c = itemsBMP.GetPixel(i*3 + px/3-6, py/3-2)
                        if c.ToArgb() <> System.Drawing.Color.Black.ToArgb() then
                            tile.SetPixel(px, py, c)
            yield tile
        for i = 3 to 5 do  // take-any, potion shop, rupee
            let b = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    if px>=5*3 && px<10*3 && py>=1*3 && py<10*3 then 
                        b.SetPixel(px, py, bmp.GetPixel(i*5+(px-5*3)/3, (py-1*3)/3))
                    else
                        b.SetPixel(px, py, TRANS_BG)
            yield b
        for _i = 0 to 0 do  // 'X'
            let b = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    b.SetPixel(px, py, System.Drawing.Color.Black)
            yield b
        |]
    tiles

let numberedMapIconInteriorBMPs, letteredMapIconInteriorBMPs =
    let f u = [|
        for bmp in [yield! u; yield! nonUniqueMapIconBMPs] do
            let r = new System.Drawing.Bitmap(5*3,9*3)
            for px = 0 to 5*3-1 do
                for py = 0 to 9*3-1 do
                    r.SetPixel(px, py, bmp.GetPixel(5*3+px, 1*3+py))
            yield r
        |]
    f uniqueNumberedMapIconBMPs, f uniqueLetteredMapIconBMPs

let linkFaceForward_bmp,linkRunRight_bmp,linkFaceRight_bmp,linkGotTheThing_bmp =
    let imageStream = GetResourceStream("link_icons.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let a = [|
        for i = 0 to 3 do
            let r = new System.Drawing.Bitmap(16, 16)
            for x = 0 to 15 do
                for y = 0 to 15 do
                    r.SetPixel(x, y, bmp.GetPixel(16*i+x, y))
            yield r
        |]
    a.[0], a.[1], a.[2], a.[3]
    
let mouseIconButtonColorsBMP =
    let imageStream = GetResourceStream("mouse-icon-button-colors.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    bmp
let mouseIconButtonColors2BMP =
    let imageStream = GetResourceStream("mouse-icon-button-colors-2.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    bmp

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
*)

let WarpMouseCursorTo(pos:Point) =
    Win32.SetCursor(pos.X, pos.Y)
    PlaySoundForSpeechRecognizedAndUsedToMark()
