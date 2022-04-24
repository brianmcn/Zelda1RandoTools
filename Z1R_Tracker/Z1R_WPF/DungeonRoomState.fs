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
    | Patra
    | BlueWizzrobe
    | BlueDarknut
    | Manhandla
    | Other
    member this.AsHotKeyName() =
        match this with
        | Unmarked    -> "MonsterDetail_Unmarked"
        | Gleeok      -> "MonsterDetail_Gleeok"
        | Bow         -> "MonsterDetail_Bow"
        | Digdogger   -> "MonsterDetail_Digdogger"
        | BlueBubble  -> "MonsterDetail_BlueBubble"
        | RedBubble   -> "MonsterDetail_RedBubble"
        | Dodongo     -> "MonsterDetail_Dodongo"
        | Patra       -> "MonsterDetail_Patra"
        | BlueWizzrobe-> "MonsterDetail_BlueWizzrobe"
        | BlueDarknut -> "MonsterDetail_BlueDarknut"
        | Manhandla   -> "MonsterDetail_Manhandla"
        | Other       -> "MonsterDetail_Other"
    member this.IsNotMarked = this = MonsterDetail.Unmarked
    member this.Bmp() =
        match this with
        | Unmarked    -> null
        | Gleeok      -> Graphics.gleeok_bmp
        | Bow         -> Graphics.gohma_bmp
        | Digdogger   -> Graphics.digdogger_bmp
        | BlueBubble  -> Graphics.blue_bubble_bmp
        | RedBubble   -> Graphics.red_bubble_bmp
        | Dodongo     -> Graphics.dodongo_bmp
        | Patra       -> Graphics.patra_bmp
        | BlueWizzrobe-> Graphics.wizzrobe_bmp
        | BlueDarknut -> Graphics.blue_darknut_bmp
        | Manhandla   -> Graphics.manhandla_bmp
        | Other       -> Graphics.other_monster_bmp
    member this.DisplayDescription =
        match this with
        | Unmarked    -> "(None)"
        | Gleeok      -> "Gleeok"
        | Bow         -> "Gohma"
        | Digdogger   -> "Digdogger"
        | BlueBubble  -> "Blue Bubble"
        | RedBubble   -> "Red Bubble"
        | Dodongo     -> "Dodongo"
        | Patra       -> "Patra"
        | BlueWizzrobe-> "Blue Wizzrobe"
        | BlueDarknut -> "Blue Darknut"
        | Manhandla   -> "Manhandla"
        | Other       -> "Other"
    static member All() = 
        [| MonsterDetail.Gleeok; MonsterDetail.Bow; MonsterDetail.Digdogger; MonsterDetail.Dodongo; 
           MonsterDetail.Patra; MonsterDetail.BlueWizzrobe; MonsterDetail.BlueDarknut; MonsterDetail.Manhandla;
           MonsterDetail.Other; MonsterDetail.BlueBubble; MonsterDetail.RedBubble; MonsterDetail.Unmarked; |]
    static member FromHotKeyName(hkn) =
        let mutable r = MonsterDetail.Unmarked
        for x in MonsterDetail.All() do
            if x.AsHotKeyName()=hkn then
                r <- x
        r

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
    member this.Bmp() =
        match this with
        | Unmarked     -> null
        | Triforce     -> Graphics.zi_triforce_bmp
        | Heart        -> Graphics.zi_heart_bmp
        | OtherKeyItem -> Graphics.zi_other_item_bmp
        | BombPack     -> Graphics.zi_bomb_bmp
        | Key          -> Graphics.zi_key_bmp
        | FiveRupee    -> Graphics.zi_fiver_bmp
        | Map          -> Graphics.zi_map_bmp
        | Compass      -> Graphics.zi_compass_bmp
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
    static member All() =
        [| FloorDropDetail.Triforce; FloorDropDetail.Heart; FloorDropDetail.OtherKeyItem; FloorDropDetail.BombPack;
            FloorDropDetail.Key; FloorDropDetail.FiveRupee; FloorDropDetail.Map; FloorDropDetail.Compass; FloorDropDetail.Unmarked; |]
    static member FromHotKeyName(hkn) =
        let mutable r = FloorDropDetail.Unmarked
        for x in FloorDropDetail.All() do
            if x.AsHotKeyName()=hkn then
                r <- x
        r

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
    | LavaMoat
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
    // off the map
    | OffTheMap
    // level 9 only
    | Gannon   // (replace bomb upgrade)
    | Zelda   // (replace meat block)
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
        | LavaMoat                -> "RoomType_LavaMoat"
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
        | OffTheMap               -> "RoomType_OffTheMap"
        | Gannon                  -> "RoomType_Gannon"
        | Zelda                   -> "RoomType_Zelda"
    member this.IsNotMarked = this = RoomType.Unmarked
    member this.IsOldMan = this = RoomType.OldManHint || this = RoomType.BombUpgrade || this = RoomType.HungryGoriyaMeatBlock || this = RoomType.LifeOrMoney
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
        | ItemBasement            -> "Room with staircase to a basement item"
        | StaircaseToUnknown      -> "Room with staircase (unknown destination)"
        | Transport1              -> "Transport staircase #1 (one of matched pair)"
        | Transport2              -> "Transport staircase #2 (one of matched pair)"
        | Transport3              -> "Transport staircase #3 (one of matched pair)"
        | Transport4              -> "Transport staircase #4 (one of matched pair)"
        | Transport5              -> "Transport staircase #5 (one of matched pair)"
        | Transport6              -> "Transport staircase #6 (one of matched pair)"
        | Transport7              -> "Transport staircase #7 (one of matched pair)"
        | Transport8              -> "Transport staircase #8 (one of matched pair)"
        | Chevy                   -> "Chevy (four-way moat-ladder-block)"
        | DoubleMoat              -> "Double moat"
        | TopMoat                 -> "North moat"
        | RightMoat               -> "East moat"
        | CircleMoat              -> "Circle moat"
        | Tee                     -> "Tee (moat isolating south exit)"
        | LavaMoat                -> "Lava moat (weird shaped moat)"
        | VChute                  -> "Vertical chute"
        | HChute                  -> "Horizontal chute"
        | Turnstile               -> "Turnstile"
        | OldManHint              -> "NPC with hint"
        | BombUpgrade             -> "Bomb Upgrade (75-125 rupees)"
        | LifeOrMoney             -> "Life or Money (pay to escape) room"
        | HungryGoriyaMeatBlock   -> "Hungry Goriya (meat block)"
        | StartEnterFromE         -> "Dungeon entrance (from east)"
        | StartEnterFromW         -> "Dungeon entrance (from west)"
        | StartEnterFromN         -> "Dungeon entrance (from north)"
        | StartEnterFromS         -> "Dungeon entrance (from south)"
        | OffTheMap               -> "(Off the map)"
        | Gannon                  -> "Gannon"
        | Zelda                   -> "Zelda"
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
        | LavaMoat                -> Graphics.dungeonRoomBmpPairs.[33]
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
        | OffTheMap               -> Graphics.dungeonRoomBmpPairs.[30]
        | Gannon                  -> snd Graphics.dungeonRoomBmpPairs.[31], snd Graphics.dungeonRoomBmpPairs.[31]
        | Zelda                   -> snd Graphics.dungeonRoomBmpPairs.[32], snd Graphics.dungeonRoomBmpPairs.[32]
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
        RoomType.LavaMoat
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
        RoomType.OffTheMap
        RoomType.Gannon
        RoomType.Zelda
        RoomType.Unmarked
        |]
    static member FromHotKeyName(hkn) =
        let mutable r = RoomType.Unmarked
        for x in RoomType.All() do
            if x.AsHotKeyName()=hkn then
                r <- x
        r

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
    member this.IsEmpty = roomType.IsNotMarked || (roomType = RoomType.OffTheMap)
    member this.IsGannonOrZelda = (roomType = RoomType.Gannon) || (roomType = RoomType.Zelda)
    member this.MonsterDetail with get() = monsterDetail and set(x) = monsterDetail <- x
    member this.FloorDropDetail with get() = floorDropDetail and set(x) = floorDropDetail <- x
    member this.FloorDropAppearsBright with get() = floorDropShouldAppearBright
    member this.ToggleFloorDropBrightness() = floorDropShouldAppearBright <- not floorDropShouldAppearBright
    member this.CurrentDisplay() : FrameworkElement =
        // optimize the common case to avoid new-ing up an extra canvas
        if roomType = RoomType.Unmarked && monsterDetail = MonsterDetail.Unmarked && floorDropDetail = FloorDropDetail.Unmarked then
            upcast (Graphics.BMPtoImage (roomType.UncompletedBmp()))
        else
            let K = 18.
            let c = new Canvas(Width=13.*3., Height=9.*3.)  // will draw outside canvas
            match roomType with
            | RoomType.OffTheMap ->
                canvasAdd(c, new Canvas(Width=float(21*3), Height=float(17*3), Background=Brushes.Black, Opacity=0.6), -12., -12.)
            | _ ->
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
                let monsterIcon = Graphics.BMPtoImage(md.Bmp())
                canvasAdd(c, monsterIcon, -5., -3.)
                if isCompleted then
                    let shouldDarken =
                        match md with
                        | MonsterDetail.BlueBubble | MonsterDetail.RedBubble -> false
                        | _ -> true
                    if shouldDarken then
                        let dp = new DockPanel(Width=K, Height=K, Background=Brushes.Black, Opacity=DARKEN)
                        canvasAdd(c, dp, -5., -3.)
            match floorDropDetail with
            | FloorDropDetail.Unmarked -> ()
            | fd ->
                let floorDropIcon = Graphics.BMPtoImage(fd.Bmp())
                canvasAdd(c, floorDropIcon, 44.-K, 30.-K)
                if not floorDropShouldAppearBright then
                    let dp = new DockPanel(Width=K, Height=K, Background=Brushes.Black, Opacity=DARKEN)
                    canvasAdd(c, dp, 44.-K, 30.-K)
            upcast c

