module Dungeon

open Avalonia.Media
open Avalonia.Controls
open Avalonia
open Avalonia.Layout

[<RequireQualifiedAccess>]
type DelayedPopupState =
    | NONE
    | SOON
    | ACTIVE_NOW

// door colors
let unknown = new SolidColorBrush(Color.FromRgb(30uy, 30uy, 45uy)) :> Brush
let no = new SolidColorBrush(Color.FromRgb(105uy, 0uy, 0uy)) :> Brush
let yes = new SolidColorBrush(Color.FromRgb(60uy,120uy,60uy)) :> Brush
let blackedOut = new SolidColorBrush(Color.FromRgb(15uy, 15uy, 25uy))  :> Brush

[<RequireQualifiedAccess>]
type DoorState = | UNKNOWN | NO | YES | BLACKEDOUT

type Door(state:DoorState, redraw) =
    let mutable state = state
    member _this.State with get() = state and set(x) = state <- x; redraw(x)

type GrabHelper() =
    let mutable isGrabMode = false
    let mutable grabMouseX, grabMouseY = -1, -1
    let grabContiguousRooms = Array2D.zeroCreate 8 8
    let mutable roomStatesIfGrabWereACut = null
    let mutable roomIsCircledIfGrabWereACut = null
    let mutable roomCompletedIfGrabWereACut = null
    let mutable horizontalDoorsIfGrabWereACut = null
    let mutable verticalDoorsIfGrabWereACut = null

    member this.PreviewGrab(mouseX, mouseY, roomStates:int[,]) =
        if not this.IsGrabMode || this.HasGrab then
            failwith "bad"
        if roomStates.[mouseX,mouseY] = 0 then
            failwith "bad"
        // compute set of contiguous rooms
        let contiguous = Array2D.zeroCreate 8 8
        let q = new System.Collections.Generic.Queue<_>()
        q.Enqueue(mouseX, mouseY)
        while q.Count > 0 do
            let roomX,roomY = q.Dequeue()
            contiguous.[roomX,roomY] <- true
            let unvisited(x,y) = x>=0 && x<=7 && y>=0 && y<=7 && not contiguous.[x,y]
            let attempt(x,y) = if unvisited(x,y) && roomStates.[x,y]<>0 && roomStates.[x,y]<>10 then q.Enqueue(x,y)
            attempt(roomX-1,roomY)
            attempt(roomX+1,roomY)
            attempt(roomX,roomY-1)
            attempt(roomX,roomY+1)
        // return them
        contiguous
    member this.StartGrab(mouseX, mouseY, roomStates:int[,], roomIsCircled:bool[,], roomCompleted:bool[,], horizontalDoors:Door[,], verticalDoors:Door[,]) =
        let contigous = this.PreviewGrab(mouseX, mouseY, roomStates)
        // save them
        grabMouseX <- mouseX
        grabMouseY <- mouseY
        contigous |> Array2D.iteri (fun x y v -> grabContiguousRooms.[x,y] <- v)
        roomStatesIfGrabWereACut <- Array2D.init 8 8 (fun x y -> if contigous.[x,y] then 0 else roomStates.[x,y])
        roomIsCircledIfGrabWereACut <- Array2D.init 8 8 (fun x y -> if contigous.[x,y] then false else roomIsCircled.[x,y])
        roomCompletedIfGrabWereACut <- Array2D.init 8 8 (fun x y -> if contigous.[x,y] then false else roomCompleted.[x,y])
        horizontalDoorsIfGrabWereACut <- Array2D.init 7 8 (fun x y -> if contigous.[x,y] || contigous.[x+1,y] then DoorState.UNKNOWN else horizontalDoors.[x,y].State)
        verticalDoorsIfGrabWereACut <- Array2D.init 8 7 (fun x y -> if contigous.[x,y] || contigous.[x,y+1] then DoorState.UNKNOWN else verticalDoors.[x,y].State)
        contigous

    member this.PreviewDrop(mouseX, mouseY, roomStates:int[,]) =
        if not this.IsGrabMode || not this.HasGrab then
            failwith "bad"
        let dx,dy = mouseX-grabMouseX, mouseY-grabMouseY
        let contiguousOk = Array2D.zeroCreate 8 8
        let contiguousWarn = Array2D.zeroCreate 8 8
        for x = 0 to 7 do
            for y = 0 to 7 do
                let i,j = x-dx, y-dy
                if i>=0 && i<=7 && j>=0 && j<=7 then
                    if grabContiguousRooms.[i,j] then
                        if roomStatesIfGrabWereACut.[x,y]<>0 then
                            contiguousWarn.[x,y] <- true
                        else
                            contiguousOk.[x,y] <- true
        contiguousOk, contiguousWarn

    member this.DoDrop(mouseX, mouseY, roomStates:int[,], roomIsCircled:bool[,], roomCompleted:bool[,], horizontalDoors:Door[,], verticalDoors:Door[,]) =  // mutates arrays
        if not this.IsGrabMode || not this.HasGrab then
            failwith "bad"
        let dx,dy = mouseX-grabMouseX, mouseY-grabMouseY
        let oldRoomStates = roomStates.Clone() :?> int[,]
        let oldRoomIsCircled = roomIsCircled.Clone() :?> bool[,]
        let oldRoomCompleted = roomCompleted.Clone() :?> bool[,]
        let oldHorizontalDoors = horizontalDoors |> Array2D.map (fun c -> c.State)
        let oldVerticalDoors = verticalDoors |> Array2D.map (fun c -> c.State)
        roomStatesIfGrabWereACut |> Array2D.iteri (fun x y v -> roomStates.[x,y] <- v)
        roomIsCircledIfGrabWereACut |> Array2D.iteri (fun x y v -> roomIsCircled.[x,y] <- v)
        roomCompletedIfGrabWereACut |> Array2D.iteri (fun x y v -> roomCompleted.[x,y] <- v)
        horizontalDoorsIfGrabWereACut |> Array2D.iteri (fun x y v -> horizontalDoors.[x,y].State <- v)
        verticalDoorsIfGrabWereACut |> Array2D.iteri (fun x y v -> verticalDoors.[x,y].State <- v)
        for x = 0 to 7 do
            for y = 0 to 7 do
                let i,j = x-dx, y-dy
                if i>=0 && i<=7 && j>=0 && j<=7 then
                    if grabContiguousRooms.[i,j] then
                        roomStates.[x,y] <- oldRoomStates.[i,j]
                        roomIsCircled.[x,y] <- oldRoomIsCircled.[i,j]
                        roomCompleted.[x,y] <- oldRoomCompleted.[i,j]
                        let do_door(target:Door[,], x, y, source:DoorState[,], i, j) =
                            if source.[i,j] = DoorState.YES || source.[i,j] = DoorState.NO then
                                target.[x,y].State <- source.[i,j]
                        if x<7 && i<7 then
                            do_door(horizontalDoors,x,y,oldHorizontalDoors,i,j)  // door right of room
                        if x>0 && i>0 then
                            do_door(horizontalDoors,x-1,y,oldHorizontalDoors,i-1,j)  // door left of room
                        if y<7 && j<7 then
                            do_door(verticalDoors,x,y,oldVerticalDoors,i,j)  // door below room
                        if y>0 && j>0 then
                            do_door(verticalDoors,x,y-1,oldVerticalDoors,i,j-1)  // door above room
        this.Abort()


    member this.ToggleGrabMode() = isGrabMode <- not isGrabMode
    member this.IsGrabMode = isGrabMode
    member this.HasGrab = grabMouseX <> -1
    member this.Abort() =
        isGrabMode <- false
        grabMouseX <- -1
        grabMouseY <- -1
        for x = 0 to 7 do
            for y = 0 to 7 do
                grabContiguousRooms.[x,y] <- false
        roomStatesIfGrabWereACut <- null
        roomIsCircledIfGrabWereACut <- null
        roomCompletedIfGrabWereACut <- null
    member this.Log() =
        printfn "log"

