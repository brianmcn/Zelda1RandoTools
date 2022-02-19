module SaveAndLoad

[<AllowNullLiteral>]
type Overworld() =
    member val Quest = -1 with get,set
    member val Map : int[] = null with get,set

type AllData() =
    member val Version = "" with get,set
    member val Overworld : Overworld = null with get,set

let SaveOverworld(prefix) =
    // - overworld (OWQuest, overworldMapMarks, overworldMapExtraData, mapLastChangedTime/recompute)
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
    lines.Add(sprintf """}""")
    lines |> Seq.map (fun s -> prefix+s) |> Seq.toArray

let SaveAll() =  // can throw
    let lines = [|
        yield sprintf """{"""
        yield sprintf """    "Version": "%s",""" OverworldData.VersionString
        yield! SaveOverworld("    ")
        yield sprintf """}"""
        |]
    let filename = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "zt-save-" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".json")
    //let filename = "J:\\-impossiblesdkgfjhsdg;kdahfskjgfdhsgfh;lahjds;ljfdhs;ljfhldashfldashlfadshgflhjdgflajdgfjkl"  // test errors
    System.IO.File.WriteAllLines(filename, lines)
    filename

let LoadAll(filename) =  // can throw
    let json = System.IO.File.ReadAllText(filename)
    let data = System.Text.Json.JsonSerializer.Deserialize<AllData>(json, new System.Text.Json.JsonSerializerOptions(AllowTrailingCommas=true))
    data
    