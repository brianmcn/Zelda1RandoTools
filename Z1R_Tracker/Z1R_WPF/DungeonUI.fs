﻿module DungeonUI

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let canvasAdd = Graphics.canvasAdd

////////////////////////

module AhhGlobalVariables =
    let mutable showShopLocatorInstanceFunc = fun(_item:int) -> ()
    let mutable hideLocator = fun() -> ()
    let mutable resetDungeonsForRouters = fun() -> ()

let TH = 24 // text height

open HotKeys.MyKey
module FloatHelper =
    type System.Double with
        member this.IsBetween(x,y) =
            this >= x && this <= y
open FloatHelper
open DungeonRoomState
open CustomComboBoxes.GlobalFlag

let unmarkRemarkBehavior(cm:CustomComboBoxes.CanvasManager, (_i,_j,hk), boxViewsAndActivations:(Canvas*((int*CustomComboBoxes.GridClickDismissalWarpReturn)->unit))[], dungeonIndex, returnPos) =
    let dungeon = TrackerModel.GetDungeon(dungeonIndex)
    let blockerLogic(maybe:TrackerModel.DungeonBlocker) =
        let full = maybe.HardCanonical()
        if not(full.PlayerCouldBeBlockedByThis()) then
            Graphics.ErrorBeepWithReminderLogText(sprintf "Tried to set blocker %s, but you already have item which unblocks it" (full.AsHotKeyName()))
        elif dungeonIndex = 8 then  
            Graphics.ErrorBeepWithReminderLogText("Blockers cannot be marked for dungeon 9")
        else
            // NOTE: this logic ignores Blockers' AppliesTo info
            let mutable firstFull, firstMaybe, firstEmpty = -1, -1, -1
            for bi = 0 to TrackerModel.DungeonBlockersContainer.MAX_BLOCKERS_PER_DUNGEON-1 do
                let b = TrackerModel.DungeonBlockersContainer.GetDungeonBlocker(dungeonIndex, bi)
                if b = full then 
                    if firstFull = -1 then firstFull <- bi
                elif b = maybe then
                    if firstMaybe = -1 then firstMaybe <- bi
                elif b = TrackerModel.DungeonBlocker.NOTHING then
                    if firstEmpty = -1 then firstEmpty <- bi
            if firstFull <> -1 then
                () // there is already a hard block marked, do nothing
            elif firstMaybe <> -1 then
                // change maybe to full
                TrackerModel.DungeonBlockersContainer.SetDungeonBlocker(dungeonIndex, firstMaybe, full)
            elif firstEmpty <> -1 then
                // mark a maybe
                TrackerModel.DungeonBlockersContainer.SetDungeonBlocker(dungeonIndex, firstEmpty, maybe)
            else
                // there was no prior blocker of this kind, but there's no empty box to mark one
                Graphics.ErrorBeepWithReminderLogText(sprintf "There are no empty blocker boxes to mark a %s in this dungeon" (full.AsHotKeyName()))
    let activateBox(boxIndex) =
        let view,activate = boxViewsAndActivations.[boxIndex]
        let pos = view.TranslatePoint(Point(15.,15.), cm.AppMainCanvas)
        Graphics.WarpMouseCursorTo(pos)
        activate(0, CustomComboBoxes.GridClickDismissalWarpReturn.WarpTo returnPos)
    let boxes = dungeon.Boxes
    let validBoxIndices =
        if TrackerModel.IsHiddenDungeonNumbers() then
            let lc = dungeon.LabelChar
            let twoItemDungeons = if TrackerModel.IsSecondQuestDungeons then "123567" else "234567"
            if twoItemDungeons.Contains(lc.ToString()) then
                [0..1]     // filter out ghostbustered third box
            else
                [0..boxes.Length-1]
        else
            [0..boxes.Length-1]
    match hk with
    | Some(Choice1Of4(RoomType.ItemBasement)) -> 
        // find the bottommost empty basement, if it exists, otherwise just choose bottommost non-ghostbustered box
        let idxs = validBoxIndices |> List.rev
        let mutable r = -1
        for i in idxs do
            if r = -1 && boxes.[i].CurrentlyHasBasementStair && boxes.[i].CellCurrent() = -1 then
                r <- i
        if r = -1 then
            r <- idxs.Head
        // r is now the index of the box we want
        activateBox(r)
    | Some(Choice1Of4(RoomType.Chevy)) 
    | Some(Choice1Of4(RoomType.CircleMoat)) 
    | Some(Choice1Of4(RoomType.DoubleMoat)) 
    | Some(Choice1Of4(RoomType.LavaMoat)) 
    | Some(Choice1Of4(RoomType.RightMoat)) 
    | Some(Choice1Of4(RoomType.TopMoat)) -> 
        blockerLogic(TrackerModel.DungeonBlocker.MAYBE_LADDER)
    | Some(Choice1Of4(RoomType.HungryGoriyaMeatBlock)) -> 
        blockerLogic(TrackerModel.DungeonBlocker.MAYBE_BAIT)
    | Some(Choice1Of4(RoomType.LifeOrMoney)) -> 
        blockerLogic(TrackerModel.DungeonBlocker.MAYBE_MONEY)

    | Some(Choice2Of4(MonsterDetail.Bow)) ->
        blockerLogic(TrackerModel.DungeonBlocker.MAYBE_BOW_AND_ARROW)
    | Some(Choice2Of4(MonsterDetail.Digdogger)) ->
        blockerLogic(TrackerModel.DungeonBlocker.MAYBE_RECORDER)
    | Some(Choice2Of4(MonsterDetail.Dodongo)) ->
        blockerLogic(TrackerModel.DungeonBlocker.MAYBE_BOMB)

    | Some(Choice3Of4(FloorDropDetail.Triforce)) ->
        dungeon.ToggleTriforce()
    | Some(Choice3Of4(FloorDropDetail.OtherKeyItem)) ->
        // find the topmost empty non-basement, if it exists, otherwise just choose topmost box
        let idxs = validBoxIndices
        let mutable r = -1
        for i in idxs do
            if r = -1 && not(boxes.[i].CurrentlyHasBasementStair) && boxes.[i].CellCurrent() = -1 then
                r <- i
        if r = -1 then
            r <- idxs.Head
        // r is now the index of the box we want
        activateBox(r)
    | Some(Choice3Of4(FloorDropDetail.Heart)) ->
        // if there is a playerHas=NO floor heart, then YES it...
        let mutable r = -1
        for i in validBoxIndices do
            if r = -1 && not(boxes.[i].CurrentlyHasBasementStair) && boxes.[i].CellCurrent() = TrackerModel.ITEMS.HEARTCONTAINER && boxes.[i].PlayerHas() = TrackerModel.PlayerHas.NO then
                r <- i
        if r <> -1 then
            boxes.[r].SetPlayerHas(TrackerModel.PlayerHas.YES)
        else
            // otherwise, if there is an empty non-basement box, fill it with a heart
            r <- -1
            for i in validBoxIndices do
                if r = -1 && not(boxes.[i].CurrentlyHasBasementStair) && boxes.[i].CellCurrent() = -1 then
                    r <- i
            if r <> -1 then
                if not(boxes.[r].AttemptToSet(TrackerModel.ITEMS.HEARTCONTAINER,TrackerModel.PlayerHas.YES)) then
                    Graphics.ErrorBeepWithReminderLogText("There are no Heart Containers remaining in the item pool; cannot mark another")
            else
                Graphics.ErrorBeepWithReminderLogText("There were no floor-drop item boxes remaining in this dungeon, to mark a floor-heart pickup")
    | Some(Choice3Of4(FloorDropDetail.Map)) ->
        // toggle has-map checkbox
        dungeon.PlayerHasMapOfThisDungeon <- not dungeon.PlayerHasMapOfThisDungeon
    | _ -> ()

let MakeSmallLocalTrackerPanel(dungeonIndex, ghostBuster) =
    let sp = new StackPanel(Orientation=Orientation.Vertical)
    // color/number canvas
    if TrackerModel.IsHiddenDungeonNumbers() then
        let colorCanvas = Views.MakeColorNumberCanvasForHDN(dungeonIndex)
        sp.Children.Add(colorCanvas) |> ignore
    else
        sp.Children.Add(new DockPanel(Height=28.)) |> ignore
    // triforce
    let dungeonView = if dungeonIndex < 8 then Views.MakeTriforceDisplayView(None, dungeonIndex, None) else Views.MakeLevel9View(None)
    sp.Children.Add(dungeonView) |> ignore
    // item boxes
    let d = TrackerModel.GetDungeon(dungeonIndex)
    for box in d.Boxes do
        let c = new Canvas(Width=30., Height=30.)
        let view,_activate = Views.MakeBoxItemWithExtraDecorations(None, box, false, None)
        canvasAdd(c, view, 0., 0.)
        sp.Children.Add(c) |> ignore
        if dungeonIndex <> 8 && TrackerModel.IsHiddenDungeonNumbers() && sp.Children.Count = 5 then
            let r = new Shapes.Rectangle(Width=30., Height=30., Fill=new VisualBrush(ghostBuster), IsHitTestVisible=false)
            canvasAdd(c, r, 0., 0.)
    sp

// the triforce and item inset
let MakeLocalTrackerPanel(cm:CustomComboBoxes.CanvasManager, pos:Point, sunglasses, level, ghostBuster, posDestinationWhenMoveCursorLeftF) =
    let dungeonIndex = level-1
    let yellow = Graphics.freeze(new SolidColorBrush(Color.FromArgb(byte(sunglasses*255.), Colors.Yellow.R, Colors.Yellow.G, Colors.Yellow.B)))
    let sp = new StackPanel(Orientation=Orientation.Vertical, Opacity=sunglasses)
    let BT = 3.
    let SPM = 3.
    let border = new Border(Child=sp, BorderThickness=Thickness(BT), BorderBrush=Brushes.DimGray, Background=Brushes.Black)
    let interiorHighlightCanvas = new Canvas()
    interiorHighlightCanvas.Children.Add(border) |> ignore
    let mutable y = 0.
    let AddCursorBehaviorTo(e:UIElement, canDown, canUp) =
        y <- y + 30.
        e.MyKeyAdd(fun ea ->
            match HotKeys.GlobalHotKeyProcessor.TryGetValue(ea.Key) with
            | Some(HotKeys.GlobalHotkeyTargets.MoveCursorDown) -> 
                ea.Handled <- true
                if canDown then
                    let pos = e.TranslatePoint(Point(Views.IDEAL_BOX_MOUSE_X,Views.IDEAL_BOX_MOUSE_Y+30.), cm.AppMainCanvas)
                    Graphics.NavigationallyWarpMouseCursorTo(pos)
            | Some(HotKeys.GlobalHotkeyTargets.MoveCursorUp) -> 
                ea.Handled <- true
                if canUp then
                    let pos = e.TranslatePoint(Point(Views.IDEAL_BOX_MOUSE_X,Views.IDEAL_BOX_MOUSE_Y-30.), cm.AppMainCanvas)
                    Graphics.NavigationallyWarpMouseCursorTo(pos)
            | Some(HotKeys.GlobalHotkeyTargets.MoveCursorLeft) -> 
                ea.Handled <- true
                Graphics.NavigationallyWarpMouseCursorTo(posDestinationWhenMoveCursorLeftF())
            | Some(HotKeys.GlobalHotkeyTargets.MoveCursorRight) -> 
                ea.Handled <- true   // just to prevent mouse escape (allow user to 'hammer right' in the dungeon area to get to this panel
            | _ -> ()
            )
    // draw triforce (or label if 9) and N boxes, populated as now
    // color/number canvas
    if TrackerModel.IsHiddenDungeonNumbers() then
        let colorCanvas = Views.MakeColorNumberCanvasForHDN(dungeonIndex)
        sp.Children.Add(colorCanvas) |> ignore
    // triforce
    let dungeonView = if dungeonIndex < 8 then Views.MakeTriforceDisplayView(Some(cm), dungeonIndex, None) else Views.MakeLevel9View(None)
    AddCursorBehaviorTo(dungeonView, true, false)
    sp.Children.Add(dungeonView) |> ignore
    // item boxes
    let d = TrackerModel.GetDungeon(dungeonIndex)
    let boxViewsAndActivations =
        [|
            for box in d.Boxes do
                let c = new Canvas(Width=30., Height=30.)
                let view,activate = Views.MakeBoxItem(cm, box)
                AddCursorBehaviorTo(view, not(obj.ReferenceEquals(box, d.Boxes.[d.Boxes.Length-1])), true)
                canvasAdd(c, view, 0., 0.)
                sp.Children.Add(c) |> ignore
                if level <> 9 && TrackerModel.IsHiddenDungeonNumbers() && sp.Children.Count = 5 then
                    let r = new Shapes.Rectangle(Width=30., Height=30., Fill=new VisualBrush(ghostBuster), IsHitTestVisible=false)
                    canvasAdd(c, r, 0., 0.)
                yield view,activate
        |]
    sp.Margin <- Thickness(SPM)
    // dynamic highlight
    let line,triangle = Graphics.makeArrow(30.*float dungeonIndex+15., 36.+float(d.Boxes.Length+1)*30., pos.X+21., pos.Y-3., yellow)
    let rect = new Shapes.Rectangle(Width=36., Height=6.+float(d.Boxes.Length+1+if TrackerModel.IsHiddenDungeonNumbers()then 1 else 0)*30., Stroke=yellow, StrokeThickness=3.)
    line.Opacity <- 0.
    triangle.Opacity <- 0.
    rect.Opacity <- 0.
    let c = new Canvas()
    canvasAdd(c, line, 0., 0.)
    canvasAdd(c, triangle, 0., 0.)
    canvasAdd(c, rect, 30.*float dungeonIndex-3., 27. - if TrackerModel.IsHiddenDungeonNumbers() then 30. else 0.)
    let highlight() =
        if c.Parent = null then
            canvasAdd(cm.AppMainCanvas, c, 0., 0.)
        line.Opacity <- 1.
        triangle.Opacity <- 1.
        rect.Opacity <- 1.
        border.BorderBrush <- yellow
    let unhighlight() = 
        cm.AppMainCanvas.Children.Remove(c)
        line.Opacity <- 0.
        triangle.Opacity <- 0.
        rect.Opacity <- 0.
        border.BorderBrush <- Brushes.DimGray
    sp.MouseEnter.Add(fun _ -> highlight())
    sp.MouseLeave.Add(fun _ -> unhighlight())
    interiorHighlightCanvas, unhighlight, (fun () -> dungeonView.TranslatePoint(Point(Views.IDEAL_BOX_MOUSE_X,Views.IDEAL_BOX_MOUSE_Y), cm.AppMainCanvas)), boxViewsAndActivations

let fortyFiveDegrees = 
    let r = new RotateTransform(45.)
    if r.CanFreeze then
        r.Freeze()
    r
