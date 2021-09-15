module Dungeon

open Avalonia.Media
open Avalonia.Controls

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