///////////////////////////////////////////

let flatColorArray = [|
    // dungeon colors
    0x808080
    0xADADAD
    0xE0E0E0

    0x0050F1
    0x4BA0FF
    0xB6D8FF
    
    0x3B34FF
    0x8A84FF
    0xD0CDFF
    
    0x8022E8
    0xD172FF
    0xEDC6FF
    
    0xBB1EA5
    0xFF6DF7
    0xFFC4FC
    
    0xDB294E
    0xFF799E
    0xFFC8D8
    
    0xD74000
    0xFF9047
    0xFFD2B4
    
    0xB15E00
    0xFFAE0A
    0xFFDE9C
    
    0x737900
    0xC4CA00
    0xE7E994
    
    0x2D8B00
    0x7DDC13
    0xCAF19F

    0x008F08
    0x41E157
    0xB2F3BB

    0x008460
    0x21D5B0
    0xA5EEDF

    0x006DB5
    0x25BEFF
    0xA6E5FF

    0x000000   // add black as default
    0x000000   
    0x000000   
    |]
let threeTallColorArray = 
    let a = ResizeArray()
    let mutable i = 0
    while i < flatColorArray.Length do
        a.Add(flatColorArray.[i])
        i <- i + 3
    i <- 1
    while i < flatColorArray.Length do
        a.Add(flatColorArray.[i])
        i <- i + 3
    i <- 2
    while i < flatColorArray.Length do
        a.Add(flatColorArray.[i])
        i <- i + 3
    a.ToArray()

