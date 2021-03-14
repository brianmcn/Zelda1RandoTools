module OverworldRouting

(*
Suppose I am at overworld location (x,y).  What are the optimal routes to all unmarked accessible spots and uncompleted dungeons?
Consider any road warps and whistle-to-completed-dungeons, and Lost Woods behavior, river, etc.

A simple grid of screens with NSEW neighbors does not properly capture all adjacency data (e.g. cant walk L-R thru dead end tree), but it's close.

A better solution is to model as a more abstract adjacency graph.  While some rooms are full vertices on a grid, there's about a dozen modeled as 'east half of room and 
west half of room' as two separate vertices, and a couple with 'north half and south half' as separate vertices.  Each vertex knows its adjacencies and the costs to go to them.

After starting with a static graph with adjacency traversal costs, a pass adjusts costs or adds adjacencies based on the Ladder, Raft, Any Roads, and 
Whistleable Dungeons.  The adjusted graph can then be used for a breadth-first search to find optimal routes.
*)

type OverworldScreenPortion =
    | FULL     // most screens
    | NORTH    // north half of a screen
    | SOUTH    // south half of a screen
    | EAST     // east half of a screen
    | WEST     // west half of a screen

type Vertex =
    | Vertex of int * int * OverworldScreenPortion  // x coord, y coord, portion ... 0,0 is upper left death mountain

