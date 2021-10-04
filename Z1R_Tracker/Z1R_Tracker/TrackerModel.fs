module TrackerModel

// model to track the state of the tracker independent of any UI/graphics layer

//////////////////////////////////////////////////////////////////////////////////////////
// Options

open System.Text.Json
open System.Text.Json.Serialization

[<RequireQualifiedAccess>]
type ReminderCategory =
    | DungeonFeedback
    | SwordHearts
    | CoastItem
    | RecorderPBSpotsAndBoomstickBook
    | HaveKeyLadder
    | Blockers

module Options =
    type Bool(init) =
        let mutable v = init
        member this.Value with get() = v and set(x) = v <- x
    module Overworld =
        let mutable DrawRoutes = Bool(true)
        let mutable HighlightNearby = Bool(true)
        let mutable ShowMagnifier = Bool(true)
    module VoiceReminders =
        let mutable DungeonFeedback = Bool(true)
        let mutable SwordHearts = Bool(true)
        let mutable CoastItem = Bool(true)
        let mutable RecorderPBSpotsAndBoomstickBook = Bool(true)
        let mutable HaveKeyLadder = Bool(true)
        let mutable Blockers = Bool(true)
    module VisualReminders =
        let mutable DungeonFeedback = Bool(true)
        let mutable SwordHearts = Bool(true)
        let mutable CoastItem = Bool(true)
        let mutable RecorderPBSpotsAndBoomstickBook = Bool(true)
        let mutable HaveKeyLadder = Bool(true)
        let mutable Blockers = Bool(true)
    let mutable ListenForSpeech = Bool(true)
    let mutable RequirePTTForSpeech = Bool(false)
    let mutable PlaySoundWhenUseSpeech = Bool(true)
    let mutable IsSecondQuestDungeons = Bool(false)
    let mutable MirrorOverworld = Bool(false)
    let mutable ShowBroadcastWindow = Bool(false)
    let mutable IsMuted = false
    let mutable Volume = 30

    type ReadWrite() =
        member val DrawRoutes = true with get,set
        member val HighlightNearby = true with get,set
        member val ShowMagnifier = true with get,set

        member val Voice_DungeonFeedback = true with get,set
        member val Voice_SwordHearts = true with get,set
        member val Voice_CoastItem = true with get,set
        member val Voice_RecorderPBSpotsAndBoomstickBook = true with get,set
        member val Voice_HaveKeyLadder = true with get,set
        member val Voice_Blockers = true with get,set

        member val Visual_DungeonFeedback = true with get,set
        member val Visual_SwordHearts = true with get,set
        member val Visual_CoastItem = true with get,set
        member val Visual_RecorderPBSpotsAndBoomstickBook = true with get,set
        member val Visual_HaveKeyLadder = true with get,set
        member val Visual_Blockers = true with get,set
        
        member val ListenForSpeech = true with get,set
        member val RequirePTTForSpeech = false with get,set
        member val PlaySoundWhenUseSpeech = true with get,set
        member val IsSecondQuestDungeons = false with get,set
        member val MirrorOverworld = false with get,set
        member val ShowBroadcastWindow = false with get,set

        member val IsMuted = false with get, set
        member val Volume = 30 with get, set

    let mutable private cachedSettingJson = null

    let private read(filename) =
        try
            cachedSettingJson <- System.IO.File.ReadAllText(filename)
            let data = JsonSerializer.Deserialize<ReadWrite>(cachedSettingJson, new JsonSerializerOptions(AllowTrailingCommas=true))
            Overworld.DrawRoutes.Value <- data.DrawRoutes
            Overworld.HighlightNearby.Value <- data.HighlightNearby
            Overworld.ShowMagnifier.Value <- data.ShowMagnifier

            VoiceReminders.DungeonFeedback.Value <- data.Voice_DungeonFeedback
            VoiceReminders.SwordHearts.Value <-     data.Voice_SwordHearts
            VoiceReminders.CoastItem.Value <-       data.Voice_CoastItem
            VoiceReminders.RecorderPBSpotsAndBoomstickBook.Value <- data.Voice_RecorderPBSpotsAndBoomstickBook
            VoiceReminders.HaveKeyLadder.Value <-   data.Voice_HaveKeyLadder
            VoiceReminders.Blockers.Value <-        data.Voice_Blockers

            VisualReminders.DungeonFeedback.Value <- data.Visual_DungeonFeedback
            VisualReminders.SwordHearts.Value <-     data.Visual_SwordHearts
            VisualReminders.CoastItem.Value <-       data.Visual_CoastItem
            VisualReminders.RecorderPBSpotsAndBoomstickBook.Value <- data.Visual_RecorderPBSpotsAndBoomstickBook
            VisualReminders.HaveKeyLadder.Value <-   data.Visual_HaveKeyLadder
            VisualReminders.Blockers.Value <-        data.Visual_Blockers

            ListenForSpeech.Value <- data.ListenForSpeech
            RequirePTTForSpeech.Value <- data.RequirePTTForSpeech
            PlaySoundWhenUseSpeech.Value <- data.PlaySoundWhenUseSpeech
            IsSecondQuestDungeons.Value <- data.IsSecondQuestDungeons
            MirrorOverworld.Value <- data.MirrorOverworld
            ShowBroadcastWindow.Value <- data.ShowBroadcastWindow
            IsMuted <- data.IsMuted
            Volume <- max 0 (min 100 data.Volume)
        with e ->
            printfn "failed to read settings file '%s':" filename 
            printfn "%s" (e.ToString())
            printfn ""

    let private write(filename) =
        let data = ReadWrite()
        data.DrawRoutes <- Overworld.DrawRoutes.Value
        data.HighlightNearby <- Overworld.HighlightNearby.Value
        data.ShowMagnifier <- Overworld.ShowMagnifier.Value

        data.Voice_DungeonFeedback <- VoiceReminders.DungeonFeedback.Value
        data.Voice_SwordHearts <-     VoiceReminders.SwordHearts.Value
        data.Voice_CoastItem <-       VoiceReminders.CoastItem.Value
        data.Voice_RecorderPBSpotsAndBoomstickBook <- VoiceReminders.RecorderPBSpotsAndBoomstickBook.Value
        data.Voice_HaveKeyLadder <-   VoiceReminders.HaveKeyLadder.Value
        data.Voice_Blockers <-        VoiceReminders.Blockers.Value

        data.Visual_DungeonFeedback <- VisualReminders.DungeonFeedback.Value
        data.Visual_SwordHearts <-     VisualReminders.SwordHearts.Value
        data.Visual_CoastItem <-       VisualReminders.CoastItem.Value
        data.Visual_RecorderPBSpotsAndBoomstickBook <- VisualReminders.RecorderPBSpotsAndBoomstickBook.Value
        data.Visual_HaveKeyLadder <-   VisualReminders.HaveKeyLadder.Value
        data.Visual_Blockers <-        VisualReminders.Blockers.Value

        data.ListenForSpeech <- ListenForSpeech.Value
        data.RequirePTTForSpeech <- RequirePTTForSpeech.Value
        data.PlaySoundWhenUseSpeech <- PlaySoundWhenUseSpeech.Value
        data.IsSecondQuestDungeons <- IsSecondQuestDungeons.Value
        data.MirrorOverworld <- MirrorOverworld.Value
        data.ShowBroadcastWindow <- ShowBroadcastWindow.Value
        data.IsMuted <- IsMuted
        data.Volume <- Volume

        try
            let json = JsonSerializer.Serialize<ReadWrite>(data, new JsonSerializerOptions(WriteIndented=true))
            if json <> cachedSettingJson then
                cachedSettingJson <- json
                System.IO.File.WriteAllText(filename, cachedSettingJson)
        with e ->
            printfn "failed to write settings file '%s':" filename
            printfn "%s" (e.ToString())
            printfn ""

    let mutable private settingsFile = null
    
    let readSettings() =
        if settingsFile = null then
            settingsFile <- System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Z1R_Tracker_settings.json")
        read(settingsFile)
    let writeSettings() =
        if settingsFile = null then
            settingsFile <- System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Z1R_Tracker_settings.json")
        write(settingsFile)

