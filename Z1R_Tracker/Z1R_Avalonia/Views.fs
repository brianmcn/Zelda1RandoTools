module Views

open Avalonia.Controls
open Avalonia.Media
open Avalonia

let canvasAdd = Graphics.canvasAdd
(*
This module is for reusable display elements with the following properties:
 - they represent a display of some portion of the TrackerModel
 - they can redraw themselves by listening for changes to the TrackerModel, and never need to otherwise be redrawn, as their state is entirely TrackerModel-evented
 - they might optionally also have interactive/update abilities to change the model (which will, of course, be reflected back in their display view)
*)

let hintHighlightBrush = new LinearGradientBrush(StartPoint=RelativePoint(0.,0.,RelativeUnit.Relative),EndPoint=RelativePoint(1.,1.,RelativeUnit.Relative))
hintHighlightBrush.GradientStops.Add(new GradientStop(Colors.Yellow, 0.))
hintHighlightBrush.GradientStops.Add(new GradientStop(Colors.DarkGreen, 1.))
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

let MakeTriforceDisplayView(trackerIndex) =
    let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)
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
    let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=CustomComboBoxes.no, StrokeThickness=3.0)
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
    box.Changed.Add(fun _ -> redraw())
    let mutable popupIsActive = false
    let activateComboBox(activationDelta) =
        popupIsActive <- true
        let pos = c.TranslatePoint(Point(),cm.AppMainCanvas)
        CustomComboBoxes.DisplayItemComboBox(cm, pos.Value.X, pos.Value.Y, box.CellCurrent(), activationDelta, (fun (newBoxCellValue, newPlayerHas) ->
            box.Set(newBoxCellValue, newPlayerHas)
            popupIsActive <- false
            ), (fun () -> popupIsActive <- false))
    c.PointerPressed.Add(fun ea -> 
        if not popupIsActive then
            let pp = ea.GetCurrentPoint(c)
            if pp.Properties.IsLeftButtonPressed || pp.Properties.IsMiddleButtonPressed || pp.Properties.IsRightButtonPressed then 
                if box.CellCurrent() = -1 then
                    activateComboBox(0)
                else
                    box.SetPlayerHas(CustomComboBoxes.MouseButtonEventArgsToPlayerHas pp)
                    redraw()
        )
    // item
    c.PointerWheelChanged.Add(fun x -> if not popupIsActive then activateComboBox(if x.Delta.Y<0. then 1 else -1))
    redrawBoxes.Add(fun() -> redraw())
    redraw()
    c
