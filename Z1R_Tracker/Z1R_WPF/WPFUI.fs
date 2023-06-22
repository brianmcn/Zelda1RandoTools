module WPFUI

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

open OverworldMapTileCustomization
open DungeonUI.AhhGlobalVariables
open HotKeys.MyKey
open OverworldItemGridUI
open Reminders
open MainTrackerTriforcesAndItems

module OW_ITEM_GRID_LOCATIONS = OverworldMapTileCustomization.OW_ITEM_GRID_LOCATIONS

let currentMaxHeartsTextBox = new TextBox(Width=100., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                                            BorderThickness=Thickness(0.), Text=sprintf "Max Hearts: %d" TrackerModel.playerComputedStateSummary.PlayerHearts)
let owRemainingScreensTextBox = new TextBox(Width=110., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                                            BorderThickness=Thickness(0.), Text=sprintf "%d OW spots left" TrackerModel.mapStateSummary.OwSpotsRemain)
let owRemainingScreensTextBoxContainerPanelThatSeesMouseEvents = (let dp = new DockPanel(Background=Brushes.Black) in dp.Children.Add(owRemainingScreensTextBox) |> ignore; dp)
let owGettableScreensTextBox = new TextBox(Width=80., Height=20., FontSize=14., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                                            BorderThickness=Thickness(0.), Text=sprintf "%d gettable" TrackerModel.mapStateSummary.OwGettableLocations.Count)
let owGettableScreensCheckBox = new CheckBox(Content = owGettableScreensTextBox, IsChecked=true)
let mutable highlightOpenCavesCheckBox : CheckBox = null

type RouteDestination = LinkRouting.RouteDestination

let NoCyan(_i,_j) = false
let drawRoutesToImpl(routeDestinationOption, point, i, j, drawRouteMarks, maxBoldGYR, maxPaleGYR, whatToCyan) =
    let maxPaleGYR = if owGettableScreensCheckBox.IsChecked.HasValue && owGettableScreensCheckBox.IsChecked.Value then OverworldRouteDrawing.All else maxPaleGYR
    let unmarked = TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1)
    let interestingButInaccesible = ResizeArray()
    let owTargetworthySpots = Array2D.zeroCreate 16 8
    let processHint(hz:TrackerModel.HintZone,couldBeLetterDungeon) =
        for i = 0 to 15 do
            for j = 0 to 7 do
                if OverworldData.owMapZone.[j].[i] = hz.AsDataChar() then
                    let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                    let cbld = (couldBeLetterDungeon && cur>=0 && cur<=7 && TrackerModel.GetDungeon(cur).LabelChar='?')
                    if cur = -1 || cbld then
                        if TrackerModel.mapStateSummary.OwGettableLocations.Contains(i,j) || cbld then
                            owTargetworthySpots.[i,j] <- true
                            unmarked.[i,j] <- true  // for cbld case
                        else
                            interestingButInaccesible.Add(i,j)
    let mutable lightUpDestinations = false
    match routeDestinationOption with
    | Some(RouteDestination.OW_MAP(spots)) ->
        for x,y in spots do
            owTargetworthySpots.[x,y] <- true
        OverworldRouteDrawing.drawPathsImpl(owTargetworthySpots, unmarked, point, i, j, true, false, 0, 0, whatToCyan)
        lightUpDestinations <- true
    | Some(RouteDestination.HINTZONE(hz,couldBeLetterDungeon)) ->
        processHint(hz,couldBeLetterDungeon)
        OverworldRouteDrawing.drawPathsImpl(owTargetworthySpots, unmarked, point, i, j, true, false, OverworldRouteDrawing.All, 0, whatToCyan)
    | Some(RouteDestination.UNMARKEDINSTANCEFUNC(f)) ->
        for x = 0 to 15 do
            for y = 0 to 7 do
                if unmarked.[x,y] && f(x,y) then
                    owTargetworthySpots.[x,y] <- true
        OverworldRouteDrawing.drawPathsImpl(owTargetworthySpots, unmarked, point, i, j, true, false, OverworldRouteDrawing.MaxGYR, OverworldRouteDrawing.All, whatToCyan)
    | None ->
        OverworldRouteDrawing.drawPathsImpl(TrackerModel.mapStateSummary.OwRouteworthySpots, unmarked, point, i, j, drawRouteMarks, true, maxBoldGYR, maxPaleGYR, whatToCyan)
    for x,y in interestingButInaccesible do
        OverworldRouteDrawing.routeDrawingLayer.GetHighlightTile(x,y).MakeRed()
    if lightUpDestinations then
        for x = 0 to 15 do
            for y = 0 to 7 do
                if owTargetworthySpots.[x,y] then
                    OverworldRouteDrawing.routeDrawingLayer.GetHighlightTile(x,y).MakeGreen()

let resetTimerEvent = new Event<unit>()
let mutable currentlyMousedOWX, currentlyMousedOWY = -1, -1

