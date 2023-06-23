module Views

open System.Windows.Controls
open System.Windows.Media
open System.Windows

open HotKeys.MyKey

let canvasAdd = Graphics.canvasAdd

(*
This module is for reusable display elements with the following properties:
 - they represent a display of some portion of the TrackerModel
 - they can redraw themselves by listening for changes to the TrackerModel, and never need to otherwise be redrawn, as their state is entirely TrackerModel-evented
 - they might optionally also have interactive/update abilities to change the model (which will, of course, be reflected back in their display view)
*)

type GlobalBoxMouseOverHighlight() =
    let globalBoxMouseOverHighlight = new System.Windows.Shapes.Rectangle(Width=34., Height=34., Stroke=Brushes.DarkTurquoise, StrokeThickness=2.0, Opacity=0.0)
    let mutable setGlobalBoxMouseOverHighlight = fun(_b,_e:UIElement) -> ()
    member this.DetachFromGlobalCanvas(c:Canvas) =
        c.Children.Remove(globalBoxMouseOverHighlight)
    member this.AttachToGlobalCanvas(c) =
        canvasAdd(c, globalBoxMouseOverHighlight, 0., 0.)
        Canvas.SetZIndex(globalBoxMouseOverHighlight, 99999)
        setGlobalBoxMouseOverHighlight <- (fun(b,e) ->
            if b then
                let pos = e.TranslatePoint(Point(-2.,-2.), c)
                Canvas.SetLeft(globalBoxMouseOverHighlight, pos.X)
                Canvas.SetTop(globalBoxMouseOverHighlight, pos.Y)
                globalBoxMouseOverHighlight.Opacity <- 1.0
            else
                globalBoxMouseOverHighlight.Opacity <- 0.0
            )
    member this.ApplyBehavior(e:UIElement) =
        e.MouseEnter.Add(fun _ -> setGlobalBoxMouseOverHighlight(true,e))
        e.MouseLeave.Add(fun _ -> setGlobalBoxMouseOverHighlight(false,e))
let appMainCanvasGlobalBoxMouseOverHighlight = new GlobalBoxMouseOverHighlight()

let hintHighlightBrush = new LinearGradientBrush(Colors.Yellow, Colors.DarkGreen, 45.)
let makeHintHighlight(size) = new Shapes.Rectangle(Width=size, Height=size, StrokeThickness=0., Fill=hintHighlightBrush)

let emptyUnfoundTriforce_bmp(i) =
    match TrackerModel.DungeonTrackerInstance.TheDungeonTrackerInstance.Kind with
    | TrackerModel.DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS -> Graphics.emptyUnfoundLetteredTriforce_bmps.[i]
    | TrackerModel.DungeonTrackerInstanceKind.DEFAULT -> Graphics.emptyUnfoundNumberedTriforce_bmps.[i]
let emptyFoundTriforce_bmp(i) =
    match TrackerModel.DungeonTrackerInstance.TheDungeonTrackerInstance.Kind with
    | TrackerModel.DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS -> Graphics.emptyFoundLetteredTriforce_bmps.[i]
    | TrackerModel.DungeonTrackerInstanceKind.DEFAULT -> Graphics.emptyFoundNumberedTriforce_bmps.[i]
let fullUnfoundTriforce_bmp(i) =
    match TrackerModel.DungeonTrackerInstance.TheDungeonTrackerInstance.Kind with
    | TrackerModel.DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS -> Graphics.fullLetteredUnfoundTriforce_bmps.[i]
    | TrackerModel.DungeonTrackerInstanceKind.DEFAULT -> Graphics.fullNumberedUnfoundTriforce_bmps.[i]
let fullFoundTriforce_bmp(i) =
    match TrackerModel.DungeonTrackerInstance.TheDungeonTrackerInstance.Kind with
    | TrackerModel.DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS -> Graphics.fullLetteredFoundTriforce_bmps.[i]
    | TrackerModel.DungeonTrackerInstanceKind.DEFAULT -> Graphics.fullNumberedFoundTriforce_bmps.[i]

