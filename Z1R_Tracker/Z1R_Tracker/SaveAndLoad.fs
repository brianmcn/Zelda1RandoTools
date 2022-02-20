﻿module SaveAndLoad

[<AllowNullLiteral>]
type Overworld() =
    member val Quest = -1 with get,set
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

//////////////////////////////////////////////////////////////////////////////

let SaveOverworld(prefix) =
    let lines = ResizeArray()
    lines.Add(sprintf """"Overworld": {""")
    lines.Add(sprintf """    "Quest": %d,""" (TrackerModel.owInstance.Quest.AsInt()))
    lines.Add(sprintf """    "Map": [""")
    for j = 0 to 7 do
        let sb = new System.Text.StringBuilder("        ")
        for i = 0 to 15 do
            let cur = TrackerModel.overworldMapMarks.[i,j].Current()
            let k = if TrackerModel.MapSquareChoiceDomainHelper.IsItem(cur) then TrackerModel.MapSquareChoiceDomainHelper.SHOP else cur
            let ed = if cur = -1 then -1 else TrackerModel.getOverworldMapExtraData(i,j,k)
            let comma = if j=7 && i=15 then "" else ","
            sb.Append(sprintf "%2d,%2d%s  " cur ed comma) |> ignore
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
    lines.Add(""""Blockers": [""")
    for i = 0 to 7 do
        lines.Add(sprintf """    [ "%s", "%s" ]%s""" (TrackerModel.DungeonBlockersContainer.GetDungeonBlocker(i,0).AsHotKeyName()) (TrackerModel.DungeonBlockersContainer.GetDungeonBlocker(i,1).AsHotKeyName()) (if i<>7 then "," else ""))
    lines.Add("""],""")
    lines |> Seq.map (fun s -> prefix+s) |> Seq.toArray

let SaveAll(notesText:string, dungeonModelsJsonLines:string[]) =  // can throw
    let lines = [|
        yield sprintf """{"""
        yield sprintf """    "Version": "%s",""" OverworldData.VersionString
        yield! SaveOverworld("    ")
        yield! SaveItems("    ")
        yield! SavePlayerProgressAndTakeAnyHearts("    ")
        yield! SaveStartingItemsAndExtras("    ")
        yield! SaveBlockers("    ")
        yield sprintf """    "Notes": %s,""" (System.Text.Json.JsonSerializer.Serialize notesText)
        yield sprintf """    "DungeonMaps": [ {"""
        yield! dungeonModelsJsonLines |> Array.map (fun s -> "    "+s)
        yield sprintf """    ]"""
        yield sprintf """}"""
        |]
    let filename = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "zt-save-" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".json")
    //let filename = "J:\\-impossiblesdkgfjhsdg;kdahfskjgfdhsgfh;lahjds;ljfdhs;ljfhldashfldashlfadshgflhjdgflajdgfjkl"  // test errors
    System.IO.File.WriteAllLines(filename, lines)
    filename
    