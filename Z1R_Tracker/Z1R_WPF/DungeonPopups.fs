module DungeonPopups

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

open CustomComboBoxes
open DungeonRoomState
let canvasAdd = Graphics.canvasAdd

let sunglassesOpacity = 0.85

let mutable THE_DIFF = 0.   // a kludge, until/unless I come up with a better way to thread the layout thru global popup logic

let dungeonRoomExplainer, setOpacity =
    let mkTxt(txt) = new TextBox(Text=txt, Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                                    BorderThickness=Thickness(0.), FontSize=16., VerticalAlignment=VerticalAlignment.Center)
    let border(fe) = new Border(Child=fe, BorderThickness=Thickness(1.), BorderBrush=Brushes.Gray)
    let marginAndSunglasses(c:FrameworkElement) = c.Margin <- Thickness(6.,0.,0.,0.); c.Opacity <- sunglassesOpacity; c
    let left(tb:TextBox) = 
        tb.HorizontalContentAlignment <- HorizontalAlignment.Left
        tb.HorizontalAlignment <- HorizontalAlignment.Left
        tb.Margin <- Thickness(3.,0.,0.,0.)
        tb
    let right(tb:TextBox) = 
        tb.HorizontalContentAlignment <- HorizontalAlignment.Right
        tb.HorizontalAlignment <- HorizontalAlignment.Right
        tb.Margin <- Thickness(0.,0.,3.,0.)
        tb
    let bg = Graphics.iconExtras_bmp.GetPixel(12,12)
    let ellipsis = Graphics.transformColor(Graphics.iconExtras_bmp, fun c -> if c.ToArgb()=bg.ToArgb() then System.Drawing.Color.Black else c)

    let dp = new DockPanel()
    let header = mkTxt("UI reminder: When mousing a room on the dungeon map...")
    header.HorizontalContentAlignment <- HorizontalAlignment.Center
    DockPanel.SetDock(header, Dock.Top)
    dp.Children.Add(header) |> ignore
    let g = makeGrid(3,4,250,33)
    g.ColumnDefinitions.[0].Width <- GridLength(218.)
    g.ColumnDefinitions.[1].Width <- GridLength(378.)
    g.ColumnDefinitions.[2].Width <- GridLength(172.)

    let tb = right <| mkTxt("Left-click")
    gridAdd(g, border(tb), 0, 0)
    let tb = left <| mkTxt("Toggle room brightness (or rotate lobby arrow)")
    gridAdd(g, border(tb), 1, 0)
    let sp = new StackPanel(Orientation=Orientation.Horizontal)
    let drs = new DungeonRoomState()
    drs.RoomType <- RoomType.MaybePushBlock
    drs.IsComplete <- true
    sp.Children.Add(drs.CurrentDisplay()|>marginAndSunglasses) |> ignore
    let drs = new DungeonRoomState()
    drs.RoomType <- RoomType.MaybePushBlock
    drs.IsComplete <- false
    sp.Children.Add(drs.CurrentDisplay()|>marginAndSunglasses) |> ignore
    gridAdd(g, border(sp), 2, 0)

    let tb = right <| mkTxt("Right-click")
    gridAdd(g, border(tb), 0, 1)
    let tb = left <| mkTxt("Invoke the 'Select a Room Type' popup menu")
    gridAdd(g, border(tb), 1, 1)
    let sp = new StackPanel(Orientation=Orientation.Horizontal)
    let drs = new DungeonRoomState()
    drs.RoomType <- RoomType.MaybePushBlock
    drs.IsComplete <- false
    sp.Children.Add(drs.CurrentDisplay()|>marginAndSunglasses) |> ignore
    let drs = new DungeonRoomState()
    drs.RoomType <- RoomType.Chevy
    drs.IsComplete <- false
    sp.Children.Add(drs.CurrentDisplay()|>marginAndSunglasses) |> ignore
    let drs = new DungeonRoomState()
    drs.RoomType <- RoomType.Transport1
    drs.IsComplete <- false
    sp.Children.Add(drs.CurrentDisplay()|>marginAndSunglasses) |> ignore
    sp.Children.Add(ellipsis |> Graphics.BMPtoImage) |> ignore
    gridAdd(g, border(sp), 2, 1)

    let tb = right <| mkTxt("Scroll-up/shift-left-click")
    gridAdd(g, border(tb), 0, 2)
    let tb = left <| mkTxt("Invoke the 'Select a Monster Detail' popup menu")
    gridAdd(g, border(tb), 1, 2)
    let sp = new StackPanel(Orientation=Orientation.Horizontal)
    let drs = new DungeonRoomState()
    drs.RoomType <- RoomType.MaybePushBlock
    drs.IsComplete <- false
    sp.Children.Add(drs.CurrentDisplay()|>marginAndSunglasses) |> ignore
    let drs = new DungeonRoomState()
    drs.RoomType <- RoomType.MaybePushBlock
    drs.IsComplete <- false
    drs.MonsterDetail <- MonsterDetail.Gleeok
    sp.Children.Add(drs.CurrentDisplay()|>marginAndSunglasses) |> ignore
    let drs = new DungeonRoomState()
    drs.RoomType <- RoomType.MaybePushBlock
    drs.IsComplete <- false
    drs.MonsterDetail <- MonsterDetail.Digdogger
    sp.Children.Add(drs.CurrentDisplay()|>marginAndSunglasses) |> ignore
    sp.Children.Add(ellipsis |> Graphics.BMPtoImage) |> ignore
    gridAdd(g, border(sp), 2, 2)

    let tb = right <| mkTxt("Scroll-down/shift-right-click")
    gridAdd(g, border(tb), 0, 3)
    let tb = left <| mkTxt("Invoke the 'Select a Floor Drop Detail' popup menu")
    gridAdd(g, border(tb), 1, 3)
    let sp = new StackPanel(Orientation=Orientation.Horizontal)
    let drs = new DungeonRoomState()
    drs.RoomType <- RoomType.MaybePushBlock
    drs.IsComplete <- false
    sp.Children.Add(drs.CurrentDisplay()|>marginAndSunglasses) |> ignore
    let drs = new DungeonRoomState()
    drs.RoomType <- RoomType.MaybePushBlock
    drs.IsComplete <- false
    drs.FloorDropDetail <- FloorDropDetail.Triforce
    sp.Children.Add(drs.CurrentDisplay()|>marginAndSunglasses) |> ignore
    let drs = new DungeonRoomState()
    drs.RoomType <- RoomType.MaybePushBlock
    drs.IsComplete <- false
    drs.FloorDropDetail <- FloorDropDetail.BombPack
    sp.Children.Add(drs.CurrentDisplay()|>marginAndSunglasses) |> ignore
    sp.Children.Add(ellipsis |> Graphics.BMPtoImage) |> ignore
    gridAdd(g, border(sp), 2, 3)

    dp.Children.Add(g) |> ignore
    (new Border(Child=dp, BorderThickness=Thickness(0.,3.,0.,3.), BorderBrush=Brushes.DimGray, Background=Brushes.Black) :> FrameworkElement),
        fun op -> dp.Opacity <- op

