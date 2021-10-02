module OptionsMenu

open Avalonia.Controls
open Avalonia.Media
open Avalonia
open Avalonia.Layout

let link(cb:CheckBox, b:TrackerModel.Options.Bool) =
    cb.IsChecked <- System.Nullable.op_Implicit b.Value
    cb.Checked.Add(fun _ -> b.Value <- true)
    cb.Unchecked.Add(fun _ -> b.Value <- false)

let data1 = [|
    "Draw routes", "Constantly display routing lines when mousing over overworld tiles", TrackerModel.Options.Overworld.DrawRoutes
    "Highlight nearby", "Highlight nearest unmarked overworld tiles when mousing", TrackerModel.Options.Overworld.HighlightNearby
    "Show magnifier", "Display magnified view of overworld tiles when mousing", TrackerModel.Options.Overworld.ShowMagnifier
    |]
let data2 = [|
    "Dungeon feedback", "Note when dungeons are located/completed, triforces obtained, and go-time", 
        TrackerModel.Options.VoiceReminders.DungeonFeedback, TrackerModel.Options.VisualReminders.DungeonFeedback
    "Sword hearts", "Remind to consider white/magical sword when you get 4-6 or 10-14 hearts", 
        TrackerModel.Options.VoiceReminders.SwordHearts,     TrackerModel.Options.VisualReminders.SwordHearts
    "Coast Item", "Reminder to fetch to coast item when you have the ladder", 
        TrackerModel.Options.VoiceReminders.CoastItem,       TrackerModel.Options.VisualReminders.CoastItem
    "Recorder/PB spots", "Occasional reminder of how many recorder/power-bracelet spots remain", 
        TrackerModel.Options.VoiceReminders.RecorderPBSpots, TrackerModel.Options.VisualReminders.RecorderPBSpots
    "Have any key/ladder", "One-time reminder, a little while after obtaining these items, that you have them", 
        TrackerModel.Options.VoiceReminders.HaveKeyLadder,   TrackerModel.Options.VisualReminders.HaveKeyLadder
    "Blockers", "Reminder when you may have become unblocked on a previously-aborted dungeon", 
        TrackerModel.Options.VoiceReminders.Blockers,        TrackerModel.Options.VisualReminders.Blockers
    |]

