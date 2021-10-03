module PieMenus

open System.Windows.Controls
open System.Windows.Media
open System.Windows

let canvasAdd = Graphics.canvasAdd
let OMTW = Graphics.OMTW

let takeAnyW = 330.
let takeAnyH = 220.

let takeAnyCandlePanel = 
    let c1 = new Canvas(Width=30., Height=30., Background=Brushes.Black)
    c1.Children.Add(new Shapes.Rectangle(Width=30., Height=30., Stroke=CustomComboBoxes.no, StrokeThickness=3.0)) |> ignore
    canvasAdd(c1, Graphics.BMPtoImage Graphics.blue_candle_bmp, 4., 4.)
    let c2 = new Canvas(Width=30., Height=30., Background=Brushes.Black)
    c2.Children.Add(new Shapes.Rectangle(Width=30., Height=30., Stroke=CustomComboBoxes.yes, StrokeThickness=3.0)) |> ignore
    canvasAdd(c2, Graphics.BMPtoImage Graphics.blue_candle_bmp, 4., 4.)
    let col = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
    let group = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    let row = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center, Margin=Thickness(0.,0.,40.,0.))
    row.Children.Add(Graphics.BMPtoImage Graphics.owHeartEmpty_bmp) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.iconRightArrow_bmp) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.owHeartSkipped_bmp) |> ignore  // TODO
    group.Children.Add(row) |> ignore
    let row = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center, Margin=Thickness(0.,0.,40.,0.))
    row.Children.Add(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[24].[0]) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.iconRightArrow_bmp) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[24].[0]) |> ignore  // TODO
    group.Children.Add(row) |> ignore
    let row = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    row.Children.Add(c1) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.iconRightArrow_bmp) |> ignore
    row.Children.Add(c2) |> ignore
    group.Children.Add(row) |> ignore
    col.Children.Add(group) |> ignore
    let image = Graphics.BMPtoImage Graphics.takeAnyCandleBMP
    image.Width <- takeAnyW
    image.Height <- takeAnyH
    image.Stretch <- Stretch.UniformToFill
    col.Children.Add(image) |> ignore
    let b = new Border(Child=col, BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Width=takeAnyW+6., Height=takeAnyH+6.+30., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    b

let takeAnyPotionPanel = 
    let col = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
    let group = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    let row = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center, Margin=Thickness(0.,0.,40.,0.))
    row.Children.Add(Graphics.BMPtoImage Graphics.owHeartEmpty_bmp) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.iconRightArrow_bmp) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.owHeartSkipped_bmp) |> ignore  // TODO
    group.Children.Add(row) |> ignore
    let row = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    row.Children.Add(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[24].[0]) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.iconRightArrow_bmp) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[24].[0]) |> ignore  // TODO
    group.Children.Add(row) |> ignore
    col.Children.Add(group) |> ignore
    let image = Graphics.BMPtoImage Graphics.takeAnyPotionBMP
    image.Width <- takeAnyW
    image.Height <- takeAnyH
    image.Stretch <- Stretch.UniformToFill
    col.Children.Add(image) |> ignore
    let b = new Border(Child=col, BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Width=takeAnyW+6., Height=takeAnyH+6.+30., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    b

let takeAnyHeartPanel = 
    let col = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
    let group = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    let row = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center, Margin=Thickness(0.,0.,40.,0.))
    row.Children.Add(Graphics.BMPtoImage Graphics.owHeartEmpty_bmp) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.iconRightArrow_bmp) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.owHeartFull_bmp) |> ignore  // TODO
    group.Children.Add(row) |> ignore
    let row = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    row.Children.Add(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[24].[0]) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.iconRightArrow_bmp) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[24].[0]) |> ignore  // TODO
    group.Children.Add(row) |> ignore
    col.Children.Add(group) |> ignore
    let image = Graphics.BMPtoImage Graphics.takeAnyHeartBMP
    image.Width <- takeAnyW
    image.Height <- takeAnyH
    image.Stretch <- Stretch.UniformToFill
    col.Children.Add(image) |> ignore
    let b = new Border(Child=col, BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Width=takeAnyW+6., Height=takeAnyH+6.+30., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    b

let takeAnyLeavePanel = 
    let col = new StackPanel(Orientation=Orientation.Vertical, Background=Brushes.Black)
    let group = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    let row = new StackPanel(Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Center)
    row.Children.Add(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[24].[0]) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.iconRightArrow_bmp) |> ignore
    row.Children.Add(Graphics.BMPtoImage Graphics.theInteriorBmpTable.[24].[0]) |> ignore  // TODO
    group.Children.Add(row) |> ignore
    let image = Graphics.BMPtoImage Graphics.takeAnyLeaveBMP
    image.Width <- takeAnyW
    image.Height <- takeAnyH
    image.Stretch <- Stretch.UniformToFill
    col.Children.Add(image) |> ignore
    col.Children.Add(group) |> ignore
    let b = new Border(Child=col, BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Width=takeAnyW+6., Height=takeAnyH+6.+30., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    b

let makeTakeAnyPieMenu(cm,h) =
    let c = new Canvas(IsHitTestVisible=true)
    let ps = ResizeArray()
    for p, dock in [takeAnyCandlePanel,Dock.Top; takeAnyPotionPanel,Dock.Left; takeAnyHeartPanel,Dock.Right; takeAnyLeavePanel,Dock.Bottom] do
        let dp = new DockPanel(Width=16.*OMTW-20., Height=h, LastChildFill=false)
        DockPanel.SetDock(p, dock)
        dp.Children.Add(p) |> ignore
        canvasAdd(c, dp, 10., 10.)
        ps.Add(p)
    let innerH = h - 2.*takeAnyCandlePanel.Height
    let g = new Grid(Width=16.*OMTW-20., Height=h)
    let circle = new Shapes.Ellipse(Width=innerH, Height=innerH, Stroke=Brushes.LightGray, StrokeThickness=3., HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center)
    g.Children.Add(circle) |> ignore
    canvasAdd(c, g, 10., 10.)
    let center = Point(8.*OMTW,h*0.5)
    c.MouseMove.Add(fun ea ->
        let pos = ea.GetPosition(c)
        let vector = Point.Subtract(pos, center)
        let distance = vector.Length
        ps |> Seq.iter (fun p -> p.BorderBrush <- Brushes.Gray)
        if distance > innerH/2. then
            if vector.X > 0. && vector.X > abs(vector.Y) then
                ps.[2].BorderBrush <- Brushes.Yellow
            elif vector.X < 0. && abs(vector.X) > abs(vector.Y) then
                ps.[1].BorderBrush <- Brushes.Yellow
            elif vector.Y < 0. && abs(vector.Y) > abs(vector.X) then
                ps.[0].BorderBrush <- Brushes.Yellow
            elif vector.Y > 0. && vector.Y > abs(vector.X) then
                ps.[3].BorderBrush <- Brushes.Yellow
        )
    CustomComboBoxes.DoModal(cm, 0., 0., c, (fun()->())) |> ignore
    