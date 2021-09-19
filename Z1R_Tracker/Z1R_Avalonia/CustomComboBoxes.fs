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

///////////////////////////////////////////////////////////////////

let DoModalCore(appMainCanvas:Canvas, placeElementOntoCanvas, removeElementFromCanvas, element:Control, blackSunglassesOpacity, onClose) =
    // rather than use MouseCapture() API, just draw a canvas over entire window which will intercept all mouse gestures
    let c = new Canvas(Width=appMainCanvas.Width, Height=appMainCanvas.Height, Background=Brushes.Transparent, IsHitTestVisible=true, Opacity=1.)
    appMainCanvas.Children.Add(c) |> ignore
    let sunglasses = new Canvas(Width=appMainCanvas.Width, Height=appMainCanvas.Height, Background=Brushes.Black, IsHitTestVisible=false, Opacity=blackSunglassesOpacity)
    c.Children.Add(sunglasses) |> ignore
    // place the element
    placeElementOntoCanvas(c, element)
    let dismiss() =
        removeElementFromCanvas(c, element)
        appMainCanvas.Children.Remove(c) |> ignore
    // catch mouse clicks outside the element
    c.PointerPressed.Add(fun ea ->
        let pos = ea.GetPosition(c)
        if pos.X = 0. && pos.Y = 0. then
            () // ignore the click, it's e.g. in another window
        elif (pos.X < element.Bounds.Left || pos.X > element.Bounds.Right) || (pos.Y < element.Bounds.Top || pos.Y > element.Bounds.Bottom) then
            let pp = ea.GetCurrentPoint(c)
            if pp.Properties.IsLeftButtonPressed || pp.Properties.IsMiddleButtonPressed || pp.Properties.IsRightButtonPressed then 
                onClose()
                dismiss()
        )
    dismiss // return a dismissal handle, which the caller can use to dismiss the dialog based on their own criteria; note that onClose() is not called by the dismissal handle

let DoModal(appMainCanvas:Canvas, x, y, element, onClose) =
    DoModalCore(appMainCanvas, (fun (c,e) -> canvasAdd(c, e, x, y)), (fun (c,e) -> c.Children.Remove(e) |> ignore), element, 0.5, onClose)

let DoModalDocked(appMainCanvas:Canvas, dock, element, onClose) =
    let d = new DockPanel(Width=appMainCanvas.Width, Height=appMainCanvas.Height, LastChildFill=false)
    DoModalCore(appMainCanvas, 
                    (fun (c,e) -> 
                        DockPanel.SetDock(e, dock)
                        d.Children.Add(e) |> ignore
                        canvasAdd(c, d, 0., 0.)),
                    (fun (c,e) -> d.Children.Remove(e) |> ignore), element, 0.5, onClose)

/////////////////////////////////////////

// draw a pretty dashed rectangle with thickness ST around outside of (0,0) to (W,H)
let MakePrettyDashes(canvas:Canvas, brush, W, H, ST, dash:float, space:float) =
    let dashLength = dash * ST
    let spaceLength = space * ST
    let computeDashArray(length) =
        let usable = length - dashLength
        let n = System.Math.Round(usable/(dashLength + spaceLength)) |> int
        let usableSpace = usable - float n * dashLength
        let actualSpaceLength = usableSpace / float n
        let a = [| 
            yield dash
            for i=0 to n-1 do 
                yield actualSpaceLength / ST
                yield dash
            yield 0.  // avalonia requires an even number of entries
        |]
        a
    let topLine = new Shapes.Line(StartPoint=Point(-ST, -ST/2.), EndPoint=Point(W + ST, -ST/2.), StrokeThickness=ST, Stroke=brush)
    topLine.StrokeDashArray <- new Avalonia.Collections.AvaloniaList<_>(computeDashArray (W+2.*ST))
    canvasAdd(canvas, topLine, 0., 0.)
    let bottomLine = new Shapes.Line(StartPoint=Point(-ST, H+ST/2.), EndPoint=Point(W + ST, H+ST/2.), StrokeThickness=ST, Stroke=brush)
    bottomLine.StrokeDashArray <- new Avalonia.Collections.AvaloniaList<_>(computeDashArray (W+2.*ST))
    canvasAdd(canvas, bottomLine, 0., 0.)
    let leftLine = new Shapes.Line(StartPoint=Point(-ST/2., -ST), EndPoint=Point(-ST/2., H+ST), StrokeThickness=ST, Stroke=brush)
    leftLine.StrokeDashArray <- new Avalonia.Collections.AvaloniaList<_>(computeDashArray (H+2.*ST))
    canvasAdd(canvas, leftLine, 0., 0.)
    let rightLine = new Shapes.Line(StartPoint=Point(W+ST/2., -ST), EndPoint=Point(W+ST/2., H+ST), StrokeThickness=ST, Stroke=brush)
    rightLine.StrokeDashArray <- new Avalonia.Collections.AvaloniaList<_>(computeDashArray (H+2.*ST))
    canvasAdd(canvas, rightLine, 0., 0.)


