module DungeonHighlightsUI

open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let canvasAdd = Graphics.canvasAdd

let makeHighlights(level, dungeonTabs:TabControl, dungeonBodyHighlightCanvas:Canvas, rightwardCanvas:Canvas, 
                        roomStates:DungeonRoomState.DungeonRoomState[,], usedTransports:int[], currentOutlineDisplayState:int[],
                        horizontalDoors:Dungeon.Door[,], verticalDoors:Dungeon.Door[,], blockersHoverEvent:Event<_>) =
    let startAnimationFuncs = ResizeArray()
    let endAnimationFuncs = ResizeArray()

    let setupCanvas = new Canvas()
    let mutable finishedSetup = false
    let anim = new Animation.DoubleAnimation(1.0, 1.3, new Duration(System.TimeSpan.FromSeconds(0.75)))
    anim.RepeatBehavior <- Animation.RepeatBehavior.Forever
    anim.AutoReverse <- true
    // horizontal doors
    let horizontalDoorHighlights = Array2D.zeroCreate 7 8
    for i = 0 to 6 do
        for j = 0 to 7 do
            let d = new Canvas(Width=12., Height=16., IsHitTestVisible=false)
            let rect = new Shapes.Rectangle(Width=12., Height=16., Stroke=Brushes.DarkTurquoise, StrokeThickness=2., Fill=Dungeon.unknown, Opacity=0.)
            let st = new ScaleTransform(1.0, 1.0, CenterX=d.Width/2., CenterY=d.Height/2.)
            rect.RenderTransform <- st
            startAnimationFuncs.Add(fun() -> st.BeginAnimation(ScaleTransform.ScaleXProperty, anim))
            endAnimationFuncs.Add(fun() -> st.BeginAnimation(ScaleTransform.ScaleXProperty, null))
            startAnimationFuncs.Add(fun() -> st.BeginAnimation(ScaleTransform.ScaleYProperty, anim))
            endAnimationFuncs.Add(fun() -> st.BeginAnimation(ScaleTransform.ScaleYProperty, null))
            d.Children.Add(rect) |> ignore
            horizontalDoorHighlights.[i,j] <- rect
            canvasAdd(setupCanvas, d, float(i*(39+12)+39), float(j*(27+12)+6))
    // vertical doors
    let verticalDoorHighlights = Array2D.zeroCreate 8 7
    for i = 0 to 7 do
        for j = 0 to 6 do
            let d = new Canvas(Width=24., Height=12., IsHitTestVisible=false)
            let rect = new Shapes.Rectangle(Width=24., Height=12., Stroke=Brushes.DarkTurquoise, StrokeThickness=2., Fill=Dungeon.unknown, Opacity=0.)
            let st = new ScaleTransform(1.0, 1.0, CenterX=d.Width/2., CenterY=d.Height/2.)
            rect.RenderTransform <- st
            startAnimationFuncs.Add(fun() -> st.BeginAnimation(ScaleTransform.ScaleXProperty, anim))
            endAnimationFuncs.Add(fun() -> st.BeginAnimation(ScaleTransform.ScaleXProperty, null))
            startAnimationFuncs.Add(fun() -> st.BeginAnimation(ScaleTransform.ScaleYProperty, anim))
            endAnimationFuncs.Add(fun() -> st.BeginAnimation(ScaleTransform.ScaleYProperty, null))
            d.Children.Add(rect) |> ignore
            verticalDoorHighlights.[i,j] <- rect
            canvasAdd(setupCanvas, d, float(i*(39+12)+8), float(j*(27+12)+27))

    let len = 2.2
    let anim = new Animation.DoubleAnimation(0., len+len, new Duration(System.TimeSpan.FromSeconds(0.75)))
    anim.RepeatBehavior <- Animation.RepeatBehavior.Forever
    // rooms
    let roomHighlights = Array2D.zeroCreate 8 8
    for i = 0 to 7 do
        for j = 0 to 7 do
            let brush = Brushes.Magenta
            let extra = 6
            let ellipse = new Shapes.Ellipse(Width=float(13*3+12+2*extra), Height=float(9*3+12+2*extra), Stroke=brush, StrokeThickness=6., IsHitTestVisible=false, Opacity=0.)
            ellipse.StrokeDashArray <- new DoubleCollection( seq[len;len] )
            startAnimationFuncs.Add(fun() -> ellipse.BeginAnimation(Shapes.Ellipse.StrokeDashOffsetProperty, anim))
            endAnimationFuncs.Add(fun() -> ellipse.BeginAnimation(Shapes.Ellipse.StrokeDashOffsetProperty, null))
