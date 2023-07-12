module Layout

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media
open OverworldItemGridUI            // has canvasAdd, etc
open OverworldMapTileCustomization  // has a lot of constants

let RIGHT_COL = 410.
let WEBCAM_LINE = OMTW*16.-200.  // height of upper area is 150, so 200 wide is 4x3 box in upper right; timer and other controls here could be obscured
let kittyWidth = THRU_MAIN_MAP_AND_ITEM_PROGRESS_H - THRU_MAIN_MAP_H

type IApplicationLayoutBase =
    abstract member AddMainTracker : mainTracker:UIElement -> unit
    abstract member AddNumberedTriforceCanvas : triforceCanvas:Canvas * i:int -> unit 
    abstract member AddItemGridStuff : owItemGrid:UIElement * toggleBookShieldCheckBox:UIElement * bookIsAtlasCheckBox:UIElement * highlightOpenCavesCheckBox:UIElement * 
                                            timerResetButton:UIElement * spotSummaryTB:UIElement * mirrorOW:UIElement * hideIconsEyeball:UIElement * moreFQSQoptionsButton:Button -> unit
    abstract member AddHideQuestCheckboxes : hideFirstQuestCheckBox:UIElement * hideSecondQuestCheckBox:UIElement -> unit
    abstract member AddLinkRouting : linkIcon:UIElement * currentTargetIcon:UIElement -> unit
    abstract member AddWebcamLine : unit -> unit
    abstract member AddOverworldCanvas : overworldCanvas:UIElement -> unit
    abstract member AddLegend : legendCanvas:UIElement -> unit
    abstract member AddItemProgress : itemProgressCanvas:UIElement * itemProgressTB:UIElement -> unit
    abstract member AddVersionButton : vb:UIElement -> unit
    abstract member AddHintDecoderButton : tb:UIElement -> unit
    abstract member AddKittyAndLogo : kitty:Image * logoBorder:UIElement * ztlogo:Image -> unit
    abstract member AddShowHotKeysButton : showHotKeysButton:UIElement -> unit
    abstract member AddShowRunCustomButton : showRunCustomButton:UIElement -> unit
    abstract member AddSaveButton : saveButton:UIElement -> unit
    abstract member AddUserCustomContentButton : uccButton:UIElement -> unit
    abstract member AddDungeonTabsOverlay : dungeonTabsOverlay:UIElement -> unit
    abstract member AddDungeonTabs : dungeonTabsWholeCanvas:UIElement -> unit
    abstract member GetDungeonY : unit -> float
    abstract member AddBlockers : blockerGrid:UIElement -> unit
    abstract member AddNotesSeedsFlags : notesTextBox:UIElement * seedAndFlagsDisplayCanvas:UIElement -> unit
    abstract member AddExtraDungeonRightwardStuff : grabModeTextBlock:UIElement * rightwardCanvas:UIElement -> unit
    abstract member AddOWRemainingScreens : owRemainingScreensTextBox:UIElement -> unit
    abstract member AddOWGettableScreens : owGettableScreensCheckBox:UIElement -> unit
    abstract member AddCurrentMaxHearts : currentMaxHeartsTextBox:UIElement -> unit
    abstract member AddShowCoords : showCoordsCB:UIElement * havePotionLetterImage:Image -> unit
    abstract member AddOWZoneOverlay : zone_checkbox:UIElement -> unit
    abstract member AddMouseHoverExplainer : mouseHoverExplainerIcon:UIElement * c:UIElement -> unit
    abstract member AddLinkTarget : currentTargetGhostBuster:UIElement -> unit
    abstract member AddTimeline : t1c:UIElement * t2c:UIElement * t3c:UIElement-> unit
    abstract member GetTimelineBounds : unit -> Rect
    abstract member GetFullAppBounds : unit -> Rect
    abstract member AddReminderDisplayOverlay : reminderDisplayOuterDockPanel:UIElement -> unit
    abstract member AddPostGameDecorationCanvas : postgameDecorationCanvas:UIElement -> unit
    abstract member AddSpotSummary : spotSummaryCanvas:UIElement -> unit
    abstract member AddDiskIcon : diskIcon:UIElement -> unit
    abstract member AllDone : unit -> unit
    abstract member AddTopLayerHover : fe:FrameworkElement * x:float * y:float -> unit
    abstract member ClearTopLayerHovers : unit -> unit
    abstract member FocusOverworld : unit -> unit
    abstract member FocusDungeon : unit -> unit
    abstract member IsShort : bool

