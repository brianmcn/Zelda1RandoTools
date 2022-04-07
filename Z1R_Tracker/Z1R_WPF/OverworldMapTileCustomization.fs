module OverworldMapTileCustomization

open System.Windows.Controls
open System.Windows
open System.Windows.Media

let OMTW = Graphics.OMTW
let canvasAdd = Graphics.canvasAdd

///////////////////////////////////////////////

// there are a few bits of code that need to 'point' at the owItemGrid in the top right of the main tracker
// factor out the locations here

module OW_ITEM_GRID_LOCATIONS =
    let OFFSET = 280.  // the x coordinate of grid; the y is always 30. (just below the numbered triforces in HDN)
    // there is a 5x4 grid of icons, each 30x30, here are their 0-based grid coords
    let WHITE_SWORD_ICON = 0,0
    let WHITE_SWORD_ITEM_BOX = 1,0
    let MAGS_BOX = 2,0
    let WOOD_SWORD_BOX = 3,0
    let BOOMSTICK_BOX = 4,0

    let LADDER_ICON = 0,1
    let LADDER_ITEM_BOX = 1,1
    let BLUE_CANDLE_BOX = 2,1
    let WOOD_ARROW_BOX = 3,1
    let BLUE_RING_BOX = 4,1

    let ARMOS_ICON = 0,2
    let ARMOS_ITEM_BOX = 1,2
    // nothing at 2,2
    let GANON_BOX = 3,2
    let ZELDA_BOX = 4,2

    let HEARTS = 0,3  // and 1,2 and 2,3 and 3,3; nothing at 4,3

    let BOMB_RIGHT_OF_BLUE_RING = 40.  // bomb icon is 40 pixels right of blue ring

    let Locate(gridX, gridY) = (OFFSET + 30.*float gridX, 30. + 30.*float gridY)
    let LocateBomb() =
        let x,y = Locate(BLUE_RING_BOX)
        x+BOMB_RIGHT_OF_BLUE_RING, y

///////////////////////////////////////////////

type MapStateProxy(state) =
    static member NumStates = Graphics.theInteriorBmpTable.Length
    member this.State = state
    member this.IsX = state=MapStateProxy.NumStates-1
    member this.IsDungeon = state >= 0 && state < 9
    member this.IsWarp = state >= 9 && state < 13
    member this.IsThreeItemShop = TrackerModel.MapSquareChoiceDomainHelper.IsItem(state)
    member this.IsInteresting = not(state = -1 || this.IsX)
    member this.DefaultInteriorBmp() =  // used by grid popup, reminders, and Link destination icons; unchanging so e.g. dungeons in popup are always yellow
        if state = -1 then
            null
        elif this.IsDungeon then
            if TrackerModel.IsHiddenDungeonNumbers() then Graphics.theInteriorBmpTable.[state].[2] else Graphics.theInteriorBmpTable.[state].[0]
        else
            Graphics.theInteriorBmpTable.[state].[0]

let resizeMapTileImage(image:Image) =
    image.Width <- OMTW
    image.Height <- float(11*3)
    image.Stretch <- Stretch.Fill
    image.StretchDirection <- StretchDirection.Both
    image
            
let computeExtraDecorationArrow(topX, topY, pos:Point) =
    // in appMainCanvas coordinates:
    // bottom middle of the box, as an arrow target
    let tx,ty = topX+15., topY+30.
    // top middle of the box we are drawing on the map, as an arrow source
    let sx,sy = pos.X+15., pos.Y
    let line,triangle = Graphics.makeArrow(tx, ty, sx, sy, Brushes.Yellow)
    // rectangle for remote box highlight
    let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=Brushes.Yellow, StrokeThickness=3.)
    let extraDecorations : (FrameworkElement*float*float)[] = [|
        upcast line, -pos.X-3., -pos.Y-3.
        upcast triangle, -pos.X-3., -pos.Y-3.
        upcast rect, topX-pos.X-3., topY-pos.Y-3.
        |]
    extraDecorations

let (armosX, armosY), (sword2x,sword2y) = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.ARMOS_ITEM_BOX), OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.WHITE_SWORD_ITEM_BOX)

