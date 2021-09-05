module OptionsMenu

open Avalonia.Controls
open Avalonia.Media
open Avalonia
open Avalonia.Layout

let link(cb:CheckBox, b:TrackerModel.Options.Bool) =
    cb.IsChecked <- System.Nullable.op_Implicit b.Value
    cb.Checked.Add(fun _ -> b.Value <- true)
    cb.Unchecked.Add(fun _ -> b.Value <- false)

let data1 = [|
    "Draw routes", "Constantly display routing lines when mousing over overworld tiles", TrackerModel.Options.Overworld.DrawRoutes
    "Highlight nearby", "Highlight nearest unmarked overworld tiles when mousing", TrackerModel.Options.Overworld.HighlightNearby
    "Show magnifier", "Display magnified view of overworld tiles when mousing", TrackerModel.Options.Overworld.ShowMagnifier
    |]

let makeOptionsCanvas(width, height, heightOffset) = 
    let optionsCanvas = new Canvas(Width=width, Height=height, Background=Brushes.White, Opacity=0., IsHitTestVisible=false)

    let options1sp = new StackPanel(Orientation=Orientation.Vertical, Margin=Thickness(10.,0.,10.,0.))
    let tb = new TextBox(Text="Overworld settings", IsReadOnly=true, Margin=Thickness(0.,heightOffset,0.,0.), FontWeight=FontWeight.Bold)
    options1sp.Children.Add(tb) |> ignore
    for text,tip,b in data1 do
        let cb = new CheckBox(Content=new TextBox(Text=text,IsReadOnly=true))
        ToolTip.SetTip(cb,tip)
        link(cb, b)
        options1sp.Children.Add(cb) |> ignore
    let optionsAllsp = new StackPanel(Orientation=Orientation.Horizontal)
    optionsAllsp.Children.Add(options1sp) |> ignore

    Graphics.canvasAdd(optionsCanvas, optionsAllsp, 0., 0.)
    optionsCanvas