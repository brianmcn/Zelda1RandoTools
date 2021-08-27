module OverworldData

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

type OWQuest = 
    | FIRST
    | SECOND
    | MIXED_FIRST
    | MIXED_SECOND

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

type OverworldInstance(quest) =  // TODO figure out where mirror overworld layer should go, and right translation between screen coords and map coords
    member this.AlwaysEmpty(x,y) =
        match quest with
        | FIRST ->        PrivateInternals.owMapSquaresFirstQuestAlwaysEmpty.[y].Chars(x) = 'X'
        | SECOND ->       PrivateInternals.owMapSquaresSecondQuestAlwaysEmpty.[y].Chars(x) = 'X'
        | MIXED_FIRST ->  PrivateInternals.owMapSquaresMixedQuestAlwaysEmpty.[y].Chars(x) = 'X'
        | MIXED_SECOND -> PrivateInternals.owMapSquaresMixedQuestAlwaysEmpty.[y].Chars(x) = 'X'
    member this.Ladderable(x,y) =
        match quest with
        | FIRST ->        false
        | _ ->            PrivateInternals.owMapSquaresSecondQuestLadderable.[y].Chars(x) = 'X'
    member this.Raftable(x,y) =
        PrivateInternals.owMapSquaresRaftable.[y].Chars(x) = 'X'
    member this.Whistleable(x,y) =
        match quest with
        | FIRST ->        PrivateInternals.owMapSquaresFirstQuestWhistleable.[y].Chars(x) = 'X'
        | SECOND ->       PrivateInternals.owMapSquaresSecondQuestWhistleable.[y].Chars(x) = 'X'
        | MIXED_FIRST ->  (PrivateInternals.owMapSquaresFirstQuestWhistleable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestWhistleable.[y].Chars(x) = 'X')
        | MIXED_SECOND -> (PrivateInternals.owMapSquaresFirstQuestWhistleable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestWhistleable.[y].Chars(x) = 'X')
    member this.PowerBraceletable(x,y) =
        match quest with
        | FIRST ->        PrivateInternals.owMapSquaresFirstQuestPowerBraceletable.[y].Chars(x) = 'X'
        | SECOND ->       PrivateInternals.owMapSquaresSecondQuestPowerBraceletable.[y].Chars(x) = 'X'
        | MIXED_FIRST ->  (PrivateInternals.owMapSquaresFirstQuestPowerBraceletable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestPowerBraceletable.[y].Chars(x) = 'X')
        | MIXED_SECOND -> (PrivateInternals.owMapSquaresFirstQuestPowerBraceletable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestPowerBraceletable.[y].Chars(x) = 'X')
    member this.Burnable(x,y) =
        match quest with
        | FIRST ->        PrivateInternals.owMapSquaresFirstQuestBurnable.[y].Chars(x) = 'X'
        | SECOND ->       PrivateInternals.owMapSquaresSecondQuestBurnable.[y].Chars(x) = 'X'
        | MIXED_FIRST ->  (PrivateInternals.owMapSquaresFirstQuestBurnable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestBurnable.[y].Chars(x) = 'X')
        | MIXED_SECOND -> (PrivateInternals.owMapSquaresFirstQuestBurnable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestBurnable.[y].Chars(x) = 'X')
    member this.Bombable(x,y) =
        match quest with
        | FIRST ->        PrivateInternals.owMapSquaresFirstQuestBombable.[y].Chars(x) = 'X'
        | SECOND ->       PrivateInternals.owMapSquaresSecondQuestBombable.[y].Chars(x) = 'X'
        | MIXED_FIRST ->  (PrivateInternals.owMapSquaresFirstQuestBombable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestBombable.[y].Chars(x) = 'X')
        | MIXED_SECOND -> (PrivateInternals.owMapSquaresFirstQuestBombable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestBombable.[y].Chars(x) = 'X')

// yellow brown rocks in the top half of '\'
let overworldSouthEastRockTiles = [|
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
    5,  1,  9,  1
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
    10,  7, 12,  4
    10,  7, 14,  4
    11,  0, 13,  1
    11,  0, 14,  2
    11,  1, 13,  2
    11,  1, 14,  4
    11,  7,  2,  4
    12,  0, 11,  1
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
    |]

// yellow brown rocks in the top half of '/'
let overworldNorthEastRockTiles = [|
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
    |]

let owNErock = 
    let a = Array2D.init 16 8 (fun _ _ -> Array2D.zeroCreate 16 11)
    for tx,ty,x,y in overworldNorthEastRockTiles do
        a.[tx,ty].[x,y] <- true
    a

let owSErock = 
    let a = Array2D.init 16 8 (fun _ _ -> Array2D.zeroCreate 16 11)
    for tx,ty,x,y in overworldSouthEastRockTiles do
        a.[tx,ty].[x,y] <- true
    a