let makeColor = Graphics.makeColor
let canvasAdd = Graphics.canvasAdd

let HiddenDungeonColorChooserPopup(appMainCanvas, tileX, tileY, tileW, tileH, originalColor, dungeonIndex, onClose) =
    let colors = threeTallColorArray
    let tileCanvas = new Canvas(Width=tileW, Height=tileH)
    let gridElementsSelectablesAndIDs = colors |> Array.mapi (fun i c -> new Canvas(Width=30., Height=30., Background=new SolidColorBrush(makeColor(c))) :> Control, true, i)
    let originalStateIndex = match colors |> Array.tryFindIndex (fun c -> c=originalColor) with Some i -> i | None -> colors.Length-1
    tileCanvas.Background <- new SolidColorBrush(makeColor(colors.[originalStateIndex]))
    let activationDelta = 0
    let gnc = colors.Length / 3
    let gnr = 3
    let gcw,grh = 30,30
    let gx,gy = -100., tileH+20.
    let redrawTile(i) = tileCanvas.Background <- new SolidColorBrush(makeColor(colors.[i]))
    let onClick(dismiss, _ea, state) =
        TrackerModel.GetDungeon(dungeonIndex).Color <- colors.[state]
        dismiss()
        onClose()
    let extraDecorations = []
    let brushes=CustomComboBoxes.ModalGridSelectBrushes.Defaults()
    let gridClickDismissalDoesMouseWarpBackToTileCenter = false
    CustomComboBoxes.DoModalGridSelect(appMainCanvas, tileX, tileY, tileCanvas, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (gnc, gnr, gcw, grh),
        gx, gy, redrawTile, onClick, onClose, extraDecorations, brushes, gridClickDismissalDoesMouseWarpBackToTileCenter)

let HiddenDungeonNumberChooserPopup(appMainCanvas, tileX, tileY, tileW, tileH, originalLabelChar:char, dungeonIndex, onClose) =
    let tileCanvas = new Canvas(Width=tileW, Height=tileH, Background=Brushes.Black)
    let dp = new DockPanel(Width=tileW, Height=tileH)
    canvasAdd(tileCanvas, dp, 0., 0.)
    let mkTxt(ch) =
        new TextBox(Width=60., Height=60., FontSize=36., Foreground=Brushes.White, Background=Brushes.Black, IsHitTestVisible=false, 
                    BorderThickness=Thickness(0.), Text=sprintf "%c" ch, VerticalContentAlignment=VerticalAlignment.Center, HorizontalContentAlignment=HorizontalAlignment.Center,
                    VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center)
    let theTB = mkTxt(originalLabelChar)
    dp.Children.Add(theTB) |> ignore
    let gridElementsSelectablesAndIDs = [|
        for ch in "?12345678" do
            yield (mkTxt(ch) :> Control), true, ch
        |]
    let originalStateIndex = 
        let i = "?12345678".IndexOf(originalLabelChar)
        if i = -1 then 0 else i
    let activationDelta = 0
    let gnc,gnr = 3,3
    let gcw,grh = 60,60
    let gx,gy = -100., tileH+20.
    let redrawTile(ch) = theTB.Text <- sprintf "%c" ch
    let onClick(dismiss, _ea, ch) =
        TrackerModel.GetDungeon(dungeonIndex).LabelChar <- ch
        dismiss()
        onClose()
    let extraDecorations = []
    let brushes=CustomComboBoxes.ModalGridSelectBrushes.Defaults()
    let gridClickDismissalDoesMouseWarpBackToTileCenter = false
    CustomComboBoxes.DoModalGridSelect(appMainCanvas, tileX, tileY, tileCanvas, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (gnc, gnr, gcw, grh),
        gx, gy, redrawTile, onClick, onClose, extraDecorations, brushes, gridClickDismissalDoesMouseWarpBackToTileCenter)