let makeAll(mainWindow:Window, cm:CustomComboBoxes.CanvasManager, drawingCanvas:Canvas, owMapNum, heartShuffle, kind, loadData:DungeonSaveAndLoad.AllData option, 
                showProgress, speechRecognitionInstance:SpeechRecognition.SpeechRecognitionInstance) = async {
    let ctxt = System.Threading.SynchronizationContext.Current
    let refocusMainWindow() =   // keep hotkeys working
        async {
            do! Async.Sleep(500)  // give new window time to pop up
            do! Async.SwitchToContext(ctxt)
            mainWindow.Focus() |> ignore
        } |> Async.StartImmediate
    match loadData with
    | Some(data) ->
        Graphics.alternativeOverworldMapFilename <- data.AlternativeOverworldMapFilename
        Graphics.shouldInitiallyHideOverworldMap <- data.ShouldInitiallyHideOverworldMap
        // rest of data is loaded at end, but these are needed at start
    | _ -> ()
    // initialize based on startup parameters
    let owMapBMPs, isMixed, owInstance, owMapNum, maxOverworldRemain =
        match owMapNum, loadData with
        | 0, _ -> Graphics.overworldMapBMPs(0), false, new OverworldData.OverworldInstance(OverworldData.OWQuest.FIRST), 0, TrackerModel.MaxRemain1Q
        | 1, _ -> Graphics.overworldMapBMPs(1), false, new OverworldData.OverworldInstance(OverworldData.OWQuest.SECOND), 1, TrackerModel.MaxRemain2Q
        | 2, _ -> Graphics.overworldMapBMPs(2), true,  new OverworldData.OverworldInstance(OverworldData.OWQuest.MIXED_FIRST), 2, TrackerModel.MaxRemainMQ
        | 3, _ -> Graphics.overworldMapBMPs(3), true,  new OverworldData.OverworldInstance(OverworldData.OWQuest.MIXED_SECOND), 3, TrackerModel.MaxRemainMQ
        | 4, _ -> Graphics.overworldMapBMPs(4), false,  new OverworldData.OverworldInstance(OverworldData.OWQuest.BLANK), 4, TrackerModel.MaxRemainUQ
        | 999, Some(data) -> 
            match data.Overworld.Quest with
            | 0 -> Graphics.overworldMapBMPs(0), false, new OverworldData.OverworldInstance(OverworldData.OWQuest.FIRST), 0, TrackerModel.MaxRemain1Q
            | 1 -> Graphics.overworldMapBMPs(1), false, new OverworldData.OverworldInstance(OverworldData.OWQuest.SECOND), 1, TrackerModel.MaxRemain2Q
            | 2 -> Graphics.overworldMapBMPs(2), true,  new OverworldData.OverworldInstance(OverworldData.OWQuest.MIXED_FIRST), 2, TrackerModel.MaxRemainMQ
            | 3 -> Graphics.overworldMapBMPs(3), true,  new OverworldData.OverworldInstance(OverworldData.OWQuest.MIXED_SECOND), 3, TrackerModel.MaxRemainMQ
            | 4 -> Graphics.overworldMapBMPs(4), false,  new OverworldData.OverworldInstance(OverworldData.OWQuest.BLANK), 4, TrackerModel.MaxRemainUQ
            | _ -> failwith "bad load data at root.Overworld.Quest"
        | _ -> failwith "bad/unsupported (owMapNum,loadData)"
    do! showProgress("after ow map load")
    if owMapNum < 0 || owMapNum > 4 then
        failwith "bad owMapNum"
    let isStandardHyrule = owMapNum <> 4   // should features assume we know the overworld map as standard/mirrored z1r
    let drawRoutesTo(routeDestinationOption, point, i, j, drawRouteMarks, maxBoldGYR, maxPaleGYR, whatToCyan) =
        if isStandardHyrule then drawRoutesToImpl(routeDestinationOption, point, i, j, drawRouteMarks, maxBoldGYR, maxPaleGYR, whatToCyan)
        else ()
    TrackerModel.initializeAll(owInstance, kind)
    if not heartShuffle then
        for i = 0 to 7 do
            TrackerModel.GetDungeon(i).Boxes.[0].Set(TrackerModel.ITEMS.HEARTCONTAINER, TrackerModel.PlayerHas.NO)
    let spokenOWTiles = TrackerModel.overworldTiles(owInstance.Quest.IsFirstQuestOW,TrackerModel.IsHiddenDungeonNumbers()) |> Array.map (fun (_,_,s,_) -> s)

    // make the entire UI
    let whetherToCyanOpenCavesOrArmos() = 
        if highlightOpenCavesCheckBox<>null && highlightOpenCavesCheckBox.IsChecked.HasValue && highlightOpenCavesCheckBox.IsChecked.Value then
            // There are 2 reasons to highlight some open caves.
            // First, you might be looking for wood sword cave to obtain free wood sword (or candle)
            if (TrackerModel.playerComputedStateSummary.SwordLevel > 0 && TrackerModel.playerComputedStateSummary.CandleLevel > 0)
                        || TrackerModel.mapStateSummary.Sword1Location <> TrackerModel.NOTFOUND then
                // (highlighting ALL open caves is no longer meaningful, but...)
                if TrackerModel.armosBox.CellCurrent() = -1 then
                    // Second, if your armos box is still empty, highlight the armos
                    (fun (x,y) -> owInstance.HasArmos(x,y) && TrackerModel.overworldMapMarks.[x,y].Current() = -1)
                else
                    NoCyan
            else
                (fun (x,y) -> owInstance.Nothingable(x,y) && TrackerModel.overworldMapMarks.[x,y].Current() = -1)
        else
            NoCyan
    let isSpecificRouteTargetActive,currentRouteTarget,eliminateCurrentRouteTarget,changeCurrentRouteTarget =
        let routeTargetLastClickedTime = new TrackerModel.LastChangedTime(TimeSpan.FromMinutes(10.))
        let mutable routeTarget = None
        let isSpecificRouteTargetActive() = DateTime.Now - routeTargetLastClickedTime.Time < TimeSpan.FromSeconds(10.)
        let currentRouteTarget() =
            if isSpecificRouteTargetActive() then
                routeTarget
            else
                None
        let eliminateCurrentRouteTarget() =
            routeTarget <- None
            routeTargetLastClickedTime.SetAgo(TimeSpan.FromMinutes(10.))
        let changeCurrentRouteTarget(newTarget) =
            routeTargetLastClickedTime.SetNow()
            routeTarget <- Some(newTarget)
        isSpecificRouteTargetActive,currentRouteTarget,eliminateCurrentRouteTarget,changeCurrentRouteTarget
    
    let appMainCanvas = cm.AppMainCanvas
    let layout = 
        if TrackerModelOptions.ShorterAppWindow.Value then
            new Layout.ShorterApplicationLayout(cm) :> Layout.IApplicationLayoutBase
        else
            new Layout.ApplicationLayout(cm) :> Layout.IApplicationLayoutBase
    let boxItemImpl, extrasImage, updateNumberedTriforceDisplayImpl, updateNumberedTriforceDisplayIfItExists, level9ColorCanvas = 
        MainTrackerTriforcesAndItems.setup(cm, owInstance, layout, kind)

    let owLocatorTileRectangles = Array2D.zeroCreate 16 8
    let redrawOWCircle = ref(fun (_x,_y) -> ())
    let hideFirstQuestCheckBox, hideSecondQuestCheckBox, moreFQSQoptionsButton = MakeFQSQStuff(cm, isMixed, owLocatorTileRectangles, redrawOWCircle)
    if isMixed then
        layout.AddHideQuestCheckboxes(hideFirstQuestCheckBox, hideSecondQuestCheckBox)

    let mutable toggleBookShieldCheckBox : CheckBox = null
    let mutable bookIsAtlasCheckBox : CheckBox = null
    let MakeManualSave() = SaveAndLoad.SaveAll(notesTextBox.Text, DungeonUI.theDungeonTabControl.SelectedIndex, exportDungeonModelsJsonLines(), DungeonSaveAndLoad.SaveDrawingLayer(), 
                                    Graphics.alternativeOverworldMapFilename, Graphics.shouldInitiallyHideOverworldMap, currentRecorderDestinationIndex, 
                                    toggleBookShieldCheckBox.IsChecked.Value, bookIsAtlasCheckBox.IsChecked.Value, SaveAndLoad.ManualSave)

    let mirrorOW = new Border(Child=Graphics.BMPtoImage Graphics.mirrorOverworldBMP, BorderBrush=Brushes.Gray, BorderThickness=Thickness(1.))
    mirrorOW.MouseEnter.Add(fun _ -> mirrorOW.BorderBrush <- Brushes.DarkGray)
    mirrorOW.MouseLeave.Add(fun _ -> mirrorOW.BorderBrush <- Brushes.Gray)
    mirrorOW.MouseDown.Add(fun _ ->
        TrackerModelOptions.Overworld.MirrorOverworld.Value <- not TrackerModelOptions.Overworld.MirrorOverworld.Value 
        TrackerModelOptions.writeSettings()
        doUIUpdateEvent.Trigger()
        )
    mirrorOW.ToolTip <- "Toggle mirrored overworld"
    ToolTipService.SetPlacement(mirrorOW, System.Windows.Controls.Primitives.PlacementMode.Top)
    let mutable temporarilyHideAllIconsWhileHoveringEyeball = false
    let hideIconsEyeball = new Border(Child=Graphics.BMPtoImage Graphics.eyeballBMP, BorderBrush=Brushes.Gray, BorderThickness=Thickness(1.))
    hideIconsEyeball.MouseEnter.Add(fun _ -> temporarilyHideAllIconsWhileHoveringEyeball <- true; OptionsMenu.requestRedrawOverworldEvent.Trigger())
    hideIconsEyeball.MouseLeave.Add(fun _ -> temporarilyHideAllIconsWhileHoveringEyeball <- false; OptionsMenu.requestRedrawOverworldEvent.Trigger())
    hideIconsEyeball.ToolTip <- "Hover to hide icons\n(peek at unobscured overworld map)"
    ToolTipService.SetPlacement(hideIconsEyeball, System.Windows.Controls.Primitives.PlacementMode.Bottom)
    let white_sword_canvas, mags_canvas, redrawWhiteSwordCanvas, redrawMagicalSwordCanvas, spotSummaryCanvas, invokeExtras,
        owItemGrid, toggleBookShieldCB, bookIsAtlasCB, highlightOpenCavesCB, timerResetButton, spotSummaryTB = 
            MakeItemGrid(cm, boxItemImpl, timelineItems, owInstance, extrasImage, resetTimerEvent, isStandardHyrule, doUIUpdateEvent, MakeManualSave)
    toggleBookShieldCheckBox <- toggleBookShieldCB
    bookIsAtlasCheckBox <- bookIsAtlasCB
    highlightOpenCavesCheckBox <- highlightOpenCavesCB
    if isStandardHyrule then
        layout.AddItemGridStuff(owItemGrid, toggleBookShieldCheckBox, bookIsAtlasCheckBox, highlightOpenCavesCheckBox, timerResetButton, spotSummaryTB, mirrorOW, hideIconsEyeball, moreFQSQoptionsButton)
    else
        layout.AddItemGridStuff(owItemGrid, toggleBookShieldCheckBox, bookIsAtlasCheckBox, highlightOpenCavesCheckBox, timerResetButton, spotSummaryTB, mirrorOW, hideIconsEyeball, null)

    do! showProgress("link")

    // overworld map grouping, as main point of support for mirroring
    let mutable animateOverworldTile = fun _ -> ()
    let animateOverworldTileIfOptionIsChecked(i,j) = animateOverworldTile(i,j)  // the option is checked in the body - all OW tile changes should call this
    let mirrorOverworldFEs = ResizeArray<FrameworkElement>()   // overworldCanvas (on which all map is drawn) is here, as well as individual tiny textual/icon elements that need to be re-flipped
    let overworldCanvas = new Canvas(Width=OMTW*16., Height=11.*3.*8.)
    layout.AddOverworldCanvas(overworldCanvas)
    mirrorOverworldFEs.Add(overworldCanvas)

    let blockerQueries = ResizeArray()
    let stepAnimateLink = 
        if isStandardHyrule then   // Routing only works on standard map
            let stepAnimateLink, linkIcon, currentTargetIcon = 
                LinkRouting.SetupLinkRouting(cm, changeCurrentRouteTarget, eliminateCurrentRouteTarget, isSpecificRouteTargetActive, blockerQueries, updateNumberedTriforceDisplayImpl,
                                               (fun() -> displayIsCurrentlyMirrored), MapStateProxy(14).DefaultInteriorBmp(), owInstance, redrawWhiteSwordCanvas, redrawMagicalSwordCanvas)
            layout.AddLinkRouting(linkIcon, currentTargetIcon)
            stepAnimateLink
        else 
            fun () -> ()

    do! showProgress("overworld start start 1")

    layout.AddWebcamLine()
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // ow map opaque fixed bottom layer
    let X_OPACITY = Graphics.X_OPACITY
    let owOpaqueMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    owOpaqueMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    for i = 0 to 15 do
        for j = 0 to 7 do
            let image = Graphics.BMPtoImage(owMapBMPs.[i,j])
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
                let icon = Graphics.BMPtoImage(Graphics.theFullTileBmpTable.[TrackerModel.MapSquareChoiceDomainHelper.DARK_X].[0]) // "X"
                icon.Opacity <- X_OPACITY
                canvasAdd(c, icon, 0., 0.)
    canvasAdd(overworldCanvas, owOpaqueMapGrid, 0., 0.)

    // layer to place darkening icons - dynamic icons that are below route-drawing but above the fixed base layer
    let owDarkeningMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owDarkeningMapGridCanvases = Array2D.zeroCreate 16 8
    owDarkeningMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    for i = 0 to 15 do
        for j = 0 to 7 do
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            gridAdd(owDarkeningMapGrid, c, i, j)
            owDarkeningMapGridCanvases.[i,j] <- c
    canvasAdd(overworldCanvas, owDarkeningMapGrid, 0., 0.)

    // layer to place 'hiding' icons - dynamic darkening icons that are below route-drawing but above the previous layers
    if isMixed then // HFQ/HSQ
        let owHidingMapGrid = makeGrid(16, 8, int OMTW, 11*3)
        let owHidingMapGridCanvases = Array2D.zeroCreate 16 8
        owHidingMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
        for i = 0 to 15 do
            for j = 0 to 7 do
                let c = new Canvas(Width=OMTW, Height=float(11*3))
                gridAdd(owHidingMapGrid, c, i, j)
                owHidingMapGridCanvases.[i,j] <- c
        canvasAdd(overworldCanvas, owHidingMapGrid, 0., 0.)
        let hide(x,y) =
            let hideColor = Brushes.DarkSlateGray
            let hideOpacity = 0.7
            let mark = new Shapes.Ellipse(Width=OMTW-2., Height=float(11*3)-2., Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
            canvasAdd(owHidingMapGridCanvases.[x,y], mark, 0., 0.)
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
    do  // SailNot/NoFeats
        let owHidingMapGrid = makeGrid(16, 8, int OMTW, 11*3)
        let owHidingMapGridCanvases = Array2D.zeroCreate 16 8
        owHidingMapGrid.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
        for i = 0 to 15 do
            for j = 0 to 7 do
                let c = new Canvas(Width=OMTW, Height=float(11*3))
                gridAdd(owHidingMapGrid, c, i, j)
                owHidingMapGridCanvases.[i,j] <- c
        canvasAdd(overworldCanvas, owHidingMapGrid, 0., 0.)
        let hide(x,y) =
            let hideColor = Brushes.DarkSlateGray
            let hideOpacity = 0.7
            let mark = new Shapes.Ellipse(Width=OMTW-2., Height=float(11*3)-2., Stroke=hideColor, StrokeThickness = 3., Fill=hideColor, Opacity=hideOpacity)
            canvasAdd(owHidingMapGridCanvases.[x,y], mark, 0., 0.)
        hideFeatsOfStrength <- 
            (fun b ->  // hide feats of strength
                for x = 0 to 15 do
                    for y = 0 to 7 do
                        if owInstance.PowerBraceletable(x,y) || owInstance.GravePushable(x,y) then
                            if not b then
                                owHidingMapGridCanvases.[x,y].Children.Clear()
                            else
                                hide(x,y)
            )
        hideRaftSpots <-
            (fun b ->   // hide raft spots (sail not)
                for x = 0 to 15 do
                    for y = 0 to 7 do
                        if owInstance.Raftable(x,y) then
                            if not b then
                                owHidingMapGridCanvases.[x,y].Children.Clear()
                            else
                                hide(x,y)
            )

    // ow route drawing layer
    OverworldRouteDrawing.routeDrawingLayer.AddSelfTo(overworldCanvas)

    // middle click overworld circles
    let makeOwCircle(brush) = new Shapes.Ellipse(Width=float(11*3)-2., Height=float(11*3)-2., Stroke=brush, StrokeThickness=3.0, IsHitTestVisible=false)
    let pinkBrush = new SolidColorBrush(Color.FromRgb(0xFFuy, 0x40uy, 0x99uy))
    let owCircleColor(data) = if data >= 200 then Brushes.Yellow elif data >= 100 then pinkBrush else Brushes.Cyan
    let overworldCirclesCanvas = new Canvas(Width=16.*OMTW, Height=float(8*11*3))  
    overworldCirclesCanvas.IsHitTestVisible <- false
    let owCircleRedraws = Array2D.init 16 8 (fun i j -> 
        let c = new Canvas(Width=OMTW, Height=float(11*3))
        mirrorOverworldFEs.Add(c)
        canvasAdd(overworldCirclesCanvas, c, OMTW*float(i), float(j*11*3))
        let redraw() =
            c.Children.Clear()
            let v = TrackerModel.overworldMapCircles.[i,j]
            let brush = owCircleColor(v)
            let v = v % 100
            if v <> 0 then
                canvasAdd(c, makeOwCircle(brush), 11.5*OMTW/48.-3., 0.)
            if (v >= 48 && v <= 57) || (v >= 65 && v <= 90) then
                let tb = new TextBox(Text=sprintf "%c" (char v), FontSize=12., FontWeight=FontWeights.Bold, Foreground=brush, Background=Brushes.Black, 
                                        IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.))
                c.Children.Add(tb) |> ignore
                Canvas.SetLeft(tb, 0.)
                Canvas.SetBottom(tb, 0.)
        redraw)
    redrawOWCircle := fun (x,y) -> owCircleRedraws.[x,y]()

    do! showProgress("overworld magnifier")
    let onMouseForMagnifier, dungeonTabsOverlay, dungeonTabsOverlayContent = UIComponents.MakeMagnifier(mirrorOverworldFEs, owMapNum, owMapBMPs)
    do! showProgress("overworld start 2")
    // ow map -> dungeon tabs interaction
    let selectDungeonTabEvent = new Event<_>()
    // ow map
    let AsyncBrieflyHighlightAnOverworldLocation(loc) = async {
            hideLocator()  // we may be moused in a dungeon right now
            showLocatorExactLocation loc
            do! Async.Sleep(3000)
            do! Async.SwitchToContext ctxt
            hideLocator()  // this does mean the dungeon location highlight will disappear if we're moused in a dungeon
        } 
    let owMapGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owCanvases = Array2D.zeroCreate 16 8
    let owUpdateFunctions = Array2D.create 16 8 (fun _ _ -> ())
    let drawCompletedDungeonHighlight(c,x,y,isWider) =
        // darken the number
        let w = if isWider then 27.0 else 15.0
        let rect = new System.Windows.Shapes.Rectangle(Width=w*OMTW/48., Height=27.0, Stroke=System.Windows.Media.Brushes.Black, StrokeThickness = 3.,
                                                        Fill=System.Windows.Media.Brushes.Black, Opacity=0.4, IsHitTestVisible=false)
        let diff = (if displayIsCurrentlyMirrored then 18.0*OMTW/48. else 15.0*OMTW/48.) - (if isWider then 6.0 else 0.0)
        canvasAdd(c, rect, x*OMTW+diff, float(y*11*3)+3.0)
    let drawDarkening(c,x,y) =
        let rect = new System.Windows.Shapes.Rectangle(Width=OMTW, Height=float(11*3), Stroke=System.Windows.Media.Brushes.Black, StrokeThickness = 3.,
                                                        Fill=System.Windows.Media.Brushes.Black, Opacity=X_OPACITY)
        canvasAdd(c, rect, x*OMTW, float(y*11*3))
    let ntf = UIHelpers.NotTooFrequently(System.TimeSpan.FromSeconds(0.25))
    overworldCanvas.MouseLeave.Add(fun _ -> OverworldRouteDrawing.routeDrawingLayer.Clear())
    do! showProgress("overworld before 16x8 loop")
    let centerOf(i,j) = overworldCanvas.TranslatePoint(Point(float(i)*OMTW+OMTW/2., float(j*11*3)+float(11*3)/2.), appMainCanvas)
    let owMapDummyImages = Array2D.init 16 8 (fun i j -> 
        let image = Graphics.BMPtoImage(owMapBMPs.[i,j])
        image.Opacity <- 0.001
        image
        )
    for i = 0 to 15 do
        for j = 0 to 7 do
            let activateCircleLabelPopup() =
                if not popupIsActive then
                    popupIsActive <- true
                    let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, FontSize=16., TextAlignment=TextAlignment.Center,
                                            Text="Press a key A-Z or 0-9 to label this circle\nOR\nLeft-click to make an un-labeled circle\nOR\nRight-click to remove this circle")
                    let element = new Canvas(Width=appMainCanvas.Width, Height=appMainCanvas.Height, Background=Brushes.Transparent, IsHitTestVisible=true)
                    canvasAdd(element, tb, 250., 10.)
                    let baseColor = if TrackerModel.overworldMapCircles.[i,j] >= 200 then 200 elif TrackerModel.overworldMapCircles.[i,j] >= 100 then 100 else 0
                    let displayCircle = makeOwCircle(owCircleColor(baseColor))
                    canvasAdd(element, displayCircle, float (if displayIsCurrentlyMirrored then (15-i) else i)*OMTW + 8.5*OMTW/48., 150.+float(j*11*3))
                    let wh = new System.Threading.ManualResetEvent(false)
                    element.MouseDown.Add(fun ea ->
                        if ea.ChangedButton = Input.MouseButton.Left then
                            TrackerModel.overworldMapCircles.[i,j] <- baseColor + 1
                            wh.Set() |> ignore
                        elif ea.ChangedButton = Input.MouseButton.Right then
                            TrackerModel.overworldMapCircles.[i,j] <- baseColor + 0
                            wh.Set() |> ignore
                        )
                    element.MyKeyAdd(fun ea ->
                        let key = snd ea.Key
                        if key >= Input.Key.D0 && key <= Input.Key.D9 then
                            TrackerModel.overworldMapCircles.[i,j] <- baseColor + int key - int Input.Key.D0 + int '0'
                            wh.Set() |> ignore
                        elif key >= Input.Key.A && key <= Input.Key.Z then
                            TrackerModel.overworldMapCircles.[i,j] <- baseColor + int key - int Input.Key.A + int 'A'
                            wh.Set() |> ignore
                        )
                    async {
                        do! CustomComboBoxes.DoModalCore(cm, wh, (fun (c,e) -> canvasAdd(c, e, 0., 0.)), (fun (c,e) -> c.Children.Remove(e)), element, 0.7)
                        owCircleRedraws.[i,j]()
                        popupIsActive <- false
                        } |> Async.StartImmediate
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            gridAdd(owMapGrid, c, i, j)
            // we need a dummy image to make the canvas absorb the mouse interactions, so just re-draw the map at almost 0 opacity
            canvasAdd(c, owMapDummyImages.[i,j], 0., 0.)
            // highlight mouse, do mouse-sensitive stuff
            let rect = new System.Windows.Shapes.Rectangle(Width=OMTW-4., Height=float(11*3)-4., Stroke=System.Windows.Media.Brushes.White)
            c.MouseEnter.Add(fun ea ->  canvasAdd(c, rect, 2., 2.)
                                        // draw routes
                                        let mousePos = ea.GetPosition(c)
                                        let mousePos = if displayIsCurrentlyMirrored then Point(OMTW - mousePos.X, mousePos.Y) else mousePos
                                        ntf.SendThunk(fun () -> 
                                            drawRoutesTo(currentRouteTarget(), mousePos, i, j, TrackerModelOptions.Overworld.DrawRoutes.Value, 
                                                            (if TrackerModelOptions.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxGYR else 0),
                                                            (if TrackerModelOptions.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxGYR else 0),
                                                            whetherToCyanOpenCavesOrArmos())
                                            )
                                        if TrackerModelOptions.Overworld.ShowMagnifier.Value then
                                            onMouseForMagnifier(i,j)   // somewhat expensive, only call if it's getting shown
                                        // track current location for F5 & speech recognition purposes
                                        currentlyMousedOWX <- i
                                        currentlyMousedOWY <- j
                                        )
            c.MouseLeave.Add(fun _ -> c.Children.Remove(rect) |> ignore
                                      dungeonTabsOverlayContent.Children.Clear()
                                      dungeonTabsOverlay.Opacity <- 0.
                                      )
            c.MyKeyAdd(fun ea ->
                match HotKeys.GlobalHotKeyProcessor.TryGetValue(ea.Key) with
                | Some(HotKeys.GlobalHotkeyTargets.MoveCursorRight) -> 
                    ea.Handled <- true
                    if i<15 then
                        Graphics.NavigationallyWarpMouseCursorTo(centerOf(i+1,j))
                | Some(HotKeys.GlobalHotkeyTargets.MoveCursorLeft) -> 
                    ea.Handled <- true
                    if i>0 then
                        let n = i-1   // without this I seem to encounter a compiler bug?!?
                        Graphics.NavigationallyWarpMouseCursorTo(centerOf(n,j))
                | Some(HotKeys.GlobalHotkeyTargets.MoveCursorDown) -> 
                    ea.Handled <- true
                    if j<7 then
                        Graphics.NavigationallyWarpMouseCursorTo(centerOf(i,j+1))
                | Some(HotKeys.GlobalHotkeyTargets.MoveCursorUp) -> 
                    ea.Handled <- true
                    if j>0 then
                        Graphics.NavigationallyWarpMouseCursorTo(centerOf(i,j-1))
                | _ -> ()
            )
            // icon
            if owInstance.AlwaysEmpty(i,j) then
                // already set up as permanent opaque layer, in code above, so nothing else to do
                // except...
                if i=9 && j=3 || i=3 && j=4 || (owInstance.Quest=OverworldData.OWQuest.SECOND && i=11 && j=0) then // fairy spots
                    let image = Graphics.BMPtoImage Graphics.fairy_bmp
                    canvasAdd(c, image, OMTW/2.-8., 1.)
                if i=15 && j=5 then // coast item ladder spot
                    let extraDecorationsF(boxPos:Point) =
                        // ladderBox position in main canvas
                        let lx,ly = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.LADDER_ITEM_BOX)
                        seq(OverworldMapTileCustomization.computeExtraDecorationArrow(lx, ly, boxPos))
                    let coastBoxOnOwGrid = Views.MakeBoxItemWithExtraDecorations(Some(cm), TrackerModel.ladderBox, false, Some extraDecorationsF)
                    mirrorOverworldFEs.Add(coastBoxOnOwGrid)
                    canvasAdd(c, coastBoxOnOwGrid, OMTW-31., 1.)
                    //TrackerModel.ladderBox.Changed.Add(fun _ -> // this would make the box go away instantly once got
                    doUIUpdateEvent.Publish.Add(fun _ ->          // this waits a second, which gives visual feeback when left-clicked & allows hotkeys to skip it via multi-hotkey
                        if not(TrackerModel.ladderBox.IsDone()) then
                            coastBoxOnOwGrid.Opacity <- 1.
                            coastBoxOnOwGrid.IsHitTestVisible <- true
                        else
                            coastBoxOnOwGrid.Opacity <- 0.
                            coastBoxOnOwGrid.IsHitTestVisible <- false
                        )
                c.MouseDown.Add(fun ea -> 
                    if ea.ChangedButton = Input.MouseButton.Middle then
                        // middle click toggles circle
                        TrackerModel.toggleOverworldMapCircle(i,j)
                        owCircleRedraws.[i,j]()
                    elif ea.ChangedButton = Input.MouseButton.Left then
                        if Input.Keyboard.IsKeyDown(Input.Key.LeftShift) || Input.Keyboard.IsKeyDown(Input.Key.RightShift) then
                            // shift left click activates label popup
                            activateCircleLabelPopup()
                    elif ea.ChangedButton = Input.MouseButton.Right then
                        if Input.Keyboard.IsKeyDown(Input.Key.LeftShift) || Input.Keyboard.IsKeyDown(Input.Key.RightShift) then
                            // shift right click cycles color
                            TrackerModel.overworldMapCircles.[i,j] <- TrackerModel.overworldMapCircles.[i,j] + 100
                            if TrackerModel.overworldMapCircles.[i,j] >= 300 then
                                TrackerModel.overworldMapCircles.[i,j] <- TrackerModel.overworldMapCircles.[i,j] - 300
                            owCircleRedraws.[i,j]()
                    )
                c.MouseWheel.Add(fun x -> 
                    if TrackerModel.overworldMapCircles.[i,j] <> 0 then
                        if x.Delta<0 then
                            TrackerModel.nextOverworldMapCircleColor(i,j)
                        else
                            TrackerModel.prevOverworldMapCircleColor(i,j)
                        owCircleRedraws.[i,j]()
                    )
            else
                let redrawGridSpot() =
                    // cant remove-by-identity because of non-uniques; remake whole canvas
                    owDarkeningMapGridCanvases.[i,j].Children.Clear()
                    c.Children.Clear()    // deparents owMapDummyImages.[i,j]
                    // we need a dummy image to make the canvas absorb the mouse interactions, so just re-draw the map at almost 0 opacity
                    canvasAdd(c, owMapDummyImages.[i,j], 0., 0.)
                    let ms = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                    let shouldAppearLikeDarkX,iconBMP,extraDecorations = GetIconBMPAndExtraDecorations(cm,ms,i,j)
                    // be sure to draw in appropriate layer
                    if iconBMP <> null then 
                        if temporarilyHideAllIconsWhileHoveringEyeball then
                            let icon = Graphics.BMPtoImage(Graphics.theFullTileBmpTable.[TrackerModel.MapSquareChoiceDomainHelper.DARK_X].[0])
                            icon.Opacity <- X_OPACITY
                            canvasAdd(owDarkeningMapGridCanvases.[i,j], icon, 0., 0.)  // the icon 'is' the darkening
                        elif ms.IsX || shouldAppearLikeDarkX then
                            if ms.IsX && TrackerModel.getOverworldMapExtraData(i,j,ms.State)=ms.State then
                                // used when Graphics.CanHideAndReveal()
                                let blankTile = Graphics.BMPtoImage Graphics.blankTileBmp
                                canvasAdd(c, blankTile, 0., 0.)
                            else
                                let icon = Graphics.BMPtoImage(Graphics.theFullTileBmpTable.[TrackerModel.MapSquareChoiceDomainHelper.DARK_X].[0])
                                icon.Opacity <- X_OPACITY
                                canvasAdd(owDarkeningMapGridCanvases.[i,j], icon, 0., 0.)  // the icon 'is' the darkening
                        else
                            let icon = Graphics.BMPtoImage iconBMP
                            icon.Opacity <- 1.0
                            drawDarkening(owDarkeningMapGridCanvases.[i,j], 0., 0)     // darken below icon and routing marks
                            canvasAdd(c, icon, 0., 0.)                                 // add icon above routing
                            for fe,x,y in extraDecorations do
                                canvasAdd(c, fe, x, y)
                let isLegalHere(state) = if state = TrackerModel.MapSquareChoiceDomainHelper.ARMOS then owInstance.HasArmos(i,j) else true
                let updateGridSpot delta phrase = 
                    async {
                        // figure out what new state we just interacted-to
                        if delta = 777 then 
                            let curState = TrackerModel.overworldMapMarks.[i,j].Current()
                            if curState = -1 then
                                // if unmarked, use voice to set new state
                                match speechRecognitionInstance.ConvertSpokenPhraseToMapCell(phrase) with
                                | Some newState -> 
                                    if isLegalHere(newState) && TrackerModel.overworldMapMarks.[i,j].AttemptToSet(newState) then
                                        if newState >=0 && newState <=8 then
                                            selectDungeonTabEvent.Trigger(newState)
                                        Graphics.PlaySoundForSpeechRecognizedAndUsedToMark()
                                        let pos = c.TranslatePoint(Point(OMTW/2., 11.*3./2.), appMainCanvas)
                                        match overworldAcceleratorTable.TryGetValue(newState) with
                                        | (true,f) -> 
                                            do! f(cm,c,i,j)
                                            Graphics.WarpMouseCursorTo(pos)
                                        | _ -> ()
                                        animateOverworldTileIfOptionIsChecked(i,j)
                                | None -> ()
                            elif MapStateProxy(curState).IsThreeItemShop && TrackerModel.getOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP)=0 then
                                // if item shop with only one item marked, use voice to set other item
                                match speechRecognitionInstance.ConvertSpokenPhraseToMapCell(phrase) with
                                | Some newState -> 
                                    if TrackerModel.MapSquareChoiceDomainHelper.IsItem(newState) then
                                        TrackerModel.setOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP,TrackerModel.MapSquareChoiceDomainHelper.ToItem(newState))
                                        Graphics.PlaySoundForSpeechRecognizedAndUsedToMark()
                                        animateOverworldTileIfOptionIsChecked(i,j)
                                | None -> ()
                        elif delta = 0 then 
                            ()
                        else failwith "bad delta"
                        redrawGridSpot()
                        } |> Async.StartImmediate
                owUpdateFunctions.[i,j] <- updateGridSpot 
                owCanvases.[i,j] <- c
                mirrorOverworldFEs.Add(c)
                mirrorOverworldFEs.Add(owDarkeningMapGridCanvases.[i,j])
                let popupIsActiveRef = ref false
                let SetNewValue(currentState, originalState) = async {
                    if isLegalHere(currentState) && TrackerModel.overworldMapMarks.[i,j].AttemptToSet(currentState) then
                        if originalState<>TrackerModel.MapSquareChoiceDomainHelper.DARK_X && originalState <> -1 then  
                            // remind destructive changes
                            RemindOverworldOverwrites(i, j, originalState, currentState, spokenOWTiles, AsyncBrieflyHighlightAnOverworldLocation(i,j))
                        if currentState >=0 && currentState <=8 then
                            selectDungeonTabEvent.Trigger(currentState)
                        match overworldAcceleratorTable.TryGetValue(currentState) with
                        | (true,f) -> do! f(cm,c,i,j)
                        | _ -> ()
                        redrawGridSpot()
                        if originalState = -1 && currentState <> -1 then OverworldRouteDrawing.routeDrawingLayer.GetHighlightTile(i,j).Hide()  // dismiss any green/yellow highlight on this tile
                        animateOverworldTileIfOptionIsChecked(i,j)
                    else
                        System.Media.SystemSounds.Asterisk.Play()  // e.g. they tried to set armos on non-armos, or tried to set Level1 when already found elsewhere
                }
                let activatePopup(activationDelta) =
                    popupIsActiveRef := true
                    let GCOL,GROW = 8,5
                    let GCOUNT = GCOL*GROW
                    let pos = c.TranslatePoint(Point(), appMainCanvas)
                    let ST = CustomComboBoxes.borderThickness
                    let tileImage = Graphics.BMPtoImage(owMapBMPs.[i,j])
                    let tileCanvas = new Canvas(Width=OMTW, Height=11.*3.)
                    let originalState = TrackerModel.overworldMapMarks.[i,j].Current()
                    let gridWidth = float(GCOL*(5*3+2*int ST)+int ST)
                    let gridxPosition = 
                        if pos.X < OMTW*2. then 
                            -ST // left align
                        elif pos.X > OMTW*13. then 
                            OMTW - gridWidth  // right align
                        else
                            (OMTW - gridWidth)/2.  // center align
                    let typicalGESAI(n) : FrameworkElement*_*_ =
                        let isSelectable = ((n = originalState) || TrackerModel.mapSquareChoiceDomain.CanAddUse(n)) && isLegalHere(n)
                        upcast Graphics.BMPtoImage(MapStateProxy(n).DefaultInteriorBmp()), isSelectable, n
                    let gridElementsSelectablesAndIDs : (FrameworkElement*bool*int)[] = [|
                        // three full rows
                        for n = 0 to 23 do
                            yield typicalGESAI(n)
                        // money row
                        yield null, false, -999  // null asks selector to 'leave a hole' here
                        for n = 24 to 29 do
                            yield typicalGESAI(n)
                        yield null, false, -999  // null asks selector to 'leave a hole' here
                        // other row
                        for n = 30 to 34 do
                            yield typicalGESAI(n)
                        yield upcast new Canvas(Width=5.*3., Height=9.*3., Background=Graphics.overworldCommonestFloorColorDarkBrush), true, 35
                        yield upcast new Canvas(Width=5.*3., Height=9.*3., Background=Graphics.overworldCommonestFloorColorBrush), true, -1
                        yield null, false, -999  // null asks selector to 'leave a hole' here
                        |]
                    let shopsOnTop = TrackerModelOptions.Overworld.ShopsFirst.Value // start with shops, rather than dungeons, on top of grid
                    let gridElementsSelectablesAndIDs = 
                        if shopsOnTop then [| yield! gridElementsSelectablesAndIDs.[16..]; yield! gridElementsSelectablesAndIDs.[..15] |] else gridElementsSelectablesAndIDs
                    let originalStateIndex = gridElementsSelectablesAndIDs |> Array.findIndex (fun (_,_,s) -> s = originalState)
                    if gridElementsSelectablesAndIDs.Length <> GCOUNT then
                        failwith "bad ow grid tile layout"
                    async {
                        let! r = CustomComboBoxes.DoModalGridSelect(cm, pos.X, pos.Y, tileCanvas,
                                    gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (GCOL, GROW, 5*3, 9*3), float(5*3)/2., float(9*3)/2., gridxPosition, 11.*3.+ST,
                                    (fun (currentState) -> 
                                        tileCanvas.Children.Clear()
                                        canvasAdd(tileCanvas, tileImage, 0., 0.)
                                        let _,bmp,_ = GetIconBMPAndExtraDecorations(cm, MapStateProxy(currentState), i, j)
                                        if bmp <> null then
                                            let icon = bmp |> Graphics.BMPtoImage
                                            if MapStateProxy(currentState).IsX then
                                                icon.Opacity <- X_OPACITY
                                            canvasAdd(tileCanvas, icon, 0., 0.)
                                        let s = if currentState = -1 then "Unmarked" else let _,_,_,s = TrackerModel.dummyOverworldTiles.[currentState] in s
                                        let s = HotKeys.OverworldHotKeyProcessor.AppendHotKeyToDescription(s,currentState)
                                        let text = new TextBox(Text=s, Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                                                                    FontSize=16., HorizontalContentAlignment=HorizontalAlignment.Center)
                                        let textBorder = 
                                            if currentState >= TrackerModel.MapSquareChoiceDomainHelper.SHOP && currentState < TrackerModel.MapSquareChoiceDomainHelper.SHOP + TrackerModel.MapSquareChoiceDomainHelper.NUM_ITEMS then
                                                let extra = new TextBox(Text="After marking a shop, left-click\nit again to mark a second item", 
                                                                            Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                                                                            FontSize=10., HorizontalContentAlignment=HorizontalAlignment.Center)
                                                let sp = new StackPanel(Orientation=Orientation.Vertical)
                                                sp.Children.Add(text) |> ignore
                                                sp.Children.Add(new DockPanel(Background=Brushes.Gray, Height=1., Margin=Thickness(12.,0.,12.,0.))) |> ignore
                                                sp.Children.Add(extra) |> ignore
                                                new Border(BorderThickness=Thickness(ST), Child=sp, Background=Brushes.Black, BorderBrush=Brushes.Gray)
                                            else
                                                new Border(BorderThickness=Thickness(ST), Child=text, Background=Brushes.Black, BorderBrush=Brushes.Gray)
                                        let dp = new DockPanel(LastChildFill=false, Width=gridWidth)
                                        DockPanel.SetDock(textBorder, Dock.Bottom)
                                        dp.Children.Add(textBorder) |> ignore
                                        Canvas.SetBottom(dp, 33.)
                                        Canvas.SetLeft(dp, gridxPosition)
                                        tileCanvas.Children.Add(dp) |> ignore
                                        ),
                                    (fun (_ea, currentState) -> CustomComboBoxes.DismissPopupWithResult(currentState)),
                                    [], CustomComboBoxes.ModalGridSelectBrushes.Defaults(), true, None)
                        match r with
                        | Some(currentState) -> do! SetNewValue(currentState, originalState)
                        | None -> ()
                        popupIsActiveRef := false
                        } |> Async.StartImmediate
                c.MouseDown.Add(fun ea -> 
                    if not !popupIsActiveRef then
                        if ea.ChangedButton = Input.MouseButton.Left then
                            if Input.Keyboard.IsKeyDown(Input.Key.LeftShift) || Input.Keyboard.IsKeyDown(Input.Key.RightShift) then
                                // shift left click
                                activateCircleLabelPopup()
                            else
                                // left click is the 'special interaction'
                                let pos = c.TranslatePoint(Point(), appMainCanvas)
                                let msp = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
                                if msp.IsX then
                                    if Graphics.shouldInitiallyHideOverworldMap then
                                        let ex = TrackerModel.getOverworldMapExtraData(i,j,msp.State)
                                        async {
                                            if ex=msp.State then
                                                // hidden -> unmarked           (and recall that unmarked left clicks to DontCare)
                                                TrackerModel.setOverworldMapExtraData(i,j,msp.State,0)
                                                do! SetNewValue(-1,msp.State)  
                                            else
                                                // DontCare -> hidden           (thus left click is a 3-cycle hidden, unmarked, DontCare)
                                                TrackerModel.setOverworldMapExtraData(i,j,msp.State,msp.State)
                                            redrawGridSpot()
                                        } |> Async.StartImmediate
                                    else
                                        activatePopup(0)  // thus, if you have unmarked, then left-click left-click pops up, as the first marks X, and the second now pops up
                                else
                                    async {
                                        let! needRedraw, needHighlightTileHide = DoLeftClick(cm,msp,i,j,pos,popupIsActiveRef)
                                        if needRedraw then 
                                            redrawGridSpot()
                                            animateOverworldTileIfOptionIsChecked(i,j)
                                        if needHighlightTileHide then OverworldRouteDrawing.routeDrawingLayer.GetHighlightTile(i,j).Hide()  // dismiss any green/yellow highlight on this tile
                                    } |> Async.StartImmediate
                        elif ea.ChangedButton = Input.MouseButton.Right then
                            if Input.Keyboard.IsKeyDown(Input.Key.LeftShift) || Input.Keyboard.IsKeyDown(Input.Key.RightShift) then
                                // shift right click cycles circle color
                                TrackerModel.nextOverworldMapCircleColor(i,j)
                                owCircleRedraws.[i,j]()
                            else
                                // right click activates the popup selector
                                activatePopup(0)
                        elif ea.ChangedButton = Input.MouseButton.Middle then
                            // middle click toggles circle
                            TrackerModel.toggleOverworldMapCircle(i,j)
                            owCircleRedraws.[i,j]()
                    )
                c.MouseWheel.Add(fun x -> 
                    if not !popupIsActiveRef then 
                        if TrackerModel.overworldMapCircles.[i,j] <> 0 then   // if there's a circle, then it takes scrollwheel priority
                            if x.Delta<0 then
                                TrackerModel.nextOverworldMapCircleColor(i,j)
                            else
                                TrackerModel.prevOverworldMapCircleColor(i,j)
                            owCircleRedraws.[i,j]()
                        else
                            activatePopup(if x.Delta<0 then 1 else -1)
                    )
                c.MyKeyAdd(fun ea ->
                    if not !popupIsActiveRef then
                        match HotKeys.OverworldHotKeyProcessor.TryGetValue(ea.Key) with
                        | Some(hotKeyedState) -> 
                            ea.Handled <- true
                            let originalState = TrackerModel.overworldMapMarks.[i,j].Current()
                            let state = OverworldMapTileCustomization.DoSpecialHotKeyHandlingForOverworldTiles(i, j, originalState, hotKeyedState)
                            async {
                                do! SetNewValue(state, originalState)
                            } |> Async.StartImmediate
                        | None -> ()
                    )
                if Graphics.shouldInitiallyHideOverworldMap then
                    TrackerModel.overworldMapMarks.[i,j].Set(TrackerModel.MapSquareChoiceDomainHelper.DARK_X)
                    TrackerModel.setOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.DARK_X,TrackerModel.MapSquareChoiceDomainHelper.DARK_X)
                    redrawGridSpot()
    if speechRecognitionInstance <> null then
        speechRecognitionInstance.AttachSpeechRecognizedToApp(appMainCanvas, (fun recognizedText ->
                                if currentlyMousedOWX >= 0 then // can hear speech before we have moused over any (uninitialized location)
                                    let c = owCanvases.[currentlyMousedOWX,currentlyMousedOWY]
                                    if c <> null && c.IsMouseOver then  // canvas can be null for always-empty grid places
                                        // Note: IsMouseOver appears only be true if we are along the way from a mouse click to the visual root.
                                        // As a result, the update never fires when a popup window is active, because the popup sunglasses would always 
                                        // intercept the click in a different part of the Visual tree.  This is good, because we don't want speech 
                                        // mutating the world while a popup is active (the popup's modality should block speech).
                                        // I guess we could also ask 'cm' if a popup is active.
                                        owUpdateFunctions.[currentlyMousedOWX,currentlyMousedOWY] 777 recognizedText
                            ))
    OptionsMenu.requestRedrawOverworldEvent.Publish.Add(fun _ ->
        for i = 0 to 15 do
            for j = 0 to 7 do
                owUpdateFunctions.[i,j] 0 null  // redraw tile
        )
    canvasAdd(overworldCanvas, owMapGrid, 0., 0.)
    let ensureRespectingOwGettableScreensAndOpenCavesCheckBoxes() =
        let showGettables = owGettableScreensCheckBox.IsChecked.HasValue && owGettableScreensCheckBox.IsChecked.Value
        let maxPale = if showGettables then OverworldRouteDrawing.All else 0
        OverworldRouteDrawing.drawPathsImpl(TrackerModel.mapStateSummary.OwRouteworthySpots, 
                    TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), Point(0.,0.), 0, 0, false, true, 0, maxPale, whetherToCyanOpenCavesOrArmos())
            
    owMapGrid.MouseLeave.Add(fun _ -> ensureRespectingOwGettableScreensAndOpenCavesCheckBoxes())

    do! showProgress("overworld finish, legend")

    let recorderingCanvas = new Canvas(Width=16.*OMTW, Height=float(8*11*3))  // really the 'extra top layer' canvas for adding final marks to overworld map
    recorderingCanvas.IsHitTestVisible <- false  // do not let this layer see/absorb mouse interactions
    canvasAdd(overworldCanvas, recorderingCanvas, 0., 0.)
    
    canvasAdd(overworldCanvas, overworldCirclesCanvas, 0., 0.)

    // legend
    let recorderDestinationButtonCanvas, anyRoadLegendIcon, dungeonLegendIconArea, updateCurrentRecorderDestinationNumeral, legendCanvas, startIcon, theCustomWaypointIcon = 
            UIComponents.MakeLegend(cm, doUIUpdateEvent)
    layout.AddLegend(legendCanvas)
    let redrawItemProgressBar, itemProgressCanvas, itemProgressTB = UIComponents.MakeItemProgressBar(owInstance)
    layout.AddItemProgress(itemProgressCanvas, itemProgressTB)

    
    // Version
    let vb = CustomComboBoxes.makeVersionButtonWithBehavior(cm)
    // vb.Click.Add(fun _ -> failwith "crash")                     // Uncomment this for crash testing
    layout.AddVersionButton(vb)

    // hint decoder
    if isStandardHyrule then   // Hints only apply to z1r and standard map zones
        layout.AddHintDecoderButton(UIComponents.MakeHintDecoderUI(cm))

    // WANT!
    let kitty = new Image()
    let imageStream = Graphics.GetResourceStream("CroppedBrianKitty.png")
    kitty.Source <- System.Windows.Media.Imaging.BitmapFrame.Create(imageStream)
    kitty.Width <- THRU_MAIN_MAP_AND_ITEM_PROGRESS_H - THRU_MAIN_MAP_H
    kitty.Height <- THRU_MAIN_MAP_AND_ITEM_PROGRESS_H - THRU_MAIN_MAP_H
    let ztlogo = new Image()
    let imageStream = Graphics.GetResourceStream("ZTlogo64x64.png")
    ztlogo.Source <- System.Windows.Media.Imaging.BitmapFrame.Create(imageStream)
    ztlogo.Width <- 40.
    ztlogo.Height <- 40.
    let logoBorder = new Border(BorderThickness=Thickness(1.), BorderBrush=Brushes.Gray, Child=ztlogo)
    layout.AddKittyAndLogo(kitty, logoBorder, ztlogo)
    

    // show hotkeys button
    let showHotKeysTB = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Graphics.almostBlack, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Show HotKeys", IsHitTestVisible=false)
    let showHotKeysButton = new Button(Content=showHotKeysTB)
    layout.AddShowHotKeysButton(showHotKeysButton)
    let showHotKeys(isRightClick) =
        let none,p = OverworldMapTileCustomization.MakeMappedHotKeysDisplay()
        let w = new Window(Title="Z-Tracker HotKeys", Owner=Application.Current.MainWindow, Content=p, ResizeMode=ResizeMode.CanResizeWithGrip)
        let save() = 
            TrackerModelOptions.HotKeyWindowLTWH <- sprintf "%d,%d,%d,%d" (int w.Left) (int w.Top) (int w.Width) (int w.Height)
            TrackerModelOptions.writeSettings()
        let leftTopWidthHeight = TrackerModelOptions.HotKeyWindowLTWH
        let matches = System.Text.RegularExpressions.Regex.Match(leftTopWidthHeight, """^(-?\d+),(-?\d+),(\d+),(\d+)$""")
        if not none && not isRightClick && matches.Success then
            w.Left <- float matches.Groups.[1].Value
            w.Top <- float matches.Groups.[2].Value
            w.Width <- float matches.Groups.[3].Value
            w.Height <- float matches.Groups.[4].Value
        else
            p.Measure(Size(1280., 720.))
            w.Width <- p.DesiredSize.Width + 16.
            w.Height <- p.DesiredSize.Height + 40.
        w.SizeChanged.Add(fun _ -> save(); refocusMainWindow())
        w.LocationChanged.Add(fun _ -> save(); refocusMainWindow())
        w.Show()
    showHotKeysButton.Click.Add(fun _ -> showHotKeys(false))
    showHotKeysButton.MouseRightButtonDown.Add(fun _ -> showHotKeys(true))

    // show/run custom button
    let showRunCustomTB = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Graphics.almostBlack, IsReadOnly=true, BorderThickness=Thickness(0.), 
                                        Text="Show/Run\nCustom", IsHitTestVisible=false, TextAlignment=TextAlignment.Center)
    let showRunCustomButton = new Button(Content=showRunCustomTB)
    layout.AddShowRunCustomButton(showRunCustomButton)
    showRunCustomButton.Click.Add(fun _ -> ShowRunCustom.DoShowRunCustom(refocusMainWindow))

    let saveTB = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Graphics.almostBlack, IsReadOnly=true, BorderThickness=Thickness(0.), 
                                        Text="Save", IsHitTestVisible=false, TextAlignment=TextAlignment.Center)
    let saveButton = new Button(Content=saveTB)
    layout.AddSaveButton(saveButton)
    saveButton.Click.Add(fun _ -> 
        if not popupIsActive then
            popupIsActive <- true
            async {
                try
                    let filename = MakeManualSave()
                    let filename = System.IO.Path.GetFileName(filename)  // remove directory info (could have username in path, don't display PII on-screen)
                    let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Information, sprintf "Z-Tracker data saved to file\n%s" filename, ["Ok"])
                    ignore r
                    popupIsActive <- false
                with e ->
                    let! r = CustomComboBoxes.DoModalMessageBox(cm, System.Drawing.SystemIcons.Error, sprintf "Z-Tracker was unable to save the\ntracker state to a file\nError:\n%s" e.Message, ["Ok"])
                    ignore r
                    popupIsActive <- false
            } |> Async.StartImmediate
        )

    let uccTB = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Graphics.almostBlack, IsReadOnly=true, BorderThickness=Thickness(0.), 
                                        Text="UCC", IsHitTestVisible=false, TextAlignment=TextAlignment.Center)
    let uccButton = new Button(Content=uccTB)
    if System.IO.File.Exists(UserCustomLayer.checklistFilename) then
        layout.AddUserCustomContentButton(uccButton)
        uccButton.Click.Add(fun _ -> 
            if not popupIsActive then
                popupIsActive <- true
                async {
                    do! UserCustomLayer.InteractWithUserCustom(cm, timelineItems, invokeExtras)
                    popupIsActive <- false
                    } |> Async.StartImmediate
            )