let DoMonsterDetailPopup(cm:CanvasManager, boxX, boxY, currentMonsterDetail) = async {
    let innerc = new Canvas(Width=18., Height=18., Background=Brushes.Black)  // just has monster icon drawn on it, not the box
    Graphics.SilentlyWarpMouseCursorTo(Point(boxX+12., boxY+12.))
    let redraw(md:MonsterDetail) =
        innerc.Children.Clear()
        let bmp = md.Bmp()
        if bmp <> null then
            let image = Graphics.BMPtoImage(bmp)
            image.Opacity <- sunglassesOpacity
            canvasAdd(innerc, image, 0., 0.)
        innerc
    redraw(currentMonsterDetail) |> ignore
    let all = MonsterDetail.All()
    let gridElementsSelectablesAndIDs = [|
        for n = 0 to 31 do
            let fe:FrameworkElement = if n>=all.Length then null elif all.[n].IsNotMarked then upcast new Canvas() else upcast (all.[n].Bmp() |> Graphics.BMPtoImage)
            let isSelectable = n < all.Length
            let ident = if n>=all.Length then MonsterDetail.Unmarked else all.[n]
            yield fe, isSelectable, ident
        |]
    let originalStateIndex = Array.IndexOf(all, currentMonsterDetail)
    let onClick(_ea,ident) =
        // we're getting a click with mouse event args ea on one of the selectable items in the grid, namely ident. take appropriate action.
        DismissPopupWithResult(ident)
    let gridX, gridY = 21., -3.
    let redrawTile(ident) =
        // the user has changed the current selection via mousing or scrolling, redraw the preview tile appropriately to display ident
        let innerc = redraw(ident)
        let s = ident.DisplayDescription
        let s = HotKeys.DungeonRoomHotKeyProcessor.AppendHotKeyToDescription(s, Choice2Of4 ident)
        let text = new TextBox(Text=s, Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                                    FontSize=16., HorizontalContentAlignment=HorizontalAlignment.Center)
        let textBorder = new Border(BorderThickness=Thickness(3.), Child=text, Background=Brushes.Black, BorderBrush=Brushes.Gray)
        let dp = new DockPanel(LastChildFill=false)
        dp.Children.Add(textBorder) |> ignore
        innerc.Children.Add(dp) |> ignore
        DockPanel.SetDock(textBorder, Dock.Right)
        Canvas.SetTop(dp, -3.)
        Canvas.SetLeft(dp, 217.)
    let extraDecorations = 
        let text = new TextBox(Text="Select a Monster Detail for this room", Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                                    BorderThickness=Thickness(0.), FontSize=16., HorizontalContentAlignment=HorizontalAlignment.Center)
        let textBorder = new Border(BorderThickness=Thickness(3.), Child=text, Background=Brushes.Black, BorderBrush=Brushes.Gray)
        [
            textBorder :> FrameworkElement, -4., -30.
            dungeonRoomExplainer, -3.-boxX, 342.-boxY-THE_DIFF
        ]
    setOpacity(0.8)
    return! DoModalGridSelect(cm, boxX+3., boxY+3., innerc, gridElementsSelectablesAndIDs, originalStateIndex, 0, (8, 4, 18, 18), 9., 9., gridX, gridY, 
                                redrawTile, onClick, extraDecorations, itemBoxModalGridSelectBrushes, WarpToCenter, None, "MonsterDetail", None)
    }

