module OverworldMapTileCustomization

open Avalonia
open Avalonia.Controls
open Avalonia.Media

let OMTW = Graphics.OMTW
let canvasAdd = Graphics.canvasAdd

type MapStateProxy(state) =
    static member NumStates = Graphics.theInteriorBmpTable.Length
    member this.State = state
    member this.IsX = state=MapStateProxy.NumStates-1
    member this.IsDungeon = state >= 0 && state < 9
    member this.IsWarp = state >= 9 && state < 13
    member this.IsThreeItemShop = TrackerModel.MapSquareChoiceDomainHelper.IsItem(state)
    member this.IsInteresting = not(state = -1 || this.IsX)
    member this.CurrentBMP() =
        if state = -1 then
            null
        elif this.IsDungeon then
            if TrackerModel.IsHiddenDungeonNumbers() then 
                if TrackerModel.GetDungeon(state).PlayerHasTriforce() && TrackerModel.playerComputedStateSummary.HaveRecorder then
                    Graphics.theFullTileBmpTable.[state].[3] 
                else
                    Graphics.theFullTileBmpTable.[state].[2] 
            else 
                if TrackerModel.GetDungeon(state).PlayerHasTriforce() && TrackerModel.playerComputedStateSummary.HaveRecorder then
                    Graphics.theFullTileBmpTable.[state].[1]
                else
                    Graphics.theFullTileBmpTable.[state].[0]
        else
            Graphics.theFullTileBmpTable.[state].[0]
    member this.CurrentInteriorBMP() =  // so that the grid popup is unchanging, always choose same representative (e.g. yellow dungeon)
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
    let extraDecorations : (Control*float*float)[] = [|
        upcast line, -pos.X-3., -pos.Y-3.
        upcast triangle, -pos.X-3., -pos.Y-3.
        upcast rect, topX-pos.X-3., topY-pos.Y-3.
        |]
    extraDecorations

let armosX, armosY, sword2x,sword2y = // armos/sword2 box position in main canvas
    let OW_ITEM_GRID_OFFSET_X,OW_ITEM_GRID_OFFSET_Y = 280.,60.  // copied brittle-y from elsewhere
    OW_ITEM_GRID_OFFSET_X+30., OW_ITEM_GRID_OFFSET_Y+30., OW_ITEM_GRID_OFFSET_X+30., OW_ITEM_GRID_OFFSET_Y+60.

let DoRemoteItemComboBox(cm:CustomComboBoxes.CanvasManager, activationDelta, trackerModelBoxToUpdate:TrackerModel.Box,
                            topX,topY,pos:Point) = async {  // topX,topY,pos are relative to appMainCanvas; top is for tracker box, pos is for mouse-local box
    let extraDecorations = computeExtraDecorationArrow(topX, topY, pos)
    let! r = CustomComboBoxes.DisplayItemComboBox(cm, pos.X, pos.Y, trackerModelBoxToUpdate.CellCurrent(), activationDelta, extraDecorations)
    match r with
    | Some(newBoxCellValue, newPlayerHas) ->
        trackerModelBoxToUpdate.Set(newBoxCellValue, newPlayerHas)
        TrackerModel.forceUpdate()
    | None -> ()
    }

let overworldAcceleratorTable = new System.Collections.Generic.Dictionary<_,_>()
overworldAcceleratorTable.Add(TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY, (fun (cm:CustomComboBoxes.CanvasManager,c:Canvas,i,j) -> async {
    let pos = c.TranslatePoint(Point(OMTW/2.,float(11*3)/2.), cm.AppMainCanvas).Value
    let! shouldMarkTakeAnyAsComplete = PieMenus.TakeAnyPieMenuAsync(cm, 572.)
    Graphics.WarpMouseCursorTo(pos)
    TrackerModel.setOverworldMapExtraData(i, j, if shouldMarkTakeAnyAsComplete then TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY else 0)
    }))
overworldAcceleratorTable.Add(TrackerModel.MapSquareChoiceDomainHelper.SWORD1, (fun (cm:CustomComboBoxes.CanvasManager,c:Canvas,i,j) -> async {
    let pos = c.TranslatePoint(Point(OMTW/2.,float(11*3)/2.), cm.AppMainCanvas).Value
    let! shouldMarkTakeAnyAsComplete = PieMenus.TakeThisPieMenuAsync(cm, 572.)
    Graphics.WarpMouseCursorTo(pos)
    TrackerModel.setOverworldMapExtraData(i, j, if shouldMarkTakeAnyAsComplete then TrackerModel.MapSquareChoiceDomainHelper.SWORD1 else 0)
    }))
overworldAcceleratorTable.Add(TrackerModel.MapSquareChoiceDomainHelper.ARMOS, (fun (cm:CustomComboBoxes.CanvasManager,c:Canvas,_i,_j) -> async {
    let pos = c.TranslatePoint(Point(OMTW/2.-15.,1.), cm.AppMainCanvas).Value  // place to draw the local box
    do! DoRemoteItemComboBox(cm, 0, TrackerModel.armosBox, armosX, armosY, pos)
    }))