let makeOutlineShapesImpl(quest:string[]) =
    let outlines = ResizeArray<FrameworkElement>()
    let color = Brushes.MediumPurple
    let hatchBrush = 
        let myGeometryGroup = new GeometryGroup()
        myGeometryGroup.Children.Add(new LineGeometry(new Point(0., 0.), new Point(9., 0.)))
        myGeometryGroup.Children.Add(new LineGeometry(new Point(0., 9.), new Point(0., 0.)))
        let p = new Windows.Media.Pen(Brush=color, Thickness=3., StartLineCap=PenLineCap.Square, EndLineCap=PenLineCap.Square)
        let myDrawing = new GeometryDrawing(null, p, myGeometryGroup)
        new DrawingBrush(Drawing=myDrawing, Viewbox=Rect(0., 0., 9., 9.), ViewboxUnits=BrushMappingMode.Absolute, Viewport=Rect(0., 0., 9., 9.), ViewportUnits=BrushMappingMode.Absolute, 
                          TileMode=TileMode.Tile, Stretch=Stretch.UniformToFill, Transform = fortyFiveDegrees)
    // fixed dungeon drawing outlines - vertical segments
    for i = 0 to 6 do
        for j = 0 to 7 do
            if quest.[j].Chars(i) <> quest.[j].Chars(i+1) then
                let s = new Shapes.Line(X1=float(i*(39+12)+39+12/2), Y1=float(TH+j*(27+12)-12/2), X2=float(i*(39+12)+39+12/2), Y2=float(TH+j*(27+12)+27+12/2), 
                                Stroke=color, StrokeThickness=3., IsHitTestVisible=false)
                outlines.Add(s)
    // fixed dungeon drawing outlines - horizontal segments
    for i = 0 to 7 do
        for j = 0 to 6 do
            if quest.[j].Chars(i) <> quest.[j+1].Chars(i) then
                let s = new Shapes.Line(X1=float(i*(39+12)-12/2), Y1=float(TH+(j+1)*(27+12)-12/2), X2=float(i*(39+12)+39+12/2), Y2=float(TH+(j+1)*(27+12)-12/2), 
                                Stroke=color, StrokeThickness=3., IsHitTestVisible=false)
                outlines.Add(s)
    // fixed dungeon drawing outlines - off-map (non-)rooms
    for i = 0 to 7 do
        for j = 0 to 7 do
            if quest.[j].Chars(i) <> 'X' then
                let s = new Shapes.Rectangle(Width=float(39+12), Height=float(27+12), Fill=hatchBrush, IsHitTestVisible=false, Opacity=0.45)
                Canvas.SetLeft(s, float(i*(39+12)-12/2))
                Canvas.SetTop(s, float(TH + j*(27+12)-12/2))
                outlines.Add(s)
    outlines
let makeFirstQuestOutlineShapes(dungeonNumber) = makeOutlineShapesImpl(DungeonData.firstQuest.[dungeonNumber])
let makeSecondQuestOutlineShapes(dungeonNumber) = makeOutlineShapesImpl(DungeonData.secondQuest.[dungeonNumber])

////////////////////////

let defaultRoom() = if TrackerModelOptions.DefaultRoomPreferNonDescriptToMaybePushBlock.Value then RoomType.NonDescript else RoomType.MaybePushBlock

type TrackerLocation =
    | OVERWORLD
    | DUNGEON

let mutable theDungeonTabControl = null : TabControl
let HFF = new FontFamily("Courier New")
let rainbowBrush = 
    let gsc = new GradientStopCollection()
    gsc.Add(new GradientStop(Colors.Red, 0.))
    gsc.Add(new GradientStop(Colors.Orange, 0.2))
    gsc.Add(new GradientStop(Colors.Yellow, 0.4))
    gsc.Add(new GradientStop(Colors.LightGreen, 0.6))
    gsc.Add(new GradientStop(Colors.LightBlue, 0.8))
    gsc.Add(new GradientStop(Colors.MediumPurple, 1.))
    new LinearGradientBrush(gsc, 90.)