///////////////////////////////////////////////////////////////////////////

// abstraction for a set of scrollable choices
type ChoiceDomain(name:string,maxUsesArray:int[]) =
    // index keys are integers used as identifiers, values are max number of times it can appear among a set of cells
    // examples: ladder has max use of 1, heart container has max use of 9 in heartShuffle mode, bomb shop has max use of 999 (infinity)
    let name = name
    let maxUsesArray = Array.copy maxUsesArray
    let uses = Array.zeroCreate maxUsesArray.Length
    let ev = new Event<_>()
    [<CLIEvent>]
    member _this.Changed = ev.Publish
    member _this.Name = name  // just useful for debugging etc
    member _this.MaxKey = uses.Length-1
    member this.RemoveUse(key) =
        if key = -1 then  // since Next/Prev FreeKey can return -1 to mean the implicit empty slot, seems ok to RemoveUse() it, it implicitly has infinity current uses
            ev.Trigger(this,key)
        else
            if uses.[key] > 0 then
                uses.[key] <- uses.[key] - 1
                ev.Trigger(this,key)
            else
                failwith "choice domain underflow"
    member this.AddUse(key) =
        if key = -1 then  // since Next/Prev FreeKey can return -1 to mean the implicit empty slot, seems ok to AddUse() it, it implicitly has infinity max uses
            ev.Trigger(this,key)
        else
            if uses.[key] >= maxUsesArray.[key] then
                failwith "choice domain overflow"
            else
                uses.[key] <- uses.[key] + 1
                ev.Trigger(this,key)
    member this.NextFreeKeyWithAllowance(key, allowance) =
        if key = uses.Length-1 then
            -1
        elif (allowance = key+1) || (uses.[key+1] < maxUsesArray.[key+1]) then
            key+1
        else
            this.NextFreeKeyWithAllowance(key+1, allowance)
    member this.NextFreeKey(key) = this.NextFreeKeyWithAllowance(key, -999)  
    member this.PrevFreeKeyWithAllowance(key, allowance) =
        if key = -1 then
            this.PrevFreeKeyWithAllowance(uses.Length, allowance)
        elif key = 0 then
            -1
        elif (allowance = key-1) || (uses.[key-1] < maxUsesArray.[key-1]) then
            key-1
        else
            this.PrevFreeKeyWithAllowance(key-1, allowance)
    member this.PrevFreeKey(key) = this.PrevFreeKeyWithAllowance(key, -999)  
    member _this.NumUses(key) = uses.[key]
    member _this.MaxUses(key) = maxUsesArray.[key]
    member _this.CanAddUse(key) = (key = -1) || (uses.[key] < maxUsesArray.[key])
        
type Cell(cd:ChoiceDomain) =
    // a location that can hold one item, e.g. armos box that can hold red candle or white sword or whatnot, or
    // map square that can hold dungeon 4 or bomb shop or whatnot
    // this is about player-knowing-location-contents, not about players _having_ the things there
    let mutable state = -1 // -1 means empty, 0-N are item identifiers
    member _this.Current() = state
    member this.Next() =
        if state <> -1 then
            cd.RemoveUse(state)
        state <- cd.NextFreeKey(state)
        if state <> -1 then
            cd.AddUse(state)
    member this.Prev() =
        if state <> -1 then
            cd.RemoveUse(state)
        state <- cd.PrevFreeKey(state)
        if state <> -1 then
            cd.AddUse(state)
    member this.Set(newState) =
        if newState < -1 || newState > cd.MaxKey then
            failwith "Cell.Set out of range"
        if state = newState then
            ()
        else
            cd.AddUse(newState)
            cd.RemoveUse(state)
            state <- newState
    member this.AttemptToSet(newState) =
        try
            this.Set(newState)
            true
        with _e -> 
            false

//////////////////////////////////////////////////////////////////////////////////////////

module ITEMS =
    let BOOKSHIELD = 0
    let BOOMERANG = 1
    let BOW = 2
    let POWERBRACELET = 3
    let LADDER = 4
    let MAGICBOOMERANG = 5
    let KEY = 6
    let RAFT = 7
    let RECORDER = 8
    let REDCANDLE = 9
    let REDRING = 10
    let SILVERARROW = 11
    let WAND = 12
    let WHITESWORD = 13
    let HEARTCONTAINER = 14
    let AsPronounceString(n, isbook) =
        match n with
        | 0 -> if isbook then "book" else "shield"
        | 1 -> "boomerang"
        | 2 -> "beau"
        | 3 -> "power bracelet"
        | 4 -> "ladder"
        | 5 -> "magic boomerang"
        | 6 -> "any key"
        | 7 -> "raft"
        | 8 -> "recorder"
        | 9 -> "red candle"
        | 10 -> "red ring"
        | 11 -> "silver arrow"
        | 12 -> "wand"
        | 13 -> "white sword"
        | 14 -> "heart container"
        | _ -> failwith "bad ITEMS id"

let allItemWithHeartShuffleChoiceDomain = ChoiceDomain("allItemsWithHeartShuffle", [|
    1 // book 
    1 // boomerang
    1 // bow
    1 // power_bracelet
    1 // ladder
    1 // magic_boomerang
    1 // key
    1 // raft 
    1 // recorder
    1 // red_candle
    1 // red_ring
    1 // silver_arrow
    1 // wand
    1 // white_sword
    9 // heart_container
    |])

//////////////////////////////////////////////////////////////////////////////////////////

let mapSquareChoiceDomain = ChoiceDomain("mapSquare", [|
    1 // dungeon 1          // 0
    1 // dungeon 2
    1 // dungeon 3
    1 // dungeon 4
    1 // dungeon 5
    1 // dungeon 6          // 5
    1 // dungeon 7
    1 // dungeon 8
    1 // dungeon 9
    1 // any road 1
    1 // any road 2         // 10 
    1 // any road 3
    1 // any road 4
    1 // sword 3
    1 // sword 2
    1 // sword 1            // 15
    999 // arrow shop
    999 // bomb shop
    999 // book shop             
    999 // candle shop      
    999 // blue ring shop   // 20
    999 // meat shop
    999 // key shop
    999 // shield shop
    1   // armos item
    999 // hint shop        // 25
    4   // take any         
    999 // potion shop      
    999 // money
    999 // X (nothing, but visited)
    |])
// Note: if you make changes to above/below, also check: recomputeMapStateSummary(), Graphics.theInteriorBmpTable, SpeechRecognition, OverworldMapTileCustomization, ui's isLegalHere()
type MapSquareChoiceDomainHelper = 
    // item shop stuff
    static member ARROW = 16
    static member BOMB = 17
    static member BOOK = 18
    static member BLUE_CANDLE = 19
    static member BLUE_RING = 20
    static member MEAT = 21
    static member KEY = 22
    static member SHIELD = 23
    static member NUM_ITEMS = 8 // 8 possible types of items can be tracked, listed above
    static member IsItem(state) = state >= 16 && state <= 23
    static member ToItem(state) = if MapSquareChoiceDomainHelper.IsItem(state) then state-15 else 0   // format used by TrackerModel.overworldMapExtraData
    // other stuff
    static member SWORD2 = 14
    static member SWORD1 = 15
    static member ARMOS = 24
    static member TAKE_ANY = 26
    static member DARK_X = 29

