module Graphics

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

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

let emptyZHelper =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ZHelperEmpty.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    for i = 0 to bmp.Width-1 do
        for j = 0 to bmp.Height-1 do
            let c = bmp.GetPixel(i,j)
            if false then //c.R = 53uy && c.G = 53uy && c.B = 53uy then
                bmp.SetPixel(i, j, System.Drawing.Color.Black)
    bmp
let fullZHelper =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ZHelperFull.png")
    let bmp = new System.Drawing.Bitmap(imageStream)
    for i = 0 to bmp.Width-1 do
        for j = 0 to bmp.Height-1 do
            let c = bmp.GetPixel(i,j)
            if c.R = 53uy && c.G = 53uy && c.B = 53uy then
                bmp.SetPixel(i, j, System.Drawing.Color.Black)
            if c.R = 27uy && c.G = 27uy && c.B = 53uy then
                bmp.SetPixel(i, j, System.Drawing.Color.Black)
    bmp
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
                bmp.MakeTransparent(System.Drawing.Color.Black)
                yield bmp
        |]
    BMPtoImage items.[0], BMPtoImage items.[1], BMPtoImage items.[2], BMPtoImage items.[4], BMPtoImage items.[6], BMPtoImage items.[8], 
        BMPtoImage items.[10], BMPtoImage items.[12], BMPtoImage items.[14], BMPtoImage items.[15], BMPtoImage items.[16], BMPtoImage items.[17]

let mutable heart_container_bmp = null
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
                bmp.MakeTransparent(System.Drawing.Color.Black)
                yield bmp
        |]
    heart_container_bmp <- items.[0]
    BMPtoImage items.[0], BMPtoImage items.[2], BMPtoImage items.[4], BMPtoImage items.[3]
let copyHeartContainer() =
    let bmp = new System.Drawing.Bitmap(7*3,7*3)
    for i = 0 to 20 do
        for j = 0 to 20 do
            bmp.SetPixel(i, j, heart_container_bmp.GetPixel(i,j))
    BMPtoImage bmp

let allItems = [| book; boomerang; bow; power_bracelet; ladder; magic_boomerang; key; raft; recorder; red_candle; red_ring; silver_arrow; wand; white_sword; heart_container |]
let allItemsWithHeartShuffle = 
    [| yield! allItems; for i = 0 to 7 do yield copyHeartContainer() |]

let ow_key_white_sword = 
    let zh = fullZHelper
    let bmp = new System.Drawing.Bitmap(7*3,7*3)
    let xoff,yoff = 574+36*2, 91+30*0  // index into ZHelperFull
    for px = 0 to 7*3-1 do
        for py = 0 to 7*3-1 do
            bmp.SetPixel(px, py, zh.GetPixel(xoff + px, yoff + py))
    BMPtoImage bmp
let brown_sword, magical_sword = 
    let zh = fullZHelper
    let bmp1 = new System.Drawing.Bitmap(7*3,7*3)
    let bmp2 = new System.Drawing.Bitmap(7*3,7*3)
    let xoff,yoff = 574+36*2, 91+30*0  // index into ZHelperFull
    for px = 0 to 7*3-1 do
        for py = 0 to 7*3-1 do
            let c = zh.GetPixel(xoff + px, yoff + py)
            let c1,c2 =
                if c.R = 255uy && c.G = 255uy && c.B = 255uy then
                    System.Drawing.Color.Brown, c
                elif c.R = 128uy && c.G = 128uy && c.B = 255uy then
                    System.Drawing.Color.LightGreen, System.Drawing.Color.Red 
                else
                    c,c
            bmp1.SetPixel(px, py, c1)
            bmp2.SetPixel(px, py, c2)
    bmp1.MakeTransparent(System.Drawing.Color.Black)
    bmp2.MakeTransparent(System.Drawing.Color.Black)
    BMPtoImage bmp1, BMPtoImage bmp2
let blue_candle = 
    let zh = fullZHelper
    let i,j = 6,0
    let bmp = new System.Drawing.Bitmap(7*3,7*3)
    let xoff,yoff = 250+36*i, 61+36*j  // index into ZHelperFull
    for px = 0 to 7*3-1 do
        for py = 0 to 7*3-1 do
            let c = zh.GetPixel(xoff + px, yoff + py)
            let c = 
                if c.R = 255uy && c.G = 0uy && c.B = 0uy then
                    System.Drawing.Color.FromArgb(96,96,255)
                else
                    c
            bmp.SetPixel(px, py, c)
    bmp.MakeTransparent(System.Drawing.Color.Black)
    BMPtoImage bmp
let blue_ring = 
    let zh = fullZHelper
    let i,j = 8,1
    let bmp = new System.Drawing.Bitmap(7*3,7*3)
    let xoff,yoff = 250+36*i, 61+36*j  // index into ZHelperFull
    for px = 0 to 7*3-1 do
        for py = 0 to 7*3-1 do
            let c = zh.GetPixel(xoff + px, yoff + py)
            let c = 
                if c.R = 255uy && c.G = 0uy && c.B = 0uy then
                    System.Drawing.Color.FromArgb(96,96,255)
                else
                    c
            bmp.SetPixel(px, py, c)
    bmp.MakeTransparent(System.Drawing.Color.Black)
    BMPtoImage bmp

let ow_key_ladder = 
    let zh = fullZHelper
    let bmp = new System.Drawing.Bitmap(7*3,7*3)
    let xoff,yoff = 250+36*3, 61+36*0  // index into ZHelperFull
    for px = 0 to 7*3-1 do
        for py = 0 to 7*3-1 do
            bmp.SetPixel(px, py, zh.GetPixel(xoff + px, yoff + py))
    BMPtoImage bmp
    
