module Reminders

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

open OverworldItemGridUI
open CustomComboBoxes.GlobalFlag

let voice = OptionsMenu.voice
let upcb(bmp) : FrameworkElement = upcast Graphics.BMPtoImage bmp
let mutable reminderAgent = MailboxProcessor.Start(fun _ -> async{return ()})
let reminderLogSP = new StackPanel(Orientation=Orientation.Vertical)
let mutable anyReminderGrayedOut = false
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
            reminderAgent.Post(text, category, TrackerModelOptions.IsMuted, shouldRemindVoice, icons, shouldRemindVisual, visualUpdateToSynchronizeWithReminder)
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
            popupIsActive <- true
            let wh = new System.Threading.ManualResetEvent(false)
            let sp = new StackPanel(Orientation=Orientation.Vertical)
            sp.Children.Add(new TextBox(Text="Recent reminders log", Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=20., IsHitTestVisible=false, 
                                            BorderThickness=Thickness(0.), Margin=Thickness(6.,0.,6.,0.), HorizontalAlignment=HorizontalAlignment.Center)) |> ignore
            if anyReminderGrayedOut then
                sp.Children.Add(new TextBox(Text="(Grayed-out reminders are those you disabled in the Options Menu)", Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., 
                                                IsHitTestVisible=false, BorderThickness=Thickness(0.), Margin=Thickness(6.,0.,6.,0.), HorizontalAlignment=HorizontalAlignment.Center)) |> ignore
            sp.Children.Add(new DockPanel(Background=Brushes.Gray, Margin=Thickness(6.,3.,6.,0.), Height=3.)) |> ignore
            let reminderView = new ScrollViewer(Content=reminderLogSP, VerticalScrollBarVisibility=ScrollBarVisibility.Auto, MaxHeight=340., Margin=Thickness(3.))
            sp.Children.Add(reminderView) |> ignore
            let b = new Border(BorderBrush=Brushes.Gray, Background=Brushes.Black, BorderThickness=Thickness(3.), Child=sp)
            async {
                do! CustomComboBoxes.DoModal(cm, wh, 10., 10., b)
                reminderView.Content <- null // deparent log for future reuse
                popupIsActive <- false
            } |> Async.StartImmediate
        )
    reminderDisplayInnerBorder.MouseEnter.Add(fun _ -> reminderDisplayInnerBorder.BorderBrush <- Brushes.Cyan)
    reminderDisplayInnerBorder.MouseLeave.Add(fun _ -> reminderDisplayInnerBorder.BorderBrush <- Brushes.Lime)
    DockPanel.SetDock(reminderDisplayInnerBorder, Dock.Top)
    reminderDisplayOuterDockPanel.Children.Add(reminderDisplayInnerBorder) |> ignore
    reminderAgent <- MailboxProcessor.Start(fun inbox -> 
        let rec messageLoop() = async {
            let! (text,category,wasMuted,shouldRemindVoice,icons,shouldRemindVisual,visualUpdateToSynchronizeWithReminder) = inbox.Receive()
            do! Async.SwitchToContext(ctxt)
            let sp = new StackPanel(Orientation=Orientation.Horizontal, Background=Brushes.Black, Margin=Thickness(6.))
            for i in icons do
                i.Margin <- Thickness(3.)
                sp.Children.Add(i) |> ignore
            // since a lot of reminders can queue up, ensure muting applies to both Post time and Play time, as it's frustrating to hit 'Disable all' and still hear more play
            let muted = (wasMuted || TrackerModelOptions.IsMuted)   
            if not(muted) then
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
                if shouldRemindVoice && voice.Volume <> 0 then
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
            sp.ToolTip <- sprintf "%s\n(%s)" text category.DisplayName
            let hasAny = reminderLogSP.Children.Count > 0
            let grayOut = muted || (not shouldRemindVisual && not shouldRemindVoice)
            if grayOut then
                anyReminderGrayedOut <- true
            reminderLogSP.Children.Insert(0, new Border(Child=sp, BorderBrush=Brushes.Gray, BorderThickness=Thickness(0.,0.,0.,if hasAny then 1. else 0.), Opacity=if grayOut then 0.5 else 1.0))
            return! messageLoop()
            }
        messageLoop()
        )
    reminderDisplayOuterDockPanel

////////////////////////////////////////////////////

open OverworldMapTileCustomization

