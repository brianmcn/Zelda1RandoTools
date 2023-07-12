module OverworldItemGridUI

// the top-right portion of the main UI

open System.Windows
open System.Windows.Controls 
open System.Windows.Media

open OverworldMapTileCustomization
open HotKeys.MyKey
open DungeonUI.AhhGlobalVariables
open CustomComboBoxes.GlobalFlag


let canvasAdd = Graphics.canvasAdd
let gridAdd = Graphics.gridAdd
let gridAddTuple(g,e,(x,y)) = gridAdd(g,e,x,y)
let makeGrid = Graphics.makeGrid

let makeHintHighlight = Views.makeHintHighlight

let OMTW = OverworldRouteDrawing.OMTW  // overworld map tile width - at normal aspect ratio, is 48 (16*3)

[<RequireQualifiedAccess>]
type ShowLocatorDescriptor =
    | DungeonNumber of int   // 0-7 means dungeon 1-8
    | DungeonIndex of int    // 0-8 means 123456789 or ABCDEFGH9 in top-left-ui presentation order
    | Sword1
    | Sword2
    | Sword3

// some global variables needed across various UI components
let TCH = 127  // timeline height
let TH = DungeonUI.TH // text height
let THRU_MAIN_MAP_H = float(150 + 8*11*3)
let THRU_MAP_AND_LEGEND_H = THRU_MAIN_MAP_H + float(11*3)
let THRU_MAIN_MAP_AND_ITEM_PROGRESS_H = THRU_MAP_AND_LEGEND_H + 30.
let THRU_BLOCKERS_H = THRU_MAIN_MAP_AND_ITEM_PROGRESS_H + 36.*3.
let START_DUNGEON_AND_NOTES_AREA_H = THRU_MAIN_MAP_AND_ITEM_PROGRESS_H
let THRU_DUNGEON_AND_NOTES_AREA_H = START_DUNGEON_AND_NOTES_AREA_H + float(TH + 30 + (3 + 27*8 + 12*7 + 3) + 3)  // 3 is for a little blank space after this but before timeline
let START_TIMELINE_H = THRU_DUNGEON_AND_NOTES_AREA_H
let THRU_TIMELINE_H = START_TIMELINE_H + float TCH
let LEFT_OFFSET = 78.0
let BLOCKERS_AND_NOTES_OFFSET = 408. + 42.  // dungeon area and side-tracker-panel
let ITEM_PROGRESS_FIRST_ITEM = 129.
let hmsTimeTextBox = new TextBox(Width=148.,Height=56.,Text="timer",FontSize=42.0,Background=Brushes.Transparent,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false)
let broadcastTimeTextBox = 
    let r = new TextBox(Text="timer",FontSize=42.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false)
    let timerOpacity() = 
        r.Opacity <- if TrackerModelOptions.HideTimer.Value then 0.0 else 1.0
        hmsTimeTextBox.Opacity <- if TrackerModelOptions.HideTimer.Value then 0.0 else 1.0
    timerOpacity()
    OptionsMenu.hideTimerChanged.Publish.Add(timerOpacity)
    r

// some global mutable variables needed across various UI components
let mutable displayIsCurrentlyMirrored = false
let mutable notesTextBox = null : TextBox
let mutable currentRecorderDestinationIndex = 0

let mutable hideFeatsOfStrength = fun (_b:bool) -> ()
let mutable hideRaftSpots = fun (_b:bool) -> ()

let mutable exportDungeonModelsJsonLines = fun () -> (null:string[])
let mutable legendStartIconButtonBehavior = fun () -> ()

open DungeonUI.AhhGlobalVariables
let mutable showLocatorExactLocation = fun(_x:int,_y:int) -> ()
let mutable showLocatorHintedZone = fun(_hz:TrackerModel.HintZone,_also:bool) -> ()
let mutable showLocatorInstanceFunc = fun(_f:int*int->bool) -> ()
let mutable showHintShopLocator = fun() -> ()
let mutable showLocatorPotionAndTakeAny = fun() -> ()
let mutable showLocatorRupees = fun() -> ()
let mutable showLocatorNoneFound = fun() -> ()
let mutable showLocator = fun(_sld:ShowLocatorDescriptor) -> ()


let hintBG = Graphics.freeze(new SolidColorBrush(Color.FromArgb(255uy,0uy,0uy,120uy)))
let HintZoneDisplayTextBox(s) : FrameworkElement = 
    if s="" then
        upcast new Canvas()   // don't display anything, we pass "" for HintZone.Unknown on the MainTracker, and also want to avoid hintBG from the textbox
    else
        let tb = new TextBox(Text=s, Foreground=Brushes.Orange, Background=hintBG, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                             FontSize=12., FontWeight=FontWeights.Bold, HorizontalContentAlignment=HorizontalAlignment.Center, TextAlignment=TextAlignment.Center,
                             HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center) 
        upcast Graphics.center(tb, 24, 24)
let hintyBrush = 
    let HHS,HHE = Views.HHS, Views.HHE
    let c = Color.FromRgb(HHS.R/2uy + HHE.R/2uy, HHS.G/2uy + HHE.G/2uy, HHS.B/2uy + HHE.B/2uy)
    Graphics.freeze(new SolidColorBrush(c))
let learnMoreHintDecoration =
    let img = Graphics.BMPtoImage Graphics.z1rSampleHintBMP
    let showHotKeysWidthToRightEdge = THRU_MAIN_MAP_AND_ITEM_PROGRESS_H - THRU_MAIN_MAP_H + 115.
    let w = showHotKeysWidthToRightEdge - 24.
    img.Height <- img.Height / img.Width * w
    img.Width <- w
    let sp = new StackPanel(Orientation=Orientation.Vertical)
    let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), FontSize=12., Margin=Thickness(3.,0.,0.,0.)) 
    tb.Text <- "Learn more about z1r hints via\nthe Hint Decoder button\n<--\n\nSample Hint:"
    sp.Children.Add(new DockPanel(Height=6.)) |> ignore
    sp.Children.Add(tb) |> ignore
    sp.Children.Add(new Border(BorderBrush=Brushes.DarkSlateBlue, BorderThickness=Thickness(3.), Child=img, Width=showHotKeysWidthToRightEdge-18., Margin=Thickness(6.))) |> ignore
    let b = new Border(BorderBrush=Brushes.LightGray, BorderThickness=Thickness(3.), Background=Brushes.Black, Child=sp, Width=showHotKeysWidthToRightEdge, IsHitTestVisible=false)
    b, 16.*OMTW - showHotKeysWidthToRightEdge, THRU_MAIN_MAP_H
