module DungeonRoomStateUI

open DungeonRoomState

open System.Windows.Controls
open System.Windows.Media
open System.Windows

let dungeonRoomMouseButtonExplainerDecoration =
    let ST = CustomComboBoxes.borderThickness
    let h = 9.*3.*2.+ST*4.
    let d = new DockPanel(Height=h, LastChildFill=true, Background=Brushes.Black, Opacity=0.8)
    let mouseBMP = Graphics.mouseIconButtonColors2BMP
    let mouse = Graphics.BMPtoImage mouseBMP
    mouse.Height <- h
    mouse.Width <- float(mouseBMP.Width) * h / float(mouseBMP.Height)
    mouse.Stretch <- Stretch.Uniform
    let mouse = new Border(BorderThickness=Thickness(0.,0.,ST,0.), BorderBrush=Brushes.Gray, Child=mouse)
    d.Children.Add(mouse) |> ignore
    DockPanel.SetDock(mouse,Dock.Left)
    let sp = new StackPanel(Orientation=Orientation.Vertical, VerticalAlignment=VerticalAlignment.Bottom)
    d.Children.Add(sp) |> ignore
    let completed, uncompleted = new DungeonRoomState(), new DungeonRoomState()
    completed.RoomType <- RoomType.MaybePushBlock
    completed.IsComplete <- true
    uncompleted.RoomType <- RoomType.MaybePushBlock
    uncompleted.IsComplete <- false
    for color, text, pict in [Brushes.DarkMagenta,"Completed room",completed.CurrentDisplay(false); Brushes.DarkCyan,"Uncompleted room",uncompleted.CurrentDisplay(false)] do
        let p = new StackPanel(Orientation=Orientation.Horizontal, Margin=Thickness(ST))
        pict.Margin <- Thickness(ST,0.,2.*ST,0.)
        p.Children.Add(pict) |> ignore
        let tb = new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                                Text=text, VerticalAlignment=VerticalAlignment.Center, BorderThickness=Thickness(ST), BorderBrush=color)
        p.Children.Add(tb) |> ignore
        sp.Children.Add(p) |> ignore
    let b = new Border(Background=Brushes.Black, BorderThickness=Thickness(ST), BorderBrush=Brushes.DimGray, Child=d)
    b.MouseDown.Add(fun ea -> ea.Handled <- true)  // absorb mouse clicks, so that clicking explainer decoration does not dismiss popup due to being outside-the-area click
    b

let dungeonRoomMouseButtonExplainerDecoration2 =
    let ST = CustomComboBoxes.borderThickness
    let h = 9.*3.*2.+ST*4.
    let d = new DockPanel(Height=h, LastChildFill=true, Background=Brushes.Black, Opacity=0.8)
    let mouseBMP = Graphics.mouseIconButtonColors2BMP
    let mouse = Graphics.BMPtoImage mouseBMP
    mouse.Height <- h
    mouse.Width <- float(mouseBMP.Width) * h / float(mouseBMP.Height)
    mouse.Stretch <- Stretch.Uniform
    let mouse = new Border(BorderThickness=Thickness(0.,0.,ST,0.), BorderBrush=Brushes.Gray, Child=mouse)
    d.Children.Add(mouse) |> ignore
    DockPanel.SetDock(mouse,Dock.Left)
    let sp = new StackPanel(Orientation=Orientation.Vertical, VerticalAlignment=VerticalAlignment.Bottom)
    d.Children.Add(sp) |> ignore
    let completed, uncompleted = new DungeonRoomState(), new DungeonRoomState()
    completed.RoomType <- RoomType.MaybePushBlock
    completed.IsComplete <- true
    uncompleted.RoomType <- RoomType.MaybePushBlock
    uncompleted.IsComplete <- false
    for color, text, pict in [Brushes.DarkMagenta,"Done, take me back to the map",completed.CurrentDisplay(false); Brushes.DarkCyan,"I want to specify more details",uncompleted.CurrentDisplay(false)] do
        let p = new StackPanel(Orientation=Orientation.Horizontal, Margin=Thickness(ST))
        pict.Margin <- Thickness(ST,0.,2.*ST,0.)
        p.Children.Add(pict) |> ignore
        let tb = new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                                Text=text, VerticalAlignment=VerticalAlignment.Center, BorderThickness=Thickness(ST), BorderBrush=color)
        p.Children.Add(tb) |> ignore
        sp.Children.Add(p) |> ignore
    let b = new Border(Background=Brushes.Black, BorderThickness=Thickness(ST), BorderBrush=Brushes.DimGray, Child=d)
    b.MouseDown.Add(fun ea -> ea.Handled <- true)  // absorb mouse clicks, so that clicking explainer decoration does not dismiss popup due to being outside-the-area click
    b
                
