module SaveAndLoad

let AutoSaveFilename = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "zt-save-zz-autosave.json")

[<AllowNullLiteral>]
type Overworld() =
    member val Quest = -1 with get,set
    member val MirrorOverworld = false with get,set
    member val StartIconX = -1 with get,set
    member val StartIconY = -1 with get,set
    member val Map : int[] = null with get,set

[<AllowNullLiteral>]
type Box() =
    member val CellCurrent = -1 with get,set
    member val PlayerHas = 0 with get,set
    member this.TryApply(b : TrackerModel.Box) = b.AttemptToSet(this.CellCurrent, TrackerModel.PlayerHas.FromInt this.PlayerHas)

[<AllowNullLiteral>]
type Dungeon() =
    member val Triforce = false with get,set
    member val Color = 0 with get,set
    member val LabelChar = "?" with get,set
    member val Boxes : Box[] = null with get,set
    member this.TryApply(d : TrackerModel.Dungeon) =
        if this.Triforce <> d.PlayerHasTriforce() then
            d.ToggleTriforce()
        d.Color <- this.Color
        d.LabelChar <- this.LabelChar.[0]
        (this.Boxes, d.Boxes) ||> Array.map2 (fun bs bd -> bs.TryApply(bd)) |> Array.fold (fun a b -> a && b) true

[<AllowNullLiteral>]
type Items() =
    member val HiddenDungeonNumbers = false with get,set
    member val SecondQuestDungeons = false with get,set
    member val WhiteSwordBox : Box = null with get,set
    member val LadderBox : Box = null with get,set
    member val ArmosBox : Box = null with get,set
    member val Dungeons : Dungeon[] = null with get,set

[<AllowNullLiteral>]
type Blocker() =
    member val Kind = "" with get,set
    member val AppliesTo : bool[] = null with get,set  // map, compass, tri, box1, box2, box3

[<AllowNullLiteral>]
type Hints() =
    member val LocationHints : int[] = null with get,set
    member val NoFeatOfStrengthHint = false with get,set
    member val SailNotHint = false with get,set

[<AllowNullLiteral>]
type PlayerProgressAndTakeAnyHeartsModel() =
    member val TakeAnyHearts : int[] = null with get,set
    member val PlayerHasBoomBook = false with get,set
    member val PlayerHasWoodSword = false with get,set
    member val PlayerHasWoodArrow = false with get,set
    member val PlayerHasBlueRing = false with get,set
    member val PlayerHasBlueCandle = false with get,set
    member val PlayerHasMagicalSword = false with get,set
    member val PlayerHasDefeatedGanon = false with get,set
    member val PlayerHasRescuedZelda = false with get,set
    member val PlayerHasBombs = false with get,set
    static member Create() =
        let r = new PlayerProgressAndTakeAnyHeartsModel()
        r.TakeAnyHearts <- Array.init 4 (fun i -> TrackerModel.playerProgressAndTakeAnyHearts.GetTakeAnyHeart(i))
        r.PlayerHasBoomBook <- TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Value()
        r.PlayerHasWoodSword <- TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword.Value()
        r.PlayerHasWoodArrow <- TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodArrow.Value()
        r.PlayerHasBlueRing <- TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueRing.Value()
        r.PlayerHasBlueCandle <- TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle.Value()
        r.PlayerHasMagicalSword <- TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Value()
        r.PlayerHasDefeatedGanon <- TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasDefeatedGanon.Value()
        r.PlayerHasRescuedZelda <- TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Value()
        r.PlayerHasBombs <- TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBombs.Value()
        r
    member this.Apply() =
        for i = 0 to 3 do
            TrackerModel.playerProgressAndTakeAnyHearts.SetTakeAnyHeart(i, this.TakeAnyHearts.[i])
        TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Set(this.PlayerHasBoomBook)
        TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodSword.Set(this.PlayerHasWoodSword)
        TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasWoodArrow.Set(this.PlayerHasWoodArrow)
        TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueRing.Set(this.PlayerHasBlueRing)
        TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBlueCandle.Set(this.PlayerHasBlueCandle)
        TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasMagicalSword.Set(this.PlayerHasMagicalSword)
        TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasDefeatedGanon.Set(this.PlayerHasDefeatedGanon)
        TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasRescuedZelda.Set(this.PlayerHasRescuedZelda)
        TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBombs.Set(this.PlayerHasBombs)

