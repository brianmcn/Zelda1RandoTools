module WPFUI

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

open OverworldMapTileCustomization
open HotKeys.MyKey
open OverworldItemGridUI

module OW_ITEM_GRID_LOCATIONS = OverworldMapTileCustomization.OW_ITEM_GRID_LOCATIONS

let voice = OptionsMenu.voice

let upcb(bmp) : FrameworkElement = upcast Graphics.BMPtoImage bmp
let mutable silenceAllRemindersDuringCurrentLoad = false
let mutable reminderAgent = MailboxProcessor.Start(fun _ -> async{return ()})
let SendReminder(category, text:string, icons:seq<FrameworkElement>) =
    if not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Value()) then  // if won the game, quit sending reminders
        let shouldRemindVoice, shouldRemindVisual =
            match category with
            | TrackerModel.ReminderCategory.Blockers ->        TrackerModel.Options.VoiceReminders.Blockers.Value,        TrackerModel.Options.VisualReminders.Blockers.Value
            | TrackerModel.ReminderCategory.CoastItem ->       TrackerModel.Options.VoiceReminders.CoastItem.Value,       TrackerModel.Options.VisualReminders.CoastItem.Value
            | TrackerModel.ReminderCategory.DungeonFeedback -> TrackerModel.Options.VoiceReminders.DungeonFeedback.Value, TrackerModel.Options.VisualReminders.DungeonFeedback.Value
            | TrackerModel.ReminderCategory.HaveKeyLadder ->   TrackerModel.Options.VoiceReminders.HaveKeyLadder.Value,   TrackerModel.Options.VisualReminders.HaveKeyLadder.Value
            | TrackerModel.ReminderCategory.RecorderPBSpotsAndBoomstickBook -> TrackerModel.Options.VoiceReminders.RecorderPBSpotsAndBoomstickBook.Value, TrackerModel.Options.VisualReminders.RecorderPBSpotsAndBoomstickBook.Value
            | TrackerModel.ReminderCategory.SwordHearts ->     TrackerModel.Options.VoiceReminders.SwordHearts.Value,     TrackerModel.Options.VisualReminders.SwordHearts.Value
        if not(silenceAllRemindersDuringCurrentLoad) && (shouldRemindVoice || shouldRemindVisual) then 
            reminderAgent.Post(text, shouldRemindVoice, icons, shouldRemindVisual)
let ReminderTextBox(txt) : FrameworkElement = 
    upcast new TextBox(Text=txt, Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=20., FontWeight=FontWeights.Bold, IsHitTestVisible=false,
        VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), TextAlignment=TextAlignment.Center)

let routeDrawingCanvas = new Canvas(Width=16.*OMTW, Height=float(8*11*3))

let makeGhostBuster() =  // for marking off the third box of completed 2-item dungeons in Hidden Dungeon Numbers
    let c = new Canvas(Width=30., Height=30., Opacity=0.0, IsHitTestVisible=false)
    let circle = new Shapes.Ellipse(Width=30., Height=30., StrokeThickness=3., Stroke=Brushes.Gray)
    let slash = new Shapes.Line(X1=30.*(1.-0.707), X2=30.*0.707, Y1=30.*0.707, Y2=30.*(1.-0.707), StrokeThickness=3., Stroke=Brushes.Gray)
    canvasAdd(c, circle, 0., 0.)
    canvasAdd(c, slash, 0., 0.)
    c
let mainTrackerGhostbusters = Array.init 8 (fun _ -> makeGhostBuster())
let updateGhostBusters() =
    if TrackerModel.IsHiddenDungeonNumbers() then
        for i = 0 to 7 do
            let lc = TrackerModel.GetDungeon(i).LabelChar
            let twoItemDungeons = if TrackerModel.Options.IsSecondQuestDungeons.Value then "123567" else "234567"
            if twoItemDungeons.Contains(lc.ToString()) then
                mainTrackerGhostbusters.[i].Opacity <- 1.0
            else
                mainTrackerGhostbusters.[i].Opacity <- 0.0
let triforceInnerCanvases = Array.zeroCreate 8
let mainTrackerCanvases : Canvas[,] = Array2D.zeroCreate 9 5
let mainTrackerCanvasShaders : Canvas[,] = Array2D.init 8 5 (fun _i j -> new Canvas(Width=30., Height=30., Background=Brushes.Black, Opacity=(if j=1 then 0.4 else 0.3), IsHitTestVisible=false))
let currentMaxHeartsTextBox = new TextBox(Width=100., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "Max Hearts: %d" TrackerModel.playerComputedStateSummary.PlayerHearts)
let owRemainingScreensTextBox = new TextBox(Width=110., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "%d OW spots left" TrackerModel.mapStateSummary.OwSpotsRemain)
let owGettableScreensTextBox = new TextBox(Width=80., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "%d gettable" TrackerModel.mapStateSummary.OwGettableLocations.Count)
let owGettableScreensCheckBox = new CheckBox(Content = owGettableScreensTextBox, IsChecked=true)
let ensureRespectingOwGettableScreensCheckBox() =
    if owGettableScreensCheckBox.IsChecked.HasValue && owGettableScreensCheckBox.IsChecked.Value then
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, 
                    TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), Point(0.,0.), 0, 0, false, true, 0, OverworldRouteDrawing.All)

type RouteDestination = LinkRouting.RouteDestination

let drawRoutesTo(routeDestinationOption, routeDrawingCanvas, point, i, j, drawRouteMarks, maxBoldGYR, maxPaleGYR) =
    let maxPaleGYR = if owGettableScreensCheckBox.IsChecked.HasValue && owGettableScreensCheckBox.IsChecked.Value then OverworldRouteDrawing.All else maxPaleGYR
    let unmarked = TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1)
    let interestingButInaccesible = ResizeArray()
    let owTargetworthySpots = Array2D.zeroCreate 16 8
    let processHint(hz:TrackerModel.HintZone,couldBeLetterDungeon) =
        for i = 0 to 15 do
            for j = 0 to 7 do
                if OverworldData.owMapZone.[j].[i] = hz.AsDataChar() then
                    let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                    let cbld = (couldBeLetterDungeon && cur>=0 && cur<=7 && TrackerModel.GetDungeon(cur).LabelChar='?')
                    if cur = -1 || cbld then
                        if TrackerModel.mapStateSummary.OwGettableLocations.Contains(i,j) || cbld then
                            owTargetworthySpots.[i,j] <- true
                            unmarked.[i,j] <- true  // for cbld case
                        else
                            interestingButInaccesible.Add(i,j)
    match routeDestinationOption with
    | Some(RouteDestination.SHOP(targetItem)) ->
        for x = 0 to 15 do
            for y = 0 to 7 do
                let msp = MapStateProxy(TrackerModel.overworldMapMarks.[x,y].Current())
                if msp.State = targetItem || (msp.IsThreeItemShop && TrackerModel.getOverworldMapExtraData(x,y,TrackerModel.MapSquareChoiceDomainHelper.SHOP) = TrackerModel.MapSquareChoiceDomainHelper.ToItem(targetItem)) then
                    owTargetworthySpots.[x,y] <- true
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, owTargetworthySpots, unmarked, point, i, j, true, false, maxBoldGYR, maxPaleGYR)
    | Some(RouteDestination.OW_MAP(x,y)) ->
        owTargetworthySpots.[x,y] <- true
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, owTargetworthySpots, unmarked, point, i, j, true, false, maxBoldGYR, maxPaleGYR)
    | Some(RouteDestination.HINTZONE(hz,couldBeLetterDungeon)) ->
        processHint(hz,couldBeLetterDungeon)
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, owTargetworthySpots, unmarked, point, i, j, true, false, OverworldRouteDrawing.All, 0)
    | Some(RouteDestination.UNMARKEDINSTANCEFUNC(f)) ->
        for x = 0 to 15 do
            for y = 0 to 7 do
                if unmarked.[x,y] && f(x,y) then
                    owTargetworthySpots.[x,y] <- true
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, owTargetworthySpots, unmarked, point, i, j, true, false, OverworldRouteDrawing.MaxGYR, OverworldRouteDrawing.All)
    | None ->
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, unmarked, point, i, j, drawRouteMarks, true, maxBoldGYR, maxPaleGYR)
    for i,j in interestingButInaccesible do
        let rect = new Graphics.TileHighlightRectangle()
        rect.MakeRed()
        for s in rect.Shapes do
            Graphics.canvasAdd(routeDrawingCanvas, s, OMTW*float(i), float(j*11*3))


let resetTimerEvent = new Event<unit>()
let mutable currentlyMousedOWX, currentlyMousedOWY = -1, -1
let mutable notesTextBox = null : TextBox
let mutable timeTextBox = null : TextBox
let H = 30
let RIGHT_COL = 440.
let WEBCAM_LINE = OMTW*16.-200.  // height of upper area is 150, so 200 wide is 4x3 box in upper right; timer and other controls here could be obscured
let resizeMapTileImage = OverworldMapTileCustomization.resizeMapTileImage

