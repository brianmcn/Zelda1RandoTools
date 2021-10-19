module DungeonRoomState

open System.Windows.Controls
open System.Windows.Media
open System.Windows

let canvasAdd = Graphics.canvasAdd

let mkTxt(txt) = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                              Text=txt, VerticalAlignment=VerticalAlignment.Center, BorderThickness=Thickness(0.))
        
[<RequireQualifiedAccess>]
type MonsterDetail =
    | Unmarked
    | Gleeok      
    | Bow         
    | Digdogger   
    | BlueBubble  
    | RedBubble   
    | Dodongo     
    member this.AsHotKeyName() =
        match this with
        | Unmarked    -> "MonsterDetail_Unmarked"
        | Gleeok      -> "MonsterDetail_Gleeok"
        | Bow         -> "MonsterDetail_Bow"
        | Digdogger   -> "MonsterDetail_Digdogger"
        | BlueBubble  -> "MonsterDetail_BlueBubble"
        | RedBubble   -> "MonsterDetail_RedBubble"
        | Dodongo     -> "MonsterDetail_Dodongo"
    member this.IsNotMarked = this = MonsterDetail.Unmarked
    member this.Bmp(bigIcons) =
        match this with
        | Unmarked    -> null
        | Gleeok      -> (if bigIcons then snd else fst) Graphics.dungeonRoomMonsters.[0]
        | Bow         -> (if bigIcons then snd else fst) Graphics.dungeonRoomMonsters.[1]
        | Digdogger   -> (if bigIcons then snd else fst) Graphics.dungeonRoomMonsters.[2]
        | BlueBubble  -> (if bigIcons then snd else fst) Graphics.dungeonRoomMonsters.[3]
        | RedBubble   -> (if bigIcons then snd else fst) Graphics.dungeonRoomMonsters.[4]
        | Dodongo     -> (if bigIcons then snd else fst) Graphics.dungeonRoomMonsters.[5]
    member this.DisplayDescription =
        match this with
        | Unmarked    -> "(None)"
        | Gleeok      -> "Gleeok"
        | Bow         -> "Gohma (need Bow)"
        | Digdogger   -> "Digdogger"
        | BlueBubble  -> "Blue Bubble"
        | RedBubble   -> "Red Bubble"
        | Dodongo     -> "Dodongo"
    member this.LegendIcon() =
        let tb = mkTxt(this.DisplayDescription)
        let sp = new StackPanel(Orientation=Orientation.Horizontal)
        if this.IsNotMarked then
            sp.Children.Add(new DockPanel(Width=24., Height=24.)) |> ignore
        else
            sp.Children.Add(Graphics.BMPtoImage(this.Bmp(true))) |> ignore
        sp.Children.Add(tb) |> ignore
        sp
    static member All() = 
        [| MonsterDetail.Gleeok; MonsterDetail.Bow; MonsterDetail.Digdogger; MonsterDetail.Dodongo; MonsterDetail.BlueBubble; MonsterDetail.RedBubble; MonsterDetail.Unmarked; |]

[<RequireQualifiedAccess>]
type FloorDropDetail =
    | Unmarked
    | Triforce
    | Heart
    | OtherKeyItem
    | BombPack
    | Key
    | FiveRupee
    | Map
    | Compass
    member this.AsHotKeyName() =
        match this with
        | Unmarked     -> "FloorDropDetail_Unmarked"
        | Triforce     -> "FloorDropDetail_Triforce"
        | Heart        -> "FloorDropDetail_Heart"
        | OtherKeyItem -> "FloorDropDetail_OtherKeyItem"
        | BombPack     -> "FloorDropDetail_BombPack"
        | Key          -> "FloorDropDetail_Key"
        | FiveRupee    -> "FloorDropDetail_FiveRupee"
        | Map          -> "FloorDropDetail_Map"
        | Compass      -> "FloorDropDetail_Compass"
    member this.IsNotMarked = this = FloorDropDetail.Unmarked
    member this.Bmp(bigIcons) =
        match this with
        | Unmarked     -> null
        | Triforce     -> (if bigIcons then snd else fst) Graphics.dungeonRoomFloorDrops.[0]
        | Heart        -> (if bigIcons then snd else fst) Graphics.dungeonRoomFloorDrops.[1]
        | OtherKeyItem -> (if bigIcons then snd else fst) Graphics.dungeonRoomFloorDrops.[2]
        | BombPack     -> (if bigIcons then snd else fst) Graphics.dungeonRoomFloorDrops.[3]
        | Key          -> (if bigIcons then snd else fst) Graphics.dungeonRoomFloorDrops.[4]
        | FiveRupee    -> (if bigIcons then snd else fst) Graphics.dungeonRoomFloorDrops.[5]
        | Map          -> (if bigIcons then snd else fst) Graphics.dungeonRoomFloorDrops.[6]
        | Compass      -> (if bigIcons then snd else fst) Graphics.dungeonRoomFloorDrops.[7]
    member this.DisplayDescription =
        match this with
        | Unmarked     -> "(None)"
        | Triforce     -> "Triforce"
        | Heart        -> "Heart Container"
        | OtherKeyItem -> "Other Key Item"
        | BombPack     -> "Bomb Pack"
        | Key          -> "Key (single use)"
        | FiveRupee    -> "Five-Rupee"
        | Map          -> "Map"
        | Compass      -> "Compass"
    member this.LegendIcon() =
        let tb = mkTxt(this.DisplayDescription)
        let sp = new StackPanel(Orientation=Orientation.Horizontal)
        if this.IsNotMarked then
            sp.Children.Add(new DockPanel(Width=24., Height=24.)) |> ignore
        else
            sp.Children.Add(Graphics.BMPtoImage(this.Bmp(true))) |> ignore
        sp.Children.Add(tb) |> ignore
        sp
    static member All() =
        [| FloorDropDetail.Triforce; FloorDropDetail.Heart; FloorDropDetail.OtherKeyItem; FloorDropDetail.BombPack;
            FloorDropDetail.Key; FloorDropDetail.FiveRupee; FloorDropDetail.Map; FloorDropDetail.Compass; FloorDropDetail.Unmarked; |]

