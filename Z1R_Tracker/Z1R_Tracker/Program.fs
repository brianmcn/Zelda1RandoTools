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
    bmimage

let emptyZHelper =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ZHelperEmpty.png")
    new System.Drawing.Bitmap(imageStream)
let fullZHelper =
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ZHelperFull.png")
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
    items.[0], items.[1], items.[2], items.[4], items.[6], items.[8], items.[10], items.[12], items.[14], items.[15], items.[16], items.[17]

let heart_container, power_bracelet, white_sword, armos = 
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
    items.[0], items.[2], items.[4], items.[3]
    
let emptyTriforces = 
    let zh = emptyZHelper
    [|
        for i = 0 to 7 do
            let bmp = new System.Drawing.Bitmap(10*3,10*3)
            let xoff,yoff = 1,29  // index into ZHelperEmpty
            for px = 0 to 10*3-1 do
                for py = 0 to 10*3-1 do
                    bmp.SetPixel(px, py, zh.GetPixel(xoff + i*10*3 + px, yoff + py))
            yield bmp
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
            yield bmp
    |]
let emptyHeart = 
    let zh = emptyZHelper
    let bmp = new System.Drawing.Bitmap(10*3,10*3)
    let xoff,yoff = 1,59  // index into ZHelperEmpty
    for px = 0 to 10*3-1 do
        for py = 0 to 10*3-1 do
            bmp.SetPixel(px, py, zh.GetPixel(xoff + px, yoff + py))
    bmp
let fullHeart = 
    let zh = fullZHelper
    let bmp = new System.Drawing.Bitmap(10*3,10*3)
    let xoff,yoff = 1,82  // index into ZHelperFull
    for px = 0 to 10*3-1 do
        for py = 0 to 10*3-1 do
            bmp.SetPixel(px, py, zh.GetPixel(xoff + px, yoff + py))
    bmp
let skippedHeart = 
    let zh = fullZHelper
    let bmp = new System.Drawing.Bitmap(10*3,10*3)
    let xoff,yoff = 649,52  // index into ZHelperFull
    for px = 0 to 10*3-1 do
        for py = 0 to 10*3-1 do
            bmp.SetPixel(px, py, zh.GetPixel(xoff + px, yoff + py))
    bmp

let makeOverworldMap() =
    let ze = emptyZHelper
    // 1 160 3
    // 16x11(x3)
    let overworldMap = new System.Drawing.Bitmap(16*16*3,8*11*3)
    let xoff,yoff = 1,160  // index into ZHelperEmpty
    for x = 0 to 15 do
        for y = 0 to 7 do
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    overworldMap.SetPixel(x*16*3 + px, y*11*3 + py, ze.GetPixel(xoff + x*16*3 + px, yoff + y*16*3 + py))
    overworldMap