// TODO: factor out background brush color (assumes black)
type ModalGridSelectBrushes(originalTileHighlightBrush, gridSelectableHighlightBrush, gridNotSelectableHighlightBrush, borderBrush) =
    member _this.OriginalTileHighlightBrush = originalTileHighlightBrush
    member _this.GridSelectableHighlightBrush = gridSelectableHighlightBrush
    member _this.GridNotSelectableHighlightBrush = gridNotSelectableHighlightBrush
    member _this.BorderBrush = borderBrush
    static member Defaults() =
        new ModalGridSelectBrushes(Brushes.Lime, Brushes.Lime, Brushes.Red, Brushes.Gray)
let borderThickness = 3.  // TODO should this be a param?

let DoModalGridSelect<'a>(appMainCanvas, tileX, tileY, tileCanvas:Canvas, // tileCanvas - an empty Canvas with just Width and Height set, one which you will redrawTile your preview-tile
                            gridElementsSelectablesAndIDs:(Control*bool*'a)[], // array of display elements, whether they're selectable, and your stateID name/identifier for them
                            originalStateIndex:int, // originalStateIndex is array index into the array
                            activationDelta:int, // activationDelta is -1/0/1 if we should give initial input of scrollup/none/scrolldown
                            (gnc, gnr, gcw, grh),   // grid: numCols, numRows, colWidth, rowHeight (heights of elements; this control will add border highlight)
                            gx, gy,   // where to place grid (x,y) relative to (0,0) being the (unbordered) corner of your TileCanvas
                            redrawTile,  // we pass you currentStateID
                            onClick,  // called on tile click or selectable grid click, you choose what to do:   (dismissPopupFunc, mousebuttonEA, currentStateID) -> unit
                            onClose,  // called when user clicks outside modal, and it dismisses itself
                            extraDecorations:seq<Control*float*float>,  // extra things to draw at (x,y)s
                            brushes:ModalGridSelectBrushes,
                            gridClickDismissalDoesMouseWarpBackToTileCenter  // TODO currently unused
                            ) =
    let popupCanvas = new Canvas()  // we will draw outside the canvas
    canvasAdd(popupCanvas, tileCanvas, 0., 0.)
    let ST = borderThickness
    MakePrettyDashes(popupCanvas, brushes.OriginalTileHighlightBrush, tileCanvas.Width, tileCanvas.Height, ST, 2., 1.2)
    if gridElementsSelectablesAndIDs.Length > gnr*gnc then
        failwith "the grid is not big enough to accomodate all the choices"
    let grid = makeGrid(gnc, gnr, gcw+2*int ST, grh+2*int ST)
    grid.Background <- Brushes.Black
    let mutable currentState = originalStateIndex   // the only bit of local mutable state during the modal - it ranges from 0..gridElements.Length-1
    let mutable dismissDoModalPopup = fun () -> ()
    let selfCleanup() =
        for (d,x,y) in extraDecorations do
            popupCanvas.Children.Remove(d) |> ignore
    let dismiss() =
        dismissDoModalPopup()
        selfCleanup()
    let isSelectable() = let _,s,_ = gridElementsSelectablesAndIDs.[currentState] in s
    let stateID() = let _,_,x = gridElementsSelectablesAndIDs.[currentState] in x
    let redrawGridFuncs = ResizeArray()
    let changeCurrentState(newState) =
        currentState <- newState
        redrawTile(stateID())
        for r in redrawGridFuncs do r()
    let next() =
        currentState <- (currentState+1) % gridElementsSelectablesAndIDs.Length
        while not(isSelectable()) do
            currentState <- (currentState+1) % gridElementsSelectablesAndIDs.Length
        changeCurrentState(currentState)
    let prev() =
        currentState <- (currentState-1+gridElementsSelectablesAndIDs.Length) % gridElementsSelectablesAndIDs.Length
        while not(isSelectable()) do
            currentState <- (currentState-1+gridElementsSelectablesAndIDs.Length) % gridElementsSelectablesAndIDs.Length
        changeCurrentState(currentState)
    let snapBack() = changeCurrentState(originalStateIndex)
    // original tile
    redrawTile(stateID())
    tileCanvas.PointerWheelChanged.Add(fun x -> if x.Delta.Y<0. then next() else prev())
    tileCanvas.PointerPressed.Add(fun ea -> 
        ea.Handled <- true
        onClick(dismiss, ea, stateID())
        )
    tileCanvas.PointerLeave.Add(fun _ -> snapBack())
    // grid of choices
    for x = 0 to gnc-1 do
        for y = 0 to gnr-1 do
            let n = y*gnc + x
            let icon,isSelectable,_ = if n < gridElementsSelectablesAndIDs.Length then gridElementsSelectablesAndIDs.[n] else null,false,Unchecked.defaultof<_>
            if icon <> null then
                let c = new Canvas(Background=Brushes.Black, Width=float gcw, Height=float grh)  // ensure the canvas has a surface on which to receive mouse clicks
                c.Children.Add(icon) |> ignore
                if not(isSelectable) then // grey out
                    c.Children.Add(new Canvas(Width=float gcw, Height=float grh, Background=Brushes.Black, Opacity=0.6, IsHitTestVisible=false)) |> ignore
                let b = new Border(BorderThickness=Thickness(ST), Child=c)
                b.PointerEnter.Add(fun _ -> changeCurrentState(n))
                let redraw() = b.BorderBrush <- (if n = currentState then (if isSelectable then brushes.GridSelectableHighlightBrush else brushes.GridNotSelectableHighlightBrush) else Brushes.Black)
                redrawGridFuncs.Add(redraw)
                redraw()
                b.PointerPressed.Add(fun ea -> 
                    ea.Handled <- true
                    if isSelectable then
                        onClick(dismiss, ea, stateID())
                    )
                gridAdd(grid, b, x, y)
            else
                let dp = new DockPanel(Background=Brushes.Black)
                dp.PointerEnter.Add(fun _ -> snapBack())
                dp.PointerPressed.Add(fun ea -> ea.Handled <- true)  // empty grid elements swallow clicks because we don't want to commit or dismiss
                gridAdd(grid, dp, x, y)
    grid.PointerLeave.Add(fun _ -> snapBack())
    let b = new Border(BorderThickness=Thickness(ST), BorderBrush=brushes.BorderBrush, Child=grid)
    canvasAdd(popupCanvas, b, gx, gy)
    for (d,x,y) in extraDecorations do
        canvasAdd(popupCanvas, d, x, y)
    // initial input
    match activationDelta with
    | -1 -> prev()
    |  0 -> ()
    |  1 -> next()
    | _ -> failwith "bad activationDelta"
    // activate the modal
    dismissDoModalPopup <- DoModal(appMainCanvas, tileX, tileY, popupCanvas, (fun () -> onClose(); selfCleanup()))