type ApplicationLayout(cm:CustomComboBoxes.CanvasManager) =
    let appMainCanvas = cm.AppMainCanvas
    let topLayerHoverCanvas = new Canvas()   // for temporary bits that appear on hover, to ensure they appear on top of everything else in the app, when no other popup active
    interface IApplicationLayoutBase with
        member this.AddMainTracker(mainTracker) =
            canvasAdd(appMainCanvas, mainTracker, 0., 0.)
        member this.AddNumberedTriforceCanvas(triforceCanvas, i) =
            canvasAdd(appMainCanvas, triforceCanvas, OW_ITEM_GRID_LOCATIONS.OFFSET+30.*float i, 0.)
        member this.AddItemGridStuff(owItemGrid, toggleBookShieldCheckBox, bookIsAtlasCheckBox, highlightOpenCavesCheckBox, timerResetButton, spotSummaryTB, mirrorOW, hideIconsEyeball, moreFQSQoptionsButton) =
            canvasAdd(appMainCanvas, owItemGrid, OW_ITEM_GRID_LOCATIONS.OFFSET, 30.)
            canvasAdd(appMainCanvas, toggleBookShieldCheckBox, OW_ITEM_GRID_LOCATIONS.OFFSET+180., 35.)
            canvasAdd(appMainCanvas, bookIsAtlasCheckBox, OW_ITEM_GRID_LOCATIONS.OFFSET+180.+55., 35.)
            canvasAdd(appMainCanvas, highlightOpenCavesCheckBox, 510., 120.)
            canvasAdd(appMainCanvas, timerResetButton, 12.8*OMTW, 60.)
            canvasAdd(appMainCanvas, spotSummaryTB, 12.8*OMTW, 90.)
            canvasAdd(appMainCanvas, hideIconsEyeball, WEBCAM_LINE+5., 0.)
            canvasAdd(appMainCanvas, mirrorOW, WEBCAM_LINE+5., 70.)
            canvasAdd(appMainCanvas, moreFQSQoptionsButton, WEBCAM_LINE+70., 120.)
        member this.AddHideQuestCheckboxes(hideFirstQuestCheckBox, hideSecondQuestCheckBox) = 
            canvasAdd(appMainCanvas, hideFirstQuestCheckBox,  WEBCAM_LINE + 5., 110.) 
            canvasAdd(appMainCanvas, hideSecondQuestCheckBox, WEBCAM_LINE + 5., 130.)
        member this.AddLinkRouting(linkIcon, currentTargetIcon) =
            canvasAdd(appMainCanvas, linkIcon, 16.*OMTW-60., 120.)
            canvasAdd(appMainCanvas, currentTargetIcon, 16.*OMTW-30., 120.)
        member this.AddWebcamLine() =
            let webcamLine = new Canvas(Background=Brushes.Orange, Width=2., Height=150., Opacity=0.4)
            canvasAdd(appMainCanvas, webcamLine, WEBCAM_LINE, 0.)
        member this.AddOverworldCanvas(overworldCanvas) =
            canvasAdd(appMainCanvas, overworldCanvas, 0., 150.)
        member this.AddLegend(legendCanvas) = 
            canvasAdd(appMainCanvas, legendCanvas, 0., THRU_MAIN_MAP_H)
        member this.AddItemProgress(itemProgressCanvas, itemProgressTB) = 
            canvasAdd(appMainCanvas, itemProgressCanvas, 0., THRU_MAP_AND_LEGEND_H)
            canvasAdd(appMainCanvas, itemProgressTB, 50., THRU_MAP_AND_LEGEND_H + 4.)
        member this.AddVersionButton(vb) = 
            canvasAdd(appMainCanvas, vb, 0., THRU_MAP_AND_LEGEND_H + 4.)
        member this.AddHintDecoderButton(tb) =
            canvasAdd(appMainCanvas, tb, 510., THRU_MAP_AND_LEGEND_H + 6.)
        member this.AddKittyAndLogo(kitty:Image, logoBorder, ztlogo:Image) =
            canvasAdd(appMainCanvas, kitty, 16.*OMTW - kitty.Width - 12., THRU_MAIN_MAP_H)
            canvasAdd(appMainCanvas, logoBorder, 16.*OMTW - ztlogo.Width - 2., THRU_MAIN_MAP_H + kitty.Height - ztlogo.Height - 6.)
        member this.AddShowHotKeysButton(showHotKeysButton) = 
            canvasAdd(appMainCanvas, showHotKeysButton, 16.*OMTW - kittyWidth - 115., THRU_MAIN_MAP_H)
        member this.AddShowRunCustomButton(showRunCustomButton) = 
            canvasAdd(appMainCanvas, showRunCustomButton, 16.*OMTW - kittyWidth - 115., THRU_MAIN_MAP_H + 22.)
        member this.AddSaveButton(saveButton) = 
            canvasAdd(appMainCanvas, saveButton, 16.*OMTW - kittyWidth - 50., THRU_MAIN_MAP_H + 22.)
        member this.AddUserCustomContentButton(uccButton) = 
            canvasAdd(appMainCanvas, uccButton, 16.*OMTW - kittyWidth - 50., THRU_MAIN_MAP_H + 42.)
        member this.AddDungeonTabsOverlay(dungeonTabsOverlay) =  // for magnifier
            canvasAdd(appMainCanvas, dungeonTabsOverlay, 0., START_DUNGEON_AND_NOTES_AREA_H+float(TH))
        member this.AddDungeonTabs(dungeonTabsWholeCanvas) =
            canvasAdd(appMainCanvas, dungeonTabsWholeCanvas, 0., START_DUNGEON_AND_NOTES_AREA_H)
        member this.GetDungeonY() =
            START_DUNGEON_AND_NOTES_AREA_H
        member this.AddBlockers(blockerGrid) =
            canvasAdd(appMainCanvas, blockerGrid, BLOCKERS_AND_NOTES_OFFSET, START_DUNGEON_AND_NOTES_AREA_H) 
        member this.AddNotesSeedsFlags(notesTextBox, seedAndFlagsDisplayCanvas) =
            canvasAdd(appMainCanvas, notesTextBox, BLOCKERS_AND_NOTES_OFFSET, THRU_BLOCKERS_H) 
            canvasAdd(appMainCanvas, seedAndFlagsDisplayCanvas, BLOCKERS_AND_NOTES_OFFSET, THRU_BLOCKERS_H) 
        member this.AddExtraDungeonRightwardStuff(grabModeTextBlock, rightwardCanvas) = 
            canvasAdd(appMainCanvas, grabModeTextBlock, BLOCKERS_AND_NOTES_OFFSET, START_DUNGEON_AND_NOTES_AREA_H) // grab mode instructions
            canvasAdd(appMainCanvas, rightwardCanvas, BLOCKERS_AND_NOTES_OFFSET, START_DUNGEON_AND_NOTES_AREA_H)   // extra place for dungeonTabs to draw atop blockers/notes, e.g. hover minimaps
        member this.AddOWRemainingScreens(owRemainingScreensTextBox) =
            canvasAdd(appMainCanvas, owRemainingScreensTextBox, RIGHT_COL, 90.)
        member this.AddOWGettableScreens(owGettableScreensCheckBox) =
            canvasAdd(appMainCanvas, owGettableScreensCheckBox, RIGHT_COL, 110.)
        member this.AddCurrentMaxHearts(currentMaxHeartsTextBox) = 
            canvasAdd(appMainCanvas, currentMaxHeartsTextBox, RIGHT_COL, 130.)
        member this.AddShowCoords(showCoordsCB, havePotionLetterImage) = 
            canvasAdd(appMainCanvas, havePotionLetterImage, OW_ITEM_GRID_LOCATIONS.OFFSET+183., 63.)
            canvasAdd(appMainCanvas, showCoordsCB, OW_ITEM_GRID_LOCATIONS.OFFSET+200., 72.)
        member this.AddOWZoneOverlay(zone_checkbox) =
            canvasAdd(appMainCanvas, zone_checkbox, OW_ITEM_GRID_LOCATIONS.OFFSET+200., 52.)
        member this.AddMouseHoverExplainer(mouseHoverExplainerIcon, c) =
            canvasAdd(appMainCanvas, mouseHoverExplainerIcon, 540., 0.)
            canvasAdd(appMainCanvas, c, 0., 0.)
        member this.AddLinkTarget(currentTargetGhostBuster) =
            canvasAdd(appMainCanvas, currentTargetGhostBuster, 16.*OMTW-30., 120.)  // location where Link's currentTarget is
        member this.AddTimeline(t1c, t2c, t3c) =
            let TCX = 22.  // should be 24, but moving two pixels to the left is simplest fix to get timeline items on 3n pixel boundaries so 2/3 size looks good
            canvasAdd(appMainCanvas, t1c, TCX, START_TIMELINE_H)
            canvasAdd(appMainCanvas, t2c, TCX, START_TIMELINE_H)
            canvasAdd(appMainCanvas, t3c, TCX, START_TIMELINE_H)
        member this.GetTimelineBounds() =
            Rect(Point(0.,START_TIMELINE_H), Point(appMainCanvas.Width,THRU_TIMELINE_H))
        member this.GetFullAppBounds() =
            Rect(Point(0.,0.), Point(appMainCanvas.Width,THRU_TIMELINE_H))
        member this.AddReminderDisplayOverlay(reminderDisplayOuterDockPanel) =
            canvasAdd(appMainCanvas, reminderDisplayOuterDockPanel, 0., START_TIMELINE_H)
        member this.AddPostGameDecorationCanvas(postgameDecorationCanvas) =
            canvasAdd(appMainCanvas, postgameDecorationCanvas, 0., START_TIMELINE_H)
        member this.AddSpotSummary(spotSummaryCanvas) =
            canvasAdd(appMainCanvas, spotSummaryCanvas, 50., 3.)
        member this.AddDiskIcon(diskIcon) =
            canvasAdd(appMainCanvas, diskIcon, OMTW*16.-40., START_TIMELINE_H+60.)
        member this.AllDone() =
            canvasAdd(appMainCanvas, topLayerHoverCanvas, 0., 0.)
        member this.AddTopLayerHover(fe, x, y) = canvasAdd(topLayerHoverCanvas, fe, x, y)
        member this.ClearTopLayerHovers() = topLayerHoverCanvas.Children.Clear()
        member this.FocusOverworld() = ()
        member this.FocusDungeon() = ()
        member this.IsShort = false