let DoRemoteItemComboBox(cm:CustomComboBoxes.CanvasManager, activationDelta, trackerModelBoxToUpdate:TrackerModel.Box,
                            topX,topY,pos:Point) = async {  // topX,topY,pos are relative to appMainCanvas; top is for tracker box, pos is for mouse-local box
    let extraDecorations = computeExtraDecorationArrow(topX, topY, pos)
    let! r = CustomComboBoxes.DisplayItemComboBox(cm, pos.X, pos.Y, trackerModelBoxToUpdate.CellCurrent(), activationDelta, trackerModelBoxToUpdate.PlayerHas(), extraDecorations)
    match r with
    | Some(newBoxCellValue, newPlayerHas) ->
        trackerModelBoxToUpdate.Set(newBoxCellValue, newPlayerHas)
        TrackerModel.forceUpdate()
    | None -> ()
    }

let overworldAcceleratorTable = new System.Collections.Generic.Dictionary<_,_>()
overworldAcceleratorTable.Add(TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY, (fun (cm:CustomComboBoxes.CanvasManager,c:Canvas,i,j) -> async {
    let pos = c.TranslatePoint(Point(OMTW/2.,float(11*3)/2.), cm.AppMainCanvas)  
    let! shouldMarkTakeAnyAsComplete = PieMenus.TakeAnyPieMenuAsync(cm, 666.)
    Graphics.WarpMouseCursorTo(pos)
    TrackerModel.setOverworldMapExtraData(i, j, TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY, if shouldMarkTakeAnyAsComplete then TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY else 0)
    }))
overworldAcceleratorTable.Add(TrackerModel.MapSquareChoiceDomainHelper.SWORD1, (fun (cm:CustomComboBoxes.CanvasManager,c:Canvas,i,j) -> async {
    let pos = c.TranslatePoint(Point(OMTW/2.,float(11*3)/2.), cm.AppMainCanvas)  
    let! shouldMarkTakeAnyAsComplete = PieMenus.TakeThisPieMenuAsync(cm, 666.)
    Graphics.WarpMouseCursorTo(pos)
    TrackerModel.setOverworldMapExtraData(i, j, TrackerModel.MapSquareChoiceDomainHelper.SWORD1, if shouldMarkTakeAnyAsComplete then TrackerModel.MapSquareChoiceDomainHelper.SWORD1 else 0)
    }))
overworldAcceleratorTable.Add(TrackerModel.MapSquareChoiceDomainHelper.ARMOS, (fun (cm:CustomComboBoxes.CanvasManager,c:Canvas,_i,_j) -> async {
    let pos = c.TranslatePoint(Point(OMTW/2.-15.,1.), cm.AppMainCanvas)  // place to draw the local box
    do! DoRemoteItemComboBox(cm, 0, TrackerModel.armosBox, armosX, armosY, pos)
    }))
overworldAcceleratorTable.Add(TrackerModel.MapSquareChoiceDomainHelper.SWORD2, (fun (cm:CustomComboBoxes.CanvasManager,c:Canvas,_i,_j) -> async {
    let pos = c.TranslatePoint(Point(OMTW/2.-15.,1.), cm.AppMainCanvas)  // place to draw the local box
    do! DoRemoteItemComboBox(cm, 0, TrackerModel.sword2Box, sword2x, sword2y, pos)
    }))

let sword2LeftSideFullTileBmp =
    let interiorBmp = Graphics.theInteriorBmpTable.[TrackerModel.MapSquareChoiceDomainHelper.SWORD2].[0]
    let fullTileBmp = new System.Drawing.Bitmap(16*3,11*3)
    for px = 0 to 16*3-1 do
        for py = 0 to 11*3-1 do
            if px>=1*3 && px<6*3 && py>=1*3 && py<10*3 then 
                fullTileBmp.SetPixel(px, py, interiorBmp.GetPixel(px-1*3, py-1*3))
            else
                fullTileBmp.SetPixel(px, py, Graphics.TRANS_BG)
    fullTileBmp

let makeTwoItemShopBmp(item1, item2) =  // 0-based, -1 for blank
    if item1 = item2 then
        let state = item1 + TrackerModel.MapSquareChoiceDomainHelper.ARROW
        Graphics.theFullTileBmpTable.[state].[0]
    else
        // cons up a two-item shop image
        let tile = new System.Drawing.Bitmap(16*3,11*3)
        for px = 0 to 16*3-1 do
            for py = 0 to 11*3-1 do
                // two-icon area
                if px/3 >= 3 && px/3 <= 11 && py/3 >= 1 && py/3 <= 9 then
                    tile.SetPixel(px, py, Graphics.itemBackgroundColor)
                else
                    tile.SetPixel(px, py, Graphics.TRANS_BG)
                if item1 >=0 then
                    // icon 1
                    if px/3 >= 4 && px/3 <= 6 && py/3 >= 2 && py/3 <= 8 then
                        let c = Graphics.itemsBMP.GetPixel(item1*3 + px/3-4, py/3-2)
                        if c.ToArgb() <> System.Drawing.Color.Black.ToArgb() then
                            tile.SetPixel(px, py, c)
                if item2 >= 0 then
                    // icon 2
                    if px/3 >= 8 && px/3 <= 10 && py/3 >= 2 && py/3 <= 8 then
                        let c = Graphics.itemsBMP.GetPixel(item2*3 + px/3-8, py/3-2)
                        if c.ToArgb() <> System.Drawing.Color.Black.ToArgb() then
                            tile.SetPixel(px, py, c)
        tile

