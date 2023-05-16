module Layout

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media
open OverworldItemGridUI            // has canvasAdd, etc
open OverworldMapTileCustomization  // has a lot of constants

let RIGHT_COL = 440.
let WEBCAM_LINE = OMTW*16.-200.  // height of upper area is 150, so 200 wide is 4x3 box in upper right; timer and other controls here could be obscured
let kittyWidth = THRU_MAIN_MAP_AND_ITEM_PROGRESS_H - THRU_MAIN_MAP_H

type ApplicationLayout(appMainCanvas:Canvas) =
    member this.AddMainTracker(mainTracker:Grid) =
        canvasAdd(appMainCanvas, mainTracker, 0., 0.)
    member this.AddNumberedTriforceCanvas(triforceCanvas, i) =
        canvasAdd(appMainCanvas, triforceCanvas, OW_ITEM_GRID_LOCATIONS.OFFSET+30.*float i, 0.)
    member this.AddHideQuestCheckboxes(hideFirstQuestCheckBox, hideSecondQuestCheckBox) = 
        canvasAdd(appMainCanvas, hideFirstQuestCheckBox,  WEBCAM_LINE + 10., 130.) 
        canvasAdd(appMainCanvas, hideSecondQuestCheckBox, WEBCAM_LINE + 60., 130.)
    member this.AddWebcamLine() =
        let webcamLine = new Canvas(Background=Brushes.Orange, Width=2., Height=150., Opacity=0.4)
        canvasAdd(appMainCanvas, webcamLine, WEBCAM_LINE, 0.)
    member this.AddOverworldCanvas(overworldCanvas) =
        canvasAdd(appMainCanvas, overworldCanvas, 0., 150.)
    member this.AddItemProgress(itemProgressCanvas, itemProgressTB) = 
        canvasAdd(appMainCanvas, itemProgressCanvas, 0., THRU_MAP_AND_LEGEND_H)
        canvasAdd(appMainCanvas, itemProgressTB, 50., THRU_MAP_AND_LEGEND_H + 4.)
    member this.AddVersionButton(vb) = 
        canvasAdd(appMainCanvas, vb, 0., THRU_MAP_AND_LEGEND_H + 4.)
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
    member this.AddShowCoords(showCoordsCB) = 
        canvasAdd(appMainCanvas, showCoordsCB, OW_ITEM_GRID_LOCATIONS.OFFSET+200., 72.)
    member this.AddOWZoneOverlay(zone_checkbox) =
        canvasAdd(appMainCanvas, zone_checkbox, OW_ITEM_GRID_LOCATIONS.OFFSET+200., 52.)
    member this.AddMouseHoverExplainer(mouseHoverExplainerIcon, c) =
        canvasAdd(appMainCanvas, mouseHoverExplainerIcon, 540., 0.)
        canvasAdd(appMainCanvas, c, 0., 0.)
    member this.AddLinkTarget(currentTargetGhostBuster) =
        canvasAdd(appMainCanvas, currentTargetGhostBuster, 16.*OMTW-30., 120.)  // location where Link's currentTarget is
    member this.AddTimelineAndButtons(t1c, t2c, t3c, moreOptionsButton, drawButton) =
        canvasAdd(appMainCanvas, t1c, 24., START_TIMELINE_H)
        canvasAdd(appMainCanvas, t2c, 24., START_TIMELINE_H)
        canvasAdd(appMainCanvas, t3c, 24., START_TIMELINE_H)
        canvasAdd(appMainCanvas, moreOptionsButton, 0., START_TIMELINE_H)
        canvasAdd(appMainCanvas, drawButton, 0., START_TIMELINE_H+25.)
    member this.GetTimelineBounds() =
        Rect(Point(0.,START_TIMELINE_H), Point(appMainCanvas.Width,THRU_TIMELINE_H))
    member this.AddReminderDisplayOverlay(reminderDisplayOuterDockPanel) =
        canvasAdd(appMainCanvas, reminderDisplayOuterDockPanel, 0., START_TIMELINE_H)
    member this.AddPostGameDecorationCanvas(postgameDecorationCanvas) =
        canvasAdd(appMainCanvas, postgameDecorationCanvas, 0., START_TIMELINE_H)
    member this.AddSpotSummary(spotSummaryCanvas) =
// TODO - not good layout overall, cut off in broadcast, and in new ideas
        canvasAdd(appMainCanvas, spotSummaryCanvas, 50., 30.)  // height chosen to make broadcast-window-cutoff be reasonable
    member this.AddDiskIcon(diskIcon) =
        canvasAdd(appMainCanvas, diskIcon, OMTW*16.-40., START_TIMELINE_H+60.)