let DoModalDungeonRoomSelectAndDecorate(cm:CustomComboBoxes.CanvasManager, originalRoomState:DungeonRoomState, usedTransports:_[], setNewValue, positionAtEntranceRoomIcons) = async {
    let tweak(im:Image) = im.Opacity <- 0.65; im
    let tileSunglasses = 0.75

    let popupCanvas = cm.CreatePopup(0.75)
    let workingCopy = originalRoomState.Clone()

    let makeCaption(txt, centered) = 
        let tb = new TextBox(Text=txt, FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, BorderThickness=Thickness(0.), IsReadOnly=true, IsHitTestVisible=false,
                                VerticalAlignment=VerticalAlignment.Center)
        if centered then
            tb.TextAlignment <- TextAlignment.Center
        tb
    let SCALE = 3.
    
    // FIRST choose a room type, accelerate into the popup selector
    let tileX,tileY = 535., 730.
    let brushes = (new CustomComboBoxes.ModalGridSelectBrushes(Brushes.Lime, Brushes.Lime, Brushes.Red, Brushes.Gray)).Dim(0.6)
    let ST = CustomComboBoxes.borderThickness
    let! allDone = async {
        let tileCanvas = new Canvas(Width=float(13*3)*SCALE, Height=float(9*3)*SCALE)
        let extra = 30.
        let tileSurroundingCanvas = new Canvas(Width=float(13*3)*SCALE+extra, Height=float(9*3)*SCALE+extra, Background=Brushes.DarkOliveGreen)
        canvasAdd(popupCanvas, tileSurroundingCanvas, tileX-extra/2., tileY-extra/2.)
        let grid = 
            [|
            RoomType.DoubleMoat; RoomType.CircleMoat; RoomType.LifeOrMoney; RoomType.BombUpgrade; RoomType.HungryGoriyaMeatBlock; RoomType.StartEnterFromE; RoomType.Gannon;
            RoomType.TopMoat; RoomType.Chevy; RoomType.OldManHint; RoomType.VChute; RoomType.HChute; RoomType.StartEnterFromN; RoomType.Zelda;
            RoomType.RightMoat; RoomType.Unmarked; RoomType.MaybePushBlock; RoomType.NonDescript; RoomType.Tee; RoomType.StartEnterFromS; RoomType.LavaMoat;
            RoomType.ItemBasement; RoomType.StaircaseToUnknown; RoomType.Transport1; RoomType.Transport2; RoomType.Transport3; RoomType.StartEnterFromW; RoomType.OffTheMap;
            RoomType.Transport4; RoomType.Transport5; RoomType.Transport6; RoomType.Transport7; RoomType.Transport8; RoomType.Turnstile; RoomType.Turnstile;
            |]
        let gridElementsSelectablesAndIDs : (FrameworkElement*_*_)[] = grid |> Array.mapi (fun i rt ->
            let isLegal = (rt = originalRoomState.RoomType) || (match rt.KnownTransportNumber with | None -> true | Some n -> usedTransports.[n]<>2)
            if i = grid.Length-1 then
                null, false, rt
            else
                upcast tweak(Graphics.BMPtoImage(rt.UncompletedBmp())), isLegal, rt
            )
        let originalStateIndex = match (grid |> Array.tryFindIndex (fun x -> x = originalRoomState.RoomType)) with Some x -> x | _ -> grid |> Array.findIndex (fun x -> x = RoomType.Unmarked)
        let activationDelta = 0
        let (gnc, gnr, gcw, grh) = 7, 5, 13*3, 9*3
        let totalGridWidth = float gnc*(float gcw + 2.*ST)
        let totalGridHeight = float gnr*(float grh + 2.*ST)
        let gx,gy = -96.,-75.-totalGridHeight
        let fullRoom = originalRoomState.Clone()  // a copy with the original decorations, used for redrawTile display
        fullRoom.IsComplete <- false
        let redrawTile(curState:RoomType) =
            Graphics.unparent(dungeonRoomMouseButtonExplainerDecoration2)
            tileCanvas.Children.Clear()
            fullRoom.RoomType <- curState
            let fullRoomDisplay = fullRoom.CurrentDisplay(false)
            fullRoomDisplay.RenderTransform <- new ScaleTransform(SCALE, SCALE)
            RenderOptions.SetBitmapScalingMode(fullRoomDisplay, BitmapScalingMode.NearestNeighbor)
            fullRoomDisplay.Opacity <- tileSunglasses
            canvasAdd(tileCanvas, fullRoomDisplay, 0., 0.)
            let textWidth = 328.
            let topText = Graphics.center(makeCaption("Select a room type", true), int textWidth, 24)
            let s = HotKeys.DungeonRoomHotKeyProcessor.AppendHotKeyToDescription(curState.DisplayDescription, Choice1Of3 curState)
            let bottomText = Graphics.center(makeCaption(s, true), int textWidth, 49)
            let dp = new DockPanel(Width=textWidth, Height=totalGridHeight+75., LastChildFill=false)
            DockPanel.SetDock(topText, Dock.Top)
            DockPanel.SetDock(bottomText, Dock.Bottom)
            dp.Children.Add(topText) |> ignore
            dp.Children.Add(bottomText) |> ignore
            let frame = new Border(BorderBrush=Brushes.DimGray, BorderThickness=Thickness(3.), Background=Brushes.Black, Child=dp)
            let sp = new StackPanel(Orientation=Orientation.Vertical)
            sp.Children.Add(dungeonRoomMouseButtonExplainerDecoration2) |> ignore
            sp.Children.Add(frame) |> ignore
            canvasAdd(tileCanvas, sp, gx+totalGridWidth/2.-textWidth/2., -97.-dp.Height)
        let onClick(ea:Input.MouseButtonEventArgs, curState) = 
            if (ea.ChangedButton = Input.MouseButton.Left || ea.ChangedButton = Input.MouseButton.Right) && ea.ButtonState = Input.MouseButtonState.Pressed then
                CustomComboBoxes.DismissPopupWithResult(curState, ea.ChangedButton = Input.MouseButton.Left)
            else
                CustomComboBoxes.StayPoppedUp
        let extraDecorations = [| |]
        let gridClickDismissalDoesMouseWarpBackToTileCenter = true
        if positionAtEntranceRoomIcons then
            // position mouse on entrance icons
            Graphics.WarpMouseCursorTo(Point(tileX+gx+5.5*(float gcw + ST*2.), tileY+gy+totalGridHeight/2.))
        else
            // position mouse on existing RoomType
            let x = originalStateIndex % gnc
            let y = originalStateIndex / gnc
            Graphics.WarpMouseCursorTo(Point(tileX+gx+(float x+0.5)*(float gcw + ST*2.), tileY+gy+(float y+0.5)*(float grh + ST*2.)))
        let! r = CustomComboBoxes.DoModalGridSelect(cm, tileX, tileY, tileCanvas, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (gnc, gnr, gcw, grh),
                                                    gx, gy, redrawTile, onClick, extraDecorations, brushes, gridClickDismissalDoesMouseWarpBackToTileCenter, None)
        workingCopy.IsComplete <- true
        popupCanvas.Children.Remove(tileSurroundingCanvas)
        match r with
        | None -> return true
        | Some(curState, allDone) -> 
            workingCopy.RoomType <- curState
            if allDone then
                setNewValue(workingCopy)
            return allDone
        }

    let makeBorderMouseOverBehavior(b:Border, otherEnterEffect, otherLeaveEffect) =
        b.MouseEnter.Add(fun _ -> b.BorderBrush <- Brushes.Lime; otherEnterEffect())
        b.MouseLeave.Add(fun _ -> b.BorderBrush <- Brushes.Gray; otherLeaveEffect())

    let cleanup() =
        popupCanvas.Children.Clear()  // un-parent the reusable dungeonRoomMouseButtonExplainerDecoration
        cm.DismissPopup()
    if allDone then
        cleanup()
    else
        // NEXT allow any number of optional decoration modifications, or let user left/right click the main tile to finish and choose IsComplete
        workingCopy.IsComplete <- false // to make the preview bright
        let snapBackWorkingCopy = workingCopy.Clone()  // state we may eventually commit
        let drmbed = dungeonRoomMouseButtonExplainerDecoration
        drmbed.BorderBrush <- Brushes.Gray
        let mre = new System.Threading.ManualResetEvent(false)
        let cleanupAndUnblock() = cleanup(); mre.Set() |> ignore
        let handlePossibleConfirmAndDismiss(ea:Input.MouseButtonEventArgs) =
            ea.Handled <- true
            if (ea.ChangedButton = Input.MouseButton.Left || ea.ChangedButton = Input.MouseButton.Right) && ea.ButtonState = Input.MouseButtonState.Pressed then
                snapBackWorkingCopy.IsComplete <- ea.ChangedButton = Input.MouseButton.Left
                if not(snapBackWorkingCopy.IsComplete) && snapBackWorkingCopy.RoomType.IsNotMarked then
                    snapBackWorkingCopy.RoomType <- RoomType.OffTheMap   // ad-hoc way to mark this RoomType, since it doesn't fit in the grid (Unmarked right-click in the detail menu)
                setNewValue(snapBackWorkingCopy)
                cleanupAndUnblock()
        let mutable workingCopyDisplay = null
        popupCanvas.MouseDown.Add(fun _ -> cleanupAndUnblock())
        let redrawWorkingCopy() =
            if workingCopyDisplay <> null then
                popupCanvas.Children.Remove(workingCopyDisplay)
            workingCopyDisplay <- new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(1.), Child=workingCopy.CurrentDisplay(false))
            workingCopyDisplay.LayoutTransform <- new ScaleTransform(SCALE, SCALE)
            RenderOptions.SetBitmapScalingMode(workingCopyDisplay, BitmapScalingMode.NearestNeighbor)
            workingCopyDisplay.IsHitTestVisible <- true
            makeBorderMouseOverBehavior(workingCopyDisplay, (fun () -> drmbed.BorderBrush <- Brushes.Lime), (fun () -> drmbed.BorderBrush <- Brushes.Gray))
            workingCopyDisplay.MouseDown.Add(fun ea -> handlePossibleConfirmAndDismiss(ea))
            Canvas.SetLeft(workingCopyDisplay, tileX)
            Canvas.SetTop(workingCopyDisplay, tileY)
            popupCanvas.Children.Insert(0, workingCopyDisplay)  // add to front, so draw under dashes and corner decos
        redrawWorkingCopy()

        let txt1 = "Optionally, modify corner decorations\n" +
                    "by clicking decorations here.\n" +
                    "Then click the tile below to finish,\n" +
                    "using the appropriate mouse button\n" +
                    "to mark whether the room has been completed"
        let cap = Graphics.center(makeCaption(txt1, true), 360, 120)
        cap.Opacity <- 0.9
        let explainer1 = new Border(BorderBrush=Brushes.DimGray, BorderThickness=Thickness(1.), Background=Brushes.Black, Child=cap)
        canvasAdd(popupCanvas, explainer1, 406., tileY-430.)
        Canvas.SetRight(drmbed, popupCanvas.Width - tileX + 10.)
        Canvas.SetBottom(drmbed, popupCanvas.Height - (tileY+(9.*3.+2.)*SCALE))
        popupCanvas.Children.Add(drmbed) |> ignore

        let monsterPanel = new StackPanel(Orientation=Orientation.Vertical)
        for m in MonsterDetail.All() do
            let icon = m.LegendIcon()
            let b = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Background=Brushes.Black, Child=icon)
            makeBorderMouseOverBehavior(b, (fun () -> 
                workingCopy.MonsterDetail <- m
                redrawWorkingCopy()), (fun () -> 
                workingCopy.MonsterDetail <- snapBackWorkingCopy.MonsterDetail
                redrawWorkingCopy()))
            b.MouseDown.Add(fun ea ->
                ea.Handled <- true
                snapBackWorkingCopy.MonsterDetail <- m
                workingCopy.MonsterDetail <- m
                redrawWorkingCopy()
                async {
                    monsterPanel.Opacity <- 0.5
                    monsterPanel.IsEnabled <- false
                    do! Async.Sleep(1500)
                    monsterPanel.IsEnabled <- true
                    monsterPanel.Opacity <- 1.0
                } |> Async.StartImmediate
                )
            monsterPanel.Children.Add(b) |> ignore
        let b = new Border(BorderThickness=Thickness(0.), Background=Brushes.Black, Child=monsterPanel)
        // just after the user clicking a selection, during the 'cooldown', don't want a click to cancel the whole popup; let it be accelerator for confirm & dismiss, like clicking preview tile
        b.MouseDown.Add(fun ea -> handlePossibleConfirmAndDismiss(ea))   
        Canvas.SetLeft(b, 406.)
        Canvas.SetBottom(b, popupCanvas.Height - tileY + 20.)
        popupCanvas.Children.Add(b) |> ignore

        let floorDropPanel = new StackPanel(Orientation=Orientation.Vertical)
        for f in FloorDropDetail.All() do
            let icon = f.LegendIcon()
            let b = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Background=Brushes.Black, Child=icon)
            makeBorderMouseOverBehavior(b, (fun () -> 
                workingCopy.FloorDropDetail <- f
                redrawWorkingCopy()), (fun () -> 
                workingCopy.FloorDropDetail <- snapBackWorkingCopy.FloorDropDetail
                redrawWorkingCopy()))
            b.MouseDown.Add(fun ea ->
                ea.Handled <- true
                snapBackWorkingCopy.FloorDropDetail <- f
                workingCopy.FloorDropDetail <- f
                redrawWorkingCopy()
                async {
                    floorDropPanel.Opacity <- 0.5
                    floorDropPanel.IsEnabled <- false
                    do! Async.Sleep(1500)
                    floorDropPanel.IsEnabled <- true
                    floorDropPanel.Opacity <- 1.0
                } |> Async.StartImmediate
                )
            floorDropPanel.Children.Add(b) |> ignore
        let b = new Border(BorderThickness=Thickness(0.), Background=Brushes.Black, Child=floorDropPanel)
        // just after the user clicking a selection, during the 'cooldown', don't want a click to cancel the whole popup; let it be accelerator for confirm & dismiss, like clicking preview tile
        b.MouseDown.Add(fun ea -> handlePossibleConfirmAndDismiss(ea))   
        Canvas.SetRight(b, 0.)
        Canvas.SetBottom(b, popupCanvas.Height - tileY + 20.)
        popupCanvas.Children.Add(b) |> ignore

        let! _ = Async.AwaitWaitHandle(mre)
        ()
    }