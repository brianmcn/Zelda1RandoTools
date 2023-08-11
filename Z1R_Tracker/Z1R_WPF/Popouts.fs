module Popouts

open System.Windows
open System.Windows.Media
open System.Windows.Controls

let MINIMIZED_THRESHOLD = ShowRunCustom.MINIMIZED_THRESHOLD

let mutable theMainWindow : Window = null
let mutable theMainWindowHasClosed = false
let mutable ctxt : System.Threading.SynchronizationContext = null
let Initialize(mainWindow,ctx) =
    theMainWindow <- mainWindow
    ctxt <- ctx
let refocusMainWindow() =   // keep hotkeys working
    async {
        do! Async.Sleep(500)  // give new window time to pop up
        do! Async.SwitchToContext(ctxt)
        theMainWindow.Focus() |> ignore
    } |> Async.StartImmediate

////////////////////////////////////////////////////////

let showHotKeys(isRightClick) =
    let none,p = OverworldMapTileCustomization.MakeMappedHotKeysDisplay()
    let w = new Window(Title="Z-Tracker HotKeys", Owner=Application.Current.MainWindow, Content=p, ResizeMode=ResizeMode.CanResizeWithGrip)
    let save() = 
        TrackerModelOptions.HotKeyWindowLTWH <- sprintf "%d,%d,%d,%d" (int w.Left) (int w.Top) (int w.Width) (int w.Height)
        TrackerModelOptions.writeSettings()
    let leftTopWidthHeight = TrackerModelOptions.HotKeyWindowLTWH
    let matches = System.Text.RegularExpressions.Regex.Match(leftTopWidthHeight, """^(-?\d+),(-?\d+),(\d+),(\d+)$""")
    if not none && not isRightClick && matches.Success && not(float matches.Groups.[1].Value < MINIMIZED_THRESHOLD) then
        w.Left <- float matches.Groups.[1].Value
        w.Top <- float matches.Groups.[2].Value
        w.Width <- float matches.Groups.[3].Value
        w.Height <- float matches.Groups.[4].Value
    else
        p.Measure(Size(1280., 720.))
        w.Width <- p.DesiredSize.Width + 16.
        w.Height <- p.DesiredSize.Height + 40.
    w.SizeChanged.Add(fun _ -> save(); refocusMainWindow())
    w.LocationChanged.Add(fun _ -> save(); refocusMainWindow())
    w.Show()

////////////////////////////////////////////////

let makeFauxItemsAndHeartsHUD() =
    let r = new System.Drawing.Bitmap(98,61+18)
    let best = Graphics.allItemsHUDBestBMP
    let worst = Graphics.allItemsHUDWorstBMP
    let BLACK = System.Drawing.Color.Black
    // draw full HUD
    for x = 0 to best.Width-1 do
        for y = 0 to best.Height-1 do
            r.SetPixel(x,y,best.GetPixel(x,y))
    // erase or change based on inventory
    let maybeErase(x1,x2,y1,y2,b) =
        if not(b) then
            for x = x1 to x2 do
                for y = y1 to y2 do
                    r.SetPixel(x,y,BLACK)
    let copyWorse(x1,x2,y1,y2,b) =
        if b then
            for x = x1 to x2 do
                for y = y1 to y2 do
                    r.SetPixel(x,y,worst.GetPixel(x,y))
    // top row
    maybeErase(6,19,0,15,TrackerModel.playerComputedStateSummary.HaveRaft)
    maybeErase(29,36,0,15,TrackerModel.PlayerHasTheBook())
    maybeErase(42,48,3,11,TrackerModel.playerComputedStateSummary.RingLevel>0)
    copyWorse(42,48,3,11,TrackerModel.playerComputedStateSummary.RingLevel=1)
    maybeErase(53,68,0,15,TrackerModel.playerComputedStateSummary.HaveLadder)
    maybeErase(73,81,0,15,TrackerModel.playerComputedStateSummary.HaveAnyKey)
    maybeErase(85,92,0,15,TrackerModel.playerComputedStateSummary.HavePowerBracelet)
    // middle row
    maybeErase(10,14,28,35,TrackerModel.playerComputedStateSummary.BoomerangLevel>0)
    copyWorse(10,14,28,35,TrackerModel.playerComputedStateSummary.BoomerangLevel=1)
    maybeErase(33,40,24,39,TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBombs.Value())
    maybeErase(55,59,24,39,TrackerModel.playerComputedStateSummary.ArrowLevel>0)
    copyWorse(55,59,24,39,TrackerModel.playerComputedStateSummary.ArrowLevel=1)
    maybeErase(61,69,24,39,TrackerModel.playerComputedStateSummary.HaveBow)
    maybeErase(81,88,24,39,TrackerModel.playerComputedStateSummary.CandleLevel>0)
    copyWorse(81,88,24,39,TrackerModel.playerComputedStateSummary.CandleLevel=1)
    // bottom row
    maybeErase(12,14,40,55,TrackerModel.playerComputedStateSummary.HaveRecorder)
    copyWorse(33,40,40,55,true)  // worse has a greyscale meat, tracker doesn't know if player has
    copyWorse(57,64,40,55,true)  // worse shows potion letter, maybeErased below
    maybeErase(57,64,40,55,TrackerModel.havePotionLetter.Value)
    maybeErase(83,86,40,55,TrackerModel.playerComputedStateSummary.HaveWand)
    // add hearts display
    if TrackerModel.playerComputedStateSummary.PlayerHearts > 8 then
        // top row
        for i = 0 to TrackerModel.playerComputedStateSummary.PlayerHearts - 9 do
            for dx = 0 to 7 do
                for dy = 0 to 7 do
                    r.SetPixel(12+i*8+dx, 63+dy, Graphics.empty_small_heart_bmp.GetPixel(dx,dy))
    // bottom row, 3 red
    for i = 0 to (min TrackerModel.playerComputedStateSummary.PlayerHearts 3)-1 do
        for dx = 0 to 7 do
            for dy = 0 to 7 do
                r.SetPixel(12+i*8+dx, 71+dy, Graphics.filled_small_heart_bmp.GetPixel(dx,dy))
    // bottom row rest
    for i = 3 to (min TrackerModel.playerComputedStateSummary.PlayerHearts 8) - 1 do
        for dx = 0 to 7 do
            for dy = 0 to 7 do
                r.SetPixel(12+i*8+dx, 71+dy, Graphics.empty_small_heart_bmp.GetPixel(dx,dy))
    // maybe change green to white/red based on ring? given randomize tunic colors, seems frivolous, so naah
    r
