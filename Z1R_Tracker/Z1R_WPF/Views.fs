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

let MakeTriforceDisplayView(cm:CustomComboBoxes.CanvasManager, trackerIndex, owInstanceOpt, makeInteractive) =
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
    redraw()
    // interactions
    if makeInteractive then
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
    // redraw if label changed, as that can (un)link an existing hint
    dungeon.HiddenDungeonColorOrLabelChanged.Add(fun _ -> redraw())
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


let redrawBoxes = ResizeArray()
TrackerModel.IsCurrentlyBookChanged.Add(fun _ ->
    TrackerModel.forceUpdate()
    for f in redrawBoxes do
        f()
    )
let MakeBoxItemWithExtraDecorations(cm:CustomComboBoxes.CanvasManager, box:TrackerModel.Box, accelerateIntoComboBox, computeExtraDecorationsWhenPopupActivatedOrMouseOver) = 
    let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
    if box.Stair <> TrackerModel.StairKind.Never then
        let stairImg = Graphics.basement_stair_bmp |> Graphics.BMPtoImage
        match box.Stair with
        | TrackerModel.StairKind.LikeL2 ->
            let update() = stairImg.Opacity <- if TrackerModel.Options.IsSecondQuestDungeons.Value && TrackerModel.Options.ShowBasementInfo.Value then 1.0 else 0.0
            OptionsMenu.secondQuestDungeonsOptionChanged.Publish.Add(update)
            OptionsMenu.showBasementInfoOptionChanged.Publish.Add(update)
        | TrackerModel.StairKind.LikeL3 ->
            let update() = stairImg.Opacity <- if not(TrackerModel.Options.IsSecondQuestDungeons.Value) && TrackerModel.Options.ShowBasementInfo.Value then 1.0 else 0.0
            OptionsMenu.secondQuestDungeonsOptionChanged.Publish.Add(update)
            OptionsMenu.showBasementInfoOptionChanged.Publish.Add(update)
        | TrackerModel.StairKind.Always ->
            let update() = stairImg.Opacity <- if TrackerModel.Options.ShowBasementInfo.Value then 1.0 else 0.0
            OptionsMenu.showBasementInfoOptionChanged.Publish.Add(update)
        | _ -> ()
        canvasAdd(c, stairImg, 3., 3.)
    let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., Stroke=CustomComboBoxes.no, StrokeThickness=3.0)
    c.Children.Add(rect) |> ignore
    let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has item drawn on it, not the box
    c.Children.Add(innerc) |> ignore
    let redraw() =
        // redraw inner canvas
        innerc.Children.Clear()
        let bmp = CustomComboBoxes.boxCurrentBMP(box.CellCurrent(), false)
        if bmp <> null then
            canvasAdd(innerc, new Canvas(Background=Brushes.Black, Width=21., Height=21.), 3., 3.)  // cover up any stair drawing
            if box.PlayerHas() = TrackerModel.PlayerHas.NO then
                let image = Graphics.BMPtoImage(Graphics.greyscale bmp)  //Graphics.desaturate(bmp,0.99))
                canvasAdd(innerc, image, 4., 4.)
            else
                let image = Graphics.BMPtoImage(bmp) //Graphics.desaturate(bmp,0.60))
                canvasAdd(innerc, image, 4., 4.)
                //image.Stretch <- Stretch.Uniform
                //image.Width <- 14.
                //image.Height <- 14.
                //canvasAdd(innerc, image, 8., 8.)
        // redraw box outline
        match box.PlayerHas() with
        | TrackerModel.PlayerHas.YES -> rect.Stroke <- CustomComboBoxes.yes
        | TrackerModel.PlayerHas.NO -> rect.Stroke <- if bmp=null then CustomComboBoxes.no else CustomComboBoxes.noAndNotEmpty
        | TrackerModel.PlayerHas.SKIPPED -> 
            if bmp=null then 
                rect.Stroke <- CustomComboBoxes.skippedAndEmpty 
            else 
                rect.Stroke <- CustomComboBoxes.skipped
                CustomComboBoxes.placeSkippedItemXDecoration(innerc)
    redraw()
    // interactions
    let mutable popupIsActive = false
    let activateComboBox(activationDelta) =
        popupIsActive <- true
        let pos = c.TranslatePoint(Point(),cm.AppMainCanvas)
        let extraDecorations = computeExtraDecorationsWhenPopupActivatedOrMouseOver(pos)
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
    let hoverCanvas = new Canvas()
    c.MouseEnter.Add(fun _ ->
        cm.AppMainCanvas.Children.Remove(hoverCanvas)  // safeguard, in case MouseEnter/MouseLeave parity is broken
        let pos = c.TranslatePoint(Point(),cm.AppMainCanvas)
        let extraDecorations = computeExtraDecorationsWhenPopupActivatedOrMouseOver(pos)
        hoverCanvas.Children.Clear()
        for fe, x, y in extraDecorations do
            canvasAdd(hoverCanvas, fe, x+3., y+3.)   // +3s because decorations are relative to the combobox popup, which is over the interior icon area, excluding the rectangle border
        canvasAdd(cm.AppMainCanvas, hoverCanvas, pos.X, pos.Y) |> ignore
        )
    c.MouseLeave.Add(fun _ -> cm.AppMainCanvas.Children.Remove(hoverCanvas))
    // redraw on changes
    redrawBoxes.Add(fun() -> redraw())
    box.Changed.Add(fun _ -> redraw())
    c
let MakeBoxItem(cm:CustomComboBoxes.CanvasManager, box:TrackerModel.Box) = 
    MakeBoxItemWithExtraDecorations(cm, box, false, fun(_)->[])
