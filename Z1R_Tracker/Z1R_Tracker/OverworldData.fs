module OverworldData

let VersionString = "1.3.0h"
let ProgramNameString = sprintf "Z-Tracker v%s" VersionString
let Website = "https://github.com/brianmcn/Zelda1RandoTools"
let AboutBody = sprintf "%s by Dr. Brian Lorgon111\n\nLearn more at\n%s\n" ProgramNameString Website



let hintMeanings = [|
    "Aquamentus Awaits", "Level 1"
    "Dodongo Dwells", "Level 2"
    "Manhandla Threatens", "Level 3"
    "Gleeok Lurks", "Level 4"
    "Digdogger Gazes", "Level 5"
    "Gohma Creeps", "Level 6"
    "Goriya Grumbles", "Level 7"
    "Gleeok Returns", "Level 8"
    "entrance to death", "Level 9"
    "(npc) has (item) at", "White Sword item"
    "Meet (npc) at", "Magical Sword"
    |]

[<RequireQualifiedAccess>]
type OWQuest = 
    | FIRST
    | SECOND
    | MIXED_FIRST
    | MIXED_SECOND
    | BLANK
    member this.IsFirstQuestOW = match this with |FIRST|MIXED_FIRST -> true | _ -> false
    member this.AsInt() =
        match this with
        | FIRST -> 0
        | SECOND -> 1
        | MIXED_FIRST -> 2
        | MIXED_SECOND -> 3
        | BLANK -> 4
    member this.FromInt(x) =
        if x=0 then FIRST
        elif x=1 then SECOND
        elif x=2 then MIXED_FIRST
        elif x=3 then MIXED_SECOND
        elif x=4 then BLANK
        else failwith "bad OWQuest.FromInt value"

let owMapZone = [|
    "MMMMMMMMMMLHHHCC"
    "MMMMMMMRRRRHHHCC"
    "GGGGGGLRDDDDDCCC"
    "GGGGLLLLLLDDFFCC"
    "GGLLLLLLLDDDFFFC"
    "GWWWLLLSSLLFFFFC"
    "GWWWWRSSSLLFFFFC"
    "WWWWWRSSSSSCCCCC"
    |]

