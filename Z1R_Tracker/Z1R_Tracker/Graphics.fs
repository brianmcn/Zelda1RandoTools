module Graphics

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let BMPtoImage(bmp:System.Drawing.Bitmap) =
    let ms = new System.IO.MemoryStream()
    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp)
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

let emptyZHelper =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ZHelperEmpty.png")
    new System.Drawing.Bitmap(imageStream)
let fullZHelper =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ZHelperFull.png")
    new System.Drawing.Bitmap(imageStream)
let overworldImage =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("s_map_overworld.png")
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
    
let boomerang, bow, magic_boomerang, raft, ladder, recorder, wand, red_candle, book, key, silver_arrow, red_ring = 
    let zh = fullZHelper
    let items = 
        [|
        for i = 0 to 8 do
            for j = 0 to 1 do
                let bmp = new System.Drawing.Bitmap(7*3,7*3)
                let xoff,yoff = 250+36*i, 61+36*j  // index into ZHelperFull
                for px = 0 to 7*3-1 do
                    for py = 0 to 7*3-1 do
                        bmp.SetPixel(px, py, zh.GetPixel(xoff + px, yoff + py))
                yield bmp
        |]
    BMPtoImage items.[0], BMPtoImage items.[1], BMPtoImage items.[2], BMPtoImage items.[4], BMPtoImage items.[6], BMPtoImage items.[8], 
        BMPtoImage items.[10], BMPtoImage items.[12], BMPtoImage items.[14], BMPtoImage items.[15], BMPtoImage items.[16], BMPtoImage items.[17]

let heart_container, power_bracelet, white_sword, ow_key_armos = 
    let zh = fullZHelper
    let items = 
        [|
        for i = 0 to 2 do
            for j = 0 to 1 do
                let bmp = new System.Drawing.Bitmap(7*3,7*3)
                let xoff,yoff = 574+36*i, 91+30*j  // index into ZHelperFull
                for px = 0 to 7*3-1 do
                    for py = 0 to 7*3-1 do
                        bmp.SetPixel(px, py, zh.GetPixel(xoff + px, yoff + py))
                yield bmp
        |]
    BMPtoImage items.[0], BMPtoImage items.[2], BMPtoImage items.[4], BMPtoImage items.[3]

let allItems = [| book; boomerang; bow; power_bracelet; ladder; magic_boomerang; key; raft; recorder; red_candle; red_ring; silver_arrow; wand; white_sword; heart_container |]

let ow_key_white_sword = 
    let zh = fullZHelper
    let bmp = new System.Drawing.Bitmap(7*3,7*3)
    let xoff,yoff = 574+36*2, 91+30*0  // index into ZHelperFull
    for px = 0 to 7*3-1 do
        for py = 0 to 7*3-1 do
            bmp.SetPixel(px, py, zh.GetPixel(xoff + px, yoff + py))
    BMPtoImage bmp

let ow_key_ladder = 
    let zh = fullZHelper
    let bmp = new System.Drawing.Bitmap(7*3,7*3)
    let xoff,yoff = 250+36*3, 61+36*0  // index into ZHelperFull
    for px = 0 to 7*3-1 do
        for py = 0 to 7*3-1 do
            bmp.SetPixel(px, py, zh.GetPixel(xoff + px, yoff + py))
    BMPtoImage bmp
    
let emptyTriforces = 
    let zh = emptyZHelper
    [|
        for i = 0 to 7 do
            let bmp = new System.Drawing.Bitmap(10*3,10*3)
            let xoff,yoff = 1,29  // index into ZHelperEmpty
            for px = 0 to 10*3-1 do
                for py = 0 to 10*3-1 do
                    bmp.SetPixel(px, py, zh.GetPixel(xoff + i*10*3 + px, yoff + py))
            yield BMPtoImage bmp
    |]
let fullTriforces = 
    let zh = fullZHelper
    [|
        for i = 0 to 7 do
            let bmp = new System.Drawing.Bitmap(10*3,10*3)
            let xoff,yoff = 1,52  // index into ZHelperFull
            for px = 0 to 10*3-1 do
                for py = 0 to 10*3-1 do
                    bmp.SetPixel(px, py, zh.GetPixel(xoff + i*10*3 + px, yoff + py))
            yield BMPtoImage bmp
    |]
let emptyHearts = 
    let zh = emptyZHelper
    [|
        for i = 0 to 7 do
            let bmp = new System.Drawing.Bitmap(10*3,10*3)
            let xoff,yoff = 1,59  // index into ZHelperEmpty
            for px = 0 to 10*3-1 do
                for py = 0 to 10*3-1 do
                    bmp.SetPixel(px, py, zh.GetPixel(xoff + px, yoff + py))
            yield BMPtoImage bmp
    |]
let fullHearts = 
    let zh = fullZHelper
    [|
        for i = 0 to 7 do
            let bmp = new System.Drawing.Bitmap(10*3,10*3)
            let xoff,yoff = 1,82  // index into ZHelperFull
            for px = 0 to 10*3-1 do
                for py = 0 to 10*3-1 do
                    bmp.SetPixel(px, py, zh.GetPixel(xoff + px, yoff + py))
            yield BMPtoImage bmp
    |]
