module SaveAndLoad

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
    SaveBox("        ", TrackerModel.sword2Box)
    lines.Add(sprintf """    }, "LadderBox": {""")
    SaveBox("        ", TrackerModel.ladderBox)
    lines.Add(sprintf """    }, "ArmosBox": {""")
    SaveBox("        ", TrackerModel.armosBox)
    lines.Add(sprintf """    }, "Dungeons": [""")
    for i = 0 to 8 do
        let d = TrackerModel.GetDungeon(i)
        lines.Add(sprintf """            { "Triforce": %b, "Color": %d, "LabelChar": "%s", "Boxes": [ {""" (d.PlayerHasTriforce()) d.Color (d.LabelChar.ToString())) |> ignore
        for box in d.Boxes do
            SaveBox("                    ", box)
            lines.Add("                }, {")
        lines.RemoveAt(lines.Count-1)
        if i<>8 then
            lines.Add("        } ] },")
        else
            lines.Add("} ] } ] },")
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
        yield! SaveBlockers("    ")
        yield sprintf """    "Notes": %s,""" (System.Text.Json.JsonSerializer.Serialize notesText)
        yield sprintf """    "DungeonMaps": """
        yield! dungeonModelsJsonLines |> Array.map (fun s -> "        "+s)
        yield sprintf """}"""
        |]
    let filename = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "zt-save-" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".json")
    //let filename = "J:\\-impossiblesdkgfjhsdg;kdahfskjgfdhsgfh;lahjds;ljfdhs;ljfhldashfldashlfadshgflhjdgflajdgfjkl"  // test errors
    System.IO.File.WriteAllLines(filename, lines)
    filename
    