[<RequireQualifiedAccess>]
type RoomType =
    | Unmarked
    | NonDescript
    // staircase types
    | MaybePushBlock
    | ItemBasement
    | StaircaseToUnknown
    | Transport1
    | Transport2
    | Transport3
    | Transport4
    | Transport5
    | Transport6
    | Transport7
    | Transport8
    // moats
    | Chevy
    | DoubleMoat
    | TopMoat
    | RightMoat
    | CircleMoat
    // other geometry
    | Tee
    | VChute
    | HChute
    | Turnstile
    // npcs
    | OldManHint
    | BombUpgrade
    | LifeOrMoney
    | HungryGoriyaMeatBlock
    // start rooms
    | StartEnterFromE
    | StartEnterFromW
    | StartEnterFromN
    | StartEnterFromS
    member this.AsHotKeyName() =
        match this with
        | Unmarked                -> "RoomType_Unmarked"
        | NonDescript             -> "RoomType_NonDescript"
        | MaybePushBlock          -> "RoomType_MaybePushBlock"
        | ItemBasement            -> "RoomType_ItemBasement"
        | StaircaseToUnknown      -> "RoomType_StaircaseToUnknown"
        | Transport1              -> "RoomType_Transport1"
        | Transport2              -> "RoomType_Transport2"
        | Transport3              -> "RoomType_Transport3"
        | Transport4              -> "RoomType_Transport4"
        | Transport5              -> "RoomType_Transport5"
        | Transport6              -> "RoomType_Transport6"
        | Transport7              -> "RoomType_Transport7"
        | Transport8              -> "RoomType_Transport8"
        | Chevy                   -> "RoomType_Chevy"
        | DoubleMoat              -> "RoomType_DoubleMoat"
        | TopMoat                 -> "RoomType_TopMoat"
        | RightMoat               -> "RoomType_RightMoat"
        | CircleMoat              -> "RoomType_CircleMoat"
        | Tee                     -> "RoomType_Tee"
        | VChute                  -> "RoomType_VChute"
        | HChute                  -> "RoomType_HChute"
        | Turnstile               -> "RoomType_Turnstile"
        | OldManHint              -> "RoomType_OldManHint"
        | BombUpgrade             -> "RoomType_BombUpgrade"
        | LifeOrMoney             -> "RoomType_LifeOrMoney"
        | HungryGoriyaMeatBlock   -> "RoomType_HungryGoriyaMeatBlock"
        | StartEnterFromE         -> "RoomType_StartEnterFromE"
        | StartEnterFromW         -> "RoomType_StartEnterFromW"
        | StartEnterFromN         -> "RoomType_StartEnterFromN"
        | StartEnterFromS         -> "RoomType_StartEnterFromS"
    member this.IsNotMarked = this = RoomType.Unmarked
    member this.NextEntranceRoom() = 
        match this with
        | RoomType.StartEnterFromS -> Some(RoomType.StartEnterFromW)
        | RoomType.StartEnterFromW -> Some(RoomType.StartEnterFromN)
        | RoomType.StartEnterFromN -> Some(RoomType.StartEnterFromE) 
        | RoomType.StartEnterFromE -> Some(RoomType.StartEnterFromS) 
        | _ -> None
    member this.KnownTransportNumber =
        match this with
        | Transport1 -> Some(1)
        | Transport2 -> Some(2)
        | Transport3 -> Some(3)
        | Transport4 -> Some(4)
        | Transport5 -> Some(5)
        | Transport6 -> Some(6)
        | Transport7 -> Some(7)
        | Transport8 -> Some(8)
        | _ -> None
    member this.DisplayDescription =
        match this with
        | Unmarked                -> "(Unmarked)"
        | NonDescript             -> "Empty/non-descript room"
        | MaybePushBlock          -> "Room which might have a staircase"
        | ItemBasement            -> "Room whose staircase leads to a basement item"
        | StaircaseToUnknown      -> "Room with staircase (unknown destination)"
        | Transport1              -> "Transport staircase #1 (one of a matched pair)"
        | Transport2              -> "Transport staircase #2 (one of a matched pair)"
        | Transport3              -> "Transport staircase #3 (one of a matched pair)"
        | Transport4              -> "Transport staircase #4 (one of a matched pair)"
        | Transport5              -> "Transport staircase #5 (one of a matched pair)"
        | Transport6              -> "Transport staircase #6 (one of a matched pair)"
        | Transport7              -> "Transport staircase #7 (one of a matched pair)"
        | Transport8              -> "Transport staircase #8 (one of a matched pair)"
        | Chevy                   -> "Chevy (four-way moat-ladder-block)"
        | DoubleMoat              -> "Double moat"
        | TopMoat                 -> "North moat"
        | RightMoat               -> "East moat"
        | CircleMoat              -> "Circle moat"
        | Tee                     -> "Tee (moat isolating south exit)"
        | VChute                  -> "Vertical chute"
        | HChute                  -> "Horizontal chute"
        | Turnstile               -> "Turnstile"
        | OldManHint              -> "NPC with hint"
        | BombUpgrade             -> "Bomb Upgrade"
        | LifeOrMoney             -> "Life or Money (pay to escape) room"
        | HungryGoriyaMeatBlock   -> "Hungry Goriya (meat block)"
        | StartEnterFromE         -> "Dungeon entrance (from east)"
        | StartEnterFromW         -> "Dungeon entrance (from west)"
        | StartEnterFromN         -> "Dungeon entrance (from north)"
        | StartEnterFromS         -> "Dungeon entrance (from south)"
    member private this.BmpPair() =
        match this with
        | Unmarked                -> fst Graphics.dungeonRoomBmpPairs.[0], fst Graphics.dungeonRoomBmpPairs.[0]
        | NonDescript             -> Graphics.dungeonRoomBmpPairs.[1]
        | MaybePushBlock          -> Graphics.dungeonRoomBmpPairs.[10]
        | ItemBasement            -> Graphics.dungeonRoomBmpPairs.[11]
        | StaircaseToUnknown      -> Graphics.dungeonRoomBmpPairs.[25]
        | Transport1              -> Graphics.dungeonRoomBmpPairs.[17]
        | Transport2              -> Graphics.dungeonRoomBmpPairs.[18]
        | Transport3              -> Graphics.dungeonRoomBmpPairs.[19]
        | Transport4              -> Graphics.dungeonRoomBmpPairs.[20]
        | Transport5              -> Graphics.dungeonRoomBmpPairs.[21]
        | Transport6              -> Graphics.dungeonRoomBmpPairs.[22]
        | Transport7              -> Graphics.dungeonRoomBmpPairs.[23]
        | Transport8              -> Graphics.dungeonRoomBmpPairs.[24]
        | Chevy                   -> Graphics.dungeonRoomBmpPairs.[3]
        | DoubleMoat              -> Graphics.dungeonRoomBmpPairs.[2]
        | TopMoat                 -> Graphics.dungeonRoomBmpPairs.[5]
        | RightMoat               -> Graphics.dungeonRoomBmpPairs.[4]
        | CircleMoat              -> Graphics.dungeonRoomBmpPairs.[6]
        | Tee                     -> Graphics.dungeonRoomBmpPairs.[9]
        | VChute                  -> Graphics.dungeonRoomBmpPairs.[7]
        | HChute                  -> Graphics.dungeonRoomBmpPairs.[8]
        | Turnstile               -> Graphics.dungeonRoomBmpPairs.[16]
        | OldManHint              -> Graphics.dungeonRoomBmpPairs.[12]
        | BombUpgrade             -> Graphics.dungeonRoomBmpPairs.[15]
        | LifeOrMoney             -> Graphics.dungeonRoomBmpPairs.[14]
        | HungryGoriyaMeatBlock   -> Graphics.dungeonRoomBmpPairs.[13]
        | StartEnterFromE         -> Graphics.dungeonRoomBmpPairs.[29]
        | StartEnterFromW         -> Graphics.dungeonRoomBmpPairs.[26]
        | StartEnterFromN         -> Graphics.dungeonRoomBmpPairs.[27]
        | StartEnterFromS         -> Graphics.dungeonRoomBmpPairs.[28]
    member this.CompletedBmp()   = this.BmpPair() |> snd
    member this.UncompletedBmp() = this.BmpPair() |> fst
    static member All() = [|
        RoomType.NonDescript
        RoomType.MaybePushBlock
        RoomType.ItemBasement
        RoomType.StaircaseToUnknown
        RoomType.Transport1
        RoomType.Transport2
        RoomType.Transport3
        RoomType.Transport4
        RoomType.Transport5
        RoomType.Transport6
        RoomType.Transport7
        RoomType.Transport8
        RoomType.Chevy
        RoomType.DoubleMoat
        RoomType.TopMoat
        RoomType.RightMoat
        RoomType.CircleMoat
        RoomType.Tee
        RoomType.VChute
        RoomType.HChute
        RoomType.Turnstile
        RoomType.OldManHint
        RoomType.BombUpgrade
        RoomType.LifeOrMoney
        RoomType.HungryGoriyaMeatBlock
        RoomType.StartEnterFromE
        RoomType.StartEnterFromW
        RoomType.StartEnterFromN
        RoomType.StartEnterFromS
        RoomType.Unmarked
        |]

