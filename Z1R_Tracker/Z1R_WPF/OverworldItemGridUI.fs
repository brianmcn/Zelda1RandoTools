module OverworldItemGridUI

// the top-right portion of the main UI

open System.Windows
open System.Windows.Controls 
open System.Windows.Media

open OverworldMapTileCustomization

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
let START_DUNGEON_AND_NOTES_AREA_H = THRU_MAIN_MAP_AND_ITEM_PROGRESS_H
let THRU_DUNGEON_AND_NOTES_AREA_H = START_DUNGEON_AND_NOTES_AREA_H + float(TH + 30 + (3 + 27*8 + 12*7 + 3) + 3)  // 3 is for a little blank space after this but before timeline
let START_TIMELINE_H = THRU_DUNGEON_AND_NOTES_AREA_H
let THRU_TIMELINE_H = START_TIMELINE_H + float TCH
let LEFT_OFFSET = 78.0
let BLOCKERS_AND_NOTES_OFFSET = 408. + 42.  // dungeon area and side-tracker-panel
let ITEM_PROGRESS_FIRST_ITEM = 130.
let hmsTimeTextBox = new TextBox(Text="timer",FontSize=42.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false)
let broadcastTimeTextBox = new TextBox(Text="timer",FontSize=42.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false)

// some global mutable variables needed across various UI components
let mutable popupIsActive = false

let mutable displayIsCurrentlyMirrored = false
let mutable notesTextBox = null : TextBox
let mutable isCurrentlyLoadingASave = false
let mutable currentRecorderDestinationIndex = 0

let mutable hideFeatsOfStrength = fun (_b:bool) -> ()
let mutable hideRaftSpots = fun (_b:bool) -> ()

let mutable exportDungeonModelsJsonLines = fun () -> null
let mutable legendStartIconButtonBehavior = fun () -> ()

let mutable showLocatorExactLocation = fun(_x:int,_y:int) -> ()
let mutable showLocatorHintedZone = fun(_hz:TrackerModel.HintZone,_also:bool) -> ()
let mutable showLocatorInstanceFunc = fun(_f:int*int->bool) -> ()
let mutable showShopLocatorInstanceFunc = fun(_item:int) -> ()
let mutable showLocatorPotionAndTakeAny = fun() -> ()
let mutable showLocatorNoneFound = fun() -> ()
let mutable showLocator = fun(_sld:ShowLocatorDescriptor) -> ()
let mutable hideLocator = fun() -> ()

