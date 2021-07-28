module WPFUI

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let voice = new System.Speech.Synthesis.SpeechSynthesizer()
let mutable voiceRemindersForRecorder = true
let mutable voiceRemindersForPowerBracelet = true
let speechRecognizer = new System.Speech.Recognition.SpeechRecognitionEngine()
let mapStatePhrases = [|
        "any road"
        "sword three"
        "sword two"
        "hint shop"
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
let convertSpokenPhraseToMapCell(phrase:string) =
    let phrase = phrase.Substring(wakePhrase.Length+1)
    let newState = TrackerModel.mapSquareChoiceDomain.MaxKey-mapStatePhrases.Length + Array.IndexOf(mapStatePhrases, phrase)
    if newState = 12 then // any road
        if   TrackerModel.mapSquareChoiceDomain.NumUses( 9) < TrackerModel.mapSquareChoiceDomain.MaxUses( 9) then
            Some 9
        elif TrackerModel.mapSquareChoiceDomain.NumUses(10) < TrackerModel.mapSquareChoiceDomain.MaxUses(10) then
            Some 10
        elif TrackerModel.mapSquareChoiceDomain.NumUses(11) < TrackerModel.mapSquareChoiceDomain.MaxUses(11) then
            Some 11
        elif TrackerModel.mapSquareChoiceDomain.NumUses(12) < TrackerModel.mapSquareChoiceDomain.MaxUses(12) then
            Some 12
        else
            None
    else
        if TrackerModel.mapSquareChoiceDomain.NumUses(newState) < TrackerModel.mapSquareChoiceDomain.MaxUses(newState) then
            Some newState
        else
            None

type MapStateProxy(state) =
    let U = Graphics.uniqueMapIcons.Length 
    let NU = Graphics.nonUniqueMapIconBMPs.Length
    member this.State = state
    member this.IsX = state = U+NU-1
    member this.IsUnique = state >= 0 && state < U
    member this.IsDungeon = state >= 0 && state < 9
    member this.IsWarp = state >= 9 && state < 13
    member this.IsSword3 = state=13
    member this.IsSword2 = state=14
    member this.HasTransparency = state >= 0 && state < 13 || state >= U+1 && state < U+8   // dungeons, warps, swords, and item-shops
    member this.IsInteresting = not(state = -1 || this.IsX)
    member this.Current() =
        if state = -1 then
            null
        elif state < U then
            Graphics.uniqueMapIcons.[state]
        else
            Graphics.BMPtoImage Graphics.nonUniqueMapIconBMPs.[state-U]

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

let triforceInnerCanvases = Array.zeroCreate 8
let mainTrackerCanvases : Canvas[,] = Array2D.zeroCreate 8 4
let mainTrackerCanvasShaders : Canvas[,] = Array2D.init 8 4 (fun _ _ -> new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black, Opacity=0.4))
let currentHeartsTextBox = new TextBox(Width=200., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "Current Hearts: %d" TrackerModel.playerComputedStateSummary.PlayerHearts)
let owRemainingScreensCheckBox = new CheckBox(Content = new TextBox(Width=200., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text=sprintf "OW spots left: %d" TrackerModel.mapStateSummary.OwSpotsRemain))

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
let mutable notesTextBox = null : TextBox
let mutable timeTextBox = null : TextBox
let H = 30
let RIGHT_COL = 560.
let TLH = (1+9+5+9)*3  // timeline height
let TH = 24 // text height
let makeAll(owMapNum) =
    let timelineItems = ResizeArray()
    let stringReverse (s:string) = new string(s.ToCharArray() |> Array.rev)
    let owMapBMPs, isMixed, owInstance =
        match owMapNum with
        | 0 -> Graphics.overworldMapBMPs(0), false, new OverworldData.OverworldInstance(OverworldData.FIRST)
        | 1 -> Graphics.overworldMapBMPs(1), false, new OverworldData.OverworldInstance(OverworldData.SECOND)
        | 2 -> Graphics.overworldMapBMPs(2), true,  new OverworldData.OverworldInstance(OverworldData.MIXED_FIRST)
        | 3 -> Graphics.overworldMapBMPs(3), true,  new OverworldData.OverworldInstance(OverworldData.MIXED_SECOND)
        | _ -> failwith "bad/unsupported owMapNum"
    TrackerModel.initializeAll(owInstance)
    let whichItems = Graphics.allItemsWithHeartShuffle 
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
        innerc.Children.Add(if TrackerModel.mapStateSummary.DungeonLocations.[i]=TrackerModel.NOTFOUND then Graphics.emptyUnfoundTriforces.[i] else Graphics.emptyFoundTriforces.[i]) |> ignore
    for i = 0 to 7 do
        let image = Graphics.emptyUnfoundTriforces.[i]
        let c = new Canvas(Width=30., Height=30.)
        mainTrackerCanvases.[i,0] <- c
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has triforce drawn on it, not the eventual shading of updateDungeon()
        triforceInnerCanvases.[i] <- innerc
        c.Children.Add(innerc) |> ignore
        canvasAdd(innerc, image, 0., 0.)
        c.MouseDown.Add(fun _ -> 
            if not(TrackerModel.dungeons.[i].PlayerHasTriforce()) then 
                innerc.Children.Clear()
                innerc.Children.Add(Graphics.fullTriforces.[i]) |> ignore 
            else 
                updateEmptyTriforceDisplay(i)
            TrackerModel.dungeons.[i].ToggleTriforce()
        )
        gridAdd(mainTracker, c, i, 0)
        timelineItems.Add(new TimelineItem(innerc, (fun()->TrackerModel.dungeons.[i].PlayerHasTriforce())))
    let hearts = whichItems.[14..]
    let remindShortly(f, text:string) =
        let cxt = System.Threading.SynchronizationContext.Current 
        async { 
            do! Async.Sleep(60000)  // 60s
            do! Async.SwitchToContext(cxt)
            if f() then
                do! Async.SwitchToThreadPool()
                voice.Speak(text) 
        } |> Async.Start
    let boxItemImpl(box:TrackerModel.Box) = 
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let no = System.Windows.Media.Brushes.DarkRed
        let yes = System.Windows.Media.Brushes.LimeGreen 
        let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., Stroke=no)
        rect.StrokeThickness <- 3.0
        c.Children.Add(rect) |> ignore
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has item drawn on it, not the box
        c.Children.Add(innerc) |> ignore
        c.MouseLeftButtonDown.Add(fun _ ->
            if obj.Equals(rect.Stroke, no) then
                rect.Stroke <- yes
            else
                rect.Stroke <- no
            box.TogglePlayerHas()
        )
        // item
        c.MouseWheel.Add(fun x -> 
            innerc.Children.Clear()
            if x.Delta<0 then
                box.CellNext()
            else
                box.CellPrev()
            let mutable i = box.CellCurrent()
            // find unique heart FrameworkElement to display
            while i>=14 && whichItems.[i].Parent<>null do
                i <- i + 1
            let fe = if i = -1 then null else whichItems.[i]
            canvasAdd(innerc, fe, 4., 4.)
        )
        timelineItems.Add(new TimelineItem(innerc, (fun()->obj.Equals(rect.Stroke,yes))))
        c
    // items
    for i = 0 to 8 do
        for j = 0 to 2 do
            let mutable c = new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black)
            if j=0 || j=1 || (i=0 || i=7) then
                c <- boxItemImpl(TrackerModel.dungeons.[i].Boxes.[j])
                gridAdd(mainTracker, c, i, j+1)
            if i < 8 then
                mainTrackerCanvases.[i,j+1] <- c

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
                                Graphics.owHeartsEmpty.[i] 
                            elif next = 1 then 
                                Graphics.owHeartsFull.[i] 
                            else 
                                Graphics.owHeartsSkipped.[i]), 0., 0.)
            TrackerModel.playerProgressAndTakeAnyHearts.SetTakeAnyHeart(i,next)
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
    gridAdd(owItemGrid, boxItemImpl(TrackerModel.ladderBox), 1, 0)
    gridAdd(owItemGrid, boxItemImpl(TrackerModel.armosBox), 1, 1)
    gridAdd(owItemGrid, boxItemImpl(TrackerModel.sword2Box), 1, 2)
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
    gridAdd(owItemGrid, basicBoxImpl("Acquired wood sword (mark timeline)",    Graphics.brown_sword  , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword.Toggle())), 0, 0)
    gridAdd(owItemGrid, basicBoxImpl("Acquired blue candle (mark timeline)",   Graphics.blue_candle  , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle.Toggle())), 0, 1)
    gridAdd(owItemGrid, basicBoxImpl("Acquired blue ring (mark timeline)",     Graphics.blue_ring    , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueRing.Toggle())), 1, 0)
    gridAdd(owItemGrid, basicBoxImpl("Acquired magical sword (mark timeline)", Graphics.magical_sword, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Toggle())), 1, 1)
    canvasAdd(c, owItemGrid, OFFSET+90., 30.)
    // boomstick book, to mark when purchase in boomstick seed (normal book would still be used to mark finding shield in dungeon)
    canvasAdd(c, basicBoxImpl("Purchased boomstick book (mark timeline)", Graphics.boom_book, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Toggle())), OFFSET+120., 0.)
    // mark the dungeon wins on timeline via ganon/zelda boxes
    canvasAdd(c, basicBoxImpl("Killed Ganon (mark timeline)",  Graphics.ganon, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasDefeatedGanon.Toggle())), OFFSET+90., 90.)
    canvasAdd(c, basicBoxImpl("Rescued Zelda (mark timeline)", Graphics.zelda, (fun b -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Toggle(); if b then notesTextBox.Text <- notesTextBox.Text + "\n" + timeTextBox.Text)), OFFSET+120., 90.)

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
            if owInstance.AlwaysEmpty(i,j) then
                let icon = Graphics.BMPtoImage Graphics.nonUniqueMapIconBMPs.[Graphics.nonUniqueMapIconBMPs.Length-1] // "X"
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

    // ow route drawing layer
    let routeDrawingCanvas = new Canvas(Width=float(16*16*3), Height=float(8*11*3))
    routeDrawingCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(c, routeDrawingCanvas, 0., 120.)

    // ow map
    let owMapGrid = makeGrid(16, 8, 16*3, 11*3)
    let owCanvases = Array2D.zeroCreate 16 8
    let owUpdateFunctions = Array2D.create 16 8 (fun _ _ -> ())
    let drawRectangleCornersHighlight(c,x,y,color) =
        // full rectangles badly obscure routing paths, so we just draw corners
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
                                        // draw routes
                                        OverworldRouting.drawPaths(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, ea.GetPosition(c), i, j)
                                        // track current location for F5 & speech recognition purposes
                                        currentlyMousedOWX <- i
                                        currentlyMousedOWY <- j
                                        mostRecentMouseEnterTime <- DateTime.Now)
            c.MouseLeave.Add(fun _ -> c.Children.Remove(rect) |> ignore
                                      routeDrawingCanvas.Children.Clear())
            // icon
            if owInstance.AlwaysEmpty(i,j) then
                () // already set up as permanent opaque layer, in code above
            else
                let updateGridSpot delta phrase =
                    // cant remove-by-identity because of non-uniques; remake whole canvas
                    owDarkeningMapGridCanvases.[i,j].Children.Clear()
                    c.Children.Clear()
                    // we need a dummy image to make the canvas absorb the mouse interactions, so just re-draw the map at 0 opacity
                    let image = Graphics.BMPtoImage(owMapBMPs.[i,j])
                    image.Opacity <- 0.0
                    canvasAdd(c, image, 0., 0.)
                    // figure out what new state we just interacted-to
                    if delta = 777 then 
                        if TrackerModel.overworldMapMarks.[i,j].Current() = -1 then
                            match convertSpokenPhraseToMapCell(phrase) with
                            | Some newState -> TrackerModel.overworldMapMarks.[i,j].TrySet(newState)
                            | None -> ()
                    else
                        if delta = 1 then
                            TrackerModel.overworldMapMarks.[i,j].Next()
                        elif delta = -1 then 
                            TrackerModel.overworldMapMarks.[i,j].Prev() 
                        elif delta = 0 then 
                            ()
                        else failwith "bad delta"
                    let ms = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                    let icon = ms.Current()
                    // be sure to draw in appropriate layer
                    let canvasToDrawOn =
                        if ms.HasTransparency && not ms.IsSword3 && not ms.IsSword2 then
                            if not ms.IsDungeon || (ms.State < 8 && TrackerModel.dungeons.[ms.State].IsComplete) then
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
                    canvasAdd(canvasToDrawOn, icon, 0., 0.)
                    if ms.IsDungeon then
                        drawDungeonHighlight(canvasToDrawOn,0.,0)
                    if ms.IsWarp then
                        drawWarpHighlight(canvasToDrawOn,0.,0)
                    if OverworldData.owMapSquaresSecondQuestOnly.[j].Chars(i) = 'X' then
                        secondQuestOnlyInterestingMarks.[i,j] <- ms.IsInteresting 
                    if OverworldData.owMapSquaresFirstQuestOnly.[j].Chars(i) = 'X' then
                        firstQuestOnlyInterestingMarks.[i,j] <- ms.IsInteresting 
                owUpdateFunctions.[i,j] <- updateGridSpot 
                owCanvases.[i,j] <- c
                c.MouseLeftButtonDown.Add(fun _ -> 
                        updateGridSpot 1 ""
                )
                c.MouseRightButtonDown.Add(fun _ -> updateGridSpot -1 "")
                c.MouseWheel.Add(fun x -> updateGridSpot (if x.Delta<0 then 1 else -1) "")
    speechRecognizer.SpeechRecognized.Add(fun r ->
        //printfn "conf: %f" r.Result.Confidence
        if r.Result.Confidence > 0.94f then  // empirical tests suggest this confidence threshold is good to avoid false positives
            let timeDiff = DateTime.Now - mostRecentMouseEnterTime
            if timeDiff < System.TimeSpan.FromSeconds(20.0) && timeDiff > System.TimeSpan.FromSeconds(1.0) then
                c.Dispatcher.Invoke(fun () -> 
                        if currentlyMousedOWX >= 0 then // can hear speech before we have moused over any (uninitialized location)
                            let c = owCanvases.[currentlyMousedOWX,currentlyMousedOWY]
                            if c <> null && c.IsMouseOver then  // canvas can be null for always-empty grid places
                                owUpdateFunctions.[currentlyMousedOWX,currentlyMousedOWY] 777 r.Result.Text 
                    )
        )
    canvasAdd(c, owMapGrid, 0., 120.)

    let recorderingCanvas = new Canvas(Width=float(16*16*3), Height=float(8*11*3))  // really the 'extra top layer' canvas for adding final marks to overworld map
    recorderingCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(c, recorderingCanvas, 0., 120.)
    let startIcon = new System.Windows.Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=System.Windows.Media.Brushes.Lime, StrokeThickness=3.0)

