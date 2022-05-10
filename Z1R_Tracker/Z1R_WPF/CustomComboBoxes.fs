module CustomComboBoxes

open System.Windows.Controls
open System.Windows.Media
open System.Windows

let canvasAdd = Graphics.canvasAdd
let gridAdd = Graphics.gridAdd
let makeGrid = Graphics.makeGrid

////////////////////////////////////////////////////////////
// ItemComboBox

let MouseButtonEventArgsToPlayerHas(ea:Input.MouseButtonEventArgs) =
    if ea.ChangedButton = Input.MouseButton.Left then TrackerModel.PlayerHas.YES
    elif ea.ChangedButton = Input.MouseButton.Right then TrackerModel.PlayerHas.NO
    else TrackerModel.PlayerHas.SKIPPED
let no  = new SolidColorBrush(Color.FromRgb(0xA8uy,0x00uy,0x00uy))
let noAndNotEmpty = Brushes.Red
let yes = new SolidColorBrush(Color.FromRgb(0x32uy,0xA8uy,0x32uy))
let skipped = Brushes.MediumPurple
let skippedAndEmpty = Brushes.White
let boxCurrentBMP(boxCellCurrent, isForTimeline) =
    match boxCellCurrent with
    | -1 -> null
    |  0 -> (if TrackerModel.IsCurrentlyBook() then Graphics.book_bmp else Graphics.magic_shield_bmp)
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