let MakeItemGrid(cm:CustomComboBoxes.CanvasManager, boxItemImpl, timelineItems:ResizeArray<Timeline.TimelineItem>, owInstance:OverworldData.OverworldInstance, 
                    extrasImage:Image, resetTimerEvent:Event<unit>) =
    let appMainCanvas = cm.AppMainCanvas
    let owItemGrid = makeGrid(6, 4, 30, 30)
    canvasAdd(appMainCanvas, owItemGrid, OW_ITEM_GRID_LOCATIONS.OFFSET, 30.)
    // ow 'take any' hearts
    for i = 0 to 3 do
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let redraw() = 
            c.Children.Clear()
            let curState = TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(i)
            if curState=0 then canvasAdd(c, Graphics.BMPtoImage(Graphics.owHeartEmpty_bmp), 0., 0.)
            elif curState=1 then canvasAdd(c, Graphics.BMPtoImage(Graphics.owHeartFull_bmp), 0., 0.)
            else canvasAdd(c, Graphics.BMPtoImage(Graphics.owHeartEmpty_bmp), 0., 0.); CustomComboBoxes.placeSkippedItemXDecoration(c)
        redraw()
        TrackerModel.playerProgressAndTakeAnyHearts.TakeAnyHeartChanged.Add(fun n -> if n=i then redraw())
        let f b = TrackerModel.playerProgressAndTakeAnyHearts.SetTakeAnyHeart(i, (TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(i) + (if b then 1 else -1) + 3) % 3)
        c.MouseLeftButtonDown.Add(fun _ -> f true)
        c.MouseRightButtonDown.Add(fun _ -> f false)
        c.MouseWheel.Add(fun x -> f (x.Delta<0))
        c.MouseEnter.Add(fun _ -> showLocatorPotionAndTakeAny())
        c.MouseLeave.Add(fun _ -> hideLocator())
        let HEARTX, HEARTY = OW_ITEM_GRID_LOCATIONS.HEARTS
        gridAdd(owItemGrid, c, HEARTX+i, HEARTY)
        timelineItems.Add(new Timeline.TimelineItem(sprintf "TakeAnyHeart%d" (i+1), fun()->Graphics.owHeartFull_bmp))
    // ladder, armos, white sword items
    let ladderBoxImpl = boxItemImpl("LadderBox", TrackerModel.ladderBox, true)
    let armosBoxImpl  = boxItemImpl("ArmosBox", TrackerModel.armosBox, false)
    let sword2BoxImpl = boxItemImpl("WhiteSwordBox", TrackerModel.sword2Box, true)
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
    let redrawWhiteSwordCanvas(c:Canvas) =
        c.Children.Clear()
        if not(TrackerModel.playerComputedStateSummary.HaveWhiteSwordItem) &&           // don't have it yet
                TrackerModel.mapStateSummary.Sword2Location=TrackerModel.NOTFOUND &&    // have not found cave
                TrackerModel.GetLevelHint(9)<>TrackerModel.HintZone.UNKNOWN then        // have a hint
            canvasAdd(c, makeHintHighlight(21.), 4., 4.)
        canvasAdd(c, Graphics.BMPtoImage Graphics.white_sword_bmp, 4., 4.)
        Views.drawTinyIconIfLocationIsOverworldBlock(c, Some(owInstance), TrackerModel.mapStateSummary.Sword2Location)
    redrawWhiteSwordCanvas(white_sword_canvas)
    (*  don't need to do this, as redrawWhiteSwordCanvas() is currently called every doUIUpdate, heh
    // redraw after we can look up its new location coordinates
    let newLocation = Views.SynthesizeANewLocationKnownEvent(TrackerModel.mapSquareChoiceDomain.Changed |> Event.filter (fun (_,key) -> key=TrackerModel.MapSquareChoiceDomainHelper.SWORD2))
    newLocation.Add(fun _ -> redrawWhiteSwordCanvas())
    *)
    gridAddTuple(owItemGrid, rerouteClick(white_sword_canvas, sword2BoxImpl), OW_ITEM_GRID_LOCATIONS.WHITE_SWORD_ICON)
    white_sword_canvas.ToolTip <- "The item box to the right is for the item found in the White Sword Cave, which will be found somewhere on the overworld."
    white_sword_canvas.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.Sword2))
    white_sword_canvas.MouseLeave.Add(fun _ -> hideLocator())

    // brown sword, blue candle, blue ring, magical sword
    let veryBasicBoxImpl(bmp:System.Drawing.Bitmap, timelineID, prop:TrackerModel.BoolProperty) =
        let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
        let no = CustomComboBoxes.no
        let yes = CustomComboBoxes.yes
        let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., StrokeThickness=3.0)
        c.Children.Add(rect) |> ignore
        let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // just has item drawn on it, not the box
        c.Children.Add(innerc) |> ignore
        let redraw() =
            if prop.Value() then
                rect.Stroke <- yes
            else
                rect.Stroke <- no
        redraw()
        prop.Changed.Add(fun _ -> redraw())
        c.MouseDown.Add(fun _ -> prop.Toggle())
        canvasAdd(innerc, Graphics.BMPtoImage bmp, 4., 4.)
        match timelineID with
        | Some tid -> timelineItems.Add(new Timeline.TimelineItem(tid, fun()->bmp))
        | None -> ()
        c
    let basicBoxImpl(tts, tid, img, prop) =
        let c = veryBasicBoxImpl(img, Some(tid), prop)
        c.ToolTip <- tts
        c
    let basicBoxImplNoTimeline(tts, img, prop) =
        let c = veryBasicBoxImpl(img, None, prop)
        c.ToolTip <- tts
        c
    let wood_sword_box = basicBoxImpl("Acquired wood sword (mark timeline)", "WoodSword", Graphics.brown_sword_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword)    
    wood_sword_box.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.Sword1))
    wood_sword_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, wood_sword_box, OW_ITEM_GRID_LOCATIONS.WOOD_SWORD_BOX)
    let wood_arrow_box = basicBoxImpl("Acquired wood arrow (mark timeline)", "WoodArrow", Graphics.wood_arrow_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodArrow)
    wood_arrow_box.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.ARROW))
    wood_arrow_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, wood_arrow_box, OW_ITEM_GRID_LOCATIONS.WOOD_ARROW_BOX)
    let blue_candle_box = basicBoxImpl("Acquired blue candle (mark timeline, affects routing)", "BlueCandle", Graphics.blue_candle_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle)
    blue_candle_box.MouseEnter.Add(fun _ -> if TrackerModel.playerComputedStateSummary.CandleLevel=0 then showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BLUE_CANDLE) else showLocatorInstanceFunc(owInstance.Burnable))
    blue_candle_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, blue_candle_box, OW_ITEM_GRID_LOCATIONS.BLUE_CANDLE_BOX)
    let blue_ring_box = basicBoxImpl("Acquired blue ring (mark timeline)", "BlueRing", Graphics.blue_ring_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueRing)
    blue_ring_box.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BLUE_RING))
    blue_ring_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, blue_ring_box, OW_ITEM_GRID_LOCATIONS.BLUE_RING_BOX)
    let mags_box = basicBoxImpl("Acquired magical sword (mark timeline)", "MagicalSword", Graphics.magical_sword_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword)
    let mags_canvas = mags_box.Children.[1] :?> Canvas // a tiny bit fragile
    let redrawMagicalSwordCanvas(c:Canvas) =
        c.Children.Clear()
        if not(TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) &&   // dont have sword
                TrackerModel.mapStateSummary.Sword3Location=TrackerModel.NOTFOUND &&           // not yet located cave
                TrackerModel.GetLevelHint(10)<>TrackerModel.HintZone.UNKNOWN then              // have a hint
            canvasAdd(c, makeHintHighlight(21.), 4., 4.)
        canvasAdd(c, Graphics.BMPtoImage Graphics.magical_sword_bmp, 4., 4.)
    redrawMagicalSwordCanvas(mags_canvas)
    gridAddTuple(owItemGrid, mags_box, OW_ITEM_GRID_LOCATIONS.MAGS_BOX)
    mags_box.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.Sword3))
    mags_box.MouseLeave.Add(fun _ -> hideLocator())
    // boomstick book, to mark when purchase in boomstick seed (normal book will become shield found in dungeon)
    let boom_book_box = basicBoxImpl("Purchased boomstick book (mark timeline)", "BoomstickBook", Graphics.boom_book_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook)
    boom_book_box.MouseEnter.Add(fun _ -> showLocatorExactLocation(TrackerModel.mapStateSummary.BoomBookShopLocation))
    boom_book_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, boom_book_box, OW_ITEM_GRID_LOCATIONS.BOOMSTICK_BOX)
    // mark the dungeon wins on timeline via ganon/zelda boxes
    gridAddTuple(owItemGrid, basicBoxImpl("Killed Gannon (mark timeline)", "Gannon", Graphics.ganon_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasDefeatedGanon), OW_ITEM_GRID_LOCATIONS.GANON_BOX)
    let zelda_box = basicBoxImpl("Rescued Zelda (mark timeline)", "Zelda",  Graphics.zelda_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda)
    gridAddTuple(owItemGrid, zelda_box,  OW_ITEM_GRID_LOCATIONS.ZELDA_BOX)
    // hover zelda to display hidden overworld icons (note that Armos/Sword2/Sword3 will not be darkened)
    zelda_box.MouseEnter.Add(fun _ -> OverworldMapTileCustomization.temporarilyDisplayHiddenOverworldTileMarks <- true; OptionsMenu.requestRedrawOverworldEvent.Trigger())
    zelda_box.MouseLeave.Add(fun _ -> OverworldMapTileCustomization.temporarilyDisplayHiddenOverworldTileMarks <- false; OptionsMenu.requestRedrawOverworldEvent.Trigger())
    TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Changed.Add(fun b -> 
        if b then 
            notesTextBox.Text <- notesTextBox.Text + "\n" + hmsTimeTextBox.Text
            TrackerModel.LastChangedTime.PauseAll()
            if TrackerModelOptions.SaveOnCompletion.Value && not(isCurrentlyLoadingASave) then
                try
                    SaveAndLoad.SaveAll(notesTextBox.Text, DungeonUI.theDungeonTabControl.SelectedIndex, exportDungeonModelsJsonLines(), DungeonSaveAndLoad.SaveDrawingLayer(), currentRecorderDestinationIndex, SaveAndLoad.FinishedSave) |> ignore
                with e ->
                    ()
        else
            TrackerModel.LastChangedTime.ResumeAll()
        )
    // mark whether player currently has bombs, for overworld routing
    let bombIcon = veryBasicBoxImpl(Graphics.bomb_bmp, None, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBombs)
    bombIcon.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BOMB))
    bombIcon.MouseLeave.Add(fun _ -> hideLocator())
    bombIcon.ToolTip <- "Player currently has bombs (affects routing)"
    gridAddTuple(owItemGrid, bombIcon, OW_ITEM_GRID_LOCATIONS.BOMB_BOX)

    // shield versus book icon (for boomstick flags/seeds)
    let toggleBookShieldCheckBox  = new CheckBox(Content=new TextBox(Text="S/B",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    toggleBookShieldCheckBox.ToolTip <- "Shield item icon instead of book item icon"
    toggleBookShieldCheckBox.IsChecked <- System.Nullable.op_Implicit false
    toggleBookShieldCheckBox.Checked.Add(fun _ -> TrackerModel.ToggleIsCurrentlyBook())
    toggleBookShieldCheckBox.Unchecked.Add(fun _ -> TrackerModel.ToggleIsCurrentlyBook())
    canvasAdd(appMainCanvas, toggleBookShieldCheckBox, OW_ITEM_GRID_LOCATIONS.OFFSET+180., 30.)

    let highlightOpenCaves = Graphics.BMPtoImage Graphics.openCaveIconBmp
    highlightOpenCaves.ToolTip <- "Highlight unmarked open caves"
    ToolTipService.SetPlacement(highlightOpenCaves, System.Windows.Controls.Primitives.PlacementMode.Top)
    highlightOpenCaves.MouseEnter.Add(fun _ -> showLocatorInstanceFunc(owInstance.Nothingable))
    highlightOpenCaves.MouseLeave.Add(fun _ -> hideLocator())
    canvasAdd(appMainCanvas, highlightOpenCaves, 540., 120.)

    // these panels need to be created once, at startup time, as they have side effects that populate the timelineItems set
    let weaponsRowPanel = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    weaponsRowPanel.Children.Add(basicBoxImplNoTimeline("Wood sword", Graphics.brown_sword_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("White sword", "WhiteSword", Graphics.white_sword_bmp, TrackerModel.startingItemsAndExtras.PlayerHasWhiteSword)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImplNoTimeline("Magical sword", Graphics.magical_sword_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImplNoTimeline("Wood arrow", Graphics.wood_arrow_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodArrow)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Silver arrow", "SilverArrow", Graphics.silver_arrow_bmp, TrackerModel.startingItemsAndExtras.PlayerHasSilverArrow)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Bow", "Bow", Graphics.bow_bmp, TrackerModel.startingItemsAndExtras.PlayerHasBow)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Wand", "Wand", Graphics.wand_bmp, TrackerModel.startingItemsAndExtras.PlayerHasWand)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImplNoTimeline("Blue candle", Graphics.blue_candle_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Red candle", "RedCandle", Graphics.red_candle_bmp, TrackerModel.startingItemsAndExtras.PlayerHasRedCandle)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Boomerang", "Boomerang", Graphics.boomerang_bmp, TrackerModel.startingItemsAndExtras.PlayerHasBoomerang)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Magic boomerang", "MagicBoomerang", Graphics.magic_boomerang_bmp, TrackerModel.startingItemsAndExtras.PlayerHasMagicBoomerang)) |> ignore
    let utilityRowPanel = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    utilityRowPanel.Children.Add(basicBoxImplNoTimeline("Blue ring", Graphics.blue_ring_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueRing)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Red ring", "RedRing", Graphics.red_ring_bmp, TrackerModel.startingItemsAndExtras.PlayerHasRedRing)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Power bracelet", "PowerBracelet", Graphics.power_bracelet_bmp, TrackerModel.startingItemsAndExtras.PlayerHasPowerBracelet)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Ladder", "Ladder", Graphics.ladder_bmp, TrackerModel.startingItemsAndExtras.PlayerHasLadder)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Raft", "Raft", Graphics.raft_bmp, TrackerModel.startingItemsAndExtras.PlayerHasRaft)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Recorder", "Recorder", Graphics.recorder_bmp, TrackerModel.startingItemsAndExtras.PlayerHasRecorder)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Any key", "AnyKey", Graphics.key_bmp, TrackerModel.startingItemsAndExtras.PlayerHasAnyKey)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Book", "Book", Graphics.book_bmp, TrackerModel.startingItemsAndExtras.PlayerHasBook)) |> ignore
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
    extrasImage.MouseDown.Add(fun _ -> 
        if not popupIsActive then
            popupIsActive <- true
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
            async {
                do! CustomComboBoxes.DoModal(cm, wh, 20., 155., whole)
                whole.Children.Clear() // to reparent extrasPanel again next popup
                popupIsActive <- false
                } |> Async.StartImmediate
        )

    // timer reset
    let timerResetButton = Graphics.makeButton("Pause/Reset timer", Some(16.), Some(Brushes.Orange))
    canvasAdd(appMainCanvas, timerResetButton, 12.8*OMTW, 60.)
    timerResetButton.Click.Add(fun _ ->
        if not popupIsActive then
            popupIsActive <- true
            SaveAndLoad.MaybePollSeedAndFlags()
            let firstButton = Graphics.makeButton("Timer has been Paused.\nClick here to Resume.\n(Look below for Reset info.)", Some(16.), Some(Brushes.Orange))
            let secondButton = Graphics.makeButton("Timer has been Paused.\nClick here to confirm you want to Reset the timer,\nor click anywhere else to Resume.", Some(16.), Some(Brushes.Orange))
            let mutable userPressedReset = false
            let sp = new StackPanel(Orientation=Orientation.Vertical)
            sp.Children.Add(firstButton) |> ignore
            sp.Children.Add(new DockPanel(Height=300.)) |> ignore
            sp.Children.Add(secondButton) |> ignore
            let wh = new System.Threading.ManualResetEvent(false)
            firstButton.Click.Add(fun _ ->
                wh.Set() |> ignore
                )
            secondButton.Click.Add(fun _ ->
                resetTimerEvent.Trigger()
                userPressedReset <- true
                wh.Set() |> ignore
                )
            async {
                TrackerModel.LastChangedTime.PauseAll()
                do! CustomComboBoxes.DoModal(cm, wh, 50., 200., sp)
                TrackerModel.LastChangedTime.ResumeAll()
                popupIsActive <- false
                if userPressedReset then
                    legendStartIconButtonBehavior()  // jump into the 'place the start spot' popup
                } |> Async.StartImmediate
        )
    // spot summary
    let spotSummaryTB = new Border(Child=new TextBox(Text="Spot Summary", FontSize=16., IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), Foreground=Brushes.Orange, Background=Brushes.Black), 
                                    BorderThickness=Thickness(1.), IsHitTestVisible=true, Background=Brushes.Black)
    let spotSummaryCanvas = new Canvas()
    spotSummaryTB.MouseEnter.Add(fun _ ->
        spotSummaryCanvas.Children.Clear()
        spotSummaryCanvas.Children.Add(OverworldMapTileCustomization.MakeRemainderSummaryDisplay()) |> ignore
        )   
    spotSummaryTB.MouseLeave.Add(fun _ -> spotSummaryCanvas.Children.Clear())
    canvasAdd(appMainCanvas, spotSummaryTB, 12.8*OMTW, 90.)

    white_sword_canvas, mags_canvas, redrawWhiteSwordCanvas, redrawMagicalSwordCanvas, spotSummaryCanvas
