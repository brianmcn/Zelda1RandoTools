module CustomComboBoxes

open Avalonia.Controls
open Avalonia.Media
open Avalonia
open Avalonia.Layout

let canvasAdd = Graphics.canvasAdd
let gridAdd = Graphics.gridAdd
let makeGrid = Graphics.makeGrid

// (See WPF version for giant design discussion comment)

////////////////////////////////////////////////////////////
// ItemComboBox

let MouseButtonEventArgsToPlayerHas(ea:Input.PointerPoint) =  // a poor name for Avalonia, but I like keeping the codebases easy to compare
    if ea.Properties.IsLeftButtonPressed then TrackerModel.PlayerHas.YES
    elif ea.Properties.IsRightButtonPressed then TrackerModel.PlayerHas.NO
    else TrackerModel.PlayerHas.SKIPPED
let no = Brushes.DarkRed
let yes = Brushes.LimeGreen 
let skipped = Brushes.MediumPurple
let boxCurrentBMP(isCurrentlyBook, boxCellCurrent, isForTimeline) =
    match boxCellCurrent with
    | -1 -> null
    |  0 -> (if !isCurrentlyBook then Graphics.book_bmp else Graphics.magic_shield_bmp)
    |  1 -> Graphics.boomerang_bmp
    |  2 -> Graphics.bow_bmp
    |  3 -> Graphics.power_bracelet_bmp
    |  4 -> Graphics.ladder_bmp
    |  5 -> Graphics.magic_boomerang_bmp
    |  6 -> Graphics.key_bmp
    |  7 -> Graphics.raft_bmp
    |  8 -> Graphics.recorder_bmp
    |  9 -> Graphics.red_candle_bmp
    | 10 -> Graphics.red_ring_bmp
    | 11 -> Graphics.silver_arrow_bmp
    | 12 -> Graphics.wand_bmp
    | 13 -> Graphics.white_sword_bmp
    |  _ -> if isForTimeline then Graphics.owHeartFull_bmp else Graphics.heart_container_bmp

let mutable private c, sunglasses = (null:Canvas), null
let mutable private primaryBoxCellCurrent = -99
let mutable private primaryBoxRedraw = fun() -> ()
let mutable private commitFunc = fun(_boxCellCurrent,_playerHas) -> ()
let private gridBoxRedrawFuncs = ResizeArray()
let private isGreyed = Array2D.zeroCreate 4 4
let private g = makeGrid(4, 4, 30, 30)
let private gridContainer = new StackPanel(Orientation=Orientation.Vertical)

let private currentAsArrayIndex(current,a:_[,]) =
    match current with
    | 0 -> a.[0,0]
    | 1 -> a.[1,0]
    | 2 -> a.[2,0]
    | 3 -> a.[3,0]
    | 4 -> a.[0,1]
    | 5 -> a.[1,1]
    | 6 -> a.[2,1]
    | 7 -> a.[3,1]
    | 8 -> a.[0,2]
    | 9 -> a.[1,2]
    | 10 -> a.[2,2]
    | 11 -> a.[3,2]
    | 12 -> a.[0,3]
    | 13 -> a.[1,3]
    | 14 -> a.[2,3]
    | -1 -> a.[3,3]
    | _ -> failwith "bad currentAsArrayIndex"
let private Modulus(current) =   // -1..14
    let x = (current + 16) % 16
    if x = 15 then - 1 else x
let private Next(current) =
    let mutable current = Modulus(current+1)
    while currentAsArrayIndex(current,isGreyed) do
        current <- Modulus(current+1)
    current
let private Prev(current) =
    let mutable current = Modulus(current-1)
    while currentAsArrayIndex(current,isGreyed) do
        current <- Modulus(current-1)
    current
    