let DoFloorDropDetailPopup(cm:CanvasManager, boxX, boxY, currentFloorDropDetail) = async {
    let innerc = new Canvas(Width=18., Height=18., Background=Brushes.Black)  // just has floordrop icon drawn on it, not the box
    Graphics.SilentlyWarpMouseCursorTo(Point(boxX+9., boxY+9.))
    let redraw(fd:FloorDropDetail) =
        innerc.Children.Clear()
        let bmp = fd.Bmp()
        if bmp <> null then
            let image = Graphics.BMPtoImage(bmp)
            image.Opacity <- sunglassesOpacity
            canvasAdd(innerc, image, 0., 0.)
        innerc
    redraw(currentFloorDropDetail) |> ignore
    let all = FloorDropDetail.All()
    let gridElementsSelectablesAndIDs = [|
        for n = 0 to 8 do
            let fe:FrameworkElement = if n>=all.Length then null elif all.[n].IsNotMarked then upcast new Canvas() else upcast (all.[n].Bmp() |> Graphics.BMPtoImage)
            let isSelectable = n < all.Length
            let ident = if n>=all.Length then FloorDropDetail.Unmarked else all.[n]
            yield fe, isSelectable, ident
        |]
    let originalStateIndex = Array.IndexOf(all, currentFloorDropDetail)
    let onClick(_ea,ident) =
        // we're getting a click with mouse event args ea on one of the selectable items in the grid, namely ident. take appropriate action.
        DismissPopupWithResult(ident)
    let gridX, gridY = 21., -3.
    let redrawTile(ident) =
        // the user has changed the current selection via mousing or scrolling, redraw the preview tile appropriately to display ident
        let innerc = redraw(ident)
        let s = ident.DisplayDescription
        let s = HotKeys.DungeonRoomHotKeyProcessor.AppendHotKeyToDescription(s, Choice3Of4 ident)
        let text = new TextBox(Text=s, Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                                    FontSize=16., HorizontalContentAlignment=HorizontalAlignment.Center)
        let textBorder = new Border(BorderThickness=Thickness(3.), Child=text, Background=Brushes.Black, BorderBrush=Brushes.Gray)
        let dp = new DockPanel(LastChildFill=false)
        dp.Children.Add(textBorder) |> ignore
        innerc.Children.Add(dp) |> ignore
        DockPanel.SetDock(textBorder, Dock.Right)
        Canvas.SetTop(dp, -3.)
        Canvas.SetLeft(dp, 99.)
    let extraDecorations = 
        let text = new TextBox(Text="Select a Floor Drop Detail for this room", Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                                    BorderThickness=Thickness(0.), FontSize=16., HorizontalContentAlignment=HorizontalAlignment.Center)
        let textBorder = new Border(BorderThickness=Thickness(3.), Child=text, Background=Brushes.Black, BorderBrush=Brushes.Gray)
        [
            textBorder :> FrameworkElement, -4., -30.
            dungeonRoomExplainer, -3.-boxX, 342.-boxY-THE_DIFF
        ]
    setOpacity(0.8)
    return! DoModalGridSelect(cm, boxX+3., boxY+3., innerc, gridElementsSelectablesAndIDs, originalStateIndex, 0, (3, 3, 18, 18), 9., 9., gridX, gridY, 
                                redrawTile, onClick, extraDecorations, itemBoxModalGridSelectBrushes, WarpToCenter, None, "FloorDropDetail", None)
    }