//            let canim = new Animation.ColorAnimation(Colors.Magenta, Colors.Cyan, new Duration(System.TimeSpan.FromSeconds(0.75)))
//            canim.AutoReverse <- true
//            canim.RepeatBehavior <- Animation.RepeatBehavior.Forever
//            brush.BeginAnimation(SolidColorBrush.ColorProperty, canim)
            canvasAdd(setupCanvas, ellipse, float(i*51-12/2-extra), float(j*39-12/2-extra))
            roomHighlights.[i,j] <- ellipse
    let isThereARoom(x,y) =  // 0=no, 1=yes, 2=maybe
        if roomStates.[x,y].RoomType = DungeonRoomState.RoomType.OffTheMap then
            0
        elif roomStates.[x,y].RoomType <> DungeonRoomState.RoomType.Unmarked then
            1
        else  // is Unmarked, use other context
            // never a room behind lobby arrow
            if   y < 7 && roomStates.[x,y+1].RoomType = DungeonRoomState.RoomType.StartEnterFromN then
                0
            elif y > 0 && roomStates.[x,y-1].RoomType = DungeonRoomState.RoomType.StartEnterFromS then
                0
            elif x < 7 && roomStates.[x+1,y].RoomType = DungeonRoomState.RoomType.StartEnterFromW then
                0
            elif x > 0 && roomStates.[x-1,y].RoomType = DungeonRoomState.RoomType.StartEnterFromE then
                0
            else
                // use the vanilla map outline the user has chosen, if any
                let cur = currentOutlineDisplayState.[level-1]
                if cur = 0 then
                    2
                else
                    let data = 
                        if cur <= 9 then
                            DungeonData.firstQuest.[cur-1]    // 1-9 is 1Q
                        else
                            DungeonData.secondQuest.[cur-10]  // 10-18 is 2Q
                    if data.[y].Chars(x) = 'X' then
                        2 // there is a room there, but the player has not marked the room as existing on map, and so we want to make it a possible bomb target to get into, so report 'maybe'
                    else
                        0
    let isThereANonZeldaRoom(x,y) = if roomStates.[x,y].RoomType = DungeonRoomState.RoomType.Zelda then 0 else isThereARoom(x,y)
    let mutable isCurrentlyHighlighting = false
    let highlight() =
        if isCurrentlyHighlighting then
            ()
        else
        isCurrentlyHighlighting <- true
        let mutable anyFound = false
        // possible bomb walls
        for i = 0 to 6 do
            for j = 0 to 7 do
                if horizontalDoors.[i,j].State = Dungeon.DoorState.UNKNOWN then
                    if isThereANonZeldaRoom(i,j)+isThereANonZeldaRoom(i+1,j)=3 then // one yes and one maybe
                        if level = 9 && (roomStates.[i,j].RoomType = DungeonRoomState.RoomType.StartEnterFromS || roomStates.[i+1,j].RoomType = DungeonRoomState.RoomType.StartEnterFromS) then
                            () // do nothing, left/right walls of L9 lobby unbombable
                        else
                            horizontalDoorHighlights.[i,j].Opacity <- 1.0
                            anyFound <- true
        for i = 0 to 7 do
            for j = 0 to 6 do
                if verticalDoors.[i,j].State = Dungeon.DoorState.UNKNOWN then
                    if isThereANonZeldaRoom(i,j)+isThereANonZeldaRoom(i,j+1)=3 then // one yes and one maybe
                        // npc hints & bomb upgrades never can bomb north
                        if roomStates.[i,j+1].RoomType <> DungeonRoomState.RoomType.OldManHint && roomStates.[i,j+1].RoomType <> DungeonRoomState.RoomType.BombUpgrade then
                            verticalDoorHighlights.[i,j].Opacity <- 1.0
                            anyFound <- true
        // rooms with possible continuations
        for i = 0 to 7 do
            for j = 0 to 7 do
                // an incomplete room that might have a push-block may yet reveal a transport
                if roomStates.[i,j].RoomType = DungeonRoomState.RoomType.MaybePushBlock && not(roomStates.[i,j].IsComplete) then
                    roomHighlights.[i,j].Opacity <- 1.0
                    anyFound <- true
                // a '?' stair may have been forgotten to traverse
                if roomStates.[i,j].RoomType = DungeonRoomState.RoomType.StaircaseToUnknown then
                    roomHighlights.[i,j].Opacity <- 1.0
                    anyFound <- true
                // only marked one of a transport pair
                match roomStates.[i,j].RoomType.KnownTransportNumber with
                | None -> ()
                | Some n -> 
                    if usedTransports.[n] = 1 then
                        roomHighlights.[i,j].Opacity <- 1.0
                        anyFound <- true
                // blocked? un-traversed doorway (could be key, moat, just forgotten, ...)
                if isThereARoom(i,j)=2 then
                    if i > 0 && horizontalDoors.[i-1,j].IsTraversible ||
                            i < 7 && horizontalDoors.[i,j].IsTraversible ||
                            j > 0 && verticalDoors.[i,j-1].IsTraversible ||
                            j < 7 && verticalDoors.[i,j].IsTraversible then
                        roomHighlights.[i,j].Opacity <- 1.0
                        anyFound <- true
        // Note: ladder blocks are kind of implicit, you either mark a door behind a moat, or it would show as a potential bomb wall
        if anyFound then
            for f in startAnimationFuncs do
                f()
            rightwardCanvas.Children.Clear()
            let blockersOutline = new Shapes.Rectangle(Width=106., Height=38., Stroke=Brushes.DarkCyan, StrokeThickness=3., IsHitTestVisible=false)
            rightwardCanvas.Children.Add(blockersOutline) |> ignore
            let explain = new TextBlock(TextWrapping=TextWrapping.Wrap, FontSize=14., Foreground=Brushes.Gray, Background=Brushes.Black, IsHitTestVisible=false, Margin=Thickness(3.),
                                            Text="""Where else can I go?

Mouse-hovering BLOCKERS for a few seconds displays these "possible continuations" in the dungeon.

Walls you might still need to try bombing are marked Cyan.

Rooms that might contain un-taken transport stairs, or that you have not visited, are circled Magenta.""")
            let b = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.0), Child=explain, Width=310., Background=Brushes.Black)
            Canvas.SetBottom(b, 0.)
            rightwardCanvas.Children.Add(b) |> ignore
    let unhighlight() =
        if isCurrentlyHighlighting then
            isCurrentlyHighlighting <- false
            for i = 0 to 6 do
                for j = 0 to 7 do
                    horizontalDoorHighlights.[i,j].Opacity <- 0.0
            for i = 0 to 7 do
                for j = 0 to 6 do
                    verticalDoorHighlights.[i,j].Opacity <- 0.0
            for i = 0 to 7 do
                for j = 0 to 7 do
                    roomHighlights.[i,j].Opacity <- 0.0
            for f in endAnimationFuncs do
                f()
            rightwardCanvas.Children.Clear()
    blockersHoverEvent.Publish.Add(fun b ->
        if level-1 = dungeonTabs.SelectedIndex then
            if b then
                if not finishedSetup then
                    finishedSetup <- true
                    //printfn "finishing dungeon %d highlight setup" level
                    dungeonBodyHighlightCanvas.Children.Add(setupCanvas) |> ignore   // this is a heavy thing, do it on-demand rather than during 'Loading UI' at start
                highlight()
            else
                unhighlight()
        )
