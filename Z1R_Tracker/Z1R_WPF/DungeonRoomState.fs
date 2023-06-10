module DungeonRoomState

open System.Windows.Controls
open System.Windows.Media
open System.Windows

let canvasAdd = Graphics.canvasAdd

let mkTxt(txt) = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                              Text=txt, VerticalAlignment=VerticalAlignment.Center, BorderThickness=Thickness(0.))

[<RequireQualifiedAccess>]
type DoorDirection =
    | East
    | West
    | North
    | South
[<RequireQualifiedAccess>]
type DoorAction =
    | Increment
    | Decrement
type DoorHotKeyResponse(dd:DoorDirection,da:DoorAction) =
    member _this.Direction = dd
    member _this.Action = da

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
    | Vire
    | Zol
    | PolsVoice
    | RedTektite
    | RedGoriya
    | Rope
    | Stalfos
    | Wallmaster
    | Gel
    | Keese
    | Likelike
    | Gibdo
    | RedLynel
    | BlueMoblin
    | Aquamentus
    | BlueLanmola
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
        | Vire        -> "MonsterDetail_Vire"
        | Zol         -> "MonsterDetail_Zol"
        | PolsVoice   -> "MonsterDetail_PolsVoice"
        | RedTektite  -> "MonsterDetail_RedTektite"
        | RedGoriya   -> "MonsterDetail_RedGoriya"
        | Rope        -> "MonsterDetail_Rope"
        | Stalfos     -> "MonsterDetail_Stalfos"
        | Wallmaster  -> "MonsterDetail_Wallmaster"
        | Gel         -> "MonsterDetail_Gel"
        | Keese       -> "MonsterDetail_Keese"
        | Likelike    -> "MonsterDetail_Likelike"
        | Gibdo       -> "MonsterDetail_Gibdo"
        | RedLynel    -> "MonsterDetail_RedLynel"
        | BlueMoblin  -> "MonsterDetail_BlueMoblin"
        | Aquamentus  -> "MonsterDetail_Aquamentus"
        | BlueLanmola -> "MonsterDetail_BlueLanmola"
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
        | Vire        -> Graphics.vire_bmp
        | Zol         -> Graphics.zol_bmp
        | PolsVoice   -> Graphics.pols_voice_bmp
        | RedTektite  -> Graphics.red_tektite
        | RedGoriya   -> Graphics.red_goriya
        | Rope        -> Graphics.rope
        | Stalfos     -> Graphics.stalfos
        | Wallmaster  -> Graphics.wallmaster
        | Gel         -> Graphics.gel
        | Keese       -> Graphics.keese
        | Likelike    -> Graphics.likelike
        | Gibdo       -> Graphics.gibdo
        | RedLynel    -> Graphics.red_lynel
        | BlueMoblin  -> Graphics.blue_moblin
        | Aquamentus  -> Graphics.aquamentus
        | BlueLanmola -> Graphics.blue_lanmola
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
        | BlueWizzrobe-> "Wizzrobe"
        | BlueDarknut -> "Darknut"
        | Manhandla   -> "Manhandla"
        | Vire        -> "Vire"
        | Zol         -> "Zol"
        | PolsVoice   -> "Pols Voice"
        | RedGoriya   -> "Goriya"
        | RedTektite  -> "Tektite"
        | Rope        -> "Rope"
        | Stalfos     -> "Stalfos"
        | Wallmaster  -> "Wallmaster"
        | Gel         -> "Gel"
        | Keese       -> "Keese"
        | Likelike    -> "Likelike"
        | Gibdo       -> "Gibdo"
        | RedLynel    -> "Lynel"
        | BlueMoblin  -> "Moblin"
        | Aquamentus  -> "Aquamentus"
        | BlueLanmola -> "Lanmola"
        | Other       -> "Other"
    static member All() = 
        [| MonsterDetail.Gleeok; MonsterDetail.Bow; MonsterDetail.Digdogger; MonsterDetail.Dodongo; MonsterDetail.Patra; MonsterDetail.Manhandla; MonsterDetail.Aquamentus; 
           MonsterDetail.BlueLanmola; MonsterDetail.BlueWizzrobe; MonsterDetail.BlueDarknut; MonsterDetail.RedLynel; MonsterDetail.PolsVoice; MonsterDetail.RedGoriya; MonsterDetail.Gibdo; 
           MonsterDetail.Vire; MonsterDetail.Keese; MonsterDetail.Zol; MonsterDetail.Gel; MonsterDetail.Stalfos; MonsterDetail.Wallmaster; MonsterDetail.Likelike; 
           MonsterDetail.Other; MonsterDetail.Rope; MonsterDetail.BlueMoblin; MonsterDetail.RedTektite; MonsterDetail.BlueBubble; MonsterDetail.RedBubble; MonsterDetail.Unmarked; |]
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
        | BombPack     -> Graphics.zi_alt_bomb_bmp
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
    member this.IsOffMap = this = RoomType.OffTheMap
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
    member private this.BmpPair(pairs:_[]) =
        match this with
        | Unmarked                -> fst pairs.[0], fst pairs.[0]
        | NonDescript             -> pairs.[1]
        | MaybePushBlock          -> pairs.[10]
        | ItemBasement            -> pairs.[11]
        | StaircaseToUnknown      -> pairs.[25]
        | Transport1              -> pairs.[17]
        | Transport2              -> pairs.[18]
        | Transport3              -> pairs.[19]
        | Transport4              -> pairs.[20]
        | Transport5              -> pairs.[21]
        | Transport6              -> pairs.[22]
        | Transport7              -> pairs.[23]
        | Transport8              -> pairs.[24]
        | Chevy                   -> pairs.[3]
        | DoubleMoat              -> pairs.[2]
        | TopMoat                 -> pairs.[5]
        | RightMoat               -> pairs.[4]
        | CircleMoat              -> pairs.[6]
        | Tee                     -> pairs.[9]
        | LavaMoat                -> pairs.[33]
        | VChute                  -> pairs.[7]
        | HChute                  -> pairs.[8]
        | Turnstile               -> pairs.[16]
        | OldManHint              -> 
            if TrackerModelOptions.BookForHelpfulHints.Value then pairs.[12]
            else snd pairs.[12], snd pairs.[12]
        | BombUpgrade             -> pairs.[15]
        | LifeOrMoney             -> pairs.[14]
        | HungryGoriyaMeatBlock   -> pairs.[13]
        | StartEnterFromE         -> pairs.[29]
        | StartEnterFromW         -> pairs.[26]
        | StartEnterFromN         -> pairs.[27]
        | StartEnterFromS         -> pairs.[28]
        | OffTheMap               -> pairs.[30]
        | Gannon                  -> snd pairs.[31], snd pairs.[31]
        | Zelda                   -> snd pairs.[32], snd pairs.[32]
    member this.CompletedBmp()   = this.BmpPair(Graphics.dungeonRoomBmpPairs) |> snd
    member this.UncompletedBmp() = this.BmpPair(Graphics.dungeonRoomBmpPairs) |> fst
    member this.TinyCompletedBmp()   = this.BmpPair(Graphics.dungeonRoomTinyBmpPairs) |> snd
    member this.TinyUncompletedBmp() = this.BmpPair(Graphics.dungeonRoomTinyBmpPairs) |> fst
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

let mutable isDoingDragPaintOffTheMap = false
let veryDark = new SolidColorBrush(Color.FromArgb(255uy, 60uy, 10uy, 20uy))
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
                let black = new Canvas(Width=13.*3.+12., Height=9.*3.+12., Background=Brushes.Black, Opacity=0.6)
                canvasAdd(c, black, -6., -6.)
                if isDoingDragPaintOffTheMap then
                    //canvasAdd(black, new Shapes.Rectangle(Width=black.Width, Height=black.Height, Stroke=veryDark, StrokeThickness=1.), 0., 0.)
                    canvasAdd(black, new Shapes.Rectangle(Width=13.*3., Height=9.*3., Stroke=veryDark, StrokeThickness=2.), 6., 6.)
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

