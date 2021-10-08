module HotKeys

open Avalonia
open Avalonia.Controls

////////////////////////////////////////////////////////////
// Main Window listens for KeyDown, then sends RoutedEvent to element under the mouse

module MyKey =
    type MyKeyRoutedEventHandler = delegate of obj * MyKeyRoutedEventArgs -> unit
    and  MyKeyRoutedEventArgs(ch) =
        inherit Avalonia.Interactivity.RoutedEventArgs(MyKey.MyKeyEvent)
        member _this.Key : char = ch
    and  MyKey() =
        static let myKeyEvent = Avalonia.Interactivity.RoutedEvent.Register("ZTrackerMyKey", Avalonia.Interactivity.RoutingStrategies.Bubble)
        static member MyKeyEvent = myKeyEvent

    type Control with
        member this.MyKeyAdd(f) = this.AddHandler(MyKey.MyKeyEvent, new MyKeyRoutedEventHandler(fun o ea -> f(ea)))

open MyKey

let InitializeWindow(w:Window, rootCanvas) =
    w.PointerMoved.Add(fun ea ->
        let p = ea.GetPosition(rootCanvas)
        Graphics.curMouse <- p
        )
    w.KeyDown.Add(fun ea ->
        //printfn "keydown"
        if ea.Key = Input.Key.A then
            //printfn "sending A at (%A)" Graphics.curMouse
            let all = Avalonia.VisualTree.VisualExtensions.GetVisualsAt(rootCanvas, Graphics.curMouse)
            let sorted = Avalonia.VisualTree.VisualExtensions.SortByZIndex(all) |> Seq.rev
            let mutable ok = false
            for viz in sorted do
                if not ok then
                    match viz with
                    | :? Control as c -> 
                        if c.IsHitTestVisible && c.Opacity > 0. then
                            //printfn "found control %A" c
                            //printfn "%A" (sorted.GetType())
                            c.RaiseEvent(new MyKeyRoutedEventArgs('A'))
                            ok <- true
                        else
                            () //printfn "walking past %A" viz
                    | _ -> 
                        () //printfn "walking past %A" viz
        )

////////////////////////////////////////////////////////////
