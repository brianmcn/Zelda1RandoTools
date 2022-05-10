module UserCustomLayer

(*
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
    265,165," ","Kraid.png"
    301,165," ","Ridley.png"
    265,185," ","MorphBall.png"
    301,185," ","IceBeam.png"
    265,205," ","WaveBeam.png"
    301,205," ","LongBeam.png"
    265,225," ","Bombs.png"
    301,225," ","HighJump.png"
    265,245," ","ScrewAttack.png"
    301,245," ","Varia.png"
    |] |> Array.map (fun (x,y,label,ti) -> (x,y,6,6,label,ti))
let backgroundImageFile = """z1m1-metroid-ingame-regions-and-checklist.png"""
*)

open System.Windows
open System.Windows.Controls
open System.Windows.Media

let extraIconsDirectory = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ExtraIcons")
let checklistFilename = System.IO.Path.Combine(extraIconsDirectory, "UserCustomChecklist.json")

let mutable theCanvas = null
let InitializeUserCustom(cm:CustomComboBoxes.CanvasManager, timelineItems:ResizeArray<_>) = async {
    if SaveAndLoad.theUserCustomChecklist = null then
        let mutable errorText = ""
        if System.IO.File.Exists(checklistFilename) then
            try
                let json = System.IO.File.ReadAllText(checklistFilename)
                SaveAndLoad.theUserCustomChecklist <- System.Text.Json.JsonSerializer.Deserialize<SaveAndLoad.UserCustomChecklist>(json)
            with e ->
                errorText <- sprintf "Error loading UserCustomChecklist.json\n\n%s" (e.ToString())
        else
            errorText <- "No UserCustomChecklist.json file exists."
        if errorText <> "" then
            if errorText.Length > 600 then
                errorText <- errorText.Substring(0, 600) + "..."
            let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Error, errorText, ["Open ExtraIcons folder"; "Ok"])
            if r <> "Ok" then
                let fileToSelect = checklistFilename
                let args = sprintf "/Select, \"%s\"" fileToSelect
                let psi = new System.Diagnostics.ProcessStartInfo("Explorer.exe", args)
                System.Diagnostics.Process.Start(psi) |> ignore
    if SaveAndLoad.theUserCustomChecklist <> null && theCanvas = null then
        let backgroundImageFilename = System.IO.Path.Combine(extraIconsDirectory, SaveAndLoad.theUserCustomChecklist.BackgroundImageFilename)
        let backgroundImageBmp = System.Drawing.Bitmap.FromFile(backgroundImageFilename) :?> System.Drawing.Bitmap

        let W,H = cm.AppMainCanvas.Width, OverworldItemGridUI.THRU_BLOCKERS_H
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
        for n = 0 to SaveAndLoad.theUserCustomChecklist.Items.Length-1 do
            let ucc = SaveAndLoad.theUserCustomChecklist.Items.[n]
            let x,y,w,h,label,ti = ucc.Left, ucc.Top, ucc.Width, ucc.Height, ucc.DisplayLabel, ucc.TimelineIconFilename
            // timeline
            let ev = 
                if ti <> "" then
                    let ident = "UserCustom_" + ti
                    if not(TrackerModel.TimelineItemModel.All.ContainsKey(ident)) then
                        let bmpFile = System.IO.Path.Combine(extraIconsDirectory, ti)
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
            let tb = mkTxt(label)
            let b = new Button(BorderThickness=Thickness(T), Padding=Thickness(0.), Content=new Viewbox(Child=tb, Stretch=Stretch.Fill), 
                                Width=2.*T+ws, Height=2.*T+hs)
            Graphics.canvasAdd(c, b, float x*scale-T, float y*scale-T)
            let redraw() =
                if SaveAndLoad.theUserCustomChecklist.Items.[n].IsChecked then
                    tb.Foreground <- Brushes.Black
                    tb.Background <- Brushes.Lime
                else
                    tb.Foreground <- Brushes.White
                    tb.Background <- Brushes.Black
            redraw()
            b.Click.Add(fun _ ->
                SaveAndLoad.theUserCustomChecklist.Items.[n].IsChecked <- not SaveAndLoad.theUserCustomChecklist.Items.[n].IsChecked
                redraw()
                if ev.IsSome then
                    ev.Value.Trigger(SaveAndLoad.theUserCustomChecklist.Items.[n].IsChecked)
                )
        theCanvas <- c
    }

let InteractWithUserCustom(cm:CustomComboBoxes.CanvasManager, timelineItems) = async {
    do! InitializeUserCustom(cm, timelineItems)
    if theCanvas <> null then
        let wh = new System.Threading.ManualResetEvent(false)
        do! CustomComboBoxes.DoModal(cm, wh, 0., 0., theCanvas)
    }