//////////////////////////////////////////////////////////////////////////////////////////

(*
Two main kinds of UI changes can occur:
 - user can scroll/change a Cell or click a Box, which requires immediate visual feedback
 - user can change model state requiring model to be recomputed, where feedback need not be immediate
     - indeed, 'buffering' these changes can be good, as scrolling an item often begets another scroll (need to scroll through many to find item user wants)

Thus, UI-driven live changes to UI do not need change events - if UI scrolls, UI is responsible to redraw the scrolled Cell right now.

And model changes need to just record some sub-portion of the model which has become stale, and then have some policy by which after some time, the entire model
gets recomputed and a fresh model is published via change events to the UI for a display update.

In practice, chose to divide state into a few main groups, which keep of when they last changed, and then have simple dependencies to recompute stale data and bring entire model back up to date.

Model is not enitrely threadsafe; will do all these computations on the UI thread.
*)


//////////////////////////////////////////////////////////////////////////////////////////
// Player Progress

type BoolProperty(initState,changedFunc) =
    let mutable state = initState
    let changed = new Event<_>()
    member _this.Set(b) =
        state <- b
        changedFunc()
        changed.Trigger(state)
    member _this.Toggle() =
        state <- not state
        changedFunc()
        changed.Trigger(state)
    member _this.Value() = state
    member _this.Changed = changed.Publish
let mutable playerProgressLastChangedTime = System.DateTime.Now
type PlayerProgressAndTakeAnyHearts() =
    // describes the state directly accessible in the upper right portion of the UI
    let takeAnyHearts = [| 0; 0; 0; 0 |]   // 0 = untaken (open heart on UI), 1 = taken heart (red heart on UI), 2 = taken potion/candle (X out empty heart on UI)
    let playerHasBoomBook      = BoolProperty(false,fun()->playerProgressLastChangedTime <- System.DateTime.Now)
    let playerHasWoodSword     = BoolProperty(false,fun()->playerProgressLastChangedTime <- System.DateTime.Now)
    let playerHasWoodArrow     = BoolProperty(false,fun()->playerProgressLastChangedTime <- System.DateTime.Now)
    let playerHasBlueRing      = BoolProperty(false,fun()->playerProgressLastChangedTime <- System.DateTime.Now)
    let playerHasBlueCandle    = BoolProperty(false,fun()->playerProgressLastChangedTime <- System.DateTime.Now)
    let playerHasMagicalSword  = BoolProperty(false,fun()->playerProgressLastChangedTime <- System.DateTime.Now)
    let playerHasDefeatedGanon = BoolProperty(false,fun()->playerProgressLastChangedTime <- System.DateTime.Now)
    let playerHasRescuedZelda  = BoolProperty(false,fun()->playerProgressLastChangedTime <- System.DateTime.Now)
    let playerHasBombs         = BoolProperty(false,fun()->playerProgressLastChangedTime <- System.DateTime.Now)
    let takeAnyHeartChanged = new Event<_>()
    member _this.GetTakeAnyHeart(i) = takeAnyHearts.[i]
    member _this.SetTakeAnyHeart(i,v) = 
        takeAnyHearts.[i] <- v
        playerProgressLastChangedTime <- System.DateTime.Now
        takeAnyHeartChanged.Trigger(i)
    member _this.TakeAnyHeartChanged = takeAnyHeartChanged.Publish
    member _this.PlayerHasBoomBook      = playerHasBoomBook
    member _this.PlayerHasWoodSword     = playerHasWoodSword     
    member _this.PlayerHasWoodArrow     = playerHasWoodArrow
    member _this.PlayerHasBlueRing      = playerHasBlueRing      
    member _this.PlayerHasBlueCandle    = playerHasBlueCandle    
    member _this.PlayerHasMagicalSword  = playerHasMagicalSword  
    member _this.PlayerHasDefeatedGanon = playerHasDefeatedGanon 
    member _this.PlayerHasRescuedZelda  = playerHasRescuedZelda  
    member _this.PlayerHasBombs         = playerHasBombs
let playerProgressAndTakeAnyHearts = PlayerProgressAndTakeAnyHearts()

//////////////////////////////////////////////////////////////////////////////////////////
// Dungeons and Boxes

let mutable dungeonsAndBoxesLastChangedTime = System.DateTime.Now
[<RequireQualifiedAccess>]
type PlayerHas = | YES | NO | SKIPPED
type Box() =
    // this contains both a Cell (player-knowing-location-contents), and a bool (whether the players _has_ the thing there)
    let cell = new Cell(allItemWithHeartShuffleChoiceDomain)
    let mutable playerHas = PlayerHas.NO
    let changed = new Event<_>()
    member _this.Changed = changed.Publish
    member _this.PlayerHas() = playerHas
    member _this.CellNextFreeKey() = allItemWithHeartShuffleChoiceDomain.NextFreeKey(cell.Current())
    member _this.CellPrevFreeKey() = allItemWithHeartShuffleChoiceDomain.PrevFreeKey(cell.Current())
    member _this.CellPrev() = 
        cell.Prev()
        dungeonsAndBoxesLastChangedTime <- System.DateTime.Now
        changed.Trigger()
    member _this.CellNext() = 
        cell.Next()
        dungeonsAndBoxesLastChangedTime <- System.DateTime.Now
        changed.Trigger()
    member _this.CellCurrent() = cell.Current()
    member _this.Set(v,ph) = 
        cell.Set(v)
        playerHas <- ph
        dungeonsAndBoxesLastChangedTime <- System.DateTime.Now
        changed.Trigger()
    member _this.SetPlayerHas(v) = 
        playerHas <- v
        dungeonsAndBoxesLastChangedTime <- System.DateTime.Now
        changed.Trigger()

let ladderBox = Box()
let armosBox  = Box()
let sword2Box = Box()

[<RequireQualifiedAccess>]
type DungeonTrackerInstanceKind =
    | HIDE_DUNGEON_NUMBERS
    | DEFAULT

type DungeonTrackerInstance(kind) =
    static let mutable theInstance = DungeonTrackerInstance(DungeonTrackerInstanceKind.DEFAULT)
    let finalBoxOf1Or4 = new Box()  // only relevant in DEFAULT
    let dungeons = 
        match kind with
        | DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS -> [| 
            for i = 0 to 7 do 
                yield new Dungeon(i,3) 
            yield new Dungeon(8,2)
            |]
        | DungeonTrackerInstanceKind.DEFAULT -> [|
            new Dungeon(0, 2)
            new Dungeon(1, 2)
            new Dungeon(2, 2)
            new Dungeon(3, 2)
            new Dungeon(4, 2)
            new Dungeon(5, 2)
            new Dungeon(6, 2)
            new Dungeon(7, 3)
            new Dungeon(8, 2)
            |]
    member _this.Kind = kind
    member _this.Dungeons(i) = dungeons.[i]
    member _this.FinalBoxOf1Or4 =
        match kind with
        | DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS -> failwith "FinalBoxOf1Or4 does not exist in HIDE_DUNGEON_NUMBERS"
        | DungeonTrackerInstanceKind.DEFAULT -> finalBoxOf1Or4
    member _this.AllBoxes() =
        [|
        for d in dungeons do
            yield! d.Boxes
        yield ladderBox
        yield armosBox
        yield sword2Box
        |]
    static member TheDungeonTrackerInstance with get() = theInstance and set(x) = theInstance <- x