[<AllowNullLiteral>]
type StartingItemsAndExtrasModel() =
    member val HDNStartingTriforcePieces : bool[] = null with get,set
    member val PlayerHasWhiteSword = false with get,set
    member val PlayerHasSilverArrow = false with get,set
    member val PlayerHasBow = false with get,set
    member val PlayerHasWand = false with get,set
    member val PlayerHasRedCandle = false with get,set
    member val PlayerHasBoomerang = false with get,set
    member val PlayerHasMagicBoomerang = false with get,set
    member val PlayerHasRedRing = false with get,set
    member val PlayerHasPowerBracelet = false with get,set
    member val PlayerHasLadder = false with get,set
    member val PlayerHasRaft = false with get,set
    member val PlayerHasRecorder = false with get,set
    member val PlayerHasAnyKey = false with get,set
    member val PlayerHasBook = false with get,set
    member val MaxHeartsDifferential = 0 with get,set
    static member Create() =
        let r = new StartingItemsAndExtrasModel()
        r.HDNStartingTriforcePieces <- Array.init 8 (fun i -> TrackerModel.startingItemsAndExtras.HDNStartingTriforcePieces.[i].Value())
        r.PlayerHasWhiteSword <- TrackerModel.startingItemsAndExtras.PlayerHasWhiteSword.Value()
        r.PlayerHasSilverArrow <- TrackerModel.startingItemsAndExtras.PlayerHasSilverArrow.Value()
        r.PlayerHasBow <- TrackerModel.startingItemsAndExtras.PlayerHasBow.Value()
        r.PlayerHasWand <- TrackerModel.startingItemsAndExtras.PlayerHasWand.Value()
        r.PlayerHasRedCandle <- TrackerModel.startingItemsAndExtras.PlayerHasRedCandle.Value()
        r.PlayerHasBoomerang <- TrackerModel.startingItemsAndExtras.PlayerHasBoomerang.Value()
        r.PlayerHasMagicBoomerang <- TrackerModel.startingItemsAndExtras.PlayerHasMagicBoomerang.Value()
        r.PlayerHasRedRing <- TrackerModel.startingItemsAndExtras.PlayerHasRedRing.Value()
        r.PlayerHasPowerBracelet <- TrackerModel.startingItemsAndExtras.PlayerHasPowerBracelet.Value()
        r.PlayerHasLadder <- TrackerModel.startingItemsAndExtras.PlayerHasLadder.Value()
        r.PlayerHasRaft <- TrackerModel.startingItemsAndExtras.PlayerHasRaft.Value()
        r.PlayerHasRecorder <- TrackerModel.startingItemsAndExtras.PlayerHasRecorder.Value()
        r.PlayerHasAnyKey <- TrackerModel.startingItemsAndExtras.PlayerHasAnyKey.Value()
        r.PlayerHasBook <- TrackerModel.startingItemsAndExtras.PlayerHasBook.Value()
        r.MaxHeartsDifferential <- TrackerModel.startingItemsAndExtras.MaxHeartsDifferential
        r
    member this.Apply() =
        for i = 0 to 7 do
            TrackerModel.startingItemsAndExtras.HDNStartingTriforcePieces.[i].Set(this.HDNStartingTriforcePieces.[i])
        TrackerModel.startingItemsAndExtras.PlayerHasWhiteSword.Set(this.PlayerHasWhiteSword)
        TrackerModel.startingItemsAndExtras.PlayerHasSilverArrow.Set(this.PlayerHasSilverArrow)
        TrackerModel.startingItemsAndExtras.PlayerHasBow.Set(this.PlayerHasBow)
        TrackerModel.startingItemsAndExtras.PlayerHasWand.Set(this.PlayerHasWand)
        TrackerModel.startingItemsAndExtras.PlayerHasRedCandle.Set(this.PlayerHasRedCandle)
        TrackerModel.startingItemsAndExtras.PlayerHasBoomerang.Set(this.PlayerHasBoomerang)
        TrackerModel.startingItemsAndExtras.PlayerHasMagicBoomerang.Set(this.PlayerHasMagicBoomerang)
        TrackerModel.startingItemsAndExtras.PlayerHasRedRing.Set(this.PlayerHasRedRing)
        TrackerModel.startingItemsAndExtras.PlayerHasPowerBracelet.Set(this.PlayerHasPowerBracelet)
        TrackerModel.startingItemsAndExtras.PlayerHasLadder.Set(this.PlayerHasLadder)
        TrackerModel.startingItemsAndExtras.PlayerHasRaft.Set(this.PlayerHasRaft)
        TrackerModel.startingItemsAndExtras.PlayerHasRecorder.Set(this.PlayerHasRecorder)
        TrackerModel.startingItemsAndExtras.PlayerHasAnyKey.Set(this.PlayerHasAnyKey)
        TrackerModel.startingItemsAndExtras.PlayerHasBook.Set(this.PlayerHasBook)
        TrackerModel.startingItemsAndExtras.MaxHeartsDifferential <- this.MaxHeartsDifferential

