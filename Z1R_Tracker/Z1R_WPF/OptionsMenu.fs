module OptionsMenu

open System.Windows.Controls
open System.Windows.Media
open System.Windows

let voice = new System.Speech.Synthesis.SpeechSynthesizer()
let mutable microphoneFailedToInitialize = false
let mutable gamepadFailedToInitialize = false

let broadcastWindowOptionChanged = new Event<unit>()
let BOARDInsteadOfLEVELOptionChanged = new Event<unit>()
let secondQuestDungeonsOptionChanged = new Event<unit>()
let showBasementInfoOptionChanged = new Event<unit>()
let bookForHelpfulHintsOptionChanged = new Event<unit>()
let requestRedrawOverworldEvent = new Event<unit>()

let link(cb:CheckBox, b:TrackerModelOptions.Bool, needFU, otherEffect) =
    let effect() = 
        if needFU then 
            TrackerModel.forceUpdate()
        otherEffect()
    cb.IsChecked <- System.Nullable.op_Implicit b.Value
    cb.Checked.Add(fun _ -> b.Value <- true; effect())
    cb.Unchecked.Add(fun _ -> b.Value <- false; effect())

let data1o = [|
    "Draw routes", "Constantly display routing lines when mousing over overworld tiles", TrackerModelOptions.Overworld.DrawRoutes, true, (fun()->()), None
    "Show screen scrolls", "Routing lines assume the player can screen scroll\nScreen scrolls appear as curved lines", TrackerModelOptions.Overworld.RoutesCanScreenScroll, true, (fun()->()), Some(Thickness(20.,0.,0.,0.))
    "Highlight nearby", "Highlight nearest unmarked gettable overworld tiles when mousing", TrackerModelOptions.Overworld.HighlightNearby, false, (fun()->()), None
    "Show magnifier", "Display magnified view of overworld tiles when mousing", TrackerModelOptions.Overworld.ShowMagnifier, false, (fun()->()), None
    "Mirror overworld", "Flip the overworld map East<->West", TrackerModelOptions.Overworld.MirrorOverworld, true, (fun()->()), None
    "Shops before dungeons", "In the overworld map tile popup, the grid starts with shops when this is checked\n(starts with dungeons when unchecked)", TrackerModelOptions.Overworld.ShopsFirst, false, (fun()->()), None
    |]

let data1d = [|
    "BOARD instead of LEVEL", "Check this to change the dungeon column labels to BOARD-N instead of LEVEL-N", TrackerModelOptions.BOARDInsteadOfLEVEL, false, BOARDInsteadOfLEVELOptionChanged.Trigger
    "Second quest dungeons", "Check this if dungeon 4, rather than dungeon 1, has 3 items (no effect when Hidden Dungeon Numbers)", TrackerModelOptions.IsSecondQuestDungeons, false, secondQuestDungeonsOptionChanged.Trigger
    "Show basement info", "Check this if empty dungeon item boxes should suggest whether they are found as\nbasement items rather than floor drops (no effect when Hidden Dungeon Numbers)", TrackerModelOptions.ShowBasementInfo, false, showBasementInfoOptionChanged.Trigger
    "Do door inference", "Check this to mark a green door when you mark a new room, if the point of entry can be inferred", TrackerModelOptions.DoDoorInference, false, fun()->()
    "Book for Helpful Hints", "Check this if both 'Book To Understand Old Men' flag is on, and\n'Helpful' hints are available. The tracker will let you left-click\nOld Man Hint rooms to toggle whether you have read them yet.", TrackerModelOptions.BookForHelpfulHints, false, bookForHelpfulHintsOptionChanged.Trigger
    |]