let MakeInventoryAndHearts() =
    let bmp = makeFauxItemsAndHeartsHUD()
    let img = Graphics.BMPtoImage bmp
    img.Width <- 3. * img.Width
    img.Height <- 3. * img.Height
    img.Stretch <- Stretch.UniformToFill
    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor)
    let sp = new StackPanel(Orientation=Orientation.Vertical)
    sp.Children.Add(img) |> ignore
    let playerKnowsLocationOfWoodBoomer = TrackerModel.DungeonTrackerInstance.TheDungeonTrackerInstance.AllBoxes() |> Seq.exists (fun b -> b.CellCurrent()=TrackerModel.ITEMS.BOOMERANG)
    let playerKnowsLocationOfWhiteSword = TrackerModel.DungeonTrackerInstance.TheDungeonTrackerInstance.AllBoxes() |> Seq.exists (fun b -> b.CellCurrent()=TrackerModel.ITEMS.WHITESWORD)
    if TrackerModel.playerComputedStateSummary.BoomerangLevel=2 && not(playerKnowsLocationOfWoodBoomer) then
        let tb = new TextBox(FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), 
                                Text="Note: having magical boomerang may make\n   it hard to notice wooden boomerang pick up")
        sp.Children.Add(tb) |> ignore
    if TrackerModel.playerComputedStateSummary.SwordLevel=3 && not(playerKnowsLocationOfWhiteSword) then
        let tb = new TextBox(FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), 
                                Text="Note: having magical sword may make it\n   hard to notice if white sword picked up")
        sp.Children.Add(tb) |> ignore
    sp
let MakeRemainingItemsDisplay() = 
    let img(i) = let r = CustomComboBoxes.boxCurrentBMP(i, None) |> Graphics.BMPtoImage in r.Margin <- Thickness(0.,0.,3.,0.); r
    let hsp = new StackPanel(Orientation=Orientation.Horizontal, Margin=Thickness(3.))
    for i in TrackerModel.ITEMS.PriorityOrderForRemainingDisplay do
        if TrackerModel.allItemWithHeartShuffleChoiceDomain.CanAddUse(i) then
            hsp.Children.Add(img(i)) |> ignore
    let h = TrackerModel.ITEMS.HEARTCONTAINER
    let c = TrackerModel.allItemWithHeartShuffleChoiceDomain.MaxUses(h) - TrackerModel.allItemWithHeartShuffleChoiceDomain.NumUses(h)
    for x = 1 to c do
        hsp.Children.Add(img(h)) |> ignore
    if hsp.Children.Count=0 then
        hsp.Children.Add(new TextBox(Text="None",FontSize=12.,IsReadOnly=true,IsHitTestVisible=false,BorderThickness=Thickness(0.),Foreground=Brushes.Orange,Background=Brushes.Black)) |> ignore
    let vsp = new StackPanel(Orientation=Orientation.Vertical)
    vsp.Children.Add(new TextBox(Text="Remaining unmarked items:",FontSize=12.,IsReadOnly=true,IsHitTestVisible=false,BorderThickness=Thickness(0.),Foreground=Brushes.Orange,Background=Brushes.Black)) |> ignore
    vsp.Children.Add(hsp) |> ignore
    vsp

