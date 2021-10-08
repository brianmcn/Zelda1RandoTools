module OverworldMapTileCustomization

open System.Windows.Controls
open System.Windows
open System.Windows.Media

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
    let pos = c.TranslatePoint(Point(OMTW/2.,float(11*3)/2.), cm.AppMainCanvas)  
    let! shouldMarkTakeAnyAsComplete = PieMenus.TakeAnyPieMenuAsync(cm, 666.)
    Graphics.WarpMouseCursorTo(pos)
    TrackerModel.setOverworldMapExtraData(i, j, if shouldMarkTakeAnyAsComplete then TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY else 0)
    }))
overworldAcceleratorTable.Add(TrackerModel.MapSquareChoiceDomainHelper.SWORD1, (fun (cm:CustomComboBoxes.CanvasManager,c:Canvas,i,j) -> async {
    let pos = c.TranslatePoint(Point(OMTW/2.,float(11*3)/2.), cm.AppMainCanvas)  
    let! shouldMarkTakeAnyAsComplete = PieMenus.TakeThisPieMenuAsync(cm, 666.)
    Graphics.WarpMouseCursorTo(pos)
    TrackerModel.setOverworldMapExtraData(i, j, if shouldMarkTakeAnyAsComplete then TrackerModel.MapSquareChoiceDomainHelper.SWORD1 else 0)
    }))
overworldAcceleratorTable.Add(TrackerModel.MapSquareChoiceDomainHelper.ARMOS, (fun (cm:CustomComboBoxes.CanvasManager,c:Canvas,_i,_j) -> async {
    let pos = c.TranslatePoint(Point(OMTW/2.-15.,1.), cm.AppMainCanvas)  // place to draw the local box
    do! DoRemoteItemComboBox(cm, 0, TrackerModel.armosBox, armosX, armosY, pos)
    }))
