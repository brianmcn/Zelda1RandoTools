module UIComponents

open OverworldItemGridUI

open System.Windows
open System.Windows.Controls 
open System.Windows.Media

let UNICODE_UP = "\U0001F845"
let UNICODE_LEFT = "\U0001F844"
let UNICODE_DOWN = "\U0001F847"
let UNICODE_RIGHT = "\U0001F846"
let arrowColor = new SolidColorBrush(Color.FromArgb(255uy,0uy,180uy,250uy))
let bgColor = new SolidColorBrush(Color.FromArgb(220uy,0uy,0uy,0uy))

let MakeMagnifier(mirrorOverworldFEs:ResizeArray<FrameworkElement>, owMapNum, owMapBMPs:System.Drawing.Bitmap[,]) =
    // nearby ow tiles magnified overlay
    let ENLARGE = 8.
    let POP = 1  // width of entrance border
    let BT = 2.  // border thickness of the interior 3x3 grid of tiles
    let dungeonTabsOverlay = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(5.), Background=Brushes.Black, Opacity=0., IsHitTestVisible=false)
    let DTOCW,DTOCH = 3.*16.*ENLARGE + 4.*BT, 3.*11.*ENLARGE + 4.*BT
    let dungeonTabsOverlayContent = new Canvas(Width=DTOCW, Height=DTOCH)
    mirrorOverworldFEs.Add(dungeonTabsOverlayContent)
    let dtocPlusLegend = new StackPanel(Orientation=Orientation.Vertical)
    dtocPlusLegend.Children.Add(dungeonTabsOverlayContent) |> ignore
    let dtocLegend = new StackPanel(Orientation=Orientation.Horizontal, Background=Graphics.almostBlack)
    for outer,inner,desc in [Brushes.Cyan, Brushes.Black, "open cave"
                             Brushes.Black, Brushes.Cyan, "bomb spot"
                             Brushes.Black, Brushes.Red, "burn spot"
                             Brushes.Black, Brushes.Yellow, "recorder spot"
                             Brushes.Black, Brushes.Magenta, "pushable spot"] do
        let black = new Canvas(Width=ENLARGE + 2.*(float POP + 1.), Height=ENLARGE + 2.*(float POP + 1.), Background=Brushes.Black)
        let outer = new Canvas(Width=ENLARGE + 2.*(float POP), Height=ENLARGE + 2.*(float POP), Background=outer)
        let inner = new Canvas(Width=ENLARGE, Height=ENLARGE, Background=inner)
        canvasAdd(black, outer, 1., 1.)
        canvasAdd(black, inner, 1.+float POP, 1.+float POP)
        dtocLegend.Children.Add(black) |> ignore
        let text = new TextBox(Text=desc, Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                                    FontSize=12., HorizontalContentAlignment=HorizontalAlignment.Center)
        dtocLegend.Children.Add(text) |> ignore
    dtocPlusLegend.Children.Add(dtocLegend) |> ignore
    dungeonTabsOverlay.Child <- dtocPlusLegend
    let overlayTiles = Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let bmp = new System.Drawing.Bitmap(16*int ENLARGE, 11*int ENLARGE)
            for x = 0 to 15 do
                for y = 0 to 10 do
                    let c = owMapBMPs.[i,j].GetPixel(x*3, y*3)
                    for px = 0 to int ENLARGE - 1 do
                        for py = 0 to int ENLARGE - 1 do
                            // diagonal rocks
                            let c = 
                                // The diagonal rock data is based on the first quest map. A few screens are different in 2nd/mixed quest.
                                // So we apply a kludge to load the correct diagonal data.
                                let i,j = 
                                    if owMapNum=1 && i=4 && j=7 then // second quest has a cave like 14,5 here
                                        14,5
                                    elif owMapNum=1 && i=11 && j=0 then // second quest has fairy here, borrow 2,4
                                        2,4
                                    elif owMapNum<>0 && i=12 && j=3 then // non-first quest has a whistle lake here, borrow 2,4
                                        2,4
                                    else
                                        i,j
                                if OverworldData.owNEupperRock.[i,j].[x,y] then
                                    if px+py > int ENLARGE - 1 then 
                                        owMapBMPs.[i,j].GetPixel(x*3, (y+1)*3)
                                    else 
                                        c
                                elif OverworldData.owSEupperRock.[i,j].[x,y] then
                                    if px < py then 
                                        owMapBMPs.[i,j].GetPixel(x*3, (y+1)*3)
                                    else 
                                        c
                                elif OverworldData.owNElowerRock.[i,j].[x,y] then
                                    if px+py < int ENLARGE - 1 then 
                                        owMapBMPs.[i,j].GetPixel(x*3, (y-1)*3)
                                    else 
                                        c
                                elif OverworldData.owSElowerRock.[i,j].[x,y] then
                                    if px > py then 
                                        owMapBMPs.[i,j].GetPixel(x*3, (y-1)*3)
                                    else 
                                        c
                                else 
                                    c
                            // edges of squares
                            let c = 
                                if (px+1) % int ENLARGE = 0 || (py+1) % int ENLARGE = 0 then
                                    System.Drawing.Color.FromArgb(int c.R / 2, int c.G / 2, int c.B / 2)
                                else
                                    c
                            bmp.SetPixel(x*int ENLARGE + px, y*int ENLARGE + py, c)
            // make the entrances 'pop'
            // No 'entrance pixels' are on the edge of a tile, and we would be drawing outside bitmap array bounds if they were, so only iterate over interior pixels:
            for x = 1 to 14 do
                for y = 1 to 9 do
                    let c = owMapBMPs.[i,j].GetPixel(x*3, y*3)
                    let border = 
                        if c.ToArgb() = System.Drawing.Color.Black.ToArgb() then    // black open cave
                            let c2 = owMapBMPs.[i,j].GetPixel((x-1)*3, y*3)
                            if c2.ToArgb() = System.Drawing.Color.Black.ToArgb() then    // also black to the left, this is vanilla 6 two-wide entrance, only show one
                                None
                            else
                                Some(System.Drawing.Color.FromArgb(0xFF,0x00,0xCC,0xCC))
                        elif c.ToArgb() = System.Drawing.Color.FromArgb(0xFF,0x00,0xFF,0xFF).ToArgb() then  // cyan bomb spot
                            Some(System.Drawing.Color.FromArgb(0xFF,0x00,0x00,0x00))
                        elif c.ToArgb() = System.Drawing.Color.FromArgb(0xFF,0xFF,0xFF,0x00).ToArgb() then  // yellow recorder spot
                            Some(System.Drawing.Color.FromArgb(0xFF,0x00,0x00,0x00))
                        elif c.ToArgb() = System.Drawing.Color.FromArgb(0xFF,0xFF,0x00,0x00).ToArgb() then  // red burn spot
                            Some(System.Drawing.Color.FromArgb(0xFF,0x00,0x00,0x00))
                        elif c.ToArgb() = System.Drawing.Color.FromArgb(0xFF,0xFF,0x00,0xFF).ToArgb() then  // magenta pushblock spot
                            Some(System.Drawing.Color.FromArgb(0xFF,0x00,0x00,0x00))
                        else
                            None
                    match border with
                    | Some bc -> 
                        // thin black outline
                        for px = x*int ENLARGE - POP - 1 to (x+1)*int ENLARGE - 1 + POP + 1 do
                            for py = y*int ENLARGE - POP - 1 to (y+1)*int ENLARGE - 1 + POP + 1 do
                                bmp.SetPixel(px, py, System.Drawing.Color.Black)
                        // border color
                        for px = x*int ENLARGE - POP to (x+1)*int ENLARGE - 1 + POP do
                            for py = y*int ENLARGE - POP to (y+1)*int ENLARGE - 1 + POP do
                                bmp.SetPixel(px, py, bc)
                        // inner actual pixel
                        for px = x*int ENLARGE to (x+1)*int ENLARGE - 1 do
                            for py = y*int ENLARGE to (y+1)*int ENLARGE - 1 do
                                bmp.SetPixel(px, py, c)
                    | None -> ()
            overlayTiles.[i,j] <- Graphics.BMPtoImage bmp
    let makeArrow(text) = 
        let tb = new TextBox(Text=text, FontSize=20., Foreground=arrowColor, Background=bgColor, IsReadOnly=true, BorderThickness=Thickness(0.))
        tb.Clip <- new RectangleGeometry(Rect(0., 6., 30., 18.))
        tb
    let onMouseForMagnifier(i,j) = 
        // show enlarged version of current & nearby rooms
        dungeonTabsOverlayContent.Children.Clear()
        // fill whole canvas black, so elements behind don't show through
        canvasAdd(dungeonTabsOverlayContent, new Shapes.Rectangle(Width=dungeonTabsOverlayContent.Width, Height=dungeonTabsOverlayContent.Height, Fill=Brushes.Black), 0., 0.)
        let xmin = min (max (i-1) 0) 13
        let ymin = min (max (j-1) 0) 5
        // draw a white highlight rectangle behind the tile where mouse is
        let rect = new Shapes.Rectangle(Width=16.*ENLARGE + 2.*BT, Height=11.*ENLARGE + 2.*BT, Fill=Brushes.White)
        canvasAdd(dungeonTabsOverlayContent, rect, float (i-xmin)*(16.*ENLARGE+BT), float (j-ymin)*(11.*ENLARGE+BT))
        // draw the 3x3 tiles
        for x = 0 to 2 do
            for y = 0 to 2 do
                let dx = BT+float x*(16.*ENLARGE+BT)
                let dy = BT+float y*(11.*ENLARGE+BT)
                canvasAdd(dungeonTabsOverlayContent, overlayTiles.[xmin+x,ymin+y], dx, dy)
                if xmin+x = 1 && ymin+y = 6 then // Lost Woods
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_UP),   dx+3., dy-2.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_LEFT), dx+3., dy+18.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_DOWN), dx+3., dy+38.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_LEFT), dx+3., dy+58.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_RIGHT),dx+101., dy+28.)
                if xmin+x = 11 && ymin+y = 1 then // Lost Hills
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_UP),   dx+20., dy-2.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_UP),   dx+40., dy-2.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_UP),   dx+60., dy-2.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_UP),   dx+80., dy-2.)
                    canvasAdd(dungeonTabsOverlayContent, makeArrow(UNICODE_LEFT), dx+3.,  dy+28.)
        if TrackerModel.Options.Overworld.ShowMagnifier.Value then 
            dungeonTabsOverlay.Opacity <- 1.0

    onMouseForMagnifier, dungeonTabsOverlay, dungeonTabsOverlayContent