let entranceRoomArrowColorBrush = 
    let c = (Graphics.dungeonRoomBmpPairs.[28] |> snd).GetPixel(18, 24)
    new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B))

let scale(bmp, scale) = 
    if bmp = null then
        null
    else
        let icon = Graphics.BMPtoImage(bmp)
        icon.Width <- icon.Width * scale
        icon.Height <- icon.Height * scale
        icon.Stretch <- Stretch.UniformToFill
        RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.NearestNeighbor)
        icon

type DungeonRoomState private(isCompleted, roomType, monsterDetail, floorDropDetail, floorDropShouldAppearBright) =
    let mutable isCompleted = isCompleted
    let mutable roomType = roomType
    let mutable monsterDetail = monsterDetail
    let mutable floorDropDetail = floorDropDetail
    let mutable floorDropShouldAppearBright = floorDropShouldAppearBright
    let DARKEN = 0.5
    new() = DungeonRoomState(false, RoomType.Unmarked, MonsterDetail.Unmarked, FloorDropDetail.Unmarked, true)
    member this.Clone() = new DungeonRoomState(isCompleted, roomType, monsterDetail, floorDropDetail, floorDropShouldAppearBright)
    member this.IsComplete with get() = isCompleted and set(x) = isCompleted <- x
    member this.RoomType with get() = roomType and set(x) = roomType <- x
    member this.IsEmpty = roomType.IsNotMarked
    member this.MonsterDetail with get() = monsterDetail and set(x) = monsterDetail <- x
    member this.FloorDropDetail with get() = floorDropDetail and set(x) = floorDropDetail <- x
    member this.ToggleFloorDropBrightness() = floorDropShouldAppearBright <- not floorDropShouldAppearBright
    member this.CurrentDisplay(bigIcons) =
        let K = if bigIcons then 24. else 15.
        let c = new Canvas(Width=13.*3., Height=9.*3.)  // will draw outside canvas
        let roomIcon = Graphics.BMPtoImage (if isCompleted then roomType.CompletedBmp() else roomType.UncompletedBmp())
        canvasAdd(c, roomIcon, 0., 0.)
        match roomType with
        | RoomType.StartEnterFromE -> canvasAdd(c, new Canvas(Background=entranceRoomArrowColorBrush, Width=3., Height=9.), 13.*3., 3.*3.)
        | RoomType.StartEnterFromW -> canvasAdd(c, new Canvas(Background=entranceRoomArrowColorBrush, Width=3., Height=9.), -1.*3., 3.*3.)
        | RoomType.StartEnterFromN -> canvasAdd(c, new Canvas(Background=entranceRoomArrowColorBrush, Width=9., Height=3.), 5.*3., -1.*3.)
        | RoomType.StartEnterFromS -> canvasAdd(c, new Canvas(Background=entranceRoomArrowColorBrush, Width=9., Height=3.), 5.*3., 9.*3.)
        | _ -> ()
        match monsterDetail with
        | MonsterDetail.Unmarked -> ()
        | md ->
            let monsterIcon = Graphics.BMPtoImage(md.Bmp(bigIcons))
            canvasAdd(c, monsterIcon, -3., -3.)
            if isCompleted then
                let shouldDarken =
                    match md with
                    | MonsterDetail.BlueBubble | MonsterDetail.RedBubble -> false
                    | _ -> true
                if shouldDarken then
                    let dp = new DockPanel(Width=K, Height=K, Background=Brushes.Black, Opacity=DARKEN)
                    canvasAdd(c, dp, -3., -3.)
        match floorDropDetail with
        | FloorDropDetail.Unmarked -> ()
        | fd ->
            let floorDropIcon = Graphics.BMPtoImage(fd.Bmp(bigIcons))
            canvasAdd(c, floorDropIcon, 42.-K, 30.-K)
            if not floorDropShouldAppearBright then
                let dp = new DockPanel(Width=K, Height=K, Background=Brushes.Black, Opacity=DARKEN)
                canvasAdd(c, dp, 42.-K, 30.-K)
        c