#if NOT_RACE_LEGAL
    // minimap overlay button
    let minimapOverlayTB = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Graphics.almostBlack, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Minimap overlay", IsHitTestVisible=false)
    let minimapOverlayButton = new Button(Content=minimapOverlayTB)
    canvasAdd(appMainCanvas, minimapOverlayButton, 16.*OMTW - kitty.Width - 115., THRU_MAIN_MAP_H + 22.)
    let mutable minimapOverlay = fun _irc -> ()
    minimapOverlayButton.Click.Add(fun _ -> minimapOverlay(false))
    minimapOverlayButton.MouseRightButtonDown.Add(fun _ -> minimapOverlay(true))

    // near-mouse HUD button
    let nearMouseHUDTB = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Graphics.almostBlack, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Near-mouse HUD", IsHitTestVisible=false)
    let nearMouseHUDButton = new Button(Content=nearMouseHUDTB)
    canvasAdd(appMainCanvas, nearMouseHUDButton, 16.*OMTW - kitty.Width - 115., THRU_MAIN_MAP_H + 44.)
    let mutable nearMouseHUD = fun _irc -> ()
    nearMouseHUDButton.Click.Add(fun _ -> nearMouseHUD(false))
    nearMouseHUDButton.MouseRightButtonDown.Add(fun _ -> nearMouseHUD(true))
