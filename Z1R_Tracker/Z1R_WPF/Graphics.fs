module Graphics

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let [| boomerang_bmp; bow_bmp; magic_boomerang_bmp; raft_bmp; ladder_bmp; recorder_bmp; wand_bmp; red_candle_bmp; book_bmp; key_bmp; 
        silver_arrow_bmp; wood_arrow_bmp; red_ring_bmp; magic_shield_bmp; boom_book_bmp; 
        heart_container_bmp; power_bracelet_bmp; white_sword_bmp; ow_key_armos_bmp;
        brown_sword_bmp; magical_sword_bmp; blue_candle_bmp; blue_ring_bmp;
        ganon_bmp; zelda_bmp; bomb_bmp |] =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("icons7x7.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    [|  for i = 0 to bmp.Width/7 - 1 do
            let r = new System.Drawing.Bitmap(7*3,7*3)
            for px = 0 to 7*3-1 do
                for py = 0 to 7*3-1 do
                    r.SetPixel(px, py, bmp.GetPixel(px/3 + i*7, py/3))
            yield r
    |]

let emptyUnfoundTriforce_bmps, emptyFoundTriforce_bmps, fullTriforce_bmps, owHeartSkipped_bmp, owHeartEmpty_bmp, owHeartFull_bmp = 
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("icons10x10.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    let all = [|
        for i = 0 to bmp.Width/10 - 1 do
            let r = new System.Drawing.Bitmap(10*3,10*3)
            for px = 0 to 10*3-1 do
                for py = 0 to 10*3-1 do
                    r.SetPixel(px, py, bmp.GetPixel(px/3 + i*10, py/3))
            yield r
        |]
    all.[0..7], all.[8..15], all.[16..23], all.[24], all.[25], all.[26]

let [| dungeonUnexploredRoomBMP; dungeonExploredRoomBMP; dungeonVChuteBMP; dungeonHChuteBMP; dungeonTeeBMP; dungeonTriforceBMP; dungeonPrincessBMP; dungeonStartBMP;
        dn1bmp; dn2bmp; dn3bmp; dn4bmp; dn5bmp; dn6bmp; dn7bmp; dn8bmp; dn9bmp |] =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("icons13x9.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    [|  for i = 0 to bmp.Width/13 - 1 do
            let r = new System.Drawing.Bitmap(13*3,9*3)
            for px = 0 to 13*3-1 do
                for py = 0 to 9*3-1 do
                    r.SetPixel(px, py, bmp.GetPixel(px/3 + i*13, py/3))
            yield r
    |]
let dungeonNumberBMPs = [| dn1bmp; dn2bmp; dn3bmp; dn4bmp; dn5bmp; dn6bmp; dn7bmp; dn8bmp; dn9bmp |]

let [| cdungeonUnexploredRoomBMP; cdungeonExploredRoomBMP; cdungeonVChuteBMP; cdungeonHChuteBMP; cdungeonTeeBMP; cdungeonTriforceBMP; cdungeonPrincessBMP; cdungeonStartBMP;
        cdn1bmp; cdn2bmp; cdn3bmp; cdn4bmp; cdn5bmp; cdn6bmp; cdn7bmp; cdn8bmp; cdn9bmp |] =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("icons13x9.png")
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
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("s_map_overworld_strip8.png")
    // 8 maps in here: 1st quest, 2nd quest, 1st quest with mixed secrets, 2nd quest with mixed secrets, and then horizontal-reflected versions of each of those
    new System.Drawing.Bitmap(imageStream)
let zhMapIcons =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("s_icon_overworld_strip39.png")
    new System.Drawing.Bitmap(imageStream)
let zhDungeonIcons =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("s_btn_tr_dungeon_cell_strip3.png")
    new System.Drawing.Bitmap(imageStream)
let zhDungeonNums =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("s_btn_tr_dungeon_num_strip18.png")
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

let allItems = [| book; boomerang; bow; power_bracelet; ladder; magic_boomerang; key; raft; recorder; red_candle; red_ring; silver_arrow; wand; white_sword |]
let allItemsWithHeartShuffle = 
    [| yield! allItems; for i = 0 to 8 do yield makeVBrect(copyHeartContainer()) |]

let emptyUnfoundTriforces, emptyFoundTriforces , fullTriforces = emptyUnfoundTriforce_bmps |> Array.map BMPtoImage, emptyFoundTriforce_bmps |> Array.map BMPtoImage, fullTriforce_bmps |> Array.map BMPtoImage
let owHeartsSkipped, owHeartsEmpty, owHeartsFull = Array.init 4 (fun _ -> BMPtoImage owHeartSkipped_bmp), Array.init 4 (fun _ -> BMPtoImage owHeartEmpty_bmp), Array.init 4 (fun _ -> BMPtoImage owHeartFull_bmp)
let timelineHeart = 
    let bmp = new System.Drawing.Bitmap(9*3,9*3)
    for x = 1*3 to 10*3-1 do
        for y = 1*3 to 10*3-1 do
            bmp.SetPixel(x-3, y-3, owHeartFull_bmp.GetPixel(x,y))
    BMPtoImage bmp

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
let uniqueMapIcons, d1bmp, w1bmp =
    let m = zhMapIcons 
    let BLACK = m.GetPixel(( 9*16*3 + 24)/3, (21)/3)
    let tiles = [|
        // levels 1-9
        for i in [2..10] do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    if px/3 >= 5 && px/3 <= 9 && py/3 >= 2 && py/3 <= 8 then
                        let c = m.GetPixel((i*16*3 + px)/3, (py)/3)
                        tile.SetPixel(px, py, if c = BLACK then c else System.Drawing.Color.Yellow)
                    else
                        tile.SetPixel(px, py, TRANS_BG)
            yield tile
        // warps 1-4
        for i in [11..14] do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    if px/3 >= 5 && px/3 <= 9 && py/3 >= 2 && py/3 <= 8 then
                        let c = m.GetPixel(((i-9)*16*3 + px)/3, (py)/3)
                        tile.SetPixel(px, py, if c = BLACK then c else System.Drawing.Color.Aqua)
                    else
                        tile.SetPixel(px, py, TRANS_BG)
            yield tile
        // sword 3, sword 2
        for i in [19..20] do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    tile.SetPixel(px, py, m.GetPixel((i*16*3 + px)/3, (py)/3))
            yield tile
    |]
    tiles |> Array.map BMPtoImage, tiles.[0], tiles.[9]

let itemBackgroundColor = System.Drawing.Color.FromArgb(0xEF,0x83,0)
let itemsBMP = 
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("icons3x7.png")
    new System.Drawing.Bitmap(imageStream)
let nonUniqueMapIconBMPs = 
    let m = zhMapIcons 
    [|
        // hint shop
        for i in [22] do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    tile.SetPixel(px, py, m.GetPixel((i*16*3 + px)/3, (py)/3))
            yield tile
        // 3-item shops
        for i = 0 to 6 do
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
        // others
        for i in [yield 15; yield 32; yield 34; yield 38] do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    tile.SetPixel(px, py, m.GetPixel((i*16*3 + px)/3, (py)/3))
                    if i=38 then
                        if tile.GetPixel(px,py).ToArgb() = System.Drawing.Color.Black.ToArgb() then
                            tile.SetPixel(px,py,System.Drawing.Color.DarkGray)
                        else
                            tile.SetPixel(px,py,System.Drawing.Color.Black)
            yield tile
    |]

let ganon,zelda,bomb = 
    BMPtoImage ganon_bmp, BMPtoImage zelda_bmp, BMPtoImage bomb_bmp