let FastHintSelector(cm, levelHintIndex, px, py, activationDelta, shouldDescriptionGoOnLeft) = async {
    let tb = HintZoneDisplayTextBox
    let gesai(hz:TrackerModel.HintZone) = tb(hz.AsDisplayTwoChars()), true, hz
    let gridElementsSelectablesAndIDs = [|
        gesai(TrackerModel.HintZone.DEATH_MOUNTAIN)
        gesai(TrackerModel.HintZone.RIVER)
        gesai(TrackerModel.HintZone.LOST_HILLS)
        gesai(TrackerModel.HintZone.COAST)
        gesai(TrackerModel.HintZone.GRAVE)
        gesai(TrackerModel.HintZone.LAKE)
        gesai(TrackerModel.HintZone.DESERT)
        null, false, TrackerModel.HintZone.UNKNOWN
        gesai(TrackerModel.HintZone.DEAD_WOODS)
        gesai(TrackerModel.HintZone.NEAR_START)
        gesai(TrackerModel.HintZone.FOREST)
        gesai(TrackerModel.HintZone.UNKNOWN)
        |]
    let originalStateIndex = 
        let h = TrackerModel.GetLevelHint(levelHintIndex)
        gridElementsSelectablesAndIDs |> Array.findIndexBack (fun (_,_,x) -> x=h)
    let onClick(_ea,ident) = CustomComboBoxes.DismissPopupWithResult(ident)
    let tile = new Canvas(Width=24., Height=24., Background=Brushes.Black)
    let levelHintDescription = new TextBox(Text="", Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), FontSize=20.)
    let levelHintLocation    = new TextBox(Text="", Foreground=Brushes.Orange, Background=hintBG, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), FontSize=20.)
    let boxDecoration : FrameworkElement = upcast new Border(BorderBrush=hintyBrush, BorderThickness=Thickness(3.), Width=30., Height=30.)
    let descDecoration = 
        let b = new Border(Background=Brushes.Black, BorderBrush=Brushes.DarkGray, BorderThickness=Thickness(3.), Width=200., Height=112.)
        let sp = new StackPanel(Orientation=Orientation.Vertical)
        sp.Children.Add(levelHintDescription) |> ignore
        sp.Children.Add(levelHintLocation) |> ignore
        b.Child <- sp
        upcast b : FrameworkElement
    let redrawTile(hz:TrackerModel.HintZone) = 
        tile.Children.Clear()
        tile.Children.Add(HintZoneDisplayTextBox(hz.AsDisplayTwoChars())) |> ignore
        levelHintDescription.Text <- OverworldData.hintMeaningsDecriptionTextForUI.[levelHintIndex]
        levelHintLocation.Text <- HotKeys.HintZoneHotKeyProcessor.AppendHotKeyToDescription(hz.ToString(), hz)
        hideLocator()
        showLocatorHintedZone(hz, false)
    let brushes = CustomComboBoxes.ModalGridSelectBrushes(hintyBrush, Brushes.Lime, Brushes.Red, Brushes.DarkGray)
    let learnDeco, learnX, learnY = learnMoreHintDecoration
    let extraDecorations = [
        (if shouldDescriptionGoOnLeft then descDecoration, -203., 27. else descDecoration, 153., 27.)
        boxDecoration, -3., 27.
        upcast learnDeco, learnX - px, learnY - py
        ]
    let! r = CustomComboBoxes.DoModalGridSelect(cm, px, py, tile, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (4, 3, 24, 24), 
                                17., 12., 27., 27., redrawTile, onClick, extraDecorations, brushes, CustomComboBoxes.WarpToCenter, None, "FastHintSelector", Some(0.2))
    hideLocator()
    return r
    }
let ApplyFastHintSelectorBehavior(cm, (px, py), fe:FrameworkElement, i, activateOnClick, shouldDescriptionGoOnLeft) =
    fe.MouseWheel.Add(fun x ->
        if not popupIsActive then 
            popupIsActive <- true
            Graphics.SilentlyWarpMouseCursorTo(Point(px+15., py+15.))   // white/magical sword can activate scroll from icons below; just always center on box when activated
            async {
                let! r = FastHintSelector(cm, i, px+3., py+3., (if x.Delta<0 then 1 else -1), shouldDescriptionGoOnLeft)
                match r with
                | Some hz -> TrackerModel.SetLevelHint(i, hz)
                | _ -> ()
                popupIsActive <- false
            } |> Async.StartImmediate
        )
    if activateOnClick then
        fe.MouseDown.Add(fun ea ->
            if not popupIsActive then 
                popupIsActive <- true
                ea.Handled <- true
                async {
                    let! r = FastHintSelector(cm, i, px+3., py+3., 0, shouldDescriptionGoOnLeft)
                    match r with
                    | Some hz -> TrackerModel.SetLevelHint(i, hz)
                    | _ -> ()
                    popupIsActive <- false
                } |> Async.StartImmediate
            )