#endif

    do! showProgress("misc")

    let blockerDungeonSunglasses : FrameworkElement[] = Array.zeroCreate 8
    let mutable oneTimeRemindLadder, oneTimeRemindAnyKey = None, None
    doUIUpdateEvent.Publish.Add(fun () ->
//        let diagSW = System.Diagnostics.Stopwatch.StartNew()
        if displayIsCurrentlyMirrored <> TrackerModelOptions.Overworld.MirrorOverworld.Value then
            // model changed, align the view
            displayIsCurrentlyMirrored <- not displayIsCurrentlyMirrored
            if displayIsCurrentlyMirrored then
                for fe in mirrorOverworldFEs do
                    fe.RenderTransform <- new ScaleTransform(-1., 1., fe.ActualWidth/2., fe.ActualHeight/2.)
            else
                for fe in mirrorOverworldFEs do
                    fe.RenderTransform <- null
        // redraw triforce display (some may have located/unlocated/hinted)
        updateNumberedTriforceDisplayIfItExists()
        // redraw white/magical swords (may have located/unlocated/hinted)
        redrawWhiteSwordCanvas(white_sword_canvas)
        redrawMagicalSwordCanvas(mags_canvas)
        // update specific-blockers that may have been (un)blocked
        for f in Views.redrawBoxes do f()
        for f in Views.redrawTriforces do f()
        // update overworld marks (may be hiding useless shops, and they just got an item rendering a shop useless (e.g. magic key and key shops))
        OptionsMenu.requestRedrawOverworldEvent.Trigger()

        recorderingCanvas.Children.Clear()
        // TODO event for redraw item progress? does any of this event interface make sense? hmmm
        redrawItemProgressBar()

        TrackerModel.allUIEventingLogic( {new TrackerModel.ITrackerEvents with
            member _this.CurrentHearts(h) = currentMaxHeartsTextBox.Text <- sprintf "Max Hearts: %d" h
            member _this.AnnounceConsiderSword2() = RemindSword2()
            member _this.AnnounceConsiderSword3() = 
                SendReminderImpl(TrackerModel.ReminderCategory.SwordHearts, "Consider the magical sword", [upcb(Graphics.iconRightArrow_bmp); upcb(Graphics.magical_sword_bmp)],
                                    Some(AsyncBrieflyHighlightAnOverworldLocation(TrackerModel.mapStateSummary.Sword3Location)))
            member _this.OverworldSpotsRemaining(remain,gettable) = 
                owRemainingScreensTextBox.Text <- sprintf "%d OW spots left" remain
                owGettableScreensTextBox.Text <- sprintf "%d gettable" gettable
            member _this.DungeonLocation(i,x,y,hasTri,isCompleted) =
                if isCompleted then
                    drawCompletedDungeonHighlight(recorderingCanvas,float x,y,(TrackerModel.IsHiddenDungeonNumbers() && TrackerModel.GetDungeon(i).LabelChar<>'?'))
                owUpdateFunctions.[x,y] 0 null
            member _this.AnyRoadLocation(i,x,y) = ()
            member _this.Armos(x,y)  = owUpdateFunctions.[x,y] 0 null  // redraw the tile, to update bright/dark or remove icon if player hides useless icons
            member _this.Sword3(x,y) = owUpdateFunctions.[x,y] 0 null  // redraw the tile, to update bright/dark or remove icon if player hides useless icons
            member _this.Sword2(x,y) = owUpdateFunctions.[x,y] 0 null  // redraw the tile, to update bright/dark or remove icon if player hides useless icons
            member _this.RoutingInfo(haveLadder,haveRaft,currentRecorderWarpDestinations,currentAnyRoadDestinations,_owRouteworthySpots) = 
                // clear and redraw routing
                OverworldRouting.repopulate(haveLadder,haveRaft,currentRecorderWarpDestinations|>Seq.map fst,currentAnyRoadDestinations,displayIsCurrentlyMirrored)
                // redraw recorder dests
                for (i,j),idx in currentRecorderWarpDestinations do
                    //let L = 6.0
                    //let bg = new Shapes.Ellipse(Width=float(11*3)-2.+2.0*L, Height=float(11*3)-2.+L, Stroke=Brushes.Black, StrokeThickness=3.0, IsHitTestVisible=false)
                    let bg = new Shapes.Rectangle(Width=35., Height=35., Stroke=Brushes.Black, StrokeThickness=3.0, IsHitTestVisible=false, RenderTransform=new RotateTransform(45.))
                    bg.Effect <- new Effects.BlurEffect(Radius=5.0, KernelType=Effects.KernelType.Gaussian)
                    //let fg = new Shapes.Ellipse(Width=float(11*3)-2.+2.0*L, Height=float(11*3)-2.+L, Stroke=UIComponents.RecorderEllipseColor(), StrokeThickness=3.0, IsHitTestVisible=false)
                    let fg = new Shapes.Rectangle(Width=35., Height=35., Stroke=UIComponents.RecorderEllipseColor(), StrokeThickness=3.0, IsHitTestVisible=false, RenderTransform=new RotateTransform(45.))
                    if Graphics.canUseEffectsWithoutDestroyingPerformance then
                        //canvasAdd(recorderingCanvas, bg, OMTW*float i+7.-L+1., float(11*3*j)-L/2.+1.)
                        canvasAdd(recorderingCanvas, bg, OMTW*float i+23., float(11*3*j)-9.)
                    //canvasAdd(recorderingCanvas, fg, OMTW*float i+7.-L+1., float(11*3*j)-L/2.+1.)
                    canvasAdd(recorderingCanvas, fg, OMTW*float i+23., float(11*3*j)-9.)
                    if not(TrackerModel.recorderToNewDungeons) then
                        // label the vanilla spots, so player knows the order
                        let tb = new TextBox(Text=sprintf "%c" (char idx + char '1'), FontSize=12., FontWeight=FontWeights.Bold, Foreground=Brushes.White, Background=Brushes.Black, 
                                                IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.))
                        recorderingCanvas.Children.Add(tb) |> ignore
                        Canvas.SetLeft(tb, OMTW*float i+2.)
                        Canvas.SetBottom(tb, float(11*3*(7-j))+2.)
                // redraw routes and highlights
                let pos = System.Windows.Input.Mouse.GetPosition(overworldCanvas)
                let i,j = int(Math.Floor(pos.X / OMTW)), int(Math.Floor(pos.Y / (11.*3.)))
                if i>=0 && i<16 && j>=0 && j<8 then
                    drawRoutesTo(currentRouteTarget(), Point(0.,0.), i, j, TrackerModelOptions.Overworld.DrawRoutes.Value, 
                                    (if TrackerModelOptions.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxGYR else 0),
                                    (if TrackerModelOptions.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxGYR else 0),
                                    whetherToCyanOpenCavesOrArmos())
                else
                    ensureRespectingOwGettableScreensAndOpenCavesCheckBoxes()
            member _this.AnnounceCompletedDungeon(i) = 
                let icons = [upcb(MapStateProxy(i).DefaultInteriorBmp()); upcb(Graphics.iconCheckMark_bmp)]
                if TrackerModel.IsHiddenDungeonNumbers() then
                    let labelChar = TrackerModel.GetDungeon(i).LabelChar
                    if labelChar <> '?' then
                        SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, sprintf "Dungeon %c is complete" labelChar, icons)
                    else
                        SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "This dungeon is complete", icons)
                else
                    SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, sprintf "Dungeon %d is complete" (i+1), icons)
            member _this.CompletedDungeons(a) =
                for i = 0 to 7 do
                    // top ui
                    for j = 1 to 4 do
                        mainTrackerCanvases.[i,j].Children.Remove(mainTrackerCanvasShaders.[i,j]) |> ignore
                    if a.[i] then
                        for j = 1 to 4 do  // don't shade the color swatches
                            mainTrackerCanvases.[i,j].Children.Add(mainTrackerCanvasShaders.[i,j]) |> ignore
                    // blockers ui
                    if a.[i] then
                        blockerDungeonSunglasses.[i].Opacity <- 0.3
                    else
                        blockerDungeonSunglasses.[i].Opacity <- 1.
            member _this.AnnounceFoundDungeonCount(n) = RemindFoundDungeonCount(n)
            member _this.AnnounceTriforceCount(n) = RemindTriforceCount(n, AsyncBrieflyHighlightAnOverworldLocation)
            member _this.AnnounceTriforceAndGo(triforceCount, tagSummary) = 
                let needSomeThingsicons = [
                    for _i = 1 to tagSummary.MissingDungeonCount do
                        yield upcb(Graphics.greyscale Graphics.genericDungeonInterior_bmp)
                    if not tagSummary.HaveBow then
                        yield upcb(Graphics.greyscale Graphics.bow_bmp)
                    if not tagSummary.HaveSilvers && not tagSummary.SilversKnownToBeInLevel9 then
                        yield upcb(Graphics.greyscale Graphics.silver_arrow_bmp)
                    ]
                let triforceAndGoIcons = [
                    if triforceCount<>8 then
                        if needSomeThingsicons.Length<>0 then
                            yield upcb(Graphics.iconRightArrow_bmp)
                        for _i = 1 to (8-triforceCount) do
                            yield upcb(Graphics.greyTriforce_bmp)
                    yield upcb(Graphics.iconRightArrow_bmp)
                    if not tagSummary.HaveSilvers && tagSummary.SilversKnownToBeInLevel9 then
                        yield upcb(Graphics.greyscale Graphics.silver_arrow_bmp)
                    yield upcb(Graphics.ganon_bmp)
                    ]
                let icons = [yield! needSomeThingsicons; yield! triforceAndGoIcons]
                let go = if triforceCount=8 then "go time" else "triforce and go"
                let silverDisclaimer = if not tagSummary.HaveSilvers && tagSummary.SilversKnownToBeInLevel9 then ", but you will need the silver arrows from 9" else ""
                match tagSummary.Level with
                | 101 -> SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "You might be "+go+silverDisclaimer, icons)
                | 102 -> SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "You are probably "+go+silverDisclaimer, icons)
                | 103 -> SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, "You are "+go+silverDisclaimer, icons)
                | 0 -> ()
                | _ -> SendReminder(TrackerModel.ReminderCategory.DungeonFeedback, (sprintf "You need %s to be " (if needSomeThingsicons.Length>1 then "some things" else "something"))+go, icons)
            member _this.RemindUnblock(blockerType, dungeons, detail) =
                let name(d) =
                    if TrackerModel.IsHiddenDungeonNumbers() then
                        (char(int 'A' + d)).ToString()
                    else
                        (1+d).ToString()
                let icons = ResizeArray()
                let mutable sentence = "Now that you have"
                match blockerType with
                | TrackerModel.DungeonBlocker.COMBAT ->
                    let words = ResizeArray()
                    for d in detail do
                        match d with
                        | TrackerModel.CombatUnblockerDetail.BETTER_SWORD -> words.Add(" a better sword,"); icons.Add(upcb(Graphics.swordLevelToBmp(TrackerModel.playerComputedStateSummary.SwordLevel)))
                        | TrackerModel.CombatUnblockerDetail.BETTER_ARMOR -> words.Add(" better armor,"); icons.Add(upcb(Graphics.ringLevelToBmp(TrackerModel.playerComputedStateSummary.RingLevel)))
                        | TrackerModel.CombatUnblockerDetail.WAND -> words.Add(" the wand,"); icons.Add(upcb(Graphics.wand_bmp))
                    sentence <- sentence + System.String.Concat words
                | TrackerModel.DungeonBlocker.BOW_AND_ARROW -> sentence <- sentence + " a beau and arrow,"; icons.Add(upcb(Graphics.bow_and_arrow_bmp))
                | TrackerModel.DungeonBlocker.RECORDER -> sentence <- sentence + " the recorder,"; icons.Add(upcb(Graphics.recorder_bmp))
                | TrackerModel.DungeonBlocker.LADDER -> sentence <- sentence + " the ladder,"; icons.Add(upcb(Graphics.ladder_bmp))
                | TrackerModel.DungeonBlocker.KEY -> sentence <- sentence + " the magic key,"; icons.Add(upcb(Graphics.key_bmp))
                | TrackerModel.DungeonBlocker.BOMB -> sentence <- sentence + " bombs,"; icons.Add(upcb(Graphics.bomb_bmp))
                | _ -> ()
                sentence <- sentence + " consider dungeon" + (if Seq.length dungeons > 1 then "s " else " ")
                icons.Add(upcb(Graphics.iconRightArrow_bmp))
                let d = Seq.head dungeons
                sentence <- sentence + name(d)
                icons.Add(upcb(MapStateProxy(d).DefaultInteriorBmp()))
                for d in Seq.tail dungeons do
                    sentence <- sentence + " and " + name(d)
                    icons.Add(upcb(MapStateProxy(d).DefaultInteriorBmp()))
                SendReminder(TrackerModel.ReminderCategory.Blockers, sentence, icons)
            member _this.RemindShortly(itemId) = 
                if itemId = TrackerModel.ITEMS.KEY then
                    oneTimeRemindAnyKey <- Some(new TrackerModel.LastChangedTime(), (fun() ->
                        oneTimeRemindAnyKey <- None
                        if TrackerModel.playerComputedStateSummary.HaveAnyKey then
                            SendReminder(TrackerModel.ReminderCategory.HaveKeyLadder, "Don't forget that you have the magic key", [upcb(Graphics.key_bmp)])
                        else
                            TrackerModel.remindedAnyKey <- false))
                elif itemId = TrackerModel.ITEMS.LADDER then
                    oneTimeRemindLadder <- Some(new TrackerModel.LastChangedTime(), (fun() ->
                        oneTimeRemindLadder <- None
                        if TrackerModel.playerComputedStateSummary.HaveLadder then
                            SendReminder(TrackerModel.ReminderCategory.HaveKeyLadder, "Don't forget that you have the ladder", [upcb(Graphics.ladder_bmp)])
                        else
                            TrackerModel.remindedLadder <- false))
                else
                    failwith "bad reminder"
            })
        // place start icon in top layer at very top (above e.g. completed dungeon highlight)
        if TrackerModel.startIconX <> -1 then
            canvasAdd(recorderingCanvas, startIcon, 11.5*OMTW/48.-3.+OMTW*float(TrackerModel.startIconX), float(TrackerModel.startIconY*11*3))
        if TrackerModel.customWaypointX <> -1 then
            canvasAdd(recorderingCanvas, theCustomWaypointIcon, -2.+OMTW*float(TrackerModel.customWaypointX), float(TrackerModel.customWaypointY*11*3)-5.)