(*
note: a possible alternate way to 'sunglasses' would be to capture a snapshot as a bitmap, and then place it, here is some code i found that may be relevant
public void SaveUIElementToImageBMPFile(UIElement VisualElement, string FilePath) {
System.Windows.Media.Imaging.RenderTargetBitmap targetBitmap =
      new System.Windows.Media.Imaging.RenderTargetBitmap((int)VisualElement.RenderSize.Width,
                                                          (int)VisualElement.RenderSize.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
VisualElement.Measure(VisualElement.RenderSize);  
VisualElement.Arrange(new Rect(VisualElement.RenderSize)); 
targetBitmap.Render(VisualElement);
System.Windows.Media.Imaging.BmpBitmapEncoder bmpencoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
bmpencoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(targetBitmap));
using (System.IO.FileStream filestream = new System.IO.FileStream(FilePath, System.IO.FileMode.Create))
{
    bmpencoder.Save(filestream);
    filestream.Close();
}
*)
let BROADCAST_KLUDGE = 50.  // the bottom-half broadcast window has the bottom of timeline peek out beyond the popup-sunglasses, causing bottom-timeline to be bright; this 'fixes' it
type CanvasManager(rootCanvas:Canvas, appMainCanvas:Canvas) as this =
    static let mutable theOnlyCanvasManager = None
    do
        if rootCanvas.Width <> appMainCanvas.Width || rootCanvas.Height <> appMainCanvas.Height then
            failwith "rootCanvas and appMainCanvas should be the same size"
        if not(obj.Equals(appMainCanvas.Parent,rootCanvas)) || not(rootCanvas.Children.Count=1) then
            failwith "rootCanvas must have appMainCanvas as its only child"
        if theOnlyCanvasManager.IsSome then
            failwith "created more than one CanvasManager"
        theOnlyCanvasManager <- Some(this)
    let popupCanvasStack = new System.Collections.Generic.Stack<_>()
    let opacityStack = new System.Collections.Generic.Stack<_>()
    let afterCreatePopupCanvas = new Event<_>()
    let beforeDismissPopupCanvas = new Event<_>()
    let width = rootCanvas.Width
    let height = rootCanvas.Height
    let sunglasses = new Canvas(Width=width, Height=height+BROADCAST_KLUDGE, Background=Brushes.Black, IsHitTestVisible=false)
    member _this.Width = width
    member _this.Height = height
    member _this.RootCanvas = rootCanvas           // basically, no one should touch this, except to set mainWindow.Content <- cm.RootCanvas
    member _this.AppMainCanvas = appMainCanvas     // this is where the app should be putting all its content
    // and down here is where/how the popups should be putting their content
    member _this.PopupCanvasStack = popupCanvasStack
    member _this.CreatePopup(blackSunglassesOpacity) = 
        opacityStack.Push(blackSunglassesOpacity)
        sunglasses.Opacity <- opacityStack |> Seq.max
        // remove sunglasses from prior prior layer (if there was one)
        if sunglasses.Parent <> null then
            (sunglasses.Parent :?> Canvas).Children.Remove(sunglasses)
        // put the sunglasses over the prior child (appMainCanvas or previous popupCanvas)
        (rootCanvas.Children.[rootCanvas.Children.Count-1] :?> Canvas).Children.Add(sunglasses) |> ignore
        // put a new popup canvas in the root
        let popupCanvas = new Canvas(Width=appMainCanvas.Width, Height=appMainCanvas.Height, Background=Brushes.Transparent, IsHitTestVisible=true, Opacity=1.)
        rootCanvas.Children.Add(popupCanvas) |> ignore
        popupCanvasStack.Push(popupCanvas)
        afterCreatePopupCanvas.Trigger(popupCanvas)
        // return the popup canvas
        popupCanvas
    member _this.DismissPopup() =
        // remove the popup canvas
        let pc = popupCanvasStack.Pop()
        beforeDismissPopupCanvas.Trigger(pc)
        rootCanvas.Children.Remove(pc)
        opacityStack.Pop() |> ignore
        // remove the sunglasses from the prior child
        (rootCanvas.Children.[rootCanvas.Children.Count-1] :?> Canvas).Children.Remove(sunglasses) |> ignore
        // place the sunglasses on the prior prior layer (if there is one)
        if rootCanvas.Children.Count-2 >= 0 then
            sunglasses.Opacity <- opacityStack |> Seq.max
            (rootCanvas.Children.[rootCanvas.Children.Count-2] :?> Canvas).Children.Add(sunglasses) |> ignore
    // and here is how the broadcast window can listen for popup activity
    member _this.AfterCreatePopupCanvas = afterCreatePopupCanvas.Publish
    member _this.BeforeDismissPopupCanvas = beforeDismissPopupCanvas.Publish
    static member TheOnlyCanvasManager with get() = theOnlyCanvasManager.Value

// TODO rename DoModals
let DoModalCore(cm:CanvasManager, wh:System.Threading.ManualResetEvent, placeElementOntoCanvas, removeElementFromCanvas, element:FrameworkElement, blackSunglassesOpacity) = async {
    // rather than use MouseCapture() API, just draw a canvas over entire window which will intercept all mouse gestures
    let c = cm.CreatePopup(blackSunglassesOpacity)
    // place the element
    placeElementOntoCanvas(c, element)
    // catch mouse clicks outside the element
    c.MouseDown.Add(fun ea ->
        let pos = ea.GetPosition(element)
        if (pos.X < 0. || pos.X > element.ActualWidth) || (pos.Y < 0. || pos.Y > element.ActualHeight) then
            if ea.ButtonState = Input.MouseButtonState.Pressed &&
                    (ea.ChangedButton = Input.MouseButton.Left || ea.ChangedButton = Input.MouseButton.Middle || ea.ChangedButton = Input.MouseButton.Right) then
                ea.Handled <- true
                wh.Set() |> ignore
        )
    let ctxt = System.Threading.SynchronizationContext.Current
    let! _ = Async.AwaitWaitHandle(wh)
    do! Async.SwitchToContext(ctxt)
    removeElementFromCanvas(c, element)
    cm.DismissPopup()
    }

let DoModal(cm:CanvasManager, wh:System.Threading.ManualResetEvent, x, y, element) =
    DoModalCore(cm, wh, (fun (c,e) -> canvasAdd(c, e, x, y)), (fun (c,e) -> c.Children.Remove(e)), element, 0.5)

let DoModalDocked(cm:CanvasManager, wh:System.Threading.ManualResetEvent, dock, element) =
    let d = new DockPanel(Width=cm.Width, Height=cm.Height, LastChildFill=false)
    DoModalCore(cm, wh,
                    (fun (c,e) -> 
                        DockPanel.SetDock(e, dock)
                        d.Children.Add(e) |> ignore
                        canvasAdd(c, d, 0., 0.)),
                    (fun (_c,e) -> d.Children.Remove(e)), element, 0.5)

/////////////////////////////////////////

let DoModalMessageBoxCore(cm:CanvasManager, icon:System.Drawing.Icon, mainText, buttonTexts:seq<string>, x, y) = async { // returns buttonText if a button was pressed, or null if dismissed
    let grid = new Grid()
    grid.RowDefinitions.Add(new RowDefinition(Height=GridLength(1.0, GridUnitType.Star)))
    grid.RowDefinitions.Add(new RowDefinition(Height=GridLength.Auto))

    let mainDock = new DockPanel()
    let image = Graphics.BMPtoImage(icon.ToBitmap())
    image.HorizontalAlignment <- HorizontalAlignment.Left
    image.Margin <- Thickness(30.,0.,0.,0.)
    mainDock.Children.Add(image) |> ignore
    DockPanel.SetDock(image, Dock.Left)
    let mainTextBlock = new TextBox(Text=mainText, Background=Brushes.Transparent, BorderThickness=Thickness(0.), IsReadOnly=true, 
                                        TextWrapping=TextWrapping.Wrap, MaxWidth=cm.AppMainCanvas.Width-x-100., Width=System.Double.NaN, 
                                        VerticalAlignment=VerticalAlignment.Center, Margin=Thickness(12.,20.,41.,15.))
    mainDock.Children.Add(mainTextBlock) |> ignore
    grid.Children.Add(mainDock) |> ignore
    Grid.SetRow(mainDock, 0)

    let wh = new System.Threading.ManualResetEvent(false)
    let mutable result = null
    let buttonDock = new DockPanel(Margin=Thickness(5.,0.,0.,0.))
    let mutable first = true
    for bt in buttonTexts |> Seq.rev do
        let b = new Button(MinWidth=88., MaxWidth=200., Height=26., Margin=Thickness(5.), HorizontalAlignment=HorizontalAlignment.Right, HorizontalContentAlignment=HorizontalAlignment.Stretch, VerticalContentAlignment=VerticalAlignment.Stretch)
        if first then
            b.Focus() |> ignore
            first <- false
        DockPanel.SetDock(b, Dock.Right)
        buttonDock.Children.Add(b) |> ignore
        b.Content <- new TextBox(Text=bt, IsReadOnly=true, IsHitTestVisible=false, TextAlignment=TextAlignment.Center, BorderThickness=Thickness(0.), FontSize=16.,
                                    Background=Graphics.almostBlack, Margin=Thickness(0.))
        b.Click.Add(fun _ -> result <- bt; wh.Set() |> ignore)
    grid.Children.Add(buttonDock) |> ignore
    Grid.SetRow(buttonDock, 1)

    let b = new Border(Child=grid, Background=Brushes.Black, BorderThickness=Thickness(5.), BorderBrush=Brushes.Gray, MaxWidth=cm.AppMainCanvas.Width-x-10.)
    let style = new Style(typeof<TextBox>)
    style.Setters.Add(new Setter(TextBox.ForegroundProperty, Brushes.Orange))
    style.Setters.Add(new Setter(TextBox.BackgroundProperty, Brushes.Black))
    style.Setters.Add(new Setter(TextBox.BorderBrushProperty, Brushes.Orange))
    b.Resources.Add(typeof<TextBox>, style)
    let style = new Style(typeof<Button>)
    style.Setters.Add(new Setter(Button.BorderBrushProperty, Brushes.Orange))
    style.Setters.Add(new Setter(Button.BackgroundProperty, Brushes.DarkGray))
    b.Resources.Add(typeof<Button>, style)

    do! DoModal(cm, wh, x, y, b)
    return result
    }
let DoModalMessageBox(cm:CanvasManager, icon:System.Drawing.Icon, mainText, buttonTexts:seq<string>) = 
    DoModalMessageBoxCore(cm, icon, mainText, buttonTexts, 150., 200.)

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
            for _i=0 to n-1 do 
                yield actualSpaceLength / ST
                yield dash
        |]
        a
    let topLine = new Shapes.Line(X1 = -ST, Y1 = -ST/2., X2 = W + ST, Y2 = -ST/2., StrokeThickness=ST, Stroke=brush)
    topLine.StrokeDashArray <- new DoubleCollection(computeDashArray (W+2.*ST))
    canvasAdd(canvas, topLine, 0., 0.)
    let bottomLine = new Shapes.Line(X1 = -ST, Y1 = H+ST/2., X2 = W + ST, Y2 = H+ST/2., StrokeThickness=ST, Stroke=brush)
    bottomLine.StrokeDashArray <- new DoubleCollection(computeDashArray (W+2.*ST))
    canvasAdd(canvas, bottomLine, 0., 0.)
    let leftLine = new Shapes.Line(X1 = -ST/2., Y1 = -ST, X2 = -ST/2., Y2 = H+ST, StrokeThickness=ST, Stroke=brush)
    leftLine.StrokeDashArray <- new DoubleCollection(computeDashArray (H+2.*ST))
    canvasAdd(canvas, leftLine, 0., 0.)
    let rightLine = new Shapes.Line(X1 = W+ST/2., Y1 = -ST, X2 = W+ST/2., Y2 = H+ST, StrokeThickness=ST, Stroke=brush)
    rightLine.StrokeDashArray <- new DoubleCollection(computeDashArray (H+2.*ST))
    canvasAdd(canvas, rightLine, 0., 0.)

