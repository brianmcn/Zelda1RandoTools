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

let InitializeWindow(w:Window, notesTextBox:System.Windows.Controls.TextBox) =
    w.Focusable <- true
    w.PreviewKeyDown.Add(fun ea ->
        let x = Input.Mouse.DirectlyOver
        if x <> null && not(notesTextBox.IsKeyboardFocused) then
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
// always Bottom/Left/Top/Right order, how PieMenus code works
let TakeAnyNames = [|
    "Contextual_TakeAny_None"
    "Contextual_TakeAny_Potion"
    "Contextual_TakeAny_Candle"
    "Contextual_TakeAny_Heart"
    |]
let TakeThisNames = [|
    "Contextual_TakeThis_None"
    "Contextual_TakeThis_Candle"
    "Contextual_TakeThis_Sword"
|]

[<RequireQualifiedAccess>]
type GlobalHotkeyTargets =
    | ToggleMagicalSword
    | ToggleWoodSword
    | ToggleBoomBook
    | ToggleBlueCandle
    | ToggleWoodArrow
    | ToggleBlueRing
    | ToggleBombs
    | ToggleGannon
    | ToggleZelda
    | DungeonTab1
    | DungeonTab2
    | DungeonTab3
    | DungeonTab4
    | DungeonTab5
    | DungeonTab6
    | DungeonTab7
    | DungeonTab8
    | DungeonTab9
    | DungeonTabS
    | MoveCursorRight
    | MoveCursorLeft
    | MoveCursorUp
    | MoveCursorDown
    | LeftClick
    | MiddleClick
    | RightClick
    member this.AsHotKeyName() =
        match this with
        | GlobalHotkeyTargets.ToggleMagicalSword -> "ToggleMagicalSword"
        | GlobalHotkeyTargets.ToggleWoodSword    -> "ToggleWoodSword"
        | GlobalHotkeyTargets.ToggleBoomBook     -> "ToggleBoomBook"
        | GlobalHotkeyTargets.ToggleBlueCandle   -> "ToggleBlueCandle"
        | GlobalHotkeyTargets.ToggleWoodArrow    -> "ToggleWoodArrow"
        | GlobalHotkeyTargets.ToggleBlueRing     -> "ToggleBlueRing"
        | GlobalHotkeyTargets.ToggleBombs        -> "ToggleBombs"
        | GlobalHotkeyTargets.ToggleGannon       -> "ToggleGannon"
        | GlobalHotkeyTargets.ToggleZelda        -> "ToggleZelda"
        | GlobalHotkeyTargets.DungeonTab1        -> "DungeonTab1"
        | GlobalHotkeyTargets.DungeonTab2        -> "DungeonTab2"
        | GlobalHotkeyTargets.DungeonTab3        -> "DungeonTab3"
        | GlobalHotkeyTargets.DungeonTab4        -> "DungeonTab4"
        | GlobalHotkeyTargets.DungeonTab5        -> "DungeonTab5"
        | GlobalHotkeyTargets.DungeonTab6        -> "DungeonTab6"
        | GlobalHotkeyTargets.DungeonTab7        -> "DungeonTab7"
        | GlobalHotkeyTargets.DungeonTab8        -> "DungeonTab8"
        | GlobalHotkeyTargets.DungeonTab9        -> "DungeonTab9"
        | GlobalHotkeyTargets.DungeonTabS        -> "DungeonTabS"
        | GlobalHotkeyTargets.MoveCursorLeft     -> "MoveCursorLeft"
        | GlobalHotkeyTargets.MoveCursorRight    -> "MoveCursorRight"
        | GlobalHotkeyTargets.MoveCursorUp       -> "MoveCursorUp"
        | GlobalHotkeyTargets.MoveCursorDown     -> "MoveCursorDown"
        | GlobalHotkeyTargets.LeftClick          -> "LeftClick"
        | GlobalHotkeyTargets.MiddleClick        -> "MiddleClick"
        | GlobalHotkeyTargets.RightClick         -> "RightClick"
    member this.AsHotKeyDisplay() : System.Windows.FrameworkElement =
        let mkTxt(s) : System.Windows.FrameworkElement = 
            upcast new System.Windows.Controls.TextBox(Background=System.Windows.Media.Brushes.Black, Foreground=System.Windows.Media.Brushes.White, 
                                                    Text=s, IsReadOnly=true, IsHitTestVisible=false, HorizontalContentAlignment=HorizontalAlignment.Center, 
                                                    HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), Padding=Thickness(0.))
        let tab(level) : System.Windows.FrameworkElement =
            let labelChar = if level=9 then '9' elif level=10 then 'S' elif TrackerModel.IsHiddenDungeonNumbers() then (char(int 'A' - 1 + level)) else (char(int '0' + level))
            mkTxt(sprintf "Tab%c" labelChar)
        match this with
        | GlobalHotkeyTargets.ToggleMagicalSword -> upcast (Graphics.magical_sword_bmp |> Graphics.BMPtoImage)
        | GlobalHotkeyTargets.ToggleWoodSword    -> upcast (Graphics.brown_sword_bmp |> Graphics.BMPtoImage)
        | GlobalHotkeyTargets.ToggleBoomBook     -> upcast (Graphics.boom_book_bmp |> Graphics.BMPtoImage)
        | GlobalHotkeyTargets.ToggleBlueCandle   -> upcast (Graphics.blue_candle_bmp |> Graphics.BMPtoImage)
        | GlobalHotkeyTargets.ToggleWoodArrow    -> upcast (Graphics.wood_arrow_bmp |> Graphics.BMPtoImage)
        | GlobalHotkeyTargets.ToggleBlueRing     -> upcast (Graphics.blue_ring_bmp |> Graphics.BMPtoImage)
        | GlobalHotkeyTargets.ToggleBombs        -> upcast (Graphics.bomb_bmp |> Graphics.BMPtoImage)
        | GlobalHotkeyTargets.ToggleGannon       -> upcast (Graphics.ganon_bmp |> Graphics.BMPtoImage)
        | GlobalHotkeyTargets.ToggleZelda        -> upcast (Graphics.zelda_bmp |> Graphics.BMPtoImage)
        | GlobalHotkeyTargets.DungeonTab1        -> tab(1)
        | GlobalHotkeyTargets.DungeonTab2        -> tab(2)
        | GlobalHotkeyTargets.DungeonTab3        -> tab(3)
        | GlobalHotkeyTargets.DungeonTab4        -> tab(4)
        | GlobalHotkeyTargets.DungeonTab5        -> tab(5)
        | GlobalHotkeyTargets.DungeonTab6        -> tab(6)
        | GlobalHotkeyTargets.DungeonTab7        -> tab(7)
        | GlobalHotkeyTargets.DungeonTab8        -> tab(8)
        | GlobalHotkeyTargets.DungeonTab9        -> tab(9)
        | GlobalHotkeyTargets.DungeonTabS        -> tab(10)
        | GlobalHotkeyTargets.MoveCursorLeft     -> mkTxt("\u2190")
        | GlobalHotkeyTargets.MoveCursorRight    -> mkTxt("\u2192")
        | GlobalHotkeyTargets.MoveCursorUp       -> mkTxt("\u2191")
        | GlobalHotkeyTargets.MoveCursorDown     -> mkTxt("\u2193")
        | GlobalHotkeyTargets.LeftClick          -> mkTxt("LMB")
        | GlobalHotkeyTargets.MiddleClick        -> mkTxt("MMB")
        | GlobalHotkeyTargets.RightClick         -> mkTxt("RMB")
    static member All = [|
        GlobalHotkeyTargets.ToggleMagicalSword
        GlobalHotkeyTargets.ToggleWoodSword   
        GlobalHotkeyTargets.ToggleBoomBook    
        GlobalHotkeyTargets.ToggleBlueCandle  
        GlobalHotkeyTargets.ToggleWoodArrow   
        GlobalHotkeyTargets.ToggleBlueRing    
        GlobalHotkeyTargets.ToggleBombs       
        GlobalHotkeyTargets.ToggleGannon      
        GlobalHotkeyTargets.ToggleZelda       
        GlobalHotkeyTargets.DungeonTab1       
        GlobalHotkeyTargets.DungeonTab2       
        GlobalHotkeyTargets.DungeonTab3       
        GlobalHotkeyTargets.DungeonTab4       
        GlobalHotkeyTargets.DungeonTab5       
        GlobalHotkeyTargets.DungeonTab6       
        GlobalHotkeyTargets.DungeonTab7       
        GlobalHotkeyTargets.DungeonTab8       
        GlobalHotkeyTargets.DungeonTab9       
        GlobalHotkeyTargets.DungeonTabS       
        GlobalHotkeyTargets.MoveCursorLeft
        GlobalHotkeyTargets.MoveCursorRight
        GlobalHotkeyTargets.MoveCursorUp
        GlobalHotkeyTargets.MoveCursorDown
        GlobalHotkeyTargets.LeftClick
        GlobalHotkeyTargets.MiddleClick
        GlobalHotkeyTargets.RightClick
        |]


