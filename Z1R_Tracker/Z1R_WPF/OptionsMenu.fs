module OptionsMenu

open System.Windows.Controls
open System.Windows.Media
open System.Windows

let voice = new System.Speech.Synthesis.SpeechSynthesizer()
let mutable microphoneFailedToInitialize = false
let mutable gamepadFailedToInitialize = false

let link(cb:CheckBox, b:TrackerModel.Options.Bool) =
    cb.IsChecked <- System.Nullable.op_Implicit b.Value
    cb.Checked.Add(fun _ -> b.Value <- true)
    cb.Unchecked.Add(fun _ -> b.Value <- false)

let data1 = [|
    "Draw routes", "Constantly display routing lines when mousing over overworld tiles", TrackerModel.Options.Overworld.DrawRoutes
    "Highlight nearby", "Highlight nearest unmarked overworld tiles when mousing", TrackerModel.Options.Overworld.HighlightNearby
    "Show magnifier", "Display magnified view of overworld tiles when mousing", TrackerModel.Options.Overworld.ShowMagnifier
    |]

let data21 = [|
    "Dungeon feedback", "Note when dungeons are located/completed, triforces obtained, and go-time", TrackerModel.Options.VoiceReminders.DungeonFeedback
    "Sword hearts", "Remind to consider white/magical sword when you get 4-6 or 10-14 hearts", TrackerModel.Options.VoiceReminders.SwordHearts
    "Coast Item", "Reminder to fetch to coast item when you have the ladder", TrackerModel.Options.VoiceReminders.CoastItem
    |]

let data22 = [|
    "Recorder/PB spots", "Occasional reminder of how many recorder/power-bracelet spots remain", TrackerModel.Options.VoiceReminders.RecorderPBSpots
    "Have any key/ladder", "One-time reminder, a little while after obtaining these items, that you have them", TrackerModel.Options.VoiceReminders.HaveKeyLadder
    |]