// make a superscript icon to help visualize implications of 'Force OW block' rando flag
let drawTinyIconIfLocationIsOverworldBlock(c:Canvas, owInstanceOpt:OverworldData.OverworldInstance option, location) =
    match owInstanceOpt with
    | Some owInstance ->
        if location<>TrackerModel.NOTFOUND then
            // mark overworld block in the upper right corner
            let icon =
                if owInstance.Raftable(location) then
                    Graphics.BMPtoImage Graphics.raft_bmp
                elif owInstance.PowerBraceletable(location) then
                    Graphics.BMPtoImage Graphics.power_bracelet_bmp
                elif owInstance.Ladderable(location) then
                    Graphics.BMPtoImage Graphics.ladder_bmp
                elif owInstance.Whistleable(location) then
                    Graphics.BMPtoImage Graphics.recorder_bmp
                else
                    null
            if icon <> null then
                icon.Width <- 7.
                icon.Height <- 7.
                canvasAdd(c, icon, 23., 5.)
    | None -> ()
let SynthesizeANewLocationKnownEvent(mapChoiceDomainChangePublished:IEvent<_>) =
    let resultEvent = new Event<_>()
    // if location changes...
    let mutable needDetailAboutNewLocation = false
    mapChoiceDomainChangePublished.Add(fun _ -> 
        needDetailAboutNewLocation <- true
        )
    // ... Trigger after we can look up its new location coordinates
    TrackerModel.mapStateSummaryComputedEvent.Publish.Add(fun _ ->
        if needDetailAboutNewLocation then
            resultEvent.Trigger()
            needDetailAboutNewLocation <- false
        )
    resultEvent.Publish

// putting the mouse in the very center of triforce/item boxes (30x30) kind of obscures the numeral or pixel art
// if we are warping the cursor to one of these, ideally put it here in the box, to look nice
let IDEAL_BOX_MOUSE_X = 22.
let IDEAL_BOX_MOUSE_Y = 17.