let MakeItemGrid(cm:CustomComboBoxes.CanvasManager, boxItemImpl, timelineItems:ResizeArray<Timeline.TimelineItem>, owInstance:OverworldData.OverworldInstance, 
                    extrasImage:Image, resetTimerEvent:Event<unit>, isStandardHyrule, doUIUpdateEvent:Event<unit>, makeManualSave) =
    let owItemGrid = makeGrid(6, 4, 30, 30)
    // ow 'take any' hearts
    for i = 0 to 3 do
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let redraw() = 
            c.Children.Clear()
            let curState = TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(i)
            if curState=0 then canvasAdd(c, Graphics.BMPtoImage(Graphics.owHeartEmpty_bmp), 0., 0.)
            elif curState=1 then canvasAdd(c, Graphics.BMPtoImage(Graphics.owHeartFull_bmp), 0., 0.)
            else canvasAdd(c, Graphics.BMPtoImage(Graphics.owHeartEmpty_bmp), 0., 0.); Graphics.placeSkippedItemXDecoration(c)
        redraw()
        TrackerModel.playerProgressAndTakeAnyHearts.TakeAnyHeartChanged.Add(fun n -> if n=i then redraw())
        let f b = TrackerModel.playerProgressAndTakeAnyHearts.SetTakeAnyHeart(i, (TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(i) + (if b then 1 else -1) + 3) % 3)
        c.MouseLeftButtonDown.Add(fun _ -> f true)
        c.MouseRightButtonDown.Add(fun _ -> f false)
        c.MouseWheel.Add(fun x -> f (x.Delta<0))
        c.MouseEnter.Add(fun _ -> showLocatorPotionAndTakeAny())
        c.MouseLeave.Add(fun _ -> hideLocator())
        Views.appMainCanvasGlobalBoxMouseOverHighlight.ApplyBehavior(c)
        let HEARTX, HEARTY = OW_ITEM_GRID_LOCATIONS.HEARTS
        gridAdd(owItemGrid, c, HEARTX+i, HEARTY)
        timelineItems.Add(new Timeline.TimelineItem(match i+1 with
                                                    | 1 -> Timeline.TimelineID.TakeAnyHeart1
                                                    | 2 -> Timeline.TimelineID.TakeAnyHeart2
                                                    | 3 -> Timeline.TimelineID.TakeAnyHeart3
                                                    | 4 -> Timeline.TimelineID.TakeAnyHeart4
                                                    | _ -> failwith "bad take any #"
                                                    , fun()->Graphics.heartFromTakeAny_bmp))
    // ladder, armos, white sword items
    let ladderBoxImpl = boxItemImpl(Timeline.TimelineID.LadderBox, TrackerModel.ladderBox, true)
    let armosBoxImpl  = boxItemImpl(Timeline.TimelineID.ArmosBox, TrackerModel.armosBox, false)
    let sword2BoxImpl = boxItemImpl(Timeline.TimelineID.WhiteSwordBox, TrackerModel.sword2Box, true)
    gridAddTuple(owItemGrid, ladderBoxImpl, OW_ITEM_GRID_LOCATIONS.LADDER_ITEM_BOX)
    gridAddTuple(owItemGrid, armosBoxImpl,  OW_ITEM_GRID_LOCATIONS.ARMOS_ITEM_BOX)
    gridAddTuple(owItemGrid, sword2BoxImpl, OW_ITEM_GRID_LOCATIONS.WHITE_SWORD_ITEM_BOX)
    let rerouteClick(fe:FrameworkElement, newDest:FrameworkElement) = fe.MouseDown.Add(fun ea -> newDest.RaiseEvent(ea)); fe
    let ladderIcon = Graphics.BMPtoImage Graphics.ladder_bmp
    gridAddTuple(owItemGrid, rerouteClick(ladderIcon, ladderBoxImpl), OW_ITEM_GRID_LOCATIONS.LADDER_ICON)
    ladderIcon.ToolTip <- "The item box to the right is for the item found off the coast, at coords F16."
    let armos = Graphics.BMPtoImage Graphics.ow_key_armos_bmp
    armos.MouseEnter.Add(fun _ -> showLocatorInstanceFunc(owInstance.HasArmos))
    armos.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, rerouteClick(armos, armosBoxImpl), OW_ITEM_GRID_LOCATIONS.ARMOS_ICON)
    armos.ToolTip <- "The item box to the right is for the item found under an Armos robot on the overworld."
    let white_sword_canvas = new Canvas(Width=30., Height=30.)
    let wsHintCanvas = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // Background to accept mouse input
    let redrawWhiteSwordCanvas(c:Canvas) =
        c.Children.Clear()
        if not(TrackerModel.IsHiddenDungeonNumbers()) then
            canvasAdd(c, wsHintCanvas, 0., -30.)
        if not(TrackerModel.playerComputedStateSummary.HaveWhiteSwordItem) &&           // don't have it yet
                TrackerModel.mapStateSummary.Sword2Location=TrackerModel.NOTFOUND &&    // have not found cave
                TrackerModel.GetLevelHint(9)<>TrackerModel.HintZone.UNKNOWN then        // have a hint
            canvasAdd(c, makeHintHighlight(21.), 4., 4.)
        canvasAdd(c, Graphics.BMPtoImage Graphics.white_sword_bmp, 4., 4.)
        Views.drawTinyIconIfLocationIsOverworldBlock(c, Some(owInstance), TrackerModel.mapStateSummary.Sword2Location)
    redrawWhiteSwordCanvas(white_sword_canvas)
    if not(TrackerModel.IsHiddenDungeonNumbers()) then
        TrackerModel.LevelHintChanged(9).Add(fun hz -> 
            wsHintCanvas.Children.Clear()
            canvasAdd(wsHintCanvas, HintZoneDisplayTextBox(if hz=TrackerModel.HintZone.UNKNOWN then "" else hz.AsDisplayTwoChars()), 3., 3.)
            redrawWhiteSwordCanvas(white_sword_canvas))
        let px,py = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.WHITE_SWORD_ICON)
        ApplyFastHintSelectorBehavior(cm, (px,py-30.), white_sword_canvas, 9, false, true)
        ApplyFastHintSelectorBehavior(cm, (px,py-30.), wsHintCanvas, 9, true, true)
        wsHintCanvas.MyKeyAdd(fun ea -> 
            match HotKeys.HintZoneHotKeyProcessor.TryGetValue(ea.Key) with
            | Some(hz) -> 
                ea.Handled <- true
                TrackerModel.SetLevelHint(9, hz)
            | _ -> ()
            )
    (*  don't need to do this, as redrawWhiteSwordCanvas() is currently called every doUIUpdate, heh
    // redraw after we can look up its new location coordinates
    let newLocation = Views.SynthesizeANewLocationKnownEvent(TrackerModel.mapSquareChoiceDomain.Changed |> Event.filter (fun (_,key) -> key=TrackerModel.MapSquareChoiceDomainHelper.SWORD2))
    newLocation.Add(fun _ -> redrawWhiteSwordCanvas())
    *)
    gridAddTuple(owItemGrid, rerouteClick(white_sword_canvas, sword2BoxImpl), OW_ITEM_GRID_LOCATIONS.WHITE_SWORD_ICON)
    white_sword_canvas.ToolTip <- "The item box to the right is for the item found in the White Sword Cave,\nwhich will be found somewhere on the overworld.\n(4-6 hearts to lift)"
    white_sword_canvas.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.Sword2))
    white_sword_canvas.MouseLeave.Add(fun _ -> hideLocator())

    let extrasCanvasGlobalBoxMouseOverHighlight = new Views.GlobalBoxMouseOverHighlight()
    // brown sword, blue candle, blue ring, magical sword
    let veryBasicBoxImpl(bmp:System.Drawing.Bitmap, timelineID, prop:TrackerModel.BoolProperty, located:TrackerModel.IEventingReader<bool>, superseded:TrackerModel.IEventingReader<bool>) =
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let no = CustomComboBoxes.no
        let yes = CustomComboBoxes.yes
        let loc = Brushes.Yellow
        let sup = CustomComboBoxes.skipped
        let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., StrokeThickness=3.0)
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has item drawn on it, not the box
        let redraw() =
            c.Children.Clear()
            c.Children.Add(rect) |> ignore
            c.Children.Add(innerc) |> ignore
            if prop.Value() then
                rect.Stroke <- yes
                if obj.Equals(TrackerModel.foundBombShop, located) && located.Value then  // special case handling for bombs
                    canvasAdd(c, new Shapes.Rectangle(Width=24., Height=24., StrokeThickness=2.0, Stroke=loc), 3., 3.)
            elif superseded.Value then
                rect.Stroke <- sup
                Graphics.placeSkippedItemXDecoration(c)
            elif located.Value then
                rect.Stroke <- loc
            else
                rect.Stroke <- no
        redraw()
        prop.Changed.Add(fun _ -> redraw())
        located.Changed.Add(fun _ -> redraw())
        superseded.Changed.Add(fun _ -> redraw())
        c.MouseDown.Add(fun _ -> prop.Toggle())
        Views.appMainCanvasGlobalBoxMouseOverHighlight.ApplyBehavior(c)
        extrasCanvasGlobalBoxMouseOverHighlight.ApplyBehavior(c)
        canvasAdd(innerc, Graphics.BMPtoImage bmp, 4., 4.)
        match timelineID with
        | Some tid -> timelineItems.Add(new Timeline.TimelineItem(tid, fun()->bmp))
        | None -> ()
        c
    let basicBoxImpl(tts, tid, img, prop, located, superseded) =
        let c = veryBasicBoxImpl(img, Some(tid), prop, located, superseded)
        c.ToolTip <- tts
        c
    let basicBoxImplNoTimeline(tts, img, prop, located, superseded) =
        let c = veryBasicBoxImpl(img, None, prop, located, superseded)
        c.ToolTip <- tts
        c
    let FALSE = TrackerModel.FALSE   // always false for e.g. boxes that never light up Yellow to say their shop is found
    let yellowWoodSwordLogic = new TrackerModel.SyntheticEventingBool((fun() -> TrackerModel.playerSwordLevel.Value=0 && TrackerModel.woodSwordCaveFound.Value), [TrackerModel.woodSwordCaveFound.Changed; TrackerModel.playerSwordLevel.Changed])
    let woodSwordSuperseded = new TrackerModel.SyntheticEventingBool((fun() -> TrackerModel.playerSwordLevel.Value>=1), [TrackerModel.playerSwordLevel.Changed])
    let wood_sword_box = basicBoxImpl("Acquired wood sword (mark timeline)", Timeline.TimelineID.WoodSword, Graphics.brown_sword_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword, yellowWoodSwordLogic, woodSwordSuperseded)    
    wood_sword_box.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.Sword1))
    wood_sword_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, wood_sword_box, OW_ITEM_GRID_LOCATIONS.WOOD_SWORD_BOX)
    let yellowArrowLogic = new TrackerModel.SyntheticEventingBool((fun() -> TrackerModel.playerArrowLevel.Value=0 && TrackerModel.foundArrowShop.Value), [TrackerModel.foundArrowShop.Changed; TrackerModel.playerArrowLevel.Changed])
    let woodArrowSuperseded = new TrackerModel.SyntheticEventingBool((fun() -> TrackerModel.playerArrowLevel.Value>=2), [TrackerModel.playerArrowLevel.Changed])
    let wood_arrow_box = basicBoxImpl("Acquired wood arrow (mark timeline)", Timeline.TimelineID.WoodArrow, Graphics.wood_arrow_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodArrow, yellowArrowLogic, woodArrowSuperseded)
    wood_arrow_box.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.ARROW))
    wood_arrow_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, wood_arrow_box, OW_ITEM_GRID_LOCATIONS.WOOD_ARROW_BOX)
    let yellowCandleLogic = new TrackerModel.SyntheticEventingBool((fun() -> TrackerModel.playerCandleLevel.Value=0 && TrackerModel.foundCandleShop.Value), [TrackerModel.foundCandleShop.Changed; TrackerModel.playerCandleLevel.Changed])
    let blueCandleSuperseded = new TrackerModel.SyntheticEventingBool((fun() -> TrackerModel.playerCandleLevel.Value>=2), [TrackerModel.playerCandleLevel.Changed])
    let blue_candle_box = basicBoxImpl("Acquired blue candle (mark timeline, affects gettables and routing)", Timeline.TimelineID.BlueCandle, Graphics.blue_candle_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle, yellowCandleLogic, blueCandleSuperseded)
    blue_candle_box.MouseEnter.Add(fun _ -> if TrackerModel.playerComputedStateSummary.CandleLevel=0 then showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BLUE_CANDLE) else showLocatorInstanceFunc(owInstance.Burnable))
    blue_candle_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, blue_candle_box, OW_ITEM_GRID_LOCATIONS.BLUE_CANDLE_BOX)
    let yellowRingLogic = new TrackerModel.SyntheticEventingBool((fun() -> TrackerModel.playerRingLevel.Value=0 && TrackerModel.foundBlueRingShop.Value), [TrackerModel.foundBlueRingShop.Changed; TrackerModel.playerRingLevel.Changed])
    let blueRingSuperseded = new TrackerModel.SyntheticEventingBool((fun() -> TrackerModel.playerRingLevel.Value>=2), [TrackerModel.playerRingLevel.Changed])
    let blue_ring_box = basicBoxImpl("Acquired blue ring (mark timeline)", Timeline.TimelineID.BlueRing, Graphics.blue_ring_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueRing, yellowRingLogic, blueRingSuperseded)
    blue_ring_box.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BLUE_RING))
    blue_ring_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, blue_ring_box, OW_ITEM_GRID_LOCATIONS.BLUE_RING_BOX)
    let mags_box = basicBoxImpl("Acquired magical sword (mark timeline)\n(10-14 hearts to lift)", Timeline.TimelineID.MagicalSword, Graphics.magical_sword_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword, TrackerModel.magsCaveFound, FALSE)
    let mags_canvas = mags_box.Children.[1] :?> Canvas // a tiny bit fragile
    let magsHintCanvas = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // Background to accept mouse input
    let redrawMagicalSwordCanvas(c:Canvas) =
        c.Children.Clear()
        if not(TrackerModel.IsHiddenDungeonNumbers()) then
            canvasAdd(c, magsHintCanvas, 0., -30.)
        if not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) &&   // dont have sword
                TrackerModel.mapStateSummary.Sword3Location=TrackerModel.NOTFOUND &&           // not yet located cave
                TrackerModel.GetLevelHint(10)<>TrackerModel.HintZone.UNKNOWN then              // have a hint
            canvasAdd(c, makeHintHighlight(21.), 4., 4.)
        canvasAdd(c, Graphics.BMPtoImage Graphics.magical_sword_bmp, 4., 4.)
    redrawMagicalSwordCanvas(mags_canvas)
    gridAddTuple(owItemGrid, mags_box, OW_ITEM_GRID_LOCATIONS.MAGS_BOX)
    if not(TrackerModel.IsHiddenDungeonNumbers()) then
        TrackerModel.LevelHintChanged(10).Add(fun hz -> 
            magsHintCanvas.Children.Clear()
            canvasAdd(magsHintCanvas, HintZoneDisplayTextBox(if hz=TrackerModel.HintZone.UNKNOWN then "" else hz.AsDisplayTwoChars()), 3., 3.)
            redrawMagicalSwordCanvas(mags_canvas))
        let px,py = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.MAGS_BOX)
        ApplyFastHintSelectorBehavior(cm, (px,py-30.), mags_canvas, 10, false, true)
        ApplyFastHintSelectorBehavior(cm, (px,py-30.), magsHintCanvas, 10, true, true)
        magsHintCanvas.MyKeyAdd(fun ea -> 
            match HotKeys.HintZoneHotKeyProcessor.TryGetValue(ea.Key) with
            | Some(hz) -> 
                ea.Handled <- true
                TrackerModel.SetLevelHint(10, hz)
            | _ -> ()
            )
    mags_box.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.Sword3))
    mags_box.MouseLeave.Add(fun _ -> hideLocator())
    // boomstick book, to mark when purchase in boomstick seed (normal book will become shield found in dungeon)
    let boom_book_box = basicBoxImpl("Purchased boomstick book (mark timeline)", Timeline.TimelineID.BoomstickBook, Graphics.boom_book_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook, TrackerModel.foundBookShop, FALSE)
    boom_book_box.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BOOK))
    boom_book_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, boom_book_box, OW_ITEM_GRID_LOCATIONS.BOOMSTICK_BOX)
    // mark the dungeon wins on timeline via ganon/zelda boxes
    gridAddTuple(owItemGrid, basicBoxImpl("Killed Gannon (mark timeline)", Timeline.TimelineID.Gannon, Graphics.ganon_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasDefeatedGanon, FALSE, FALSE), OW_ITEM_GRID_LOCATIONS.GANON_BOX)
    let zelda_box = basicBoxImpl("Rescued Zelda (mark timeline)", Timeline.TimelineID.Zelda,  Graphics.zelda_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda, FALSE, FALSE)
    gridAddTuple(owItemGrid, zelda_box,  OW_ITEM_GRID_LOCATIONS.ZELDA_BOX)
    // hover zelda to display hidden overworld icons (note that Armos/Sword2/Sword3 will not be darkened)
    zelda_box.MouseEnter.Add(fun _ -> 
        for i=0 to 15 do for j=0 to 7 do OverworldMapTileCustomization.temporarilyDisplayHiddenOverworldTileMarks.[i,j] <- true
        OptionsMenu.requestRedrawOverworldEvent.Trigger())
    zelda_box.MouseLeave.Add(fun _ -> 
        for i=0 to 15 do for j=0 to 7 do OverworldMapTileCustomization.temporarilyDisplayHiddenOverworldTileMarks.[i,j] <- false
        OptionsMenu.requestRedrawOverworldEvent.Trigger())
    // shield versus book icon (for boomstick flags/seeds)
    let toggleBookShieldCheckBox = 
        let sp = new StackPanel(Orientation=Orientation.Horizontal)
        let shieldIcon = Graphics.BMPtoImage Graphics.magic_shield_bmp
        shieldIcon.Width <- 14.
        shieldIcon.Height <- 14.
        sp.Children.Add(shieldIcon) |> ignore
        let boomBookIcon = Graphics.BMPtoImage Graphics.boom_book_bmp
        boomBookIcon.Width <- 14.
        boomBookIcon.Height <- 14.
        sp.Children.Add(boomBookIcon) |> ignore
        new CheckBox(Content=sp)
    toggleBookShieldCheckBox.ToolTip <- "Shield instead of book (in item pool, for boomstick seeds)"
    toggleBookShieldCheckBox.IsChecked <- System.Nullable.op_Implicit false
    toggleBookShieldCheckBox.Checked.Add(fun _ -> TrackerModel.ToggleIsCurrentlyBook())
    toggleBookShieldCheckBox.Unchecked.Add(fun _ -> TrackerModel.ToggleIsCurrentlyBook())
    // book is atlas
    let bookIsAtlasCheckBox = 
        let sp = new StackPanel(Orientation=Orientation.Horizontal)
        let bookIcon = Graphics.BMPtoImage Graphics.book_bmp
        bookIcon.Width <- 14.
        bookIcon.Height <- 14.
        sp.Children.Add(bookIcon) |> ignore
        let mapIcon = Graphics.BMPtoImage Graphics.zi_map_bmp
        mapIcon.Width <- 18.
        mapIcon.Height <- 18.
        let c = new Canvas(Width=16., Height=16., ClipToBounds=true)  // icon has border, canvas will clip it
        canvasAdd(c, mapIcon, -1., -1.)
        sp.Children.Add(c) |> ignore
        new CheckBox(Content=sp)
    bookIsAtlasCheckBox.ToolTip <- "Book is an Atlas (of all dungeon maps)"
    bookIsAtlasCheckBox.IsChecked <- System.Nullable.op_Implicit false
    bookIsAtlasCheckBox.Checked.Add(fun _ -> TrackerModel.ToggleBookIsAtlas())
    bookIsAtlasCheckBox.Unchecked.Add(fun _ -> TrackerModel.ToggleBookIsAtlas())
    TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Changed.Add(fun b -> 
        if b then 
            notesTextBox.Text <- notesTextBox.Text + "\n" + hmsTimeTextBox.Text
            TrackerModel.LastChangedTime.PauseAll()
            if TrackerModelOptions.SaveOnCompletion.Value && not(Timeline.isCurrentlyLoadingASave) then
                try
                    SaveAndLoad.SaveAll(notesTextBox.Text, DungeonUI.theDungeonTabControl.SelectedIndex, exportDungeonModelsJsonLines(), DungeonSaveAndLoad.SaveDrawingLayer(), 
                                        Graphics.alternativeOverworldMapFilename, Graphics.shouldInitiallyHideOverworldMap, currentRecorderDestinationIndex, 
                                        toggleBookShieldCheckBox.IsChecked.Value, bookIsAtlasCheckBox.IsChecked.Value, SaveAndLoad.FinishedSave) |> ignore
                with _e ->
                    ()
        else
            TrackerModel.LastChangedTime.ResumeAll()
        )
    // mark whether player currently has bombs, for overworld routing
    let bombIcon = veryBasicBoxImpl(Graphics.bomb_bmp, None, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBombs, TrackerModel.foundBombShop, TrackerModel.FALSE)
    bombIcon.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BOMB))
    bombIcon.MouseLeave.Add(fun _ -> hideLocator())
    bombIcon.ToolTip <- "Player currently has bombs (affects gettables and routing)"
    gridAddTuple(owItemGrid, bombIcon, OW_ITEM_GRID_LOCATIONS.BOMB_BOX)

    // hover target to look for money - highlight gambling money making game casinos, as well as unknown secrets and un-taken secret spots
    let rupeeIcon = Graphics.BMPtoImage Graphics.rupee_bmp
    rupeeIcon.MouseEnter.Add(fun _ -> showLocatorRupees())
    rupeeIcon.MouseLeave.Add(fun _ -> hideLocator())
    rupeeIcon.ToolTip <- "When hovered, highlights locations of Money Making Game,\nUnknown Secret, and not-yet-taken-Secret overworld spots"
    gridAddTuple(owItemGrid, rupeeIcon, OW_ITEM_GRID_LOCATIONS.RUPEE_ICON)

    let highlightOpenCavesCB = 
        if isStandardHyrule then
            let highlightOpenCaves = Graphics.BMPtoImage Graphics.openCaveIconBmp
            let cb = new CheckBox(Content=highlightOpenCaves, IsChecked=TrackerModelOptions.Overworld.OpenCaves.Value)
            cb.ToolTip <- "Highlight unmarked open caves (when hovered)\n" +
                "When checked, highlights all unmarked open caves until you\neither mark wood sword cave or obtain both a sword and a candle;\n" +
                "after that, highlights just the armos locations until you mark armos item."
            ToolTipService.SetPlacement(cb, System.Windows.Controls.Primitives.PlacementMode.Top)
            ToolTipService.SetShowDuration(cb, 15000)
            cb.MouseEnter.Add(fun _ -> showLocatorInstanceFunc(owInstance.Nothingable))
            cb.MouseLeave.Add(fun _ -> hideLocator())
            cb.Checked.Add(fun _ -> TrackerModelOptions.Overworld.OpenCaves.Value <- true; TrackerModelOptions.writeSettings())
            cb.Unchecked.Add(fun _ -> TrackerModelOptions.Overworld.OpenCaves.Value <- false; TrackerModelOptions.writeSettings())
            cb
        else
            null

    // these panels need to be created once, at startup time, as they have side effects that populate the timelineItems set
    let weaponsRowPanel = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    weaponsRowPanel.Children.Add(basicBoxImplNoTimeline("Wood sword", Graphics.brown_sword_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword, FALSE, FALSE)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("White sword", Timeline.TimelineID.WhiteSword, Graphics.white_sword_bmp, TrackerModel.startingItemsAndExtras.PlayerHasWhiteSword, FALSE, FALSE)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImplNoTimeline("Magical sword", Graphics.magical_sword_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword, FALSE, FALSE)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImplNoTimeline("Wood arrow", Graphics.wood_arrow_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodArrow, FALSE, FALSE)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Silver arrow", Timeline.TimelineID.SilverArrow, Graphics.silver_arrow_bmp, TrackerModel.startingItemsAndExtras.PlayerHasSilverArrow, FALSE, FALSE)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Bow", Timeline.TimelineID.Bow, Graphics.bow_bmp, TrackerModel.startingItemsAndExtras.PlayerHasBow, FALSE, FALSE)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Wand", Timeline.TimelineID.Wand, Graphics.wand_bmp, TrackerModel.startingItemsAndExtras.PlayerHasWand, FALSE, FALSE)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImplNoTimeline("Blue candle", Graphics.blue_candle_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle, FALSE, FALSE)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Red candle", Timeline.TimelineID.RedCandle, Graphics.red_candle_bmp, TrackerModel.startingItemsAndExtras.PlayerHasRedCandle, FALSE, FALSE)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Boomerang", Timeline.TimelineID.Boomerang, Graphics.boomerang_bmp, TrackerModel.startingItemsAndExtras.PlayerHasBoomerang, FALSE, FALSE)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Magic boomerang", Timeline.TimelineID.MagicBoomerang, Graphics.magic_boomerang_bmp, TrackerModel.startingItemsAndExtras.PlayerHasMagicBoomerang, FALSE, FALSE)) |> ignore
    let utilityRowPanel = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    utilityRowPanel.Children.Add(basicBoxImplNoTimeline("Blue ring", Graphics.blue_ring_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueRing, FALSE, FALSE)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Red ring", Timeline.TimelineID.RedRing, Graphics.red_ring_bmp, TrackerModel.startingItemsAndExtras.PlayerHasRedRing, FALSE, FALSE)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Power bracelet", Timeline.TimelineID.PowerBracelet, Graphics.power_bracelet_bmp, TrackerModel.startingItemsAndExtras.PlayerHasPowerBracelet, FALSE, FALSE)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Ladder", Timeline.TimelineID.Ladder, Graphics.ladder_bmp, TrackerModel.startingItemsAndExtras.PlayerHasLadder, FALSE, FALSE)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Raft", Timeline.TimelineID.Raft, Graphics.raft_bmp, TrackerModel.startingItemsAndExtras.PlayerHasRaft, FALSE, FALSE)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Recorder", Timeline.TimelineID.Recorder, Graphics.recorder_bmp, TrackerModel.startingItemsAndExtras.PlayerHasRecorder, FALSE, FALSE)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Magic key", Timeline.TimelineID.AnyKey, Graphics.key_bmp, TrackerModel.startingItemsAndExtras.PlayerHasAnyKey, FALSE, FALSE)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Book", Timeline.TimelineID.Book, Graphics.book_bmp, TrackerModel.startingItemsAndExtras.PlayerHasBook, FALSE, FALSE)) |> ignore
    let mutable extrasPanelAndepRefresh = None
    let makeExtrasPanelAndepRefresh() =
        let mutable refreshTDD = fun () -> ()
        let mkTxt(size,txt) = 
            new TextBox(IsHitTestVisible=false, BorderThickness=Thickness(0.), FontSize=size, Margin=Thickness(5.),
                            VerticalContentAlignment=VerticalAlignment.Center, HorizontalContentAlignment=HorizontalAlignment.Center, 
                            Text=txt, Foreground=Brushes.Orange, Background=Brushes.Black)
        let leftPanel = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
        let headerDescription1 = mkTxt(20., "Starting Items and Extra Drops")
        let iconedHeader = new StackPanel(Orientation=Orientation.Horizontal)
        iconedHeader.Children.Add(Graphics.BMPtoImage Graphics.iconExtras_bmp) |> ignore
        iconedHeader.Children.Add(headerDescription1) |> ignore
        let headerDescription2 = mkTxt(16., "Mark any items you start the game with\nor get as monster drops/extra dungeon drops\nin this section")
        leftPanel.Children.Add(iconedHeader) |> ignore
        leftPanel.Children.Add(headerDescription2) |> ignore
        leftPanel.Children.Add(new DockPanel(Height=10.)) |> ignore
        let triforcePanel = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
        for i = 0 to 7 do
            let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Black)
            let redraw() =
                innerc.Children.Clear()
                if TrackerModel.GetTriforceHaves().[i] then
                    innerc.Children.Add(Graphics.BMPtoImage(Graphics.fullNumberedFoundTriforce_bmps.[i])) |> ignore 
                else
                    innerc.Children.Add(Graphics.BMPtoImage(Graphics.emptyFoundNumberedTriforce_bmps.[i])) |> ignore 
            redraw()
            if TrackerModel.IsHiddenDungeonNumbers() then
                for j = 0 to 7 do
                    TrackerModel.GetDungeon(j).PlayerHasTriforceChanged.Add(fun _ -> redraw(); refreshTDD())
                    TrackerModel.GetDungeon(j).HiddenDungeonColorOrLabelChanged.Add(fun _ -> redraw(); refreshTDD())
            else
                TrackerModel.GetDungeon(i).PlayerHasTriforceChanged.Add(fun _ -> redraw(); refreshTDD())
            innerc.MouseDown.Add(fun _ -> 
                if TrackerModel.IsHiddenDungeonNumbers() then
                    TrackerModel.startingItemsAndExtras.HDNStartingTriforcePieces.[i].Toggle()
                else
                    TrackerModel.GetDungeon(i).ToggleTriforce()
                redraw()
                refreshTDD()
                )
            extrasCanvasGlobalBoxMouseOverHighlight.ApplyBehavior(innerc)
            triforcePanel.Children.Add(innerc) |> ignore
        leftPanel.Children.Add(triforcePanel) |> ignore
        leftPanel.Children.Add(new DockPanel(Height=10.)) |> ignore
        leftPanel.Children.Add(weaponsRowPanel) |> ignore
        leftPanel.Children.Add(new DockPanel(Height=10.)) |> ignore
        leftPanel.Children.Add(utilityRowPanel) |> ignore
        let maxHeartsPanel = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
        let maxHeartsText = mkTxt(12., sprintf "Max Hearts: %d" TrackerModel.playerComputedStateSummary.PlayerHearts)
        let adjustText = mkTxt(12., " You can adjust hearts here:")
        let plusOne = new Button(Content=" +1 ")
        let minusOne = new Button(Content=" -1 ")
        plusOne.Click.Add(fun _ -> 
            TrackerModel.startingItemsAndExtras.MaxHeartsDifferential <- TrackerModel.startingItemsAndExtras.MaxHeartsDifferential + 1
            TrackerModel.recomputePlayerStateSummary()
            maxHeartsText.Text <- sprintf "Max Hearts: %d" TrackerModel.playerComputedStateSummary.PlayerHearts
            )
        minusOne.Click.Add(fun _ -> 
            TrackerModel.startingItemsAndExtras.MaxHeartsDifferential <- TrackerModel.startingItemsAndExtras.MaxHeartsDifferential - 1
            TrackerModel.recomputePlayerStateSummary()
            maxHeartsText.Text <- sprintf "Max Hearts: %d" TrackerModel.playerComputedStateSummary.PlayerHearts
            )
        maxHeartsPanel.Children.Add(maxHeartsText) |> ignore
        maxHeartsPanel.Children.Add(adjustText) |> ignore
        maxHeartsPanel.Children.Add(plusOne) |> ignore
        maxHeartsPanel.Children.Add(minusOne) |> ignore
        leftPanel.Children.Add(new DockPanel(Height=10.)) |> ignore
        leftPanel.Children.Add(maxHeartsPanel) |> ignore
        let b = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(4.), Background=Brushes.Black, Child=leftPanel)
        b.MouseDown.Add(fun ea -> ea.Handled <- true)
        let bp = new StackPanel(Orientation=Orientation.Vertical)
        bp.Children.Add(b) |> ignore
        
        let refText = mkTxt(12., "Vanilla dungeon item reference\nDon't click here")
        let bottomPanel = new StackPanel(Orientation=Orientation.Vertical, Opacity=0.6)
        bottomPanel.Children.Add(refText) |> ignore
        let q1 = new StackPanel(Orientation=Orientation.Horizontal)
        q1.Children.Add(mkTxt(20., "1Q")) |> ignore
        q1.Children.Add(Graphics.BMPtoImage Graphics.firstQuestItemReferenceBMP) |> ignore
        bottomPanel.Children.Add(q1) |> ignore
        let q2 = new StackPanel(Orientation=Orientation.Horizontal)
        q2.Children.Add(mkTxt(20., "2Q")) |> ignore
        q2.Children.Add(Graphics.BMPtoImage Graphics.secondQuestItemReferenceBMP) |> ignore
        bottomPanel.Children.Add(q2) |> ignore
        bp.Children.Add(new DockPanel(Height=12.)) |> ignore
        let b = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(4.), Background=Brushes.Black, Child=bottomPanel)
        b.MouseDown.Add(fun ea -> ea.Handled <- true)
        bp.Children.Add(b) |> ignore
        
        let spacer = new DockPanel(Width=30.)
        let panel = new StackPanel(Orientation=Orientation.Horizontal)
        refreshTDD <- fun () ->
            panel.Children.Clear()
            panel.Children.Add(bp) |> ignore
            panel.Children.Add(spacer) |> ignore
            let tdd = Dungeon.MakeTriforceDecoderDiagram()
            tdd.MouseDown.Add(fun ea -> ea.Handled <- true)
            panel.Children.Add(tdd) |> ignore
        refreshTDD()
        let refresh() =
            maxHeartsText.Text <- sprintf "Max Hearts: %d" TrackerModel.playerComputedStateSummary.PlayerHearts
        panel, refresh
    let watchForPopups = ref false
    cm.AfterCreatePopupCanvas.Add(fun pc -> if !watchForPopups then extrasCanvasGlobalBoxMouseOverHighlight.AttachToGlobalCanvas(pc))
    cm.BeforeDismissPopupCanvas.Add(fun pc -> if !watchForPopups then extrasCanvasGlobalBoxMouseOverHighlight.DetachFromGlobalCanvas(pc))
    let invokeExtras = async {
        let wh = new System.Threading.ManualResetEvent(false)
        let whole = new Canvas(Width=cm.Width, Height=cm.Height)
        let mouseClickInterceptor = new Canvas(Width=cm.Width, Height=cm.Height, Background=Brushes.Black, Opacity=0.01)
        whole.Children.Add(mouseClickInterceptor) |> ignore
        if extrasPanelAndepRefresh.IsNone then
            extrasPanelAndepRefresh <- Some(makeExtrasPanelAndepRefresh())  // created on-demand, to improve app startup time
        let extrasPanel, epRefresh = extrasPanelAndepRefresh.Value
        epRefresh()
        whole.Children.Add(extrasPanel) |> ignore
        mouseClickInterceptor.MouseDown.Add(fun _ -> wh.Set() |> ignore)  // if they click outside the two interior panels that swallow clicks, dismiss it
        watchForPopups := true
        do! CustomComboBoxes.DoModal(cm, wh, 20., 155., whole)
        watchForPopups := false
        whole.Children.Clear() // to reparent extrasPanel again next popup
        } 
    extrasImage.MouseDown.Add(fun _ -> 
            async {
                if not popupIsActive then
                    popupIsActive <- true
                    do! invokeExtras
                    popupIsActive <- false
            } |> Async.StartImmediate
        )

    // timer reset
    let timerResetButton = Graphics.makeButton("Pause/Reset timer", Some(16.), Some(Brushes.Orange))
    timerResetButton.Click.Add(fun _ ->
        if not popupIsActive then
            popupIsActive <- true
            SaveAndLoad.MaybePollSeedAndFlags()
            let resumeButton = Graphics.makeButton("Timer has been Paused.\nClick here to Resume.\n(Look below for Reset info.)", Some(16.), Some(Brushes.Orange))
            let restartTimerButton = Graphics.makeButton("Timer has been Paused.\nClick here to confirm you want to\nReset the TIMER to 0:00:00.", Some(16.), Some(Brushes.Orange))
            let resetTrackerButton = Graphics.makeButton("Click here to Reset the TRACKER,\n(remove inventory but preserve maps)\nfor groundhog/routers/4+4 purposes.", Some(16.), Some(Brushes.Orange))
            let shutdownAndRestartButton = Graphics.makeButton("Click here to close Z-Tracker,\nand restart the app. All your\ncurrent work will be discarded!", Some(16.), Some(Brushes.Orange))
            let mutable userPressedReset = false
            let sp = new StackPanel(Orientation=Orientation.Vertical)
            let hsp1 = new StackPanel(Orientation=Orientation.Horizontal)
            hsp1.Children.Add(resumeButton) |> ignore
            hsp1.Children.Add(new DockPanel(Width=50.)) |> ignore
            hsp1.Children.Add(shutdownAndRestartButton) |> ignore
            sp.Children.Add(hsp1) |> ignore
            sp.Children.Add(new DockPanel(Height=300.)) |> ignore
            let hsp2 = new StackPanel(Orientation=Orientation.Horizontal)
            hsp2.Children.Add(restartTimerButton) |> ignore
            resumeButton.HorizontalAlignment <- HorizontalAlignment.Left
            resumeButton.Width <- 370.
            restartTimerButton.Width <- 370.
            hsp2.Children.Add(new DockPanel(Width=50.)) |> ignore
            hsp2.Children.Add(resetTrackerButton) |> ignore
            sp.Children.Add(hsp2) |> ignore
            let wh = new System.Threading.ManualResetEvent(false)
            resumeButton.Click.Add(fun _ ->
                wh.Set() |> ignore
                )
            restartTimerButton.Click.Add(fun _ ->
                resetTimerEvent.Trigger()
                userPressedReset <- true
                // In addition to just resetting the timer, the user clicking 'Reset' should zero out OverworldSpotsRemainingOverTime and Timeline data, for e.g. scenario
                // where you repeatedly play flags where you start with map knowledge and/or items, so that the graph and timeline display all this at time 0.
                // Note: the resetTimerEvent is only about the 0:00:00 timer, for example, after loading data off disk, it fires that event, since loading takes several seconds,
                // so only this user-activated section of code (and not the event) should reset OverworldSpotsRemainingOverTime/Timeline data.
                TrackerModel.timelineDataOverworldSpotsRemain.Clear()
                TrackerModel.timelineDataOverworldSpotsRemain.Add(0, TrackerModel.mapStateSummary.OwSpotsRemain)
                for (KeyValue(_,v)) in TrackerModel.TimelineItemModel.All do
                    v.ResetTotalSeconds()
                TrackerModel.TimelineItemModel.TriggerTimelineChanged()  // redraw
                wh.Set() |> ignore
                )
            restartTimerButton.MyKeyAdd(fun ea ->
                match HotKeys.GlobalHotKeyProcessor.TryGetValue(ea.Key) with
                | Some(hotKeyedState) -> 
                    ea.Handled <- true
                    match hotKeyedState with
                    | HotKeys.GlobalHotkeyTargets.LeftClick          -> Graphics.Win32.LeftMouseClick();
                    | _ -> ()
                | _ -> ()
            )
            resetTrackerButton.Click.Add(fun _ ->
                async {
                    try
                        // make hard save
                        let filename = makeManualSave()
                        let filename = System.IO.Path.GetFileName(filename)  // remove directory info (could have username in path, don't display PII on-screen)
                        let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Information, sprintf "Z-Tracker data saved to file\n%s\nThe tracker will now be reset." filename, ["Ok"])
                        ignore r
                        // remove triforces
                        for i = 0 to 7 do
                            let dung = TrackerModel.GetDungeon(i)
                            if dung.PlayerHasTriforce() then
                                dung.ToggleTriforce()
                        // remove (red-ify) all items (keep skipped as marked)
                        for b in TrackerModel.DungeonTrackerInstance.TheDungeonTrackerInstance.AllBoxes() do
                            if b.PlayerHas() <> TrackerModel.PlayerHas.SKIPPED then
                                b.SetPlayerHas(TrackerModel.PlayerHas.NO)
                        // clear mags/shop items and the take-any heart boxes
                        TrackerModel.playerProgressAndTakeAnyHearts.ResetAll()
                        // secrets reset to bright green, take-anys to bright red heart, letter bright, wood sword bright
                        let toBrighten = [| 
                            TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY
                            TrackerModel.MapSquareChoiceDomainHelper.SWORD1
                            TrackerModel.MapSquareChoiceDomainHelper.LARGE_SECRET
                            TrackerModel.MapSquareChoiceDomainHelper.MEDIUM_SECRET
                            TrackerModel.MapSquareChoiceDomainHelper.SMALL_SECRET
                            TrackerModel.MapSquareChoiceDomainHelper.THE_LETTER
                            |]
                        for i = 0 to 15 do
                            for j = 0 to 7 do
                                let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                                if toBrighten |> Array.exists (fun x -> x = cur) then
                                    let bright = if cur = TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY || cur = TrackerModel.MapSquareChoiceDomainHelper.SWORD1 then 0 else cur
                                    TrackerModel.setOverworldMapExtraData(i,j,cur,bright)
                        // not clear blockers (maybe 4+4 and were keyblocked still)
                        // dungeon maps (make darkened floor drops become bright again)
                        DungeonUI.AhhGlobalVariables.resetDungeonsForRouters()
                        // make reminders play again
                        TrackerModel.ResetForGroundhogOrRoutersOrFourPlusFourEtc()
                        // redraw UI
                        TrackerModel.forceUpdate()
                        doUIUpdateEvent.Trigger()
                        wh.Set() |> ignore
                    with e ->
                        let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Error, sprintf "Z-Tracker was unable to save the\ntracker state to a file\nError:\n%s" e.Message, ["Ok"])
                        ignore r
                } |> Async.StartImmediate)
            shutdownAndRestartButton.Click.Add(fun _ ->
                async {
                    let restartText = "Discard all my work"
                    let! r = CustomComboBoxes.DoModalMessageBoxCore(cm, System.Drawing.SystemIcons.Warning, "You are about to shutdown and restart the application.", 
                                                                        [restartText; "Wait! Take me back!"], 100., 300.)
                    if r = restartText then
                        Graphics.RestartTheApplication()
                    } |> Async.StartImmediate
                )
            async {
                TrackerModel.LastChangedTime.PauseAll()
                do! CustomComboBoxes.DoModal(cm, wh, 50., 200., sp)
                TrackerModel.LastChangedTime.ResumeAll()
                popupIsActive <- false
                if userPressedReset then
                    if (TrackerModel.startIconX,TrackerModel.startIconY) = TrackerModel.NOTFOUND then  // don't re-ask if already placed, e.g. known start, 4+4, groundhog, etc
                        legendStartIconButtonBehavior()  // jump into the 'place the start spot' popup
                } |> Async.StartImmediate
        )
    // spot summary
    let spotSummaryTB = new Border(Child=new TextBox(Text="Spot Summary", FontSize=16., IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), Foreground=Brushes.Orange, Background=Brushes.Black), 
                                    BorderThickness=Thickness(1.), IsHitTestVisible=true, Background=Brushes.Black)
    let spotSummaryCanvas = new Canvas()
    if isStandardHyrule then   // Spot Summary only makes sense in standard map
        spotSummaryTB.MouseEnter.Add(fun _ ->
            spotSummaryCanvas.Children.Clear()
            spotSummaryCanvas.Children.Add(OverworldMapTileCustomization.MakeRemainderSummaryDisplay()) |> ignore
            )   
        spotSummaryTB.MouseLeave.Add(fun _ -> spotSummaryCanvas.Children.Clear())
        
    white_sword_canvas, mags_canvas, redrawWhiteSwordCanvas, redrawMagicalSwordCanvas, spotSummaryCanvas, invokeExtras,
        owItemGrid, toggleBookShieldCheckBox, bookIsAtlasCheckBox, highlightOpenCavesCB, timerResetButton, spotSummaryTB