let makeItemBoxPicture(boxCellCurrent, isCurrentlyBook, isScrollable) =
    let c = new Canvas(Width=30., Height=30., Background=Brushes.Black, IsHitTestVisible=true)
    let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=Brushes.Black, StrokeThickness=3.0, IsHitTestVisible=false)
    c.Children.Add(rect) |> ignore
    let innerc = new Canvas(Width=30., Height=30., Background=Brushes.Transparent, IsHitTestVisible=false)  // just has item drawn on it, not the box
    c.Children.Add(innerc) |> ignore
    let redraw(n) =
        innerc.Children.Clear()
        let bmp = boxCurrentBMP(isCurrentlyBook, n, false)
        if bmp <> null then
            let image = Graphics.BMPtoImage(bmp)
            image.IsHitTestVisible <- false
            canvasAdd(innerc, image, 4., 4.)
    redraw(boxCellCurrent)
    let greyOut = new Canvas(Width=30., Height=30., Background=Brushes.Black, Opacity=0.0, IsHitTestVisible=false)
    c.Children.Add(greyOut) |> ignore
    if isScrollable then
        primaryBoxCellCurrent <- boxCellCurrent
        primaryBoxRedraw <- (fun() -> redraw(primaryBoxCellCurrent))
        c.PointerWheelChanged.Add(fun x -> 
            if x.Delta.Y<0. then
                primaryBoxCellCurrent <- Next(primaryBoxCellCurrent)
            else
                primaryBoxCellCurrent <- Prev(primaryBoxCellCurrent)
            redraw(primaryBoxCellCurrent)  // redraw self
            // redraw popup grid
            for f in gridBoxRedrawFuncs do
                f()
        )
    c, rect, greyOut

let private gridItemBoxPicturesIsCurrentlyBook = Array2D.init 4 4 (fun i j ->
    let boxCellCurrent = Modulus(j*4 + i)
    boxCellCurrent, makeItemBoxPicture(boxCellCurrent, ref true, false)
    )
let private gridItemBoxPicturesIsNotCurrentlyBook = Array2D.init 4 4 (fun i j ->
    let boxCellCurrent = Modulus(j*4 + i)
    boxCellCurrent, makeItemBoxPicture(boxCellCurrent, ref false, false)
    )
let private DismissItemComboBox() =
    c.IsHitTestVisible <- false
    c.Opacity <- 0.
