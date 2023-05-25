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

    [<DllImport("kernel32.dll")>]
    extern IntPtr GetConsoleWindow()
    [<DllImport("user32.dll")>]
    extern bool ShowWindow(IntPtr hWnd, int nCmdShow)
    let SW_HIDE = 0
    let SW_SHOW = 5
    let SW_MINIMIZE = 6

    (*
    [<ComImport>]
    [<Guid("00021401-0000-0000-C000-000000000046")>]
    internal type ShellLink

    let shellLink = 
        let ty = System.Type.GetTypeFromCLSID (System.Guid "00021401-0000-0000-C000-000000000046")
        Activator.CreateInstance ty
    *)

    [<ComImport>]
    [<InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>]
    [<Guid("000214F9-0000-0000-C000-000000000046")>]
    type IShellLink =
        abstract member GetPath : [<Out; MarshalAs(UnmanagedType.LPWStr)>] pszFile:System.Text.StringBuilder * cchMaxPath:int * [<Out>]pfd:IntPtr * fFlags:int -> unit
        abstract member GetIDList : [<Out>]ppidl:IntPtr -> unit
        abstract member SetIDList : pidl:IntPtr -> unit
        abstract member GetDescription : [<Out; MarshalAs(UnmanagedType.LPWStr)>] pszName:System.Text.StringBuilder * cchMaxName:int -> unit
        abstract member SetDescription : [<MarshalAs(UnmanagedType.LPWStr)>] pszName:string -> unit
        abstract member GetWorkingDirectory : [<Out; MarshalAs(UnmanagedType.LPWStr)>] pszDir:System.Text.StringBuilder * cchMaxPath:int -> unit
        abstract member SetWorkingDirectory : [<MarshalAs(UnmanagedType.LPWStr)>] pszDir:string -> unit
        abstract member GetArguments : [<Out; MarshalAs(UnmanagedType.LPWStr)>] pszArgs:System.Text.StringBuilder * cchMaxPath:int -> unit
        abstract member SetArguments : [<MarshalAs(UnmanagedType.LPWStr)>] pszArgs:string -> unit
        abstract member GetHotkey : [<Out>] pwHotkey:int16 -> unit
        abstract member SetHotkey : wHotkey:int16 -> unit 
        abstract member GetShowCmd : [<Out>] piShowCmd:int -> unit
        abstract member SetShowCmd : iShowCmd:int -> unit
        abstract member GetIconLocation : [<Out; MarshalAs(UnmanagedType.LPWStr)>] pszIconPath:System.Text.StringBuilder * cchIconPath:int * [<Out>]piIcon:int -> unit
        abstract member SetIconLocation : [<MarshalAs(UnmanagedType.LPWStr)>] pszIconPath:string * iIcon:int -> unit
        abstract member SetRelativePath : [<MarshalAs(UnmanagedType.LPWStr)>] pszPathRel:string * dwReserved:int -> unit
        abstract member Resolve : hwnd:IntPtr * fFlags:int -> unit
        abstract member SetPath : [<MarshalAs(UnmanagedType.LPWStr)>] pszFile:string -> unit

    
type MyWindowBase() as this = 
    inherit Window()
    let mutable source = null
    let VK_F10 = 0x79
#if DEBUG
#else
    let VK_F5 = 0x74
    let MOD_NONE = 0u
#endif
    let startTime = TrackerModel.theStartTime
    do
        // full window
        let timer = new System.Windows.Threading.DispatcherTimer()
        timer.Interval <- TimeSpan.FromSeconds(0.1)
        timer.Tick.Add(fun _ -> this.Update(false))
        timer.Start()
    member this.SetStartTimeToNow() = startTime.SetNow()
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

let MakeTipTextBox(txt) =
    new TextBox(Text=txt, FontSize=14., IsReadOnly=true, TextAlignment=TextAlignment.Left, HorizontalAlignment=HorizontalAlignment.Left, TextWrapping=TextWrapping.Wrap, BorderThickness=Thickness(0.))

let ApplyKonamiCodeEasterEgg(cm:CustomComboBoxes.CanvasManager, fe:FrameworkElement) =
    let mutable i = 0
    let code = [|
        Input.Key.Up
        Input.Key.Up
        Input.Key.Down
        Input.Key.Down
        Input.Key.Left
        Input.Key.Right
        Input.Key.Left
        Input.Key.Right
        Input.Key.B
        Input.Key.A
        |]
    // Note: they need to click on an element inside the window (such as text of a tip) first to give focus to fe
    fe.PreviewKeyDown.Add(fun ea ->
        if i < code.Length && ea.Key = code.[i] then
            i <- i + 1
//            printfn "%A" ea.Key
            if i = code.Length then
                let wh = new System.Threading.ManualResetEvent(false)
                let tips = new StackPanel(Orientation=Orientation.Vertical)
                let header(txt) =
                    let tb = MakeTipTextBox(txt)
                    tb.BorderThickness <- Thickness(0.,0.,0.,2.)
                    tb.BorderBrush <- Brushes.Orange
                    tb.FontSize <- tb.FontSize + 2.
                    tb.Margin <- Thickness(0., 15., 0., 10.)
                    tb
                let mutable even = false
                let body(txt) =
                    let tb = MakeTipTextBox(txt)
                    tb.Margin <- Thickness(0., 8., 0., 0.)
                    if even then
                        tb.Background <- Brushes.Black
                    else
                        tb.Background <- Graphics.almostBlack
                    even <- not even
                    tb
                tips.Children.Add(header("ALL OF TIPS")) |> ignore   // grammar mimics 'ALL OF ITEMS' in LoZ startup scroll
                tips.Children.Add(header("Novice-level tips")) |> ignore
                for t in DungeonData.Factoids.noviceTips do
                    tips.Children.Add(body(t)) |> ignore
                tips.Children.Add(header("Intermediate-level tips")) |> ignore
                for t in DungeonData.Factoids.intermediateTips do
                    tips.Children.Add(body(t)) |> ignore
                tips.Children.Add(header("Advanced-level tips")) |> ignore
                for t in DungeonData.Factoids.advancedTips do
                    tips.Children.Add(body(t)) |> ignore
                tips.Children.Add(header("Z-Tracker tips")) |> ignore
                for t in DungeonData.Factoids.zTrackerTips do
                    tips.Children.Add(body(t)) |> ignore
                let sv = new ScrollViewer(HorizontalAlignment=HorizontalAlignment.Left, VerticalAlignment=VerticalAlignment.Top, Content=tips)
                let BUFFER = 160.
                let tipsBorder = new Border(Width=cm.AppMainCanvas.Width-BUFFER, Height=cm.AppMainCanvas.Height-BUFFER, Background=Brushes.Black, 
                                            BorderBrush=Brushes.Orange, BorderThickness=Thickness(3.), Child=sv)
                tipsBorder.Resources.Add(typeof<TextBox>, fe.Resources.[typeof<TextBox>])
                CustomComboBoxes.DoModal(cm, wh, BUFFER/2., BUFFER/2., tipsBorder) |> Async.StartImmediate
        )