let redrawTriforces = ResizeArray()
let MakeTriforceDisplayView(cmo:CustomComboBoxes.CanvasManager option, trackerIndex, owInstanceOpt) =    // cmo=Some(cm) means "makeInteractive"
    let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Black)
    let dungeon = TrackerModel.GetDungeon(trackerIndex)
    let redraw() =
        innerc.Children.Clear()
        let found = dungeon.HasBeenLocated()
        if not(TrackerModel.IsHiddenDungeonNumbers()) then
            if not(found) && TrackerModel.GetLevelHint(trackerIndex)<>TrackerModel.HintZone.UNKNOWN then
                innerc.Children.Add(makeHintHighlight(30.)) |> ignore
        else
            let label = dungeon.LabelChar
            if label >= '1' && label <= '8' then
                let hintIndex = int label - int '1'
                if not(found) && TrackerModel.GetLevelHint(hintIndex)<>TrackerModel.HintZone.UNKNOWN then
                    innerc.Children.Add(makeHintHighlight(30.)) |> ignore
        if not(dungeon.PlayerHasTriforce()) then 
            innerc.Children.Add(Graphics.BMPtoImage(if not(found) then emptyUnfoundTriforce_bmp(trackerIndex) else emptyFoundTriforce_bmp(trackerIndex))) |> ignore
        else
            innerc.Children.Add(Graphics.BMPtoImage(if not(found) then fullUnfoundTriforce_bmp(trackerIndex) else fullFoundTriforce_bmp(trackerIndex))) |> ignore 
        drawTinyIconIfLocationIsOverworldBlock(innerc, owInstanceOpt, TrackerModel.mapStateSummary.DungeonLocations.[trackerIndex])
        if TrackerModel.DungeonBlockersContainer.MAX_BLOCKERS_PER_DUNGEON <> 3 then
            failwith "This UI was designed for 3 blockers per dungeon"
        if not(dungeon.PlayerHasTriforce()) then // draw specific blockers
            let i = trackerIndex
            for j = 0 to 2 do
                if i<>8 && TrackerModel.DungeonBlockersContainer.GetDungeonBlockerAppliesTo(i,j,2) then
                    let blocker = TrackerModel.DungeonBlockersContainer.GetDungeonBlocker(i,j)
                    let bmp = Graphics.blockerHardCanonicalBMP(blocker)
                    if bmp <> null then
                        let img = Graphics.BMPtoImage bmp
                        img.Width <- 7.
                        img.Height <- 7.
                        if not(blocker.PlayerCouldBeBlockedByThis()) then
                            canvasAdd(innerc, new Canvas(Width=9., Height=9., Background=Brushes.Lime),  float(1+j*10), 7.+16.)
                            canvasAdd(innerc, new Canvas(Width=7., Height=7., Background=Brushes.Black), float(2+j*10), 8.+16.)
                        else
                            canvasAdd(innerc, new Canvas(Width=9., Height=9., Background=Brushes.Black), float(1+j*10), 7.+16.)
                        canvasAdd(innerc, img, float(2+j*10), 8.+16.)
    redraw()
    // interactions
    match cmo with
    | Some(cm) ->
        let mutable popupIsActive = false
        innerc.MouseDown.Add(fun _ -> 
            if not popupIsActive then
                dungeon.ToggleTriforce()
                if dungeon.PlayerHasTriforce() && TrackerModel.IsHiddenDungeonNumbers() && dungeon.LabelChar='?' then
                    // if it's hidden dungeon numbers, the player just got a triforce, and the player has not yet set the dungeon number, then popup the number chooser
                    popupIsActive <- true
                    let pos = innerc.TranslatePoint(Point(15., 15.), cm.AppMainCanvas)
                    async {
                        do! Dungeon.HiddenDungeonCustomizerPopup(cm, trackerIndex, dungeon.Color, dungeon.LabelChar, true, pos)
                        popupIsActive <- false
                        } |> Async.StartImmediate
            )
        Dungeon.HotKeyAHiddenDungeonLabel(innerc, dungeon, None)
        appMainCanvasGlobalBoxMouseOverHighlight.ApplyBehavior(innerc)
    | _ -> ()
    // redraw if PlayerHas changes
    dungeon.PlayerHasTriforceChanged.Add(fun _ -> redraw())
    // redraw after we can look up its new location coordinates
    let newLocation = SynthesizeANewLocationKnownEvent(dungeon.HasBeenLocatedChanged)
    newLocation.Add(fun _ -> redraw())
    // redraw if hinting changes
    if not(TrackerModel.IsHiddenDungeonNumbers()) then
        TrackerModel.LevelHintChanged(trackerIndex).Add(fun _ -> redraw())
    else
        for i = 0 to 7 do TrackerModel.LevelHintChanged(i).Add(fun _ -> redraw())   // just redraw on any hints, rather than try to subscribe/unsubscribe based on LabelChar changes
    // redraw if label changed, as that can (un)link an existing hint; or if blockers changed (may need to rewdraw specific-blocker)
    dungeon.HiddenDungeonColorOrLabelChanged.Add(fun _ -> redraw())
    TrackerModel.DungeonBlockersContainer.AnyBlockerChanged.Add(fun _ -> redraw())
    redrawTriforces.Add(redraw)
    innerc