and Dungeon(id,numBoxes) =
    let mutable playerHasTriforce = false                     // just ignore this for dungeon 9 (id=8)
    let boxes = Array.init numBoxes (fun _ -> new Box())
    let mutable color = 0                // 0xRRGGBB format   // just ignore this for dungeon 9 (id=8)
    let mutable labelChar = '?'          // ?12345678         // just ignore this for dungeon 9 (id=8)
    let hiddenDungeonColorLabelChangeEvent = new Event<_>()
    member _this.HasBeenLocated() = mapSquareChoiceDomain.NumUses(id) = 1
    member _this.PlayerHasTriforce() = playerHasTriforce
    member _this.ToggleTriforce() = playerHasTriforce <- not playerHasTriforce; dungeonsAndBoxesLastChangedTime <- System.DateTime.Now
    member _this.Boxes = 
        match DungeonTrackerInstance.TheDungeonTrackerInstance.Kind with
        | DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS -> boxes
        | DungeonTrackerInstanceKind.DEFAULT ->
            if id=0 && not(Options.IsSecondQuestDungeons.Value) || id=3 && Options.IsSecondQuestDungeons.Value then
                [| yield! boxes; yield DungeonTrackerInstance.TheDungeonTrackerInstance.FinalBoxOf1Or4 |]
            else
                boxes
    member this.IsComplete = 
        match DungeonTrackerInstance.TheDungeonTrackerInstance.Kind with
        | DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS ->
            if playerHasTriforce then
                let mutable numBoxesDone = 0
                for b in boxes do
                    if b.PlayerHas() <> PlayerHas.NO then
                        numBoxesDone <- numBoxesDone + 1
                let twoBoxers = if Options.IsSecondQuestDungeons.Value then "123567" else "234567"
                numBoxesDone = 3 || (numBoxesDone = 2 && (twoBoxers |> Seq.contains this.LabelChar))
            else
                false
        | DungeonTrackerInstanceKind.DEFAULT ->
            playerHasTriforce && this.Boxes |> Array.forall (fun b -> b.PlayerHas() <> PlayerHas.NO)
    // for Hidden Dungeon Numbers
    member _this.Color with get() = color and set(x) = color <- x; hiddenDungeonColorLabelChangeEvent.Trigger(color,labelChar)
    member _this.LabelChar with get() = labelChar and set(x) = labelChar <- x; hiddenDungeonColorLabelChangeEvent.Trigger(color,labelChar); dungeonsAndBoxesLastChangedTime <- System.DateTime.Now
    member _this.HiddenDungeonColorOrLabelChanged = hiddenDungeonColorLabelChangeEvent.Publish

let GetDungeon(i) = DungeonTrackerInstance.TheDungeonTrackerInstance.Dungeons(i)
let IsHiddenDungeonNumbers() = DungeonTrackerInstance.TheDungeonTrackerInstance.Kind = DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS
let GetTriforceHaves() =
    if IsHiddenDungeonNumbers() then
        let haves = Array.zeroCreate 8
        for i = 0 to 7 do
            let d = GetDungeon(i)
            if d.PlayerHasTriforce() then
                if d.LabelChar >= '1' && d.LabelChar <= '8' then
                    let n = int d.LabelChar - int '1'
                    haves.[n] <- true
        haves
    else
        [| for i = 0 to 7 do yield GetDungeon(i).PlayerHasTriforce() |]

//////////////////////////////////////////////////////////////////////////////////////////
// Player computed state summary

type PlayerComputedStateSummary(haveRecorder,haveLadder,haveAnyKey,haveCoastItem,haveWhiteSwordItem,havePowerBracelet,haveRaft,playerHearts,
                                swordLevel,candleLevel,ringLevel,haveBow,arrowLevel,haveWand,haveBook,boomerangLevel) = 
    // computed from Boxes and other bits
    member _this.HaveRecorder = haveRecorder
    member _this.HaveLadder = haveLadder
    member _this.HaveAnyKey = haveAnyKey
    member _this.HaveCoastItem = haveCoastItem
    member _this.HaveWhiteSwordItem = haveWhiteSwordItem
    member _this.HavePowerBracelet = havePowerBracelet
    member _this.HaveRaft = haveRaft
    member _this.PlayerHearts = playerHearts // TODO can't handle money-or-life rooms losing heart, or flags that start with more hearts
    member _this.SwordLevel = swordLevel
    member _this.CandleLevel = candleLevel
    member _this.RingLevel = ringLevel
    member _this.HaveBow = haveBow
    member _this.ArrowLevel = arrowLevel
    member _this.HaveWand = haveWand
    member _this.HaveBookOrShield = haveBook
    member _this.BoomerangLevel = boomerangLevel
let mutable playerComputedStateSummary = PlayerComputedStateSummary(false,false,false,false,false,false,false,3,0,0,0,false,0,false,false,0)
let mutable playerComputedStateSummaryLastComputedTime = System.DateTime.Now
let recomputePlayerStateSummary() =
    let mutable haveRecorder,haveLadder,haveAnyKey,haveCoastItem,haveWhiteSwordItem,havePowerBracelet,haveRaft,playerHearts = false,false,false,false,false,false,false,3
    let mutable swordLevel,candleLevel,ringLevel,haveBow,arrowLevel,haveWand,haveBookOrShield,boomerangLevel = 0,0,0,false,0,false,false,0
    for b in DungeonTrackerInstance.TheDungeonTrackerInstance.AllBoxes() do
        if b.PlayerHas() = PlayerHas.YES then
            if b.CellCurrent() = ITEMS.RECORDER then
                haveRecorder <- true
            elif b.CellCurrent() = ITEMS.LADDER then
                haveLadder <- true
            elif b.CellCurrent() = ITEMS.KEY then
                haveAnyKey <- true
            elif b.CellCurrent() = ITEMS.POWERBRACELET then
                havePowerBracelet <- true
            elif b.CellCurrent() = ITEMS.RAFT then
                haveRaft <- true
            elif b.CellCurrent() = ITEMS.REDCANDLE then
                candleLevel <- 2
            elif b.CellCurrent() = ITEMS.HEARTCONTAINER then
                playerHearts <- playerHearts + 1
            elif b.CellCurrent() = ITEMS.BOOKSHIELD then
                haveBookOrShield <- true
            elif b.CellCurrent() = ITEMS.BOOMERANG then
                boomerangLevel <- max boomerangLevel 1
            elif b.CellCurrent() = ITEMS.BOW then
                haveBow <- true
            elif b.CellCurrent() = ITEMS.MAGICBOOMERANG then
                boomerangLevel <- 2
            elif b.CellCurrent() = ITEMS.REDRING then
                ringLevel <- 2
            elif b.CellCurrent() = ITEMS.SILVERARROW then
                arrowLevel <- 2
            elif b.CellCurrent() = ITEMS.WAND then
                haveWand <- true
            elif b.CellCurrent() = ITEMS.WHITESWORD then
                swordLevel <- max swordLevel 2
    if playerProgressAndTakeAnyHearts.PlayerHasBlueCandle.Value() then
        candleLevel <- max candleLevel 1
    if playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value() then
        swordLevel <- 3
    if playerProgressAndTakeAnyHearts.PlayerHasWoodSword.Value() then
        swordLevel <- max swordLevel 1
    if playerProgressAndTakeAnyHearts.PlayerHasBlueRing.Value() then
        ringLevel <- max ringLevel 1
    if playerProgressAndTakeAnyHearts.PlayerHasWoodArrow.Value() then
        arrowLevel <- max arrowLevel 1
    if ladderBox.PlayerHas() <> PlayerHas.NO then
        haveCoastItem <- true
    if sword2Box.PlayerHas() <> PlayerHas.NO then
        haveWhiteSwordItem <- true
    for h = 0 to 3 do
        if playerProgressAndTakeAnyHearts.GetTakeAnyHeart(h) = 1 then
            playerHearts <- playerHearts + 1
    playerComputedStateSummary <- PlayerComputedStateSummary(haveRecorder,haveLadder,haveAnyKey,haveCoastItem,haveWhiteSwordItem,havePowerBracelet,haveRaft,playerHearts,
                                                                swordLevel,candleLevel,ringLevel,haveBow,arrowLevel,haveWand,haveBookOrShield,boomerangLevel)
    playerComputedStateSummaryLastComputedTime <- System.DateTime.Now