let makeAll(mainWindow:Window, cm:CustomComboBoxes.CanvasManager, owMapNum, heartShuffle, kind, loadData:DungeonSaveAndLoad.AllData option, 
                showProgress, speechRecognitionInstance:SpeechRecognition.SpeechRecognitionInstance) = async {
    let refocusMainWindow() =   // keep hotkeys working
        async {
            let ctxt = System.Threading.SynchronizationContext.Current
            do! Async.Sleep(500)  // give new window time to pop up
            do! Async.SwitchToContext(ctxt)
            mainWindow.Focus() |> ignore
        } |> Async.StartImmediate
    // initialize based on startup parameters
    let owMapBMPs, isMixed, owInstance =
        match owMapNum, loadData with
        | 0, _ -> Graphics.overworldMapBMPs(0), false, new OverworldData.OverworldInstance(OverworldData.FIRST)
        | 1, _ -> Graphics.overworldMapBMPs(1), false, new OverworldData.OverworldInstance(OverworldData.SECOND)
        | 2, _ -> Graphics.overworldMapBMPs(2), true,  new OverworldData.OverworldInstance(OverworldData.MIXED_FIRST)
        | 3, _ -> Graphics.overworldMapBMPs(3), true,  new OverworldData.OverworldInstance(OverworldData.MIXED_SECOND)
        | 999, Some(data) -> 
            match data.Overworld.Quest with
            | 0 -> Graphics.overworldMapBMPs(0), false, new OverworldData.OverworldInstance(OverworldData.FIRST)
            | 1 -> Graphics.overworldMapBMPs(1), false, new OverworldData.OverworldInstance(OverworldData.SECOND)
            | 2 -> Graphics.overworldMapBMPs(2), true,  new OverworldData.OverworldInstance(OverworldData.MIXED_FIRST)
            | 3 -> Graphics.overworldMapBMPs(3), true,  new OverworldData.OverworldInstance(OverworldData.MIXED_SECOND)
            | _ -> failwith "bad load data at root.Overworld.Quest"
        | _ -> failwith "bad/unsupported (owMapNum,loadData)"
    TrackerModel.initializeAll(owInstance, kind)
    if not heartShuffle then
        for i = 0 to 7 do
            TrackerModel.GetDungeon(i).Boxes.[0].Set(TrackerModel.ITEMS.HEARTCONTAINER, TrackerModel.PlayerHas.NO)

    // make the entire UI
    let timelineItems = ResizeArray()

    let isSpecificRouteTargetActive,currentRouteTarget,eliminateCurrentRouteTarget,changeCurrentRouteTarget =
        let routeTargetLastClickedTime = new TrackerModel.LastChangedTime(TimeSpan.FromMinutes(10.))
        let mutable routeTarget = None
        let isSpecificRouteTargetActive() = DateTime.Now - routeTargetLastClickedTime.Time < TimeSpan.FromSeconds(10.)
        let currentRouteTarget() =
            if isSpecificRouteTargetActive() then
                routeTarget
            else
                None
        let eliminateCurrentRouteTarget() =
            routeTarget <- None
            routeTargetLastClickedTime.SetAgo(TimeSpan.FromMinutes(10.))
        let changeCurrentRouteTarget(newTarget) =
            routeTargetLastClickedTime.SetNow()
            routeTarget <- Some(newTarget)
        isSpecificRouteTargetActive,currentRouteTarget,eliminateCurrentRouteTarget,changeCurrentRouteTarget
    
    let doUIUpdateEvent = new Event<unit>()

    let appMainCanvas = cm.AppMainCanvas
    let mainTracker = makeGrid(9, 5, H, H)
    canvasAdd(appMainCanvas, mainTracker, 0., 0.)

    // numbered triforce display - the extra row of triforce in IsHiddenDungeonNumbers
    let updateNumberedTriforceDisplayImpl(c:Canvas,i) =
        let level = i+1
        let levelLabel = char(int '0' + level)
        let mutable index = -1
        for j = 0 to 7 do
            if TrackerModel.GetDungeon(j).LabelChar = levelLabel then
                index <- j
        let mutable found,hasTriforce = false,false
        if index <> -1 then
            found <- TrackerModel.GetDungeon(index).HasBeenLocated()
            hasTriforce <- TrackerModel.GetDungeon(index).PlayerHasTriforce() 
        hasTriforce <- hasTriforce || TrackerModel.startingItemsAndExtras.HDNStartingTriforcePieces.[i].Value()
        let hasHint = not(found) && TrackerModel.GetLevelHint(i)<>TrackerModel.HintZone.UNKNOWN
        c.Children.Clear()
        if hasHint then
            c.Children.Add(makeHintHighlight(30.)) |> ignore
        if not hasTriforce then
            if not found then
                c.Children.Add(Graphics.BMPtoImage Graphics.emptyUnfoundNumberedTriforce_bmps.[i]) |> ignore
            else
                c.Children.Add(Graphics.BMPtoImage Graphics.emptyFoundNumberedTriforce_bmps.[i]) |> ignore
        else
            if not found then
                c.Children.Add(Graphics.BMPtoImage Graphics.fullNumberedUnfoundTriforce_bmps.[i]) |> ignore
            else
                c.Children.Add(Graphics.BMPtoImage Graphics.fullNumberedFoundTriforce_bmps.[i]) |> ignore
    let updateNumberedTriforceDisplayIfItExists =
        if TrackerModel.IsHiddenDungeonNumbers() then
            let numberedTriforceCanvases = Array.init 8 (fun _ -> new Canvas(Width=30., Height=30.))
            for i = 0 to 7 do
                let c = numberedTriforceCanvases.[i]
                canvasAdd(appMainCanvas, c, OW_ITEM_GRID_LOCATIONS.OFFSET+30.*float i, 0.)
                c.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.DungeonNumber i))
                c.MouseLeave.Add(fun _ -> hideLocator())
            let update() =
                for i = 0 to 7 do
                    updateNumberedTriforceDisplayImpl(numberedTriforceCanvases.[i], i)
            update
        else
            fun () -> ()
    updateNumberedTriforceDisplayIfItExists()
    // triforce
    for i = 0 to 7 do
        if TrackerModel.IsHiddenDungeonNumbers() then
            // triforce dungeon color
            let colorCanvas = new Canvas(Width=28., Height=28., Background=Brushes.Black)
            //mainTrackerCanvases.[i,0] <- colorCanvas
            let colorButton = new Button(Width=30., Height=30., BorderThickness=Thickness(1.), Margin=Thickness(0.), Padding=Thickness(0.), BorderBrush=Brushes.DimGray, Content=colorCanvas)
            let mutable popupIsActive = false
            colorButton.Click.Add(fun _ -> 
                if not popupIsActive && TrackerModel.IsHiddenDungeonNumbers() then
                    popupIsActive <- true
                    let pos = colorButton.TranslatePoint(Point(15., 15.), appMainCanvas)
                    async {
                        do! Dungeon.HiddenDungeonCustomizerPopup(cm, i, TrackerModel.GetDungeon(i).Color, TrackerModel.GetDungeon(i).LabelChar, false, pos)
                        popupIsActive <- false
                        } |> Async.StartImmediate
                )
            gridAdd(mainTracker, colorButton, i, 0)
            let dungeon = TrackerModel.GetDungeon(i)
            Dungeon.HotKeyAHiddenDungeonLabel(colorCanvas, dungeon, None)
            dungeon.HiddenDungeonColorOrLabelChanged.Add(fun (color,labelChar) -> 
                colorCanvas.Background <- new SolidColorBrush(Graphics.makeColor(color))
                colorCanvas.Children.Clear()
                let color = if Graphics.isBlackGoodContrast(color) then System.Drawing.Color.Black else System.Drawing.Color.White
                if TrackerModel.GetDungeon(i).LabelChar <> '?' then  // ? and 7 look alike, and also it is easier to parse 'blank' as unknown/unset dungeon number
                    colorCanvas.Children.Add(Graphics.BMPtoImage(Graphics.alphaNumOnTransparentBmp(labelChar, color, 28, 28, 3, 2))) |> ignore
                )
            colorButton.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.DungeonIndex i))
            colorButton.MouseLeave.Add(fun _ -> hideLocator())
        else
            let colorCanvas = new Canvas(Width=28., Height=28., Background=Brushes.Black)
            gridAdd(mainTracker, colorCanvas, i, 0)
        // triforce itself and label
        let c = new Canvas(Width=30., Height=30.)
        mainTrackerCanvases.[i,1] <- c
        let innerc = Views.MakeTriforceDisplayView(cm,i,Some(owInstance), true)
        triforceInnerCanvases.[i] <- innerc
        c.Children.Add(innerc) |> ignore
        c.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.DungeonIndex i))
        c.MouseLeave.Add(fun _ -> hideLocator())
        gridAdd(mainTracker, c, i, 1)
        let fullTriforceBmp =
            match kind with
            | TrackerModel.DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS -> Graphics.fullLetteredFoundTriforce_bmps.[i]
            | TrackerModel.DungeonTrackerInstanceKind.DEFAULT -> Graphics.fullNumberedFoundTriforce_bmps.[i]
        timelineItems.Add(new Timeline.TimelineItem(fun()->if TrackerModel.GetDungeon(i).PlayerHasTriforce() then Some(fullTriforceBmp) else None))
    let level9NumeralCanvas = Views.MakeLevel9View(Some(owInstance))
    gridAdd(mainTracker, level9NumeralCanvas, 8, 1) 
    mainTrackerCanvases.[8,1] <- level9NumeralCanvas
    level9NumeralCanvas.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.DungeonIndex 8))
    level9NumeralCanvas.MouseLeave.Add(fun _ -> hideLocator())
    let boxItemImpl(box:TrackerModel.Box, requiresForceUpdate) = 
        let c = Views.MakeBoxItem(cm, box)
        box.Changed.Add(fun _ -> if requiresForceUpdate then TrackerModel.forceUpdate())
        c.MouseEnter.Add(fun _ -> 
            match box.CellCurrent() with
            | 3 -> showLocatorInstanceFunc(owInstance.PowerBraceletable)
            | 4 -> showLocatorInstanceFunc(owInstance.Ladderable)
            | 7 -> showLocatorInstanceFunc(owInstance.Raftable)
            | 8 -> showLocatorInstanceFunc(owInstance.Whistleable)
            | 9 -> showLocatorInstanceFunc(owInstance.Burnable)
            | _ -> ()
            )
        c.MouseLeave.Add(fun _ -> hideLocator())
        timelineItems.Add(new Timeline.TimelineItem(fun()->if box.PlayerHas()=TrackerModel.PlayerHas.YES then Some(CustomComboBoxes.boxCurrentBMP(box.CellCurrent(), true)) else None))
        c
    // dungeon 9 doesn't need a color, we display a 'found summary' here instead
    let level9ColorCanvas = new Canvas(Width=30., Height=30., Background=Brushes.Black)  
    gridAdd(mainTracker, level9ColorCanvas, 8, 0) 
    mainTrackerCanvases.[8,0] <- level9ColorCanvas
    let foundDungeonsTB1 = new TextBox(Text="0/9", FontSize=20., Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true)
    let foundDungeonsTB2 = new TextBox(Text="found", FontSize=12., Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true)
    canvasAdd(level9ColorCanvas, foundDungeonsTB1, 4., -6.)
    canvasAdd(level9ColorCanvas, foundDungeonsTB2, 4., 16.)
    let updateFoundDungeonsCount() =
        let mutable r = 0
        for trackerIndex = 0 to 8 do    
            let d = TrackerModel.GetDungeon(trackerIndex)
            if d.HasBeenLocated() then
                r <- r + 1
        foundDungeonsTB1.Text <- sprintf "%d/9" r
    for trackerIndex = 0 to 8 do    
        let d = TrackerModel.GetDungeon(trackerIndex)
        d.HasBeenLocatedChanged.Add(fun _ -> updateFoundDungeonsCount())
    // items
    let finalCanvasOf1Or4 = 
        if TrackerModel.IsHiddenDungeonNumbers() then
            null
        else        
            boxItemImpl(TrackerModel.DungeonTrackerInstance.TheDungeonTrackerInstance.FinalBoxOf1Or4, false)
    if TrackerModel.IsHiddenDungeonNumbers() then
        for i = 0 to 8 do
            for j = 0 to 2 do
                let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
                gridAdd(mainTracker, c, i, j+2)
                if j<>2 || i <> 8 then   // dungeon 9 does not have 3 items
                    canvasAdd(c, boxItemImpl(TrackerModel.GetDungeon(i).Boxes.[j], false), 0., 0.)
                mainTrackerCanvases.[i,j+2] <- c
                if j=2 && i<> 8 then
                    canvasAdd(c, mainTrackerGhostbusters.[i], 0., 0.)
    else
        for i = 0 to 8 do
            for j = 0 to 2 do
                let c = new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black)
                gridAdd(mainTracker, c, i, j+2)
                if j=0 || j=1 || i=7 then
                    canvasAdd(c, boxItemImpl(TrackerModel.GetDungeon(i).Boxes.[j], false), 0., 0.)
                mainTrackerCanvases.[i,j+2] <- c
    let extrasImage = Graphics.BMPtoImage Graphics.iconExtras_bmp
    extrasImage.ToolTip <- "Starting items and extra drops"
    ToolTipService.SetPlacement(extrasImage, System.Windows.Controls.Primitives.PlacementMode.Top)
    gridAdd(mainTracker, extrasImage, 8, 4)
    do 
        let RedrawForSecondQuestDungeonToggle() =
            if not(TrackerModel.IsHiddenDungeonNumbers()) then
                mainTrackerCanvases.[0,4].Children.Remove(finalCanvasOf1Or4) |> ignore
                mainTrackerCanvases.[3,4].Children.Remove(finalCanvasOf1Or4) |> ignore
                if TrackerModel.Options.IsSecondQuestDungeons.Value then
                    canvasAdd(mainTrackerCanvases.[3,4], finalCanvasOf1Or4, 0., 0.)
                else
                    canvasAdd(mainTrackerCanvases.[0,4], finalCanvasOf1Or4, 0., 0.)
        RedrawForSecondQuestDungeonToggle()
        OptionsMenu.secondQuestDungeonsOptionChanged.Publish.Add(fun _ -> 
            RedrawForSecondQuestDungeonToggle()
            doUIUpdateEvent.Trigger()  // CompletedDungeons may change
            updateGhostBusters()
            )
        if TrackerModel.IsHiddenDungeonNumbers() then
            for i = 0 to 7 do
                TrackerModel.GetDungeon(i).HiddenDungeonColorOrLabelChanged.Add(fun _ -> updateGhostBusters())

    // in mixed quest, buttons to hide first/second quest
    let thereAreMarks(questOnly:string[]) =
        let mutable r = false
        for x = 0 to 15 do 
            for y = 0 to 7 do
                if questOnly.[y].Chars(x) = 'X' && MapStateProxy(TrackerModel.overworldMapMarks.[x,y].Current()).IsInteresting then
                    r <- true
        r
    let mutable hideFirstQuestFromMixed = fun _b -> ()
    let mutable hideSecondQuestFromMixed = fun _b -> ()

    let hideFirstQuestCheckBox  = new CheckBox(Content=new TextBox(Text="HFQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    hideFirstQuestCheckBox.ToolTip <- "Hide First Quest\nIn a mixed quest overworld tracker, shade out the first-quest-only spots.\nUseful if you're unsure if randomizer flags are mixed quest or second quest.\nCan't be used if you've marked a first-quest-only spot as having something."
    ToolTipService.SetShowDuration(hideFirstQuestCheckBox, 12000)
    let hideSecondQuestCheckBox = new CheckBox(Content=new TextBox(Text="HSQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    hideSecondQuestCheckBox.ToolTip <- "Hide Second Quest\nIn a mixed quest overworld tracker, shade out the second-quest-only spots.\nUseful if you're unsure if randomizer flags are mixed quest or first quest.\nCan't be used if you've marked a second-quest-only spot as having something."
    ToolTipService.SetShowDuration(hideSecondQuestCheckBox, 12000)

    hideFirstQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
    hideFirstQuestCheckBox.Checked.Add(fun _ -> 
        if thereAreMarks(OverworldData.owMapSquaresFirstQuestOnly) then
            System.Media.SystemSounds.Asterisk.Play()   // warn, but let them
        hideFirstQuestFromMixed false
        hideSecondQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        )
    hideFirstQuestCheckBox.Unchecked.Add(fun _ -> hideFirstQuestFromMixed true)

    hideSecondQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
    hideSecondQuestCheckBox.Checked.Add(fun _ -> 
        if thereAreMarks(OverworldData.owMapSquaresSecondQuestOnly) then
            System.Media.SystemSounds.Asterisk.Play()   // warn, but let them
        hideSecondQuestFromMixed false
        hideFirstQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        )
    hideSecondQuestCheckBox.Unchecked.Add(fun _ -> hideSecondQuestFromMixed true)
    if isMixed then
        canvasAdd(appMainCanvas, hideFirstQuestCheckBox,  WEBCAM_LINE + 10., 130.) 
        canvasAdd(appMainCanvas, hideSecondQuestCheckBox, WEBCAM_LINE + 60., 130.)

    let white_sword_canvas, mags_canvas, redrawWhiteSwordCanvas, redrawMagicalSwordCanvas, spotSummaryCanvas = 
        MakeItemGrid(cm, boxItemImpl, timelineItems, owInstance, notesTextBox, extrasImage, timeTextBox, resetTimerEvent)

    do! showProgress()

    // overworld map grouping, as main point of support for mirroring
    let mirrorOverworldFEs = ResizeArray<FrameworkElement>()   // overworldCanvas (on which all map is drawn) is here, as well as individual tiny textual/icon elements that need to be re-flipped
    let overworldCanvas = new Canvas(Width=OMTW*16., Height=11.*3.*8.)
    canvasAdd(appMainCanvas, overworldCanvas, 0., 150.)
    mirrorOverworldFEs.Add(overworldCanvas)

    let stepAnimateLink = LinkRouting.SetupLinkRouting(cm, changeCurrentRouteTarget, eliminateCurrentRouteTarget, isSpecificRouteTargetActive, updateNumberedTriforceDisplayImpl,
                                                        (fun() -> displayIsCurrentlyMirrored), MapStateProxy(14).DefaultInteriorBmp(), owInstance, redrawWhiteSwordCanvas, redrawMagicalSwordCanvas)

    do! showProgress()

    let webcamLine = new Canvas(Background=Brushes.Orange, Width=2., Height=150., Opacity=0.4)
    canvasAdd(appMainCanvas, webcamLine, WEBCAM_LINE, 0.)

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // ow map opaque fixed bottom layer
    let X_OPACITY = 0.55
    let owOpaqueMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    owOpaqueMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    for i = 0 to 15 do
        for j = 0 to 7 do
            let image = resizeMapTileImage(Graphics.BMPtoImage(owMapBMPs.[i,j]))
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            canvasAdd(c, image, 0., 0.)
            gridAdd(owOpaqueMapGrid, c, i, j)
            // shading between map tiles
            let OPA = 0.25
            let bottomShade = new Canvas(Width=OMTW, Height=float(3), Background=Brushes.Black, Opacity=OPA)
            canvasAdd(c, bottomShade, 0., float(10*3))
            let rightShade  = new Canvas(Width=float(3), Height=float(11*3), Background=Brushes.Black, Opacity=OPA)
            canvasAdd(c, rightShade, OMTW-3., 0.)
            // permanent icons
            if owInstance.AlwaysEmpty(i,j) then
                let icon = resizeMapTileImage <| Graphics.BMPtoImage(Graphics.theFullTileBmpTable.[TrackerModel.MapSquareChoiceDomainHelper.DARK_X].[0]) // "X"
                icon.Opacity <- X_OPACITY
                canvasAdd(c, icon, 0., 0.)
    canvasAdd(overworldCanvas, owOpaqueMapGrid, 0., 0.)

    // layer to place darkening icons - dynamic icons that are below route-drawing but above the fixed base layer
    // this layer is also used to draw map icons that get drawn below routing, such as potion shops
    let owDarkeningMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owDarkeningMapGridCanvases = Array2D.zeroCreate 16 8
    owDarkeningMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            gridAdd(owDarkeningMapGrid, c, i, j)
            owDarkeningMapGridCanvases.[i,j] <- c
    canvasAdd(overworldCanvas, owDarkeningMapGrid, 0., 0.)

    // layer to place 'hiding' icons - dynamic darkening icons that are below route-drawing but above the previous layers
    do  // HFQ/HSQ
        let owHidingMapGrid = makeGrid(16, 8, int OMTW, 11*3)
        let owHidingMapGridCanvases = Array2D.zeroCreate 16 8
        owHidingMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
        for i = 0 to 15 do
            for j = 0 to 7 do
                let c = new Canvas(Width=OMTW, Height=float(11*3))
                gridAdd(owHidingMapGrid, c, i, j)
                owHidingMapGridCanvases.[i,j] <- c
        canvasAdd(overworldCanvas, owHidingMapGrid, 0., 0.)
        let hide(x,y) =
            let hideColor = Brushes.DarkSlateGray
            let hideOpacity = 0.7
            let mark = new Shapes.Ellipse(Width=OMTW-2., Height=float(11*3)-2., Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
            canvasAdd(owHidingMapGridCanvases.[x,y], mark, 0., 0.)
        hideSecondQuestFromMixed <- 
            (fun unhide ->  // make mixed appear reduced to 1st quest
                for x = 0 to 15 do
                    for y = 0 to 7 do
                        if OverworldData.owMapSquaresSecondQuestOnly.[y].Chars(x) = 'X' then
                            if unhide then
                                owHidingMapGridCanvases.[x,y].Children.Clear()
                            else
                                hide(x,y)
            )
        hideFirstQuestFromMixed <-
            (fun unhide ->   // make mixed appear reduced to 2nd quest
                for x = 0 to 15 do
                    for y = 0 to 7 do
                        if OverworldData.owMapSquaresFirstQuestOnly.[y].Chars(x) = 'X' then
                            if unhide then
                                owHidingMapGridCanvases.[x,y].Children.Clear()
                            else
                                hide(x,y)
            )
    do  // SailNot/NoFeats
        let owHidingMapGrid = makeGrid(16, 8, int OMTW, 11*3)
        let owHidingMapGridCanvases = Array2D.zeroCreate 16 8
        owHidingMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
        for i = 0 to 15 do
            for j = 0 to 7 do
                let c = new Canvas(Width=OMTW, Height=float(11*3))
                gridAdd(owHidingMapGrid, c, i, j)
                owHidingMapGridCanvases.[i,j] <- c
        canvasAdd(overworldCanvas, owHidingMapGrid, 0., 0.)
        let hide(x,y) =
            let hideColor = Brushes.DarkSlateGray
            let hideOpacity = 0.7
            let mark = new Shapes.Ellipse(Width=OMTW-2., Height=float(11*3)-2., Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
            canvasAdd(owHidingMapGridCanvases.[x,y], mark, 0., 0.)
        hideFeatsOfStrength <- 
            (fun b ->  // hide feats of strength
                for x = 0 to 15 do
                    for y = 0 to 7 do
                        if owInstance.PowerBraceletable(x,y) || owInstance.GravePushable(x,y) then
                            if not b then
                                owHidingMapGridCanvases.[x,y].Children.Clear()
                            else
                                hide(x,y)
            )
        hideRaftSpots <-
            (fun b ->   // hide raft spots (sail not)
                for x = 0 to 15 do
                    for y = 0 to 7 do
                        if owInstance.Raftable(x,y) then
                            if not b then
                                owHidingMapGridCanvases.[x,y].Children.Clear()
                            else
                                hide(x,y)
            )

    // ow route drawing layer
    routeDrawingCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(overworldCanvas, routeDrawingCanvas, 0., 0.)

    let onMouseForMagnifier, dungeonTabsOverlay, dungeonTabsOverlayContent = UIComponents.MakeMagnifier(mirrorOverworldFEs, owMapNum, owMapBMPs)
    // ow map -> dungeon tabs interaction
    let selectDungeonTabEvent = new Event<_>()
    let mutable mostRecentlyScrolledDungeonIndex = -1
    let mostRecentlyScrolledDungeonIndexTime = new TrackerModel.LastChangedTime()
    // ow map
    let owMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owCanvases = Array2D.zeroCreate 16 8
    let owUpdateFunctions = Array2D.create 16 8 (fun _ _ -> ())
    let trackerLocationMoused = new Event<_>()
    let trackerDungeonMoused = new Event<_>()
    let drawCompletedIconHighlight(c,x,y,isWider) =
        let w = if isWider then 27.0 else 15.0
        let rect = new System.Windows.Shapes.Rectangle(Width=w*OMTW/48., Height=27.0, Stroke=System.Windows.Media.Brushes.Black, StrokeThickness = 3.,
                                                        Fill=System.Windows.Media.Brushes.Black, Opacity=0.4, IsHitTestVisible=false)
        let diff = (if displayIsCurrentlyMirrored then 18.0*OMTW/48. else 15.0*OMTW/48.) - (if isWider then 6.0 else 0.0)
        canvasAdd(c, rect, x*OMTW+diff, float(y*11*3)+3.0)
    let drawCompletedDungeonHighlight(c,x,y,isWider) =
        // darken the number
        drawCompletedIconHighlight(c,x,y,isWider)
    let drawDarkening(c,x,y) =
        let rect = new System.Windows.Shapes.Rectangle(Width=OMTW, Height=float(11*3), Stroke=System.Windows.Media.Brushes.Black, StrokeThickness = 3.,
                                                        Fill=System.Windows.Media.Brushes.Black, Opacity=X_OPACITY)
        canvasAdd(c, rect, x*OMTW, float(y*11*3))
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            gridAdd(owMapGrid, c, i, j)
            // we need a dummy image to make the canvas absorb the mouse interactions, so just re-draw the map at almost 0 opacity
            let image = resizeMapTileImage <| Graphics.BMPtoImage(owMapBMPs.[i,j])
            image.Opacity <- 0.001
            canvasAdd(c, image, 0., 0.)
            // highlight mouse, do mouse-sensitive stuff
            let rect = new System.Windows.Shapes.Rectangle(Width=OMTW-4., Height=float(11*3)-4., Stroke=System.Windows.Media.Brushes.White)
            c.MouseEnter.Add(fun ea ->  canvasAdd(c, rect, 2., 2.)
                                        // draw routes
                                        let mousePos = ea.GetPosition(c)
                                        let mousePos = if displayIsCurrentlyMirrored then Point(OMTW - mousePos.X, mousePos.Y) else mousePos
                                        drawRoutesTo(currentRouteTarget(), routeDrawingCanvas, mousePos, i, j, TrackerModel.Options.Overworld.DrawRoutes.Value, 
                                                        (if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxGYR else 0),
                                                        (if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxGYR else 0))
                                        onMouseForMagnifier(i,j)
                                        trackerLocationMoused.Trigger(DungeonUI.TrackerLocation.OVERWORLD, i, j)
                                        // track current location for F5 & speech recognition purposes
                                        currentlyMousedOWX <- i
                                        currentlyMousedOWY <- j
                                        )
            c.MouseLeave.Add(fun _ -> c.Children.Remove(rect) |> ignore
                                      trackerLocationMoused.Trigger(DungeonUI.TrackerLocation.OVERWORLD, -1, -1)
                                      dungeonTabsOverlayContent.Children.Clear()
                                      dungeonTabsOverlay.Opacity <- 0.
                                      routeDrawingCanvas.Children.Clear())
            // icon
            if owInstance.AlwaysEmpty(i,j) then
                // already set up as permanent opaque layer, in code above, so nothing else to do
                // except...
                if i=9 && j=3 || i=3 && j=4 || (owInstance.Quest=OverworldData.OWQuest.SECOND && i=11 && j=0) then // fairy spots
                    let image = Graphics.BMPtoImage Graphics.fairy_bmp
                    canvasAdd(c, image, OMTW/2.-8., 1.)
                if i=15 && j=5 then // ladder spot
                    let extraDecorationsF(boxPos:Point) =
                        // ladderBox position in main canvas
                        let lx,ly = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.LADDER_ITEM_BOX)
                        OverworldMapTileCustomization.computeExtraDecorationArrow(lx, ly, boxPos)
                    let coastBoxOnOwGrid = Views.MakeBoxItemWithExtraDecorations(cm, TrackerModel.ladderBox, false, extraDecorationsF)
                    mirrorOverworldFEs.Add(coastBoxOnOwGrid)
                    canvasAdd(c, coastBoxOnOwGrid, OMTW-31., 1.)
                    //TrackerModel.ladderBox.Changed.Add(fun _ -> // this would make the box go away instantly once got
                    doUIUpdateEvent.Publish.Add(fun _ ->          // this waits a second, which gives visual feeback when left-clicked & allows hotkeys to skip it via multi-hotkey
                        if not(TrackerModel.ladderBox.IsDone()) then
                            coastBoxOnOwGrid.Opacity <- 1.
                            coastBoxOnOwGrid.IsHitTestVisible <- true
                        else
                            coastBoxOnOwGrid.Opacity <- 0.
                            coastBoxOnOwGrid.IsHitTestVisible <- false
                        )
            else
                let redrawGridSpot() =
                    // cant remove-by-identity because of non-uniques; remake whole canvas
                    owDarkeningMapGridCanvases.[i,j].Children.Clear()
                    c.Children.Clear()
                    // we need a dummy image to make the canvas absorb the mouse interactions, so just re-draw the map at almost 0 opacity
                    let image = resizeMapTileImage <| Graphics.BMPtoImage(owMapBMPs.[i,j])
                    image.Opacity <- 0.001
                    canvasAdd(c, image, 0., 0.)
                    let ms = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                    let iconBMP,extraDecorations = GetIconBMPAndExtraDecorations(cm,ms,i,j)
                    // be sure to draw in appropriate layer
                    if iconBMP <> null then 
                        let icon = resizeMapTileImage(Graphics.BMPtoImage iconBMP)
                        if ms.IsX then
                            icon.Opacity <- X_OPACITY
                            canvasAdd(owDarkeningMapGridCanvases.[i,j], icon, 0., 0.)  // the icon 'is' the darkening
                        else
                            icon.Opacity <- 1.0
                            drawDarkening(owDarkeningMapGridCanvases.[i,j], 0., 0)     // darken below icon and routing marks
                            canvasAdd(c, icon, 0., 0.)                                 // add icon above routing
                            for fe,x,y in extraDecorations do
                                canvasAdd(c, fe, x, y)
                let isLegalHere(state) = if state = TrackerModel.MapSquareChoiceDomainHelper.ARMOS then owInstance.HasArmos(i,j) else true
                let updateGridSpot delta phrase = 
                    async {
                        // figure out what new state we just interacted-to
                        if delta = 777 then 
                            let curState = TrackerModel.overworldMapMarks.[i,j].Current()
                            if curState = -1 then
                                // if unmarked, use voice to set new state
                                match speechRecognitionInstance.ConvertSpokenPhraseToMapCell(phrase) with
                                | Some newState -> 
                                    if isLegalHere(newState) && TrackerModel.overworldMapMarks.[i,j].AttemptToSet(newState) then
                                        if newState >=0 && newState <=8 then
                                            selectDungeonTabEvent.Trigger(newState)
                                        Graphics.PlaySoundForSpeechRecognizedAndUsedToMark()
                                        let pos = c.TranslatePoint(Point(OMTW/2., 11.*3./2.), appMainCanvas)
                                        match overworldAcceleratorTable.TryGetValue(newState) with
                                        | (true,f) -> 
                                            do! f(cm,c,i,j)
                                            Graphics.WarpMouseCursorTo(pos)
                                        | _ -> ()
                                | None -> ()
                            elif MapStateProxy(curState).IsThreeItemShop && TrackerModel.getOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP)=0 then
                                // if item shop with only one item marked, use voice to set other item
                                match speechRecognitionInstance.ConvertSpokenPhraseToMapCell(phrase) with
                                | Some newState -> 
                                    if TrackerModel.MapSquareChoiceDomainHelper.IsItem(newState) then
                                        TrackerModel.setOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP,TrackerModel.MapSquareChoiceDomainHelper.ToItem(newState))
                                        Graphics.PlaySoundForSpeechRecognizedAndUsedToMark()
                                | None -> ()
                        elif delta = 1 then
                            TrackerModel.overworldMapMarks.[i,j].Next()
                            while not(isLegalHere(TrackerModel.overworldMapMarks.[i,j].Current())) do TrackerModel.overworldMapMarks.[i,j].Next()
                            let newState = TrackerModel.overworldMapMarks.[i,j].Current()
                            if newState >=0 && newState <=7 then
                                mostRecentlyScrolledDungeonIndex <- newState
                                mostRecentlyScrolledDungeonIndexTime.SetNow()
                        elif delta = -1 then 
                            TrackerModel.overworldMapMarks.[i,j].Prev() 
                            while not(isLegalHere(TrackerModel.overworldMapMarks.[i,j].Current())) do TrackerModel.overworldMapMarks.[i,j].Prev()
                            let newState = TrackerModel.overworldMapMarks.[i,j].Current()
                            if newState >=0 && newState <=7 then
                                mostRecentlyScrolledDungeonIndex <- newState
                                mostRecentlyScrolledDungeonIndexTime.SetNow()
                        elif delta = 0 then 
                            ()
                        else failwith "bad delta"
                        redrawGridSpot()
                        } |> Async.StartImmediate
                owUpdateFunctions.[i,j] <- updateGridSpot 
                owCanvases.[i,j] <- c
                mirrorOverworldFEs.Add(c)
                mirrorOverworldFEs.Add(owDarkeningMapGridCanvases.[i,j])
                let popupIsActive = ref false
                let SetNewValue(currentState, originalState) = async {
                    if isLegalHere(currentState) && TrackerModel.overworldMapMarks.[i,j].AttemptToSet(currentState) then
                        if currentState >=0 && currentState <=8 then
                            selectDungeonTabEvent.Trigger(currentState)
                        match overworldAcceleratorTable.TryGetValue(currentState) with
                        | (true,f) -> do! f(cm,c,i,j)
                        | _ -> ()
                        redrawGridSpot()
                        if originalState = -1 && currentState <> -1 then doUIUpdateEvent.Trigger()  // immediate update to dismiss green/yellow highlight from current tile
                    else
                        System.Media.SystemSounds.Asterisk.Play()  // e.g. they tried to set armos on non-armos, or tried to set Level1 when already found elsewhere
                }
                let activatePopup(activationDelta) =
                    popupIsActive := true
                    let GCOL,GROW = 8,5
                    let GCOUNT = GCOL*GROW
                    let pos = c.TranslatePoint(Point(), appMainCanvas)
                    let ST = CustomComboBoxes.borderThickness
                    let tileImage = resizeMapTileImage <| Graphics.BMPtoImage(owMapBMPs.[i,j])
                    let tileCanvas = new Canvas(Width=OMTW, Height=11.*3.)
                    let originalState = TrackerModel.overworldMapMarks.[i,j].Current()
                    let gridWidth = float(GCOL*(5*3+2*int ST)+int ST)
                    let gridxPosition = 
                        if pos.X < OMTW*2. then 
                            -ST // left align
                        elif pos.X > OMTW*13. then 
                            OMTW - gridWidth  // right align
                        else
                            (OMTW - gridWidth)/2.  // center align
                    let typicalGESAI(n) : FrameworkElement*_*_ =
                        let isSelectable = ((n = originalState) || TrackerModel.mapSquareChoiceDomain.CanAddUse(n)) && isLegalHere(n)
                        upcast Graphics.BMPtoImage(MapStateProxy(n).DefaultInteriorBmp()), isSelectable, n
                    let gridElementsSelectablesAndIDs : (FrameworkElement*bool*int)[] = [|
                        // three full rows
                        for n = 0 to 23 do
                            yield typicalGESAI(n)
                        // money row
                        yield null, false, -999  // null asks selector to 'leave a hole' here
                        for n = 24 to 29 do
                            yield typicalGESAI(n)
                        yield null, false, -999  // null asks selector to 'leave a hole' here
                        // other row
                        for n = 30 to 34 do
                            yield typicalGESAI(n)
                        yield upcast new Canvas(Width=5.*3., Height=9.*3., Background=Graphics.overworldCommonestFloorColorBrush, Opacity=X_OPACITY), true, 35
                        yield upcast new Canvas(Width=5.*3., Height=9.*3., Background=Graphics.overworldCommonestFloorColorBrush), true, -1
                        yield null, false, -999  // null asks selector to 'leave a hole' here
                        |]
                    let shopsOnTop = TrackerModel.Options.Overworld.ShopsFirst.Value // start with shops, rather than dungeons, on top of grid
                    let gridElementsSelectablesAndIDs = 
                        if shopsOnTop then [| yield! gridElementsSelectablesAndIDs.[16..]; yield! gridElementsSelectablesAndIDs.[..15] |] else gridElementsSelectablesAndIDs
                    let originalStateIndex = gridElementsSelectablesAndIDs |> Array.findIndex (fun (_,_,s) -> s = originalState)
                    if gridElementsSelectablesAndIDs.Length <> GCOUNT then
                        failwith "bad ow grid tile layout"
                    async {
                        let! r = CustomComboBoxes.DoModalGridSelect(cm, pos.X, pos.Y, tileCanvas,
                                    gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (GCOL, GROW, 5*3, 9*3), gridxPosition, 11.*3.+ST,
                                    (fun (currentState) -> 
                                        tileCanvas.Children.Clear()
                                        canvasAdd(tileCanvas, tileImage, 0., 0.)
                                        let bmp,_ = GetIconBMPAndExtraDecorations(cm, MapStateProxy(currentState), i, j)
                                        if bmp <> null then
                                            let icon = bmp |> Graphics.BMPtoImage |> resizeMapTileImage
                                            if MapStateProxy(currentState).IsX then
                                                icon.Opacity <- X_OPACITY
                                            canvasAdd(tileCanvas, icon, 0., 0.)
                                        let s = if currentState = -1 then "Unmarked" else let _,_,s = TrackerModel.dummyOverworldTiles.[currentState] in s
                                        let text = new TextBox(Text=s, Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                                                                    FontSize=16., HorizontalContentAlignment=HorizontalAlignment.Center)
                                        let textBorder = new Border(BorderThickness=Thickness(ST), Child=text, Background=Brushes.Black, BorderBrush=Brushes.Gray)
                                        let dp = new DockPanel(LastChildFill=false, Width=gridWidth)
                                        DockPanel.SetDock(textBorder, Dock.Bottom)
                                        dp.Children.Add(textBorder) |> ignore
                                        Canvas.SetBottom(dp, 33.)
                                        Canvas.SetLeft(dp, gridxPosition)
                                        tileCanvas.Children.Add(dp) |> ignore
                                        ),
                                    (fun (_ea, currentState) -> CustomComboBoxes.DismissPopupWithResult(currentState)),
                                    [], CustomComboBoxes.ModalGridSelectBrushes.Defaults(), true, None)
                        match r with
                        | Some(currentState) -> do! SetNewValue(currentState, originalState)
                        | None -> ()
                        popupIsActive := false
                        } |> Async.StartImmediate
                c.MouseRightButtonDown.Add(fun _ -> 
                    if not !popupIsActive then
                        // right click activates the popup selector
                        activatePopup(0)
                    )
                c.MouseLeftButtonDown.Add(fun _ -> 
                    if not !popupIsActive then
                        // left click is the 'special interaction'
                        let pos = c.TranslatePoint(Point(), appMainCanvas)
                        let msp = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                        if msp.IsX then
                            activatePopup(0)  // thus, if you have unmarked, then left-click left-click pops up, as the first marks X, and the second now pops up
                        else
                            async {
                                let! needRedraw, needUIUpdate = DoLeftClick(cm,msp,i,j,pos,popupIsActive)
                                if needRedraw then redrawGridSpot()
                                if needUIUpdate then doUIUpdateEvent.Trigger()  // immediate update to dismiss green/yellow highlight from current tile
                            } |> Async.StartImmediate
                    )
                c.MouseWheel.Add(fun x -> if not !popupIsActive then activatePopup(if x.Delta<0 then 1 else -1))
                c.MyKeyAdd(fun ea ->
                    if not !popupIsActive then
                        match HotKeys.OverworldHotKeyProcessor.TryGetValue(ea.Key) with
                        | Some(hotKeyedState) -> 
                            ea.Handled <- true
                            let originalState = TrackerModel.overworldMapMarks.[i,j].Current()
                            let state = OverworldMapTileCustomization.DoSpecialHotKeyHandlingForOverworldTiles(i, j, originalState, hotKeyedState)
                            Async.StartImmediate <| SetNewValue(state, originalState)
                        | None -> ()
                    )
        if i%3 = 2 then
            do! showProgress()
    if speechRecognitionInstance <> null then
        speechRecognitionInstance.AttachSpeechRecognizedToApp(appMainCanvas, (fun recognizedText ->
                                if currentlyMousedOWX >= 0 then // can hear speech before we have moused over any (uninitialized location)
                                    let c = owCanvases.[currentlyMousedOWX,currentlyMousedOWY]
                                    if c <> null && c.IsMouseOver then  // canvas can be null for always-empty grid places
                                        // Note: IsMouseOver appears only be true if we are along the way from a mouse click to the visual root.
                                        // As a result, the update never fires when a popup window is active, because the popup sunglasses would always 
                                        // intercept the click in a different part of the Visual tree.  This is good, because we don't want speech 
                                        // mutating the world while a popup is active (the popup's modality should block speech).
                                        // I guess we could also ask 'cm' if a popup is active.
                                        owUpdateFunctions.[currentlyMousedOWX,currentlyMousedOWY] 777 recognizedText
                            ))
    canvasAdd(overworldCanvas, owMapGrid, 0., 0.)
    owMapGrid.MouseLeave.Add(fun _ -> ensureRespectingOwGettableScreensCheckBox())

    do! showProgress()

    let recorderingCanvas = new Canvas(Width=16.*OMTW, Height=float(8*11*3))  // really the 'extra top layer' canvas for adding final marks to overworld map
    recorderingCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(overworldCanvas, recorderingCanvas, 0., 0.)
    
    // legend
    let makeStartIcon() = new System.Windows.Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=System.Windows.Media.Brushes.Lime, StrokeThickness=3.0, IsHitTestVisible=false)
    let startIcon = makeStartIcon()
    let recorderDestinationLegendIcon, anyRoadLegendIcon = UIComponents.MakeLegend(cm, resizeMapTileImage, drawCompletedDungeonHighlight, makeStartIcon, doUIUpdateEvent)
    let redrawItemProgressBar = UIComponents.MakeItemProgressBar(appMainCanvas, owInstance)
    
    // Version
    let vb = CustomComboBoxes.makeVersionButtonWithBehavior(cm)
    canvasAdd(appMainCanvas, vb, 0., THRU_MAP_AND_LEGEND_H + 4.)

    // hint decoder
    UIComponents.MakeHintDecoderUI(cm)

    // WANT!
    let kitty = new Image()
    let imageStream = Graphics.GetResourceStream("CroppedBrianKitty.png")
    kitty.Source <- System.Windows.Media.Imaging.BitmapFrame.Create(imageStream)
    kitty.Width <- THRU_MAIN_MAP_AND_ITEM_PROGRESS_H - THRU_MAIN_MAP_H
    kitty.Height <- THRU_MAIN_MAP_AND_ITEM_PROGRESS_H - THRU_MAIN_MAP_H
    canvasAdd(appMainCanvas, kitty, 16.*OMTW - kitty.Width - 12., THRU_MAIN_MAP_H)
    let ztlogo = new Image()
    let imageStream = Graphics.GetResourceStream("ZTlogo64x64.png")
    ztlogo.Source <- System.Windows.Media.Imaging.BitmapFrame.Create(imageStream)
    ztlogo.Width <- 40.
    ztlogo.Height <- 40.
    let logoBorder = new Border(BorderThickness=Thickness(1.), BorderBrush=Brushes.Gray, Child=ztlogo)
    canvasAdd(appMainCanvas, logoBorder, 16.*OMTW - ztlogo.Width - 2., THRU_MAIN_MAP_H + kitty.Height - ztlogo.Height - 6.)

    // show hotkeys button
    let showHotKeysTB = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Graphics.almostBlack, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Show HotKeys", IsHitTestVisible=false)
    let showHotKeysButton = new Button(Content=showHotKeysTB)
    canvasAdd(appMainCanvas, showHotKeysButton, 16.*OMTW - kitty.Width - 115., THRU_MAIN_MAP_H)
    let showHotKeys(isRightClick) =
        let none,p = OverworldMapTileCustomization.MakeMappedHotKeysDisplay()
        let w = new Window()
        w.Title <- "Z-Tracker HotKeys"
        w.Owner <- Application.Current.MainWindow
        w.Content <- p
        let save() = 
            TrackerModel.Options.HotKeyWindowLTWH <- sprintf "%d,%d,%d,%d" (int w.Left) (int w.Top) (int w.Width) (int w.Height)
            TrackerModel.Options.writeSettings()
        let leftTopWidthHeight = TrackerModel.Options.HotKeyWindowLTWH
        let matches = System.Text.RegularExpressions.Regex.Match(leftTopWidthHeight, """^(-?\d+),(-?\d+),(\d+),(\d+)$""")
        if not none && not isRightClick && matches.Success then
            w.Left <- float matches.Groups.[1].Value
            w.Top <- float matches.Groups.[2].Value
            w.Width <- float matches.Groups.[3].Value
            w.Height <- float matches.Groups.[4].Value
        else
            p.Measure(Size(1280., 720.))
            w.Width <- p.DesiredSize.Width + 16.
            w.Height <- p.DesiredSize.Height + 40.
        w.SizeChanged.Add(fun _ -> save(); refocusMainWindow())
        w.LocationChanged.Add(fun _ -> save(); refocusMainWindow())
        w.Show()
    showHotKeysButton.Click.Add(fun _ -> showHotKeys(false))
    showHotKeysButton.MouseRightButtonDown.Add(fun _ -> showHotKeys(true))

    // show/run custom button
    let showRunCustomTB = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Graphics.almostBlack, IsReadOnly=true, BorderThickness=Thickness(0.), 
                                        Text="Show/Run\nCustom", IsHitTestVisible=false, TextAlignment=TextAlignment.Center)
    let showRunCustomButton = new Button(Content=showRunCustomTB)
    canvasAdd(appMainCanvas, showRunCustomButton, 16.*OMTW - kitty.Width - 115., THRU_MAIN_MAP_H + 22.)
    showRunCustomButton.Click.Add(fun _ -> ShowRunCustom.DoShowRunCustom(refocusMainWindow))
    //showRunCustomButton.MouseRightButtonDown.Add(fun _ -> )

    let mutable exportDungeonModelsJsonLines = fun () -> null
    let mutable popupIsActive = false
    let saveTB = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Graphics.almostBlack, IsReadOnly=true, BorderThickness=Thickness(0.), 
                                        Text="Save", IsHitTestVisible=false, TextAlignment=TextAlignment.Center)
    let saveButton = new Button(Content=saveTB)
    canvasAdd(appMainCanvas, saveButton, 16.*OMTW - kitty.Width - 50., THRU_MAIN_MAP_H + 22.)
    saveButton.Click.Add(fun _ -> 
        if not popupIsActive then
            popupIsActive <- true
            async {
                try
                    let filename = SaveAndLoad.SaveAll(notesTextBox.Text, exportDungeonModelsJsonLines())
                    let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Information, sprintf "Z-Tracker data saved to file\n%s" filename, ["Ok"])
                    ignore r
                    popupIsActive <- false
                with e ->
                    let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Error, sprintf "Z-Tracker was unable to save the\ntracker state to a file\nError:\n%s" e.Message, ["Ok"])
                    //let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Error, sprintf "Z-Tracker was unable to save file\nError:\n%s" (e.ToString()), ["Ok"])
                    ignore r
                    popupIsActive <- false
                    //System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e).Throw()
            } |> Async.StartImmediate
        )