let GetIconBMPAndExtraDecorations(cm, ms:MapStateProxy,i,j) =
    if ms.State = -1 then
        null, []
    elif ms.IsThreeItemShop && TrackerModel.getOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP) <> 0 then
        let item1 = ms.State - TrackerModel.MapSquareChoiceDomainHelper.ARROW  // 0-based
        let item2 = TrackerModel.getOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP) - 1   // 0-based
        let tile = makeTwoItemShopBmp(item1, item2)
        tile, []
    // secrets default to being dark
    elif ms.State = TrackerModel.MapSquareChoiceDomainHelper.LARGE_SECRET then
        if TrackerModel.getOverworldMapExtraData(i,j,ms.State)=TrackerModel.MapSquareChoiceDomainHelper.LARGE_SECRET then
            Graphics.theFullTileBmpTable.[ms.State].[0], []
        else
            Graphics.theFullTileBmpTable.[ms.State].[1], []
    elif ms.State = TrackerModel.MapSquareChoiceDomainHelper.MEDIUM_SECRET then
        if TrackerModel.getOverworldMapExtraData(i,j,ms.State)=TrackerModel.MapSquareChoiceDomainHelper.MEDIUM_SECRET then
            Graphics.theFullTileBmpTable.[ms.State].[0], []
        else
            Graphics.theFullTileBmpTable.[ms.State].[1], []
    elif ms.State = TrackerModel.MapSquareChoiceDomainHelper.SMALL_SECRET then
        if TrackerModel.getOverworldMapExtraData(i,j,ms.State)=TrackerModel.MapSquareChoiceDomainHelper.SMALL_SECRET then
            Graphics.theFullTileBmpTable.[ms.State].[0], []
        else
            Graphics.theFullTileBmpTable.[ms.State].[1], []
    // door repair & potion letter always dark (but light in the grid selector)
    elif ms.State = TrackerModel.MapSquareChoiceDomainHelper.DOOR_REPAIR_CHARGE then
        Graphics.theFullTileBmpTable.[ms.State].[1], []
    elif ms.State = TrackerModel.MapSquareChoiceDomainHelper.THE_LETTER then
        Graphics.theFullTileBmpTable.[ms.State].[1], []
    // take any and sword1 default to being light (accelerators often darken them)
    elif ms.State = TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY then
        if TrackerModel.getOverworldMapExtraData(i,j,ms.State)=TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY then
            Graphics.theFullTileBmpTable.[ms.State].[1], []
        else
            Graphics.theFullTileBmpTable.[ms.State].[0], []
    elif ms.State = TrackerModel.MapSquareChoiceDomainHelper.SWORD1 then
        if TrackerModel.getOverworldMapExtraData(i,j,ms.State)=TrackerModel.MapSquareChoiceDomainHelper.SWORD1 then
            Graphics.theFullTileBmpTable.[ms.State].[1], []
        else
            Graphics.theFullTileBmpTable.[ms.State].[0], []
    // hint shop default to being light, user can darken if bought all, or if was white/magic sword hint they already saw
    elif ms.State = TrackerModel.MapSquareChoiceDomainHelper.HINT_SHOP then
        if TrackerModel.getOverworldMapExtraData(i,j,ms.State)=TrackerModel.MapSquareChoiceDomainHelper.HINT_SHOP then
            Graphics.theFullTileBmpTable.[ms.State].[1], []
        else
            Graphics.theFullTileBmpTable.[ms.State].[0], []
    elif ms.State = TrackerModel.MapSquareChoiceDomainHelper.SWORD2 then
        if not(TrackerModel.sword2Box.IsDone()) then
            let extraDecorationsF(boxPos:Point) =
                let extraDecorations = computeExtraDecorationArrow(sword2x, sword2y, boxPos)
                extraDecorations
            sword2LeftSideFullTileBmp, [Views.MakeBoxItemWithExtraDecorations(cm, TrackerModel.sword2Box, false, extraDecorationsF), OMTW-30., 1.]
        else
            Graphics.theFullTileBmpTable.[ms.State].[0], []
    elif ms.IsDungeon then
        let combine(number:System.Drawing.Bitmap, letter:System.Drawing.Bitmap) =
            let fullTileBmp = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    if px>=3*3 && px<8*3 && py>=1*3 && py<10*3 then 
                        fullTileBmp.SetPixel(px, py, number.GetPixel(px-3*3, py-1*3))
                    elif px>=7*3 && px<12*3 && py>=1*3 && py<10*3 then // sharing one 'pixel'
                        fullTileBmp.SetPixel(px, py, letter.GetPixel(px-7*3, py-1*3))  
                    else
                        fullTileBmp.SetPixel(px, py, Graphics.TRANS_BG)
            fullTileBmp
        if TrackerModel.IsHiddenDungeonNumbers() then 
            let isGreen = TrackerModel.GetDungeon(ms.State).PlayerHasTriforce() && TrackerModel.playerComputedStateSummary.HaveRecorder
            if TrackerModel.GetDungeon(ms.State).LabelChar <> '?' then
                if isGreen then
                    let letter = Graphics.theInteriorBmpTable.[ms.State].[3]
                    let number = Graphics.theInteriorBmpTable.[int(TrackerModel.GetDungeon(ms.State).LabelChar) - int('1')].[1]
                    combine(number,letter), []
                else
                    let letter = Graphics.theInteriorBmpTable.[ms.State].[2]
                    let number = Graphics.theInteriorBmpTable.[int(TrackerModel.GetDungeon(ms.State).LabelChar) - int('1')].[0]
                    combine(number,letter), []
            else
                if isGreen then
                    Graphics.theFullTileBmpTable.[ms.State].[3], []
                else
                    Graphics.theFullTileBmpTable.[ms.State].[2], []
        else 
            if TrackerModel.GetDungeon(ms.State).PlayerHasTriforce() && TrackerModel.playerComputedStateSummary.HaveRecorder then
                Graphics.theFullTileBmpTable.[ms.State].[1], []
            else
                Graphics.theFullTileBmpTable.[ms.State].[0], []
    else
        Graphics.theFullTileBmpTable.[ms.State].[0], []

