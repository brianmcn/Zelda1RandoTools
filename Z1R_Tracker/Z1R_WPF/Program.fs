open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

// TODO
// free form text for seed flags?

let mutable f5WasRecentlyPressed = false

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
    member this.StartTime = startTime
    abstract member Update : bool -> unit
    default this.Update(f10Press) = ()
    override this.OnSourceInitialized(e) =
        base.OnSourceInitialized(e)
        let helper = new System.Windows.Interop.WindowInteropHelper(this)
        source <- System.Windows.Interop.HwndSource.FromHwnd(helper.Handle)
        source.AddHook(System.Windows.Interop.HwndSourceHook(fun a b c d e -> this.HwndHook(a,b,c,d,&e)))
        this.RegisterHotKey()
    override this.OnClosed(e) =
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
    member this.HwndHook(hwnd:IntPtr, msg:int, wParam:IntPtr, lParam:IntPtr, handled:byref<bool>) : IntPtr =
        let WM_HOTKEY = 0x0312
        if msg = WM_HOTKEY then
            if wParam.ToInt32() = Winterop.HOTKEY_ID then
                //let ctrl_bits = lParam.ToInt32() &&& 0xF  // see WM_HOTKEY docs
                let key = lParam.ToInt32() >>> 16
                if key = VK_F10 then
                    startTime <- DateTime.Now
                    this.Update(true)
                if key = VK_F5 then
                    f5WasRecentlyPressed <- true
        IntPtr.Zero

type MyWindow() as this = 
    inherit MyWindowBase()
    let mutable canvas, updateTimeline = null, fun _ -> ()
    let mutable lastUpdateMinute = 0
    let hmsTimeTextBox = new TextBox(Text="timer",FontSize=42.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false)
    //                 items  ow map  prog  dungeon tabs                      timeline   
    let HEIGHT = float(30*4 + 11*3*9 + 30 + WPFUI.TH + 30 + 27*8 + 12*7 + 3 + WPFUI.TCH + 6 + 40) // (what is the final 40?)
    let WIDTH = float(16*16*3 + 16)  // ow map width (what is the final 16?)
    do
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

        let stackPanel = new StackPanel(Orientation=Orientation.Vertical)
        let spacing = Thickness(0., 10., 0., 0.)

        let tb = new TextBox(Text="Choose overworld quest:", Margin=spacing)
        stackPanel.Children.Add(tb) |> ignore
        let owQuest = new ComboBox(IsEditable=false,IsReadOnly=true)
        owQuest.ItemsSource <- [|
                "First Quest"
                "Second Quest"
                "Mixed - First Quest"
                "Mixed - Second Quest"
            |]
        owQuest.SelectedIndex <- 2
        stackPanel.Children.Add(owQuest) |> ignore
        
        let tb = new TextBox(Text="Settings (most can be changed later, using 'Options...' button above timeline):", Margin=spacing)
        stackPanel.Children.Add(tb) |> ignore
        TrackerModel.Options.readSettings()
        WPFUI.voice.Volume <- TrackerModel.Options.Volume
        let options = OptionsMenu.makeOptionsCanvas(float(16*16*3), float(WPFUI.TCH+6), 0.)
        options.IsHitTestVisible <- true
        options.Opacity <- 1.0
        stackPanel.Children.Add(options) |> ignore

        let tb = new TextBox(Text="\nNote: once you start, you can use F5 to\nplace the 'start spot' icon at your mouse,\nor F10 to reset the timer to 0, at any time\n",IsReadOnly=true, Margin=spacing)
        stackPanel.Children.Add(tb) |> ignore

        let startButton = new Button(Content=new TextBox(Text="Start Z-Tracker",IsReadOnly=true), Margin=spacing)
        stackPanel.Children.Add(startButton) |> ignore
        
        let hstackPanel = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
        hstackPanel.Children.Add(stackPanel) |> ignore
        this.Content <- hstackPanel
        
        startButton.Click.Add(fun _ -> 
                let tb = new TextBox(Text="\nLoading UI...\n", IsReadOnly=true, Margin=spacing)
                stackPanel.Children.Add(tb) |> ignore
                let ctxt = System.Threading.SynchronizationContext.Current
                Async.Start (async {
                    do! Async.SwitchToContext ctxt
                    TrackerModel.Options.writeSettings()
                    if TrackerModel.Options.ListenForSpeech.Value then
                        printfn "Initializing microphone for speech recognition..."
                        try
                            WPFUI.speechRecognizer.SetInputToDefaultAudioDevice()
                            WPFUI.speechRecognizer.RecognizeAsync(System.Speech.Recognition.RecognizeMode.Multiple)
                        with ex ->
                            printfn "An exception setting up speech, speech recognition will be non-functional, but rest of app will work. Exception:"
                            printfn "%s" (ex.ToString())
                            printfn ""
                            OptionsMenu.microphoneFailedToInitialize <- true
                    else
                        printfn "Speech recognition will be disabled"
                        OptionsMenu.microphoneFailedToInitialize <- true
                    let c,u = WPFUI.makeAll(owQuest.SelectedIndex)
                    canvas <- c
                    updateTimeline <- u
                    Graphics.canvasAdd(canvas, hmsTimeTextBox, WPFUI.RIGHT_COL+40., 0.)
                    this.Content <- canvas
                    System.Windows.Application.Current.DispatcherUnhandledException.Add(fun e -> 
                        let ex = e.Exception
                        printfn "An unhandled exception from UI thread:"
                        printfn "%s" (ex.ToString())
                        printfn "press Enter to end"
                        System.Console.ReadLine() |> ignore
                        )
                    System.AppDomain.CurrentDomain.UnhandledException.Add(fun e -> 
                        let ex = e.ExceptionObject
                        printfn "An unhandled exception from background thread:"
                        printfn "%s" (ex.ToString())
                        printfn "press Enter to end"
                        System.Console.ReadLine() |> ignore
                        )
                })
            )
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
        // update start icon
        if f5WasRecentlyPressed then
            TrackerModel.startIconX <- WPFUI.currentlyMousedOWX
            TrackerModel.startIconY <- WPFUI.currentlyMousedOWY
            TrackerModel.forceUpdate()
            f5WasRecentlyPressed <- false

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
            app.Run(MyWindow()) |> ignore
#if DEBUG
#else
    with e ->
        printfn "crashed with exception"
        printfn "%s" (e.ToString())
#endif
    
    
    printfn "press enter to end"
    System.Console.ReadLine() |> ignore
    0