////////////////////////////////

let itemBoxMouseButtonExplainerDecoration =
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
        let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=color, StrokeThickness=3.0, IsHitTestVisible=false)
        p.Children.Add(rect) |> ignore
        let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, Text=text, VerticalAlignment=VerticalAlignment.Center, BorderThickness=Thickness(0.))
        p.Children.Add(tb) |> ignore
        sp.Children.Add(p) |> ignore
    let b = new Border(Background=Brushes.Black, BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Child=d)
    let fe : Control = upcast b
    fe
let itemBoxModalGridSelectBrushes = new ModalGridSelectBrushes(Brushes.Yellow, Brushes.Yellow, new SolidColorBrush(Color.FromRgb(140uy,10uy,0uy)), Brushes.Gray)

let DisplayItemComboBox(appMainCanvas:Canvas, boxX, boxY, boxCellCurrent, activationDelta, isCurrentlyBook, 
                            commitFunction,  // the user clicked a selection, we're dismissing the modal, this is how we notify you of the final choice
                            onClose) =
    let innerc = new Canvas(Width=24., Height=24., Background=Brushes.Black)  // just has item drawn on it, not the box
    let redraw(n) =
        innerc.Children.Clear()
        let bmp = boxCurrentBMP(isCurrentlyBook, n, false)
        if bmp <> null then
            let image = Graphics.BMPtoImage(bmp)
            canvasAdd(innerc, image, 1., 1.)
    redraw(boxCellCurrent)
    let gridElementsSelectablesAndIDs = [|
        for n = 0 to 15 do
            let fe:Control = if n=15 then upcast new Canvas() else upcast (boxCurrentBMP(isCurrentlyBook, n, false) |> Graphics.BMPtoImage)
            let isSelectable = n = 15 || n=boxCellCurrent || TrackerModel.allItemWithHeartShuffleChoiceDomain.CanAddUse(n)
            let ident = if n=15 then -1 else n
            yield fe, isSelectable, ident
        |]
    let originalStateIndex = if boxCellCurrent = -1 then 15 else boxCellCurrent
    let onClick(dismissPopup,ea:Input.PointerPressedEventArgs,ident) =
        let pp = ea.GetCurrentPoint(appMainCanvas)
        // we're getting a click with mouse event args ea on one of the selectable items in the grid, namely ident. take appropriate action.
        dismissPopup()
        commitFunction(ident, MouseButtonEventArgsToPlayerHas pp)
    let redrawTile(ident) =
        // the user has changed the current selection via mousing or scrolling, redraw the preview tile appropriately to display ident
        redraw(ident)
    let selfCleanup() =
        // the user clicked outside the modal dialog, it is dismissing itself, do any final cleanup we need to do
        ()
    let allCleanup() = selfCleanup(); onClose()
    let extraDecorations = [itemBoxMouseButtonExplainerDecoration, -3., 138.]
    DoModalGridSelect(appMainCanvas, boxX+3., boxY+3., innerc, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (4, 4, 21, 21), -3., 27., 
        redrawTile, onClick, onClose, extraDecorations, new ModalGridSelectBrushes(Brushes.Yellow, Brushes.Yellow, new SolidColorBrush(Color.FromRgb(140uy,10uy,0uy)), Brushes.Gray), true)