let MakeLegend(cm:CustomComboBoxes.CanvasManager, resizeMapTileImage:Image->Image, drawCompletedDungeonHighlight, makeStartIcon, doUIUpdateEvent:Event<unit>) =
    let appMainCanvas = cm.AppMainCanvas

    // map legend
    let legendCanvas = new Canvas()
    canvasAdd(appMainCanvas, legendCanvas, LEFT_OFFSET, THRU_MAIN_MAP_H)

    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="The LEGEND\nof Z-Tracker")
    canvasAdd(appMainCanvas, tb, 0., THRU_MAIN_MAP_H)

    let shrink(bmp) = resizeMapTileImage <| Graphics.BMPtoImage bmp
    let firstDungeonBMP = if TrackerModel.IsHiddenDungeonNumbers() then Graphics.theFullTileBmpTable.[0].[2] else Graphics.theFullTileBmpTable.[0].[0]
    canvasAdd(legendCanvas, shrink firstDungeonBMP, 0., 0.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Active\nDungeon")
    canvasAdd(legendCanvas, tb, OMTW*0.8, 0.)

    let firstGreenDungeonBMP = if TrackerModel.IsHiddenDungeonNumbers() then Graphics.theFullTileBmpTable.[0].[3] else Graphics.theFullTileBmpTable.[0].[1]
    canvasAdd(legendCanvas, shrink firstDungeonBMP, 2.1*OMTW, 0.)
    drawCompletedDungeonHighlight(legendCanvas,2.1,0,false)
    canvasAdd(legendCanvas, shrink firstGreenDungeonBMP, 2.5*OMTW, 0.)
    drawCompletedDungeonHighlight(legendCanvas,2.5,0,false)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Completed\nDungeon")
    canvasAdd(legendCanvas, tb, 3.3*OMTW, 0.)

    let recorderDestinationLegendIcon = shrink firstGreenDungeonBMP
    canvasAdd(legendCanvas, recorderDestinationLegendIcon, 4.8*OMTW, 0.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Recorder\nDestination")
    canvasAdd(legendCanvas, tb, 5.6*OMTW, 0.)

    let anyRoadLegendIcon = shrink(Graphics.theFullTileBmpTable.[9].[0])
    canvasAdd(legendCanvas, anyRoadLegendIcon, 7.1*OMTW, 0.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Any Road\n(Warp)")
    canvasAdd(legendCanvas, tb, 7.9*OMTW, 0.)

    let legendStartIconButtonCanvas = new Canvas(Background=Graphics.almostBlack, Width=OMTW*1.45, Height=11.*3.)
    let legendStartIcon = makeStartIcon()
    canvasAdd(legendStartIconButtonCanvas, legendStartIcon, 0.+4.*OMTW/48., 0.)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Graphics.almostBlack, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Start\nSpot", IsHitTestVisible=false)
    canvasAdd(legendStartIconButtonCanvas, tb, 0.8*OMTW, 0.)
    let legendStartIconButton = new Button(Content=legendStartIconButtonCanvas)
    canvasAdd(legendCanvas, legendStartIconButton, 9.1*OMTW, 0.)
    legendStartIconButtonBehavior <- (fun () ->
        if not popupIsActive then
            popupIsActive <- true
            let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, FontSize=16.,
                                    Text="Click an overworld map tile to move the Start Spot icon there, or click anywhere outside the map to cancel")
            let element = new Canvas(Width=OMTW*16., Height=float(8*11*3), Background=Brushes.Transparent, IsHitTestVisible=true)
            canvasAdd(element, tb, 0., -30.)
            let hoverIcon = makeStartIcon()
            element.MouseLeave.Add(fun _ -> element.Children.Remove(hoverIcon))
            element.MouseMove.Add(fun ea ->
                let mousePos = ea.GetPosition(element)
                let i = int(mousePos.X / OMTW)
                let j = int(mousePos.Y / (11.*3.))
                element.Children.Remove(hoverIcon)
                canvasAdd(element, hoverIcon, float i*OMTW + 8.5*OMTW/48., float(j*11*3))
                )
            let wh = new System.Threading.ManualResetEvent(false)
            element.MouseDown.Add(fun ea ->
                let mousePos = ea.GetPosition(element)
                let i = int(mousePos.X / OMTW)
                let j = int(mousePos.Y / (11.*3.))
                if i>=0 && i<=15 && j>=0 && j<=7 then
                    TrackerModel.startIconX <- if displayIsCurrentlyMirrored then (15-i) else i
                    TrackerModel.startIconY <- j
                    doUIUpdateEvent.Trigger()
                    wh.Set() |> ignore
                )
            async {
                do! CustomComboBoxes.DoModal(cm, wh, 0., 150., element)
                popupIsActive <- false
                } |> Async.StartImmediate
        )
    legendStartIconButton.Click.Add(fun _ -> legendStartIconButtonBehavior())
    recorderDestinationLegendIcon, anyRoadLegendIcon

let MakeItemProgressBar(appMainCanvas, owInstance:OverworldData.OverworldInstance) =
    // item progress
    let itemProgressCanvas = new Canvas(Width=16.*OMTW, Height=30.)
    canvasAdd(appMainCanvas, itemProgressCanvas, 0., THRU_MAP_AND_LEGEND_H)
    let tb = new TextBox(FontSize=12., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, BorderThickness=Thickness(0.), Text="Item Progress", IsHitTestVisible=false)
    canvasAdd(appMainCanvas, tb, 50., THRU_MAP_AND_LEGEND_H + 4.)
    itemProgressCanvas.MouseMove.Add(fun ea ->
        let pos = ea.GetPosition(itemProgressCanvas)
        let x = pos.X - ITEM_PROGRESS_FIRST_ITEM
        if x >  30. && x <  60. then
            showLocatorInstanceFunc(owInstance.Burnable)
        if x > 240. && x < 270. then
            showLocatorInstanceFunc(owInstance.Ladderable)
        if x > 270. && x < 300. then
            showLocatorInstanceFunc(owInstance.Whistleable)
        if x > 300. && x < 330. then
            showLocatorInstanceFunc(owInstance.PowerBraceletable)
        if x > 330. && x < 360. then
            showLocatorInstanceFunc(owInstance.Raftable)
        )
    itemProgressCanvas.MouseLeave.Add(fun _ -> hideLocator())
    let redrawItemProgressBar() = 
        itemProgressCanvas.Children.Clear()
        let mutable x, y = ITEM_PROGRESS_FIRST_ITEM, 3.
        let DX = 30.
        canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.swordLevelToBmp(TrackerModel.playerComputedStateSummary.SwordLevel)), x, y)
        x <- x + DX
        match TrackerModel.playerComputedStateSummary.CandleLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.red_candle_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.blue_candle_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.red_candle_bmp, x, y)
        | _ -> failwith "bad CandleLevel"
        x <- x + DX
        canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.ringLevelToBmp(TrackerModel.playerComputedStateSummary.RingLevel)), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveBow then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.bow_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.bow_bmp), x, y)
        x <- x + DX
        match TrackerModel.playerComputedStateSummary.ArrowLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.silver_arrow_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.wood_arrow_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.silver_arrow_bmp, x, y)
        | _ -> failwith "bad ArrowLevel"
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveWand then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.wand_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.wand_bmp), x, y)
        x <- x + DX
        if TrackerModel.IsCurrentlyBook() then
            // book seed
            if TrackerModel.playerComputedStateSummary.HaveBookOrShield then
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.book_bmp, x, y)
            else
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.book_bmp), x, y)
        else
            // boomstick seed
            if TrackerModel.playerProgressAndTakeAnyHearts.PlayerHasBoomBook.Value() then
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.boom_book_bmp, x, y)
            else
                canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.boom_book_bmp), x, y)
        x <- x + DX
        match TrackerModel.playerComputedStateSummary.BoomerangLevel with
        | 0 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.magic_boomerang_bmp), x, y)
        | 1 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.boomerang_bmp, x, y)
        | 2 -> canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.magic_boomerang_bmp, x, y)
        | _ -> failwith "bad BoomerangLevel"
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveLadder then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.ladder_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.ladder_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveRecorder then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.recorder_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.recorder_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HavePowerBracelet then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.power_bracelet_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.power_bracelet_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveRaft then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.raft_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.raft_bmp), x, y)
        x <- x + DX
        if TrackerModel.playerComputedStateSummary.HaveAnyKey then
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage Graphics.key_bmp, x, y)
        else
            canvasAdd(itemProgressCanvas, Graphics.BMPtoImage(Graphics.greyscale Graphics.key_bmp), x, y)
    redrawItemProgressBar

