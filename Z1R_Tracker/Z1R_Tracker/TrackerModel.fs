module TrackerModel

// model to track the state of the tracker independent of any UI/graphics layer

//////////////////////////////////////////////////////////////////////////////////////////
// Options

open System.Text.Json
open System.Text.Json.Serialization

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
        let mutable RecorderPBSpots = Bool(true)
        let mutable HaveKeyLadder = Bool(true)
    let mutable ListenForSpeech = Bool(true)
    let mutable IsSecondQuestDungeons = Bool(false)
    let mutable IsMuted = false
    let mutable Volume = 30

    type ReadWrite() =
        member val DrawRoutes = true with get,set
        member val HighlightNearby = true with get,set
        member val ShowMagnifier = true with get,set

        member val DungeonFeedback = true with get,set
        member val SwordHearts = true with get,set
        member val CoastItem = true with get,set
        member val RecorderPBSpots = true with get,set
        member val HaveKeyLadder = true with get,set
        
        member val ListenForSpeech = true with get,set
        member val IsSecondQuestDungeons = false with get,set

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

            VoiceReminders.DungeonFeedback.Value <- data.DungeonFeedback
            VoiceReminders.SwordHearts.Value <- data.SwordHearts
            VoiceReminders.CoastItem.Value <- data.CoastItem
            VoiceReminders.RecorderPBSpots.Value <- data.RecorderPBSpots
            VoiceReminders.HaveKeyLadder.Value <- data.HaveKeyLadder

            ListenForSpeech.Value <- data.ListenForSpeech
            IsSecondQuestDungeons.Value <- data.IsSecondQuestDungeons
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

        data.DungeonFeedback <- VoiceReminders.DungeonFeedback.Value
        data.SwordHearts <- VoiceReminders.SwordHearts.Value
        data.CoastItem <- VoiceReminders.CoastItem.Value
        data.RecorderPBSpots <- VoiceReminders.RecorderPBSpots.Value
        data.HaveKeyLadder <- VoiceReminders.HaveKeyLadder.Value

        data.ListenForSpeech <- ListenForSpeech.Value
        data.IsSecondQuestDungeons <- IsSecondQuestDungeons.Value
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
        if uses.[key] > 0 then
            uses.[key] <- uses.[key] - 1
            ev.Trigger(this,key)
        else
            failwith "choice domain underflow"
    member this.AddUse(key) =
        if uses.[key] >= maxUsesArray.[key] then
            failwith "choice domain overflow"
        else
            uses.[key] <- uses.[key] + 1
            ev.Trigger(this,key)
    member this.NextFreeKey(key) =
        if key = uses.Length-1 then
            -1
        elif uses.[key+1] < maxUsesArray.[key+1] then
            key+1
        else
            this.NextFreeKey(key+1)
    member this.PrevFreeKey(key) =
        if key = -1 then
            this.PrevFreeKey(uses.Length)
        elif key = 0 then
            -1
        elif uses.[key-1] < maxUsesArray.[key-1] then
            key-1
        else
            this.PrevFreeKey(key-1)
    member _this.NumUses(key) = uses.[key]
    member _this.MaxUses(key) = maxUsesArray.[key]
        
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
    member this.TrySet(newState) =
        if newState < -1 || newState > cd.MaxKey then
            failwith "TrySet out of range"
        try
            cd.AddUse(newState)
            state <- newState
        with _ -> ()

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
    1 // dungeon 1
    1 // dungeon 2
    1 // dungeon 3
    1 // dungeon 4
    1 // dungeon 5
    1 // dungeon 6
    1 // dungeon 7
    1 // dungeon 8
    1 // dungeon 9
    1 // any road 1
    1 // any road 2
    1 // any road 3
    1 // any road 4
    1 // sword 3
    1 // sword 2
    999 // hint shop
    999 // blue ring shop
    999 // meat shop
    999 // key shop
    999 // candle shop
    999 // book shop
    999 // bomb shop
    999 // arrow shop
    999 // shield shop
    999 // take any
    999 // potion shop
    999 // money
    999 // X (nothing, but visited)
    |])

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
    member _this.Toggle() =
        state <- not state
        changedFunc()
    member _this.Value() = state
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
    member _this.GetTakeAnyHeart(i) = takeAnyHearts.[i]
    member _this.SetTakeAnyHeart(i,v) = takeAnyHearts.[i] <- v; playerProgressLastChangedTime <- System.DateTime.Now
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
type Box() =
    // this contains both a Cell (player-knowing-location-contents), and a bool (whether the players _has_ the thing there)
    let cell = new Cell(allItemWithHeartShuffleChoiceDomain)
    let mutable playerHas = false
    member _this.PlayerHas() = playerHas
    member _this.CellPrev() = 
        cell.Prev()
        dungeonsAndBoxesLastChangedTime <- System.DateTime.Now
    member _this.CellNext() = 
        cell.Next()
        dungeonsAndBoxesLastChangedTime <- System.DateTime.Now
    member _this.CellCurrent() = cell.Current()
    member _this.TogglePlayerHas() = 
        playerHas <- not playerHas
        dungeonsAndBoxesLastChangedTime <- System.DateTime.Now

