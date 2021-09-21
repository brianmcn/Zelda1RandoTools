module Timeline

open System.Windows.Media
open System.Windows.Controls
open System.Windows

let canvasAdd = Graphics.canvasAdd
type TimelineItem(f) =
    let mutable finishedMinute = 99999
    let mutable bmp = null
    member this.Sample(minute) =
        if bmp=null then
            match f() with
            | Some b ->
                bmp <- b
                finishedMinute <- minute
            | _ -> ()
    member this.IsDone = bmp<>null
    member this.FinishedMinute = finishedMinute
    member this.Bmp = bmp

let TLC = Brushes.SandyBrown   // timeline color
let ICON_SPACING = 6.
let BIG_HASH = 15.
let LINE_THICKNESS = 3.
type Timeline(iconSize, numRows, numTicks, ticksPerHash, lineWidth, minutesPerTick, leftText, midText, rightText, topRowReserveWidth:float) =
    let iconAreaHeight = float numRows*(iconSize+ICON_SPACING)
    let timelineCanvas = new Canvas(Height=iconAreaHeight+BIG_HASH, Width=lineWidth)
    let itemCanvas = new Canvas(Height=iconAreaHeight+BIG_HASH, Width=lineWidth)
    let line1 = new Shapes.Line(X1=0., Y1=iconAreaHeight+BIG_HASH/2., X2=lineWidth, Y2=iconAreaHeight+BIG_HASH/2., Stroke=TLC, StrokeThickness=LINE_THICKNESS)
    let curTime = new Shapes.Line(X1=0., Y1=iconAreaHeight, X2=0., Y2=iconAreaHeight+BIG_HASH, Stroke=Brushes.White, StrokeThickness=LINE_THICKNESS)
    let mutable iconAreaFilled = Array2D.zeroCreate (int lineWidth + 1) numRows
    let xf(i) =      i*(lineWidth/float numTicks)
    let x(i) = float i*(lineWidth/float numTicks)
    do
        //printfn "tick width = %f, iconWidth = %f, ratio = %f" (x 1) iconSize (iconSize / x 1)
        canvasAdd(timelineCanvas, itemCanvas, 0., 0.)  // should be first, so e.g. curTime draws atop it
        for i = 0 to numTicks do
            if i%(2*ticksPerHash)=0 then
                let line = new Shapes.Line(X1=x(i), Y1=iconAreaHeight, X2=x(i), Y2=iconAreaHeight+BIG_HASH, Stroke=TLC, StrokeThickness=LINE_THICKNESS)
                canvasAdd(timelineCanvas, line, 0., 0.)
            elif i%(ticksPerHash)=0 then
                let line = new Shapes.Line(X1=x(i), Y1=iconAreaHeight+BIG_HASH/4., X2=x(i), Y2=iconAreaHeight+BIG_HASH*3./4., Stroke=TLC, StrokeThickness=LINE_THICKNESS)
                canvasAdd(timelineCanvas, line, 0., 0.)
        canvasAdd(timelineCanvas, line1, 0., 0.)
        canvasAdd(timelineCanvas, curTime, 0., 0.)
        // text labels
        for x = 0 to int topRowReserveWidth do
            iconAreaFilled.[x, 0] <- 99
        let tb = new TextBlock(Text=leftText,FontSize=12.,Foreground=TLC,Background=Brushes.Black)
        let mkft(t) = new FormattedText(t, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch), tb.FontSize, tb.Foreground, new NumberSubstitution(), TextFormattingMode.Display)
        let ft = mkft(leftText)
        if ft.Height > iconSize then
            printfn "bad ft1"
        let ypos = iconAreaHeight - ft.Height
        let xpos = 0. - ft.Width/2.
        canvasAdd(timelineCanvas, new TextBlock(Text=leftText,FontSize=12.,Foreground=TLC,Background=Brushes.Black), xpos, ypos)
        for x = 0 to int (xpos + ft.Width) do
            iconAreaFilled.[x, numRows-1] <- 99
        let ft = mkft(midText)
        if ft.Height > iconSize then
            printfn "bad ft2"
        let ypos = iconAreaHeight - ft.Height
        let xpos = lineWidth/2. - ft.Width/2.
        canvasAdd(timelineCanvas, new TextBlock(Text=midText,FontSize=12.,Foreground=TLC,Background=Brushes.Black), xpos, ypos)
        for x = int xpos to int (xpos + ft.Width) do
            iconAreaFilled.[x, numRows-1] <- 99
        let ft = mkft(rightText)
        if ft.Height > iconSize then
            printfn "bad ft3"
        let ypos = iconAreaHeight - ft.Height
        let xpos = lineWidth - ft.Width/2.
        canvasAdd(timelineCanvas, new TextBlock(Text=rightText,FontSize=12.,Foreground=TLC,Background=Brushes.Black), xpos, ypos)
        for x = int xpos to int lineWidth do
            iconAreaFilled.[x, numRows-1] <- 99
    member this.Canvas = timelineCanvas
    member this.Update(minute, timelineItems:seq<TimelineItem>) =
        let tick = minute / minutesPerTick
        if tick < 0 || tick > numTicks then
            ()
        else
            for ti in timelineItems do
                ti.Sample(minute)
            this.DrawItemsAndGuidelines(timelineItems)
            curTime.X1 <- xf(float minute / float minutesPerTick)
            curTime.X2 <- xf(float minute / float minutesPerTick)
    member private this.DrawItemsAndGuidelines(timelineItems) =
        // redraw guidelines and items
        itemCanvas.Children.Clear()
        let iconAreaFilled = Array2D.copy iconAreaFilled  // grab fresh copy of fixed timeline labels, to mutate each time we redraw
        let buckets = new System.Collections.Generic.Dictionary<_,_>()
        for ti in timelineItems do
            if ti.IsDone then
                let tick = float ti.FinishedMinute/float minutesPerTick |> ceil |> int   // e.g. minute 4 at 3 minutesPerTick should wind up in bucket 6, not bucket 3
                if not(buckets.ContainsKey(tick)) then
                    buckets.Add(tick, ResizeArray())
                buckets.[tick].Add(ti.Bmp)
        for tick = 0 to numTicks do
            if buckets.ContainsKey(tick) then
                let rowBmps = ResizeArray()
                let xmin,xmax = x(tick)-iconSize/2. , x(tick)+iconSize/2.
                let xmin,xmax = max 0 (int xmin), min (int xmax) (int lineWidth)
                for bmp in buckets.[tick] do
                    let mutable bestRow, bestPenalty = 0, 99999
                    for row = numRows-1 downto 0 do
                        let mutable penalty = 0
                        for x=xmin to xmax do
                            penalty <- penalty + iconAreaFilled.[x,row]
                        if penalty < bestPenalty then
                            bestRow <- row
                            bestPenalty <- penalty
                    for x=xmin to xmax do
                        iconAreaFilled.[x,bestRow] <- iconAreaFilled.[x,bestRow] + 1
                    rowBmps.Add(bestRow, bmp)
                if rowBmps.Count > 0 then
                    // guideline
                    let bottomRow = rowBmps |> Seq.maxBy fst |> fst
                    let line = new Shapes.Line(X1=x(tick), Y1=float(bottomRow+1)*(iconSize+ICON_SPACING)-ICON_SPACING, X2=x(tick), Y2=iconAreaHeight+BIG_HASH/2., Stroke=Brushes.Gray, StrokeThickness=LINE_THICKNESS)
                    canvasAdd(itemCanvas, line, 0., 0.)
                    // items
                    for row,bmp in rowBmps do
                        let img = Graphics.BMPtoImage bmp
                        img.Width <- iconSize
                        img.Height <- iconSize
                        canvasAdd(itemCanvas, img, float xmin, float row*(iconSize+ICON_SPACING))