let MakeHintDecoderUI(cm:CustomComboBoxes.CanvasManager) =
    let HINTGRID_W, HINTGRID_H = 180., 36.
    let hintGrid = makeGrid(3,OverworldData.hintMeanings.Length,int HINTGRID_W,int HINTGRID_H)
    let mutable row=0 
    let updateViewFunctions = Array.create 11 (fun _ -> ())
    let mkTxt(text) = 
        new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, 
                    Width=HINTGRID_W-6., Height=HINTGRID_H-6., BorderThickness=Thickness(0.), VerticalAlignment=VerticalAlignment.Center, Text=text)
    for a,b in OverworldData.hintMeanings do
        let thisRow = row
        gridAdd(hintGrid, new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(1.), Text=a), 0, row)
        let tb = new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(1.), Text=b)
        let dp = new DockPanel(LastChildFill=true)
        let bmp = 
            if row < 8 then
                Graphics.emptyUnfoundNumberedTriforce_bmps.[row]
            elif row = 8 then
                Graphics.unfoundL9_bmp
            elif row = 9 then
                Graphics.white_sword_bmp
            else
                Graphics.magical_sword_bmp
        let image = Graphics.BMPtoImage bmp
        image.Width <- 32.
        image.Stretch <- Stretch.None
        let b = new Border(Child=image, BorderThickness=Thickness(1.), BorderBrush=Brushes.LightGray, Background=Brushes.Black)
        DockPanel.SetDock(b, Dock.Left)
        dp.Children.Add(b) |> ignore
        dp.Children.Add(tb) |> ignore
        gridAdd(hintGrid, dp, 1, row)
        let button = new Button()
        gridAdd(hintGrid, button, 2, row)
        let updateView() =
            let hintZone = TrackerModel.GetLevelHint(thisRow)
            if hintZone.ToIndex() = 0 then
                b.Background <- Brushes.Black
            else
                b.Background <- Views.hintHighlightBrush
            button.Content <- mkTxt(hintZone.ToString())
        updateViewFunctions.[thisRow] <- updateView
        let mutable popupIsActive = false  // second level of popup, need local copy
        let activatePopup(activationDelta) =
            popupIsActive <- true
            let tileX, tileY = (let p = button.TranslatePoint(Point(),cm.AppMainCanvas) in p.X+3., p.Y+3.)
            let tileCanvas = new Canvas(Width=HINTGRID_W-6., Height=HINTGRID_H-6., Background=Brushes.Black)
            let redrawTile(i) =
                tileCanvas.Children.Clear()
                canvasAdd(tileCanvas, mkTxt(TrackerModel.HintZone.FromIndex(i).ToString()), 3., 3.)
            let gridElementsSelectablesAndIDs = [|
                for i = 0 to 10 do
                    yield mkTxt(TrackerModel.HintZone.FromIndex(i).ToString()) :> FrameworkElement, true, i
                |]
            let originalStateIndex = TrackerModel.GetLevelHint(thisRow).ToIndex()
            let (gnc, gnr, gcw, grh) = 1, 11, int HINTGRID_W-6, int HINTGRID_H-6
            let gx,gy = HINTGRID_W-3., -HINTGRID_H*float(thisRow)-9.
            let onClick(_ea, i) = CustomComboBoxes.DismissPopupWithResult(i)
            let extraDecorations = []
            let brushes = CustomComboBoxes.ModalGridSelectBrushes.Defaults()
            let gridClickDismissalDoesMouseWarpBackToTileCenter = false
            async {
                let! r = CustomComboBoxes.DoModalGridSelect(cm, tileX, tileY, tileCanvas, gridElementsSelectablesAndIDs, originalStateIndex, activationDelta, (gnc, gnr, gcw, grh),
                                                gx, gy, redrawTile, onClick, extraDecorations, brushes, gridClickDismissalDoesMouseWarpBackToTileCenter, None)
                match r with
                | Some(i) ->
                    TrackerModel.SetLevelHint(thisRow, TrackerModel.HintZone.FromIndex(i))
                    TrackerModel.forceUpdate()
                    updateView()
                | None -> ()
                popupIsActive <- false
                } |> Async.StartImmediate
        button.Click.Add(fun _ -> if not popupIsActive then activatePopup(0))
        button.MouseWheel.Add(fun x -> if not popupIsActive then activatePopup(if x.Delta>0 then -1 else 1))
        row <- row + 1
    let hintDescriptionTextBox = 
        new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.,0.,0.,4.), 
                    Text="Each hinted-but-not-yet-found location will cause a 'halo' to appear on\n"+
                         "the triforce/sword icon in the upper portion of the tracker, and hovering the\n"+
                         "halo will show the possible locations for that dungeon or sword cave.")
    let hintSP = new StackPanel(Orientation=Orientation.Vertical)
    hintSP.Children.Add(hintDescriptionTextBox) |> ignore
    hintSP.Children.Add(hintGrid) |> ignore
    let makeHintText(txt) = new TextBox(FontSize=16., Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, Text=txt)
    let otherChoices = new DockPanel(LastChildFill=true)
    let otherTB = makeHintText("There are a few other types of hints. To see them, click here:")
    let otherButton = new Button(Content=new Label(FontSize=16., Content="Other hints"))
    DockPanel.SetDock(otherButton, Dock.Right)
    otherChoices.Children.Add(otherTB)|> ignore
    otherChoices.Children.Add(otherButton)|> ignore
    hintSP.Children.Add(otherChoices) |> ignore
    let hintBorder = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(8.), Background=Brushes.Black, Child=hintSP)
    let tb = Graphics.makeButton("Hint Decoder", Some(12.), Some(Brushes.Orange))
    canvasAdd(cm.AppMainCanvas, tb, 510., THRU_MAP_AND_LEGEND_H + 6.)
    tb.Click.Add(fun _ -> 
        if not popupIsActive then
            popupIsActive <- true
            for i = 0 to 10 do
                updateViewFunctions.[i]()
            let wh = new System.Threading.ManualResetEvent(false)
            let mutable otherButtonWasClicked = false
            otherButton.Click.Add(fun _ ->
                otherButtonWasClicked <- true
                wh.Set() |> ignore
                )
            async {
                do! CustomComboBoxes.DoModal(cm, wh, 0., 65., hintBorder)
                if otherButtonWasClicked then
                    wh.Reset() |> ignore
                    let otherSP = new StackPanel(Orientation=Orientation.Vertical)
                    let otherTopTB = makeHintText("Here are the meanings of some hints, which you need to track on your own:")
                    otherTopTB.BorderThickness <- Thickness(0.,0.,0.,4.)
                    otherSP.Children.Add(otherTopTB) |> ignore
                    for desc,mean in 
                        [|
                        "A feat of strength will lead to...", "Either push a gravestone, or push\nan overworld rock requiring Power Bracelet"
                        "Sail across the water...", "Raft required to reach a place"
                        "Play a melody...", "Either an overworld recorder spot, or a\nDigdogger in a dungeon logically blocks..."
                        "Fire the arrow...", "In a dungeon, Gohma logically blocks..."
                        "Step over the water...", "Ladder required to obtain... (coast item,\noverworld river, or dungeon moat)"
                        |] do
                        let dp = new DockPanel(LastChildFill=true)
                        let d = makeHintText(desc)
                        d.Width <- 240.
                        dp.Children.Add(d) |> ignore
                        let m = makeHintText(mean)
                        DockPanel.SetDock(m, Dock.Right)
                        dp.Children.Add(m) |> ignore
                        otherSP.Children.Add(dp) |> ignore
                    let otherBottomTB = makeHintText("Here are the meanings of a couple final hints, which the tracker can help with\nby darkening the overworld spots you can logically ignore\n(click the checkbox to darken corresponding spots on the overworld)")
                    otherBottomTB.BorderThickness <- Thickness(0.,4.,0.,4.)
                    otherSP.Children.Add(otherBottomTB) |> ignore
                    let featsCheckBox  = new CheckBox(Content=makeHintText("No feat of strength... (Power Bracelet / pushing graves not required)"))
                    featsCheckBox.IsChecked <- System.Nullable.op_Implicit TrackerModel.NoFeatOfStrengthHintWasGiven
                    featsCheckBox.Checked.Add(fun _ -> TrackerModel.NoFeatOfStrengthHintWasGiven <- true; hideFeatsOfStrength true)
                    featsCheckBox.Unchecked.Add(fun _ -> TrackerModel.NoFeatOfStrengthHintWasGiven <- false; hideFeatsOfStrength false)
                    otherSP.Children.Add(featsCheckBox) |> ignore
                    let raftsCheckBox  = new CheckBox(Content=makeHintText("Sail not... (Raft not required)"))
                    raftsCheckBox.IsChecked <- System.Nullable.op_Implicit TrackerModel.SailNotHintWasGiven
                    raftsCheckBox.Checked.Add(fun _ -> TrackerModel.SailNotHintWasGiven <- true; hideRaftSpots true)
                    raftsCheckBox.Unchecked.Add(fun _ -> TrackerModel.SailNotHintWasGiven <- false; hideRaftSpots false)
                    otherSP.Children.Add(raftsCheckBox) |> ignore
                    let otherHintBorder = new Border(BorderBrush=Brushes.Gray, BorderThickness=Thickness(8.), Background=Brushes.Black, Child=otherSP)
                    do! CustomComboBoxes.DoModal(cm, wh, 0., 65., otherHintBorder)
                popupIsActive <- false
                } |> Async.StartImmediate
        )