overworldAcceleratorTable.Add(TrackerModel.MapSquareChoiceDomainHelper.SWORD2, (fun (cm:CustomComboBoxes.CanvasManager,c:Canvas,_i,_j) -> async {
    let pos = c.TranslatePoint(Point(OMTW/2.-15.,1.), cm.AppMainCanvas)  // place to draw the local box
    do! DoRemoteItemComboBox(cm, 0, TrackerModel.sword2Box, sword2x, sword2y, pos)   // TODO
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

let DoRightClick(msp:MapStateProxy,i,j,_pos:Point,_popupIsActive:byref<bool>) =  // returns tuple of two booleans (needRedrawGridSpot, needUIUpdate)
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

(*
        let item1 = msp.State - TrackerModel.MapSquareChoiceDomainHelper.ARROW  // 0-based
        let item2 = TrackerModel.getOverworldMapExtraData(i,j) - 1   // 0-based
        let ST = CustomComboBoxes.borderThickness
        let tileImage = resizeMapTileImage <| Graphics.BMPtoImage(makeTwoItemShopBmp(item1,item2))
        let tileCanvas = new Canvas(Width=OMTW, Height=11.*3.)
        canvasAdd(tileCanvas, tileImage, 0., 0.)
        let originalState = item2
        let originalStateIndex = item2
        let gridxPosition = 
            if (displayIsCurrentlyMirrored && i>13) || (not displayIsCurrentlyMirrored && i<2) then 
                -ST // left align
            elif (displayIsCurrentlyMirrored && i<2) || (not displayIsCurrentlyMirrored && i>13) then 
                OMTW - float(8*(5*3+2*int ST)+int ST)  // right align
            else
                (OMTW - float(8*(5*3+2*int ST)+int ST))/2.  // center align
        let gridElementsSelectablesAndIDs : (FrameworkElement*bool*int)[] = Array.init 8 (fun n ->
            let i = n + TrackerModel.MapSquareChoiceDomainHelper.ARROW
            upcast Graphics.BMPtoImage(MapStateProxy(i).CurrentInteriorBMP()), true, n
            )
        CustomComboBoxes.DoModalGridSelect(cm, pos.X, pos.Y, tileCanvas,
            gridElementsSelectablesAndIDs, originalStateIndex, 0, (8, 1, 5*3, 9*3), gridxPosition, 11.*3.+ST,
            (fun (currentState) -> 
                tileCanvas.Children.Clear()
                let tileImage = resizeMapTileImage <| Graphics.BMPtoImage(makeTwoItemShopBmp(item1,currentState))
                canvasAdd(tileCanvas, tileImage, 0., 0.)
                ),
            (fun (dismissPopup, _ea, currentState) ->
                TrackerModel.setOverworldMapExtraData(i,j,currentState+1)  // extraData is 1-based
                async {
                    match overworldAcceleratorTable.TryGetValue(currentState) with
                    | (true,f) -> do! f(cm,c,i,j)
                    | _ -> ()
                    redrawGridSpot()
                    dismissPopup()
                    popupIsActive <- false
                    if originalState = -1 && currentState <> -1 then doUIUpdate()  // immediate update to dismiss green/yellow highlight from current tile
                    } |> Async.StartImmediate
                ),
            (fun () -> popupIsActive <- false),
            [], CustomComboBoxes.ModalGridSelectBrushes.Defaults(), true)


        let nr,nu =
            pIA <- true
            let! r = doStuff()
            pIA <- false
            match r with
            | alt1 -> a,b
            | altn -> c,d

        let nr,nu = DMGS(fun k -> ... k(nr,nu), fun k -> ... k(nr,nu))   // k called at most once, must be tail call
        if nr then r()
        if nu then u()


        tileCanvas.MouseDown.Add(fun ea -> 
            ea.Handled <- true
            onClick(dismiss, ea, stateID())
            )
        ...
        dismissDoModalPopup <- DoModal(cm, tileX, tileY, popupCanvas, (fun () -> onClose(); selfCleanup()))


        tileCanvas.MouseDown.Add(fun ea -> 
            ea.Handled <- true
            let shouldDismiss = onClick(ea, stateID())   // have effect if dismissing
            if shouldDismiss then
                wh.Set()
            )
        ...
        do! DoModalAsync(wh, cm, tileX, tileY, popupCanvas)   // sets wh and returns if user clicks outside
        onClose() 
        selfCleanup()



        CustomComboBoxes.DoModalGridSelect(blahblahblah,
            (fun (dismissPopup, _ea, currentState) ->
                TrackerModel.setOverworldMapExtraData(i,j,currentState+1)  // extraData is 1-based
                async {
                    match overworldAcceleratorTable.TryGetValue(currentState) with
                    | (true,f) -> do! f(cm,c,i,j)
                    | _ -> ()
                    redrawGridSpot()
                    dismissPopup()
                    popupIsActive <- false
                    if originalState = -1 && currentState <> -1 then doUIUpdate()  // immediate update to dismiss green/yellow highlight from current tile
                    } |> Async.StartImmediate
                ),
            (fun () -> popupIsActive <- false), blahblah)


        do! CustomComboBoxes.DoModalGridSelectAsync(blahblahblah,
            (fun (_ea, currentState) ->
                TrackerModel.setOverworldMapExtraData(i,j,currentState+1)  // extraData is 1-based
                async {
                    match overworldAcceleratorTable.TryGetValue(currentState) with
                    | (true,f) -> do! f(cm,c,i,j)
                    | _ -> ()
                    needsRedraw <- true
                    if originalState = -1 && currentState <> -1 then needsUIUpdate <- true
                    return true    // shouldDismiss
                    } |> Async.StartImmediate
                ),
            blahblah)
        popupIsActive <- false


        let! needRedraw,needsUIUpdate = CustomComboBoxes.DoModalGridSelectAsync(blahblahblah,
            (fun (_ea, currentState) ->
                TrackerModel.setOverworldMapExtraData(i,j,currentState+1)  // extraData is 1-based
                async {
                    match overworldAcceleratorTable.TryGetValue(currentState) with
                    | (true,f) -> do! f(cm,c,i,j)
                    | _ -> ()
                    return Some(true, originalState = -1 && currentState <> -1)   // Some(r) means dismiss and return this; None means keep the UI up
                    } |> Async.StartImmediate
                ),
            blahblah)
        popupIsActive <- false

        let mutable r = None
        tileCanvas.MouseDown.Add(fun ea -> 
            ea.Handled <- true
            match onClick(ea, stateID()) with
            | Some(result) -> r <- Some(result); wh.Set()
            | None -> ()
            )
        ...
        do! DoModalAsync(wh, cm, tileX, tileY, popupCanvas)   // sets wh and returns if user clicks outside
        onClose() 
        selfCleanup()
        r

        UIevent.Add(fun _ -> Async.StartImmediate <| async {
        let pos = c.TranslatePoint(Point(), appMainCanvas)
        let msp = MapStateProxy(TrackerModel.overworldMapMarks.[i,j].Current())
        let! needRedraw, needUIUpdate = DoRightClickAsync(msp,i,j,pos)
        if needRedraw then redrawGridSpot()
        if needUIUpdate then doUIUpdate()  // immediate update to dismiss green/yellow highlight from current tile
        popupIsActive <- false
        })



        let onClick(dismiss, _ea, i) =
            // update model
            TrackerModel.SetLevelHint(thisRow, TrackerModel.HintZone.FromIndex(i))
            TrackerModel.forceUpdate()
            // update view
            if i = 0 then
                b.Background <- Brushes.Black
            else
                b.Background <- Views.hintHighlightBrush
            button.Content <- mkTxt(TrackerModel.HintZone.FromIndex(i).ToString())
            // cleanup
            dismiss()
            popupIsActive <- false
        let onClose() = popupIsActive <- false
        let extraDecorations = []
        let brushes = CustomComboBoxes.ModalGridSelectBrushes.Defaults()
        let gridClickDismissalDoesMouseWarpBackToTileCenter = false
        CustomComboBoxes.DoModalGridSelect(cm, tileX, tileY, tileCanvas, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (gnc, gnr, gcw, grh),
                                            gx, gy, redrawTile, onClick, onClose, extraDecorations, brushes, gridClickDismissalDoesMouseWarpBackToTileCenter)
        
        =>
        
        // maybe not RQA
        type PopupClickBehavior<'a> =
            | DismissPopupWithResult of 'a     // return result
            | DismissPopupWithNoResult   // tear down popup as though user clicked outside it
            | StayPoppedUp          // keep awaiting more clicks

        //let onClick(_ea, i) = COMMIT(i)  // Some(i)     IGNORE() or KEEPGOING() would be None    Hmm, but what if caller wants to say 'this click means 'call dismissal handle with no result'
        //let onClick(_ea, i) = COMMIT(i)  // Some(Some(i))    IGNORE() or KEEPGOING() would be None    dismiss with no result would be Some(None)
        let onClick(_ea, i) = DismissPopupWithResult(i)
        let extraDecorations = []
        let brushes = CustomComboBoxes.ModalGridSelectBrushes.Defaults()
        let gridClickDismissalDoesMouseWarpBackToTileCenter = false
        let! iopt = CustomComboBoxes.DoModalGridSelectAsync(cm, tileX, tileY, tileCanvas, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (gnc, gnr, gcw, grh),
                                            gx, gy, redrawTile, onClick, extraDecorations, brushes, gridClickDismissalDoesMouseWarpBackToTileCenter)
        match iopt with
        | Some i ->
            // update model
            TrackerModel.SetLevelHint(thisRow, TrackerModel.HintZone.FromIndex(i))
            TrackerModel.forceUpdate()
            // update view
            if i = 0 then
                b.Background <- Brushes.Black
            else
                b.Background <- Views.hintHighlightBrush
            button.Content <- mkTxt(TrackerModel.HintZone.FromIndex(i).ToString())
        | None -> () // user clicked outside popup, or the popup onClick() behavior decided to end it with no result
        // cleanup
        popupIsActive <- false

        
        
        aside, can imagine shop interface accelerator where tileCanvas has 'select first item' above it with two blank spots, and then like
        let onClick(_ea, i) = 
            if item1 = -1 then
                item1 <- i
                textbox.Text <- 'select second item'
                redrawTileCanvas()
                StayPoppedUp
            else 
                item2 <- i           
                DismissPopupWithResult(item1, item2)


*)

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

