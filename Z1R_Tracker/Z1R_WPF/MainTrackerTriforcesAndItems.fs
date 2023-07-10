module MainTrackerTriforcesAndItems

open System
open System.Windows
open System.Windows.Controls 
open System.Windows.Media

open DungeonUI.AhhGlobalVariables
open HotKeys.MyKey
open OverworldItemGridUI
open CustomComboBoxes.GlobalFlag

let makeGhostBusterImpl(color,isForLinkRouting) =  // for marking off the third box of completed 2-item dungeons in Hidden Dungeon Numbers
    let c = new Canvas(Width=30., Height=30., Opacity=0.0, IsHitTestVisible=false)
    let circle = new Shapes.Ellipse(Width=30., Height=30., StrokeThickness=3., Stroke=color)
    let slash = new Shapes.Line(X1=30.*(1.-0.707), X2=30.*0.707, Y1=30.*0.707, Y2=30.*(1.-0.707), StrokeThickness=3., Stroke=color)
    canvasAdd(c, circle, 0., 0.)
    canvasAdd(c, slash, 0., 0.)
    if isForLinkRouting then
        let txt = new TextBox(FontSize=10., Foreground=color, Background=Brushes.Black, IsReadOnly=true, IsHitTestVisible=false, BorderThickness=Thickness(0.), Text="None\nfound")
        Canvas.SetBottom(txt,30.)
        Canvas.SetLeft(txt,0.)
        c.Children.Add(txt) |> ignore
    c
let makeGhostBuster() = makeGhostBusterImpl(Brushes.Gray, false)
let mainTrackerGhostbusters = Array.init 8 (fun _ -> makeGhostBuster())
let updateGhostBusters() =
    if TrackerModel.IsHiddenDungeonNumbers() then
        for i = 0 to 7 do
            let lc = TrackerModel.GetDungeon(i).LabelChar
            let twoItemDungeons = if TrackerModel.IsSecondQuestDungeons then "123567" else "234567"
            if twoItemDungeons.Contains(lc.ToString()) then
                mainTrackerGhostbusters.[i].Opacity <- 1.0
            else
                mainTrackerGhostbusters.[i].Opacity <- 0.0

let triforceInnerCanvases = Array.zeroCreate 8
let mainTrackerCanvases : Canvas[,] = Array2D.zeroCreate 9 5
let mainTrackerCanvasShaders : Canvas[,] = Array2D.init 8 5 (fun _i j -> new Canvas(Width=30., Height=30., Background=Brushes.Black, Opacity=(if j=1 then 0.4 else 0.3), IsHitTestVisible=false))
let timelineItems = ResizeArray()
let doUIUpdateEvent = new Event<unit>()

