module OverworldRouteDrawing

open System.Windows.Controls
open System.Windows.Media

open OverworldRouting
open System.Windows

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
    let line = new Shapes.Line(X1=x1, X2=x2, Y1=y1, Y2=y2, Stroke=color, StrokeThickness=3., IsHitTestVisible=false)
    line
let makeCurve(v1,v2,color) =
    let x1,y1 = coords(v1)
    let x2,y2 = coords(v2)
    let pf = new PathFigure(Point(x1,y1), [new BezierSegment(Point(x1,y1-12.), Point(x2,y2-12.), Point(x2,y2), true)], false)
    let curve = new Shapes.Path(Stroke=color, StrokeThickness=3., IsHitTestVisible=false, Data=new PathGeometry([pf]))
    curve

let MaxGYR = 12 // default
let All = 128
let color1 = new SolidColorBrush(Color.FromArgb(230uy, 255uy, 255uy, 255uy))
let color2 = new SolidColorBrush(Color.FromArgb(180uy, 255uy, 255uy, 255uy))
let color3 = new SolidColorBrush(Color.FromArgb(150uy, 255uy, 255uy, 255uy))
let color4 = new SolidColorBrush(Color.FromArgb(120uy, 255uy, 255uy, 255uy))
let color5 = new SolidColorBrush(Color.FromArgb(100uy, 255uy, 255uy, 255uy))
let color6 = new SolidColorBrush(Color.FromArgb( 85uy, 255uy, 255uy, 255uy))
let colorAlt1 = new SolidColorBrush(Color.FromArgb(230uy, 160uy, 220uy, 255uy))
let colorAlt2 = new SolidColorBrush(Color.FromArgb(200uy, 160uy, 220uy, 255uy))
let colorAlt3 = new SolidColorBrush(Color.FromArgb(175uy, 160uy, 220uy, 255uy))
let colorAlt4 = new SolidColorBrush(Color.FromArgb(150uy, 160uy, 220uy, 255uy))
let colorAlt5 = new SolidColorBrush(Color.FromArgb(125uy, 160uy, 220uy, 255uy))
let colorAlt6 = new SolidColorBrush(Color.FromArgb(100uy, 160uy, 220uy, 255uy))
let drawPathsImpl(routeDrawingCanvas:Canvas, owRouteworthySpots:_[,], owUnmarked:bool[,], mousePos:System.Windows.Point, i, j, drawRouteMarks, fadeOut, maxBoldGYR, maxPaleGYR, whatToCyan) = 
    routeDrawingCanvas.Children.Clear()
    let ok, st = screenTypes.TryGetValue((i,j))
    if not ok then
        failwith "missing st"
    else
        let v = 
            match st with
            | WHOLE -> Vertex(i,j,FULL)
            | NS -> Vertex(i,j,if mousePos.Y>float(11*3/2) then SOUTH else NORTH)
            | EW -> Vertex(i,j,if mousePos.X>float(int OMTW/2) then EAST else WEST)
        let goal = Vertex(-1,-1,FULL) // non-existent goal will map all reachable locations
        let color(cost) =
            // white path that is bright near the cursor, but opacity falls off quickly as you get more cost-distance away
            //if   cost <=  4 then color1   // too bright
            if   cost <=  8 then color2
            elif cost <= 14 then color3
            elif cost <= 22 then color4
            elif cost <= 30 then color5
            else                 color6
        let colorAlt(cost) =
            // white path that is bright near the cursor, but opacity falls off quickly as you get more cost-distance away
            //if   cost <=  4 then colorAlt1   // too bright
            if   cost <=  8 then colorAlt2
            elif cost <= 14 then colorAlt3
            elif cost <= 22 then colorAlt4
            elif cost <= 30 then colorAlt5
            else                 colorAlt6
        let d = findAllBestPaths(adjacencyDict, v, goal)
        let visited = new System.Collections.Generic.HashSet<_>()
        let accumulatedLines = ResizeArray<Shapes.Shape>()
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
                        let preferLadder = 
                            TrackerModel.playerComputedStateSummary.HaveLadder && (
                                match p,g with
                                | Vertex(7,1,_), Vertex(7,1,_) -> true
                                | Vertex(7,2,_), Vertex(7,2,_) -> true
                                | _ -> false
                                )
                        if drawRouteMarks then 
                            if isRecorderWarpDestination(g) && not canWalk then
                                ()  // don't bother drawing lines to every recorder warp destination - is patently obvious and just clutters screen
                            elif (isWarpDestination(p) || isWarpDestination(g)) && not canWalk then
                                // non-walkable any road warps get a light dashed line
                                let x1,y1 = coords(g)
                                let x2,y2 = coords(p)
                                let line = new Shapes.Line(X1=x1, X2=x2, Y1=y1, Y2=y2, Stroke=color(999), StrokeThickness=3., IsHitTestVisible=false)
                                line.StrokeDashArray.Add(1.0)  // number of thicknesses on...
                                line.StrokeDashArray.Add(1.0)  // number of thicknesses off...
                                accumulatedLines.Add(line)
                            elif allPossibleScreenScrolls.Contains(p,g) && not(preferLadder) then
                                if p=Vertex(0,6,FULL) && g=Vertex(15,5,FULL) then   // world wrap
                                    accumulatedLines.Add(makeCurve(Vertex(-1,6,FULL), Vertex(0,6,FULL), if fadeOut then colorAlt(cost) else colorAlt(0)))
                                    accumulatedLines.Add(makeCurve(Vertex(15,5,FULL), Vertex(16,5,FULL), if fadeOut then colorAlt(cost) else colorAlt(0)))
                                else
                                    accumulatedLines.Add(makeCurve(g, p, if fadeOut then colorAlt(cost) else colorAlt(0)))
                            else
                                // normal walk is solid line with fading color based on cost
                                accumulatedLines.Add(makeLine(g, p, if fadeOut then color(cost) else color(0)))
                        draw(p)
        // draw routes to everywhere
        //for v in adjacencyDict.Keys do
        //    draw(v)
        if false then   // just turn on if want to visualize full map topography
            // draw all possible routes
            accumulatedLines.Clear()
            let preferLadder, cost = false, 0
            for p in adjacencyDict.Keys do
                for g,_cost in adjacencyDict.[p] do
                    if allPossibleScreenScrolls.Contains(p,g) && not(preferLadder) then
                        if p=Vertex(0,6,FULL) && g=Vertex(15,5,FULL) then   // world wrap
                            accumulatedLines.Add(makeCurve(Vertex(-1,6,FULL), Vertex(0,6,FULL), if fadeOut then colorAlt(cost) else colorAlt(0)))
                            accumulatedLines.Add(makeCurve(Vertex(15,5,FULL), Vertex(16,5,FULL), if fadeOut then colorAlt(cost) else colorAlt(0)))
                        else
                            accumulatedLines.Add(makeCurve(g, p, if fadeOut then colorAlt(cost) else colorAlt(0)))
                    else
                        // normal walk is solid line with fading color based on cost
                        accumulatedLines.Add(makeLine(g, p, if fadeOut then color(cost) else color(0)))
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
                            pq.Enqueue(cost, (i,j))
        let N = maxBoldGYR
        let M = maxPaleGYR
        // bold highlight cheapest N unmarked, pale highlight next cheapest M unmarked
        if N+M > 0 then
            let toHighlight = ResizeArray()
            let rec iterate(N,recentCost) =
                if not pq.IsEmpty then
                    let nextCost,(i,j) = pq.Dequeue()
                    if maxBoldGYR > 0 && (N > 0 || nextCost = recentCost) then
                        toHighlight.Add(i,j,true)
                        iterate(N-1,nextCost)
                    elif N > -M then
                        toHighlight.Add(i,j,false)
                        iterate(N-1,999999)
            if not pq.IsEmpty then
                let recentCost,(i,j) = pq.Dequeue()
                toHighlight.Add(i,j,N>0)
                iterate(N-1,recentCost)
            for (i,j,bright) in toHighlight do
                let thr = new Graphics.TileHighlightRectangle()
                let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                if cur>=0 && cur<=7 then // most callers pass in owUnmarked equal to TrackerModel.overworldMapMarks, but some pass dungeon letters A-H as unmarked too, color them accessible
                    if bright then
                        thr.MakeGreen()
                    else
                        thr.MakePaleGreen()
                elif not(TrackerModel.mapStateSummary.OwGettableLocations.Contains(i,j)) then  
                    if bright then
                        thr.MakeRed()  // many callers pass in routeworthy meaning 'acccesible & interesting', but some just pass 'interesting' and here is how we display 'inaccesible'
                    else
                        thr.MakePaleRed()
                elif TrackerModel.owInstance.SometimesEmpty(i,j) then
                    if bright then
                        thr.MakeYellow()
                    else
                        thr.MakePaleYellow()
                else
                    if bright then
                        thr.MakeGreen()
                    else
                        thr.MakePaleGreen()
                // cyan overrides all
                if not(whatToCyan(i,j)) then
                    canvasAdd(routeDrawingCanvas, thr.Shape, OMTW*float(i), float(j*11*3))
        for i = 0 to 15 do
            for j = 0 to 7 do
                if whatToCyan(i,j) then
                    let thr = new Graphics.TileHighlightRectangle()
                    thr.MakeCyan()
                    canvasAdd(routeDrawingCanvas, thr.Shape, OMTW*float(i), float(j*11*3))
        for line in accumulatedLines do
            canvasAdd(routeDrawingCanvas, line, 0., 0.)  // we want the lines drawn atop everything else







