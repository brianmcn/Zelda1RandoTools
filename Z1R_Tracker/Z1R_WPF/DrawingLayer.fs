module DrawingLayer

open System.Windows
open System.Windows.Controls
open System.Windows.Media

open CustomComboBoxes

let unborder(bmp:System.Drawing.Bitmap) =  // turn 18x18 into 16x16
    let r = new System.Drawing.Bitmap(16,16)
    for i = 0 to 15 do
        for j = 0 to 15 do
            r.SetPixel(i, j, bmp.GetPixel(i+1,j+1))
    r

let mutable anyErrorMessageRelatedToExtraIcons = ""
let extraIconsDirectory = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ExtraIcons")
let ExtraIconsResourceTable =
    let arr = 
        let mutable currentFilename = ""
        try
            System.IO.Directory.CreateDirectory(extraIconsDirectory) |> ignore
            let results = ResizeArray()
            for filename in System.IO.Directory.EnumerateFiles(extraIconsDirectory, "*.png") do
                currentFilename <- System.IO.Path.GetFileName(filename)
                let bmp = System.Drawing.Bitmap.FromFile(filename) :?> System.Drawing.Bitmap
                let shortFilename = System.IO.Path.GetFileNameWithoutExtension(filename)
                results.Add(shortFilename, bmp)
            if results.Count = 0 then
                anyErrorMessageRelatedToExtraIcons <- "No .png files found in ExtraIcons directory"
                [| |]
            else
                results.ToArray()
        with e ->
            anyErrorMessageRelatedToExtraIcons <- if currentFilename<>"" then sprintf "While loading\n%s\nfrom ExtraIcons folder:\n%s" currentFilename (e.ToString()) else (e.ToString())
            [| |]
    dict arr
let ZTrackerResourceTable = 
    [|
        yield! [|
            // 7x7s
            "Ladder", Graphics.ladder_bmp
            "Heart", Graphics.heart_container_bmp
            "Boomerang", Graphics.boomerang_bmp
            "Bow", Graphics.bow_bmp
            "MagicBoomerang", Graphics.magic_boomerang_bmp
            "Raft", Graphics.raft_bmp
            "Recorder", Graphics.recorder_bmp
            "Wand", Graphics.wand_bmp
            "RedCandle", Graphics.red_candle_bmp
            "Book", Graphics.book_bmp
            "Key", Graphics.key_bmp
            "SilverArrow", Graphics.silver_arrow_bmp
            "WoodArrow", Graphics.wood_arrow_bmp
            "RedRing", Graphics.red_ring_bmp
            "MagicShield", Graphics.magic_shield_bmp
            "BoomBook", Graphics.boom_book_bmp
            "PowerBracelet", Graphics.power_bracelet_bmp
            "WhiteSword", Graphics.white_sword_bmp
            "Armos", Graphics.ow_key_armos_bmp
            "WoodSword", Graphics.brown_sword_bmp
            "MagicalSword", Graphics.magical_sword_bmp
            "BlueCandle", Graphics.blue_candle_bmp
            "BlueRing", Graphics.blue_ring_bmp
            "Gannon", Graphics.ganon_bmp
            "Zelda", Graphics.zelda_bmp
            "Bomb", Graphics.bomb_bmp
            "BowAndArrow", Graphics.bow_and_arrow_bmp
            "Bait", Graphics.bait_bmp
            "QuestionMarks", Graphics.question_marks_bmp
            "Rupee", Graphics.rupee_bmp
            "BasementStair", Graphics.basement_stair_bmp
            // 16x16 monsters
            "Digdogger", unborder <| Graphics.digdogger_bmp      
            "Gleeok", unborder <| Graphics.gleeok_bmp
            "Gohma", unborder <| Graphics.gohma_bmp
            "Manhandla", unborder <| Graphics.manhandla_bmp
            "Wizzrobe", unborder <| Graphics.wizzrobe_bmp
            "Patra", unborder <| Graphics.patra_bmp
            "Dodongo", unborder <| Graphics.dodongo_bmp
            "RedBubble", unborder <| Graphics.red_bubble_bmp
            "BlueBubble", unborder <| Graphics.blue_bubble_bmp
            "Darknut", unborder <| Graphics.blue_darknut_bmp
            "Yellow", unborder <| Graphics.other_monster_bmp
            // some others
            "OldMan", unborder <| Graphics.old_man_bmp
            "Fairy", Graphics.fairy_bmp
            // 16x16 items
            "Triforce", unborder <| Graphics.zi_triforce_bmp
            "FiveRupee", unborder <| Graphics.zi_fiver_bmp
            "Map", unborder <| Graphics.zi_map_bmp
            "Compass", unborder <| Graphics.zi_compass_bmp
        |]
        for i = 0 to Graphics.theInteriorBmpTable.Length-1 do
            yield sprintf "Overworld%2d" i, Graphics.theInteriorBmpTable.[i].[0]
    |] |> dict

