module CustomComboBoxes

open System.Windows.Controls
open System.Windows.Media
open System.Windows

let canvasAdd = Graphics.canvasAdd
let gridAdd = Graphics.gridAdd
let makeGrid = Graphics.makeGrid

(*

A possible way to 'save the UI' is to get rid of scrolling
 - a custom combo-box selector lets you click a box to reveal a set of selections, and click again to select
    - the modality is not as UI-ugly as modal windows are
    - the modality prevents the 'unknown intention' issue of not knowing if the user is mid-scroll or done-scrolling, to know when to take semantic action

Item-box-color is a somewhat (but not entirely) orthogonal issue
 - ZHelper and lazy/speedy folks won't want to set the box color
 - setting green-by-default on scroll creates semantic-ambiguity issues
 - setting green-by-default on hotkey/combobox select is better, except the person who learns an item via hint might not realize semantics of 'green box' (or existence of red/purple populated box)
 - a possible solution is to cause first-edit of an item box to set it to the 'blinking yellow' state, which can then be resolved green/red/purple by left/right/middle clicking, as before
    - people will hate blinking yellow, and do whatever they can to get rid of it, which helps with green/red state discovery
    - muscle memory will soon kick in to do the right thing



[3:48 PM] Lorgon111: Actually, having drawn a state-transition diagram on paper, I realize the the Grid/Radial menu popup is actually orthogonal to the 'solve the scroll crisis for item boxes' issue.  
I can remove the popup menu entirely and it still would behave the same:
 - when you click or scroll an empty item box, it puts you into a modal UI.  I'd probably put a black canvas with 50% opacity over the whole UI, except the item box (and its popup, if doing that)
 - while in the modal UI, the box outline color is bright yellow, which is 'indeterminate state'.  This is View only, the TrackerModel is unaware that anything is changing.
 - if you click outside the item box (or its popup, if doing that), then this gets treated as a 'dismiss modal dialog and undo'.  You revert to the pre-modal state and exit the mode.
 - if you scroll wheel (or click a grid item in the popup), it changes the View to display the newly selected item in the box (and highlight the new grid square in the popup), but keeps the yellow outline & modality, and still does not update TrackerModel
 - at any time, if you click inside the item box itself (or click a currently highlighted grid item (which would often be a second consecutive click inside the popup)), it behaves as a 'commit'
       - the type of final click (left, right, middle) determines the committed box outline color (green, red, purple)

Now here is the series of gestures for common interactions:
 - "I got the ladder from dungeon 4": (1) hover the D4 item box (2) scroll to the ladder (3) left click it - this is the exact same set of interactions as today
 - "I got a hint the ladder is in D4": (1) hover the D4 item box (2) scroll to the ladder (3) right click it - this has one extra step (#3) from today
 - "After the prior hint, I went to D4 and got that ladder": (1) hover D4 box with ladder (2) click it (3) left click it again - this has one extra step (#3) from today
Could argue that non-empty boxes should be non-modal for clicks, as today, hm.(edited)

[3:56 PM] Lorgon111: I guess for non-empty boxes, there are two classes of reasons to interact with them:
 - (1) it was a previously hinted item, it is currently a red box, and now I intend to make it green (or purple) because I got (intentionally avoided) the item
 - (2) the previous mark is erroneous:
       - (a) I selected and committed the wrong item, and need to change it.  Scrolling will clearly put you back in the modal state and you can apply a fix
       - (b) I used the wrong click to commit and the box is the wrong color and I want to change it
Based on looking at those, it feels like for both (1) and (2b) it would be fine for clicks to be non-modal and behave as today.  The only time a click would put you in a mode is if you click an 
empty item box, as clicking an empty item box is kind of non-sensical so it makes sense to ask you to do the extra effort of dealing with the mode.

These interactions would not need to prevent you from creating green or purple empty boxes.  I do not know why you would want to do this, but I don't think it's worth extra edge case behavior 
to try to exclude it, and maybe folks will give such marks an ad-hoc meaning.(edited)

[4:08 PM] Lorgon111: The advantage of the popup is that having scrolling cause "modal black out" of almost the entire UI will be jarring.  The popup kind of adds context, especially for the 
first-time user, about what just happened.  The main purpose of the popup might be to 'teach the mode' and the expectation would be most users will hate it and go look for the option to turn 
the popup off.  But they will have learned the mode in the process.

The key design issue for me, I think, is if I can make the mode sufficiently obvious and non-jarring.  If you don't 'black out' the rest of the UI enough, then the naive user may not notice 
the mode, click elsewhere, not notice the 'dismiss-undo', and later wonder what happened to their scroll mark.  But if you do black out the rest of the UI enough, it's a bit jarring and 
screen-flashy maybe?  I guess I need to try it out with different opacities to understand.  And this is also the reason to consider 'blinking yellow' for the modal/indeterminate itemBox 
border color - the blinking makes it clearer that 'you still have business here to finish' even if the subtle-blackout of the remaining UI didn't clue it.

Just got to mock it up and see how it looks.(edited)

[4:13 PM] Lorgon111: I think I will implement the popup, regardless, for those who may prefer "click-move-click-click" to "click-scroooooollllllll-click". But there will be an option to not 
display the popup, for those who find it distracting.(edited)

[4:16 PM] Lorgon111: The previous paragraphs are entirely about the item boxes.  The design may inform the design for overworld tiles and dungeon boxes, but those elements are different, in that
overworld tiles do not have a click behavior, and dungeon boxes do not have TrackerModel semantics.

*)

////////////////////////////////////////////////////////////
// ItemComboBox

let MouseButtonEventArgsToPlayerHas(ea:Input.MouseButtonEventArgs) =
    if ea.ChangedButton = Input.MouseButton.Left then TrackerModel.PlayerHas.YES
    elif ea.ChangedButton = Input.MouseButton.Right then TrackerModel.PlayerHas.NO
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

let DoModalCore(appMainCanvas:Canvas, placeElementOntoCanvas, removeElementFromCanvas, element:FrameworkElement, onClose) =
    // rather than use MouseCapture() API, just draw a canvas over entire window which will intercept all mouse gestures
    let c = new Canvas(Width=appMainCanvas.Width, Height=appMainCanvas.Height, Background=Brushes.Transparent, IsHitTestVisible=true, Opacity=1.)
    appMainCanvas.Children.Add(c) |> ignore
    let sunglasses = new Canvas(Width=appMainCanvas.Width, Height=appMainCanvas.Height, Background=Brushes.Black, IsHitTestVisible=false, Opacity=0.5)
    c.Children.Add(sunglasses) |> ignore
    // place the element
    placeElementOntoCanvas(c, element)
    let dismiss() =
        removeElementFromCanvas(c, element)
        appMainCanvas.Children.Remove(c)
    // catch mouse clicks outside the element
    c.MouseDown.Add(fun ea ->
        let pos = ea.GetPosition(element)
        if (pos.X < 0. || pos.X > element.ActualWidth) || (pos.Y < 0. || pos.Y > element.ActualHeight) then
            if ea.ButtonState = Input.MouseButtonState.Pressed &&
                    (ea.ChangedButton = Input.MouseButton.Left || ea.ChangedButton = Input.MouseButton.Middle || ea.ChangedButton = Input.MouseButton.Right) then
                onClose()
                dismiss()
        )
    dismiss // return a dismissal handle, which the caller can use to dismiss the dialog based on their own criteria; note that onClose() is not called by the dismissal handle

let DoModal(appMainCanvas:Canvas, x, y, element, onClose) =
    DoModalCore(appMainCanvas, (fun (c,e) -> canvasAdd(c, e, x, y)), (fun (c,e) -> c.Children.Remove(e)), element, onClose)

let DoModalDocked(appMainCanvas:Canvas, dock, element, onClose) =
    let d = new DockPanel(Width=appMainCanvas.Width, Height=appMainCanvas.Height, LastChildFill=false)
    DoModalCore(appMainCanvas, 
                    (fun (c,e) -> 
                        DockPanel.SetDock(e, dock)
                        d.Children.Add(e) |> ignore
                        canvasAdd(c, d, 0., 0.)),
                    (fun (c,e) -> d.Children.Remove(e)), element, onClose)

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
    static member Defaults() =
        new ModalGridSelectBrushes(Brushes.Lime, Brushes.Lime, Brushes.Red, Brushes.Gray)
let borderThickness = 3.  // TODO should this be a param?
let DoModalGridSelect<'a>(appMainCanvas, tileX, tileY, tileCanvas:Canvas, // tileCanvas - an empty Canvas with just Width and Height set, one which you will redrawTile your preview-tile
                            gridElementsSelectablesAndIDs:(FrameworkElement*bool*'a)[], // array of display elements, whether they're selectable, and your stateID name/identifier for them
                            originalStateIndex:int, // originalStateIndex is array index into the array
                            activationDelta:int, // activationDelta is -1/0/1 if we should give initial input of scrollup/none/scrolldown
                            (gnc, gnr, gcw, grh),   // grid: numCols, numRows, colWidth, rowHeight (heights of elements; this control will add border highlight)
                            gx, gy,   // where to place grid (x,y) relative to (0,0) being the (unbordered) corner of your TileCanvas
                            onClick,  // called on tile click or selectable grid click, you choose what to do:   (dismissPopupFunc, mousebuttonEA, currentStateID) -> unit
                            redrawTile,  // we pass you currentStateID
                            onClose,
                            extraDecoration:option<FrameworkElement*float*float>,  // extra thing to draw at an (x,y)
                            brushes:ModalGridSelectBrushes
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
    let mutable dismissPopup = fun () -> ()
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
        onClick(dismissPopup, ea, stateID())
        )
    tileCanvas.MouseLeave.Add(fun _ -> snapBack())
    // grid of choices
    for x = 0 to gnc-1 do
        for y = 0 to gnr-1 do
            let n = y*gnc + x
            let icon,isSelectable,_ = if n < gridElementsSelectablesAndIDs.Length then gridElementsSelectablesAndIDs.[n] else null,false,Unchecked.defaultof<_>
            if icon <> null then
                let c = new Canvas()
                c.Children.Add(icon) |> ignore
                if not(isSelectable) then // grey out
                    c.Children.Add(new Canvas(Width=float gcw, Height=float grh, Background=Brushes.Black, Opacity=0.6, IsHitTestVisible=false)) |> ignore
                let b = new Border(BorderThickness=Thickness(ST), Child=c)
                b.MouseEnter.Add(fun _ -> changeCurrentState(n))
                let redraw() = b.BorderBrush <- (if n = currentState then (if isSelectable then brushes.GridSelectableHighlightBrush else brushes.GridNotSelectableHighlightBrush) else Brushes.Black)
                redrawGridFuncs.Add(redraw)
                redraw()
                b.MouseDown.Add(fun ea -> 
                    ea.Handled <- true
                    if isSelectable then
                        onClick(dismissPopup, ea, stateID())
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
    match extraDecoration with
    | None -> ()
    | Some(d,x,y) -> canvasAdd(popupCanvas, d, x, y)
    // initial input
    match activationDelta with
    | -1 -> prev()
    |  0 -> ()
    |  1 -> next()
    | _ -> failwith "bad activationDelta"
    // activate the modal
    dismissPopup <- DoModal(appMainCanvas, tileX, tileY, popupCanvas, onClose)

////////////////////////////////

let DisplayItemComboBox(appMainCanvas:Canvas, boxX, boxY, boxCellCurrent, activationDelta, isCurrentlyBook, commitFunction) =
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
            let fe:FrameworkElement = if n=15 then upcast new Canvas() else upcast (boxCurrentBMP(isCurrentlyBook, n, false) |> Graphics.BMPtoImage)
            let isOriginallyCurrent = n=boxCellCurrent
            let isSelectable = n = 15 || isOriginallyCurrent || TrackerModel.allItemWithHeartShuffleChoiceDomain.CanAddUse(n)
            let ident = if n=15 then -1 else n
            yield fe, isSelectable, ident
        |]
    let originalStateIndex = if boxCellCurrent = -1 then 15 else boxCellCurrent
    let onClick(dismissPopup,ea,ident) =
        // we're getting a click with mouse event args ea on one of the selectable items in the grid, namely ident. take appropriate action.
        dismissPopup()
        commitFunction(ident, MouseButtonEventArgsToPlayerHas ea)
    let redrawTile(ident) =
        // the user has changed the current selection via mousing or scrolling, redraw the preview tile appropriately to display ident
        redraw(ident)
    let onClose() =
        // the user clicked outside the modal dialog, it is dismissing itself, do any final cleanup we need to do
        ()
    let extraDecoration =
        Some(
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
                let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=color, StrokeThickness=3.0, IsHitTestVisible=false)
                p.Children.Add(rect) |> ignore
                let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, Text=text, VerticalAlignment=VerticalAlignment.Center, BorderThickness=Thickness(0.))
                p.Children.Add(tb) |> ignore
                sp.Children.Add(p) |> ignore
            let b = new Border(Background=Brushes.Black, BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Child=d)
            let fe : FrameworkElement = upcast b
            fe, -3., 138.  // TODO what is Y
            )
    DoModalGridSelect(appMainCanvas, boxX+3., boxY+3., innerc, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (4, 4, 21, 21), -3., 27., 
        onClick, redrawTile, onClose, extraDecoration, new ModalGridSelectBrushes(Brushes.Yellow, Brushes.Yellow, new SolidColorBrush(Color.FromRgb(140uy,10uy,0uy)), Brushes.Gray))