let H = emptyHeart.Height 
let makeAll() =
    let ow = makeOverworldMap()
    let all = new System.Drawing.Bitmap(ow.Width, H + H + H + H + ow.Height)
    for i = 0 to all.Width-1 do
        for j = 0 to all.Height-1 do
            all.SetPixel(i, j, System.Drawing.Color.Black)

    // triforce
    for i = 0 to 7 do
        let bmp = fullTriforces.[i]
        for px = 0 to 10*3-1 do
            for py = 0 to 10*3-1 do
                all.SetPixel(i*10*3 + px, py, bmp.GetPixel(px, py))
    // floor hearts
    for i = 0 to 7 do
        let bmp = fullHeart
        for px = 0 to 10*3-1 do
            for py = 0 to 10*3-1 do
                all.SetPixel(i*10*3 + px, H + py, bmp.GetPixel(px, py))
    // items
    let items = Array2D.zeroCreate 9 2
    items.[0,0] <- boomerang
    items.[0,1] <- bow
    items.[1,0] <- magic_boomerang
    items.[2,0] <- raft
    items.[3,0] <- ladder
    items.[4,0] <- recorder
    items.[5,0] <- wand
    items.[6,0] <- red_candle 
    items.[7,0] <- key
    items.[7,1] <- book
    items.[8,0] <- red_ring 
    items.[8,1] <- silver_arrow
    for i = 0 to 8 do
        for j = 0 to 1 do
            // box
            if j=0 || (i=0 || i=7 || i=8) then
                for px = 0 to 10*3-1 do
                    for py = 0 to 10*3-1 do
                        if px<3 || py<3 then
                            ()
                        elif px<6 || px>26 || py<6 || py>26 then
                            all.SetPixel(i*10*3 + px, j*H + H + H + py, System.Drawing.Color.Gray)
            // item
            if items.[i,j] <> null then
                for px = 0 to 7*3-1 do
                    for py = 0 to 7*3-1 do
                        all.SetPixel(i*10*3 + px + 6, j*H + H + H + py + 6, items.[i,j].GetPixel(px,py))

    let OFFSET = 400
    // ow hearts
    for i = 0 to 3 do
        let bmp = if i=0 then skippedHeart elif i=3 then emptyHeart else fullHeart
        for px = 0 to 10*3-1 do
            for py = 0 to 10*3-1 do
                all.SetPixel(OFFSET+i*10*3 + px, py, bmp.GetPixel(px, py))
    // ladder, armos, white sword items
    for j = 0 to 2 do
        // key
        for px = 0 to 7*3-1 do
            for py = 0 to 7*3-1 do
                all.SetPixel(OFFSET + px + 6, j*H + H + py + 6, (if j=0 then ladder elif j=1 then armos else white_sword).GetPixel(px,py))
        // box
        for px = 0 to 10*3-1 do
            for py = 0 to 10*3-1 do
                if px<3 || py<3 then
                    ()
                elif px<6 || px>26 || py<6 || py>26 then
                    all.SetPixel(OFFSET+40 + px, j*H + H + py, System.Drawing.Color.Gray)
        // item
        for px = 0 to 7*3-1 do
            for py = 0 to 7*3-1 do
                all.SetPixel(OFFSET+40 + px + 6, j*H + H + py + 6, (if j=0 then heart_container elif j=1 then power_bracelet else white_sword).GetPixel(px,py))

    for i = 0 to ow.Width-1 do
        for j = 0 to ow.Height-1 do
            all.SetPixel(i, H+H+H+H+j, ow.GetPixel(i,j))
    all

let highlightTile = 
    new System.Drawing.Bitmap(16*3, 11*3)

let average(pixel1:System.Drawing.Color, pixel2:System.Drawing.Color) =
    System.Drawing.Color.FromArgb(  (int pixel1.R + int pixel2.R) / 2,
                                    (int pixel1.G + int pixel2.G) / 2,
                                    (int pixel1.B + int pixel2.B) / 2 )

type MyWindow() as this = 
    inherit Window()
    let i = new Image()
    let content = i
    let update() = 
        let mouse = System.Windows.Input.Mouse.GetPosition(content)
        if mouse.Y >= float(4*H) then
            let tileX = int(mouse.X) / (16*3)
            let tileY = (int(mouse.Y)-H-H-H-H) / (11*3)
            printfn "%f %f     %d %d" mouse.X mouse.Y tileX tileY 
            let bmp = makeAll()
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    let x = tileX*16*3 + px
                    let y = H+H+H+H+tileY*11*3 + py
                    bmp.SetPixel(x, y, average(bmp.GetPixel(x, y), highlightTile.GetPixel(px,py)))
            i.Source <- BMPtoImage(bmp)

    do
        let all = makeAll()
        i.Source <- BMPtoImage(all)
        i.Height <- float all.Height 
        i.Width <- float all.Width 

        // full window
        this.Title <- "Zelda 1 Randomizer"
        this.Content <- content
        //this.Width <- 1280.0 - 720.0
        //this.Width <- float(makeAll().Width)
        //this.Height <- 720.0
        //this.Height <- float(makeAll().Height)
        this.SizeToContent <- SizeToContent.WidthAndHeight 
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        this.Left <- 1100.0
        this.Top <- 20.0

        this.MouseMove.Add(fun _ -> update())

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
