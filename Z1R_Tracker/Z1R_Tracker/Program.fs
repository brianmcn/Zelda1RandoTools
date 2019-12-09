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
    // testing
    for i = 0 to 8 do
        let bmp = if i <> 8 then emptyTriforces.[i] else emptyHeart
        for px = 0 to 10*3-1 do
            for py = 0 to 10*3-1 do
                overworldMap.SetPixel(i*10*3 + px, py, bmp.GetPixel(px, py))
    for i = 0 to 9 do
        let bmp = if i < 8 then fullTriforces.[i] elif i=8 then fullHeart else skippedHeart
        for px = 0 to 10*3-1 do
            for py = 0 to 10*3-1 do
                overworldMap.SetPixel(i*10*3 + px, 60 + py, bmp.GetPixel(px, py))

    let mutable xoff = 0
    for bmp in [boomerang; bow; magic_boomerang; raft; ladder; recorder; wand; red_candle; book; key; silver_arrow; red_ring; heart_container; power_bracelet; white_sword; armos] do
        for px = 0 to 7*3-1 do
            for py = 0 to 7*3-1 do
                overworldMap.SetPixel(xoff+px, 120 + py, bmp.GetPixel(px, py))
        xoff <- xoff + 7*3
    overworldMap
    

type MyWindow() as this = 
    inherit Window()
    let owImage = new Image()
    let content = owImage
    let update() = ()
    do
        owImage.Source <- BMPtoImage(makeOverworldMap())

        // full window
        this.Title <- "Zelda 1 Randomizer"
        this.Content <- content
        this.Width <- 1280.0 - 720.0
        this.Height <- 720.0
        //this.SizeToContent <- SizeToContent.Height
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        this.Left <- 1300.0
        this.Top <- 20.0

        let timer = new System.Windows.Threading.DispatcherTimer()
        timer.Interval <- TimeSpan.FromSeconds(0.5)  // TODO decide time
        timer.Tick.Add(fun _ -> update())
        timer.Start()
        

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
