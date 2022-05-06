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
let staticMirrorScreenScrolls = 
    let table = ResizeArray()
    // R -> L
    table.Add(Vertex(3,0,FULL), Vertex(4,0,FULL), 2)
    table.Add(Vertex(9,0,FULL), Vertex(10,0,FULL), 2)
    table.Add(Vertex(1,1,FULL), Vertex(2,1,FULL), 2)
    table.Add(Vertex(5,2,FULL), Vertex(6,2,FULL), 2)
    table.Add(Vertex(14,2,FULL), Vertex(15,2,FULL), 2)
    table.Add(Vertex(3,3,FULL), Vertex(4,3,FULL), 2)
    table.Add(Vertex(8,3,FULL), Vertex(9,3,FULL), 2)
    table.Add(Vertex(11,3,FULL), Vertex(12,3,FULL), 2)
    table.Add(Vertex(1,4,FULL), Vertex(2,4,FULL), 2)
    table.Add(Vertex(4,7,FULL), Vertex(5,7,WEST), 2)
    // cross rivers / coast splits
    table.Add(Vertex(7,1,EAST), Vertex(7,1,WEST), 2)
    table.Add(Vertex(7,1,WEST), Vertex(7,1,EAST), 2)
    table.Add(Vertex(7,2,EAST), Vertex(7,2,WEST), 2)
    table.Add(Vertex(7,2,WEST), Vertex(7,2,EAST), 2)
    table.Add(Vertex(5,5,EAST), Vertex(5,5,WEST), 2)
    table.Add(Vertex(5,5,WEST), Vertex(5,5,EAST), 2)
    table.Add(Vertex(13,1,EAST), Vertex(13,1,WEST), 2)
    table.Add(Vertex(13,1,WEST), Vertex(13,1,EAST), 2)
    table
let staticNormalScreenScrolls = 
    let table = ResizeArray()
    // R -> L
    table.Add(Vertex(12,0,FULL), Vertex(11,0,FULL), 2)
    table.Add(Vertex(2,1,FULL), Vertex(1,1,FULL), 2)
    table.Add(Vertex(6,2,FULL), Vertex(5,2,FULL), 2)
    table.Add(Vertex(5,3,FULL), Vertex(4,3,FULL), 2)
    table.Add(Vertex(14,3,FULL), Vertex(13,3,FULL), 2)
    table.Add(Vertex(1,5,FULL), Vertex(0,5,FULL), 2)
    table.Add(Vertex(5,7,EAST), Vertex(4,7,FULL), 2)
    table.Add(Vertex(5,7,EAST), Vertex(5,7,WEST), 2)
    // L -> R
    ()
    // cross rivers splits
    table.Add(Vertex(7,1,EAST), Vertex(7,1,WEST), 2)
    table.Add(Vertex(7,1,WEST), Vertex(7,1,EAST), 2)
    table.Add(Vertex(7,2,EAST), Vertex(7,2,WEST), 2)
    table.Add(Vertex(7,2,WEST), Vertex(7,2,EAST), 2)
    table.Add(Vertex(5,5,EAST), Vertex(5,5,WEST), 2)
    table.Add(Vertex(5,5,WEST), Vertex(5,5,EAST), 2)
    // cross coast split
    table.Add(Vertex(13,1,EAST), Vertex(13,1,WEST), 2)
    table.Add(Vertex(13,1,WEST), Vertex(13,1,EAST), 2)
    table
let allPossibleScreenScrolls =
    let mutable s = Set.empty
    for f,t,_c in staticMirrorScreenScrolls do
        s <- Set.add (f,t) s
    for f,t,_c in staticNormalScreenScrolls do
        s <- Set.add (f,t) s
    // normal world wrap (requires ladder)
    s <- Set.add (Vertex(0,6,FULL), Vertex(15,5,FULL)) s   
    // mirror coast (requires ladder)
    s <- Set.add(Vertex(15,4,FULL), Vertex(14,4,FULL)) s
    s <- Set.add(Vertex(15,5,FULL), Vertex(14,5,FULL)) s
    s

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

