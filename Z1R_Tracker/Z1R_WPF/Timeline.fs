module Timeline

open System.Windows.Media
open System.Windows.Controls
open System.Windows

let canvasAdd = Graphics.canvasAdd
type TimelineItem(ident : string, f) =
    let model = TrackerModel.TimelineItemModel.All.[ident]
    member this.IsDone = model.FinishedTotalSeconds <> -1
    member this.FinishedTotalSeconds = model.FinishedTotalSeconds
    member this.Bmp = f()
    member this.Has = model.Has
    member this.Identifier = ident

let TLC = Brushes.SandyBrown   // timeline color
let ICON_SPACING = 6.
let BIG_HASH = 15.
let LINE_THICKNESS = 3.
let numTicks, ticksPerHash = 60, 5
type Timeline(iconSize, numRows, lineWidth, minutesPerTick, sevenTexts:string[], topRowReserveWidth:float) =
    let iconAreaHeight = float numRows*(iconSize+ICON_SPACING)
    let timelineCanvas = new Canvas(Height=iconAreaHeight+BIG_HASH, Width=lineWidth)
    let itemCanvas = new Canvas(Height=iconAreaHeight+BIG_HASH, Width=lineWidth)
    let timeToolTip = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, BorderThickness=Thickness(3.0), FontSize=16.0, IsHitTestVisible=false)
    let line1 = new Shapes.Line(X1=0., Y1=iconAreaHeight+BIG_HASH/2., X2=lineWidth, Y2=iconAreaHeight+BIG_HASH/2., Stroke=TLC, StrokeThickness=LINE_THICKNESS)
    let curTime = new Shapes.Line(X1=0., Y1=iconAreaHeight, X2=0., Y2=iconAreaHeight+BIG_HASH, Stroke=Brushes.White, StrokeThickness=LINE_THICKNESS)
    let mutable iconAreaFilled = Array2D.zeroCreate (int lineWidth + 1) numRows
    let xf(i) =      i*(lineWidth/float numTicks)
    let x(i) = float i*(lineWidth/float numTicks)
    do
        let tb = new TextBlock(Text="0h",FontSize=12.,Foreground=TLC,Background=Brushes.Black)
        let mkft(t) = new FormattedText(t, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, 
                                            new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch), 
                                            tb.FontSize, tb.Foreground, new NumberSubstitution(), TextFormattingMode.Display, VisualTreeHelper.GetDpi(timelineCanvas).PixelsPerDip)
        let mutable n = 0
        let placeTextLabel(x) =
            let ft = mkft(sevenTexts.[n])
            if ft.Height > iconSize then
                printfn "bad formatted text"
            let ypos = iconAreaHeight + 6.
            let xpos = x - ft.Width/2.
            canvasAdd(timelineCanvas, new TextBlock(Text=sevenTexts.[n],FontSize=12.,Foreground=TLC,Background=Brushes.Black), xpos, ypos)
            n <- n + 1
        canvasAdd(timelineCanvas, itemCanvas, 0., 0.)  // should be first, so e.g. curTime draws atop it
        for i = 0 to numTicks do
            if i%(2*ticksPerHash)=0 then
                let line = new Shapes.Line(X1=x(i), Y1=iconAreaHeight, X2=x(i), Y2=iconAreaHeight+BIG_HASH, Stroke=TLC, StrokeThickness=LINE_THICKNESS)
                canvasAdd(timelineCanvas, line, 0., 0.)
                placeTextLabel(x(i))
            elif i%(ticksPerHash)=0 then
                let line = new Shapes.Line(X1=x(i), Y1=iconAreaHeight+BIG_HASH/4., X2=x(i), Y2=iconAreaHeight+BIG_HASH*3./4., Stroke=TLC, StrokeThickness=LINE_THICKNESS)
                canvasAdd(timelineCanvas, line, 0., 0.)
        canvasAdd(timelineCanvas, line1, 0., 0.)
        canvasAdd(timelineCanvas, curTime, 0., 0.)
        // text labels
        for x = 0 to int topRowReserveWidth do
            iconAreaFilled.[x, 0] <- 99
    member this.Canvas = timelineCanvas
    member this.Update(minute, timelineItems:seq<TimelineItem>) =
        let tick = minute / minutesPerTick
        if tick < 0 || tick > numTicks then
            ()
        else
            this.DrawItemsAndGuidelines(timelineItems)
            curTime.X1 <- xf(float (minute / minutesPerTick))
            curTime.X2 <- xf(float (minute / minutesPerTick))
    member private this.DrawItemsAndGuidelines(timelineItems) =
        // redraw guidelines and items
        itemCanvas.Children.Clear()
        let iconAreaFilled = Array2D.copy iconAreaFilled  // grab fresh copy of fixed timeline labels, to mutate each time we redraw
        let buckets = new System.Collections.Generic.Dictionary<_,_>()
        for ti in timelineItems do
            if ti.IsDone then
                let tick = float ti.FinishedTotalSeconds/(60.*float minutesPerTick) |> int
                if not(buckets.ContainsKey(tick)) then
                    buckets.Add(tick, ResizeArray())
                buckets.[tick].Add(ti.Bmp, ti.FinishedTotalSeconds, ti.Has, ti.Identifier)
        for tick = 0 to numTicks do
            if buckets.ContainsKey(tick) then
                let rowBmps = ResizeArray()
                let xminOrig,xmaxOrig = x(tick)-iconSize/2. , x(tick)+iconSize/2.
                let xmin,xmax = max 0 (int xminOrig), min (int xmaxOrig) (int lineWidth)
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
                    for row,(bmp,totalSeconds,has,ident) in rowBmps do
                        let c = new Canvas(Width=iconSize, Height=iconSize)
                        let img = Graphics.BMPtoImage ((if has = TrackerModel.PlayerHas.NO then Graphics.greyscale else id) bmp)
                        img.Width <- iconSize
                        img.Height <- iconSize
                        c.Children.Add(img) |> ignore
                        if has = TrackerModel.PlayerHas.SKIPPED then CustomComboBoxes.placeSkippedItemXDecorationImpl(c, iconSize)
                        c.MouseEnter.Add(fun _ ->
                            itemCanvas.Children.Remove(timeToolTip)
                            timeToolTip.Text <- System.TimeSpan.FromSeconds(float totalSeconds).ToString("""hh\:mm\:ss""") + "\n" + ident
                            let x = xminOrig
                            let x = min x (float(16*16*3 - 100))  // don't go off right screen edge
                            canvasAdd(itemCanvas, timeToolTip, x, float row*(iconSize+ICON_SPACING)-35.)
                            )
                        c.MouseLeave.Add(fun _ ->
                            itemCanvas.Children.Remove(timeToolTip)
                            )
                        canvasAdd(itemCanvas, c, float xminOrig, float row*(iconSize+ICON_SPACING))