let toggleables = [| 
    TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY
    TrackerModel.MapSquareChoiceDomainHelper.SWORD1
    TrackerModel.MapSquareChoiceDomainHelper.HINT_SHOP
    TrackerModel.MapSquareChoiceDomainHelper.LARGE_SECRET
    TrackerModel.MapSquareChoiceDomainHelper.MEDIUM_SECRET
    TrackerModel.MapSquareChoiceDomainHelper.SMALL_SECRET
    |]
let ToggleOverworldTileIfItIsToggleable(i, j, state) =
    if toggleables |> Array.contains state then
        // left click to toggle it 'used'
        let ex = TrackerModel.getOverworldMapExtraData(i,j,state)
        if ex=state then
            TrackerModel.setOverworldMapExtraData(i,j,state,0)
        else
            TrackerModel.setOverworldMapExtraData(i,j,state,state)

let DoLeftClick(cm,msp:MapStateProxy,i,j,pos:Point,popupIsActive:ref<bool>) = async { // returns tuple of two booleans (needRedrawGridSpot, needUIUpdate)
    if msp.State = -1 then
        // left click empty tile changes to 'X'
        TrackerModel.overworldMapMarks.[i,j].Prev() 
        return true, true
    elif msp.IsThreeItemShop then
        popupIsActive := true
        let item1 = msp.State - TrackerModel.MapSquareChoiceDomainHelper.ARROW  // 0-based
        let item2 = TrackerModel.getOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP) - 1   // 0-based
        let ST = CustomComboBoxes.borderThickness
        let tileImage = resizeMapTileImage <| Graphics.BMPtoImage(makeTwoItemShopBmp(item1,item2))
        let tileCanvas = new Canvas(Width=OMTW, Height=11.*3.)
        canvasAdd(tileCanvas, tileImage, 0., 0.)
        let originalState = if item2 = -1 then item1 else item2
        let originalStateIndex = originalState
        let gridxPosition = 
            if pos.X < OMTW*2. then 
                -ST // left align
            elif pos.X > OMTW*13. then 
                OMTW - float(8*(5*3+2*int ST)+int ST)  // right align
            else
                (OMTW - float(8*(5*3+2*int ST)+int ST))/2.  // center align
        let gridElementsSelectablesAndIDs : (FrameworkElement*bool*int)[] = Array.init 8 (fun n ->
            let i = n + TrackerModel.MapSquareChoiceDomainHelper.ARROW
            upcast Graphics.BMPtoImage(MapStateProxy(i).DefaultInteriorBmp()), true, n
            )
        let! g = CustomComboBoxes.DoModalGridSelect(cm, pos.X, pos.Y, tileCanvas,
                        gridElementsSelectablesAndIDs, originalStateIndex, 0, (8, 1, 5*3, 9*3), gridxPosition, 11.*3.+ST,
                        (fun (currentState) -> 
                            tileCanvas.Children.Clear()
                            let tileImage = resizeMapTileImage <| Graphics.BMPtoImage(makeTwoItemShopBmp(item1,currentState))
                            canvasAdd(tileCanvas, tileImage, 0., 0.)
                            ),
                        (fun (_ea, currentState) -> CustomComboBoxes.DismissPopupWithResult(currentState)),
                        [], CustomComboBoxes.ModalGridSelectBrushes.Defaults(), true, None)
        let r =
            match g with
            | Some(currentState) ->
                TrackerModel.setOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP,currentState+1)  // extraData is 1-based
                true, (originalState = -1 && currentState <> -1)
            | None -> false, false
        popupIsActive := false
        return r
    else
        if toggleables |> Array.contains msp.State then
            // left click to toggle it 'used'
            ToggleOverworldTileIfItIsToggleable(i, j, msp.State)
            return true, false
        else
            return false, false
    }