///////////////////////////////////////////////////////////////////////////////////////

type ShorterApplicationLayout(cm:CustomComboBoxes.CanvasManager) =
    inherit ApplicationLayout(cm) 
    let appMainCanvas = cm.AppMainCanvas
    let topLayerHoverCanvas = new Canvas()   // for temporary bits that appear on hover, to ensure they appear on top of everything else in the app, when no other popup active
    let blockerGridHeight = float(36*3) // from blocker code
    let RED_LINE = 3.
    let DUNGEON_H = START_TIMELINE_H - THRU_MAIN_MAP_AND_ITEM_PROGRESS_H
    let TIMELINE_H = THRU_TIMELINE_H - START_TIMELINE_H
    let H = START_DUNGEON_AND_NOTES_AREA_H + RED_LINE + blockerGridHeight + TIMELINE_H  // the top one is the larger of the two, so always have window that size
    let afterSoldItemBoxesX = OW_ITEM_GRID_LOCATIONS.OFFSET + 120.
    let W = appMainCanvas.Width
    let theCanvas = new Canvas(Width=W, Height=H)
    // we always draw both the upper and lower, so that e.g. when mouse in upper, a timelime update (the timeline canonically lives in the lower) 
    // still draws immediately and we see the projection immediately.  do this by putting a black canvas in between, and swapping order drawn atop each other
    let black = new Canvas(Width=W, Height=H, Background=Brushes.Black)
    let upper = new Canvas(Width=W, Height=H)
    let lower = new Canvas(Width=W, Height=H)
    let mutable currentlyUpper = true
    // constants for upper half
    let upperTimelineStart = THRU_MAIN_MAP_AND_ITEM_PROGRESS_H + RED_LINE + blockerGridHeight
    // constants for lower half
    let dungeonStart = H - TIMELINE_H - DUNGEON_H
    let notesStart = H - TIMELINE_H - DUNGEON_H + blockerGridHeight
    let timelineStart = H - TIMELINE_H
    let switchToDungeon() =
        currentlyUpper <- false
        Canvas.SetZIndex(upper, 0)
        Canvas.SetZIndex(lower, 2)
    let switchToOverworld() =
        currentlyUpper <- true
        Canvas.SetZIndex(upper, 2)
        Canvas.SetZIndex(lower, 0)
    do
        canvasAdd(appMainCanvas, theCanvas, 0., 0.)  
        canvasAdd(theCanvas, upper, 0., 0.)  
        canvasAdd(theCanvas, black, 0., 0.)  
        canvasAdd(theCanvas, lower, 0., 0.)  
        Canvas.SetZIndex(upper, 2)
        Canvas.SetZIndex(black, 1)
        Canvas.SetZIndex(lower, 0)
        let upperThreshold = THRU_MAIN_MAP_AND_ITEM_PROGRESS_H + RED_LINE
        let lowerThreshold = H - TIMELINE_H - DUNGEON_H - RED_LINE
        let diff = upperThreshold - lowerThreshold
        DungeonPopups.THE_DIFF <- diff
        appMainCanvas.MouseMove.Add(fun e ->
            if cm.PopupCanvasStack.Count = 0 then
                let pos = e.GetPosition(appMainCanvas)
                if currentlyUpper && pos.Y > upperThreshold then
                    switchToDungeon()
                    Graphics.SilentlyWarpMouseCursorTo(Point(pos.X, pos.Y-diff))
                if not(currentlyUpper) && pos.Y < lowerThreshold then
                    switchToOverworld()
                    Graphics.SilentlyWarpMouseCursorTo(Point(pos.X, pos.Y+diff))
        )
    interface IApplicationLayoutBase with
        member this.AddMainTracker(mainTracker) =
            canvasAdd(upper, mainTracker, 0., 0.)
            let mainView = Broadcast.makeViewRectImpl(Point(0.,0.), Point(OW_ITEM_GRID_LOCATIONS.OFFSET,float(30*5)), upper)
            canvasAdd(lower, mainView, 0., 0.)
        member this.AddNumberedTriforceCanvas(triforceCanvas, i) =
            canvasAdd(upper, triforceCanvas, OW_ITEM_GRID_LOCATIONS.OFFSET+30.*float i, 0.)
            if i=1 then // only once
                let ntView = Broadcast.makeViewRectImpl(Point(OW_ITEM_GRID_LOCATIONS.OFFSET,0.), Point(OW_ITEM_GRID_LOCATIONS.OFFSET+float(30*8), float(30)), upper)
                canvasAdd(lower, ntView, OW_ITEM_GRID_LOCATIONS.OFFSET, 0.)
        member this.AddItemGridStuff(owItemGrid, toggleBookShieldCheckBox, bookIsAtlasCheckBox, highlightOpenCavesCheckBox, timerResetButton, spotSummaryTB, mirrorOW, hideIconsEyeball, moreFQSQoptionsButton) =
            canvasAdd(upper, owItemGrid, OW_ITEM_GRID_LOCATIONS.OFFSET, 30.)
            canvasAdd(upper, toggleBookShieldCheckBox, OW_ITEM_GRID_LOCATIONS.OFFSET+180., 35.)
            canvasAdd(upper, bookIsAtlasCheckBox, OW_ITEM_GRID_LOCATIONS.OFFSET+180.+55., 35.)
            canvasAdd(upper, highlightOpenCavesCheckBox, 510., 120.)
            canvasAdd(upper, timerResetButton, 12.8*OMTW, 60.)
            canvasAdd(upper, spotSummaryTB, 12.8*OMTW, 90.)
            canvasAdd(appMainCanvas, hideIconsEyeball, WEBCAM_LINE+5., 0.)
            canvasAdd(upper, mirrorOW, WEBCAM_LINE+5., 70.)
            canvasAdd(upper, moreFQSQoptionsButton, WEBCAM_LINE+70., 120.)
            // just capture a swath of stuff
            let swathView = Broadcast.makeViewRectImpl(Point(OW_ITEM_GRID_LOCATIONS.OFFSET,30.), Point(WEBCAM_LINE, float(30*5)), upper)
            canvasAdd(lower, swathView, OW_ITEM_GRID_LOCATIONS.OFFSET, 30.)
        member this.AddHideQuestCheckboxes(hideFirstQuestCheckBox, hideSecondQuestCheckBox) = 
            canvasAdd(upper, hideFirstQuestCheckBox,  WEBCAM_LINE + 5., 110.) 
            canvasAdd(upper, hideSecondQuestCheckBox, WEBCAM_LINE + 5., 130.)
        member this.AddLinkRouting(linkIcon, currentTargetIcon) =
            canvasAdd(upper, linkIcon, 16.*OMTW-60., 120.)
            canvasAdd(upper, currentTargetIcon, 16.*OMTW-30., 120.)
        member this.AddWebcamLine() =
            let webcamLine = new Canvas(Background=Brushes.Orange, Width=2., Height=150., Opacity=0.4)
            canvasAdd(upper, webcamLine, WEBCAM_LINE, 0.)
        member this.AddOverworldCanvas(overworldCanvas) =
            canvasAdd(upper, overworldCanvas, 0., 150.)
            let scaleW = (W - afterSoldItemBoxesX) / W
            let scaleH = (180.-45.)/(THRU_MAIN_MAP_H-150.)
            let owm = Broadcast.makeViewRectImpl(Point(0.,150.), Point(W,THRU_MAIN_MAP_H), upper)
            let scaleTrans = new ScaleTransform(scaleW,scaleH)
            if scaleTrans.CanFreeze then
                scaleTrans.Freeze()
            owm.RenderTransform <- scaleTrans
            canvasAdd(lower, owm, afterSoldItemBoxesX, dungeonStart-(180.-45.)-RED_LINE)
        member this.AddLegend(legendCanvas) = 
            canvasAdd(upper, legendCanvas, 0., THRU_MAIN_MAP_H)
        member this.AddItemProgress(itemProgressCanvas, itemProgressTB) = 
            canvasAdd(upper, itemProgressCanvas, 0., THRU_MAP_AND_LEGEND_H)
            canvasAdd(upper, itemProgressTB, 50., THRU_MAP_AND_LEGEND_H + 4.)
            let pro = Broadcast.makeViewRectImpl(Point(ITEM_PROGRESS_FIRST_ITEM,THRU_MAP_AND_LEGEND_H), 
                                Point(ITEM_PROGRESS_FIRST_ITEM + 13.*30.-11.,THRU_MAIN_MAP_AND_ITEM_PROGRESS_H), upper)  // -11. because 'Hint decoder' button infringes into empty space by Magic Key 
            canvasAdd(lower, pro, 20., 150.+20.)
            let tb = new TextBox(Text="Move mouse above red line for overworld view", Foreground=Brushes.Orange, Background=Brushes.Black, 
                                            IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), FontSize=16., VerticalAlignment=VerticalAlignment.Center)
            canvasAdd(lower, tb, 50., 180.+20.)
            let lineCenter = dungeonStart - RED_LINE/2.
            let line = new Shapes.Line(X1=0., X2=W, Y1=lineCenter, Y2=lineCenter, Stroke=Brushes.Red, StrokeThickness=3.)
            canvasAdd(lower, line, 0., 0.)
        member this.AddVersionButton(vb) = 
            canvasAdd(upper, vb, 0., THRU_MAP_AND_LEGEND_H + 4.)
            let v = Broadcast.makeViewRectImpl(Point(0.,THRU_MAP_AND_LEGEND_H), Point(50.,THRU_MAIN_MAP_AND_ITEM_PROGRESS_H), upper)
            canvasAdd(lower, v, 0., 180.+20.)
        member this.AddHintDecoderButton(tb) =
            canvasAdd(upper, tb, 510., THRU_MAP_AND_LEGEND_H + 6.)
        member this.AddKittyAndLogo(kitty:Image, logoBorder, ztlogo:Image) =
            canvasAdd(upper, kitty, 16.*OMTW - kitty.Width - 12., THRU_MAIN_MAP_H)
            canvasAdd(upper, logoBorder, 16.*OMTW - ztlogo.Width - 2., THRU_MAIN_MAP_H + kitty.Height - ztlogo.Height - 6.)
            // WANT!
            let kitty = new Image()
            let imageStream = Graphics.GetResourceStream("CroppedBrianKitty.png")
            kitty.Source <- System.Windows.Media.Imaging.BitmapFrame.Create(imageStream)
            kitty.Width <- 45.
            kitty.Height <- 45.
            canvasAdd(lower, kitty, afterSoldItemBoxesX+150. + 5., 45.)
            let ztlogo = new Image()
            let imageStream = Graphics.GetResourceStream("ZTlogo64x64.png")
            ztlogo.Source <- System.Windows.Media.Imaging.BitmapFrame.Create(imageStream)
            ztlogo.Width <- 30.
            ztlogo.Height <- 30.
            let logoBorder = new Border(BorderThickness=Thickness(1.), BorderBrush=Brushes.Gray, Child=ztlogo)
            canvasAdd(lower, logoBorder, afterSoldItemBoxesX+150. + 5. + 25., 56.)
        member this.AddShowHotKeysButton(showHotKeysButton) = 
            canvasAdd(upper, showHotKeysButton, 16.*OMTW - kittyWidth - 115., THRU_MAIN_MAP_H)
        member this.AddShowRunCustomButton(showRunCustomButton) = 
            canvasAdd(upper, showRunCustomButton, 16.*OMTW - kittyWidth - 115., THRU_MAIN_MAP_H + 22.)
        member this.AddSaveButton(saveButton) = 
            canvasAdd(upper, saveButton, 16.*OMTW - kittyWidth - 50., THRU_MAIN_MAP_H + 22.)
        member this.AddUserCustomContentButton(uccButton) = 
            // canvasAdd(upper, uccButton, 16.*OMTW - kittyWidth - 50., THRU_MAIN_MAP_H + 42.)
            ignore uccButton // UCC is disabled in ShorterApplicationLayout
        member this.AddDungeonTabsOverlay(dungeonTabsOverlay) =  // for magnifier
            // canvasAdd(upper, dungeonTabsOverlay, 0., THRU_MAP_AND_LEGEND_H)
            ignore dungeonTabsOverlay // magnifier is disabled in ShorterApplicationLayout
        member this.AddDungeonTabs(dungeonTabsWholeCanvas) =
            canvasAdd(lower, dungeonTabsWholeCanvas, 0., dungeonStart)
        member this.GetDungeonY() =
            dungeonStart
        member this.AddBlockers(blockerGrid) =
            canvasAdd(lower, blockerGrid, BLOCKERS_AND_NOTES_OFFSET, dungeonStart) 
            let blockersView = Broadcast.makeViewRectImpl(Point(BLOCKERS_AND_NOTES_OFFSET,dungeonStart), Point(W,dungeonStart + blockerGridHeight), lower)
            canvasAdd(upper, blockersView, BLOCKERS_AND_NOTES_OFFSET, THRU_MAIN_MAP_AND_ITEM_PROGRESS_H)
        member this.AddNotesSeedsFlags(notesTextBox, seedAndFlagsDisplayCanvas) =
            canvasAdd(lower, notesTextBox, BLOCKERS_AND_NOTES_OFFSET, notesStart) 
            canvasAdd(lower, seedAndFlagsDisplayCanvas, BLOCKERS_AND_NOTES_OFFSET, notesStart) 
            let notesView = Broadcast.makeViewRectImpl(Point(BLOCKERS_AND_NOTES_OFFSET,notesStart), Point(W,notesStart + blockerGridHeight), lower)
            canvasAdd(upper, notesView, 0., THRU_MAIN_MAP_AND_ITEM_PROGRESS_H + RED_LINE)
            let tb = new TextBox(Text="Move mouse\nbelow red line\nto switch to\ndungeon view", Foreground=Brushes.Orange, Background=Brushes.Black, 
                                            IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), FontSize=16., VerticalAlignment=VerticalAlignment.Center)
            let b = new Border(Child=tb, BorderThickness=Thickness(0., 3., 0., 0.))
            canvasAdd(upper, b, W - BLOCKERS_AND_NOTES_OFFSET + 5., THRU_MAIN_MAP_AND_ITEM_PROGRESS_H + RED_LINE)
            let lineCenter = THRU_MAIN_MAP_AND_ITEM_PROGRESS_H + RED_LINE/2.
            let line = new Shapes.Line(X1=0., X2=W, Y1=lineCenter, Y2=lineCenter, Stroke=Brushes.Red, StrokeThickness=3.)
            canvasAdd(upper, line, 0., 0.)
        member this.AddExtraDungeonRightwardStuff(grabModeTextBlock, rightwardCanvas) = 
            canvasAdd(lower, grabModeTextBlock, BLOCKERS_AND_NOTES_OFFSET, dungeonStart) // grab mode instructions
            canvasAdd(lower, rightwardCanvas, BLOCKERS_AND_NOTES_OFFSET, dungeonStart)   // extra place for dungeonTabs to draw atop blockers/notes, e.g. hover minimaps
        member this.AddOWRemainingScreens(owRemainingScreensTextBox) =
            canvasAdd(upper, owRemainingScreensTextBox, RIGHT_COL, 90.)
        member this.AddOWGettableScreens(owGettableScreensCheckBox) =
            canvasAdd(upper, owGettableScreensCheckBox, RIGHT_COL, 110.)
        member this.AddCurrentMaxHearts(currentMaxHeartsTextBox) = 
            canvasAdd(upper, currentMaxHeartsTextBox, RIGHT_COL, 130.)
        member this.AddShowCoords(showCoordsCB, havePotionLetterImage) = 
            canvasAdd(upper, havePotionLetterImage, OW_ITEM_GRID_LOCATIONS.OFFSET+183., 63.)
            canvasAdd(upper, showCoordsCB, OW_ITEM_GRID_LOCATIONS.OFFSET+200., 72.)
        member this.AddOWZoneOverlay(zone_checkbox) =
            canvasAdd(upper, zone_checkbox, OW_ITEM_GRID_LOCATIONS.OFFSET+200., 52.)
        member this.AddMouseHoverExplainer(mouseHoverExplainerIcon, c) =
            canvasAdd(upper, mouseHoverExplainerIcon, 540., 0.)
            canvasAdd(upper, c, 0., 0.)
        member this.AddLinkTarget(currentTargetGhostBuster) =
            canvasAdd(upper, currentTargetGhostBuster, 16.*OMTW-30., 120.)  // location where Link's currentTarget is
        member this.AddTimeline(t1c, t2c, t3c) =
            let TCX = 22.  // should be 24, but moving two pixels to the left is simplest fix to get timeline items on 3n pixel boundaries so 2/3 size looks good
            canvasAdd(lower, t1c, TCX, timelineStart)
            canvasAdd(lower, t2c, TCX, timelineStart)
            canvasAdd(lower, t3c, TCX, timelineStart)
            let timelineView = Broadcast.makeViewRectImpl(Point(0.,timelineStart), Point(W,timelineStart + TIMELINE_H), lower)
            canvasAdd(upper, timelineView, 0., upperTimelineStart)
        member this.GetTimelineBounds() =
            Rect(Point(0.,timelineStart), Point(W,H))
        member this.GetFullAppBounds() =
            Rect(Point(0.,0.), Point(W,H))
        member this.AddReminderDisplayOverlay(reminderDisplayOuterDockPanel) =
            canvasAdd(lower, reminderDisplayOuterDockPanel, 0., timelineStart)
        member this.AddPostGameDecorationCanvas(postgameDecorationCanvas) =
            canvasAdd(lower, postgameDecorationCanvas, 0., timelineStart)
        member this.AddSpotSummary(spotSummaryCanvas) =
            canvasAdd(upper, spotSummaryCanvas, 50., 3.)
        member this.AddDiskIcon(diskIcon) =
            canvasAdd(lower, diskIcon, OMTW*16.-40., timelineStart+60.)
            cm.SetHeight(H)  // canvas manager always knows the world as though SmallerAppWindowScaleFactor = 1.0
            // change the main app window height
            let H = 
                if TrackerModelOptions.SmallerAppWindow.Value then 
                    H*TrackerModelOptions.SmallerAppWindowScaleFactor
                else
                    H
            let CHROME_HEIGHT = 39.  // Windows app border
            ((cm.RootCanvas.Parent :?> Canvas).Parent :?> Window).Height <- H + CHROME_HEIGHT // this is fragile, but don't know a better way right now
        member this.AllDone() =
            canvasAdd(appMainCanvas, topLayerHoverCanvas, 0., 0.)
        member this.AddTopLayerHover(fe, x, y) = canvasAdd(topLayerHoverCanvas, fe, x, y)
        member this.ClearTopLayerHovers() = topLayerHoverCanvas.Children.Clear()
        member this.FocusOverworld() = 
            let ctxt = System.Threading.SynchronizationContext.Current
            async {
                do! Async.Sleep(10)   // the mouse was just warped before this call, pump events to process the MouseMove
                do! Async.SwitchToContext(ctxt)
                switchToOverworld()
            } |> Async.StartImmediate
        member this.FocusDungeon() = 
            let ctxt = System.Threading.SynchronizationContext.Current
            async {
                do! Async.Sleep(10)   // the mouse was just warped before this call, pump events to process the MouseMove
                do! Async.SwitchToContext(ctxt)
                switchToDungeon()
            } |> Async.StartImmediate
        member this.IsShort = true