let owHeartsSkipped = 
    let zh = fullZHelper
    [|
        for i = 0 to 3 do
            let bmp = new System.Drawing.Bitmap(10*3,10*3)
            let xoff,yoff = 646,52  // index into ZHelperFull
            for px = 0 to 10*3-1 do
                for py = 0 to 10*3-1 do
                    bmp.SetPixel(px, py, zh.GetPixel(xoff + px, yoff + py))
            yield BMPtoImage bmp
    |]
let owHeartsEmpty = 
    let zh = emptyZHelper
    [|
        for i = 0 to 3 do
            let bmp = new System.Drawing.Bitmap(10*3,10*3)
            let xoff,yoff = 1,59  // index into ZHelperEmpty
            for px = 0 to 10*3-1 do
                for py = 0 to 10*3-1 do
                    bmp.SetPixel(px, py, zh.GetPixel(xoff + px, yoff + py))
            yield BMPtoImage bmp
    |]
let owHeartsFull = 
    let zh = fullZHelper
    [|
        for i = 0 to 3 do
            let bmp = new System.Drawing.Bitmap(10*3,10*3)
            let xoff,yoff = 1,82  // index into ZHelperFull
            for px = 0 to 10*3-1 do
                for py = 0 to 10*3-1 do
                    bmp.SetPixel(px, py, zh.GetPixel(xoff + px, yoff + py))
            yield BMPtoImage bmp
    |]

let overworldMapBMPs =
    let m = overworldImage
    let tiles = Array2D.zeroCreate 16 8
    for x = 0 to 15 do
        for y = 0 to 7 do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    tile.SetPixel(px, py, m.GetPixel((x*16*3 + px)/3, (y*11*3 + py)/3))
            tiles.[x,y] <- tile
    tiles

let owMapSquaresAlwaysEmpty = [|
    "X.X...X.XX......"
    ".X...X.XXX.X...."
    "X........XXX..X."
    "XXX..XX.XXXX..XX"
    "XX.X........X..X"
    "X.XXXX.XXXX.XX.."
    "XX...X...X..X.X."
    "..XX......X...XX"
    |]

let uniqueMapIcons =
    let m = zhMapIcons 
    [|
        // levels 1-9, warps 1-4, sword 3, sword 2
        for i in [yield![2..14]; yield![19..20]] do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    tile.SetPixel(px, py, m.GetPixel((i*16*3 + px)/3, (py)/3))
            yield BMPtoImage tile
    |]

let nonUniqueMapIconBMPs = 
    let m = zhMapIcons 
    [|
        for i in [yield![24..26]; yield![28..32]; yield 34; yield 38] do
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

let dungeonUnexploredRoomBMP =
    let m = zhDungeonIcons
    let tile = new System.Drawing.Bitmap(13*3, 9*3)
    for px = 0 to 13*3-1 do
        for py = 0 to 9*3-1 do
            tile.SetPixel(px, py, m.GetPixel(px/3, py/3))
    tile

let dungeonExploredRoomBMP =
    let m = zhDungeonIcons
    let tile = new System.Drawing.Bitmap(13*3, 9*3)
    for px = 0 to 13*3-1 do
        for py = 0 to 9*3-1 do
            tile.SetPixel(px, py, m.GetPixel(13+px/3, py/3))
    tile

let dungeonTriforceBMP =
    let m = zhDungeonIcons
    let tile = new System.Drawing.Bitmap(13*3, 9*3)
    for px = 0 to 13*3-1 do
        for py = 0 to 9*3-1 do
            tile.SetPixel(px, py, System.Drawing.Color.Yellow)
    tile

let dungeonPrincessBMP =
    let m = zhDungeonIcons
    let tile = new System.Drawing.Bitmap(13*3, 9*3)
    for px = 0 to 13*3-1 do
        for py = 0 to 9*3-1 do
            tile.SetPixel(px, py, System.Drawing.Color.Red)
    tile

let dungeonNumberBMPs = 
    let m = zhDungeonNums
    let x = System.Drawing.Color.FromArgb(255, 128, 128, 128)
    let colors = 
        [|
            System.Drawing.Color.Pink 
            System.Drawing.Color.Aqua
            System.Drawing.Color.Orange 
            System.Drawing.Color.FromArgb(0, 140, 0)
            System.Drawing.Color.FromArgb(230, 0, 230) 
            System.Drawing.Color.FromArgb(220, 220, 0)
            System.Drawing.Color.Lime 
            System.Drawing.Color.Brown 
            System.Drawing.Color.Blue
        |]
    [|
        for i = 0 to 8 do
            let tile = dungeonExploredRoomBMP.Clone() :?> System.Drawing.Bitmap 
            for px = 0 to 9*3-1 do
                for py = 0 to 9*3-1 do
                    let c = m.GetPixel((i*9*3+px)/3, py/3)
                    let r = 
                        if c.ToArgb() = x.ToArgb() then
                            colors.[i]
                        else
                            c
                    tile.SetPixel(px+2*3, py, r)
            yield tile
    |]