let data2 = [|
    "Dungeon feedback", "Note when dungeons are located/completed, triforces obtained, and go-time", 
        TrackerModelOptions.VoiceReminders.DungeonFeedback, TrackerModelOptions.VisualReminders.DungeonFeedback
    "Sword hearts", "Remind to consider white/magical sword when you get 4-6 or 10-14 hearts", 
        TrackerModelOptions.VoiceReminders.SwordHearts,     TrackerModelOptions.VisualReminders.SwordHearts
    "Coast Item", "Reminder to fetch to coast item when you have the ladder", 
        TrackerModelOptions.VoiceReminders.CoastItem,       TrackerModelOptions.VisualReminders.CoastItem
    "Recorder/PB/Boomstick", "Periodic reminders of how many recorder/power-bracelet spots remain, or that the boomstick is available", 
        TrackerModelOptions.VoiceReminders.RecorderPBSpotsAndBoomstickBook, TrackerModelOptions.VisualReminders.RecorderPBSpotsAndBoomstickBook
    "Have any key/ladder", "One-time reminder, a little while after obtaining these items, that you have them", 
        TrackerModelOptions.VoiceReminders.HaveKeyLadder,   TrackerModelOptions.VisualReminders.HaveKeyLadder
    "Blockers", "Reminder when you may have become unblocked on a previously-aborted dungeon", 
        TrackerModelOptions.VoiceReminders.Blockers,        TrackerModelOptions.VisualReminders.Blockers
    "Door Repair Count", "Each time you uncover a door repair charge, remind the count of how many you have found", 
        TrackerModelOptions.VoiceReminders.DoorRepair,        TrackerModelOptions.VisualReminders.DoorRepair
    |]