module PrivateInternals =
    let owMapSquaresArmos = [|
        "................"
        "............X..."
        "....X..........."
        "....X........X.."
        "..............X."
        "................"
        "................"
        "................"
        |]

    let owMapSquaresRaftable = [|
        "................"
        "................"
        "...............X"
        "................"
        ".....X.........."
        "................"
        "................"
        "................"
        |]

    let owMapSquaresFirstQuestBombable = [|
        ".X.X.X.X.....X.."
        "X.XXX.X.......X."
        "......XX....XX.."
        "...X............"
        "................"
        "................"
        ".......X........"
        ".X....X....XXX.."
        |]

    let owMapSquaresSecondQuestBombable = [|
        "XXXX...X.....X.."
        "X.XXXXX.XX....X."
        "......X......X.."
        "...X............"
        "................"
        "................"
        "................"
        "......X.....XX.."
        |]

    let owMapSquaresFirstQuestBurnable = [|
            "................"
            "................"
            "........X......."
            "................"
            "......XXX..X.X.."
            ".X....X....X...."
            "..XX....X.XX.X.."
            "........X......."
        |]

    let owMapSquaresSecondQuestBurnable = [|
            "................"
            "................"
            "........X......."
            "................"
            "......X.X..X.X.."
            ".X.X..X....X...."
            "...X....X.X.X..."
            "........X......."
        |]

    let owMapSquaresFirstQuestPowerBraceletable = [|
        "................"
        ".............X.."
        "...X............"
        "................"
        ".........X......"
        "................"
        "................"
        ".........X......"
        |]

    let owMapSquaresSecondQuestPowerBraceletable = [|
        ".........X......"
        ".X.........X.X.."
        "...X............"
        "................"
        ".........X......"
        "................"
        "................"
        ".........X......"
        |]

    let owMapSquaresSecondQuestLadderable = [|
        "................"
        "........XX......"
        "................"
        "................"
        "................"
        "................"
        "................"
        "................"
        |]

    let owMapSquaresFirstQuestWhistleable = [|
        "................"
        "................"
        "................"
        "................"
        "..X............."
        "................"
        "................"
        "................"
        |]

    let owMapSquaresSecondQuestWhistleable = [|
        "......X........."
        "................"
        ".........X.X...."
        "X.........X.X..."
        "................"
        "........X......."
        "X.............X."
        "..X............."
        |]

    let owMapSquaresFirstQuestGravePushable = [|
        "................"
        "................"
        ".X.............."
        "................"
        "................"
        "................"
        "................"
        "................"
        |]

    let owMapSquaresSecondQuestGravePushable = [|
        "................"
        "................"
        "X..............."
        "................"
        "................"
        "................"
        "................"
        "................"
        |]

    let owMapSquaresFirstQuestAlwaysEmpty = [|
        "X.X...X.XX......"
        ".X...X.XXX.X...."
        "X........XXX..X."
        "XXX..XX.XXXX..XX"
        "XX.X........X..X"
        "X.XXXX.XXXX.XX.X"
        "XX...X...X..X.X."
        "..XX......X...XX"
        |]

    let owMapSquaresSecondQuestAlwaysEmpty = [|
        ".....X..X..X...."
        ".......X........"
        ".X.....X..X.X.X."
        ".XX..XX.XX.X..XX"
        "XXXX...X....X..X"
        "X.X.XX.X.XX.XX.X"
        ".XX..X.X.X.X.X.."
        ".X.X......XX..XX"
        |]

    let owMapSquaresMixedQuestSometimesEmpty = [|
        "X.X..XX..X......"
        ".X...X..XX.X...."
        "XX.....X.X.XX..."
        "X.........X....."
        ".......X........"
        "...X....X......."
        "X.X....X...XXXX."
        ".XX........X...."
        |]

    let owMapSquaresMixedQuestAlwaysEmpty = [|
        for i = 0 to 7 do
            let mutable s = ""
            for j = 0 to 15 do
                if owMapSquaresFirstQuestAlwaysEmpty.[i].[j] = 'X' && owMapSquaresSecondQuestAlwaysEmpty.[i].[j] = 'X' then
                    s <- s + "X"
                else
                    s <- s + "."
            yield s
        |]
// end PrivateInternals

let owMapSquaresSecondQuestOnly = [|
    for i = 0 to 7 do
        let mutable s = ""
        for j = 0 to 15 do
            if PrivateInternals.owMapSquaresFirstQuestAlwaysEmpty.[i].[j] = 'X' && PrivateInternals.owMapSquaresSecondQuestAlwaysEmpty.[i].[j] <> 'X' then
                s <- s + "X"
            else
                s <- s + "."
        yield s
    |]

let owMapSquaresFirstQuestOnly = [|
    for i = 0 to 7 do
        let mutable s = ""
        for j = 0 to 15 do
            if PrivateInternals.owMapSquaresFirstQuestAlwaysEmpty.[i].[j] <> 'X' && PrivateInternals.owMapSquaresSecondQuestAlwaysEmpty.[i].[j] = 'X' then
                s <- s + "X"
            else
                s <- s + "."
        yield s
    |]