let populateStaticOverworldData(ladder, raft) =
    let adjacencyTransitionCostTable = ResizeArray()   // fromVertex, ToVertex, cost
    let symmetricAdd(f,t,c) = // from, to, cost
        adjacencyTransitionCostTable.Add(f, t, c)
        adjacencyTransitionCostTable.Add(t, f, c)
    let commonAddRight(x,y) =
        symmetricAdd(Vertex(x,y,FULL), Vertex(x+1,y,FULL), 2)
    let commonAddDown(x,y) =
        symmetricAdd(Vertex(x,y,FULL), Vertex(x,y+1,FULL), 2)
    let commonAddRightAndDown(x,y) =
        commonAddRight(x,y)
        commonAddDown(x,y)
    commonAddRightAndDown(0,0)
    commonAddRightAndDown(1,0)
    commonAddRightAndDown(2,0)
    commonAddDown(3,0)
    commonAddRight(4,0)
    commonAddRight(5,0)
    commonAddRight(6,0)
    commonAddRight(7,0)
    commonAddRight(8,0)
    commonAddDown(10,0)
    commonAddRightAndDown(12,0)
    commonAddDown(14,0)
    commonAddDown(15,0)
    commonAddRight(0,1)
    commonAddRight(2,1)
    commonAddRight(3,1)
    commonAddRightAndDown(4,1)
    commonAddRightAndDown(5,1)
    commonAddRight(8,1)
    commonAddRight(9,1)
    commonAddRight(10,1)
    commonAddDown(12,1)
    commonAddRight(14,1)
    commonAddRightAndDown(0,2)
    commonAddDown(1,2)
    commonAddDown(2,2)
    commonAddRight(4,2)
    commonAddDown(5,2)
    commonAddDown(6,2)
    commonAddRightAndDown(8,2)
    commonAddRight(9,2)
    commonAddRightAndDown(10,2)
    commonAddRightAndDown(11,2)
    commonAddRight(12,2)
    commonAddRightAndDown(13,2)
    commonAddDown(14,2)
    commonAddRightAndDown(0,3)
    commonAddRightAndDown(1,3)
    commonAddRight(2,3)
    commonAddDown(4,3)
    commonAddRight(5,3)
    commonAddRight(7,3)
    commonAddDown(8,3)
    commonAddDown(9,3)
    commonAddRightAndDown(10,3)
    commonAddDown(12,3)
    commonAddDown(13,3)
    commonAddRight(14,3)
    commonAddDown(15,3)
    commonAddRightAndDown(0,4)
    commonAddDown(2,4)
    commonAddDown(3,4)
    commonAddDown(4,4)
    commonAddRightAndDown(6,4)
    commonAddRight(7,4)
    commonAddDown(8,4)
    commonAddRightAndDown(9,4)
    commonAddRight(12,4)
    commonAddRightAndDown(13,4)
    commonAddDown(15,4)
    commonAddDown(0,5)
    commonAddRight(1,5)
    commonAddRight(2,5)
    commonAddRightAndDown(3,5)
    commonAddDown(4,5)
    commonAddRight(6,5)
    commonAddRight(7,5)
    commonAddRightAndDown(8,5)
    commonAddRightAndDown(9,5)
    commonAddRightAndDown(10,5)
    commonAddDown(11,5)
    commonAddRight(13,5)
    commonAddDown(15,5)
    commonAddRightAndDown(3,6)
    commonAddRight(4,6)
    commonAddRight(5,6)
    commonAddRightAndDown(6,6)
    commonAddRightAndDown(7,6)
    commonAddRightAndDown(8,6)
    commonAddRight(9,6)
    commonAddRight(10,6)
    commonAddDown(11,6)
    commonAddRight(14,6)
    commonAddDown(15,6)
    commonAddRight(0,7)
    commonAddRight(3,7)
    commonAddRight(6,7)
    commonAddRight(7,7)
    commonAddRight(8,7)
    commonAddRight(9,7)
    commonAddRight(10,7)
    commonAddRight(11,7)
    commonAddRight(12,7)
    commonAddRight(13,7)
    commonAddRight(14,7)
    // 3,2 is an EW portion
    symmetricAdd(Vertex(3,2,WEST), Vertex(3,3,FULL), 2)
    symmetricAdd(Vertex(3,2,EAST), Vertex(3,3,FULL), 2)
    symmetricAdd(Vertex(3,2,EAST), Vertex(4,2,FULL), 2)
    // 7,1 is an EW portion
    symmetricAdd(Vertex(7,1,WEST), Vertex(7,0,FULL), 2)
    symmetricAdd(Vertex(7,1,WEST), Vertex(6,1,FULL), 2)
    symmetricAdd(Vertex(7,1,EAST), Vertex(8,1,FULL), 2)
    symmetricAdd(Vertex(7,1,EAST), Vertex(7,2,EAST), 2)
    // 7,2 is an EW portion
    symmetricAdd(Vertex(7,2,WEST), Vertex(6,2,FULL), 2)
    symmetricAdd(Vertex(7,2,EAST), Vertex(8,2,FULL), 2)
    // 13,1 is an EW portion
    symmetricAdd(Vertex(13,1,WEST), Vertex(12,1,FULL), 2)
    symmetricAdd(Vertex(13,1,WEST), Vertex(13,2,FULL), 2)
    symmetricAdd(Vertex(13,1,EAST), Vertex(13,0,FULL), 2)
    symmetricAdd(Vertex(13,1,EAST), Vertex(14,1,FULL), 2)
    symmetricAdd(Vertex(13,1,EAST), Vertex(13,2,FULL), 2)
    // 11,4 is an EW portion
    symmetricAdd(Vertex(11,4,WEST), Vertex(10,4,FULL), 2)
    symmetricAdd(Vertex(11,4,WEST), Vertex(11,5,FULL), 2)
    symmetricAdd(Vertex(11,4,EAST), Vertex(11,3,FULL), 2)
    symmetricAdd(Vertex(11,4,EAST), Vertex(11,5,FULL), 2)
    // 2,6 is an EW portion
    symmetricAdd(Vertex(2,6,WEST), Vertex(1,6,FULL), 2)
    symmetricAdd(Vertex(2,6,WEST), Vertex(2,5,FULL), 2)
    symmetricAdd(Vertex(2,6,EAST), Vertex(3,6,FULL), 2)
    symmetricAdd(Vertex(2,6,EAST), Vertex(2,5,FULL), 2)
    // 2,7 is a NW portion
    symmetricAdd(Vertex(2,7,NORTH), Vertex(2,6,WEST), 2)
    symmetricAdd(Vertex(2,7,NORTH), Vertex(3,7,FULL), 2)
    symmetricAdd(Vertex(2,7,SOUTH), Vertex(1,7,FULL), 2)
    symmetricAdd(Vertex(2,7,SOUTH), Vertex(3,7,FULL), 2)
    // 5,5 is an EW portion
    symmetricAdd(Vertex(5,5,WEST), Vertex(4,5,FULL), 2)
    symmetricAdd(Vertex(5,5,WEST), Vertex(5,6,FULL), 2)
    symmetricAdd(Vertex(5,5,EAST), Vertex(6,5,FULL), 2)
    symmetricAdd(Vertex(5,5,EAST), Vertex(5,6,FULL), 2)
    // 5,7 is an EW portion
    symmetricAdd(Vertex(5,7,WEST), Vertex(5,6,FULL), 2)
    symmetricAdd(Vertex(5,7,EAST), Vertex(6,7,FULL), 2)
    symmetricAdd(Vertex(5,7,EAST), Vertex(5,6,FULL), 2)
    // 12,5 is a NS portion and 12,6 is an EW portion
    symmetricAdd(Vertex(11,5,FULL), Vertex(12,5,NORTH), 2)
    symmetricAdd(Vertex(11,5,FULL), Vertex(12,5,SOUTH), 2)
    symmetricAdd(Vertex(13,5,FULL), Vertex(12,5,NORTH), 2)
    symmetricAdd(Vertex(12,6,WEST), Vertex(11,6,FULL), 2)
    symmetricAdd(Vertex(12,6,WEST), Vertex(12,5,SOUTH), 2)
    symmetricAdd(Vertex(12,6,EAST), Vertex(13,6,WEST), 2)
    symmetricAdd(Vertex(12,6,EAST), Vertex(12,5,NORTH), 2)
    // 13,6 is an EW portion
    symmetricAdd(Vertex(13,5,FULL), Vertex(13,6,WEST), 2)
    symmetricAdd(Vertex(13,6,EAST), Vertex(14,6,FULL), 2)
    symmetricAdd(Vertex(13,6,EAST), Vertex(13,5,FULL), 2)

    // static asymmetries
    // lost woods
    adjacencyTransitionCostTable.Add(Vertex(0,6,FULL), Vertex(1,6,FULL), 2)
    adjacencyTransitionCostTable.Add(Vertex(1,6,FULL), Vertex(0,6,FULL), 8)
    adjacencyTransitionCostTable.Add(Vertex(1,5,FULL), Vertex(1,6,FULL), 2)
    adjacencyTransitionCostTable.Add(Vertex(1,7,FULL), Vertex(1,6,FULL), 2)
    // lost hills
    adjacencyTransitionCostTable.Add(Vertex(11,0,FULL), Vertex(11,1,FULL), 2)
    adjacencyTransitionCostTable.Add(Vertex(11,1,FULL), Vertex(11,0,FULL), 8)
    adjacencyTransitionCostTable.Add(Vertex(12,1,FULL), Vertex(11,1,FULL), 2)

    // conditional state
    if ladder then
        symmetricAdd(Vertex(7,1,EAST),Vertex(7,1,WEST),1)
        symmetricAdd(Vertex(7,2,EAST),Vertex(7,2,WEST),1)
    if raft then
        symmetricAdd(Vertex(15,2,FULL),Vertex(15,3,FULL),2)
        symmetricAdd(Vertex(5,4,FULL),Vertex(5,5,EAST),2)
    else
        // we want every vertex in the table, so add dummy edge from raft spot to itself
        symmetricAdd(Vertex(15,2,FULL),Vertex(15,2,FULL),1)
        symmetricAdd(Vertex(5,4,FULL),Vertex(5,4,FULL),1)
    adjacencyTransitionCostTable