// TODO: factor out background brush color (assumes black)
type ModalGridSelectBrushes(originalTileHighlightBrush, gridSelectableHighlightBrush, gridNotSelectableHighlightBrush, borderBrush) =
    member _this.OriginalTileHighlightBrush = originalTileHighlightBrush
    member _this.GridSelectableHighlightBrush = gridSelectableHighlightBrush
    member _this.GridNotSelectableHighlightBrush = gridNotSelectableHighlightBrush
    member _this.BorderBrush = borderBrush
    member this.Dim(opacity) =
        let dim(scb:SolidColorBrush) =
            let c = scb.Color
            let nc = Color.FromArgb(byte(255.*opacity), c.R, c.G, c.B)
            new SolidColorBrush(nc)
        new ModalGridSelectBrushes(dim(this.OriginalTileHighlightBrush), dim(this.GridSelectableHighlightBrush), dim(this.GridNotSelectableHighlightBrush), dim(this.BorderBrush))
    static member Defaults() =
        new ModalGridSelectBrushes(Brushes.Lime, Brushes.Lime, Brushes.Red, Brushes.Gray)

let borderThickness = 3.  // TODO should this be a param?

type PopupClickBehavior<'a> =
    | DismissPopupWithResult of 'a     // return result
    | DismissPopupWithNoResult         // tear down popup as though user clicked outside it
    | StayPoppedUp                     // keep awaiting more clicks