//////////////////////////////////////////////////////////////////////////////////////////
// Map

let mutable owInstance = new OverworldData.OverworldInstance(OverworldData.FIRST)

let mutable mapLastChangedTime = System.DateTime.Now
let overworldMapMarks = Array2D.init 16 8 (fun _ _ -> new Cell(mapSquareChoiceDomain))  
let private overworldMapExtraData = Array2D.create 16 8 0   
// extra data, used by 
//  - 3-item shops to store the second item, where 0 is none and 1-MapStateProxy.NUM_ITEMS are those items
//  - take any (24), where 24 means 'taken' (dark) and anything else means not yet taken (bright)
let getOverworldMapExtraData(i,j) = overworldMapExtraData.[i,j]
let setOverworldMapExtraData(i,j,x) = 
    overworldMapExtraData.[i,j] <- x
    mapLastChangedTime <- System.DateTime.Now
do
    mapSquareChoiceDomain.Changed.Add(fun _ -> mapLastChangedTime <- System.DateTime.Now)
let NOTFOUND = (-1,-1)
type MapStateSummary(dungeonLocations,anyRoadLocations,armosLocation,sword3Location,sword2Location,boomBookShopLocation,owSpotsRemain,owGettableLocations,
                        owWhistleSpotsRemain,owPowerBraceletSpotsRemain,owRouteworthySpots,firstQuestOnlyInterestingMarks,secondQuestOnlyInterestingMarks) =
    member _this.DungeonLocations = dungeonLocations
    member _this.AnyRoadLocations = anyRoadLocations
    member _this.ArmosLocation = armosLocation
    member _this.Sword3Location = sword3Location
    member _this.Sword2Location = sword2Location
    member _this.BoomBookShopLocation = boomBookShopLocation
    member _this.OwSpotsRemain = owSpotsRemain
    member _this.OwGettableLocations = owGettableLocations
    member _this.OwWhistleSpotsRemain = owWhistleSpotsRemain
    member _this.OwPowerBraceletSpotsRemain = owPowerBraceletSpotsRemain
    member _this.OwRouteworthySpots = owRouteworthySpots
    member _this.FirstQuestOnlyInterestingMarks = firstQuestOnlyInterestingMarks
    member _this.SecondQuestOnlyInterestingMarks = secondQuestOnlyInterestingMarks
let mutable mapStateSummary = MapStateSummary(null,null,NOTFOUND,NOTFOUND,NOTFOUND,NOTFOUND,0,ResizeArray(),null,0,null,null,null)
let mutable mapStateSummaryLastComputedTime = System.DateTime.Now
let recomputeMapStateSummary() =
    let dungeonLocations = Array.create 9 NOTFOUND
    let anyRoadLocations = Array.create 4 NOTFOUND
    let mutable armosLocation = NOTFOUND
    let mutable sword3Location = NOTFOUND
    let mutable sword2Location = NOTFOUND
    let mutable boomBookShopLocation = NOTFOUND  // i think there can be at most one?
    let mutable owSpotsRemain = 0
    let owGettableLocations = ResizeArray()
    let owWhistleSpotsRemain = ResizeArray()
    let mutable owPowerBraceletSpotsRemain = 0
    let mutable owRouteworthySpots = Array2D.create 16 8 false
    let firstQuestOnlyInterestingMarks = Array2D.zeroCreate 16 8
    let secondQuestOnlyInterestingMarks = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            if not(owInstance.AlwaysEmpty(i,j)) then
                match overworldMapMarks.[i,j].Current() with
                | x when x>=0 && x<9 -> 
                    dungeonLocations.[x] <- i,j
                    if x < 8 && not(GetDungeon(x).IsComplete) then
                        owRouteworthySpots.[i,j] <- true
                    let mutable playerHasAllTriforce = true
                    for i = 0 to 7 do
                        if not(GetDungeon(i).PlayerHasTriforce()) then
                            playerHasAllTriforce <- false
                    if x = 8 && playerHasAllTriforce then
                        owRouteworthySpots.[i,j] <- true
                | x when x>=9 && x<13 -> 
                    anyRoadLocations.[x-9] <- i,j
                | 13 -> 
                    sword3Location <- i,j
                    if not(playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) && playerComputedStateSummary.PlayerHearts >= 10 then
                        owRouteworthySpots.[i,j] <- true
                | 14 -> 
                    sword2Location <- i,j
                    if not playerComputedStateSummary.HaveWhiteSwordItem && playerComputedStateSummary.PlayerHearts >= 4 then
                        owRouteworthySpots.[i,j] <- true
                | n when n=MapSquareChoiceDomainHelper.ARMOS -> 
                    armosLocation <- i,j
                    if armosBox.PlayerHas() = PlayerHas.NO then
                        owRouteworthySpots.[i,j] <- true
                | -1 ->
                    owSpotsRemain <- owSpotsRemain + 1
                    if owInstance.Whistleable(i,j) then
                        owWhistleSpotsRemain.Add(i,j)
                    if owInstance.PowerBraceletable(i,j) then
                        owPowerBraceletSpotsRemain <- owPowerBraceletSpotsRemain + 1
                    if (owInstance.Whistleable(i,j) && not playerComputedStateSummary.HaveRecorder) ||
                        (owInstance.PowerBraceletable(i,j) && not playerComputedStateSummary.HavePowerBracelet) ||
                        (owInstance.Ladderable(i,j) && not playerComputedStateSummary.HaveLadder) ||
                        (owInstance.Raftable(i,j) && not playerComputedStateSummary.HaveRaft) ||
                        (owInstance.Bombable(i,j) && not(playerProgressAndTakeAnyHearts.PlayerHasBombs.Value())) ||
                        (owInstance.Burnable(i,j) && playerComputedStateSummary.CandleLevel=0) then
                        ()
                    else
                        owRouteworthySpots.[i,j] <- true
                        owGettableLocations.Add(i,j)
                | _ -> () // shop or whatnot
                let cur = overworldMapMarks.[i,j].Current()
                if MapSquareChoiceDomainHelper.IsItem(cur) then
                    if cur = MapSquareChoiceDomainHelper.BOOK || (getOverworldMapExtraData(i,j) = MapSquareChoiceDomainHelper.ToItem(MapSquareChoiceDomainHelper.BOOK)) then
                        boomBookShopLocation <- i,j
                let isInteresting = overworldMapMarks.[i,j].Current() <> -1 && overworldMapMarks.[i,j].Current() <> mapSquareChoiceDomain.MaxKey
                if OverworldData.owMapSquaresSecondQuestOnly.[j].Chars(i) = 'X' then 
                    secondQuestOnlyInterestingMarks.[i,j] <- isInteresting 
                if OverworldData.owMapSquaresFirstQuestOnly.[j].Chars(i) = 'X' then 
                    firstQuestOnlyInterestingMarks.[i,j] <- isInteresting 
    owRouteworthySpots.[15,5] <- playerComputedStateSummary.HaveLadder && not playerComputedStateSummary.HaveCoastItem // gettable coast item is routeworthy
    mapStateSummary <- MapStateSummary(dungeonLocations,anyRoadLocations,armosLocation,sword3Location,sword2Location,boomBookShopLocation,owSpotsRemain,owGettableLocations,
                                        owWhistleSpotsRemain,owPowerBraceletSpotsRemain,owRouteworthySpots,firstQuestOnlyInterestingMarks,secondQuestOnlyInterestingMarks)
    mapStateSummaryLastComputedTime <- System.DateTime.Now