#if NOT_RACE_LEGAL
    // minimap overlay button
    let minimapOverlayTB = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Graphics.almostBlack, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Minimap overlay", IsHitTestVisible=false)
    let minimapOverlayButton = new Button(Content=minimapOverlayTB)
    canvasAdd(appMainCanvas, minimapOverlayButton, 16.*OMTW - kitty.Width - 115., THRU_MAIN_MAP_H + 22.)
    let mutable minimapOverlay = fun _irc -> ()
    minimapOverlayButton.Click.Add(fun _ -> minimapOverlay(false))
    minimapOverlayButton.MouseRightButtonDown.Add(fun _ -> minimapOverlay(true))

    // near-mouse HUD button
    let nearMouseHUDTB = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Graphics.almostBlack, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Near-mouse HUD", IsHitTestVisible=false)
    let nearMouseHUDButton = new Button(Content=nearMouseHUDTB)
    canvasAdd(appMainCanvas, nearMouseHUDButton, 16.*OMTW - kitty.Width - 115., THRU_MAIN_MAP_H + 44.)
    let mutable nearMouseHUD = fun _irc -> ()
    nearMouseHUDButton.Click.Add(fun _ -> nearMouseHUD(false))
    nearMouseHUDButton.MouseRightButtonDown.Add(fun _ -> nearMouseHUD(true))