let makeAdjacencyDict(adjacencyTransitionCostTable) =
    let d = new System.Collections.Generic.Dictionary<_,_>()
    for f,t,c in adjacencyTransitionCostTable do
        let ok, r = d.TryGetValue(f)
        if not ok then
            d.Add(f, [(t,c)])
        else
            d.Remove(f) |> ignore
            d.Add(f, (t,c)::r)
    d

type ScreenType =
    | WHOLE
    | NS
    | EW
let generateScreenTypeList(a) =
    // sanity check all (x,y) rooms have concordant 'portion'
    let d = new System.Collections.Generic.Dictionary<_,_>()
    let tryAdd(Vertex(x,y,p)) =
        let ok, st = d.TryGetValue((x,y))
        if ok then
            match st,p with
            | WHOLE, FULL -> ()
            | NS, NORTH -> ()
            | NS, SOUTH -> ()
            | EW, EAST -> ()
            | EW, WEST -> ()
            | _ -> failwith "mismatch"
        else
            d.Add((x,y), match p with | FULL -> WHOLE | NORTH|SOUTH -> NS |EAST|WEST -> EW)
    for v1, v2, _c in a do
        tryAdd(v1)
        tryAdd(v2)
    if d.Keys.Count <> 16*8 then  // sanity check all grid spots have a screen type
        failwith "bad map data"   // but only works when raft=true, else no vertex for those spots as no edges go there
    d

let convertToCanonicalVertex(x,y,st:System.Collections.Generic.Dictionary<_,_>) =
    let ok, t = st.TryGetValue((x,y))
    if ok then
        match t with
        | WHOLE -> Vertex(x,y,FULL)
        | NS -> Vertex(x,y,SOUTH)  // the only NS screen with a destination has it in the south
        | EW -> Vertex(x,y,EAST)   // TODO better accuracy
    else
        failwith "impossible st"
