module PieMenus

open Avalonia.Controls
open Avalonia.Media
open Avalonia
open Avalonia.Layout

let canvasAdd = Graphics.canvasAdd
let OMTW = Graphics.OMTW

let FourWayPieMenu(cm,h,bordersDocksBehaviors:(Border*_*_)[]) = async {
    let wh = new System.Threading.ManualResetEvent(false)
    let c = new Canvas(IsHitTestVisible=true)
    let someDrawnPixelToSeeMouseMoves = new Canvas(Width=16.*OMTW-20., Height=h, Background=Brushes.Black, Opacity=0.01)
    c.Children.Add(someDrawnPixelToSeeMouseMoves)
    let ps = ResizeArray()
    let mutable leftPanel,topPanel,rightPanel,bottomPanel = None,None,None,None
    let selfCleanupFuncs = ResizeArray()    
    for p, dock, behavior in bordersDocksBehaviors do
        match dock with
        | Dock.Left   -> match leftPanel   with | Some _ -> failwith "multiple lefts"   | None -> leftPanel   <- Some(p, behavior)
        | Dock.Right  -> match rightPanel  with | Some _ -> failwith "multiple rights"  | None -> rightPanel  <- Some(p, behavior)
        | Dock.Top    -> match topPanel    with | Some _ -> failwith "multiple tops"    | None -> topPanel    <- Some(p, behavior)
        | Dock.Bottom -> match bottomPanel with | Some _ -> failwith "multiple bottoms" | None -> bottomPanel <- Some(p, behavior)
        | _ -> failwith "bad Dock value"
        let dp = new DockPanel(Width=16.*OMTW-20., Height=h, LastChildFill=false)
        DockPanel.SetDock(p, dock)
        dp.Children.Add(p) |> ignore
        selfCleanupFuncs.Add(fun () -> dp.Children.Clear())  // deparent the panels so we can reuse them
        canvasAdd(c, dp, 10., 10.)
        ps.Add(p)
    let onCloseOrDismiss() =
        ps |> Seq.iter (fun p -> p.BorderBrush <- Brushes.Gray)
        for f in selfCleanupFuncs do f()
    let innerH = h - 2.*(let b,_,_ = bordersDocksBehaviors.[0] in b.Height)
    let g = new Grid(Width=16.*OMTW-20., Height=h)
    let circle = new Shapes.Ellipse(Width=innerH, Height=innerH, Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    g.Children.Add(circle) |> ignore
    //let slash = new Shapes.Line(X1=0., Y1=innerH, X2=innerH, Y2=0., Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    //g.Children.Add(slash) |> ignore
    let r = innerH/2.
    let root2 = 1.414
    let delta = r - r/root2
    let lineCanvas = new Canvas(Width=innerH, Height=innerH)
    let slash1 = new Shapes.Line(StartPoint=Point(0., innerH), EndPoint=Point(delta, innerH-delta), Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    lineCanvas.Children.Add(slash1) |> ignore
    let slash2 = new Shapes.Line(StartPoint=Point(innerH-delta, delta), EndPoint=Point(innerH, 0.), Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    lineCanvas.Children.Add(slash2) |> ignore
    let backslash1 = new Shapes.Line(StartPoint=Point(0., 0.), EndPoint=Point(delta, delta), Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    lineCanvas.Children.Add(backslash1) |> ignore
    let backslash2 = new Shapes.Line(StartPoint=Point(innerH-delta, innerH-delta), EndPoint=Point(innerH, innerH), Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    lineCanvas.Children.Add(backslash2) |> ignore
    g.Children.Add(lineCanvas) |> ignore
    //let backslash = new Shapes.Line(X1=0., Y1=0., X2=innerH, Y2=innerH, Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    //g.Children.Add(backslash) |> ignore
    let outerCircle = new Shapes.Ellipse(Width=innerH*1.414, Height=innerH*1.414, Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    outerCircle.StrokeDashArray <- new Collections.AvaloniaList<_>([|1.;2.|])
    g.Children.Add(outerCircle) |> ignore
    canvasAdd(c, g, 10., 10.)
    let center = Point(8.*OMTW,h*0.5)
    let mutable currentSelection = -1
    c.PointerMoved.Add(fun ea ->
        let pos = ea.GetPosition(c)
        let vector = pos - center
        let distance = sqrt(vector.X*vector.X + vector.Y*vector.Y)
        ps |> Seq.iter (fun p -> p.BorderBrush <- Brushes.Gray)
        currentSelection <- -1
        if distance > innerH/2. then
            if vector.X > 0. && vector.X > abs(vector.Y) then
                match rightPanel with
                | Some(p,_) ->
                    p.BorderBrush <- Brushes.Yellow
                    currentSelection <- int Dock.Right
                | None -> currentSelection <- -1
            elif vector.X < 0. && abs(vector.X) > abs(vector.Y) then
                match leftPanel with
                | Some(p,_) ->
                    p.BorderBrush <- Brushes.Yellow
                    currentSelection <- int Dock.Left
                | None -> currentSelection <- -1
            elif vector.Y < 0. && abs(vector.Y) > abs(vector.X) then
                match topPanel with
                | Some(p,_) ->
                    p.BorderBrush <- Brushes.Yellow
                    currentSelection <- int Dock.Top
                | None -> currentSelection <- -1
            elif vector.Y > 0. && vector.Y > abs(vector.X) then
                match bottomPanel with
                | Some(p,_) ->
                    p.BorderBrush <- Brushes.Yellow
                    currentSelection <- int Dock.Bottom
                | None -> currentSelection <- -1
            else
                currentSelection <- -1
        else
            currentSelection <- -1
        )
    let click() =
        if currentSelection=int Dock.Left then
            match leftPanel with
            | Some(_,b) -> b()
            | None -> failwith "impossible"
        elif currentSelection=int Dock.Top then
            match topPanel with
            | Some(_,b) -> b()
            | None -> failwith "impossible"
        elif currentSelection=int Dock.Right then
            match rightPanel with
            | Some(_,b) -> b()
            | None -> failwith "impossible"
        elif currentSelection=int Dock.Bottom then
            match bottomPanel with
            | Some(_,b) -> b()
            | None -> failwith "impossible"
        else // cancel
            ()
    c.PointerPressed.Add(fun ea ->
        ea.Handled <- true
        click()
        wh.Set() |> ignore
        )
    let mutable isFirstTimeMouseUp = true
    c.PointerReleased.Add(fun ea ->
        if isFirstTimeMouseUp && currentSelection = -1 then
            isFirstTimeMouseUp <- false
        else
            ea.Handled <- true
            click()
            wh.Set() |> ignore
        )
    Graphics.WarpMouseCursorTo(center)
    do! CustomComboBoxes.DoModal(cm, wh, 0., 0., c)
    onCloseOrDismiss()
    }


let takeAnyW = 270.
let takeAnyH = 180.
let BT = 5.
let rightMarginSize = 10.

let resizeImage(screenshotBMP) =
    let image = Graphics.BMPtoImage screenshotBMP
    image.Width <- takeAnyW
    image.Height <- takeAnyH
    image.Stretch <- Stretch.UniformToFill
    image
let makeSkippedHeart() =
    let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
    canvasAdd(c, Graphics.BMPtoImage(Graphics.owHeartEmpty_bmp), 0., 0.)
    CustomComboBoxes.placeSkippedItemXDecoration(c)
    c
let makeItemBox(itemBMP, yesno) =
    let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
    c.Children.Add(new Shapes.Rectangle(Width=30., Height=30., Stroke=yesno, StrokeThickness=3.0)) |> ignore
    canvasAdd(c, Graphics.BMPtoImage itemBMP, 4., 4.)
    c
let makeXtoY(x,y,rightMargin,group:StackPanel) =
    let row = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center, Margin=Thickness(0.,0.,rightMargin,0.))
    row.Children.Add(x) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.iconRightArrow_bmp) |> ignore
    row.Children.Add(y) |> ignore
    group.Children.Add(row) |> ignore

let TAKE_ANY = TrackerModel.MapSquareChoiceDomainHelper.TAKE_ANY
let SWORD1 = TrackerModel.MapSquareChoiceDomainHelper.SWORD1

// TAKE ANY ONE YOU WANT

let takeAnyCandlePanel = 
    let c1 = makeItemBox(Graphics.blue_candle_bmp,CustomComboBoxes.no)
    let c2 = makeItemBox(Graphics.blue_candle_bmp,CustomComboBoxes.yes)
    let col = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
    let group = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    makeXtoY(Graphics.BMPtoImage Graphics.owHeartEmpty_bmp, makeSkippedHeart(), rightMarginSize, group)
    makeXtoY(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[0], Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[1], rightMarginSize, group)
    makeXtoY(c1, c2, 0., group)
    col.Children.Add(group) |> ignore
    col.Children.Add(resizeImage Graphics.takeAnyCandleBMP) |> ignore
    let b = new Border(Child=col, BorderBrush=Brushes.Gray, BorderThickness=Thickness(BT), Width=takeAnyW+2.*BT, Height=takeAnyH+2.*BT+30., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    b

let takeAnyPotionPanel = 
    let col = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
    let group = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    makeXtoY(Graphics.BMPtoImage Graphics.owHeartEmpty_bmp, makeSkippedHeart(), rightMarginSize, group)
    makeXtoY(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[0], Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[1], 0., group)
    col.Children.Add(group) |> ignore
    col.Children.Add(resizeImage Graphics.takeAnyPotionBMP) |> ignore
    let b = new Border(Child=col, BorderBrush=Brushes.Gray, BorderThickness=Thickness(BT), Width=takeAnyW+2.*BT, Height=takeAnyH+2.*BT+30., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    b

let takeAnyHeartPanel = 
    let col = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
    let group = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    makeXtoY(Graphics.BMPtoImage Graphics.owHeartEmpty_bmp, Graphics.BMPtoImage Graphics.owHeartFull_bmp, rightMarginSize, group)
    makeXtoY(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[0], Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[1], 0., group)
    col.Children.Add(group) |> ignore
    col.Children.Add(resizeImage Graphics.takeAnyHeartBMP) |> ignore
    let b = new Border(Child=col, BorderBrush=Brushes.Gray, BorderThickness=Thickness(BT), Width=takeAnyW+6., Height=takeAnyH+2.*BT+30., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    b

let takeAnyLeavePanel = 
    let col = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
    let group = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    makeXtoY(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[0], Graphics.BMPtoImage Graphics.theInteriorBmpTable.[TAKE_ANY].[0], 0., group)
    col.Children.Add(resizeImage Graphics.takeAnyLeaveBMP) |> ignore
    col.Children.Add(group) |> ignore
    let b = new Border(Child=col, BorderBrush=Brushes.Gray, BorderThickness=Thickness(BT), Width=takeAnyW+6., Height=takeAnyH+2.*BT+30., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    b

let TakeAnyPieMenuAsync(cm,h) =
    let whichHeart() =
        if TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(0)=0 then 0
        elif TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(1)=0 then 1
        elif TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(2)=0 then 2
        elif TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(3)=0 then 3
        else 
//            System.Media.SystemSounds.Asterisk.Play()  // warn the user something is awry
            -1
    let mutable r = false
    let candleBehavior() =
        let which = whichHeart()
        if which <> -1 then
            TrackerModel.playerProgressAndTakeAnyHearts.SetTakeAnyHeart(which, 2)
        TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle.Set(true)
        r <- true
    let potionBehavior() =
        let which = whichHeart()
        if which <> -1 then
            TrackerModel.playerProgressAndTakeAnyHearts.SetTakeAnyHeart(which, 2)
        r <- true
    let heartBehavior() =
        let which = whichHeart()
        if which <> -1 then
            TrackerModel.playerProgressAndTakeAnyHearts.SetTakeAnyHeart(which, 1)
        r <- true
    let bordersDocksBehaviors = [|
        takeAnyCandlePanel, Dock.Top,    candleBehavior
        takeAnyPotionPanel, Dock.Left,   potionBehavior
        takeAnyHeartPanel,  Dock.Right,  heartBehavior
        takeAnyLeavePanel,  Dock.Bottom, fun()->()
        |]
    async {
        do! FourWayPieMenu(cm, h, bordersDocksBehaviors)
        return r
    }

// IT'S DANGEROUS TO GO ALONE - TAKE THIS

let takeThisCandlePanel = 
    let c1 = makeItemBox(Graphics.blue_candle_bmp,CustomComboBoxes.no)
    let c2 = makeItemBox(Graphics.blue_candle_bmp,CustomComboBoxes.yes)
    let col = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
    let group = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    makeXtoY(c1, c2, rightMarginSize, group)
    makeXtoY(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[SWORD1].[0], Graphics.BMPtoImage Graphics.theInteriorBmpTable.[SWORD1].[1], 0., group)
    col.Children.Add(group) |> ignore
    col.Children.Add(resizeImage Graphics.takeThisCandleBMP) |> ignore
    let b = new Border(Child=col, BorderBrush=Brushes.Gray, BorderThickness=Thickness(BT), Width=takeAnyW+6., Height=takeAnyH+2.*BT+30., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    b

let takeThisWoodSwordPanel = 
    let c1 = makeItemBox(Graphics.brown_sword_bmp,CustomComboBoxes.no)
    let c2 = makeItemBox(Graphics.brown_sword_bmp,CustomComboBoxes.yes)
    let col = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
    let group = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    makeXtoY(c1, c2, rightMarginSize, group)
    makeXtoY(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[SWORD1].[0], Graphics.BMPtoImage Graphics.theInteriorBmpTable.[SWORD1].[1], 0., group)
    col.Children.Add(group) |> ignore
    col.Children.Add(resizeImage Graphics.takeThisWoodSwordBMP) |> ignore
    let b = new Border(Child=col, BorderBrush=Brushes.Gray, BorderThickness=Thickness(BT), Width=takeAnyW+6., Height=takeAnyH+2.*BT+30., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    b

let takeThisLeavePanel = 
    let col = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
    let group = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    makeXtoY(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[SWORD1].[0], Graphics.BMPtoImage Graphics.theInteriorBmpTable.[SWORD1].[0], 0., group)
    col.Children.Add(resizeImage Graphics.takeThisLeaveBMP) |> ignore
    col.Children.Add(group) |> ignore
    let b = new Border(Child=col, BorderBrush=Brushes.Gray, BorderThickness=Thickness(BT), Width=takeAnyW+6., Height=takeAnyH+2.*BT+30., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    b

let TakeThisPieMenuAsync(cm,h) =
    let mutable r = false
    let candleBehavior() =
        TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle.Set(true)
        r <- true
    let swordBehavior() =
        TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword.Set(true)
        r <- true
    let bordersDocksBehaviors = [|
        takeThisWoodSwordPanel, Dock.Top,    swordBehavior
        takeThisCandlePanel,    Dock.Left,   candleBehavior
        takeThisLeavePanel,     Dock.Bottom, fun()->()
        |]
    async {
        do! FourWayPieMenu(cm, h, bordersDocksBehaviors)
        return r
    }
