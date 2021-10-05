open System
open Avalonia
open Avalonia.Controls
open Avalonia.Media
open Avalonia.Layout

type MyCommand(f) =
    let ev = new Event<EventArgs>()
    interface System.Windows.Input.ICommand with
        member this.CanExecute(_x) = true
        member this.Execute(x) = f(x)
        //[<CLIEventAttribute>]
        //member this.CanExecuteChanged = ev.Publish
        member this.add_CanExecuteChanged(h) = ev.Publish.AddHandler(fun o ea -> h.Invoke(o,ea))
        member this.remove_CanExecuteChanged(h) = ev.Publish.RemoveHandler(fun o ea -> h.Invoke(o,ea))

type MyWindow() as this = 
    inherit Window()
    let mutable startTime = DateTime.Now
    let mutable updateTimeline = fun _ -> ()
    let mutable lastUpdateMinute = 0
    let hmsTimeTextBox = new TextBox(Text="timer",FontSize=32.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false)
    //                 items  ow map  prog  timeline     dungeon tabs                
    let HEIGHT = float(30*5 + 11*3*9 + 30 + UI.TCH + 6 + UI.TH + UI.TH + 27*8 + 12*7 + 30)
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
        (*
        this.KeyBindings.Add(new Avalonia.Input.KeyBinding(Gesture=Avalonia.Input.KeyGesture.Parse("F10"), Command=new MyCommand(fun _ -> 
                printfn "F10 was pressed"
                startTime <- DateTime.Now
                this.Update(true)
                )))
        *)

        let dock(x) =
            let d = new DockPanel(LastChildFill=false, HorizontalAlignment=HorizontalAlignment.Center)
            d.Children.Add(x) |> ignore
            d
        let spacing = Thickness(0., 10., 0., 0.)

        // full window
        this.Title <- "Z-Tracker for Zelda 1 Randomizer"
        let stackPanel = new StackPanel(Orientation=Orientation.Vertical)

        let tb = new TextBox(Text="Startup Options:",IsReadOnly=true, Margin=spacing, TextAlignment=TextAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center)
        stackPanel.Children.Add(tb) |> ignore

        let box(n) = 
            let c = new Canvas(Width=30., Height=30., Background=Brushes.Black, IsHitTestVisible=true)
            let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=CustomComboBoxes.no, StrokeThickness=3.0, IsHitTestVisible=false)
            c.Children.Add(rect) |> ignore
            let bmp = CustomComboBoxes.boxCurrentBMP(n, false)
            if bmp <> null then
                let image = Graphics.BMPtoImage(bmp)
                image.IsHitTestVisible <- false
                Graphics.canvasAdd(c, image, 4., 4.)
            c
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
        let row1boxesHearts = ResizeArray()
        let row1BoxesEmpty = ResizeArray()
        for i = 0 to 2 do
            let pict = box(14)
            row1boxesHearts.Add(pict)
            Graphics.gridAdd(hsGrid, pict, i, 1)
            let pict = box(-1)
            row1BoxesEmpty.Add(pict)
            Graphics.gridAdd(hsGrid, pict, i, 1)
            let pict = box(-1)
            Graphics.gridAdd(hsGrid, pict, i, 2)
        let tunrHeartShuffleOn() =
            for b in row1boxesHearts do
                b.Opacity <- 0.
            for b in row1BoxesEmpty do
                b.Opacity <- 1.
        let turnHeartShuffleOff() =
            for b in row1boxesHearts do
                b.Opacity <- 1.
            for b in row1BoxesEmpty do
                b.Opacity <- 0.
        tunrHeartShuffleOn()
        let cutoffCanvas = new Canvas(Width=85., Height=85., ClipToBounds=true)
        cutoffCanvas.Children.Add(hsGrid) |> ignore
        let border = new Border(BorderBrush=Brushes.DarkGray, BorderThickness=Thickness(8.,8.,0.,0.), Child=cutoffCanvas)

        let checkboxSP = new StackPanel(Orientation=Orientation.Vertical, VerticalAlignment=VerticalAlignment.Center)

        let hscb = new CheckBox(Content=new TextBox(Text="Heart Shuffle",IsReadOnly=true), Margin=Thickness(10.))
        hscb.IsChecked <- System.Nullable.op_Implicit true
        hscb.Checked.Add(fun _ -> tunrHeartShuffleOn())
        hscb.Unchecked.Add(fun _ -> turnHeartShuffleOff())
        checkboxSP.Children.Add(hscb) |> ignore

        let hdcb = new CheckBox(Content=new TextBox(Text="Hide Dungeon Numbers",IsReadOnly=true), Margin=Thickness(10.))
        hdcb.IsChecked <- System.Nullable.op_Implicit false
        hdcb.Checked.Add(fun _ -> turnHideDungeonNumbersOn())
        hdcb.Unchecked.Add(fun _ -> turnHideDungeonNumbersOff())
        checkboxSP.Children.Add(hdcb) |> ignore

        hsPanel.Children.Add(checkboxSP) |> ignore
        hsPanel.Children.Add(border) |> ignore
        stackPanel.Children.Add(hsPanel) |> ignore


        let tb = dock <| new TextBox(Text="Settings (most can be changed later, using 'Options...' button above timeline):", Margin=spacing)
        stackPanel.Children.Add(tb) |> ignore
        TrackerModel.Options.readSettings()
        let options = OptionsMenu.makeOptionsCanvas()
        stackPanel.Children.Add(options) |> ignore

        let tb = dock <| new TextBox(Text="\nNote: once you start, you can click the\n'start spot' icon in the legend\nto mark your start screen at any time\n",IsReadOnly=true, Margin=spacing, MaxWidth=300.)
        stackPanel.Children.Add(tb) |> ignore
        let Quests = [|
                "First Quest Overworld"
                "Second Quest Overworld"
                "Mixed - First Quest Overworld"
                "Mixed - Second Quest Overworld"
            |]
        let mutable startButtonHasBeenClicked = false
        for i = 0 to 3 do
            let startButton = new Button(Content=new TextBox(Text=Quests.[i],IsReadOnly=true,IsHitTestVisible=false), MaxWidth=300.)
            stackPanel.Children.Add(startButton) |> ignore
            startButton.Click.Add(fun _ ->
                    if startButtonHasBeenClicked then () else
                    startButtonHasBeenClicked <- true
                    let tb = new TextBox(Text="\nLoading UI...\n", IsReadOnly=true, MaxWidth=300.)
                    stackPanel.Children.Add(tb) |> ignore
                    let ctxt = System.Threading.SynchronizationContext.Current
                    Async.Start (async {
                        do! Async.Sleep(10) // get off UI thread so UI will update
                        do! Async.SwitchToContext ctxt
                        TrackerModel.Options.writeSettings()
                        printfn "you pressed start after selecting %d" i
                        this.Background <- Brushes.Black
                        let heartShuffle = hscb.IsChecked.HasValue && hscb.IsChecked.Value
                        let kind = 
                            if hdcb.IsChecked.HasValue && hdcb.IsChecked.Value then
                                TrackerModel.DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS
                            else
                                TrackerModel.DungeonTrackerInstanceKind.DEFAULT
                        let cm,u = UI.makeAll(i, heartShuffle, kind)
                        UI.resetTimerEvent.Publish.Add(fun _ -> lastUpdateMinute <- 0; updateTimeline(0); startTime <- DateTime.Now)
                        updateTimeline <- u
                        UI.canvasAdd(cm.AppMainCanvas, hmsTimeTextBox, UI.RIGHT_COL+80., 0.)
                        this.Content <- cm.RootCanvas
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
            desktop.MainWindow <- new MyWindow()
        | _ -> ()
        base.OnFrameworkInitializationCompleted()

[<EntryPoint>]
let main argv =
    // Avalonia configuration, don't remove; also used by visual designer.
    let builder = Avalonia.AppBuilder.Configure<App>()
    let builder = Avalonia.AppBuilderDesktopExtensions.UsePlatformDetect(builder)
    let builder = Avalonia.LoggingExtensions.LogToTrace(builder, Avalonia.Logging.LogEventLevel.Warning, [||])
    Avalonia.ClassicDesktopStyleApplicationLifetimeExtensions.StartWithClassicDesktopLifetime(builder, argv)
