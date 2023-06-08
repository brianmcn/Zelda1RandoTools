module Reminders

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

open OverworldItemGridUI

let voice = OptionsMenu.voice
let upcb(bmp) : FrameworkElement = upcast Graphics.BMPtoImage bmp
let mutable reminderAgent = MailboxProcessor.Start(fun _ -> async{return ()})
let reminderLogSP = new StackPanel(Orientation=Orientation.Vertical)
let SendReminderImpl(category, text:string, icons:seq<FrameworkElement>, visualUpdateToSynchronizeWithReminder) =
    if not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Value()) then  // if won the game, quit sending reminders
        let shouldRemindVoice, shouldRemindVisual =
            match category with
            | TrackerModel.ReminderCategory.Blockers ->        TrackerModelOptions.VoiceReminders.Blockers.Value,        TrackerModelOptions.VisualReminders.Blockers.Value
            | TrackerModel.ReminderCategory.CoastItem ->       TrackerModelOptions.VoiceReminders.CoastItem.Value,       TrackerModelOptions.VisualReminders.CoastItem.Value
            | TrackerModel.ReminderCategory.DungeonFeedback -> TrackerModelOptions.VoiceReminders.DungeonFeedback.Value, TrackerModelOptions.VisualReminders.DungeonFeedback.Value
            | TrackerModel.ReminderCategory.HaveKeyLadder ->   TrackerModelOptions.VoiceReminders.HaveKeyLadder.Value,   TrackerModelOptions.VisualReminders.HaveKeyLadder.Value
            | TrackerModel.ReminderCategory.RecorderPBSpotsAndBoomstickBook -> TrackerModelOptions.VoiceReminders.RecorderPBSpotsAndBoomstickBook.Value, TrackerModelOptions.VisualReminders.RecorderPBSpotsAndBoomstickBook.Value
            | TrackerModel.ReminderCategory.SwordHearts ->     TrackerModelOptions.VoiceReminders.SwordHearts.Value,     TrackerModelOptions.VisualReminders.SwordHearts.Value
            | TrackerModel.ReminderCategory.DoorRepair ->      TrackerModelOptions.VoiceReminders.DoorRepair.Value,      TrackerModelOptions.VisualReminders.DoorRepair.Value
            | TrackerModel.ReminderCategory.OverworldOverwrites -> TrackerModelOptions.VoiceReminders.OverworldOverwrites.Value, TrackerModelOptions.VisualReminders.OverworldOverwrites.Value
        if not(Timeline.isCurrentlyLoadingASave) then 
            reminderAgent.Post(text, shouldRemindVoice, icons, shouldRemindVisual, visualUpdateToSynchronizeWithReminder)
let SendReminder(category, text:string, icons:seq<FrameworkElement>) =
    SendReminderImpl(category, text, icons, None)
let ReminderTextBox(txt) : FrameworkElement = 
    upcast new TextBox(Text=txt, Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=20., FontWeight=FontWeights.Bold, IsHitTestVisible=false,
        VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), TextAlignment=TextAlignment.Center)