let emptyUnfoundTriforces, emptyFoundTriforces = 
    let zh = emptyZHelper
    let a = [|
        for i = 0 to 7 do
            let bmp1 = new System.Drawing.Bitmap(10*3,10*3)
            let bmp2 = new System.Drawing.Bitmap(10*3,10*3)
            let xoff,yoff = 1,29  // index into ZHelperEmpty
            for px = 0 to 10*3-1 do
                for py = 0 to 10*3-1 do
                    let c = zh.GetPixel(xoff + i*10*3 + px, yoff + py)
                    bmp1.SetPixel(px, py, c)
                    bmp2.SetPixel(px, py, if c.ToArgb() = System.Drawing.Color.FromArgb(128,128,128).ToArgb() then System.Drawing.Color.White else c)
            bmp1.MakeTransparent(System.Drawing.Color.Black)
            bmp2.MakeTransparent(System.Drawing.Color.Black)
            yield BMPtoImage bmp1, BMPtoImage bmp2
    |]
    a |> Array.map fst, a |> Array.map snd
let fullTriforces = 
    let zh = fullZHelper
    [|
        for i = 0 to 7 do
            let bmp = new System.Drawing.Bitmap(10*3,10*3)
            let xoff,yoff = 1,52  // index into ZHelperFull
            for px = 0 to 10*3-1 do
                for py = 0 to 10*3-1 do
                    bmp.SetPixel(px, py, zh.GetPixel(xoff + i*10*3 + px, yoff + py))
            bmp.MakeTransparent(System.Drawing.Color.Black)
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
let timelineHeart = 
    let zh = fullZHelper
    let bmp = new System.Drawing.Bitmap(9*3,9*3)
    let xoff,yoff = 1,82  // index into ZHelperFull
    for px = 3 to 10*3-1 do
        for py = 3 to 10*3-1 do
            bmp.SetPixel(px-3, py-3, zh.GetPixel(xoff + px, yoff + py))
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

let owMapSquaresFirstQuestAlwaysEmpty = [|
    "X.X...X.XX......"
    ".X...X.XXX.X...."
    "X........XXX..X."
    "XXX..XX.XXXX..XX"
    "XX.X........X..X"
    "X.XXXX.XXXX.XX.X"
    "XX...X...X..X.X."
    "..XX......X...XX"
    |]

let owMapSquaresSecondQuestAlwaysEmpty = [|
    ".....X..X..X...."
    ".......X........"
    ".X.....X..X.X.X."
    ".XX..XX.XX.X..XX"
    "XXXX...X....X..X"
    "X.X.XX.X.XX.XX.X"
    ".XX..X.X.X.X.X.."
    ".X.X......XX..XX"
    |]

let owMapSquaresMixedQuestAlwaysEmpty = [|
    for i = 0 to 7 do
        let mutable s = ""
        for j = 0 to 15 do
            if owMapSquaresFirstQuestAlwaysEmpty.[i].[j] = 'X' && owMapSquaresSecondQuestAlwaysEmpty.[i].[j] = 'X' then
                s <- s + "X"
            else
                s <- s + "."
        yield s
    |]

let owMapSquaresFirstQuestOnlyIfMixed = [|
    for i = 0 to 7 do
        let mutable s = ""
        for j = 0 to 15 do
            if owMapSquaresFirstQuestAlwaysEmpty.[i].[j] = 'X' && owMapSquaresSecondQuestAlwaysEmpty.[i].[j] <> 'X' then
                s <- s + "X"
            else
                s <- s + "."
        yield s
    |]

let owMapSquaresSecondQuestOnlyIfMixed = [|
    for i = 0 to 7 do
        let mutable s = ""
        for j = 0 to 15 do
            if owMapSquaresFirstQuestAlwaysEmpty.[i].[j] <> 'X' && owMapSquaresSecondQuestAlwaysEmpty.[i].[j] = 'X' then
                s <- s + "X"
            else
                s <- s + "."
        yield s
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
        for i in [yield![24..26]; yield![28..31]; yield 15; yield 32; yield 34; yield 38] do
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

let dungeonVChuteBMP =
    let tile = dungeonExploredRoomBMP.Clone() :?> System.Drawing.Bitmap 
    let c = tile.GetPixel(0,0)
    for py = 0 to 9*3-1 do
        tile.SetPixel(13, py, c)
        tile.SetPixel(14, py, c)
        tile.SetPixel(15, py, c)
        tile.SetPixel(23, py, c)
        tile.SetPixel(24, py, c)
        tile.SetPixel(25, py, c)
    tile    

let dungeonHChuteBMP =
    let tile = dungeonExploredRoomBMP.Clone() :?> System.Drawing.Bitmap 
    let c = tile.GetPixel(0,0)
    for px = 0 to 13*3-1 do
        tile.SetPixel(px,  9, c)
        tile.SetPixel(px, 10, c)
        tile.SetPixel(px, 11, c)
        tile.SetPixel(px, 15, c)
        tile.SetPixel(px, 16, c)
        tile.SetPixel(px, 17, c)
    tile    

let dungeonTeeBMP =
    let tile = dungeonExploredRoomBMP.Clone() :?> System.Drawing.Bitmap 
    let c = tile.GetPixel(0,0)
    for py = 5*3 to 9*3-1 do
        tile.SetPixel(15, py, c)
        tile.SetPixel(16, py, c)
        tile.SetPixel(17, py, c)
        tile.SetPixel(21, py, c)
        tile.SetPixel(22, py, c)
        tile.SetPixel(23, py, c)
    for px = 4*3 to 9*3-1 do
        for py = 9 to 11 do
            tile.SetPixel(px,  py, c)
    for px in [12;13;14;24;25;26] do
        for py = 12 to 17 do
            tile.SetPixel(px,  py, c)
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