let makeOptionsCanvas(cm:CustomComboBoxes.CanvasManager, includePopupExplainer) = 
    let width = cm.AppMainCanvas.Width
    let all = new Border(BorderThickness=Thickness(2.), BorderBrush=Brushes.DarkGray, Background=Brushes.Black)
    let optionsAllsp = new StackPanel(Orientation=Orientation.Horizontal, Width=width, Background=Brushes.Black)
    let AddStyle(e:FrameworkElement) = 
        let style = new Style(typeof<TextBox>)
        style.Setters.Add(new Setter(TextBox.BorderThicknessProperty, Thickness(0.)))
        style.Setters.Add(new Setter(TextBox.BorderBrushProperty, Brushes.DarkGray))
        style.Setters.Add(new Setter(TextBox.FontSizeProperty, 16.))
        style.Setters.Add(new Setter(TextBox.ForegroundProperty, Brushes.Orange))
        style.Setters.Add(new Setter(TextBox.BackgroundProperty, Brushes.Black))
        e.Resources.Add(typeof<TextBox>, style)
        let style = new Style(typeof<CheckBox>)
        style.Setters.Add(new Setter(CheckBox.HeightProperty, 22.))
        e.Resources.Add(typeof<CheckBox>, style)
    AddStyle(all)

    let header(tb:TextBox) = 
        tb.Margin <- Thickness(0., 0., 0., 6.)
        tb.BorderThickness <- Thickness(0., 0., 0., 1.)
        tb
    let options1sp = new StackPanel(Orientation=Orientation.Vertical, Margin=Thickness(10.,0.,10.,0.))
    let tb = new TextBox(Text="Overworld settings", IsReadOnly=true, FontWeight=FontWeights.Bold) |> header
    options1sp.Children.Add(tb) |> ignore
    for text,tip,b,needFU,oe,marginOpt in data1o do
        let cb = new CheckBox(Content=new TextBox(Text=text,IsReadOnly=true))
        if marginOpt.IsSome then
            cb.Margin <- marginOpt.Value
        cb.ToolTip <- tip
        ToolTipService.SetShowDuration(cb, 10000)
        link(cb, b, needFU, oe)
        options1sp.Children.Add(cb) |> ignore
    let moreButton = Graphics.makeButton(" More settings... ",None,None)
    moreButton.HorizontalAlignment <- HorizontalAlignment.Left
    options1sp.Children.Add(moreButton) |> ignore
    let mutable popupIsActive = false
    moreButton.Click.Add(fun _ ->
        if not popupIsActive then
            popupIsActive <- true
            let wh = new System.Threading.ManualResetEvent(false)
            let sp = new StackPanel(Orientation=Orientation.Vertical)
            let tb = new TextBox(Text="Overworld marks to hide", IsReadOnly=true, FontWeight=FontWeights.Bold) |> header
            sp.Children.Add(tb) |> ignore
            let desc = "Sometimes you want to mark certain map tiles (e.g. Door Repairs) so the tracker can help you (e.g. by keeping count), but " +
                        "you don't want to clutter your overworld map with icons (e.g. Door icons) that you don't need to see or come back to.  " +
                        "In these cases, you can opt to 'hide' certain icons, so that they appear like \"Don't Care\" spots (just grayed out tile " +
                        "with no icon) rather than with an icon on the map.\n\n" + 
                        "Check each tile that you would prefer to hide after you mark it."
            let tb = new TextBox(Text=desc, IsReadOnly=true, TextWrapping=TextWrapping.Wrap)
            sp.Children.Add(tb) |> ignore
            let len = TrackerModel.MapSquareChoiceDomainHelper.TilesThatSupportHidingOverworldMarks.Length
            let firstHalf = TrackerModel.MapSquareChoiceDomainHelper.TilesThatSupportHidingOverworldMarks.[0..(len/2-1)]
            let secondHalf = TrackerModel.MapSquareChoiceDomainHelper.TilesThatSupportHidingOverworldMarks.[(len/2)..]
            let boxes = new StackPanel(Orientation=Orientation.Horizontal, Margin=Thickness(20.,5.,0.,5.))
            let first = new StackPanel(Orientation=Orientation.Vertical)
            let second = new StackPanel(Orientation=Orientation.Vertical)
            boxes.Children.Add(first) |> ignore
            boxes.Children.Add(second) |> ignore
            sp.Children.Add(boxes) |> ignore
            let addTo(sp:StackPanel, a) = 
                for tile in a do
                    let desc = let _,_,s = TrackerModel.dummyOverworldTiles.[tile] in s
                    let desc = 
                        let i = desc.IndexOf('\n')
                        if i <> -1 then
                            desc.Substring(0, i)
                        else
                        desc
                    let cb = new CheckBox(Content=new TextBox(Text=desc,IsReadOnly=true), Margin=Thickness(20.,0.,0.,0.))
                    let b = TrackerModel.MapSquareChoiceDomainHelper.AsTrackerModelOptionsOverworldTilesToHide(tile)
                    link(cb, b, false, requestRedrawOverworldEvent.Trigger)
                    sp.Children.Add(cb) |> ignore
            addTo(first, firstHalf)
            addTo(second, secondHalf)
            let desc = "Note that even when hidden, certain tiles can be toggled 'bright' by left-clicking them.  For example, a Hint Shop where " +
                        "you have not yet bought out all the hints, but intend to return later, could be left-clicked to toggle it from dark to " +
                        "bright.  This behavior is retained even if you choose to hide the tile: left-clicking toggles between a hidden icon and " +
                        "a bright icon in that case.\n\n" + 
                        "You can mouse-hover the Zelda icon in the top of the tracker to temporarily make all hidden icons re-appear, if desired."
            let tb = new TextBox(Text=desc, IsReadOnly=true, TextWrapping=TextWrapping.Wrap)
            sp.Children.Add(tb) |> ignore
            AddStyle(sp)
            let b = new Border(Child=sp, BorderThickness=Thickness(2.), BorderBrush=Brushes.DarkGray, Background=Brushes.Black, Padding=Thickness(5.), Width=650.)
            async {
                do! CustomComboBoxes.DoModalDocked(cm, wh, Dock.Bottom, b)
                popupIsActive <- false
            } |> Async.StartImmediate
        )

    let tb = new TextBox(Text="Dungeon settings", IsReadOnly=true, FontWeight=FontWeights.Bold) |> header
    options1sp.Children.Add(tb) |> ignore
    for text,tip,b,needFU,oe in data1d do
        let cb = new CheckBox(Content=new TextBox(Text=text,IsReadOnly=true))
        cb.ToolTip <- tip
        ToolTipService.SetShowDuration(cb, 10000)
        link(cb, b, needFU, oe)
        options1sp.Children.Add(cb) |> ignore
    optionsAllsp.Children.Add(options1sp) |> ignore

    let options2sp = new StackPanel(Orientation=Orientation.Vertical, Margin=Thickness(10.,2.,10.,0.))
    let tb = new TextBox(Text="Reminders", IsReadOnly=true, FontWeight=FontWeights.Bold) |> header
    options2sp.Children.Add(tb) |> ignore
    // volume and slider
    let options2Topsp = new StackPanel(Orientation=Orientation.Horizontal, Margin=Thickness(0.,0.,0.,6.))
    let volumeText = new TextBox(Text="Volume",IsReadOnly=true, Margin=Thickness(0.))
    options2Topsp.Children.Add(volumeText) |> ignore
    let slider = new Slider(Orientation=Orientation.Horizontal, Maximum=100., TickFrequency=10., TickPlacement=Primitives.TickPlacement.Both, IsSnapToTickEnabled=true, Width=200.)
    slider.Value <- float TrackerModelOptions.Volume
    slider.ValueChanged.Add(fun _ -> 
        TrackerModelOptions.Volume <- int slider.Value
        Graphics.volumeChanged.Trigger(TrackerModelOptions.Volume)
        if not(TrackerModelOptions.IsMuted) then 
            voice.Volume <- TrackerModelOptions.Volume
        )
    let dp = new DockPanel(VerticalAlignment=VerticalAlignment.Center, Margin=Thickness(0.))
    dp.Children.Add(slider) |> ignore
    options2Topsp.Children.Add(dp) |> ignore
    options2sp.Children.Add(options2Topsp) |> ignore
    // stop
    let muteCB = new CheckBox(Content=new TextBox(Text="Stop all",IsReadOnly=true))
    muteCB.ToolTip <- "Turn off all reminders"
    muteCB.IsChecked <- System.Nullable.op_Implicit TrackerModelOptions.IsMuted
    muteCB.Checked.Add(fun _ -> TrackerModelOptions.IsMuted <- true; voice.Volume <- 0)
    muteCB.Unchecked.Add(fun _ -> TrackerModelOptions.IsMuted <- false; voice.Volume <- TrackerModelOptions.Volume)
    options2sp.Children.Add(muteCB) |> ignore
    // other settings
    let options2Grid = new Grid()
    for i = 1 to 3 do
        options2Grid.ColumnDefinitions.Add(new ColumnDefinition(Width=GridLength.Auto))
    for i = 0 to data2.Length do
        options2Grid.RowDefinitions.Add(new RowDefinition(Height=GridLength.Auto))
    let voiceTB = new TextBox(Text="Voice",IsReadOnly=true)
    voiceTB.ToolTip <- "The reminder will be spoken aloud"
    Graphics.gridAdd(options2Grid, voiceTB, 0, 0)
    let visualTB = new TextBox(Text="Visual",IsReadOnly=true)
    visualTB.ToolTip <- "The reminder will be displayed as icons in the upper right of the Timeline"
    Graphics.gridAdd(options2Grid, visualTB, 1, 0)
    let mutable row = 1
    for text,tip,bVoice,bVisual in data2 do
        if row%2=1 then
            let backgroundColor() = new DockPanel(Background=Graphics.almostBlack)
            Graphics.gridAdd(options2Grid, backgroundColor(), 0, row)
            Graphics.gridAdd(options2Grid, backgroundColor(), 1, row)
            Graphics.gridAdd(options2Grid, backgroundColor(), 2, row)
        let cbVoice = new CheckBox(HorizontalAlignment=HorizontalAlignment.Center)
        link(cbVoice, bVoice, false, fun()->())
        Graphics.gridAdd(options2Grid, cbVoice, 0, row)
        let cbVisual = new CheckBox(HorizontalAlignment=HorizontalAlignment.Center)
        link(cbVisual, bVisual, false, fun()->())
        Graphics.gridAdd(options2Grid, cbVisual, 1, row)
        let tb = new TextBox(Text=text,IsReadOnly=true, Background=Brushes.Transparent)
        tb.ToolTip <- tip
        ToolTipService.SetShowDuration(tb, 10000)
        Graphics.gridAdd(options2Grid, tb, 2, row)
        row <- row + 1

    options2sp.Children.Add(options2Grid) |> ignore
    optionsAllsp.Children.Add(new DockPanel(Width=2.,Background=Brushes.Gray)) |> ignore
    optionsAllsp.Children.Add(options2sp) |> ignore
    optionsAllsp.Children.Add(new DockPanel(Width=2.,Background=Brushes.Gray)) |> ignore

    let options3sp = new StackPanel(Orientation=Orientation.Vertical, Margin=Thickness(10.,2.,0.,0.))
    let tb = new TextBox(Text="Other", IsReadOnly=true, FontWeight=FontWeights.Bold) |> header
    options3sp.Children.Add(tb) |> ignore

    let cb = new CheckBox(Content=new TextBox(Text="Animate tile changes",IsReadOnly=true))
    cb.IsChecked <- System.Nullable.op_Implicit TrackerModelOptions.AnimateTileChanges.Value
    cb.Checked.Add(fun _ -> TrackerModelOptions.AnimateTileChanges.Value <- true)
    cb.Unchecked.Add(fun _ -> TrackerModelOptions.AnimateTileChanges.Value <- false)
    cb.ToolTip <- "When you change an overworld map spot or a dungeon room type, briefly animate the rectangle to highlight what changed"
    ToolTipService.SetShowDuration(cb, 10000)
    options3sp.Children.Add(cb) |> ignore

    let cb = new CheckBox(Content=new TextBox(Text="Save on completion",IsReadOnly=true))
    cb.IsChecked <- System.Nullable.op_Implicit TrackerModelOptions.SaveOnCompletion.Value
    cb.Checked.Add(fun _ -> TrackerModelOptions.SaveOnCompletion.Value <- true)
    cb.Unchecked.Add(fun _ -> TrackerModelOptions.SaveOnCompletion.Value <- false)
    cb.ToolTip <- "When you click Zelda to complete the seed, automatically save the full tracker state to a file"
    ToolTipService.SetShowDuration(cb, 10000)
    options3sp.Children.Add(cb) |> ignore

    let cb = new CheckBox(Content=new TextBox(Text="Snoop for seed&flags",IsReadOnly=true))
    cb.IsChecked <- System.Nullable.op_Implicit TrackerModelOptions.SnoopSeedAndFlags.Value
    cb.Checked.Add(fun _ -> TrackerModelOptions.SnoopSeedAndFlags.Value <- true; SaveAndLoad.MaybePollSeedAndFlags())
    cb.Unchecked.Add(fun _ -> TrackerModelOptions.SnoopSeedAndFlags.Value <- false)
    cb.ToolTip <- "Periodically check for other system windows (e.g. fceux)\nthat appear to have a seed and flag in the title, to\ninclude with save data and optionally display"
    ToolTipService.SetShowDuration(cb, 10000)
    options3sp.Children.Add(cb) |> ignore

    let cb = new CheckBox(Content=new TextBox(Text="Display seed&flags",IsReadOnly=true), Margin=Thickness(20.,0.,0.,0.))
    cb.IsChecked <- System.Nullable.op_Implicit TrackerModelOptions.DisplaySeedAndFlags.Value
    cb.Checked.Add(fun _ -> TrackerModelOptions.DisplaySeedAndFlags.Value <- true; SaveAndLoad.seedAndFlagsUpdated.Trigger())
    cb.Unchecked.Add(fun _ -> TrackerModelOptions.DisplaySeedAndFlags.Value <- false; SaveAndLoad.seedAndFlagsUpdated.Trigger())
    cb.ToolTip <- "Display seed & flags (if known) in the bottom corner of Notes box"
    options3sp.Children.Add(cb) |> ignore

    let cb = new CheckBox(Content=new TextBox(Text="Listen for speech",IsReadOnly=true))
    if microphoneFailedToInitialize then
        cb.IsEnabled <- false
        cb.IsChecked <- System.Nullable.op_Implicit false
        cb.ToolTip <- "Disabled (microphone was not initialized properly during startup)"
        ToolTipService.SetShowDuration(cb, 10000)
        ToolTipService.SetShowOnDisabled(cb, true)
    else
        cb.ToolTip <- "Use the microphone to listen for spoken map update commands\nExample: say 'tracker set bomb shop' while hovering an unmarked map tile"
        ToolTipService.SetShowDuration(cb, 10000)
        link(cb, TrackerModelOptions.ListenForSpeech, false, fun()->())
    options3sp.Children.Add(cb) |> ignore

    let cb = new CheckBox(Content=new TextBox(Text="Confirmation sound",IsReadOnly=true), Margin=Thickness(20.,0.,0.,0.))
    if microphoneFailedToInitialize then
        cb.IsEnabled <- false
        cb.IsChecked <- System.Nullable.op_Implicit false
        cb.ToolTip <- "Disabled (microphone was not initialized properly during startup)"
        ToolTipService.SetShowDuration(cb, 10000)
        ToolTipService.SetShowOnDisabled(cb, true)
    else
        cb.ToolTip <- "Play a confirmation sound whenever speech recognition is used to make an update to the tracker"
        ToolTipService.SetShowDuration(cb, 10000)
        link(cb, TrackerModelOptions.PlaySoundWhenUseSpeech, false, fun()->())
    options3sp.Children.Add(cb) |> ignore