open HotKeys.MyKey

let MakeBlockers(cm:CustomComboBoxes.CanvasManager, levelTabSelected:Event<int>, blockersHoverEvent:Event<bool>, blockerDungeonSunglasses:FrameworkElement[]) =
    let appMainCanvas = cm.AppMainCanvas
    // blockers
    let blocker_gsc = new GradientStopCollection([new GradientStop(Color.FromArgb(255uy, 60uy, 180uy, 60uy), 0.)
                                                  new GradientStop(Color.FromArgb(255uy, 80uy, 80uy, 80uy), 0.4)
                                                  new GradientStop(Color.FromArgb(255uy, 80uy, 80uy, 80uy), 0.6)
                                                  new GradientStop(Color.FromArgb(255uy, 180uy, 60uy, 60uy), 1.0)
                                                 ])
    let blocker_brush = new LinearGradientBrush(blocker_gsc, Point(0.,0.), Point(1.,1.))
    let makeBlockerBox(dungeonIndex, blockerIndex) =
        let make() =
            let c = new Canvas(Width=30., Height=30., Background=Brushes.Black, IsHitTestVisible=true)
            let rect = new Shapes.Rectangle(Width=30., Height=30., Stroke=Brushes.Gray, StrokeThickness=3.0, IsHitTestVisible=false)
            let redraw(n) = 
                c.Children.Clear()
                match n with
                | TrackerModel.DungeonBlocker.MAYBE_LADDER 
                | TrackerModel.DungeonBlocker.MAYBE_RECORDER
                | TrackerModel.DungeonBlocker.MAYBE_BAIT
                | TrackerModel.DungeonBlocker.MAYBE_BOMB
                | TrackerModel.DungeonBlocker.MAYBE_BOW_AND_ARROW
                | TrackerModel.DungeonBlocker.MAYBE_KEY
                | TrackerModel.DungeonBlocker.MAYBE_MONEY
                    -> rect.Stroke <- blocker_brush
                | TrackerModel.DungeonBlocker.NOTHING -> rect.Stroke <- Brushes.Gray
                | _ -> rect.Stroke <- Brushes.LightGray
                c.Children.Add(rect) |> ignore
                canvasAdd(c, Graphics.blockerCurrentBMP(n) , 3., 3.)
                c
            c, redraw
        let c,redraw = make()
        let mutable current = TrackerModel.DungeonBlocker.NOTHING
        redraw(current) |> ignore
        TrackerModel.DungeonBlockersContainer.AnyBlockerChanged.Add(fun _ ->
            current <- TrackerModel.DungeonBlockersContainer.GetDungeonBlocker(dungeonIndex, blockerIndex)
            redraw(current) |> ignore
            )
        let SetNewValue(db) = TrackerModel.DungeonBlockersContainer.SetDungeonBlocker(dungeonIndex, blockerIndex, db)
        let activate(activationDelta) =
            popupIsActive <- true
            let pc, predraw = make()
            let popupRedraw(n) =
                let innerc = predraw(n)
                let s = HotKeys.BlockerHotKeyProcessor.AppendHotKeyToDescription(n.DisplayDescription(), n)
                let text = new TextBox(Text=s, Foreground=Brushes.Orange, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.),
                                            FontSize=16., HorizontalContentAlignment=HorizontalAlignment.Center)
                let textBorder = new Border(BorderThickness=Thickness(3.), Child=text, Background=Brushes.Black, BorderBrush=Brushes.Gray)
                let dp = new DockPanel(LastChildFill=false)
                DockPanel.SetDock(textBorder, Dock.Right)
                dp.Children.Add(textBorder) |> ignore
                Canvas.SetTop(dp, 30.)
                Canvas.SetRight(dp, 120.)
                innerc.Children.Add(dp) |> ignore
            let pos = c.TranslatePoint(Point(), appMainCanvas)
            let canBeBlocked(db:TrackerModel.DungeonBlocker) =
                match db.HardCanonical() with
                | TrackerModel.DungeonBlocker.LADDER -> not TrackerModel.playerComputedStateSummary.HaveLadder
                | TrackerModel.DungeonBlocker.RECORDER -> not TrackerModel.playerComputedStateSummary.HaveRecorder
                | TrackerModel.DungeonBlocker.BOW_AND_ARROW -> not (TrackerModel.playerComputedStateSummary.HaveBow && TrackerModel.playerComputedStateSummary.ArrowLevel > 0)
                | TrackerModel.DungeonBlocker.KEY -> not TrackerModel.playerComputedStateSummary.HaveAnyKey
                | _ -> true
            async {
                let! r = CustomComboBoxes.DoModalGridSelect(cm, pos.X, pos.Y, pc, TrackerModel.DungeonBlocker.All |> Array.map (fun db ->
                                (if db=TrackerModel.DungeonBlocker.NOTHING then upcast Canvas() else upcast Graphics.blockerCurrentBMP(db)), canBeBlocked(db), db), 
                                System.Array.IndexOf(TrackerModel.DungeonBlocker.All, current), activationDelta, (4, 4, 24, 24), -90., 30., popupRedraw,
                                (fun (_ea,db) -> CustomComboBoxes.DismissPopupWithResult(db)), [], CustomComboBoxes.ModalGridSelectBrushes.Defaults(), true, None)
                match r with
                | Some(db) -> SetNewValue(db)
                | None -> () 
                popupIsActive <- false
                } |> Async.StartImmediate
        c.MouseWheel.Add(fun x -> if not popupIsActive then activate(if x.Delta<0 then 1 else -1))
        c.MouseDown.Add(fun _ -> if not popupIsActive then activate(0))
        c.MyKeyAdd(fun ea -> 
            if not popupIsActive then
                match HotKeys.BlockerHotKeyProcessor.TryGetValue(ea.Key) with
                | Some(db) -> 
                    ea.Handled <- true
                    if current = db then
                        SetNewValue(TrackerModel.DungeonBlocker.NOTHING)    // idempotent hotkeys behave as a toggle
                    else
                        SetNewValue(db)
                | None -> ()
            )
        c

    let blockerColumnWidth = int((appMainCanvas.Width-BLOCKERS_AND_NOTES_OFFSET)/3.)
    let blockerGrid = makeGrid(3, 3, blockerColumnWidth, 38)
    let blockerHighlightBrush = new SolidColorBrush(Color.FromRgb(50uy, 70uy, 50uy))
    blockerGrid.Height <- float(38*3)
    for i = 0 to 2 do
        for j = 0 to 2 do
            if i=0 && j=0 then
                let d = new DockPanel(LastChildFill=false, Background=Brushes.Black)
                let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text="BLOCKERS", Width=float blockerColumnWidth, IsHitTestVisible=false,
                                        VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.), TextAlignment=TextAlignment.Center)
                d.ToolTip <- "The icons you set in this area can remind you of what blocked you in a dungeon.\nFor example, a ladder represents being ladder blocked, or a sword means you need better weapons.\nSome reminders will trigger when you get the item that may unblock you."
                ToolTipService.SetPlacement(d, Primitives.PlacementMode.Top)
                d.Children.Add(tb) |> ignore
                d.MouseEnter.Add(fun _ -> blockersHoverEvent.Trigger(true))
                d.MouseLeave.Add(fun _ -> blockersHoverEvent.Trigger(false))
                gridAdd(blockerGrid, d, i, j)
            else
                let dungeonIndex = (3*j+i)-1
                let labelChar = if TrackerModel.IsHiddenDungeonNumbers() then "ABCDEFGH".[dungeonIndex] else "12345678".[dungeonIndex]
                let d = new DockPanel(LastChildFill=true)
                levelTabSelected.Publish.Add(fun level -> if level=dungeonIndex+1 then d.Background <- blockerHighlightBrush else d.Background <- Brushes.Black)
                let sp = new StackPanel(Orientation=Orientation.Horizontal)
                let tb = new TextBox(Foreground=Brushes.Orange, Background=Brushes.Black, FontSize=12., Text=sprintf "%c" labelChar, Width=10., Height=14., IsHitTestVisible=false,
                                        VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, BorderThickness=Thickness(0.),
                                        TextAlignment=TextAlignment.Center, Margin=Thickness(2.,0.,0.,0.))
                sp.Children.Add(tb) |> ignore
                for i = 0 to TrackerModel.DungeonBlockersContainer.MAX_BLOCKERS_PER_DUNGEON-1 do
                    sp.Children.Add(makeBlockerBox(dungeonIndex, i)) |> ignore
                d.Children.Add(sp) |> ignore
                gridAdd(blockerGrid, d, i, j)
                blockerDungeonSunglasses.[dungeonIndex] <- upcast sp // just reduce its opacity
    canvasAdd(appMainCanvas, blockerGrid, BLOCKERS_AND_NOTES_OFFSET, START_DUNGEON_AND_NOTES_AREA_H) 
    blockerGrid