let brushes = (new CustomComboBoxes.ModalGridSelectBrushes(Brushes.Lime, Brushes.Lime, Brushes.Red, Brushes.Gray)).Dim(0.6)
let DoDungeonRoomSelectPopup(cm:CustomComboBoxes.CanvasManager, originalRoomState:DungeonRoomState, usedTransports:_[], setNewValue, positionAtEntranceRoomIcons, gridClickDismissalWarpReturn) = async {
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
    let SCALE = 2.
    
    let tileX,tileY = 555., 768.
    let ST = CustomComboBoxes.borderThickness

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
            upcast tweak(Graphics.BItoImage(rt.UncompletedBI())), isLegal, rt
        )
    let originalStateIndex = match (grid |> Array.tryFindIndex (fun x -> x = originalRoomState.RoomType)) with Some x -> x | _ -> grid |> Array.findIndex (fun x -> x = RoomType.Unmarked)
    let activationDelta = 0
    let (gnc, gnr, gcw, grh) = 7, 5, 13*3, 9*3
    let totalGridWidth = float gnc*(float gcw + 2.*ST)
    let totalGridHeight = float gnr*(float grh + 2.*ST)
    let gx,gy = -117.,-78.-totalGridHeight
    let fullRoom = originalRoomState.Clone()  // a copy with the original decorations, used for redrawTile display
    fullRoom.IsComplete <- false
    let redrawTile(curState:RoomType) =
        tileCanvas.Children.Clear()
        fullRoom.RoomType <- curState
        let fullRoomDisplay = fullRoom.CurrentDisplay()
        let scaleTrans = new ScaleTransform(SCALE, SCALE)
        if scaleTrans.CanFreeze then
            scaleTrans.Freeze()
        fullRoomDisplay.RenderTransform <- scaleTrans
        RenderOptions.SetBitmapScalingMode(fullRoomDisplay, BitmapScalingMode.NearestNeighbor)
        fullRoomDisplay.Opacity <- tileSunglasses
        canvasAdd(tileCanvas, fullRoomDisplay, 0., 0.)
        let textWidth = 328.
        let topText = Graphics.center(makeCaption("Select a room type", true), int textWidth, 24)
        let s = HotKeys.DungeonRoomHotKeyProcessor.AppendHotKeyToDescription(curState.DisplayDescription, Choice1Of4 curState)
        let bottomText = Graphics.center(makeCaption(s, true), int textWidth, 49)
        let dp = new DockPanel(Width=textWidth, Height=totalGridHeight+75., LastChildFill=false)
        DockPanel.SetDock(topText, Dock.Top)
        DockPanel.SetDock(bottomText, Dock.Bottom)
        dp.Children.Add(topText) |> ignore
        dp.Children.Add(bottomText) |> ignore
        let frame = new Border(BorderBrush=Brushes.DimGray, BorderThickness=Thickness(3.), Background=Brushes.Black, Child=dp)
        let sp = new StackPanel(Orientation=Orientation.Vertical)
        sp.Children.Add(frame) |> ignore
        canvasAdd(tileCanvas, sp, gx+totalGridWidth/2.-textWidth/2., -27.-dp.Height)
    let onClick(ea:Input.MouseButtonEventArgs, curState) = 
        if (ea.ChangedButton = Input.MouseButton.Left || ea.ChangedButton = Input.MouseButton.Right) && ea.ButtonState = Input.MouseButtonState.Pressed then
            CustomComboBoxes.DismissPopupWithResult(curState)
        else
            CustomComboBoxes.StayPoppedUp
    let extraDecorations = [| 
        dungeonRoomExplainer, -3.-tileX, 342.-tileY
        |]
    setOpacity(0.5)
    if positionAtEntranceRoomIcons then
        // position mouse on entrance icons
        Graphics.WarpMouseCursorTo(Point(tileX+gx+5.5*(float gcw + ST*2.), tileY+gy+totalGridHeight/2.))
    else
        // position mouse on existing RoomType
        let x = originalStateIndex % gnc
        let y = originalStateIndex / gnc
        Graphics.WarpMouseCursorTo(Point(tileX+gx+(float x+0.5)*(float gcw + ST*2.), tileY+gy+(float y+0.5)*(float grh + ST*2.)-THE_DIFF))
    let! r = CustomComboBoxes.DoModalGridSelect(cm, tileX, tileY-THE_DIFF, tileCanvas, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (gnc, gnr, gcw, grh),
                       float gcw/2., float grh/2., gx, gy, redrawTile, onClick, extraDecorations, brushes, gridClickDismissalWarpReturn, None, "DungeonRoom", None)
    workingCopy.IsComplete <- true
    match r with
    | None -> ()
    | Some(curState) -> 
        workingCopy.RoomType <- curState
        setNewValue(workingCopy)

    popupCanvas.Children.Clear()
    cm.DismissPopup()
    }