[<AllowNullLiteral>]
type TimelineDatum() =
    member val Ident = "" with get,set
    member val Seconds = -1 with get,set
    member val Has = 1 with get,set

//////////////////////////////////////////////////////////////////////////////

let SaveOverworld(prefix) =
    let lines = ResizeArray()
    lines.Add(sprintf """"Overworld": {""")
    lines.Add(sprintf """    "Quest": %d,""" (TrackerModel.owInstance.Quest.AsInt()))
    lines.Add(sprintf """    "MirrorOverworld": %b,""" (TrackerModel.Options.Overworld.MirrorOverworld.Value))
    lines.Add(sprintf """    "StartIconX": %d,""" TrackerModel.startIconX)
    lines.Add(sprintf """    "StartIconY": %d,""" TrackerModel.startIconY)
    lines.Add(sprintf """    "Map": [""")
    for j = 0 to 7 do
        let sb = new System.Text.StringBuilder("        ")
        for i = 0 to 15 do
            let cur = TrackerModel.overworldMapMarks.[i,j].Current()
            let k = if TrackerModel.MapSquareChoiceDomainHelper.IsItem(cur) then TrackerModel.MapSquareChoiceDomainHelper.SHOP else cur
            let ed = if cur = -1 then -1 else TrackerModel.getOverworldMapExtraData(i,j,k)   // TODO why -1 case, not exist?
            let circle = TrackerModel.overworldMapCircles.[i,j]
            let comma = if j=7 && i=15 then "" else ","
            sb.Append(sprintf "%2d,%2d,%3d%s  " cur ed circle comma) |> ignore
        lines.Add(sb.ToString())
    lines.Add(sprintf """    ]""")
    lines.Add(sprintf """},""")
    lines |> Seq.map (fun s -> prefix+s) |> Seq.toArray

let SaveItems(prefix) =
    let lines = ResizeArray()
    let SaveBox(pre, box:TrackerModel.Box) =
        lines.Add(sprintf """%s%s"CellCurrent": %d, "PlayerHas": %d""" prefix pre (box.CellCurrent()) (box.PlayerHas().AsInt()))
    lines.Add(sprintf """"Items": {""")
    lines.Add(sprintf """    "HiddenDungeonNumbers": %b,""" (TrackerModel.IsHiddenDungeonNumbers()))
    lines.Add(sprintf """    "SecondQuestDungeons": %b,""" TrackerModel.Options.IsSecondQuestDungeons.Value)
    lines.Add(sprintf """    "WhiteSwordBox": {""")
    SaveBox("    ", TrackerModel.sword2Box)
    lines.Add(sprintf """    }, "LadderBox": {""")
    SaveBox("    ", TrackerModel.ladderBox)
    lines.Add(sprintf """    }, "ArmosBox": {""")
    SaveBox("    ", TrackerModel.armosBox)
    lines.Add(sprintf """    }, "Dungeons": [""")
    for i = 0 to 8 do
        let d = TrackerModel.GetDungeon(i)
        lines.Add(sprintf """            { "Triforce": %b, "Color": %d, "LabelChar": "%s", "Boxes": [ {""" (d.PlayerHasTriforce()) d.Color (d.LabelChar.ToString())) |> ignore
        for box in d.Boxes do
            SaveBox("            ", box)
            lines.Add("                }, {")
        lines.RemoveAt(lines.Count-1)
        if i<>8 then
            lines.Add("        } ] },")
        else
            lines.Add("} ] } ] },")
    lines |> Seq.map (fun s -> prefix+s) |> Seq.toArray

let SavePlayerProgressAndTakeAnyHearts(prefix) =
    let m = PlayerProgressAndTakeAnyHeartsModel.Create()
    let lines = ResizeArray()
    lines.Add(""""PlayerProgressAndTakeAnyHearts": {""")
    lines.Add(sprintf """    "TakeAnyHearts": [ %d, %d, %d, %d ],""" m.TakeAnyHearts.[0] m.TakeAnyHearts.[1] m.TakeAnyHearts.[2] m.TakeAnyHearts.[3])
    lines.Add(sprintf """    "PlayerHasBoomBook": %b,""" m.PlayerHasBoomBook)
    lines.Add(sprintf """    "PlayerHasWoodSword": %b,""" m.PlayerHasWoodSword)
    lines.Add(sprintf """    "PlayerHasWoodArrow": %b,""" m.PlayerHasWoodArrow)
    lines.Add(sprintf """    "PlayerHasBlueRing": %b,""" m.PlayerHasBlueRing)
    lines.Add(sprintf """    "PlayerHasBlueCandle": %b,""" m.PlayerHasBlueCandle)
    lines.Add(sprintf """    "PlayerHasMagicalSword": %b,""" m.PlayerHasMagicalSword)
    lines.Add(sprintf """    "PlayerHasDefeatedGanon": %b,""" m.PlayerHasDefeatedGanon)
    lines.Add(sprintf """    "PlayerHasRescuedZelda": %b,""" m.PlayerHasRescuedZelda)
    lines.Add(sprintf """    "PlayerHasBombs": %b""" m.PlayerHasBombs)
    lines.Add("""},""")
    lines |> Seq.map (fun s -> prefix+s) |> Seq.toArray

