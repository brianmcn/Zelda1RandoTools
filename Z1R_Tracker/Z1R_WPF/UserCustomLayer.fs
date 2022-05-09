module UserCustomLayer

// TODO eventually load this from a json file

let rawDataMetroid = 
    [|
    //brinstar
    19,113,"E"
    72,113,"U"
    57, 41,"E"
    121,17,"E"
    194,25,"U"
    211,25,"U"
    201,41,"E"
    201,57,"U"
    128,73,"M"
    153,73,"E"
    146,89,"U"
    // norfair
    218,81, "U"
    227,81, "U"
    209,89, "U"
    218,89, "U"
    227,89, "U"
    218,97, "E"
    145,113,"U"
    218,113,"M"
    137,121,"U"
    153,121,"U"
    162,121,"M"
    121,129,"E"
    217,137,"E"
    209,153,"U"
    225,161,"U"
    145,169,"E"
    154,177,"U"
    163,177,"U"
    // kraid
    78,137,"M"
    78,145,"M"
    78,153,"M"
    39,169,"U"
    74,169,"U"
    74,177,"U"
    46,193,"M"
    46,209,"M"
    82,201,"U"
    46,217,"U"
    37,225,"M"
    46,225,"M"
    55,225,"U"
    57,233,"Q"
    // ridley
    146,193,"U"
    113,201,"M"
    138,201,"U"
    147,201,"M"
    156,201,"M"
    165,201,"M"
    174,201,"M"
    113,217,"M"
    190,217,"U"
    217,217,"U"
    121,233,"Q"
    113,241,"M"
    168,241,"U"
    |] |> Array.map (fun (x,y,s) -> (x,y,6,6,s))

let backgroundImageFile = """C:\Users\Admin1\Desktop\z1m1-metroid-ingame-regions.png"""

let backgroundImageBmp = System.Drawing.Bitmap.FromFile(backgroundImageFile) :?> System.Drawing.Bitmap

open System.Windows
open System.Windows.Controls
open System.Windows.Media

let mutable theCanvas = null
let Initialize(cm:CustomComboBoxes.CanvasManager, thruBlockersHeight) =
    if theCanvas = null then
        let W,H = cm.AppMainCanvas.Width, thruBlockersHeight
        let hRatio = H / float(backgroundImageBmp.Height)
        let wRatio = W / float(backgroundImageBmp.Width)
        let scale = min hRatio wRatio

        let img = Graphics.BMPtoImage backgroundImageBmp
        img.Stretch <- Stretch.Uniform
        img.Width <- float(backgroundImageBmp.Width) * scale
        img.Height <- float(backgroundImageBmp.Height) * scale
        let c = new Canvas(Width=W, Height=H, Background=Brushes.Black)
        Graphics.canvasAdd(c, img, 0., 0.)

        let mkTxt(text) = new TextBox(Text=text, BorderThickness=Thickness(0.), Foreground=Brushes.White, Background=Brushes.Black, IsHitTestVisible=false, IsReadOnly=true)
        let T = 3.
        for x,y,w,h,label in rawDataMetroid do
            let ws, hs = float w*scale, float h*scale
            let content1 = new Viewbox(Child=mkTxt(label), Stretch=Stretch.Fill)
            let content2 = new DockPanel(Background=Brushes.Lime, Width=ws, Height=hs)
            let mutable is1 = true
            let b = new Button(BorderThickness=Thickness(T), Padding=Thickness(0.), Content=content1, 
                                Width=2.*T+ws, Height=2.*T+hs)
            Graphics.canvasAdd(c, b, float x*scale-T, float y*scale-T)
            b.Click.Add(fun _ ->
                b.Content <- if is1 then content2 :> FrameworkElement else content1:> FrameworkElement
                is1 <- not is1
                )
        theCanvas <- c
    theCanvas

let InteractWithUserCustom(cm:CustomComboBoxes.CanvasManager, thruBlockersHeight) = async {
    let c = Initialize(cm,thruBlockersHeight)
    let wh = new System.Threading.ManualResetEvent(false)
    do! CustomComboBoxes.DoModal(cm, wh, 0., 0., c)
    }