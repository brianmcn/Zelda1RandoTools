module UserCustomLayer

// TODO eventually load this from a json file

let rawDataMetroid = 
    [|
    //brinstar
    19,113,"E",""
    72,113,"U",""
    57, 41,"E",""
    121,17,"E",""
    194,25,"U",""
    211,25,"U",""
    201,41,"E",""
    201,57,"U",""
    128,73,"M",""
    153,73,"E",""
    146,89,"U",""
    // norfair
    218,81, "U",""
    227,81, "U",""
    209,89, "U",""
    218,89, "U",""
    227,89, "U",""
    218,97, "E",""
    145,113,"U",""
    218,113,"M",""
    137,121,"U",""
    153,121,"U",""
    162,121,"M",""
    121,129,"E",""
    217,137,"E",""
    209,153,"U",""
    225,161,"U",""
    145,169,"E",""
    154,177,"U",""
    163,177,"U",""
    // kraid
    78,137,"M",""
    78,145,"M",""
    78,153,"M",""
    39,169,"U",""
    74,169,"U",""
    74,177,"U",""
    46,193,"M",""
    46,209,"M",""
    82,201,"U",""
    46,217,"U",""
    37,225,"M",""
    46,225,"M",""
    55,225,"U",""
    57,233,"Q",""
    // ridley
    146,193,"U",""
    113,201,"M",""
    138,201,"U",""
    147,201,"M",""
    156,201,"M",""
    165,201,"M",""
    174,201,"M",""
    113,217,"M",""
    190,217,"U",""
    217,217,"U",""
    121,233,"Q",""
    113,241,"M",""
    168,241,"U",""
    // checklist
    265,165," ","Kraid"
    301,165," ","Ridley"
    265,185," ","MorphBall"
    301,185," ","IceBeam"
    265,205," ","WaveBeam"
    301,205," ","LongBeam"
    265,225," ","Bombs"
    301,225," ","HighJump"
    265,245," ","ScrewAttack"
    301,245," ","Varia"
    |] |> Array.map (fun (x,y,label,ti) -> (x,y,6,6,label,ti))

let backgroundImageFile = """C:\Users\Admin1\Desktop\z1m1-metroid-ingame-regions-and-checklist.png"""

let backgroundImageBmp = System.Drawing.Bitmap.FromFile(backgroundImageFile) :?> System.Drawing.Bitmap

open System.Windows
open System.Windows.Controls
open System.Windows.Media

let mutable theCanvas = null
let Initialize(cm:CustomComboBoxes.CanvasManager, thruBlockersHeight, timelineItems:ResizeArray<_>) =
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
        for x,y,w,h,label,ti in rawDataMetroid do
            // timeline
            let ev = 
                if ti <> "" then
                    let ident = "UserCustom_" + ti
                    if not(TrackerModel.TimelineItemModel.All.ContainsKey(ident)) then
                        let bmpFile = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ExtraIcons\\"+ti+".png")
                        let orig = System.Drawing.Bitmap.FromFile(bmpFile) :?> System.Drawing.Bitmap
                        let bmp = 
                            if orig.Width=16 && orig.Height=16 then
                                let bmp = new System.Drawing.Bitmap(21,21)
                                for i = 0 to 20 do
                                    for j = 0 to 20 do
                                        bmp.SetPixel(i,j,System.Drawing.Color.Transparent)
                                for i = 1 to 18 do
                                    for j = 1 to 18 do
                                        bmp.SetPixel(i,j,System.Drawing.Color.Black)
                                for i = 2 to 17 do
                                    for j = 2 to 17 do
                                        bmp.SetPixel(i,j,orig.GetPixel(i-2,j-2))
                                bmp
                            else
                                orig
                        let ev = new Event<bool>()
                        let tim = new TrackerModel.TimelineItemModel(TrackerModel.TimelineItemDescription.UserCustom(ident, ev))
                        TrackerModel.TimelineItemModel.All.Add(ident, tim)
                        let ti = new Timeline.TimelineItem(ident, fun() -> bmp)
                        timelineItems.Add(ti)
                        Some(ev)
                    else
                        None
                else
                    None
            // canvas interaction
            let ws, hs = float w*scale, float h*scale
            let mutable isChecked = false
            let tb = mkTxt(label)
            let b = new Button(BorderThickness=Thickness(T), Padding=Thickness(0.), Content=new Viewbox(Child=tb, Stretch=Stretch.Fill), 
                                Width=2.*T+ws, Height=2.*T+hs)
            Graphics.canvasAdd(c, b, float x*scale-T, float y*scale-T)
            b.Click.Add(fun _ ->
                isChecked <- not isChecked
                if isChecked then
                    tb.Foreground <- Brushes.Black
                    tb.Background <- Brushes.Lime
                else
                    tb.Foreground <- Brushes.White
                    tb.Background <- Brushes.Black
                if ev.IsSome then
                    ev.Value.Trigger(isChecked)
                )
        theCanvas <- c
    theCanvas

let InteractWithUserCustom(cm:CustomComboBoxes.CanvasManager, thruBlockersHeight, timelineItems) = async {
    let c = Initialize(cm, thruBlockersHeight, timelineItems)
    let wh = new System.Threading.ManualResetEvent(false)
    do! CustomComboBoxes.DoModal(cm, wh, 0., 0., c)
    }