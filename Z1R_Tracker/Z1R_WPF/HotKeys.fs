module HotKeys

open System.Windows

////////////////////////////////////////////////////////////
// Main Window listens for KeyDown, then sends RoutedEvent to element under the mouse

module MyKey =
    type MyKeyRoutedEventHandler = delegate of obj * MyKeyRoutedEventArgs -> unit
    and  MyKeyRoutedEventArgs(key) =
        inherit RoutedEventArgs(MyKey.MyKeyEvent)
        member _this.Key : Input.Key = key
    and  MyKey() =
        static let myKeyEvent = EventManager.RegisterRoutedEvent("ZTrackerMyKey", RoutingStrategy.Bubble, typeof<MyKeyRoutedEventHandler>, typeof<MyKey>)
        static member MyKeyEvent = myKeyEvent

    type UIElement with
        member this.MyKeyAdd(f) = this.AddHandler(MyKey.MyKeyEvent, new MyKeyRoutedEventHandler(fun o ea -> f(ea)))

open MyKey

let InitializeWindow(w:Window) =
    w.Focusable <- true
    w.PreviewKeyDown.Add(fun ea ->
        let keycode = int ea.Key
        if keycode >=34 && keycode <=69 || ea.Key=Input.Key.OemMinus then  // 0-9a-z_ are all the hotkeys I bind
            let x = Input.Mouse.DirectlyOver
            if x <> null then
                let ea = new MyKeyRoutedEventArgs(ea.Key)
                x.RaiseEvent(ea)
        )

let convertAlpha_NumToKey(ch) =
    if ch >= 'a' && ch <= 'z' then
        enum<Input.Key>(int ch - int 'a' + 44)
    elif ch >= 'A' && ch <= 'Z' then
        enum<Input.Key>(int ch - int 'A' + 44)
    elif ch = '_' then
        Input.Key.OemMinus  // Minus and Underscore on same key on typical keyboard; only handling this as _ is part of \w regex
    elif ch >= '0' && ch <= '9' then
        enum<Input.Key>(int ch - int '0' + 34)
    else
        failwithf "bad input to convertAlpha_NumToKey '%c'" ch


////////////////////////////////////////////////////////////

type UserError(msg) =
    inherit System.Exception(msg)

let AllDungeonRoomNames = [|
    for x in DungeonRoomState.RoomType.All() do
        yield "DungeonRoom_" + x.AsHotKeyName()
    for x in DungeonRoomState.MonsterDetail.All() do
        yield "DungeonRoom_" + x.AsHotKeyName()
    for x in DungeonRoomState.FloorDropDetail.All() do
        yield "DungeonRoom_" + x.AsHotKeyName()
    |]

let MakeDefaultHotKeyFile(filename:string) =
    let lines = ResizeArray()
    (sprintf """# %s HotKeys

# General form is 'SelectorName = key'
#  - key can be 0-9 or a-z
#  - SelectorName can be any of the list below
# You can leave the key blank to not bind a hotkey to that selector

# Blank lines, or lines that start with '#', are comments and are ignored by the parser
# All other lines must have the 'General form' syntax, though whitespace is optional and ignored

    """ OverworldData.ProgramNameString).Split('\n') |> Array.iter (fun line -> lines.Add(line.Replace("\r","")))
    // items
    lines.Add("# ITEMS - these hotkey bindings take effect when mouse-hovering an item box")
    lines.Add("# Note that BookOrShield refers to Book in a non-boomstick-seed, and Shield in a boomstick seed")
    for i = 0 to 14 do
        lines.Add("Item_" + TrackerModel.ITEMS.AsHotKeyName(i) + " = ")
    lines.Add("Item_Nothing = ")
    lines.Add("")
    // overworld map tiles
    lines.Add("# OVERWORLD - these hotkey bindings take effect when mouse-hovering an overworld map tile")
    lines.Add("# Note that Level1-Level8 refer to dungeons A-H if using the 'Hide Dungeon Numbers' flag setting")
    for i = 0 to TrackerModel.dummyOverworldTiles.Length-1 do
        lines.Add("Overworld_" + TrackerModel.MapSquareChoiceDomainHelper.AsHotKeyName(i) + " = ")
    lines.Add("Overworld_Nothing = ")
    lines.Add("")
    // blockers
    lines.Add("# BLOCKERS - these hotkey bindings take effect when mouse-hovering a blocker box")
    for b in TrackerModel.DungeonBlocker.All do
        lines.Add(b.AsHotKeyName() + " = ")
    lines.Add("")
    // dungeon rooms
    lines.Add("# DUNGEON ROOMS - these hotkey bindings take effect when mouse-hovering a room in a dungeon")
    for x in AllDungeonRoomNames do
        lines.Add(x + " = ")
    lines.Add("")
    System.IO.File.WriteAllLines(filename, lines)