open DungeonSaveAndLoad  // has Model types for DrawingLayer

let mutable mouse = None
let imgHighlightBorder = new Border(BorderThickness=Thickness(2.), BorderBrush=Brushes.Pink, Width=36., Height=36., Opacity=0.0)

let makeImage(bmp:System.Drawing.Bitmap) = 
    let img = Graphics.BMPtoImage bmp
    if bmp.Width = 16 || bmp.Height = 16 then
        img.Width <- 32.
        img.Height <- 32.
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor)
        img.Stretch <- Stretch.Uniform
    else
        img.Width <- 32.
        img.Height <- 32.
        img.Stretch <- Stretch.Uniform
        img.StretchDirection <- StretchDirection.DownOnly
    img.IsHitTestVisible <- true
    img.MouseEnter.Add(fun _ea ->
        imgHighlightBorder.Opacity <- 1.0
        let pos = img.TranslatePoint(Point(), CanvasManager.TheOnlyCanvasManager.AppMainCanvas)
        Canvas.SetLeft(imgHighlightBorder, pos.X-2.)
        Canvas.SetTop(imgHighlightBorder, pos.Y-2.)
        )
    img.MouseLeave.Add(fun _ea ->
        imgHighlightBorder.Opacity <- 0.0
        )
    img.MouseDown.Add(fun ea ->  // only lives in a IsHitTestVisible canvas during interaction mode, so can always have this code
        let p = img.Parent
        match p with
        | :? Canvas as c -> 
            let remove() =
                c.Children.Remove(img)
                let mutable toRemove = -1
                for i = 0 to AllDrawingLayerStamps.Count-1 do
                    let _,_,_,x = AllDrawingLayerStamps.[i]
                    if obj.ReferenceEquals(x, img) then
                        toRemove <- i
                if toRemove <> -1 then
                    let (icon,_,_,_) = AllDrawingLayerStamps.[toRemove]
                    AllDrawingLayerStamps.RemoveAt(toRemove)
                    ea.Handled <- true
                    icon
                else
                    failwith "stamp not found"
            // left-click pick up to move
            if ea.ChangedButton = Input.MouseButton.Left then
                let icon = remove()
                mouse <- Some(icon, img)
            // right-click = remove self
            elif ea.ChangedButton = Input.MouseButton.Right then
                remove() |> ignore
            // middle click toggles half-size
            elif ea.ChangedButton = Input.MouseButton.Middle then
                if img.RenderTransform = null || obj.ReferenceEquals(img.RenderTransform, Transform.Identity) then
                    img.RenderTransform <- new ScaleTransform(0.5, 0.5)
                else
                    img.RenderTransform <- null
        | _ -> ()
        )
    img

let LoadDrawingLayer(model:DrawingLayerIconModel[], drawingCanvas) =
    if model <> null then
        for icon in model do
            if icon.Extra then
                if ExtraIconsResourceTable.ContainsKey(icon.Name) then
                    let img = makeImage(ExtraIconsResourceTable.[icon.Name])
                    if icon.HalfSize then img.RenderTransform <- new ScaleTransform(0.5, 0.5)
                    AllDrawingLayerStamps.Add(DrawingLayerIcon.ExtraIcon icon.Name, icon.X, icon.Y, img)
            else
                if ZTrackerResourceTable.ContainsKey(icon.Name) then
                    let img = makeImage(ZTrackerResourceTable.[icon.Name])
                    if icon.HalfSize then img.RenderTransform <- new ScaleTransform(0.5, 0.5)
                    AllDrawingLayerStamps.Add(DrawingLayerIcon.ZTracker icon.Name, icon.X, icon.Y, img)
    for _,x,y,img in AllDrawingLayerStamps do
        canvasAdd(drawingCanvas, img, float x, float y)

