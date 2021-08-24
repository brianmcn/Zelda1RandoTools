module WPFUI

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let voice = new System.Speech.Synthesis.SpeechSynthesizer()
let mutable voiceRemindersForRecorder = true
let mutable voiceRemindersForPowerBracelet = true
let speechRecognizer = new System.Speech.Recognition.SpeechRecognitionEngine()
let wakePhrase = "tracker set"
let mapStatePhrases = [|
        "any road"
        "sword three"
        "sword two"
        "hint shop"
        "arrow shop"
        "bomb shop"
        "book shop"
        "candle shop"
        "blue ring shop"
        "meat shop"
        "key shop"
        "shield shop"
        "take any"
        "potion shop"
        "money"
        |]
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
    static member ARROW = 16
    static member BOMB = 17
    static member BOOK = 18
    static member CANDLE = 19
    static member BLUE_RING = 20
    static member MEAT = 21
    static member KEY = 22
    static member SHIELD = 23
    static member NUM_ITEMS = 8 // 8 possible types of items can be tracked, listed above
    static member IsItem(state) = state >=16 && state <=23
    static member ToItem(state) = if MapStateProxy.IsItem(state) then state-15 else 0   // format used by TrackerModel.overworldMapExtraData
    member this.State = state
    member this.IsX = state = U+NU-1
    member this.IsUnique = state >= 0 && state < U
    member this.IsDungeon = state >= 0 && state < 9
    member this.IsWarp = state >= 9 && state < 13
    member this.IsSword3 = state=13
    member this.IsSword2 = state=14
    member this.IsThreeItemShop = MapStateProxy.IsItem(state)
    member this.HasTransparency = state >= 0 && state < 13 || state >= U+1 && state < U+MapStateProxy.NUM_ITEMS+1   // dungeons, warps, swords, and item-shops
    member this.IsInteresting = not(state = -1 || this.IsX)
    member this.Current() =
        if state = -1 then
            null
        elif state < U then
            Graphics.uniqueMapIcons.[state]
        else
            Graphics.BMPtoImage Graphics.nonUniqueMapIconBMPs.[state-U]

let drawRoutesTo(targetItemStateOption, routeDrawingCanvas, point, i, j) =
    match targetItemStateOption with
    | Some(targetItemState) ->
        let owTargetworthySpots = Array2D.zeroCreate 16 8
        for x = 0 to 15 do
            for y = 0 to 7 do
                let msp = MapStateProxy(TrackerModel.overworldMapMarks.[x,y].Current())
                if msp.State = targetItemState || (msp.IsThreeItemShop && TrackerModel.overworldMapExtraData.[x,y] = MapStateProxy.ToItem(targetItemState)) then
                    owTargetworthySpots.[x,y] <- true
        OverworldRouteDrawing.drawPaths(routeDrawingCanvas, owTargetworthySpots, 
                                        Array2D.zeroCreate 16 8, point, i, j)
    | None ->
        OverworldRouteDrawing.drawPaths(routeDrawingCanvas, TrackerModel.mapStateSummary.OwRouteworthySpots, 
                                        TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), point, i, j)

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
    for i = 0 to nc-1 do
        grid.ColumnDefinitions.Add(new ColumnDefinition(Width=GridLength(float cw)))
    for i = 0 to nr-1 do
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
        if Graphics.owHeartsFull |> Array.exists (fun x -> c.Children.Contains(x)) then
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
let OMTW = OverworldRouteDrawing.OMTW  // overworld map tile width - at normal aspect ratio, is 48 (16*3)
let resizeMapTileImage(image:Image) =
    image.Width <- OMTW
    image.Height <- float(11*3)
    image.Stretch <- Stretch.Fill
    image.StretchDirection <- StretchDirection.Both
    image
