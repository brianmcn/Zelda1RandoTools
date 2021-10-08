open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

open System.Runtime.InteropServices 
module Winterop = 
    [<DllImport("User32.dll")>]
    extern bool RegisterHotKey(IntPtr hWnd,int id,uint32 fsModifiers,uint32 vk)

    [<DllImport("User32.dll")>]
    extern bool UnregisterHotKey(IntPtr hWnd,int id)

    let HOTKEY_ID = 9000
    
type MyWindowBase() as this = 
    inherit Window()
    let mutable source = null
    let VK_F5 = 0x74
    let VK_F10 = 0x79
    let MOD_NONE = 0u
    let mutable startTime = DateTime.Now
    do
        // full window
        let timer = new System.Windows.Threading.DispatcherTimer()
        timer.Interval <- TimeSpan.FromSeconds(1.0)
        timer.Tick.Add(fun _ -> this.Update(false))
        timer.Start()
    member this.SetStartTime(x) = startTime <- x
    member this.StartTime = startTime
    abstract member Update : bool -> unit
    default this.Update(_f10Press) = ()
    override this.OnSourceInitialized(e) =
        base.OnSourceInitialized(e)
        let helper = new System.Windows.Interop.WindowInteropHelper(this)
        source <- System.Windows.Interop.HwndSource.FromHwnd(helper.Handle)
        source.AddHook(System.Windows.Interop.HwndSourceHook(fun a b c d e -> this.HwndHook(a,b,c,d,&e)))
        this.RegisterHotKey()
    override this.OnClosed(e) =
        if source <> null then
            source.RemoveHook(System.Windows.Interop.HwndSourceHook(fun a b c d e -> this.HwndHook(a,b,c,d,&e)))
        source <- null
        this.UnregisterHotKey()
        base.OnClosed(e)
    member this.RegisterHotKey() =
#if DEBUG
        // in debug mode, do not register hotkeys, as I need e.g. F10 to work to use the debugger!
        ()
#else
        let helper = new System.Windows.Interop.WindowInteropHelper(this);
        if(not(Winterop.RegisterHotKey(helper.Handle, Winterop.HOTKEY_ID, MOD_NONE, uint32 VK_F10))) then
            // handle error
            ()
        if(not(Winterop.RegisterHotKey(helper.Handle, Winterop.HOTKEY_ID, MOD_NONE, uint32 VK_F5))) then
            // handle error
            ()
#endif
    member this.UnregisterHotKey() =
        let helper = new System.Windows.Interop.WindowInteropHelper(this)
        Winterop.UnregisterHotKey(helper.Handle, Winterop.HOTKEY_ID) |> ignore
    member this.HwndHook(_hwnd:IntPtr, msg:int, wParam:IntPtr, lParam:IntPtr, handled:byref<bool>) : IntPtr =
        let WM_HOTKEY = 0x0312
        if msg = WM_HOTKEY then
            if wParam.ToInt32() = Winterop.HOTKEY_ID then
                //let ctrl_bits = lParam.ToInt32() &&& 0xF  // see WM_HOTKEY docs
                let key = lParam.ToInt32() >>> 16
                if key = VK_F10 then
                    //startTime <- DateTime.Now
                    this.Update(true)
        IntPtr.Zero

