module DungeonSaveAndLoad

open DungeonRoomState

[<AllowNullLiteral>]
type DungeonRoomModel() =
    member val IsCompleted = false with get,set
    member val RoomType = "" with get,set
    member val MonsterDetail = "" with get,set
    member val FloorDropDetail = "" with get,set
    member val FloorDropShouldAppearBright = false with get,set
    member this.IsDefault =
        not(this.IsCompleted) && 
            this.RoomType=RoomType.Unmarked.AsHotKeyName() &&
            this.MonsterDetail=MonsterDetail.Unmarked.AsHotKeyName() &&
            this.FloorDropDetail=FloorDropDetail.Unmarked.AsHotKeyName() &&
            this.FloorDropShouldAppearBright
    member this.AsDungeonRoomState() =
        let r = new DungeonRoomState()
        r.IsComplete <- this.IsCompleted
        r.RoomType <- RoomType.FromHotKeyName this.RoomType
        r.MonsterDetail <- MonsterDetail.FromHotKeyName this.MonsterDetail
        r.FloorDropDetail <- FloorDropDetail.FromHotKeyName this.FloorDropDetail
        if this.FloorDropShouldAppearBright <> r.FloorDropAppearsBright then
            r.ToggleFloorDropBrightness()
        r

let DungeonRoomStateAsModel(state : DungeonRoomState) =
    let r = new DungeonRoomModel()
    r.IsCompleted <- state.IsComplete
    r.RoomType <- state.RoomType.AsHotKeyName()
    r.MonsterDetail <- state.MonsterDetail.AsHotKeyName()
    r.FloorDropDetail <- state.FloorDropDetail.AsHotKeyName()
    r.FloorDropShouldAppearBright <- state.FloorDropAppearsBright
    r

type DungeonModel() =  // these are serialized in j,i order, to be more human-readable
    member val HorizontalDoors : int[][] = null with get,set
    member val VerticalDoors : int[][] = null with get,set
    member val RoomIsCircled : bool[][] = null with get,set
    member val RoomStates : DungeonRoomModel[][] = null with get,set

let SaveDungeonModel(prefix, model:DungeonModel) =
    let lines = ResizeArray()
    lines.Add(""""HorizontalDoors": [""")
    for j = 0 to 7 do
        let sb = new System.Text.StringBuilder("    [ ")
        for i = 0 to 6 do
            sb.Append(sprintf "%d%s" model.HorizontalDoors.[i].[j] (if i=6 then "" else ",")) |> ignore
        sb.Append(sprintf " ]%s" (if j=7 then "" else ",")) |> ignore
        lines.Add(sb.ToString())
    lines.Add("""],""")
    lines.Add(""""VerticalDoors": [""")
    for j = 0 to 6 do
        let sb = new System.Text.StringBuilder("    [ ")
        for i = 0 to 7 do
            sb.Append(sprintf "%d%s" model.VerticalDoors.[i].[j] (if i=7 then "" else ",")) |> ignore
        sb.Append(sprintf " ]%s" (if j=6 then "" else ",")) |> ignore
        lines.Add(sb.ToString())
    lines.Add("""],""")
    lines.Add(""""RoomIsCircled": [""")
    for j = 0 to 7 do
        let sb = new System.Text.StringBuilder("    [ ")
        for i = 0 to 7 do
            sb.Append(sprintf "%b%s" model.RoomIsCircled.[i].[j] (if i=7 then "" else ",")) |> ignore
        sb.Append(sprintf " ]%s" (if j=7 then "" else ",")) |> ignore
        lines.Add(sb.ToString())
    lines.Add("""],""")
    lines.Add(""""RoomStates": [""")
    for j = 0 to 7 do
        lines.Add("    [")
        for i = 0 to 7 do
            let drm = model.RoomStates.[i].[j]
            if drm.IsDefault then
                lines.Add(sprintf """    null%s""" (if i=7 then "" else ","))
            else
                lines.Add(sprintf """    { "IsCompleted": %b, "RoomType": "%s", "MonsterDetail": "%s", "FloorDropDetail": "%s", "FloorDropShouldAppearBright": %b }%s"""
                                        drm.IsCompleted drm.RoomType drm.MonsterDetail drm.FloorDropDetail drm.FloorDropShouldAppearBright (if i=7 then "" else ","))
        lines.Add(sprintf "    ]%s" (if j=7 then "" else ","))
    lines.Add("""]""")
    lines |> Seq.map (fun s -> prefix+s) |> Seq.toArray

let SaveAllDungeons(models: DungeonModel[]) =
    [|
        for i = 0 to 8 do
            yield! SaveDungeonModel("    ", models.[i])
            yield sprintf "    }%s" (if i=8 then "" else ", {")
    |]

open SaveAndLoad
        
type AllData() =
    member val Version = "" with get,set
    member val Overworld : Overworld = null with get,set
    member val Items : Items = null with get,set
    member val PlayerProgressAndTakeAnyHearts : PlayerProgressAndTakeAnyHeartsModel = null with get,set
    member val StartingItemsAndExtras : StartingItemsAndExtrasModel = null with get,set
    member val Blockers : string[][] = null with get,set
    member val Notes = "" with get,set
    member val DungeonMaps : DungeonModel[] = null with get,set

let LoadAll(filename) =  // can throw
    let json = System.IO.File.ReadAllText(filename)
    let data = System.Text.Json.JsonSerializer.Deserialize<AllData>(json, new System.Text.Json.JsonSerializerOptions(AllowTrailingCommas=true))
    data
