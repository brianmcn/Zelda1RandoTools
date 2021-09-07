module Dungeon

open System.Windows.Media
open System.Windows.Controls

// door colors
let unknown = new SolidColorBrush(Color.FromRgb(40uy, 40uy, 50uy)) :> Brush
let no = System.Windows.Media.Brushes.DarkRed :> Brush
let yes = new SolidColorBrush(Color.FromRgb(0uy,180uy,0uy)) :> Brush
let blackedOut = new SolidColorBrush(Color.FromRgb(15uy, 15uy, 25uy))  :> Brush

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
    member this.StartGrab(mouseX, mouseY, roomStates:int[,], roomIsCircled:bool[,], roomCompleted:bool[,], horizontalDoors:Canvas[,], verticalDoors:Canvas[,]) =
        let contigous = this.PreviewGrab(mouseX, mouseY, roomStates)
        // save them
        grabMouseX <- mouseX
        grabMouseY <- mouseY
        contigous |> Array2D.iteri (fun x y v -> grabContiguousRooms.[x,y] <- v)
        roomStatesIfGrabWereACut <- Array2D.init 8 8 (fun x y -> if contigous.[x,y] then 0 else roomStates.[x,y])
        roomIsCircledIfGrabWereACut <- Array2D.init 8 8 (fun x y -> if contigous.[x,y] then false else roomIsCircled.[x,y])
        roomCompletedIfGrabWereACut <- Array2D.init 8 8 (fun x y -> if contigous.[x,y] then false else roomCompleted.[x,y])
        horizontalDoorsIfGrabWereACut <- Array2D.init 7 8 (fun x y -> if contigous.[x,y] || contigous.[x+1,y] then unknown else horizontalDoors.[x,y].Background)
        verticalDoorsIfGrabWereACut <- Array2D.init 8 7 (fun x y -> if contigous.[x,y] || contigous.[x,y+1] then unknown else verticalDoors.[x,y].Background)
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

    member this.DoDrop(mouseX, mouseY, roomStates:int[,], roomIsCircled:bool[,], roomCompleted:bool[,], horizontalDoors:Canvas[,], verticalDoors:Canvas[,]) =  // mutates arrays
        if not this.IsGrabMode || not this.HasGrab then
            failwith "bad"
        let dx,dy = mouseX-grabMouseX, mouseY-grabMouseY
        let oldRoomStates = roomStates.Clone() :?> int[,]
        let oldRoomIsCircled = roomIsCircled.Clone() :?> bool[,]
        let oldRoomCompleted = roomCompleted.Clone() :?> bool[,]
        let oldHorizontalDoors = horizontalDoors |> Array2D.map (fun c -> c.Background)
        let oldVerticalDoors = verticalDoors |> Array2D.map (fun c -> c.Background)
        roomStatesIfGrabWereACut |> Array2D.iteri (fun x y v -> roomStates.[x,y] <- v)
        roomIsCircledIfGrabWereACut |> Array2D.iteri (fun x y v -> roomIsCircled.[x,y] <- v)
        roomCompletedIfGrabWereACut |> Array2D.iteri (fun x y v -> roomCompleted.[x,y] <- v)
        horizontalDoorsIfGrabWereACut |> Array2D.iteri (fun x y v -> horizontalDoors.[x,y].Background <- v)
        verticalDoorsIfGrabWereACut |> Array2D.iteri (fun x y v -> verticalDoors.[x,y].Background <- v)
        for x = 0 to 7 do
            for y = 0 to 7 do
                let i,j = x-dx, y-dy
                if i>=0 && i<=7 && j>=0 && j<=7 then
                    if grabContiguousRooms.[i,j] then
                        roomStates.[x,y] <- oldRoomStates.[i,j]
                        roomIsCircled.[x,y] <- oldRoomIsCircled.[i,j]
                        roomCompleted.[x,y] <- oldRoomCompleted.[i,j]
                        let door(target:Canvas[,], x, y, source:Brush[,], i, j) =
                            if obj.Equals(source.[i,j],yes) || obj.Equals(source.[i,j],no) then
                                target.[x,y].Background <- source.[i,j]
                        if x<7 && i<7 then
                            door(horizontalDoors,x,y,oldHorizontalDoors,i,j)  // door right of room
                        if x>0 && i>0 then
                            door(horizontalDoors,x-1,y,oldHorizontalDoors,i-1,j)  // door left of room
                        if y<7 && j<7 then
                            door(verticalDoors,x,y,oldVerticalDoors,i,j)  // door below room
                        if y>0 && j>0 then
                            door(verticalDoors,x,y-1,oldVerticalDoors,i,j-1)  // door above room
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