let dungeonRoomMouseButtonExplainerDecoration =
    let ST = CustomComboBoxes.borderThickness
    let h = 9.*3.*2.+ST*4.
    let d = new DockPanel(Height=h, LastChildFill=true, Background=Brushes.Black, Opacity=0.8)
    let mouseBMP = Graphics.mouseIconButtonColors2BMP
    let mouse = Graphics.BMPtoImage mouseBMP
    mouse.Height <- h
    mouse.Width <- float(mouseBMP.Width) * h / float(mouseBMP.Height)
    mouse.Stretch <- Stretch.Uniform
    let mouse = new Border(BorderThickness=Thickness(0.,0.,ST,0.), BorderBrush=Brushes.Gray, Child=mouse)
    d.Children.Add(mouse) |> ignore
    DockPanel.SetDock(mouse,Dock.Left)
    let sp = new StackPanel(Orientation=Orientation.Vertical, VerticalAlignment=VerticalAlignment.Bottom)
    d.Children.Add(sp) |> ignore
    let completed, uncompleted = new DungeonRoomState(), new DungeonRoomState()
    completed.RoomType <- RoomType.MaybePushBlock
    completed.IsComplete <- true
    uncompleted.RoomType <- RoomType.MaybePushBlock
    uncompleted.IsComplete <- false
    for color, text, pict in [Brushes.DarkMagenta,"Completed room",completed.CurrentDisplay(false); Brushes.DarkCyan,"Uncompleted room",uncompleted.CurrentDisplay(false)] do
        let p = new StackPanel(Orientation=Orientation.Horizontal, Margin=Thickness(ST))
        pict.Margin <- Thickness(ST,0.,2.*ST,0.)
        p.Children.Add(pict) |> ignore
        let tb = new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                                Text=text, VerticalAlignment=VerticalAlignment.Center, BorderThickness=Thickness(ST), BorderBrush=color)
        p.Children.Add(tb) |> ignore
        sp.Children.Add(p) |> ignore
    let b = new Border(Background=Brushes.Black, BorderThickness=Thickness(ST), BorderBrush=Brushes.DimGray, Child=d)
    b

