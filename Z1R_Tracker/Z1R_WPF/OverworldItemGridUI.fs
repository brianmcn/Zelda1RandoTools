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
let THRU_BLOCKERS_H = THRU_MAIN_MAP_AND_ITEM_PROGRESS_H + 38.*3.
let START_DUNGEON_AND_NOTES_AREA_H = THRU_MAIN_MAP_AND_ITEM_PROGRESS_H
let THRU_DUNGEON_AND_NOTES_AREA_H = START_DUNGEON_AND_NOTES_AREA_H + float(TH + 30 + (3 + 27*8 + 12*7 + 3) + 3)  // 3 is for a little blank space after this but before timeline
let START_TIMELINE_H = THRU_DUNGEON_AND_NOTES_AREA_H
let THRU_TIMELINE_H = START_TIMELINE_H + float TCH
let LEFT_OFFSET = 78.0
let BLOCKERS_AND_NOTES_OFFSET = 408. + 42.  // dungeon area and side-tracker-panel
let ITEM_PROGRESS_FIRST_ITEM = 130.
let hmsTimeTextBox = new TextBox(Width=148., Height=56., Text="timer",FontSize=42.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false)
let broadcastTimeTextBox = new TextBox(Text="timer",FontSize=42.0,Background=Brushes.Black,Foreground=Brushes.LightGreen,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false)

// some global mutable variables needed across various UI components
let mutable popupIsActive = false

let mutable displayIsCurrentlyMirrored = false
let mutable notesTextBox = null : TextBox
let mutable currentRecorderDestinationIndex = 0

let mutable hideFeatsOfStrength = fun (_b:bool) -> ()
let mutable hideRaftSpots = fun (_b:bool) -> ()

let mutable exportDungeonModelsJsonLines = fun () -> null
let mutable legendStartIconButtonBehavior = fun () -> ()