let makeOptionsCanvas(width, height, heightOffset) = 
    let optionsCanvas = new Canvas(Width=width, Height=height, Background=Brushes.White, Opacity=0., IsHitTestVisible=false)
    let style = new Style(typeof<TextBox>)
    style.Setters.Add(new Setter(TextBox.BorderThicknessProperty, Thickness(0.)))
    style.Setters.Add(new Setter(TextBox.FontSizeProperty, 16.))
    optionsCanvas.Resources.Add(typeof<TextBox>, style)
    let style = new Style(typeof<CheckBox>)
    style.Setters.Add(new Setter(CheckBox.HeightProperty, 22.))
    optionsCanvas.Resources.Add(typeof<CheckBox>, style)

    let options1sp = new StackPanel(Orientation=Orientation.Vertical, Margin=Thickness(10.,0.,10.,0.))
    let tb = new TextBox(Text="Overworld settings", IsReadOnly=true, Margin=Thickness(0.,heightOffset,0.,0.), FontWeight=FontWeights.Bold)
    options1sp.Children.Add(tb) |> ignore
    for text,tip,b in data1 do
        let cb = new CheckBox(Content=new TextBox(Text=text,IsReadOnly=true))
        cb.ToolTip <- tip
        link(cb, b)
        options1sp.Children.Add(cb) |> ignore
    let optionsAllsp = new StackPanel(Orientation=Orientation.Horizontal)
    optionsAllsp.Children.Add(options1sp) |> ignore

    let options2sp = new StackPanel(Orientation=Orientation.Vertical, Margin=Thickness(10.,2.,10.,0.))
    let tb = new TextBox(Text="Voice reminders", IsReadOnly=true, FontWeight=FontWeights.Bold)
    options2sp.Children.Add(tb) |> ignore
    let options2Topsp = new StackPanel(Orientation=Orientation.Horizontal, Margin=Thickness(0.,0.,0.,6.))
    let muteCB = new CheckBox(Content=new TextBox(Text="Mute all",IsReadOnly=true))
    muteCB.ToolTip <- "Silence all voice reminders"
    muteCB.IsChecked <- System.Nullable.op_Implicit TrackerModel.Options.IsMuted
    muteCB.Checked.Add(fun _ -> TrackerModel.Options.IsMuted <- true; voice.Volume <- 0)
    muteCB.Unchecked.Add(fun _ -> TrackerModel.Options.IsMuted <- false; voice.Volume <- TrackerModel.Options.Volume)
    options2Topsp.Children.Add(muteCB) |> ignore
    let volumeText = new TextBox(Text="Volume",IsReadOnly=true, Margin=Thickness(10., 0., 0., 0.))
    options2Topsp.Children.Add(volumeText) |> ignore
    let slider = new Slider(Orientation=Orientation.Horizontal, Maximum=100., TickFrequency=10., TickPlacement=Primitives.TickPlacement.Both, IsSnapToTickEnabled=true, Width=200.)
    slider.Value <- float TrackerModel.Options.Volume
    slider.ValueChanged.Add(fun _ -> TrackerModel.Options.Volume <- int slider.Value; if not(TrackerModel.Options.IsMuted) then voice.Volume <- TrackerModel.Options.Volume)
    let dp = new DockPanel(VerticalAlignment=VerticalAlignment.Center, Margin=Thickness(0.))
    dp.Children.Add(slider) |> ignore
    options2Topsp.Children.Add(dp) |> ignore

    options2sp.Children.Add(options2Topsp) |> ignore

    let options2V1sp = new StackPanel(Orientation=Orientation.Vertical)
    for text,tip,b in data21 do
        let cb = new CheckBox(Content=new TextBox(Text=text,IsReadOnly=true))
        cb.ToolTip <- tip
        link(cb, b)
        options2V1sp.Children.Add(cb) |> ignore
    let options2V2sp = new StackPanel(Orientation=Orientation.Vertical, Margin=Thickness(10.,0.,0.,0.))
    for text,tip,b in data22 do
        let cb = new CheckBox(Content=new TextBox(Text=text,IsReadOnly=true))
        cb.ToolTip <- tip
        link(cb, b)
        options2V2sp.Children.Add(cb) |> ignore

    let options2Hsp = new StackPanel(Orientation=Orientation.Horizontal, Margin=Thickness(0.,0.,0.,0.))
    options2Hsp.Children.Add(options2V1sp) |> ignore
    options2Hsp.Children.Add(options2V2sp) |> ignore
    options2sp.Children.Add(options2Hsp) |> ignore
    optionsAllsp.Children.Add(new Canvas(Height=height,Width=2.,Background=Brushes.Black)) |> ignore
    optionsAllsp.Children.Add(options2sp) |> ignore
    optionsAllsp.Children.Add(new Canvas(Height=height,Width=2.,Background=Brushes.Black)) |> ignore

    let options3sp = new StackPanel(Orientation=Orientation.Vertical, Margin=Thickness(10.,2.,0.,0.))
    let tb = new TextBox(Text="Other", IsReadOnly=true, FontWeight=FontWeights.Bold)
    options3sp.Children.Add(tb) |> ignore

    let cb = new CheckBox(Content=new TextBox(Text="Listen for speech",IsReadOnly=true))
    if microphoneFailedToInitialize then
        cb.IsEnabled <- false
        cb.IsChecked <- System.Nullable.op_Implicit false
        cb.ToolTip <- "Disabled (microphone was not initialized properly during startup)"
        ToolTipService.SetShowOnDisabled(cb, true)
    else
        cb.ToolTip <- "Use the microphone to listen for spoken map update commands\nExample: say 'tracker set bomb shop' while hovering an unmarked map tile"
        link(cb, TrackerModel.Options.ListenForSpeech)
    options3sp.Children.Add(cb) |> ignore

    let cb = new CheckBox(Content=new TextBox(Text="Confirmation sound",IsReadOnly=true), Margin=Thickness(20.,0.,0.,0.))
    if microphoneFailedToInitialize then
        cb.IsEnabled <- false
        cb.IsChecked <- System.Nullable.op_Implicit false
        cb.ToolTip <- "Disabled (microphone was not initialized properly during startup)"
        ToolTipService.SetShowOnDisabled(cb, true)
    else
        cb.ToolTip <- "Play a confirmation sound whenever speech recognition is used to make an update to the tracker"
        link(cb, TrackerModel.Options.PlaySoundWhenUseSpeech)
    options3sp.Children.Add(cb) |> ignore

    let cb = new CheckBox(Content=new TextBox(Text="Require PTT",IsReadOnly=true), Margin=Thickness(20.,0.,0.,0.))
    if microphoneFailedToInitialize then
        cb.IsEnabled <- false
        cb.IsChecked <- System.Nullable.op_Implicit false
        cb.ToolTip <- "Disabled (microphone was not initialized properly during startup)"
        ToolTipService.SetShowOnDisabled(cb, true)
    elif gamepadFailedToInitialize then
        cb.IsEnabled <- false
        cb.IsChecked <- System.Nullable.op_Implicit false
        cb.ToolTip <- "Disabled (gamepad was not initialized properly during startup)"
        ToolTipService.SetShowOnDisabled(cb, true)
    else
        link(cb, TrackerModel.Options.RequirePTTForSpeech)
        cb.ToolTip <- "Only listen for speech when Push-To-Talk button is held (SNES gamepad left shoulder button)"
    options3sp.Children.Add(cb) |> ignore

    let cb = new CheckBox(Content=new TextBox(Text="Second quest dungeons",IsReadOnly=true))
    cb.IsChecked <- System.Nullable.op_Implicit TrackerModel.Options.IsSecondQuestDungeons.Value
    cb.Checked.Add(fun _ -> TrackerModel.Options.IsSecondQuestDungeons.Value <- true; TrackerModel.forceUpdate())
    cb.Unchecked.Add(fun _ -> TrackerModel.Options.IsSecondQuestDungeons.Value <- false; TrackerModel.forceUpdate())
    cb.ToolTip <- "Check this if dungeon 4, rather than dungeon 1, has 3 items"
    options3sp.Children.Add(cb) |> ignore

    let cb = new CheckBox(Content=new TextBox(Text="Mirror overworld",IsReadOnly=true))
    cb.IsChecked <- System.Nullable.op_Implicit TrackerModel.Options.MirrorOverworld.Value
    cb.Checked.Add(fun _ -> TrackerModel.Options.MirrorOverworld.Value <- true; TrackerModel.forceUpdate())
    cb.Unchecked.Add(fun _ -> TrackerModel.Options.MirrorOverworld.Value <- false; TrackerModel.forceUpdate())
    cb.ToolTip <- "Flip the overworld map East<->West"
    options3sp.Children.Add(cb) |> ignore

    optionsAllsp.Children.Add(options3sp) |> ignore
    Graphics.canvasAdd(optionsCanvas, optionsAllsp, 0., 0.)
    optionsCanvas