////////////////////////////////////////////////////////////////////////

let makeMouseMagnifierWindow(cm:CustomComboBoxes.CanvasManager) =
    let mmWindow = new Window()
    mmWindow.Title <- "Z-Tracker mouse magnifier"
    mmWindow.ResizeMode <- ResizeMode.CanResizeWithGrip
    mmWindow.SizeToContent <- SizeToContent.Manual
    mmWindow.WindowStartupLocation <- WindowStartupLocation.Manual
    mmWindow.Owner <- Application.Current.MainWindow

    let MSF = // main scale factor
        if TrackerModelOptions.SmallerAppWindow.Value then
            TrackerModelOptions.SmallerAppWindowScaleFactor
        else
            1.0
    let defaultStartSizeFactor = 0.66666666666666
    mmWindow.Width <- cm.AppMainCanvas.Width * MSF * defaultStartSizeFactor
    mmWindow.Height <- cm.AppMainCanvas.Width * MSF * defaultStartSizeFactor  // square start

    let c = new Canvas(Background=Brushes.DarkSlateBlue)
    c.UseLayoutRounding <- true
    mmWindow.Content <- c
    // use BitmapCacheBrush rather than VisualBrush because we want to NearestNeightbor all the pixels
    let wholeView = new Shapes.Rectangle(Width=cm.RootCanvas.Width, Height=cm.RootCanvas.Height, Fill=new BitmapCacheBrush(cm.RootCanvas))
    RenderOptions.SetBitmapScalingMode(wholeView, BitmapScalingMode.NearestNeighbor)

    let SCALE = 3.0 * MSF
    let st = new ScaleTransform(SCALE, SCALE)
    wholeView.RenderTransform <- st
    canvasAdd(c, wholeView, 0., 0.)

    let addFakeMouse(c:Canvas) =
        let fakeMouse = new Shapes.Polygon(Fill=Brushes.White)
        fakeMouse.Points <- new PointCollection([Point(0.,0.); Point(12.,6.); Point(6.,12.)])
        c.Children.Add(fakeMouse) |> ignore
        fakeMouse
    let fakeMouse = addFakeMouse(c)
    let round(x:float) = System.Math.Round(x)
    let update() =        
        Canvas.SetLeft(fakeMouse, round(c.ActualWidth / 2.))
        Canvas.SetTop(fakeMouse, round(c.ActualHeight / 2.))
        st.CenterX <- round(c.ActualWidth / 2.)
        st.CenterY <- round(c.ActualHeight / 2.)
    let desc = "You can resize this window by grabbing\nthe bottom right of it, or move this window\nby grabbing the title bar with the mouse."
    let text = new TextBox(Text=desc, Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                                FontSize=16., Margin=Thickness(6.0))
    let b = new Border(Child=text, BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Background=Brushes.Black)
    let mutable loaded = false
    let w = mmWindow
    let save() = 
        TrackerModelOptions.MouseMagnifierWindowLTWH <- sprintf "%d,%d,%d,%d" (int w.Left) (int w.Top) (int w.Width) (int w.Height)
        TrackerModelOptions.writeSettings()
    let leftTopWidthHeight = TrackerModelOptions.MouseMagnifierWindowLTWH
    let matches = System.Text.RegularExpressions.Regex.Match(leftTopWidthHeight, """^(-?\d+),(-?\d+),(\d+),(\d+)$""")
    if matches.Success then
        w.Left <- float matches.Groups.[1].Value
        w.Top <- float matches.Groups.[2].Value
        w.Width <- float matches.Groups.[3].Value
        w.Height <- float matches.Groups.[4].Value
    mmWindow.LocationChanged.Add(fun _ ->
        if loaded then b.Opacity <- 0.  // dismiss instructions
        save()
        )
    mmWindow.SizeChanged.Add(fun _ ->
        update()
        if loaded then b.Opacity <- 0.  // dismiss instructions
        save()
        )
    mmWindow.Loaded.Add(fun _ -> 
        update()
        canvasAdd(c, b, 20., 20.)
        let ctxt = System.Threading.SynchronizationContext.Current
        async {
            do! Async.SwitchToThreadPool()
            do! Async.Sleep(8000)
            do! Async.SwitchToContext(ctxt)
            b.Opacity <- 0.  // dismiss instructions after a short time
        } |> Async.StartImmediate
        loaded <- true
        )
    cm.RootCanvas.MouseMove.Add(fun ea ->   // we need RootCanvas to see mouse moving in popups
        let mousePos = ea.GetPosition(cm.AppMainCanvas)
        Canvas.SetLeft(wholeView, round(SCALE * ((c.ActualWidth  / 2.) - mousePos.X)))
        Canvas.SetTop( wholeView, round(SCALE * ((c.ActualHeight / 2.) - mousePos.Y)))
        )
    mmWindow

let setupMouseMagnifier(cm, refocusMainWindow) =
    let mutable mouseMagnifierWindow = null
    if TrackerModelOptions.ShowMouseMagnifierWindow.Value then
        mouseMagnifierWindow <- makeMouseMagnifierWindow(cm)
        mouseMagnifierWindow.Show()
        refocusMainWindow()
    OptionsMenu.mouseMagnifierWindowOptionChanged.Publish.Add(fun () ->
        // close existing
        if mouseMagnifierWindow<>null then
            mouseMagnifierWindow.Close()
            mouseMagnifierWindow <- null
        // maybe restart
        if TrackerModelOptions.ShowMouseMagnifierWindow.Value then
            mouseMagnifierWindow <- makeMouseMagnifierWindow(cm)
            mouseMagnifierWindow.Show()
            refocusMainWindow()
        )