//        if diagSW.ElapsedMilliseconds > 100L then
//            printfn "doUIUpdate %dms" diagSW.ElapsedMilliseconds
        )
    let threshold = TimeSpan.FromMilliseconds(500.0)
    let periodicReminders = new PeriodicReminders()
    let timer = new System.Windows.Threading.DispatcherTimer()
    timer.Interval <- TimeSpan.FromSeconds(1.0)
    timer.Tick.Add(fun _ -> 
        if not(TrackerModel.LastChangedTime.IsPaused) then
            let hasUISettledDown = 
                DateTime.Now - TrackerModel.playerProgressLastChangedTime.Time > threshold &&
                DateTime.Now - TrackerModel.dungeonsAndBoxesLastChangedTime.Time > threshold &&
                DateTime.Now - TrackerModel.mapLastChangedTime.Time > threshold
            if hasUISettledDown then
                let hasTheModelChanged = TrackerModel.recomputeWhatIsNeeded()  
                if hasTheModelChanged then
                    doUIUpdateEvent.Trigger()
            stepAnimateLink()
            periodicReminders.Check()
            // one-time reminders
            match oneTimeRemindAnyKey with
            | None -> ()
            | Some(lct, thunk) ->
                if (DateTime.Now - lct.Time).Minutes > 1 then  // 2 min
                    thunk()
            match oneTimeRemindLadder with
            | None -> ()
            | Some(lct, thunk) ->
                if (DateTime.Now - lct.Time).Minutes > 1 then  // 2 min
                    thunk()
        )

    // create overworld locator stuff (added to correct layer of visual tree later in the code)
    let owLocatorGrid = makeGrid(16, 8, int OMTW, 11*3)
    let owLocatorCanvas = new Canvas()
    for i = 0 to 15 do
        for j = 0 to 7 do
            let z = new Graphics.TileHighlightRectangle()
            z.Hide()
            owLocatorTileRectangles.[i,j] <- z
            gridAdd(owLocatorGrid, z.Shape, i, j)

    // Dungeon level trackers
    let rightwardCanvas = new Canvas()
    let levelTabSelected = new Event<_>()  // blockers listens, to subtly highlight a dungeon
    let blockersHoverEvent = new Event<bool>()
    let contentCanvasMouseEnterFunc(level) =
        if level>=10 then // 10+ = summary tab, show all dungeon locations; 11 means moused over 1, 12 means 2, ...
            for i = 0 to 15 do
                for j = 0 to 7 do
                    let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                    if cur >= TrackerModel.MapSquareChoiceDomainHelper.DUNGEON_1 && cur <= TrackerModel.MapSquareChoiceDomainHelper.DUNGEON_9 then
                        let curLevel = cur-TrackerModel.MapSquareChoiceDomainHelper.DUNGEON_1 // 0-8
                        if (curLevel = level-11) || (level=10) then  // if hovering this particular dungeon within summary tab, or if hovering 'S' header
                            owLocatorTileRectangles.[i,j].MakeGreenWithBriefAnimation()
                        elif not(TrackerModel.GetDungeon(curLevel).IsComplete) then
                            owLocatorTileRectangles.[i,j].MakeBoldGreen()
                        else
                            () // do nothing - don't highlight completed dungeons
            drawRoutesTo(None, Point(), 0, 0, false, 0, 
                (if owGettableScreensCheckBox.IsChecked.HasValue && owGettableScreensCheckBox.IsChecked.Value then OverworldRouteDrawing.MaxGYR else 0),
                whetherToCyanOpenCavesOrArmos())
        else
            let i,j = TrackerModel.mapStateSummary.DungeonLocations.[level-1]
            if (i,j) <> TrackerModel.NOTFOUND then
                // when mouse in a dungeon map, show its location...
                showLocatorExactLocation(TrackerModel.mapStateSummary.DungeonLocations.[level-1])
                // ...and behave like we are moused there
                drawRoutesTo(None, Point(), i, j, TrackerModelOptions.Overworld.DrawRoutes.Value, 
                                    (if TrackerModelOptions.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxGYR else 0),
                                    (if TrackerModelOptions.Overworld.HighlightNearby.Value then OverworldRouteDrawing.MaxGYR else 0),
                                    whetherToCyanOpenCavesOrArmos())
    level9ColorCanvas.MouseEnter.Add(fun _ -> contentCanvasMouseEnterFunc(10))
    level9ColorCanvas.MouseLeave.Add(fun _ -> hideLocator())
    let! dungeonTabs,posToWarpToWhenTabbingFromOverworld,grabModeTextBlock,exportDungeonModelsJsonLinesF,importDungeonModels = 
        DungeonUI.makeDungeonTabs(cm, (fun x -> layout.AddDungeonTabs(x)), (fun () -> layout.GetDungeonY()), selectDungeonTabEvent, TH, rightwardCanvas,
                                    levelTabSelected, blockersHoverEvent, mainTrackerGhostbusters, showProgress, contentCanvasMouseEnterFunc, (fun _level -> hideLocator()))
    exportDungeonModelsJsonLines <- exportDungeonModelsJsonLinesF
    layout.AddDungeonTabsOverlay(dungeonTabsOverlay)

    do! showProgress("blockers")
    
    // blockers
    let blockerGrid = UIComponents.MakeBlockers(cm, blockerQueries, levelTabSelected, blockersHoverEvent, blockerDungeonSunglasses, contentCanvasMouseEnterFunc, (fun _level -> hideLocator()))
    layout.AddBlockers(blockerGrid)

    do! showProgress("notes, gettables")
    // notes    
    notesTextBox <- new TextBox(Width=appMainCanvas.Width-BLOCKERS_AND_NOTES_OFFSET, Height=dungeonTabs.Height - blockerGrid.Height,
                            FontSize=20., Foreground=Brushes.LimeGreen , Background=Brushes.Black, AcceptsReturn=true)
    let notesFilename = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Notes.txt")
    if not(System.IO.File.Exists(notesFilename)) then
        notesTextBox.Text <- "Notes\n"
    else
        notesTextBox.Text <- System.IO.File.ReadAllText(notesFilename)
    let seedAndFlagsDisplayCanvas = new Canvas(Width=notesTextBox.Width, Height=notesTextBox.Height)
    layout.AddNotesSeedsFlags(notesTextBox, seedAndFlagsDisplayCanvas)
    let seedAndFlagsTB = new TextBox(FontSize=16., Background=Brushes.Transparent, Foreground=Brushes.Orange, 
                                        IsHitTestVisible=false, IsReadOnly=true, Focusable=false, Text="\n", BorderThickness=Thickness(0.))
    Canvas.SetRight(seedAndFlagsTB, 0.)
    Canvas.SetBottom(seedAndFlagsTB, 3.)
    seedAndFlagsDisplayCanvas.Children.Add(seedAndFlagsTB) |> ignore
    SaveAndLoad.seedAndFlagsUpdated.Publish.Add(fun _ ->
        if TrackerModelOptions.DisplaySeedAndFlags.Value then
            seedAndFlagsTB.Text <- sprintf "Seed & Flags: %s\n%s" SaveAndLoad.lastKnownSeed SaveAndLoad.lastKnownFlags
        else
            seedAndFlagsTB.Text <- "\n"
        )

    grabModeTextBlock.Opacity <- 0.
    grabModeTextBlock.Width <- notesTextBox.Width
    layout.AddExtraDungeonRightwardStuff(grabModeTextBlock, rightwardCanvas)

    // remaining OW spots
    layout.AddOWRemainingScreens(owRemainingScreensTextBoxContainerPanelThatSeesMouseEvents)
    owRemainingScreensTextBoxContainerPanelThatSeesMouseEvents.MouseEnter.Add(fun _ ->
        let unmarked = TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1)
        OverworldRouteDrawing.drawPathsImpl(unmarked, unmarked, Point(0.,0.), 0, 0, false, true, OverworldRouteDrawing.All, 0, NoCyan)
        if not(TrackerModel.playerComputedStateSummary.HaveRaft) then
            // drawPathsImpl cannot reach the raft locations and won't color them, so just ad-hoc those two spots
            for i,j in [5,4 ; 15,2] do
                let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                if cur = -1 then  // they may have marked the raft spots (e.g. Any Road), so only if unmarked...
                    OverworldRouteDrawing.routeDrawingLayer.GetHighlightTile(i,j).MakePaleRed()
        )
    owRemainingScreensTextBoxContainerPanelThatSeesMouseEvents.MouseLeave.Add(fun _ -> ensureRespectingOwGettableScreensAndOpenCavesCheckBoxes())
    if isStandardHyrule then   // Gettables only makes sense in standard map
        layout.AddOWGettableScreens(owGettableScreensCheckBox)
    else
        owGettableScreensCheckBox.IsChecked <- false
    owGettableScreensCheckBox.Checked.Add(fun _ -> TrackerModel.forceUpdate()) 
    owGettableScreensCheckBox.Unchecked.Add(fun _ -> TrackerModel.forceUpdate())
    owGettableScreensCheckBox.MouseEnter.Add(fun _ -> 
        OverworldRouteDrawing.drawPathsImpl(TrackerModel.mapStateSummary.OwRouteworthySpots, 
                                            TrackerModel.overworldMapMarks |> Array2D.map (fun cell -> cell.Current() = -1), Point(0.,0.), 0, 0, false, true, 0, OverworldRouteDrawing.All, NoCyan)
        )
    owGettableScreensCheckBox.MouseLeave.Add(fun _ -> ensureRespectingOwGettableScreensAndOpenCavesCheckBoxes())

    do! showProgress("coords/zone overlays")

    // current max hearts
    layout.AddCurrentMaxHearts(currentMaxHeartsTextBox)
    // coordinate grid
    let placeholderCanvas = new Canvas()  // for startup perf, only add in coords & zone overlays on demand
    let zoneCanvas = new Canvas()
    let owCoordsGrid = makeGrid(16, 8, int OMTW, 11*3)
    owCoordsGrid.Opacity <- 0.
    let mutable placeholderFinished = false
    let ensurePlaceholderFinished() =
        if not placeholderFinished then
            placeholderFinished <- true
            canvasAdd(placeholderCanvas, owCoordsGrid, 0., 0.)
            canvasAdd(placeholderCanvas, zoneCanvas, 0., 0.)
    for i = 0 to 15 do
        for j = 0 to 7 do
            // https://stackoverflow.com/questions/4114590/how-to-make-wpf-text-on-aero-glass-background-readable
            let tbb = new TextBox(Text=sprintf "%c  %d" (char (int 'A' + j)) (i+1),
                                    Foreground=Brushes.Black, Background=Brushes.Transparent, BorderThickness=Thickness(0.0), 
                                    FontFamily=FontFamily("Consolas"), FontSize=16.0, FontWeight=FontWeights.Bold, IsHitTestVisible=false)
            tbb.Effect <- new Effects.BlurEffect(Radius=5.0, KernelType=Effects.KernelType.Gaussian)
            let tb = new TextBox(Text=sprintf "%c  %d" (char (int 'A' + j)) (i+1),
                                    Foreground=Brushes.White, Background=Brushes.Transparent, BorderThickness=Thickness(0.0), 
                                    FontFamily=FontFamily("Consolas"), FontSize=16.0, FontWeight=FontWeights.Bold, IsHitTestVisible=false)
            let c = new Canvas(Width=OMTW, Height=float(11*3))
            if Graphics.canUseEffectsWithoutDestroyingPerformance then
                canvasAdd(c, tbb, 2., 6.)
            canvasAdd(c, tb, 2., 6.)
            gridAdd(owCoordsGrid, c, i, j) 
    mirrorOverworldFEs.Add(owCoordsGrid)
    canvasAdd(overworldCanvas, placeholderCanvas, 0., 0.)
    let showCoords = new TextBox(Text="Coords",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false)
    let cb = new CheckBox(Content=showCoords)
    cb.IsChecked <- System.Nullable.op_Implicit false
    cb.Checked.Add(fun _ -> ensurePlaceholderFinished(); owCoordsGrid.Opacity <- 0.85)
    cb.Unchecked.Add(fun _ -> owCoordsGrid.Opacity <- 0.0)
    cb.MouseEnter.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then (ensurePlaceholderFinished(); owCoordsGrid.Opacity <- 0.85))
    cb.MouseLeave.Add(fun _ -> if not cb.IsChecked.HasValue || not cb.IsChecked.Value then owCoordsGrid.Opacity <- 0.0)
    layout.AddShowCoords(cb)

    // zone overlay
    let zone_checkbox, addZoneName, changeZoneOpacity, allOwMapZoneBlackCanvases = 
        if isStandardHyrule then   // Zones only makes sense in standard map
            UIComponents.MakeZoneOverlay(zoneCanvas, ensurePlaceholderFinished, mirrorOverworldFEs)
        else
            new CheckBox(IsChecked=false), (fun _ -> ()), (fun _ -> ()), Array2D.init 16 8 (fun _ _ -> new Canvas())
    if isStandardHyrule then
        layout.AddOWZoneOverlay(zone_checkbox)

    // mouse hover explainer
    layout.AddMouseHoverExplainer(UIComponents.MakeMouseHoverExplainer(appMainCanvas))

    do! showProgress("locators/timeline/reminders")

    canvasAdd(overworldCanvas, owLocatorGrid, 0., 0.)
    canvasAdd(overworldCanvas, owLocatorCanvas, 0., 0.)

    showLocatorExactLocation <- (fun (x,y) ->
        if (x,y) <> TrackerModel.NOTFOUND then
            let OPA = 0.85
            let COL = Brushes.Cyan
            let leftLine = new Shapes.Line(X1=OMTW*float x, Y1=0., X2=OMTW*float x, Y2=float(8*11*3), Stroke=COL, StrokeThickness=2., IsHitTestVisible=false, Opacity=OPA)
            canvasAdd(owLocatorCanvas, leftLine, 0., 0.)
            let rightLine = new Shapes.Line(X1=OMTW*float (x+1)-1., Y1=0., X2=OMTW*float (x+1)-1., Y2=float(8*11*3), Stroke=COL, StrokeThickness=2., IsHitTestVisible=false, Opacity=OPA)
            canvasAdd(owLocatorCanvas, rightLine, 0., 0.)
            let topLine = new Shapes.Line(X1=0., Y1=float(y*11*3), X2=OMTW*float(16*3), Y2=float(y*11*3), Stroke=COL, StrokeThickness=2., IsHitTestVisible=false, Opacity=OPA)
            canvasAdd(owLocatorCanvas, topLine, 0., 0.)
            let bottomLine = new Shapes.Line(X1=0., Y1=float((y+1)*11*3)-1., X2=OMTW*float(16*3), Y2=float((y+1)*11*3)-1., Stroke=COL, StrokeThickness=2., IsHitTestVisible=false, Opacity=OPA)
            canvasAdd(owLocatorCanvas, bottomLine, 0., 0.)
        )
    showLocatorHintedZone <- (fun (hinted_zone, alsoHighlightABCDEFGH) ->
        OverworldRouteDrawing.routeDrawingLayer.Clear()
        if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
            // have hint, so draw that zone...
            let inZone = Array2D.init 16 8 (fun x y -> OverworldData.owMapZone.[y].[x] = hinted_zone.AsDataChar())
            for x = 0 to 15 do
                for y = 0 to 7 do
                    if not(inZone.[x,y]) then
                        allOwMapZoneBlackCanvases.[x,y].Opacity <- 0.7
            let isZone(x,y) = inZone.[max 0 (min 15 x), max 0 (min 7 y)]  // if x or y out of bounds, grab nearby in-bounds value
            let ST, OFF, COLOR = 3., 3./2., Brushes.LightGray
            for x = 0 to 15 do
                for y = 0 to 7 do
                    let mkLine(x1,y1,x2,y2) = 
                        let line = new Shapes.Line(X1=x1, Y1=y1, X2=x2, Y2=y2, Stroke=COLOR, StrokeThickness=ST, IsHitTestVisible=false)
                        line.StrokeDashArray <- new DoubleCollection( seq[1.; 1.5] )
                        line
                    if isZone(x,y) && not(isZone(x-1,y)) then  // left
                        let line = mkLine(OMTW*float(x)-OFF, float(y*11*3), OMTW*float(x)-OFF, float((y+1)*11*3))
                        canvasAdd(owLocatorCanvas, line, 0., 0.)
                    if isZone(x,y) && not(isZone(x+1,y)) then  // right
                        let line = mkLine(OMTW*float(x+1)+OFF, float(y*11*3), OMTW*float(x+1)+OFF, float((y+1)*11*3))
                        canvasAdd(owLocatorCanvas, line, 0., 0.)
                    if isZone(x,y) && not(isZone(x,y-1)) then  // top
                        let line = mkLine(OMTW*float(x), float(y*11*3)-OFF, OMTW*float(x+1), float(y*11*3)-OFF)
                        canvasAdd(owLocatorCanvas, line, 0., 0.)
                    if isZone(x,y) && not(isZone(x,y+1)) then  // bottom
                        let line = mkLine(OMTW*float(x), float((y+1)*11*3)+OFF, OMTW*float(x+1), float((y+1)*11*3)+OFF)
                        canvasAdd(owLocatorCanvas, line, 0., 0.)
            for i = 0 to 15 do
                for j = 0 to 7 do
                    // ... and highlight all undiscovered tiles
                    if OverworldData.owMapZone.[j].[i] = hinted_zone.AsDataChar() then
                        let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                        let isLetteredNumberlessDungeon = (alsoHighlightABCDEFGH && cur>=0 && cur<=7 && TrackerModel.GetDungeon(cur).LabelChar='?')
                        if cur = -1 || isLetteredNumberlessDungeon then
                            if TrackerModel.mapStateSummary.OwGettableLocations.Contains(i,j) then
                                if owInstance.SometimesEmpty(i,j) then
                                    owLocatorTileRectangles.[i,j].MakeYellow()
                                else
                                    owLocatorTileRectangles.[i,j].MakeGreen()
                            else
                                if isLetteredNumberlessDungeon then  // OwGettableLocations does not contain already-marked spots
                                    owLocatorTileRectangles.[i,j].MakeGreen()
                                else
                                    owLocatorTileRectangles.[i,j].MakeRed()
        )
    showLocatorInstanceFunc <- (fun f ->
        OverworldRouteDrawing.routeDrawingLayer.Clear()
        for i = 0 to 15 do
            for j = 0 to 7 do
                if f(i,j) && TrackerModel.overworldMapMarks.[i,j].Current() = -1 then
                    if owInstance.SometimesEmpty(i,j) then
                        owLocatorTileRectangles.[i,j].MakeYellowWithBriefAnimation()
                    else
                        owLocatorTileRectangles.[i,j].MakeGreenWithBriefAnimation()
        )
    showHintShopLocator <- (fun () ->
        OverworldRouteDrawing.routeDrawingLayer.Clear()
        let mutable anyFound = false
        for i = 0 to 15 do
            for j = 0 to 7 do
                let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                if cur = TrackerModel.MapSquareChoiceDomainHelper.HINT_SHOP then
                    owLocatorTileRectangles.[i,j].MakeGreenWithBriefAnimation()
                    OverworldMapTileCustomization.temporarilyDisplayHiddenOverworldTileMarks.[i,j] <- true
                    owUpdateFunctions.[i,j] 0 null  // redraw tile, with icon shown
                    anyFound <- true
        if not(anyFound) then
            showLocatorNoneFound()
        )
    showShopLocatorInstanceFunc <- (fun item ->
        OverworldRouteDrawing.routeDrawingLayer.Clear()
        let mutable anyFound = false
        for i = 0 to 15 do
            for j = 0 to 7 do
                let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                if MapStateProxy(cur).IsThreeItemShop && 
                        (cur = item || (TrackerModel.getOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP) = TrackerModel.MapSquareChoiceDomainHelper.ToItem(item))) then
                    owLocatorTileRectangles.[i,j].MakeGreenWithBriefAnimation()
                    OverworldMapTileCustomization.temporarilyDisplayHiddenOverworldTileMarks.[i,j] <- true
                    owUpdateFunctions.[i,j] 0 null  // redraw tile, with icon shown
                    anyFound <- true
        if not(anyFound) then
            showLocatorNoneFound()
        )
    showLocatorPotionAndTakeAny <- (fun () ->
        OverworldRouteDrawing.routeDrawingLayer.Clear()
        let mutable anyFound = false
        for i = 0 to 15 do
            for j = 0 to 7 do
                let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                if cur = TrackerModel.MapSquareChoiceDomainHelper.POTION_SHOP || 
                    (cur = TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY && TrackerModel.getOverworldMapExtraData(i,j,cur)<>cur) then
                    owLocatorTileRectangles.[i,j].MakeGreenWithBriefAnimation()
                    OverworldMapTileCustomization.temporarilyDisplayHiddenOverworldTileMarks.[i,j] <- true  // probably unnecessary, as these can't be hidden
                    owUpdateFunctions.[i,j] 0 null  // redraw tile, with icon shown
                    anyFound <- true
        if not(anyFound) then
            showLocatorNoneFound()
        )
    showLocatorRupees <- (fun () ->
        OverworldRouteDrawing.routeDrawingLayer.Clear()
        let mutable anyFound = false
        for i = 0 to 15 do
            for j = 0 to 7 do
                let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                if cur = TrackerModel.MapSquareChoiceDomainHelper.MONEY_MAKING_GAME || cur = TrackerModel.MapSquareChoiceDomainHelper.UNKNOWN_SECRET ||
                            (cur = TrackerModel.MapSquareChoiceDomainHelper.LARGE_SECRET && TrackerModel.getOverworldMapExtraData(i,j,cur)<>0) ||
                            (cur = TrackerModel.MapSquareChoiceDomainHelper.MEDIUM_SECRET && TrackerModel.getOverworldMapExtraData(i,j,cur)<>0) ||
                            (cur = TrackerModel.MapSquareChoiceDomainHelper.SMALL_SECRET && TrackerModel.getOverworldMapExtraData(i,j,cur)<>0) then
                    owLocatorTileRectangles.[i,j].MakeGreenWithBriefAnimation()
                    OverworldMapTileCustomization.temporarilyDisplayHiddenOverworldTileMarks.[i,j] <- true
                    owUpdateFunctions.[i,j] 0 null  // redraw tile, with icon shown
                    anyFound <- true
        if not(anyFound) then
            showLocatorNoneFound()
        )
    recorderDestinationButtonCanvas.MouseEnter.Add(fun _ ->
        OverworldRouteDrawing.routeDrawingLayer.Clear()
        for i = 0 to 15 do
            for j = 0 to 7 do
                // Note: in HDN, you might have found dungeon G, but if you have starting triforce 4, and dunno if 4=G, we don't know if can recorder there
                if TrackerModel.playerComputedStateSummary.HaveRecorder && OverworldRouting.recorderDests |> Seq.contains (i,j) then
                    owLocatorTileRectangles.[i,j].MakeGreenWithBriefAnimation()
        )
    recorderDestinationButtonCanvas.MouseLeave.Add(fun _ -> hideLocator())
    anyRoadLegendIcon.MouseEnter.Add(fun _ ->
        OverworldRouteDrawing.routeDrawingLayer.Clear()
        for i = 0 to 15 do
            for j = 0 to 7 do
                let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                if cur >= TrackerModel.MapSquareChoiceDomainHelper.WARP_1 && cur <= TrackerModel.MapSquareChoiceDomainHelper.WARP_4 then
                    owLocatorTileRectangles.[i,j].MakeGreenWithBriefAnimation()
        )
    anyRoadLegendIcon.MouseLeave.Add(fun _ -> hideLocator())
    dungeonLegendIconArea.MouseEnter.Add(fun _ -> contentCanvasMouseEnterFunc(10))   // the 'show all dungeons' functionality
    dungeonLegendIconArea.MouseLeave.Add(fun _ -> hideLocator())
    showLocator <- (fun sld ->
        match sld with
        | ShowLocatorDescriptor.DungeonNumber(n) ->
            let mutable index = -1
            for i = 0 to 7 do
                if TrackerModel.GetDungeon(i).LabelChar = char(int '1' + n) then
                    index <- i
            let showHint() =
                let hinted_zone = TrackerModel.GetLevelHint(n)
                if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                    showLocatorHintedZone(hinted_zone,true)
            if index <> -1 then
                let loc = TrackerModel.mapStateSummary.DungeonLocations.[index]
                if loc <> TrackerModel.NOTFOUND then
                    showLocatorExactLocation(loc)
                else
                    showHint()
            else
                showHint()
        | ShowLocatorDescriptor.DungeonIndex(i) ->
            let loc = TrackerModel.mapStateSummary.DungeonLocations.[i]
            if loc <> TrackerModel.NOTFOUND then
                showLocatorExactLocation(loc)
            else
                if TrackerModel.IsHiddenDungeonNumbers() then
                    let label = TrackerModel.GetDungeon(i).LabelChar
                    if label >= '1' && label <= '8' then
                        let index = int label - int '1'
                        let hinted_zone = TrackerModel.GetLevelHint(index)
                        if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                            showLocatorHintedZone(hinted_zone,true)
                else
                    let hinted_zone = TrackerModel.GetLevelHint(i)
                    if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                        showLocatorHintedZone(hinted_zone,false)
        | ShowLocatorDescriptor.Sword1 ->
            let loc = TrackerModel.mapStateSummary.Sword1Location
            if loc <> TrackerModel.NOTFOUND then
                showLocatorExactLocation(loc)
        | ShowLocatorDescriptor.Sword2 ->
            let loc = TrackerModel.mapStateSummary.Sword2Location
            if loc <> TrackerModel.NOTFOUND then
                showLocatorExactLocation(loc)
            else
                let hinted_zone = TrackerModel.GetLevelHint(9)
                if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                    showLocatorHintedZone(hinted_zone,false)
        | ShowLocatorDescriptor.Sword3 ->
            let loc = TrackerModel.mapStateSummary.Sword3Location
            if loc <> TrackerModel.NOTFOUND then
                showLocatorExactLocation(loc)
            else
                let hinted_zone = TrackerModel.GetLevelHint(10)
                if hinted_zone <> TrackerModel.HintZone.UNKNOWN then
                    showLocatorHintedZone(hinted_zone,false)
        )
    let currentTargetGhostBuster = makeGhostBusterImpl(Brushes.Red, true)
    currentTargetGhostBuster.Opacity <- 0.
    layout.AddLinkTarget(currentTargetGhostBuster)
    showLocatorNoneFound <- (fun () ->
        currentTargetGhostBuster.Opacity <- 1.
        // ensureRespectingOwGettableScreensAndOpenCavesCheckBoxes()  // do not do this, we want the map to be blank, to make the ghostbuster 'pop' more and the non-found-ness to be more apparent
        )
    hideLocator <- (fun () ->
        if not zone_checkbox.IsChecked.HasValue || not zone_checkbox.IsChecked.Value then changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,false)
        allOwMapZoneBlackCanvases |> Array2D.iteri (fun _x _y zbc -> zbc.Opacity <- 0.0)
        for i = 0 to 15 do
            for j = 0 to 7 do
                owLocatorTileRectangles.[i,j].Hide()
                if OverworldMapTileCustomization.temporarilyDisplayHiddenOverworldTileMarks.[i,j] then
                    OverworldMapTileCustomization.temporarilyDisplayHiddenOverworldTileMarks.[i,j] <- false
                    owUpdateFunctions.[i,j] 0 null  // redraw tile, with icon possibly hidden
        owLocatorCanvas.Children.Clear()
        currentTargetGhostBuster.Opacity <- 0.
        ensureRespectingOwGettableScreensAndOpenCavesCheckBoxes()
        )

    addZoneName(TrackerModel.HintZone.DEATH_MOUNTAIN, "DEATH\nMOUNTAIN", 2.5, 0.3)
    addZoneName(TrackerModel.HintZone.GRAVE,          "GRAVE", 1.5, 2.8)
    addZoneName(TrackerModel.HintZone.DEAD_WOODS,     "DEAD\nWOODS", 1.4, 5.3)
    addZoneName(TrackerModel.HintZone.LAKE,           "LAKE 1", 10.2, 0.1)
    addZoneName(TrackerModel.HintZone.LAKE,           "LAKE 2", 5.5, 3.5)
    addZoneName(TrackerModel.HintZone.LAKE,           "LAKE 3", 9.4, 5.5)
    addZoneName(TrackerModel.HintZone.RIVER,          "RIVER 1", 7.3, 1.1)
    addZoneName(TrackerModel.HintZone.RIVER,          "RIV\nER2", 5.1, 6.2)
    addZoneName(TrackerModel.HintZone.NEAR_START,     "START", 7.3, 6.2)
    addZoneName(TrackerModel.HintZone.DESERT,         "DESERT", 10.3, 3.1)
    addZoneName(TrackerModel.HintZone.FOREST,         "FOREST", 12.3, 5.1)
    addZoneName(TrackerModel.HintZone.LOST_HILLS,     "LOST\nHILLS", 12.4, 0.3)
    addZoneName(TrackerModel.HintZone.COAST,          "COAST", 14.3, 2.7)

    // timeline, options menu, reminders
    let moreOptionsButton = Graphics.makeButton("Options...", Some(12.), Some(Brushes.Orange))
    moreOptionsButton.MaxHeight <- 25.
    moreOptionsButton.Measure(new Size(System.Double.PositiveInfinity, 25.))

    let optionsCanvas = OptionsMenu.makeOptionsCanvas(cm, true, isStandardHyrule)
    optionsCanvas.Opacity <- 1.
    optionsCanvas.IsHitTestVisible <- true

    let theTimeline1 = new Timeline.Timeline(21., 4, appMainCanvas.Width-48., 1, [|"0:00";"0:10";"0:20";"0:30";"0:40";"0:50";"1:00"|], moreOptionsButton.DesiredSize.Width-24.)
    let theTimeline2 = new Timeline.Timeline(21., 4, appMainCanvas.Width-48., 2, [|"0:00";"0:20";"0:40";"1:00";"1:20";"1:40";"2:00"|], moreOptionsButton.DesiredSize.Width-24.)
    let theTimeline3 = new Timeline.Timeline(21., 4, appMainCanvas.Width-48., 3, [|"0:00";"0:30";"1:00";"1:30";"2:00";"2:30";"3:00"|], moreOptionsButton.DesiredSize.Width-24.)
    theTimeline1.Canvas.Opacity <- 1.
    theTimeline2.Canvas.Opacity <- 0.
    theTimeline3.Canvas.Opacity <- 0.
    let drawTimeline(minute) =
        if minute <= 60 then
            theTimeline1.Canvas.Opacity <- 1.
            theTimeline2.Canvas.Opacity <- 0.
            theTimeline3.Canvas.Opacity <- 0.
            theTimeline1.Update(minute, timelineItems, maxOverworldRemain)
        elif minute <= 120 then
            theTimeline1.Canvas.Opacity <- 0.
            theTimeline2.Canvas.Opacity <- 1.
            theTimeline3.Canvas.Opacity <- 0.
            theTimeline2.Update(minute, timelineItems, maxOverworldRemain)
        else
            theTimeline1.Canvas.Opacity <- 0.
            theTimeline2.Canvas.Opacity <- 0.
            theTimeline3.Canvas.Opacity <- 1.
            theTimeline3.Update(minute, timelineItems, maxOverworldRemain)
    TrackerModel.TimelineItemModel.TimelineChanged.Add(drawTimeline)
    for j = 0 to 7 do
        TrackerModel.GetDungeon(j).HiddenDungeonColorOrLabelChanged.Add(fun _ -> TrackerModel.TimelineItemModel.TriggerTimelineChanged()) // e.g. to change heart label from E -> 3
    OptionsMenu.secondQuestDungeonsOptionChanged.Publish.Add(fun _ -> TrackerModel.TimelineItemModel.TriggerTimelineChanged())  // e.g. to change heart label from 1 -> 4

    moreOptionsButton.Click.Add(fun _ -> 
        if not popupIsActive then
            popupIsActive <- true
            let wh = new System.Threading.ManualResetEvent(false)
            async {
                do! CustomComboBoxes.DoModalDocked(cm, wh, Dock.Bottom, optionsCanvas)
                TrackerModelOptions.writeSettings()
                popupIsActive <- false
                } |> Async.StartImmediate
        )
    let drawButton = Graphics.makeButton("D\nr\na\nw", Some(12.), Some(Brushes.Orange))
    layout.AddTimelineAndButtons(theTimeline1.Canvas, theTimeline2.Canvas, theTimeline3.Canvas, moreOptionsButton, drawButton)
    drawButton.Click.Add(fun _ -> 
        if not popupIsActive then
            popupIsActive <- true
            async {
                do! DrawingLayer.InteractWithDrawingLayer(cm, drawingCanvas)
                popupIsActive <- false
                } |> Async.StartImmediate
        )

    let reminderDisplayOuterDockPanel = SetupReminderDisplayAndProcessing(cm)
    layout.AddReminderDisplayOverlay(reminderDisplayOuterDockPanel)

    // postgameDecorationCanvas atop timeline & reminders
    do
        let postgameDecorationCanvas = new Canvas(Width=appMainCanvas.Width, Opacity=0., IsHitTestVisible=false)   // no hit test so it doesn't absorb clicks to reminder log
        layout.AddPostGameDecorationCanvas(postgameDecorationCanvas)
        let sp = new StackPanel(Orientation=Orientation.Horizontal, Background=Brushes.Black, Height=24.)  // height to cover the "log" button
        Canvas.SetRight(sp, 0.)
        Canvas.SetTop(sp, 0.)
        postgameDecorationCanvas.Children.Add(sp) |> ignore
        let makeViewRectImpl(c:FrameworkElement) = new Shapes.Rectangle(Width=c.Width, Height=c.Height, Fill=new VisualBrush(c))
        let timerClone = makeViewRectImpl(hmsTimeTextBox)
        timerClone.LayoutTransform <- new ScaleTransform(20./timerClone.Height, 20./timerClone.Height)
        sp.Children.Add(timerClone) |> ignore
        sp.Children.Add(makeViewRectImpl(owRemainingScreensTextBox)) |> ignore
        let screenshotTimelineFilename = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "most-recent-completion-timeline.png")
        let screenshotFullFilename = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "most-recent-completion-full-screenshot.png")
        TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Changed.Add(fun b -> 
            if b then
                postgameDecorationCanvas.Opacity <- 1.
                if not(Timeline.isCurrentlyLoadingASave) then
                    async {
                        // select the dungeon summary tab, so everything on-screen at once
                        selectDungeonTabEvent.Trigger(9)  
                        if TrackerModelOptions.ShorterAppWindow.Value then
                            Graphics.NavigationallyWarpMouseCursorTo(dungeonTabs.TranslatePoint(posToWarpToWhenTabbingFromOverworld, appMainCanvas))
                            layout.FocusDungeon()
                        // wait for finish drawing (e.g. zelda is not yet drawn on the timeline)
                        do! Async.Sleep(10)  
                        do! Async.SwitchToContext(ctxt)
                        try
                            for filename, viewboxF in [screenshotFullFilename, (fun () -> layout.GetFullAppBounds())
                                                       screenshotTimelineFilename, (fun () -> layout.GetTimelineBounds())
                                                       ] do
                                // screenshot timeline region
                                let vb = new VisualBrush(cm.RootCanvas.Parent :?> Canvas)    // whole canvas (parent of root), so that we can capture the timer display in the full screenshot
                                vb.ViewboxUnits <- BrushMappingMode.Absolute
                                let orig = viewboxF()
                                let mutable rect = viewboxF()
                                vb.Stretch <- Stretch.Uniform
                                let factor = 
                                    if TrackerModelOptions.SmallerAppWindow.Value then 
                                        let r = TrackerModelOptions.SmallerAppWindowScaleFactor 
                                        rect.Location <- Point(rect.Left * r, rect.Top * r)
                                        rect.Size <- Size(rect.Width * r, rect.Height * r)
                                        r
                                    else 1.0
                                vb.Viewbox <- rect
                                let visual = new DrawingVisual(Transform=new ScaleTransform(1. / factor, 1. / factor))
                                do 
                                    use dc = visual.RenderOpen()
                                    dc.DrawRectangle(vb, null, Rect(Size(vb.Viewbox.Width, vb.Viewbox.Height)))
                                let bitmap = new Imaging.RenderTargetBitmap(int(orig.Width), int(orig.Height), 96., 96., PixelFormats.Default)
                                bitmap.Render(visual)
                                let encoder = new Imaging.PngBitmapEncoder()
                                encoder.Frames.Add(Imaging.BitmapFrame.Create(bitmap))
                                do
                                    use stream = System.IO.File.Create(filename)
                                    encoder.Save(stream)
                                System.IO.File.SetCreationTime(filename, System.DateTime.Now)
                                System.IO.File.SetLastWriteTime(filename, System.DateTime.Now)
                        with e ->
                            printfn "%s" (e.ToString())
                    } |> Async.StartImmediate
            else
                postgameDecorationCanvas.Opacity <- 0.
            )

    let refocusKeyboard() = 
        let w = Window.GetWindow(appMainCanvas)
        Input.Keyboard.ClearFocus()                // ensure that clicks outside the Notes area de-focus it, by clearing keyboard focus...
        Input.FocusManager.SetFocusedElement(w, null)  // ... and by removing its logical mouse focus
        Input.Keyboard.Focus(w) |> ignore  // refocus keyboard to main window so MyKey still works
    appMainCanvas.MouseDown.Add(fun _ -> refocusKeyboard())

    do! showProgress("broadcast/load/animation")

    // broadcast window
    Broadcast.MakeBroadcastWindow(cm, drawingCanvas, blockerGrid, dungeonTabsOverlayContent, refocusMainWindow)