let MakeLevel9View(owInstanceOpt) =
    let level9NumeralCanvas = new Canvas(Width=30., Height=30., Background=Brushes.Black)
    let dungeon = TrackerModel.GetDungeon(8)
    let redraw() =
        level9NumeralCanvas.Children.Clear()
        let l9found = dungeon.HasBeenLocated()
        let img = Graphics.BMPtoImage(if not(l9found) then Graphics.unfoundL9_bmp else Graphics.foundL9_bmp)
        if not(l9found) && TrackerModel.GetLevelHint(8)<>TrackerModel.HintZone.UNKNOWN then
            canvasAdd(level9NumeralCanvas, makeHintHighlight(30.), 0., 0.)
        canvasAdd(level9NumeralCanvas, img, 0., 0.)
        drawTinyIconIfLocationIsOverworldBlock(level9NumeralCanvas, owInstanceOpt, TrackerModel.mapStateSummary.DungeonLocations.[8])
    redraw()
    // redraw after we can look up its new location coordinates
    let newLocation = SynthesizeANewLocationKnownEvent(dungeon.HasBeenLocatedChanged)
    newLocation.Add(fun _ -> redraw())
    // redraw if hinting changes
    TrackerModel.LevelHintChanged(8).Add(fun _ -> redraw())
    level9NumeralCanvas
let MakeColorNumberCanvasForHDN(dungeonIndex) =
    // color/number canvas
    if TrackerModel.IsHiddenDungeonNumbers() then
        let colorCanvas = new Canvas(Width=28., Height=28., Background=Brushes.Black)
        let d = TrackerModel.GetDungeon(dungeonIndex)
        let redraw(color,labelChar) =
            colorCanvas.Background <- new SolidColorBrush(Graphics.makeColor(color))
            colorCanvas.Children.Clear()
            let color = if Graphics.isBlackGoodContrast(color) then System.Drawing.Color.Black else System.Drawing.Color.White
            if d.LabelChar <> '?' then
                colorCanvas.Children.Add(Graphics.BMPtoImage(Graphics.alphaNumOnTransparentBmp(labelChar, color, 28, 28, 3, 2))) |> ignore
        redraw(d.Color, d.LabelChar)
        d.HiddenDungeonColorOrLabelChanged.Add(redraw)
        colorCanvas
    else
        null

let redrawBoxes = ResizeArray()
TrackerModel.IsCurrentlyBookChanged.Add(fun _ ->
    TrackerModel.forceUpdate()
    for f in redrawBoxes do
        f()
    )
TrackerModel.DungeonBlockersContainer.AnyBlockerChanged.Add(fun _ ->
    for f in redrawBoxes do
        f()
    )