let DisplayRemoteItemComboBox(appMainCanvas:Canvas, boxX, boxY, boxCellCurrent, activationDelta, isCurrentlyBook, gridX, gridY, commitFunction, onClose, extraDecorations) =
    let innerc = new Canvas(Width=24., Height=24., Background=Brushes.Black)  // just has item drawn on it, not the box
    let redraw(n) =
        innerc.Children.Clear()
        let bmp = boxCurrentBMP(isCurrentlyBook, n, false)
        if bmp <> null then
            let image = Graphics.BMPtoImage(bmp)
            canvasAdd(innerc, image, 1., 1.)
    redraw(boxCellCurrent)
    let gridElementsSelectablesAndIDs = [|
        for n = 0 to 15 do
            let fe:Control = if n=15 then upcast new Canvas() else upcast (boxCurrentBMP(isCurrentlyBook, n, false) |> Graphics.BMPtoImage)
            let isSelectable = n = 15 || n=boxCellCurrent || TrackerModel.allItemWithHeartShuffleChoiceDomain.CanAddUse(n)
            let ident = if n=15 then -1 else n
            yield fe, isSelectable, ident
        |]
    let originalStateIndex = if boxCellCurrent = -1 then 15 else boxCellCurrent
    let onClick(dismissPopup,ea:Input.PointerPressedEventArgs,ident) = 
        let pp = ea.GetCurrentPoint(appMainCanvas)
        dismissPopup()
        commitFunction(ident, MouseButtonEventArgsToPlayerHas pp)
    let redrawTile(ident) = redraw(ident)
    DoModalGridSelect(appMainCanvas, boxX+3., boxY+3., innerc, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (4, 4, 21, 21), gridX, gridY, 
        redrawTile, onClick, onClose, extraDecorations, itemBoxModalGridSelectBrushes, true)