let dungeonRoomMouseButtonExplainerDecoration2 =
    let ST = CustomComboBoxes.borderThickness
    let h = 9.*3.*2.+ST*4.
    let d = new DockPanel(Height=h, LastChildFill=true, Background=Brushes.Black, Opacity=0.8)
    let mouseBMP = Graphics.mouseIconButtonColors2BMP
    let mouse = Graphics.BMPtoImage mouseBMP
    mouse.Height <- h
    mouse.Width <- float(mouseBMP.Width) * h / float(mouseBMP.Height)
    mouse.Stretch <- Stretch.Uniform
    let mouse = new Border(BorderThickness=Thickness(0.,0.,ST,0.), BorderBrush=Brushes.Gray, Child=mouse)
    d.Children.Add(mouse) |> ignore
    DockPanel.SetDock(mouse,Dock.Left)
    let sp = new StackPanel(Orientation=Orientation.Vertical, VerticalAlignment=VerticalAlignment.Bottom)
    d.Children.Add(sp) |> ignore
    let completed, uncompleted = new DungeonRoomState(), new DungeonRoomState()
    completed.RoomType <- RoomType.MaybePushBlock
    completed.IsComplete <- true
    uncompleted.RoomType <- RoomType.MaybePushBlock
    uncompleted.IsComplete <- false
    for color, text, pict in [Brushes.DarkMagenta,"Done, take me back to the map",completed.CurrentDisplay(false); Brushes.DarkCyan,"I want to specify more details",uncompleted.CurrentDisplay(false)] do
        let p = new StackPanel(Orientation=Orientation.Horizontal, Margin=Thickness(ST))
        pict.Margin <- Thickness(ST,0.,2.*ST,0.)
        p.Children.Add(pict) |> ignore
        let tb = new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                                Text=text, VerticalAlignment=VerticalAlignment.Center, BorderThickness=Thickness(ST), BorderBrush=color)
        p.Children.Add(tb) |> ignore
        sp.Children.Add(p) |> ignore
    let b = new Border(Background=Brushes.Black, BorderThickness=Thickness(ST), BorderBrush=Brushes.DimGray, Child=d)
    b
                
