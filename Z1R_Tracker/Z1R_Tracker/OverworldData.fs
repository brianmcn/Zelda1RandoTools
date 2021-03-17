module OverworldData

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

let owMapZoneImages =
    let avg(c1:System.Drawing.Color, c2:System.Drawing.Color) = System.Drawing.Color.FromArgb((int c1.R + int c2.R)/2, (int c1.G + int c2.G)/2, (int c1.B + int c2.B)/2)
    let colors = 
        dict [
            'M', avg(System.Drawing.Color.Pink, System.Drawing.Color.Crimson)
            'L', System.Drawing.Color.BlueViolet 
            'R', System.Drawing.Color.LightSeaGreen 
            'H', System.Drawing.Color.Gray
            'C', System.Drawing.Color.LightBlue 
            'G', avg(System.Drawing.Color.LightSteelBlue, System.Drawing.Color.SteelBlue)
            'D', System.Drawing.Color.Orange 
            'F', System.Drawing.Color.LightGreen 
            'S', System.Drawing.Color.DarkGray 
            'W', System.Drawing.Color.Brown
        ]
    let imgs = Array2D.zeroCreate 16 8
    for x = 0 to 15 do
        for y = 0 to 7 do
            let tile = new System.Drawing.Bitmap(16*3,11*3)
            for px = 0 to 16*3-1 do
                for py = 0 to 11*3-1 do
                    tile.SetPixel(px, py, colors.Item(owMapZone.[y].[x]))
            imgs.[x,y] <- Graphics.BMPtoImage tile
    imgs

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

let owMapSquaresFirstQuestOnlyIfMixed = [|
    for i = 0 to 7 do
        let mutable s = ""
        for j = 0 to 15 do
            if owMapSquaresFirstQuestAlwaysEmpty.[i].[j] = 'X' && owMapSquaresSecondQuestAlwaysEmpty.[i].[j] <> 'X' then
                s <- s + "X"
            else
                s <- s + "."
        yield s
    |]

let owMapSquaresSecondQuestOnlyIfMixed = [|
    for i = 0 to 7 do
        let mutable s = ""
        for j = 0 to 15 do
            if owMapSquaresFirstQuestAlwaysEmpty.[i].[j] <> 'X' && owMapSquaresSecondQuestAlwaysEmpty.[i].[j] = 'X' then
                s <- s + "X"
            else
                s <- s + "."
        yield s
    |]