type OverworldInstance(quest) =
    member this.Quest = quest
    member this.AlwaysEmpty(x,y) =
        match quest with
        | OWQuest.FIRST ->        PrivateInternals.owMapSquaresFirstQuestAlwaysEmpty.[y].Chars(x) = 'X'
        | OWQuest.SECOND ->       PrivateInternals.owMapSquaresSecondQuestAlwaysEmpty.[y].Chars(x) = 'X'
        | OWQuest.MIXED_FIRST ->  PrivateInternals.owMapSquaresMixedQuestAlwaysEmpty.[y].Chars(x) = 'X'
        | OWQuest.MIXED_SECOND -> PrivateInternals.owMapSquaresMixedQuestAlwaysEmpty.[y].Chars(x) = 'X'
        | OWQuest.BLANK ->        false
    member this.Ladderable(x,y) =
        match quest with
        | OWQuest.FIRST ->        false
        | OWQuest.BLANK ->        false
        | _ ->            PrivateInternals.owMapSquaresSecondQuestLadderable.[y].Chars(x) = 'X'
    member this.HasArmos(x,y) =
        match quest with
        | OWQuest.BLANK -> false
        | _ -> PrivateInternals.owMapSquaresArmos.[y].Chars(x) = 'X'
    member this.Raftable(x,y) =
        match quest with
        | OWQuest.BLANK -> false
        | _ -> PrivateInternals.owMapSquaresRaftable.[y].Chars(x) = 'X'
    member this.Whistleable(x,y) =
        match quest with
        | OWQuest.FIRST ->        PrivateInternals.owMapSquaresFirstQuestWhistleable.[y].Chars(x) = 'X'
        | OWQuest.SECOND ->       PrivateInternals.owMapSquaresSecondQuestWhistleable.[y].Chars(x) = 'X'
        | OWQuest.MIXED_FIRST ->  (PrivateInternals.owMapSquaresFirstQuestWhistleable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestWhistleable.[y].Chars(x) = 'X')
        | OWQuest.MIXED_SECOND -> (PrivateInternals.owMapSquaresFirstQuestWhistleable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestWhistleable.[y].Chars(x) = 'X')
        | OWQuest.BLANK ->        false
    member this.PowerBraceletable(x,y) =
        match quest with
        | OWQuest.FIRST ->        PrivateInternals.owMapSquaresFirstQuestPowerBraceletable.[y].Chars(x) = 'X'
        | OWQuest.SECOND ->       PrivateInternals.owMapSquaresSecondQuestPowerBraceletable.[y].Chars(x) = 'X'
        | OWQuest.MIXED_FIRST ->  (PrivateInternals.owMapSquaresFirstQuestPowerBraceletable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestPowerBraceletable.[y].Chars(x) = 'X')
        | OWQuest.MIXED_SECOND -> (PrivateInternals.owMapSquaresFirstQuestPowerBraceletable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestPowerBraceletable.[y].Chars(x) = 'X')
        | OWQuest.BLANK ->        false
    member this.GravePushable(x,y) =
        match quest with
        | OWQuest.FIRST ->        PrivateInternals.owMapSquaresFirstQuestGravePushable.[y].Chars(x) = 'X'
        | OWQuest.SECOND ->       PrivateInternals.owMapSquaresSecondQuestGravePushable.[y].Chars(x) = 'X'
        | OWQuest.MIXED_FIRST ->  (PrivateInternals.owMapSquaresFirstQuestGravePushable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestGravePushable.[y].Chars(x) = 'X')
        | OWQuest.MIXED_SECOND -> (PrivateInternals.owMapSquaresFirstQuestGravePushable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestGravePushable.[y].Chars(x) = 'X')
        | OWQuest.BLANK ->        false
    member this.Burnable(x,y) =
        match quest with
        | OWQuest.FIRST ->        PrivateInternals.owMapSquaresFirstQuestBurnable.[y].Chars(x) = 'X'
        | OWQuest.SECOND ->       PrivateInternals.owMapSquaresSecondQuestBurnable.[y].Chars(x) = 'X'
        | OWQuest.MIXED_FIRST ->  (PrivateInternals.owMapSquaresFirstQuestBurnable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestBurnable.[y].Chars(x) = 'X')
        | OWQuest.MIXED_SECOND -> (PrivateInternals.owMapSquaresFirstQuestBurnable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestBurnable.[y].Chars(x) = 'X')
        | OWQuest.BLANK ->        false
    member this.Bombable(x,y) =
        match quest with
        | OWQuest.FIRST ->        PrivateInternals.owMapSquaresFirstQuestBombable.[y].Chars(x) = 'X'
        | OWQuest.SECOND ->       PrivateInternals.owMapSquaresSecondQuestBombable.[y].Chars(x) = 'X'
        | OWQuest.MIXED_FIRST ->  (PrivateInternals.owMapSquaresFirstQuestBombable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestBombable.[y].Chars(x) = 'X')
        | OWQuest.MIXED_SECOND -> (PrivateInternals.owMapSquaresFirstQuestBombable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestBombable.[y].Chars(x) = 'X')
        | OWQuest.BLANK ->        false
    member this.Nothingable(x,y) = not(this.Bombable(x,y) || this.Burnable(x,y) || this.Ladderable(x,y) || this.PowerBraceletable(x,y) || this.Raftable(x,y) || this.Whistleable(x,y))
    member this.SometimesEmpty(x,y) =
        match quest with
        | OWQuest.FIRST ->        false
        | OWQuest.SECOND ->       false
        | OWQuest.MIXED_FIRST ->  PrivateInternals.owMapSquaresMixedQuestSometimesEmpty.[y].Chars(x) = 'X'
        | OWQuest.MIXED_SECOND -> PrivateInternals.owMapSquaresMixedQuestSometimesEmpty.[y].Chars(x) = 'X'
        | OWQuest.BLANK ->        true

