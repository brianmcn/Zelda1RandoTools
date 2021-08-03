module OverworldData

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
        | MIXED_SECOND -> PrivateInternals.owMapSquaresMixedQuestAlwaysEmpty.[y].Chars(x) = 'X' || (x=11 && y=0)  // first quest vanilla 5 is a dead fairy in second quest mixed and always empty // TODO maybe untrue?
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
        | MIXED_FIRST ->  (PrivateInternals.owMapSquaresFirstQuestWhistleable.[y].Chars(x) = 'X' || PrivateInternals.owMapSquaresSecondQuestWhistleable.[y].Chars(x) = 'X') && not(x=12 && y=3) // TODO actually, 12,3 appears to be random, probably also 11,0?
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