type MyWindow() as this = 
    inherit MyWindowBase()
    let mutable updateTimeline = fun _ -> ()
    let mutable lastUpdateMinute = 0
    //                             items  ow map  prog  dungeon tabs                                    timeline   
    let HEIGHT_SANS_CHROME = float(30*5 + 11*3*9 + 30 + OverworldItemGridUI.TH + 30 + 27*8 + 12*7 + 3 + OverworldItemGridUI.TCH + 6)
    let WIDTH_SANS_CHROME = float(16*16*3)  // ow map width
    let CHROME_WIDTH, CHROME_HEIGHT = 16., 39.  // Windows app border
    let HEIGHT = HEIGHT_SANS_CHROME + CHROME_HEIGHT
    let WIDTH = WIDTH_SANS_CHROME + CHROME_WIDTH
    let mutable loggedAnyCrash = false
    let mutable promptedCrashRecovery = false
    let mutable gotThruStartup = false
    let crashLogFilename = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Z1R_Tracker_crash_log.txt")
    let dateTimeFormat = "yyyy-MM-dd-HH:mm:ss"
    do
        let logCrashInfo(s:string) =
            printfn "%s" s
            try
                if not loggedAnyCrash then
                    loggedAnyCrash <- true
                    System.IO.File.AppendAllText(crashLogFilename, sprintf "\n\nBEGIN CRASH LOG -- %s -- %s\n" OverworldData.ProgramNameString (DateTime.Now.ToString(dateTimeFormat)))
                System.IO.File.AppendAllText(crashLogFilename, sprintf "%s\n" s)
            with _ -> ()
        let finishCrashInfoImpl(extra) =
            let finalText = if gotThruStartup then extra else "(during startup)\n"  // this intentionally interacts with crash recovery dialog
            System.IO.File.AppendAllText(crashLogFilename, sprintf "END CRASH LOG\n%s\n%s" (DateTime.Now.ToString(dateTimeFormat)) finalText)
        let finishCrashInfo() = finishCrashInfoImpl("")
        let handle(ex:System.Exception) =
            match ex with
            | :? TrackerModelOptions.IntentionalApplicationShutdown as ias ->
                logCrashInfo <| sprintf "%s" ias.Message
                finishCrashInfo()
            | :? HotKeys.UserError as hue ->
                logCrashInfo ""
                logCrashInfo "Error parsing HotKeys.txt:"
                logCrashInfo ""
                logCrashInfo <| sprintf "%s" hue.Message
                logCrashInfo ""
                logCrashInfo "You should fix this error by editing the text file."
                logCrashInfo "Or you can delete it, and an empty hotkeys template file will be created in its place."
                logCrashInfo ""
                finishCrashInfo()
                System.Threading.Thread.Sleep(2000)
                let fileToSelect = HotKeys.HotKeyFilename
                let args = sprintf "/Select, \"%s\"" fileToSelect
                let psi = new System.Diagnostics.ProcessStartInfo("Explorer.exe", args)
                System.Diagnostics.Process.Start(psi) |> ignore
            | _ ->
                logCrashInfo <| sprintf "%s" (ex.ToString())
                finishCrashInfo()
        System.Windows.Application.Current.DispatcherUnhandledException.Add(fun e -> 
            let ex = e.Exception
            logCrashInfo <| sprintf "An unhandled exception from UI thread:"
            handle(ex)
            e.Handled <- true
            finishCrashInfo()
            Application.Current.Shutdown()
            )
        System.AppDomain.CurrentDomain.UnhandledException.Add(fun e -> 
            match e.ExceptionObject with
            | :? System.Exception as ex ->
                logCrashInfo <| sprintf "An unhandled exception from background thread:"
                handle(ex)
                finishCrashInfo()
            | _ ->
                logCrashInfo <| sprintf "An unhandled exception from background thread occurred."
                finishCrashInfo()
            )

        HotKeys.PopulateHotKeyTables()
        let mutable settingsWereSuccessfullyRead = false
        TrackerModelOptions.readSettings()
        settingsWereSuccessfullyRead <- true
        WPFUI.voice.Volume <- TrackerModelOptions.Volume

        do
            let shellLink = 
                let ty = System.Type.GetTypeFromCLSID (System.Guid "00021401-0000-0000-C000-000000000046")
                Activator.CreateInstance ty
            let isl = shellLink :?> Winterop.IShellLink
            let cwd = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)
            isl.SetDescription("Launch Z-Tracker")
            isl.SetPath(System.IO.Path.Combine(cwd, "Z1R_WPF.exe"))
            isl.SetWorkingDirectory(cwd)
            isl.SetIconLocation(System.IO.Path.Combine(cwd, "icons/ztlogo64x64.ico"), 0)
            let ipf = isl :?> System.Runtime.InteropServices.ComTypes.IPersistFile
            ipf.Save(System.IO.Path.Combine(cwd, "ZTracker.lnk"), false)

        Graphics.theWindow <- this
        // full window
        this.Title <- "Z-Tracker for Zelda 1 Randomizer"
        this.ResizeMode <- ResizeMode.CanMinimize
        this.SizeToContent <- SizeToContent.Manual
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        let APP_WIDTH, APP_HEIGHT = 
            if TrackerModelOptions.SmallerAppWindow.Value then 
                round(WIDTH_SANS_CHROME*TrackerModelOptions.SmallerAppWindowScaleFactor + CHROME_WIDTH), round(HEIGHT_SANS_CHROME*TrackerModelOptions.SmallerAppWindowScaleFactor + CHROME_HEIGHT)
            else 
                WIDTH, HEIGHT
        //printfn "%f, %f" APP_WIDTH APP_HEIGHT
        let leftTop = TrackerModelOptions.MainWindowLT
        let matches = System.Text.RegularExpressions.Regex.Match(leftTop, """^(-?\d+),(-?\d+)$""")
        if matches.Success then
            this.Left <- float matches.Groups.[1].Value
            this.Top <- float matches.Groups.[2].Value
        this.LocationChanged.Add(fun _ ->
            if this.Left < -30000.0 || this.Top < -30000.0 then
                ()  // when you Minimize the window, -32000,-32000 are reported. Don't save that, as it's hard to recover window later
            else
                TrackerModelOptions.MainWindowLT <- sprintf "%d,%d" (int this.Left) (int this.Top)
                TrackerModelOptions.writeSettings()
            )
        this.Width <- APP_WIDTH
        this.Height <- APP_HEIGHT
        this.FontSize <- 18.
        this.Loaded.Add(fun _ -> this.Focus() |> ignore)
        this.Background <- Brushes.Black  // on a device with say 125% pixels and UseLayoutRounding=true, there's a white line at the bottom/right edge where RootCanvas doesn't cover app 

        let appMainCanvas, cm =  // a scope, so code below is less likely to touch rootCanvas
            let APP_CONTENT_HEIGHT = HEIGHT_SANS_CHROME
            let rootCanvas =    new Canvas(Width=16.*Graphics.OMTW, Height=APP_CONTENT_HEIGHT, Background=Brushes.Black)
            rootCanvas.UseLayoutRounding <- true
            let appMainCanvas = new Canvas(Width=16.*Graphics.OMTW, Height=APP_CONTENT_HEIGHT, Background=Brushes.Black)
            let style = new Style(typeof<ToolTip>)
            style.Setters.Add(new Setter(ToolTip.ForegroundProperty, Brushes.Orange))
            style.Setters.Add(new Setter(ToolTip.BackgroundProperty, Graphics.almostBlack))
            style.Setters.Add(new Setter(ToolTip.BorderBrushProperty, Brushes.DarkGray))
            rootCanvas.Resources.Add(typeof<ToolTip>, style)
            Graphics.canvasAdd(rootCanvas, appMainCanvas, 0., 0.)
            let cm = new CustomComboBoxes.CanvasManager(rootCanvas, appMainCanvas)
            appMainCanvas, cm
        let wholeCanvas, hmsTimerCanvas = new Canvas(), new Canvas()
        this.Content <- wholeCanvas
        let drawingCanvasHolder = new Canvas()  // gets the app RenderTransform (can't RenderTransform the drawingCanvas itself without screwing up Broadcast Window)
        let drawingCanvas = new Canvas(IsHitTestVisible=false)
        drawingCanvasHolder.Children.Add(drawingCanvas) |> ignore
        if TrackerModelOptions.SmallerAppWindow.Value then 
            let trans = new ScaleTransform(TrackerModelOptions.SmallerAppWindowScaleFactor, TrackerModelOptions.SmallerAppWindowScaleFactor)
            cm.RootCanvas.RenderTransform <- trans
            hmsTimerCanvas.RenderTransform <- trans
            drawingCanvasHolder.RenderTransform <- trans
        wholeCanvas.Children.Add(cm.RootCanvas) |> ignore
        wholeCanvas.Children.Add(hmsTimerCanvas) |> ignore
        wholeCanvas.Children.Add(drawingCanvasHolder) |> ignore
        cm.AfterCreatePopupCanvas.Add(fun _ -> drawingCanvas.Opacity <- 0.)
        cm.BeforeDismissPopupCanvas.Add(fun _ -> if cm.PopupCanvasStack.Count=0 then drawingCanvas.Opacity <- 1.)
        let mainDock = new DockPanel(Width=appMainCanvas.Width, Height=appMainCanvas.Height)
        ApplyKonamiCodeEasterEgg(cm, mainDock)
        appMainCanvas.Children.Add(mainDock) |> ignore

        let addDarkTheme(rd:ResourceDictionary) = 
            let style = new Style(typeof<TextBox>)
            style.Setters.Add(new Setter(TextBox.ForegroundProperty, Brushes.Orange))
            style.Setters.Add(new Setter(TextBox.BackgroundProperty, Brushes.Black))
            style.Setters.Add(new Setter(TextBox.BorderBrushProperty, Brushes.Orange))
            rd.Add(typeof<TextBox>, style)
            let style = new Style(typeof<Button>)
            style.Setters.Add(new Setter(Button.BorderBrushProperty, Brushes.Orange))
            style.Setters.Add(new Setter(Button.BackgroundProperty, Brushes.DarkGray))
            rd.Add(typeof<Button>, style)
            let style = new Style(typeof<ToolTip>)
            style.Setters.Add(new Setter(ToolTip.ForegroundProperty, Brushes.Orange))
            style.Setters.Add(new Setter(ToolTip.BackgroundProperty, Graphics.almostBlack))
            style.Setters.Add(new Setter(ToolTip.BorderBrushProperty, Brushes.DarkGray))
            rd.Add(typeof<ToolTip>, style)

        let mainStackPanel = new StackPanel(Orientation=Orientation.Vertical)
        let spacing = Thickness(0., 8., 0., 0.)
        let smallSpacing = Thickness(0., 3., 0., 0.)

        let mutable startButtonHasBeenClicked = false
        do        
            let menu(wh:Threading.ManualResetEvent) = 
                let mkTxt(txt) = new TextBox(Text=txt,IsReadOnly=true, Margin=spacing, //TextAlignment=TextAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, 
                                                BorderThickness=Thickness(0.), FontSize=16.)
                let sp = new StackPanel(Orientation=Orientation.Vertical, Width=appMainCanvas.Width-60., Margin=Thickness(10.))
                let title = new TextBox(Text="Window Size",IsReadOnly=true, TextAlignment=TextAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, 
                                            BorderThickness=Thickness(0.,0.,0.,3.), FontSize=20.)
                sp.Children.Add(title) |> ignore
                sp.Children.Add(mkTxt("Z-Tracker can run in a few window sizes: '4/3 size', 'Default', '5/6 size', or '2/3 size'.")) |> ignore
                let MODE8,MODE6,MODE5,MODE4 = "4/3 size (Largest)", "Default", "5/6 size", "2/3 size (Smallest)"
                let curMode = 
                    if TrackerModelOptions.SmallerAppWindow.Value then 
                        if TrackerModelOptions.SmallerAppWindowScaleFactor = 2.0/3.0 then 
                            MODE4
                        elif TrackerModelOptions.SmallerAppWindowScaleFactor = 5.0/6.0 then 
                            MODE5
                        elif TrackerModelOptions.SmallerAppWindowScaleFactor = 4.0/3.0 then 
                            MODE8
                        else
                            sprintf "scale factor: %f" TrackerModelOptions.SmallerAppWindowScaleFactor
                    else 
                        MODE6
                sp.Children.Add(mkTxt(sprintf "The current Z-Tracker window size is '%s'.  You can change the setting here:" curMode)) |> ignore
                sp.Children.Add(new DockPanel(Height=20.)) |> ignore
                let rb8 = new RadioButton(Content=new TextBox(Text=MODE8,IsReadOnly=true,IsHitTestVisible=false,BorderThickness=Thickness(0.),FontSize=16.),Margin=Thickness(20.,0.,0.,0.))
                let rb6 = new RadioButton(Content=new TextBox(Text=MODE6,IsReadOnly=true,IsHitTestVisible=false,BorderThickness=Thickness(0.),FontSize=16.),Margin=Thickness(20.,0.,0.,0.))
                let rb5 = new RadioButton(Content=new TextBox(Text=MODE5,IsReadOnly=true,IsHitTestVisible=false,BorderThickness=Thickness(0.),FontSize=16.),Margin=Thickness(20.,0.,0.,0.))
                let rb4 = new RadioButton(Content=new TextBox(Text=MODE4,IsReadOnly=true,IsHitTestVisible=false,BorderThickness=Thickness(0.),FontSize=16.),Margin=Thickness(20.,0.,0.,0.))
                if TrackerModelOptions.SmallerAppWindow.Value then
                    if curMode=MODE5 then
                        rb5.IsChecked <- System.Nullable.op_Implicit true
                    elif curMode=MODE8 then
                        rb8.IsChecked <- System.Nullable.op_Implicit true
                    else
                        rb4.IsChecked <- System.Nullable.op_Implicit true
                else
                    rb6.IsChecked <- System.Nullable.op_Implicit true
                let mutable desireSmaller = TrackerModelOptions.SmallerAppWindow.Value
                let mutable desireScale = 2.0/3.0
                rb8.Checked.Add(fun _ -> desireSmaller <- true; desireScale <- 4.0/3.0)
                rb6.Checked.Add(fun _ -> desireSmaller <- false)
                rb5.Checked.Add(fun _ -> desireSmaller <- true; desireScale <- 5.0/6.0)
                rb4.Checked.Add(fun _ -> desireSmaller <- true; desireScale <- 2.0/3.0)
                let inner = new StackPanel(Orientation=Orientation.Vertical, HorizontalAlignment=HorizontalAlignment.Center)
                inner.Children.Add(rb8) |> ignore
                inner.Children.Add(rb6) |> ignore
                inner.Children.Add(rb5) |> ignore
                inner.Children.Add(rb4) |> ignore
                sp.Children.Add(inner) |> ignore
                
                let title = new TextBox(Text="Window Shape",IsReadOnly=true, TextAlignment=TextAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, 
                                            BorderThickness=Thickness(0.,0.,0.,3.), FontSize=20., Margin=spacing)
                sp.Children.Add(title) |> ignore
                sp.Children.Add(mkTxt("Z-Tracker can run in two window shapes: 'Tall' (default) and 'Square' (smaller/shorter).")) |> ignore
                sp.Children.Add(mkTxt("The 'Square' option auto-swaps between Overworld and Dungeon focus, based on your mouse.")) |> ignore
                sp.Children.Add(mkTxt("Note: This option only affects the main application; the startup options screen is always 'Tall'.")) |> ignore
                sp.Children.Add(mkTxt("Note: 'Square' disables some features: overworld magnifier, broadcast window, Draw&UCC buttons.")) |> ignore
                let mutable desireShort = TrackerModelOptions.ShorterAppWindow.Value
                let tallC = new Canvas(Width=77., Height=99., Background=Brushes.Red)
                let squareC = new Canvas(Width=77., Height=77., Background=Brushes.Red)
                let tx(s) = new TextBox(Text=s,IsReadOnly=true,FontSize=16.,BorderThickness=Thickness(0.),Foreground=Brushes.Black,Background=Brushes.Transparent,FontWeight=FontWeights.Bold,
                                        Width=77., TextAlignment=TextAlignment.Center, IsHitTestVisible=false)
                Graphics.canvasAdd(tallC, tx("Tall"), 0., 30.)
                Graphics.canvasAdd(squareC, tx("Square"), 0., 30.)
                let rbTall   = new RadioButton(Content=tallC,   VerticalContentAlignment=VerticalAlignment.Center, IsChecked=System.Nullable.op_Implicit (not desireShort))
                let rbSquare = new RadioButton(Content=squareC, VerticalContentAlignment=VerticalAlignment.Center, IsChecked=System.Nullable.op_Implicit desireShort)
                let inner = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center, Margin=spacing)
                rbTall.Checked.Add(fun _ -> desireShort <- false)
                rbSquare.Checked.Add(fun _ -> desireShort <- true)
                inner.Children.Add(rbTall) |> ignore
                inner.Children.Add(new DockPanel(Width=30.)) |> ignore  // spacer
                inner.Children.Add(rbSquare) |> ignore
                sp.Children.Add(inner) |> ignore
                
                sp.Children.Add(new DockPanel(Background=Brushes.Gray, Margin=Thickness(20., 8., 20., 0.), Height=4.)) |> ignore
                let warn = mkTxt("Changes to these settings will only take effect next time:")
                warn.FontSize <- 20.
                warn.FontWeight <- FontWeights.Bold
                sp.Children.Add(warn) |> ignore
                let buttons = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center, Margin=spacing)
                let makeButton(head, text) =
                    let sp = new StackPanel(Background=Graphics.almostBlack, Orientation=Orientation.Vertical)
                    let tb1 = new TextBox(Text=head,IsReadOnly=true,IsHitTestVisible=false,TextAlignment=TextAlignment.Center,BorderThickness=Thickness(0.),
                                            Background=Graphics.almostBlack,FontSize=24.,FontWeight=FontWeights.Bold)
                    let tb2 = new TextBox(Text=text,IsReadOnly=true,IsHitTestVisible=false,TextAlignment=TextAlignment.Center,BorderThickness=Thickness(0.),
                                            Background=Graphics.almostBlack,FontSize=16.)
                    sp.Children.Add(tb1) |> ignore
                    sp.Children.Add(tb2) |> ignore
                    let button = new Button(Content=sp, HorizontalContentAlignment=HorizontalAlignment.Stretch, VerticalContentAlignment=VerticalAlignment.Stretch,BorderThickness=Thickness(3.,3.,3.,3.))
                    button
                let sb = makeButton("Save changes","Save changes and close Z-Tracker\n(changes take effect next time)")
                let cb = makeButton("Discard changes","Don't make any changes\n(exit this popup menu)")
                cb.Click.Add(fun _ -> wh.Set() |> ignore)
                sb.Click.Add(fun _ ->
                    TrackerModelOptions.SmallerAppWindow.Value <- desireSmaller
                    TrackerModelOptions.SmallerAppWindowScaleFactor <- desireScale
                    TrackerModelOptions.ShorterAppWindow.Value <- desireShort
                    TrackerModelOptions.writeSettings()
                    this.Close()
                    )
                buttons.Children.Add(sb) |> ignore
                buttons.Children.Add(new DockPanel(Width=30.)) |> ignore  // spacing
                buttons.Children.Add(cb) |> ignore
                sp.Children.Add(buttons) |> ignore
                addDarkTheme(sp.Resources)
                new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Background=Brushes.Black, Child=sp)

            let fs = if TrackerModelOptions.SmallerAppWindow.Value then 16. else 12.
            let topBar = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
            let tb = new TextBox(Text="Z-Tracker window too large/small?", Margin=Thickness(0.,0.,10.,0.),
                                    IsReadOnly=true, BorderThickness=Thickness(0.), FontSize=fs, VerticalAlignment=VerticalAlignment.Center)
            topBar.Children.Add(tb) |> ignore
            let b = Graphics.makeButton("Click here for options", Some(fs), None)
            let mutable popupIsActive = false
            b.Click.Add(fun _ -> 
                if startButtonHasBeenClicked then () else
                if not popupIsActive then
                    popupIsActive <- true
                    let wh = new Threading.ManualResetEvent(false)
                    CustomComboBoxes.DoModal(cm, wh, 20., 70., menu(wh)) |> Async.StartImmediate
                    popupIsActive <- false
                )
            topBar.Children.Add(b) |> ignore
            let curFactor = if TrackerModelOptions.SmallerAppWindow.Value then TrackerModelOptions.SmallerAppWindowScaleFactor else 1.0
            let workingAreaTooSmallForCurrentHeight = SystemParameters.WorkArea.Height < (1000.0 * curFactor)
            let likelyDoesntFit = workingAreaTooSmallForCurrentHeight  // 'smaller' now might mean larger    && not(TrackerModelOptions.SmallerAppWindow.Value)
            let barColor = 
                if likelyDoesntFit then 
                    new SolidColorBrush(Color.FromRgb(120uy, 30uy, 30uy))   // reddish
                else 
                    new SolidColorBrush(Color.FromRgb(50uy, 50uy, 50uy))    // grayish
            let dp = new DockPanel(Height=(if curFactor<1.0 then 40. else 30.), LastChildFill=true, Background=barColor)
            dp.Children.Add(topBar) |> ignore
            mainStackPanel.Children.Add(dp) |> ignore

        let stackPanel = new StackPanel(Orientation=Orientation.Vertical, Width=WIDTH_SANS_CHROME)
        let startupOptionsCanvas = new Canvas()
        startupOptionsCanvas.Children.Add(stackPanel) |> ignore
        mainStackPanel.Children.Add(startupOptionsCanvas) |> ignore
        do  // version button, drawn atop without affecting layout
            let dp = new DockPanel(Width=WIDTH_SANS_CHROME, Height=30., LastChildFill=false)
            let vb = Graphics.dock(CustomComboBoxes.makeVersionButtonWithBehavior(cm), Dock.Top)
            DockPanel.SetDock(vb, Dock.Right)
            dp.Children.Add(vb) |> ignore
            startupOptionsCanvas.Children.Add(dp) |> ignore

        let tb = new TextBox(Text="Startup Options:",IsReadOnly=true, TextAlignment=TextAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.))
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
        let row1boxes = Array.init 3 (fun _ -> new TrackerModel.Box(TrackerModel.StairKind.Never, TrackerModel.BoxOwner.None))
        for i = 0 to 2 do
            Graphics.gridAdd(hsGrid, Views.MakeBoxItem(cm, row1boxes.[i]), i, 1)
            Graphics.gridAdd(hsGrid, Views.MakeBoxItem(cm, new TrackerModel.Box(TrackerModel.StairKind.Never, TrackerModel.BoxOwner.None)), i, 2)
        let turnHeartShuffleOn() = for b in row1boxes do b.Set(-1, TrackerModel.PlayerHas.NO)
        let turnHeartShuffleOff() = for b in row1boxes do b.Set(14, TrackerModel.PlayerHas.NO)
        turnHeartShuffleOn()
        let cutoffCanvas = new Canvas(Width=85., Height=85., ClipToBounds=true, IsHitTestVisible=false)
        cutoffCanvas.Children.Add(hsGrid) |> ignore
        let border = new Border(BorderBrush=Brushes.DarkGray, BorderThickness=Thickness(8.,8.,0.,0.), Child=cutoffCanvas)

        let checkboxSP = new StackPanel(Orientation=Orientation.Vertical, VerticalAlignment=VerticalAlignment.Center)
        let hscb = new CheckBox(Content=new TextBox(Text="Heart Shuffle",IsReadOnly=true,BorderThickness=Thickness(0.)), Margin=Thickness(10.))
        Graphics.scaleUpCheckBoxBox(hscb, 1.66)
        hscb.IsChecked <- System.Nullable.op_Implicit true
        hscb.Checked.Add(fun _ -> turnHeartShuffleOn())
        hscb.Unchecked.Add(fun _ -> turnHeartShuffleOff())
        checkboxSP.Children.Add(hscb) |> ignore

        let hdcb = new CheckBox(Content=new TextBox(Text="Hide Dungeon Numbers",IsReadOnly=true,BorderThickness=Thickness(0.)), Margin=Thickness(10.))
        Graphics.scaleUpCheckBoxBox(hdcb, 1.66)
        hdcb.IsChecked <- System.Nullable.op_Implicit false
        hdcb.Checked.Add(fun _ -> turnHideDungeonNumbersOn())
        hdcb.Unchecked.Add(fun _ -> turnHideDungeonNumbersOff())
        checkboxSP.Children.Add(hdcb) |> ignore

        hsPanel.Children.Add(checkboxSP) |> ignore
        hsPanel.Children.Add(border) |> ignore
        stackPanel.Children.Add(hsPanel) |> ignore

        stackPanel.Children.Add(new DockPanel(Height=10.)) |> ignore

        let ctxt = System.Threading.SynchronizationContext.Current
        let doStartup(n, loadData : DungeonSaveAndLoad.AllData option) = async {
            // loadData takes precedence over user selections
            let heartShuffle = loadData.IsSome || (hscb.IsChecked.HasValue && hscb.IsChecked.Value)
            let kind = 
                if loadData.IsSome then
                    if loadData.Value.Items.HiddenDungeonNumbers then 
                        TrackerModel.DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS 
                    else 
                        TrackerModel.DungeonTrackerInstanceKind.DEFAULT
                else
                    if hdcb.IsChecked.HasValue && hdcb.IsChecked.Value then
                        TrackerModel.DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS
                    else
                        TrackerModel.DungeonTrackerInstanceKind.DEFAULT
            if loadData.IsSome then
                TrackerModelOptions.IsSecondQuestDungeons.Value <- loadData.Value.Items.SecondQuestDungeons

            let mutable speechRecognitionInstance = null
            if TrackerModelOptions.ListenForSpeech.Value then
                printfn "Initializing microphone for speech recognition..."
                try
                    speechRecognitionInstance <- new SpeechRecognition.SpeechRecognitionInstance(kind)
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

            let loadingText = new System.Text.StringBuilder("Loading UI...")
            let tb = new TextBox(Text=loadingText.ToString(), IsReadOnly=true, Margin=spacing, Padding=Thickness(5.), MaxWidth=WIDTH/2.)
            stackPanel.Children.Add(tb) |> ignore
            let totalsw = System.Diagnostics.Stopwatch.StartNew()
            let sw = System.Diagnostics.Stopwatch.StartNew()
            let displayStartupTimeDiagnostics(s) = if false then printfn "%s" s  // for debugging startup perf
            let showProgress(label) = 
                async {
                    loadingText.Append('.') |> ignore
                    tb.Text <- loadingText.ToString()
                    do! Async.Sleep(1) // pump to make 'Loading UI' text update
                    do! Async.SwitchToContext ctxt
                    displayStartupTimeDiagnostics(sprintf "prev took %dms" sw.ElapsedMilliseconds)
                    displayStartupTimeDiagnostics(label)
                    sw.Restart()
                }
            // move mainDock to topmost while app is built behind it
            Canvas.SetZIndex(mainDock, 9999)
            do! showProgress("start")
            match loadData with
            | Some data -> lastUpdateMinute <- (data.TimeInSeconds / 60)
            | _ -> ()
            let! u = WPFUI.makeAll(this, cm, drawingCanvas, n, heartShuffle, kind, loadData, showProgress, speechRecognitionInstance)
            updateTimeline <- u
            displayStartupTimeDiagnostics(sprintf "total startup took %dms" totalsw.ElapsedMilliseconds)
            appMainCanvas.Children.Remove(mainDock)  // remove for good
            HotKeys.InitializeWindow(this, OverworldItemGridUI.notesTextBox)
            WPFUI.resetTimerEvent.Publish.Add(fun _ -> lastUpdateMinute <- 0; updateTimeline(0); this.SetStartTimeToNow())
            if loadData.IsNone then
                WPFUI.resetTimerEvent.Trigger()  // takes a few seconds to load everything, reset timer at start
            Graphics.canvasAdd(hmsTimerCanvas, OverworldItemGridUI.hmsTimeTextBox, Layout.RIGHT_COL+160., 0.)
            gotThruStartup <- true
            if promptedCrashRecovery then
                finishCrashInfoImpl("prompted for crash recovery, user chose not to, successfully started")
            }

        this.Closed.Add(fun _ ->  // still does not handle 'rude' shutdown, like if they close the console window
            if settingsWereSuccessfullyRead then      // don't overwrite an unreadable file, the user may have been intentionally hand-editing it and needs feedback
                TrackerModelOptions.writeSettings()  // save any settings changes they made before closing the startup window
            )
        let startButtonBehavior(n) = 
            if startButtonHasBeenClicked then () else
            startButtonHasBeenClicked <- true
            turnHeartShuffleOn()  // To draw the display, I have been interacting with the global ChoiceDomain for items.  This switches all the boxes back to empty, 'zeroing out' what we did.
            async {
                TrackerModelOptions.writeSettings()

                if false then   // this feature is currently unused
                    Gamepad.ControllerFailureEvent.Publish.Add(handle)
                    OptionsMenu.gamepadFailedToInitialize <- not(Gamepad.Initialize())

                let mutable loadData = None
                if n = 999 then
                    let ofd = new Microsoft.Win32.OpenFileDialog()
                    ofd.InitialDirectory <- System.AppDomain.CurrentDomain.BaseDirectory
                    ofd.Filter <- "ZTracker saves|zt-save-*.json"
                    let r = ofd.ShowDialog(this)
                    if r.HasValue && r.Value then
                        try
                            let json = System.IO.File.ReadAllText(ofd.FileName)
                            let ver = System.Text.Json.JsonSerializer.Deserialize<DungeonSaveAndLoad.JustVersion>(json, new System.Text.Json.JsonSerializerOptions(AllowTrailingCommas=true))
                            if ver.Version <> OverworldData.VersionString then
                                let msg = sprintf "You are running Z-Tracker version '%s' but the\nsave file was created using version '%s'.\nLoading this file might not work, but Z-Tracker will attempt to load it anyway." 
                                                    OverworldData.VersionString ver.Version
                                let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Error, msg, ["Attempt Load"])
                                ignore r
                            loadData <- Some(DungeonSaveAndLoad.LoadAll(json))
                        with e ->
                            let msg = sprintf "Loading the save file\n%s\nfailed with error:\n%s"  ofd.FileName e.Message
                            let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Error, msg, ["Exit"])
                            ignore r
                    else
                        let msg = sprintf "Failed to load a save file."
                        let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Error, msg, ["Exit"])
                        ignore r
                do! doStartup(n,loadData)
            } |> Async.StartImmediate
        let quests = [|
            0, "First Quest Overworld"
            1, "Second Quest Overworld"
            2, "Mixed - First Quest Overworld"
            3, "Mixed - Second Quest Overworld\n(or randomized quest)"
            999, "from a previously saved state"
            |]
        for n,q in quests do
            let startButton = Graphics.makeButton(sprintf "Start: %s" q, None, None)
            if n=999 then
                let tb = new TextBox(Text="- OR -",IsReadOnly=true, Margin=smallSpacing, TextAlignment=TextAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.))
                stackPanel.Children.Add(tb) |> ignore
            if n=0 then
                startButton.Margin <-Thickness(0.)
            elif n=999 then
                startButton.Margin <- smallSpacing
            else
                startButton.Margin <- spacing
            if n=0 then
                let dp = new DockPanel(LastChildFill=true, Width=WIDTH/2.)
                let otherButton = Graphics.makeButton(". . .", None, None)
                let tb = new TextBox(Text="See more\noptions",IsReadOnly=true, TextAlignment=TextAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(2.), Opacity=0.)
                appMainCanvas.Children.Add(tb) |> ignore
                otherButton.MouseEnter.Add(fun _ ->
                    let pos = otherButton.TransformToAncestor(appMainCanvas).Transform(Point(otherButton.ActualWidth+3., 0.))
                    Canvas.SetLeft(tb, pos.X)
                    Canvas.SetTop(tb, pos.Y)
                    tb.Opacity <- 1.0
                    )
                otherButton.MouseLeave.Add(fun _ -> tb.Opacity <- 0.0)
                otherButton.Margin <-Thickness(6.,0.,0.,0.)
                dp.Children.Add(otherButton) |> ignore
                otherButton.Click.Add(fun _ -> 
                    let dialog1 = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
                    addDarkTheme(dialog1.Resources)
                    let tb1 = new TextBox(IsReadOnly=true, BorderThickness=Thickness(0.), Foreground=Brushes.Orange)
                    tb1.Text <- "Z-Tracker was designed for use with fcoughlin's Zelda 1 Randomizer.\n\n" +
                                
                                "There are other randomizers/ROM-Hacks for Zelda 1 which use other\n" + 
                                "(sometimes randomized) overworld maps.  Z-Tracker may not work with\n" +
                                "these perfectly, but the options here are designed to allow you to use\n" +
                                "Z-Tracker with non-standard overworld maps, for a slightly degraded, but\n" +
                                "workable tracking experience.\n\n" + 

                                "There are two main options:\n" +
                                " - use a 'blank' 16x8 grid for overworld map tracking\n" +
                                " - use a custom .png map file, supplied by your other randomizer/ROM-Hack\n\n" +

                                "If you do supply a custom map file, you can also choose whether you want the map\n" +
                                "to be fully revealed/visible at the outset, or whether each of the 16x8 tiles\n" +
                                "should be individually hidden, until you click each tile to reveal it.\n\n" +

                                "Choose an option below:"
                    dialog1.Children.Add(tb1) |> ignore
                    let wh = new System.Threading.ManualResetEvent(false)
                    let mutable choice = None
                    for txt,n in [
                                    "I just want a blank 16x8 grid", 0
                                    "I want to select a map file on disk, and have it fully revealed at the start", 1
                                    "I want to select a map file on disk, but have each tile hidden at the start", 2
                                 ] do
                        let button = Graphics.makeButton(txt, None, None)
                        button.Margin <- Thickness(0., 10., 0., 0.)
                        dialog1.Children.Add(button) |> ignore
                        button.Click.Add(fun _ -> if choice.IsNone then choice <- Some(n); wh.Set() |> ignore)
                    async {
                        do! CustomComboBoxes.DoModal(cm, wh, 30., 30., new Border(Child=dialog1, BorderThickness=Thickness(3.), BorderBrush=Brushes.Orange, Background=Brushes.Black, Padding=Thickness(5.)))
                        Graphics.alternativeOverworldMapFilename <- ""
                        match choice with
                        | None -> ()  // just exit the modal and go back to startup screen if they click outside
                        | Some choice ->
                            if choice<>0 then
                                let ofd = new Microsoft.Win32.OpenFileDialog()
                                ofd.InitialDirectory <- System.AppDomain.CurrentDomain.BaseDirectory
                                ofd.Filter <- "Overworld map images|*.png"
                                let r = ofd.ShowDialog(this)
                                if r.HasValue && r.Value then
                                    Graphics.alternativeOverworldMapFilename <- ofd.FileName
                            Graphics.shouldInitiallyHideOverworldMap <- (choice=0 || choice=2)
                            let text = (if choice=0 then "You have chosen a blank map grid.\n\n" else "You have chosen to load a map file.\n\n") +

                                        "Some randomizers have behavior that Z-Tracker does not natively support.  For example, " +
                                        "in z1m1 you might be able to purchase a Ladder in an overworld shop.  There is no native " +
                                        "Z-Tracker support for marking an overworld tile as a Ladder shop.  But you can add some " +
                                        "abitrary markup to the app in a few ways:\n" +
                                        " - click the 'Draw' button in the bottom left, to place arbitrary icons\n" +
                                        "      (e.g. you might put '$' and Ladder icons on an overworld tile)\n" +
                                        " - shift-left-click an overworld tile, to circle and label it\n" +
                                        "      (e.g. you might mark a tile with a cyan circle and an 'L')\n" +
                                        " - type text into the 'Notes' text box\n"+
                                        "      (e.g. you might type 'Ladder for sale at tile B-4')\n\n" +

                                        "Do whatever works for you.  Good luck!"
                            let! _r = CustomComboBoxes.DoModalMessageBoxCore(cm, System.Drawing.SystemIcons.Information, text, ["Ok"], 30., 30.)
                            startButtonBehavior(4)
                    } |> Async.StartImmediate
                    )
                DockPanel.SetDock(otherButton, Dock.Right)
                dp.Children.Add(startButton) |> ignore
                stackPanel.Children.Add(dp) |> ignore
            else
                startButton.Width <- WIDTH/2.
                stackPanel.Children.Add(startButton) |> ignore
            startButton.Click.Add(fun _ -> startButtonBehavior(n))

        let tipsp = new StackPanel(Orientation=Orientation.Vertical)
        let tb = MakeTipTextBox("Random tip:")
        tipsp.Children.Add(tb) |> ignore
        let tb = MakeTipTextBox(DungeonData.Factoids.allTips.[ System.Random().Next(DungeonData.Factoids.allTips.Length) ])   // correct app behavior
        //let tb = MakeTipTextBox(DungeonData.Factoids.allTips |> Array.sortBy (fun s -> s.Length) |> Seq.last)   // show the longest tip (to test screen layout)
        tb.Margin <- spacing
        tipsp.Children.Add(tb) |> ignore
        stackPanel.Children.Add(new Border(Child=tipsp, BorderThickness=Thickness(1.), Margin=Thickness(0., 20., 0., 0.), Padding=Thickness(5.), BorderBrush=Brushes.Orange, Width=WIDTH*2./3.)) |> ignore

        let bottomSP = new StackPanel(Orientation=Orientation.Vertical, HorizontalAlignment=HorizontalAlignment.Center)
        bottomSP.Children.Add(new Shapes.Rectangle(HorizontalAlignment=HorizontalAlignment.Stretch, Fill=Brushes.Black, Height=2., Margin=spacing)) |> ignore
        let tb = new TextBox(Text="Settings (most can be changed later, using 'Options...' button above timeline):", HorizontalAlignment=HorizontalAlignment.Center, 
                                Margin=Thickness(0.,0.,0.,5.), BorderThickness=Thickness(0.))
        bottomSP.Children.Add(tb) |> ignore
        let options = OptionsMenu.makeOptionsCanvas(cm, false, true)
        bottomSP.Children.Add(options) |> ignore
        mainDock.Children.Add(bottomSP) |> ignore
        DockPanel.SetDock(bottomSP, Dock.Bottom)

        mainDock.Children.Add(mainStackPanel) |> ignore

        // "dark theme"
        mainDock.Background <- Brushes.Black
        addDarkTheme(mainDock.Resources)

        if System.IO.File.Exists(crashLogFilename) then
            let lines = System.IO.File.ReadAllLines(crashLogFilename)
            if lines.Length > 1 then
                match DateTime.TryParseExact(lines.[lines.Length-1], dateTimeFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None) with
                | false, _ -> ()
                | true, crashTime ->
                    if DateTime.Now - crashTime < TimeSpan.FromMinutes(10.) then
                        // it crashed in the past 10 minutes
                        if System.IO.File.Exists(SaveAndLoad.AutoSaveFilename) then
                            let autoSaveTime = System.IO.File.GetLastWriteTime(SaveAndLoad.AutoSaveFilename)
                            if autoSaveTime > (crashTime - TimeSpan.FromMinutes(2.)) then
                                async {
                                // there was an autosave just before the crash
                                let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Error, 
                                                                            "It appears that Z-Tracker recently crashed.\n\n"+
                                                                                "There is a recent auto-save from a\n"+
                                                                                "minute or so before the crash.\n\n"+
                                                                                "Would you like to try loading the auto-save?", ["Yes, load auto-save"; "No"])
                                if r <> "No" then
                                    let json = System.IO.File.ReadAllText(SaveAndLoad.AutoSaveFilename)
                                    let loadData = Some(DungeonSaveAndLoad.LoadAll(json))
                                    do! doStartup(999, loadData)
                                    // successful reload of autosave, call this so next startup won't also trigger recovery dialog
                                    finishCrashInfoImpl("successful reload of autosave")
                                else
                                    promptedCrashRecovery <- true
                                } |> Async.StartImmediate
        
    override this.Update(f10Press) =
        base.Update(f10Press)
        // update time
        let ts = DateTime.Now - this.StartTime.Time
        let h,m,s = ts.Hours, ts.Minutes, ts.Seconds
        let time = sprintf "%2d:%02d:%02d" h m s
        OverworldItemGridUI.hmsTimeTextBox.Text <- time
        OverworldItemGridUI.broadcastTimeTextBox.Text <- time
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
        let ts = DateTime.Now - this.StartTime.Time
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
        let mutable ts = DateTime.Now - this.StartTime.Time
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
        this.WindowState <- WindowState.Minimized
        this.Loaded.Add(fun _ ->
            this.Visibility <- Visibility.Hidden
            
(*
            // method 1
            let resHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height  // e.g. 1440
            let actualHeight = SystemParameters.PrimaryScreenHeight  // e.g. 960
            let scale = float resHeight / actualHeight  // e.g.   1.5 
            let waHeight = SystemParameters.WorkArea.Height
            printfn "method1: scale = %f    actualHeight=%f    waHeight=%f" scale actualHeight waHeight
            // TODO if waHeight < 1000.0 then suggest Smaller
//caffy
//method1: scale = 1.250000    actualHeight=864.000000    waHeight=824.000000
//method2: scale = 1.250000
            // method 2
            let dpiScale = VisualTreeHelper.GetDpi(this)
            let scale = dpiScale.DpiScaleY
            printfn "method2: scale = %f" scale
*)

            let mainW = new MyWindow()
            mainW.Owner <- this
            mainW.Show()
            let handle = Winterop.GetConsoleWindow()
            Winterop.ShowWindow(handle, Winterop.SW_MINIMIZE) |> ignore
            mainW.Closed.Add(fun _ -> this.Close())
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
    
    
//    printfn "press Enter to end"
//    System.Console.ReadLine() |> ignore
    0