// Note to self; NumPad . and + are called Decimal and Add, not OemPeriod or OemPlus.  NumPad 'enter' is not supported, sadly.
let MakeDefaultHotKeyFile(filename:string) =
    let lines = ResizeArray()
    (sprintf """# %s HotKeys

# General form is 'SelectorName = key'
#  - key can be 0-9 or a-z, or alternately of the form \nnn where nnn is the numeric key code from
#        https://docs.microsoft.com/en-us/dotnet/api/system.windows.input.key#fields
#    (so for example \75 should be used as the key name for NumPad1)
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
    lines.Add("# OVERWORLD - these hotkey bindings take effect when mouse-hovering an overworld map tile")
    lines.Add("# Note that Level1-Level8 refer to dungeons A-H if using the 'Hide Dungeon Numbers' flag setting")
    for i = 0 to TrackerModel.dummyOverworldTiles.Length-1 do
        lines.Add("Overworld_" + TrackerModel.MapSquareChoiceDomainHelper.AsHotKeyName(i) + " = ")
    lines.Add("Overworld_Nothing = ")
    lines.Add("")
    lines.Add("# BLOCKERS - these hotkey bindings take effect when mouse-hovering a blocker box")
    for b in TrackerModel.DungeonBlocker.All do
        lines.Add(b.AsHotKeyName() + " = ")
    lines.Add("")
    lines.Add("# DUNGEON ROOMS - these hotkey bindings take effect when mouse-hovering a room in a dungeon")
    for x in AllDungeonRoomNames do
        lines.Add(x + " = ")
    lines.Add("")
    lines.Add("# CONTEXTUAL CHOICES - these hotkey bindings only take effect when the corresponding menus are on-screen")
    for x in TakeAnyNames do
        lines.Add(x + " = ")
    for x in TakeThisNames do
        lines.Add(x + " = ")
    lines.Add("")
    lines.Add("# GLOBAL - these hotkey bindings take effect anywhere, and cannot conflict with any other non-contextuals")
    for x in GlobalHotkeyTargets.All do
        lines.Add("Global_" + x.AsHotKeyName() + " = ")
    lines.Add("")
    System.IO.File.WriteAllLines(filename, lines)

let ParseHotKeyDataFile(filename:string) =
    let lines = System.IO.File.ReadAllLines(filename)
    let commentRegex = new System.Text.RegularExpressions.Regex("^#.*$", System.Text.RegularExpressions.RegexOptions.None)
    let emptyLineRegex = new System.Text.RegularExpressions.Regex("^\s*$", System.Text.RegularExpressions.RegexOptions.None)
    let dataRegex = new System.Text.RegularExpressions.Regex("^\s*(\w+)\s*=\s*(\w)?\s*$", System.Text.RegularExpressions.RegexOptions.None)
    let data2Regex = new System.Text.RegularExpressions.Regex("""^\s*(\w+)\s*=\s*\\(\d+)\s*$""", System.Text.RegularExpressions.RegexOptions.None)
    let data = ResizeArray()
    let mutable lineNumber = 1
    for line in lines do
        if commentRegex.IsMatch(line) || emptyLineRegex.IsMatch(line) then
            () // skip
        else
            let m = dataRegex.Match(line)
            if not m.Success then
                let m = data2Regex.Match(line)
                if not m.Success then
                    raise <| new UserError(sprintf "Error parsing '%s', line %d" filename lineNumber)
                else
                    let name = m.Groups.[1].Value
                    let value = if m.Groups.Count > 2 then Some(enum<Input.Key>(int m.Groups.[2].Value)) else None
                    data.Add(name, value, (lineNumber, filename))

            else
                // Groups.[0] is the whole match
                let name = m.Groups.[1].Value
                let value = if m.Groups.Count > 2 && m.Groups.[2].Value.Length > 0 then Some(convertAlpha_NumToKey(m.Groups.[2].Value.[0])) else None
                data.Add(name, value, (lineNumber, filename))
        lineNumber <- lineNumber + 1
    data

////////////////////////////////////////////////////////////

let keyUniverse = [| for i = 1 to 172 do yield enum<Input.Key>(i) |]   // it appears Input.Key does not range outside 1 to 172
let PrettyKey(key:Input.Key) =
    let s = key.ToString()
    if System.Text.RegularExpressions.Regex.IsMatch(s, "D\d") then  // the 'name' of 0-9 across top keyboard is D0-D9
        sprintf "%c" s.[1]
    else
        s
type HotKeyProcessor<'v when 'v : equality>(contextName) =
    let table = new System.Collections.Generic.Dictionary<Input.Key,'v>()
    let stateToKeys = new System.Collections.Generic.Dictionary<_,_>()  // caching
    member this.ContextName = contextName
    member this.TryGetValue(k) = 
        match table.TryGetValue(k) with
        | true, v -> Some(v)
        | _ -> None
    member this.ContainsKey(k) = table.ContainsKey(k)
    member this.Keys() = table.Keys |> Seq.toArray
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
                match this.TryGetValue(k) with
                | Some x -> if x = state then r.Add(k)
                | None -> ()
            stateToKeys.Add(state, r)
            r
        else
            stateToKeys.[state]
    member this.AsPrettyHotKeyOpt(state) =
        let keys = this.StateToKeys(state)
        if keys.Count > 0 then
            Some(sprintf "HotKey = %s" (PrettyKey(keys.[0])))
        else
            None
    member this.AppendHotKeyToDescription(desc, state) =
        let keys = this.StateToKeys(state)
        if keys.Count > 0 then
            sprintf "%s\nHotKey = %s" desc (PrettyKey(keys.[0]))
        else
            desc

let ItemHotKeyProcessor = new HotKeyProcessor<int>("Item")
let OverworldHotKeyProcessor = new HotKeyProcessor<int>("Overworld")
let BlockerHotKeyProcessor = new HotKeyProcessor<TrackerModel.DungeonBlocker>("Blocker")
let DungeonRoomHotKeyProcessor = new HotKeyProcessor<Choice<DungeonRoomState.RoomType,DungeonRoomState.MonsterDetail,DungeonRoomState.FloorDropDetail> >("DungeonRoom")
let TakeAnyHotKeyProcessor = new HotKeyProcessor<int>("TakeAny")
let TakeThisHotKeyProcessor = new HotKeyProcessor<int>("TakeThis")
let GlobalHotKeyProcessor = new HotKeyProcessor<GlobalHotkeyTargets>("Global")

let HotKeyFilename = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "HotKeys.txt")

let PopulateHotKeyTables() =
    let filename = HotKeyFilename
    if not(System.IO.File.Exists(filename)) then
        MakeDefaultHotKeyFile(filename)
    let data = ParseHotKeyDataFile(filename)
    for name, chOpt, (lineNumber, filename) in data do
        let Add(hkp:HotKeyProcessor<_>, keyOpt, x) =
            match keyOpt with
            | None -> ()  // Foo=    means Foo is not bound to a hotkey on this line
            | Some key ->
                if not(hkp.TryAdd(key,x)) then
                    raise <| new UserError(sprintf "Keyboard key '%s' given multiple meanings for '%s' context; second occurrence at line %d of '%s'" (PrettyKey key) hkp.ContextName lineNumber filename)
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
                for i = 0 to 3 do do
                    if name = TakeAnyNames.[i] then
                        Add(TakeAnyHotKeyProcessor, chOpt, i)
                        found <- true
            if not found then
                for i = 0 to 2 do do
                    if name = TakeThisNames.[i] then
                        Add(TakeThisHotKeyProcessor, chOpt, i)
                        found <- true
            if not found then
                for x in GlobalHotkeyTargets.All do
                    if name = "Global_"+ x.AsHotKeyName() then
                        Add(GlobalHotKeyProcessor, chOpt, x)
                        found <- true
            if not found then
                raise <| new UserError(sprintf "Bad name '%s' specified in '%s', line %d" name filename lineNumber)
    // global conflict check
    for k in GlobalHotKeyProcessor.Keys() do
        let error(kind) =
            let msg = sprintf "Global hotkey '%s' was bound as '%s', but that key was also bound to %s entry" (PrettyKey k) ("Global_"+GlobalHotKeyProcessor.TryGetValue(k).Value.AsHotKeyName()) kind
            raise <| new UserError(msg)
        if ItemHotKeyProcessor.ContainsKey(k) then
            error "an 'Item_...'"
        if OverworldHotKeyProcessor.ContainsKey(k) then
            error "an 'Overworld_...'"
        if BlockerHotKeyProcessor.ContainsKey(k) then
            error "a 'Blocker_...'"
        // don't bother with contextual hotkey conflicts, they're ok
        if DungeonRoomHotKeyProcessor.ContainsKey(k) then
            error "a 'DungeonRoom_...'"