let MakeBoxItemWithExtraDecorations(cmo:CustomComboBoxes.CanvasManager option, box:TrackerModel.Box, accelerateIntoComboBox, computeExtraDecorationsWhenPopupActivatedOrMouseOverOpt) = 
    let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
    if box.Stair <> TrackerModel.StairKind.Never then
        let stairImg = Graphics.basement_stair_bmp |> Graphics.BMPtoImage
        match box.Stair with
        | TrackerModel.StairKind.LikeL2 ->
            let update() = stairImg.Opacity <- if TrackerModelOptions.IsSecondQuestDungeons.Value && TrackerModelOptions.ShowBasementInfo.Value then 1.0 else 0.0
            OptionsMenu.secondQuestDungeonsOptionChanged.Publish.Add(update)
            OptionsMenu.showBasementInfoOptionChanged.Publish.Add(update)
            update()
        | TrackerModel.StairKind.LikeL3 ->
            let update() = stairImg.Opacity <- if not(TrackerModelOptions.IsSecondQuestDungeons.Value) && TrackerModelOptions.ShowBasementInfo.Value then 1.0 else 0.0
            OptionsMenu.secondQuestDungeonsOptionChanged.Publish.Add(update)
            OptionsMenu.showBasementInfoOptionChanged.Publish.Add(update)
            update()
        | TrackerModel.StairKind.Always ->
            let update() = stairImg.Opacity <- if TrackerModelOptions.ShowBasementInfo.Value then 1.0 else 0.0
            OptionsMenu.showBasementInfoOptionChanged.Publish.Add(update)
            update()
        | _ -> ()
        canvasAdd(c, stairImg, 3., 3.)
    let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., Stroke=CustomComboBoxes.no, StrokeThickness=3.0)
    c.Children.Add(rect) |> ignore
    let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has item drawn on it, not the box
    c.Children.Add(innerc) |> ignore
    let innerCanvasStairwayHider = new Canvas(Background=Brushes.Black, Width=21., Height=21.)
    let redraw() =
        // redraw inner canvas
        innerc.Children.Clear()
        let bmp = CustomComboBoxes.boxCurrentBMP(box.CellCurrent(), None)
        if bmp <> null then
            canvasAdd(innerc, innerCanvasStairwayHider, 3., 3.)  // cover up any stair drawing
            if box.PlayerHas() = TrackerModel.PlayerHas.NO then
                let image = Graphics.BMPtoImage(Graphics.greyscale bmp)
                canvasAdd(innerc, image, 4., 4.)
            else
                let image = Graphics.BMPtoImage(bmp)
                canvasAdd(innerc, image, 4., 4.)
        // redraw box outline
        match box.PlayerHas() with
        | TrackerModel.PlayerHas.YES -> rect.Stroke <- CustomComboBoxes.yes
        | TrackerModel.PlayerHas.NO -> 
            if bmp=null then 
                rect.Stroke <- 
                    try CustomComboBoxes.noPct(TrackerModel.DungeonTrackerInstance.TheDungeonTrackerInstance.AllBoxProgress.Value) 
                    with _ -> CustomComboBoxes.no  // we create fake Views on the startup menu to preview Heart Shuffle; TheDungeonTrackerInstance does not exist yet and throws
            else 
                rect.Stroke <- CustomComboBoxes.noAndNotEmpty
        | TrackerModel.PlayerHas.SKIPPED -> 
            if bmp=null then 
                rect.Stroke <- CustomComboBoxes.skippedAndEmpty 
            else 
                rect.Stroke <- CustomComboBoxes.skipped
                Graphics.placeSkippedItemXDecoration(innerc)
        // redraw specific-blockers
        match box.PlayerHas() with
        | TrackerModel.PlayerHas.YES -> ()
        | _ ->
            let owner = 
                match box.Owner with
                | TrackerModel.BoxOwner.Dungeon1or4 -> 
                    if TrackerModelOptions.IsSecondQuestDungeons.Value then TrackerModel.BoxOwner.DungeonIndexAndNth(3,2) else TrackerModel.BoxOwner.DungeonIndexAndNth(0,2)
                | _ -> box.Owner
            match owner with
            | TrackerModel.BoxOwner.DungeonIndexAndNth(i,n) -> 
                if TrackerModel.DungeonBlockersContainer.MAX_BLOCKERS_PER_DUNGEON <> 3 then
                    failwith "This UI was designed for 3 blockers per dungeon"
                for j = 0 to 2 do
                    if i<>8 && TrackerModel.DungeonBlockersContainer.GetDungeonBlockerAppliesTo(i,j,n+3) then
                        let blocker = TrackerModel.DungeonBlockersContainer.GetDungeonBlocker(i,j)
                        let bmp = Graphics.blockerHardCanonicalBMP(blocker)
                        if bmp <> null then
                            let img = Graphics.BMPtoImage bmp
                            img.Width <- 7.
                            img.Height <- 7.
                            if not(blocker.PlayerCouldBeBlockedByThis()) then
                                canvasAdd(innerc, new Canvas(Width=9., Height=9., Background=Brushes.Lime),  float(1+j*10), 4.+16.)
                                canvasAdd(innerc, new Canvas(Width=7., Height=7., Background=Brushes.Black), float(2+j*10), 5.+16.)
                            else
                                canvasAdd(innerc, new Canvas(Width=9., Height=9., Background=Brushes.Black), float(1+j*10), 4.+16.)
                            canvasAdd(innerc, img, float(2+j*10), 5.+16.)
            | _ -> ()
    redraw()
    try TrackerModel.DungeonTrackerInstance.TheDungeonTrackerInstance.AllBoxProgress.Changed.Add(fun _ -> redraw())
    with _ -> () // we create fake Views on the startup menu to preview Heart Shuffle; TheDungeonTrackerInstance does not exist yet and throws
    // interactions
    match cmo with
    | Some cm ->
        let mutable popupIsActive = false
        let activateComboBox(activationDelta) =
            popupIsActive <- true
            let pos = c.TranslatePoint(Point(),cm.AppMainCanvas)
            let extraDecorations = match computeExtraDecorationsWhenPopupActivatedOrMouseOverOpt with | Some f -> f(pos) | None -> seq[]
            async {
                let! r = CustomComboBoxes.DisplayItemComboBox(cm, pos.X, pos.Y, box.CellCurrent(), activationDelta, box.PlayerHas(), extraDecorations)
                match r with
                | Some(newBoxCellValue, newPlayerHas) -> box.Set(newBoxCellValue, newPlayerHas)
                | None -> ()
                popupIsActive <- false
                } |> Async.StartImmediate
        c.MouseDown.Add(fun ea ->
            if not popupIsActive then
                if ea.ButtonState = Input.MouseButtonState.Pressed &&
                        (ea.ChangedButton = Input.MouseButton.Left || ea.ChangedButton = Input.MouseButton.Middle || ea.ChangedButton = Input.MouseButton.Right) then
                    ea.Handled <- true
                    if box.CellCurrent() = -1 then
                        activateComboBox(0)
                    else
                        let desire = CustomComboBoxes.MouseButtonEventArgsToPlayerHas ea
                        if desire = box.PlayerHas() then
                            activateComboBox(0) // rather than idempotent gesture doing nothing, a second try (e.g. left click and already-have item) reactivates popup (easier to discover than scroll)
                        else
                            box.SetPlayerHas(desire)
            )
        c.MouseWheel.Add(fun ea -> 
            if not popupIsActive then 
                ea.Handled <- true
                activateComboBox(if ea.Delta<0 then 1 else -1)
            )
        c.MyKeyAdd(fun ea -> 
            if not popupIsActive then
                match HotKeys.ItemHotKeyProcessor.TryGetValue(ea.Key) with
                | Some(i) ->
                    ea.Handled <- true
                    if i <> -1 && box.CellCurrent() = i then
                        // if this box already contains the hotkey'd item, pressing the hotkey cycles the PlayerHas state NO -> YES -> SKIPPED
                        if box.PlayerHas() = TrackerModel.PlayerHas.NO then
                            box.Set(i, TrackerModel.PlayerHas.YES)
                        elif box.PlayerHas() = TrackerModel.PlayerHas.YES then
                            box.Set(i, TrackerModel.PlayerHas.SKIPPED)
                        else
                            box.Set(i, TrackerModel.PlayerHas.NO)
                    elif i = -1 then
                        if box.CellCurrent() <> -1 then
                            box.Set(i, TrackerModel.PlayerHas.NO)      // emptying a full box always goes to NO first
                        elif box.PlayerHas()=TrackerModel.PlayerHas.NO then
                            box.Set(i, TrackerModel.PlayerHas.SKIPPED) // emptying an empty box toggles the box outline color
                        else
                            box.Set(i, TrackerModel.PlayerHas.NO)      // emptying an empty box toggles the box outline color
                    else
                        // changing from empty/other-item box to this item value always NO on first hotkey press, as this is 'model harmless'
                        if not(box.AttemptToSet(i, TrackerModel.PlayerHas.NO)) then
                            System.Media.SystemSounds.Asterisk.Play()  // e.g. they tried to set this box to Bow when another box already has 'Bow'
                | None -> ()
            )
        if accelerateIntoComboBox then
            c.Loaded.Add(fun _ -> activateComboBox(0))
        // hover behavior
        appMainCanvasGlobalBoxMouseOverHighlight.ApplyBehavior(c)
        match computeExtraDecorationsWhenPopupActivatedOrMouseOverOpt with
        | Some f ->
            let hoverCanvas = new Canvas()
            c.MouseEnter.Add(fun _ ->
                cm.AppMainCanvas.Children.Remove(hoverCanvas)  // safeguard, in case MouseEnter/MouseLeave parity is broken
                let pos = c.TranslatePoint(Point(),cm.AppMainCanvas)
                let extraDecorations = f(pos)
                hoverCanvas.Children.Clear()
                for fe, x, y in extraDecorations do
                    canvasAdd(hoverCanvas, fe, x+3., y+3.)   // +3s because decorations are relative to the combobox popup, which is over the interior icon area, excluding the rectangle border
                canvasAdd(cm.AppMainCanvas, hoverCanvas, pos.X, pos.Y) |> ignore
                )
            c.MouseLeave.Add(fun _ -> cm.AppMainCanvas.Children.Remove(hoverCanvas))
        | None -> ()
    | _ -> ()
    // redraw on changes
    redrawBoxes.Add(fun() -> redraw())
    box.Changed.Add(fun _ -> redraw())
    c