let setup(cm:CustomComboBoxes.CanvasManager, owInstance:OverworldData.OverworldInstance, layout:Layout.IApplicationLayoutBase, kind:TrackerModel.DungeonTrackerInstanceKind) =
    let mainTrackerGrid = makeGrid(9, 5, 30, 30)
    let mainTrackerCanvas = new Canvas()
    mainTrackerCanvas.Children.Add(mainTrackerGrid) |> ignore
    layout.AddMainTracker(mainTrackerCanvas)

    // items (we draw these before drawing triforces, as triforce display can draw slightly atop the item boxes, when there's a triforce-specific-blocker drawn)
    let boxItemImpl(tid, box:TrackerModel.Box, requiresForceUpdate) = 
        let c = Views.MakeBoxItem(cm, box)
        box.Changed.Add(fun _ -> if requiresForceUpdate then TrackerModel.forceUpdate())
        c.MouseEnter.Add(fun _ -> 
            match box.CellCurrent() with
            | 3 -> showLocatorInstanceFunc(owInstance.PowerBraceletable)
            | 4 -> showLocatorInstanceFunc(owInstance.Ladderable)
            | 7 -> showLocatorInstanceFunc(owInstance.Raftable)
            | 8 -> showLocatorInstanceFunc(owInstance.Whistleable)
            | 9 -> showLocatorInstanceFunc(owInstance.Burnable)
            | _ -> ()
            )
        c.MouseLeave.Add(fun _ -> hideLocator())
        timelineItems.Add(new Timeline.TimelineItem(tid, fun()->CustomComboBoxes.boxCurrentBMP(box.CellCurrent(), Some(tid))))
        c
    if TrackerModel.IsHiddenDungeonNumbers() then
        for i = 0 to 8 do
            for j = 0 to 2 do
                let c = new Canvas(Width=30., Height=30., Background=Brushes.Black)
                gridAdd(mainTrackerGrid, c, i, j+2)
                if j<>2 || i <> 8 then   // dungeon 9 does not have 3 items
                    canvasAdd(c, boxItemImpl(Timeline.TimelineID.LevelBox(i+1, j+1), TrackerModel.GetDungeon(i).Boxes.[j], false), 0., 0.)
                mainTrackerCanvases.[i,j+2] <- c
                if j=2 && i<> 8 then
                    canvasAdd(c, mainTrackerGhostbusters.[i], 0., 0.)
    else
        for i = 0 to 8 do
            for j = 0 to 2 do
                let c = new Canvas(Width=30., Height=30., Background=System.Windows.Media.Brushes.Black)
                gridAdd(mainTrackerGrid, c, i, j+2)
                if j=0 || j=1 || i=7 then
                    canvasAdd(c, boxItemImpl(Timeline.TimelineID.LevelBox(i+1, j+1), TrackerModel.GetDungeon(i).Boxes.[j], false), 0., 0.)
                mainTrackerCanvases.[i,j+2] <- c
    let extrasImage = Graphics.BMPtoImage Graphics.iconExtras_bmp
    extrasImage.ToolTip <- "Starting items and extra drops"
    ToolTipService.SetPlacement(extrasImage, System.Windows.Controls.Primitives.PlacementMode.Top)
    gridAdd(mainTrackerGrid, extrasImage, 8, 4)
    let IDEAL = Point(Views.IDEAL_BOX_MOUSE_X, Views.IDEAL_BOX_MOUSE_Y)
    extrasImage.MyKeyAdd(fun ea ->
        match HotKeys.GlobalHotKeyProcessor.TryGetValue(ea.Key) with
        | Some(HotKeys.GlobalHotkeyTargets.MoveCursorLeft) -> 
            ea.Handled <- true
            Graphics.NavigationallyWarpMouseCursorTo(mainTrackerCanvases.[7,4].TranslatePoint(IDEAL,cm.AppMainCanvas))
        | Some(HotKeys.GlobalHotkeyTargets.MoveCursorUp) -> 
            ea.Handled <- true
            Graphics.NavigationallyWarpMouseCursorTo(mainTrackerCanvases.[8,3].TranslatePoint(IDEAL,cm.AppMainCanvas))
        | _ -> ())
    Views.appMainCanvasGlobalBoxMouseOverHighlight.ApplyBehavior(extrasImage)
    let finalCanvasOf1Or4 = 
        if TrackerModel.IsHiddenDungeonNumbers() then
            null
        else        
            boxItemImpl(Timeline.TimelineID.Level1or4Box3, TrackerModel.DungeonTrackerInstance.TheDungeonTrackerInstance.FinalBoxOf1Or4, false)
    let toggleSecondQuestDungeonCanvas =
        if TrackerModel.IsHiddenDungeonNumbers() then
            null
        else        
            let c = new Canvas(Width=30., Height=30., Background=Graphics.freeze(new SolidColorBrush(Color.FromRgb(55uy,55uy,85uy))))
            let rect = new System.Windows.Shapes.Rectangle(Width=30., Height=30., Stroke=Brushes.White, StrokeThickness=3.0, Opacity=0.0)
            c.Children.Add(rect) |> ignore
            let pf = new PathFigure(Point(0.,20.), [new BezierSegment(Point(0.,10.), Point(60.,10.), Point(60.,20.), true)], false)
            let curve = new Shapes.Path(Stroke=Brushes.White, StrokeThickness=3., IsHitTestVisible=false, Data=new PathGeometry([pf]), Opacity=0.0)
            let tb1 = new TextBox(IsHitTestVisible=false, BorderThickness=Thickness(0.), FontSize=9., Margin=Thickness(0.),
                                    VerticalContentAlignment=VerticalAlignment.Center, HorizontalContentAlignment=HorizontalAlignment.Center, 
                                    Text="click to toggle", Foreground=Brushes.White, Background=Brushes.Black, Opacity=0.0)
            let tb2 = new TextBox(IsHitTestVisible=false, BorderThickness=Thickness(0.), FontSize=9., Margin=Thickness(0.),
                                    VerticalContentAlignment=VerticalAlignment.Center, HorizontalContentAlignment=HorizontalAlignment.Center, 
                                    Text="dungeon quest", Foreground=Brushes.White, Background=Brushes.Black, Opacity=0.0)
            let explainer = new TextBox(IsHitTestVisible=false, BorderThickness=Thickness(3.), BorderBrush=Brushes.Gray, FontSize=14., Margin=Thickness(0.),
                                        Text="Usually dungeon 1 has three items and dungeon 4 has two.\n"+
                                        "However if you are playing with the 'second quest dungeons' flag\n"+
                                        "then dungeon 4 gets a third item and dungeon 1 will only have 2.\n"+
                                        "This toggle is used to decide which dungeon (1 or 4) gets the third box.", 
                                        Padding=Thickness(3.), Foreground=Brushes.Orange, Background=Brushes.Black)
            canvasAdd(mainTrackerCanvas, curve, 30., 118.)
            canvasAdd(mainTrackerCanvas, tb1, 30., 115.)
            canvasAdd(mainTrackerCanvas, tb2, 28., 137.)
            c.MouseEnter.Add(fun _ ->
                rect.Opacity <- 1.0
                curve.Opacity <- 1.0
                tb1.Opacity <- 1.0
                tb2.Opacity <- 1.0
                layout.AddTopLayerHover(explainer, 140., 54.)
                )
            c.MouseLeave.Add(fun _ ->
                rect.Opacity <- 0.0
                curve.Opacity <- 0.0
                tb1.Opacity <- 0.0
                tb2.Opacity <- 0.0
                layout.ClearTopLayerHovers()
                )
            c.MouseDown.Add(fun _ ->
                TrackerModel.IsSecondQuestDungeons <- not TrackerModel.IsSecondQuestDungeons
                rect.Opacity <- 0.0
                curve.Opacity <- 0.0
                tb1.Opacity <- 0.0
                tb2.Opacity <- 0.0
                layout.ClearTopLayerHovers()
                OptionsMenu.secondQuestDungeonsOptionChanged.Trigger()
                )
            c
    // numbered triforce display - the extra row of triforce in IsHiddenDungeonNumbers
    let updateNumberedTriforceDisplayImpl(c:Canvas,i) =
        let hasTriforce, index = TrackerModel.doesPlayerHaveTriforceAndWhichDungeonIndexIsIt(i)
        let found = if index = -1 then false else TrackerModel.GetDungeon(index).HasBeenLocated()
        let hasHint = not(found) && TrackerModel.GetLevelHint(i)<>TrackerModel.HintZone.UNKNOWN
        c.Children.Clear()
        if hasHint then
            c.Children.Add(makeHintHighlight(30.)) |> ignore
        if not hasTriforce then
            if not found then
                c.Children.Add(Graphics.BMPtoImage Graphics.emptyUnfoundNumberedTriforce_bmps.[i]) |> ignore
            else
                c.Children.Add(Graphics.BMPtoImage Graphics.emptyFoundNumberedTriforce_bmps.[i]) |> ignore
        else
            if not found then
                c.Children.Add(Graphics.BMPtoImage Graphics.fullNumberedUnfoundTriforce_bmps.[i]) |> ignore
            else
                c.Children.Add(Graphics.BMPtoImage Graphics.fullNumberedFoundTriforce_bmps.[i]) |> ignore
    let updateNumberedTriforceDisplayIfItExists =
        if TrackerModel.IsHiddenDungeonNumbers() then
            let numberedTriforceCanvases = Array.init 8 (fun _ -> new Canvas(Width=30., Height=30.))
            for i = 0 to 7 do
                let c = numberedTriforceCanvases.[i]
                layout.AddNumberedTriforceCanvas(c, i)
                c.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.DungeonNumber i))
                c.MouseLeave.Add(fun _ -> hideLocator())
            let update() =
                for i = 0 to 7 do
                    updateNumberedTriforceDisplayImpl(numberedTriforceCanvases.[i], i)
            update
        else
            fun () -> ()
    updateNumberedTriforceDisplayIfItExists()
    // triforce
    for i = 0 to 7 do
        if TrackerModel.IsHiddenDungeonNumbers() then
            // triforce dungeon color
            let colorCanvas = new Canvas(Width=28., Height=28., Background=Brushes.Black)
            //mainTrackerCanvases.[i,0] <- colorCanvas
            let colorButton = new Button(Width=30., Height=30., BorderThickness=Thickness(1.), Margin=Thickness(0.), Padding=Thickness(0.), BorderBrush=Brushes.DimGray, Content=colorCanvas)
            colorButton.Click.Add(fun _ -> 
                if not popupIsActive && TrackerModel.IsHiddenDungeonNumbers() then
                    popupIsActive <- true
                    let pos = colorButton.TranslatePoint(Point(15., 15.), cm.AppMainCanvas)
                    async {
                        do! Dungeon.HiddenDungeonCustomizerPopup(cm, i, TrackerModel.GetDungeon(i).Color, TrackerModel.GetDungeon(i).LabelChar, false, false, pos)
                        popupIsActive <- false
                        } |> Async.StartImmediate
                )
            gridAdd(mainTrackerGrid, colorButton, i, 0)
            let dungeon = TrackerModel.GetDungeon(i)
            Dungeon.HotKeyAHiddenDungeonLabel(colorCanvas, dungeon, None)
            dungeon.HiddenDungeonColorOrLabelChanged.Add(fun (color,labelChar) -> 
                colorCanvas.Background <- Graphics.freeze(new SolidColorBrush(Graphics.makeColor(color)))
                colorCanvas.Children.Clear()
                let color = if Graphics.isBlackGoodContrast(color) then System.Drawing.Color.Black else System.Drawing.Color.White
                if TrackerModel.GetDungeon(i).LabelChar <> '?' then  // ? and 7 look alike, and also it is easier to parse 'blank' as unknown/unset dungeon number
                    colorCanvas.Children.Add(Graphics.BMPtoImage(Graphics.alphaNumOnTransparentBmp(labelChar, color, 28, 28, 3, 2))) |> ignore
                )
            colorButton.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.DungeonIndex i))
            colorButton.MouseLeave.Add(fun _ -> hideLocator())
        else
            let hintCanvas = new Canvas(Width=30., Height=30., Background=Brushes.Transparent)  // Background to accept mouse input
            TrackerModel.LevelHintChanged(i).Add(fun hz -> 
                hintCanvas.Children.Clear()
                canvasAdd(hintCanvas, OverworldItemGridUI.HintZoneDisplayTextBox(if hz=TrackerModel.HintZone.UNKNOWN then "" else hz.AsDisplayTwoChars()), 3., 3.)
                )
            OverworldItemGridUI.ApplyFastHintSelectorBehavior(cm, (float(30*i),0.), hintCanvas, i, true, false)
            mainTrackerCanvases.[i,0] <- hintCanvas
            Views.appMainCanvasGlobalBoxMouseOverHighlight.ApplyBehavior(hintCanvas)
            hintCanvas.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.DungeonIndex i))
            hintCanvas.MouseLeave.Add(fun _ -> hideLocator())
            hintCanvas.MyKeyAdd(fun ea -> 
                match HotKeys.HintZoneHotKeyProcessor.TryGetValue(ea.Key) with
                | Some(hz) -> 
                    ea.Handled <- true
                    TrackerModel.SetLevelHint(i, hz)
                | _ -> ()
                )
            gridAdd(mainTrackerGrid, hintCanvas, i, 0)
        // triforce itself and label
        let c = new Canvas(Width=30., Height=30.)
        mainTrackerCanvases.[i,1] <- c
        let innerc = Views.MakeTriforceDisplayView(Some(cm),i,Some(owInstance))
        triforceInnerCanvases.[i] <- innerc
        c.Children.Add(innerc) |> ignore
        c.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.DungeonIndex i))
        c.MouseLeave.Add(fun _ -> hideLocator())
        gridAdd(mainTrackerGrid, c, i, 1)
        timelineItems.Add(new Timeline.TimelineItem(Timeline.TimelineID.Triforce(i+1), fun()->
            match kind with
            | TrackerModel.DungeonTrackerInstanceKind.DEFAULT -> Graphics.fullNumberedFoundTriforce_bmps.[i]
            | TrackerModel.DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS -> 
                if TrackerModel.GetDungeon(i).LabelChar <> '?' then
                    let num = int(TrackerModel.GetDungeon(i).LabelChar) - int '1'
                    Graphics.fullNumberedFoundTriforce_bmps.[num]
                else
                    Graphics.fullLetteredFoundTriforce_bmps.[i]
            ))
    let level9NumeralCanvas = Views.MakeLevel9View(Some(owInstance))
    gridAdd(mainTrackerGrid, level9NumeralCanvas, 8, 1) 
    mainTrackerCanvases.[8,1] <- level9NumeralCanvas
    level9NumeralCanvas.MouseEnter.Add(fun _ -> showLocator(ShowLocatorDescriptor.DungeonIndex 8))
    level9NumeralCanvas.MouseLeave.Add(fun _ -> hideLocator())
    // dungeon 9 doesn't need a color, we display a 'found summary' here instead
    let level9ColorCanvas = new Canvas(Width=30., Height=30., Background=Brushes.Black)  
    if not(TrackerModel.IsHiddenDungeonNumbers()) then
        TrackerModel.LevelHintChanged(8).Add(fun hz -> 
            level9ColorCanvas.Children.Clear()
            canvasAdd(level9ColorCanvas, OverworldItemGridUI.HintZoneDisplayTextBox(if hz=TrackerModel.HintZone.UNKNOWN then "" else hz.AsDisplayTwoChars()), 3., 3.)
            )
        OverworldItemGridUI.ApplyFastHintSelectorBehavior(cm, (float(30*8), 0.), level9ColorCanvas, 8, true, false)
        level9ColorCanvas.MyKeyAdd(fun ea -> 
            match HotKeys.HintZoneHotKeyProcessor.TryGetValue(ea.Key) with
            | Some(hz) -> 
                ea.Handled <- true
                TrackerModel.SetLevelHint(8, hz)
            | _ -> ()
            )
    gridAdd(mainTrackerGrid, level9ColorCanvas, 8, 0) 
    mainTrackerCanvases.[8,0] <- level9ColorCanvas
