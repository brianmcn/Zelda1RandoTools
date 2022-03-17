﻿module DungeonHighlightsUI

open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let canvasAdd = Graphics.canvasAdd

let makeHighlights(level, dungeonBodyHighlightCanvas, roomStates:DungeonRoomState.DungeonRoomState[,], currentOutlineDisplayState:int[],
                        horizontalDoors:Dungeon.Door[,], verticalDoors:Dungeon.Door[,], blockersHoverEvent:Event<_>) =
    // horizontal doors
    let horizontalDoorHighlights = Array2D.zeroCreate 7 8
    for i = 0 to 6 do
        for j = 0 to 7 do
            let d = new Canvas(Width=12., Height=16., IsHitTestVisible=false)
            let rect = new Shapes.Rectangle(Width=12., Height=16., Stroke=Brushes.Cyan, StrokeThickness=2., Fill=Dungeon.unknown, Opacity=0.)
            let st = new ScaleTransform(1.0, 1.0, CenterX=d.Width/2., CenterY=d.Height/2.)
            rect.RenderTransform <- st
            let anim = new Animation.DoubleAnimation(1.0, 1.3, new Duration(System.TimeSpan.FromSeconds(0.75)))
            anim.RepeatBehavior <- Animation.RepeatBehavior.Forever
            anim.AutoReverse <- true
            st.BeginAnimation(ScaleTransform.ScaleXProperty, anim)
            st.BeginAnimation(ScaleTransform.ScaleYProperty, anim)
            d.Children.Add(rect) |> ignore
            horizontalDoorHighlights.[i,j] <- rect
            canvasAdd(dungeonBodyHighlightCanvas, d, float(i*(39+12)+39), float(j*(27+12)+6))
    // vertical doors
    let verticalDoorHighlights = Array2D.zeroCreate 8 7
    for i = 0 to 7 do
        for j = 0 to 6 do
            let d = new Canvas(Width=24., Height=12., IsHitTestVisible=false)
            let rect = new Shapes.Rectangle(Width=24., Height=12., Stroke=Brushes.Cyan, StrokeThickness=2., Fill=Dungeon.unknown, Opacity=0.)
            let st = new ScaleTransform(1.0, 1.0, CenterX=d.Width/2., CenterY=d.Height/2.)
            rect.RenderTransform <- st
            let anim = new Animation.DoubleAnimation(1.0, 1.3, new Duration(System.TimeSpan.FromSeconds(0.75)))
            anim.RepeatBehavior <- Animation.RepeatBehavior.Forever
            anim.AutoReverse <- true
            st.BeginAnimation(ScaleTransform.ScaleXProperty, anim)
            st.BeginAnimation(ScaleTransform.ScaleYProperty, anim)
            d.Children.Add(rect) |> ignore
            verticalDoorHighlights.[i,j] <- rect
            canvasAdd(dungeonBodyHighlightCanvas, d, float(i*(39+12)+8), float(j*(27+12)+27))
    // rooms
    let roomHighlights = Array2D.zeroCreate 8 8
    for i = 0 to 7 do
        for j = 0 to 7 do
            let brush = new SolidColorBrush(Colors.Magenta)
            let extra = 6
            let ellipse = new Shapes.Ellipse(Width=float(13*3+12+2*extra), Height=float(9*3+12+2*extra), Stroke=brush, StrokeThickness=6., IsHitTestVisible=false, Opacity=0.)
            let len = 2.2
            ellipse.StrokeDashArray <- new DoubleCollection( seq[len;len] )
            let anim = new Animation.DoubleAnimation(0., len+len, new Duration(System.TimeSpan.FromSeconds(0.75)))
            anim.RepeatBehavior <- Animation.RepeatBehavior.Forever
            ellipse.BeginAnimation(Shapes.Ellipse.StrokeDashOffsetProperty, anim)
//            let canim = new Animation.ColorAnimation(Colors.Magenta, Colors.Cyan, new Duration(System.TimeSpan.FromSeconds(0.75)))
//            canim.AutoReverse <- true
//            canim.RepeatBehavior <- Animation.RepeatBehavior.Forever
//            brush.BeginAnimation(SolidColorBrush.ColorProperty, canim)
            canvasAdd(dungeonBodyHighlightCanvas, ellipse, float(i*51-12/2-extra), float(j*39-12/2-extra))
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
    let highlight() =
        // possible bomb walls
        for i = 0 to 6 do
            for j = 0 to 7 do
                if horizontalDoors.[i,j].State = Dungeon.DoorState.UNKNOWN then
                    if isThereARoom(i,j)+isThereARoom(i+1,j)=3 then // one yes and one maybe
                        horizontalDoorHighlights.[i,j].Opacity <- 1.0
        for i = 0 to 7 do
            for j = 0 to 6 do
                if verticalDoors.[i,j].State = Dungeon.DoorState.UNKNOWN then
                    if isThereARoom(i,j)+isThereARoom(i,j+1)=3 then // one yes and one maybe
                        // npc hints & bomb upgrades never can bomb north
                        if roomStates.[i,j+1].RoomType <> DungeonRoomState.RoomType.OldManHint && roomStates.[i,j+1].RoomType <> DungeonRoomState.RoomType.BombUpgrade then
                            verticalDoorHighlights.[i,j].Opacity <- 1.0
        // rooms with blockers
        for i = 0 to 7 do
            for j = 0 to 7 do
                // blocked by bow/recorder
                if roomStates.[i,j].MonsterDetail = DungeonRoomState.MonsterDetail.Bow ||
                    roomStates.[i,j].MonsterDetail = DungeonRoomState.MonsterDetail.Digdogger then  
                    // Note: even if room is 'complete', still considers blocker because e.g. standing Key, gohma, shutter
                    roomHighlights.[i,j].Opacity <- 1.0
                // blocked by meat
                if roomStates.[i,j].RoomType = DungeonRoomState.RoomType.HungryGoriyaMeatBlock && not(roomStates.[i,j].IsComplete) then
                    roomHighlights.[i,j].Opacity <- 1.0
                // blocked? un-traversed doorway (could be key, moat, just forgotten, ...)
                if isThereARoom(i,j)=2 then
                    if i > 0 && horizontalDoors.[i-1,j].IsYesOrLocked ||
                            i < 7 && horizontalDoors.[i,j].IsYesOrLocked ||
                            j > 0 && verticalDoors.[i,j-1].IsYesOrLocked ||
                            j < 7 && verticalDoors.[i,j].IsYesOrLocked then
                        roomHighlights.[i,j].Opacity <- 1.0
        // Note: ladder blocks are kind of implicit, you either mark a door behind a moat, or it would show as a potential bomb wall
    let unhighlight() =
        for i = 0 to 6 do
            for j = 0 to 7 do
                horizontalDoorHighlights.[i,j].Opacity <- 0.0
        for i = 0 to 7 do
            for j = 0 to 6 do
                verticalDoorHighlights.[i,j].Opacity <- 0.0
        for i = 0 to 7 do
            for j = 0 to 7 do
                roomHighlights.[i,j].Opacity <- 0.0
    blockersHoverEvent.Publish.Add(fun b ->
        if b then
            highlight()
        else
            unhighlight()
        )