let InteractWithDrawingLayer(cm:CanvasManager, thruBlockersHeight, drawingCanvas:Canvas) = async {
    let wh = new System.Threading.ManualResetEvent(false)
    let mutable thisSessionHasEnded = false  // we keep adding new handlers to RootCanvas, no way to remove them, but need to ensure old ones quit running

    let interactionCanvas = new Canvas(Width=cm.AppMainCanvas.Width, Height=cm.AppMainCanvas.Height)
    interactionCanvas.Children.Add(imgHighlightBorder) |> ignore
    let topCanvasToHoldIcons = new Canvas(Width=cm.AppMainCanvas.Width, Height=thruBlockersHeight, IsHitTestVisible=true)
    interactionCanvas.Children.Add(topCanvasToHoldIcons) |> ignore
    let mouseCarry = new Border(BorderBrush=Brushes.White, BorderThickness=Thickness(1.), Opacity=0., IsHitTestVisible=false)
    let DIFF = 31.
    cm.RootCanvas.MouseMove.Add(fun ea ->
        if not thisSessionHasEnded then
            match mouse with
            | None -> ()
            | Some(_icon, img) ->
                let pos = ea.GetPosition(cm.AppMainCanvas)
                if pos.Y < thruBlockersHeight then
                    interactionCanvas.Children.Remove(mouseCarry)
                    interactionCanvas.Children.Add(mouseCarry) |> ignore
                    mouseCarry.Child <- img
                    mouseCarry.Opacity <- 0.6
                    Canvas.SetLeft(mouseCarry, pos.X - DIFF)
                    Canvas.SetTop(mouseCarry, pos.Y - DIFF)
        )
    cm.RootCanvas.MouseDown.Add(fun ea ->
        if not thisSessionHasEnded then
            match mouse with
            | None -> ()
            | Some(icon, img) ->
                let pos = ea.GetPosition(cm.AppMainCanvas)
                if pos.Y < thruBlockersHeight then
                    // left-click = place a stamp
                    if ea.ChangedButton = Input.MouseButton.Left then
                        AllDrawingLayerStamps.Add(icon, int (pos.X-DIFF), int (pos.Y-DIFF), img)
                        mouse <- None
                        mouseCarry.Child <- null
                        mouseCarry.Opacity <- 0.0
                        canvasAdd(topCanvasToHoldIcons, img, pos.X-DIFF, pos.Y-DIFF)
                        ea.Handled <- true
                    // right-click = quit carrying stamp
                    elif ea.ChangedButton = Input.MouseButton.Right then
                        mouse <- None
                        mouseCarry.Child <- null
                        mouseCarry.Opacity <- 0.0
                        ea.Handled <- true
        )

    let bottomCanvas = new Canvas(Width=interactionCanvas.Width, Height=interactionCanvas.Height - thruBlockersHeight, Background = Brushes.Black)
    canvasAdd(interactionCanvas, bottomCanvas, 0., thruBlockersHeight)
    bottomCanvas.MouseMove.Add(fun _ea ->
        interactionCanvas.Children.Remove(mouseCarry) // if the mouse move into the bottom zone, remove the trailing icon
        )

    let alt = new SolidColorBrush(Color.FromRgb(0x5Fuy,0x2Fuy,0x5Fuy))
    let mkTxtImpl(text,bt,bg) = new TextBox(Text=text, BorderBrush=bg, BorderThickness=Thickness(bt), FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, 
                                            IsHitTestVisible=false, IsReadOnly=true)
    let mkTxtBT(text,bt) = mkTxtImpl(text, bt, Brushes.DarkSlateGray)
    let mkTxt(text) = mkTxtImpl(text, 1., Brushes.DarkSlateGray)
    let mkTxtAlt(text) = mkTxtImpl(text, 1., alt)

    let hsp = new StackPanel(Orientation=Orientation.Horizontal)
    hsp.Children.Add(mkTxt("Left click a button below\nto 'pick up' an icon")) |> ignore
    hsp.Children.Add(new DockPanel(Width=8.)) |> ignore
    hsp.Children.Add(mkTxt("Then left click above\nto place it anywhere")) |> ignore
    hsp.Children.Add(new DockPanel(Width=8.)) |> ignore
    hsp.Children.Add(mkTxtAlt("Left click a placed icon\nto pick it up again")) |> ignore
    hsp.Children.Add(new DockPanel(Width=8.)) |> ignore
    hsp.Children.Add(mkTxtAlt("Right click a placed\nicon to remove it")) |> ignore
    hsp.Children.Add(new DockPanel(Width=8.)) |> ignore
    hsp.Children.Add(mkTxt("When finished, click\nbutton to the right")) |> ignore
    hsp.Children.Add(new DockPanel(Width=8.)) |> ignore
    let doneEditingButton = Graphics.makeButton("Done editing", Some(20.), Some(Brushes.Orange))
    hsp.Children.Add(doneEditingButton) |> ignore
    canvasAdd(bottomCanvas, hsp, 10., 10.)
    doneEditingButton.Click.Add(fun _ -> wh.Set() |> ignore)

    canvasAdd(bottomCanvas, mkTxtAlt("Middle click a placed icon to toggle it half-size"), 275., 45.)

    // Native icons
    let ztIconGrid = new System.Windows.Controls.Primitives.UniformGrid(Rows=8, Columns=12)
    for (KeyValue(k,v)) in ZTrackerResourceTable do
        let img = makeImage(v)
        img.IsHitTestVisible <- false // the button label should not respond to clicks
        let c = new Canvas(Width=32., Height=32.)
        let dark = new Canvas(Width=32., Height=32., Background=Brushes.Black, Opacity=0.65)
        c.Children.Add(dark) |> ignore
        c.Children.Add(img) |> ignore
        let button = new Button(Content=c, Background=Graphics.almostBlack)
        button.Click.Add(fun _ -> 
            mouse <- Some(DrawingLayerIcon.ZTracker(k), makeImage(v))
            )
        ztIconGrid.Children.Add(button) |> ignore
    canvasAdd(bottomCanvas, mkTxtBT("Here are some default icons that come with Z-Tracker:", 0.), 10., 80.)
    canvasAdd(bottomCanvas, ztIconGrid, 10., 100.)
    // ExtraIcons
    let extraIconGrid = new System.Windows.Controls.Primitives.UniformGrid(Rows=7, Columns=8)
    for (KeyValue(k,v)) in ExtraIconsResourceTable do
        let img = makeImage(v)
        img.IsHitTestVisible <- false // the button label should not respond to clicks
        let c = new Canvas(Width=32., Height=32.)
        let dark = new Canvas(Width=32., Height=32., Background=Brushes.Black, Opacity=0.65)
        c.Children.Add(dark) |> ignore
        c.Children.Add(img) |> ignore
        let button = new Button(Content=c, Background=Graphics.almostBlack)
        button.Click.Add(fun _ -> 
            mouse <- Some(DrawingLayerIcon.ExtraIcon(k), makeImage(v))
            )
        extraIconGrid.Children.Add(button) |> ignore
    let X = 460.
    let openFolderButton = Graphics.makeButton("Open ExtraIcons Folder", Some(12.), Some(Brushes.Orange))
    canvasAdd(bottomCanvas, openFolderButton, X, 80.)
    canvasAdd(bottomCanvas, mkTxtBT("<- add .png files here", 0.), X+150., 80.)
    canvasAdd(bottomCanvas, mkTxtBT("(must re-start Z-Tracker to reload these icons)", 0.), X, 100.)
    canvasAdd(bottomCanvas, mkTxtBT("Here are icons found in your ExtraIcons folder:", 0.), X, 120.)
    canvasAdd(bottomCanvas, extraIconGrid, X, 140.)
    openFolderButton.Click.Add(fun _ ->
        let psi = new System.Diagnostics.ProcessStartInfo("Explorer.exe", extraIconsDirectory)
        System.Diagnostics.Process.Start(psi) |> ignore
        )
    if anyErrorMessageRelatedToExtraIcons <> "" then
        let errorTB = mkTxt("Error while loading ExtraIcons:\n" + anyErrorMessageRelatedToExtraIcons)
        bottomCanvas.Children.Add(errorTB) |> ignore
        Canvas.SetLeft(errorTB, X)
        Canvas.SetBottom(errorTB, 0.)

    // when invoked...

    // move all images from top layer to here (note: they retain attached properties for Canvas.Left/Top)
    let all = ResizeArray()
    for i = 0 to drawingCanvas.Children.Count-1 do
        all.Add(drawingCanvas.Children.Item(i))
    drawingCanvas.Children.Clear()
    for x in all do
        topCanvasToHoldIcons.Children.Add(x) |> ignore
    // interact
    do! DoModalDocked(cm, wh, Dock.Bottom, interactionCanvas)
    // turn off the heavy parts of the mouse listener, which will continue to fire
    mouse <- None 
    thisSessionHasEnded <- true
    // cleanup
    interactionCanvas.Children.Remove(imgHighlightBorder)
    // move images back to top layer
    let all = ResizeArray()
    for i = 0 to topCanvasToHoldIcons.Children.Count-1 do
        all.Add(topCanvasToHoldIcons.Children.Item(i))
    topCanvasToHoldIcons.Children.Clear()
    for x in all do
        drawingCanvas.Children.Add(x) |> ignore
    }