let MakeBoxItem(cm:CustomComboBoxes.CanvasManager, box:TrackerModel.Box) = 
    MakeBoxItemWithExtraDecorations(Some(cm), box, false, None)

// blocker view
let blocker_gsc = new GradientStopCollection([new GradientStop(Color.FromArgb(255uy, 60uy, 180uy, 60uy), 0.)
                                              new GradientStop(Color.FromArgb(255uy, 80uy, 80uy, 80uy), 0.4)
                                              new GradientStop(Color.FromArgb(255uy, 80uy, 80uy, 80uy), 0.6)
                                              new GradientStop(Color.FromArgb(255uy, 180uy, 60uy, 60uy), 1.0)
                                             ])
let blocker_brush = new LinearGradientBrush(blocker_gsc, Point(0.,0.), Point(1.,1.))
let MakeBlockerCore() =
    let c = new Canvas(Width=30., Height=30., Background=Brushes.Black, IsHitTestVisible=true)
    let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=Brushes.Gray, StrokeThickness=3.0, IsHitTestVisible=false)
    let redraw(n) = 
        c.Children.Clear()
        match n with
        | TrackerModel.DungeonBlocker.MAYBE_LADDER 
        | TrackerModel.DungeonBlocker.MAYBE_RECORDER
        | TrackerModel.DungeonBlocker.MAYBE_BAIT
        | TrackerModel.DungeonBlocker.MAYBE_BOMB
        | TrackerModel.DungeonBlocker.MAYBE_BOW_AND_ARROW
        | TrackerModel.DungeonBlocker.MAYBE_KEY
        | TrackerModel.DungeonBlocker.MAYBE_MONEY
            -> rect.Stroke <- blocker_brush
        | TrackerModel.DungeonBlocker.NOTHING -> rect.Stroke <- Brushes.Gray
        | _ -> rect.Stroke <- Brushes.LightGray
        c.Children.Add(rect) |> ignore
        canvasAdd(c, Graphics.blockerCurrentDisplay(n) , 3., 3.)
        c
    c, redraw
let MakeBlockerView(dungeonIndex, blockerIndex) =
    let c,redraw = MakeBlockerCore()
    let mutable current = TrackerModel.DungeonBlockersContainer.GetDungeonBlocker(dungeonIndex, blockerIndex)
    redraw(current) |> ignore
    TrackerModel.DungeonBlockersContainer.AnyBlockerChanged.Add(fun _ ->
        current <- TrackerModel.DungeonBlockersContainer.GetDungeonBlocker(dungeonIndex, blockerIndex)
        redraw(current) |> ignore
        )
    c