let initializeAll(instance:OverworldData.OverworldInstance, dungeonTrackerInstance) =
    DungeonTrackerInstance.TheDungeonTrackerInstance <- dungeonTrackerInstance
    owInstance <- instance
    for i = 0 to 15 do
        for j = 0 to 7 do
            if owInstance.AlwaysEmpty(i,j) then
                overworldMapMarks.[i,j].Prev()   // set to 'X'
    recomputeMapStateSummary()

//////////////////////////////////////////////////////////////////////////////////////////
// Dungeon blockers

// only some of these can participate in semantic reminders, obviously...
[<RequireQualifiedAccess>]
type DungeonBlocker =
    | COMBAT    // need better weapon/armor
    | BOW_AND_ARROW
    | RECORDER
    | LADDER  
    | BAIT    
    | KEY
    | BOMB
//    | MONEY     // money or life room? bomb upgrade?
    | NOTHING
    member this.Next() =
        match this with
        | DungeonBlocker.COMBAT -> DungeonBlocker.BOW_AND_ARROW
        | DungeonBlocker.BOW_AND_ARROW -> DungeonBlocker.RECORDER
        | DungeonBlocker.RECORDER -> DungeonBlocker.LADDER
        | DungeonBlocker.LADDER -> DungeonBlocker.BAIT
        | DungeonBlocker.BAIT -> DungeonBlocker.KEY
        | DungeonBlocker.KEY -> DungeonBlocker.BOMB
        | DungeonBlocker.BOMB -> DungeonBlocker.NOTHING
        | DungeonBlocker.NOTHING -> DungeonBlocker.COMBAT
    member this.Prev() =
        match this with
        | DungeonBlocker.COMBAT -> DungeonBlocker.NOTHING
        | DungeonBlocker.BOW_AND_ARROW -> DungeonBlocker.COMBAT
        | DungeonBlocker.RECORDER -> DungeonBlocker.BOW_AND_ARROW
        | DungeonBlocker.LADDER -> DungeonBlocker.RECORDER
        | DungeonBlocker.BAIT -> DungeonBlocker.LADDER
        | DungeonBlocker.KEY -> DungeonBlocker.BAIT
        | DungeonBlocker.BOMB -> DungeonBlocker.KEY
        | DungeonBlocker.NOTHING -> DungeonBlocker.BOMB
    static member All = [| 
        DungeonBlocker.COMBAT
        DungeonBlocker.BOW_AND_ARROW
        DungeonBlocker.RECORDER
        DungeonBlocker.LADDER
        DungeonBlocker.BAIT
        DungeonBlocker.KEY
        DungeonBlocker.BOMB
        DungeonBlocker.NOTHING
        |]
[<RequireQualifiedAccess>]
type CombatUnblockerDetail =
    | BETTER_SWORD
    | BETTER_ARMOR
    | WAND
let MAX_BLOCKERS_PER_DUNGEON = 2
let dungeonBlockers = Array2D.create 8 MAX_BLOCKERS_PER_DUNGEON DungeonBlocker.NOTHING  // Note: we don't need to LastComputedTime-invalidate anything when the blocker set changes

//////////////////////////////////////////////////////////////////////////////////////////

let recomputeWhatIsNeeded() =
    let mutable changed = false
    if playerProgressLastChangedTime > playerComputedStateSummaryLastComputedTime ||
        dungeonsAndBoxesLastChangedTime > playerComputedStateSummaryLastComputedTime then
        recomputePlayerStateSummary()
        changed <- true
    if playerComputedStateSummaryLastComputedTime > mapStateSummaryLastComputedTime ||
        mapLastChangedTime > mapStateSummaryLastComputedTime then
        recomputeMapStateSummary()
        changed <- true
    changed

//////////////////////////////////////////////////////////////////////////////////////////
// Other minor bits
    
let mutable shieldBook = false // if true, boomstick seed - no eventing here, UI should synchronously swap shield/book icons
let mutable startIconX,startIconY = NOTFOUND  // UI can poke and display these

[<RequireQualifiedAccess>]
type HintZone =
    | UNKNOWN
    | DEATH_MOUNTAIN
    | LAKE
    | LOST_HILLS
    | RIVER
    | GRAVE
    | DESERT
    | COAST
    | DEAD_WOODS
    | NEAR_START
    | FOREST
    member this.AsDataChar() =  // as per OverworldData.owMapZone
        match this with
        | UNKNOWN -> '_'
        | DEATH_MOUNTAIN -> 'M'
        | LAKE -> 'L'
        | LOST_HILLS -> 'H'
        | RIVER -> 'R'
        | GRAVE -> 'G'
        | DESERT -> 'D'
        | COAST -> 'C'
        | DEAD_WOODS -> 'W'
        | NEAR_START -> 'S'
        | FOREST -> 'F'
    override this.ToString() =
        match this with
        | UNKNOWN -> "?????"
        | DEATH_MOUNTAIN -> "Death Mountain"
        | LAKE -> "Lake"
        | LOST_HILLS -> "Lost Hills"
        | RIVER -> "River"
        | GRAVE -> "Grave"
        | DESERT -> "Desert"
        | COAST -> "Coast"
        | DEAD_WOODS -> "Dead Woods"
        | NEAR_START -> "Close to Start"
        | FOREST -> "Forest"
    static member FromIndex(i) =
        match i with
        | 0 -> UNKNOWN
        | 1 -> DEATH_MOUNTAIN
        | 2 -> LAKE
        | 3 -> LOST_HILLS
        | 4 -> RIVER
        | 5 -> GRAVE
        | 6 -> DESERT
        | 7 -> COAST
        | 8 -> DEAD_WOODS
        | 9 -> NEAR_START
        | 10 -> FOREST
        | _ -> failwith "bad HintZone index"
    member this.ToIndex() =
        match this with
        | UNKNOWN -> 0
        | DEATH_MOUNTAIN -> 1
        | LAKE -> 2
        | LOST_HILLS -> 3
        | RIVER -> 4
        | GRAVE -> 5
        | DESERT -> 6
        | COAST -> 7
        | DEAD_WOODS -> 8
        | NEAR_START -> 9
        | FOREST -> 10
let levelHints = Array.create 11 HintZone.UNKNOWN   // 0-8 is L1-9, 9 is WS, 10 is MS

let forceUpdate() = 
    // UI can force an update for a few bits that we don't model well yet
    // TODO ideally dont want this, feels like kludge?
    mapLastChangedTime <- System.DateTime.Now
                