////////////////////////////////////////////////////////

type PopoutKind =
    | SpotSummaryPopout
    | InventoryAndHeartsPopout
    | RemainingItemsPoput

let mutable theSpotSummaryPopoutWindow : Window = null
let mutable theInventoryAndHeartsPopoutWindow : Window = null
let mutable theRemainingItemsPopoutWindow : Window = null

let spotSummaryContent() =
    let dp = new DockPanel(Background=Brushes.Black)
    let redraw() =
        dp.Children.Clear()
        dp.Children.Add(OverworldMapTileCustomization.MakeRemainderSummaryDisplay()) |> ignore
    redraw()
    TrackerModel.mapStateSummaryComputedEvent.Publish.Add(fun _ -> 
//        printfn "redraw spot summary"
        redraw()
        )
    dp  // already has a border
let inventoryAndHeartsContent(doUIUpdateEvent:Event<unit>) =
    let dp = new DockPanel(Background=Brushes.Black)
    let redraw() =
        dp.Children.Clear()
        dp.Children.Add(MakeInventoryAndHearts()) |> ignore
    redraw()
    doUIUpdateEvent.Publish.Add(fun _ -> 
//        printfn "redraw inventory and hearts"
        redraw()
        )
    new Border(Child=dp, BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.))
let remainingItemsContent(doUIUpdateEvent:Event<unit>) =
    let dp = new DockPanel(Background=Brushes.Black)
    let redraw() =
        dp.Children.Clear()
        dp.Children.Add(MakeRemainingItemsDisplay()) |> ignore
    redraw()
    doUIUpdateEvent.Publish.Add(fun _ -> 
//        printfn "redraw remaining items"
        redraw()
        )
    new Border(Child=dp, BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.))