let FinalBoxOf1Or4 = new Box()
type Dungeon(id,numBoxes) =
    let mutable playerHasTriforce = false  // just ignore this for dungeon 9 (id=8)
    let boxes = Array.init numBoxes (fun _ -> new Box())
    member _this.HasBeenLocated() =
        mapSquareChoiceDomain.NumUses(id) = 1
    member _this.PlayerHasTriforce() = playerHasTriforce
    member _this.ToggleTriforce() = playerHasTriforce <- not playerHasTriforce; dungeonsAndBoxesLastChangedTime <- System.DateTime.Now
    member _this.Boxes = 
        if id=0 && not(Options.IsSecondQuestDungeons.Value) || id=3 && Options.IsSecondQuestDungeons.Value then
            [| yield! boxes; yield FinalBoxOf1Or4 |]
        else
            boxes
    member this.IsComplete = playerHasTriforce && this.Boxes |> Array.forall (fun b -> b.PlayerHas())

let dungeons = [|
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
let ladderBox = Box()
let armosBox  = Box()
let sword2Box = Box()

let allBoxes = [|
    for d in dungeons do
        yield! d.Boxes
    yield ladderBox
    yield armosBox
    yield sword2Box
    |]

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
    for b in allBoxes do
        if b.PlayerHas() then
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
    if ladderBox.PlayerHas() then
        haveCoastItem <- true
    if sword2Box.PlayerHas() then
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

let overworldMapMarks = Array2D.init 16 8 (fun _ _ -> new Cell(mapSquareChoiceDomain))  
let overworldMapExtraData = Array2D.create 16 8 0   // extra data, currently only used by 3-item shops to store the second item, where 0 is none and 1-7 are those items
let mutable mapLastChangedTime = System.DateTime.Now
do
    mapSquareChoiceDomain.Changed.Add(fun _ -> mapLastChangedTime <- System.DateTime.Now)
let NOTFOUND = (-1,-1)
type MapStateSummary(dungeonLocations,anyRoadLocations,sword3Location,sword2Location,owSpotsRemain,owGettableLocations,
                        owWhistleSpotsRemain,owPowerBraceletSpotsRemain,owRouteworthySpots,firstQuestOnlyInterestingMarks,secondQuestOnlyInterestingMarks) =
    member _this.DungeonLocations = dungeonLocations
    member _this.AnyRoadLocations = anyRoadLocations
    member _this.Sword3Location = sword3Location
    member _this.Sword2Location = sword2Location
    member _this.OwSpotsRemain = owSpotsRemain
    member _this.OwGettableLocations = owGettableLocations
    member _this.OwWhistleSpotsRemain = owWhistleSpotsRemain
    member _this.OwPowerBraceletSpotsRemain = owPowerBraceletSpotsRemain
    member _this.OwRouteworthySpots = owRouteworthySpots
    member _this.FirstQuestOnlyInterestingMarks = firstQuestOnlyInterestingMarks
    member _this.SecondQuestOnlyInterestingMarks = secondQuestOnlyInterestingMarks
let mutable mapStateSummary = MapStateSummary(null,null,NOTFOUND,NOTFOUND,0,ResizeArray(),null,0,null,null,null)
let mutable mapStateSummaryLastComputedTime = System.DateTime.Now
let recomputeMapStateSummary() =
    let dungeonLocations = Array.create 9 NOTFOUND
    let anyRoadLocations = Array.create 4 NOTFOUND
    let mutable sword3Location = NOTFOUND
    let mutable sword2Location = NOTFOUND
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
                    if x < 8 && not(dungeons.[x].IsComplete) then
                        owRouteworthySpots.[i,j] <- true
                    if x = 8 && dungeons |> Array.forall (fun d -> d.PlayerHasTriforce()) then
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
                let isInteresting = overworldMapMarks.[i,j].Current() <> -1 && overworldMapMarks.[i,j].Current() <> mapSquareChoiceDomain.MaxKey
                if OverworldData.owMapSquaresSecondQuestOnly.[j].Chars(i) = 'X' then 
                    secondQuestOnlyInterestingMarks.[i,j] <- isInteresting 
                if OverworldData.owMapSquaresFirstQuestOnly.[j].Chars(i) = 'X' then 
                    firstQuestOnlyInterestingMarks.[i,j] <- isInteresting 
    owRouteworthySpots.[15,5] <- playerComputedStateSummary.HaveLadder && not playerComputedStateSummary.HaveCoastItem // gettable coast item is routeworthy
    mapStateSummary <- MapStateSummary(dungeonLocations,anyRoadLocations,sword3Location,sword2Location,owSpotsRemain,owGettableLocations,
                                        owWhistleSpotsRemain,owPowerBraceletSpotsRemain,owRouteworthySpots,firstQuestOnlyInterestingMarks,secondQuestOnlyInterestingMarks)
    mapStateSummaryLastComputedTime <- System.DateTime.Now
let initializeAll(instance:OverworldData.OverworldInstance) =
    owInstance <- instance
    for i = 0 to 15 do
        for j = 0 to 7 do
            if owInstance.AlwaysEmpty(i,j) then
                overworldMapMarks.[i,j].Prev()   // set to 'X'
    recomputeMapStateSummary()

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


let forceUpdate() = 
    // UI can force an update for a few bits that we don't model well yet
    // TODO ideally dont want this, feels like kludge?
    mapLastChangedTime <- System.DateTime.Now
                
//////////////////////////////////////////////////////////////////////////////////////////

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
    abstract Sword3 : int*int -> unit // x,y
    abstract Sword2 : int*int -> unit // x,y
    abstract CoastItem : unit -> unit
    abstract RoutingInfo : bool*bool*seq<int*int>*seq<int*int>*bool[,] -> unit // haveLadder haveRaft currentRecorderWarpDestinations currentAnyRoadDestinations owRouteworthySpots
    // dungeons
    abstract AnnounceCompletedDungeon : int -> unit
    abstract CompletedDungeons : bool[] -> unit     // for current shading
    abstract AnnounceFoundDungeonCount : int -> unit
    abstract AnnounceTriforceCount : int -> unit
    // items
    abstract RemindShortly : int -> unit

let haveAnnouncedHearts = Array.zeroCreate 17
let haveAnnouncedCompletedDungeons = Array.zeroCreate 8
let mutable previouslyAnnouncedFoundDungeonCount = 0
let mutable previouslyAnnouncedTriforceCount = 0
let mutable remindedLadder, remindedAnyKey = false, false
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
            ite.DungeonLocation(d, x, y, dungeons.[d].PlayerHasTriforce(), dungeons.[d].IsComplete)
    for a = 0 to 3 do
        if mapStateSummary.AnyRoadLocations.[a] <> NOTFOUND then
            let x,y = mapStateSummary.AnyRoadLocations.[a]
            ite.AnyRoadLocation(a, x, y)
    for x,y in mapStateSummary.OwWhistleSpotsRemain do
        ite.WhistleableLocation(x, y)
    if mapStateSummary.Sword3Location <> NOTFOUND then
        ite.Sword3(mapStateSummary.Sword3Location)
    if mapStateSummary.Sword2Location <> NOTFOUND then
        ite.Sword2(mapStateSummary.Sword2Location)
    ite.CoastItem()
    let recorderDests = [|
        if playerComputedStateSummary.HaveRecorder then
            for d = 0 to 7 do
                if mapStateSummary.DungeonLocations.[d] <> NOTFOUND && dungeons.[d].PlayerHasTriforce() then
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
        if not haveAnnouncedCompletedDungeons.[d] && dungeons.[d].IsComplete then
            ite.AnnounceCompletedDungeon(d)
            haveAnnouncedCompletedDungeons.[d] <- true
    ite.CompletedDungeons [| for d = 0 to 7 do yield dungeons.[d].IsComplete |]
    let mutable numFound = 0
    for d = 0 to 8 do
        if mapStateSummary.DungeonLocations.[d]<>NOTFOUND then
            numFound <- numFound + 1
    if numFound > previouslyAnnouncedFoundDungeonCount then
        ite.AnnounceFoundDungeonCount(numFound)
        previouslyAnnouncedFoundDungeonCount <- numFound
    let mutable triforces = 0
    for d = 0 to 8 do
        if dungeons.[d].PlayerHasTriforce() then
            triforces <- triforces + 1
    if triforces > previouslyAnnouncedTriforceCount then
        ite.AnnounceTriforceCount(triforces)
        previouslyAnnouncedTriforceCount <- triforces
    // items
    if not remindedLadder && playerComputedStateSummary.HaveLadder then
        ite.RemindShortly(ITEMS.LADDER)
        remindedLadder <- true
    if not remindedAnyKey && playerComputedStateSummary.HaveAnyKey then
        ite.RemindShortly(ITEMS.KEY)
        remindedAnyKey <- true


        






