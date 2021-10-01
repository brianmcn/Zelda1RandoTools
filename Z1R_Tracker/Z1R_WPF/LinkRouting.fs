module LinkRouting

open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let canvasAdd = Graphics.canvasAdd
let OMTW = OverworldRouteDrawing.OMTW

[<RequireQualifiedAccess>]
type RouteDestination =
    | SHOP of int
    | OW_MAP of int * int
    | HINTZONE of TrackerModel.HintZone * bool // bool is couldBeLetterDungeon

let SetupLinkRouting(cm:CustomComboBoxes.CanvasManager, offset, changeCurrentRouteTarget, eliminateCurrentRouteTarget, isSpecificRouteTargetActive,
                        updateTriforceDisplayImpl, updateNumberedTriforceDisplayImpl, updateLevel9NumeralImpl, isMirrored, sword2bmp) =
    let appMainCanvas = cm.AppMainCanvas
    let OFFSET = offset
    // help the player route to locations
    let linkIcon = new Canvas(Width=30., Height=30., Background=Brushes.Black)
    let mutable linkIconN = 0
    let setLinkIconImpl(n,linkIcon:Canvas) =
        linkIconN <- n
        let bmp =
            if n=0 then Graphics.linkFaceForward_bmp
            elif n=1 then Graphics.linkFaceRight_bmp
            elif n=2 then Graphics.linkRunRight_bmp
            elif n=3 then Graphics.linkGotTheThing_bmp
            else failwith "bad link position"
        let linkImage = bmp |> Graphics.BMPtoImage
        linkImage.Width <- 30.
        linkImage.Height <- 30.
        linkImage.Stretch <- Stretch.UniformToFill
        linkIcon.Children.Clear()
        canvasAdd(linkIcon, linkImage, 0., 0.)
    let setLinkIcon(n) = setLinkIconImpl(n,linkIcon)
    setLinkIcon(0)
    canvasAdd(appMainCanvas, linkIcon, 16.*OMTW-60., 120.)
    linkIcon.ToolTip <- "Click me and I'll help route you to a destination!"
    let currentTargetIcon = new Canvas(Width=30., Height=30.)
    canvasAdd(appMainCanvas, currentTargetIcon, 16.*OMTW-30., 120.)
    let stepAnimateLink() =
        // link animation
        if not(isSpecificRouteTargetActive()) then
            currentTargetIcon.Children.Clear()
            setLinkIcon(0)
        else
            if linkIconN=1 then
                setLinkIcon(2)
            elif linkIconN=2 then
                setLinkIcon(1)
    do   // scope for local variable names to not leak out
        let mutable popupIsActive = false
        let activatePopup() =
            popupIsActive <- true
            setLinkIcon(3)
            let wholeAppCanvas = new Canvas(Width=16.*OMTW, Height=1999., Background=Brushes.Transparent, IsHitTestVisible=true)  // TODO right height? I guess too big is ok
            let dismissHandle = CustomComboBoxes.DoModalCore(cm, 
                                                                (fun (c,e) -> canvasAdd(c,e,0.,0.)), 
                                                                (fun (c,e) -> c.Children.Remove(e) |> ignore), 
                                                                wholeAppCanvas, 0.01, (fun () -> popupIsActive <- false))
            let dismiss() = dismissHandle(); setLinkIcon(1); popupIsActive <- false
        
            let fakeSunglassesOverTopThird = new Canvas(Width=16.*OMTW, Height=150., Background=Brushes.Black, Opacity=0.50)
            canvasAdd(wholeAppCanvas, fakeSunglassesOverTopThird, 0., 0.)
            let fakeSunglassesOverBottomThird = new Canvas(Width=16.*OMTW, Height=1999., Background=Brushes.Black, Opacity=0.50)
            canvasAdd(wholeAppCanvas, fakeSunglassesOverBottomThird, 0., 150.+8.*11.*3.)
            let explanation = 
                new TextBox(Background=Brushes.Black, Foreground=Brushes.Orange, FontSize=18.,
                            Text="--Temporarily show routing only to a specific destination--\n"+
                                    "Choose a route destination:\n"+
                                    " - click an overworld map tile to route to that tile\n"+
                                    " - click a highlighted shop icon to route to any shops you've marked with that item\n"+
                                    " - click a highlighted triforce to route to that dungeon if location known or hinted\n"+
                                    " - click highlighted white/magical sword to route to that cave, if location known or hinted\n"+
                                    " - click anywhere else to cancel temporary routing\n"+
                                    "Link will 'chase' an icon in upper right while this is active")
                                    // TODO item progress, route to all burnables/powerbraceletables/etc?
            let b = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(3.), Child=explanation, Width=OMTW*16.-6., Height=230., IsHitTestVisible=false)
            canvasAdd(wholeAppCanvas, b, 0., 150.+8.*11.*3. + 100.)

            // bright, clickable targets
            let duplicateLinkIcon = new Canvas(Width=30., Height=30., Background=Brushes.Black)
            canvasAdd(wholeAppCanvas, duplicateLinkIcon, 16.*OMTW-60., 120.)
            setLinkIconImpl(3,duplicateLinkIcon)
            let makeIconTargetImpl(draw, drawLinkTarget, x, y, routeDest) =
                let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
                draw(c)
                canvasAdd(wholeAppCanvas, c, x, y)
                let borderRect = new Shapes.Rectangle(Width=30., Height=30., Stroke=Brushes.White, StrokeThickness=1.)
                canvasAdd(c, borderRect, 0., 0.)
                c.MouseDown.Add(fun ea -> 
                    ea.Handled <- true  // so it doesn't bubble up to wholeAppCanvas, which would treat it as an outside-region click and eliminate-target-and-dismiss
                    changeCurrentRouteTarget(routeDest)
                    dismiss()
                    currentTargetIcon.Children.Clear()
                    drawLinkTarget(currentTargetIcon)
                    )
            let makeIconTarget(draw, x, y, routeDest) = makeIconTargetImpl(draw, draw, x, y, routeDest)
            let makeShopIconTarget(draw, x, y, shopDest) =
                let mutable found = false
                for i = 0 to 15 do
                    for j = 0 to 7 do
                        let cur = TrackerModel.overworldMapMarks.[i,j].Current()
                        if cur = shopDest || (TrackerModel.getOverworldMapExtraData(i,j) = TrackerModel.MapSquareChoiceDomainHelper.ToItem(shopDest)) then
                            found <- true
                if found then
                    makeIconTarget(draw, x, y, RouteDestination.SHOP(shopDest))
            // shops
            makeShopIconTarget((fun c-> canvasAdd(c, Graphics.BMPtoImage Graphics.bomb_bmp, 4., 4.)), OFFSET+160., 60., TrackerModel.MapSquareChoiceDomainHelper.BOMB)
            makeShopIconTarget((fun c-> canvasAdd(c, Graphics.BMPtoImage Graphics.boom_book_bmp, 4., 4.)), OFFSET+120., 30., TrackerModel.MapSquareChoiceDomainHelper.BOOK)
            makeShopIconTarget((fun c-> canvasAdd(c, Graphics.BMPtoImage Graphics.blue_ring_bmp, 4., 4.)), OFFSET+120., 60., TrackerModel.MapSquareChoiceDomainHelper.BLUE_RING)
            makeShopIconTarget((fun c-> canvasAdd(c, Graphics.BMPtoImage Graphics.wood_arrow_bmp, 4., 4.)), OFFSET+120., 90., TrackerModel.MapSquareChoiceDomainHelper.ARROW)
            makeShopIconTarget((fun c-> canvasAdd(c, Graphics.BMPtoImage Graphics.blue_candle_bmp, 4., 4.)), OFFSET+90., 90., TrackerModel.MapSquareChoiceDomainHelper.BLUE_CANDLE)
            // triforces
            if TrackerModel.IsHiddenDungeonNumbers() then
                // letters
                for i = 0 to 7 do
                    // located, letters
                    if TrackerModel.GetDungeon(i).HasBeenLocated() then
                        let x,y = TrackerModel.mapStateSummary.DungeonLocations.[i]
                        makeIconTarget((fun c -> updateTriforceDisplayImpl(c,i)), 0.+float i*30., 30., RouteDestination.OW_MAP(x,y))
                    else
                        // hint letter due to numbered hint
                        let label = TrackerModel.GetDungeon(i).LabelChar
                        if label >= '1' && label <= '8' then
                            let index = int label - int '1'
                            if TrackerModel.levelHints.[index]<>TrackerModel.HintZone.UNKNOWN then
                                makeIconTarget((fun c -> updateTriforceDisplayImpl(c,i)), 0.+float i*30., 30., RouteDestination.HINTZONE(TrackerModel.levelHints.[index], true))
                // numbers
                for n = 0 to 7 do
                    let mutable index = -1
                    for i = 0 to 7 do
                        if TrackerModel.GetDungeon(i).LabelChar = char(int '1' + n) then
                            index <- i
                    // located or hinted numbers
                    let hint() =
                        if TrackerModel.levelHints.[n] <> TrackerModel.HintZone.UNKNOWN then
                            makeIconTarget((fun c -> updateNumberedTriforceDisplayImpl(c,n)), OFFSET+float n*30., 0., RouteDestination.HINTZONE(TrackerModel.levelHints.[n], true))
                    if index <> -1 then
                        let loc = TrackerModel.mapStateSummary.DungeonLocations.[index]
                        if loc <> TrackerModel.NOTFOUND then
                            makeIconTarget((fun c -> updateNumberedTriforceDisplayImpl(c,n)), OFFSET+float n*30., 0., RouteDestination.OW_MAP(loc))
                        else
                            hint()
                    else
                        hint()
            else
                for i = 0 to 7 do
                    if TrackerModel.GetDungeon(i).HasBeenLocated() then
                        let x,y = TrackerModel.mapStateSummary.DungeonLocations.[i]
                        makeIconTarget((fun c -> updateTriforceDisplayImpl(c,i)), 0.+float i*30., 30., RouteDestination.OW_MAP(x,y))
                    elif TrackerModel.levelHints.[i] <> TrackerModel.HintZone.UNKNOWN then
                        makeIconTarget((fun c -> updateTriforceDisplayImpl(c,i)), 0.+float i*30., 30., RouteDestination.HINTZONE(TrackerModel.levelHints.[i], false))
            if TrackerModel.GetDungeon(8).HasBeenLocated() then
                let x,y = TrackerModel.mapStateSummary.DungeonLocations.[8]
                makeIconTarget((fun c -> updateLevel9NumeralImpl(c)), 0.+8.*30., 30., RouteDestination.OW_MAP(x,y))
            elif TrackerModel.levelHints.[8] <> TrackerModel.HintZone.UNKNOWN then
                makeIconTarget((fun c -> updateLevel9NumeralImpl(c)), 0.+8.*30., 30., RouteDestination.HINTZONE(TrackerModel.levelHints.[8], false))
            // swords
            if TrackerModel.mapStateSummary.Sword2Location <> TrackerModel.NOTFOUND || TrackerModel.levelHints.[9] <> TrackerModel.HintZone.UNKNOWN then
                let (x,y) as loc = TrackerModel.mapStateSummary.Sword2Location
                let dest = if loc <> TrackerModel.NOTFOUND then RouteDestination.OW_MAP(x,y) else RouteDestination.HINTZONE(TrackerModel.levelHints.[9], false)
                makeIconTargetImpl((fun c-> canvasAdd(c, Graphics.BMPtoImage Graphics.white_sword_bmp, 4., 4.)), 
                    (fun c -> 
                        // white sword seems dodgy for link to chase, since it's actually the cave which likely has something else, so draw the map marker instead
                        let image = sword2bmp |> Graphics.BMPtoImage
                        canvasAdd(c, image, 7., 1.)), OFFSET, 120., dest)
            if TrackerModel.mapStateSummary.Sword3Location <> TrackerModel.NOTFOUND || TrackerModel.levelHints.[10] <> TrackerModel.HintZone.UNKNOWN then
                let (x,y) as loc = TrackerModel.mapStateSummary.Sword3Location
                let dest = if loc <> TrackerModel.NOTFOUND then RouteDestination.OW_MAP(x,y) else RouteDestination.HINTZONE(TrackerModel.levelHints.[10], false)
                makeIconTarget((fun c-> canvasAdd(c, Graphics.BMPtoImage Graphics.magical_sword_bmp, 4., 4.)), OFFSET+60., 120., dest)
            wholeAppCanvas.MouseDown.Add(fun ea ->
                let pos = ea.GetPosition(wholeAppCanvas)
                if pos.Y > 150. && pos.Y < 150.+8.*11.*3. then
                    // overworld map tile
                    let i = pos.X / OMTW |> int
                    let j = (pos.Y-150.) / (11.*3.) |> int
                    let i = if isMirrored() then 15-i else i
                    changeCurrentRouteTarget(RouteDestination.OW_MAP(i,j))
                    // draw crosshairs icon
                    currentTargetIcon.Children.Clear()
                    canvasAdd(currentTargetIcon, new Canvas(Width=30., Height=30., Background=Graphics.overworldCommonestFloorColorBrush), 0., 0.)
                    let tb = new TextBox(FontSize=18., Foreground=Brushes.Black, Background=Brushes.Transparent, Text="+", IsHitTestVisible=false, BorderThickness=Thickness(0.))
                    canvasAdd(currentTargetIcon, tb, 7., 1.)
                    let borderRect = new Shapes.Rectangle(Width=30., Height=30., Stroke=Brushes.White, StrokeThickness=1.)
                    canvasAdd(currentTargetIcon, borderRect, 0., 0.)
                    dismiss()
                else
                    // they clicked elsewhere
                    eliminateCurrentRouteTarget()
                    dismiss()
                    setLinkIcon(0)
                )
        linkIcon.MouseDown.Add(fun _ ->
            if not popupIsActive then
                activatePopup()
            )
    stepAnimateLink
