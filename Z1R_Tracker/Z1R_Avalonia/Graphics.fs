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
    ms.Seek(0L, System.IO.SeekOrigin.Begin) |> ignore
    let bmp = new Avalonia.Media.Imaging.Bitmap(ms)
    let image = new Avalonia.Controls.Image()
    image.Source <- bmp
    image.Width <- bmp.Size.Width
    image.Height <- bmp.Size.Height
    image

let OMTW = 40.  // overworld map tile width - at normal aspect ratio, is 48 (16*3)
type TileHighlightRectangle() =
    (*
    // full rectangles badly obscure routing paths, so we just draw corners
    let L1,L2,R1,R2 = 2.+0.0, 2.+(OMTW-4.)/2.-6., 2.+(OMTW-4.)/2.+6., 2.+OMTW-4.
    let T1,T2,B1,B2 = 2.+0.0, 2.+10.0, 2.+19.0, 2.+29.0
    *)
    let shapes = [|
        new Shapes.Rectangle(Width=OMTW,Height=11.*3.,Stroke=Brushes.Lime,StrokeThickness=4.,Opacity=1.0,IsHitTestVisible=false)
        (*
        new Shapes.Line(StartPoint=Point(L1,T1+1.5), EndPoint=Point(L2,T1+1.5), Stroke=Brushes.Lime, StrokeThickness = 3., IsHitTestVisible=false)
        new Shapes.Line(StartPoint=Point(L1+1.5,T1), EndPoint=Point(L1+1.5,T2), Stroke=Brushes.Lime, StrokeThickness = 3., IsHitTestVisible=false)
        new Shapes.Line(StartPoint=Point(L1,B2-1.5), EndPoint=Point(L2,B2-1.5), Stroke=Brushes.Lime, StrokeThickness = 3., IsHitTestVisible=false)
        new Shapes.Line(StartPoint=Point(L1+1.5,B1), EndPoint=Point(L1+1.5,B2), Stroke=Brushes.Lime, StrokeThickness = 3., IsHitTestVisible=false)
        new Shapes.Line(StartPoint=Point(R1,T1+1.5), EndPoint=Point(R2,T1+1.5), Stroke=Brushes.Lime, StrokeThickness = 3., IsHitTestVisible=false)
        new Shapes.Line(StartPoint=Point(R2-1.5,T1), EndPoint=Point(R2-1.5,T2), Stroke=Brushes.Lime, StrokeThickness = 3., IsHitTestVisible=false)
        new Shapes.Line(StartPoint=Point(R1,B2-1.5), EndPoint=Point(R2,B2-1.5), Stroke=Brushes.Lime, StrokeThickness = 3., IsHitTestVisible=false)
        new Shapes.Line(StartPoint=Point(R2-1.5,B1), EndPoint=Point(R2-1.5,B2), Stroke=Brushes.Lime, StrokeThickness = 3., IsHitTestVisible=false)
        *)
        |]
    member _this.MakeRed() = for s in shapes do (s.Stroke <- Brushes.Red; s.Opacity <- 0.7)
    member _this.MakeYellow() = for s in shapes do (s.Stroke <- Brushes.Yellow; s.Opacity <- 0.75)
    member _this.MakeGreen() = for s in shapes do (s.Stroke <- Brushes.Lime; s.Opacity <- 0.55)
    member _this.MakePaleRed() = for s in shapes do (s.Stroke <- Brushes.Red; s.Opacity <- 0.35)
    member _this.MakePaleYellow() = for s in shapes do (s.Stroke <- Brushes.Yellow; s.Opacity <- 0.4)
    member _this.MakePaleGreen() = for s in shapes do (s.Stroke <- Brushes.Lime; s.Opacity <- 0.3)
    member _this.Hide() = for s in shapes do (s.Opacity <- 0.0)
    member _this.Shapes = shapes

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

let alphaNumOnTransparentBmp(ch, color, w:int, h:int, x, y) =
    let r = new System.Drawing.Bitmap(w,h)
    for px = 0 to w-1 do
        for py = 0 to h-1 do
            r.SetPixel(px, py, System.Drawing.Color.Transparent)
    paintAlphanumerics3x5(ch, color, r, x, y)
    r

let (boomerang_bmp, bow_bmp, magic_boomerang_bmp, raft_bmp, ladder_bmp, recorder_bmp, wand_bmp, red_candle_bmp, book_bmp, key_bmp, 
        silver_arrow_bmp, wood_arrow_bmp, red_ring_bmp, magic_shield_bmp, boom_book_bmp, 
        heart_container_bmp, power_bracelet_bmp, white_sword_bmp, ow_key_armos_bmp,
        brown_sword_bmp, magical_sword_bmp, blue_candle_bmp, blue_ring_bmp,
        ganon_bmp, zelda_bmp, bomb_bmp, bow_and_arrow_bmp, bait_bmp) =
    let imageStream = GetResourceStream("icons7x7.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let a = [|  
        for i = 0 to bmp.Width/7 - 1 do
            let r = new System.Drawing.Bitmap(7*3,7*3)
            for px = 0 to 7*3-1 do
                for py = 0 to 7*3-1 do
                    r.SetPixel(px, py, bmp.GetPixel(px/3 + i*7, py/3))
            yield r
        |]
    (a.[0], a.[1], a.[2], a.[3], a.[4], a.[5], a.[6], a.[7], a.[8], a.[9],
        a.[10], a.[11], a.[12], a.[13], a.[14], a.[15], a.[16], a.[17], a.[18], a.[19],
        a.[20], a.[21], a.[22], a.[23], a.[24], a.[25], a.[26], a.[27])

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
    

