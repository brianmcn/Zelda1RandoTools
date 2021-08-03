open System
open Avalonia
open Avalonia.Controls

type MyWindow() as this = 
    inherit Window()
    do
        // full window
        this.Title <- "Zelda 1 Randomizer"


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
            desktop.MainWindow <- new MyWindow()
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
