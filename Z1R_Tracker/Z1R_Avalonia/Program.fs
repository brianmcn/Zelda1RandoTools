open System
open Avalonia
open Avalonia.Controls
open Avalonia.Media
open Avalonia.Layout

type MyCommand(f) =
    let ev = new Event<EventArgs>()
    interface System.Windows.Input.ICommand with
        member this.CanExecute(x) = true
        member this.Execute(x) = f(x)
        //[<CLIEventAttribute>]
        //member this.CanExecuteChanged = ev.Publish
        member this.add_CanExecuteChanged(h) = ev.Publish.AddHandler(fun o ea -> h.Invoke(o,ea))
        member this.remove_CanExecuteChanged(h) = ev.Publish.RemoveHandler(fun o ea -> h.Invoke(o,ea))

type MyWindow(owMapNum) as this = 
    inherit Window()
    let mutable startTime = DateTime.Now
    let mutable canvas, updateTimeline = null, fun _ -> ()
    let mutable lastUpdateMinute = 0
    let hmsTimeTextBox = new TextBox(Text="timer",FontSize=32.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false)
    //                 items  ow map  prog  timeline     dungeon tabs                
    let HEIGHT = float(30*4 + 11*3*9 + 30 + UI.TCH + 6 + UI.TH + UI.TH + 27*8 + 12*7 + 30)
    let WIDTH = 16.*OverworldRouteDrawing.OMTW
    do
        printfn "W,H = %d,%d" (int WIDTH) (int HEIGHT)
        UI.timeTextBox <- hmsTimeTextBox
        this.Width <- WIDTH + 30. // TODO fudging it
        this.Height <- HEIGHT
        // TODO ideally this should be a free font included in the assembly
        this.FontFamily <- FontFamily("Segoe UI")
        let timer = new Avalonia.Threading.DispatcherTimer()
        timer.Interval <- TimeSpan.FromSeconds(1.0)
        timer.Tick.Add(fun _ -> this.Update(false))
        timer.Start()
        this.KeyBindings.Add(new Avalonia.Input.KeyBinding(Gesture=Avalonia.Input.KeyGesture.Parse("F5"), Command=new MyCommand(fun x -> 
                printfn "F5 was pressed"
                TrackerModel.startIconX <- UI.currentlyMousedOWX
                TrackerModel.startIconY <- UI.currentlyMousedOWY
                TrackerModel.forceUpdate()
                )))
        this.KeyBindings.Add(new Avalonia.Input.KeyBinding(Gesture=Avalonia.Input.KeyGesture.Parse("F10"), Command=new MyCommand(fun x -> 
                printfn "F10 was pressed"
                startTime <- DateTime.Now
                this.Update(true)
                )))

        let dock(x) =
            let d = new DockPanel(LastChildFill=false, HorizontalAlignment=HorizontalAlignment.Center)
            d.Children.Add(x) |> ignore
            d
        let spacing = Thickness(0., 10., 0., 0.)

        // full window
        this.Title <- "Z-Tracker for Zelda 1 Randomizer"
        let stackPanel = new StackPanel(Orientation=Orientation.Vertical)

        let box(n) = 
            let pict,rect,_ = CustomComboBoxes.makeItemBoxPicture(n, ref false, false)
            rect.Stroke <- CustomComboBoxes.no
            pict
        let hsPanel = new StackPanel(Margin=spacing, MaxWidth=WIDTH/2., Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
        let hsGrid = Graphics.makeGrid(3, 3, 30, 30)
        hsGrid.Background <- Brushes.Black
        for i = 0 to 2 do
            let image = Graphics.BMPtoImage Graphics.emptyFoundTriforce_bmps.[i]
            Graphics.gridAdd(hsGrid, image, i, 0)
        let row1boxesA = ResizeArray()
        let row1boxesB = ResizeArray()
        for i = 0 to 2 do
            let pict = box(14)
            row1boxesA.Add(pict)
            Graphics.gridAdd(hsGrid, pict, i, 1)
            let pict = box(-1)
            row1boxesB.Add(pict)
            Graphics.gridAdd(hsGrid, pict, i, 1)
            let pict = box(-1)
            Graphics.gridAdd(hsGrid, pict, i, 2)
        let yes() =
            for b in row1boxesA do
                b.Opacity <- 0.
            for b in row1boxesB do
                b.Opacity <- 1.
        let no() =
            for b in row1boxesA do
                b.Opacity <- 1.
            for b in row1boxesB do
                b.Opacity <- 0.
        yes()
        let cutoffCanvas = new Canvas(Width=80., Height=80., ClipToBounds=true)
        cutoffCanvas.Children.Add(hsGrid) |> ignore
        let border = new Border(BorderBrush=Brushes.DarkGray, BorderThickness=Thickness(8.,8.,0.,0.), Child=cutoffCanvas)
        let hscb = new CheckBox(Content=new TextBox(Text="Heart Shuffle",IsReadOnly=true), VerticalAlignment=VerticalAlignment.Center, Margin=Thickness(10.))
        hscb.IsChecked <- System.Nullable.op_Implicit true
        hscb.Checked.Add(fun _ -> yes())
        hscb.Unchecked.Add(fun _ -> no())
        hsPanel.Children.Add(hscb) |> ignore
        hsPanel.Children.Add(border) |> ignore
        stackPanel.Children.Add(hsPanel) |> ignore


        let tb = dock <| new TextBox(Text="Settings (most can be changed later, using 'Options...' button above timeline):", Margin=spacing)
        stackPanel.Children.Add(tb) |> ignore
        TrackerModel.Options.readSettings()
        let options = OptionsMenu.makeOptionsCanvas(16.*OverworldRouteDrawing.OMTW, float(UI.TCH+6), 0.)
        options.IsHitTestVisible <- true
        options.Opacity <- 1.0
        stackPanel.Children.Add(options) |> ignore

        let tb = dock <| new TextBox(Text="\nNote: once you start, you can use F5 to\nplace the 'start spot' icon at your mouse,\nor F10 to reset the timer to 0, at any time\n",IsReadOnly=true, Margin=spacing, MaxWidth=300.)
        stackPanel.Children.Add(tb) |> ignore
        let Quests = [|
                "First Quest"
                "Second Quest"
                "Mixed - First Quest"
                "Mixed - Second Quest"
            |]
        for i = 0 to 3 do
            let startButton = new Button(Content=new TextBox(Text=Quests.[i],IsReadOnly=true,IsHitTestVisible=false), MaxWidth=300.)
            stackPanel.Children.Add(startButton) |> ignore
            startButton.Click.Add(fun _ ->
                    let tb = new TextBox(Text="\nLoading UI...\n", IsReadOnly=true, MaxWidth=300.)
                    stackPanel.Children.Add(tb) |> ignore
                    let ctxt = System.Threading.SynchronizationContext.Current
                    Async.Start (async {
                        do! Async.Sleep(10) // get off UI thread so UI will update
                        do! Async.SwitchToContext ctxt
                        TrackerModel.Options.writeSettings()
                        printfn "you pressed start after selecting %d" i
                        this.Background <- Brushes.Black
                        if hscb.IsChecked.HasValue && hscb.IsChecked.Value then
                            ()
                        else
                            for i = 0 to 7 do
                                TrackerModel.dungeons.[i].Boxes.[0].Set(14,TrackerModel.PlayerHas.NO)
                        let c,u = UI.makeAll(i)
                        canvas <- c
                        updateTimeline <- u
                        UI.canvasAdd(canvas, hmsTimeTextBox, UI.RIGHT_COL+80., 0.)
                        this.Content <- canvas
                    })
                )
        let hstackPanel = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
        hstackPanel.Children.Add(stackPanel) |> ignore
        this.Content <- hstackPanel
    member this.Update(f10Press) =
        // update time
        let ts = DateTime.Now - startTime
        let h,m,s = ts.Hours, ts.Minutes, ts.Seconds
        hmsTimeTextBox.Text <- sprintf "%2d:%02d:%02d" h m s
        // update timeline
        //if f10Press || int ts.TotalSeconds%5 = 0 then
        //    updateTimeline(int ts.TotalSeconds/5)
        let curMinute = int ts.TotalMinutes
        if f10Press || curMinute > lastUpdateMinute then
            lastUpdateMinute <- curMinute
            updateTimeline(curMinute)

type App() =
    inherit Avalonia.Application()
    override this.Initialize() =
        () //Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this)
        this.Styles.AddRange [ 
            new Avalonia.Markup.Xaml.Styling.StyleInclude(baseUri=null, Source = Uri("resm:Avalonia.Themes.Default.DefaultTheme.xaml?assembly=Avalonia.Themes.Default"))
            new Avalonia.Markup.Xaml.Styling.StyleInclude(baseUri=null, Source = Uri("resm:Avalonia.Themes.Default.Accents.BaseDark.xaml?assembly=Avalonia.Themes.Default"))
        ]
    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime as desktop ->
            desktop.MainWindow <- new MyWindow(0)
        | _ -> ()
        base.OnFrameworkInitializationCompleted()

[<EntryPoint>]
let main argv =
    // Avalonia configuration, don't remove; also used by visual designer.
    let builder = Avalonia.AppBuilder.Configure<App>()
    let builder = Avalonia.AppBuilderDesktopExtensions.UsePlatformDetect(builder)
    let builder = Avalonia.LoggingExtensions.LogToTrace(builder, Avalonia.Logging.LogEventLevel.Warning, [||])
    Avalonia.ClassicDesktopStyleApplicationLifetimeExtensions.StartWithClassicDesktopLifetime(builder, argv)
