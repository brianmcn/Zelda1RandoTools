module CustomMessageBox

open Avalonia
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Media

(*
sample use:

let cmb = new CustomMessageBox.CustomMessageBox("Verify changes", "You moved a dungeon segment. Keep this change?", ["Keep changes"; "Undo"])
cmb.Owner <- Window.GetWindow(this)
cmb.ShowDialog() |> ignore
if cmb.MessageBoxResult = null || cmb.MessageBoxResult = "Undo" then
    // undo
else
    // keep

*)

type CustomMessageBox(title, mainText, buttonTexts:seq<string>) as this =
    inherit Window()

    let mutable result = null
    let mutable focus = fun() -> ()

    do
        this.WindowStartupLocation <- WindowStartupLocation.CenterOwner
        this.Topmost <- true
        this.Title <- title
        this.SizeToContent <- SizeToContent.WidthAndHeight

        let grid = new Grid()
        grid.RowDefinitions.Add(new RowDefinition(Height=GridLength(1.0, GridUnitType.Star)))
        grid.RowDefinitions.Add(new RowDefinition(Height=GridLength.Auto))

        let mainDock = new DockPanel()
        let image = Graphics.BMPtoImage(System.Drawing.SystemIcons.Question.ToBitmap())
        image.HorizontalAlignment <- HorizontalAlignment.Left
        image.Margin <- Thickness(30.,0.,0.,0.)
        mainDock.Children.Add(image) |> ignore
        DockPanel.SetDock(image, Dock.Left)
        let mainTextBlock = new TextBlock(Text=mainText, TextWrapping=TextWrapping.Wrap, MaxWidth=500., Width=System.Double.NaN, 
                                            VerticalAlignment=VerticalAlignment.Center, Margin=Thickness(12.,20.,41.,15.))
        mainDock.Children.Add(mainTextBlock) |> ignore
        grid.Children.Add(mainDock) |> ignore
        Grid.SetRow(mainDock, 0)

        let buttonDock = new DockPanel(Margin=Thickness(5.,0.,0.,0.))
        let mutable first = true
        for bt in buttonTexts |> Seq.rev do
            let b = new Button(MinWidth=88., MaxWidth=160., Height=26., Margin=Thickness(5.), HorizontalAlignment=HorizontalAlignment.Right)
            if first then
                focus <- fun () -> b.Focus() |> ignore
                first <- false
            DockPanel.SetDock(b, Dock.Right)
            buttonDock.Children.Add(b) |> ignore
            b.Content <- new Label(Content=bt, Padding=Thickness(0.), Margin=Thickness(10.,0.,10.,0.))
            b.Click.Add(fun _ -> result <- bt; this.Close())
        grid.Children.Add(buttonDock) |> ignore
        Grid.SetRow(buttonDock, 1)

        this.Content <- grid

    override this.OnOpened(e) =
        focus()
        base.OnOpened(e)

    member this.MessageBoxResult = result