(*  // this is not (yet) a fully supported feature, so don't publish it on the options menu
    let cb = new CheckBox(Content=new TextBox(Text="Require PTT",IsReadOnly=true), Margin=Thickness(20.,0.,0.,0.))
    if microphoneFailedToInitialize then
        cb.IsEnabled <- false
        cb.IsChecked <- System.Nullable.op_Implicit false
        cb.ToolTip <- "Disabled (microphone was not initialized properly during startup)"
        ToolTipService.SetShowDuration(cb, 10000)
        ToolTipService.SetShowOnDisabled(cb, true)
    elif gamepadFailedToInitialize then
        cb.IsEnabled <- false
        cb.IsChecked <- System.Nullable.op_Implicit false
        cb.ToolTip <- "Disabled (gamepad was not initialized properly during startup)"
        ToolTipService.SetShowDuration(cb, 10000)
        ToolTipService.SetShowOnDisabled(cb, true)
    else
        link(cb, TrackerModelOptions.RequirePTTForSpeech, false)
        cb.ToolTip <- "Only listen for speech when Push-To-Talk button is held (SNES gamepad left shoulder button)"
        ToolTipService.SetShowDuration(cb, 10000)
    options3sp.Children.Add(cb) |> ignore
*)

    let cb = new CheckBox(Content=new TextBox(Text="Broadcast window",IsReadOnly=true))
    cb.IsChecked <- System.Nullable.op_Implicit TrackerModelOptions.ShowBroadcastWindow.Value
    cb.Checked.Add(fun _ -> TrackerModelOptions.ShowBroadcastWindow.Value <- true; broadcastWindowOptionChanged.Trigger())
    cb.Unchecked.Add(fun _ -> TrackerModelOptions.ShowBroadcastWindow.Value <- false; broadcastWindowOptionChanged.Trigger())
    cb.ToolTip <- "Open a separate, smaller window, for stream capture.\nYou still interact with the original large window,\nbut the smaller window will focus the view on either the overworld or\nthe dungeon tabs, based on your mouse position."
    ToolTipService.SetShowDuration(cb, 10000)
    options3sp.Children.Add(cb) |> ignore

    let rb3 = new RadioButton(Content=new TextBox(Text="Full size broadcast",IsReadOnly=true), Margin=Thickness(20.,0.,0.,0.))
    let rb2 = new RadioButton(Content=new TextBox(Text="2/3 size broadcast",IsReadOnly=true), Margin=Thickness(20.,0.,0.,0.))
    let rb1 = new RadioButton(Content=new TextBox(Text="1/3 size broadcast",IsReadOnly=true), Margin=Thickness(20.,0.,0.,0.))
    match TrackerModelOptions.BroadcastWindowSize with
    | 3 -> rb3.IsChecked <- System.Nullable.op_Implicit true
    | 2 -> rb2.IsChecked <- System.Nullable.op_Implicit true
    | 1 -> rb1.IsChecked <- System.Nullable.op_Implicit true
    | _ -> failwith "impossible BroadcastWindowSize"
    rb3.Checked.Add(fun _ -> TrackerModelOptions.BroadcastWindowSize <- 3; broadcastWindowOptionChanged.Trigger())
    rb2.Checked.Add(fun _ -> TrackerModelOptions.BroadcastWindowSize <- 2; broadcastWindowOptionChanged.Trigger())
    rb1.Checked.Add(fun _ -> TrackerModelOptions.BroadcastWindowSize <- 1; broadcastWindowOptionChanged.Trigger())
    options3sp.Children.Add(rb3) |> ignore
    options3sp.Children.Add(rb2) |> ignore
    options3sp.Children.Add(rb1) |> ignore

    let cb = new CheckBox(Content=new TextBox(Text="Include overworld magnifier",IsReadOnly=true), Margin=Thickness(20.,0.,0.,0.))
    cb.IsChecked <- System.Nullable.op_Implicit TrackerModelOptions.BroadcastWindowIncludesOverworldMagnifier.Value
    cb.Checked.Add(fun _ -> TrackerModelOptions.BroadcastWindowIncludesOverworldMagnifier.Value <- true; broadcastWindowOptionChanged.Trigger())
    cb.Unchecked.Add(fun _ -> TrackerModelOptions.BroadcastWindowIncludesOverworldMagnifier.Value <- false; broadcastWindowOptionChanged.Trigger())
    cb.ToolTip <- "Whether to include the overworld magnifier when it is on-screen, which will obscure some other elements"
    ToolTipService.SetShowDuration(cb, 10000)
    options3sp.Children.Add(cb) |> ignore

    optionsAllsp.Children.Add(options3sp) |> ignore

    let total = new StackPanel(Orientation=Orientation.Vertical)
    if includePopupExplainer then
        let tb1 = new TextBox(Text="Options Menu", IsReadOnly=true, FontWeight=FontWeights.Bold, HorizontalAlignment=HorizontalAlignment.Center)
        let tb2 = new TextBox(Text="options are automatically applied and saved when dismissing this popup (by clicking outside it)", 
                                IsReadOnly=true, Margin=Thickness(0.,0.,0.,6.), HorizontalAlignment=HorizontalAlignment.Center)
        total.Children.Add(tb1) |> ignore
        total.Children.Add(tb2) |> ignore
        total.Children.Add(new DockPanel(Height=2.,Background=Brushes.Gray)) |> ignore

    total.Children.Add(optionsAllsp) |> ignore

    all.Child <- total
    all