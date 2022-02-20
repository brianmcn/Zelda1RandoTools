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
    let VK_F5 = 0x74
    let VK_F10 = 0x79
    let MOD_NONE = 0u
    let startTime = new TrackerModel.LastChangedTime()
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
    let hmsTimeTextBox = new TextBox(Text="timer",FontSize=42.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false)
    //                             items  ow map  prog  dungeon tabs                                    timeline   
    let HEIGHT_SANS_CHROME = float(30*5 + 11*3*9 + 30 + OverworldItemGridUI.TH + 30 + 27*8 + 12*7 + 3 + OverworldItemGridUI.TCH + 6)
    let WIDTH_SANS_CHROME = float(16*16*3)  // ow map width
    let CHROME_WIDTH, CHROME_HEIGHT = 16., 40.  // Windows app border
    let HEIGHT = HEIGHT_SANS_CHROME + CHROME_HEIGHT
    let WIDTH = WIDTH_SANS_CHROME + CHROME_WIDTH
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
            | :? HotKeys.UserError as hue ->
                logCrashInfo ""
                logCrashInfo "Error parsing HotKeys.txt:"
                logCrashInfo ""
                logCrashInfo <| sprintf "%s" hue.Message
                logCrashInfo ""
                logCrashInfo "You should fix this error by editing the text file."
                logCrashInfo "Or you can delete it, and an empty hotkeys template file will be created in its place."
                logCrashInfo ""
                System.Threading.Thread.Sleep(2000)
                let fileToSelect = HotKeys.HotKeyFilename
                let args = sprintf "/Select, \"%s\"" fileToSelect
                let psi = new System.Diagnostics.ProcessStartInfo("Explorer.exe", args)
                System.Diagnostics.Process.Start(psi) |> ignore
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

        HotKeys.InitializeWindow(this)
        HotKeys.PopulateHotKeyTables()
        let mutable settingsWereSuccessfullyRead = false
        TrackerModel.Options.readSettings()
        settingsWereSuccessfullyRead <- true
        WPFUI.voice.Volume <- TrackerModel.Options.Volume
        
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
        WPFUI.timeTextBox <- hmsTimeTextBox
        // full window
        this.Title <- "Z-Tracker for Zelda 1 Randomizer"
        this.ResizeMode <- ResizeMode.NoResize
        this.SizeToContent <- SizeToContent.Manual
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        let APP_WIDTH, APP_HEIGHT = 
            if TrackerModel.Options.SmallerAppWindow.Value then 
                round(WIDTH_SANS_CHROME*0.666666 + CHROME_WIDTH), round(HEIGHT_SANS_CHROME*0.666666 + CHROME_HEIGHT)
            else 
                WIDTH, HEIGHT
        //printfn "%f, %f" APP_WIDTH APP_HEIGHT
        let leftTop = TrackerModel.Options.MainWindowLT
        let matches = System.Text.RegularExpressions.Regex.Match(leftTop, """^(-?\d+),(-?\d+)$""")
        if matches.Success then
            this.Left <- float matches.Groups.[1].Value
            this.Top <- float matches.Groups.[2].Value
        this.LocationChanged.Add(fun _ ->
            TrackerModel.Options.MainWindowLT <- sprintf "%d,%d" (int this.Left) (int this.Top)
            TrackerModel.Options.writeSettings()
            )
        this.Width <- APP_WIDTH
        this.Height <- APP_HEIGHT
        this.FontSize <- 18.
        this.Loaded.Add(fun _ -> this.Focus() |> ignore)

        let appMainCanvas, cm =  // a scope, so code below is less likely to touch rootCanvas
            //                             items  ow map  prog  dungeon tabs                                    timeline
            let APP_CONTENT_HEIGHT = float(30*5 + 11*3*9 + 30 + OverworldItemGridUI.TH + 30 + 27*8 + 12*7 + 3 + OverworldItemGridUI.TCH + 6)
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
        if TrackerModel.Options.SmallerAppWindow.Value then 
            let trans = new ScaleTransform(0.666666, 0.666666)
            cm.RootCanvas.RenderTransform <- trans
        this.Content <- cm.RootCanvas
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

        let stackPanel = new StackPanel(Orientation=Orientation.Vertical)
        let spacing = Thickness(0., 8., 0., 0.)

        do        
            let menu(wh:Threading.ManualResetEvent) = 
                let mkTxt(txt) = new TextBox(Text=txt,IsReadOnly=true, Margin=spacing, //TextAlignment=TextAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, 
                                                BorderThickness=Thickness(0.), FontSize=16.)
                let sp = new StackPanel(Orientation=Orientation.Vertical, Width=appMainCanvas.Width-100., Margin=Thickness(20.))
                let title = new TextBox(Text="Window Size",IsReadOnly=true, TextAlignment=TextAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, 
                                            BorderThickness=Thickness(0.,0.,0.,3.), FontSize=20.)
                sp.Children.Add(title) |> ignore
                sp.Children.Add(mkTxt("Z-Tracker can run in either of two window sizes: 'Default' (larger) or '2/3 size' (smaller).")) |> ignore
                sp.Children.Add(mkTxt("Changes to this setting will only take effect the next time you start the application.")) |> ignore
                sp.Children.Add(mkTxt(sprintf "The current Z-Tracker window in '%s' mode.  You can change the setting here:" (if TrackerModel.Options.SmallerAppWindow.Value then "2/3 size" else "Default"))) |> ignore
                sp.Children.Add(new DockPanel(Height=20.)) |> ignore
                let rb3 = new RadioButton(Content=new TextBox(Text="Default" , IsReadOnly=true, BorderThickness=Thickness(0.), FontSize=16.), Margin=Thickness(20.,0.,0.,0.))
                let rb2 = new RadioButton(Content=new TextBox(Text="2/3 size", IsReadOnly=true, BorderThickness=Thickness(0.), FontSize=16.), Margin=Thickness(20.,0.,0.,0.))
                if TrackerModel.Options.SmallerAppWindow.Value then
                    rb2.IsChecked <- System.Nullable.op_Implicit true
                else
                    rb3.IsChecked <- System.Nullable.op_Implicit true
                let mutable desireSmaller = TrackerModel.Options.SmallerAppWindow.Value
                rb3.Checked.Add(fun _ -> desireSmaller <- false)
                rb2.Checked.Add(fun _ -> desireSmaller <- true)
                let inner = new StackPanel(Orientation=Orientation.Vertical, HorizontalAlignment=HorizontalAlignment.Center)
                inner.Children.Add(rb3) |> ignore
                inner.Children.Add(rb2) |> ignore
                sp.Children.Add(inner) |> ignore
                let buttons = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center, Margin=spacing)
                let sb = Graphics.makeButton("Save changes and close Z-Tracker\n(changes take effect next time)", Some(16.), None)
                let cb = Graphics.makeButton("Don't make any changes\n(exit this popup menu)", Some(16.), None)
                cb.Click.Add(fun _ -> wh.Set() |> ignore)
                sb.Click.Add(fun _ ->
                    TrackerModel.Options.SmallerAppWindow.Value <- desireSmaller
                    TrackerModel.Options.writeSettings()
                    this.Close()
                    )
                buttons.Children.Add(sb) |> ignore
                buttons.Children.Add(new DockPanel(Width=30.)) |> ignore  // spacing
                buttons.Children.Add(cb) |> ignore
                sp.Children.Add(buttons) |> ignore
                addDarkTheme(sp.Resources)
                new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Background=Brushes.Black, Child=sp)

            let fs = if TrackerModel.Options.SmallerAppWindow.Value then 16. else 12.
            let topBar = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
            let tb = new TextBox(Text=sprintf "Z-Tracker window too %s? " (if TrackerModel.Options.SmallerAppWindow.Value then "small" else "large"), 
                                    IsReadOnly=true, BorderThickness=Thickness(0.), FontSize=fs, VerticalAlignment=VerticalAlignment.Center)
            topBar.Children.Add(tb) |> ignore
            let b = Graphics.makeButton("Click here for options", Some(fs), None)
            let mutable popupIsActive = false
            b.Click.Add(fun _ -> 
                if not popupIsActive then
                    popupIsActive <- true
                    let wh = new Threading.ManualResetEvent(false)
                    CustomComboBoxes.DoModal(cm, wh, 50., 100., menu(wh)) |> Async.StartImmediate
                    popupIsActive <- false
                )
            topBar.Children.Add(b) |> ignore
            let workingAreaTooSmallForDefaultHeight = SystemParameters.WorkArea.Height < 1000.0
            let likelyDoesntFit = workingAreaTooSmallForDefaultHeight && not(TrackerModel.Options.SmallerAppWindow.Value)
            let barColor = 
                if likelyDoesntFit then 
                    new SolidColorBrush(Color.FromRgb(120uy, 30uy, 30uy))   // reddish
                else 
                    new SolidColorBrush(Color.FromRgb(50uy, 50uy, 50uy))    // grayish
            let dp = new DockPanel(Height=(if TrackerModel.Options.SmallerAppWindow.Value then 40. else 30.), LastChildFill=true, Background=barColor)
            dp.Children.Add(topBar) |> ignore
            stackPanel.Children.Add(dp) |> ignore
            let spacer = new DockPanel(Height=30., LastChildFill=false)
            let vb = Graphics.dock(CustomComboBoxes.makeVersionButtonWithBehavior(cm), Dock.Top)
            DockPanel.SetDock(vb, Dock.Right)
            spacer.Children.Add(vb) |> ignore
            stackPanel.Children.Add(spacer) |> ignore

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

        stackPanel.Children.Add(new DockPanel(Height=20.)) |> ignore

        let mutable startButtonHasBeenClicked = false
        this.Closed.Add(fun _ ->  // still does not handle 'rude' shutdown, like if they close the console window
            if settingsWereSuccessfullyRead then      // don't overwrite an unreadable file, the user may have been intentionally hand-editing it and needs feedback
                TrackerModel.Options.writeSettings()  // save any settings changes they made before closing the startup window
            )
        let quests = [|
            0, "First Quest Overworld"
            1, "Second Quest Overworld"
            2, "Mixed - First Quest Overworld"
            3, "Mixed - Second Quest Overworld\n(or randomized quest)"
            999, "from a previously saved state"
            |]
        for n,q in quests do
            let startButton = Graphics.makeButton(sprintf "Start: %s" q, None, None)
            startButton.Margin <- spacing
            startButton.Width <- WIDTH/2.
            if n=999 then
                let tb = new TextBox(Text="- OR -",IsReadOnly=true, Margin=spacing, TextAlignment=TextAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.))
                stackPanel.Children.Add(tb) |> ignore
            stackPanel.Children.Add(startButton) |> ignore
            startButton.Click.Add(fun _ -> 
                if startButtonHasBeenClicked then () else
                startButtonHasBeenClicked <- true
                turnHeartShuffleOn()  // To draw the display, I have been interacting with the global ChoiceDomain for items.  This switches all the boxes back to empty, 'zeroing out' what we did.
                let ctxt = System.Threading.SynchronizationContext.Current
                async {
                    TrackerModel.Options.writeSettings()

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
                                loadData <- Some(DungeonSaveAndLoad.LoadAll(ofd.FileName))
                                if loadData.Value.Version <> OverworldData.VersionString then
                                    let msg = sprintf "You are running Z-Tracker version '%s' but the\nsave file was created using version '%s'.\nLoading this file is not supported." 
                                                        OverworldData.VersionString loadData.Value.Version
                                    let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Error, msg, ["Exit"])
                                    ignore r
                                    loadData <- None
                            with e ->
                                let msg = sprintf "Loading the save file\n%s\nfailed with error:\n%s"  ofd.FileName e.Message
                                let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Error, msg, ["Exit"])
                                ignore r
                        else
                            let msg = sprintf "Failed to load a save file."
                            let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Error, msg, ["Exit"])
                            ignore r

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
                        TrackerModel.Options.IsSecondQuestDungeons.Value <- loadData.Value.Items.SecondQuestDungeons

                    let mutable speechRecognitionInstance = null
                    if TrackerModel.Options.ListenForSpeech.Value then
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

                    let tb = new TextBox(Text="\nLoading UI...\n", IsReadOnly=true, Margin=spacing, MaxWidth=WIDTH/2.)
                    stackPanel.Children.Add(tb) |> ignore
                    let showProgress() = 
                        async {
                            tb.Text <- tb.Text.Replace(".\n", "..\n")
                            // move mainDock to topmost layer again
                            appMainCanvas.Children.Remove(mainDock)
                            appMainCanvas.Children.Add(mainDock) |> ignore
                            do! Async.Sleep(1) // pump to make 'Loading UI' text update
                            do! Async.SwitchToContext ctxt
                        }
                    do! showProgress()
                    let! u = WPFUI.makeAll(this, cm, n, heartShuffle, kind, loadData, showProgress, speechRecognitionInstance)
                    updateTimeline <- u
                    appMainCanvas.Children.Remove(mainDock)  // remove for good
                    WPFUI.resetTimerEvent.Publish.Add(fun _ -> lastUpdateMinute <- 0; updateTimeline(0); this.SetStartTimeToNow())
                    WPFUI.resetTimerEvent.Trigger()  // takes a few seconds to load everything, reset timer at start
                    Graphics.canvasAdd(cm.AppMainCanvas, hmsTimeTextBox, WPFUI.RIGHT_COL+160., 0.)
                } |> Async.StartImmediate
            )

        let tipsp = new StackPanel(Orientation=Orientation.Vertical)
        let tb = MakeTipTextBox("Random tip:")
        tipsp.Children.Add(tb) |> ignore
        let tb = MakeTipTextBox(DungeonData.Factoids.allTips.[ System.Random().Next(DungeonData.Factoids.allTips.Length) ])
        tb.Margin <- spacing
        tipsp.Children.Add(tb) |> ignore
        stackPanel.Children.Add(new Border(Child=tipsp, BorderThickness=Thickness(1.), Margin=Thickness(0., 30., 0., 0.), Padding=Thickness(5.), BorderBrush=Brushes.Orange, Width=WIDTH*2./3.)) |> ignore

        let bottomSP = new StackPanel(Orientation=Orientation.Vertical, HorizontalAlignment=HorizontalAlignment.Center)
        bottomSP.Children.Add(new Shapes.Rectangle(HorizontalAlignment=HorizontalAlignment.Stretch, Fill=Brushes.Black, Height=2., Margin=spacing)) |> ignore
        let tb = new TextBox(Text="Settings (most can be changed later, using 'Options...' button above timeline):", HorizontalAlignment=HorizontalAlignment.Center, 
                                Margin=Thickness(0.,0.,0.,10.), BorderThickness=Thickness(0.))
        bottomSP.Children.Add(tb) |> ignore
        let options = OptionsMenu.makeOptionsCanvas(float(16*16*3), false)
        bottomSP.Children.Add(options) |> ignore
        mainDock.Children.Add(bottomSP) |> ignore
        DockPanel.SetDock(bottomSP, Dock.Bottom)

        mainDock.Children.Add(stackPanel) |> ignore

        // "dark theme"
        mainDock.Background <- Brushes.Black
        addDarkTheme(mainDock.Resources)
        
    override this.Update(f10Press) =
        base.Update(f10Press)
        // update time
        let ts = DateTime.Now - this.StartTime.Time
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