// yellow brown rocks in the top half of '\'
let overworldSouthEastUpperRockTiles = [|
    1,  0,  2,  2
    1,  0,  3,  4
    1,  0,  5,  4
    1,  0, 11,  1
    1,  0, 13,  1
    1,  0, 14,  3
    1,  0, 15,  4
    1,  1, 14,  2
    1,  7,  4,  1
    1,  7, 12,  1
    1,  7, 13,  2
    1,  7, 14,  3
    2,  0, 11,  2
    2,  0, 12,  3
    2,  0, 13,  4
    2,  3,  0,  3
    2,  3,  3,  2
    2,  3,  4,  3
    2,  3,  5,  4
    2,  3, 10,  1
    2,  3, 11,  2
    2,  3, 12,  3
    2,  3, 13,  4
    2,  4, 14,  2
    3,  0,  2,  1
    3,  0,  3,  2
    3,  0,  4,  3
    3,  0,  5,  4
    3,  1,  3,  2
    3,  1,  4,  3
    3,  1,  5,  4
    3,  2,  7,  2
    3,  4, 14,  2
    4,  0,  5,  4
    4,  0,  6,  5
    4,  0, 15,  2
    4,  1,  2,  1
    4,  1, 14,  2
    4,  1, 15,  4
    4,  2,  3,  2
    4,  2,  7,  1
    4,  2, 13,  1
    4,  3, 14,  3
    5,  0,  1,  2
    5,  0,  3,  6
    5,  0,  9,  6
    5,  0, 14,  2
    5,  0, 15,  4
    5,  1,  1,  5
    5,  1,  5,  5
    5,  1,  9,  1
    5,  1, 10,  2
    5,  1, 14,  2
    5,  2,  7,  1
    5,  2, 14,  2
    5,  3,  7,  3
    5,  3, 12,  3
    6,  0,  1,  4
    6,  0,  5,  2
    6,  0,  8,  1
    6,  0,  9,  2
    6,  0, 10,  4
    6,  0, 12,  4
    6,  0, 14,  4
    6,  1,  0,  2
    6,  1,  3,  1
    6,  1,  4,  2
    6,  1,  8,  4
    6,  1, 11,  4
    6,  2,  5,  2
    6,  2,  6,  3
    6,  2, 10,  3
    6,  2, 13,  3
    6,  3,  0,  3
    6,  3,  4,  3
    6,  3,  7,  3
    7,  0,  8,  3
    7,  0,  9,  4
    7,  0, 13,  4
    7,  1,  1,  1
    7,  1,  2,  2
    7,  1,  3,  4
    7,  2,  0,  3
    7,  2,  3,  3
    7,  2, 11,  3
    8,  0,  1,  4
    8,  0,  5,  2
    8,  0,  8,  1
    8,  0,  9,  2
    8,  0, 10,  4
    8,  0, 12,  4
    8,  0, 14,  4
    9,  0,  2,  2
    9,  0, 13,  2
    9,  0, 14,  3
    9,  3, 14,  2
    9,  7,  0,  1
    9,  7,  1,  2
    9,  7,  2,  3
    9,  7,  3,  4
    9,  7,  4,  5
    9,  7,  7,  1
    9,  7, 12,  2
    9,  7, 13,  4
    10,  0, 14,  2
    10,  7,  1,  4
    10,  7,  5,  2
    10,  7,  8,  1
    10,  7,  9,  2
    10,  7, 10,  4
    10,  7, 12,  4
    10,  7, 14,  4
    11,  0, 13,  1
    11,  0, 14,  2
    11,  1, 13,  2
    11,  1, 14,  4
    11,  7,  2,  4
    12,  0, 11,  1
    12,  0, 12,  3
    12,  2,  5,  6
    12,  2, 12,  4
    13,  0,  2,  2
    13,  0,  3,  3
    13,  0,  7,  1
    13,  0, 10,  2
    13,  0, 11,  3
    13,  0, 12,  4
    13,  0, 14,  5
    13,  1,  4,  2
    13,  1, 11,  3
    14,  7,  2,  3
    14,  7,  5,  3
    14,  7, 10,  3
    14,  7, 13,  3
    14,  7, 15,  3
    15,  6,  0,  1
    15,  7,  1,  3
    // green rocks
    4,  6,  0,  3
    4,  6,  5,  1
    4,  6, 11,  1
    4,  6, 14,  1
    4,  7, 14,  2
    5,  6, 14,  1
    5,  7,  7,  1
    6,  6, 14,  2
    6,  6, 15,  3
    6,  7, 14,  3
    6,  7, 15,  4
    7,  6,  9,  2
    7,  6, 14,  2
    7,  6, 15,  3
    7,  7,  9,  4
    10,  4,  0,  3
    10,  4,  4,  1
    10,  4, 13,  2
    10,  4, 14,  3
    12,  3, 13,  1
    12,  3, 14,  2
    12,  4,  3,  1
    12,  4,  5,  1
    12,  4,  6,  4
    12,  4, 12,  1
    12,  4, 13,  4
    14,  5,  6,  1
    14,  5,  8,  2
    14,  5,  9,  3
    14,  5, 14,  2
    |]

