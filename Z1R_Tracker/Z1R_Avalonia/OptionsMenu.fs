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

let makeOptionsCanvas(heightOffset) = 
    let options1sp = new StackPanel(Orientation=Orientation.Vertical, Margin=Thickness(10.,0.,10.,0.))
    let tb = new TextBox(Text="Overworld settings", IsReadOnly=true, Margin=Thickness(0.,heightOffset,0.,0.), FontWeight=FontWeight.Bold, BorderBrush=Brushes.Transparent, IsHitTestVisible=false)
    options1sp.Children.Add(tb) |> ignore
    for text,tip,b in data1 do
        let cb = new CheckBox(Content=new TextBox(Text=text,IsReadOnly=true, BorderBrush=Brushes.Transparent, IsHitTestVisible=false))
        ToolTip.SetTip(cb,tip)
        link(cb, b)
        options1sp.Children.Add(cb) |> ignore
    let optionsAllsp = new StackPanel(Orientation=Orientation.Horizontal, Margin=Thickness(2.))
    optionsAllsp.Children.Add(options1sp) |> ignore

    let options3sp = new StackPanel(Orientation=Orientation.Vertical, Margin=Thickness(10.,0.,0.,0.))
    let tb = new TextBox(Text="Other", IsReadOnly=true, FontWeight=FontWeight.Bold, BorderBrush=Brushes.Transparent, IsHitTestVisible=false)
    options3sp.Children.Add(tb) |> ignore
    let cb = new CheckBox(Content=new TextBox(Text="Second quest dungeons",IsReadOnly=true, BorderBrush=Brushes.Transparent, IsHitTestVisible=false))
    cb.IsChecked <- System.Nullable.op_Implicit TrackerModel.Options.IsSecondQuestDungeons.Value
    cb.Checked.Add(fun _ -> TrackerModel.Options.IsSecondQuestDungeons.Value <- true; TrackerModel.forceUpdate())
    cb.Unchecked.Add(fun _ -> TrackerModel.Options.IsSecondQuestDungeons.Value <- false; TrackerModel.forceUpdate())
    ToolTip.SetTip(cb,"Check this if dungeon 4, rather than dungeon 1, has 3 items")
    options3sp.Children.Add(cb) |> ignore
    let cb = new CheckBox(Content=new TextBox(Text="Mirror overworld",IsReadOnly=true, BorderBrush=Brushes.Transparent, IsHitTestVisible=false))
    cb.IsChecked <- System.Nullable.op_Implicit TrackerModel.Options.MirrorOverworld.Value
    cb.Checked.Add(fun _ -> TrackerModel.Options.MirrorOverworld.Value <- true; TrackerModel.forceUpdate())
    cb.Unchecked.Add(fun _ -> TrackerModel.Options.MirrorOverworld.Value <- false; TrackerModel.forceUpdate())
    ToolTip.SetTip(cb,"Flip the overworld map East<->West")
    options3sp.Children.Add(cb) |> ignore

    optionsAllsp.Children.Add(options3sp) |> ignore

    optionsAllsp