let SetupReminderDisplayAndProcessing(cm) =
    let ctxt = System.Threading.SynchronizationContext.Current
    let reminderDisplayOuterDockPanel = new DockPanel(Width=OMTW*16., Height=THRU_TIMELINE_H-START_TIMELINE_H, LastChildFill=false)
    let reminderLogTB = new TextBox(Text="log", Foreground=Brushes.Orange, FontSize=10., Background=Brushes.Black, IsHitTestVisible=false, BorderThickness=Thickness(0.), Margin=Thickness(0.,0.,0.,2.))
    let reminderDisplayInnerBorder = new Border(Child=reminderLogTB, BorderThickness=Thickness(3.), BorderBrush=Brushes.Lime, Background=Brushes.Black, HorizontalAlignment=HorizontalAlignment.Right)
    DockPanel.SetDock(reminderDisplayInnerBorder, Dock.Right)
    reminderDisplayInnerBorder.MouseDown.Add(fun _ ->
        if not popupIsActive then
            let wh = new System.Threading.ManualResetEvent(false)
            let sp = new StackPanel(Orientation=Orientation.Vertical)
            sp.Children.Add(new TextBox(Text="Recent reminders log", Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=20., IsHitTestVisible=false, 
                                            BorderThickness=Thickness(0.), Margin=Thickness(6.,0.,6.,0.), HorizontalAlignment=HorizontalAlignment.Center)) |> ignore
            sp.Children.Add(new DockPanel(Background=Brushes.Gray, Margin=Thickness(6.,3.,6.,0.), Height=3.)) |> ignore
            let reminderView = new ScrollViewer(Content=reminderLogSP, VerticalScrollBarVisibility=ScrollBarVisibility.Auto, MaxHeight=360., Margin=Thickness(3.))
            sp.Children.Add(reminderView) |> ignore
            let b = new Border(BorderBrush=Brushes.Gray, Background=Brushes.Black, BorderThickness=Thickness(3.), Child=sp)
            async {
                do! CustomComboBoxes.DoModal(cm, wh, 10., 10., b)
                reminderView.Content <- null // deparent log for future reuse
            } |> Async.StartImmediate
        )
    reminderDisplayInnerBorder.MouseEnter.Add(fun _ -> reminderDisplayInnerBorder.BorderBrush <- Brushes.Cyan)
    reminderDisplayInnerBorder.MouseLeave.Add(fun _ -> reminderDisplayInnerBorder.BorderBrush <- Brushes.Lime)
    DockPanel.SetDock(reminderDisplayInnerBorder, Dock.Top)
    reminderDisplayOuterDockPanel.Children.Add(reminderDisplayInnerBorder) |> ignore
    reminderAgent <- MailboxProcessor.Start(fun inbox -> 
        let rec messageLoop() = async {
            let! (text,shouldRemindVoice,icons,shouldRemindVisual,visualUpdateToSynchronizeWithReminder) = inbox.Receive()
            do! Async.SwitchToContext(ctxt)
            let sp = new StackPanel(Orientation=Orientation.Horizontal, Background=Brushes.Black, Margin=Thickness(6.))
            for i in icons do
                i.Margin <- Thickness(3.)
                sp.Children.Add(i) |> ignore
            if not(TrackerModelOptions.IsMuted) then
                let iconCount = sp.Children.Count
                if shouldRemindVisual then
                    Graphics.PlaySoundForReminder()
                    reminderDisplayInnerBorder.Child <- sp
                match visualUpdateToSynchronizeWithReminder with
                | None -> ()
                | Some vu -> Async.StartImmediate vu
                do! Async.SwitchToThreadPool()
                if shouldRemindVisual then
                    do! Async.Sleep(200) // give reminder clink sound time to play
                let startSpeakTime = DateTime.Now
                if shouldRemindVoice then
                    voice.Speak(text) 
                if shouldRemindVisual then
                    let minimumDuration = TimeSpan.FromSeconds(max 3 iconCount |> float)  // ensure at least 3s, and at least 1s per icon
                    let elapsed = DateTime.Now - startSpeakTime
                    if elapsed < minimumDuration then
                        let ms = (minimumDuration - elapsed).TotalMilliseconds |> int
                        do! Async.Sleep(ms)   // ensure ui displayed a minimum time
                do! Async.SwitchToContext(ctxt)
            // once reminder no longer displayed, log it.
            let timeString = OverworldItemGridUI.hmsTimeTextBox.Text
            reminderDisplayInnerBorder.Child <- reminderLogTB   // also deparents sp
            sp.Margin <- Thickness(0.)
            sp.Children.Insert(0, new DockPanel(Background=Brushes.Gray, Width=18., Height=3., Margin=Thickness(6.,0.,6.,0.), VerticalAlignment=VerticalAlignment.Center))
            sp.Children.Insert(0, ReminderTextBox(timeString))
            sp.ToolTip <- text
            let hasAny = reminderLogSP.Children.Count > 0
            reminderLogSP.Children.Insert(0, new Border(Child=sp, BorderBrush=Brushes.Gray, BorderThickness=Thickness(0.,0.,0.,if hasAny then 1. else 0.)))
            return! messageLoop()
            }
        messageLoop()
        )
    reminderDisplayOuterDockPanel