(*
CustomComboBoxes.DoModalGridSelect(cm, tileX, tileY, tileCanvas, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (gnc, gnr, gcw, grh),
    gx, gy, redrawTile, onClick, extraDecorations, brushes, gridClickDismissalDoesMouseWarpBackToTileCenter, who)
*)
let DoModalGridSelect<'State,'Result>
        (cm:CanvasManager, tileX, tileY, tileCanvas:Canvas, // tileCanvas - an empty Canvas with just Width and Height set, one which you will redrawTile your preview-tile
                gridElementsSelectablesAndIDs:(FrameworkElement*bool*'State)[], // array of display elements, whether they're selectable, and your stateID name/identifier for them
                originalStateIndex:int, // originalStateIndex is array index into the array
                activationDelta:int, // activationDelta is -1/0/1 if we should give initial input of scrollup/none/scrolldown
                (gnc, gnr, gcw, grh),   // grid: numCols, numRows, colWidth, rowHeight (heights of elements; this control will add border highlight)
                gx, gy,   // where to place grid (x,y) relative to (0,0) being the (unbordered) corner of your TileCanvas
                redrawTile,  // we pass you currentStateID
                onClick,  // called on tile click or selectable grid click, you choose what to do:   (mousebuttonEA, currentStateID) -> PopupClickBehavior<'Result>
                extraDecorations:seq<FrameworkElement*float*float>,  // extra things to draw at (x,y)s
                brushes:ModalGridSelectBrushes,
                gridClickDismissalDoesMouseWarpBackToTileCenter,
                who:System.Threading.ManualResetEvent option      // pass an unset one, if caller wants to be able to early-dismiss the dialog on its own
                ) = async {
    let wh = match who with Some(x) -> x | _ -> new System.Threading.ManualResetEvent(false)
    let mutable result = None
    let popupCanvas = new Canvas()  // we will draw outside the canvas
    canvasAdd(popupCanvas, tileCanvas, 0., 0.)
    let ST = borderThickness
    MakePrettyDashes(popupCanvas, brushes.OriginalTileHighlightBrush, tileCanvas.Width, tileCanvas.Height, ST, 2., 1.2)
    if gridElementsSelectablesAndIDs.Length > gnr*gnc then
        failwith "the grid is not big enough to accomodate all the choices"
    let grid = makeGrid(gnc, gnr, gcw+2*int ST, grh+2*int ST)
    grid.Background <- Brushes.Black
    let mutable currentState = originalStateIndex   // the only bit of local mutable state during the modal - it ranges from 0..gridElements.Length-1
    let selfCleanup() =
        for (d,_x,_y) in extraDecorations do
            popupCanvas.Children.Remove(d)
    let dismiss() =
        wh.Set() |> ignore
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
    tileCanvas.MouseWheel.Add(fun x -> if x.Delta<0 then next() else prev())
    tileCanvas.MouseDown.Add(fun ea -> 
        ea.Handled <- true
        match onClick(ea, stateID()) with
        | DismissPopupWithResult(r:'Result) -> result <- Some(r); dismiss()
        | DismissPopupWithNoResult -> dismiss()
        | StayPoppedUp -> ()
        )
    tileCanvas.MouseLeave.Add(fun _ -> snapBack())
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
                b.MouseEnter.Add(fun _ -> changeCurrentState(n))
                let redraw() = b.BorderBrush <- (if n = currentState then (if isSelectable then brushes.GridSelectableHighlightBrush else brushes.GridNotSelectableHighlightBrush) else Brushes.Black)
                redrawGridFuncs.Add(redraw)
                redraw()
                let mouseWarpDismiss() =
                    let pos = tileCanvas.TranslatePoint(Point(tileCanvas.Width/2.,tileCanvas.Height/2.), cm.AppMainCanvas)
                    Graphics.WarpMouseCursorTo(pos)
                    dismiss()
                b.MouseDown.Add(fun ea -> 
                    ea.Handled <- true
                    if isSelectable then
                        let dismisser = if gridClickDismissalDoesMouseWarpBackToTileCenter then mouseWarpDismiss else dismiss
                        match onClick(ea, stateID()) with
                        | DismissPopupWithResult(r) -> result <- Some(r); dismisser()
                        | DismissPopupWithNoResult -> dismisser()
                        | StayPoppedUp -> ()
                    )
                gridAdd(grid, b, x, y)
            else
                let dp = new DockPanel(Background=Brushes.Black)
                dp.MouseEnter.Add(fun _ -> snapBack())
                dp.MouseDown.Add(fun ea -> ea.Handled <- true)  // empty grid elements swallow clicks because we don't want to commit or dismiss
                gridAdd(grid, dp, x, y)
    grid.MouseLeave.Add(fun _ -> snapBack())
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
    do! DoModal(cm, wh, tileX, tileY, popupCanvas)
    selfCleanup()
    return result
    }

////////////////////////////////

let placeSkippedItemXDecorationImpl(innerc:Canvas, size) =
    innerc.Children.Add(new Shapes.Line(Stroke=skipped, StrokeThickness=3., X1=0., Y1=0., X2=size, Y2=size)) |> ignore
    innerc.Children.Add(new Shapes.Line(Stroke=skipped, StrokeThickness=3., X1=size, Y1=0., X2=0., Y2=size)) |> ignore
let placeSkippedItemXDecoration(innerc) = placeSkippedItemXDecorationImpl(innerc, 30.)
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
        let c = new Canvas(Width=30., Height=30.)
        let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=color, StrokeThickness=3.0, IsHitTestVisible=false)
        c.Children.Add(rect) |> ignore
        if obj.Equals(color, skipped) then
            placeSkippedItemXDecoration(c)
        p.Children.Add(c) |> ignore
        let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                                Text=text, VerticalAlignment=VerticalAlignment.Center, BorderThickness=Thickness(0.))
        p.Children.Add(tb) |> ignore
        sp.Children.Add(p) |> ignore
    let b = new Border(Background=Brushes.Black, BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Child=d)
    b.MouseDown.Add(fun ea -> ea.Handled <- true)  // absorb mouse clicks, so that clicking explainer decoration does not dismiss popup due to being outside-the-area click
    let fe : FrameworkElement = upcast b
    fe