///////////////////////////////////////////////////

let DoSpecialHotKeyHandlingForOverworldTiles(i, j, originalState, hotKeyedState) =
    // rather than have many idempotent keys, turn some of them into useful actions
    let orig = MapStateProxy(originalState)
    if orig.IsThreeItemShop then
        let item2 = TrackerModel.getOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP) - 1   // 0-based
        let item2AsState = item2 + TrackerModel.MapSquareChoiceDomainHelper.ARROW
        if hotKeyedState = originalState then           // pressed same key as left item
            if item2 = -1 then
                -1   // no second item, so turn back to empty tile
            else
                item2AsState   // change first item to second item
        elif MapStateProxy(hotKeyedState).IsThreeItemShop then
            // first item is a different shop, is hotkey the second item? if so can manipulate
            if item2AsState = hotKeyedState then        // pressed same key as right item
                TrackerModel.setOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP, 0)  // remove item2
                originalState   // tell caller not to change item1, despite the hotkey
            elif item2 = -1 then
                let v = hotKeyedState - TrackerModel.MapSquareChoiceDomainHelper.ARROW + 1
                TrackerModel.setOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SHOP, v)  // add as item2
                originalState   // tell caller not to change item1, despite the hotkey
            else
                hotKeyedState
        else
            hotKeyedState
    elif hotKeyedState=originalState && toggleables |> Array.contains originalState then
        ToggleOverworldTileIfItIsToggleable(i, j, originalState)
        hotKeyedState
    else
        hotKeyedState

///////////////////////////////////////////////////