// yellow brown rocks in the top half of '/'
let overworldNorthEastUpperRockTiles = [|
    0,  1,  1,  3
    0,  1,  2,  2
    0,  7,  1,  4
    0,  7,  2,  3
    0,  7,  3,  2
    1,  0,  1,  3
    1,  0,  4,  4
    1,  0,  8,  4
    1,  0, 10,  1
    1,  0, 12,  1
    1,  7,  3,  1
    1,  7,  9,  1
    1,  7, 15,  3
    2,  0,  9,  4
    2,  0, 10,  2
    2,  1,  1,  5
    2,  1,  5,  4
    2,  1,  6,  3
    2,  1,  7,  2
    2,  3,  1,  3
    2,  3,  2,  2
    2,  3,  8,  4
    2,  3,  9,  1
    2,  4,  1,  2
    3,  0,  0,  4
    3,  0,  1,  1
    3,  1, 12,  4
    3,  1, 13,  3
    3,  1, 14,  2
    3,  2,  1,  2
    3,  2, 10,  2
    3,  3,  1,  4
    3,  4,  1,  2
    4,  0,  1,  5
    4,  0,  4,  4
    4,  0,  7,  5
    4,  0,  8,  4
    4,  0,  9,  3
    4,  0, 10,  2
    4,  1,  1,  1
    4,  2,  4,  2
    4,  2,  6,  1
    4,  2, 12,  1
    4,  3,  1,  2
    4,  4,  1,  2
    5,  0,  0,  2
    5,  0,  2,  2
    5,  0,  6,  6
    5,  0, 12,  6
    5,  1,  2,  5
    5,  1,  6,  5
    5,  1,  7,  4
    5,  1,  8,  1
    5,  1, 11,  2
    5,  1, 15,  2
    5,  2,  6,  1
    5,  3,  6,  3
    5,  3, 11,  3
    5,  3, 15,  3
    6,  0,  0,  4
    6,  0,  2,  4
    6,  0,  3,  3
    6,  0,  4,  2
    6,  0,  6,  2
    6,  0,  7,  1
    6,  0, 11,  4
    6,  0, 13,  4
    6,  1,  1,  2
    6,  1,  2,  1
    6,  1,  5,  2
    6,  1, 10,  4
    6,  1, 12,  4
    6,  1, 13,  3
    6,  1, 14,  2
    6,  2,  1,  2
    6,  2,  7,  3
    6,  2,  8,  2
    6,  2, 12,  3
    6,  2, 15,  3
    6,  3,  1,  3
    6,  3,  6,  3
    6,  3,  8,  3
    7,  0,  6,  4
    7,  0,  7,  3
    7,  0, 12,  4
    7,  1,  0,  1
    7,  2,  2,  3
    7,  2,  5,  3
    7,  2, 13,  3
    8,  0,  0,  4
    8,  0,  2,  4
    8,  0,  3,  3
    8,  0,  4,  2
    8,  0,  6,  2
    8,  0,  7,  1
    8,  0, 11,  4
    8,  0, 13,  4
    9,  0,  0,  4
    9,  0,  1,  2
    9,  0,  3,  2
    9,  3,  1,  2
    9,  7,  5,  5
    9,  7,  6,  1
    10,  0,  1,  2
    10,  7,  0,  4
    10,  7,  2,  4
    10,  7,  3,  3
    10,  7,  4,  2
    10,  7,  6,  2
    10,  7,  7,  1
    10,  7, 11,  4
    10,  7, 13,  4
    11,  0,  1,  3
    11,  0,  2,  2
    11,  0, 12,  1
    11,  1,  1,  4
    11,  1,  2,  2
    11,  7,  1,  4
    11,  7,  4,  4
    11,  7,  5,  3
    11,  7,  6,  2
    12,  0,  6,  4
    12,  0,  7,  2
    12,  0, 10,  1
    12,  0, 13,  3
    12,  0, 14,  2
    12,  1,  0,  4
    12,  1,  1,  2
    12,  2, 10,  6
    13,  0,  4,  3
    13,  0,  5,  2
    13,  0,  6,  1
    13,  1,  6,  5
    13,  1, 10,  3
    13,  2,  3,  4
    13,  2,  6,  1
    14,  1,  9,  2
    14,  7,  4,  3
    14,  7,  9,  3
    14,  7, 12,  3
    14,  7, 14,  3
    15,  4,  2,  5
    15,  4,  3,  4
    15,  4,  4,  1
    15,  5,  2,  5
    15,  5,  3,  4
    15,  5,  4,  1
    15,  6,  4,  1
    15,  7,  0,  3
    15,  7,  4,  3
    // green rocks
    4,  6,  1,  3
    4,  6,  2,  2
    4,  6,  8,  1
    4,  6, 12,  1
    4,  7,  0,  3
    4,  7,  1,  2
    5,  6,  1,  1
    5,  7,  1,  2
    5,  7,  3,  1
    6,  7, 11,  1
    7,  6,  0,  3
    7,  6,  1,  2
    7,  6, 10,  2
    7,  7,  1,  4
    7,  7,  2,  3
    7,  7,  3,  2
    7,  7,  6,  1
    8,  6,  0,  3
    8,  6,  1,  2
    8,  7,  0,  4
    8,  7,  1,  3
    10,  4,  1,  3
    10,  4,  2,  2
    10,  4,  3,  1
    10,  4, 15,  3
    12,  3,  1,  3
    12,  3,  2,  2
    12,  3, 12,  1
    12,  4,  1,  3
    12,  4,  2,  1
    12,  4,  4,  1
    12,  4, 10,  4
    12,  4, 11,  1
    12,  4, 15,  4
    14,  5,  0,  3
    14,  5,  1,  2
    14,  5,  5,  1
    14,  5, 10,  3
    14,  5, 11,  2
    |]