let itemBoxModalGridSelectBrushes = new ModalGridSelectBrushes(Brushes.Yellow, Brushes.Yellow, new SolidColorBrush(Color.FromRgb(140uy,10uy,0uy)), Brushes.Gray)

let DisplayItemComboBox(cm:CanvasManager, boxX, boxY, boxCellCurrent, activationDelta, originalPlayerHas, callerExtraDecorations) = async {
    let innerc = new Canvas(Width=24., Height=24., Background=Brushes.Black)  // just has item drawn on it, not the box
    let redraw(n) =
        innerc.Children.Clear()
        let bmp = boxCurrentBMP(n, false)
        if bmp <> null then
            let image = Graphics.BMPtoImage(bmp)
            canvasAdd(innerc, image, 1., 1.)
        innerc
    redraw(boxCellCurrent) |> ignore
    let gridElementsSelectablesAndIDs = [|
        for n = 0 to 15 do
            let fe:FrameworkElement = if n=15 then upcast new Canvas() else upcast (boxCurrentBMP(n, false) |> Graphics.BMPtoImage)
            let isSelectable = n = 15 || n=boxCellCurrent || TrackerModel.allItemWithHeartShuffleChoiceDomain.CanAddUse(n)
            let ident = if n=15 then -1 else n
            yield fe, isSelectable, ident
        |]
    let originalStateIndex = if boxCellCurrent = -1 then 15 else boxCellCurrent
    let onClick(ea,ident) =
        // we're getting a click with mouse event args ea on one of the selectable items in the grid, namely ident. take appropriate action.
        DismissPopupWithResult(ident, 
            let newPH = MouseButtonEventArgsToPlayerHas ea
            if ident = -1 then 
                if originalPlayerHas=TrackerModel.PlayerHas.NO && newPH = TrackerModel.PlayerHas.SKIPPED then
                    TrackerModel.PlayerHas.SKIPPED   // middle click an empty box toggles it to white
                else
                    TrackerModel.PlayerHas.NO 
            else 
                newPH)
    let decorationsShouldGoToTheLeft = boxX > Graphics.OMTW*8.
    let gridX, gridY = if decorationsShouldGoToTheLeft then -117., -3. else 27., -3.
    let decoX,decoY = if decorationsShouldGoToTheLeft then -152., 108. else 27., 108.
    let redrawTile(ident) =
        // the user has changed the current selection via mousing or scrolling, redraw the preview tile appropriately to display ident
        let innerc = redraw(ident)
        let s = if ident = -1 then "Unmarked" else TrackerModel.ITEMS.AsDisplayDescription(ident)
        let s = HotKeys.ItemHotKeyProcessor.AppendHotKeyToDescription(s, ident)
        let text = new TextBox(Text=s, Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                                    FontSize=16., HorizontalContentAlignment=HorizontalAlignment.Center)
        let textBorder = new Border(BorderThickness=Thickness(3.), Child=text, Background=Brushes.Black, BorderBrush=Brushes.Gray)
        let dp = new DockPanel(LastChildFill=false)
        dp.Children.Add(textBorder) |> ignore
        innerc.Children.Add(dp) |> ignore
        if decorationsShouldGoToTheLeft then
            DockPanel.SetDock(textBorder, Dock.Right)
            Canvas.SetTop(dp, -3.)
            Canvas.SetRight(dp, 138.)
        else
            DockPanel.SetDock(textBorder, Dock.Right)
            Canvas.SetTop(dp, -3.)
            Canvas.SetLeft(dp, 138.)
    let extraDecorations = [yield itemBoxMouseButtonExplainerDecoration, decoX, decoY; yield! callerExtraDecorations]
    return! DoModalGridSelect(cm, boxX+3., boxY+3., innerc, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (4, 4, 21, 21), gridX, gridY, 
                                redrawTile, onClick, extraDecorations, itemBoxModalGridSelectBrushes, true, None)
    }

let makeVersionButtonWithBehavior(cm:CanvasManager) =
    let vb = Graphics.makeButton(sprintf "v%s" OverworldData.VersionString, Some(12.), Some(Brushes.Orange))
    let mutable popupIsActive = false
    vb.Click.Add(fun _ ->
        if not popupIsActive then
            async {
                let! r = DoModalMessageBox(cm, System.Drawing.SystemIcons.Information, OverworldData.AboutBody, ["Go to website"; "Ok"])
                popupIsActive <- false
                if r = "Go to website" then
                    System.Diagnostics.Process.Start(OverworldData.Website) |> ignore
            } |> Async.StartImmediate
        )
    vb