open DungeonUI.AhhGlobalVariables
let mutable showLocatorExactLocation = fun(_x:int,_y:int) -> ()
let mutable showLocatorHintedZone = fun(_hz:TrackerModel.HintZone,_also:bool) -> ()
let mutable showLocatorInstanceFunc = fun(_f:int*int->bool) -> ()
let mutable showHintShopLocator = fun() -> ()
let mutable showLocatorPotionAndTakeAny = fun() -> ()
let mutable showLocatorNoneFound = fun() -> ()
let mutable showLocator = fun(_sld:ShowLocatorDescriptor) -> ()

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

    let extrasCanvasGlobalBoxMouseOverHighlight = new Views.GlobalBoxMouseOverHighlight()
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
        Views.appMainCanvasGlobalBoxMouseOverHighlight.ApplyBehavior(c)
        extrasCanvasGlobalBoxMouseOverHighlight.ApplyBehavior(c)
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
    let wood_sword_box = basicBoxImpl("Acquired wood sword (mark timeline)", Timeline.TimelineID.WoodSword, Graphics.brown_sword_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword)    
    wood_sword_box.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.Sword1))
    wood_sword_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, wood_sword_box, OW_ITEM_GRID_LOCATIONS.WOOD_SWORD_BOX)
    let wood_arrow_box = basicBoxImpl("Acquired wood arrow (mark timeline)", Timeline.TimelineID.WoodArrow, Graphics.wood_arrow_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodArrow)
    wood_arrow_box.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.ARROW))
    wood_arrow_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, wood_arrow_box, OW_ITEM_GRID_LOCATIONS.WOOD_ARROW_BOX)
    let blue_candle_box = basicBoxImpl("Acquired blue candle (mark timeline, affects routing)", Timeline.TimelineID.BlueCandle, Graphics.blue_candle_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle)
    blue_candle_box.MouseEnter.Add(fun _ -> if TrackerModel.playerComputedStateSummary.CandleLevel=0 then showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BLUE_CANDLE) else showLocatorInstanceFunc(owInstance.Burnable))
    blue_candle_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, blue_candle_box, OW_ITEM_GRID_LOCATIONS.BLUE_CANDLE_BOX)
    let blue_ring_box = basicBoxImpl("Acquired blue ring (mark timeline)", Timeline.TimelineID.BlueRing, Graphics.blue_ring_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueRing)
    blue_ring_box.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BLUE_RING))
    blue_ring_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, blue_ring_box, OW_ITEM_GRID_LOCATIONS.BLUE_RING_BOX)
    let mags_box = basicBoxImpl("Acquired magical sword (mark timeline)", Timeline.TimelineID.MagicalSword, Graphics.magical_sword_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword)
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
    let boom_book_box = basicBoxImpl("Purchased boomstick book (mark timeline)", Timeline.TimelineID.BoomstickBook, Graphics.boom_book_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook)
    boom_book_box.MouseEnter.Add(fun _ -> showShopLocatorInstanceFunc(TrackerModel.MapSquareChoiceDomainHelper.BOOK))
    boom_book_box.MouseLeave.Add(fun _ -> hideLocator())
    gridAddTuple(owItemGrid, boom_book_box, OW_ITEM_GRID_LOCATIONS.BOOMSTICK_BOX)
    // mark the dungeon wins on timeline via ganon/zelda boxes
    gridAddTuple(owItemGrid, basicBoxImpl("Killed Gannon (mark timeline)", Timeline.TimelineID.Gannon, Graphics.ganon_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasDefeatedGanon), OW_ITEM_GRID_LOCATIONS.GANON_BOX)
    let zelda_box = basicBoxImpl("Rescued Zelda (mark timeline)", Timeline.TimelineID.Zelda,  Graphics.zelda_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda)
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
        let slash = new TextBox(Text="/",FontSize=12.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true)
        sp.Children.Add(slash) |> ignore
        let boomBookIcon = Graphics.BMPtoImage Graphics.boom_book_bmp
        boomBookIcon.Width <- 14.
        boomBookIcon.Height <- 14.
        sp.Children.Add(boomBookIcon) |> ignore
        new CheckBox(Content=sp)
    toggleBookShieldCheckBox.ToolTip <- "Shield instead of book (in item pool, for boomstick seeds)"
    toggleBookShieldCheckBox.IsChecked <- System.Nullable.op_Implicit false
    toggleBookShieldCheckBox.Checked.Add(fun _ -> TrackerModel.ToggleIsCurrentlyBook())
    toggleBookShieldCheckBox.Unchecked.Add(fun _ -> TrackerModel.ToggleIsCurrentlyBook())
    TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Changed.Add(fun b -> 
        if b then 
            notesTextBox.Text <- notesTextBox.Text + "\n" + hmsTimeTextBox.Text
            TrackerModel.LastChangedTime.PauseAll()
            if TrackerModelOptions.SaveOnCompletion.Value && not(Timeline.isCurrentlyLoadingASave) then
                try
                    SaveAndLoad.SaveAll(notesTextBox.Text, DungeonUI.theDungeonTabControl.SelectedIndex, exportDungeonModelsJsonLines(), DungeonSaveAndLoad.SaveDrawingLayer(), 
                                        Graphics.alternativeOverworldMapFilename, Graphics.shouldInitiallyHideOverworldMap, currentRecorderDestinationIndex, 
                                        toggleBookShieldCheckBox.IsChecked.Value, SaveAndLoad.FinishedSave) |> ignore
                with _e ->
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

    let highlightOpenCaves = 
        if isStandardHyrule then
            let highlightOpenCaves = Graphics.BMPtoImage Graphics.openCaveIconBmp
            highlightOpenCaves.ToolTip <- "Highlight unmarked open caves"
            ToolTipService.SetPlacement(highlightOpenCaves, System.Windows.Controls.Primitives.PlacementMode.Top)
            highlightOpenCaves.MouseEnter.Add(fun _ -> showLocatorInstanceFunc(owInstance.Nothingable))
            highlightOpenCaves.MouseLeave.Add(fun _ -> hideLocator())
            highlightOpenCaves
        else
            null

    // these panels need to be created once, at startup time, as they have side effects that populate the timelineItems set
    let weaponsRowPanel = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    weaponsRowPanel.Children.Add(basicBoxImplNoTimeline("Wood sword", Graphics.brown_sword_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("White sword", Timeline.TimelineID.WhiteSword, Graphics.white_sword_bmp, TrackerModel.startingItemsAndExtras.PlayerHasWhiteSword)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImplNoTimeline("Magical sword", Graphics.magical_sword_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImplNoTimeline("Wood arrow", Graphics.wood_arrow_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodArrow)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Silver arrow", Timeline.TimelineID.SilverArrow, Graphics.silver_arrow_bmp, TrackerModel.startingItemsAndExtras.PlayerHasSilverArrow)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Bow", Timeline.TimelineID.Bow, Graphics.bow_bmp, TrackerModel.startingItemsAndExtras.PlayerHasBow)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Wand", Timeline.TimelineID.Wand, Graphics.wand_bmp, TrackerModel.startingItemsAndExtras.PlayerHasWand)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImplNoTimeline("Blue candle", Graphics.blue_candle_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Red candle", Timeline.TimelineID.RedCandle, Graphics.red_candle_bmp, TrackerModel.startingItemsAndExtras.PlayerHasRedCandle)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Boomerang", Timeline.TimelineID.Boomerang, Graphics.boomerang_bmp, TrackerModel.startingItemsAndExtras.PlayerHasBoomerang)) |> ignore
    weaponsRowPanel.Children.Add(basicBoxImpl("Magic boomerang", Timeline.TimelineID.MagicBoomerang, Graphics.magic_boomerang_bmp, TrackerModel.startingItemsAndExtras.PlayerHasMagicBoomerang)) |> ignore
    let utilityRowPanel = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    utilityRowPanel.Children.Add(basicBoxImplNoTimeline("Blue ring", Graphics.blue_ring_bmp, TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueRing)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Red ring", Timeline.TimelineID.RedRing, Graphics.red_ring_bmp, TrackerModel.startingItemsAndExtras.PlayerHasRedRing)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Power bracelet", Timeline.TimelineID.PowerBracelet, Graphics.power_bracelet_bmp, TrackerModel.startingItemsAndExtras.PlayerHasPowerBracelet)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Ladder", Timeline.TimelineID.Ladder, Graphics.ladder_bmp, TrackerModel.startingItemsAndExtras.PlayerHasLadder)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Raft", Timeline.TimelineID.Raft, Graphics.raft_bmp, TrackerModel.startingItemsAndExtras.PlayerHasRaft)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Recorder", Timeline.TimelineID.Recorder, Graphics.recorder_bmp, TrackerModel.startingItemsAndExtras.PlayerHasRecorder)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Any key", Timeline.TimelineID.AnyKey, Graphics.key_bmp, TrackerModel.startingItemsAndExtras.PlayerHasAnyKey)) |> ignore
    utilityRowPanel.Children.Add(basicBoxImpl("Book", Timeline.TimelineID.Book, Graphics.book_bmp, TrackerModel.startingItemsAndExtras.PlayerHasBook)) |> ignore
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
                        // not change dungeon maps
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
        owItemGrid, toggleBookShieldCheckBox, highlightOpenCaves, timerResetButton, spotSummaryTB