let populateDynamic(ladder, raft, currentRecorderWarpDestinations,currentAnyRoads) =
    let a = populateStaticOverworldData(ladder,raft)
    let st = generateScreenTypeList(a)
    let allVertex = new System.Collections.Generic.HashSet<_>()
    for v1,v2,_ in a do
        allVertex.Add(v1) |> ignore
        allVertex.Add(v2) |> ignore
    let addExtra(srcs,dests,cost) =
        for x,y in dests do
            for v in srcs do
                a.Add(v, convertToCanonicalVertex(x,y,st), cost)
    addExtra(allVertex, currentRecorderWarpDestinations, 7)
    addExtra(currentAnyRoads |> Seq.map (fun (x,y) -> convertToCanonicalVertex(x,y,st)), currentAnyRoads, 4)
    let d = makeAdjacencyDict(a)
    st, d
let mutable screenTypes, adjacencyDict = populateDynamic(false,false,ResizeArray(),ResizeArray())
let mutable adjacencyDictSansWarps = new System.Collections.Generic.Dictionary<_,_>()
let mutable recorderDests = ResizeArray()
let mutable anyRoads = new System.Collections.Generic.HashSet<_>()
let repopulate(ladder,raft,currentRecorderWarpDestinations,currentAnyRoads) =
    recorderDests <- currentRecorderWarpDestinations
    anyRoads <- currentAnyRoads
    let st,ad = populateDynamic(ladder,raft,currentRecorderWarpDestinations,currentAnyRoads)
    screenTypes <- st
    adjacencyDict <- ad
    let _st,ad = populateDynamic(ladder,raft,ResizeArray(),ResizeArray())
    adjacencyDictSansWarps <- ad

open System.Windows.Controls
open System.Windows.Media

let canvasAdd(c:Canvas, item, left, top) =
    if item <> null then
        c.Children.Add(item) |> ignore
        Canvas.SetTop(item, top)
        Canvas.SetLeft(item, left)

let coords(Vertex(x,y,p)) =
    let cx = float(x*16*3+8*3)
    let cy = float(y*11*3+5*3)
    match p with
    | FULL -> cx,cy
    | NORTH -> cx+4.,cy-8.  // north and west
    | SOUTH -> cx-6.,cy+8.  // south and east
    | EAST -> cx+12.,cy
    | WEST -> cx-12.,cy
    
let drawLine(c,v1,v2,color) =
    let x1,y1 = coords(v1)
    let x2,y2 = coords(v2)
    let line = new System.Windows.Shapes.Line(X1=x1, X2=x2, Y1=y1, Y2=y2, Stroke=color, StrokeThickness=3., IsHitTestVisible=false)
    canvasAdd(c, line, 0., 0.)

/////////////////////////////////////////////

// generic code
type PriorityQueue() =
    let mutable pq = Set.empty 
    member this.Enqueue(pri,v) = 
        pq <- pq.Add(pri,v)
    member this.Dequeue() =
        let r = pq.MinimumElement 
        pq <- pq.Remove(r)
        r
    member this.IsEmpty = pq.IsEmpty 
    member this.MinimumPriority = fst pq.MinimumElement

let findAllBestPaths(adjacencyCostDict:System.Collections.Generic.IDictionary<Vertex,_>, f, t) =
    // uses breadth-first cost search to find in minimal cost order
    // returns Dictionary<node,(bestCostToGetHere,[allBestPredecessorNodes])>
    let visited = new System.Collections.Generic.Dictionary<_,_>()
    let q = new PriorityQueue()
    let mutable bestCost = System.Int32.MaxValue 
    let results = ResizeArray()
    q.Enqueue(0, (f, None))   // costSoFar, nodeToTry, predecessor
    while not q.IsEmpty && q.MinimumPriority <= bestCost do
        let cost, (cur, pred) = q.Dequeue()
        if cur = t && cost < bestCost then
            bestCost <- cost
        let newBest() =
            // log it
            visited.Add(cur, (cost, match pred with None -> [] | Some x -> [x]))
            // continue with all nexts
            let ok, nexts = adjacencyCostDict.TryGetValue(cur)
            if ok then
                for next,c in (nexts:_ list) do
                    q.Enqueue(cost + c, (next, Some cur))
        let ok, old = visited.TryGetValue(cur)
        if not ok then
            // first visit
            newBest()
        else
            let (oldCost, oldPreds) = old
            // not first visit...
            if oldCost < cost then
                // current is worse cost, do nothing
                ()
            elif cost < oldCost then
                // current is better cost, replace
                visited.Remove(cur) |> ignore
                newBest()
            else
                // current is equal cost; multiple preds get here at same cost
                visited.Remove(cur) |> ignore
                let thisPred = match pred with | Some x -> x | None -> failwith "impossible"
                visited.Add(cur, (cost, thisPred::oldPreds))
                // no need to enqueue successor adjacencies, someone prior already did that 
    visited