(*
    let foundDungeonsTB1 = new TextBox(Text="0/9", FontSize=20., Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false)
    let foundDungeonsTB2 = new TextBox(Text="found", FontSize=12., Background=Brushes.Black,Foreground=Brushes.Orange,BorderThickness=Thickness(0.0),IsReadOnly=true,IsHitTestVisible=false)
    canvasAdd(level9ColorCanvas, foundDungeonsTB1, 4., -6.)
    canvasAdd(level9ColorCanvas, foundDungeonsTB2, 4., 16.)
*)
    for i = 0 to mainTrackerCanvases.GetLength(0)-1 do
        for j = 0 to mainTrackerCanvases.GetLength(1)-1 do
            if mainTrackerCanvases.[i,j] <> null then
                mainTrackerCanvases.[i,j].MyKeyAdd(fun ea ->
                    match HotKeys.GlobalHotKeyProcessor.TryGetValue(ea.Key) with
                    | Some(HotKeys.GlobalHotkeyTargets.MoveCursorRight) -> 
                        ea.Handled <- true
                        if i<mainTrackerCanvases.GetLength(0)-1 then
                            Graphics.NavigationallyWarpMouseCursorTo(mainTrackerCanvases.[i+1,j].TranslatePoint(IDEAL,cm.AppMainCanvas))
                    | Some(HotKeys.GlobalHotkeyTargets.MoveCursorLeft) -> 
                        ea.Handled <- true
                        if i>0 then
                            Graphics.NavigationallyWarpMouseCursorTo(mainTrackerCanvases.[i-1,j].TranslatePoint(IDEAL,cm.AppMainCanvas))
                    | Some(HotKeys.GlobalHotkeyTargets.MoveCursorUp) -> 
                        ea.Handled <- true
                        if j>0 then
                            Graphics.NavigationallyWarpMouseCursorTo(mainTrackerCanvases.[i,j-1].TranslatePoint(IDEAL,cm.AppMainCanvas))
                    | Some(HotKeys.GlobalHotkeyTargets.MoveCursorDown) -> 
                        ea.Handled <- true
                        if j<4 then
                            Graphics.NavigationallyWarpMouseCursorTo(mainTrackerCanvases.[i,j+1].TranslatePoint(IDEAL,cm.AppMainCanvas))
                    | _ -> ()
                )