type PortionDetail =
    // on grid spots with two halves, we may need to inquire about
    | STAIRS               // where the canonical stairs/cave to enter the thing is
    | ANY_ROAD_ARRIVAL     // where we appear on the screen when exiting here from an any road
    | TORNADO_ARRIVAL      // where we appear on the screen after exiting the warp tornado
let convertToCanonicalVertex(x,y,st:System.Collections.Generic.Dictionary<_,_>,portionDetail) =
    let ok, t = st.TryGetValue((x,y))
    if ok then
        match t with
        | WHOLE -> Vertex(x,y,FULL)
        | NS -> Vertex(x,y,SOUTH)  // the only NS screen with a destination (2,7) has it all in the south
        | EW -> 
            Vertex(x,y, match x,y,portionDetail with
                        |  3,2,TORNADO_ARRIVAL -> EAST
                        |  3,2,_ -> WEST
                        | 13,1,TORNADO_ARRIVAL -> EAST
                        | 13,1,_ -> WEST
                        | 13,6,_ -> WEST   // TODO check
                        |  5,7,STAIRS -> WEST
                        |  5,7,_ -> EAST   // TODO check
                        | 12,6,STAIRS -> WEST
                        | 12,6,_ -> EAST   // TODO check
                        | 11,4,STAIRS -> EAST
                        | 11,4,_ -> WEST
                        |  2,6,STAIRS -> EAST
                        |  2,6,_ -> WEST
                        | _ -> EAST
                    )
    else
        failwith "impossible st"
let populateDynamic(ladder, raft, currentRecorderWarpDestinations, currentAnyRoads, isMirror) =
    let a = ResizeArray()
    if TrackerModelOptions.Overworld.RoutesCanScreenScroll.Value then
        if isMirror then
            a.AddRange(staticMirrorScreenScrolls)
            if ladder then
                a.Add(Vertex(15,4,FULL), Vertex(14,4,FULL), 2)
                a.Add(Vertex(15,5,FULL), Vertex(14,5,FULL), 2)
        else
            a.AddRange(staticNormalScreenScrolls)
            if ladder then
                a.Add(Vertex(0,6,FULL), Vertex(15,5,FULL), 3)   // world wrap
    a.AddRange(populateStaticOverworldData(ladder,raft))
    let st = generateScreenTypeList(a)
    let allVertex = new System.Collections.Generic.HashSet<_>()
    for v1,v2,_ in a do
        allVertex.Add(v1) |> ignore
        allVertex.Add(v2) |> ignore
    let addExtra(srcs,dests,pd,cost) =
        for x,y in dests do
            for v in srcs do
                a.Add(v, convertToCanonicalVertex(x,y,st,pd), cost)
    addExtra(allVertex, currentRecorderWarpDestinations, TORNADO_ARRIVAL, 7)
    addExtra(currentAnyRoads |> Seq.map (fun (x,y) -> convertToCanonicalVertex(x,y,st,STAIRS)), currentAnyRoads, ANY_ROAD_ARRIVAL, 4)
    let d = makeAdjacencyDict(a)
    st, d
let mutable screenTypes, adjacencyDict = populateDynamic(false,false,ResizeArray(),ResizeArray(), false)
let mutable adjacencyDictSansWarps = new System.Collections.Generic.Dictionary<_,_>()
let mutable recorderDests : seq<int*int> = upcast ResizeArray()
let mutable anyRoads : seq<int*int> = upcast ResizeArray()
let repopulate(ladder,raft,currentRecorderWarpDestinations,currentAnyRoads,isMirror) =
    recorderDests <- currentRecorderWarpDestinations
    anyRoads <- currentAnyRoads
    let st,ad = populateDynamic(ladder,raft,currentRecorderWarpDestinations,currentAnyRoads,isMirror)
    screenTypes <- st
    adjacencyDict <- ad
    let _st,ad = populateDynamic(ladder,raft,ResizeArray(),ResizeArray(),isMirror)
    adjacencyDictSansWarps <- ad

/////////////////////////////////////////////

// generic code
type PriorityQueue<'T when 'T : comparison>() =
    let mutable pq = Set.empty 
    member this.Enqueue(pri,v:'T) = 
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
    let q = PriorityQueue()
    let mutable bestCost = System.Int32.MaxValue 
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