// TODO figure out where this code should go
    let doUIUpdate() =
        // TODO found/not-found may need an update, only have event for found, hmm... for now just force redraw these on each update
        for i = 0 to 7 do
            if not(TrackerModel.dungeons.[i].PlayerHasTriforce()) then
                updateEmptyTriforceDisplay(i)
        recorderingCanvas.Children.Clear()
        // place start icon in top layer
        if TrackerModel.startIconX <> -1 then
            canvasAdd(recorderingCanvas, startIcon, 8.5+float(TrackerModel.startIconX*16*3), float(TrackerModel.startIconY*11*3))
        TrackerModel.allUIEventingLogic( {new TrackerModel.ITrackerEvents with
            member _this.CurrentHearts(h) = currentHeartsTextBox.Text <- sprintf "Current Hearts: %d" h
            member _this.AnnounceConsiderSword2() = async { voice.Speak("Consider getting the white sword item") } |> Async.Start
            member _this.AnnounceConsiderSword3() = async { voice.Speak("Consider the magical sword") } |> Async.Start
            member _this.OverworldSpotsRemaining(n) = (owRemainingScreensCheckBox.Content :?> TextBox).Text <- sprintf "OW spots left: %d" n 
            member _this.DungeonLocation(i,x,y,hasTri,isCompleted) =
                if isCompleted then
                    drawCompletedDungeonHighlight(recorderingCanvas,float x,y)
                // highlight any triforce dungeons as recorder warp destinations
                if TrackerModel.playerComputedStateSummary.HaveRecorder && hasTri then
                    drawDungeonRecorderWarpHighlight(recorderingCanvas,float x,y)
                // highlight 9 after get all triforce
                if i = 8 && TrackerModel.dungeons.[0..7] |> Array.forall (fun d -> d.PlayerHasTriforce()) then
                    let rect = new Canvas(Width=float(16*3), Height=float(11*3), Background=System.Windows.Media.Brushes.Pink)
                    rect.BeginAnimation(UIElement.OpacityProperty, fasterBlinkAnimation)
                    canvasAdd(recorderingCanvas, rect, float(x*16*3), float(y*11*3))
            member _this.AnyRoadLocation(i,x,y) = ()
            member _this.WhistleableLocation(x,y) =
                drawWhistleableHighlight(recorderingCanvas,float x,y)
            member _this.Sword3(x,y) = 
                if not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) && TrackerModel.playerComputedStateSummary.PlayerHearts>=10 then
                    let rect = new Canvas(Width=float(16*3), Height=float(11*3), Background=System.Windows.Media.Brushes.Pink)
                    rect.BeginAnimation(UIElement.OpacityProperty, fasterBlinkAnimation)
                    canvasAdd(recorderingCanvas, rect, float(x*16*3), float(y*11*3))
            member _this.Sword2(x,y) = ()
            member _this.RoutingInfo(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations,owRouteworthySpots) = 
                routeDrawingCanvas.Children.Clear()
                OverworldRouting.repopulate(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations)
                // unexplored but gettable spots highlight
                if owRemainingScreensCheckBox.IsChecked.HasValue && owRemainingScreensCheckBox.IsChecked.Value then
                    for x = 0 to 15 do
                        for y = 0 to 7 do
                            if owRouteworthySpots.[x,y] && TrackerModel.overworldMapMarks.[x,y].Current() = -1 then
                                canvasAdd(recorderingCanvas, owRemainSpotHighlighters.[x,y], float(x*16*3), float(y*11*3))
            member _this.AnnounceCompletedDungeon(i) = async { voice.Speak(sprintf "Dungeon %d is complete" (i+1)) } |> Async.Start
            member _this.CompletedDungeons(a) =
                for i = 0 to 7 do
                    for j = 0 to 3 do
                        mainTrackerCanvases.[i,j].Children.Remove(mainTrackerCanvasShaders.[i,j]) |> ignore
                    if a.[i] then
                        for j = 0 to 3 do
                            mainTrackerCanvases.[i,j].Children.Add(mainTrackerCanvasShaders.[i,j]) |> ignore
            member _this.AnnounceFoundDungeonCount(n) = 
                async {
                    if n = 1 then
                        voice.Speak("You have located one dungeon") 
                    elif n = 9 then
                        voice.Speak("Congratulations, you have located all 9 dungeons")
                    else
                        voice.Speak(sprintf "You have located %d dungeons" n) 
                } |> Async.Start
            member _this.AnnounceTriforceCount(n) = 
                if n = 1 then
                    async { voice.Speak("You now have one triforce") } |> Async.Start
                else
                    async { voice.Speak(sprintf "You now have %d triforces" n) } |> Async.Start
                if n = 8 && not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) then
                    async { voice.Speak("Consider the magical sword before dungeon nine") } |> Async.Start
            member _this.RemindShortly(itemId) = 
                let f, g, text =
                    if itemId = TrackerModel.ITEMS.KEY then
                        (fun() -> TrackerModel.playerComputedStateSummary.HaveAnyKey), (fun() -> TrackerModel.remindedAnyKey <- false), "Don't forget that you have the any key"
                    elif itemId = TrackerModel.ITEMS.LADDER then
                        (fun() -> TrackerModel.playerComputedStateSummary.HaveLadder), (fun() -> TrackerModel.remindedLadder <- false), "Don't forget that you have the ladder"
                    else
                        failwith "bad reminder"
                let cxt = System.Threading.SynchronizationContext.Current 
                async { 
                    do! Async.Sleep(60000)  // 60s
                    do! Async.SwitchToContext(cxt)
                    if f() then
                        do! Async.SwitchToThreadPool()
                        voice.Speak(text) 
                    else
                        g()
                } |> Async.Start
            })
    let threshold = TimeSpan.FromMilliseconds(500.0)
    let mutable ladderTime, recorderTime, powerBraceletTime = DateTime.Now, DateTime.Now, DateTime.Now
    let mutable owPreviouslyAnnouncedWhistleSpotsRemain, owPreviouslyAnnouncedPowerBraceletSpotsRemain = 0, 0
    let timer = new System.Windows.Threading.DispatcherTimer()
    timer.Interval <- TimeSpan.FromSeconds(1.0)
    timer.Tick.Add(fun _ -> 
        let hasUISettledDown = 
            DateTime.Now - TrackerModel.playerProgressLastChangedTime > threshold &&
            DateTime.Now - TrackerModel.dungeonsAndBoxesLastChangedTime > threshold &&
            DateTime.Now - TrackerModel.mapLastChangedTime > threshold
        if hasUISettledDown then
            let hasTheModelChanged = TrackerModel.recomputeWhatIsNeeded()  
            if hasTheModelChanged then
                doUIUpdate()
        // remind ladder
        if (DateTime.Now - ladderTime).Minutes > 2 then  // every 3 mins
            if TrackerModel.playerComputedStateSummary.HaveLadder then
                if not(TrackerModel.playerComputedStateSummary.HaveCoastItem) then
                    async { voice.Speak("Get the coast item with the ladder") } |> Async.Start
                    ladderTime <- DateTime.Now
        // remind whistle spots
        if (DateTime.Now - recorderTime).Minutes > 2 then  // every 3 mins
            if TrackerModel.playerComputedStateSummary.HaveRecorder && voiceRemindersForRecorder then
                let owWhistleSpotsRemain = TrackerModel.mapStateSummary.OwWhistleSpotsRemain.Count
                if owWhistleSpotsRemain >= owPreviouslyAnnouncedWhistleSpotsRemain && owWhistleSpotsRemain > 0 then
                    if owWhistleSpotsRemain = 1 then
                        async { voice.Speak("There is one recorder spot") } |> Async.Start
                    else
                        async { voice.Speak(sprintf "There are %d recorder spots" owWhistleSpotsRemain) } |> Async.Start
                recorderTime <- DateTime.Now
                owPreviouslyAnnouncedWhistleSpotsRemain <- owWhistleSpotsRemain
        // remind power bracelet spots
        if (DateTime.Now - powerBraceletTime).Minutes > 2 then  // every 3 mins
            if TrackerModel.playerComputedStateSummary.HavePowerBracelet && voiceRemindersForPowerBracelet then
                if TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain >= owPreviouslyAnnouncedPowerBraceletSpotsRemain && TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain > 0 then
                    if TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain = 1 then
                        async { voice.Speak("There is one power bracelet spot") } |> Async.Start
                    else
                        async { voice.Speak(sprintf "There are %d power bracelet spots" TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain) } |> Async.Start
                powerBraceletTime <- DateTime.Now
                owPreviouslyAnnouncedPowerBraceletSpotsRemain <- TrackerModel.mapStateSummary.OwPowerBraceletSpotsRemain
        )
    timer.Start()

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
    owRemainingScreensCheckBox.Checked.Add(fun _ -> TrackerModel.forceUpdate()) 
    owRemainingScreensCheckBox.Unchecked.Add(fun _ -> TrackerModel.forceUpdate())
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
            gridAdd(owMapZoneGrid, c, i, j)
    canvasAdd(c, owMapZoneGrid, 0., 120.)

    let owMapZoneBoundaries = ResizeArray()
    let makeLine(x1, x2, y1, y2) = 
        let line = new System.Windows.Shapes.Line(X1=float(x1*16*3), X2=float(x2*16*3), Y1=float(y1*11*3), Y2=float(y2*11*3), Stroke=Brushes.White, StrokeThickness=3.)
        line.IsHitTestVisible <- false // transparent to mouse
        line
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
    TrackerModel.forceUpdate()
    c, updateTimeline