let InitializeItemComboBox(appMainCanvas:Canvas) =
    // rather than use MouseCapture() API, just draw a canvas over entire window which will intercept all mouse gestures
    c <- new Canvas(Width=appMainCanvas.Width, Height=appMainCanvas.Height, Background=Brushes.Transparent, IsHitTestVisible=false, Opacity=0.)
    appMainCanvas.Children.Add(c) |> ignore
    sunglasses <- new Canvas(Width=appMainCanvas.Width, Height=appMainCanvas.Height, Background=Brushes.Black, IsHitTestVisible=false, Opacity=0.5)
    c.PointerPressed.Add(fun ea ->
        let pp = ea.GetCurrentPoint(c)
        if pp.Properties.IsLeftButtonPressed || pp.Properties.IsMiddleButtonPressed || pp.Properties.IsRightButtonPressed then 
            // if there were something to do, we would undo it here, but there is no model or view change, other than...
            DismissItemComboBox()
        )
    // establish all grid mouse interaction logic
    for i = 0 to 3 do
        for j = 0 to 3 do
            for a in [gridItemBoxPicturesIsCurrentlyBook; gridItemBoxPicturesIsNotCurrentlyBook] do
                let n,(pict,rect,greyOut) = a.[i,j]
                pict.PointerEnter.Add(fun ea ->
                    // unselect prior
                    let _,(_,oldrect,_) = currentAsArrayIndex(primaryBoxCellCurrent, a)
                    oldrect.Stroke <- Brushes.Black
                    // select it
                    primaryBoxCellCurrent <- n
                    rect.Stroke <- Brushes.Yellow
                    // update primary box
                    primaryBoxRedraw()
                    )
                pict.PointerPressed.Add(fun ea ->
                    let pp = ea.GetCurrentPoint(c)
                    if pp.Properties.IsLeftButtonPressed || pp.Properties.IsMiddleButtonPressed || pp.Properties.IsRightButtonPressed then 
                        if greyOut.Opacity = 0. then // not greyed out
                                // select it (no need to update primary box or other grid cell ui, we're about to dismiss entire ui)
                                primaryBoxCellCurrent <- n
                                // commit it
                                ea.Handled <- true
                                DismissItemComboBox()
                                commitFunc(primaryBoxCellCurrent, MouseButtonEventArgsToPlayerHas pp)
                        else
                            ea.Handled <- true  // clicking a greyed out one does nothing, but don't want to let event continue to outer canvas and dismiss the UI, just swallow the click
                    )
    // arrange visual tree of gridContainer
    let d = new DockPanel(Height=90., LastChildFill=true, Background=Brushes.Black)
    let mouseBMP = Graphics.mouseIconButtonColorsBMP
    let mouse = Graphics.BMPtoImage mouseBMP
    mouse.Height <- 90.
    mouse.Width <- float(mouseBMP.Width) * 90. / float(mouseBMP.Height)
    mouse.Stretch <- Stretch.Uniform
    d.Children.Add(mouse) |> ignore
    DockPanel.SetDock(mouse,Dock.Left)
    let sp = new StackPanel(Orientation=Orientation.Vertical)
    d.Children.Add(sp) |> ignore
    for color, text in [yes,"Have it"; skipped,"Don't want it"; no,"Don't have it"] do
        let p = new StackPanel(Orientation=Orientation.Horizontal)
        let pict,rect,greyOut = makeItemBoxPicture(-1, ref false, false)
        rect.Stroke <- color
        greyOut.Opacity <- 0.
        p.Children.Add(pict) |> ignore
        let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, Text=text, VerticalAlignment=VerticalAlignment.Center, BorderThickness=Thickness(0.))
        p.Children.Add(tb) |> ignore
        sp.Children.Add(p) |> ignore
    let left(x) = 
        let sp = new StackPanel(Orientation=Orientation.Horizontal)
        sp.Children.Add(x) |> ignore
        sp
    gridContainer.Children.Add(left <| new Border(Background=Brushes.Black, BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.,3.,3.,0.), Child=g)) |> ignore
    gridContainer.Children.Add(left <| new Border(Background=Brushes.Black, BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.,3.,3.,3.), Child=d)) |> ignore

let DisplayItemComboBox(appMainCanvas:Canvas, boxX, boxY, boxCellCurrent, boxCellFirstSelection, isCurrentlyBook, commitFunction) =
    commitFunc <- commitFunction
    c.IsHitTestVisible <- true
    c.Opacity <- 1.
    c.Children.Clear()
    c.Children.Add(sunglasses) |> ignore
    // put the box at boxX,boxY
    let pict,rect,greyOut = makeItemBoxPicture(boxCellFirstSelection, isCurrentlyBook, true)
    rect.Stroke <- Brushes.Yellow
    greyOut.Opacity <- 0.
    canvasAdd(c, pict, boxX, boxY)
    // clicking the primary box
    pict.PointerPressed.Add(fun ea ->
        let pp = ea.GetCurrentPoint(c)
        if pp.Properties.IsLeftButtonPressed || pp.Properties.IsMiddleButtonPressed || pp.Properties.IsRightButtonPressed then 
            ea.Handled <- true
            DismissItemComboBox()
            commitFunc(primaryBoxCellCurrent, MouseButtonEventArgsToPlayerHas pp)
        )
    // populate grid with right set of pictures
    g.Children.Clear()
    for i = 0 to 3 do
        for j = 0 to 3 do
            let n,(pict,rect,greyOut) = if !isCurrentlyBook then gridItemBoxPicturesIsCurrentlyBook.[i,j] else gridItemBoxPicturesIsNotCurrentlyBook.[i,j]
            let isOriginallyCurrent = n=boxCellCurrent
            let isSelectable = (n = -1) || isOriginallyCurrent || (TrackerModel.allItemWithHeartShuffleChoiceDomain.NumUses(n) < TrackerModel.allItemWithHeartShuffleChoiceDomain.MaxUses(n))
            isGreyed.[i,j] <- not(isSelectable)
            if isGreyed.[i,j] then
                greyOut.Opacity <- 0.6
            else
                greyOut.Opacity <- 0.0
            let redraw() =
                let isCurrent = n=primaryBoxCellCurrent
                if isCurrent then
                    rect.Stroke <- Brushes.Yellow
                else
                    rect.Stroke <- Brushes.Black
            redraw()
            gridBoxRedrawFuncs.Add(redraw)
            gridAdd(g, pict, i, j)
    // find the right location next to box to place the grid
    canvasAdd(c, gridContainer, boxX, boxY+30.)  // TODO eventually deal with going off the lower/right edge?
    // TODO tweak sunglasses opacity?
    // TODO tweak greyedOut opacity?
    // TODO do the greyed out ones need a red X or a ghostbusters on them, to make it clearer they are unclickable?



