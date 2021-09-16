module Graphics

open System
open System.Windows
open Avalonia.Media
open Avalonia.Controls

let GetResourceStream(name) =
    let assets = Avalonia.AvaloniaLocator.Current.GetService(typeof<Avalonia.Platform.IAssetLoader>) :?> Avalonia.Platform.IAssetLoader
    assets.Open(new Uri("resm:Z1R_Avalonia." + name))

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

let BMPtoImage(bmp:System.Drawing.Bitmap) =
    let ms = new System.IO.MemoryStream()
    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png)  // must be png (not bmp) to save transparency info
    ms.Seek(0L, System.IO.SeekOrigin.Begin) |> ignore
    let bmp = new Avalonia.Media.Imaging.Bitmap(ms)
    let image = new Avalonia.Controls.Image()
    image.Source <- bmp
    image.Width <- bmp.Size.Width
    image.Height <- bmp.Size.Height
    image

// see also
// https://stackoverflow.com/questions/63184765/wpf-left-click-and-drag
// https://stackoverflow.com/questions/12802122/wpf-handle-drag-and-drop-as-well-as-left-click
let setupClickVersusDrag(e:Control, onClick, onStartDrag) =  // will only call onClick on 'clicks', will tell you when drag starts, you can call DoDragDrop or not
    let mutable isDragging = false
    let mutable startPoint = None
    e.AddHandler(Avalonia.Input.InputElement.PointerPressedEvent, new EventHandler<Avalonia.Input.PointerPressedEventArgs>(fun o ea -> 
        startPoint <- Some(ea.GetPosition(null))
        ), Avalonia.Interactivity.RoutingStrategies.Tunnel, false)
    e.AddHandler(Avalonia.Input.InputElement.PointerMovedEvent, new EventHandler<Avalonia.Input.PointerEventArgs>(fun o ea ->
        let pp = ea.GetCurrentPoint(e)
        if (pp.Properties.IsLeftButtonPressed || pp.Properties.IsMiddleButtonPressed || pp.Properties.IsRightButtonPressed)
                && not isDragging && startPoint.IsSome then
            let pos = ea.GetPosition(null)
            let MIN = 4.
            if Math.Abs(pos.X - startPoint.Value.X) > MIN || Math.Abs(pos.Y - startPoint.Value.Y) > MIN then
                isDragging <- true
                onStartDrag(ea)
                isDragging <- false
        ), Avalonia.Interactivity.RoutingStrategies.Tunnel, false)
    e.PointerReleased.Add(fun ea ->
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

let [| boomerang_bmp; bow_bmp; magic_boomerang_bmp; raft_bmp; ladder_bmp; recorder_bmp; wand_bmp; red_candle_bmp; book_bmp; key_bmp; 
        silver_arrow_bmp; wood_arrow_bmp; red_ring_bmp; magic_shield_bmp; boom_book_bmp; 
        heart_container_bmp; power_bracelet_bmp; white_sword_bmp; ow_key_armos_bmp;
        brown_sword_bmp; magical_sword_bmp; blue_candle_bmp; blue_ring_bmp;
        ganon_bmp; zelda_bmp; bomb_bmp; bow_and_arrow_bmp; bait_bmp  |] =
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
let emptyUnfoundTriforce_bmps = [|
    for i = 0 to 7 do
        let bmp = emptyTriforce_bmp.Clone() :?> System.Drawing.Bitmap
        paintAlphanumerics3x5(char(int '1' + i), UNFOUND_NUMERAL_COLOR, bmp, 4, 4)
        yield bmp
    |]
let emptyFoundTriforce_bmps = [|
    for i = 0 to 7 do
        let bmp = emptyTriforce_bmp.Clone() :?> System.Drawing.Bitmap
        paintAlphanumerics3x5(char(int '1' + i), System.Drawing.Color.White, bmp, 4, 4)
        yield bmp
    |]
let fullTriforce_bmps = [|
    for i = 0 to 7 do
        let bmp = fullTriforce_bmp.Clone() :?> System.Drawing.Bitmap
        paintAlphanumerics3x5(char(int '1' + i), System.Drawing.Color.White, bmp, 4, 4)
        yield bmp
    |]
let unfoundL9_bmp,foundL9_bmp =
    let u = new System.Drawing.Bitmap(10*3,10*3)
    let f = new System.Drawing.Bitmap(10*3,10*3)
    paintAlphanumerics3x5('9', UNFOUND_NUMERAL_COLOR, u, 4, 4)
    paintAlphanumerics3x5('9', System.Drawing.Color.White, f, 4, 4)
    u, f
    

let allItemBMPs = [| book_bmp; boomerang_bmp; bow_bmp; power_bracelet_bmp; ladder_bmp; magic_boomerang_bmp; key_bmp; raft_bmp; recorder_bmp; red_candle_bmp; red_ring_bmp; silver_arrow_bmp; wand_bmp; white_sword_bmp |]
let allItemBMPsWithHeartShuffle = [| yield! allItemBMPs; for i = 0 to 8 do yield heart_container_bmp |]

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
                    ur.SetPixel(px, py, bmp.GetPixel(px/3 + i*13,    py/3))
                    cr.SetPixel(px, py, bmp.GetPixel(px/3 + i*13,  9+py/3))
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
    let imageStream = GetResourceStream("s_map_overworld_strip8.png")
    // 8 maps in here: 1st quest, 2nd quest, 1st quest with mixed secrets, 2nd quest with mixed secrets, and then horizontal-reflected versions of each of those
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


let emptyUnfoundTriforces, emptyFoundTriforces , fullTriforces = emptyUnfoundTriforce_bmps |> Array.map BMPtoImage, emptyFoundTriforce_bmps |> Array.map BMPtoImage, fullTriforce_bmps |> Array.map BMPtoImage
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
let uniqueMapIconBMPs =
    let imageStream = GetResourceStream("ow_icons5x9.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let tiles = [|  
        for i = 1 to 9 do  // levels 1-9
            let b = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    if px>=5*3 && px<10*3 && py>=1*3 && py<10*3 then 
                        b.SetPixel(px, py, System.Drawing.Color.Yellow)
                    else
                        b.SetPixel(px, py, TRANS_BG)
            paintAlphanumerics3x5(i.ToString().[0], System.Drawing.Color.Black, b, 6, 3)
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
        for i = 0 to 0 do  // 'X'
            let b = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    b.SetPixel(px, py, System.Drawing.Color.Black)
            yield b
        |]
    tiles

let mapIconInteriorBMPs =
    [|
    for bmp in [yield! uniqueMapIconBMPs; yield! nonUniqueMapIconBMPs] do
        let r = new System.Drawing.Bitmap(5*3,9*3)
        for px = 0 to 5*3-1 do
            for py = 0 to 9*3-1 do
                r.SetPixel(px, py, bmp.GetPixel(5*3+px, 1*3+py))
        yield r
    |]

let mouseIconButtonColorsBMP =
    let imageStream = GetResourceStream("mouse-icon-button-colors.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    bmp
let mouseIconButtonColors2BMP =
    let imageStream = GetResourceStream("mouse-icon-button-colors-2.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    bmp

let overworldCommonestFloorColorBrush = new SolidColorBrush(Color.FromRgb(204uy,176uy,136uy))