let DoModalDungeonRoomSelectAndDecorate(cm:CustomComboBoxes.CanvasManager, originalRoomState:DungeonRoomState, usedTransports:_[], setNewValue, positionAtEntranceRoomIcons) = async {
    let tweak(im:Image) = im.Opacity <- 0.65; im
    let tileSunglasses = 0.75

    let popupCanvas = cm.CreatePopup(0.75)
    let workingCopy = originalRoomState.Clone()

    let makeCaption(txt, centered) = 
        let tb = new TextBox(Text=txt, FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, BorderThickness=Thickness(0.), IsReadOnly=true, IsHitTestVisible=false,
                                VerticalAlignment=VerticalAlignment.Center)
        if centered then
            tb.TextAlignment <- TextAlignment.Center
        tb
    let SCALE = 3.
    
    // FIRST choose a room type, accelerate into the popup selector
    let tileX,tileY = 535., 730.
    let brushes = (new CustomComboBoxes.ModalGridSelectBrushes(Brushes.Lime, Brushes.Lime, Brushes.Red, Brushes.Gray)).Dim(0.6)
    let ST = CustomComboBoxes.borderThickness
    let! allDone = async {
        let tileCanvas = new Canvas(Width=float(13*3)*SCALE, Height=float(9*3)*SCALE)
        let extra = 30.
        let tileSurroundingCanvas = new Canvas(Width=float(13*3)*SCALE+extra, Height=float(9*3)*SCALE+extra, Background=Brushes.DarkOliveGreen)
        canvasAdd(popupCanvas, tileSurroundingCanvas, tileX-extra/2., tileY-extra/2.)
        let grid = [|
                RoomType.DoubleMoat; RoomType.CircleMoat; RoomType.LifeOrMoney; RoomType.BombUpgrade; RoomType.HungryGoriyaMeatBlock; RoomType.StartEnterFromE;
                RoomType.TopMoat; RoomType.Chevy; RoomType.OldManHint; RoomType.VChute; RoomType.HChute; RoomType.StartEnterFromN;
                RoomType.RightMoat; RoomType.Unmarked; RoomType.MaybePushBlock; RoomType.NonDescript; RoomType.Tee; RoomType.StartEnterFromS;
                RoomType.ItemBasement; RoomType.StaircaseToUnknown; RoomType.Transport1; RoomType.Transport2; RoomType.Transport3; RoomType.StartEnterFromW;
                RoomType.Transport4; RoomType.Transport5; RoomType.Transport6; RoomType.Transport7; RoomType.Transport8; RoomType.Turnstile;
            |]
        let gridElementsSelectablesAndIDs : (FrameworkElement*_*_)[] = grid |> Array.mapi (fun _i rt ->
            let isLegal = (rt = originalRoomState.RoomType) || (match rt.KnownTransportNumber with | None -> true | Some n -> usedTransports.[n]<>2)
            upcast tweak(Graphics.BMPtoImage(rt.UncompletedBmp())), isLegal, rt
            )
        let originalStateIndex = grid |> Array.findIndex (fun x -> x = originalRoomState.RoomType)
        let activationDelta = 0
        let (gnc, gnr, gcw, grh) = 6, 5, 13*3, 9*3
        let totalGridWidth = float gnc*(float gcw + 2.*ST)
        let totalGridHeight = float gnr*(float grh + 2.*ST)
        let gx,gy = -78.,-50.-totalGridHeight
        let fullRoom = originalRoomState.Clone()  // a copy with the original decorations, used for redrawTile display
        fullRoom.IsComplete <- false
        let redrawTile(curState:RoomType) =
            Graphics.unparent(dungeonRoomMouseButtonExplainerDecoration2)
            tileCanvas.Children.Clear()
            fullRoom.RoomType <- curState
            let fullRoomDisplay = fullRoom.CurrentDisplay(false)
            fullRoomDisplay.RenderTransform <- new ScaleTransform(SCALE, SCALE)
            RenderOptions.SetBitmapScalingMode(fullRoomDisplay, BitmapScalingMode.NearestNeighbor)
            fullRoomDisplay.Opacity <- tileSunglasses
            canvasAdd(tileCanvas, fullRoomDisplay, 0., 0.)
            let textWidth = 340.
            let topText = Graphics.center(makeCaption("Select a room type", true), int textWidth, 24)
            let bottomText = Graphics.center(makeCaption(curState.DisplayDescription, true), int textWidth, 24)
            let dp = new DockPanel(Width=textWidth, Height=totalGridHeight+50., LastChildFill=false)
            DockPanel.SetDock(topText, Dock.Top)
            DockPanel.SetDock(bottomText, Dock.Bottom)
            dp.Children.Add(topText) |> ignore
            dp.Children.Add(bottomText) |> ignore
            let frame = new Border(BorderBrush=Brushes.DimGray, BorderThickness=Thickness(3.), Background=Brushes.Black, Child=dp)
            let sp = new StackPanel(Orientation=Orientation.Vertical)
            sp.Children.Add(dungeonRoomMouseButtonExplainerDecoration2) |> ignore
            sp.Children.Add(frame) |> ignore
            canvasAdd(tileCanvas, sp, gx+totalGridWidth/2.-textWidth/2., -97.-dp.Height)
        let onClick(ea:Input.MouseButtonEventArgs, curState) = 
            if (ea.ChangedButton = Input.MouseButton.Left || ea.ChangedButton = Input.MouseButton.Right) && ea.ButtonState = Input.MouseButtonState.Pressed then
                CustomComboBoxes.DismissPopupWithResult(curState, ea.ChangedButton = Input.MouseButton.Left)
            else
                CustomComboBoxes.StayPoppedUp
        let extraDecorations = [| |]
        let gridClickDismissalDoesMouseWarpBackToTileCenter = true
        if positionAtEntranceRoomIcons then
            // position mouse on entrance icons
            Graphics.WarpMouseCursorTo(Point(tileX+gx+5.5*(float gcw + ST*2.), tileY+gy+totalGridHeight/2.))
        elif originalRoomState.RoomType.IsNotMarked then
            // position mouse on center room (MaybePushBlock)
            Graphics.WarpMouseCursorTo(Point(tileX+gx+2.5*(float gcw + ST*2.), tileY+gy+totalGridHeight/2.))
        else
            // position moude on existing RoomType
            let x = originalStateIndex % gnc
            let y = originalStateIndex / gnc
            Graphics.WarpMouseCursorTo(Point(tileX+gx+(float x+0.5)*(float gcw + ST*2.), tileY+gy+(float y+0.5)*(float grh + ST*2.)))
        let! r = CustomComboBoxes.DoModalGridSelect(cm, tileX, tileY, tileCanvas, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (gnc, gnr, gcw, grh),
                                                    gx, gy, redrawTile, onClick, extraDecorations, brushes, gridClickDismissalDoesMouseWarpBackToTileCenter)
        workingCopy.IsComplete <- true
        popupCanvas.Children.Remove(tileSurroundingCanvas)
        match r with
        | None -> return true
        | Some(curState, allDone) -> 
            workingCopy.RoomType <- curState
            if allDone then
                setNewValue(workingCopy)
            return allDone
        }

    let makeBorderMouseOverBehavior(b:Border, otherEnterEffect, otherLeaveEffect) =
        b.MouseEnter.Add(fun _ -> b.BorderBrush <- Brushes.Lime; otherEnterEffect())
        b.MouseLeave.Add(fun _ -> b.BorderBrush <- Brushes.Gray; otherLeaveEffect())

    let cleanup() =
        popupCanvas.Children.Clear()  // un-parent the reusable dungeonRoomMouseButtonExplainerDecoration
        cm.DismissPopup()
    if allDone then
        cleanup()
    else
        // NEXT allow any number of optional decoration modifications, or let user left/right click the main tile to finish and choose IsComplete
        workingCopy.IsComplete <- false // to make the preview bright
        let snapBackWorkingCopy = workingCopy.Clone()  // state we may eventually commit
        let drmbed = dungeonRoomMouseButtonExplainerDecoration
        drmbed.BorderBrush <- Brushes.Gray
        let mre = new System.Threading.ManualResetEvent(false)
        let cleanupAndUnblock() = cleanup(); mre.Set() |> ignore
        let handlePossibleConfirmAndDismiss(ea:Input.MouseButtonEventArgs) =
            ea.Handled <- true
            if (ea.ChangedButton = Input.MouseButton.Left || ea.ChangedButton = Input.MouseButton.Right) && ea.ButtonState = Input.MouseButtonState.Pressed then
                snapBackWorkingCopy.IsComplete <- ea.ChangedButton = Input.MouseButton.Left
                setNewValue(snapBackWorkingCopy)
                cleanupAndUnblock()
        let mutable workingCopyDisplay = null
        popupCanvas.MouseDown.Add(fun _ -> cleanupAndUnblock())
        let redrawWorkingCopy() =
            if workingCopyDisplay <> null then
                popupCanvas.Children.Remove(workingCopyDisplay)
            workingCopyDisplay <- new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(1.), Child=workingCopy.CurrentDisplay(false))
            workingCopyDisplay.LayoutTransform <- new ScaleTransform(SCALE, SCALE)
            RenderOptions.SetBitmapScalingMode(workingCopyDisplay, BitmapScalingMode.NearestNeighbor)
            workingCopyDisplay.IsHitTestVisible <- true
            makeBorderMouseOverBehavior(workingCopyDisplay, (fun () -> drmbed.BorderBrush <- Brushes.Lime), (fun () -> drmbed.BorderBrush <- Brushes.Gray))
            workingCopyDisplay.MouseDown.Add(fun ea -> handlePossibleConfirmAndDismiss(ea))
            Canvas.SetLeft(workingCopyDisplay, tileX)
            Canvas.SetTop(workingCopyDisplay, tileY)
            popupCanvas.Children.Insert(0, workingCopyDisplay)  // add to front, so draw under dashes and corner decos
        redrawWorkingCopy()

        let txt1 = "Optionally, modify corner decorations\n" +
                    "by clicking decorations here.\n" +
                    "Then click the tile below to finish,\n" +
                    "using the appropriate mouse button\n" +
                    "to mark whether the room has been completed"
        let cap = Graphics.center(makeCaption(txt1, true), 360, 120)
        cap.Opacity <- 0.9
        let explainer1 = new Border(BorderBrush=Brushes.DimGray, BorderThickness=Thickness(1.), Background=Brushes.Black, Child=cap)
        canvasAdd(popupCanvas, explainer1, 406., tileY-430.)
        Canvas.SetRight(drmbed, popupCanvas.Width - tileX + 10.)
        Canvas.SetBottom(drmbed, popupCanvas.Height - (tileY+(9.*3.+2.)*SCALE))
        popupCanvas.Children.Add(drmbed) |> ignore

        let monsterPanel = new StackPanel(Orientation=Orientation.Vertical)
        for m in MonsterDetail.All() do
            let icon = m.LegendIcon()
            let b = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Background=Brushes.Black, Child=icon)
            makeBorderMouseOverBehavior(b, (fun () -> 
                workingCopy.MonsterDetail <- m
                redrawWorkingCopy()), (fun () -> 
                workingCopy.MonsterDetail <- snapBackWorkingCopy.MonsterDetail
                redrawWorkingCopy()))
            b.MouseDown.Add(fun ea ->
                ea.Handled <- true
                snapBackWorkingCopy.MonsterDetail <- m
                workingCopy.MonsterDetail <- m
                redrawWorkingCopy()
                async {
                    monsterPanel.Opacity <- 0.5
                    monsterPanel.IsEnabled <- false
                    do! Async.Sleep(1500)
                    monsterPanel.IsEnabled <- true
                    monsterPanel.Opacity <- 1.0
                } |> Async.StartImmediate
                )
            monsterPanel.Children.Add(b) |> ignore
        let b = new Border(BorderThickness=Thickness(0.), Background=Brushes.Black, Child=monsterPanel)
        // just after the user clicking a selection, during the 'cooldown', don't want a click to cancel the whole popup; let it be accelerator for confirm & dismiss, like clicking preview tile
        b.MouseDown.Add(fun ea -> handlePossibleConfirmAndDismiss(ea))   
        Canvas.SetLeft(b, 406.)
        Canvas.SetBottom(b, popupCanvas.Height - tileY + 20.)
        popupCanvas.Children.Add(b) |> ignore

        let floorDropPanel = new StackPanel(Orientation=Orientation.Vertical)
        for f in FloorDropDetail.All() do
            let icon = f.LegendIcon()
            let b = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Background=Brushes.Black, Child=icon)
            makeBorderMouseOverBehavior(b, (fun () -> 
                workingCopy.FloorDropDetail <- f
                redrawWorkingCopy()), (fun () -> 
                workingCopy.FloorDropDetail <- snapBackWorkingCopy.FloorDropDetail
                redrawWorkingCopy()))
            b.MouseDown.Add(fun ea ->
                ea.Handled <- true
                snapBackWorkingCopy.FloorDropDetail <- f
                workingCopy.FloorDropDetail <- f
                redrawWorkingCopy()
                async {
                    floorDropPanel.Opacity <- 0.5
                    floorDropPanel.IsEnabled <- false
                    do! Async.Sleep(1500)
                    floorDropPanel.IsEnabled <- true
                    floorDropPanel.Opacity <- 1.0
                } |> Async.StartImmediate
                )
            floorDropPanel.Children.Add(b) |> ignore
        let b = new Border(BorderThickness=Thickness(0.), Background=Brushes.Black, Child=floorDropPanel)
        // just after the user clicking a selection, during the 'cooldown', don't want a click to cancel the whole popup; let it be accelerator for confirm & dismiss, like clicking preview tile
        b.MouseDown.Add(fun ea -> handlePossibleConfirmAndDismiss(ea))   
        Canvas.SetRight(b, 0.)
        Canvas.SetBottom(b, popupCanvas.Height - tileY + 20.)
        popupCanvas.Children.Add(b) |> ignore

        let! _ = Async.AwaitWaitHandle(mre)
        ()
    }