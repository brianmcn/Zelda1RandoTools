open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let voice = new System.Speech.Synthesis.SpeechSynthesizer()
let mutable voiceRemindersForRecorder = true
let mutable voiceRemindersForPowerBracelet = true

let canvasAdd(c:Canvas, item, left, top) =
    if item <> null then
        c.Children.Add(item) |> ignore
        Canvas.SetTop(item, top)
        Canvas.SetLeft(item, left)
let gridAdd(g:Grid, x, c, r) =
    g.Children.Add(x) |> ignore
    Grid.SetColumn(x, c)
    Grid.SetRow(x, r)
let makeGrid(nc, nr, cw, rh) =
    let grid = new Grid()
    for i = 0 to nc do
        grid.ColumnDefinitions.Add(new ColumnDefinition(Width=GridLength(float cw)))
    for i = 0 to nr do
        grid.RowDefinitions.Add(new RowDefinition(Height=GridLength(float rh)))
    grid

type ItemState(whichItems:FrameworkElement[]) =
    let mutable state = -1
    member this.Current() =
        if state = -1 then
            null
        else
            whichItems.[state]
    member private this.Impl(forward) = 
        if forward then 
            state <- state + 1
        else
            state <- state - 1
        if state < -1 then
            state <- whichItems.Length-1
        if state >= whichItems.Length then
            state <- -1
        if state <> -1 && whichItems.[state].Parent <> null then
            if forward then this.Next() else this.Prev()
        elif state = -1 then
            null
        else
            whichItems.[state]
    member this.Next() = this.Impl(true)
    member this.Prev() = this.Impl(false)

let speechRecognizer = new System.Speech.Recognition.SpeechRecognitionEngine()
let mapStatePhrases = [|
        "sword three"
        "sword two"
        "blue ring shop"
        "meat shop"
        "key shop"
        "candle shop"
        "book shop"
        "bomb shop"
        "arrow shop"
        "take any"
        "potion shop"
        "money"
        |]
let wakePhrase = "tracker set"
speechRecognizer.LoadGrammar(new System.Speech.Recognition.Grammar(let gb = new System.Speech.Recognition.GrammarBuilder(wakePhrase) in gb.Append(new System.Speech.Recognition.Choices(mapStatePhrases)); gb))
type MapState() =
    let mutable state = -1
    let U = Graphics.uniqueMapIcons.Length 
    let NU = Graphics.nonUniqueMapIconBMPs.Length
    member this.SetStateTo(phrase:string) =
        let phrase = phrase.Substring(wakePhrase.Length+1)
        state <- U+NU-1-mapStatePhrases.Length + Array.IndexOf(mapStatePhrases, phrase)
        this.Current()
    member this.SetStateToX() =   // sets to final state ('X' icon)
        state <- U+NU-1
        this.Current()
    member this.State = state
    member this.IsX = state = U+NU-1
    member this.IsUnique = state >= 0 && state < U
    member this.IsDungeon = state >= 0 && state < 9
    member this.IsWarp = state >= 9 && state < 13
    member this.IsSword3 = state=13
    member this.IsSword2 = state=14
    member this.HasTransparency = state >= 0 && state < 13 || state >= U && state < U+7   // dungeons, warps, swords, and shops
    member this.IsInteresting = not(state = -1 || this.IsX)
    member this.Current() =
        if state = -1 then
            null
        elif state < U then
            Graphics.uniqueMapIcons.[state]
        else
            Graphics.BMPtoImage Graphics.nonUniqueMapIconBMPs.[state-U]
    member private this.Impl(forward) = 
        if forward then 
            state <- state + 1
        else
            state <- state - 1
        if state < -1 then
            state <- U+NU-1
        if state >= U+NU then
            state <- -1
        if state >=0 && state < U && Graphics.uniqueMapIcons.[state].Parent <> null then
            if forward then this.Next() else this.Prev()
        else this.Current()
    member this.Next() = this.Impl(true)
    member this.Prev() = this.Impl(false)

let mutable recordering = fun() -> ()
let mutable refreshOW = fun() -> ()
let mutable refreshRouteDrawing = fun() -> ()
let currentRecorderWarpDestinations = ResizeArray()
let currentAnyRoadDestinations = new System.Collections.Generic.HashSet<_>()
let mutable haveRecorder = false
let mutable haveLadder = false
let mutable haveCoastItem = false
let mutable haveWhiteSwordItem = false
let mutable havePowerBracelet = false
let mutable haveRaft = false
let mutable haveMagicalSword = false
let mutable playerHearts = 3  // start with 3
let mutable owSpotsRemain = -1
let mutable owWhistleSpotsRemain = 0
let mutable owPreviouslyAnnouncedWhistleSpotsRemain = 0
let mutable owPowerBraceletSpotsRemain = 0
let mutable owPreviouslyAnnouncedPowerBraceletSpotsRemain = 0
let triforces = Array.zeroCreate 8   // bools - do we have this triforce
let foundDungeon = Array.zeroCreate 8   // bools - have we found this dungeon yet (based on overworld marks)
let completedDungeon = Array.zeroCreate 8   // bools - do we have all items (and triforce) from this dungeon
let mutable previouslyAnnouncedFoundDungeonCount = 0
let mutable foundWhiteSwordLocation = false
let mutable foundMagicalSwordLocation = false
let triforceInnerCanvases = Array.zeroCreate 8
let owRouteworthySpots = Array2D.create 16 8 false
let owCurrentState = Array2D.create 16 8 -1
let dungeonRemains = [| 4; 3; 3; 3; 3; 3; 3; 4 |]
let mainTrackerCanvases : Canvas[,] = Array2D.zeroCreate 8 4
let mainTrackerCanvasShaders : Canvas[,] = Array2D.init 8 4 (fun _ _ -> new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black, Opacity=0.4))
let currentHeartsTextBox = new TextBox(Width=200., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "Current Hearts: %d" playerHearts)
let owRemainingScreensCheckBox = new CheckBox(Content = new TextBox(Width=200., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "OW spots left: %d" owSpotsRemain))
let haveAnnouncedHearts = Array.zeroCreate 17
let updateTotalHearts(x) = 
    playerHearts <- playerHearts + x
    currentHeartsTextBox.Text <- sprintf "Current Hearts: %d" playerHearts
    if playerHearts >=4 && playerHearts <= 6 && not haveAnnouncedHearts.[playerHearts] then
        haveAnnouncedHearts.[playerHearts] <- true
        if not haveWhiteSwordItem && foundWhiteSwordLocation then
            async { voice.Speak("Consider getting the white sword item") } |> Async.Start
    if playerHearts >=10 && playerHearts <= 14 && not haveAnnouncedHearts.[playerHearts] then
        haveAnnouncedHearts.[playerHearts] <- true
        if not haveMagicalSword && foundMagicalSwordLocation then
            async { voice.Speak("Consider the magical sword") } |> Async.Start
    if not haveMagicalSword && foundMagicalSwordLocation then
        recordering() // make sword3 blink if gettable
let updateOWSpotsRemain(delta) = 
    owSpotsRemain <- owSpotsRemain + delta
    (owRemainingScreensCheckBox.Content :?> TextBox).Text <- sprintf "OW spots left: %d" owSpotsRemain
let updateDungeon(dungeonIndex, itemDiff) =
    if dungeonIndex >= 0 && dungeonIndex < 8 then
        let priorComplete = dungeonRemains.[dungeonIndex] = 0
        dungeonRemains.[dungeonIndex] <- dungeonRemains.[dungeonIndex] + itemDiff
        if not priorComplete && dungeonRemains.[dungeonIndex] = 0 then
            completedDungeon.[dungeonIndex] <- true
            recordering()
            async { voice.Speak(sprintf "Dungeon %d is complete" (dungeonIndex+1)) } |> Async.Start
            for j = 0 to 3 do
                mainTrackerCanvases.[dungeonIndex,j].Children.Add(mainTrackerCanvasShaders.[dungeonIndex,j]) |> ignore
        elif priorComplete && not(dungeonRemains.[dungeonIndex] = 0) then
            completedDungeon.[dungeonIndex] <- false
            recordering()
            for j = 0 to 3 do
                mainTrackerCanvases.[dungeonIndex,j].Children.Remove(mainTrackerCanvasShaders.[dungeonIndex,j]) |> ignore
let foundDungeonAnnouncmentCheck() =
    let curFound = (foundDungeon |> FSharp.Collections.Array.filter id).Length 
    let cxt = System.Threading.SynchronizationContext.Current 
    if curFound > previouslyAnnouncedFoundDungeonCount then
        async { 
            do! Async.Sleep(5000) // might just be scrolling by, see if still true 5s later
            do! Async.SwitchToContext(cxt)
            let curFound = (foundDungeon |> FSharp.Collections.Array.filter id).Length 
            if curFound > previouslyAnnouncedFoundDungeonCount then
                previouslyAnnouncedFoundDungeonCount <- curFound 
                do! Async.SwitchToThreadPool()
                if curFound = 1 then
                    voice.Speak("You have located one dungeon") 
                elif curFound = 8 then
                    voice.Speak("Congratulations, you have located all 8 dungeons") 
                else
                    voice.Speak(sprintf "You have located %d dungeons" curFound) 
        } |> Async.Start
let debug() =
    for j = 0 to 7 do
        for i = 0 to 15 do
            printf "%3d " owCurrentState.[i,j]
        printfn ""
    printfn ""

type TimelineItem(c:Canvas, isDone:unit->bool) =
    member this.Canvas = c
    member this.IsHeart() = 
        if Graphics.fullHearts |> Array.exists (fun x -> c.Children.Contains(x)) then
            true
        elif Graphics.owHeartsFull |> Array.exists (fun x -> c.Children.Contains(x)) then
            true
        elif Graphics.allItemsWithHeartShuffle.[14..] |> Array.exists (fun x -> c.Children.Contains(x)) then
            true
        else
            false
    member this.IsDone() = isDone()

let mutable f5WasRecentlyPressed = false
let mutable currentlyMousedOWX, currentlyMousedOWY = -1, -1
let mutable startIconX, startIconY = -1, -1