type MyWindow() as this = 
    inherit MyWindowBase()
    let mutable updateTimeline = fun _ -> ()
    let mutable lastUpdateMinute = 0
    let hmsTimeTextBox = new TextBox(Text="timer",FontSize=42.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false)
    //                 items  ow map  prog  dungeon tabs                      timeline   
    let HEIGHT = float(30*5 + 11*3*9 + 30 + WPFUI.TH + 30 + 27*8 + 12*7 + 3 + WPFUI.TCH + 6 + 40) // (what is the final 40?)
    let WIDTH = float(16*16*3 + 16)  // ow map width (what is the final 16?)
    let mutable loggedAnyCrash = false
    let crashLogFilename = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Z1R_Tracker_crash_log.txt")
    do
        let logCrashInfo(s:string) =
            printfn "%s" s
            try
                if not loggedAnyCrash then
                    loggedAnyCrash <- true
                    System.IO.File.AppendAllText(crashLogFilename, sprintf "BEGIN CRASH LOG -- %s -- %s\n" OverworldData.ProgramNameString (DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss")))
                System.IO.File.AppendAllText(crashLogFilename, sprintf "%s\n" s)
            with _ -> ()
        let handle(ex:System.Exception) =
            match ex with
            | :? TrackerModel.IntentionalApplicationShutdown as ias ->
                logCrashInfo <| sprintf "%s" ias.Message
            | _ ->
                logCrashInfo <| sprintf "%s" (ex.ToString())
        System.Windows.Application.Current.DispatcherUnhandledException.Add(fun e -> 
            let ex = e.Exception
            logCrashInfo <| sprintf "An unhandled exception from UI thread:"
            handle(ex)
            e.Handled <- true
            Application.Current.Shutdown()
            )
        System.AppDomain.CurrentDomain.UnhandledException.Add(fun e -> 
            match e.ExceptionObject with
            | :? System.Exception as ex ->
                logCrashInfo <| sprintf "An unhandled exception from background thread:"
                handle(ex)
            | _ ->
                logCrashInfo <| sprintf "An unhandled exception from background thread occurred."
            )

        Graphics.theWindow <- this
        WPFUI.timeTextBox <- hmsTimeTextBox
        // full window
        this.Title <- "Z-Tracker for Zelda 1 Randomizer"
        this.SizeToContent <- SizeToContent.Manual
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        this.Left <- System.Windows.SystemParameters.PrimaryScreenWidth - WIDTH
        this.Top <- 0.0
        this.Width <- WIDTH
        this.Height <- HEIGHT
        this.FontSize <- 18.

        let appMainCanvas, cm =  // a scope, so code below is less likely to touch rootCanvas
            //                             items  ow map  prog  dungeon tabs                timeline
            let APP_CONTENT_HEIGHT = float(30*5 + 11*3*9 + 30 + WPFUI.TH + 30 + 27*8 + 12*7 + 3 + WPFUI.TCH + 6)
            let rootCanvas =    new Canvas(Width=16.*WPFUI.OMTW, Height=APP_CONTENT_HEIGHT, Background=Brushes.Black)
            let appMainCanvas = new Canvas(Width=16.*WPFUI.OMTW, Height=APP_CONTENT_HEIGHT, Background=Brushes.Black)
            let style = new Style(typeof<ToolTip>)
            style.Setters.Add(new Setter(ToolTip.ForegroundProperty, Brushes.Orange))
            style.Setters.Add(new Setter(ToolTip.BackgroundProperty, Graphics.almostBlack))
            style.Setters.Add(new Setter(ToolTip.BorderBrushProperty, Brushes.DarkGray))
            rootCanvas.Resources.Add(typeof<ToolTip>, style)
            WPFUI.canvasAdd(rootCanvas, appMainCanvas, 0., 0.)
            let cm = new CustomComboBoxes.CanvasManager(rootCanvas, appMainCanvas)
            appMainCanvas, cm
        this.Content <- cm.RootCanvas
        let mainDock = new DockPanel(Width=appMainCanvas.Width, Height=appMainCanvas.Height)
        appMainCanvas.Children.Add(mainDock) |> ignore

        let stackPanel = new StackPanel(Orientation=Orientation.Vertical)
        let spacing = Thickness(0., 10., 0., 0.)

        let tb = new TextBox(Text="Startup Options:",IsReadOnly=true, Margin=spacing, TextAlignment=TextAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.))
        stackPanel.Children.Add(tb) |> ignore

        let hsPanel = new StackPanel(Margin=spacing, MaxWidth=WIDTH/2., Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
        let hsGrid = Graphics.makeGrid(3, 3, 30, 30)
        hsGrid.Background <- Brushes.Black
        let triforcesNumbered = ResizeArray()
        let triforcesLettered = ResizeArray()
        for i = 0 to 2 do
            let image = Graphics.BMPtoImage Graphics.emptyUnfoundNumberedTriforce_bmps.[i]
            triforcesNumbered.Add(image)
            Graphics.gridAdd(hsGrid, image, i, 0)
            let image = Graphics.BMPtoImage Graphics.emptyUnfoundLetteredTriforce_bmps.[i]
            triforcesLettered.Add(image)
            Graphics.gridAdd(hsGrid, image, i, 0)
        let turnHideDungeonNumbersOn() =
            for b in triforcesNumbered do
                b.Opacity <- 0.
            for b in triforcesLettered do
                b.Opacity <- 1.
        let turnHideDungeonNumbersOff() =
            for b in triforcesNumbered do
                b.Opacity <- 1.
            for b in triforcesLettered do
                b.Opacity <- 0.
        turnHideDungeonNumbersOff()
        let row1boxes = Array.init 3 (fun _ -> new TrackerModel.Box())
        for i = 0 to 2 do
            Graphics.gridAdd(hsGrid, Views.MakeBoxItem(cm, row1boxes.[i]), i, 1)
            Graphics.gridAdd(hsGrid, Views.MakeBoxItem(cm, new TrackerModel.Box()), i, 2)
        let turnHeartShuffleOn() = for b in row1boxes do b.Set(-1, TrackerModel.PlayerHas.NO)
        let turnHeartShuffleOff() = for b in row1boxes do b.Set(14, TrackerModel.PlayerHas.NO)
        turnHeartShuffleOn()
        let cutoffCanvas = new Canvas(Width=85., Height=85., ClipToBounds=true, IsHitTestVisible=false)
        cutoffCanvas.Children.Add(hsGrid) |> ignore
        let border = new Border(BorderBrush=Brushes.DarkGray, BorderThickness=Thickness(8.,8.,0.,0.), Child=cutoffCanvas)

        let checkboxSP = new StackPanel(Orientation=Orientation.Vertical, VerticalAlignment=VerticalAlignment.Center)
        let hscb = new CheckBox(Content=new TextBox(Text="Heart Shuffle",IsReadOnly=true,BorderThickness=Thickness(0.)), Margin=Thickness(10.))
        hscb.IsChecked <- System.Nullable.op_Implicit true
        hscb.Checked.Add(fun _ -> turnHeartShuffleOn())
        hscb.Unchecked.Add(fun _ -> turnHeartShuffleOff())
        checkboxSP.Children.Add(hscb) |> ignore

        let hdcb = new CheckBox(Content=new TextBox(Text="Hide Dungeon Numbers",IsReadOnly=true,BorderThickness=Thickness(0.)), Margin=Thickness(10.))
        hdcb.IsChecked <- System.Nullable.op_Implicit false
        hdcb.Checked.Add(fun _ -> turnHideDungeonNumbersOn())
        hdcb.Unchecked.Add(fun _ -> turnHideDungeonNumbersOff())
        checkboxSP.Children.Add(hdcb) |> ignore

        hsPanel.Children.Add(checkboxSP) |> ignore
        hsPanel.Children.Add(border) |> ignore
        stackPanel.Children.Add(hsPanel) |> ignore

        let tb = new TextBox(Text="\nNote: once you start, you can click the\n'start spot' icon in the legend\nto mark your start screen at any time\n",
                                IsReadOnly=true, Margin=spacing, TextAlignment=TextAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center)
        stackPanel.Children.Add(tb) |> ignore

        let mutable startButtonHasBeenClicked = false
        let mutable settingsWereSuccessfullyRead = false
        this.Closed.Add(fun _ ->  // still does not handle 'rude' shutdown, like if they close the console window
            if settingsWereSuccessfullyRead then      // don't overwrite an unreadable file, the user may have been intentionally hand-editing it and needs feedback
                TrackerModel.Options.writeSettings()  // save any settings changes they made before closing the startup window
            )
        let quests = [|
            0, "First Quest Overworld"
            1, "Second Quest Overworld"
            2, "Mixed - First Quest Overworld"
            3, "Mixed - Second Quest Overworld"
            |]
        for n,q in quests do
            let startButton = Graphics.makeButton(sprintf "Start: %s" q, None, None)
            startButton.Margin <- spacing
            startButton.Width <- WIDTH/2.
            stackPanel.Children.Add(startButton) |> ignore
            startButton.Click.Add(fun _ -> 
                if startButtonHasBeenClicked then () else
                startButtonHasBeenClicked <- true
                turnHeartShuffleOn()  // To draw the display, I have been interacting with the global ChoiceDomain for items.  This switches all the boxes back to empty, 'zeroing out' what we did.
                let tb = new TextBox(Text="\nLoading UI...\n", IsReadOnly=true, Margin=spacing, MaxWidth=WIDTH/2.)
                stackPanel.Children.Add(tb) |> ignore
                let ctxt = System.Threading.SynchronizationContext.Current
                Async.Start (async {
                    do! Async.SwitchToContext ctxt
                    TrackerModel.Options.writeSettings()

                    OptionsMenu.gamepadFailedToInitialize <- not(Gamepad.Initialize())

                    let heartShuffle = hscb.IsChecked.HasValue && hscb.IsChecked.Value
                    let kind = 
                        if hdcb.IsChecked.HasValue && hdcb.IsChecked.Value then
                            TrackerModel.DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS
                        else
                            TrackerModel.DungeonTrackerInstanceKind.DEFAULT
                    let speechRecognitionInstance = new SpeechRecognition.SpeechRecognitionInstance(kind)
                    if TrackerModel.Options.ListenForSpeech.Value then
                        printfn "Initializing microphone for speech recognition..."
                        try
                            SpeechRecognition.speechRecognizer.SetInputToDefaultAudioDevice()
                            SpeechRecognition.speechRecognizer.RecognizeAsync(System.Speech.Recognition.RecognizeMode.Multiple)
                        with ex ->
                            printfn "An exception setting up speech, speech recognition will be non-functional, but rest of app will work. Exception:"
                            printfn "%s" (ex.ToString())
                            printfn ""
                            OptionsMenu.microphoneFailedToInitialize <- true
                    else
                        printfn "Speech recognition will be disabled"
                        OptionsMenu.microphoneFailedToInitialize <- true
                    appMainCanvas.Children.Remove(mainDock)
                    let u = WPFUI.makeAll(cm, n, heartShuffle, kind, speechRecognitionInstance)
                    updateTimeline <- u
                    WPFUI.resetTimerEvent.Publish.Add(fun _ -> lastUpdateMinute <- 0; updateTimeline(0); this.SetStartTime(DateTime.Now))
                    Graphics.canvasAdd(cm.AppMainCanvas, hmsTimeTextBox, WPFUI.RIGHT_COL+160., 0.)
                    //let trans = new ScaleTransform(0.666666, 0.666666)   // does not look awful, but mouse warping does not account for change
                    //cm.RootCanvas.RenderTransform <- trans
                })
            )

        let bottomSP = new StackPanel(Orientation=Orientation.Vertical, HorizontalAlignment=HorizontalAlignment.Center)
        bottomSP.Children.Add(new Shapes.Rectangle(HorizontalAlignment=HorizontalAlignment.Stretch, Fill=Brushes.Black, Height=2., Margin=spacing)) |> ignore
        let tb = new TextBox(Text="Settings (most can be changed later, using 'Options...' button above timeline):", HorizontalAlignment=HorizontalAlignment.Center, 
                                Margin=Thickness(0.,0.,0.,10.), BorderThickness=Thickness(0.))
        bottomSP.Children.Add(tb) |> ignore
        TrackerModel.Options.readSettings()
        settingsWereSuccessfullyRead <- true
        WPFUI.voice.Volume <- TrackerModel.Options.Volume
        let options = OptionsMenu.makeOptionsCanvas(float(16*16*3))
        bottomSP.Children.Add(options) |> ignore
        mainDock.Children.Add(bottomSP) |> ignore
        DockPanel.SetDock(bottomSP, Dock.Bottom)

        mainDock.Children.Add(stackPanel) |> ignore

        // "dark theme"
        mainDock.Background <- Brushes.Black
        let style = new Style(typeof<TextBox>)
        style.Setters.Add(new Setter(TextBox.ForegroundProperty, Brushes.Orange))
        style.Setters.Add(new Setter(TextBox.BackgroundProperty, Brushes.Black))
        style.Setters.Add(new Setter(TextBox.BorderBrushProperty, Brushes.Orange))
        mainDock.Resources.Add(typeof<TextBox>, style)
        let style = new Style(typeof<Button>)
        style.Setters.Add(new Setter(Button.BorderBrushProperty, Brushes.Orange))
        style.Setters.Add(new Setter(Button.BackgroundProperty, Brushes.DarkGray))
        mainDock.Resources.Add(typeof<Button>, style)
        let style = new Style(typeof<ToolTip>)
        style.Setters.Add(new Setter(ToolTip.ForegroundProperty, Brushes.Orange))
        style.Setters.Add(new Setter(ToolTip.BackgroundProperty, Graphics.almostBlack))
        style.Setters.Add(new Setter(ToolTip.BorderBrushProperty, Brushes.DarkGray))
        mainDock.Resources.Add(typeof<ToolTip>, style)
        
    override this.Update(f10Press) =
        base.Update(f10Press)
        // update time
        let ts = DateTime.Now - this.StartTime
        let h,m,s = ts.Hours, ts.Minutes, ts.Seconds
        hmsTimeTextBox.Text <- sprintf "%2d:%02d:%02d" h m s
        // update timeline
        //if f10Press || (ts.TotalSeconds |> round |> int)%1 = 0 then
        //    updateTimeline((ts.TotalSeconds |> round |> int)/1)
        let curMinute = int ts.TotalMinutes
        if f10Press || curMinute > lastUpdateMinute then
            lastUpdateMinute <- curMinute
            updateTimeline(curMinute)

type TimerOnlyWindow() as this = 
    inherit MyWindowBase()
    let hmsTimeTextBox = new TextBox(Text="timer",FontSize=42.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    let canvas = new Canvas(Width=180., Height=50., Background=System.Windows.Media.Brushes.Black)
    do
        // full window
        this.Title <- "Timer"
        this.Content <- canvas
        Graphics.canvasAdd(canvas, hmsTimeTextBox, 0., 0.)
        this.SizeToContent <- SizeToContent.WidthAndHeight 
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        this.Left <- 0.0
        this.Top <- 0.0
    override this.Update(f10Press) =
        base.Update(f10Press)
        // update time
        let ts = DateTime.Now - this.StartTime
        let h,m,s = ts.Hours, ts.Minutes, ts.Seconds
        hmsTimeTextBox.Text <- sprintf "%02d:%02d:%02d" h m s

type TerrariaTimerOnlyWindow() as this = 
    inherit MyWindowBase()
    let FONT = 24.
    let hmsTimeTextBox = new TextBox(Text="timer",FontSize=FONT,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    let dayTextBox = new TextBox(Text="day",FontSize=FONT,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    let timeTextBox = new TextBox(Text="time",FontSize=FONT,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    let canvas = new Canvas(Width=170.*FONT/35., Height=FONT*16./4., Background=System.Windows.Media.Brushes.Black)
    do
        // full window
        this.Title <- "Timer"
        this.Content <- canvas
        Graphics.canvasAdd(canvas, hmsTimeTextBox, 0., 0.)
        Graphics.canvasAdd(canvas, dayTextBox, 0., FONT*5./4.)
        Graphics.canvasAdd(canvas, timeTextBox, 0., FONT*10./4.)
        this.SizeToContent <- SizeToContent.WidthAndHeight 
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        this.Left <- 0.0
        this.Top <- 0.0
        this.BorderBrush <- Brushes.LightGreen
        this.BorderThickness <- Thickness(2.)
    override this.Update(f10Press) =
        base.Update(f10Press)
        // update hms time
        let mutable ts = DateTime.Now - this.StartTime
        let h,m,s = ts.Hours, ts.Minutes, ts.Seconds
        hmsTimeTextBox.Text <- sprintf "%02d:%02d:%02d" h m s
        // update terraria time
        let mutable day = 1
        while ts >= TimeSpan.FromMinutes(20.25) do
            ts <- ts - TimeSpan.FromMinutes(24.)
            day <- day + 1
        let mutable ttime = ts + TimeSpan.FromMinutes(8.25)
        if ttime >= TimeSpan.FromMinutes(24.) then
            ttime <- ttime - TimeSpan.FromMinutes(24.)
        let m,s = ttime.Minutes, ttime.Seconds
        let m,am = if m < 12 then m,"am" else m-12,"pm"
        let m = if m=0 then 12 else m
        timeTextBox.Text <- sprintf "%02d:%02d%s" m s am
        if ts < TimeSpan.FromMinutes(11.25) then   // 11.25 is 7:30pm, 20.25 is 4:30am
            dayTextBox.Text <- sprintf "Day %d" day
        else
            dayTextBox.Text <- sprintf "Night %d" day

// in order for multiple app windows to not have a forced Z-Order from Owner-Child relationship, need a hidden dummy window to own all the visible windows
type DummyWindow() as this =
    inherit Window()
    do
        this.ShowInTaskbar <- false
        this.Title <- "Z-Tracker start-up..."
        this.Width <- 300.
        this.Height <- 100.
        this.Loaded.Add(fun _ ->
            this.Visibility <- Visibility.Hidden
            let mainW = new MyWindow()
            mainW.Owner <- this
            mainW.Show()
            )
    


[<STAThread>]
[<EntryPoint>]
let main argv = 
    printfn "Starting Z-Tracker..."

    let app = new Application()
#if DEBUG
    do
#else
    try
#endif
        if argv.Length > 0 && argv.[0] = "timeronly" then
            app.Run(TimerOnlyWindow()) |> ignore
        elif argv.Length > 0 && argv.[0] = "terraria" then
            app.Run(TerrariaTimerOnlyWindow()) |> ignore
        else
            app.Run(DummyWindow()) |> ignore
#if DEBUG
#else
    with e ->
        printfn "crashed with exception"
        printfn "%s" (e.ToString())
#endif
    
    
    printfn "press Enter to end"
    System.Console.ReadLine() |> ignore
    0