overworldAcceleratorTable.Add(TrackerModel.MapSquareChoiceDomainHelper.SWORD2, (fun (cm:CustomComboBoxes.CanvasManager,c:Canvas,_i,_j) -> async {
    let pos = c.TranslatePoint(Point(OMTW/2.-15.,1.), cm.AppMainCanvas).Value  // place to draw the local box
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
        MapStateProxy(state).CurrentBMP()
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

let GetIconBMPAndExtraDecorations(cm,ms:MapStateProxy,i,j) =
    if ms.IsThreeItemShop && TrackerModel.getOverworldMapExtraData(i,j) <> 0 then
        let item1 = ms.State - TrackerModel.MapSquareChoiceDomainHelper.ARROW  // 0-based
        let item2 = TrackerModel.getOverworldMapExtraData(i,j) - 1   // 0-based
        let tile = makeTwoItemShopBmp(item1, item2)
        tile, []
    elif ms.State = TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY then
        if TrackerModel.getOverworldMapExtraData(i,j)=TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY then
            Graphics.theFullTileBmpTable.[ms.State].[1], []
        else
            Graphics.theFullTileBmpTable.[ms.State].[0], []
    elif ms.State = TrackerModel.MapSquareChoiceDomainHelper.SWORD1 then
        if TrackerModel.getOverworldMapExtraData(i,j)=TrackerModel.MapSquareChoiceDomainHelper.SWORD1 then
            Graphics.theFullTileBmpTable.[ms.State].[1], []
        else
            Graphics.theFullTileBmpTable.[ms.State].[0], []
    elif ms.State = TrackerModel.MapSquareChoiceDomainHelper.SWORD2 then
        if TrackerModel.sword2Box.PlayerHas() = TrackerModel.PlayerHas.NO then
            let extraDecorationsF(boxPos:Point) =
                let extraDecorations = computeExtraDecorationArrow(sword2x, sword2y, boxPos)
                extraDecorations
            sword2LeftSideFullTileBmp, [Views.MakeBoxItemWithExtraDecorations(cm, TrackerModel.sword2Box, false, extraDecorationsF), OMTW-30., 1.]
        else
            Graphics.theFullTileBmpTable.[ms.State].[0], []
    else
        ms.CurrentBMP(), []

let DoRightClick(cm,msp:MapStateProxy,i,j,pos:Point,popupIsActive:ref<bool>) = async {  // returns tuple of two booleans (needRedrawGridSpot, needUIUpdate)
    if msp.State = -1 then
        // right click empty tile changes to 'X'
        TrackerModel.overworldMapMarks.[i,j].Prev() 
        return true, true
    elif msp.IsThreeItemShop then
        popupIsActive := true
        let item1 = msp.State - TrackerModel.MapSquareChoiceDomainHelper.ARROW  // 0-based
        let item2 = TrackerModel.getOverworldMapExtraData(i,j) - 1   // 0-based
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
        let gridElementsSelectablesAndIDs : (Control*bool*int)[] = Array.init 8 (fun n ->
            let i = n + TrackerModel.MapSquareChoiceDomainHelper.ARROW
            upcast Graphics.BMPtoImage(MapStateProxy(i).CurrentInteriorBMP()), true, n
            )
        let! g = CustomComboBoxes.DoModalGridSelect(cm, pos.X, pos.Y, tileCanvas,
                        gridElementsSelectablesAndIDs, originalStateIndex, 0, (8, 1, 5*3, 9*3), gridxPosition, 11.*3.+ST,
                        (fun (currentState) -> 
                            tileCanvas.Children.Clear()
                            let tileImage = resizeMapTileImage <| Graphics.BMPtoImage(makeTwoItemShopBmp(item1,currentState))
                            canvasAdd(tileCanvas, tileImage, 0., 0.)
                            ),
                        (fun (_ea, currentState) -> CustomComboBoxes.DismissPopupWithResult(currentState)),
                        [], CustomComboBoxes.ModalGridSelectBrushes.Defaults(), true)
        let r =
            match g with
            | Some(currentState) ->
                TrackerModel.setOverworldMapExtraData(i,j,currentState+1)  // extraData is 1-based
                true, (originalState = -1 && currentState <> -1)
            | None -> false, false
        popupIsActive := false
        return r
    elif msp.State = TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY then
        // right click a take-any to toggle it 'used'
        let ex = TrackerModel.getOverworldMapExtraData(i,j)
        if ex=TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY then
            TrackerModel.setOverworldMapExtraData(i,j,0)
        else
            TrackerModel.setOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY)
        return true, false
    elif msp.State = TrackerModel.MapSquareChoiceDomainHelper.SWORD1 then
        // right click the wood sword cave to toggle it 'used'
        let ex = TrackerModel.getOverworldMapExtraData(i,j)
        if ex=TrackerModel.MapSquareChoiceDomainHelper.SWORD1 then
            TrackerModel.setOverworldMapExtraData(i,j,0)
        else
            TrackerModel.setOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SWORD1)
        return true, false
    else
        return false, false
    }

