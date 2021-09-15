module Dungeon

open System.Windows.Media
open System.Windows.Controls

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

let colors = [|
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
    |]
let drawDungeonColorGrid(appMainCanvas:Canvas) =
    let w = colors.Length/3
    let grid = Graphics.makeGrid(w, 3, 30, 30)
    for i = 0 to w-1 do
        for j = 0 to 2 do
            let color = colors.[i*3+j]
            let r = (color &&& 0xFF0000) / 0x10000
            let g = (color &&& 0x00FF00) / 0x100
            let b = (color &&& 0x0000FF) / 0x1
            let brush = new SolidColorBrush(Color.FromRgb(byte r, byte g, byte b))
            Graphics.gridAdd(grid, new DockPanel(Background=brush), i, j)
    Graphics.canvasAdd(appMainCanvas, grid, 100., 400.)

    // ABCDEFGH across top (black on gray to start?)
    // clicking one brings up color picker, as hover, a big swatch shows below picker; when chosen, letter changes for good contrast
    // (DoModal might wants to return a dismiss() function, which you can call if you are done - ought it invoke your onClose()?)
    // color projection onto overworld tiles; overworld tile icons changing from 1-8 to A-H
    // color projection onto LEVEL-N (use contrast color for LEVEL text)
    // triforce numbering being '?' to start
    // LEVEL-N being LEVEL-A or whatnot, BLOCKERS 1-8 being A-H, dungeon tab names being A-H
    // some kind of letter-number associator, updates triforce numeral, some logic regarding 3rd item box?
    // all 8 dungeons having 3 boxes (HFQ/HSQ move elsewhere)
    // FQ and SQ highlights would need a bit of rework
    // use alphanumerics compositing for dungeon/anyroad overworld tiles