//////////////////////////////////////////////////////////////////////////////////////////

let unreachablePossibleDungeonSpotCount() =
    let mutable count = 0
    for x = 0 to 15 do
        for y = 0 to 7 do
            let cur = overworldMapMarks.[x,y].Current()
            if cur < 9 then  // it's marked as a dungeon, or it's unmarked so it might be an unfound dungeon
                if owInstance.Bombable(x,y) && not(playerProgressAndTakeAnyHearts.PlayerHasBombs.Value()) then
                    count <- count + 1
                if owInstance.Burnable(x,y) && not(playerComputedStateSummary.CandleLevel>0) then
                    count <- count + 1
                if owInstance.Raftable(x,y) && not(playerComputedStateSummary.HaveRaft) then
                    count <- count + 1
                if owInstance.Ladderable(x,y) && not(playerComputedStateSummary.HaveLadder) then
                    count <- count + 1
                if owInstance.Whistleable(x,y) && not(playerComputedStateSummary.HaveRecorder) then
                    count <- count + 1
    count
type TriforceAndGoSummary() =
    // Note: we're just going to assume the player has a sword (or wand in swordless), let's not go nuts with advanced flags and/or unlikely no-sword-but-all-items situations
    let haveBow = playerComputedStateSummary.HaveBow
    let haveSilvers = playerComputedStateSummary.ArrowLevel=2
    let haveLadder = playerComputedStateSummary.HaveLadder
    let haveRecorder = playerComputedStateSummary.HaveLadder
    let unreachableCount = unreachablePossibleDungeonSpotCount()
    let mutable missingTriforceFromLocatedDungeonCount = 0   // TODO advanced flags: only need N triforces to enter 9
    let mutable missingDungeonCount = 0
    let tagLevel = 
        for i = 0 to 8 do
            if not(GetDungeon(i).PlayerHasTriforce()) then
                if not(GetDungeon(i).HasBeenLocated()) then
                    missingDungeonCount <- missingDungeonCount + 1
                elif i <> 8 then // L9 has no triforce
                    missingTriforceFromLocatedDungeonCount <- missingTriforceFromLocatedDungeonCount + 1
        let compute() =
            let mutable score = 100
            let missingPenalty = 15 + unreachableCount
            score <- score - missingDungeonCount*missingPenalty                  // big penalty for unlocated dungeon
            score <- score - missingTriforceFromLocatedDungeonCount*8            // smallish penalty for missing triforce in a dungeon you already located
            if not haveBow then score <- score - 35                              // huge penalty for missing bow
            if not haveSilvers then score <- score - 30                          // huge penalty for missing silvers
            if not haveLadder then score <- score - 15                           // medium penalty for missing ladder
            if not haveRecorder then score <- score - 5                          // small penalty for missing recorder
            //printfn "score: %d" score
            if score < 0 then 0 else score
        // you might need e.g. power bracelet or raft to find missing dungeon, so never TAG without being able to locate them all  
        if missingDungeonCount=0 || unreachableCount=0 then 
            if haveBow && haveSilvers && haveLadder && haveRecorder then
                103
            elif haveBow && haveSilvers && haveLadder then
                102
            elif haveBow && haveSilvers then
                101
            else
                compute()
        else
            compute()
    member _this.Level = tagLevel  // 103 TAG, 102 probably-TAG, 101 might-be-TAG, 1-100 see features below, 0 not worth reporting
    member _this.HaveBow = haveBow
    member _this.HaveSilvers = haveSilvers
    member _this.HaveLadder = haveLadder
    member _this.HaveRecorder = haveRecorder
    member _this.MissingDungeonCount = missingDungeonCount
type ITrackerEvents =
    // hearts
    abstract CurrentHearts : int -> unit
    abstract AnnounceConsiderSword2 : unit -> unit
    abstract AnnounceConsiderSword3 : unit -> unit
    // map
    abstract OverworldSpotsRemaining : int * int -> unit  // total remaining, currently gettable
    abstract DungeonLocation : int*int*int*bool*bool -> unit   // number 0-7, x, y, hasTri, isCompleted --- to update triforce color (found), completed shading, recorderable/completed map icon
    abstract AnyRoadLocation : int*int*int -> unit // number 0-3, x, y
    abstract WhistleableLocation : int*int -> unit // x, y
    abstract Armos : int*int -> unit // x,y
    abstract Sword3 : int*int -> unit // x,y
    abstract Sword2 : int*int -> unit // x,y
    abstract CoastItem : unit -> unit
    abstract RoutingInfo : bool*bool*seq<int*int>*seq<int*int>*bool[,] -> unit // haveLadder haveRaft currentRecorderWarpDestinations currentAnyRoadDestinations owRouteworthySpots
    // dungeons
    abstract AnnounceCompletedDungeon : int -> unit
    abstract CompletedDungeons : bool[] -> unit     // for current shading
    abstract AnnounceFoundDungeonCount : int -> unit
    abstract AnnounceTriforceCount : int -> unit
    abstract AnnounceTriforceAndGo : int * TriforceAndGoSummary -> unit
    // blockers
    abstract RemindUnblock : DungeonBlocker * seq<int> * seq<CombatUnblockerDetail> -> unit
    // items
    abstract RemindShortly : int -> unit