let makeOptionsCanvas() = 
    let header(tb:TextBox) = 
        tb.Margin <- Thickness(0., 0., 0., 6.)
        tb.BorderThickness <- Thickness(0., 0., 0., 1.)
        tb
    let options1sp = new StackPanel(Orientation=Orientation.Vertical, Margin=Thickness(10.,0.,10.,0.))
    let tb = new TextBox(Text="Overworld settings", IsReadOnly=true, FontWeight=FontWeight.Bold, BorderBrush=Brushes.Transparent, IsHitTestVisible=false) |> header
    options1sp.Children.Add(tb) |> ignore
    for text,tip,b in data1 do
        let cb = new CheckBox(Content=new TextBox(Text=text,IsReadOnly=true, BorderBrush=Brushes.Transparent, IsHitTestVisible=false))
        ToolTip.SetTip(cb,tip)
        link(cb, b)
        options1sp.Children.Add(cb) |> ignore
    let optionsAllsp = new StackPanel(Orientation=Orientation.Horizontal, Margin=Thickness(2.))
    optionsAllsp.Children.Add(options1sp) |> ignore

    let options2sp = new StackPanel(Orientation=Orientation.Vertical, Margin=Thickness(10.,2.,10.,0.))
    let tb = new TextBox(Text="Reminders", IsReadOnly=true, FontWeight=FontWeight.Bold) |> header
    options2sp.Children.Add(tb) |> ignore
    // volume and slider
    let options2Topsp = new StackPanel(Orientation=Orientation.Horizontal, Margin=Thickness(0.,0.,0.,6.))
    let volumeText = new TextBox(Text="Volume",IsReadOnly=true, Margin=Thickness(0.))
    options2Topsp.Children.Add(volumeText) |> ignore
    let slider = new Slider(Orientation=Orientation.Horizontal, Maximum=100., TickFrequency=10., TickPlacement=TickPlacement.Outside, IsSnapToTickEnabled=true, Width=200.)
    slider.Value <- float TrackerModel.Options.Volume
    slider.PropertyChanged.Add(fun _ -> 
        TrackerModel.Options.Volume <- int slider.Value
        ()//Graphics.volumeChanged.Trigger(TrackerModel.Options.Volume)
        if not(TrackerModel.Options.IsMuted) then 
            ()//voice.Volume <- TrackerModel.Options.Volume
        )
    let dp = new DockPanel(VerticalAlignment=VerticalAlignment.Center, Margin=Thickness(0.))
    dp.Children.Add(slider) |> ignore
    options2Topsp.Children.Add(dp) |> ignore
    options2sp.Children.Add(options2Topsp) |> ignore
    // stop
    let muteCB = new CheckBox(Content=new TextBox(Text="Stop all",IsReadOnly=true))
    ToolTip.SetTip(muteCB, "Turn off all reminders")
    muteCB.IsChecked <- System.Nullable.op_Implicit TrackerModel.Options.IsMuted
    muteCB.Checked.Add(fun _ -> TrackerModel.Options.IsMuted <- true; (*voice.Volume <- 0*))
    muteCB.Unchecked.Add(fun _ -> TrackerModel.Options.IsMuted <- false; (*voice.Volume <- TrackerModel.Options.Volume*))
    options2sp.Children.Add(muteCB) |> ignore
    // other settings
    let options2Grid = new Grid()
    for i = 1 to 3 do
        options2Grid.ColumnDefinitions.Add(new ColumnDefinition(Width=GridLength.Auto))
    for i = 0 to data2.Length do
        options2Grid.RowDefinitions.Add(new RowDefinition(Height=GridLength.Auto))
    let voiceTB = new TextBox(Text="Voice",IsReadOnly=true)
    ToolTip.SetTip(voiceTB, "The reminder will be spoken aloud")
    Graphics.gridAdd(options2Grid, voiceTB, 0, 0)
    let visualTB = new TextBox(Text="Visual",IsReadOnly=true)
    ToolTip.SetTip(visualTB, "The reminder will be displayed as icons in the upper right of the Timeline")
    Graphics.gridAdd(options2Grid, visualTB, 1, 0)
    let mutable row = 1
    for text,tip,bVoice,bVisual in data2 do
        if row%2=1 then
            let backgroundColor() = new DockPanel(Background=Graphics.almostBlack)
            Graphics.gridAdd(options2Grid, backgroundColor(), 0, row)
            Graphics.gridAdd(options2Grid, backgroundColor(), 1, row)
            Graphics.gridAdd(options2Grid, backgroundColor(), 2, row)
        let cbVoice = new CheckBox(HorizontalAlignment=HorizontalAlignment.Center)
        link(cbVoice, bVoice)
        Graphics.gridAdd(options2Grid, cbVoice, 0, row)
        let cbVisual = new CheckBox(HorizontalAlignment=HorizontalAlignment.Center)
        link(cbVisual, bVisual)
        Graphics.gridAdd(options2Grid, cbVisual, 1, row)
        let tb = new TextBox(Text=text,IsReadOnly=true, Background=Brushes.Transparent)
        ToolTip.SetTip(tb, tip)
        Graphics.gridAdd(options2Grid, tb, 2, row)
        row <- row + 1

    options2sp.Children.Add(options2Grid) |> ignore
    optionsAllsp.Children.Add(new DockPanel(Width=2.,Background=Brushes.Gray)) |> ignore
    optionsAllsp.Children.Add(options2sp) |> ignore
    optionsAllsp.Children.Add(new DockPanel(Width=2.,Background=Brushes.Gray)) |> ignore

    let options3sp = new StackPanel(Orientation=Orientation.Vertical, Margin=Thickness(10.,0.,0.,0.))
    let tb = new TextBox(Text="Other", IsReadOnly=true, FontWeight=FontWeight.Bold, BorderBrush=Brushes.Transparent, IsHitTestVisible=false) |> header
    options3sp.Children.Add(tb) |> ignore
    let cb = new CheckBox(Content=new TextBox(Text="Second quest dungeons",IsReadOnly=true, BorderBrush=Brushes.Transparent, IsHitTestVisible=false))
    cb.IsChecked <- System.Nullable.op_Implicit TrackerModel.Options.IsSecondQuestDungeons.Value
    cb.Checked.Add(fun _ -> TrackerModel.Options.IsSecondQuestDungeons.Value <- true; TrackerModel.forceUpdate())
    cb.Unchecked.Add(fun _ -> TrackerModel.Options.IsSecondQuestDungeons.Value <- false; TrackerModel.forceUpdate())
    ToolTip.SetTip(cb,"Check this if dungeon 4, rather than dungeon 1, has 3 items")
    options3sp.Children.Add(cb) |> ignore
    let cb = new CheckBox(Content=new TextBox(Text="Mirror overworld",IsReadOnly=true, BorderBrush=Brushes.Transparent, IsHitTestVisible=false))
    cb.IsChecked <- System.Nullable.op_Implicit TrackerModel.Options.MirrorOverworld.Value
    cb.Checked.Add(fun _ -> TrackerModel.Options.MirrorOverworld.Value <- true; TrackerModel.forceUpdate())
    cb.Unchecked.Add(fun _ -> TrackerModel.Options.MirrorOverworld.Value <- false; TrackerModel.forceUpdate())
    ToolTip.SetTip(cb,"Flip the overworld map East<->West")
    options3sp.Children.Add(cb) |> ignore

    optionsAllsp.Children.Add(options3sp) |> ignore

    optionsAllsp
