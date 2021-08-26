module Timeline

open Avalonia.Controls
open Avalonia.Media
open Avalonia

let canvasAdd(c:Canvas, item, left, top) =
    if item <> null then
        c.Children.Add(item) |> ignore
        Canvas.SetTop(item, top)
        Canvas.SetLeft(item, left)

type TimelineItem(f) =
    member this.IsDone() = f()

let TLC = Brushes.SandyBrown   // timeline color
let ICON_SPACING = 6.
let BIG_HASH = 15.
let LINE_THICKNESS = 3.
type Timeline(iconSize, numRows, numTicks, ticksPerHash, lineWidth, leftText, midText, rightText) =
    let iconAreaHeight = float numRows*(iconSize+ICON_SPACING)
    let timelineCanvas = new Canvas(Height=iconAreaHeight+BIG_HASH, Width=lineWidth)
    let itemCanvas = new Canvas(Height=iconAreaHeight+BIG_HASH, Width=lineWidth)
    let items = ResizeArray()
    let guidelines = ResizeArray()
    let line1 = new Shapes.Line(StartPoint=Point(0.,iconAreaHeight+BIG_HASH/2.), EndPoint=Point(lineWidth,iconAreaHeight+BIG_HASH/2.), Stroke=TLC, StrokeThickness=LINE_THICKNESS)
    let curTime = new Shapes.Line(StartPoint=Point(0.,iconAreaHeight), EndPoint=Point(0.,iconAreaHeight+BIG_HASH), Stroke=Brushes.White, StrokeThickness=LINE_THICKNESS)
    let mutable iconAreaFilled = Array2D.zeroCreate (int lineWidth + 1) numRows
    let x(i) = float i*(lineWidth/float numTicks)
    do
        //printfn "tick width = %f, iconWidth = %f, ratio = %f" (x 1) iconSize (iconSize / x 1)
        canvasAdd(timelineCanvas, itemCanvas, 0., 0.)  // should be first, so e.g. curTime draws atop it
        for i = 0 to numTicks do
            if i%(2*ticksPerHash)=0 then
                let line = new Shapes.Line(StartPoint=Point(x(i),iconAreaHeight), EndPoint=Point(x(i),iconAreaHeight+BIG_HASH), Stroke=TLC, StrokeThickness=LINE_THICKNESS)
                canvasAdd(timelineCanvas, line, 0., 0.)
            elif i%(ticksPerHash)=0 then
                let line = new Shapes.Line(StartPoint=Point(x(i),iconAreaHeight+BIG_HASH/4.), EndPoint=Point(x(i),iconAreaHeight+BIG_HASH*3./4.), Stroke=TLC, StrokeThickness=LINE_THICKNESS)
                canvasAdd(timelineCanvas, line, 0., 0.)
        canvasAdd(timelineCanvas, line1, 0., 0.)
        canvasAdd(timelineCanvas, curTime, 0., 0.)
        // text labels
        let ft = new FormattedText(leftText, Typeface.Default, 12., TextAlignment.Center, TextWrapping.NoWrap, Size.Infinity)
        if ft.Bounds.Height > iconSize then
            printfn "bad ft1"
        let ypos = iconAreaHeight - ft.Bounds.Height
        let xpos = 0. - ft.Bounds.Width/2.
        canvasAdd(timelineCanvas, new TextBlock(Text=leftText,FontSize=12.,Foreground=TLC,Background=Brushes.Black), xpos, ypos)
        for x = 0 to int (xpos + ft.Bounds.Width) do
            iconAreaFilled.[x, numRows-1] <- true
        let ft = new FormattedText(midText, Typeface.Default, 12., TextAlignment.Center, TextWrapping.NoWrap, Size.Infinity)
        if ft.Bounds.Height > iconSize then
            printfn "bad ft2"
        let ypos = iconAreaHeight - ft.Bounds.Height
        let xpos = lineWidth/2. - ft.Bounds.Width/2.
        canvasAdd(timelineCanvas, new TextBlock(Text=midText,FontSize=12.,Foreground=TLC,Background=Brushes.Black), xpos, ypos)
        for x = int xpos to int (xpos + ft.Bounds.Width) do
            iconAreaFilled.[x, numRows-1] <- true
        let ft = new FormattedText(rightText, Typeface.Default, 12., TextAlignment.Center, TextWrapping.NoWrap, Size.Infinity)
        if ft.Bounds.Height > iconSize then
            printfn "bad ft3"
        let ypos = iconAreaHeight - ft.Bounds.Height
        let xpos = lineWidth - ft.Bounds.Width/2.
        canvasAdd(timelineCanvas, new TextBlock(Text=rightText,FontSize=12.,Foreground=TLC,Background=Brushes.Black), xpos, ypos)
        for x = int xpos to int lineWidth do
            iconAreaFilled.[x, numRows-1] <- true
    member this.Canvas = timelineCanvas
    member this.Update(tick, timelineItems:ResizeArray<TimelineItem>) =  // mutates timeLineItems
        if tick < 0 || tick > numTicks then
            ()
        else
            let dones = ResizeArray()
            let bmps = ResizeArray()
            for x in timelineItems do
                match x.IsDone() with
                | Some(bmp) ->
                    if bmp <> null then
                        dones.Add(x)
                        bmps.Add(bmp)
                | None -> ()
            for x in dones do
                timelineItems.Remove(x) |> ignore
            let rowBmps = ResizeArray()
            let xmin,xmax = x(tick)-iconSize/2. , x(tick)+iconSize/2.
            let xmin,xmax = max 0 (int xmin), min (int xmax) (int lineWidth)
            for bmp in bmps do
                let mutable bestRow, bestPenalty = 0, 99999
                for row = numRows-1 downto 0 do
                    let mutable penalty = 0
                    for x=xmin to xmax do
                        if iconAreaFilled.[x,row] then
                            penalty <- penalty + 1
                    if penalty < bestPenalty then
                        bestRow <- row
                        bestPenalty <- penalty
                for x=xmin to xmax do
                    iconAreaFilled.[x,bestRow] <- true
                rowBmps.Add(bestRow, bmp)
            if rowBmps.Count > 0 then
                // guideline
                let bottomRow = rowBmps |> Seq.maxBy fst |> fst
                let line = new Shapes.Line(StartPoint=Point(x(tick),float(bottomRow+1)*(iconSize+ICON_SPACING)-ICON_SPACING), EndPoint=Point(x(tick),iconAreaHeight+BIG_HASH/2.), Stroke=Brushes.Gray, StrokeThickness=LINE_THICKNESS)
                guidelines.Add(line, 0., 0.)
                // items
                for row,bmp in rowBmps do
                    let img = Graphics.BMPtoImage bmp
                    img.Width <- iconSize
                    img.Height <- iconSize
                    items.Add(img, float xmin, float row*(iconSize+ICON_SPACING))
            // redraw guidelines and items
            itemCanvas.Children.Clear()
            for g,x,y in guidelines do
                canvasAdd(itemCanvas, g, x, y)
            for i,x,y in items do
                canvasAdd(itemCanvas, i, x, y)
            curTime.StartPoint <- Point(x(tick), curTime.StartPoint.Y)
            curTime.EndPoint <- Point(x(tick), curTime.EndPoint.Y)