let mutable notesTextBox = null : TextBox
let mutable timeTextBox = null : TextBox
let H = 30
let RIGHT_COL = 560.
let TLH = (1+9+5+9)*3  // timeline height
let TH = 24 // text height
let makeAll(isHeartShuffle,owMapNum) =
    let timelineItems = ResizeArray()
    let stringReverse (s:string) = new string(s.ToCharArray() |> Array.rev)
    let owMapBMPs, isReflected, isMixed, owInstance =
        match owMapNum with
        | 0 -> Graphics.overworldMapBMPs(0), false, false, new OverworldData.OverworldInstance(OverworldData.FIRST)
        | 1 -> Graphics.overworldMapBMPs(1), false, false, new OverworldData.OverworldInstance(OverworldData.SECOND)
        | 2 -> Graphics.overworldMapBMPs(2), false, true,  new OverworldData.OverworldInstance(OverworldData.MIXED_FIRST)
        | 3 -> Graphics.overworldMapBMPs(3), false, true,  new OverworldData.OverworldInstance(OverworldData.MIXED_SECOND)
        | 4 -> Graphics.overworldMapBMPs(4), true, false,  new OverworldData.OverworldInstance(OverworldData.FIRST)
        | 5 -> Graphics.overworldMapBMPs(5), true, false,  new OverworldData.OverworldInstance(OverworldData.SECOND)
        | 6 -> Graphics.overworldMapBMPs(6), true, true,   new OverworldData.OverworldInstance(OverworldData.MIXED_FIRST)
        | 7 -> Graphics.overworldMapBMPs(7), true, true,   new OverworldData.OverworldInstance(OverworldData.MIXED_SECOND)
        | _ -> failwith "bad/unsupported owMapNum"
    let whichItems = 
        if isHeartShuffle then
            Graphics.allItemsWithHeartShuffle 
        else
            Graphics.allItems
    let bookOrMagicalShieldVB = whichItems.[0].Fill :?> VisualBrush
    let isCurrentlyBook = ref true
    let toggleBookMagicalShield() =
        if !isCurrentlyBook then
            bookOrMagicalShieldVB.Visual <- Graphics.magic_shield_image 
        else
            bookOrMagicalShieldVB.Visual <- Graphics.book_image
        isCurrentlyBook := not !isCurrentlyBook
    
    let c = new Canvas()
    c.Width <- float(16*16*3)

    c.Background <- System.Windows.Media.Brushes.Black 

    let mainTracker = makeGrid(9, 4, H, H)
    canvasAdd(c, mainTracker, 0., 0.)

    // triforce
    let updateEmptyTriforceDisplay(i) =
        let innerc : Canvas = triforceInnerCanvases.[i]
        innerc.Children.Clear()
        innerc.Children.Add(if not foundDungeon.[i] then Graphics.emptyUnfoundTriforces.[i] else Graphics.emptyFoundTriforces.[i]) |> ignore
    for i = 0 to 7 do
        let image = Graphics.emptyUnfoundTriforces.[i]
        let c = new Canvas(Width=30., Height=30.)
        mainTrackerCanvases.[i,0] <- c
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has triforce drawn on it, not the eventual shading of updateDungeon()
        triforceInnerCanvases.[i] <- innerc
        c.Children.Add(innerc) |> ignore
        canvasAdd(innerc, image, 0., 0.)
        c.MouseDown.Add(fun _ -> 
            if not triforces.[i] then 
                innerc.Children.Clear()
                innerc.Children.Add(Graphics.fullTriforces.[i]) |> ignore 
                triforces.[i] <- true
                if (triforces |> FSharp.Collections.Array.forall id) && not haveMagicalSword then
                    async { voice.Speak("Consider the magical sword before dungeon nine") } |> Async.Start
                else
                    let n = (triforces |> FSharp.Collections.Array.filter id).Length
                    if n = 1 then
                        async { voice.Speak("You now have one triforce") } |> Async.Start
                    else
                        async { voice.Speak(sprintf "You now have %d triforces" n) } |> Async.Start
                updateDungeon(i, -1)
                refreshOW()
                recordering()
            else 
                updateEmptyTriforceDisplay(i)
                triforces.[i] <- false
                updateDungeon(i, +1)
                refreshOW()
                recordering()
        )
        gridAdd(mainTracker, c, i, 0)
        timelineItems.Add(new TimelineItem(innerc, (fun()->triforces.[i])))
    let hearts = whichItems.[14..]
    let remindShortly(f, text:string) =
        let cxt = System.Threading.SynchronizationContext.Current 
        async { 
            do! Async.Sleep(30000)  // 30s
            do! Async.SwitchToContext(cxt)
            if f() then
                do! Async.SwitchToThreadPool()
                voice.Speak(text) 
        } |> Async.Start
    let boxItemImpl(dungeonIndex, isCoastItem, isWhiteSwordItem) = 
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let no = System.Windows.Media.Brushes.DarkRed
        let yes = System.Windows.Media.Brushes.LimeGreen 
        let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., Stroke=no)
        rect.StrokeThickness <- 3.0
        c.Children.Add(rect) |> ignore
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has item drawn on it, not the box
        c.Children.Add(innerc) |> ignore
        let is = new ItemState(whichItems |> Array.map (fun x -> upcast x))
        c.MouseLeftButtonDown.Add(fun _ ->
            if obj.Equals(rect.Stroke, no) then
                rect.Stroke <- yes
                updateDungeon(dungeonIndex, -1)
                if hearts |> Array.exists(fun x -> obj.Equals(is.Current(), x)) then
                    updateTotalHearts(1)
            else
                rect.Stroke <- no
                updateDungeon(dungeonIndex, +1)
                if hearts |> Array.exists(fun x -> obj.Equals(is.Current(), x)) then
                    updateTotalHearts(-1)
            if obj.Equals(is.Current(), Graphics.recorder) then
                haveRecorder <- obj.Equals(rect.Stroke, yes)
                recordering()
            if obj.Equals(is.Current(), Graphics.ladder) then
                haveLadder <- obj.Equals(rect.Stroke, yes)
                remindShortly((fun() ->(obj.Equals(is.Current(), Graphics.ladder) && obj.Equals(rect.Stroke, yes))), "Don't forget that you have the ladder")
            if obj.Equals(is.Current(), Graphics.key) && obj.Equals(rect.Stroke,yes) then
                remindShortly((fun() ->(obj.Equals(is.Current(), Graphics.key) && obj.Equals(rect.Stroke, yes))), "Don't forget that you have the any key")
            if obj.Equals(is.Current(), Graphics.power_bracelet) then
                havePowerBracelet <- obj.Equals(rect.Stroke, yes)
            if obj.Equals(is.Current(), Graphics.raft) then
                haveRaft <- obj.Equals(rect.Stroke, yes)
            if isCoastItem then
                haveCoastItem <- obj.Equals(rect.Stroke, yes)
            if isWhiteSwordItem then
                haveWhiteSwordItem <- obj.Equals(rect.Stroke, yes)
            // almost any change might affect completed-dungeons state, requiring overworld refresh for routeworthy-ness
            refreshOW()  
            refreshRouteDrawing()
        )
        // item
        c.MouseWheel.Add(fun x -> 
            if obj.Equals(is.Current(), Graphics.recorder) && haveRecorder then
                haveRecorder <- false
                recordering()
            if obj.Equals(is.Current(), Graphics.ladder) && haveLadder then
                haveLadder <- false
            if obj.Equals(is.Current(), Graphics.power_bracelet) && havePowerBracelet then
                havePowerBracelet <- false
            if obj.Equals(is.Current(), Graphics.raft) && haveRaft then
                haveRaft <- false
            if hearts |> Array.exists(fun x -> obj.Equals(is.Current(), x)) && obj.Equals(rect.Stroke, yes) then
                updateTotalHearts(-1)
            innerc.Children.Clear()
            canvasAdd(innerc, (if x.Delta<0 then is.Next() else is.Prev()), 4., 4.)
            if obj.Equals(is.Current(), Graphics.recorder) && obj.Equals(rect.Stroke,yes) then
                haveRecorder <- true
                recordering()
            if obj.Equals(is.Current(), Graphics.ladder) && obj.Equals(rect.Stroke,yes) then
                haveLadder <- true
                remindShortly((fun() ->(obj.Equals(is.Current(), Graphics.ladder) && obj.Equals(rect.Stroke, yes))), "Don't forget that you have the ladder")
            if obj.Equals(is.Current(), Graphics.key) && obj.Equals(rect.Stroke,yes) then
                remindShortly((fun() ->(obj.Equals(is.Current(), Graphics.key) && obj.Equals(rect.Stroke, yes))), "Don't forget that you have the any key")
            if obj.Equals(is.Current(), Graphics.power_bracelet) && obj.Equals(rect.Stroke,yes) then
                havePowerBracelet <- true
            if obj.Equals(is.Current(), Graphics.raft) && obj.Equals(rect.Stroke,yes) then
                haveRaft <- true
            if hearts |> Array.exists(fun x -> obj.Equals(is.Current(), x)) && obj.Equals(rect.Stroke, yes) then
                updateTotalHearts(1)
            if obj.Equals(rect.Stroke, yes) then
                // almost any change to an obtained object might affect completed-dungeons state, requiring overworld refresh for routeworthy-ness
                refreshOW()  
                refreshRouteDrawing()
        )
        timelineItems.Add(new TimelineItem(innerc, (fun()->obj.Equals(rect.Stroke,yes))))
        c
    let boxItem(dungeonIndex) = 
        boxItemImpl(dungeonIndex,false,false)
    // floor hearts
    if isHeartShuffle then
        for i = 0 to 7 do
            let c = boxItem(i)
            mainTrackerCanvases.[i,1] <- c
            gridAdd(mainTracker, c, i, 1)
    else
        for i = 0 to 7 do
            let image = Graphics.emptyHearts.[i]
            let c = new Canvas(Width=30., Height=30.)
            mainTrackerCanvases.[i,1] <- c
            canvasAdd(c, image, 0., 0.)
            c.MouseDown.Add(fun _ -> 
                if c.Children.Contains(Graphics.emptyHearts.[i]) then 
                    c.Children.Clear()
                    c.Children.Add(Graphics.fullHearts.[i]) |> ignore 
                    updateTotalHearts(+1)
                    updateDungeon(i, -1)
                else 
                    c.Children.Clear()
                    c.Children.Add(Graphics.emptyHearts.[i]) |> ignore
                    updateTotalHearts(-1)
                    updateDungeon(i, +1)
            )
            gridAdd(mainTracker, c, i, 1)
            timelineItems.Add(new TimelineItem(c, fun()->c.Children.Contains(Graphics.fullHearts.[i])))

    // items
    for i = 0 to 8 do
        for j = 0 to 1 do
            let mutable c = new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black)
            if j=0 || (i=0 || i=7 || i=8) then
                c <- boxItem(i)
                gridAdd(mainTracker, c, i, j+2)
            if i < 8 then
                mainTrackerCanvases.[i,j+2] <- c

    // in mixed quest, buttons to hide first/second quest
    let mutable firstQuestOnlyInterestingMarks = Array2D.zeroCreate 16 8
    let mutable secondQuestOnlyInterestingMarks = Array2D.zeroCreate 16 8
    let thereAreMarks(questOnlyInterestingMarks:_[,]) =
        let mutable r = false
        for x = 0 to 15 do 
            for y = 0 to 7 do
                if questOnlyInterestingMarks.[x,y] then
                    r <- true
        r
    let mutable hideFirstQuestFromMixed = fun b -> ()
    let mutable hideSecondQuestFromMixed = fun b -> ()

    let hideFirstQuestCheckBox  = new CheckBox(Content=new TextBox(Text="HFQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    hideFirstQuestCheckBox.ToolTip <- "Hide First Quest\nIn a mixed quest overworld tracker, shade out the first-quest-only spots.\nUseful if you're unsure if randomizer flags are mixed quest or second quest.\nCan't be used if you've marked a first-quest-only spot as having something."
    ToolTipService.SetShowDuration(hideFirstQuestCheckBox, 12000)
    let hideSecondQuestCheckBox = new CheckBox(Content=new TextBox(Text="HSQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    hideSecondQuestCheckBox.ToolTip <- "Hide Second Quest\nIn a mixed quest overworld tracker, shade out the second-quest-only spots.\nUseful if you're unsure if randomizer flags are mixed quest or first quest.\nCan't be used if you've marked a second-quest-only spot as having something."
    ToolTipService.SetShowDuration(hideSecondQuestCheckBox, 12000)

    hideFirstQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
    hideFirstQuestCheckBox.Checked.Add(fun _ -> 
        if thereAreMarks(firstQuestOnlyInterestingMarks) then
            System.Media.SystemSounds.Asterisk.Play()
            hideFirstQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        else
            hideFirstQuestFromMixed false
        hideSecondQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        )
    hideFirstQuestCheckBox.Unchecked.Add(fun _ -> hideFirstQuestFromMixed true)
    if isMixed then
        canvasAdd(c, hideFirstQuestCheckBox, 35., 100.) 

    hideSecondQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
    hideSecondQuestCheckBox.Checked.Add(fun _ -> 
        if thereAreMarks(secondQuestOnlyInterestingMarks) then
            System.Media.SystemSounds.Asterisk.Play()
            hideSecondQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        else
            hideSecondQuestFromMixed false
        hideFirstQuestCheckBox.IsChecked <- System.Nullable.op_Implicit false
        )
    hideSecondQuestCheckBox.Unchecked.Add(fun _ -> hideSecondQuestFromMixed true)
    if isMixed then
        canvasAdd(c, hideSecondQuestCheckBox, 160., 100.) 

    // WANT!
    let kitty = new Image()
    let imageStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("CroppedBrianKitty.png")
    kitty.Source <- System.Windows.Media.Imaging.BitmapFrame.Create(imageStream)
    canvasAdd(c, kitty, 285., 0.)

    let OFFSET = 400.
    // ow hearts
    let owHeartGrid = makeGrid(4, 1, 30, 30)
    for i = 0 to 3 do
        let c = new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black)
        let image = Graphics.owHeartsEmpty.[i]
        canvasAdd(c, image, 0., 0.)
        let f b =
            let cur = 
                if c.Children.Contains(Graphics.owHeartsEmpty.[i]) then 0
                elif c.Children.Contains(Graphics.owHeartsFull.[i]) then 1
                else 2
            c.Children.Clear()
            let next = (cur + (if b then 1 else -1) + 3) % 3
            canvasAdd(c, (  if next = 0 then 
                                updateTotalHearts(0-(if cur=1 then 1 else 0))
                                Graphics.owHeartsEmpty.[i] 
                            elif next = 1 then 
                                updateTotalHearts(1-(if cur=1 then 1 else 0))
                                Graphics.owHeartsFull.[i] 
                            else 
                                updateTotalHearts(0-(if cur=1 then 1 else 0))
                                Graphics.owHeartsSkipped.[i]), 0., 0.)
        c.MouseLeftButtonDown.Add(fun _ -> f true)
        c.MouseRightButtonDown.Add(fun _ -> f false)
        c.MouseWheel.Add(fun x -> f (x.Delta<0))
        gridAdd(owHeartGrid, c, i, 0)
        timelineItems.Add(new TimelineItem(c, fun()->c.Children.Contains(Graphics.owHeartsFull.[i])))
    canvasAdd(c, owHeartGrid, OFFSET, 0.)
    // ladder, armos, white sword items
    let owItemGrid = makeGrid(2, 3, 30, 30)
    gridAdd(owItemGrid, Graphics.ow_key_ladder, 0, 0)
    gridAdd(owItemGrid, Graphics.ow_key_armos, 0, 1)
    gridAdd(owItemGrid, Graphics.ow_key_white_sword, 0, 2)
    gridAdd(owItemGrid, boxItemImpl(-1,true,false), 1, 0)
    gridAdd(owItemGrid, boxItemImpl(-1,false,false), 1, 1)
    gridAdd(owItemGrid, boxItemImpl(-1,false,true), 1, 2)
    canvasAdd(c, owItemGrid, OFFSET, 30.)
    // brown sword, blue candle, blue ring, magical sword
    let owItemGrid = makeGrid(2, 2, 30, 30)
    let veryBasicBoxImpl(img, startOn, isTimeline, changedFunc) =
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let no = System.Windows.Media.Brushes.DarkRed
        let yes = System.Windows.Media.Brushes.LimeGreen 
        let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., Stroke=if startOn then yes else no)
        rect.StrokeThickness <- 3.0
        c.Children.Add(rect) |> ignore
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has item drawn on it, not the box
        c.Children.Add(innerc) |> ignore
        c.MouseLeftButtonDown.Add(fun _ ->
            if obj.Equals(rect.Stroke, no) then
                rect.Stroke <- yes
            else
                rect.Stroke <- no
            changedFunc(obj.Equals(rect.Stroke, yes))
        )
        canvasAdd(innerc, img, 4., 4.)
        if isTimeline then
            timelineItems.Add(new TimelineItem(innerc, fun()->obj.Equals(rect.Stroke,yes)))
        c
    let basicBoxImpl(tts, img, changedFunc) =
        let c = veryBasicBoxImpl(img, false, true, changedFunc)
        c.ToolTip <- tts
        c
    gridAdd(owItemGrid, basicBoxImpl("Acquired wood sword (mark timeline)",    Graphics.brown_sword  , (fun _ -> ())), 0, 0)
    gridAdd(owItemGrid, basicBoxImpl("Acquired blue candle (mark timeline)",   Graphics.blue_candle  , (fun _ -> ())), 0, 1)
    gridAdd(owItemGrid, basicBoxImpl("Acquired blue ring (mark timeline)",     Graphics.blue_ring    , (fun _ -> ())), 1, 0)
    gridAdd(owItemGrid, basicBoxImpl("Acquired magical sword (mark timeline)", Graphics.magical_sword, (fun b -> haveMagicalSword <- b; refreshOW(); recordering())), 1, 1)
    canvasAdd(c, owItemGrid, OFFSET+90., 30.)
    // boomstick book, to mark when purchase in boomstick seed (normal book would still be used to mark finding shield in dungeon)
    canvasAdd(c, basicBoxImpl("Purchased boomstick book (mark timeline)", Graphics.boom_book, (fun _ -> ())), OFFSET+120., 0.)
    // mark the dungeon wins on timeline via ganon/zelda boxes
    canvasAdd(c, basicBoxImpl("Killed Ganon (mark timeline)",  Graphics.ganon, (fun _ -> ())), OFFSET+90., 90.)
    canvasAdd(c, basicBoxImpl("Rescued Zelda (mark timeline)", Graphics.zelda, (fun b -> if b then notesTextBox.Text <- notesTextBox.Text + "\n" + timeTextBox.Text)), OFFSET+120., 90.)

    // shield versus book icon (for boomstick flags/seeds)
    let toggleBookShieldCheckBox  = new CheckBox(Content=new TextBox(Text="S/B",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    toggleBookShieldCheckBox.ToolTip <- "Shield item icon instead of book item icon"
    toggleBookShieldCheckBox.IsChecked <- System.Nullable.op_Implicit false
    toggleBookShieldCheckBox.Checked.Add(fun _ -> toggleBookMagicalShield())
    toggleBookShieldCheckBox.Unchecked.Add(fun _ -> toggleBookMagicalShield())
    canvasAdd(c, toggleBookShieldCheckBox, OFFSET+150., 0.)

    // ow map animation layer
    let fasterBlinkAnimation = new System.Windows.Media.Animation.DoubleAnimation(From=System.Nullable(0.0), To=System.Nullable(0.6), Duration=new Duration(System.TimeSpan.FromSeconds(1.0)), 
                                  AutoReverse=true, RepeatBehavior=System.Windows.Media.Animation.RepeatBehavior.Forever)
    let slowerBlinkAnimation = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames(Duration=new Duration(System.TimeSpan.FromSeconds(4.0)), RepeatBehavior=System.Windows.Media.Animation.RepeatBehavior.Forever)
    slowerBlinkAnimation.KeyFrames.Add(System.Windows.Media.Animation.LinearDoubleKeyFrame(Value=0.2, KeyTime=System.Windows.Media.Animation.KeyTime.FromPercent(0.0))) |> ignore
    slowerBlinkAnimation.KeyFrames.Add(System.Windows.Media.Animation.LinearDoubleKeyFrame(Value=0.5, KeyTime=System.Windows.Media.Animation.KeyTime.FromPercent(0.25))) |> ignore
    slowerBlinkAnimation.KeyFrames.Add(System.Windows.Media.Animation.LinearDoubleKeyFrame(Value=0.2, KeyTime=System.Windows.Media.Animation.KeyTime.FromPercent(0.5))) |> ignore
    slowerBlinkAnimation.KeyFrames.Add(System.Windows.Media.Animation.LinearDoubleKeyFrame(Value=0.2, KeyTime=System.Windows.Media.Animation.KeyTime.FromPercent(1.0))) |> ignore
    let owRemainSpotHighlighters = Array2D.init 16 8 (fun i j ->
        let rect = new Canvas(Width=float(16*3), Height=float(11*3), Background=System.Windows.Media.Brushes.Lime)
        rect.BeginAnimation(UIElement.OpacityProperty, slowerBlinkAnimation)
        rect
        )

    // ow map opaque fixed bottom layer
    let X_OPACITY = 0.4
    let owOpaqueMapGrid = makeGrid(16, 8, 16*3, 11*3)
    owOpaqueMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    owSpotsRemain <- 16*8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let image = Graphics.BMPtoImage(owMapBMPs.[i,j])
            let c = new Canvas(Width=float(16*3), Height=float(11*3))
            canvasAdd(c, image, 0., 0.)
            gridAdd(owOpaqueMapGrid, c, i, j)
            // shading between map tiles
            let OPA = 0.25
            let bottomShade = new Canvas(Width=float(16*3), Height=float(3), Background=System.Windows.Media.Brushes.Black, Opacity=OPA)
            canvasAdd(c, bottomShade, 0., float(10*3))
            let rightShade  = new Canvas(Width=float(3), Height=float(11*3), Background=System.Windows.Media.Brushes.Black, Opacity=OPA)
            canvasAdd(c, rightShade, float(15*3), 0.)
            // permanent icons
            let ms = new MapState()
            if owInstance.AlwaysEmpty(i,j) then  // TODO handle mirror
                let icon = ms.Prev()
                owCurrentState.[i,j] <- ms.State 
                owSpotsRemain <- owSpotsRemain - 1
                icon.Opacity <- X_OPACITY
                canvasAdd(c, icon, 0., 0.)
    canvasAdd(c, owOpaqueMapGrid, 0., 120.)

    // layer to place darkening icons - dynamic icons that are below route-drawing but above the fixed base layer
    // this layer is also used to draw map icons that get drawn below routing, such as potion shops
    let owDarkeningMapGrid = makeGrid(16, 8, 16*3, 11*3)
    let owDarkeningMapGridCanvases = Array2D.zeroCreate 16 8
    owDarkeningMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=float(16*3), Height=float(11*3))
            gridAdd(owDarkeningMapGrid, c, i, j)
            owDarkeningMapGridCanvases.[i,j] <- c
    canvasAdd(c, owDarkeningMapGrid, 0., 120.)

    // layer to place 'hiding' icons - dynamic darkening icons that are below route-drawing but above the previous layers
    let owHidingMapGrid = makeGrid(16, 8, 16*3, 11*3)
    let owHidingMapGridCanvases = Array2D.zeroCreate 16 8
    owHidingMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=float(16*3), Height=float(11*3))
            gridAdd(owHidingMapGrid, c, i, j)
            owHidingMapGridCanvases.[i,j] <- c
    canvasAdd(c, owHidingMapGrid, 0., 120.)
    let hide(x,y) =
        let hideColor = Brushes.DarkSlateGray // Brushes.Black
        let hideOpacity = 0.6 // 0.4
        let rect = new System.Windows.Shapes.Rectangle(Width=7.0, Height=float(11*3)-1.5, Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
        canvasAdd(owHidingMapGridCanvases.[x,y], rect, 7., 0.)
        let rect = new System.Windows.Shapes.Rectangle(Width=7.0, Height=float(11*3)-1.5, Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
        canvasAdd(owHidingMapGridCanvases.[x,y], rect, 19., 0.)
        let rect = new System.Windows.Shapes.Rectangle(Width=7.0, Height=float(11*3)-1.5, Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
        canvasAdd(owHidingMapGridCanvases.[x,y], rect, 32., 0.)
    hideSecondQuestFromMixed <- 
        (fun unhide ->  // make mixed appear reduced to 1st quest
            for x = 0 to 15 do
                for y = 0 to 7 do
                    // TODO handle mirror
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
                    // TODO handle mirror
                    if OverworldData.owMapSquaresFirstQuestOnly.[y].Chars(x) = 'X' then
                        if unhide then
                            owHidingMapGridCanvases.[x,y].Children.Clear()
                        else
                            hide(x,y)
        )

    // ow route drawing layer
    let routeDrawingCanvas = new Canvas(Width=float(16*16*3), Height=float(8*11*3))
    routeDrawingCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(c, routeDrawingCanvas, 0., 120.)

    // ow map
    let owMapGrid = makeGrid(16, 8, 16*3, 11*3)
    let owUpdateFunctions = Array2D.create 16 8 (fun _ _ -> ())
    let drawRectangleCornersHighlight(c,x,y,color) =
(*
        // when originally was full rectangle (which badly obscured routing paths)
        let rect = new System.Windows.Shapes.Rectangle(Width=float(16*3)-4., Height=float(11*3)-4., Stroke=System.Windows.Media.Brushes.Yellow, StrokeThickness = 3.)
        canvasAdd(c, rect, x*float(16*3)+2., float(y*11*3)+2.)
*)
        let L1,L2,R1,R2 = 0.0, 16.0, 28.0, 44.0
        let T1,T2,B1,B2 = 0.0, 10.0, 19.0, 29.0
        let s = new System.Windows.Shapes.Line(X1=L1, X2=L2, Y1=T1+1.5, Y2=T1+1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*float(16*3)+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=L1+1.5, X2=L1+1.5, Y1=T1, Y2=T2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*float(16*3)+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=L1, X2=L2, Y1=B2-1.5, Y2=B2-1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*float(16*3)+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=L1+1.5, X2=L1+1.5, Y1=B1, Y2=B2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*float(16*3)+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R1, X2=R2, Y1=T1+1.5, Y2=T1+1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*float(16*3)+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R2-1.5, X2=R2-1.5, Y1=T1, Y2=T2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*float(16*3)+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R1, X2=R2, Y1=B2-1.5, Y2=B2-1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*float(16*3)+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R2-1.5, X2=R2-1.5, Y1=B1, Y2=B2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*float(16*3)+2., float(y*11*3)+2.)
    let drawDungeonHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,System.Windows.Media.Brushes.Yellow)
    let drawCompletedDungeonHighlight(c,x,y) =
        // darkened rectangle corners
        let yellow = System.Windows.Media.Brushes.Yellow.Color
        let darkYellow = Color.FromRgb(yellow.R/2uy, yellow.G/2uy, yellow.B/2uy)
        drawRectangleCornersHighlight(c,x,y,new SolidColorBrush(darkYellow))
        // darken the number
        let rect = 
            new System.Windows.Shapes.Rectangle(Width=15.0, Height=21.0, Stroke=System.Windows.Media.Brushes.Black, StrokeThickness = 3.,
                Fill=System.Windows.Media.Brushes.Black, Opacity=0.4)
        canvasAdd(c, rect, x*float(16*3)+15.0, float(y*11*3)+6.0)
    let drawWarpHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,System.Windows.Media.Brushes.Aqua)
    let drawDarkening(c,x,y) =
        let rect = 
            new System.Windows.Shapes.Rectangle(Width=float(16*3)-1.5, Height=float(11*3)-1.5, Stroke=System.Windows.Media.Brushes.Black, StrokeThickness = 3.,
                Fill=System.Windows.Media.Brushes.Black, Opacity=0.4)
        canvasAdd(c, rect, x*float(16*3), float(y*11*3))
    let drawDungeonRecorderWarpHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,System.Windows.Media.Brushes.Lime)
    let drawWhistleableHighlight(c,x,y) =
        let rect = new System.Windows.Shapes.Rectangle(Width=float(16*3)-2., Height=float(11*3)-2., Stroke=System.Windows.Media.Brushes.DeepSkyBlue, StrokeThickness=2.0)
        canvasAdd(c, rect, x*float(16*3)+1., float(y*11*3)+1.)
    let mutable mostRecentMouseEnterTime = DateTime.Now 
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=float(16*3), Height=float(11*3))
            gridAdd(owMapGrid, c, i, j)
            // we need a dummy image to make the canvas absorb the mouse interactions, so just re-draw the map at 0 opacity
            let image = Graphics.BMPtoImage(owMapBMPs.[i,j])
            image.Opacity <- 0.0
            canvasAdd(c, image, 0., 0.)
            // highlight mouse, do mouse-sensitive stuff
            let rect = new System.Windows.Shapes.Rectangle(Width=float(16*3)-4., Height=float(11*3)-4., Stroke=System.Windows.Media.Brushes.White)
            c.MouseEnter.Add(fun ea ->  canvasAdd(c, rect, 2., 2.)
                                        // draw routes if desired
                                        if true then // TODO - do i want this always on?
                                            OverworldRouting.drawPaths(routeDrawingCanvas, owRouteworthySpots, ea.GetPosition(c), i, j)
                                        // track current location for F5 & speech recognition purposes
                                        currentlyMousedOWX <- i
                                        currentlyMousedOWY <- j
                                        mostRecentMouseEnterTime <- DateTime.Now)
            c.MouseLeave.Add(fun _ -> c.Children.Remove(rect) |> ignore
                                      routeDrawingCanvas.Children.Clear())
            // icon
            let ms = new MapState()
            if owInstance.AlwaysEmpty(i,j) then // TODO handle mirror
                () // already set up as permanent opaque layer, in code above
            else
                let isRaftable = owInstance.Raftable(i,j) // TODO? handle mirror overworld
                let isLadderable = owInstance.Ladderable(i,j) // TODO? handle mirror overworld
                let isWhistleable = owInstance.Whistleable(i,j)  // TODO? handle mirror overworld
                if isWhistleable then
                    drawWhistleableHighlight(c,0.,0)
                    owWhistleSpotsRemain <- owWhistleSpotsRemain + 1
                let isPowerBraceletable = owInstance.PowerBraceletable(i,j) // TODO? handle mirror overworld
                if isPowerBraceletable then
                    owPowerBraceletSpotsRemain <- owPowerBraceletSpotsRemain + 1
                let updateGridSpot delta phrase =
                    let mutable needRecordering = false
                    let prevNull = ms.Current()=null
                    if delta <> 0 then  // we are changing this grid spot...
                        if ms.IsDungeon then
                            needRecordering <- true
                            if ms.State <=7 then
                                foundDungeon.[ms.State] <- false
                                if not triforces.[ms.State] then 
                                    updateEmptyTriforceDisplay(ms.State)
                        if ms.IsWarp then
                            needRecordering <- true // any roads update route drawing
                            currentAnyRoadDestinations.Remove((i,j)) |> ignore
                        if ms.IsSword3 then
                            foundMagicalSwordLocation <- false
                            needRecordering <- true  // unblink
                        if ms.IsSword2 then
                            foundWhiteSwordLocation <- false
                        if isWhistleable && ms.State = -1 then
                            owWhistleSpotsRemain <- owWhistleSpotsRemain - 1
                        if isPowerBraceletable && ms.State = -1 then
                            owPowerBraceletSpotsRemain <- owPowerBraceletSpotsRemain - 1
                    owRouteworthySpots.[i,j] <- false  // this always gets recomputed below
                    // cant remove-by-identity because of non-uniques; remake whole canvas
                    owDarkeningMapGridCanvases.[i,j].Children.Clear()
                    c.Children.Clear()
                    // we need a dummy image to make the canvas absorb the mouse interactions, so just re-draw the map at 0 opacity
                    let image = Graphics.BMPtoImage(owMapBMPs.[i,j])
                    image.Opacity <- 0.0
                    canvasAdd(c, image, 0., 0.)
                    // figure out what new state we just interacted-to
                    let icon = if delta = 777 then (if prevNull then ms.SetStateTo(phrase) else ms.Current()) else
                                if delta = 1 then ms.Next() elif delta = -1 then ms.Prev() elif delta = 0 then ms.Current() else failwith "bad delta"
                    owCurrentState.[i,j] <- ms.State 
                    // be sure to draw in appropriate layer
                    let canvasToDrawOn =
                        if ms.HasTransparency && not ms.IsSword3 && not ms.IsSword2 then
                            if not ms.IsDungeon || (ms.State < 8 && completedDungeon.[ms.State]) then
                                drawDarkening(owDarkeningMapGridCanvases.[i,j], 0., 0)  // completed dungeons, warps, and shops get a darkened background in layer below routing
                            c
                        else
                            owDarkeningMapGridCanvases.[i,j]
                    if icon <> null then 
                        if ms.HasTransparency then
                            icon.Opacity <- 0.9
                        else
                            if ms.IsUnique then
                                icon.Opacity <- 0.6
                            elif ms.IsX then
                                icon.Opacity <- X_OPACITY
                            else
                                icon.Opacity <- 0.5
                    else
                        // spot is unmarked, note for remain counts
                        if isWhistleable then
                            drawWhistleableHighlight(canvasToDrawOn,0.,0)
                            if delta <> 0 then
                                owWhistleSpotsRemain <- owWhistleSpotsRemain + 1
                        if isPowerBraceletable && delta<>0 then
                            owPowerBraceletSpotsRemain <- owPowerBraceletSpotsRemain + 1
                    canvasAdd(canvasToDrawOn, icon, 0., 0.)
                    if ms.IsDungeon then
                        drawDungeonHighlight(canvasToDrawOn,0.,0)
                        needRecordering <- true
                        if ms.State <=7 then
                            foundDungeon.[ms.State] <- true
                            foundDungeonAnnouncmentCheck()
                            if not triforces.[ms.State] then 
                                updateEmptyTriforceDisplay(ms.State)
                            if not completedDungeon.[ms.State] then 
                                owRouteworthySpots.[i,j] <- true  // an uncompleted dungeon is routeworthy
                        if ms.State = 8 && Array.forall id triforces then
                            owRouteworthySpots.[i,j] <- true  // dungeon 9 is routeworthy if have all triforces
                    if ms.IsSword3 then
                        foundMagicalSwordLocation <- true
                        needRecordering <- true // may need to make it blink
                        if not haveMagicalSword then
                            owRouteworthySpots.[i,j] <- true  // needed mags is routeworthy
                    if ms.IsSword2 then
                        foundWhiteSwordLocation <- true
                    if ms.IsWarp then
                        drawWarpHighlight(canvasToDrawOn,0.,0)
                        currentAnyRoadDestinations.Add((i,j)) |> ignore
                        needRecordering <- true // any roads update route drawing
                    if ms.Current()=null then
                        if (isWhistleable && not haveRecorder) || (isPowerBraceletable && not havePowerBracelet) || (isRaftable && not haveRaft) || (isLadderable && not haveLadder) then
                            ()
                        else
                            owRouteworthySpots.[i,j] <- true  // an unexplored spot is routeworthy
                            if owRemainingScreensCheckBox.IsChecked.HasValue && owRemainingScreensCheckBox.IsChecked.Value then
                                canvasAdd(canvasToDrawOn, owRemainSpotHighlighters.[i,j], 0., 0.)
                    if not prevNull && ms.Current()=null then
                        updateOWSpotsRemain(1)
                    if prevNull && not(ms.Current()=null) then
                        updateOWSpotsRemain(-1)
                    if OverworldData.owMapSquaresSecondQuestOnly.[j].Chars(i) = 'X' then  // TODO handle mirror
                        secondQuestOnlyInterestingMarks.[i,j] <- ms.IsInteresting 
                    if OverworldData.owMapSquaresFirstQuestOnly.[j].Chars(i) = 'X' then  // TODO handle mirror
                        firstQuestOnlyInterestingMarks.[i,j] <- ms.IsInteresting 
                    if needRecordering then
                        recordering()
                owUpdateFunctions.[i,j] <- updateGridSpot 
                c.MouseLeftButtonDown.Add(fun _ -> 
                        updateGridSpot 1 ""
                )
                c.MouseRightButtonDown.Add(fun _ -> updateGridSpot -1 "")
                c.MouseWheel.Add(fun x -> updateGridSpot (if x.Delta<0 then 1 else -1) "")
    speechRecognizer.SpeechRecognized.Add(fun r ->
        if DateTime.Now - mostRecentMouseEnterTime < System.TimeSpan.FromSeconds(10.0) then
            c.Dispatcher.Invoke(fun () -> 
                owUpdateFunctions.[currentlyMousedOWX,currentlyMousedOWY] 777 r.Result.Text 
                )
        )
    updateOWSpotsRemain(0)
    canvasAdd(c, owMapGrid, 0., 120.)
    refreshOW <- fun () -> 
        (
            owUpdateFunctions |> Array2D.iter (fun f -> f 0 "")
            owRouteworthySpots.[15,5] <- haveLadder && not haveCoastItem // gettable coast item is routeworthy // TODO handle mirror overworld 
        )
    refreshRouteDrawing <- fun () -> 
        (
        routeDrawingCanvas.Children.Clear()
        OverworldRouting.repopulate(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations)
        )

    refreshOW()  // initialize owRouteworthySpots

    // map barriers
    let makeLineCore(x1, x2, y1, y2) = 
        let line = new System.Windows.Shapes.Line(X1=float(x1*16*3), X2=float(x2*16*3), Y1=float(y1*11*3), Y2=float(y2*11*3), Stroke=Brushes.White, StrokeThickness=3.)
        line.IsHitTestVisible <- false // transparent to mouse
        line
    let makeLine(x1, x2, y1, y2) = 
        if isReflected then
            makeLineCore(16-x1, 16-x2, y1, y2)
        else
            makeLineCore(x1,x2,y1,y2)
(*
    canvasAdd(c, makeLine(0,4,2,2), 0., 120.)
    canvasAdd(c, makeLine(2,2,1,3), 0., 120.)
    canvasAdd(c, makeLine(4,4,0,1), 0., 120.)
    canvasAdd(c, makeLine(4,7,1,1), 0., 120.)
    canvasAdd(c, makeLine(8,10,1,1), 0., 120.)
    canvasAdd(c, makeLine(10,10,0,1), 0., 120.)
    canvasAdd(c, makeLine(11,11,0,1), 0., 120.)
    canvasAdd(c, makeLine(12,12,0,1), 0., 120.)
    canvasAdd(c, makeLine(14,14,0,1), 0., 120.)
    canvasAdd(c, makeLine(15,15,0,1), 0., 120.)
    canvasAdd(c, makeLine(14,16,2,2), 0., 120.)
    canvasAdd(c, makeLine(6,7,2,2), 0., 120.)
    canvasAdd(c, makeLine(8,12,2,2), 0., 120.)
    canvasAdd(c, makeLine(4,5,3,3), 0., 120.)
    canvasAdd(c, makeLine(7,8,3,3), 0., 120.)
    canvasAdd(c, makeLine(9,10,3,3), 0., 120.)
    canvasAdd(c, makeLine(12,13,3,3), 0., 120.)
    canvasAdd(c, makeLine(2,4,4,4), 0., 120.)
    canvasAdd(c, makeLine(5,8,4,4), 0., 120.)
    canvasAdd(c, makeLine(14,15,4,4), 0., 120.)
    canvasAdd(c, makeLine(1,2,5,5), 0., 120.)
    canvasAdd(c, makeLine(7,8,5,5), 0., 120.)
    canvasAdd(c, makeLine(10,11,5,5), 0., 120.)
    canvasAdd(c, makeLine(12,13,5,5), 0., 120.)
    canvasAdd(c, makeLine(14,15,5,5), 0., 120.)
    canvasAdd(c, makeLine(6,8,6,6), 0., 120.)
    canvasAdd(c, makeLine(14,15,6,6), 0., 120.)
    canvasAdd(c, makeLine(0,1,7,7), 0., 120.)
    canvasAdd(c, makeLine(4,5,7,7), 0., 120.)
    canvasAdd(c, makeLine(9,11,7,7), 0., 120.)
    canvasAdd(c, makeLine(12,15,7,7), 0., 120.)
    canvasAdd(c, makeLine(1,1,5,6), 0., 120.)
    canvasAdd(c, makeLine(2,2,4,5), 0., 120.)
    canvasAdd(c, makeLine(3,3,2,3), 0., 120.)
    canvasAdd(c, makeLine(3,3,4,5), 0., 120.)
    canvasAdd(c, makeLine(4,4,3,5), 0., 120.)
    canvasAdd(c, makeLine(5,5,3,5), 0., 120.)
    canvasAdd(c, makeLine(5,5,7,8), 0., 120.)
    canvasAdd(c, makeLine(6,6,2,3), 0., 120.)
    canvasAdd(c, makeLine(6,6,4,5), 0., 120.)
    canvasAdd(c, makeLine(7,7,3,4), 0., 120.)
    canvasAdd(c, makeLine(9,9,3,5), 0., 120.)
    canvasAdd(c, makeLine(10,10,3,4), 0., 120.)
    canvasAdd(c, makeLine(12,12,3,5), 0., 120.)
    canvasAdd(c, makeLine(13,13,3,4), 0., 120.)
    canvasAdd(c, makeLine(14,14,3,4), 0., 120.)
    canvasAdd(c, makeLine(15,15,2,3), 0., 120.)
    canvasAdd(c, makeLine(15,15,4,6), 0., 120.)
*)

    let recorderingCanvas = new Canvas(Width=float(16*16*3), Height=float(8*11*3))  // really the 'extra top layer' canvas for adding final marks to overworld map
    recorderingCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(c, recorderingCanvas, 0., 120.)
    let startIcon = new System.Windows.Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=System.Windows.Media.Brushes.Lime, StrokeThickness=3.0)
    recordering <- (fun () ->
        recorderingCanvas.Children.Clear()
        currentRecorderWarpDestinations.Clear()
        for i = 0 to 7 do // 8 dungeons
            for x = 0 to 15 do      // 16 by
                for y = 0 to 7 do   // 8 overworld spots
                    if owCurrentState.[x,y] = i then  // if spot marked as dungeon...
                        if completedDungeon.[i] then
                            drawCompletedDungeonHighlight(recorderingCanvas,float x,y)
                        if haveRecorder && triforces.[i] then
                            // highlight any triforce dungeons as recorder warp destinations
                            drawDungeonRecorderWarpHighlight(recorderingCanvas,float x,y)
                            currentRecorderWarpDestinations.Add((x,y))
        refreshRouteDrawing()
        // highlight magical sword when it's a candidate to get
        if not haveMagicalSword && playerHearts >=10 then
            for x = 0 to 15 do
                for y = 0 to 7 do
                    if owCurrentState.[x,y] = 13 then  // sword3 = 13
                        let rect = new Canvas(Width=float(16*3), Height=float(11*3), Background=System.Windows.Media.Brushes.Pink)
                        rect.BeginAnimation(UIElement.OpacityProperty, fasterBlinkAnimation)
                        canvasAdd(recorderingCanvas, rect, float(x*16*3), float(y*11*3))
        // highlight 9 after get all triforce
        if Array.forall id triforces then
            for x = 0 to 15 do
                for y = 0 to 7 do
                    if owCurrentState.[x,y] = 8 then
                        let rect = new Canvas(Width=float(16*3), Height=float(11*3), Background=System.Windows.Media.Brushes.Pink)
                        rect.BeginAnimation(UIElement.OpacityProperty, fasterBlinkAnimation)
                        canvasAdd(recorderingCanvas, rect, float(x*16*3), float(y*11*3))
        // place start icon in top layer
        if startIconX <> -1 then
            canvasAdd(recorderingCanvas, startIcon, 8.5+float(startIconX*16*3), float(startIconY*11*3))
    )

    // map legend
    let LEFT_OFFSET = 78.0
    let legendCanvas = new Canvas()
    canvasAdd(c, legendCanvas, LEFT_OFFSET, 120. + float(8*11*3))

    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="The LEGEND\nof Z-Tracker")
    canvasAdd(c, tb, 0., 120. + float(8*11*3))

    canvasAdd(legendCanvas, Graphics.BMPtoImage Graphics.d1bmp, 0., 0.)
    drawDungeonHighlight(legendCanvas,0.,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Active\nDungeon")
    canvasAdd(legendCanvas, tb, float(16*3), 0.)

    canvasAdd(legendCanvas, Graphics.BMPtoImage Graphics.d1bmp, 2.5*float(16*3), 0.)
    drawDungeonHighlight(legendCanvas,2.5,0)
    drawCompletedDungeonHighlight(legendCanvas,2.5,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Completed\nDungeon")
    canvasAdd(legendCanvas, tb, 3.5*float(16*3), 0.)

    canvasAdd(legendCanvas, Graphics.BMPtoImage Graphics.d1bmp, 5.*float(16*3), 0.)
    drawDungeonHighlight(legendCanvas,5.,0)
    drawDungeonRecorderWarpHighlight(legendCanvas,5.,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Recorder\nDestination")
    canvasAdd(legendCanvas, tb, 6.*float(16*3), 0.)

    canvasAdd(legendCanvas, Graphics.BMPtoImage Graphics.w1bmp, 7.5*float(16*3), 0.)
    drawWarpHighlight(legendCanvas,7.5,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Any Road\n(Warp)")
    canvasAdd(legendCanvas, tb, 8.5*float(16*3), 0.)

    drawWhistleableHighlight(legendCanvas,10.,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Recorder\nSpots")
    canvasAdd(legendCanvas, tb, 11.*float(16*3), 0.)

    let legendStartIcon = new System.Windows.Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=System.Windows.Media.Brushes.Lime, StrokeThickness=3.0)
    canvasAdd(legendCanvas, legendStartIcon, 12.5*float(16*3)+8.5, 0.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Start\nSpot")
    canvasAdd(legendCanvas, tb, 13.5*float(16*3), 0.)

    let THRU_MAP_H = float(120+9*11*3)

    // timeline
    let TLC = Brushes.SandyBrown   // timeline color
    let makeTimeline(leftText, rightText) = 
        let timelineCanvas = new Canvas(Height=float TLH, Width=owMapGrid.Width)
        let tb1 = new TextBox(Text=leftText,FontSize=14.0,Background=Brushes.Black,Foreground=TLC,BorderThickness=Thickness(0.0),IsReadOnly=true)
        canvasAdd(timelineCanvas, tb1, 0., 30.)
        let tb2 = new TextBox(Text=rightText,FontSize=14.0,Background=Brushes.Black,Foreground=TLC,BorderThickness=Thickness(0.0),IsReadOnly=true)
        canvasAdd(timelineCanvas, tb2, 748., 30.)
        let line1 = new System.Windows.Shapes.Line(X1=24., X2=744., Y1=float(13*3), Y2=float(13*3), Stroke=TLC, StrokeThickness=3.)
        canvasAdd(timelineCanvas, line1, 0., 0.)
        for i = 0 to 12 do
            let d = if i%2=1 then 3 else 0
            let line = new System.Windows.Shapes.Line(X1=float(24+i*60), X2=float(24+i*60), Y1=float(11*3+d), Y2=float(15*3-d), Stroke=TLC, StrokeThickness=3.)
            canvasAdd(timelineCanvas, line, 0., 0.)
        timelineCanvas 
    let timeline1Canvas = makeTimeline("0h","1h")
    let curTime = new System.Windows.Shapes.Line(X1=float(24), X2=float(24), Y1=float(11*3), Y2=float(15*3), Stroke=Brushes.White, StrokeThickness=3.)
    canvasAdd(timeline1Canvas, curTime, 0., 0.)

    let timeline2Canvas = makeTimeline("1h","2h")
    let timeline3Canvas = makeTimeline("2h","3h")

    let top = ref true
    let updateTimeline(minute) =
        if minute < 0 || minute > 180 then
            ()
        else
            let tlc,minute = 
                if minute <= 60 then 
                    timeline1Canvas, minute 
                elif minute <= 120 then
                    timeline2Canvas, minute-60
                else 
                    timeline3Canvas, minute-120
            let items = ResizeArray()
            let hearts = ResizeArray()
            for x in timelineItems do
                if x.IsDone() then
                    if x.IsHeart() then
                        hearts.Add(x)
                    else
                        items.Add(x)
            for x in items do
                timelineItems.Remove(x) |> ignore
            for x in hearts do
                timelineItems.Remove(x) |> ignore
            // post items
            for x in items do
                let vb = new VisualBrush(Visual=x.Canvas, Opacity=1.0)
                let rect = new System.Windows.Shapes.Rectangle(Height=30., Width=30., Fill=vb)
                canvasAdd(tlc, rect, float(24+minute*12-15-1), 3.+(if !top then 0. else 42.))
                let line = new System.Windows.Shapes.Line(X1=0., X2=0., Y1=float(12*3), Y2=float(13*3), Stroke=Brushes.LightBlue, StrokeThickness=2.)
                canvasAdd(tlc, line, float(24+minute*12-1), (if !top then 0. else 3.))
                top := not !top
            // post hearts
            if hearts.Count > 0 then
                let vb = new VisualBrush(Visual=Graphics.timelineHeart, Opacity=1.0)
                let rect = new System.Windows.Shapes.Rectangle(Height=13., Width=13., Fill=vb)
                canvasAdd(tlc, rect, float(24+minute*12-3-1-2), 36. - 2.)
            // post current time
            curTime.X1 <- float(24+minute*12)
            curTime.X2 <- float(24+minute*12)
            timeline1Canvas.Children.Remove(curTime)  // have it be last
            timeline2Canvas.Children.Remove(curTime)  // have it be last
            timeline3Canvas.Children.Remove(curTime)  // have it be last
            canvasAdd(tlc, curTime, 0., 0.)
    canvasAdd(c, timeline1Canvas, 0., THRU_MAP_H)
    canvasAdd(c, timeline2Canvas, 0., THRU_MAP_H + timeline1Canvas.Height)
    canvasAdd(c, timeline3Canvas, 0., THRU_MAP_H + timeline1Canvas.Height + timeline2Canvas.Height)

    let THRU_TIMELINE_H = THRU_MAP_H + timeline1Canvas.Height + timeline2Canvas.Height + timeline3Canvas.Height + 3.

    // Level trackers
    let fixedDungeon1Outlines = ResizeArray()
    let fixedDungeon2Outlines = ResizeArray()

    let dungeonTabs = new TabControl()
    dungeonTabs.Background <- System.Windows.Media.Brushes.Black 
    canvasAdd(c, dungeonTabs , 0., THRU_TIMELINE_H)
    for level = 1 to 9 do
        let levelTab = new TabItem(Background=System.Windows.Media.Brushes.SlateGray)
        levelTab.Header <- sprintf "  %d  " level
        let dungeonCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7))

        levelTab.Content <- dungeonCanvas 
        dungeonTabs.Height <- dungeonCanvas.Height + 30.   // ok to set this 9 times
        dungeonTabs.Items.Add(levelTab) |> ignore

        let TEXT = sprintf "LEVEL-%d " level
        // horizontal doors
        let unknown = new SolidColorBrush(Color.FromRgb(55uy, 55uy, 55uy)) 
        let no = System.Windows.Media.Brushes.DarkRed
        let yes = System.Windows.Media.Brushes.Lime
        let horizontalDoorCanvases = Array2D.zeroCreate 7 8
        for i = 0 to 6 do
            for j = 0 to 7 do
                let d = new Canvas(Height=12., Width=12., Background=unknown)
                horizontalDoorCanvases.[i,j] <- d
                canvasAdd(dungeonCanvas, d, float(i*(39+12)+39), float(TH+j*(27+12)+8))
                let left _ =        
                    if not(obj.Equals(d.Background, yes)) then
                        d.Background <- yes
                    else
                        d.Background <- unknown
                d.MouseLeftButtonDown.Add(left)
                let right _ = 
                    if not(obj.Equals(d.Background, no)) then
                        d.Background <- no
                    else
                        d.Background <- unknown
                d.MouseRightButtonDown.Add(right)
                (*
                // initial values
                if (i=5 || i=6) && j=7 then
                    right()
                *)
        // vertical doors
        let verticalDoorCanvases = Array2D.zeroCreate 8 7
        for i = 0 to 7 do
            for j = 0 to 6 do
                let d = new Canvas(Height=12., Width=12., Background=unknown)
                verticalDoorCanvases.[i,j] <- d
                canvasAdd(dungeonCanvas, d, float(i*(39+12)+14), float(TH+j*(27+12)+27))
                let left _ =
                    if not(obj.Equals(d.Background, yes)) then
                        d.Background <- yes
                    else
                        d.Background <- unknown
                d.MouseLeftButtonDown.Add(left)
                let right _ = 
                    if not(obj.Equals(d.Background, no)) then
                        d.Background <- no
                    else
                        d.Background <- unknown
                d.MouseRightButtonDown.Add(right)
                (*
                // initial values
                if i=6 && j=6 then
                    left()
                *)
        // rooms
        let roomCanvases = Array2D.zeroCreate 8 8 
        let roomStates = Array2D.zeroCreate 8 8 // 0 = unexplored, 1-9 = transports, 10=vchute, 11=hchute, 12=tee, 13=tri, 14=heart, 15=start, 16=explored empty
        let ROOMS = 17 // how many types
        let usedTransports = Array.zeroCreate 10 // slot 0 unused
        for i = 0 to 7 do
            // LEVEL-9        
            let tb = new TextBox(Width=float(13*3), Height=float(TH), FontSize=float(TH-4), Foreground=Brushes.White, Background=Brushes.Black, IsReadOnly=true,
                                    Text=TEXT.Substring(i,1), BorderThickness=Thickness(0.), FontFamily=new FontFamily("Courier New"), FontWeight=FontWeights.Bold)
            canvasAdd(dungeonCanvas, tb, float(i*51)+12., 0.)
            // room map
            for j = 0 to 7 do
                let c = new Canvas(Width=float(13*3), Height=float(9*3))
                canvasAdd(dungeonCanvas, c, float(i*51), float(TH+j*39))
                let image = Graphics.BMPtoImage Graphics.dungeonUnexploredRoomBMP 
                canvasAdd(c, image, 0., 0.)
                roomCanvases.[i,j] <- c
                roomStates.[i,j] <- 0
                let f b =
                    // track transport being changed away from
                    if [1..9] |> List.contains roomStates.[i,j] then
                        usedTransports.[roomStates.[i,j]] <- usedTransports.[roomStates.[i,j]] - 1
                    // go to next state
                    roomStates.[i,j] <- ((roomStates.[i,j] + (if b then 1 else -1)) + ROOMS) % ROOMS
                    // skip transport if already used both
                    while [1..9] |> List.contains roomStates.[i,j] && usedTransports.[roomStates.[i,j]] = 2 do
                        roomStates.[i,j] <- ((roomStates.[i,j] + (if b then 1 else -1)) + ROOMS) % ROOMS
                    // note any new transports
                    if [1..9] |> List.contains roomStates.[i,j] then
                        usedTransports.[roomStates.[i,j]] <- usedTransports.[roomStates.[i,j]] + 1
                    // update UI
                    c.Children.Clear()
                    let image =
                        match roomStates.[i,j] with
                        | 0  -> Graphics.dungeonUnexploredRoomBMP 
                        | 10 -> Graphics.dungeonVChuteBMP
                        | 11 -> Graphics.dungeonHChuteBMP
                        | 12 -> Graphics.dungeonTeeBMP
                        | 13 -> Graphics.dungeonTriforceBMP 
                        | 14 -> Graphics.dungeonPrincessBMP 
                        | 15 -> Graphics.dungeonStartBMP 
                        | 16 -> Graphics.dungeonExploredRoomBMP 
                        | n  -> Graphics.dungeonNumberBMPs.[n-1]
                        |> Graphics.BMPtoImage 
                    canvasAdd(c, image, 0., 0.)
                // not allowing mouse clicks makes less likely to accidentally click room when trying to target doors with mouse
                //c.MouseLeftButtonDown.Add(fun _ -> f true)
                //c.MouseRightButtonDown.Add(fun _ -> f false)
                // shift click to mark not-on-map rooms (by "no"ing all the connections)
                c.MouseLeftButtonDown.Add(fun _ -> 
                    if System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift) then
                        if i > 0 then
                            horizontalDoorCanvases.[i-1,j].Background <- no
                        if i < 7 then
                            horizontalDoorCanvases.[i,j].Background <- no
                        if j > 0 then
                            verticalDoorCanvases.[i,j-1].Background <- no
                        if j < 7 then
                            verticalDoorCanvases.[i,j].Background <- no
                )
                c.MouseWheel.Add(fun x -> f (x.Delta<0))
                (*
                // initial values
                if i=6 && (j=6 || j=7) then
                    f false
                *)
        for quest,outlines in [| (DungeonData.firstQuest.[level-1], fixedDungeon1Outlines); (DungeonData.secondQuest.[level-1], fixedDungeon2Outlines) |] do
            // fixed dungeon drawing outlines - vertical segments
            for i = 0 to 6 do
                for j = 0 to 7 do
                    if quest.[j].Chars(i) <> quest.[j].Chars(i+1) then
                        let s = new System.Windows.Shapes.Line(X1=float(i*(39+12)+39+12/2), X2=float(i*(39+12)+39+12/2), Y1=float(TH+j*(27+12)-12/2), Y2=float(TH+j*(27+12)+27+12/2), 
                                        Stroke=Brushes.Red, StrokeThickness=3., IsHitTestVisible=false, Opacity=0.0)
                        canvasAdd(dungeonCanvas, s, 0., 0.)
                        outlines.Add(s)
            // fixed dungeon drawing outlines - horizontal segments
            for i = 0 to 7 do
                for j = 0 to 6 do
                    if quest.[j].Chars(i) <> quest.[j+1].Chars(i) then
                        let s = new System.Windows.Shapes.Line(X1=float(i*(39+12)-12/2), X2=float(i*(39+12)+39+12/2), Y1=float(TH+(j+1)*(27+12)-12/2), Y2=float(TH+(j+1)*(27+12)-12/2), 
                                        Stroke=Brushes.Red, StrokeThickness=3., IsHitTestVisible=false, Opacity=0.0)
                        canvasAdd(dungeonCanvas, s, 0., 0.)
                        outlines.Add(s)
    dungeonTabs.SelectedIndex <- 8

    let fqcb = new CheckBox(Content=new TextBox(Text="FQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    fqcb.ToolTip <- "Show vanilla first quest dungeon outlines"
    let sqcb = new CheckBox(Content=new TextBox(Text="SQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    sqcb.ToolTip <- "Show vanilla second quest dungeon outlines"

    fqcb.IsChecked <- System.Nullable.op_Implicit false
    fqcb.Checked.Add(fun _ -> fixedDungeon1Outlines |> Seq.iter (fun s -> s.Opacity <- 1.0); sqcb.IsChecked <- System.Nullable.op_Implicit false)
    fqcb.Unchecked.Add(fun _ -> fixedDungeon1Outlines |> Seq.iter (fun s -> s.Opacity <- 0.0))
    canvasAdd(c, fqcb, 310., THRU_TIMELINE_H) 

    sqcb.IsChecked <- System.Nullable.op_Implicit false
    sqcb.Checked.Add(fun _ -> fixedDungeon2Outlines |> Seq.iter (fun s -> s.Opacity <- 1.0); fqcb.IsChecked <- System.Nullable.op_Implicit false)
    sqcb.Unchecked.Add(fun _ -> fixedDungeon2Outlines |> Seq.iter (fun s -> s.Opacity <- 0.0))
    canvasAdd(c, sqcb, 360., THRU_TIMELINE_H) 

    // notes    
    let tb = new TextBox(Width=c.Width-402., Height=dungeonTabs.Height)
    notesTextBox <- tb
    tb.FontSize <- 24.
    tb.Foreground <- System.Windows.Media.Brushes.LimeGreen 
    tb.Background <- System.Windows.Media.Brushes.Black 
    tb.Text <- "Notes\n"
    tb.AcceptsReturn <- true
    canvasAdd(c, tb, 402., THRU_TIMELINE_H) 

    // audio reminders    
    let cb = new CheckBox(Content=new TextBox(Text="Audio reminders",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    cb.IsChecked <- System.Nullable.op_Implicit true
    voice.Volume <- 30
    cb.Checked.Add(fun _ -> voice.Volume <- 30)
    cb.Unchecked.Add(fun _ -> voice.Volume <- 0)
    canvasAdd(c, cb, RIGHT_COL, 60.)
    // remaining OW spots
    canvasAdd(c, owRemainingScreensCheckBox, RIGHT_COL, 80.)
    owRemainingScreensCheckBox.Checked.Add(fun _ -> refreshOW())
    owRemainingScreensCheckBox.Unchecked.Add(fun _ -> refreshOW())
    // current hearts
    canvasAdd(c, currentHeartsTextBox, RIGHT_COL, 100.)
    // audio subcategories to toggle
    let recorderAudioReminders = veryBasicBoxImpl(Graphics.recorder_audio_copy, true, false, fun b -> voiceRemindersForRecorder <- b)
    recorderAudioReminders.ToolTip <- "Periodic voice reminders about the number of remaining recorder spots"
    canvasAdd(c, recorderAudioReminders, RIGHT_COL + 140., 60.)
    let powerBraceletAudioReminders = veryBasicBoxImpl(Graphics.power_bracelet_audio_copy, true, false, fun b -> voiceRemindersForPowerBracelet <- b)
    powerBraceletAudioReminders.ToolTip <- "Periodic voice reminders about the number of remaining power bracelet spots"
    canvasAdd(c, powerBraceletAudioReminders, RIGHT_COL + 170., 60.)
    // coordinate grid
    let owCoordsGrid = makeGrid(16, 8, 16*3, 11*3)
    let owCoordsTBs = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let tb = new TextBox(Text=sprintf "%c  %d" (char (int 'A' + j)) (i+1), Foreground=Brushes.White, Background=Brushes.Transparent, BorderThickness=Thickness(0.0), 
                                    FontFamily=FontFamily("Consolas"), FontSize=16.0, FontWeight=FontWeights.Bold)
            tb.Opacity <- 0.0
            tb.IsHitTestVisible <- false // transparent to mouse
            owCoordsTBs.[i,j] <- tb
            let c = new Canvas(Width=float(16*3), Height=float(11*3))
            canvasAdd(c, tb, 2., 6.)
            let i = if isReflected then 15-i else i
            gridAdd(owCoordsGrid, c, i, j) 
    canvasAdd(c, owCoordsGrid, 0., 120.)
    let showCoords = new TextBox(Text="Coords",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true)
    let cb = new CheckBox(Content=showCoords)
    cb.IsChecked <- System.Nullable.op_Implicit false
    cb.Checked.Add(fun _ -> owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.85))
    cb.Unchecked.Add(fun _ -> owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.0))
    showCoords.MouseEnter.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.85))
    showCoords.MouseLeave.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then owCoordsTBs |> Array2D.iter (fun i -> i.Opacity <- 0.0))
    canvasAdd(c, cb, RIGHT_COL + 140., 100.)

    // zone overlay
    let owMapZoneGrid = makeGrid(16, 8, 16*3, 11*3)
    for i = 0 to 15 do
        for j = 0 to 7 do
            let image = OverworldData.owMapZoneImages.[i,j]
            image.Opacity <- 0.0
            image.IsHitTestVisible <- false // transparent to mouse
            let c = new Canvas(Width=float(16*3), Height=float(11*3))
            canvasAdd(c, image, 0., 0.)
            let i = if isReflected then 15-i else i
            gridAdd(owMapZoneGrid, c, i, j)
    canvasAdd(c, owMapZoneGrid, 0., 120.)

    let owMapZoneBoundaries = ResizeArray()
    let addLine(x1,x2,y1,y2) = 
        let line = makeLine(x1,x2,y1,y2)
        line.Opacity <- 0.0
        owMapZoneBoundaries.Add(line)
        canvasAdd(c, line, 0., 120.)
    addLine(0,7,2,2)
    addLine(7,11,1,1)
    addLine(7,7,1,2)
    addLine(10,10,0,1)
    addLine(11,11,0,2)
    addLine(8,14,2,2)
    addLine(14,14,0,2)
    addLine(6,6,2,3)
    addLine(4,4,3,4)
    addLine(2,2,4,5)
    addLine(1,1,5,7)
    addLine(0,1,7,7)
    addLine(1,4,5,5)
    addLine(2,4,4,4)
    addLine(4,6,3,3)
    addLine(4,7,6,6)
    addLine(7,12,5,5)
    addLine(9,10,4,4)
    addLine(7,10,3,3)
    addLine(7,7,2,3)
    addLine(10,10,3,4)
    addLine(9,9,4,7)
    addLine(7,7,5,6)
    addLine(4,4,5,6)
    addLine(5,5,6,8)
    addLine(6,6,6,8)
    addLine(11,11,5,8)
    addLine(9,15,7,7)
    addLine(12,12,3,5)
    addLine(13,13,2,3)
    addLine(8,8,2,3)
    addLine(12,14,3,3)
    addLine(14,15,4,4)
    addLine(15,15,4,7)
    addLine(14,14,3,4)

    let cb = new CheckBox(Content=new TextBox(Text="Show zones",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    cb.IsChecked <- System.Nullable.op_Implicit false
    cb.Checked.Add(fun _ -> OverworldData.owMapZoneImages |> Array2D.map (fun i -> i.Opacity <- 0.3) |> ignore; owMapZoneBoundaries |> Seq.iter (fun x -> x.Opacity <- 0.9))
    cb.Unchecked.Add(fun _ -> OverworldData.owMapZoneImages |> Array2D.map (fun i -> i.Opacity <- 0.0) |> ignore; owMapZoneBoundaries |> Seq.iter (fun x -> x.Opacity <- 0.0))
    canvasAdd(c, cb, 285., 100.)

    //                items  ow map   timeline    dungeon tabs                
    c.Height <- float(30*4 + 11*3*9 + 3*TLH + 3 + TH + 27*8 + 12*7 + 30)
    c, updateTimeline


// TODO
// free form text for seed flags?
// voice reminders:
//  - what else?
// TRIFORCE time splits ?
// ...2nd quest etc...

open System.Runtime.InteropServices 
module Winterop = 
    [<DllImport("User32.dll")>]
    extern bool RegisterHotKey(IntPtr hWnd,int id,uint32 fsModifiers,uint32 vk)

    [<DllImport("User32.dll")>]
    extern bool UnregisterHotKey(IntPtr hWnd,int id)

    let HOTKEY_ID = 9000

type MyWindowBase() as this = 
    inherit Window()
    let mutable source = null
    let VK_F5 = 0x74
    let VK_F10 = 0x79
    let MOD_NONE = 0u
    let mutable startTime = DateTime.Now
    do
        // full window
        let timer = new System.Windows.Threading.DispatcherTimer()
        timer.Interval <- TimeSpan.FromSeconds(1.0)
        timer.Tick.Add(fun _ -> this.Update(false))
        timer.Start()
    member this.StartTime = startTime
    abstract member Update : bool -> unit
    default this.Update(f10Press) = ()
    override this.OnSourceInitialized(e) =
        base.OnSourceInitialized(e)
        let helper = new System.Windows.Interop.WindowInteropHelper(this)
        source <- System.Windows.Interop.HwndSource.FromHwnd(helper.Handle)
        source.AddHook(System.Windows.Interop.HwndSourceHook(fun a b c d e -> this.HwndHook(a,b,c,d,&e)))
        this.RegisterHotKey()
    override this.OnClosed(e) =
        source.RemoveHook(System.Windows.Interop.HwndSourceHook(fun a b c d e -> this.HwndHook(a,b,c,d,&e)))
        source <- null
        this.UnregisterHotKey()
        base.OnClosed(e)
    member this.RegisterHotKey() =
#if DEBUG
        // in debug mode, do not register hotkeys, as I need e.g. F10 to work to use the debugger!
        ()
#else
        let helper = new System.Windows.Interop.WindowInteropHelper(this);
        if(not(Winterop.RegisterHotKey(helper.Handle, Winterop.HOTKEY_ID, MOD_NONE, uint32 VK_F10))) then
            // handle error
            ()
        if(not(Winterop.RegisterHotKey(helper.Handle, Winterop.HOTKEY_ID, MOD_NONE, uint32 VK_F5))) then
            // handle error
            ()
#endif
    member this.UnregisterHotKey() =
        let helper = new System.Windows.Interop.WindowInteropHelper(this)
        Winterop.UnregisterHotKey(helper.Handle, Winterop.HOTKEY_ID) |> ignore
    member this.HwndHook(hwnd:IntPtr, msg:int, wParam:IntPtr, lParam:IntPtr, handled:byref<bool>) : IntPtr =
        let WM_HOTKEY = 0x0312
        if msg = WM_HOTKEY then
            if wParam.ToInt32() = Winterop.HOTKEY_ID then
                //let ctrl_bits = lParam.ToInt32() &&& 0xF  // see WM_HOTKEY docs
                let key = lParam.ToInt32() >>> 16
                if key = VK_F10 then
                    startTime <- DateTime.Now
                if key = VK_F5 then
                    f5WasRecentlyPressed <- true
        IntPtr.Zero

type MyWindow(isHeartShuffle,owMapNum) as this = 
    inherit MyWindowBase()
    let mutable canvas, updateTimeline = null, fun _ -> ()
    let hmsTimeTextBox = new TextBox(Text="timer",FontSize=42.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    let mutable ladderTime        = DateTime.Now.Subtract(TimeSpan.FromMinutes(10.0)) // ladderTime        starts in past, so that can instantly work at startup for debug testing
    let mutable recorderTime      = DateTime.Now.Subtract(TimeSpan.FromMinutes(10.0)) // recorderTime      starts in past, so that can instantly work at startup for debug testing
    let mutable powerBraceletTime = DateTime.Now.Subtract(TimeSpan.FromMinutes(10.0)) // powerBraceletTime starts in past, so that can instantly work at startup for debug testing
    let da = new System.Windows.Media.Animation.DoubleAnimation(From=System.Nullable(1.0), To=System.Nullable(0.0), Duration=new Duration(System.TimeSpan.FromSeconds(0.5)), 
                AutoReverse=true, RepeatBehavior=System.Windows.Media.Animation.RepeatBehavior.Forever)
    //                 items  ow map   timeline    dungeon tabs                
    let HEIGHT = float(30*4 + 11*3*9 + 3*TLH + 3 + TH + 27*8 + 12*7 + 30 + 40) // (what is the final 40?)
    let WIDTH = float(16*16*3 + 16)  // ow map width (what is the final 16?)
    do
        timeTextBox <- hmsTimeTextBox
        // full window
        this.Title <- "Zelda 1 Randomizer"
        this.SizeToContent <- SizeToContent.Manual
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        this.Left <- 1140.0
        this.Top <- 0.0
        this.Width <- WIDTH
        this.Height <- HEIGHT
        let stackPanel = new StackPanel(Orientation=Orientation.Vertical)
        let tb = new TextBox(Text="Choose overworld quest:")
        stackPanel.Children.Add(tb) |> ignore
        let owQuest = new ComboBox(IsEditable=false,IsReadOnly=true)
        owQuest.ItemsSource <- [|
                "First Quest"
                "Second Quest"
                "Mixed - First Quest"
                "Mixed - Second Quest"
            |]
        owQuest.SelectedIndex <- owMapNum % 4
        stackPanel.Children.Add(owQuest) |> ignore
        let cb = new CheckBox(Content=new TextBox(Text="Heart Shuffle",IsReadOnly=true))
        cb.IsChecked <- Nullable<_>(isHeartShuffle)
        stackPanel.Children.Add(cb) |> ignore
        let tb = new TextBox(Text="\nNote: once you start, you can use F5 to\nplace the 'start spot' icon at your mouse,\nor F10 to reset the timer to 0, at any time\n",IsReadOnly=true)
        stackPanel.Children.Add(tb) |> ignore
        let startButton = new Button(Content=new TextBox(Text="Start Z-Tracker",IsReadOnly=true))
        stackPanel.Children.Add(startButton) |> ignore
        let hstackPanel = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
        hstackPanel.Children.Add(stackPanel) |> ignore
        this.Content <- hstackPanel
        startButton.Click.Add(fun _ -> 
                let c,u = makeAll(cb.IsChecked.Value,owQuest.SelectedIndex)
                canvas <- c
                updateTimeline <- u
                canvasAdd(canvas, hmsTimeTextBox, RIGHT_COL+40., 0.)
                this.Content <- canvas
                speechRecognizer.SetInputToDefaultAudioDevice()
                speechRecognizer.RecognizeAsync(System.Speech.Recognition.RecognizeMode.Multiple)
            )
    override this.Update(f10Press) =
        base.Update(f10Press)
        // update time
        let ts = DateTime.Now - this.StartTime
        let h,m,s = ts.Hours, ts.Minutes, ts.Seconds
        hmsTimeTextBox.Text <- sprintf "%02d:%02d:%02d" h m s
        // remind ladder
        if (DateTime.Now - ladderTime).Minutes > 2 then  // every 3 mins
            if haveLadder then
                if not haveCoastItem then
                    async { voice.Speak("Get the coast item with the ladder") } |> Async.Start
                    ladderTime <- DateTime.Now
        // remind whistle spots
        if (DateTime.Now - recorderTime).Minutes > 2 then  // every 3 mins
            if haveRecorder && voiceRemindersForRecorder then
                if owWhistleSpotsRemain >= owPreviouslyAnnouncedWhistleSpotsRemain && owWhistleSpotsRemain > 0 then
                    if owWhistleSpotsRemain = 1 then
                        async { voice.Speak("There is one recorder spot") } |> Async.Start
                    else
                        async { voice.Speak(sprintf "There are %d recorder spots" owWhistleSpotsRemain) } |> Async.Start
                recorderTime <- DateTime.Now
                owPreviouslyAnnouncedWhistleSpotsRemain <- owWhistleSpotsRemain
        // remind power bracelet spots
        if (DateTime.Now - powerBraceletTime).Minutes > 2 then  // every 3 mins
            if havePowerBracelet && voiceRemindersForPowerBracelet then
                if owPowerBraceletSpotsRemain >= owPreviouslyAnnouncedPowerBraceletSpotsRemain && owPowerBraceletSpotsRemain > 0 then
                    if owPowerBraceletSpotsRemain = 1 then
                        async { voice.Speak("There is one power bracelet spot") } |> Async.Start
                    else
                        async { voice.Speak(sprintf "There are %d power bracelet spots" owPowerBraceletSpotsRemain) } |> Async.Start
                powerBraceletTime <- DateTime.Now
                owPreviouslyAnnouncedPowerBraceletSpotsRemain <- owPowerBraceletSpotsRemain
        // update timeline
        if f10Press || ts.Seconds = 0 then
            updateTimeline(int ts.TotalMinutes)
        // update start icon
        if f5WasRecentlyPressed then
            startIconX <- currentlyMousedOWX
            startIconY <- currentlyMousedOWY
            f5WasRecentlyPressed <- false
            recordering()

type TimerOnlyWindow() as this = 
    inherit MyWindowBase()
    let hmsTimeTextBox = new TextBox(Text="timer",FontSize=42.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    let canvas = new Canvas(Width=180., Height=50., Background=System.Windows.Media.Brushes.Black)
    do
        // full window
        this.Title <- "Timer"
        this.Content <- canvas
        canvasAdd(canvas, hmsTimeTextBox, 0., 0.)
        this.SizeToContent <- SizeToContent.WidthAndHeight 
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        this.Left <- 0.0
        this.Top <- 0.0
    override this.Update(f10Press) =
        base.Update(f10Press)
        // update time
        let ts = DateTime.Now - this.StartTime
        let h,m,s = ts.Hours, ts.Minutes, ts.Seconds
        hmsTimeTextBox.Text <- sprintf "%02d:%02d:%02d" h m s

type TerrariaTimerOnlyWindow() as this = 
    inherit MyWindowBase()
    let FONT = 24.
    let hmsTimeTextBox = new TextBox(Text="timer",FontSize=FONT,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    let dayTextBox = new TextBox(Text="day",FontSize=FONT,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    let timeTextBox = new TextBox(Text="time",FontSize=FONT,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0))
    let canvas = new Canvas(Width=170.*FONT/35., Height=FONT*16./4., Background=System.Windows.Media.Brushes.Black)
    do
        // full window
        this.Title <- "Timer"
        this.Content <- canvas
        canvasAdd(canvas, hmsTimeTextBox, 0., 0.)
        canvasAdd(canvas, dayTextBox, 0., FONT*5./4.)
        canvasAdd(canvas, timeTextBox, 0., FONT*10./4.)
        this.SizeToContent <- SizeToContent.WidthAndHeight 
        this.WindowStartupLocation <- WindowStartupLocation.Manual
        this.Left <- 0.0
        this.Top <- 0.0
        this.BorderBrush <- Brushes.LightGreen
        this.BorderThickness <- Thickness(2.)
    override this.Update(f10Press) =
        base.Update(f10Press)
        // update hms time
        let mutable ts = DateTime.Now - this.StartTime
        let h,m,s = ts.Hours, ts.Minutes, ts.Seconds
        hmsTimeTextBox.Text <- sprintf "%02d:%02d:%02d" h m s
        // update terraria time
        let mutable day = 1
        while ts >= TimeSpan.FromMinutes(20.25) do
            ts <- ts - TimeSpan.FromMinutes(24.)
            day <- day + 1
        let mutable ttime = ts + TimeSpan.FromMinutes(8.25)
        if ttime >= TimeSpan.FromMinutes(24.) then
            ttime <- ttime - TimeSpan.FromMinutes(24.)
        let m,s = ttime.Minutes, ttime.Seconds
        let m,am = if m < 12 then m,"am" else m-12,"pm"
        let m = if m=0 then 12 else m
        timeTextBox.Text <- sprintf "%02d:%02d%s" m s am
        if ts < TimeSpan.FromMinutes(11.25) then   // 11.25 is 7:30pm, 20.25 is 4:30am
            dayTextBox.Text <- sprintf "Day %d" day
        else
            dayTextBox.Text <- sprintf "Night %d" day

[<STAThread>]
[<EntryPoint>]
let main argv = 
    printfn "test %A" argv

    let app = new Application()
#if DEBUG
    do
#else
    try
#endif
        let mutable owMapNum = 0
        if argv.Length > 1 then
            owMapNum <- int argv.[1]
        if argv.Length > 0 && argv.[0] = "timeronly" then
            app.Run(TimerOnlyWindow()) |> ignore
        elif argv.Length > 0 && argv.[0] = "terraria" then
            app.Run(TerrariaTimerOnlyWindow()) |> ignore
        elif argv.Length > 0 && argv.[0] = "heartShuffle" then
            app.Run(MyWindow(true,owMapNum)) |> ignore
        else
            app.Run(MyWindow(false,owMapNum)) |> ignore
#if DEBUG
#else
    with e ->
        printfn "crashed with exception"
        printfn "%s" (e.ToString())
        printfn "press enter to end"
        System.Console.ReadLine() |> ignore
#endif

    0