#endif

    do! showProgress()

    let blockerDungeonSunglasses : FrameworkElement[] = Array.zeroCreate 8
    let mutable oneTimeRemindLadder, oneTimeRemindAnyKey = None, None
    doUIUpdateEvent.Publish.Add(fun () ->
        if displayIsCurrentlyMirrored <> TrackerModel.Options.Overworld.MirrorOverworld.Value then
            // model changed, align the view
            displayIsCurrentlyMirrored <- not displayIsCurrentlyMirrored
            if displayIsCurrentlyMirrored then
                for fe in mirrorOverworldFEs do
                    fe.RenderTransform <- new ScaleTransform(-1., 1., fe.ActualWidth/2., fe.ActualHeight/2.)
            else
                for fe in mirrorOverworldFEs do
                    fe.RenderTransform <- null
        // redraw triforce display (some may have located/unlocated/hinted)
        updateNumberedTriforceDisplayIfItExists()
        // redraw white/magical swords (may have located/unlocated/hinted)
        redrawWhiteSwordCanvas(white_sword_canvas)
        redrawMagicalSwordCanvas(mags_canvas)

        recorderingCanvas.Children.Clear()
        // TODO event for redraw item progress? does any of this event interface make sense? hmmm
        redrawItemProgressBar()
        // place start icon in top layer
        if TrackerModel.startIconX <> -1 then
            canvasAdd(recorderingCanvas, startIcon, 11.5*OMTW/48.-3.+OMTW*float(TrackerModel.startIconX), float(TrackerModel.startIconY*11*3))
        TrackerModel.allUIEventingLogic( {new TrackerModel.ITrackerEvents with
            member _this.CurrentHearts(h) = currentMaxHeartsTextBox.Text <- sprintf "Max Hearts: %d" h
            member _this.AnnounceConsiderSword2() = 
                let n = TrackerModel.sword2Box.CellCurrent()
                if n = -1 then
                    SendReminder(TrackerModel.ReminderCategory.SwordHearts, "Consider getting the white sword item", 
                                    [upcb(Graphics.iconRightArrow_bmp); upcb(MapStateProxy(14).DefaultInteriorBmp())])
                else
                    SendReminder(TrackerModel.ReminderCategory.SwordHearts, sprintf "Consider getting the %s from the white sword cave" (TrackerModel.ITEMS.AsPronounceString(n)),
                                    [upcb(Graphics.iconRightArrow_bmp); upcb(MapStateProxy(14).DefaultInteriorBmp()); 
                                        upcb(CustomComboBoxes.boxCurrentBMP(TrackerModel.sword2Box.CellCurrent(), false))])
            member _this.AnnounceConsiderSword3() = SendReminder(TrackerModel.ReminderCategory.SwordHearts, "Consider the magical sword", [upcb(Graphics.iconRightArrow_bmp); upcb(Graphics.magical_sword_bmp)])
            member _this.OverworldSpotsRemaining(remain,gettable) = 
                owRemainingScreensTextBox.Text <- sprintf "%d OW spots left" remain
                owGettableScreensTextBox.Text <- sprintf "%d gettable" gettable
            member _this.DungeonLocation(i,x,y,hasTri,isCompleted) =
                if isCompleted then
                    drawCompletedDungeonHighlight(recorderingCanvas,float x,y,(TrackerModel.IsHiddenDungeonNumbers() && TrackerModel.GetDungeon(i).LabelChar<>'?'))
                owUpdateFunctions.[x,y] 0 null  // redraw the tile, e.g. to recolor based on triforce-having
            member _this.AnyRoadLocation(i,x,y) = ()
            member _this.WhistleableLocation(x,y) = ()
            member _this.Armos(x,y) = 
                if TrackerModel.armosBox.IsDone() then
                    drawCompletedIconHighlight(recorderingCanvas,float x,y,false)  // darken a gotten armos icon
            member _this.Sword3(x,y) = 
                if TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value() then
                    drawCompletedIconHighlight(recorderingCanvas,float x,y,false)  // darken a gotten magic sword cave icon
            member _this.Sword2(x,y) =
                owUpdateFunctions.[x,y] 0 null  // redraw the tile, e.g. to place/unplace the box and/or shift the icon
                if TrackerModel.sword2Box.IsDone() then
                    drawCompletedIconHighlight(recorderingCanvas,float x,y,false)  // darken a gotten white sword item cave icon
            member _this.RoutingInfo(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations,owRouteworthySpots) = 
                // clear and redraw routing
                routeDrawingCanvas.Children.Clear()
                OverworldRouting.repopulate(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations)
                let pos = System.Windows.Input.Mouse.GetPosition(routeDrawingCanvas)
                let i,j = int(Math.Floor(pos.X / OMTW)), int(Math.Floor(pos.Y / (11.*3.)))
                if i>=0 && i<16 && j>=0 && j<8 then
                    drawRoutesTo(currentRouteTarget(), routeDrawingCanvas,System.Windows.Point(0.,0.), i, j, TrackerModel.Options.Overworld.DrawRoutes.Value, 
                                    (if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxGYR else 0),
                                    (if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxGYR else 0))
                else
                    ensureRespectingOwGettableScreensCheckBox()
            member _this.AnnounceCompletedDungeon(i) = 
                let icons = [upcb(MapStateProxy(i).DefaultInteriorBmp()); upcb(Graphics.iconCheckMark_bmp)]
                if TrackerModel.IsHiddenDungeonNumbers() then
                    let labelChar = TrackerModel.GetDungeon(i).LabelChar
                    if labelChar <> '?' then
                        SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, sprintf "Dungeon %c is complete" labelChar, icons)
                    else
                        SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "This dungeon is complete", icons)
                else
                    SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, sprintf "Dungeon %d is complete" (i+1), icons)
            member _this.CompletedDungeons(a) =
                for i = 0 to 7 do
                    // top ui
                    for j = 1 to 4 do
                        mainTrackerCanvases.[i,j].Children.Remove(mainTrackerCanvasShaders.[i,j]) |> ignore
                    if a.[i] then
                        for j = 1 to 4 do  // don't shade the color swatches
                            mainTrackerCanvases.[i,j].Children.Add(mainTrackerCanvasShaders.[i,j]) |> ignore
                    // blockers ui
                    if a.[i] then
                        blockerDungeonSunglasses.[i].Opacity <- 0.3
                    else
                        blockerDungeonSunglasses.[i].Opacity <- 1.
            member _this.AnnounceFoundDungeonCount(n) = 
                if DateTime.Now - mostRecentlyScrolledDungeonIndexTime.Time < TimeSpan.FromSeconds(1.5) then
                    selectDungeonTabEvent.Trigger(mostRecentlyScrolledDungeonIndex)
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
            member _this.AnnounceTriforceCount(n) = 
                let icons = [upcb(Graphics.fullOrangeTriforce_bmp); ReminderTextBox(sprintf"%d/8"n)]
                if n = 1 then
                    SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "You now have one triforce", icons)
                else
                    SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, sprintf "You now have %d triforces" n, [yield! icons; if n=8 then yield upcb(Graphics.iconCheckMark_bmp)])
                if n = 8 && not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) then
                    SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "Consider the magical sword before dungeon nine", [upcb(Graphics.iconRightArrow_bmp); upcb(Graphics.magical_sword_bmp)])
            member _this.AnnounceTriforceAndGo(triforceCount, tagSummary) = 
                let needSomeThingsicons = [
                    for _i = 1 to tagSummary.MissingDungeonCount do
                        yield upcb(Graphics.greyscale Graphics.genericDungeonInterior_bmp)
                    if not tagSummary.HaveBow then
                        yield upcb(Graphics.greyscale Graphics.bow_bmp)
                    if not tagSummary.HaveSilvers then
                        yield upcb(Graphics.greyscale Graphics.silver_arrow_bmp)
                    ]
                let triforceAndGoIcons = [
                    if triforceCount<>8 then
                        if needSomeThingsicons.Length<>0 then
                            yield upcb(Graphics.iconRightArrow_bmp)
                        for _i = 1 to (8-triforceCount) do
                            yield upcb(Graphics.greyTriforce_bmp)
                    yield upcb(Graphics.iconRightArrow_bmp)
                    yield upcb(Graphics.ganon_bmp)
                    ]
                let icons = [yield! needSomeThingsicons; yield! triforceAndGoIcons]
                let go = if triforceCount=8 then "go time" else "triforce and go"
                match tagSummary.Level with
                | 101 -> SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "You might be "+go, icons)
                | 102 -> SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "You are probably "+go, icons)
                | 103 -> SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "You are "+go, icons)
                | 0 -> ()
                | _ -> SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, (sprintf "You need %s to be " (if needSomeThingsicons.Length>1 then "some things" else "something"))+go, icons)
            member _this.RemindUnblock(blockerType, dungeons, detail) =
                let name(d) =
                    if TrackerModel.IsHiddenDungeonNumbers() then
                        (char(int 'A' + d)).ToString()
                    else
                        (1+d).ToString()
                let icons = ResizeArray()
                let mutable sentence = "Now that you have"
                match blockerType with
                | TrackerModel.DungeonBlocker.COMBAT ->
                    let words = ResizeArray()
                    for d in detail do
                        match d with
                        | TrackerModel.CombatUnblockerDetail.BETTER_SWORD -> words.Add(" a better sword,"); icons.Add(upcb(Graphics.swordLevelToBmp(TrackerModel.playerComputedStateSummary.SwordLevel)))
                        | TrackerModel.CombatUnblockerDetail.BETTER_ARMOR -> words.Add(" better armor,"); icons.Add(upcb(Graphics.ringLevelToBmp(TrackerModel.playerComputedStateSummary.RingLevel)))
                        | TrackerModel.CombatUnblockerDetail.WAND -> words.Add(" the wand,"); icons.Add(upcb(Graphics.wand_bmp))
                    sentence <- sentence + System.String.Concat words
                | TrackerModel.DungeonBlocker.BOW_AND_ARROW -> sentence <- sentence + " a beau and arrow,"; icons.Add(upcb(Graphics.bow_and_arrow_bmp))
                | TrackerModel.DungeonBlocker.RECORDER -> sentence <- sentence + " the recorder,"; icons.Add(upcb(Graphics.recorder_bmp))
                | TrackerModel.DungeonBlocker.LADDER -> sentence <- sentence + " the ladder,"; icons.Add(upcb(Graphics.ladder_bmp))
                | TrackerModel.DungeonBlocker.KEY -> sentence <- sentence + " the any key,"; icons.Add(upcb(Graphics.key_bmp))
                | TrackerModel.DungeonBlocker.BOMB -> sentence <- sentence + " bombs,"; icons.Add(upcb(Graphics.bomb_bmp))
                | _ -> ()
                sentence <- sentence + " consider dungeon" + (if Seq.length dungeons > 1 then "s " else " ")
                icons.Add(upcb(Graphics.iconRightArrow_bmp))
                let d = Seq.head dungeons
                sentence <- sentence + name(d)
                icons.Add(upcb(MapStateProxy(d).DefaultInteriorBmp()))
                for d in Seq.tail dungeons do
                    sentence <- sentence + " and " + name(d)
                    icons.Add(upcb(MapStateProxy(d).DefaultInteriorBmp()))
                SendReminder(TrackerModel.ReminderCategory.Blockers, sentence, icons)
            member _this.RemindShortly(itemId) = 
                if itemId = TrackerModel.ITEMS.KEY then
                    oneTimeRemindAnyKey <- Some(new TrackerModel.LastChangedTime(), (fun() ->
                        oneTimeRemindAnyKey <- None
                        if TrackerModel.playerComputedStateSummary.HaveAnyKey then
                            SendReminder(TrackerModel.ReminderCategory.HaveKeyLadder, "Don't forget that you have the any key", [upcb(Graphics.key_bmp)])
                        else
                            TrackerModel.remindedAnyKey <- false))
                elif itemId = TrackerModel.ITEMS.LADDER then
                    oneTimeRemindLadder <- Some(new TrackerModel.LastChangedTime(), (fun() ->
                        oneTimeRemindLadder <- None
                        if TrackerModel.playerComputedStateSummary.HaveLadder then
                            SendReminder(TrackerModel.ReminderCategory.HaveKeyLadder, "Don't forget that you have the ladder", [upcb(Graphics.ladder_bmp)])
                        else
                            TrackerModel.remindedLadder <- false))
                else
                    failwith "bad reminder"
            })
        )
    let threshold = TimeSpan.FromMilliseconds(500.0)
    let recentlyAgo = TimeSpan.FromMinutes(3.0)
    let ladderTime, recorderTime, powerBraceletTime, boomstickTime = 
        new TrackerModel.LastChangedTime(recentlyAgo), new TrackerModel.LastChangedTime(recentlyAgo), new TrackerModel.LastChangedTime(recentlyAgo), new TrackerModel.LastChangedTime(recentlyAgo)
    let mutable owPreviouslyAnnouncedWhistleSpotsRemain, owPreviouslyAnnouncedPowerBraceletSpotsRemain = 0, 0
    let timer = new System.Windows.Threading.DispatcherTimer()
    timer.Interval <- TimeSpan.FromSeconds(1.0)
    timer.Tick.Add(fun _ -> 
        if not(TrackerModel.LastChangedTime.IsPaused) then
            let hasUISettledDown = 
                DateTime.Now - TrackerModel.playerProgressLastChangedTime.Time > threshold &&
                DateTime.Now - TrackerModel.dungeonsAndBoxesLastChangedTime.Time > threshold &&
                DateTime.Now - TrackerModel.mapLastChangedTime.Time > threshold
            if hasUISettledDown then
                let hasTheModelChanged = TrackerModel.recomputeWhatIsNeeded()  
                if hasTheModelChanged then
                    doUIUpdateEvent.Trigger()
            // link animation
            stepAnimateLink()
            // remind ladder
            if (DateTime.Now - ladderTime.Time).Minutes > 2 then  // every 3 mins
                if TrackerModel.playerComputedStateSummary.HaveLadder then
                    if not(TrackerModel.playerComputedStateSummary.HaveCoastItem) then
                        let n = TrackerModel.ladderBox.CellCurrent()
                        if n = -1 then
                            SendReminder(TrackerModel.ReminderCategory.CoastItem, "Get the coast item with the ladder", [upcb(Graphics.ladder_bmp); upcb(Graphics.iconRightArrow_bmp)])
                        else
                            SendReminder(TrackerModel.ReminderCategory.CoastItem, sprintf "Get the %s off the coast" (TrackerModel.ITEMS.AsPronounceString(n)),
                                            [upcb(Graphics.ladder_bmp); upcb(Graphics.iconRightArrow_bmp); upcb(CustomComboBoxes.boxCurrentBMP(TrackerModel.ladderBox.CellCurrent(), false))])
                        ladderTime.SetNow()
            // remind whistle spots
            if (DateTime.Now - recorderTime.Time).Minutes > 2 then  // every 3 mins
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
            if (DateTime.Now - powerBraceletTime.Time).Minutes > 2 then  // every 3 mins
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
            if (DateTime.Now - boomstickTime.Time).Minutes > 2 then  // every 3 mins
                if TrackerModel.playerComputedStateSummary.HaveWand && not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Value()) then
                    if TrackerModel.mapStateSummary.BoomBookShopLocation<>TrackerModel.NOTFOUND then
                        SendReminder(TrackerModel.ReminderCategory.RecorderPBSpotsAndBoomstickBook, "Consider buying the boomstick book", [upcb(Graphics.iconRightArrow_bmp); upcb(Graphics.boom_book_bmp)])
                        boomstickTime.SetNow()
            // one-time reminders
            match oneTimeRemindAnyKey with
            | None -> ()
            | Some(lct, thunk) ->
                if (DateTime.Now - lct.Time).Minutes > 1 then  // 2 min
                    thunk()
            match oneTimeRemindLadder with
            | None -> ()
            | Some(lct, thunk) ->
                if (DateTime.Now - lct.Time).Minutes > 1 then  // 2 min
                    thunk()
        )

    // Dungeon level trackers
    let rightwardCanvas = new Canvas()
    let levelTabSelected = new Event<_>()
    let! dungeonTabs,grabModeTextBlock,exportDungeonModelsJsonLinesF,importDungeonModels = 
        DungeonUI.makeDungeonTabs(cm, START_DUNGEON_AND_NOTES_AREA_H, selectDungeonTabEvent, trackerLocationMoused, trackerDungeonMoused, TH, rightwardCanvas, 
                                    levelTabSelected, mainTrackerGhostbusters, showProgress, (fun level ->
            let i,j = TrackerModel.mapStateSummary.DungeonLocations.[level-1]
            if (i,j) <> TrackerModel.NOTFOUND then
                // when mouse in a dungeon map, show its location...
                showLocatorExactLocation(TrackerModel.mapStateSummary.DungeonLocations.[level-1])
                // ...and behave like we are moused there
                drawRoutesTo(None, routeDrawingCanvas, Point(), i, j, TrackerModel.Options.Overworld.DrawRoutes.Value, 
                                    (if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxGYR else 0),
                                    (if TrackerModel.Options.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxGYR else 0))
            ), (fun _level -> hideLocator()))
    exportDungeonModelsJsonLines <- exportDungeonModelsJsonLinesF
    canvasAdd(appMainCanvas, dungeonTabs, 0., START_DUNGEON_AND_NOTES_AREA_H)
    canvasAdd(appMainCanvas, dungeonTabsOverlay, 0., START_DUNGEON_AND_NOTES_AREA_H+float(TH))

    do! showProgress()

    // blockers
    let blockerGrid = UIComponents.MakeBlockers(cm, levelTabSelected, blockerDungeonSunglasses)

    // notes    
    let tb = new TextBox(Width=appMainCanvas.Width-BLOCKERS_AND_NOTES_OFFSET, Height=dungeonTabs.Height - blockerGrid.Height)
    notesTextBox <- tb
    tb.FontSize <- 24.
    tb.Foreground <- System.Windows.Media.Brushes.LimeGreen 
    tb.Background <- System.Windows.Media.Brushes.Black 
    let notesFilename = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Notes.txt")
    if not(System.IO.File.Exists(notesFilename)) then
        tb.Text <- "Notes\n"
    else
        tb.Text <- System.IO.File.ReadAllText(notesFilename)
    tb.AcceptsReturn <- true
    canvasAdd(appMainCanvas, tb, BLOCKERS_AND_NOTES_OFFSET, START_DUNGEON_AND_NOTES_AREA_H + blockerGrid.Height) 

    grabModeTextBlock.Opacity <- 0.
    grabModeTextBlock.Width <- tb.Width
    canvasAdd(appMainCanvas, grabModeTextBlock, BLOCKERS_AND_NOTES_OFFSET, START_DUNGEON_AND_NOTES_AREA_H) 

    canvasAdd(appMainCanvas, rightwardCanvas, BLOCKERS_AND_NOTES_OFFSET, START_DUNGEON_AND_NOTES_AREA_H)  // extra place for dungeonTabs to draw atop blockers/notes

    // remaining OW spots
    canvasAdd(appMainCanvas, owRemainingScreensTextBox, RIGHT_COL, 90.)
    owRemainingScreensTextBox.MouseEnter.Add(fun _ ->
        let unmarked = TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1)
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, unmarked, 
                                            unmarked, Point(0.,0.), 0, 0, false, true, 0, OverworldRouteDrawing.All)
        if not(TrackerModel.playerComputedStateSummary.HaveRaft) then
            // drawPathsImpl cannot reach the raft locations and won't color them, so just ad-hoc those two spots
            for i,j in [5,4 ; 15,2] do
                let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                if cur = -1 then  // they may have marked the raft spots (e.g. Any Road), so only if unmarked...
                    let thr = new Graphics.TileHighlightRectangle()
                    thr.MakePaleRed()
                    for s in thr.Shapes do
                        canvasAdd(routeDrawingCanvas, s, OMTW*float(i), float(j*11*3))
        )
    owRemainingScreensTextBox.MouseLeave.Add(fun _ ->
        routeDrawingCanvas.Children.Clear()
        ensureRespectingOwGettableScreensCheckBox()
        )
    canvasAdd(appMainCanvas, owGettableScreensCheckBox, RIGHT_COL, 110.)
    owGettableScreensCheckBox.Checked.Add(fun _ -> TrackerModel.forceUpdate()) 
    owGettableScreensCheckBox.Unchecked.Add(fun _ -> TrackerModel.forceUpdate())
    owGettableScreensTextBox.MouseEnter.Add(fun _ -> 
        OverworldRouteDrawing.drawPathsImpl(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, 
                                            TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), Point(0.,0.), 0, 0, false, true, 0, OverworldRouteDrawing.All)
        )
    owGettableScreensTextBox.MouseLeave.Add(fun _ -> 
        if not(owGettableScreensCheckBox.IsChecked.HasValue) || not(owGettableScreensCheckBox.IsChecked.Value) then 
            routeDrawingCanvas.Children.Clear()
        )

    do! showProgress()

    // current max hearts
    canvasAdd(appMainCanvas, currentMaxHeartsTextBox, RIGHT_COL, 130.)
    // coordinate grid
    let owCoordsGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owCoordsTBs = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let tb = new TextBox(Text=sprintf "%c  %d" (char (int 'A' + j)) (i+1),  // may change with OMTW and overall layout
                                    Foreground=Brushes.White, Background=Brushes.Transparent, BorderThickness=Thickness(0.0), 
                                    FontFamily=FontFamily("Consolas"), FontSize=16.0, FontWeight=FontWeights.Bold, IsHitTestVisible=false)
            tb.Opacity <- 0.0
            tb.IsHitTestVisible <- false // transparent to mouse
            owCoordsTBs.[i,j] <- tb
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            canvasAdd(c, tb, 2., 6.)
            gridAdd(owCoordsGrid, c, i, j) 
    mirrorOverworldFEs.Add(owCoordsGrid)
    canvasAdd(overworldCanvas, owCoordsGrid, 0., 0.)
    let showCoords = new TextBox(Text="Coords",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true)
    let cb = new CheckBox(Content=showCoords)
    cb.IsChecked <- System.Nullable.op_Implicit false
    cb.Checked.Add(fun _ -> owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.85))
    cb.Unchecked.Add(fun _ -> owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.0))
    showCoords.MouseEnter.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.85))
    showCoords.MouseLeave.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.0))
    canvasAdd(appMainCanvas, cb, OW_ITEM_GRID_LOCATIONS.OFFSET+200., 72.)

    // zone overlay
    let zone_checkbox, addZoneName, changeZoneOpacity, allOwMapZoneBlackCanvases = UIComponents.MakeZoneOverlay(appMainCanvas, overworldCanvas, mirrorOverworldFEs, OW_ITEM_GRID_LOCATIONS.OFFSET)

    // mouse hover explainer
    UIComponents.MakeMouseHoverExplainer(appMainCanvas)

    let owLocatorGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owLocatorTilesZone = Array2D.zeroCreate 16 8
    let owLocatorCanvas = new Canvas()

    do! showProgress()

    for i = 0 to 15 do
        for j = 0 to 7 do
            let z = new Graphics.TileHighlightRectangle()
            z.Hide()
            owLocatorTilesZone.[i,j] <- z
            for s in z.Shapes do
                gridAdd(owLocatorGrid, s, i, j)
    canvasAdd(overworldCanvas, owLocatorGrid, 0., 0.)
    canvasAdd(overworldCanvas, owLocatorCanvas, 0., 0.)

    showLocatorExactLocation <- (fun (x,y) ->
        if (x,y) <> TrackerModel.NOTFOUND then
            let leftLine = new Shapes.Line(X1=OMTW*float x, Y1=0., X2=OMTW*float x, Y2=float(8*11*3), Stroke=Brushes.White, StrokeThickness=2., IsHitTestVisible=false)
            canvasAdd(owLocatorCanvas, leftLine, 0., 0.)
            let rightLine = new Shapes.Line(X1=OMTW*float (x+1)-1., Y1=0., X2=OMTW*float (x+1)-1., Y2=float(8*11*3), Stroke=Brushes.White, StrokeThickness=2., IsHitTestVisible=false)
            canvasAdd(owLocatorCanvas, rightLine, 0., 0.)
            let topLine = new Shapes.Line(X1=0., Y1=float(y*11*3), X2=OMTW*float(16*3), Y2=float(y*11*3), Stroke=Brushes.White, StrokeThickness=2., IsHitTestVisible=false)
            canvasAdd(owLocatorCanvas, topLine, 0., 0.)
            let bottomLine = new Shapes.Line(X1=0., Y1=float((y+1)*11*3)-1., X2=OMTW*float(16*3), Y2=float((y+1)*11*3)-1., Stroke=Brushes.White, StrokeThickness=2., IsHitTestVisible=false)
            canvasAdd(owLocatorCanvas, bottomLine, 0., 0.)
        )
    showLocatorHintedZone <- (fun (hinted_zone, alsoHighlightABCDEFGH) ->
        routeDrawingCanvas.Children.Clear()
        if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
            // have hint, so draw that zone...
            let inZone = Array2D.init 16 8 (fun x y -> OverworldData.owMapZone.[y].[x] = hinted_zone.AsDataChar())
            for x = 0 to 15 do
                for y = 0 to 7 do
                    if not(inZone.[x,y]) then
                        allOwMapZoneBlackCanvases.[x,y].Opacity <- 0.7
            let isZone(x,y) = inZone.[max 0 (min 15 x), max 0 (min 7 y)]  // if x or y out of bounds, grab nearby in-bounds value
            let ST, OFF, COLOR = 3., 3./2., Brushes.LightGray
            for x = 0 to 15 do
                for y = 0 to 7 do
                    let mkLine(x1,y1,x2,y2) = 
                        let line = new Shapes.Line(X1=x1, Y1=y1, X2=x2, Y2=y2, Stroke=COLOR, StrokeThickness=ST, IsHitTestVisible=false)
                        line.StrokeDashArray <- new DoubleCollection( seq[1.; 1.5] )
                        line
                    if isZone(x,y) && not(isZone(x-1,y)) then  // left
                        let line = mkLine(OMTW*float(x)-OFF, float(y*11*3), OMTW*float(x)-OFF, float((y+1)*11*3))
                        canvasAdd(owLocatorCanvas, line, 0., 0.)
                    if isZone(x,y) && not(isZone(x+1,y)) then  // right
                        let line = mkLine(OMTW*float(x+1)+OFF, float(y*11*3), OMTW*float(x+1)+OFF, float((y+1)*11*3))
                        canvasAdd(owLocatorCanvas, line, 0., 0.)
                    if isZone(x,y) && not(isZone(x,y-1)) then  // top
                        let line = mkLine(OMTW*float(x), float(y*11*3)-OFF, OMTW*float(x+1), float(y*11*3)-OFF)
                        canvasAdd(owLocatorCanvas, line, 0., 0.)
                    if isZone(x,y) && not(isZone(x,y+1)) then  // bottom
                        let line = mkLine(OMTW*float(x), float((y+1)*11*3)+OFF, OMTW*float(x+1), float((y+1)*11*3)+OFF)
                        canvasAdd(owLocatorCanvas, line, 0., 0.)
            for i = 0 to 15 do
                for j = 0 to 7 do
                    // ... and highlight all undiscovered tiles
                    if OverworldData.owMapZone.[j].[i] = hinted_zone.AsDataChar() then
                        let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                        let isLetteredNumberlessDungeon = (alsoHighlightABCDEFGH && cur>=0 && cur<=7 && TrackerModel.GetDungeon(cur).LabelChar='?')
                        if cur = -1 || isLetteredNumberlessDungeon then
                            if TrackerModel.mapStateSummary.OwGettableLocations.Contains(i,j) then
                                if owInstance.SometimesEmpty(i,j) then
                                    owLocatorTilesZone.[i,j].MakeYellow()
                                else
                                    owLocatorTilesZone.[i,j].MakeGreen()
                            else
                                if isLetteredNumberlessDungeon then  // OwGettableLocations does not contain already-marked spots
                                    owLocatorTilesZone.[i,j].MakeGreen()
                                else
                                    owLocatorTilesZone.[i,j].MakeRed()
        )
    showLocatorInstanceFunc <- (fun f ->
        routeDrawingCanvas.Children.Clear()
        for i = 0 to 15 do
            for j = 0 to 7 do
                if f(i,j) && TrackerModel.overworldMapMarks.[i,j].Current() = -1 then
                    if owInstance.SometimesEmpty(i,j) then
                        owLocatorTilesZone.[i,j].MakeYellow()
                    else
                        owLocatorTilesZone.[i,j].MakeGreen()
        )
    showShopLocatorInstanceFunc <- (fun item ->
        routeDrawingCanvas.Children.Clear()
        for i = 0 to 15 do
            for j = 0 to 7 do
                let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                if MapStateProxy(cur).IsThreeItemShop && 
                        (cur = item || (TrackerModel.getOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP) = TrackerModel.MapSquareChoiceDomainHelper.ToItem(item))) then
                    owLocatorTilesZone.[i,j].MakeGreen()
        )
    showLocatorPotionAndTakeAny <- (fun () ->
        routeDrawingCanvas.Children.Clear()
        for i = 0 to 15 do
            for j = 0 to 7 do
                let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                if cur = TrackerModel.MapSquareChoiceDomainHelper.POTION_SHOP || 
                    (cur = TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY && TrackerModel.getOverworldMapExtraData(i,j,cur)<>cur) then
                    owLocatorTilesZone.[i,j].MakeGreen()
        )
    recorderDestinationLegendIcon.MouseEnter.Add(fun _ ->
        routeDrawingCanvas.Children.Clear()
        for i = 0 to 15 do
            for j = 0 to 7 do
                let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                if TrackerModel.playerComputedStateSummary.HaveRecorder && MapStateProxy(cur).IsDungeon && TrackerModel.GetDungeon(cur).PlayerHasTriforce() then
                    owLocatorTilesZone.[i,j].MakeGreen()
        )
    recorderDestinationLegendIcon.MouseLeave.Add(fun _ -> hideLocator())
    anyRoadLegendIcon.MouseEnter.Add(fun _ ->
        routeDrawingCanvas.Children.Clear()
        for i = 0 to 15 do
            for j = 0 to 7 do
                let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                if cur >= TrackerModel.MapSquareChoiceDomainHelper.WARP_1 && cur <= TrackerModel.MapSquareChoiceDomainHelper.WARP_4 then
                    owLocatorTilesZone.[i,j].MakeGreen()
        )
    anyRoadLegendIcon.MouseLeave.Add(fun _ -> hideLocator())
    showLocator <- (fun sld ->
        match sld with
        | ShowLocatorDescriptor.DungeonNumber(n) ->
            let mutable index = -1
            for i = 0 to 7 do
                if TrackerModel.GetDungeon(i).LabelChar = char(int '1' + n) then
                    index <- i
            let showHint() =
                let hinted_zone = TrackerModel.GetLevelHint(n)
                if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                    showLocatorHintedZone(hinted_zone,true)
            if index <> -1 then
                let loc = TrackerModel.mapStateSummary.DungeonLocations.[index]
                if loc <> TrackerModel.NOTFOUND then
                    showLocatorExactLocation(loc)
                else
                    showHint()
            else
                showHint()
        | ShowLocatorDescriptor.DungeonIndex(i) ->
            let loc = TrackerModel.mapStateSummary.DungeonLocations.[i]
            if loc <> TrackerModel.NOTFOUND then
                showLocatorExactLocation(loc)
            else
                if TrackerModel.IsHiddenDungeonNumbers() then
                    let label = TrackerModel.GetDungeon(i).LabelChar
                    if label >= '1' && label <= '8' then
                        let index = int label - int '1'
                        let hinted_zone = TrackerModel.GetLevelHint(index)
                        if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                            showLocatorHintedZone(hinted_zone,true)
                else
                    let hinted_zone = TrackerModel.GetLevelHint(i)
                    if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                        showLocatorHintedZone(hinted_zone,false)
        | ShowLocatorDescriptor.Sword1 ->
            let loc = TrackerModel.mapStateSummary.Sword1Location
            if loc <> TrackerModel.NOTFOUND then
                showLocatorExactLocation(loc)
        | ShowLocatorDescriptor.Sword2 ->
            let loc = TrackerModel.mapStateSummary.Sword2Location
            if loc <> TrackerModel.NOTFOUND then
                showLocatorExactLocation(loc)
            else
                let hinted_zone = TrackerModel.GetLevelHint(9)
                if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                    showLocatorHintedZone(hinted_zone,false)
        | ShowLocatorDescriptor.Sword3 ->
            let loc = TrackerModel.mapStateSummary.Sword3Location
            if loc <> TrackerModel.NOTFOUND then
                showLocatorExactLocation(loc)
            else
                let hinted_zone = TrackerModel.GetLevelHint(10)
                if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                    showLocatorHintedZone(hinted_zone,false)
        )
    hideLocator <- (fun () ->
        if not zone_checkbox.IsChecked.HasValue || not zone_checkbox.IsChecked.Value then changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,false)
        allOwMapZoneBlackCanvases |> Array2D.iteri (fun _x _y zbc -> zbc.Opacity <- 0.0)
        for i = 0 to 15 do
            for j = 0 to 7 do
                owLocatorTilesZone.[i,j].Hide()
        owLocatorCanvas.Children.Clear()
        ensureRespectingOwGettableScreensCheckBox()
        )

    addZoneName(TrackerModel.HintZone.DEATH_MOUNTAIN, "DEATH\nMOUNTAIN", 2.5, 0.3)
    addZoneName(TrackerModel.HintZone.GRAVE,          "GRAVE", 1.5, 2.8)
    addZoneName(TrackerModel.HintZone.DEAD_WOODS,     "DEAD\nWOODS", 1.4, 5.3)
    addZoneName(TrackerModel.HintZone.LAKE,           "LAKE 1", 10.2, 0.1)
    addZoneName(TrackerModel.HintZone.LAKE,           "LAKE 2", 5.5, 3.5)
    addZoneName(TrackerModel.HintZone.LAKE,           "LAKE 3", 9.4, 5.5)
    addZoneName(TrackerModel.HintZone.RIVER,          "RIVER 1", 7.3, 1.1)
    addZoneName(TrackerModel.HintZone.RIVER,          "RIV\nER2", 5.1, 6.2)
    addZoneName(TrackerModel.HintZone.NEAR_START,     "START", 7.3, 6.2)
    addZoneName(TrackerModel.HintZone.DESERT,         "DESERT", 10.3, 3.1)
    addZoneName(TrackerModel.HintZone.FOREST,         "FOREST", 12.3, 5.1)
    addZoneName(TrackerModel.HintZone.LOST_HILLS,     "LOST\nHILLS", 12.4, 0.3)
    addZoneName(TrackerModel.HintZone.COAST,          "COAST", 14.3, 2.7)

    // timeline, options menu, reminders
    let moreOptionsButton = Graphics.makeButton("Options...", Some(12.), Some(Brushes.Orange))
    moreOptionsButton.MaxHeight <- 25.
    moreOptionsButton.Measure(new Size(System.Double.PositiveInfinity, 25.))

    let optionsCanvas = OptionsMenu.makeOptionsCanvas(appMainCanvas.Width, true)
    optionsCanvas.Opacity <- 1.
    optionsCanvas.IsHitTestVisible <- true

    let theTimeline1 = new Timeline.Timeline(21., 4, 60, 5, appMainCanvas.Width-48., 1, "0h", "30m", "1h", moreOptionsButton.DesiredSize.Width-24.)
    let theTimeline2 = new Timeline.Timeline(21., 4, 60, 5, appMainCanvas.Width-48., 2, "0h", "1h", "2h", moreOptionsButton.DesiredSize.Width-24.)
    let theTimeline3 = new Timeline.Timeline(21., 4, 60, 5, appMainCanvas.Width-48., 3, "0h", "1.5h", "3h", moreOptionsButton.DesiredSize.Width-24.)
    theTimeline1.Canvas.Opacity <- 1.
    theTimeline2.Canvas.Opacity <- 0.
    theTimeline3.Canvas.Opacity <- 0.
    let updateTimeline(minute) =
        if minute <= 60 then
            theTimeline1.Canvas.Opacity <- 1.
            theTimeline2.Canvas.Opacity <- 0.
            theTimeline3.Canvas.Opacity <- 0.
            theTimeline1.Update(minute, timelineItems)
        elif minute <= 120 then
            theTimeline1.Canvas.Opacity <- 0.
            theTimeline2.Canvas.Opacity <- 1.
            theTimeline3.Canvas.Opacity <- 0.
            theTimeline2.Update(minute, timelineItems)
        else
            theTimeline1.Canvas.Opacity <- 0.
            theTimeline2.Canvas.Opacity <- 0.
            theTimeline3.Canvas.Opacity <- 1.
            theTimeline3.Update(minute, timelineItems)
    canvasAdd(appMainCanvas, theTimeline1.Canvas, 24., START_TIMELINE_H)
    canvasAdd(appMainCanvas, theTimeline2.Canvas, 24., START_TIMELINE_H)
    canvasAdd(appMainCanvas, theTimeline3.Canvas, 24., START_TIMELINE_H)

    canvasAdd(appMainCanvas, moreOptionsButton, 0., START_TIMELINE_H)
    let mutable popupIsActive = false
    moreOptionsButton.Click.Add(fun _ -> 
        if not popupIsActive then
            popupIsActive <- true
            let wh = new System.Threading.ManualResetEvent(false)
            async {
                do! CustomComboBoxes.DoModalDocked(cm, wh, Dock.Bottom, optionsCanvas)
                TrackerModel.Options.writeSettings()
                popupIsActive <- false
                } |> Async.StartImmediate
        )

    // reminder display
    let cxt = System.Threading.SynchronizationContext.Current 
    let reminderDisplayOuterDockPanel = new DockPanel(Width=OMTW*16., Height=THRU_TIMELINE_H-START_TIMELINE_H, Opacity=0., LastChildFill=false)
    let reminderDisplayInnerDockPanel = new DockPanel(LastChildFill=false, Background=Brushes.Black)
    let reminderDisplayInnerBorder = new Border(Child=reminderDisplayInnerDockPanel, BorderThickness=Thickness(3.), BorderBrush=Brushes.Lime, HorizontalAlignment=HorizontalAlignment.Right)
    DockPanel.SetDock(reminderDisplayInnerBorder, Dock.Top)
    reminderDisplayOuterDockPanel.Children.Add(reminderDisplayInnerBorder) |> ignore
    canvasAdd(appMainCanvas, reminderDisplayOuterDockPanel, 0., START_TIMELINE_H)
    reminderAgent <- MailboxProcessor.Start(fun inbox -> 
        let rec messageLoop() = async {
            let! (text,shouldRemindVoice,icons,shouldRemindVisual) = inbox.Receive()
            do! Async.SwitchToContext(cxt)
            if not(TrackerModel.Options.IsMuted) then
                let sp = new StackPanel(Orientation=Orientation.Horizontal, Background=Brushes.Black, Margin=Thickness(6.))
                for i in icons do
                    i.Margin <- Thickness(3.)
                    sp.Children.Add(i) |> ignore
                let iconCount = sp.Children.Count
                if shouldRemindVisual then
                    Graphics.PlaySoundForReminder()
                reminderDisplayInnerDockPanel.Children.Clear()
                DockPanel.SetDock(sp, Dock.Right)
                reminderDisplayInnerDockPanel.Children.Add(sp) |> ignore
                if shouldRemindVisual then
                    reminderDisplayOuterDockPanel.Opacity <- 1.
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
                do! Async.SwitchToContext(cxt)
                reminderDisplayOuterDockPanel.Opacity <- 0.
            return! messageLoop()
            }
        messageLoop()
        )

    appMainCanvas.MouseDown.Add(fun _ -> 
        let w = Window.GetWindow(appMainCanvas)
        Input.Keyboard.ClearFocus()                // ensure that clicks outside the Notes area de-focus it, by clearing keyboard focus...
        Input.FocusManager.SetFocusedElement(w, null)  // ... and by removing its logical mouse focus
        Input.Keyboard.Focus(w) |> ignore  // refocus keyboard to main window so MyKey still works
        )  

    do! showProgress()

    // broadcast window
    Broadcast.MakeBroadcastWindow(cm, blockerGrid, dungeonTabsOverlayContent, refocusMainWindow)