let RemindOverworldOverwrites(i, j, originalState, currentState, spokenOWTiles:_[], highlightAsyncComp) =
    // remind destructive changes
    if originalState=TrackerModel.MapSquareChoiceDomainHelper.UNKNOWN_SECRET && 
        (currentState=TrackerModel.MapSquareChoiceDomainHelper.LARGE_SECRET ||
            currentState=TrackerModel.MapSquareChoiceDomainHelper.MEDIUM_SECRET || currentState=TrackerModel.MapSquareChoiceDomainHelper.SMALL_SECRET) then
        () // do nothing, this is a 'destructive' change that is perfectly reasonable and doesn't need a warning reminder
    else
        let row = (i+1)
        let col = (char (int 'A' + j)) 
        let changedCoords = sprintf "Changed %c%d:" col row
        let desc(state) = if state = -1 then "Unmarked" else spokenOWTiles.[state]
        let bmp(state) = 
            if state = -1 then Graphics.unmarkedBmp
            elif state = TrackerModel.MapSquareChoiceDomainHelper.DARK_X then Graphics.dontCareBmp
            else MapStateProxy(state).DefaultInteriorBmp()
        if currentState <> originalState then  // don't remind e.g. change from bomb shop to bomb+key shop
            SendReminderImpl(TrackerModel.ReminderCategory.OverworldOverwrites, sprintf "You changed %c %d from %s to %s" col row (desc originalState) (desc currentState), 
                [ReminderTextBox(changedCoords); upcb(bmp(originalState)); upcb(Graphics.iconRightArrow_bmp); upcb(bmp(currentState))], Some(highlightAsyncComp))

let RemindSword2() =
    let n = TrackerModel.sword2Box.CellCurrent()
    if n = -1 then
        SendReminder(TrackerModel.ReminderCategory.SwordHearts, "Consider getting the white sword item", 
                        [upcb(Graphics.iconRightArrow_bmp); upcb(MapStateProxy(14).DefaultInteriorBmp())])
    else
        SendReminder(TrackerModel.ReminderCategory.SwordHearts, sprintf "Consider getting the %s from the white sword cave" (TrackerModel.ITEMS.AsPronounceString(n)),
                        [upcb(Graphics.iconRightArrow_bmp); upcb(MapStateProxy(14).DefaultInteriorBmp()); 
                            upcb(CustomComboBoxes.boxCurrentBMP(TrackerModel.sword2Box.CellCurrent(), None))])

let RemindFoundDungeonCount(n) =
    let icons = [upcb(Graphics.genericDungeonInterior_bmp); ReminderTextBox(sprintf"%d/9"n)]
    if n = 1 then
        SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "You have located one dungeon", icons) 
    elif n = 9 then
        if TrackerModel.mapStateSummary.Sword2Location = TrackerModel.NOTFOUND   // if sword2 cave not found on overworld map,
                && TrackerModel.sword2Box.CellCurrent() = -1 then                // and the tracker box is still empty (some people might not mark map, but will mark item)
            let greyedSword2 = upcb(Graphics.greyscale(Graphics.theInteriorBmpTable.[14].[0]))
            SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "Congratulations, you have located all 9 dungeons, but the white sword cave is still missing", 
                            [yield! icons; yield greyedSword2])
        else
            SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "Congratulations, you have located all 9 dungeons", [yield! icons; yield upcb(Graphics.iconCheckMark_bmp)])
    else
        SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, sprintf "You have located %d dungeons" n, icons) 

let RemindTriforceCount(n, asyncBrieflyHighlightAnOverworldLocation) =
    let icons = [upcb(Graphics.fullOrangeTriforce_bmp); ReminderTextBox(sprintf"%d/8"n)]
    if n = 1 then
        SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "You now have one triforce", icons)
    else
        SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, sprintf "You now have %d triforces" n, [yield! icons; if n=8 then yield upcb(Graphics.iconCheckMark_bmp)])
    if n = 8 && not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) then
        SendReminderImpl(TrackerModel.ReminderCategory.DungeonFeedback, "Consider the magical sword before dungeon nine", [upcb(Graphics.iconRightArrow_bmp); upcb(Graphics.magical_sword_bmp)],
                            Some(asyncBrieflyHighlightAnOverworldLocation(TrackerModel.mapStateSummary.Sword3Location)))
    if n = 8 && (TrackerModel.mapStateSummary.DungeonLocations.[8] <> TrackerModel.NOTFOUND) then
        SendReminderImpl(TrackerModel.ReminderCategory.DungeonFeedback, "Dungeon nine is open", [upcb(Graphics.iconRightArrow_bmp); upcb(MapStateProxy(8).DefaultInteriorBmp())],
                            Some(asyncBrieflyHighlightAnOverworldLocation(TrackerModel.mapStateSummary.DungeonLocations.[8])))