let SaveStartingItemsAndExtras(prefix) =
    let m = StartingItemsAndExtrasModel.Create()
    let lines = ResizeArray()
    lines.Add(""""StartingItemsAndExtras": {""")
    lines.Add(sprintf """    "HDNStartingTriforcePieces": [ %b, %b, %b, %b, %b, %b, %b, %b ],""" m.HDNStartingTriforcePieces.[0] m.HDNStartingTriforcePieces.[1] m.HDNStartingTriforcePieces.[2]
                        m.HDNStartingTriforcePieces.[3] m.HDNStartingTriforcePieces.[4] m.HDNStartingTriforcePieces.[5] m.HDNStartingTriforcePieces.[6] m.HDNStartingTriforcePieces.[7])
    lines.Add(sprintf """    "PlayerHasWhiteSword": %b,""" m.PlayerHasWhiteSword)
    lines.Add(sprintf """    "PlayerHasSilverArrow": %b,""" m.PlayerHasSilverArrow)
    lines.Add(sprintf """    "PlayerHasBow": %b,""" m.PlayerHasBow)
    lines.Add(sprintf """    "PlayerHasWand": %b,""" m.PlayerHasWand)
    lines.Add(sprintf """    "PlayerHasRedCandle": %b,""" m.PlayerHasRedCandle)
    lines.Add(sprintf """    "PlayerHasBoomerang": %b,""" m.PlayerHasBoomerang)
    lines.Add(sprintf """    "PlayerHasMagicBoomerang": %b,""" m.PlayerHasMagicBoomerang)
    lines.Add(sprintf """    "PlayerHasRedRing": %b,""" m.PlayerHasRedRing)
    lines.Add(sprintf """    "PlayerHasPowerBracelet": %b,""" m.PlayerHasPowerBracelet)
    lines.Add(sprintf """    "PlayerHasLadder": %b,""" m.PlayerHasLadder)
    lines.Add(sprintf """    "PlayerHasRaft": %b,""" m.PlayerHasRaft)
    lines.Add(sprintf """    "PlayerHasRecorder": %b,""" m.PlayerHasRecorder)
    lines.Add(sprintf """    "PlayerHasAnyKey": %b,""" m.PlayerHasAnyKey)
    lines.Add(sprintf """    "PlayerHasBook": %b,""" m.PlayerHasBook)
    lines.Add(sprintf """    "MaxHeartsDifferential": %d,""" m.MaxHeartsDifferential)
    lines.Add("""},""")
    lines |> Seq.map (fun s -> prefix+s) |> Seq.toArray

let SaveBlockers(prefix) =
    let lines = ResizeArray()
    lines.Add(""""Blockers": [ [""")
    for i = 0 to 7 do
        for j = 0 to TrackerModel.DungeonBlockersContainer.MAX_BLOCKERS_PER_DUNGEON-1 do
            let s = new System.Text.StringBuilder("    ")
            s.Append(TrackerModel.DungeonBlockersContainer.AsJsonString(i,j)) |> ignore
            if j < TrackerModel.DungeonBlockersContainer.MAX_BLOCKERS_PER_DUNGEON-1 then
                s.Append(", ") |> ignore
            lines.Add(s.ToString())
        if i <> 7 then
            lines.Add("], [")
        else
            lines.Add("] ],")
    lines |> Seq.map (fun s -> prefix+s) |> Seq.toArray

let SaveHints(prefix) =
    let lines = ResizeArray()
    lines.Add(""""Hints": {""")
    let sb = new System.Text.StringBuilder("""    "LocationHints": [ """)
    for i = 0 to 10 do
        sb.Append(sprintf "%d%s " (TrackerModel.GetLevelHint(i).ToIndex()) (if i=10 then "" else ",")) |> ignore
    sb.Append("],") |> ignore
    lines.Add(sb.ToString())
    lines.Add(sprintf """    "NoFeatOfStrengthHint": %b,""" TrackerModel.NoFeatOfStrengthHintWasGiven)
    lines.Add(sprintf """    "SailNotHint": %b""" TrackerModel.SailNotHintWasGiven)
    lines.Add("""},""")
    lines |> Seq.map (fun s -> prefix+s) |> Seq.toArray