#if NOT_RACE_LEGAL
    HUDs.MakeHUDs(cm, trackerDungeonMoused, trackerLocationMoused)
#endif

    canvasAdd(appMainCanvas, spotSummaryCanvas, 50., 30.)  // height chosen to make broadcast-window-cutoff be reasonable

    // poke loaded data values
    match loadData with
    | Some(data) ->
        let a = data.Overworld.Map
        if a.Length <> 16 * 8 * 2 then
            failwith "bad load data at data.Overworld.Map"
        silenceAllRemindersDuringCurrentLoad <- true
        // Overworld
        let mutable anySetProblems = false
        for j = 0 to 7 do
            for i = 0 to 15 do
                let cur = a.[j*16*2 + i*2]
                let ed = a.[j*16*2 + i*2 + 1]
                if not(TrackerModel.overworldMapMarks.[i,j].AttemptToSet(cur)) then
                    anySetProblems <- true
                let k = if TrackerModel.MapSquareChoiceDomainHelper.IsItem(cur) then TrackerModel.MapSquareChoiceDomainHelper.SHOP else cur
                if k <> -1 then
                    TrackerModel.setOverworldMapExtraData(i,j,k,ed)
                owUpdateFunctions.[i,j] 0 null  // redraw the tile
            TrackerModel.recomputeMapStateSummary()
            doUIUpdateEvent.Trigger()
        // Items
        if not(data.Items.WhiteSwordBox.TryApply(TrackerModel.sword2Box)) then anySetProblems <- true
        if not(data.Items.LadderBox.TryApply(TrackerModel.ladderBox)) then anySetProblems <- true
        if not(data.Items.ArmosBox.TryApply(TrackerModel.armosBox)) then anySetProblems <- true
        for i = 0 to 8 do
            let ds = data.Items.Dungeons.[i]
            let dd = TrackerModel.GetDungeon(i)
            if not(ds.TryApply(dd)) then anySetProblems <- true
        TrackerModel.recomputePlayerStateSummary()
        TrackerModel.recomputeWhatIsNeeded() |> ignore
        TrackerModel.forceUpdate()
        doUIUpdateEvent.Trigger()
        // PlayerProgressAndTakeAnyHearts
        data.PlayerProgressAndTakeAnyHearts.Apply()
        // StartingItemsAndExtras
        data.StartingItemsAndExtras.Apply()
        // Blockers
        for i = 0 to 7 do
            for j = 0 to 1 do
                TrackerModel.DungeonBlockersContainer.SetDungeonBlocker(i,j,TrackerModel.DungeonBlocker.FromHotKeyName(data.Blockers.[i].[j]))
        if anySetProblems then
            () // TODO
        // Hints
        for i = 0 to 10 do
            TrackerModel.SetLevelHint(i, TrackerModel.HintZone.FromIndex(data.Hints.LocationHints.[i]))
        TrackerModel.NoFeatOfStrengthHintWasGiven <- data.Hints.NoFeatOfStrengthHint
        TrackerModel.SailNotHintWasGiven <- data.Hints.SailNotHint
        hideFeatsOfStrength TrackerModel.NoFeatOfStrengthHintWasGiven 
        hideRaftSpots TrackerModel.SailNotHintWasGiven
        // Notes
        notesTextBox.Text <- data.Notes
        // Dungeon Maps
        importDungeonModels(data.DungeonMaps)
        // done
        silenceAllRemindersDuringCurrentLoad <- false
    | _ -> ()

    TrackerModel.forceUpdate()
    timer.Start()  // don't start the tick timer updating, until the entire app is loaded
    return updateTimeline
    }

