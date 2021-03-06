﻿module Graphics

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
    
let makeVBrect(image) =
    // we need a point of indirection to swap the book and magical_shield icons, so a VisualBrush where we can poke the Visual works
    let vb = new VisualBrush(Visual=image, Opacity=1.0)
    let rect = new System.Windows.Shapes.Rectangle(Height=21., Width=21., Fill=vb)
    rect
// most of these need object identity for logic checks 
let boomerang, bow, magic_boomerang, raft, ladder, recorder, wand, red_candle, book, key, silver_arrow, red_ring, boom_book, recorder_audio_copy, magic_shield_image, book_image = 
    let zh = fullZHelper
    let makeObject(bmp) = makeVBrect(BMPtoImage bmp)
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
    makeObject items.[0], makeObject items.[1], makeObject items.[2], makeObject items.[4], makeObject items.[6], makeObject items.[8], 
        makeObject items.[10], makeObject items.[12], makeObject items.[14], makeObject items.[15], makeObject items.[16], makeObject items.[17],
        (
        let bmp = new System.Drawing.Bitmap(7*3,7*3)
        // book
        for px = 0 to 7*3-1 do
            for py = 0 to 7*3-1 do
                bmp.SetPixel(px, py, items.[14].GetPixel(px, py))
        // add bomb drawing on it
        for px = 8 to 13 do
            for py = 13 to 18 do
                bmp.SetPixel(px, py, System.Drawing.Color.Blue)
        bmp.SetPixel(10, 12, System.Drawing.Color.White)
        bmp.SetPixel(11, 11, System.Drawing.Color.White)
        makeObject bmp
        ),
        makeObject items.[8], BMPtoImage items.[7], BMPtoImage items.[14]

let mutable heart_container_bmp = null
let heart_container, power_bracelet, white_sword, ow_key_armos, power_bracelet_audio_copy = 
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
    makeVBrect(BMPtoImage items.[0]), makeVBrect(BMPtoImage items.[2]), makeVBrect(BMPtoImage items.[4]), BMPtoImage items.[3], BMPtoImage items.[2]
let copyHeartContainer() =
    let bmp = new System.Drawing.Bitmap(7*3,7*3)
    for i = 0 to 20 do
        for j = 0 to 20 do
            bmp.SetPixel(i, j, heart_container_bmp.GetPixel(i,j))
    BMPtoImage bmp

let allItems = [| book; boomerang; bow; power_bracelet; ladder; magic_boomerang; key; raft; recorder; red_candle; red_ring; silver_arrow; wand; white_sword; heart_container |]
let allItemsWithHeartShuffle = 
    [| yield! allItems; for i = 0 to 7 do yield makeVBrect(copyHeartContainer()) |]

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

let nonUniqueMapIconBMPs = 
    let m = zhMapIcons 
    [|
        // 3-item shops
        for i in [yield![24..26]; yield![28..31]] do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    //tile.SetPixel(px, py, m.GetPixel((i*16*3 + px)/3, (py)/3))
                    if px/3 >= 4 && px/3 <= 10 && py/3 >= 1 && py/3 <= 9 then
                        let c = m.GetPixel((i*16*3 + px + 12)/3, (py)/3)
                        tile.SetPixel(px, py, c)
                    else
                        tile.SetPixel(px, py, TRANS_BG)
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

let dungeonStartBMP =
    let m = zhDungeonIcons
    let tile = new System.Drawing.Bitmap(13*3, 9*3)
    for px = 0 to 13*3-1 do
        for py = 0 to 9*3-1 do
            if px<3 || px>12*3-1 || py<3 || py>8*3-1 then
                tile.SetPixel(px, py, System.Drawing.Color.LightGray)
            else
                tile.SetPixel(px, py, System.Drawing.Color.Green)
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

let ganon,zelda = 
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ganonzelda.png")
    let GZ = new System.Drawing.Bitmap(imageStream)
    let bmp = new System.Drawing.Bitmap(7*3,7*3)
    for px = 0 to 7*3-1 do
        for py = 0 to 7*3-1 do
            bmp.SetPixel(px, py, GZ.GetPixel(px/3, py/3))
    bmp.MakeTransparent(System.Drawing.Color.Black)
    let g = BMPtoImage bmp
    let bmp = new System.Drawing.Bitmap(7*3,7*3)
    for px = 0 to 7*3-1 do
        for py = 0 to 7*3-1 do
            bmp.SetPixel(px, py, GZ.GetPixel(px/3 + 7, py/3))
    bmp.MakeTransparent(System.Drawing.Color.Black)
    let z = BMPtoImage bmp
    g, z