let MakeZoneOverlay(appMainCanvas, overworldCanvas:Canvas, mirrorOverworldFEs:ResizeArray<FrameworkElement>, oiglOFFSET) =
    // zone overlay
    let owMapZoneColorCanvases, owMapZoneBlackCanvases =
        let avg(c1:System.Drawing.Color, c2:System.Drawing.Color) = System.Drawing.Color.FromArgb((int c1.R + int c2.R)/2, (int c1.G + int c2.G)/2, (int c1.B + int c2.B)/2)
        let toBrush(c:System.Drawing.Color) = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B))
        let colors = 
            dict [
                'M', avg(System.Drawing.Color.Pink, System.Drawing.Color.Crimson) |> toBrush
                'L', System.Drawing.Color.BlueViolet |> toBrush
                'R', System.Drawing.Color.LightSeaGreen |> toBrush
                'H', System.Drawing.Color.Gray |> toBrush
                'C', System.Drawing.Color.LightBlue |> toBrush
                'G', avg(System.Drawing.Color.LightSteelBlue, System.Drawing.Color.SteelBlue) |> toBrush
                'D', System.Drawing.Color.Orange |> toBrush
                'F', System.Drawing.Color.LightGreen |> toBrush
                'S', System.Drawing.Color.DarkGray |> toBrush
                'W', System.Drawing.Color.Brown |> toBrush
            ]
        let imgs,darks = Array2D.zeroCreate 16 8, Array2D.zeroCreate 16 8
        for x = 0 to 15 do
            for y = 0 to 7 do
                imgs.[x,y] <- new Canvas(Width=OMTW, Height=float(11*3), Background=colors.Item(OverworldData.owMapZone.[y].[x]), IsHitTestVisible=false)
                darks.[x,y] <- new Canvas(Width=OMTW, Height=float(11*3), Background=Brushes.Black, IsHitTestVisible=false)
        imgs, darks
    let owMapZoneGrid = makeGrid(16, 8, int OMTW, 11*3)
    let allOwMapZoneColorCanvases,allOwMapZoneBlackCanvases = Array2D.zeroCreate 16 8, Array2D.zeroCreate 16 8
    for i = 0 to 15 do
        for j = 0 to 7 do
            let zcc,zbc = owMapZoneColorCanvases.[i,j], owMapZoneBlackCanvases.[i,j]
            zcc.Opacity <- 0.0
            zbc.Opacity <- 0.0
            allOwMapZoneColorCanvases.[i,j] <- zcc
            allOwMapZoneBlackCanvases.[i,j] <- zbc
            gridAdd(owMapZoneGrid, zcc, i, j)
            gridAdd(owMapZoneGrid, zbc, i, j)
    canvasAdd(overworldCanvas, owMapZoneGrid, 0., 0.)

    let owMapZoneBoundaries = ResizeArray()
    let makeLine(x1, x2, y1, y2) = 
        let line = new System.Windows.Shapes.Line(X1=OMTW*float(x1), X2=OMTW*float(x2), Y1=float(y1*11*3), Y2=float(y2*11*3), Stroke=Brushes.White, StrokeThickness=3.)
        line.IsHitTestVisible <- false // transparent to mouse
        line
    let addLine(x1,x2,y1,y2) = 
        let line = makeLine(x1,x2,y1,y2)
        line.Opacity <- 0.0
        owMapZoneBoundaries.Add(line)
        canvasAdd(overworldCanvas, line, 0., 0.)
    addLine(0,7,2,2)
    addLine(7,11,1,1)
    addLine(7,7,1,2)
    addLine(10,10,0,1)
    addLine(11,11,0,2)
    addLine(8,14,2,2)
    addLine(14,14,0,2)
    addLine(6,6,2,3)
    addLine(4,4,3,4)
    addLine(2,2,4,5)
    addLine(1,1,5,7)
    addLine(0,1,7,7)
    addLine(1,4,5,5)
    addLine(2,4,4,4)
    addLine(4,6,3,3)
    addLine(4,7,6,6)
    addLine(7,12,5,5)
    addLine(9,10,4,4)
    addLine(7,10,3,3)
    addLine(7,7,2,3)
    addLine(10,10,3,4)
    addLine(9,9,4,7)
    addLine(7,7,5,6)
    addLine(4,4,5,6)
    addLine(5,5,6,8)
    addLine(6,6,6,8)
    addLine(11,11,5,8)
    addLine(9,15,7,7)
    addLine(12,12,3,5)
    addLine(13,13,2,3)
    addLine(8,8,2,3)
    addLine(12,14,3,3)
    addLine(14,15,4,4)
    addLine(15,15,4,7)
    addLine(14,14,3,4)

    let zoneNames = ResizeArray()  // added later, to be top of z-order
    let addZoneName(hz, name, x, y) =
        let tb = new TextBox(Text=name,FontSize=16.,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(2.),IsReadOnly=true)
        mirrorOverworldFEs.Add(tb)
        canvasAdd(overworldCanvas, tb, x*OMTW, y*11.*3.)
        tb.Opacity <- 0.
        tb.TextAlignment <- TextAlignment.Center
        tb.FontWeight <- FontWeights.Bold
        tb.IsHitTestVisible <- false
        zoneNames.Add(hz,tb)

    let changeZoneOpacity(hintZone,show) =
        let noZone = hintZone=TrackerModel.HintZone.UNKNOWN
        if show then
            if noZone then 
                allOwMapZoneColorCanvases |> Array2D.iteri (fun _x _y zcc -> zcc.Opacity <- 0.3)
            owMapZoneBoundaries |> Seq.iter (fun x -> x.Opacity <- 0.9)
            zoneNames |> Seq.iter (fun (hz,textbox) -> if noZone || hz=hintZone then textbox.Opacity <- 0.6)
        else
            allOwMapZoneColorCanvases |> Array2D.iteri (fun _x _y zcc -> zcc.Opacity <- 0.0)
            owMapZoneBoundaries |> Seq.iter (fun x -> x.Opacity <- 0.0)
            zoneNames |> Seq.iter (fun (_hz,textbox) -> textbox.Opacity <- 0.0)
    let zone_checkbox = new CheckBox(Content=new TextBox(Text="Zones",FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true))
    zone_checkbox.IsChecked <- System.Nullable.op_Implicit false
    zone_checkbox.Checked.Add(fun _ -> changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,true))
    zone_checkbox.Unchecked.Add(fun _ -> changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,false))
    zone_checkbox.MouseEnter.Add(fun _ -> if not zone_checkbox.IsChecked.HasValue || not zone_checkbox.IsChecked.Value then changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,true))
    zone_checkbox.MouseLeave.Add(fun _ -> if not zone_checkbox.IsChecked.HasValue || not zone_checkbox.IsChecked.Value then changeZoneOpacity(TrackerModel.HintZone.UNKNOWN,false))
    canvasAdd(appMainCanvas, zone_checkbox, oiglOFFSET+200., 52.)

    zone_checkbox, addZoneName, changeZoneOpacity, allOwMapZoneBlackCanvases