let DoModal(appMainCanvas:Canvas, x, y, element, onClose) =
    // rather than use MouseCapture() API, just draw a canvas over entire window which will intercept all mouse gestures
    let c = new Canvas(Width=appMainCanvas.Width, Height=appMainCanvas.Height, Background=Brushes.Transparent, IsHitTestVisible=true, Opacity=1.)
    appMainCanvas.Children.Add(c) |> ignore
    let sunglasses = new Canvas(Width=appMainCanvas.Width, Height=appMainCanvas.Height, Background=Brushes.Black, IsHitTestVisible=false, Opacity=0.5)
    c.Children.Add(sunglasses) |> ignore
    // put the element at x,y
    canvasAdd(c, element, x, y)
    // catch mouse clicks outside the element to dismiss mode
    c.PointerPressed.Add(fun ea ->
        let pp = ea.GetCurrentPoint(c)
        if pp.Properties.IsLeftButtonPressed || pp.Properties.IsMiddleButtonPressed || pp.Properties.IsRightButtonPressed then 
            // if there were something to do, we would undo it here, but there is no model or view change, other than...
            onClose()
            c.Children.Remove(element) |> ignore
            appMainCanvas.Children.Remove(c) |> ignore
        )

let DoModalDocked(appMainCanvas:Canvas, dock, element, onClose) =
    // rather than use MouseCapture() API, just draw a canvas over entire window which will intercept all mouse gestures
    let c = new Canvas(Width=appMainCanvas.Width, Height=appMainCanvas.Height, Background=Brushes.Transparent, IsHitTestVisible=true, Opacity=1.)
    appMainCanvas.Children.Add(c) |> ignore
    let sunglasses = new Canvas(Width=appMainCanvas.Width, Height=appMainCanvas.Height, Background=Brushes.Black, IsHitTestVisible=false, Opacity=0.5)
    c.Children.Add(sunglasses) |> ignore
    let d = new DockPanel(Width=appMainCanvas.Width, Height=appMainCanvas.Height, LastChildFill=false)
    // put the element docked
    DockPanel.SetDock(element, dock)
    d.Children.Add(element) |> ignore
    canvasAdd(c, d, 0., 0.)
    // catch mouse clicks outside the element to dismiss mode
    c.PointerPressed.Add(fun ea ->
        let pp = ea.GetCurrentPoint(c)
        if pp.Properties.IsLeftButtonPressed || pp.Properties.IsMiddleButtonPressed || pp.Properties.IsRightButtonPressed then 
            // if there were something to do, we would undo it here, but there is no model or view change, other than...
            onClose()
            d.Children.Remove(element) |> ignore
            appMainCanvas.Children.Remove(c) |> ignore
        )