let ParseHotKeyDataFile(filename:string) =
    let lines = System.IO.File.ReadAllLines(filename)
    let commentRegex = new System.Text.RegularExpressions.Regex("^#.*$", System.Text.RegularExpressions.RegexOptions.None)
    let emptyLineRegex = new System.Text.RegularExpressions.Regex("^\s*$", System.Text.RegularExpressions.RegexOptions.None)
    let dataRegex = new System.Text.RegularExpressions.Regex("^\s*(\w+)\s*=\s*(\w)?\s*$", System.Text.RegularExpressions.RegexOptions.None)
    let data = ResizeArray()
    let mutable lineNumber = 1
    for line in lines do
        if commentRegex.IsMatch(line) || emptyLineRegex.IsMatch(line) then
            () // skip
        else
            let m = dataRegex.Match(line)
            if not m.Success then
                raise <| new UserError(sprintf "Error parsing '%s', line %d" filename lineNumber)
            else
                // Groups.[0] is the whole match
                let name = m.Groups.[1].Value
                let value = if m.Groups.Count > 2 && m.Groups.[2].Value.Length > 0 then Some(m.Groups.[2].Value.[0]) else None
                data.Add(name, value, (lineNumber, filename))
        lineNumber <- lineNumber + 1
    data

////////////////////////////////////////////////////////////

let keyUniverse = [| yield! [|'0'..'9'|]; yield! [|'a'..'z'|]; yield '_' |]
type HotKeyProcessor<'v when 'v : equality>(contextName) =
    let table = new System.Collections.Generic.Dictionary<Input.Key,'v>()
    let stateToKeys = new System.Collections.Generic.Dictionary<_,_>()  // caching
    member this.ContextName = contextName
    member this.TryGetValue(k) = 
        match table.TryGetValue(k) with
        | true, v -> Some(v)
        | _ -> None
    member this.TryAdd(k,v) =
        if table.ContainsKey(k) then
            false
        else
            table.Add(k,v)
            true
    member this.StateToKeys(state) =
        if not(stateToKeys.ContainsKey(state)) then
            let r = ResizeArray()
            for k in keyUniverse do
                match this.TryGetValue(convertAlpha_NumToKey k) with
                | Some x -> if x = state then r.Add(k)
                | None -> ()
            stateToKeys.Add(state, r)
            r
        else
            stateToKeys.[state]
    member this.AppendHotKeyToDescription(desc, state) =
        let keys = this.StateToKeys(state)
        if keys.Count > 0 then
            sprintf "%s\nHotKey = %c" desc keys.[0]
        else
            desc

let ItemHotKeyProcessor = new HotKeyProcessor<int>("Item")
let OverworldHotKeyProcessor = new HotKeyProcessor<int>("Overworld")
let BlockerHotKeyProcessor = new HotKeyProcessor<TrackerModel.DungeonBlocker>("Blocker")
let DungeonRoomHotKeyProcessor = new HotKeyProcessor<Choice<DungeonRoomState.RoomType,DungeonRoomState.MonsterDetail,DungeonRoomState.FloorDropDetail> >("DungeonRoom")

let HotKeyFilename = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "HotKeys.txt")

let PopulateHotKeyTables() =
    let filename = HotKeyFilename
    if not(System.IO.File.Exists(filename)) then
        MakeDefaultHotKeyFile(filename)
    let data = ParseHotKeyDataFile(filename)
    for name, chOpt, (lineNumber, filename) in data do
        let Add(hkp:HotKeyProcessor<_>, chOpt, x) =
            match chOpt with
            | None -> ()  // Foo=    means Foo is not bound to a hotkey on this line
            | Some ch ->
                let key = convertAlpha_NumToKey ch
                if not(hkp.TryAdd(key,x)) then
                    raise <| new UserError(sprintf "Keyboard key '%c' given multiple meanings for '%s' context; second occurrence at line %d of '%s'" ch hkp.ContextName lineNumber filename)
        match name with
        | "Item_Nothing"          -> Add(ItemHotKeyProcessor, chOpt, -1)
        | "Overworld_Nothing"     -> Add(OverworldHotKeyProcessor, chOpt, -1)
        | _ -> 
            let mutable found = false
            for b in TrackerModel.DungeonBlocker.All do
                if name = b.AsHotKeyName() then
                    Add(BlockerHotKeyProcessor, chOpt, b)
                    found <- true
            if not found then
                for i = 0 to 14 do
                    if name = "Item_" + TrackerModel.ITEMS.AsHotKeyName(i) then
                        Add(ItemHotKeyProcessor, chOpt, i)
                        found <- true
            if not found then
                for i = 0 to TrackerModel.dummyOverworldTiles.Length-1 do
                    if name = "Overworld_" + TrackerModel.MapSquareChoiceDomainHelper.AsHotKeyName(i) then
                        Add(OverworldHotKeyProcessor, chOpt, i)
                        found <- true
            if not found then
                for x in DungeonRoomState.RoomType.All() do
                    if name = "DungeonRoom_" + x.AsHotKeyName() then
                        Add(DungeonRoomHotKeyProcessor, chOpt, Choice1Of3 x)
                        found <- true
                for x in DungeonRoomState.MonsterDetail.All() do
                    if name = "DungeonRoom_" + x.AsHotKeyName() then
                        Add(DungeonRoomHotKeyProcessor, chOpt, Choice2Of3 x)
                        found <- true
                for x in DungeonRoomState.FloorDropDetail.All() do
                    if name = "DungeonRoom_" + x.AsHotKeyName() then
                        Add(DungeonRoomHotKeyProcessor, chOpt, Choice3Of3 x)
                        found <- true
            if not found then
                raise <| new UserError(sprintf "Bad name '%s' specified in '%s', line %d" name filename lineNumber)