let allItemBMPs = [| book_bmp; boomerang_bmp; bow_bmp; power_bracelet_bmp; ladder_bmp; magic_boomerang_bmp; key_bmp; raft_bmp; recorder_bmp; red_candle_bmp; red_ring_bmp; silver_arrow_bmp; wand_bmp; white_sword_bmp |]
let allItemBMPsWithHeartShuffle = [| yield! allItemBMPs; for _i = 0 to 8 do yield heart_container_bmp |]

let (cdungeonUnexploredRoomBMP, cdungeonExploredRoomBMP, cdungeonDoubleMoatBMP, cdungeonChevyBMP, cdungeonVMoatBMP, cdungeonHMoatBMP, 
        cdungeonVChuteBMP, cdungeonHChuteBMP, cdungeonTeeBMP, cdungeonNeedWand, cdungeonBlueBubble, cdungeonNeedRecorder, cdungeonNeedBow, cdungeonTriforceBMP, cdungeonPrincessBMP, cdungeonStartBMP,
        cdn1bmp, cdn2bmp, cdn3bmp, cdn4bmp, cdn5bmp, cdn6bmp, cdn7bmp, cdn8bmp, cdn9bmp) =
    let imageStream = GetResourceStream("icons13x9.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let a = [|  
        for i = 0 to bmp.Width/13 - 1 do
            let cr = new System.Drawing.Bitmap(13*3,9*3)
            let ur = new System.Drawing.Bitmap(13*3,9*3)
            for px = 0 to 13*3-1 do
                for py = 0 to 9*3-1 do
                    ur.SetPixel(px, py, bmp.GetPixel(px/3 + i*13,    py/3))
                    cr.SetPixel(px, py, bmp.GetPixel(px/3 + i*13,  9+py/3))
            yield (ur,cr)
    |]
    (a.[0], a.[1], a.[2], a.[3], a.[4], a.[5], a.[6], a.[7], a.[8], a.[9],
        a.[10], a.[11], a.[12], a.[13], a.[14], a.[15], a.[16], a.[17], a.[18], a.[19],
        a.[20], a.[21], a.[22], a.[23], a.[24])

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
let itemBackgroundColor = System.Drawing.Color.FromArgb(0xEF,0x83,0)
let itemsBMP = 
    let imageStream = GetResourceStream("icons3x7.png")
    new System.Drawing.Bitmap(imageStream)

// each overworld map tile may have multiple icons that can represent it (e.g. dungeon 1 versus dungeon A)
// we store a table, where the array index is the mapSquareChoiceDomain index of the general entry type, and the value there is a list of all possible icons
// MapStateProxy will eventually be responsible for 'decoding' the current tracker state into the appropriate icon
let theInteriorBmpTable = Array.init 28 (fun _ -> ResizeArray())
do
    let imageStream = GetResourceStream("ow_icons5x9.png")
    let interiorIconStrip = new System.Drawing.Bitmap(imageStream)
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
    // 14  sword2
    theInteriorBmpTable.[14].Add(getInteriorIconFromStrip(1))
    // 15  hint shop
    theInteriorBmpTable.[15].Add(getInteriorIconFromStrip(2))
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
    // 24  take any
    theInteriorBmpTable.[24].Add(getInteriorIconFromStrip(3))
    // 25  potion shop
    theInteriorBmpTable.[25].Add(getInteriorIconFromStrip(4))
    // 26  money
    theInteriorBmpTable.[26].Add(getInteriorIconFromStrip(5))
    // 27  'X'
    let bmp = new System.Drawing.Bitmap(5*3,9*3)
    for px = 0 to 5*3-1 do
        for py = 0 to 9*3-1 do
            bmp.SetPixel(px, py, System.Drawing.Color.Black)
    theInteriorBmpTable.[27].Add(bmp)
// full tiles just have interior bmp in the center and transparent pixels all around (except for the final 'X' one)
let theFullTileBmpTable = Array.init 28 (fun _ -> ResizeArray())
do
    for i = 0 to 27 do
        for interiorBmp in theInteriorBmpTable.[i] do
            let fullTileBmp = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    if px>=5*3 && px<10*3 && py>=1*3 && py<10*3 then 
                        fullTileBmp.SetPixel(px, py, interiorBmp.GetPixel(px-5*3, py-1*3))
                    else
                        fullTileBmp.SetPixel(px, py, if i=27 then System.Drawing.Color.Black else TRANS_BG)
            theFullTileBmpTable.[i].Add(fullTileBmp)

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

let WarpMouseCursorTo(pos:Avalonia.Point) =
    // TODO
    //Win32.SetCursor(pos.X, pos.Y)
    //PlaySoundForSpeechRecognizedAndUsedToMark()
    ignore(pos)