let MakeRemainderSummaryDisplay() =
    let sp = new StackPanel(Orientation=Orientation.Vertical)
    let b x = new Border(Child=x, BorderThickness=Thickness(3.), BorderBrush=Brushes.Black)
    let HEIGHT = 3. + 27. + 3.

    let OPA = 0.4
    let OPA2 = 0.75

    let header(txt) =
        let text = new TextBox(Text=txt, Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                                    FontSize=16., HorizontalContentAlignment=HorizontalAlignment.Center)
        sp.Children.Add(text) |> ignore
    let horizontalRule() =
        sp.Children.Add(new Canvas(Height=5., Background=Brushes.Gray, Margin=Thickness(3.))) |> ignore

    header("Remaining Locations Summary")
    horizontalRule()

    // uniques
    header("Unique Locations")

    let row = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    for i = 0 to 8 do
        let icon = Graphics.BMPtoImage(MapStateProxy(i).DefaultInteriorBmp())
        if not(TrackerModel.mapSquareChoiceDomain.CanAddUse(i)) then
            icon.Opacity <- OPA
        row.Children.Add(b icon) |> ignore
    sp.Children.Add(row) |> ignore

    let row = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    for i in [9;10;11;12;13;14;15;30;31] do
        let icon = Graphics.BMPtoImage(MapStateProxy(i).DefaultInteriorBmp())
        if not(TrackerModel.mapSquareChoiceDomain.CanAddUse(i)) then
            icon.Opacity <- OPA
        row.Children.Add(b icon) |> ignore
    sp.Children.Add(row) |> ignore

    horizontalRule()

    // secrets 
    let LARGE, MEDIUM, SMALL, UNKNOWN = TrackerModel.MapSquareChoiceDomainHelper.LARGE_SECRET, TrackerModel.MapSquareChoiceDomainHelper.MEDIUM_SECRET, 
                                            TrackerModel.MapSquareChoiceDomainHelper.SMALL_SECRET, TrackerModel.MapSquareChoiceDomainHelper.UNKNOWN_SECRET
    let LARGE_BMP, MEDIUM_BMP, SMALL_BMP =
        Graphics.theInteriorBmpTable.[LARGE].[0], Graphics.theInteriorBmpTable.[MEDIUM].[0], Graphics.theInteriorBmpTable.[SMALL].[0]
    // grab at most 14 that the user marked
    let mutable userLarge,userMedium,userSmall,userUnknown,userTotal = 0,0,0,0,0
    for i = 0 to TrackerModel.mapSquareChoiceDomain.NumUses(LARGE)-1 do
        if userTotal<14 then
            userLarge <- userLarge + 1
            userTotal <- userTotal + 1
    for i = 0 to TrackerModel.mapSquareChoiceDomain.NumUses(MEDIUM)-1 do
        if userTotal<14 then
            userMedium <- userMedium + 1
            userTotal <- userTotal + 1
    for i = 0 to TrackerModel.mapSquareChoiceDomain.NumUses(SMALL)-1 do
        if userTotal<14 then
            userSmall <- userSmall + 1
            userTotal <- userTotal + 1
    for i = 0 to TrackerModel.mapSquareChoiceDomain.NumUses(UNKNOWN)-1 do
        if userTotal<14 then
            userUnknown <- userUnknown + 1
            userTotal <- userTotal + 1
    // place as many as possible into the 'right' bins
    let dark(bmp) =
        let i = Graphics.BMPtoImage bmp
        i.Opacity <- OPA
        i
    let large,medium,small = if TrackerModel.owInstance.Quest.IsFirstQuestOW then 3,7,4 else 1,7,6
    let userLargeIcons,userMediumIcons,userSmallIcons = ResizeArray(), ResizeArray(), ResizeArray()
    while userLarge > 0 && userLargeIcons.Count < large do
        userLargeIcons.Add(dark <| LARGE_BMP)
        userLarge <- userLarge - 1
    while userMedium > 0 && userMediumIcons.Count < medium do
        userMediumIcons.Add(dark <| MEDIUM_BMP)
        userMedium <- userMedium - 1
    while userSmall > 0 && userSmallIcons.Count < small do
        userSmallIcons.Add(dark <| SMALL_BMP)
        userSmall <- userSmall - 1
    // allocate the remainders into other bins
    while userLarge > 0 do
        if medium - userMediumIcons.Count > small - userSmallIcons.Count then
            userMediumIcons.Insert(0, dark <| LARGE_BMP)
        else
            userSmallIcons.Insert(0, dark <| LARGE_BMP)
        userLarge <- userLarge - 1
    while userMedium > 0 do
        if large - userLargeIcons.Count > small - userSmallIcons.Count then
            userLargeIcons.Add(dark <| MEDIUM_BMP)
        else
            userSmallIcons.Insert(0, dark <| MEDIUM_BMP)
        userMedium <- userMedium - 1
    while userSmall > 0 do
        if large - userLargeIcons.Count > medium - userMediumIcons.Count then
            userLargeIcons.Add(dark <| SMALL_BMP)
        else
            userMediumIcons.Add(dark <| SMALL_BMP)
        userSmall <- userSmall - 1
    let mutable largeUnknown, mediumUnknown, smallUnknown = 0,0,0
    while userUnknown > 0 do
        let largeHoles = large - userLargeIcons.Count 
        let mediumHoles = medium - userMediumIcons.Count
        let smallHoles = small - userSmallIcons.Count
        if largeHoles >= mediumHoles && largeHoles >= smallHoles then
            userLargeIcons.Add(Graphics.BMPtoImage <| MapStateProxy(UNKNOWN).DefaultInteriorBmp())
            largeUnknown <- largeUnknown + 1
        elif mediumHoles >= largeHoles && mediumHoles >= smallHoles then
            userMediumIcons.Add(Graphics.BMPtoImage <| MapStateProxy(UNKNOWN).DefaultInteriorBmp())
            mediumUnknown <- mediumUnknown + 1
        else
            userSmallIcons.Add(Graphics.BMPtoImage <| MapStateProxy(UNKNOWN).DefaultInteriorBmp())
            smallUnknown <- smallUnknown + 1
        userUnknown <- userUnknown - 1

    header("↓ Actual Secrets ↓")
    let row = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    for i = 1 to large do
        let icon = Graphics.BMPtoImage(LARGE_BMP)
        if i > large - largeUnknown then
            icon.Opacity <- OPA2
        elif i > large - userLargeIcons.Count then
            icon.Opacity <- OPA
        row.Children.Add(b icon) |> ignore
    for i = 1 to medium do
        let icon = Graphics.BMPtoImage(MEDIUM_BMP)
        if i > medium - mediumUnknown then
            icon.Opacity <- OPA2
        elif i > medium - userMediumIcons.Count then
            icon.Opacity <- OPA
        row.Children.Add(b icon) |> ignore
    for i = 1 to small do
        let icon = Graphics.BMPtoImage(SMALL_BMP)
        if i > small - smallUnknown then
            icon.Opacity <- OPA2
        elif i > small - userSmallIcons.Count then
            icon.Opacity <- OPA
        row.Children.Add(b icon) |> ignore
    sp.Children.Add(row) |> ignore

    // pad out 
    let blacken(i:Image) = i.Opacity <- 0.01; i
    for i = 1 to large-userLargeIcons.Count do userLargeIcons.Insert(0, blacken(Graphics.BMPtoImage(LARGE_BMP)))
    for i = 1 to medium-userMediumIcons.Count do userMediumIcons.Insert(0, blacken(Graphics.BMPtoImage(MEDIUM_BMP)))
    for i = 1 to small-userSmallIcons.Count do userSmallIcons.Insert(0, blacken(Graphics.BMPtoImage(SMALL_BMP)))

    let row = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    for icon in [yield! userLargeIcons; yield! userMediumIcons; yield! userSmallIcons] do
        row.Children.Add(b icon) |> ignore
    sp.Children.Add(row) |> ignore
    header("↑ Your Secret Marks ↑")

    horizontalRule()
    
    // multis
    header("Non-Unique Locations")
    let allLefts, allRights = new StackPanel(Orientation=Orientation.Vertical), new StackPanel(Orientation=Orientation.Vertical)
    for multi in [28; 29; 32; 33; 34] do
        let leftrow = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Right, Height=HEIGHT)
        let rightrow = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Left, Height=HEIGHT)
        for i = 0 to TrackerModel.mapSquareChoiceDomain.MaxUses(multi)-1 do
            let icon = Graphics.BMPtoImage(MapStateProxy(multi).DefaultInteriorBmp())
            if not(i >= TrackerModel.mapSquareChoiceDomain.NumUses(multi)) then
                icon.Opacity <- OPA
                rightrow.Children.Add(b icon) |> ignore
            else
                leftrow.Children.Add(b icon) |> ignore
        allLefts.Children.Add(leftrow) |> ignore
        allRights.Children.Add(rightrow) |> ignore
    let hsp = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    hsp.Children.Add(allLefts) |> ignore
    hsp.Children.Add(allRights) |> ignore
    sp.Children.Add(hsp) |> ignore

    horizontalRule()

    // shop summary
    header("Shop summary (only first item shown)")
    let shopCount = if TrackerModel.owInstance.Quest.IsFirstQuestOW then 12 else 15
    let foundShopRepresentatives = ResizeArray()
    for i = 0 to 15 do
        for j = 0 to 7 do
            let cur = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
            if cur.IsThreeItemShop then
                foundShopRepresentatives.Add(cur.State)
    foundShopRepresentatives.Sort()
    let row = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    for x in foundShopRepresentatives do
        row.Children.Add(b(Graphics.BMPtoImage(MapStateProxy(x).DefaultInteriorBmp()))) |> ignore
    for _i = 0 to shopCount-foundShopRepresentatives.Count-1 do
        let icon = Graphics.BMPtoImage(Graphics.greyscale(MapStateProxy(TrackerModel.MapSquareChoiceDomainHelper.HINT_SHOP).DefaultInteriorBmp()))
        icon.Opacity <- OPA
        row.Children.Add(b icon) |> ignore
    sp.Children.Add(row) |> ignore

    sp.Margin <- Thickness(3.)
    new Border(Child=sp, BorderThickness=Thickness(5.), BorderBrush=Brushes.Gray, Background=Brushes.Black)

