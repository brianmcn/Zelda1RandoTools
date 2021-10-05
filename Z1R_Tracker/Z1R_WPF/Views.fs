module Views

open System.Windows.Controls
open System.Windows.Media
open System.Windows

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
let fullTriforce_bmp(i) =
    match TrackerModel.DungeonTrackerInstance.TheDungeonTrackerInstance.Kind with
    | TrackerModel.DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS -> Graphics.fullLetteredTriforce_bmps.[i]
    | TrackerModel.DungeonTrackerInstanceKind.DEFAULT -> Graphics.fullNumberedTriforce_bmps.[i]

let MakeTriforceDisplayView(cm:CustomComboBoxes.CanvasManager, trackerIndex) =
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
            innerc.Children.Add(Graphics.BMPtoImage(fullTriforce_bmp(trackerIndex))) |> ignore 
    redraw()
    // interactions
    let mutable popupIsActive = false
    innerc.MouseDown.Add(fun _ -> 
        if not popupIsActive then
            dungeon.ToggleTriforce()
            if dungeon.PlayerHasTriforce() && TrackerModel.IsHiddenDungeonNumbers() && dungeon.LabelChar='?' then
                // if it's hidden dungeon numbers, the player just got a triforce, and the player has not yet set the dungeon number, then popup the number chooser
                popupIsActive <- true
                let pos = innerc.TranslatePoint(Point(15., 15.), cm.AppMainCanvas)
                Dungeon.HiddenDungeonCustomizerPopup(cm, trackerIndex, dungeon.Color, dungeon.LabelChar, true, pos, (fun() -> popupIsActive <- false)) |> ignore
        )
    // redraw if PlayerHas changes
    dungeon.PlayerHasTriforceChanged.Add(fun _ -> redraw())
    // redraw if location changes
    dungeon.HasBeenLocatedChanged.Add(fun _ -> redraw())
    // redraw if hinting changes
    if not(TrackerModel.IsHiddenDungeonNumbers()) then
        TrackerModel.LevelHintChanged(trackerIndex).Add(fun _ -> redraw())
    else
        for i = 0 to 7 do TrackerModel.LevelHintChanged(i).Add(fun _ -> redraw())   // just redraw on any hints, rather than try to subscribe/unsubscribe based on LabelChar changes
    // redraw if label changed, as that can (un)link an existing hint
    dungeon.HiddenDungeonColorOrLabelChanged.Add(fun _ -> redraw())
    innerc


let redrawBoxes = ResizeArray()
TrackerModel.IsCurrentlyBookChanged.Add(fun _ ->
    TrackerModel.forceUpdate()
    for f in redrawBoxes do
        f()
    )
let MakeBoxItem(cm:CustomComboBoxes.CanvasManager, box:TrackerModel.Box) = 
    let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
    let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., Stroke=CustomComboBoxes.no, StrokeThickness=3.0)
    c.Children.Add(rect) |> ignore
    let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has item drawn on it, not the box
    c.Children.Add(innerc) |> ignore
    let redraw() =
        // redraw inner canvas
        innerc.Children.Clear()
        let bmp = CustomComboBoxes.boxCurrentBMP(box.CellCurrent(), false)
        if bmp <> null then
            canvasAdd(innerc, Graphics.BMPtoImage(bmp), 4., 4.)
        // redraw box outline
        match box.PlayerHas() with
        | TrackerModel.PlayerHas.YES -> rect.Stroke <- CustomComboBoxes.yes
        | TrackerModel.PlayerHas.NO -> rect.Stroke <- CustomComboBoxes.no
        | TrackerModel.PlayerHas.SKIPPED -> rect.Stroke <- CustomComboBoxes.skipped; CustomComboBoxes.placeSkippedItemXDecoration(innerc)
    redraw()
    // interactions
    let mutable popupIsActive = false
    let activateComboBox(activationDelta) =
        popupIsActive <- true
        let pos = c.TranslatePoint(Point(),cm.AppMainCanvas)
        CustomComboBoxes.DisplayItemComboBox(cm, pos.X, pos.Y, box.CellCurrent(), activationDelta, [], (fun (newBoxCellValue, newPlayerHas) ->
            box.Set(newBoxCellValue, newPlayerHas)
            popupIsActive <- false
            ), (fun () -> popupIsActive <- false))
    c.MouseDown.Add(fun ea ->
        if not popupIsActive then
            if ea.ButtonState = Input.MouseButtonState.Pressed &&
                    (ea.ChangedButton = Input.MouseButton.Left || ea.ChangedButton = Input.MouseButton.Middle || ea.ChangedButton = Input.MouseButton.Right) then
                if box.CellCurrent() = -1 then
                    activateComboBox(0)
                else
                    box.SetPlayerHas(CustomComboBoxes.MouseButtonEventArgsToPlayerHas ea)
        )
    c.MouseWheel.Add(fun x -> if not popupIsActive then activateComboBox(if x.Delta<0 then 1 else -1))
    // redraw on changes
    redrawBoxes.Add(fun() -> redraw())
    box.Changed.Add(fun _ -> redraw())
    c
