module OverworldRouteDrawing

open Avalonia
open Avalonia.Controls
open Avalonia.Media

open OverworldRouting

let OMTW = Graphics.OMTW
let canvasAdd = Graphics.canvasAdd

let coords(Vertex(x,y,p)) =
    let cx = float(x*int OMTW+8*3)
    let cy = float(y*11*3+5*3)
    match p with
    | FULL -> cx,cy
    | NORTH -> cx+4.,cy-8.  // north and west
    | SOUTH -> cx-6.,cy+8.  // south and east
    | EAST -> cx+12.,cy
    | WEST -> cx-12.,cy

let makeLine(v1,v2,color) =
    let x1,y1 = coords(v1)
    let x2,y2 = coords(v2)
    let line = new Shapes.Line(StartPoint=Point(x1,y1), EndPoint=Point(x2,y2), Stroke=color, StrokeThickness=3., IsHitTestVisible=false)
    line

let MaxYGH = 12 // default
let drawPathsImpl(routeDrawingCanvas:Canvas, owRouteworthySpots:_[,], owUnmarked:bool[,], mousePos:Point, i, j, drawRouteMarks, fadeOut, maxYellowGreenHighlights) = 
    routeDrawingCanvas.Children.Clear()
    let ok, st = screenTypes.TryGetValue((i,j))
    if not ok then
        failwith "missing st"
    else
        let v = 
            match st with
            | WHOLE -> Vertex(i,j,FULL)
            | NS -> Vertex(i,j,if mousePos.Y>float(11*3/2) then SOUTH else NORTH)
            | EW -> Vertex(i,j,if mousePos.X>OMTW/2. then EAST else WEST)
        let goal = Vertex(-1,-1,FULL) // non-existent goal will map all reachable locations
        let color(cost) =
            // white path that is bright near the cursor, but opacity falls off quickly as you get more cost-distance away
            //if   cost <=  4 then new SolidColorBrush(Color.FromArgb(230uy, 255uy, 255uy, 255uy))   // too bright
            if   cost <=  8 then new SolidColorBrush(Color.FromArgb(180uy, 255uy, 255uy, 255uy))
            elif cost <= 14 then new SolidColorBrush(Color.FromArgb(150uy, 255uy, 255uy, 255uy))
            elif cost <= 22 then new SolidColorBrush(Color.FromArgb(120uy, 255uy, 255uy, 255uy))
            elif cost <= 30 then new SolidColorBrush(Color.FromArgb(100uy, 255uy, 255uy, 255uy))
            else                 new SolidColorBrush(Color.FromArgb( 85uy, 255uy, 255uy, 255uy))
        let d = findAllBestPaths(adjacencyDict, v, goal)
        let visited = new System.Collections.Generic.HashSet<_>()
        let accumulatedLines = ResizeArray()
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
                                xs |> Seq.exists (fun (v,_c) -> v=g)
                            else
                                false
                        if drawRouteMarks then 
                            if isRecorderWarpDestination(g) && not canWalk then
                                ()  // don't bother drawing lines to every recorder warp destination - is patently obvious and just clutters screen
                            elif (isWarpDestination(p) || isWarpDestination(g)) && not canWalk then
                                // non-walkable any road warps get a light dashed line
                                let x1,y1 = coords(g)
                                let x2,y2 = coords(p)
                                let line = new Shapes.Line(StartPoint=Point(x1,y1), EndPoint=Point(x2,y2), Stroke=color(999), StrokeThickness=3., IsHitTestVisible=false)
                                line.StrokeDashArray <- new Collections.AvaloniaList<float>()
                                line.StrokeDashArray.Add(1.0)  // number of thicknesses on...
                                line.StrokeDashArray.Add(1.0)  // number of thicknesses off...
                                accumulatedLines.Add(line)
                            else
                                // normal walk is solid line with fading color based on cost
                                accumulatedLines.Add(makeLine(g, p, if fadeOut then color(cost) else color(0)))
                        draw(p)
        // draw routes to everywhere
        //for v in adjacencyDict.Keys do
        //    draw(v)
        let pq = PriorityQueue()
        // draw routes only to routeworthy (accessible and interesting) spots
        for i = 0 to 15 do
            for j = 0 to 7 do
                if owRouteworthySpots.[i,j] then
                    let goal = convertToCanonicalVertex(i,j,screenTypes,STAIRS)
                    draw(goal)
                    // track costs to each unmarked spot
                    if owUnmarked.[i,j] then
                        let ok, r = d.TryGetValue(goal)
                        if ok then
                            let (cost,_preds) = r
                            pq.Enqueue((if ok then cost else 99999), (i,j))
        // highlight cheapest N unmarked
        let N = maxYellowGreenHighlights
        if N > 0 then
            let toHighlight = ResizeArray()
            let rec iterate(N,recentCost) =
                if not pq.IsEmpty then
                    let nextCost,(i,j) = pq.Dequeue()
                    if N > 0 || nextCost = recentCost then
                        toHighlight.Add(i,j)
                        iterate(N-1,nextCost)
            if not pq.IsEmpty then
                let recentCost,(i,j) = pq.Dequeue()
                toHighlight.Add(i,j)
                iterate(N-1,recentCost)
            for (i,j) in toHighlight do
                let thr = new Graphics.TileHighlightRectangle()
                if not(TrackerModel.mapStateSummary.OwGettableLocations.Contains(i,j)) then  
                    thr.MakeRed()  // many callers pass in routeworthy meaning 'acccesible & interesting', but some just pass 'interesting' and here is how we display 'inaccesible'
                elif TrackerModel.owInstance.SometimesEmpty(i,j) then
                    thr.MakeYellow()
                else
                    thr.MakeGreen()
                for s in thr.Shapes do
                    canvasAdd(routeDrawingCanvas, s, OMTW*float(i), float(j*11*3))
        for line in accumulatedLines do
            canvasAdd(routeDrawingCanvas, line, 0., 0.)  // we want the lines drawn atop everything else