let mutable insideMakePopoutCall = false
let makePopout(kind, isLeftClick, isRightClick, doUIUpdateEvent:Event<unit>) =
    let w = new Window(Owner=Application.Current.MainWindow, ResizeMode=ResizeMode.NoResize, SizeToContent=SizeToContent.WidthAndHeight)
    let mutable save = fun() -> ()
    let mutable noDisplay = fun() -> ()
    insideMakePopoutCall <- true
    let matches =    
        match kind with
        | SpotSummaryPopout ->
            if theSpotSummaryPopoutWindow <> null then
                theSpotSummaryPopoutWindow.Close()
            theSpotSummaryPopoutWindow <- w
            w.Title <- "Spot Summary popout"
            w.Content <- spotSummaryContent()
            save <- (fun() ->
                if w.Left < MINIMIZED_THRESHOLD then // minimized or closing
                    TrackerModelOptions.SpotSummaryPopout_DisplayedLT <- sprintf "0,%d,%d" (int w.RestoreBounds.Left) (int w.RestoreBounds.Top)
                else
                    TrackerModelOptions.SpotSummaryPopout_DisplayedLT <- sprintf "1,%d,%d" (int w.Left) (int w.Top)
                TrackerModelOptions.writeSettings()
                )
            noDisplay <- (fun () ->
                if TrackerModelOptions.SpotSummaryPopout_DisplayedLT.Length <> 0 then
                    TrackerModelOptions.SpotSummaryPopout_DisplayedLT <- "0"+TrackerModelOptions.SpotSummaryPopout_DisplayedLT.Substring(1)
                    TrackerModelOptions.writeSettings()
                )
            System.Text.RegularExpressions.Regex.Match(TrackerModelOptions.SpotSummaryPopout_DisplayedLT, """^(1|0),(-?\d+),(-?\d+)$""")
        | InventoryAndHeartsPopout ->
            if theInventoryAndHeartsPopoutWindow <> null then
                theInventoryAndHeartsPopoutWindow.Close()
            theInventoryAndHeartsPopoutWindow <- w
            w.Title <- "Inventory and Max Hearts popout"
            w.Content <- inventoryAndHeartsContent(doUIUpdateEvent)
            save <- (fun() ->
                if w.Left < MINIMIZED_THRESHOLD then // minimized or closing
                    TrackerModelOptions.InventoryAndHeartsPopout_DisplayedLT <- sprintf "0,%d,%d" (int w.RestoreBounds.Left) (int w.RestoreBounds.Top)
                else
                    TrackerModelOptions.InventoryAndHeartsPopout_DisplayedLT <- sprintf "1,%d,%d" (int w.Left) (int w.Top)
                TrackerModelOptions.writeSettings()
                )
            noDisplay <- (fun () ->
                if TrackerModelOptions.InventoryAndHeartsPopout_DisplayedLT.Length <> 0 then
                    TrackerModelOptions.InventoryAndHeartsPopout_DisplayedLT <- "0"+TrackerModelOptions.InventoryAndHeartsPopout_DisplayedLT.Substring(1)
                    TrackerModelOptions.writeSettings()
                )
            System.Text.RegularExpressions.Regex.Match(TrackerModelOptions.InventoryAndHeartsPopout_DisplayedLT, """^(1|0),(-?\d+),(-?\d+)$""")
        | RemainingItemsPoput ->
            if theRemainingItemsPopoutWindow <> null then
                theRemainingItemsPopoutWindow.Close()
            theRemainingItemsPopoutWindow <- w
            w.Title <- "Remaining Items popout"
            w.Content <- remainingItemsContent(doUIUpdateEvent)
            save <- (fun() ->
                if w.Left < MINIMIZED_THRESHOLD then // minimized or closing
                    TrackerModelOptions.RemainingItemsPoput_DisplayedLT <- sprintf "0,%d,%d" (int w.RestoreBounds.Left) (int w.RestoreBounds.Top)
                else
                    TrackerModelOptions.RemainingItemsPoput_DisplayedLT <- sprintf "1,%d,%d" (int w.Left) (int w.Top)
                TrackerModelOptions.writeSettings()
                )
            noDisplay <- (fun () ->
                if TrackerModelOptions.RemainingItemsPoput_DisplayedLT.Length <> 0 then
                    TrackerModelOptions.RemainingItemsPoput_DisplayedLT <- "0"+TrackerModelOptions.RemainingItemsPoput_DisplayedLT.Substring(1)
                    TrackerModelOptions.writeSettings()
                )
            System.Text.RegularExpressions.Regex.Match(TrackerModelOptions.RemainingItemsPoput_DisplayedLT, """^(1|0),(-?\d+),(-?\d+)$""")
    let ok,disp,left,top = 
        if matches.Success && not(float matches.Groups.[2].Value < MINIMIZED_THRESHOLD) then
            true,(int matches.Groups.[1].Value)=1, float matches.Groups.[2].Value, float matches.Groups.[3].Value
        else
            false,false,0.,0.
    if (isLeftClick && ok)                                  // the user explicitly is trying to show the window, and we are able to parse valid coords
        || (not isLeftClick && not isRightClick && disp)    // it's startup automatically looking for windows to display, and this one has disp=1 and valid coords
            then
        w.Left <- left
        w.Top <- top
    // otherwise (e.g. right click, bad data) just display a new window at default location selected by Windows
    w.LocationChanged.Add(fun _ -> save(); refocusMainWindow())
    w.Closed.Add(fun _ -> if not(theMainWindowHasClosed) && not(insideMakePopoutCall) then noDisplay())
    w.Show()
    insideMakePopoutCall <- false

let Startup(doUIUpdateEvent:Event<unit>) =
    let f(kind, setting) =
        let matches = System.Text.RegularExpressions.Regex.Match(setting, """^(1|0),(-?\d+),(-?\d+)$""")
        let disp,_left,_top = 
            if matches.Success && not(float matches.Groups.[2].Value < MINIMIZED_THRESHOLD) then
                (int matches.Groups.[1].Value)=1, float matches.Groups.[2].Value, float matches.Groups.[3].Value
            else
                false,0.,0.
        if disp then
            makePopout(kind, false, false, doUIUpdateEvent)
    for k,s in [  SpotSummaryPopout, TrackerModelOptions.SpotSummaryPopout_DisplayedLT
                  InventoryAndHeartsPopout, TrackerModelOptions.InventoryAndHeartsPopout_DisplayedLT
                  RemainingItemsPoput, TrackerModelOptions.RemainingItemsPoput_DisplayedLT
               ] do
        f(k,s)

        