let makeDungeonTabs(cm:CustomComboBoxes.CanvasManager, layoutF, posYF, selectDungeonTabEvent:Event<int>,
                    TH, rightwardCanvas:Canvas, levelTabSelected:Event<_>, blockersHoverEvent:Event<_>,
                    mainTrackerGhostbusters:Canvas[], showProgress, contentCanvasMouseEnterFunc, contentCanvasMouseLeaveFunc) = async {
    do! showProgress(sprintf "begin makeDungeonTabs")
    let dungeonTabsWholeCanvas = new Canvas(Height=float(2*TH + 3 + 27*8 + 12*7 + 3 + 6))  // need to set height, as caller uses it
    layoutF(dungeonTabsWholeCanvas)

    rightwardCanvas.Height <- dungeonTabsWholeCanvas.Height
    let outlineDrawingCanvases = Array.init 9 (fun _ -> new Canvas()) // where we draw non-shapes-dungeons overlays
    let currentOutlineDisplayState = Array.zeroCreate 9   // 0=nothing, 1-9 = FQ, 10-18 = SQ
    let doVanillaOutlineRedraw(canvasToRedraw:Canvas, state) =
        canvasToRedraw.Children.Clear() |> ignore
        if state>=1 && state<=9 then
            for s in makeFirstQuestOutlineShapes(state-1) do
                canvasToRedraw.Children.Add(s) |> ignore
        if state>=10 && state<=18 then
            for s in makeSecondQuestOutlineShapes(state-10) do
                canvasToRedraw.Children.Add(s) |> ignore
    let grabHelper = new Dungeon.GrabHelper()
    let grabModeTextBlock = 
        new Border(BorderThickness=Thickness(2.), BorderBrush=Brushes.LightGray, 
                    Child=new TextBlock(TextWrapping=TextWrapping.Wrap, FontSize=16., Foreground=Brushes.Black, Background=Brushes.Gray, IsHitTestVisible=false,
                                        Text="You are now in 'grab mode', which can be used to move an entire segment of dungeon rooms and doors at once.\n\nTo abort grab mode, click again on 'GRAB' in the upper right of the dungeon tracker.\n\nTo move a segment, first click any marked room, to pick up that room and all contiguous rooms.  Then click again on a new location to 'drop' the segment you grabbed.  After grabbing, hovering the mouse shows a preview of where you would drop.  This behaves like 'cut and paste', and adjacent doors will come along for the ride.\n\nUpon completion, you will be prompted to keep changes or undo them, so you can experiment.")
        )
    let dungeonTabs = new TabControl(FontSize=12., Background=Brushes.Black)
    theDungeonTabControl <- dungeonTabs
    let masterRoomStates = Array.init 9 (fun _ -> Array2D.init 8 8 (fun _ _ -> new DungeonRoomState()))
    let masterRedrawAllRooms = Array.init 9 (fun _ -> fun() -> ())

    
    // make the whole canvas
    canvasAdd(dungeonTabsWholeCanvas, dungeonTabs, 0., 0.) 

    let isHoveringFQSQ = new TrackerModel.EventingBool(false)

    if TrackerModel.IsHiddenDungeonNumbers() then
        let button = new Button(Content=new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, BorderThickness=Thickness(0.), 
                                                    Text="FQ/SQ", IsReadOnly=true, IsHitTestVisible=false),
                                        BorderThickness=Thickness(1.), Margin=Thickness(0.), Padding=Thickness(0.))
        canvasAdd(dungeonTabsWholeCanvas, button, 405., 0.)
        
        let mkTxt(txt,color) =
            new TextBox(Width=50., Height=30., FontSize=15., Foreground=color, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                        BorderThickness=Thickness(0.), Text=txt, VerticalContentAlignment=VerticalAlignment.Center, HorizontalContentAlignment=HorizontalAlignment.Center,
                        VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center)

        button.MouseEnter.Add(fun _ -> isHoveringFQSQ.Value <- true)
        button.MouseLeave.Add(fun _ -> isHoveringFQSQ.Value <- false)
        button.Click.Add(fun _ ->
            if not popupIsActive && not(dungeonTabs.SelectedIndex=9) then  // no behavior on summary tab
                popupIsActive <- true
                
                let ST = CustomComboBoxes.borderThickness
                let SI = dungeonTabs.SelectedIndex
                let roomStates = masterRoomStates.[SI]
                let isCompatible(q,l) =
                    let quest = if q=1 then DungeonData.firstQuest else DungeonData.secondQuest
                    let mutable ok = true
                    for x = 0 to 7 do
                        for y = 0 to 7 do
                            if not(roomStates.[x,y].IsEmpty) && quest.[l-1].[y].Chars(x)<>'X' then
                                ok <- false
                    ok
                let chooseColor(q,l) =
                    let compat = isCompatible(q,l)
                    if compat then
                        let mutable anotherHasUsed = false
                        for i = 0 to 8 do
                            if i <> dungeonTabs.SelectedIndex then
                                if TrackerModel.GetDungeon(i).LabelChar = l.ToString().Chars(0) then
                                    anotherHasUsed <- true
                                let usedState = currentOutlineDisplayState.[i]
                                let usedLevel = if usedState > 9 then usedState-9 else usedState
                                if usedLevel = l then
                                    anotherHasUsed <- true
                        if anotherHasUsed then Brushes.Yellow else Brushes.Lime
                    else
                        Brushes.Red
                let pos = outlineDrawingCanvases.[SI].TranslatePoint(Point(), cm.AppMainCanvas)
                let tileCanvas = new Canvas(Width=float(39*8 + 12*7), Height=float(TH + 27*8 + 12*7))
                let gridElementsSelectablesAndIDs = [|
                    yield (upcast mkTxt("none",Brushes.Lime):FrameworkElement), true, 0
                    for l = 1 to 9 do
                        yield (upcast mkTxt(sprintf "1Q%d" l, chooseColor(1,l)):FrameworkElement), true, l
                    yield (upcast mkTxt("none",Brushes.Lime):FrameworkElement), true, 0    // two 'none's just to make the grid look nicer
                    for l = 1 to 9 do
                        yield (upcast mkTxt(sprintf "2Q%d" l, chooseColor(2,l)):FrameworkElement), true, l+9
                    |]
                let originalStateIndex = 
                    if currentOutlineDisplayState.[SI] >= 10 then 
                        1+currentOutlineDisplayState.[SI]   // skip over the extra 'none'
                    else 
                        currentOutlineDisplayState.[SI]
                let activationDelta = 0
                let (gnc, gnr, gcw, grh) = (5, 4, 50, 30)
                let gx, gy = tileCanvas.Width + ST, 0.
                let redrawTile(state) = 
                    doVanillaOutlineRedraw(tileCanvas, state)
                let onClick(_ea, state) = CustomComboBoxes.DismissPopupWithResult(state)
                let extraDecorations = [|
                    (upcast new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Child=
                        new TextBox(FontSize=15., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), Margin=Thickness(3.),
                                    Text="Choose a vanilla dungeon outline to draw\non this dungeon map tab.\n\n"+
                                            "Green selections are compatible with your\ncurrently marked rooms. Yellow selections\n"+
                                            "are compatible, but indicate a dungeon\nnumber already in use by another tab.\n\n"+
                                            "Choose 'none' to remove outline.")
                        ):FrameworkElement), float gx, float gnr*(2.*ST+float grh)+2.*ST
                    |]
                let brushes = CustomComboBoxes.ModalGridSelectBrushes.Defaults()
                outlineDrawingCanvases.[SI].Children.Clear()  // remove current outline; the tileCanvas is transparent, and seeing the old one is bad. restored later
                async {
                    let! r = CustomComboBoxes.DoModalGridSelect(cm, pos.X, pos.Y, tileCanvas, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (gnc, gnr, gcw, grh),
                                    float gcw/2., float grh/2., gx, gy, redrawTile, onClick, extraDecorations, brushes, CustomComboBoxes.NoWarp, None, "VanillaDungeonOutline", None)
                    match r with
                    | Some(state) -> 
                        currentOutlineDisplayState.[SI] <- state
                        doVanillaOutlineRedraw(outlineDrawingCanvases.[SI], currentOutlineDisplayState.[SI])
                        if TrackerModel.GetDungeon(SI).LabelChar = '?' then
                            if state >= 1 && state <= 8 then
                                TrackerModel.GetDungeon(SI).LabelChar <- char(int '0' + state)
                            if state >= 10 && state <= 17 then
                                TrackerModel.GetDungeon(SI).LabelChar <- char(int '1' + state - 10)
                        let mutable appearsToBe2qFrontHalf = false
                        for i = 0 to 7 do
                            if currentOutlineDisplayState.[i] = 10 || currentOutlineDisplayState.[i] = 13 then   // 2Q1 or 2Q4
                                appearsToBe2qFrontHalf <- true
                        TrackerModel.IsSecondQuestDungeons <- appearsToBe2qFrontHalf
                        OptionsMenu.secondQuestDungeonsOptionChanged.Trigger()
                    | None -> ()
                    popupIsActive <- false
                    } |> Async.StartImmediate
            )
    else
        let fqcb = new Button(Content=new TextBox(Text="FQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false))
        fqcb.ToolTip <- "Show vanilla first quest dungeon outlines"
        let sqcb = new Button(Content=new TextBox(Text="SQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false))
        sqcb.ToolTip <- "Show vanilla second quest dungeon outlines"
        for button in [fqcb; sqcb] do
            button.MouseEnter.Add(fun _ -> isHoveringFQSQ.Value <- true)
            button.MouseLeave.Add(fun _ -> isHoveringFQSQ.Value <- false)
        let turnOnQuestMap(SI, delta) =
            currentOutlineDisplayState.[SI] <- SI+delta
            doVanillaOutlineRedraw(outlineDrawingCanvases.[SI], currentOutlineDisplayState.[SI])
        let turnOffQuestMap(SI) =
            outlineDrawingCanvases.[SI].Children.Clear()
            currentOutlineDisplayState.[SI] <- 0
        let pressQuestButton(SI, isfq) =
            let delta = if isfq then 1 else 10
            if currentOutlineDisplayState.[SI]<>SI+delta then turnOnQuestMap(SI, delta)
            else turnOffQuestMap(SI)
        fqcb.Click.Add(fun _ -> 
            let SI = dungeonTabs.SelectedIndex
            if SI=9 then                                        // pressing FQ on summary tab turns them all on or all off
                if currentOutlineDisplayState.[0]<>1 then
                    for d = 0 to 8 do turnOnQuestMap(d, 1)
                else
                    for d = 0 to 8 do turnOffQuestMap(d)
            else
                if SI<6 then    // update front half
                    for i=0 to 5 do
                        pressQuestButton(i, true)
                else            // update back half
                    for i=6 to 8 do
                        pressQuestButton(i, true)
            )
        canvasAdd(dungeonTabsWholeCanvas, fqcb, 402., 0.) 
        sqcb.Click.Add(fun _ -> 
            let SI = dungeonTabs.SelectedIndex
            if SI=9 then                                        // pressing SQ on summary tab turns them all on or all off
                if currentOutlineDisplayState.[0]<>10 then
                    for d = 0 to 8 do turnOnQuestMap(d, 10)
                else
                    for d = 0 to 8 do turnOffQuestMap(d)
            else
                if SI<6 then    // update front half
                    for i=0 to 5 do
                        pressQuestButton(i, false)
                else            // update back half
                    for i=6 to 8 do
                        pressQuestButton(i, false)
            )
        canvasAdd(dungeonTabsWholeCanvas, sqcb, 426., 0.) 

    let levelTabs = Array.zeroCreate 9
    let contentCanvasesWithBadYOffsets = Array.zeroCreate 9
    let contentCanvases = Array.zeroCreate 9
    let dummyCanvas = new Canvas(Opacity=0.0001, IsHitTestVisible=false)  // a kludge to help work around TabControl unloading tabs when not selected
    let localDungeonTrackerPanelWidth = 42.
    let exportFunctions = Array.create 9 (fun () -> new DungeonSaveAndLoad.DungeonModel())
    let importFunctions = Array.create 9 (fun _ -> ())
    let isFirstTimeClickingAnyRoom = Array.init 9 (fun _ -> new TrackerModel.EventingBool(true))
    let makeNumeral(labelChar) =
        new TextBox(Foreground=Brushes.Magenta, Background=Brushes.Transparent, Text=sprintf "%c" labelChar, IsReadOnly=true, IsHitTestVisible=false, FontSize=200., Opacity=0.25,
                        Height=float(27*8 + 12*7), Width=float(39*8 + 12*7), VerticalAlignment=VerticalAlignment.Center, FontWeight=FontWeights.Bold,
                        HorizontalContentAlignment=HorizontalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), Padding=Thickness(0.))
    let getLabelChar(level) = if level = 9 then '9' else if TrackerModel.IsHiddenDungeonNumbers() then (char(int 'A' - 1 + level)) else (char(int '0' + level))
    let mutable justUnmarked = None
    for level = 1 to 9 do
        let levelTab = new TabItem(Background=Brushes.Black, Foreground=Brushes.Black)
        dungeonTabs.Items.Add(levelTab) |> ignore
        levelTabs.[level-1] <- levelTab
        let labelChar = getLabelChar(level)
        let header = new TextBox(Width=13., Background=Brushes.Black, Foreground=Brushes.White, Text=sprintf "%c" labelChar, IsReadOnly=true, IsHitTestVisible=false, 
                                    HorizontalContentAlignment=HorizontalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), Padding=Thickness(0.))
        let baitMeatImage = Graphics.bait_bmp |> Graphics.BMPtoImage
        let baitMeatCheckmarkHeaderCanvas = new Canvas(Opacity=0.)
        let bombUpgradeMarkHeaderCanvas = new Canvas(Width=5., Height=5., Background=Brushes.DodgerBlue, Opacity=0.)
        let oldManUnreadHeaderCanvas = new Canvas(Width=5., Height=5., Background=Brushes.Red, Opacity=0.)
        do
            // bait meat
            let baitMeatHeaderCanvas = new Canvas(Width=8., Height=16., ClipToBounds=true)
            baitMeatImage.Opacity <- 0.
            baitMeatImage.Stretch <- Stretch.Uniform
            baitMeatImage.Height <- 14.
            canvasAdd(baitMeatHeaderCanvas, baitMeatImage, -6., 1.)
            // checkmark over bait meat
            let line1 = new Shapes.Line(X1=1., Y1=10., X2=4., Y2=13., Stroke=Brushes.Lime, StrokeThickness=2.5)
            baitMeatCheckmarkHeaderCanvas.Children.Add(line1) |> ignore
            let line2 = new Shapes.Line(X1=4., Y1=13., X2=8., Y2=4., Stroke=Brushes.Lime, StrokeThickness=2.5)
            baitMeatCheckmarkHeaderCanvas.Children.Add(line2) |> ignore
            canvasAdd(baitMeatHeaderCanvas, baitMeatCheckmarkHeaderCanvas, 0., 0.)
            // BU/OM dots
            let headerSp = new StackPanel(Orientation=Orientation.Horizontal, Background=Brushes.Black, Width=28.)
            headerSp.Children.Add(baitMeatHeaderCanvas) |> ignore
            headerSp.Children.Add(header) |> ignore
            let headerInfo = new StackPanel(Orientation=Orientation.Vertical, Width=7.)
            headerInfo.Children.Add(new Canvas(Width=3., Height=2.)) |> ignore
            headerInfo.Children.Add(bombUpgradeMarkHeaderCanvas) |> ignore
            headerInfo.Children.Add(new Canvas(Width=3., Height=2.)) |> ignore
            headerInfo.Children.Add(oldManUnreadHeaderCanvas) |> ignore
            headerSp.Children.Add(headerInfo) |> ignore
            headerSp.MouseEnter.Add(fun _ -> contentCanvasMouseEnterFunc(level))
            headerSp.MouseLeave.Add(fun _ -> contentCanvasMouseLeaveFunc(level))
            levelTab.Header <- headerSp
        TrackerModel.GetDungeon(level-1).HiddenDungeonColorOrLabelChanged.Add(fun (color,_) -> 
            header.Background <- Graphics.freeze(new SolidColorBrush(Graphics.makeColor(color)))
            header.Foreground <- if Graphics.isBlackGoodContrast(color) then Brushes.Black else Brushes.White
            )
        let tileSunglasses = 0.75
        let contentCanvasWithBadYOffset = new Canvas(Height=float(TH + 3 + 27*8 + 12*7 + 3), Width=float(3 + 39*8 + 12*7 + 3)+localDungeonTrackerPanelWidth, Background=Brushes.Black)
        levelTab.Content <- contentCanvasWithBadYOffset
        let contentCanvas = new Canvas(Width=contentCanvasWithBadYOffset.Width, Height=contentCanvasWithBadYOffset.Height, Background=Brushes.Black)
        contentCanvas.ClipToBounds <- true  // TODO does this have any bad side effects? useful so that 'midi' works properly, but suggests something was drawing off to the right
        if false then // we don't do this now; a SelectionChanged will do it later during startup, and doing it twice throws an exception
            canvasAdd(contentCanvasWithBadYOffset, contentCanvas, 0., 1.)   // so that all content is at multiples of 3, so that 2/3 view does not look blurry
        // rupee/blank/key/bomb row highlighter
        let highlightRow =
            let bmp = Dungeon.MakeLoZMinimapDisplayBmp(Array2D.zeroCreate 8 8, '?') 
            let rupeeKeyBomb = new System.Drawing.Bitmap(8, 32)
            for i = 0 to 7 do
                for j = 0 to 31 do
                    rupeeKeyBomb.SetPixel(i, j, bmp.GetPixel(72+i, 8+j))
            let i = Graphics.BMPtoImage rupeeKeyBomb
            i.Width <- 16.
            i.Height <- 64.
            i.Stretch <- Stretch.UniformToFill
            RenderOptions.SetBitmapScalingMode(i, BitmapScalingMode.NearestNeighbor)
            canvasAdd(contentCanvas, i, contentCanvas.Width-20., 3.)
            let rowHighlightGrid = Graphics.makeGrid(1,8,20,8)
            let rowHighlighter = new Canvas(Width=16., Height=8., Background=Brushes.Gray, Opacity=0.)
            Graphics.gridAdd(rowHighlightGrid, rowHighlighter, 0, 0)
            canvasAdd(contentCanvas, rowHighlightGrid, contentCanvas.Width-40., 3.)
            let highlightRow(rowOpt) =
                match rowOpt with
                | None -> rowHighlighter.Opacity <- 0.
                | Some r -> rowHighlighter.Opacity <- 1.0; Grid.SetRow(rowHighlighter, r)
            highlightRow
        let mutable oldManCount = 0
        let oldManCountTB = new TextBox(IsHitTestVisible=false, IsReadOnly=true, BorderThickness=Thickness(0.), FontSize=16., Margin=Thickness(0.), Width=45.,
                                        HorizontalContentAlignment=HorizontalAlignment.Left, Foreground=Brushes.Orange, Background=Brushes.Transparent, Focusable=false)
        let oldManBorder = new Canvas(Width=44., Height=16., Background=Brushes.Black)
        canvasAdd(oldManBorder, Graphics.BMPtoImage Graphics.old_man_bmp, 2., 0.)
        canvasAdd(oldManBorder, oldManCountTB, 18., -3.)
        oldManBorder.ToolTip <- "'Old Man Count' - the number of 'old men'\n(NPC-with-hint/Bomb-Upgrade/Hungry-Goriya/\nLife-or-Money) rooms you have marked, and\nthe total number expected in this dungeon"
        ToolTipService.SetShowDuration(oldManBorder, 8000)
        let updateOldManCountText() = 
            if TrackerModel.IsHiddenDungeonNumbers() then
                if TrackerModel.GetDungeon(level-1).LabelChar <> '?' then
                    let i = int(TrackerModel.GetDungeon(level-1).LabelChar) - int('1')
                    oldManCountTB.Text <- sprintf "%d/%d" oldManCount (TrackerModel.GetOldManCount(i))
                else
                    oldManCountTB.Text <- sprintf "%d" oldManCount
            else
                oldManCountTB.Text <- sprintf "%d/%d" oldManCount (TrackerModel.GetOldManCount(level-1))
        updateOldManCountText()
        OptionsMenu.secondQuestDungeonsOptionChanged.Publish.Add(fun _ -> updateOldManCountText())
        if TrackerModel.IsHiddenDungeonNumbers() then
            TrackerModel.GetDungeon(level-1).HiddenDungeonColorOrLabelChanged.Add(fun _ -> updateOldManCountText())
        canvasAdd(contentCanvas, oldManBorder, contentCanvas.Width-44., 69.)
        // local dungeon tracker
        let LD_X, LD_Y = contentCanvas.Width-localDungeonTrackerPanelWidth, 90.
        let pos = Point(0. + LD_X, posYF() + LD_Y)  // appMainCanvas coords where the local tracker panel will be placed
        let mutable localDungeonTrackerPanel = null
        let mutable localDungeonTrackerPanelPosToCursorRightToF = fun() -> Point()
        let mutable boxViewsAndActivations = [||]
        do
            let PopulateLocalDungeonTrackerPanel() =
                let posDestinationWhenMoveCursorLeft() = contentCanvas.TranslatePoint(Point(3. + 39.*7.5 + 12.*7.,float(TH)+ 3. + 27.*4.5 + 12.*4.), cm.AppMainCanvas)
                let ldtp,unhighlight,posIn,bvaa = 
                    MakeLocalTrackerPanel(cm, pos, tileSunglasses, level, (if level=9 then null else mainTrackerGhostbusters.[level-1]), posDestinationWhenMoveCursorLeft)
                boxViewsAndActivations <- bvaa
                localDungeonTrackerPanelPosToCursorRightToF <- posIn
                if localDungeonTrackerPanel<> null then
                    contentCanvas.Children.Remove(localDungeonTrackerPanel)  // remove old one
                localDungeonTrackerPanel <- ldtp
                canvasAdd(contentCanvas, localDungeonTrackerPanel, LD_X, LD_Y) // add new one
                // don't remove the old 'unhighlight' event listener - this is technically a leak, but unless you toggle '2nd quest dungeons' button a million times in one session, it won't matter
                dungeonTabs.SelectionChanged.Add(fun _ -> unhighlight())  // an extra safeguard, since the highlight is not a popup, but just a crazy mark over the main canvas
            OptionsMenu.secondQuestDungeonsOptionChanged.Publish.Add(fun _ -> PopulateLocalDungeonTrackerPanel())
            PopulateLocalDungeonTrackerPanel()
        // main dungeon content
        contentCanvas.MouseEnter.Add(fun _ -> contentCanvasMouseEnterFunc(level))
        contentCanvas.MouseLeave.Add(fun ea -> 
            // only hide the locator if we "actually" left
            // MouseLeave fires
            //  - when we click a room, redraw() causes the element we are Entered to be removed from the tree and replaced by another, even though we never left the bounds of the enclosing canvas
            //  - when we right-click a room, as the mouse is warped outside the canvas in the popup
            // avoid spuriously changing the overworld locator (which is expensive and visually distracting) in these cases
            let pos = ea.GetPosition(contentCanvas)
            if (pos.X < 0. || pos.X > contentCanvas.ActualWidth) || (pos.Y < 0. || pos.Y > contentCanvas.ActualHeight) then
                if cm.PopupCanvasStack.Count = 0 then
                    contentCanvasMouseLeaveFunc(level)
            )
        let dungeonCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7), Background=Brushes.Black)  
        let dungeonHeaderCanvas = new Canvas(Height=float(TH), Width=float(39*8 + 12*7))         // draw e.g. BOARD-5 here
        dungeonHeaderCanvas.ClipToBounds <- true
        canvasAdd(dungeonCanvas, dungeonHeaderCanvas, 0., 0.)
        let dungeonBodyCanvas = new Canvas(Height=float(27*8 + 12*7), Width=float(39*8 + 12*7), Background=Brushes.Black)  // draw e.g. rooms here; has background color to be a surface to see all mouse interactions
        // Clip: allow e.g. mouse highlight box slightly off left edge, or gleeok head slightly off top, but don't allow e.g. OFF to go way over right edge into side panel etc.
        dungeonBodyCanvas.Clip <- new RectangleGeometry(Rect(-3., -3., dungeonBodyCanvas.Width+6., dungeonBodyCanvas.Height+6.))  
        canvasAdd(dungeonCanvas, dungeonBodyCanvas, 0., float TH)
        let dungeonBodyHighlightCanvas = new Canvas(Height=float(27*8 + 12*7), Width=float(39*8 + 12*7))  // draw e.g. blocker highlights here
        dungeonBodyHighlightCanvas.ClipToBounds <- true
        canvasAdd(dungeonCanvas, dungeonBodyHighlightCanvas, 0., float TH)
        let numeral = makeNumeral(labelChar)
        let showNumeral() =
            numeral.Opacity <- 0.7
            let ctxt = Threading.SynchronizationContext.Current
            async { 
                do! Async.Sleep(1000)
                do! Async.SwitchToContext(ctxt)
                numeral.Opacity <- if isFirstTimeClickingAnyRoom.[level-1].Value then 0.25 else 0.0 
            } |> Async.Start
        let dungeonSourceHighlightCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7))  // draw grab-source highlights here
        let dungeonHighlightCanvas = new Canvas(Height=float(TH + 27*8 + 12*7), Width=float(39*8 + 12*7))  // draw grab highlights here
        canvasAdd(contentCanvas, dungeonCanvas, 3., 3.)
        canvasAdd(contentCanvas, dungeonSourceHighlightCanvas, 3., 3.)
        canvasAdd(contentCanvas, dungeonHighlightCanvas, 3., 3.)

        contentCanvasesWithBadYOffsets.[level-1] <- contentCanvasWithBadYOffset
        contentCanvases.[level-1] <- contentCanvas
        if level = 1 then // just set this once
            dungeonTabs.Height <- contentCanvas.Height + 30.

        // doors, rooms, and dragging prep
        let mutable skipRedrawInsideCurrentImport = false
        let redrawAllDoorFuncs = ResizeArray()
        let redrawAllDoors() = if not skipRedrawInsideCurrentImport then for f in redrawAllDoorFuncs do f()
        let roomRedrawFuncs = ResizeArray(64)
        let redrawAllRooms() = if not skipRedrawInsideCurrentImport then for f in roomRedrawFuncs do f()
        let roomCanvas = new Canvas()
        let centerOf(x,y) = // center of room at x,y; but can be floats so e.g. x+0.5,y is an adjacent door
            roomCanvas.TranslatePoint(Point(x*51.+13.*3./2.,y*39.+9.*3./2.), cm.AppMainCanvas)
        let mutable showMinimaps = fun () -> ()
        let mutable invertOffTheMapWithUnmarked = fun () -> ()
        let mutable hasEverInvertedOrDragged = false
        let roomDragDrop = new Graphics.DragDropSurface<_>(dungeonBodyCanvas, (fun (ea,initiatorFunc) ->
            let mutable whichButtonStr = ""
            if not popupIsActive then
                // drag and drop to quickly 'paint' rooms
                if not grabHelper.IsGrabMode then  // cannot initiate a drag in grab mode
                    if ea.LeftButton = System.Windows.Input.MouseButtonState.Pressed then
                        if Input.Keyboard.IsKeyDown(Input.Key.LeftShift) || Input.Keyboard.IsKeyDown(Input.Key.RightShift) then
                            whichButtonStr <- "M"  // shift-left-click behaves like middle-click
                        else
                            whichButtonStr <- "L"
                    elif ea.RightButton = System.Windows.Input.MouseButtonState.Pressed then
                        if Input.Keyboard.IsKeyDown(Input.Key.LeftShift) || Input.Keyboard.IsKeyDown(Input.Key.RightShift) then
                            ()  // shift-right-click is not an action
                        else
                            whichButtonStr <- "R"
                    elif ea.MiddleButton = System.Windows.Input.MouseButtonState.Pressed then
                        whichButtonStr <- "M"
            if whichButtonStr <> "" then
                if whichButtonStr="L" && not(hasEverInvertedOrDragged) && TrackerModelOptions.LeftClickDragAutoInverts.Value then
                    invertOffTheMapWithUnmarked()  // left-drag will auto-invert, to start painting on-map locations to an off-the-map background
                hasEverInvertedOrDragged <- true // you might right drag to unpaint, and unpaint too much, and then left drag to paint back in, don't want to invert then
                initiatorFunc(whichButtonStr)
                if whichButtonStr="R" || whichButtonStr="L" then
                    // make OffTheMap rooms show a 'grid' to make it easier to draw in empty space
                    isDoingDragPaintOffTheMap <- true
                    redrawAllRooms()
                showMinimaps()
                DragDrop.DoDragDrop(roomCanvas, whichButtonStr, DragDropEffects.Link) |> ignore
                // DoDragDrop is blocking, so we can do post-drop effects here
                isDoingDragPaintOffTheMap <- false
                redrawAllRooms()
                redrawAllDoors()
                rightwardCanvas.Children.Clear() // remove the shown minimaps
            ))
        // some room stuff that doors need to know about
        let highlight = Dungeon.highlight
        let roomHighlightOutline = new Shapes.Rectangle(Width=float(13*3)+4., Height=float(9*3)+4., Stroke=highlight, StrokeThickness=1.5, Fill=Brushes.Transparent, IsHitTestVisible=false, Opacity=0.)
        // doors
        let LL, RR, UU, DD = HotKeys.GlobalHotkeyTargets.MoveCursorLeft, HotKeys.GlobalHotkeyTargets.MoveCursorRight, 
                                HotKeys.GlobalHotkeyTargets.MoveCursorUp, HotKeys.GlobalHotkeyTargets.MoveCursorDown
        let installDoorBehavior(door:Dungeon.Door, doorCanvas:Canvas, (ai,aj,adir), (bi,bj,_bdir)) =
            if adir <> RR && adir <> DD then
                failwith "must be called with RR,LL or DD,UU in that order"
            roomDragDrop.RegisterClickable(doorCanvas, (fun ea -> 
                if not grabHelper.IsGrabMode then  // cannot interact with doors in grab mode
                    if ea.ChangedButton = Input.MouseButton.Left then
                        if Input.Keyboard.IsKeyDown(Input.Key.LeftShift) || Input.Keyboard.IsKeyDown(Input.Key.RightShift) then
                            door.Prev()
                        else
                            if door.State <> Dungeon.DoorState.YES then
                                door.State <- Dungeon.DoorState.YES
                            else
                                door.State <- Dungeon.DoorState.UNKNOWN
                    elif ea.ChangedButton = Input.MouseButton.Right then
                        if Input.Keyboard.IsKeyDown(Input.Key.LeftShift) || Input.Keyboard.IsKeyDown(Input.Key.RightShift) then
                            door.Next()
                        else
                            if door.State <> Dungeon.DoorState.NO then
                                door.State <- Dungeon.DoorState.NO
                            else
                                door.State <- Dungeon.DoorState.UNKNOWN
                    elif ea.ChangedButton = Input.MouseButton.Middle then
                        if door.State <> Dungeon.DoorState.YELLOW then
                            door.State <- Dungeon.DoorState.YELLOW
                        else
                            door.State <- Dungeon.DoorState.UNKNOWN
                    ea.Handled <- true
                ), (fun _ -> ()))
            doorCanvas.MouseWheel.Add(fun ea ->
                if not grabHelper.IsGrabMode then  // cannot interact with doors in grab mode
                    if ea.Delta<0 then door.Next() else door.Prev()
                )
            doorCanvas.MyKeyAdd(fun ea ->
                // two of the arrow keys should still work
                match HotKeys.GlobalHotKeyProcessor.TryGetValue(ea.Key) with
                | Some(HotKeys.GlobalHotkeyTargets.MoveCursorRight) -> 
                    ea.Handled <- true
                    if adir = RR then
                        Graphics.NavigationallyWarpMouseCursorTo(centerOf(float bi, float bj))
                | Some(HotKeys.GlobalHotkeyTargets.MoveCursorLeft) -> 
                    ea.Handled <- true
                    if adir = RR then
                        Graphics.NavigationallyWarpMouseCursorTo(centerOf(float ai, float aj))
                | Some(HotKeys.GlobalHotkeyTargets.MoveCursorDown) -> 
                    ea.Handled <- true
                    if adir = DD then
                        Graphics.NavigationallyWarpMouseCursorTo(centerOf(float bi, float bj))
                | Some(HotKeys.GlobalHotkeyTargets.MoveCursorUp) -> 
                    ea.Handled <- true
                    if adir = DD then
                        Graphics.NavigationallyWarpMouseCursorTo(centerOf(float ai, float aj))
                | _ -> ()
                )
        roomDragDrop.RegisterClickable(dungeonBodyCanvas, (fun _ -> ()), (fun _ -> ()))  // you can start a drag from the empty space between doors/rooms on the canvas
        // horizontal doors
        let unknown = Dungeon.unknown
        let no = Dungeon.no
        let yes = Dungeon.yes
        let yellow = Dungeon.yellow
        let purple = Dungeon.purple
        let horizontalDoors = Array2D.zeroCreate 7 8
        let hDoorHighlightOutline = new Shapes.Rectangle(Width=12., Height=16., Stroke=highlight, StrokeThickness=2., Fill=Brushes.Transparent, IsHitTestVisible=false, Opacity=0.)
        let hDoorCanvas = new Canvas()  // nesting canvases improves perf
        dungeonBodyCanvas.Children.Add(hDoorCanvas) |> ignore
        for i = 0 to 6 do
            for j = 0 to 7 do
                let d = new Canvas(Width=12., Height=16., Background=Brushes.Black)
                let rect = new Shapes.Rectangle(Width=12., Height=15., Stroke=unknown, StrokeThickness=2., Fill=unknown)
                let line = new Shapes.Line(X1 = 6., Y1 = -12., X2 = 6., Y2 = 28., StrokeThickness=3., Stroke=no, Opacity=0.)
                d.Children.Add(rect) |> ignore
                let door = new Dungeon.Door(Dungeon.DoorState.UNKNOWN, (function 
                    | Dungeon.DoorState.YES        -> rect.Stroke <- yes; rect.Fill <- yes; rect.Opacity <- 1.; line.Opacity <- 0.
                    | Dungeon.DoorState.NO         -> rect.Opacity <- 0.; line.Opacity <- 1.; if line.Parent = null then d.Children.Add(line) |> ignore  // only add lines to tree on-demand
                    | Dungeon.DoorState.YELLOW     -> rect.Stroke <- yellow; rect.Fill <- yellow; rect.Opacity <- 1.; line.Opacity <- 0.
                    | Dungeon.DoorState.PURPLE     -> rect.Stroke <- purple; rect.Fill <- purple; rect.Opacity <- 1.; line.Opacity <- 0.
                    | Dungeon.DoorState.UNKNOWN    -> 
                        rect.Stroke <- unknown; rect.Fill <- unknown; rect.Opacity <- 1.; line.Opacity <- 0.
                        if masterRoomStates.[level-1].[i,j].RoomType.IsOffMap || masterRoomStates.[level-1].[i+1,j].RoomType.IsOffMap then
                            rect.Stroke <- Brushes.Black
                            rect.Fill <- Brushes.Black
                        ))
                horizontalDoors.[i,j] <- door
                canvasAdd(hDoorCanvas, d, float(i*(39+12)+39), float(j*(27+12)+6))
                installDoorBehavior(door, d, (i,j,RR), (i+1,j,LL))
                d.MouseEnter.Add(fun _ -> if not popupIsActive && not grabHelper.IsGrabMode then 
                                                Canvas.SetLeft(hDoorHighlightOutline, float(i*(39+12)+39))
                                                Canvas.SetTop(hDoorHighlightOutline, float(j*(27+12)+6))
                                                hDoorHighlightOutline.Opacity <- Dungeon.highlightOpacity)
                d.MouseLeave.Add(fun _ -> hDoorHighlightOutline.Opacity <- 0.0)
                redrawAllDoorFuncs.Add(fun () -> door.Redraw())
        canvasAdd(dungeonBodyCanvas, hDoorHighlightOutline, 0., 0.)
        // vertical doors
        let vDoorHighlightOutline = new Shapes.Rectangle(Width=24., Height=12., Stroke=highlight, StrokeThickness=2., Fill=Brushes.Transparent, IsHitTestVisible=false, Opacity=0.)
        let verticalDoors = Array2D.zeroCreate 8 7
        let vDoorCanvas = new Canvas()  // nesting canvases improves perf
        dungeonBodyCanvas.Children.Add(vDoorCanvas) |> ignore
        for i = 0 to 7 do
            for j = 0 to 6 do
                let d = new Canvas(Width=24., Height=12., Background=Brushes.Black)
                let rect = new Shapes.Rectangle(Width=21., Height=12., Stroke=unknown, StrokeThickness=2., Fill=unknown)
                let line = new Shapes.Line(X1 = -14., Y1 = 6., X2 = 38., Y2 = 6., StrokeThickness=3., Stroke=no, Opacity=0.)
                d.Children.Add(rect) |> ignore
                let door = new Dungeon.Door(Dungeon.DoorState.UNKNOWN, (function 
                    | Dungeon.DoorState.YES        -> rect.Stroke <- yes; rect.Fill <- yes; rect.Opacity <- 1.; line.Opacity <- 0.
                    | Dungeon.DoorState.NO         -> rect.Opacity <- 0.; line.Opacity <- 1.; if line.Parent = null then d.Children.Add(line) |> ignore  // only add lines to tree on-demand
                    | Dungeon.DoorState.YELLOW     -> rect.Stroke <- yellow; rect.Fill <- yellow; rect.Opacity <- 1.; line.Opacity <- 0.
                    | Dungeon.DoorState.PURPLE     -> rect.Stroke <- purple; rect.Fill <- purple; rect.Opacity <- 1.; line.Opacity <- 0.
                    | Dungeon.DoorState.UNKNOWN    -> 
                        rect.Stroke <- unknown; rect.Fill <- unknown; rect.Opacity <- 1.; line.Opacity <- 0.
                        if masterRoomStates.[level-1].[i,j].RoomType.IsOffMap || masterRoomStates.[level-1].[i,j+1].RoomType.IsOffMap then
                            rect.Stroke <- Brushes.Black
                            rect.Fill <- Brushes.Black
                        ))
                verticalDoors.[i,j] <- door
                canvasAdd(vDoorCanvas, d, float(i*(39+12)+9), float(j*(27+12)+27))
                installDoorBehavior(door, d, (i,j,DD), (i,j+1,UU))
                d.MouseEnter.Add(fun _ -> if not popupIsActive && not grabHelper.IsGrabMode then 
                                                Canvas.SetLeft(vDoorHighlightOutline, float(i*(39+12)+8))
                                                Canvas.SetTop(vDoorHighlightOutline, float(j*(27+12)+27))
                                                vDoorHighlightOutline.Opacity <- Dungeon.highlightOpacity)
                d.MouseLeave.Add(fun _ -> vDoorHighlightOutline.Opacity <- 0.0)
                redrawAllDoorFuncs.Add(fun () -> door.Redraw())
        canvasAdd(dungeonBodyCanvas, vDoorHighlightOutline, 0., 0.)
        // for room animation, later
        let backRoomHighlightTile = new Shapes.Rectangle(Width=float(13*3)+6., Height=float(9*3)+6., StrokeThickness=3., Opacity=1.0, IsHitTestVisible=false)
        canvasAdd(dungeonBodyCanvas, backRoomHighlightTile, 0., 0.)
        // rooms
        let roomCanvases = Array2D.zeroCreate 8 8 
        let roomStates = masterRoomStates.[level-1]
        let roomIsCircled = Array2D.zeroCreate 8 8
        let roomCircles = Array2D.zeroCreate 8 8
        let usedTransports = Array.zeroCreate 9 // slot 0 unused
        let updateHeaderCanvases() =
            baitMeatImage.Opacity <- 0.
            baitMeatCheckmarkHeaderCanvas.Opacity <- 0.
            bombUpgradeMarkHeaderCanvas.Opacity <- 0.
            oldManUnreadHeaderCanvas.Opacity <- 0.
            for i = 0 to 7 do
                for j = 0 to 7 do
                    let rs = roomStates.[i,j]
                    if rs.RoomType = RoomType.HungryGoriyaMeatBlock then
                        baitMeatImage.Opacity <- 1.
                        if not(rs.IsComplete) then
                            baitMeatCheckmarkHeaderCanvas.Opacity <- 0.
                        else
                            baitMeatCheckmarkHeaderCanvas.Opacity <- 1.
                    if rs.RoomType = RoomType.BombUpgrade && not(rs.IsComplete) then
                        bombUpgradeMarkHeaderCanvas.Opacity <- 1.
                    if TrackerModelOptions.BookForHelpfulHints.Value && rs.RoomType = RoomType.OldManHint && not(rs.IsComplete) then
                        oldManUnreadHeaderCanvas.Opacity <- 1.
            levelTab.InvalidateProperty(TabItem.HeaderProperty)
        OptionsMenu.bookForHelpfulHintsOptionChanged.Publish.Add(fun _ -> 
            redrawAllRooms()        // old man rooms might be complete or incomplete, re-color them to show actual backing state
            updateHeaderCanvases()  // and add/remove red dot as appropriate
            )  
        // have map checkbox
        do
            let cbOrAtlasCanvas = new Canvas()
            let c = new Canvas(Width=16., Height=16., ClipToBounds=true)
            let haveMapCB = new CheckBox(Content=c)
            let atlasImg = Graphics.BMPtoImage Graphics.zi_atlas
            let update() = if TrackerModel.playerHasAtlas() then atlasImg.Opacity<-1.0; haveMapCB.Opacity<-0.0 else atlasImg.Opacity<-0.0; haveMapCB.Opacity<-1.0
            update()
            TrackerModel.playerHasAtlasChanged.Publish.Add(fun _ -> update())
            canvasAdd(c, Graphics.BMPtoImage Graphics.zi_map_bmp, -1., -1.)
            canvasAdd(cbOrAtlasCanvas, atlasImg, 10., -1.)
            canvasAdd(cbOrAtlasCanvas, haveMapCB, 0., 0.)
            haveMapCB.ToolTip <- "I have the map (display on summary tab)"
            haveMapCB.IsChecked <- System.Nullable.op_Implicit false
            haveMapCB.Checked.Add(fun _ -> TrackerModel.GetDungeon(level-1).PlayerHasMapOfThisDungeon <- true)
            haveMapCB.Unchecked.Add(fun _ -> TrackerModel.GetDungeon(level-1).PlayerHasMapOfThisDungeon <- false)
            canvasAdd(contentCanvas, cbOrAtlasCanvas, LD_X+5., LD_Y+171.)
            TrackerModel.GetDungeon(level-1).PlayerHasMapOfThisDungeonChanged.Add(fun _ -> haveMapCB.IsChecked <- System.Nullable.op_Implicit (TrackerModel.GetDungeon(level-1).PlayerHasMapOfThisDungeon))
        // minimap-draw-er
        let hoverCanvas = new Canvas(Width=26., Height=26., Background=Brushes.Black, IsHitTestVisible=true)
        let minimini = Dungeon.MakeMiniMiniMapBmp() |> Graphics.BMPtoImage
        minimini.Width <- 24.
        minimini.Height <- 24.
        minimini.Stretch <- Stretch.UniformToFill
        minimini.Margin <- Thickness(1.)
        let miniBorder = new Border(Child=minimini, BorderThickness=Thickness(1.), BorderBrush=Brushes.Gray)
        canvasAdd(hoverCanvas, miniBorder, 0., 0.)
        canvasAdd(contentCanvas, hoverCanvas, LD_X+8., LD_Y+191.)
        showMinimaps <- (fun () ->
            let make(markedRooms) = 
                let bmp = Dungeon.MakeLoZMinimapDisplayBmp(markedRooms, if TrackerModel.IsHiddenDungeonNumbers() then '?' else char(level+int '0')) 
                let i = Graphics.BMPtoImage bmp
                i.Width <- 240.
                i.Height <- 120.
                i.Stretch <- Stretch.UniformToFill
                RenderOptions.SetBitmapScalingMode(i, BitmapScalingMode.NearestNeighbor)
                i
            let normal = make(roomStates |> Array2D.map (fun s -> not(s.IsEmpty)))
            let i = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
            let mutable anyOff = false
            roomStates |> Array2D.iter (fun s -> if(s.RoomType=RoomType.OffTheMap) then anyOff <- true)
            if anyOff then 
                let inverse = make(roomStates |> Array2D.map (fun s -> not(s.RoomType=RoomType.OffTheMap)))
                i.Children.Add(new TextBox(FontSize=14., Foreground=Brushes.DarkGray, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                                            BorderThickness=Thickness(0.), Margin=Thickness(0.,0., 0., 6.),
                                            Text="Full map, minus any Off-The-Map\nmarks you have painted:"
                                            )) |> ignore
                i.Children.Add(inverse) |> ignore
                i.Children.Add(new DockPanel(Height=2.)) |> ignore
                i.Children.Add(new TextBox(FontSize=14., Foreground=Brushes.DarkGray, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                                            BorderThickness=Thickness(0.), Margin=Thickness(0.,6., 0., 6.),
                                            Text="Your room marks:"
                                            )) |> ignore
            i.Children.Add(normal) |> ignore
            i.Children.Add(new DockPanel(Height=2.)) |> ignore   // this, and a lot of fine-tuning of pixel margins, is to make 2/3 version also look good
            let b = new Border(Child=new Border(Child=i, BorderThickness=Thickness(10.,4.,8.,8.), BorderBrush=Brushes.Black), BorderThickness=Thickness(2.), BorderBrush=Brushes.Gray)
            Canvas.SetBottom(b, 0.)
            Canvas.SetLeft(b, 0.)
            rightwardCanvas.Children.Clear()
            rightwardCanvas.Children.Add(b) |> ignore
            )
        hoverCanvas.MouseEnter.Add(fun _ -> 
            showMinimaps()
            )
        hoverCanvas.MouseLeave.Add(fun _ -> 
            rightwardCanvas.Children.Clear()
            )
        // toggler to invert Unmarked versus OffTheMap rooms
        do
            let dp = new DockPanel(LastChildFill=true, Background=Brushes.Black)
            let i1 = RoomType.OffTheMap.CompletedBI() |> Graphics.BItoImage
            i1.Stretch <- Stretch.Uniform; i1.StretchDirection <- StretchDirection.Both; i1.Width <- 13.; i1.Height <- System.Double.NaN
            dp.Children.Add(i1) |> ignore
            let i2 = RoomType.Unmarked.CompletedBI() |> Graphics.BItoImage
            i2.Stretch <- Stretch.Uniform; i2.StretchDirection <- StretchDirection.Both; i2.Width <- 13.; i2.Height <- System.Double.NaN
            dp.Children.Add(i2) |> ignore
            DockPanel.SetDock(i1, Dock.Left)
            DockPanel.SetDock(i2, Dock.Right)
            let g = new Grid()
            let c = new Canvas(Width=8., Height=8.)
            let slash = new Shapes.Line(X1=0., X2=8., Y1=8., Y2=0., Stroke=Brushes.Orange, StrokeThickness=2.0)
            c.Children.Add(slash) |> ignore
            g.Children.Add(c) |> ignore
            dp.Children.Add(g) |> ignore
            let b = new Border(Child=dp, BorderThickness=Thickness(1.0), BorderBrush=Brushes.DarkGray, Width=40., Height=16.)
            b.MouseEnter.Add(fun _ -> b.BorderBrush <- Brushes.DarkCyan)
            b.MouseLeave.Add(fun _ -> b.BorderBrush <- Brushes.DarkGray)
            invertOffTheMapWithUnmarked <- fun () ->
                hasEverInvertedOrDragged <- true
                for x=0 to 7 do
                    for y=0 to 7 do
                        if roomStates.[x,y].RoomType.IsNotMarked then
                            roomStates.[x,y].RoomType <- RoomType.OffTheMap
                        elif roomStates.[x,y].RoomType = RoomType.OffTheMap then
                            roomStates.[x,y].RoomType <- RoomType.Unmarked
            b.MouseDown.Add(fun _ -> 
                if grabHelper.IsGrabMode then
                    ()
                else
                    invertOffTheMapWithUnmarked()
                    redrawAllRooms()
                    redrawAllDoors()
                )
            canvasAdd(contentCanvas, b, LD_X+2., LD_Y+191.+32.)
        // grab button for this tab
        let grabTB = new TextBox(FontSize=float(TH-12), Foreground=Brushes.Gray, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false,
                                Text="GRAB", BorderThickness=Thickness(0.), Margin=Thickness(0.), Padding=Thickness(0.),
                                HorizontalContentAlignment=HorizontalAlignment.Center, VerticalContentAlignment=VerticalAlignment.Center)
        let grabButton = new Button(Width=float(13*3), Height=float(TH-3), Content=grabTB, Background=Brushes.Black, BorderThickness=Thickness(2.), 
                                    Margin=Thickness(0.), Padding=Thickness(0.), Focusable=false,
                                    HorizontalContentAlignment=HorizontalAlignment.Stretch, VerticalContentAlignment=VerticalAlignment.Stretch)
        let grabRedraw() =
            if grabHelper.IsGrabMode then
                grabTB.Foreground <- Brushes.White
                grabTB.Background <- Brushes.Red
                dungeonTabs.Cursor <- System.Windows.Input.Cursors.Hand
                grabModeTextBlock.Opacity <- 1.
            else
                grabHelper.Abort()
                dungeonHighlightCanvas.Children.Clear()
                dungeonSourceHighlightCanvas.Children.Clear()
                grabTB.Foreground <- Brushes.Gray
                grabTB.Background <- Brushes.Black
                dungeonTabs.Cursor <- null
                grabModeTextBlock.Opacity <- 0.
        canvasAdd(dungeonCanvas, grabButton, float(7*51)+1., 0.)
        grabButton.Click.Add(fun _ ->
            grabHelper.ToggleGrabMode()
            grabRedraw()
            )
        dungeonTabs.SelectionChanged.Add(fun ea -> 
            try
                let x = ea.AddedItems.[0]
                if obj.ReferenceEquals(x,levelTab) then
                    dummyCanvas.Children.Clear()
                    canvasAdd(contentCanvasWithBadYOffset, contentCanvas, 0., 1.)   // re-parent the content which may have been deparented by summary tab; y=1 so that all content is at multiples of 3, so that 2/3 view does not look blurry
                    levelTabSelected.Trigger(level)
                    showNumeral()
            with _ -> ()
            // the tab has already changed, kill the current grab
            if grabHelper.IsGrabMode then 
                grabHelper.Abort()
            // and always draw (even if changing to), as it was unable to repaint when disabled earlier
            grabRedraw()
            )

        let setNewValueFunctions = Array2D.create 8 8 (fun _ -> ())
        let backgroundColorCanvas = new Canvas(Width=float(51*6+12), Height=float(TH))
        canvasAdd(dungeonHeaderCanvas, backgroundColorCanvas, 0., 0.)
        TrackerModel.GetDungeon(level-1).HiddenDungeonColorOrLabelChanged.Add(fun (color,_) ->
            backgroundColorCanvas.Background <- Graphics.freeze(new SolidColorBrush(Graphics.makeColor(color)))
            )
        let mutable animateDungeonRoomTile = fun _ -> ()
        let highlightColumnCanvases = Array.init 8 (fun _ -> new Canvas(Background=Brushes.White, Width=51., Height=float TH, Opacity=0.0))
        let highlightColumn(colOpt) =
            for i = 0 to 7 do
                highlightColumnCanvases.[i].Opacity <- 0.0
            match colOpt with
            | None -> ()
            | Some c -> highlightColumnCanvases.[c].Opacity <- 0.3
        canvasAdd(dungeonBodyCanvas, roomHighlightOutline, 0., 0.)
        let roomCirclesCanvas = new Canvas()
        dungeonBodyCanvas.Children.Add(roomCanvas) |> ignore
        dungeonBodyCanvas.Children.Add(roomCirclesCanvas) |> ignore
        for i = 0 to 7 do
            if i<>7 then
                let makeLetter(bmpFunc) =
                    let bi = bmpFunc() 
                    let img = Graphics.BItoImage bi
                    img.Width <- float TH
                    img.Height <- float TH
                    img.Stretch <- Stretch.UniformToFill
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor)
                    img
                if TrackerModel.IsHiddenDungeonNumbers() then
                    if i <> 6 || labelChar = '9' then
                        let mutable img = null
                        let update() =
                            dungeonHeaderCanvas.Children.Remove(img)
                            dungeonHeaderCanvas.Children.Remove(highlightColumnCanvases.[i])
                            img <- makeLetter(fun() -> Dungeon.MakeLetterBIInZeldaFont(((if TrackerModelOptions.BOARDInsteadOfLEVEL.Value then "BOARD" else "LEVEL")+"-9").Substring(i,1).[0], 
                                                                                                Graphics.isBlackGoodContrast(TrackerModel.GetDungeon(level-1).Color)))
                            canvasAdd(dungeonHeaderCanvas, img, float(i*51)+9., 0.)
                            canvasAdd(dungeonHeaderCanvas, highlightColumnCanvases.[i], float(i*51)-6., 0.)
                        update()
                        OptionsMenu.BOARDInsteadOfLEVELOptionChanged.Publish.Add(fun _ -> update())
                        TrackerModel.GetDungeon(level-1).HiddenDungeonColorOrLabelChanged.Add(fun _ -> 
                            if Graphics.isBlackGoodContrast(TrackerModel.GetDungeon(level-1).Color) then
                                for i = 0 to 7 do
                                    highlightColumnCanvases.[i].Background <- Brushes.Black
                            else
                                for i = 0 to 7 do
                                    highlightColumnCanvases.[i].Background <- Brushes.White
                            update()
                            )
                    else
                        let tb = new TextBox(Width=float(13*3-16), Height=float(TH+8), FontSize=float(TH+8), Foreground=Brushes.Black, Background=rainbowBrush, IsReadOnly=true, IsHitTestVisible=false,
                                                Text=sprintf"%c"labelChar, BorderThickness=Thickness(0.), FontFamily=HFF, FontWeight=FontWeights.Bold,
                                                HorizontalContentAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
                        let tbc = new Canvas(Width=tb.Width, Height=float(TH-4), ClipToBounds=true)
                        canvasAdd(tbc, tb, 0., -8.)
                        let button = new Button(Height=float(TH), Content=tbc, BorderThickness=Thickness(2.), Margin=Thickness(0.), Padding=Thickness(0.), BorderBrush=Brushes.White)
                        canvasAdd(dungeonCanvas, button, float(i*51)+6., 0.)
                        button.Click.Add(fun _ ->
                            if not popupIsActive then
                                popupIsActive <- true
                                let pos = tb.TranslatePoint(Point(tb.Width/2., tb.Height/2.), cm.AppMainCanvas)
                                async {
                                    do! Dungeon.HiddenDungeonCustomizerPopup(cm, level-1, TrackerModel.GetDungeon(level-1).Color, TrackerModel.GetDungeon(level-1).LabelChar, false, true, pos)
                                    popupIsActive <- false
                                    } |> Async.StartImmediate
                            )
                else
                    let bmpFunc() = Dungeon.MakeLetterBIInZeldaFont((sprintf "%s-%d " (if TrackerModelOptions.BOARDInsteadOfLEVEL.Value then "BOARD" else "LEVEL") level).Substring(i,1).[0], false)
                    let mutable img = makeLetter(bmpFunc)
                    canvasAdd(dungeonHeaderCanvas, img, float(i*51)+9., 0.)
                    canvasAdd(dungeonHeaderCanvas, highlightColumnCanvases.[i], float(i*51)-6., 0.)
                    OptionsMenu.BOARDInsteadOfLEVELOptionChanged.Publish.Add(fun _ -> 
                        dungeonHeaderCanvas.Children.Remove(img)
                        dungeonHeaderCanvas.Children.Remove(highlightColumnCanvases.[i])
                        img <- makeLetter(bmpFunc)
                        canvasAdd(dungeonHeaderCanvas, img, float(i*51)+9., 0.)
                        canvasAdd(dungeonHeaderCanvas, highlightColumnCanvases.[i], float(i*51)-6., 0.)
                        )
            // room map
            for j = 0 to 7 do
                let BUFFER = 2.  // I often accidentally click room when trying to target doors with mouse, make canvas smaller and draw outside it, so clicks on very edge not seen
                let c = new Canvas(Width=float(13*3)-2.*BUFFER, Height=float(9*3)-2.*BUFFER, Background=Brushes.Black, IsHitTestVisible=true)
                let ROOM_X, ROOM_Y = float(i*51)+BUFFER, float(j*39)+BUFFER
                canvasAdd(roomCanvas, c, ROOM_X, ROOM_Y)
                roomCanvases.[i,j] <- c
                roomIsCircled.[i,j] <- false
                let redraw() =
                    if not skipRedrawInsideCurrentImport then
                        c.Children.Clear()
                        roomCirclesCanvas.Children.Remove(roomCircles.[i,j])
                        let image = roomStates.[i,j].CurrentDisplayEx(usedTransports)
                        image.IsHitTestVisible <- false
                        canvasAdd(c, image, -BUFFER, -BUFFER)
                        if roomIsCircled.[i,j] then
                            let ellipse = new Shapes.Ellipse(Width=float(13*3+12), Height=float(9*3+12), Stroke=Brushes.Yellow, StrokeThickness=3., IsHitTestVisible=false)
                            //ellipse.StrokeDashArray <- new DoubleCollection( seq[0.;2.5;6.;5.;6.;5.;6.;5.;6.;5.] )
                            ellipse.StrokeDashArray <- new DoubleCollection( seq[0.;12.5;8.;15.;8.;15.;] )
                            roomCircles.[i,j] <- ellipse
                            canvasAdd(roomCirclesCanvas, ellipse, ROOM_X-6.-BUFFER, ROOM_Y-6.-BUFFER)
                redraw()
                roomRedrawFuncs.Add(fun () -> redraw())
                let usedTransportsRemoveState(roomState:DungeonRoomState) =
                    // track transport being changed away from
                    match roomState.RoomType.KnownTransportNumber with
                    | None -> false
                    | Some n -> usedTransports.[n] <- usedTransports.[n] - 1; true
                let usedTransportsAddState(roomState:DungeonRoomState) =
                    // note any new transports
                    match roomState.RoomType.KnownTransportNumber with
                    | None -> false
                    | Some n -> usedTransports.[n] <- usedTransports.[n] + 1; true
                let SetNewValue(newState:DungeonRoomState) =
                    let originalState = roomStates.[i,j]
                    if not(originalState.Equals(newState)) then   // don't do work if nothing changed
                        let originallyWasNotMarked = originalState.RoomType.IsNotMarked
                        let isLegal = newState.RoomType = originalState.RoomType || 
                                        (match newState.RoomType.KnownTransportNumber with
                                            | None -> true
                                            | Some n -> usedTransports.[n]<>2)
                        if isLegal then
                            let removedTransport = usedTransportsRemoveState(roomStates.[i,j])
                            if roomStates.[i,j].RoomType.IsOldMan then
                                oldManCount <- oldManCount - 1
                            roomStates.[i,j] <- newState
                            if roomStates.[i,j].RoomType.IsOldMan then
                                oldManCount <- oldManCount + 1
                            updateOldManCountText()
                            let addedTransport = usedTransportsAddState(roomStates.[i,j])
                            if removedTransport || addedTransport then
                                redrawAllRooms()   // to change numeral colors
                            // conservative door inference
                            let secondTransport = newState.RoomType.KnownTransportNumber.IsSome && usedTransports.[newState.RoomType.KnownTransportNumber.Value] = 2
                            if TrackerModelOptions.DoDoorInference.Value && originallyWasNotMarked && not newState.IsEmpty && not secondTransport && not newState.IsGannonOrZelda then
                                // they appear to have walked into this room from an adjacent room
                                let possibleEntries = ResizeArray()
                                let maybeAdd(door:Dungeon.Door) =
                                    if door.State <> Dungeon.DoorState.NO then
                                        possibleEntries.Add(door)
                                if i > 0 && not(roomStates.[i-1,j].IsEmpty) then
                                    maybeAdd(horizontalDoors.[i-1,j])
                                if i < 7 && not(roomStates.[i+1,j].IsEmpty) then
                                    maybeAdd(horizontalDoors.[i,j])
                                if j > 0 && not(roomStates.[i,j-1].IsEmpty) then
                                    maybeAdd(verticalDoors.[i,j-1])
                                if j < 7 && not(roomStates.[i,j+1].IsEmpty) then
                                    maybeAdd(verticalDoors.[i,j])
                                if possibleEntries.Count = 1 then
                                    let door = possibleEntries.[0]
                                    if door.State = Dungeon.DoorState.UNKNOWN then
                                        door.State <- Dungeon.DoorState.YES
                            updateHeaderCanvases()
                            redraw()
                            redrawAllDoors()   // in case off-the-map changed, this adjusts adjacent doors; TODO probably expensive during Load of a save file
                            animateDungeonRoomTile(i,j)
                        else
                            // e.g. they tried to set this room to transport4, but two transport4s already exist
                            Graphics.ErrorBeepWithReminderLogText("Cannot mark a third copy of transport-stair pair")
                setNewValueFunctions.[i,j] <- SetNewValue
                let activatePopup(positionAtEntranceRoomIcons) = async {
                    popupIsActive <- true
                    let roomPos = c.TranslatePoint(Point(), cm.AppMainCanvas)
                    let dashCanvas = new Canvas()
                    canvasAdd(c, dashCanvas, -BUFFER, -BUFFER)
                    let pretty = CustomComboBoxes.GetPrettyDashes("DungeonRoomSelectActivatePopup", Brushes.Lime, 13.*3., 9.*3., 3., 2., 1.2)
                    dashCanvas.Children.Add(pretty) |> ignore
                    let pos = Point(roomPos.X+13.*3./2., roomPos.Y+9.*3./2.)
                    do! DungeonPopups.DoDungeonRoomSelectPopup(cm, roomStates.[i,j], usedTransports, SetNewValue, positionAtEntranceRoomIcons, CustomComboBoxes.WarpTo(pos)) 
                    dashCanvas.Children.Remove(pretty)
                    c.Children.Remove(dashCanvas)
                    redraw()
                    justUnmarked <- None
                    popupIsActive <- false
                    }
                let highlightImpl(canvas,contiguous:_[,], brush) =
                    for x = 0 to 7 do
                        for y = 0 to 7 do
                            if contiguous.[x,y] then
                                let r = new Shapes.Rectangle(Width=float(13*3 + 12), Height=float(9*3 + 12), Fill=brush, Opacity=0.4, IsHitTestVisible=false)
                                canvasAdd(canvas, r, float(x*51 - 6), float(TH+y*39 - 6))
                let highlight(contiguous:_[,], brush) = highlightImpl(dungeonHighlightCanvas,contiguous,brush)
                c.MyKeyAdd(fun ea ->
                    if not popupIsActive then
                        if not grabHelper.IsGrabMode then
                            match HotKeys.GlobalHotKeyProcessor.TryGetValue(ea.Key) with
                            | Some(HotKeys.GlobalHotkeyTargets.MoveCursorRight) -> 
                                ea.Handled <- true
                                if i<7 then
                                    Graphics.NavigationallyWarpMouseCursorTo(centerOf(float i+1.0, float j))
                                    //Graphics.WarpMouseCursorTo(centerOf(float i+0.5, float j))
                                    //roomWeJustCursorNavigatedFrom <- Some(i,j)
                                else
                                    Graphics.NavigationallyWarpMouseCursorTo(localDungeonTrackerPanelPosToCursorRightToF())
                            | Some(HotKeys.GlobalHotkeyTargets.MoveCursorLeft) -> 
                                ea.Handled <- true
                                if i>0 then
                                    Graphics.NavigationallyWarpMouseCursorTo(centerOf(float i-1.0, float j))
                                    //Graphics.WarpMouseCursorTo(centerOf(float i-0.5, float j))
                                    //roomWeJustCursorNavigatedFrom <- Some(i,j)
                            | Some(HotKeys.GlobalHotkeyTargets.MoveCursorUp) -> 
                                ea.Handled <- true
                                if j>0 then
                                    Graphics.NavigationallyWarpMouseCursorTo(centerOf(float i,float j-1.0))
                                    //Graphics.WarpMouseCursorTo(centerOf(float i,float j-0.5))
                                    //roomWeJustCursorNavigatedFrom <- Some(i,j)
                            | Some(HotKeys.GlobalHotkeyTargets.MoveCursorDown) -> 
                                ea.Handled <- true
                                if j<7 then
                                    Graphics.NavigationallyWarpMouseCursorTo(centerOf(float i,float j+1.0))
                                    //Graphics.WarpMouseCursorTo(centerOf(float i,float j+0.5))
                                    //roomWeJustCursorNavigatedFrom <- Some(i,j)
                            | _ -> ()
                            if ea.Handled then ()
                            else
                            let urb(hk) =   // Unmark-Remark behavior
                                match justUnmarked with
                                | Some(x,y,phk) when x=i && y=j && phk=hk -> unmarkRemarkBehavior(cm, justUnmarked.Value, boxViewsAndActivations, level-1, centerOf(float i, float j))
                                | _ -> justUnmarked <- None
                            // idempotent action on marked part toggles to Unmarked; user can left click to toggle completed-ness
                            let hk = HotKeys.DungeonRoomHotKeyProcessor.TryGetValue(ea.Key)
                            match hk with
                            | Some(Choice1Of4(roomType)) -> 
                                ea.Handled <- true
                                let workingCopy = roomStates.[i,j].Clone()
                                if workingCopy.RoomType = roomType then
                                    workingCopy.RoomType <- RoomType.Unmarked
                                    justUnmarked <- Some(i,j,hk)
                                else
                                    workingCopy.RoomType <- roomType
                                    urb(hk)
                                SetNewValue(workingCopy)
                            | Some(Choice2Of4(monsterDetail)) -> 
                                ea.Handled <- true
                                let workingCopy = roomStates.[i,j].Clone()
                                if workingCopy.MonsterDetail = monsterDetail then
                                    workingCopy.MonsterDetail <- MonsterDetail.Unmarked
                                    justUnmarked <- Some(i,j,hk)
                                else
                                    workingCopy.MonsterDetail <- monsterDetail
                                    urb(hk)
                                SetNewValue(workingCopy)
                            | Some(Choice3Of4(floorDropDetail)) -> 
                                ea.Handled <- true
                                let workingCopy = roomStates.[i,j].Clone()
                                if workingCopy.FloorDropDetail = floorDropDetail then
                                    workingCopy.FloorDropDetail <- FloorDropDetail.Unmarked
                                    justUnmarked <- Some(i,j,hk)
                                else
                                    workingCopy.FloorDropDetail <- floorDropDetail
                                    urb(hk)
                                SetNewValue(workingCopy)
                            | Some(Choice4Of4(doorHotKeyResponse)) -> 
                                ea.Handled <- true
                                justUnmarked <- None
                                match doorHotKeyResponse.Direction, doorHotKeyResponse.Action with
                                | DoorDirection.West, DoorAction.Increment -> if i>0 then horizontalDoors.[i-1,j].Next()
                                | DoorDirection.West, DoorAction.Decrement -> if i>0 then horizontalDoors.[i-1,j].Prev()
                                | DoorDirection.East, DoorAction.Increment -> if i<7 then horizontalDoors.[i,j].Next()
                                | DoorDirection.East, DoorAction.Decrement -> if i<7 then horizontalDoors.[i,j].Prev()
                                | DoorDirection.North, DoorAction.Increment -> if j>0 then verticalDoors.[i,j-1].Next()
                                | DoorDirection.North, DoorAction.Decrement -> if j>0 then verticalDoors.[i,j-1].Prev()
                                | DoorDirection.South, DoorAction.Increment -> if j<7 then verticalDoors.[i,j].Next()
                                | DoorDirection.South, DoorAction.Decrement -> if j<7 then verticalDoors.[i,j].Prev()
                            | None -> ()
                            if ea.Handled then  // if they pressed an actual hotkey
                                isFirstTimeClickingAnyRoom.[level-1].Value <- false  // hotkey cancels first-time click accelerator, so not to interfere with all-hotkey folks
                                numeral.Opacity <- 0.0
                    )
                let mutable justHighlightedMeatShop = false
                c.MouseEnter.Add(fun _ ->
                    if not popupIsActive then
                        if grabHelper.IsGrabMode then
                            if not grabHelper.HasGrab then
                                if not(roomStates.[i,j].IsEmpty) then
                                    dungeonHighlightCanvas.Children.Clear() // clear old preview
                                    let contiguous = grabHelper.PreviewGrab(i,j,roomStates)
                                    highlight(contiguous, Brushes.Lime)
                            else
                                dungeonHighlightCanvas.Children.Clear() // clear old preview
                                let ok,warn = grabHelper.PreviewDrop(i,j)
                                highlight(ok, Brushes.Lime)
                                highlight(warn, Brushes.Yellow)
                        else
                            highlightRow(Some j)
                            highlightColumn(Some i)
                            roomHighlightOutline.Opacity <- Dungeon.highlightOpacity
                            Canvas.SetLeft(roomHighlightOutline, float(i*51)-2.)
                            Canvas.SetTop(roomHighlightOutline, float(j*39)-2.)
                            if roomStates.[i,j].RoomType = RoomType.HungryGoriyaMeatBlock then
                                AhhGlobalVariables.showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.MEAT)
                                justHighlightedMeatShop <- true
                    )
                c.MouseLeave.Add(fun _ ->
                    if not popupIsActive then
                        if grabHelper.IsGrabMode then
                            dungeonHighlightCanvas.Children.Clear() // clear old preview
                    highlightRow(None)
                    highlightColumn(None)
                    roomHighlightOutline.Opacity <- 0.0
                    if justHighlightedMeatShop then
                        justHighlightedMeatShop <- false
                        // turn off showShopLocatorInstanceFunc and go back to the highlight for this level
                        AhhGlobalVariables.hideLocator()
                        contentCanvasMouseEnterFunc(level)  
                    )
                let doMonsterDetailPopup() = 
                    async {
                        let pos = c.TranslatePoint(Point(-6.,-6.),cm.AppMainCanvas)
                        let! mdOpt = DungeonPopups.DoMonsterDetailPopup(cm, pos.X, pos.Y, roomStates.[i,j].MonsterDetail)
                        match mdOpt with
                        | Some(md) ->
                            let workingCopy = roomStates.[i,j].Clone()
                            workingCopy.MonsterDetail <- md
                            SetNewValue(workingCopy)
                        | None -> ()
                        justUnmarked <- None
                        popupIsActive <- false
                    } |> Async.StartImmediate
                let doFloorDropDetailPopup() = 
                    async {
                        let pos = c.TranslatePoint(Point(18.,6.),cm.AppMainCanvas)
                        let! fdOpt = DungeonPopups.DoFloorDropDetailPopup(cm, pos.X, pos.Y, roomStates.[i,j].FloorDropDetail)
                        match fdOpt with
                        | Some(fd) ->
                            let workingCopy = roomStates.[i,j].Clone()
                            workingCopy.FloorDropDetail <- fd
                            SetNewValue(workingCopy)
                        | None -> ()
                        justUnmarked <- None
                        popupIsActive <- false
                    } |> Async.StartImmediate
                c.MouseWheel.Add(fun x -> 
                    if not popupIsActive then
                        if not grabHelper.IsGrabMode then  // cannot scroll rooms in grab mode
                            popupIsActive <- true
                            if x.Delta>0 then
                                doMonsterDetailPopup()
                            else
                                doFloorDropDetailPopup()
                    )
                let dragBehavior(whichButtonStr) =
                    if not popupIsActive then
                        if whichButtonStr = "L" && roomStates.[i,j].RoomType = RoomType.OffTheMap then
                            isFirstTimeClickingAnyRoom.[level-1].Value <- false  // originally painting cancels the first time accelerator (for 'play half dungeon, then start maybe-marking' scenario)
                            numeral.Opacity <- 0.0
                            roomStates.[i,j].RoomType <- RoomType.Unmarked
                            roomStates.[i,j].IsComplete <- false
                            redraw()
                            redrawAllDoors()
                            showMinimaps()
                        elif whichButtonStr = "R" && roomStates.[i,j].RoomType = RoomType.Unmarked then
                            isFirstTimeClickingAnyRoom.[level-1].Value <- false  // originally painting cancels the first time accelerator (for 'play half dungeon, then start maybe-marking' scenario)
                            numeral.Opacity <- 0.0
                            roomStates.[i,j].RoomType <- RoomType.OffTheMap
                            roomStates.[i,j].IsComplete <- false
                            redraw()
                            redrawAllDoors()
                            showMinimaps()
                        elif whichButtonStr = "M" && roomStates.[i,j].RoomType.IsNotMarked then
                            isFirstTimeClickingAnyRoom.[level-1].Value <- false  // originally painting cancels the first time accelerator (for 'play half dungeon, then start maybe-marking' scenario)
                            numeral.Opacity <- 0.0
                            roomStates.[i,j].RoomType <- defaultRoom()
                            roomStates.[i,j].IsComplete <- true
                            redraw()
                            redrawAllDoors()
                            showMinimaps()
                roomDragDrop.RegisterClickable(c, (fun ea ->
                    if not popupIsActive then
                        async {
                            if ea.ChangedButton = Input.MouseButton.Left then
                                if grabHelper.IsGrabMode then
                                    if not grabHelper.HasGrab then
                                        if not(roomStates.[i,j].IsEmpty) then
                                            dungeonHighlightCanvas.Children.Clear() // clear preview
                                            let contiguous = grabHelper.StartGrab(i,j,roomStates,roomIsCircled,horizontalDoors,verticalDoors)
                                            highlightImpl(dungeonSourceHighlightCanvas, contiguous, Brushes.Pink)  // this highlight stays around until completed/aborted
                                            highlight(contiguous, Brushes.Lime)
                                    else
                                        let backupRoomStates = roomStates |> Array2D.map (fun x -> x.Clone())
                                        let backupRoomIsCircled = roomIsCircled.Clone() :?> bool[,]
                                        let backupHorizontalDoors = horizontalDoors |> Array2D.map (fun c -> c.State)
                                        let backupVerticalDoors = verticalDoors |> Array2D.map (fun c -> c.State)
                                        grabHelper.DoDrop(i,j,roomStates,roomIsCircled,horizontalDoors,verticalDoors)
                                        redrawAllRooms()  // make updated changes visual
                                        let cmb = new CustomMessageBox.CustomMessageBox("Verify changes", System.Drawing.SystemIcons.Question, "You moved a dungeon segment. Keep this change?", ["Keep changes"; "Undo"])
                                        cmb.Owner <- Window.GetWindow(c)
                                        cmb.ShowDialog() |> ignore
                                        grabRedraw()  // DoDrop completes the grab, neeed to update the visual
                                        if cmb.MessageBoxResult = null || cmb.MessageBoxResult = "Undo" then
                                            // copy back from old state
                                            backupRoomStates |> Array2D.iteri (fun x y v -> roomStates.[x,y] <- v)
                                            backupRoomIsCircled |> Array2D.iteri (fun x y v -> roomIsCircled.[x,y] <- v)
                                            redrawAllRooms()  // make reverted changes visual
                                            horizontalDoors |> Array2D.iteri (fun x y c -> c.State <- backupHorizontalDoors.[x,y])
                                            verticalDoors |> Array2D.iteri (fun x y c -> c.State <- backupVerticalDoors.[x,y])
                                else
                                    if Input.Keyboard.IsKeyDown(Input.Key.LeftShift) || Input.Keyboard.IsKeyDown(Input.Key.RightShift) then
                                        popupIsActive <- true
                                        doMonsterDetailPopup()
                                    else
                                        // plain left click
                                        let workingCopy = roomStates.[i,j].Clone()
                                        if not(isFirstTimeClickingAnyRoom.[level-1].Value) && roomStates.[i,j].RoomType.IsNotMarked then
                                            // ad hoc useful gesture for clicking unknown room - it moves it to explored & completed state
                                            workingCopy.RoomType <- defaultRoom()
                                            workingCopy.IsComplete <- true
                                            SetNewValue(workingCopy)
                                            justUnmarked <- None
                                        else
                                            if isFirstTimeClickingAnyRoom.[level-1].Value then
                                                workingCopy.RoomType <- RoomType.StartEnterFromS
                                                workingCopy.IsComplete <- true
                                                SetNewValue(workingCopy)
                                                isFirstTimeClickingAnyRoom.[level-1].Value <- false
                                                numeral.Opacity <- 0.0
                                                justUnmarked <- None
                                            else
                                                match roomStates.[i,j].RoomType.NextEntranceRoom() with
                                                | Some(next) -> 
                                                    workingCopy.RoomType <- next  // cycle the entrance arrow around cardinal positions
                                                    SetNewValue(workingCopy)
                                                    justUnmarked <- None
                                                | None ->
                                                    if roomStates.[i,j].RoomType = RoomType.OffTheMap then
                                                        // left clicking an off the map paints it back on; helps recover people who accidentally toggle all OffTheMap
                                                        workingCopy.RoomType <- RoomType.Unmarked
                                                        SetNewValue(workingCopy)
                                                        justUnmarked <- None
                                                    else
                                                        // toggle completedness
                                                        workingCopy.IsComplete <- not roomStates.[i,j].IsComplete
                                                        SetNewValue(workingCopy)
                                                        justUnmarked <- None
                                        redraw()
                                    ea.Handled <- true
                            elif ea.ChangedButton = Input.MouseButton.Right then
                                if not grabHelper.IsGrabMode then  // cannot right click rooms in grab mode
                                    if Input.Keyboard.IsKeyDown(Input.Key.LeftShift) || Input.Keyboard.IsKeyDown(Input.Key.RightShift) then
                                        popupIsActive <- true
                                        doFloorDropDetailPopup()
                                    else
                                        // plain right click
                                        do! activatePopup(isFirstTimeClickingAnyRoom.[level-1].Value)
                                        isFirstTimeClickingAnyRoom.[level-1].Value <- false
                                        numeral.Opacity <- 0.0
                                        redraw()
                                    ea.Handled <- true
                            elif ea.ChangedButton = Input.MouseButton.Middle then
                                if not grabHelper.IsGrabMode then  // cannot middle click rooms in grab mode
                                    // middle click toggles floor drops, or if none, toggle circles
                                    if roomStates.[i,j].FloorDropDetail.IsNotMarked then
                                        roomIsCircled.[i,j] <- not roomIsCircled.[i,j]
                                    else
                                        roomStates.[i,j].ToggleFloorDropBrightness()
                                    redraw()
                                    ea.Handled <- true
                        } |> Async.StartImmediate
                    ), dragBehavior)
                c.DragOver.Add(fun ea ->
                    let whichButtonStr = ea.Data.GetData(DataFormats.StringFormat) :?> string
                    dragBehavior(whichButtonStr)
                    )
                c.AllowDrop <- true
        canvasAdd(dungeonCanvas, outlineDrawingCanvases.[level-1], 0., 0.)
        // animation
        do
            let c(t) = Color.FromArgb(t,255uy,165uy,0uy)
            let scb = new SolidColorBrush(c(0uy))
            let ca = new Animation.ColorAnimation(From=Nullable<_>(c(0uy)), To=Nullable<_>(c(180uy)), Duration=new Duration(TimeSpan.FromSeconds(1.0)), AutoReverse=true)
            let frontRoomHighlightTile = new Shapes.Rectangle(Width=float(13*3)+6., Height=float(9*3)+6., StrokeThickness=3., Stroke=scb, Opacity=1.0, IsHitTestVisible=false)
            canvasAdd(dungeonBodyCanvas, frontRoomHighlightTile, 0., 0.)
            let animateRoomTile(x,y) = 
                if TrackerModelOptions.AnimateTileChanges.Value then
                    if roomStates.[x,y].RoomType = RoomType.OffTheMap then
                        Canvas.SetLeft(frontRoomHighlightTile, float(x*51)-3.)
                        Canvas.SetTop(frontRoomHighlightTile, float(y*39)-3.)
                        frontRoomHighlightTile.Stroke <- scb
                        backRoomHighlightTile.Stroke <- null
                    else
                        Canvas.SetLeft(backRoomHighlightTile, float(x*51)-3.)
                        Canvas.SetTop(backRoomHighlightTile, float(y*39)-3.)
                        frontRoomHighlightTile.Stroke <- null
                        backRoomHighlightTile.Stroke <- scb
                    scb.BeginAnimation(SolidColorBrush.ColorProperty, ca)
            animateDungeonRoomTile <- animateRoomTile
        // "sunglasses"; make it 6 extra wide&tall (3 off each edge) to cover lobby entrance arrows that draw off the map boundaries
        let darkenRect = new Shapes.Rectangle(Width=dungeonCanvas.Width+6., Height=dungeonCanvas.Height+6., StrokeThickness = 0., Fill=Brushes.Black, IsHitTestVisible=false)
        canvasAdd(dungeonCanvas, darkenRect, -3., -3.)
        let doSunglasses() = if TrackerModelOptions.GiveDungeonTrackerSunglasses.Value then darkenRect.Opacity <- 0.15 else darkenRect.Opacity <- 0.
        doSunglasses()
        OptionsMenu.dungeonSunglassesChanged.Publish.Add(fun _ -> doSunglasses())
        canvasAdd(dungeonBodyCanvas, numeral, 0., 0.)  // so numeral displays atop all else
        // highlights
        DungeonHighlightsUI.makeHighlights(level, dungeonTabs, dungeonBodyHighlightCanvas, rightwardCanvas, roomStates, usedTransports, 
                                            currentOutlineDisplayState, horizontalDoors, verticalDoors, blockersHoverEvent)
        // save and load
        exportFunctions.[level-1] <- (fun () ->
            let r = new DungeonSaveAndLoad.DungeonModel()
            r.HorizontalDoors <- Array.init 7 (fun i -> Array.init 8 (fun j -> horizontalDoors.[i,j].State.AsInt()))
            r.VerticalDoors <-   Array.init 8 (fun i -> Array.init 7 (fun j -> verticalDoors.[i,j].State.AsInt()))
            r.RoomIsCircled <-   Array.init 8 (fun i -> Array.init 8 (fun j -> roomIsCircled.[i,j]))
            r.RoomStates <-      Array.init 8 (fun i -> Array.init 8 (fun j -> roomStates.[i,j] |> DungeonSaveAndLoad.DungeonRoomStateAsModel))
            r.VanillaMapOverlay <- currentOutlineDisplayState.[level-1]
            r
            )
        importFunctions.[level-1] <- (fun (dm:DungeonSaveAndLoad.DungeonModel) ->
            skipRedrawInsideCurrentImport <- true
            for i = 0 to 7 do
                for j = 0 to 7 do
                    roomIsCircled.[i,j] <- dm.RoomIsCircled.[j].[i]
                    let jsonModel = dm.RoomStates.[j].[i]
                    if jsonModel <> null then
                        let rs = jsonModel.AsDungeonRoomState()
                        if rs.RoomType <> RoomType.Unmarked then
                            isFirstTimeClickingAnyRoom.[level-1].Value <- false
                            numeral.Opacity <- 0.0 
                        setNewValueFunctions.[i,j](rs)
            // we set the door after the rooms, because DoDoorInference may have inferred some values during the setNewValueFunctions calls, and want to overwrite
            for i = 0 to 6 do
                for j = 0 to 7 do
                    horizontalDoors.[i,j].State <- Dungeon.DoorState.FromInt dm.HorizontalDoors.[j].[i]
            for i = 0 to 7 do
                for j = 0 to 6 do
                    verticalDoors.[i,j].State <- Dungeon.DoorState.FromInt dm.VerticalDoors.[j].[i]
            currentOutlineDisplayState.[level-1] <- dm.VanillaMapOverlay
            doVanillaOutlineRedraw(outlineDrawingCanvases.[level-1], currentOutlineDisplayState.[level-1])
            // just redraw everything once at the end
            skipRedrawInsideCurrentImport <- false
            redrawAllDoors()
            redrawAllRooms()
            )
        masterRedrawAllRooms.[level-1] <- fun() -> redrawAllRooms()
        do! showProgress(sprintf "finish dungeon level %d" level)
    // end -- for level in 1 to 9 do
    do
        // summary tab
        let levelTab = new TabItem(Background=Brushes.Black, Foreground=Brushes.Black)
        dungeonTabs.Items.Add(levelTab) |> ignore
        let header = new TextBox(Width=22., Background=Brushes.Transparent, Foreground=Brushes.White, Text="S", IsReadOnly=true, IsHitTestVisible=false, 
                                 HorizontalContentAlignment=HorizontalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), Padding=Thickness(0.))
        let headerGrid = new Grid(Background=Graphics.almostBlack, Width=header.Width)
        headerGrid.Children.Add(header) |> ignore
        // hovering the "S" tab behaves like hovering the blank space in the upper left of the displayed summary tab
        headerGrid.MouseEnter.Add(fun _ -> contentCanvasMouseEnterFunc(10))
        headerGrid.MouseLeave.Add(fun _ -> contentCanvasMouseLeaveFunc(10))
        levelTab.Header <- headerGrid
        let contentCanvasWithBadYOffset = new Canvas(Height=float(TH + 3 + 27*8 + 12*7 + 3), Width=float(3 + 39*8 + 12*7 + 3)+localDungeonTrackerPanelWidth, Background=Brushes.Black)
        levelTab.Content <- contentCanvasWithBadYOffset
        //contentCanvas.MouseEnter.Add(fun _ -> contentCanvasMouseEnterFunc(10))  // just the mini's call Enter
        contentCanvasWithBadYOffset.MouseLeave.Add(fun _ -> contentCanvasMouseLeaveFunc(10))
        canvasAdd(contentCanvasWithBadYOffset, dummyCanvas, 0., 1.)
        let monsterPriority = // in order of what goes top of the list to surface, down to bottom
            [| MonsterDetail.BlueWizzrobe; MonsterDetail.BlueDarknut; MonsterDetail.RedLynel; MonsterDetail.RedGoriya; MonsterDetail.BlueMoblin;         // principal mobs
               MonsterDetail.Patra; MonsterDetail.Bow; MonsterDetail.Digdogger; MonsterDetail.Dodongo; MonsterDetail.Gleeok; MonsterDetail.Manhandla;    // blocker bosses
               MonsterDetail.PolsVoice;                                                                                                                  // not exactly a blocker, but bow helps tons
               MonsterDetail.Keese; MonsterDetail.Gel; MonsterDetail.Stalfos; MonsterDetail.Zol; MonsterDetail.RedTektite; MonsterDetail.Rope;           // easy notables
               MonsterDetail.Other; MonsterDetail.RedBubble; MonsterDetail.BlueBubble; MonsterDetail.Aquamentus; MonsterDetail.BlueLanmola;              // other notables, rest of bosses
               MonsterDetail.Moldorm; MonsterDetail.RupeeBoss; MonsterDetail.Other2; MonsterDetail.Traps;                                                // all the
               MonsterDetail.Wallmaster; MonsterDetail.Likelike; MonsterDetail.Gibdo; MonsterDetail.Vire; MonsterDetail.Unmarked |]                      // rest
        if monsterPriority.Length <> MonsterDetail.All().Length then
            failwith "design-time bug, not all monsters prioritized"
        let findAnyMarkedMonsters(dunIdx) = 
            let rs = masterRoomStates.[dunIdx]
            let hasMonster = new System.Collections.Generic.Dictionary<_,_>()
            monsterPriority |> Seq.iter (fun m -> hasMonster.Add(m, false))
            let mutable lobbyMonster = None
            for i = 0 to 7 do
                for j = 0 to 7 do
                    let monster = rs.[i,j].MonsterDetail
                    hasMonster.[ monster ] <- true
                    if rs.[i,j].RoomType.NextEntranceRoom().IsSome && monster<>MonsterDetail.Unmarked then
                        lobbyMonster <- Some(monster)
            [|
                match lobbyMonster with
                | Some m -> yield m.Bmp() |> Graphics.BMPtoImage
                | _ -> ()
                for m in monsterPriority do
                    if m <> MonsterDetail.Unmarked && hasMonster.[m] && 
                                not(lobbyMonster.IsSome && lobbyMonster.Value=m) then   // don't duplicate the lobby
                        yield m.Bmp() |> Graphics.BMPtoImage
            |]
        let w, h = int contentCanvasWithBadYOffset.Width / 3, int contentCanvasWithBadYOffset.Height / 3
        let monsterSPs : StackPanel[] = Array.init 9 (fun _ -> new StackPanel(Orientation=Orientation.Vertical, VerticalAlignment=VerticalAlignment.Center, 
                                                                                HorizontalAlignment=HorizontalAlignment.Center, Width=20., MaxHeight=float h-20.))
        let tinyMaps = Array.init 9 (fun _ -> new Canvas(Width=float(13*8), Height=float(9*8)))
        dungeonTabs.SelectionChanged.Add(fun ea -> 
            try
                let x = ea.AddedItems.[0]
                if obj.ReferenceEquals(x,levelTab) then
                    dummyCanvas.Children.Clear()
                    for i = 0 to 8 do
                        // deparent content canvases
                        contentCanvasesWithBadYOffsets.[i].Children.Clear()
                        // make them visually here (but hidden by the dummy's lack of opacity), so that updates (like BOARD<->LEVEL) get visually drawn to be picked up by VisualBrush
                        dummyCanvas.Children.Add(contentCanvases.[i]) |> ignore
                        // redraw the monster tables
                        monsterSPs.[i].Children.Clear()
                        findAnyMarkedMonsters(i) |> Seq.iter (fun img -> 
                            let c = new Canvas(Width=16., Height=16., ClipToBounds=true)
                            canvasAdd(c, img, -1., -1.)
                            monsterSPs.[i].Children.Add(c) |> ignore
                            )
                        // redraw the tiny map
                        tinyMaps.[i].Children.Clear()
                        for x = 0 to 7 do
                            for y = 0 to 7 do
                                let room = masterRoomStates.[i].[x,y]
                                let e = 
                                    match room.RoomType with
                                    | RoomType.OffTheMap -> null
                                    | roomType -> Graphics.BItoImage (if room.IsComplete then roomType.TinyCompletedBI() else roomType.TinyUncompletedBI())
                                canvasAdd(tinyMaps.[i], e, float(13*x), float(9*y))
                    levelTabSelected.Trigger(10)
            with _ -> ()
            )
        // grid
        let g = Graphics.makeGrid(3, 3, w, h)
        let summaryMode = TrackerModel.EventingInt(TrackerModelOptions.DungeonSummaryTabMode)   // 0=default, 1=preview, 2=detail
        let makeMidiPreviewBehavior(i, fe:FrameworkElement) =
            // midi at 2/3 size looks fine and covers notes area fine when hovering summary of a dungeon
            let midi = new Shapes.Rectangle(Width=2.* float w, Height=2.* float h, Fill=new VisualBrush(contentCanvases.[i]))
            let overlay = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(5.), Background=Brushes.Black, IsHitTestVisible=false, Child=midi)
            Canvas.SetLeft(overlay, 0.)
            Canvas.SetBottom(overlay, 0.)
            fe.MouseEnter.Add(fun _ ->
                rightwardCanvas.Children.Clear()
                rightwardCanvas.Children.Add(overlay) |> ignore
                levelTabSelected.Trigger(i+1)
                contentCanvasMouseEnterFunc(10+i+1)
                )
            fe.MouseLeave.Add(fun _ ->
                rightwardCanvas.Children.Clear()
                if dungeonTabs.SelectedIndex=9 then  // we may have clicked on D4, and MouseLeave fires after tab is switched
                    levelTabSelected.Trigger(10)
                contentCanvasMouseLeaveFunc(10+i+1)
                )
            fe.MouseDown.Add(fun _ ->
                rightwardCanvas.Children.Clear()
                System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new System.Action(fun () -> 
                    dungeonTabs.SelectedIndex <- i
                    levelTabSelected.Trigger(i+1)
                    )) |> ignore
                )
        let make(i) =
            let mini = new Canvas(Width=float w, Height=float h, Background=Brushes.Black)
            let miniOuterDock = new DockPanel(Width=float w, Height=float h)
            canvasAdd(mini, miniOuterDock, 0., 0.)
            do  // decide if show blank canvas for undiscovered/unmarked dungeon, or what's in the tab
                let c = new Canvas(Width=float w, Height=float h, Background=Brushes.Black)
                let n = makeNumeral(getLabelChar(i+1))
                n.FontSize <- n.FontSize / 3.
                n.Width <- n.Width / 3.
                n.Height <- n.Height / 3.
                canvasAdd(c, n, 0., float(TH/3))
                canvasAdd(mini, c, 0., 0.)
                let origNumeralColor = n.Foreground
                let notFoundText = new TextBox(Text="not yet found",Foreground=origNumeralColor,Background=Brushes.Black,IsReadOnly=true,IsHitTestVisible=false,BorderThickness=Thickness(0.),FontSize=12.)
                let completeText = new TextBox(Text="complete",Foreground=Brushes.Lime,Background=Brushes.Black,IsReadOnly=true,IsHitTestVisible=false,BorderThickness=Thickness(0.))
                canvasAdd(c, notFoundText, 30., 85.)
                canvasAdd(c, completeText, 40., 85.)
                let miniContent = new Shapes.Rectangle(Width=float w, Height=float h, Fill=new VisualBrush(contentCanvases.[i]))
                canvasAdd(c, miniContent, 0., 0.)
                let decide() =
                    if TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Value() || isHoveringFQSQ.Value || summaryMode.Value=1 then
                        // just show their marks
                        miniContent.Opacity <- 1.0
                        c.Opacity <- 1.0
                        n.Opacity <- 0.0
                        notFoundText.Opacity <- 0.0
                        completeText.Opacity <- 0.0
                    elif summaryMode.Value=2 then
                        // show the summary created further below
                        c.Opacity <- 0.0
                    elif isFirstTimeClickingAnyRoom.[i].Value && TrackerModel.mapStateSummary.DungeonLocations.[i]=TrackerModel.NOTFOUND then
                        // nothing marked, not found, show a blank canvas with the numeral
                        miniContent.Opacity <- 0.0
                        c.Opacity <- 1.0
                        n.Opacity <- 0.7
                        n.Foreground <- origNumeralColor
                        notFoundText.Opacity <- 0.7
                        completeText.Opacity <- 0.0
                    elif TrackerModel.GetDungeon(i).IsComplete then
                        // blot out their marks, green numeral
                        miniContent.Opacity <- 0.0
                        c.Opacity <- 1.0
                        n.Opacity <- 0.5
                        n.Foreground <- Brushes.Lime
                        notFoundText.Opacity <- 0.0
                        completeText.Opacity <- 0.5
                    else
                        // show the summary created further below
                        c.Opacity <- 0.0
                decide()
                TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Changed.Add(fun _ -> decide())
                isFirstTimeClickingAnyRoom.[i].Changed.Add(fun _ -> decide())
                TrackerModel.mapStateSummaryComputedEvent.Publish.Add(fun _ -> decide())
                TrackerModel.GetDungeon(i).IsCompleteWasNoticedChanged.Add(fun _ -> decide())
                isHoveringFQSQ.Changed.Add(fun _ -> decide())
                summaryMode.Changed.Add(fun _ -> TrackerModelOptions.DungeonSummaryTabMode <- summaryMode.Value; TrackerModelOptions.writeSettings(); decide())
            let canSeeMapBox =
                let canSeeMapCanvas = new Canvas(Width=16., Height=16., ClipToBounds=true)
                let mapImg = Graphics.BMPtoImage Graphics.zi_map_bmp
                let atlasImg = Graphics.BMPtoImage Graphics.zi_atlas
                let update() = if TrackerModel.playerHasAtlas() then atlasImg.Opacity<-1.0; mapImg.Opacity<-0.0 else atlasImg.Opacity<-0.0; mapImg.Opacity<-1.0
                update()
                TrackerModel.playerHasAtlasChanged.Publish.Add(fun _ -> update())
                canvasAdd(canSeeMapCanvas, mapImg, -1., -1.)
                canvasAdd(canSeeMapCanvas, atlasImg, -1., -1.)
                canSeeMapCanvas.Opacity <- if TrackerModel.GetDungeon(i).PlayerCanSeeMapOfThisDungeon then 1.0 else 0.0
                TrackerModel.GetDungeon(i).PlayerCanSeeMapOfThisDungeonChanged.Add(fun _ -> canSeeMapCanvas.Opacity <- if TrackerModel.GetDungeon(i).PlayerCanSeeMapOfThisDungeon then 1.0 else 0.0)
                new Border(Child=canSeeMapCanvas, BorderBrush=Brushes.Gray, BorderThickness=Thickness(2.))
            let mapAndMonstersSp = new StackPanel(Orientation=Orientation.Vertical, Width=20.)
            mapAndMonstersSp.Children.Add(canSeeMapBox) |> ignore
            let g = new Grid(Width=20., Height=float h-20.)   // grid to center the element
            g.Children.Add(monsterSPs.[i]) |> ignore
            mapAndMonstersSp.Children.Add(g) |> ignore
            DockPanel.SetDock(mapAndMonstersSp, Dock.Right)
            miniOuterDock.Children.Add(mapAndMonstersSp) |> ignore

            let miniMidDock = new DockPanel()
            miniOuterDock.Children.Add(miniMidDock) |> ignore
            let sltp = MakeSmallLocalTrackerPanel(i, (if i=8 then null else mainTrackerGhostbusters.[i]))
            sltp.Width <- 30.
            let scaleTrans = new ScaleTransform(2./3., 2./3.)
            if scaleTrans.CanFreeze then
                scaleTrans.Freeze()
            sltp.LayoutTransform <- scaleTrans 
            DockPanel.SetDock(sltp, Dock.Right)
            miniMidDock.Children.Add(sltp) |> ignore

            let miniMainDock = new DockPanel()
            DockPanel.SetDock(tinyMaps.[i], Dock.Bottom)
            miniMainDock.Children.Add(tinyMaps.[i]) |> ignore

            let tb = new TextBox(Text=sprintf "%c" (getLabelChar(i+1)), Foreground=Brushes.White, Background=Brushes.Black, FontSize=12., Width=10., Height=14., IsHitTestVisible=false,
                                    VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.),
                                    TextAlignment=TextAlignment.Center, Margin=Thickness(2.,0.,0.,0.))
            if i <> 8 then
                let blockerSP = new StackPanel(Orientation=Orientation.Horizontal)
                blockerSP.Children.Add(tb) |> ignore
                blockerSP.Children.Add(Views.MakeBlockerView(i,0)) |> ignore
                blockerSP.Children.Add(Views.MakeBlockerView(i,1)) |> ignore
                blockerSP.Children.Add(Views.MakeBlockerView(i,2)) |> ignore
                miniMainDock.Children.Add(blockerSP) |> ignore

            miniMidDock.Children.Add(miniMainDock) |> ignore

            makeMidiPreviewBehavior(i, mini)
            mini
        let summaryModeButton = 
            let tb = new TextBox(Text="", IsReadOnly=true, IsHitTestVisible=false, TextAlignment=TextAlignment.Center, BorderThickness=Thickness(0.), Background=Graphics.almostBlack,
                                    FontSize=12., Foreground=Brushes.Orange)
            let button = new Button(Content=tb, HorizontalContentAlignment=HorizontalAlignment.Stretch, VerticalContentAlignment=VerticalAlignment.Stretch, Width=100.)
            let updateModeText() =
                if summaryMode.Value=1 then
                    tb.Text <- "Mode: preview"
                elif summaryMode.Value=2 then
                    tb.Text <- "Mode: detail"
                else
                    tb.Text <- "Mode: default"
            updateModeText()
            button.Click.Add(fun _ ->
                if summaryMode.Value=0 then
                    summaryMode.Value <- 1
                elif summaryMode.Value=1 then
                    summaryMode.Value <- 2
                else
                    summaryMode.Value <- 0
                updateModeText()                    
                )
            button
        let text = new TextBox(Text="Dungeon Summary\n\nHover to preview tab\n\nClick to switch tab", Margin=Thickness(3.),
                               Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                               FontSize=12., HorizontalContentAlignment=HorizontalAlignment.Center)
        let dp = new DockPanel(Background=Brushes.Black, LastChildFill=true)
        DockPanel.SetDock(summaryModeButton, Dock.Top)
        dp.Children.Add(summaryModeButton) |> ignore
        dp.Children.Add(text) |> ignore
        let tb = new Border(Width=float w, Height=float h, Background=Brushes.Black, Child=dp)
        makeMidiPreviewBehavior(8, tb)
        Graphics.gridAdd(g, tb, 0, 0)
        Graphics.gridAdd(g, make(0), 1, 0)
        Graphics.gridAdd(g, make(1), 2, 0)
        Graphics.gridAdd(g, make(2), 0, 1)
        Graphics.gridAdd(g, make(3), 1, 1)
        Graphics.gridAdd(g, make(4), 2, 1)
        Graphics.gridAdd(g, make(5), 0, 2)
        Graphics.gridAdd(g, make(6), 1, 2)
        Graphics.gridAdd(g, make(7), 2, 2)
        let mutable nine = null
        TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Changed.Add(fun b -> 
            // put dungeon 9 over the intro text at the end of the seed, so you can have a screenshot of everything at once
            if b then
                if nine=null then
                    nine <- make(8)
                g.Children.Remove(tb)
                g.Children.Remove(nine)
                Graphics.gridAdd(g, nine, 0, 0)
            else
                if nine<>null then
                    g.Children.Remove(nine)
                g.Children.Remove(tb)
                Graphics.gridAdd(g, tb, 0, 0)
            )
        do // put gridlines to bound each dungeon
            let ccw,cch = contentCanvasWithBadYOffset.Width, int contentCanvasWithBadYOffset.Height
            let gc = new Canvas(Width=float ccw, Height=float cch)
            gc.Children.Add(g) |> ignore
            canvasAdd(gc, new Shapes.Line(X1=0., X2=float ccw, Y1=0.5+float cch/3., Y2=0.5+float cch/3., Stroke=Brushes.Gray, StrokeThickness=1.), 0., 0.)
            canvasAdd(gc, new Shapes.Line(X1=0., X2=float ccw, Y1=0.5+float cch*2./3., Y2=0.5+float cch*2./3., Stroke=Brushes.Gray, StrokeThickness=1.), 0., 0.)
            canvasAdd(gc, new Shapes.Line(X1=0.5+float ccw/3., X2=0.5+float ccw/3., Y1=0., Y2=float cch, Stroke=Brushes.Gray, StrokeThickness=1.), 0., 0.)
            canvasAdd(gc, new Shapes.Line(X1=0.5+float ccw*2./3., X2=0.5+float ccw*2./3., Y1=0., Y2=float cch, Stroke=Brushes.Gray, StrokeThickness=1.), 0., 0.)
            canvasAdd(contentCanvasWithBadYOffset, gc, 0., 0.)
    // end summary tab
    dungeonTabs.SelectedIndex <- 9
    selectDungeonTabEvent.Publish.Add(fun i -> dungeonTabs.SelectedIndex <- i)

    let exportDungeonModelsJsonLines() = DungeonSaveAndLoad.SaveAllDungeons [| for f in exportFunctions do yield f() |]
    let importDungeonModels(showProgress, dma : DungeonSaveAndLoad.DungeonModel[]) = async {
        do! showProgress("starting dungeon load")
        for i = 0 to 8 do
            importFunctions.[i](dma.[i])
            do! showProgress(sprintf "finished dungeon %d of 9" (i+1))
        }
    OptionsMenu.BOARDInsteadOfLEVELOptionChanged.Trigger() // to populate BOARD v LEVEL text for all tabs the first time

    AhhGlobalVariables.resetDungeonsForRouters <- (fun () ->
        for idx = 0 to 8 do
            let rs = masterRoomStates.[idx]
            for i = 0 to 7 do
                for j = 0 to 7 do
                    let room = rs.[i,j]
                    if not room.FloorDropAppearsBright then
                        room.ToggleFloorDropBrightness()
                    // don't update room completeness; for one, is wonky about e.g. lobby, and also replaying a map is mainly about marked-floor-drops loot, not clearing every room
                    //room.IsComplete <- false
            masterRedrawAllRooms.[idx]()
        )

    return dungeonTabsWholeCanvas, Point(3.+3.5*39.+3.*12.,float(2*TH)+3.+27.*4.5+12.*4.), // point within dungeonTabsWholeCanvas that is destination of 'tab from overworld' cursor warp
                grabModeTextBlock, exportDungeonModelsJsonLines, importDungeonModels
    }