/////////////////////////////////////////////

let drawPaths(routeDrawingCanvas:Canvas, owRouteworthySpots:_[,], mousePos:System.Windows.Point, i, j) = 
    routeDrawingCanvas.Children.Clear()
    let ok, st = screenTypes.TryGetValue((i,j))
    if not ok then
        failwith "missing st"
    else
        let v = 
            match st with
            | WHOLE -> Vertex(i,j,FULL)
            | NS -> Vertex(i,j,if mousePos.Y>float(11*3/2) then SOUTH else NORTH)
            | EW -> Vertex(i,j,if mousePos.X>float(16*3/2) then EAST else WEST)
        let goal = Vertex(-1,-1,FULL) // non-existent goal will map all reachable locations
        let color(cost) =
            // white path that is bright near the cursor, but opacity falls off quickly as you get more cost-distance away
            if   cost <=  4 then new SolidColorBrush(Color.FromArgb(230uy, 255uy, 255uy, 255uy))
            elif cost <=  8 then new SolidColorBrush(Color.FromArgb(180uy, 255uy, 255uy, 255uy))
            elif cost <= 14 then new SolidColorBrush(Color.FromArgb(150uy, 255uy, 255uy, 255uy))
            elif cost <= 22 then new SolidColorBrush(Color.FromArgb(120uy, 255uy, 255uy, 255uy))
            elif cost <= 30 then new SolidColorBrush(Color.FromArgb(100uy, 255uy, 255uy, 255uy))
            else                 new SolidColorBrush(Color.FromArgb( 85uy, 255uy, 255uy, 255uy))
            
        let d = findAllBestPaths(adjacencyDict, v, goal)
        let visited = new System.Collections.Generic.HashSet<_>()
        let rec draw(g) = 
            if visited.Add(g) then
                let ok, r = d.TryGetValue(g)
                if ok then
                    let (cost,preds) = r
                    for p in preds do
                        let isRecorderWarpDestination(Vertex(x,y,_)) =
                            let mutable r = false
                            for i,j in recorderDests do
                                if x=i && y=j then
                                    r <- true
                            r
                        let isWarpDestination(Vertex(x,y,_) as v) =
                            let mutable r = false
                            for i,j in anyRoads do
                                if x=i && y=j then
                                    r <- true
                            r || isRecorderWarpDestination(v)
                        let canWalk = 
                            let ok, xs = adjacencyDictSansWarps.TryGetValue(p)
                            if ok then
                                xs |> Seq.exists (fun (v,c) -> v=g)
                            else
                                false
                        if isRecorderWarpDestination(g) && not canWalk then
                            ()  // don't bother drawing lines to every recorder warp destination - is patently obvious and just clutters screen
                        elif (isWarpDestination(p) || isWarpDestination(g)) && not canWalk then
                            // non-walkable any road warps get a light dashed line
                            let x1,y1 = coords(g)
                            let x2,y2 = coords(p)
                            let line = new System.Windows.Shapes.Line(X1=x1, X2=x2, Y1=y1, Y2=y2, Stroke=color(999), StrokeThickness=3., IsHitTestVisible=false)
                            line.StrokeDashArray.Add(1.0)  // number of thicknesses on...
                            line.StrokeDashArray.Add(1.0)  // number of thicknesses off...
                            canvasAdd(routeDrawingCanvas, line, 0., 0.)
                        else
                            // normal walk is solid line with fading color based on cost
                            drawLine(routeDrawingCanvas,g,p,color(cost))
                        draw(p)
        // draw routes to everywhere
        //for v in adjacencyDict.Keys do
        //    draw(v)
        // draw routes only to routeworthy (accessible and interesting) spots
        for i = 0 to 15 do
            for j = 0 to 7 do
                if owRouteworthySpots.[i,j] then
                    draw(convertToCanonicalVertex(i,j,screenTypes))