// state-transition announcements
let haveAnnouncedHearts = Array.zeroCreate 17
let haveAnnouncedCompletedDungeons = Array.zeroCreate 8
let mutable previouslyAnnouncedFoundDungeonCount = 0
let mutable previouslyAnnouncedTriforceCount = 0
let mutable previouslyLocatedDungeonCount = 0
let mutable remindedLadder, remindedAnyKey = false, false
let mutable priorSwordWandLevel = 0
let mutable priorRingLevel = 0
let mutable priorBombs = false
let mutable priorBowArrow = false
let mutable priorRecorder = false
let mutable priorLadder = false
let mutable priorAnyKey = false
// triforce-and-go levels
let mutable previouslyAnnouncedTriforceAndGo = 0  // 0 = no, 1 = might be, 2 = probably, 3 = certainly triforce-and-go
let mutable previousCompletedDungeonCount = 0
let allUIEventingLogic(ite : ITrackerEvents) =
    // hearts
    let playerHearts = playerComputedStateSummary.PlayerHearts
    ite.CurrentHearts playerHearts
    if playerHearts >=4 && playerHearts <= 6 && not haveAnnouncedHearts.[playerHearts] then
        haveAnnouncedHearts.[playerHearts] <- true
        if not playerComputedStateSummary.HaveWhiteSwordItem && mapStateSummary.Sword2Location<>NOTFOUND then
            ite.AnnounceConsiderSword2()
    if playerHearts >=10 && playerHearts <= 14 && not haveAnnouncedHearts.[playerHearts] then
        haveAnnouncedHearts.[playerHearts] <- true
        if not(playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()) && mapStateSummary.Sword3Location<>NOTFOUND then
            ite.AnnounceConsiderSword3()
    // map
    ite.OverworldSpotsRemaining(mapStateSummary.OwSpotsRemain, mapStateSummary.OwGettableLocations.Count)
    for d = 0 to 8 do
        if mapStateSummary.DungeonLocations.[d] <> NOTFOUND then
            let x,y = mapStateSummary.DungeonLocations.[d]
            ite.DungeonLocation(d, x, y, GetDungeon(d).PlayerHasTriforce(), GetDungeon(d).IsComplete)
    for a = 0 to 3 do
        if mapStateSummary.AnyRoadLocations.[a] <> NOTFOUND then
            let x,y = mapStateSummary.AnyRoadLocations.[a]
            ite.AnyRoadLocation(a, x, y)
    for x,y in mapStateSummary.OwWhistleSpotsRemain do
        ite.WhistleableLocation(x, y)
    if mapStateSummary.ArmosLocation <> NOTFOUND then
        ite.Armos(mapStateSummary.ArmosLocation)
    if mapStateSummary.Sword3Location <> NOTFOUND then
        ite.Sword3(mapStateSummary.Sword3Location)
    if mapStateSummary.Sword2Location <> NOTFOUND then
        ite.Sword2(mapStateSummary.Sword2Location)
    ite.CoastItem()
    let recorderDests = [|
        if playerComputedStateSummary.HaveRecorder then
            for d = 0 to 7 do
                if mapStateSummary.DungeonLocations.[d] <> NOTFOUND && GetDungeon(d).PlayerHasTriforce() then
                    yield mapStateSummary.DungeonLocations.[d]
        |]
    let anyRoadDests = [|
        for d in mapStateSummary.AnyRoadLocations do
            if d <> NOTFOUND then
                yield d
        |]
    ite.RoutingInfo(playerComputedStateSummary.HaveLadder, playerComputedStateSummary.HaveRaft, recorderDests, anyRoadDests, mapStateSummary.OwRouteworthySpots)
    // dungeons
    for d = 0 to 7 do
        if not haveAnnouncedCompletedDungeons.[d] && GetDungeon(d).IsComplete then
            ite.AnnounceCompletedDungeon(d)
            haveAnnouncedCompletedDungeons.[d] <- true
    ite.CompletedDungeons [| for d = 0 to 7 do yield GetDungeon(d).IsComplete |]
    let mutable numFound = 0
    for d = 0 to 8 do
        if mapStateSummary.DungeonLocations.[d]<>NOTFOUND then
            numFound <- numFound + 1
    if numFound > previouslyAnnouncedFoundDungeonCount then
        ite.AnnounceFoundDungeonCount(numFound)
        previouslyAnnouncedFoundDungeonCount <- numFound
    let mutable triforces = 0
    for d = 0 to 8 do
        if GetDungeon(d).PlayerHasTriforce() then
            triforces <- triforces + 1
    let mutable locatedDungeons = 0
    let mutable completedDungeons = 0
    for d = 0 to 8 do
        if GetDungeon(d).IsComplete then
            completedDungeons <- completedDungeons + 1
        if GetDungeon(d).HasBeenLocated() then
            locatedDungeons <- locatedDungeons + 1
    let tagSummary = new TriforceAndGoSummary()
    let mutable justAnnouncedTAG = false
    if triforces > previouslyAnnouncedTriforceCount then
        ite.AnnounceTriforceCount(triforces)
        previouslyAnnouncedTriforceCount <- triforces
        if completedDungeons <= previousCompletedDungeonCount && tagSummary.Level > 101 then
            // just got a new triforce, it did not complete a dungeon, but the player is probably triforce and go, so remind them, so they might abandon rest of dungeon
            ite.AnnounceTriforceAndGo(triforces, tagSummary)
            justAnnouncedTAG <- true
    if locatedDungeons > previouslyLocatedDungeonCount && tagSummary.Level > 101 && not justAnnouncedTAG then
        // just located a new dungeon, the player is probably triforce and go, so remind them
        ite.AnnounceTriforceAndGo(triforces, tagSummary)
        justAnnouncedTAG <- true
    previousCompletedDungeonCount <- completedDungeons
    previouslyLocatedDungeonCount <- locatedDungeons
    if tagSummary.Level > previouslyAnnouncedTriforceAndGo then
        previouslyAnnouncedTriforceAndGo <- tagSummary.Level
        if not justAnnouncedTAG then
            ite.AnnounceTriforceAndGo(triforces, tagSummary)
    // blockers - COMBAT
    let combatUnblockers = ResizeArray()
    if playerComputedStateSummary.SwordLevel > priorSwordWandLevel then
        combatUnblockers.Add(CombatUnblockerDetail.BETTER_SWORD)
    if playerComputedStateSummary.HaveWand && (priorSwordWandLevel < 2) then
        combatUnblockers.Add(CombatUnblockerDetail.WAND)
    if playerComputedStateSummary.RingLevel > priorRingLevel 
                && (playerComputedStateSummary.SwordLevel>0 || playerComputedStateSummary.HaveWand) then  // armor won't help you win combat if you have 0 weapons
        combatUnblockers.Add(CombatUnblockerDetail.BETTER_ARMOR)
    if combatUnblockers.Count > 0 then
        let dungeonIdxs = ResizeArray()
        for i = 0 to 7 do
            if dungeonBlockers.[i,0] = DungeonBlocker.COMBAT || dungeonBlockers.[i,1] = DungeonBlocker.COMBAT then
                if not(GetDungeon(i).IsComplete) then
                    dungeonIdxs.Add(i)
        if dungeonIdxs.Count > 0 then
            if tagSummary.Level < 103 then // no need for blocker-reminder if fully-go-time
                ite.RemindUnblock(DungeonBlocker.COMBAT, dungeonIdxs, combatUnblockers)
    priorSwordWandLevel <- max playerComputedStateSummary.SwordLevel (if playerComputedStateSummary.HaveWand then 2 else 0)
    priorRingLevel <- playerComputedStateSummary.RingLevel
    // blockers - generic
    let blockerLogic(db) =
        let dungeonIdxs = ResizeArray()
        for i = 0 to 7 do
            if dungeonBlockers.[i,0] = db || dungeonBlockers.[i,1] = db then
                if not(GetDungeon(i).IsComplete) then
                    dungeonIdxs.Add(i)
        if dungeonIdxs.Count > 0 then
            if tagSummary.Level < 103 then // no need for blocker-reminder if fully-go-time
                ite.RemindUnblock(db, dungeonIdxs, [])
    // blockers - others
    if not priorBombs && playerProgressAndTakeAnyHearts.PlayerHasBombs.Value() then
        blockerLogic(DungeonBlocker.BOMB)
    priorBombs <- playerProgressAndTakeAnyHearts.PlayerHasBombs.Value()

    if not priorBowArrow && playerComputedStateSummary.HaveBow && playerComputedStateSummary.ArrowLevel>=1 then
        blockerLogic(DungeonBlocker.BOW_AND_ARROW)
    priorBowArrow <- playerComputedStateSummary.HaveBow && playerComputedStateSummary.ArrowLevel>=1

    if not priorRecorder && playerComputedStateSummary.HaveRecorder then
        blockerLogic(DungeonBlocker.RECORDER)
    priorRecorder <- playerComputedStateSummary.HaveRecorder

    if not priorLadder && playerComputedStateSummary.HaveLadder then
        blockerLogic(DungeonBlocker.LADDER)
    priorLadder <- playerComputedStateSummary.HaveLadder

    if not priorAnyKey && playerComputedStateSummary.HaveAnyKey then
        blockerLogic(DungeonBlocker.KEY)
    priorAnyKey <- playerComputedStateSummary.HaveAnyKey

    // Note: no logic for BAIT or loose KEYs, as the tracker has no reliable knowledge of this aspect of player's inventory

    // items
    if not remindedLadder && playerComputedStateSummary.HaveLadder then
        ite.RemindShortly(ITEMS.LADDER)
        remindedLadder <- true
    if not remindedAnyKey && playerComputedStateSummary.HaveAnyKey then
        ite.RemindShortly(ITEMS.KEY)
        remindedAnyKey <- true


        






