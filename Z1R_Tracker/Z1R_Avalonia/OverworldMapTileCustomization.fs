module OverworldMapTileCustomization

open Avalonia
open Avalonia.Controls
open Avalonia.Media

let OMTW = Graphics.OMTW

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

let overworldAcceleratorTable = new System.Collections.Generic.Dictionary<_,_>()
overworldAcceleratorTable.Add(TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY, (fun (appMainCanvas:Canvas,_c:Canvas,_isCurrentlyBook,i,j) -> async {
    let! shouldMarkTakeAnyAsComplete = PieMenus.TakeAnyPieMenuAsync(appMainCanvas, 572.)
    TrackerModel.setOverworldMapExtraData(i, j, if shouldMarkTakeAnyAsComplete then TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY else 0)
    }))
overworldAcceleratorTable.Add(TrackerModel.MapSquareChoiceDomainHelper.SWORD1, (fun (appMainCanvas:Canvas,_c:Canvas,_isCurrentlyBook,i,j) -> async {
    let! shouldMarkTakeAnyAsComplete = PieMenus.TakeThisPieMenuAsync(appMainCanvas, 572.)
    TrackerModel.setOverworldMapExtraData(i, j, if shouldMarkTakeAnyAsComplete then TrackerModel.MapSquareChoiceDomainHelper.SWORD1 else 0)
    }))
overworldAcceleratorTable.Add(TrackerModel.MapSquareChoiceDomainHelper.ARMOS, (fun (appMainCanvas,c:Canvas,isCurrentlyBook,_i,_j) -> async {
    let pos = c.TranslatePoint(Point(OMTW/2.-15.,1.), appMainCanvas).Value  // place to draw the local box
    let OW_ITEM_GRID_OFFSET_X,OW_ITEM_GRID_OFFSET_Y = 280.,60.  // copied brittle-y from elsewhere
    // in appMainCanvas coordinates:
    // armosBox position in main canvas
    let ax,ay = OW_ITEM_GRID_OFFSET_X+30., OW_ITEM_GRID_OFFSET_Y+30.
    // bottom middle of the box, as an arrow target
    let tx,ty = ax+15., ay+30.+3.   // +3 so arrowhead does not touch the target box
    // top middle of the box we are drawing on the map, as an arrow source
    let sx,sy = pos.X+15., pos.Y-3. // -3 so the line base does not touch the target box
    let line,triangle = Graphics.makeArrow(tx, ty, sx, sy, Brushes.Yellow)
    // rectangle for remote box highlight
    let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=Brushes.Yellow, StrokeThickness=3.)
    let gridX, gridY = -117., -3. 
    let decoX,decoY = -152., 108.
    let extraDecorations = [|
        CustomComboBoxes.itemBoxMouseButtonExplainerDecoration, decoX, decoY
        upcast line, -pos.X-3., -pos.Y-3.
        upcast triangle, -pos.X-3., -pos.Y-3.
        upcast rect, ax-pos.X-3., ay-pos.Y-3.
        |]
    let wh = new System.Threading.ManualResetEvent(false)
    CustomComboBoxes.DisplayRemoteItemComboBox(appMainCanvas, pos.X, pos.Y, TrackerModel.armosBox.CellCurrent(), 0, isCurrentlyBook, gridX, gridY, 
            (fun (newBoxCellValue, newPlayerHas) ->
                TrackerModel.armosBox.Set(newBoxCellValue, newPlayerHas)
                TrackerModel.forceUpdate()
                wh.Set() |> ignore
                ), (fun () -> wh.Set() |> ignore), extraDecorations)
    let! _ = Async.AwaitWaitHandle(wh)
    ()
    }))

let GetIconBMP(ms:MapStateProxy,i,j) =
    if ms.IsThreeItemShop && TrackerModel.getOverworldMapExtraData(i,j) <> 0 then
        let item1 = ms.State - TrackerModel.MapSquareChoiceDomainHelper.ARROW  // 0-based
        let item2 = TrackerModel.getOverworldMapExtraData(i,j) - 1   // 0-based
        // cons up a two-item shop image
        let tile = new System.Drawing.Bitmap(16*3,11*3)
        for px = 0 to 16*3-1 do
            for py = 0 to 11*3-1 do
                // two-icon area
                if px/3 >= 3 && px/3 <= 11 && py/3 >= 1 && py/3 <= 9 then
                    tile.SetPixel(px, py, Graphics.itemBackgroundColor)
                else
                    tile.SetPixel(px, py, Graphics.TRANS_BG)
                // icon 1
                if px/3 >= 4 && px/3 <= 6 && py/3 >= 2 && py/3 <= 8 then
                    let c = Graphics.itemsBMP.GetPixel(item1*3 + px/3-4, py/3-2)
                    if c.ToArgb() <> System.Drawing.Color.Black.ToArgb() then
                        tile.SetPixel(px, py, c)
                // icon 2
                if px/3 >= 8 && px/3 <= 10 && py/3 >= 2 && py/3 <= 8 then
                    let c = Graphics.itemsBMP.GetPixel(item2*3 + px/3-8, py/3-2)
                    if c.ToArgb() <> System.Drawing.Color.Black.ToArgb() then
                        tile.SetPixel(px, py, c)
        tile
    elif ms.State = TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY then
        if TrackerModel.getOverworldMapExtraData(i,j)=TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY then
            Graphics.theFullTileBmpTable.[ms.State].[1]
        else
            Graphics.theFullTileBmpTable.[ms.State].[0]
    elif ms.State = TrackerModel.MapSquareChoiceDomainHelper.SWORD1 then
        if TrackerModel.getOverworldMapExtraData(i,j)=TrackerModel.MapSquareChoiceDomainHelper.SWORD1 then
            Graphics.theFullTileBmpTable.[ms.State].[1]
        else
            Graphics.theFullTileBmpTable.[ms.State].[0]
    else
        ms.CurrentBMP()

let DoRightClick(msp:MapStateProxy,i,j) =  // returns tuple of two booleans (needRedrawGridSpot, needUIUpdate)
    if msp.State = -1 then
        // right click empty tile changes to 'X'
        TrackerModel.overworldMapMarks.[i,j].Prev() 
        true, true
    elif msp.IsThreeItemShop then
        // right click a shop cycles down the second item
        let MODULO = TrackerModel.MapSquareChoiceDomainHelper.NUM_ITEMS+1
        // next item
        let e = (TrackerModel.getOverworldMapExtraData(i,j) - 1 + MODULO) % MODULO
        // skip past duplicates
        let item1 = msp.State - TrackerModel.MapSquareChoiceDomainHelper.ARROW + 1  // 1-based
        let e = if e = item1 then (e - 1 + MODULO) % MODULO else e
        TrackerModel.setOverworldMapExtraData(i,j,e)
        true, false
    elif msp.State = TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY then
        // right click a take-any to toggle it 'used'
        let ex = TrackerModel.getOverworldMapExtraData(i,j)
        if ex=TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY then
            TrackerModel.setOverworldMapExtraData(i,j,0)
        else
            TrackerModel.setOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY)
        true, false
    elif msp.State = TrackerModel.MapSquareChoiceDomainHelper.SWORD1 then
        // right click the wood sword cave to toggle it 'used'
        let ex = TrackerModel.getOverworldMapExtraData(i,j)
        if ex=TrackerModel.MapSquareChoiceDomainHelper.SWORD1 then
            TrackerModel.setOverworldMapExtraData(i,j,0)
        else
            TrackerModel.setOverworldMapExtraData(i,j,TrackerModel.MapSquareChoiceDomainHelper.SWORD1)
        true, false
    else
        false, false