(*
    let updateFoundDungeonsCount() =
        let mutable r = 0
        for trackerIndex = 0 to 8 do    
            let d = TrackerModel.GetDungeon(trackerIndex)
            if d.HasBeenLocated() then
                r <- r + 1
        foundDungeonsTB1.Text <- sprintf "%d/9" r
    for trackerIndex = 0 to 8 do    
        let d = TrackerModel.GetDungeon(trackerIndex)
        d.HasBeenLocatedChanged.Add(fun _ -> updateFoundDungeonsCount())
*)
    do 
        let RedrawForSecondQuestDungeonToggle() =
            if not(TrackerModel.IsHiddenDungeonNumbers()) then
                mainTrackerCanvases.[0,4].Children.Remove(finalCanvasOf1Or4) |> ignore
                mainTrackerCanvases.[3,4].Children.Remove(finalCanvasOf1Or4) |> ignore
                mainTrackerCanvases.[0,4].Children.Remove(toggleSecondQuestDungeonCanvas) |> ignore
                mainTrackerCanvases.[3,4].Children.Remove(toggleSecondQuestDungeonCanvas) |> ignore
                if TrackerModel.IsSecondQuestDungeons then
                    canvasAdd(mainTrackerCanvases.[3,4], finalCanvasOf1Or4, 0., 0.)
                    canvasAdd(mainTrackerCanvases.[0,4], toggleSecondQuestDungeonCanvas, 0., 0.)
                else
                    canvasAdd(mainTrackerCanvases.[0,4], finalCanvasOf1Or4, 0., 0.)
                    canvasAdd(mainTrackerCanvases.[3,4], toggleSecondQuestDungeonCanvas, 0., 0.)
        RedrawForSecondQuestDungeonToggle()
        OptionsMenu.secondQuestDungeonsOptionChanged.Publish.Add(fun _ -> 
            RedrawForSecondQuestDungeonToggle()
            doUIUpdateEvent.Trigger()  // CompletedDungeons may change
            updateGhostBusters()
            )
        if TrackerModel.IsHiddenDungeonNumbers() then
            for i = 0 to 7 do
                TrackerModel.GetDungeon(i).HiddenDungeonColorOrLabelChanged.Add(fun _ -> updateGhostBusters())
    
    boxItemImpl, extrasImage, updateNumberedTriforceDisplayImpl, updateNumberedTriforceDisplayIfItExists, level9ColorCanvas