let HiddenDungeonCustomizerPopup(appMainCanvas, dungeonIndex, curColor, curLabel, onClose) =
    // setup main visual tree
    let mainDock = new DockPanel(Background=Brushes.Black)
    
    let text = sprintf "Dungeon %c" "ABCDEFGH".[dungeonIndex]
    let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=24., Text=text, IsHitTestVisible=false, 
                            HorizontalContentAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), Margin=Thickness(0.,0.,0.,20.))
    mainDock.Children.Add(tb) |> ignore
    DockPanel.SetDock(tb, Dock.Top)

    let innerDock = new DockPanel(LastChildFill=false)
    let button1Content = new Canvas(Width=60., Height=60., Background=new SolidColorBrush(makeColor(curColor)))
    let button1 = new Button(Content=button1Content, Margin=Thickness(20.,0.,20.,0.), BorderThickness=Thickness(5.))
    let button2Content = new TextBox(Width=60., Height=60., IsHitTestVisible=false, FontSize=36., Foreground=Brushes.White, Background=Brushes.Black, 
                                        Text=sprintf "%c" curLabel, HorizontalContentAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.))
    let button2 = new Button(Content=button2Content, Margin=Thickness(20.,0.,20.,0.), BorderThickness=Thickness(5.), HorizontalAlignment=HorizontalAlignment.Center)
    let b1a = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text="Change", IsHitTestVisible=false, 
                            HorizontalContentAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), HorizontalAlignment=HorizontalAlignment.Center)
    let b1b = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text="Color", IsHitTestVisible=false, 
                            HorizontalContentAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), HorizontalAlignment=HorizontalAlignment.Center)
    let b1sp = new StackPanel(Orientation=Orientation.Vertical)
    b1sp.Children.Add(b1a) |> ignore
    b1sp.Children.Add(button1) |> ignore
    b1sp.Children.Add(b1b) |> ignore
    let b2a = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text="Change", IsHitTestVisible=false, 
                            HorizontalContentAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), HorizontalAlignment=HorizontalAlignment.Center)
    let b2b = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text="Number", IsHitTestVisible=false, 
                            HorizontalContentAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), HorizontalAlignment=HorizontalAlignment.Center)
    let b2sp = new StackPanel(Orientation=Orientation.Vertical)
    b2sp.Children.Add(b2a) |> ignore
    b2sp.Children.Add(button2) |> ignore
    b2sp.Children.Add(b2b) |> ignore

    innerDock.Children.Add(b1sp) |> ignore
    DockPanel.SetDock(button1, Dock.Left)
    innerDock.Children.Add(b2sp) |> ignore
    DockPanel.SetDock(button2, Dock.Right)

    mainDock.Children.Add(innerDock) |> ignore

    let theBorder = new Border(BorderBrush=Brushes.Black, BorderThickness=Thickness(20.), Child=mainDock)
    let theBorder = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(5.), Child=theBorder)

    // hook up the button actions
    let mutable dismissSelf = fun() -> ()
    let close() =
        onClose()
        // TODO this should be an option based on the caller, as this dialog might be triggered from other places
        //Graphics.Win32.SetCursor(float dungeonIndex*30.+15., 15.)
        //Graphics.PlaySoundForSpeechRecognizedAndUsedToMark()

    let mutable popupIsActive = false
    button1.Click.Add(fun _ ->
        if not popupIsActive then
            popupIsActive <- true
            let pos = button1Content.TranslatePoint(Point(), appMainCanvas).Value
            HiddenDungeonColorChooserPopup(appMainCanvas, pos.X, pos.Y, button1Content.Width, button1Content.Height, curColor, dungeonIndex, 
                                            (fun () -> 
                                                dismissSelf()
                                                close()
                                                popupIsActive <- false))
        )
    button2.Click.Add(fun _ ->
        if not popupIsActive then
            popupIsActive <- true
            let pos = button2Content.TranslatePoint(Point(), appMainCanvas).Value
            HiddenDungeonNumberChooserPopup(appMainCanvas, pos.X, pos.Y, button2Content.Width, button2Content.Height, curLabel, dungeonIndex, 
                                            (fun () -> 
                                                dismissSelf()
                                                close()
                                                popupIsActive <- false))
        )

    // add main element and extra decorations 
    let popupCanvas = new Canvas()
    canvasAdd(popupCanvas, theBorder, 0., 0.)
    
    let mainX,mainY = 150.,150.

    let dungeonColorCanvas = new Canvas(Width=30., Height=30.)
    canvasAdd(popupCanvas, dungeonColorCanvas, float dungeonIndex*30. - mainX, 0. - mainY)
    CustomComboBoxes.MakePrettyDashes(dungeonColorCanvas, Brushes.Lime, 30., 30., 3., 2., 1.)

    let guideline = new Shapes.Line(StartPoint=Point(float dungeonIndex*30. - mainX + 15., 36. - mainY), EndPoint=Point(0.,0.), Stroke=Brushes.Gray, StrokeThickness=3.)
    canvasAdd(popupCanvas, guideline, 0., 0.)

    dismissSelf <- CustomComboBoxes.DoModal(appMainCanvas, mainX, mainY, popupCanvas, close)
    dismissSelf