type PeriodicReminders() =
    let recentlyAgo = TimeSpan.FromMinutes(3.0)
    let ladderTime, recorderTime, powerBraceletTime, boomstickTime = 
        new TrackerModel.LastChangedTime(recentlyAgo), new TrackerModel.LastChangedTime(recentlyAgo), new TrackerModel.LastChangedTime(recentlyAgo), new TrackerModel.LastChangedTime(recentlyAgo)
    let mutable owPreviouslyAnnouncedWhistleSpotsRemain, owPreviouslyAnnouncedPowerBraceletSpotsRemain, owPreviouslyAnnounceDoorRepairCount = 0, 0, 0
    member this.Check() =
        // remind ladder
        if (DateTime.Now - ladderTime.Time).Minutes > 2 then  // every 3 mins
            if TrackerModel.playerComputedStateSummary.HaveLadder then
                if not(TrackerModel.playerComputedStateSummary.HaveCoastItem) then
                    let n = TrackerModel.ladderBox.CellCurrent()
                    if n = -1 then
                        SendReminder(TrackerModel.ReminderCategory.CoastItem, "Get the coast item with the ladder", [upcb(Graphics.ladder_bmp); upcb(Graphics.iconRightArrow_bmp)])
                    else
                        if n = TrackerModel.ITEMS.WHITESWORD && TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value() then
                            ()   // silly to ask to grab white sword if already have mags (though note this reminder could be useful in swordless when both are bomb upgrades)
                        else
                            SendReminder(TrackerModel.ReminderCategory.CoastItem, sprintf "Get the %s off the coast" (TrackerModel.ITEMS.AsPronounceString(n)),
                                            [upcb(Graphics.ladder_bmp); upcb(Graphics.iconRightArrow_bmp); upcb(CustomComboBoxes.boxCurrentBMP(TrackerModel.ladderBox.CellCurrent(), None))])
                    ladderTime.SetNow()
        // remind whistle spots
        if (DateTime.Now - recorderTime.Time).Minutes > 4 then  // every 5 mins
            if TrackerModel.playerComputedStateSummary.HaveRecorder then
                let owWhistleSpotsRemain = TrackerModel.mapStateSummary.OwWhistleSpotsRemain.Count
                if owWhistleSpotsRemain >= owPreviouslyAnnouncedWhistleSpotsRemain && owWhistleSpotsRemain > 0 then
                    let icons = [upcb(Graphics.recorder_bmp); ReminderTextBox(owWhistleSpotsRemain.ToString())]
                    if owWhistleSpotsRemain = 1 then
                        SendReminder(TrackerModel.ReminderCategory.RecorderPBSpotsAndBoomstickBook, "There is one recorder spot", icons)
                    else
                        SendReminder(TrackerModel.ReminderCategory.RecorderPBSpotsAndBoomstickBook, sprintf "There are %d recorder spots" owWhistleSpotsRemain, icons)
                recorderTime.SetNow()
                owPreviouslyAnnouncedWhistleSpotsRemain <- owWhistleSpotsRemain
        // remind power bracelet spots
        if (DateTime.Now - powerBraceletTime.Time).Minutes > 4 then  // every 5 mins
            if TrackerModel.playerComputedStateSummary.HavePowerBracelet then
                if TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain >= owPreviouslyAnnouncedPowerBraceletSpotsRemain && TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain > 0 then
                    let icons = [upcb(Graphics.power_bracelet_bmp); ReminderTextBox(TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain.ToString())]
                    if TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain = 1 then
                        SendReminder(TrackerModel.ReminderCategory.RecorderPBSpotsAndBoomstickBook, "There is one power bracelet spot", icons)
                    else
                        SendReminder(TrackerModel.ReminderCategory.RecorderPBSpotsAndBoomstickBook, sprintf "There are %d power bracelet spots" TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain, icons)
                powerBraceletTime.SetNow()
                owPreviouslyAnnouncedPowerBraceletSpotsRemain <- TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain
        // remind boomstick book
        if (DateTime.Now - boomstickTime.Time).Minutes > 4 then  // every 5 mins
            if TrackerModel.playerComputedStateSummary.HaveWand && not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Value()) then
                let mutable boomShopFound = false
                for i = 0 to 15 do
                    for j = 0 to 7 do
                        let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                        if TrackerModel.MapSquareChoiceDomainHelper.IsItem(cur) then
                            if cur = TrackerModel.MapSquareChoiceDomainHelper.BOOK || 
                                    (TrackerModel.getOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP) = TrackerModel.MapSquareChoiceDomainHelper.ToItem(TrackerModel.MapSquareChoiceDomainHelper.BOOK)) then
                                boomShopFound <- true
                if boomShopFound then
                    SendReminder(TrackerModel.ReminderCategory.RecorderPBSpotsAndBoomstickBook, "Consider buying the boomstick book", [upcb(Graphics.iconRightArrow_bmp); upcb(Graphics.boom_book_bmp)])
                    boomstickTime.SetNow()
        // remind door repair spots
        if TrackerModel.mapSquareChoiceDomain.NumUses(TrackerModel.MapSquareChoiceDomainHelper.DOOR_REPAIR_CHARGE) > owPreviouslyAnnounceDoorRepairCount then
            let n = TrackerModel.mapSquareChoiceDomain.NumUses(TrackerModel.MapSquareChoiceDomainHelper.DOOR_REPAIR_CHARGE)
            let max = TrackerModel.mapSquareChoiceDomain.MaxUses(TrackerModel.MapSquareChoiceDomainHelper.DOOR_REPAIR_CHARGE)
            let icons = [upcb(Graphics.theInteriorBmpTable.[TrackerModel.MapSquareChoiceDomainHelper.DOOR_REPAIR_CHARGE].[0]); ReminderTextBox(sprintf "%d/%d" n max)]
            SendReminder(TrackerModel.ReminderCategory.DoorRepair, sprintf "You found %s%d of %d door repairs" (if n=max then "all " else "") n max, icons)
            owPreviouslyAnnounceDoorRepairCount <- n