type SaveType =
    | ManualSave     // user clicked 'Save'
    | FinishedSave   // user clicked Zelda and SaveOnCompletion option is on
    | AutoSave       // each time a minute has passed

let mutable lastKnownSeed, lastKnownFlags = "", ""
let seedAndFlagsUpdated = new Event<_>()
let seedAndFlagsRegex = new System.Text.RegularExpressions.Regex("_(\d+)_([a-zA-Z0-9!]+)", System.Text.RegularExpressions.RegexOptions.None)
let MaybePollSeedAndFlags() =
    if TrackerModel.Options.SnoopSeedAndFlags.Value then
        let procs = System.Diagnostics.Process.GetProcesses()
        for p in procs do
            if not(System.String.IsNullOrEmpty(p.MainWindowTitle)) then
                let m = seedAndFlagsRegex.Match(p.MainWindowTitle)
                if m.Success then
                    let seed = m.Groups.[1].Value
                    let flags = m.Groups.[2].Value
                    if seed.Length > 6 && flags.Length > 6 then   // just a guess-filter
                        lastKnownSeed <- seed
                        lastKnownFlags <- flags
                        seedAndFlagsUpdated.Trigger()

let SaveAll(notesText:string, selectedDungeonTab:int, dungeonModelsJsonLines:string[], drawingLayerJsonLines:string[], currentRecorderDestinationIndex, saveType) =  // can throw
    MaybePollSeedAndFlags()
    let totalSeconds = int (System.DateTime.Now - TrackerModel.theStartTime.Time).TotalSeconds
    let lines = [|
        yield sprintf """{"""
        yield sprintf """    "Version": "%s",""" OverworldData.VersionString
        yield sprintf """    "TimeInSeconds": %d,""" totalSeconds
        yield! SaveOverworld("    ")
        yield! SaveItems("    ")
        yield! SavePlayerProgressAndTakeAnyHearts("    ")
        yield! SaveStartingItemsAndExtras("    ")
        yield! SaveBlockers("    ")
        yield! SaveHints("    ")
        yield sprintf """    "Notes": %s,""" (System.Text.Json.JsonSerializer.Serialize notesText)
        yield sprintf """    "CurrentRecorderDestinationIndex": %d,""" currentRecorderDestinationIndex
        yield sprintf """    "DungeonTabSelected": %d,""" selectedDungeonTab
        yield sprintf """    "DungeonMaps": [ {"""
        yield! dungeonModelsJsonLines |> Array.map (fun s -> "    "+s)
        yield sprintf """    ],"""
        yield sprintf """    "DrawingLayerIcons": ["""
        yield! drawingLayerJsonLines
        yield sprintf """    ],"""
        if lastKnownSeed <> "" then
            yield sprintf """    "Seed": "%s",""" lastKnownSeed
        if lastKnownFlags <> "" then
            yield sprintf """    "Flags": "%s",""" lastKnownFlags
        // write the timeline 'pretty' at the bottom of the file, for people who want to easily see/parse splits
        yield sprintf """    "Timeline": ["""
        let tis = [|for KeyValue(_,ti) in TrackerModel.TimelineItemModel.All do yield ti |] |> Array.sortBy (fun ti -> ti.FinishedTotalSeconds)
        for i=0 to tis.Length-1 do
            let ti = tis.[i]
            yield sprintf """        { "Ident": "%-20s, "Seconds": %6d, "Has": %d }%s""" (ti.Identifier+"\"") (ti.FinishedTotalSeconds) (ti.Has.AsInt()) (if i=tis.Length-1 then "" else ",")
        yield sprintf """    ]"""
        yield sprintf """}"""
        |]
    let filename = 
        match saveType with
        | ManualSave -> System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "zt-save-manual-" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".json")
        | FinishedSave -> System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "zt-save-completed-" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".json")
        | AutoSave -> AutoSaveFilename
    //let filename = "J:\\-impossiblesdkgfjhsdg;kdahfskjgfdhsgfh;lahjds;ljfdhs;ljfhldashfldashlfadshgflhjdgflajdgfjkl"  // test errors
    System.IO.File.WriteAllLines(filename, lines)
    match saveType with
    | AutoSave -> System.IO.File.SetCreationTime(filename, System.DateTime.Now)
    | _ -> ()
    filename
    