// yellow brown rocks in the bottom half of '/'
let overworldNorthEastLowerRockTiles = [|
    1,  0, 13,  9
    1,  0, 14,  7
    1,  0, 15,  6
    1,  1, 14,  8
    1,  7, 13,  8
    1,  7, 14,  7
    2,  3,  0,  7
    2,  3, 13,  6
    2,  4, 14,  8
    3,  0,  3,  8
    3,  0,  4,  7
    3,  0,  5,  6
    3,  2,  7,  8
    3,  4, 14,  8
    4,  0,  6,  8
    4,  0, 15,  8
    4,  1,  2,  9
    4,  2,  3,  8
    4,  2,  7,  9
    4,  2, 13,  9
    5,  0,  1,  8
    5,  0,  3,  4
    5,  0,  4,  3
    5,  0,  9,  4
    5,  0, 10,  3
    5,  0, 14,  8
    5,  0, 15,  6
    5,  1,  1,  8
    5,  1,  5,  8
    5,  1,  9,  9
    5,  1, 10,  8
    5,  1, 14,  8
    5,  2,  7,  9
    5,  2, 14,  8
    6,  0,  1,  6
    6,  0,  5,  8
    6,  0,  8,  9
    6,  0,  9,  8
    6,  0, 10,  6
    6,  0, 12,  6
    6,  0, 14,  6
    6,  1,  0,  8
    6,  1,  3,  9
    6,  1,  4,  8
    6,  1, 11,  6
    6,  2,  5,  8
    6,  2,  6,  7
    7,  0,  8,  7
    7,  0,  9,  6
    7,  0, 13,  6
    8,  0,  1,  6
    8,  0,  5,  8
    8,  0,  8,  9
    8,  0,  9,  8
    8,  0, 10,  6
    8,  0, 12,  6
    8,  0, 14,  6
    9,  0,  2,  8
    9,  0, 13,  8
    9,  0, 14,  7
    9,  3, 14,  8
    9,  7,  0,  9
    9,  7,  4,  8
    9,  7,  7,  9
    9,  7, 12,  8
    9,  7, 13,  6
    10,  0, 14,  8
    10,  7,  1,  6
    10,  7,  5,  8
    10,  7,  8,  9
    10,  7,  9,  8
    10,  7, 10,  6
    10,  7, 12,  6
    10,  7, 14,  6
    11,  0, 14,  8
    11,  1, 13,  8
    11,  1, 14,  6
    11,  7,  2,  6
    12,  0, 11,  9
    12,  0, 12,  7
    12,  2,  5,  4
    12,  2,  6,  3
    13,  0,  2,  8
    13,  0,  3,  7
    13,  0, 14,  8
    13,  1,  4,  8
    15,  6,  0,  9
    // green rocks
    4,  6,  0,  7
    4,  7, 14,  8
    5,  7,  7,  9
    6,  6, 14,  8
    6,  6, 15,  7
    6,  7, 14,  7
    6,  7, 15,  6
    7,  6,  9,  8
    7,  6, 14,  8
    7,  6, 15,  7
    10,  4,  0,  7
    10,  4,  4,  9
    10,  4, 13,  8
    10,  4, 14,  7
    12,  3, 14,  8
    12,  4,  3,  9
    12,  4,  5,  9
    12,  4, 12,  9
    12,  4, 13,  6
    14,  5,  6,  9
    14,  5,  8,  8
    14,  5,  9,  7
    14,  5, 14,  8
    |]