open OverworldMapTileCustomization

let MakeMouseHoverExplainer(appMainCanvas:Canvas) =
    let mouseHoverExplainerIcon = new Button(Content=(Graphics.greyscale(Graphics.question_marks_bmp) |> Graphics.BMPtoImage))
    canvasAdd(appMainCanvas, mouseHoverExplainerIcon, 540., 0.)
    let c = new Canvas(Width=appMainCanvas.Width, Height=THRU_MAIN_MAP_AND_ITEM_PROGRESS_H, Opacity=0., IsHitTestVisible=false)
    canvasAdd(appMainCanvas, c, 0., 0.)
    let darkenTop = new Canvas(Width=OMTW*16., Height=150., Background=Brushes.Black, Opacity=0.40)
    canvasAdd(c, darkenTop, 0., 0.)
    let darkenOW = new Canvas(Width=OMTW*16., Height=11.*3.*8., Background=Brushes.Black, Opacity=0.85)
    canvasAdd(c, darkenOW, 0., 150.)
    let darkenBottom = new Canvas(Width=OMTW*16., Height=THRU_MAIN_MAP_AND_ITEM_PROGRESS_H - THRU_MAIN_MAP_H, Background=Brushes.Black, Opacity=0.40)
    canvasAdd(c, darkenBottom, 0., 150.+11.*3.*8.)

    let desc = new TextBox(Text="Mouse Hover Explainer",FontSize=30.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true)
    canvasAdd(c, desc, 450., 370.)

    let delayedDescriptions = ResizeArray()
    let mkTxt(text) = new TextBox(Text=text,FontSize=14.0,Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(1.0),IsReadOnly=true)
    let addLabel(poly:Shapes.Polyline, text, x, y) =
        poly.Points.Add(Point(x,y))
        canvasAdd(c, poly, 0., 0.)
        delayedDescriptions.Add(c, mkTxt(text), x, y)

    let ST = 2.0
    let COL = Brushes.Green
    let triforces = 
        if TrackerModel.IsHiddenDungeonNumbers() then
            new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,58.; 268.,58.; 268.,32.; 528.,32.; 528.,2.; 2.,2.; 2.,58. ] |> Seq.map Point ))
        else
            new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,58.; 268.,58.; 268.,32.; 2.,32.; 2.,58. ] |> Seq.map Point ))
    addLabel(triforces, "Show location of dungeon, if known or hinted", 10., 300.)

    let COL = Brushes.MediumVioletRed
    let dx,dy = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.WHITE_SWORD_ICON)
    let whiteSword = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,28.; 28.,28.; 28.,2.; 2.,2.; 2.,28. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(whiteSword, "Show location of white sword cave, if known or hinted", 30., 270.)

    let COL = Brushes.CornflowerBlue
    let dx,dy = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.ARMOS_ICON)
    let armos = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,28.; 28.,28.; 28.,2.; 2.,2.; 2.,28. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(armos, "Show locations of any unmarked armos", 120., 240.)

    let dx,dy = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.WOOD_ARROW_BOX)
    let shopping = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,28.; 98.,28.; 98.,2.; 58.,2.; 58.,-28.; 32.,-28.; 32.,2.; 2.,2.; 2.,28. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(shopping, "Show locations of shops containing each item", 400., 240.)

    let COL = Brushes.MediumVioletRed
    let dx,dy = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.BLUE_CANDLE_BOX)
    let shopping = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,28.; 28.,28.; 28.,2.; 2.,2.; 2.,28. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(shopping, "If have no candle, show locations of shops with blue candle\nElse show unmarked burnable bush locations", 380., 270.)

    let COL = Brushes.Green
    let dx,dy = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.MAGS_BOX)
    let magsAndWoodSword = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,28.; 58.,28.; 58.,2.; 2.,2.; 2.,28. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(magsAndWoodSword, "Show locations of magical/wood sword caves, if known or hinted", 300., 210.)

    let dx,dy = OW_ITEM_GRID_LOCATIONS.Locate(OW_ITEM_GRID_LOCATIONS.HEARTS)
    let hearts = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,28.; 118.,28.; 118.,2.; 2.,2.; 2.,28. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    hearts.Points.Add(Point(270.,170.))
    canvasAdd(c, hearts, 0., 0.)
    let desc = mkTxt("Show locations of potion shops\nand un-taken Take Anys")
    desc.TextAlignment <- TextAlignment.Right
    Canvas.SetRight(desc, c.Width-270.)
    Canvas.SetTop(desc, 170.)
    c.Children.Add(desc) |> ignore

    let openCaves = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 542.,138.; 558.,138.; 558.,122.; 542.,122.; 542.,138. ] |> Seq.map Point))
    addLabel(openCaves, "Show locations of unmarked open caves", 430., 180.)

    let COL = Brushes.MediumVioletRed
    let zonesEtAl = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 550.,50.; 475.,50.; 476.,92.; 436.,92.; 436.,130.; 535.,130.; 535.,116.; 550.,116.; 550.,50. ] |> Seq.map Point))
    addLabel(zonesEtAl, "As described", 600., 150.)

    let spotSummary = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 614.,115.; 725.,115.; 725.,90.; 614.,90.; 614.,115.; 600.,150. ] |> Seq.map Point))
    canvasAdd(c, spotSummary, 0., 0.)

    let COL = Brushes.MediumVioletRed
    let dx,dy = ITEM_PROGRESS_FIRST_ITEM+25., THRU_MAP_AND_LEGEND_H
    let candle = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,2.; 2.,28.; 28.,28.; 28.,2.; 2.,2. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    candle.Points.Add(Point(120.,410.))
    canvasAdd(c, candle, 0., 0.)
    let desc = mkTxt("Show Burnables")
    Canvas.SetRight(desc, c.Width-120.)
    Canvas.SetTop(desc, 390.)
    c.Children.Add(desc) |> ignore
    let COL = Brushes.CornflowerBlue
    let dx,dy = ITEM_PROGRESS_FIRST_ITEM+25.+7.*30., THRU_MAP_AND_LEGEND_H-2.
    let others = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 2.,2.; 2.,28.; 118.,28.; 118.,2.; 2.,2. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    others.Points.Add(Point(330.,405.))
    canvasAdd(c, others, 0., 0.)
    let desc = mkTxt("Show Ladderable/Recorderable/\nPowerBraceletable/Raftable")
    desc.TextAlignment <- TextAlignment.Right
    Canvas.SetRight(desc, c.Width-330.)
    Canvas.SetTop(desc, 370.)
    c.Children.Add(desc) |> ignore
    let COL = Brushes.MediumVioletRed
    let dx,dy = LEFT_OFFSET + 4.8*OMTW + 15., THRU_MAIN_MAP_H + 3.
    let recorderDest = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 13.,2.; 2.,2.; 2.,25.; 13.,25.; 13.,2. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    recorderDest.Points.Add(Point(330.,340.))
    canvasAdd(c, recorderDest, 0., 0.)
    let desc = mkTxt("Show recorder destinations")
    Canvas.SetRight(desc, c.Width-330.)
    Canvas.SetTop(desc, 340.)
    c.Children.Add(desc) |> ignore
    let COL = Brushes.Green
    let dx,dy = LEFT_OFFSET + 7.1*OMTW + 15., THRU_MAIN_MAP_H + 3.
    let anyRoad = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 13.,2.; 2.,2.; 2.,25.; 13.,25.; 13.,2. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(anyRoad, "Show Any Roads", 430., 340.)
    let COL = Brushes.MediumVioletRed
    let dx,dy = BLOCKERS_AND_NOTES_OFFSET+70., START_DUNGEON_AND_NOTES_AREA_H
    let blockers = new Shapes.Polyline(Stroke=COL, StrokeThickness=ST, Points=new PointCollection( [ 0.,0.; -70.,0.; -70.,36.; 50.,36.; 50.,0.; 0.,0. ] |> Seq.map (fun (x,y) -> Point(dx+x,dy+y))))
    addLabel(blockers, "Highlight potential\ndungeon continuations", 570., 320.)

    for dd in delayedDescriptions do   // ensure these draw atop all the PolyLines
        canvasAdd(dd)

    mouseHoverExplainerIcon.MouseEnter.Add(fun _ -> 
        c.Opacity <- 1.0
        )
    mouseHoverExplainerIcon.MouseLeave.Add(fun _ -> 
        c.Opacity <- 0.0
        )
