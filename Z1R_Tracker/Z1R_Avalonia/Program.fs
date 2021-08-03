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
    let hmsTimeTextBox = new TextBox(Text="timer",FontSize=42.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    //                 items  ow map  prog  timeline                     dungeon tabs                
    let HEIGHT = float(30*4 + 11*3*9 + 30)// TODO + 3*WPFUI.TLH + 3 + WPFUI.TH + 27*8 + 12*7 + 30 + 40) // (what is the final 40?)
    let WIDTH = float(16*16*3 + 16)  // ow map width (what is the final 16?)
    do
        // TODO WPFUI.timeTextBox <- hmsTimeTextBox
        let timer = new Avalonia.Threading.DispatcherTimer()
        timer.Interval <- TimeSpan.FromSeconds(1.0)
        timer.Tick.Add(fun _ -> this.Update(false))
        timer.Start()
        this.KeyBindings.Add(new Avalonia.Input.KeyBinding(Gesture=Avalonia.Input.KeyGesture.Parse("F5"), Command=new MyCommand(fun x -> 
                printfn "F5 was pressed"
                // TODO TrackerModel.startIconX <- WPFUI.currentlyMousedOWX
                // TODO TrackerModel.startIconY <- WPFUI.currentlyMousedOWY
                TrackerModel.forceUpdate()
                )))
        this.KeyBindings.Add(new Avalonia.Input.KeyBinding(Gesture=Avalonia.Input.KeyGesture.Parse("F10"), Command=new MyCommand(fun x -> 
                this.Update(true)
                )))

        // full window
        this.Title <- "Zelda 1 Randomizer"
        let stackPanel = new StackPanel(Orientation=Orientation.Vertical)
        let tb = new TextBox(Text="Choose overworld quest:")
        stackPanel.Children.Add(tb) |> ignore
        let owQuest = new ComboBox()
        owQuest.Items <- [|
                "First Quest"
                "Second Quest"
                "Mixed - First Quest"
                "Mixed - Second Quest"
            |]
        owQuest.SelectedIndex <- owMapNum % 4
        stackPanel.Children.Add(owQuest) |> ignore
        let tb = new TextBox(Text="\nNote: once you start, you can use F5 to\nplace the 'start spot' icon at your mouse,\nor F10 to reset the timer to 0, at any time\n",IsReadOnly=true)
        stackPanel.Children.Add(tb) |> ignore
        let startButton = new Button(Content=new TextBox(Text="Start Z-Tracker",IsReadOnly=true,IsHitTestVisible=false))
        stackPanel.Children.Add(startButton) |> ignore
        let hstackPanel = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
        hstackPanel.Children.Add(stackPanel) |> ignore
        this.Content <- hstackPanel
        startButton.Click.Add(fun _ ->
                printfn "you pressed start after selecting %d" owQuest.SelectedIndex
                // TODO stuff
            )
    member this.Update(f10Press) =
        // update time
        let ts = DateTime.Now - startTime
        let h,m,s = ts.Hours, ts.Minutes, ts.Seconds
        hmsTimeTextBox.Text <- sprintf "%02d:%02d:%02d" h m s
        // update timeline
        if f10Press || ts.Seconds = 0 then
            printfn "F10 was pressed"
            updateTimeline(int ts.TotalMinutes)

type App() =
    inherit Avalonia.Application()
    override this.Initialize() =
        () //Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this)
        this.Styles.AddRange [ 
            new Avalonia.Markup.Xaml.Styling.StyleInclude(baseUri=null, Source = Uri("resm:Avalonia.Themes.Default.DefaultTheme.xaml?assembly=Avalonia.Themes.Default"))
            new Avalonia.Markup.Xaml.Styling.StyleInclude(baseUri=null, Source = Uri("resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?assembly=Avalonia.Themes.Default"))
        ]
    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime as desktop ->
            desktop.MainWindow <- new MyWindow(0)
        | _ -> ()
        base.OnFrameworkInitializationCompleted()

[<EntryPoint>]
let main argv =
    let message = "from F#" // Call the function
    printfn "Hello world %s" message

    TrackerModel.initializeAll(new OverworldData.OverworldInstance(OverworldData.FIRST))
    printfn "%b" (TrackerModel.PlayerProgressAndTakeAnyHearts().PlayerHasBlueRing.Value())

    // Avalonia configuration, don't remove; also used by visual designer.
    let builder = Avalonia.AppBuilder.Configure<App>()
    let builder = Avalonia.AppBuilderDesktopExtensions.UsePlatformDetect(builder)
    let builder = Avalonia.LoggingExtensions.LogToTrace(builder, Avalonia.Logging.LogEventLevel.Warning, [||])
    Avalonia.ClassicDesktopStyleApplicationLifetimeExtensions.StartWithClassicDesktopLifetime(builder, argv)