//////////////////////////////////////////////////

let MakeMappedHotKeysDisplay() =
    let mutable total = 0
    let makePanel(states, hkp:HotKeys.HotKeyProcessor<_>, mkIcon:_->FrameworkElement, iconW, header) =
        let panel = new WrapPanel(MaxHeight=800., Orientation=Orientation.Vertical)
        let bucket = ResizeArray()
        for state in states do
            if hkp.StateToKeys(state).Count > 0 then
                let keys = hkp.StateToKeys(state) |> Seq.fold (fun s c -> s + HotKeys.PrettyKey(c) + ",") "" |> (fun s -> s.Substring(0, s.Length-1))
                let icon = mkIcon(state)
                icon.Width <- float iconW
                let border = new Border(BorderBrush=Brushes.DimGray, BorderThickness=Thickness(1.), Child=icon)
                border.Margin <- Thickness(3.)
                let txt = DungeonRoomState.mkTxt(keys)
                txt.TextAlignment <- TextAlignment.Left
                let sp = new StackPanel(Orientation=Orientation.Horizontal)
                sp.Children.Add(border) |> ignore
                sp.Children.Add(txt) |> ignore
                bucket.Add(keys, sp)
                total <- total + 1
        //for _,sp in bucket |> Seq.sortBy fst do   // sort by alphabetical of key
        for _,sp in bucket do            // "logical" order
            panel.Children.Add(sp) |> ignore
        if panel.Children.Count > 0 then
            panel.Children.Insert(0, DungeonRoomState.mkTxt(header))
            panel.Margin <- Thickness(3., 3., 13., 3.)
        panel
    let bmpElseSize (w, h) bmp =
        let icon : FrameworkElement = if bmp = null then upcast new DockPanel(Width=float w, Height=float h) else upcast Graphics.BMPtoImage bmp
        icon
    let itemPanel = makePanel([-1..14], HotKeys.ItemHotKeyProcessor, (fun item -> CustomComboBoxes.boxCurrentBMP(item, false) |> bmpElseSize(21,21)), 21, "ITEMS")
    let overworldPanel = makePanel([-1..TrackerModel.dummyOverworldTiles.Length-1], HotKeys.OverworldHotKeyProcessor, (fun state ->
        MapStateProxy(state).DefaultInteriorBmp() |> bmpElseSize(15,27)), 15, "OVERWORLD")
    let blockerPanel = makePanel(TrackerModel.DungeonBlocker.All, HotKeys.BlockerHotKeyProcessor, (fun state -> 
        upcast Graphics.blockerCurrentBMP(state)), 24, "BLOCKERS")
    let thingies = [| 
        yield! DungeonRoomState.RoomType.All() |> Seq.map Choice1Of3
        yield! DungeonRoomState.MonsterDetail.All() |> Seq.map Choice2Of3
        yield! DungeonRoomState.FloorDropDetail.All() |> Seq.map Choice3Of3
        |]
    let dungeonRoomPanel = makePanel(thingies, HotKeys.DungeonRoomHotKeyProcessor, (fun c ->
        match c with 
        | Choice1Of3 rt -> upcast Graphics.BMPtoImage(rt.UncompletedBmp())
        | Choice2Of3 md -> (let i = md.Bmp() |> bmpElseSize(18,18) in (i.HorizontalAlignment <- HorizontalAlignment.Left; i))
        | Choice3Of3 fd -> (let i = fd.Bmp() |> bmpElseSize(18,18) in (i.HorizontalAlignment <- HorizontalAlignment.Right; i))
        ), 39, "DUNGEON")
    let globalPanel = makePanel(HotKeys.GlobalHotkeyTargets.All, HotKeys.GlobalHotKeyProcessor, (fun state -> state.AsHotKeyDisplay()), 30, "GLOBALS")
    let all = new StackPanel(Orientation=Orientation.Horizontal)
    all.Children.Add(itemPanel) |> ignore
    all.Children.Add(overworldPanel) |> ignore
    all.Children.Add(blockerPanel) |> ignore
    all.Children.Add(dungeonRoomPanel) |> ignore
    all.Children.Add(globalPanel) |> ignore
    if total = 0 then
        let tb = DungeonRoomState.mkTxt("You have no HotKeys mapped.\nYou can edit HotKeys.txt to add\nsome, to use the next time you\nrestart the app.")
        tb.FontSize <- 16.
        all.Children.Add(tb) |> ignore
        let fileToSelect = HotKeys.HotKeyFilename
        let args = sprintf "/Select, \"%s\"" fileToSelect
        let psi = new System.Diagnostics.ProcessStartInfo("Explorer.exe", args)
        System.Diagnostics.Process.Start(psi) |> ignore
    total=0, new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Background=Brushes.Black, Child=all)