let mutable hideFirstQuestFromMixed = fun _b -> ()
let mutable hideSecondQuestFromMixed = fun _b -> ()

let MakeFQSQStuff(cm, isMixed, owLocatorTilesZone:Graphics.TileHighlightRectangle[,], redrawOWCircle) =
    let thereAreMarks(questOnly:string[]) =
        let mutable r = false
        for x = 0 to 15 do 
            for y = 0 to 7 do
                if questOnly.[y].Chars(x) = 'X' && MapStateProxy(TrackerModel.overworldMapMarks.[x,y].Current()).IsInteresting then
                    r <- true
        r
    let highlight(questOnly:string[], warn) =
        for x = 0 to 15 do 
            for y = 0 to 7 do
                if questOnly.[y].Chars(x) = 'X' then
                    if warn && MapStateProxy(TrackerModel.overworldMapMarks.[x,y].Current()).IsInteresting then
                        owLocatorTilesZone.[x,y].MakeRedWithBriefAnimation()
                    else
                        owLocatorTilesZone.[x,y].MakeYellowWithBriefAnimation()
    let showVanilla(first) =
        for x = 0 to 15 do 
            for y = 0 to 7 do
                let locs = if first then OverworldData.vanilla1QDungeonLocations else OverworldData.vanilla2QDungeonLocations
                if locs |> Seq.contains(x,y) then
                    owLocatorTilesZone.[x,y].MakeGreenWithBriefAnimation()
    let clearOW() = DungeonUI.AhhGlobalVariables.hideLocator(); OverworldRouteDrawing.routeDrawingLayer.Clear()

    // in mixed quest, buttons to hide first/second quest
    let hideFirstQuestCheckBox  = new CheckBox(Content=new TextBox(Text="HFQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false))
    hideFirstQuestCheckBox.ToolTip <- "Hide First Quest\nIn a mixed quest overworld tracker, shade out the first-quest-only spots.\nUseful if you're unsure if randomizer flags are mixed quest or second quest.\nCan't be used if you've marked a first-quest-only spot as having something."
    ToolTipService.SetShowDuration(hideFirstQuestCheckBox, 12000)
    let hideSecondQuestCheckBox = new CheckBox(Content=new TextBox(Text="HSQ",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false))
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
    
    let M = Thickness(2.)
    let moreFQSQoptionsButton = Graphics.makeButton("FQ/SQ...", Some(12.), Some(Brushes.Orange))
    let mkTxt(text) = new TextBox(Text=text, FontSize=16., IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), Foreground=Brushes.Orange, Background=Brushes.Black)
    moreFQSQoptionsButton.Click.Add(fun _ ->
        if not popupIsActive then
            popupIsActive <- true
            async {
                let wh = new System.Threading.ManualResetEvent(false)
                let sp = new StackPanel(Orientation=Orientation.Vertical)
                sp.Children.Add(mkTxt("Warning! These actions cannot be undone! Don't click unless you are sure!")) |> ignore
                let hsp = new StackPanel(Orientation=Orientation.Horizontal)
                sp.Children.Add(hsp) |> ignore
                let left = new StackPanel(Orientation=Orientation.Vertical)
                let right = new StackPanel(Orientation=Orientation.Vertical)
                hsp.Children.Add(left) |> ignore
                hsp.Children.Add(right) |> ignore
                if isMixed then
                    let erase1ok = not(thereAreMarks(OverworldData.owMapSquaresFirstQuestOnly))
                    let erase2ok = not(thereAreMarks(OverworldData.owMapSquaresSecondQuestOnly))
                    let color1 = if not(erase1ok) then Brushes.Red else Brushes.Yellow
                    let clearFQbutton = Graphics.makeButton("Mark all First Quest Only spots as Don't Care.\nI am certain this is Second Quest.", Some(16.), Some(color1))
                    clearFQbutton.Margin <- M
                    clearFQbutton.MouseEnter.Add(fun _ -> highlight(OverworldData.owMapSquaresFirstQuestOnly, true))
                    clearFQbutton.MouseLeave.Add(fun _ -> clearOW())
                    clearFQbutton.Click.Add(fun _ ->
                        for x = 0 to 15 do 
                            for y = 0 to 7 do
                                if OverworldData.owMapSquaresFirstQuestOnly.[y].Chars(x) = 'X' then
                                    let cell = TrackerModel.overworldMapMarks.[x,y]
                                    cell.AttemptToSet(TrackerModel.MapSquareChoiceDomainHelper.DARK_X) |> ignore
                        OptionsMenu.requestRedrawOverworldEvent.Trigger()
                        wh.Set() |> ignore
                        )
                    right.Children.Add(clearFQbutton) |> ignore
                    let color2 = if not(erase2ok) then Brushes.Red else Brushes.Yellow
                    let clearSQbutton = Graphics.makeButton("Mark all Second Quest Only spots as Don't Care.\nI am certain this is First Quest.", Some(16.), Some(color2))
                    clearSQbutton.Margin <- M
                    clearSQbutton.MouseEnter.Add(fun _ -> highlight(OverworldData.owMapSquaresSecondQuestOnly, true))
                    clearSQbutton.MouseLeave.Add(fun _ -> clearOW())
                    clearSQbutton.Click.Add(fun _ ->
                        for x = 0 to 15 do 
                            for y = 0 to 7 do
                                if OverworldData.owMapSquaresSecondQuestOnly.[y].Chars(x) = 'X' then
                                    let cell = TrackerModel.overworldMapMarks.[x,y]
                                    cell.AttemptToSet(TrackerModel.MapSquareChoiceDomainHelper.DARK_X) |> ignore
                        OptionsMenu.requestRedrawOverworldEvent.Trigger()
                        wh.Set() |> ignore
                        )
                    right.Children.Add(clearSQbutton) |> ignore
                else
                    let txt = mkTxt("(The buttons that would\notherwise appear in this space\nonly apply to Mixed Quest\noverworlds.)")
                    txt.Foreground <- Brushes.DarkGray
                    txt.Margin <- Thickness(50.,10.,0.,0.)
                    right.Children.Add(txt) |> ignore
                let mark1QdungeonLocationsButton = Graphics.makeButton("Mark vanilla First Quest\ndungeon locations.", Some(16.), Some(Brushes.Orange))
                mark1QdungeonLocationsButton.Margin <- M
                mark1QdungeonLocationsButton.MouseEnter.Add(fun _ -> showVanilla(true))
                mark1QdungeonLocationsButton.MouseLeave.Add(fun _ -> clearOW())
                mark1QdungeonLocationsButton.Click.Add(fun _ ->
                    for i = 0 to 8 do
                        let x,y = OverworldData.vanilla1QDungeonLocations.[i]
                        TrackerModel.overworldMapCircles.[x,y] <- 149+i
                        (!redrawOWCircle)(x,y)
                    wh.Set() |> ignore
                    )
                left.Children.Add(mark1QdungeonLocationsButton) |> ignore
                let mark2QdungeonLocationsButton = Graphics.makeButton("Mark vanilla Second Quest\ndungeon locations.", Some(16.), Some(Brushes.Orange))
                mark2QdungeonLocationsButton.Margin <- M
                mark2QdungeonLocationsButton.MouseEnter.Add(fun _ -> showVanilla(false))
                mark2QdungeonLocationsButton.MouseLeave.Add(fun _ -> clearOW())
                mark2QdungeonLocationsButton.Click.Add(fun _ ->
                    for i = 0 to 8 do
                        let x,y = OverworldData.vanilla2QDungeonLocations.[i]
                        TrackerModel.overworldMapCircles.[x,y] <- 249+i
                        (!redrawOWCircle)(x,y)
                    wh.Set() |> ignore
                    )
                left.Children.Add(mark2QdungeonLocationsButton) |> ignore
                let b = new Border(Child=sp, BorderBrush=Brushes.Gray, BorderThickness=Thickness(5.), Background=Brushes.Black)
                clearOW()
                do! CustomComboBoxes.DoModalCore(cm, wh, (fun (c,e) -> canvasAdd(c, e, 10., 10.)), (fun (c,e) -> c.Children.Remove(e)), b, 0.25)
                popupIsActive <- false
            } |> Async.StartImmediate
        )
    
    hideFirstQuestCheckBox, hideSecondQuestCheckBox, moreFQSQoptionsButton