#if NOT_RACE_LEGAL
    HUDs.MakeHUDs(cm, trackerDungeonMoused, trackerLocationMoused)
#endif

    layout.AddSpotSummary(spotSummaryCanvas)

    // poke loaded data values
    match loadData with
    | Some(data) ->
        // Overworld
        TrackerModelOptions.Overworld.MirrorOverworld.Value <- data.Overworld.MirrorOverworld
        TrackerModel.startIconX <- data.Overworld.StartIconX
        TrackerModel.startIconY <- data.Overworld.StartIconY
        TrackerModel.customWaypointX <- data.Overworld.CustomWaypointX
        TrackerModel.customWaypointY <- data.Overworld.CustomWaypointY
        let a = data.Overworld.Map
        if a.Length <> 16 * 8 * 3 then
            failwith "bad load data at data.Overworld.Map"
        Timeline.isCurrentlyLoadingASave <- true
        TrackerModel.currentlyIgnoringForceUpdatesDuringALoad <- true
        let mutable anySetProblems = false
        for j = 0 to 7 do
            for i = 0 to 15 do
                let cur    = a.[j*16*3 + i*3]
                let ed     = a.[j*16*3 + i*3 + 1]
                let circle = a.[j*16*3 + i*3 + 2]
                if not(TrackerModel.overworldMapMarks.[i,j].AttemptToSet(cur)) then
                    anySetProblems <- true
                let k = if TrackerModel.MapSquareChoiceDomainHelper.IsItem(cur) then TrackerModel.MapSquareChoiceDomainHelper.SHOP else cur
                if k <> -1 then
                    TrackerModel.setOverworldMapExtraData(i,j,k,ed)
                TrackerModel.overworldMapCircles.[i,j] <- circle
                owUpdateFunctions.[i,j] 0 null  // redraw the tile
                owCircleRedraws.[i,j]()
        do! showProgress(sprintf "finished loading overworld")
        // Items
        if not(data.Items.WhiteSwordBox.TryApply(TrackerModel.sword2Box)) then anySetProblems <- true
        if not(data.Items.LadderBox.TryApply(TrackerModel.ladderBox)) then anySetProblems <- true
        if not(data.Items.ArmosBox.TryApply(TrackerModel.armosBox)) then anySetProblems <- true
        for i = 0 to 8 do
            let ds = data.Items.Dungeons.[i]
            let dd = TrackerModel.GetDungeon(i)
            if not(ds.TryApply(dd)) then anySetProblems <- true
        do! showProgress(sprintf "finished loading items")
        // PlayerProgressAndTakeAnyHearts
        data.PlayerProgressAndTakeAnyHearts.Apply()
        // StartingItemsAndExtras
        data.StartingItemsAndExtras.Apply()
        // Blockers
        TrackerModel.DungeonBlockersContainer.StartIgnoreChangesDuringLoad()
        for i = 0 to 7 do
            for j = 0 to TrackerModel.DungeonBlockersContainer.MAX_BLOCKERS_PER_DUNGEON-1 do
                TrackerModel.DungeonBlockersContainer.SetDungeonBlocker(i,j,TrackerModel.DungeonBlocker.FromHotKeyName(data.Blockers.[i].[j].Kind))
                for k = 0 to TrackerModel.DungeonBlockerAppliesTo.MAX-1 do
                    TrackerModel.DungeonBlockersContainer.SetDungeonBlockerAppliesTo(i,j,k,data.Blockers.[i].[j].AppliesTo.[k])
            do! showProgress(sprintf "finished loading blockers %d of 8" (i+1))
        TrackerModel.DungeonBlockersContainer.FinishIgnoreChangesDuringLoad()
        if anySetProblems then
            () // TODO
        // Hints
        for i = 0 to 10 do
            TrackerModel.SetLevelHint(i, TrackerModel.HintZone.FromIndex(data.Hints.LocationHints.[i]))
        TrackerModel.NoFeatOfStrengthHintWasGiven <- data.Hints.NoFeatOfStrengthHint
        TrackerModel.SailNotHintWasGiven <- data.Hints.SailNotHint
        hideFeatsOfStrength TrackerModel.NoFeatOfStrengthHintWasGiven 
        hideRaftSpots TrackerModel.SailNotHintWasGiven
        // Notes
        notesTextBox.Text <- data.Notes
        // CRDI
        currentRecorderDestinationIndex <- data.CurrentRecorderDestinationIndex
        TrackerModel.recorderToNewDungeons <- data.RecorderToNewDungeons
        TrackerModel.recorderToUnbeatenDungeons <- data.RecorderToUnbeatenDungeons
        if data.IsBoomstickSeed then
            toggleBookShieldCheckBox.IsChecked <- System.Nullable.op_Implicit true
        if data.IsAtlasSeed then
            bookIsAtlasCheckBox.IsChecked <- System.Nullable.op_Implicit true
        updateCurrentRecorderDestinationNumeral()
        // Dungeon Maps
        do! importDungeonModels(showProgress, data.DungeonMaps)
        DungeonUI.theDungeonTabControl.SelectedIndex <- 0   // if they selected the summary tab, it only updates after an actual change, so poke tab 0 first, so the change occurs
        DungeonUI.theDungeonTabControl.SelectedIndex <- data.DungeonTabSelected
        // UserCustom
        if data.UserCustomChecklist <> null && data.UserCustomChecklist.Items <> null then
            SaveAndLoad.theUserCustomChecklist <- data.UserCustomChecklist
            // note that line below must happen before we populate Timeline further below, as it updates TrackerModel.TimelineItemModel.All to include UserCustom stuff
            do! UserCustomLayer.InitializeUserCustom(cm, timelineItems, invokeExtras)
        // Drawing Layer
        DrawingLayer.LoadDrawingLayer(data.DrawingLayerIcons, drawingCanvas)
        // Graphics.alternativeOverworldMapFilename & Graphics.shouldInitiallyHideOverworldMap were loaded much earlier
        // Seed & Flags
        if data.Seed <> null && data.Seed <> "" then
            SaveAndLoad.lastKnownSeed <- data.Seed
            SaveAndLoad.seedAndFlagsUpdated.Trigger()
        if data.Flags <> null && data.Flags <> "" then
            SaveAndLoad.lastKnownFlags <- data.Flags
            SaveAndLoad.seedAndFlagsUpdated.Trigger()
        // Timeline
        if data.OverworldSpotsRemainingOverTime <> null then
            for i = 0 to (data.OverworldSpotsRemainingOverTime.Length/2)-1 do
                TrackerModel.timelineDataOverworldSpotsRemain.Add(data.OverworldSpotsRemainingOverTime.[i*2], data.OverworldSpotsRemainingOverTime.[i*2+1])
        if data.Timeline <> null then
            for td in data.Timeline do
                if td.Seconds <= data.TimeInSeconds then
                    match TrackerModel.TimelineItemModel.All.TryGetValue(td.Ident) with
                    | true, v -> v.StampTotalSeconds(td.Seconds, TrackerModel.PlayerHas.FromInt(td.Has))
                    | _ -> ()
        drawTimeline(data.TimeInSeconds / 60)
        // Timer
        TrackerModel.theStartTime.SetAgo(TimeSpan.FromSeconds(float data.TimeInSeconds))
        // recompute everything, update UI
        TrackerModel.recomputePlayerStateSummary()
        TrackerModel.recomputeMapStateSummary()
        TrackerModel.recomputeWhatIsNeeded() |> ignore
        // done
        TrackerModel.currentlyIgnoringForceUpdatesDuringALoad <- false
        TrackerModel.forceUpdate()
        doUIUpdateEvent.Trigger()
        Timeline.isCurrentlyLoadingASave <- false
        TrackerModel.TimelineItemModel.TriggerTimelineChanged()  // redraw timeline once (redraws are ignored while loading)
    | _ ->
        ()

    // animation
    do
        let c(t) = Color.FromArgb(t,0uy,255uy,255uy)  // Color.FromArgb(t,255uy,165uy,0uy)
        let rgb = new RadialGradientBrush(GradientOrigin=Point(0.5,0.5), Center=Point(0.5,0.5), RadiusX=0.5, RadiusY=0.5)
        rgb.GradientStops.Add(new GradientStop(c(0uy), 0.0))
        rgb.GradientStops.Add(new GradientStop(c(0uy), 0.4))
        rgb.GradientStops.Add(new GradientStop(c(0uy), 1.0))

        let msDuration = 1500
        let ca = new Animation.ColorAnimation(From=Nullable<_>(c(0uy)), To=Nullable<_>(c(140uy)), Duration=new Duration(TimeSpan.FromMilliseconds(float msDuration)), AutoReverse=true)
        let owHighlightTile = new Shapes.Rectangle(Width=OMTW, Height=11. * 3., StrokeThickness = 0., Fill=rgb, Opacity=1.0, IsHitTestVisible=false)
        canvasAdd(overworldCanvas, owHighlightTile, OMTW*float(6), float(11*3*6))

        let animateOWTile(x,y) = 
            if (x,y) <> TrackerModel.NOTFOUND then
                OverworldMapTileCustomization.temporarilyDisplayHiddenOverworldTileMarks.[x,y] <- true
                owUpdateFunctions.[x,y] 0 null  // redraw tile, with icon shown
                if TrackerModelOptions.AnimateTileChanges.Value then
                    Canvas.SetLeft(owHighlightTile, OMTW*float(x))
                    Canvas.SetTop(owHighlightTile, float(11*3*y))
                    rgb.GradientStops.[2].BeginAnimation(GradientStop.ColorProperty, ca)
                async {
                    do! Async.Sleep(msDuration)
                    do! Async.SwitchToContext(ctxt)
                    OverworldMapTileCustomization.temporarilyDisplayHiddenOverworldTileMarks.[x,y] <- false
                    owUpdateFunctions.[x,y] 0 null  // redraw tile, with icon possibly hidden
                } |> Async.StartImmediate
        animateOverworldTile <- animateOWTile

    TrackerModel.forceUpdate()
    timer.Start()  // don't start the tick timer updating, until the entire app is loaded
    do  // auto-save every 1 min
        let diskIcon = new Border(BorderThickness=Thickness(3.), BorderBrush=Brushes.Gray, Background=Brushes.Gray, Child=Graphics.BMPtoImage(Graphics.iconDisk_bmp), Opacity=0.0)
        layout.AddDiskIcon(diskIcon)
        let timer = new System.Windows.Threading.DispatcherTimer()
        timer.Interval <- TimeSpan.FromSeconds(60.0)
        timer.Tick.Add(fun _ -> 
            try
                SaveAndLoad.SaveAll(notesTextBox.Text, DungeonUI.theDungeonTabControl.SelectedIndex, exportDungeonModelsJsonLines(), DungeonSaveAndLoad.SaveDrawingLayer(), 
                                        Graphics.alternativeOverworldMapFilename, Graphics.shouldInitiallyHideOverworldMap, currentRecorderDestinationIndex, 
                                        toggleBookShieldCheckBox.IsChecked.Value, bookIsAtlasCheckBox.IsChecked.Value, SaveAndLoad.AutoSave) |> ignore
                async {
                    diskIcon.Opacity <- 0.7
                    do! Async.Sleep(300)
                    do! Async.SwitchToContext ctxt
                    diskIcon.Opacity <- 0.4
                    do! Async.Sleep(300)
                    do! Async.SwitchToContext ctxt
                    diskIcon.Opacity <- 0.7
                    do! Async.Sleep(300)
                    do! Async.SwitchToContext ctxt
                    diskIcon.Opacity <- 0.0
                } |> Async.StartImmediate
            with e ->
                ()
        )
        timer.Start()

    appMainCanvas.MyKeyAdd(fun ea ->
        match HotKeys.GlobalHotKeyProcessor.TryGetValue(ea.Key) with
        | Some(hotKeyedState) -> 
            ea.Handled <- true
            match hotKeyedState with
            | HotKeys.GlobalHotkeyTargets.ToggleMagicalSword -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Toggle()
            | HotKeys.GlobalHotkeyTargets.ToggleWoodSword    -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword.Toggle()
            | HotKeys.GlobalHotkeyTargets.ToggleBoomBook     -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Toggle()
            | HotKeys.GlobalHotkeyTargets.ToggleBlueCandle   -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle.Toggle()
            | HotKeys.GlobalHotkeyTargets.ToggleWoodArrow    -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodArrow.Toggle()
            | HotKeys.GlobalHotkeyTargets.ToggleBlueRing     -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueRing.Toggle()
            | HotKeys.GlobalHotkeyTargets.ToggleBombs        -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBombs.Toggle()
            | HotKeys.GlobalHotkeyTargets.ToggleGannon       -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasDefeatedGanon.Toggle()
            | HotKeys.GlobalHotkeyTargets.ToggleZelda        -> TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Toggle()
            | HotKeys.GlobalHotkeyTargets.DungeonTab1        -> selectDungeonTabEvent.Trigger(0)
            | HotKeys.GlobalHotkeyTargets.DungeonTab2        -> selectDungeonTabEvent.Trigger(1)
            | HotKeys.GlobalHotkeyTargets.DungeonTab3        -> selectDungeonTabEvent.Trigger(2)
            | HotKeys.GlobalHotkeyTargets.DungeonTab4        -> selectDungeonTabEvent.Trigger(3)
            | HotKeys.GlobalHotkeyTargets.DungeonTab5        -> selectDungeonTabEvent.Trigger(4)
            | HotKeys.GlobalHotkeyTargets.DungeonTab6        -> selectDungeonTabEvent.Trigger(5)
            | HotKeys.GlobalHotkeyTargets.DungeonTab7        -> selectDungeonTabEvent.Trigger(6)
            | HotKeys.GlobalHotkeyTargets.DungeonTab8        -> selectDungeonTabEvent.Trigger(7)
            | HotKeys.GlobalHotkeyTargets.DungeonTab9        -> selectDungeonTabEvent.Trigger(8)
            | HotKeys.GlobalHotkeyTargets.DungeonTabS        -> selectDungeonTabEvent.Trigger(9)
            // some buttons (e.g. Recorder Destination counter) are Focusable, and clicking them captures future keyboard input
            | HotKeys.GlobalHotkeyTargets.LeftClick          -> Graphics.Win32.LeftMouseClick(); refocusKeyboard()
            | HotKeys.GlobalHotkeyTargets.MiddleClick        -> Graphics.Win32.MiddleMouseClick(); refocusKeyboard()       
            | HotKeys.GlobalHotkeyTargets.RightClick         -> Graphics.Win32.RightMouseClick(); refocusKeyboard()       
            | HotKeys.GlobalHotkeyTargets.ScrollUp           -> Graphics.Win32.ScrollWheelRotateUp(); refocusKeyboard()       
            | HotKeys.GlobalHotkeyTargets.ScrollDown         -> Graphics.Win32.ScrollWheelRotateDown(); refocusKeyboard()       
            | HotKeys.GlobalHotkeyTargets.ToggleCursorOverworldOrDungeon ->
                if overworldCanvas.IsMouseOver then
                    Graphics.NavigationallyWarpMouseCursorTo(dungeonTabs.TranslatePoint(posToWarpToWhenTabbingFromOverworld, appMainCanvas))
                    layout.FocusDungeon()
                else
                    Graphics.NavigationallyWarpMouseCursorTo(overworldCanvas.TranslatePoint(Point(OMTW*8.5,11.*3.*4.5), appMainCanvas))
                    layout.FocusOverworld()
            | _ -> () // MoveCursor not handled at this level
        | None -> 
            ()
    )

    Layout.setupMouseMagnifier(cm, refocusMainWindow)

    Views.appMainCanvasGlobalBoxMouseOverHighlight.AttachToGlobalCanvas(appMainCanvas)
    
    Graphics.PlaySoundForSpeechRecognizedAndUsedToMark()  // the very first call to this lags the system for some reason, so get it out of the way at startup
    do! showProgress("all done")
    return drawTimeline
    }

