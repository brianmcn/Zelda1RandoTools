module HotKeys

open System.Windows


////////////////////////////////////////////////////////////
// Main Window listens for KeyDown, then sends RoutedEvent to element under the mouse

module MyKey =
    type MyKeyRoutedEventHandler = delegate of obj * MyKeyRoutedEventArgs -> unit
    and  MyKeyRoutedEventArgs(key) =
        inherit RoutedEventArgs(MyKey.MyKeyEvent)
        member _this.Key : Input.Key = key
    and  MyKey() =
        static let myKeyEvent = EventManager.RegisterRoutedEvent("ZTrackerMyKey", RoutingStrategy.Bubble, typeof<MyKeyRoutedEventHandler>, typeof<MyKey>)
        static member MyKeyEvent = myKeyEvent

    type UIElement with
        member this.MyKeyAdd(f) = this.AddHandler(MyKey.MyKeyEvent, new MyKeyRoutedEventHandler(fun o ea -> f(ea)))

open MyKey

let InitializeWindow(w:Window) =
    w.Focusable <- true
    w.PreviewKeyDown.Add(fun ea ->
        //printfn "keydown"
        Input.Mouse.DirectlyOver.RaiseEvent(new MyKeyRoutedEventArgs(ea.Key))
        )

////////////////////////////////////////////////////////////

(*

// hot keys

Item.Book=b
...
Overworld.Level1=1
...
Blocker.Combat=c
...
DungeonRoom.Transport1=1
...
whatever.Nothing=x


*)
        