let makeAll(owMapNum, audioInitiallyOn) =
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
            bookOrMagicalShieldVB.Visual <- Graphics.BMPtoImage Graphics.magic_shield_bmp
        else
            bookOrMagicalShieldVB.Visual <- Graphics.BMPtoImage Graphics.book_bmp
        isCurrentlyBook := not !isCurrentlyBook
        TrackerModel.forceUpdate()

    let mutable routeTargetLastClickedTime = DateTime.Now - TimeSpan.FromMinutes(10.)
    let mutable routeTargetState = 0
    let currentRouteTarget() =
        if DateTime.Now - routeTargetLastClickedTime < TimeSpan.FromSeconds(10.) then
            Some(routeTargetState)
        else
            None
    let changeCurrentRouteTarget(newTargetState) =
        routeTargetLastClickedTime <- DateTime.Now
        routeTargetState <- newTargetState
    
    let c = new Canvas()
    c.Width <- float(16*16*3) // may change with OMTW and overall layout

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
    let remindShortly(f, text:string) =
        let cxt = System.Threading.SynchronizationContext.Current 
        async { 
            do! Async.Sleep(60000)  // 60s
            do! Async.SwitchToContext(cxt)
            if f() then
                do! Async.SwitchToThreadPool()
                voice.Speak(text) 
        } |> Async.Start
    let boxItemImpl(box:TrackerModel.Box, requiresForceUpdate) = 
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let no = System.Windows.Media.Brushes.DarkRed
        let yes = System.Windows.Media.Brushes.LimeGreen 
        let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., Stroke=no, StrokeThickness=3.0)
        c.Children.Add(rect) |> ignore
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has item drawn on it, not the box
        c.Children.Add(innerc) |> ignore
        c.MouseLeftButtonDown.Add(fun _ ->
            if obj.Equals(rect.Stroke, no) then
                rect.Stroke <- yes
            else
                rect.Stroke <- no
            box.TogglePlayerHas()
            if requiresForceUpdate then
                TrackerModel.forceUpdate()
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
            if requiresForceUpdate then
                TrackerModel.forceUpdate()
        )
        timelineItems.Add(new TimelineItem(innerc, (fun()->obj.Equals(rect.Stroke,yes))))
        c
    // items
    for i = 0 to 8 do
        for j = 0 to 2 do
            let mutable c = new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black)
            if j=0 || j=1 || (i=0 || i=7) then
                c <- boxItemImpl(TrackerModel.dungeons.[i].Boxes.[j], false)
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
    // ow 'take any' hearts
    let owHeartGrid = makeGrid(4, 1, 30, 30)
    for i = 0 to 3 do
        let c = new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black)
        canvasAdd(c, Graphics.owHeartsEmpty.[i], 0., 0.)
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
    gridAdd(owItemGrid, Graphics.BMPtoImage Graphics.ladder_bmp, 0, 0)
    gridAdd(owItemGrid, Graphics.ow_key_armos, 0, 1)
    gridAdd(owItemGrid, Graphics.BMPtoImage Graphics.white_sword_bmp, 0, 2)
    gridAdd(owItemGrid, boxItemImpl(TrackerModel.ladderBox, true), 1, 0)
    gridAdd(owItemGrid, boxItemImpl(TrackerModel.armosBox, false), 1, 1)
    gridAdd(owItemGrid, boxItemImpl(TrackerModel.sword2Box, true), 1, 2)
    canvasAdd(c, owItemGrid, OFFSET, 30.)
    // brown sword, blue candle, blue ring, magical sword
    let owItemGrid = makeGrid(3, 2, 30, 30)
    let veryBasicBoxImpl(img, startOn, isTimeline, changedFunc, rightClick) =
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let no = System.Windows.Media.Brushes.DarkRed
        let yes = System.Windows.Media.Brushes.LimeGreen 
        let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., Stroke=(if startOn then yes else no), StrokeThickness=3.0)
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
        c.MouseRightButtonDown.Add(fun _ -> rightClick())
        canvasAdd(innerc, img, 4., 4.)
        if isTimeline then
            timelineItems.Add(new TimelineItem(innerc, fun()->obj.Equals(rect.Stroke,yes)))
        c
    let basicBoxImpl(tts, img, changedFunc, rightClick) =
        let c = veryBasicBoxImpl(img, false, true, changedFunc, rightClick)
        c.ToolTip <- tts
        c
    gridAdd(owItemGrid, basicBoxImpl("Acquired wood sword (mark timeline)",    Graphics.BMPtoImage Graphics.brown_sword_bmp  , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword.Toggle()),    (fun () -> ())), 1, 0)
    gridAdd(owItemGrid, basicBoxImpl("Acquired wood arrow (mark timeline)",    Graphics.BMPtoImage Graphics.wood_arrow_bmp   , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodArrow.Toggle()),    (fun () -> changeCurrentRouteTarget(MapStateProxy.ARROW))), 0, 1)
    gridAdd(owItemGrid, basicBoxImpl("Acquired blue candle (mark timeline)",   Graphics.BMPtoImage Graphics.blue_candle_bmp  , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle.Toggle()),   (fun () -> changeCurrentRouteTarget(MapStateProxy.CANDLE))), 1, 1)
    gridAdd(owItemGrid, basicBoxImpl("Acquired blue ring (mark timeline)",     Graphics.BMPtoImage Graphics.blue_ring_bmp    , (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueRing.Toggle()),     (fun () -> changeCurrentRouteTarget(MapStateProxy.BLUE_RING))), 2, 0)
    gridAdd(owItemGrid, basicBoxImpl("Acquired magical sword (mark timeline)", Graphics.BMPtoImage Graphics.magical_sword_bmp, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Toggle()), (fun () -> ())), 2, 1)
    canvasAdd(c, owItemGrid, OFFSET+60., 30.)
    // boomstick book, to mark when purchase in boomstick seed (normal book will become shield found in dungeon)
    canvasAdd(c, basicBoxImpl("Purchased boomstick book (mark timeline)", Graphics.boom_book, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Toggle()), (fun () -> changeCurrentRouteTarget(MapStateProxy.BOOK))), OFFSET+120., 0.)
    // mark the dungeon wins on timeline via ganon/zelda boxes
    canvasAdd(c, basicBoxImpl("Killed Ganon (mark timeline)",  Graphics.ganon, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasDefeatedGanon.Toggle()), (fun () -> ())), OFFSET+90., 90.)
    canvasAdd(c, basicBoxImpl("Rescued Zelda (mark timeline)", Graphics.zelda, (fun b -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Toggle(); if b then notesTextBox.Text <- notesTextBox.Text + "\n" + timeTextBox.Text), (fun () -> ())), OFFSET+120., 90.)
    // mark whether player currently has bombs, for overworld routing
    let bombIcon = veryBasicBoxImpl(Graphics.bomb, false, false, (fun _ -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBombs.Toggle()), (fun () -> changeCurrentRouteTarget(MapStateProxy.BOMB)))
    bombIcon.ToolTip <- "Player currently has bombs"
    canvasAdd(c, bombIcon, OFFSET+160., 30.)

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
        let rect = new Canvas(Width=OMTW, Height=float(11*3), Background=System.Windows.Media.Brushes.Lime)
        rect.BeginAnimation(UIElement.OpacityProperty, slowerBlinkAnimation)
        rect
        )

    // ow map opaque fixed bottom layer
    let X_OPACITY = 0.4
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
                let icon = resizeMapTileImage <| Graphics.BMPtoImage Graphics.nonUniqueMapIconBMPs.[Graphics.nonUniqueMapIconBMPs.Length-1] // "X"
                icon.Opacity <- X_OPACITY
                canvasAdd(c, icon, 0., 0.)
    canvasAdd(c, owOpaqueMapGrid, 0., 120.)

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
    canvasAdd(c, owDarkeningMapGrid, 0., 120.)

    // layer to place 'hiding' icons - dynamic darkening icons that are below route-drawing but above the previous layers
    let owHidingMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owHidingMapGridCanvases = Array2D.zeroCreate 16 8
    owHidingMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            gridAdd(owHidingMapGrid, c, i, j)
            owHidingMapGridCanvases.[i,j] <- c
    canvasAdd(c, owHidingMapGrid, 0., 120.)
    let hide(x,y) =
        let hideColor = Brushes.DarkSlateGray // Brushes.Black
        let hideOpacity = 0.6 // 0.4
        let rect = new System.Windows.Shapes.Rectangle(Width=7.0*OMTW/48., Height=float(11*3)-1.5, Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
        canvasAdd(owHidingMapGridCanvases.[x,y], rect, 7.*OMTW/48., 0.)
        let rect = new System.Windows.Shapes.Rectangle(Width=7.0*OMTW/48., Height=float(11*3)-1.5, Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
        canvasAdd(owHidingMapGridCanvases.[x,y], rect, 19.*OMTW/48., 0.)
        let rect = new System.Windows.Shapes.Rectangle(Width=7.0*OMTW/48., Height=float(11*3)-1.5, Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
        canvasAdd(owHidingMapGridCanvases.[x,y], rect, 32.*OMTW/48., 0.)
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
    let routeDrawingCanvas = new Canvas(Width=16.*OMTW, Height=float(8*11*3))
    routeDrawingCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(c, routeDrawingCanvas, 0., 120.)

    // single ow tile magnified overlay
    let ENLARGE = 16.
    let dungeonTabsOverlay = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(8.), Background=Brushes.Black)
    let dungeonTabsOverlayContent = new StackPanel(Orientation=Orientation.Vertical)
    dungeonTabsOverlay.Child <- dungeonTabsOverlayContent
    let overlayText = new TextBox(Text="You are moused here:", IsReadOnly=true, FontSize=36., Background=Brushes.Black, Foreground=Brushes.Lime, BorderThickness=Thickness(0.))
    let overlayTiles = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let bmp = new System.Drawing.Bitmap(16*int ENLARGE, 11*int ENLARGE)
            for x = 0 to bmp.Width-1 do
                for y = 0 to bmp.Height-1 do
                    let c = owMapBMPs.[i,j].GetPixel(int(float x*3./ENLARGE), int(float y*3./ENLARGE))
                    let c = 
                        if (x+1) % int ENLARGE = 0 || (y+1) % int ENLARGE = 0 then
                            System.Drawing.Color.FromArgb(int c.R / 2, int c.G / 2, int c.B / 2)
                        else
                            c
                    bmp.SetPixel(x,y,c)
            overlayTiles.[i,j] <- Graphics.BMPtoImage bmp
    // ow map
    let owMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owCanvases = Array2D.zeroCreate 16 8
    let owUpdateFunctions = Array2D.create 16 8 (fun _ _ -> ())
    let drawRectangleCornersHighlight(c,x,y,color) =
        // full rectangles badly obscure routing paths, so we just draw corners
        let L1,L2,R1,R2 = 0.0, (OMTW-4.)/2.-6., (OMTW-4.)/2.+6., OMTW-4.
        let T1,T2,B1,B2 = 0.0, 10.0, 19.0, 29.0
        let s = new System.Windows.Shapes.Line(X1=L1, X2=L2, Y1=T1+1.5, Y2=T1+1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=L1+1.5, X2=L1+1.5, Y1=T1, Y2=T2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=L1, X2=L2, Y1=B2-1.5, Y2=B2-1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=L1+1.5, X2=L1+1.5, Y1=B1, Y2=B2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R1, X2=R2, Y1=T1+1.5, Y2=T1+1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R2-1.5, X2=R2-1.5, Y1=T1, Y2=T2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R1, X2=R2, Y1=B2-1.5, Y2=B2-1.5, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
        let s = new System.Windows.Shapes.Line(X1=R2-1.5, X2=R2-1.5, Y1=B1, Y2=B2, Stroke=color, StrokeThickness = 3.)
        canvasAdd(c, s, x*OMTW+2., float(y*11*3)+2.)
    let drawDungeonHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,System.Windows.Media.Brushes.Yellow)
    let drawCompletedDungeonHighlight(c,x,y) =
        // darkened rectangle corners
        let yellow = System.Windows.Media.Brushes.Yellow.Color
        let darkYellow = Color.FromRgb(yellow.R/2uy, yellow.G/2uy, yellow.B/2uy)
        drawRectangleCornersHighlight(c,x,y,new SolidColorBrush(darkYellow))
        // darken the number
        let rect = new System.Windows.Shapes.Rectangle(Width=15.0*OMTW/48., Height=21.0, Stroke=System.Windows.Media.Brushes.Black, StrokeThickness = 3.,
                                                        Fill=System.Windows.Media.Brushes.Black, Opacity=0.4)
        canvasAdd(c, rect, x*OMTW+15.0*OMTW/48., float(y*11*3)+6.0)
    let drawWarpHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,System.Windows.Media.Brushes.Aqua)
    let drawDarkening(c,x,y) =
        let rect = new System.Windows.Shapes.Rectangle(Width=OMTW-1.5, Height=float(11*3)-1.5, Stroke=System.Windows.Media.Brushes.Black, StrokeThickness = 3.,
                                                        Fill=System.Windows.Media.Brushes.Black, Opacity=0.4)
        canvasAdd(c, rect, x*OMTW, float(y*11*3))
    let drawDungeonRecorderWarpHighlight(c,x,y) =
        drawRectangleCornersHighlight(c,x,y,System.Windows.Media.Brushes.Lime)
    let drawWhistleableHighlight(c,x,y) =
        let rect = new System.Windows.Shapes.Rectangle(Width=OMTW-2., Height=float(11*3)-2., Stroke=System.Windows.Media.Brushes.DeepSkyBlue, StrokeThickness=2.0)
        canvasAdd(c, rect, x*OMTW+1., float(y*11*3)+1.)
    let mutable mostRecentMouseEnterTime = DateTime.Now 
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            gridAdd(owMapGrid, c, i, j)
            // we need a dummy image to make the canvas absorb the mouse interactions, so just re-draw the map at 0 opacity
            let image = resizeMapTileImage <| Graphics.BMPtoImage(owMapBMPs.[i,j])
            image.Opacity <- 0.0
            canvasAdd(c, image, 0., 0.)
            // highlight mouse, do mouse-sensitive stuff
            let rect = new System.Windows.Shapes.Rectangle(Width=OMTW-4., Height=float(11*3)-4., Stroke=System.Windows.Media.Brushes.White)
            c.MouseEnter.Add(fun ea ->  canvasAdd(c, rect, 2., 2.)
                                        // draw routes
                                        drawRoutesTo(currentRouteTarget(), routeDrawingCanvas, ea.GetPosition(c), i, j)
                                        // show enlarged version of current room
                                        dungeonTabsOverlayContent.Children.Add(overlayText) |> ignore
                                        dungeonTabsOverlayContent.Children.Add(overlayTiles.[i,j]) |> ignore
                                        dungeonTabsOverlay.Opacity <- 1.0
                                        // track current location for F5 & speech recognition purposes
                                        currentlyMousedOWX <- i
                                        currentlyMousedOWY <- j
                                        mostRecentMouseEnterTime <- DateTime.Now)
            c.MouseLeave.Add(fun _ -> c.Children.Remove(rect) |> ignore
                                      dungeonTabsOverlayContent.Children.Clear()
                                      dungeonTabsOverlay.Opacity <- 0.
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
                    let image = resizeMapTileImage <| Graphics.BMPtoImage(owMapBMPs.[i,j])
                    image.Opacity <- 0.0
                    canvasAdd(c, image, 0., 0.)
                    // figure out what new state we just interacted-to
                    if delta = 777 then 
                        let curState = TrackerModel.overworldMapMarks.[i,j].Current()
                        if curState = -1 then
                            // if unmarked, use voice to set new state
                            match convertSpokenPhraseToMapCell(phrase) with
                            | Some newState -> TrackerModel.overworldMapMarks.[i,j].TrySet(newState)
                            | None -> ()
                        elif MapStateProxy(curState).IsThreeItemShop && TrackerModel.overworldMapExtraData.[i,j]=0 then
                            // if item shop with only one item marked, use voice to set other item
                            match convertSpokenPhraseToMapCell(phrase) with
                            | Some newState -> 
                                if MapStateProxy.IsItem(newState) then
                                    TrackerModel.overworldMapExtraData.[i,j] <- MapStateProxy.ToItem(newState)
                            | None -> ()
                    elif delta = 1 then
                        TrackerModel.overworldMapMarks.[i,j].Next()
                    elif delta = -1 then 
                        TrackerModel.overworldMapMarks.[i,j].Prev() 
                    elif delta = 0 then 
                        ()
                    else failwith "bad delta"
                    let ms = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                    let icon = 
                        if ms.IsThreeItemShop && TrackerModel.overworldMapExtraData.[i,j] <> 0 then
                            let item1 = ms.State - 16  // 0-based
                            let item2 = TrackerModel.overworldMapExtraData.[i,j] - 1   // 0-based
                            // cons up a two-item shop image
                            let tile = new System.Drawing.Bitmap(16*3,11*3)
                            for px = 0 to 16*3-1 do
                                for py = 0 to 11*3-1 do
                                    // two-icon area
                                    if px/3 >= 3 && px/3 <= 11 && py/3 >= 1 && py/3 <= 9 then
                                        tile.SetPixel(px, py, Graphics.itemBackgroundColor)
                                    else
                                        tile.SetPixel(px, py, Graphics.TRANS_BG)
                                    // icon 1
                                    if px/3 >= 4 && px/3 <= 6 && py/3 >= 2 && py/3 <= 8 then
                                        let c = Graphics.itemsBMP.GetPixel(item1*3 + px/3-4, py/3-2)
                                        if c.ToArgb() <> System.Drawing.Color.Black.ToArgb() then
                                            tile.SetPixel(px, py, c)
                                    // icon 2
                                    if px/3 >= 8 && px/3 <= 10 && py/3 >= 2 && py/3 <= 8 then
                                        let c = Graphics.itemsBMP.GetPixel(item2*3 + px/3-8, py/3-2)
                                        if c.ToArgb() <> System.Drawing.Color.Black.ToArgb() then
                                            tile.SetPixel(px, py, c)
                            Graphics.BMPtoImage tile
                        else
                            ms.Current()
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
                        resizeMapTileImage icon |> ignore
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
                let MODULO = MapStateProxy.NUM_ITEMS+1
                c.MouseLeftButtonDown.Add(fun _ -> 
                    let msp = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                    if msp.State = -1 then
                        // left click empty tile changes to 'X'
                        updateGridSpot -1 ""
                    else
                        // left click a shop cycles up the second item
                        if msp.IsThreeItemShop then
                            // next item
                            let e = (TrackerModel.overworldMapExtraData.[i,j] + 1) % MODULO
                            // skip past duplicates
                            let item1 = msp.State - 15  // 1-based
                            let e = if e = item1 then (e + 1) % MODULO else e
                            TrackerModel.overworldMapExtraData.[i,j] <- e
                            // redraw
                            updateGridSpot 0 ""
                    )
                c.MouseRightButtonDown.Add(fun _ -> 
                    let msp = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                    // right click a shop cycles down the second item
                    if msp.IsThreeItemShop then
                        // next item
                        let e = (TrackerModel.overworldMapExtraData.[i,j] - 1 + MODULO) % MODULO
                        // skip past duplicates
                        let item1 = msp.State - 15  // 1-based
                        let e = if e = item1 then (e - 1 + MODULO) % MODULO else e
                        TrackerModel.overworldMapExtraData.[i,j] <- e
                        // redraw
                        updateGridSpot 0 ""
                    )
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

    let recorderingCanvas = new Canvas(Width=16.*OMTW, Height=float(8*11*3))  // really the 'extra top layer' canvas for adding final marks to overworld map
    recorderingCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(c, recorderingCanvas, 0., 120.)
    let startIcon = new System.Windows.Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=System.Windows.Media.Brushes.Lime, StrokeThickness=3.0)

    let THRU_MAIN_MAP_H = float(120 + 8*11*3)

    // map legend
    let LEFT_OFFSET = 78.0
    let legendCanvas = new Canvas()
    canvasAdd(c, legendCanvas, LEFT_OFFSET, THRU_MAIN_MAP_H)

    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="The LEGEND\nof Z-Tracker")
    canvasAdd(c, tb, 0., THRU_MAIN_MAP_H)

    let shrink(bmp) = resizeMapTileImage <| Graphics.BMPtoImage bmp
    canvasAdd(legendCanvas, shrink Graphics.d1bmp, 0., 0.)
    drawDungeonHighlight(legendCanvas,0.,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Active\nDungeon")
    canvasAdd(legendCanvas, tb, OMTW, 0.)

    canvasAdd(legendCanvas, shrink Graphics.d1bmp, 2.5*OMTW, 0.)
    drawDungeonHighlight(legendCanvas,2.5,0)
    drawCompletedDungeonHighlight(legendCanvas,2.5,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Completed\nDungeon")
    canvasAdd(legendCanvas, tb, 3.5*OMTW, 0.)

    canvasAdd(legendCanvas, shrink Graphics.d1bmp, 5.*OMTW, 0.)
    drawDungeonHighlight(legendCanvas,5.,0)
    drawDungeonRecorderWarpHighlight(legendCanvas,5.,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Recorder\nDestination")
    canvasAdd(legendCanvas, tb, 6.*OMTW, 0.)

    canvasAdd(legendCanvas, shrink Graphics.w1bmp, 7.5*OMTW, 0.)
    drawWarpHighlight(legendCanvas,7.5,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Any Road\n(Warp)")
    canvasAdd(legendCanvas, tb, 8.5*OMTW, 0.)

    drawWhistleableHighlight(legendCanvas,10.,0)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Recorder\nSpots")
    canvasAdd(legendCanvas, tb, 11.*OMTW, 0.)

    let legendStartIcon = new System.Windows.Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=System.Windows.Media.Brushes.Lime, StrokeThickness=3.0)
    canvasAdd(legendCanvas, legendStartIcon, 12.5*OMTW+8.5*OMTW/48., 0.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Start\nSpot")
    canvasAdd(legendCanvas, tb, 13.5*OMTW, 0.)

    let THRU_MAP_AND_LEGEND_H = THRU_MAIN_MAP_H + float(11*3)

    // item progress
    let itemProgressCanvas = new Canvas(Width=16.*OMTW, Height=30.)
    itemProgressCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(c, itemProgressCanvas, 0., THRU_MAP_AND_LEGEND_H)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Item Progress")
    canvasAdd(c, tb, 120., THRU_MAP_AND_LEGEND_H + 6.)

    let hintGrid = makeGrid(2,OverworldData.hintMeanings.Length,140,26)
    let mutable row=0 
    for a,b in OverworldData.hintMeanings do
        gridAdd(hintGrid, new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(1.), Text=a), 0, row)
        gridAdd(hintGrid, new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(1.), Text=b), 1, row)
        row <- row + 1
    let hintBorder = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(8.), Background=Brushes.Black)
    hintBorder.Child <- hintGrid
    hintBorder.Opacity <- 0.
    hintBorder.IsHitTestVisible <- false
    canvasAdd(c, hintBorder, 430., 120.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(1.), Text="Hint Decoder")
    canvasAdd(c, tb, 680., THRU_MAP_AND_LEGEND_H + 6.)
    tb.MouseEnter.Add(fun _ -> hintBorder.Opacity <- 1.0)
    tb.MouseLeave.Add(fun _ -> hintBorder.Opacity <- 0.0)

    let THRU_MAIN_MAP_AND_ITEM_PROGRESS_H = THRU_MAP_AND_LEGEND_H + 30.

    let doUIUpdate() =
        // TODO found/not-found may need an update, only have event for found, hmm... for now just force redraw these on each update
        for i = 0 to 7 do
            if not(TrackerModel.dungeons.[i].PlayerHasTriforce()) then
                updateEmptyTriforceDisplay(i)
        recorderingCanvas.Children.Clear()
        // TODO event for redraw item progress? does any of this event interface make sense? hmmm
        itemProgressCanvas.Children.Clear()
        let mutable x, y = 200., 3.
        let DX = 30.
        match TrackerModel.playerComputedStateSummary.SwordLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage (Graphics.greyscale Graphics.magical_sword_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.brown_sword_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.white_sword_bmp, x, y)
        | 3 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.magical_sword_bmp, x, y)
        | _ -> failwith "bad SwordLevel"
        x <- x + DX
        match TrackerModel.playerComputedStateSummary.CandleLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.red_candle_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.blue_candle_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.red_candle_bmp, x, y)
        | _ -> failwith "bad CandleLevel"
        x <- x + DX
        match TrackerModel.playerComputedStateSummary.RingLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.red_ring_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.blue_ring_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.red_ring_bmp, x, y)
        | _ -> failwith "bad RingLevel"
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveBow then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.bow_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.bow_bmp), x, y)
        x <- x + DX
        match TrackerModel.playerComputedStateSummary.ArrowLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.silver_arrow_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.wood_arrow_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.silver_arrow_bmp, x, y)
        | _ -> failwith "bad ArrowLevel"
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveWand then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.wand_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.wand_bmp), x, y)
        x <- x + DX
        if !isCurrentlyBook then
            // book seed
            if TrackerModel.playerComputedStateSummary.HaveBookOrShield then
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.book_bmp, x, y)
            else
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.book_bmp), x, y)
        else
            // boomstick seed
            if TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Value() then
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.boom_book_bmp, x, y)
            else
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.boom_book_bmp), x, y)
        x <- x + DX
        match TrackerModel.playerComputedStateSummary.BoomerangLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.magic_boomerang_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.boomerang_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.magic_boomerang_bmp, x, y)
        | _ -> failwith "bad BoomerangLevel"
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveLadder then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.ladder_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.ladder_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveRecorder then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.recorder_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.recorder_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HavePowerBracelet then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.power_bracelet_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.power_bracelet_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveRaft then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.raft_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.raft_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveAnyKey then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.key_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.key_bmp), x, y)
        // place start icon in top layer
        if TrackerModel.startIconX <> -1 then
            canvasAdd(recorderingCanvas, startIcon, 11.5*OMTW/48.-3.+OMTW*float(TrackerModel.startIconX), float(TrackerModel.startIconY*11*3))
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
                    let rect = new Canvas(Width=OMTW, Height=float(11*3), Background=System.Windows.Media.Brushes.Pink)
                    rect.BeginAnimation(UIElement.OpacityProperty, fasterBlinkAnimation)
                    canvasAdd(recorderingCanvas, rect, OMTW*float(x), float(y*11*3))
            member _this.AnyRoadLocation(i,x,y) = ()
            member _this.WhistleableLocation(x,y) =
                drawWhistleableHighlight(recorderingCanvas,float x,y)
            member _this.Sword3(x,y) = 
                if not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) && TrackerModel.playerComputedStateSummary.PlayerHearts>=10 then
                    let rect = new Canvas(Width=OMTW, Height=float(11*3), Background=System.Windows.Media.Brushes.Pink)
                    rect.BeginAnimation(UIElement.OpacityProperty, fasterBlinkAnimation)
                    canvasAdd(recorderingCanvas, rect, OMTW*float(x), float(y*11*3))
            member _this.Sword2(x,y) =
                if not(TrackerModel.sword2Box.PlayerHas()) && TrackerModel.sword2Box.CellCurrent() <> -1 then
                    // display known-but-ungotten item on the map
                    let itemImage = Graphics.BMPtoImage Graphics.allItemBMPsWithHeartShuffle.[TrackerModel.sword2Box.CellCurrent()]
                    itemImage.Opacity <- 1.0
                    let color = Brushes.Black
                    let border = new Border(BorderThickness=Thickness(1.), BorderBrush=color, Background=color, Child=itemImage, Opacity=0.6)
                    canvasAdd(recorderingCanvas, border, OMTW*float(x)+OMTW-24., float(y*11*3)+4.)
            member _this.CoastItem() =
                if not(TrackerModel.ladderBox.PlayerHas()) && TrackerModel.ladderBox.CellCurrent() <> -1 then
                    // display known-but-ungotten item on the map
                    let x,y = 15,5
                    let itemImage = Graphics.BMPtoImage Graphics.allItemBMPsWithHeartShuffle.[TrackerModel.ladderBox.CellCurrent()]
                    itemImage.Opacity <- 1.0
                    let color = Brushes.Black
                    let border = new Border(BorderThickness=Thickness(3.), BorderBrush=color, Background=color, Child=itemImage, Opacity=0.6)
                    canvasAdd(recorderingCanvas, border, OMTW*float(x)+OMTW-27., float(y*11*3)+1.)
            member _this.RoutingInfo(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations,owRouteworthySpots) = 
                // clear and redraw routing
                routeDrawingCanvas.Children.Clear()
                OverworldRouting.repopulate(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations)
                let pos = System.Windows.Input.Mouse.GetPosition(routeDrawingCanvas)
                let i,j = int(Math.Floor(pos.X / OMTW)), int(Math.Floor(pos.Y / (11.*3.)))
                if i>=0 && i<16 && j>=0 && j<8 then
                    drawRoutesTo(currentRouteTarget(), routeDrawingCanvas,System.Windows.Point(0.,0.), i, j)
                // unexplored but gettable spots highlight
                if owRemainingScreensCheckBox.IsChecked.HasValue && owRemainingScreensCheckBox.IsChecked.Value then
                    for x = 0 to 15 do
                        for y = 0 to 7 do
                            if owRouteworthySpots.[x,y] && TrackerModel.overworldMapMarks.[x,y].Current() = -1 then
                                canvasAdd(recorderingCanvas, owRemainSpotHighlighters.[x,y], OMTW*float(x), float(y*11*3))
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
    canvasAdd(c, timeline1Canvas, 0., THRU_MAIN_MAP_AND_ITEM_PROGRESS_H)
    canvasAdd(c, timeline2Canvas, 0., THRU_MAIN_MAP_AND_ITEM_PROGRESS_H + timeline1Canvas.Height)
    canvasAdd(c, timeline3Canvas, 0., THRU_MAIN_MAP_AND_ITEM_PROGRESS_H + timeline1Canvas.Height + timeline2Canvas.Height)

    let THRU_TIMELINE_H = THRU_MAIN_MAP_AND_ITEM_PROGRESS_H + timeline1Canvas.Height + timeline2Canvas.Height + timeline3Canvas.Height + 3.

    // Level trackers
    let fixedDungeon1Outlines = ResizeArray()
    let fixedDungeon2Outlines = ResizeArray()

    let dungeonTabs = new TabControl(FontSize=12.)
    dungeonTabs.Background <- System.Windows.Media.Brushes.Black 
    canvasAdd(c, dungeonTabs, 0., THRU_TIMELINE_H)
    for level = 1 to 9 do
        let levelTab = new TabItem(Background=System.Windows.Media.Brushes.SlateGray)
        levelTab.Header <- sprintf "  %d  " level
        let dungeonCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7))

        levelTab.Content <- dungeonCanvas 
        dungeonTabs.Height <- dungeonCanvas.Height + 30.   // ok to set this 9 times
        dungeonTabs.Items.Add(levelTab) |> ignore

        let TEXT = sprintf "LEVEL-%d " level
        // horizontal doors
        let unknown = new SolidColorBrush(Color.FromRgb(40uy, 40uy, 50uy)) 
        let no = System.Windows.Media.Brushes.DarkRed
        let yes = new System.Windows.Media.SolidColorBrush(Color.FromRgb(0uy,180uy,0uy))
        let blackedOut = new SolidColorBrush(Color.FromRgb(15uy, 15uy, 25uy)) 
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
        // rooms
        let roomCanvases = Array2D.zeroCreate 8 8 
        let roomStates = Array2D.zeroCreate 8 8 // 0 = unexplored, 1-9 = transports, 10=vchute, 11=hchute, 12=tee, 13=tri, 14=heart, 15=start, 16=explored empty
        let roomCompleted = Array2D.zeroCreate 8 8 
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
                let image = Graphics.BMPtoImage (fst Graphics.cdungeonUnexploredRoomBMP)
                canvasAdd(c, image, 0., 0.)
                roomCanvases.[i,j] <- c
                roomStates.[i,j] <- 0
                let redraw() =
                    c.Children.Clear()
                    let image =
                        match roomStates.[i,j] with
                        | 0  -> Graphics.cdungeonUnexploredRoomBMP 
                        | 10 -> Graphics.cdungeonVChuteBMP
                        | 11 -> Graphics.cdungeonHChuteBMP
                        | 12 -> Graphics.cdungeonTeeBMP
                        | 13 -> Graphics.cdungeonTriforceBMP 
                        | 14 -> Graphics.cdungeonPrincessBMP 
                        | 15 -> Graphics.cdungeonStartBMP 
                        | 16 -> Graphics.cdungeonExploredRoomBMP 
                        | n  -> Graphics.cdungeonNumberBMPs.[n-1]
                        |> (fun (u,c) -> if roomCompleted.[i,j] || roomStates.[i,j] = 0 then c else u)
                        |> Graphics.BMPtoImage 
                    canvasAdd(c, image, 0., 0.)
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
                    redraw()
                // TODO consider hitbox (accidentally click room when trying to target doors with mouse)
                c.MouseLeftButtonDown.Add(fun _ -> 
                    if roomStates.[i,j] <> 0 then
                        roomCompleted.[i,j] <- not roomCompleted.[i,j]
                    else
                        // ad hoc useful gesture for clicking unknown room - it moves it to explored & completed state in a single click
                        roomStates.[i,j] <- 16
                        roomCompleted.[i,j] <- true
                    redraw()
                    // shift click to mark not-on-map rooms (by "no"ing all the connections)
                    if System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift) then
                        if i > 0 then
                            horizontalDoorCanvases.[i-1,j].Background <- blackedOut
                        if i < 7 then
                            horizontalDoorCanvases.[i,j].Background <- blackedOut
                        if j > 0 then
                            verticalDoorCanvases.[i,j-1].Background <- blackedOut
                        if j < 7 then
                            verticalDoorCanvases.[i,j].Background <- blackedOut
                        // don't break transport count
                        if [1..9] |> List.contains roomStates.[i,j] then
                            usedTransports.[roomStates.[i,j]] <- usedTransports.[roomStates.[i,j]] - 1
                        roomStates.[i,j] <- 0
                        // black out the room (not reflected anywhere in backing room state)
                        c.Children.Clear()
                        canvasAdd(c, Graphics.BMPtoImage (snd Graphics.cdungeonUnexploredRoomBMP), 0., 0.)
                )
                c.MouseWheel.Add(fun x -> f (x.Delta<0))
                // drag and drop to quickly 'paint' rooms
                c.MouseMove.Add(fun ea ->
                    if ea.LeftButton = System.Windows.Input.MouseButtonState.Pressed then
                        DragDrop.DoDragDrop(c, "L", DragDropEffects.Link) |> ignore
                    elif ea.RightButton = System.Windows.Input.MouseButtonState.Pressed then
                        DragDrop.DoDragDrop(c, "R", DragDropEffects.Link) |> ignore
                    )
                c.DragOver.Add(fun ea ->
                    if roomStates.[i,j] = 0 then
                        if ea.Data.GetData(DataFormats.StringFormat) :?> string = "L" then
                            roomStates.[i,j] <- 16
                            roomCompleted.[i,j] <- true
                        else
                            roomStates.[i,j] <- 16
                            roomCompleted.[i,j] <- false
                        redraw()
                    )
                c.AllowDrop <- true
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
        // "sunglasses"
        let darkenRect = new Shapes.Rectangle(Width=dungeonCanvas.Width, Height=dungeonCanvas.Height, StrokeThickness = 0., Fill=Brushes.Black, Opacity=0.15, IsHitTestVisible=false)
        canvasAdd(dungeonCanvas, darkenRect, 0., 0.)
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

    canvasAdd(c, dungeonTabsOverlay, 0., THRU_TIMELINE_H)

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
    cb.IsChecked <- System.Nullable.op_Implicit audioInitiallyOn
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
    let recorderAudioReminders = veryBasicBoxImpl(Graphics.BMPtoImage Graphics.recorder_bmp, true, false, (fun b -> voiceRemindersForRecorder <- b), (fun () -> ()))
    recorderAudioReminders.ToolTip <- "Periodic voice reminders about the number of remaining recorder spots"
    canvasAdd(c, recorderAudioReminders, RIGHT_COL + 140., 60.)
    let powerBraceletAudioReminders = veryBasicBoxImpl(Graphics.BMPtoImage Graphics.power_bracelet_bmp, true, false, (fun b -> voiceRemindersForPowerBracelet <- b), (fun () -> ()))
    powerBraceletAudioReminders.ToolTip <- "Periodic voice reminders about the number of remaining power bracelet spots"
    canvasAdd(c, powerBraceletAudioReminders, RIGHT_COL + 170., 60.)
    // coordinate grid
    let owCoordsGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owCoordsTBs = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let tb = new TextBox(Text=sprintf "%c  %d" (char (int 'A' + j)) (i+1),  // may change with OMTW and overall layout
                                    Foreground=Brushes.White, Background=Brushes.Transparent, BorderThickness=Thickness(0.0), 
                                    FontFamily=FontFamily("Consolas"), FontSize=16.0, FontWeight=FontWeights.Bold)
            tb.Opacity <- 0.0
            tb.IsHitTestVisible <- false // transparent to mouse
            owCoordsTBs.[i,j] <- tb
            let c = new Canvas(Width=OMTW, Height=float(11*3))
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
    let owMapZoneBmps =
        let avg(c1:System.Drawing.Color, c2:System.Drawing.Color) = System.Drawing.Color.FromArgb((int c1.R + int c2.R)/2, (int c1.G + int c2.G)/2, (int c1.B + int c2.B)/2)
        let colors = 
            dict [
                'M', avg(System.Drawing.Color.Pink, System.Drawing.Color.Crimson)
                'L', System.Drawing.Color.BlueViolet 
                'R', System.Drawing.Color.LightSeaGreen 
                'H', System.Drawing.Color.Gray
                'C', System.Drawing.Color.LightBlue 
                'G', avg(System.Drawing.Color.LightSteelBlue, System.Drawing.Color.SteelBlue)
                'D', System.Drawing.Color.Orange 
                'F', System.Drawing.Color.LightGreen 
                'S', System.Drawing.Color.DarkGray 
                'W', System.Drawing.Color.Brown
            ]
        let imgs = Array2D.zeroCreate 16 8
        for x = 0 to 15 do
            for y = 0 to 7 do
                let tile = new System.Drawing.Bitmap(int OMTW,11*3)
                for px = 0 to int OMTW-1 do
                    for py = 0 to 11*3-1 do
                        tile.SetPixel(px, py, colors.Item(OverworldData.owMapZone.[y].[x]))
                imgs.[x,y] <- tile
        imgs

    let owMapZoneGrid = makeGrid(16, 8, int OMTW, 11*3)
    let allOwMapZoneImages = ResizeArray()
    for i = 0 to 15 do
        for j = 0 to 7 do
            let image = Graphics.BMPtoImage owMapZoneBmps.[i,j]
            image.Opacity <- 0.0
            image.IsHitTestVisible <- false // transparent to mouse
            allOwMapZoneImages.Add(image)
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            canvasAdd(c, image, 0., 0.)
            gridAdd(owMapZoneGrid, c, i, j)
    canvasAdd(c, owMapZoneGrid, 0., 120.)

    let owMapZoneBoundaries = ResizeArray()
    let makeLine(x1, x2, y1, y2) = 
        let line = new System.Windows.Shapes.Line(X1=OMTW*float(x1), X2=OMTW*float(x2), Y1=float(y1*11*3), Y2=float(y2*11*3), Stroke=Brushes.White, StrokeThickness=3.)
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

    let zoneNames = ResizeArray()
    let addZoneName(name, x, y) =
        let tb = new TextBox(Text=name,FontSize=16.,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(2.),IsReadOnly=true)
        canvasAdd(c, tb, 0. + x*OMTW, 120.+y*11.*3.)
        tb.Opacity <- 0.
        tb.TextAlignment <- TextAlignment.Center
        tb.FontWeight <- FontWeights.Bold
        tb.IsHitTestVisible <- false
        zoneNames.Add(tb)
    addZoneName("DEATH\nMOUNTAIN", 3.0, 0.3)
    addZoneName("GRAVE", 1.8, 2.5)
    addZoneName("DEAD\nWOODS", 1.8, 6.0)
    addZoneName("LAKE 1", 10.0, 0.1)
    addZoneName("LAKE 2", 5.0, 4.0)
    addZoneName("LAKE 3", 9.5, 5.5)
    addZoneName("RIVER 1", 8.3, 1.1)
    addZoneName("RIV\nER2", 5.1, 6.2)
    addZoneName("START", 7.3, 6.2)
    addZoneName("DESERT", 10.3, 3.1)
    addZoneName("FOREST", 12.3, 5.1)
    addZoneName("LOST\nHILLS", 12.3, 0.3)
    addZoneName("COAST", 14.3, 2.6)

    let changeZoneOpacity(show) =
        if show then
            allOwMapZoneImages |> Seq.iter (fun i -> i.Opacity <- 0.3)
            owMapZoneBoundaries |> Seq.iter (fun x -> x.Opacity <- 0.9)
            zoneNames |> Seq.iter (fun x -> x.Opacity <- 0.6)
        else
            allOwMapZoneImages |> Seq.iter (fun i -> i.Opacity <- 0.0)
            owMapZoneBoundaries |> Seq.iter (fun x -> x.Opacity <- 0.0)
            zoneNames |> Seq.iter (fun x -> x.Opacity <- 0.0)
    let cb = new CheckBox(Content=new TextBox(Text="Show zones",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    cb.IsChecked <- System.Nullable.op_Implicit false
    cb.Checked.Add(fun _ -> changeZoneOpacity true)
    cb.Unchecked.Add(fun _ -> changeZoneOpacity false)
    cb.MouseEnter.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then changeZoneOpacity true)
    cb.MouseLeave.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then changeZoneOpacity false)
    canvasAdd(c, cb, 285., 100.)

    //                items  ow map  prog  timeline    dungeon tabs                
    c.Height <- float(30*4 + 11*3*9 + 30 + 3*TLH + 3 + TH + 27*8 + 12*7 + 30)
    TrackerModel.forceUpdate()
    c, updateTimeline