// yellow brown rocks in the bottom half of '\'
let overworldSouthEastLowerRockTiles = [|
    0,  1,  1,  7
    0,  1,  2,  8
    0,  7,  1,  6
    0,  7,  2,  7
    0,  7,  3,  8
    1,  0,  1,  7
    1,  0, 12,  9
    1,  7, 15,  7
    2,  0,  9,  6
    2,  0, 10,  8
    2,  1,  1,  8
    2,  3,  1,  7
    2,  3,  2,  8
    2,  4,  1,  8
    3,  2,  1,  8
    3,  2, 10,  8
    3,  3,  1,  6
    3,  4,  1,  8
    4,  0,  1,  8
    4,  0,  7,  8
    4,  1,  1,  9
    4,  2,  4,  8
    4,  2,  6,  9
    4,  2, 12,  9
    4,  3,  1,  8
    4,  4,  1,  8
    5,  0,  0,  8
    5,  0,  2,  8
    5,  0,  5,  3
    5,  0,  6,  4
    5,  0, 11,  3
    5,  0, 12,  4
    5,  1,  2,  8
    5,  1,  6,  8
    5,  1,  8,  9
    5,  1, 11,  8
    5,  1, 15,  8
    5,  2,  6,  9
    6,  0,  0,  6
    6,  0,  2,  6
    6,  0,  3,  7
    6,  0,  4,  8
    6,  0,  6,  8
    6,  0,  7,  9
    6,  0, 11,  6
    6,  0, 13,  6
    6,  1,  1,  8
    6,  1,  2,  9
    6,  1,  5,  8
    6,  1, 12,  6
    6,  1, 13,  7
    6,  1, 14,  8
    6,  2,  1,  8
    6,  2,  7,  7
    6,  2,  8,  8
    7,  0,  6,  6
    7,  0,  7,  7
    7,  0, 12,  6
    8,  0,  0,  6
    8,  0,  2,  6
    8,  0,  3,  7
    8,  0,  4,  8
    8,  0,  6,  8
    8,  0,  7,  9
    8,  0, 11,  6
    8,  0, 13,  6
    9,  0,  0,  6
    9,  0,  1,  8
    9,  0,  3,  8
    9,  3,  1,  8
    9,  7,  5,  8
    9,  7,  6,  9
    10,  0,  1,  8
    10,  7,  0,  6
    10,  7,  2,  6
    10,  7,  3,  7
    10,  7,  4,  8
    10,  7,  6,  8
    10,  7,  7,  9
    10,  7, 11,  6
    10,  7, 13,  6
    11,  0,  1,  7
    11,  0,  2,  8
    11,  1,  1,  6
    11,  1,  2,  8
    11,  7,  1,  6
    11,  7,  4,  6
    11,  7,  5,  7
    11,  7,  6,  8
    12,  0,  6,  6
    12,  0,  7,  8
    12,  0, 10,  9
    12,  0, 13,  7
    12,  0, 14,  8
    12,  1,  0,  6
    12,  1,  1,  8
    12,  2,  9,  3
    12,  2, 10,  4
    13,  0,  4,  7
    13,  0,  5,  8
    13,  1,  6,  8
    13,  2,  6,  9
    14,  3,  2,  8
    15,  4,  2,  8
    15,  4,  4,  9
    15,  5,  2,  8
    15,  5,  4,  9
    15,  6,  4,  9
    // green rocks
    4,  6,  1,  7
    4,  6,  2,  8
    4,  7,  0,  7
    4,  7,  1,  8
    5,  6,  3,  9
    5,  7,  1,  8
    5,  7,  4,  9
    6,  6, 11,  9
    7,  6,  0,  7
    7,  6,  1,  8
    7,  6, 10,  8
    7,  7,  1,  6
    8,  6,  0,  7
    8,  6,  1,  8
    8,  7,  0,  6
    8,  7,  1,  7
    10,  4,  1,  7
    10,  4,  2,  8
    10,  4,  3,  9
    10,  4, 15,  7
    12,  3,  1,  7
    12,  3,  2,  8
    12,  4,  1,  7
    12,  4,  2,  9
    12,  4,  4,  9
    12,  4, 11,  9
    12,  4, 15,  6
    14,  5,  0,  7
    14,  5,  1,  8
    14,  5,  5,  9
    14,  5, 10,  7
    14,  5, 11,  8
    |]

let owNEupperRock = 
    let a = Array2D.init 16 8 (fun _ _ -> Array2D.zeroCreate 16 11)
    for tx,ty,x,y in overworldNorthEastUpperRockTiles do
        a.[tx,ty].[x,y] <- true
    a

let owSEupperRock = 
    let a = Array2D.init 16 8 (fun _ _ -> Array2D.zeroCreate 16 11)
    for tx,ty,x,y in overworldSouthEastUpperRockTiles do
        a.[tx,ty].[x,y] <- true
    a

let owNElowerRock = 
    let a = Array2D.init 16 8 (fun _ _ -> Array2D.zeroCreate 16 11)
    for tx,ty,x,y in overworldNorthEastLowerRockTiles do
        a.[tx,ty].[x,y] <- true
    a
    
let owSElowerRock = 
    let a = Array2D.init 16 8 (fun _ _ -> Array2D.zeroCreate 16 11)
    for tx,ty,x,y in overworldSouthEastLowerRockTiles do
        a.[tx,ty].[x,y] <- true
    a
    