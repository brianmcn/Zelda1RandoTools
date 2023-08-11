module ShowRunCustom

open System.Windows

// When Windows minimizes, or is closing a window, it moves it to -32000,-32000 and WPF reports those values as Left and Top.
// However, on a high-DPI device, that number gets scaled, e.g. at 1.75, you see -18285 get reported.
// Since I am using the value as a way to detect 'junk' coordinates, there's am issue deciding which coordinates are junk versus
// large and negative real.
// In practice, it seems today devices are unlikely to have a DPI scale of more than 2.0, which means -16000 would be a useful cutoff.
// So if a Left/Top coordinate is less than this value, assume junk:
let MINIMIZED_THRESHOLD = -15999.



let MakeDefaultShowRunCustomFile(filename:string) =
    System.IO.File.WriteAllText(filename, sprintf """
# %s ShowRunCustom file

# Blank lines, or lines that start with '#', are comments and are ignored by the parser

# Other lines must have one of these formats:
#     SHOW "FullPathToImageFileName"
#     nnn,nnn,nnn,nnn SHOW "FullPathToImageFileName"
#     RUN "FullPathToExe" CommandLineArguments
#     RUN "FullUrlToAWebPage"
# FullPathToImageFileName should be an image file, such as a .jpg or .png, which will be displayed in a window
# nnn,nnn,nnn,nnn are the Left, Top, Width, Height of that window (to optionally save window size/location)
# FullPathToExe should be an executable file like .exe
# CommandLineArguments are extra text passed on the command line to the Exe
# FullUrlToAWebPage should start with http:// or https:// and will open in your default browser

# All of the SHOW and RUN lines will be activated when you click the 'Show/Run Custom' button in the app.

# Below are some sample commands you might find useful.  You can activate them by removing the comment
# character '#' before RUN or SHOW, saving this file, and then clicking 'Show/Run Custom' in the app.

# Vanilla 1Q and 2Q maps
# RUN "https://z1r.fandom.com/wiki/Maps"

# The Zelda in-game inventory display completely full of items - useful reference when playing 'sprite shuffle'
# SHOW ".\ShowRunCustomImages\all-items-hud.png"

# A picture showing 'rooms that never drop in 1Q/Shapes dungeons', as well as enemy drop tables
# SHOW ".\ShowRunCustomImages\enemy-drop-table-and-rooms-that-never-drop.png"

# A textual version of the 'rooms that never drop in 1Q/Shapes dungeons'
# SHOW ".\ShowRunCustomImages\rooms-that-never-drop-text.png"
        """ OverworldData.ProgramNameString)

[<RequireQualifiedAccess>]
type Line =
    | BLANK
    | COMMENT of string
    | SHOW of string * (float*float*float*float) option
    | RUN of string * string
    member this.AsString() =
        match this with
        | Line.BLANK -> ""
        | Line.COMMENT s -> sprintf "#%s" s
        | Line.SHOW(s,None) -> sprintf "SHOW \"%s\"" s
        | Line.SHOW(s,Some(l,t,w,h)) -> sprintf "%d,%d,%d,%d SHOW \"%s\"" (int l) (int t) (int w) (int h) s
        | Line.RUN(c,a) -> sprintf "RUN \"%s\" %s" c a

let ParseShowRunCustomFile(filename:string) =
    let lines = System.IO.File.ReadAllLines(filename)
    let emptyLineRegex = new System.Text.RegularExpressions.Regex("^\s*$", System.Text.RegularExpressions.RegexOptions.None)
    let commentRegex = new System.Text.RegularExpressions.Regex("^#(.*)$", System.Text.RegularExpressions.RegexOptions.None)
    let showRegex = new System.Text.RegularExpressions.Regex("""^\s*SHOW\s*"(.*)"\s*$""", System.Text.RegularExpressions.RegexOptions.None)
    let ltwhShowRegex = new System.Text.RegularExpressions.Regex("""^\s*(-?\d+),(-?\d+),(\d+),(\d+)\s*SHOW\s*"(.*)"\s*$""", System.Text.RegularExpressions.RegexOptions.None)
    let runRegex = new System.Text.RegularExpressions.Regex("""^\s*RUN\s*"(.*)"\s*(.*)$""", System.Text.RegularExpressions.RegexOptions.None)
    let data = ResizeArray()
    let mutable lineNumber = 1
    for line in lines do
        if emptyLineRegex.IsMatch(line) then
            data.Add(Line.BLANK)
        else
            let m = commentRegex.Match(line) 
            if m.Success then
                let comment = m.Groups.[1].Value
                data.Add(Line.COMMENT comment)
            else
                let m = showRegex.Match(line) 
                if m.Success then
                    let img = m.Groups.[1].Value
                    data.Add(Line.SHOW(img,None))
                else
                    let m = ltwhShowRegex.Match(line) 
                    if m.Success then
                        let ltwh = Some(float m.Groups.[1].Value, float m.Groups.[2].Value, float m.Groups.[3].Value, float m.Groups.[4].Value)
                        let img = m.Groups.[5].Value
                        data.Add(Line.SHOW(img,ltwh))
                    else
                        let m = runRegex.Match(line) 
                        if m.Success then
                            let comm = m.Groups.[1].Value
                            let args = m.Groups.[2].Value
                            data.Add(Line.RUN(comm,args))
                        else
                            let fullText = sprintf "Error parsing '%s', line %d\n\n" filename lineNumber +
                                                    "You should fix this error by editing the text file.\n" + 
                                                    "Or you can delete it, and pressing the button again will create an empty template file in its place." 
                            raise <| new HotKeys.UserError(fullText)
        lineNumber <- lineNumber + 1
    data

let WriteShowRunCustomFile(filename:string, data:Line[]) = System.IO.File.WriteAllLines(filename, data |> Array.map (fun x -> x.AsString()))

let ShowRunCustomFilename = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ShowRunCustom.txt")

let DoShowRunCustom(refocusMainWindow) =
    let displayInfoOrError(titleKind, message) =
        let tb = new System.Windows.Controls.TextBox(FontSize=16., Foreground=System.Windows.Media.Brushes.Orange, Background=System.Windows.Media.Brushes.Black, 
                                IsReadOnly=true, BorderThickness=Thickness(0.), Text=message, IsHitTestVisible=false)
        let window = new Window(Title=sprintf "Z-Tracker %s" titleKind, Owner=Application.Current.MainWindow, Content=tb, SizeToContent=SizeToContent.WidthAndHeight)
        window.Show()
        let fileToSelect = ShowRunCustomFilename
        let args = sprintf "/Select, \"%s\"" fileToSelect
        let psi = new System.Diagnostics.ProcessStartInfo("Explorer.exe", args)
        System.Diagnostics.Process.Start(psi) |> ignore
    let mutable currentIndexBeingInterpreted = 0
    let mutable prefixErrorMessageWithContext = fun msg -> msg
    try
        let mutable didAnything = false
        if not(System.IO.File.Exists(ShowRunCustomFilename)) then
            MakeDefaultShowRunCustomFile(ShowRunCustomFilename)
        let data = ParseShowRunCustomFile(ShowRunCustomFilename) |> Seq.toArray
        // the parser gives good errors, but if we successfully parse the file, give context if a further error appears
        prefixErrorMessageWithContext <- fun msg ->
            let lineNum = currentIndexBeingInterpreted + 1
            let lineText = data.[currentIndexBeingInterpreted].AsString()
            sprintf "While doing line %d of '%s':\n\n    %s\n\nan error occurred:\n\n%s" lineNum ShowRunCustomFilename lineText msg
        for i = 0 to data.Length - 1 do
            currentIndexBeingInterpreted <- i
            match data.[i] with
            | Line.BLANK -> ()
            | Line.COMMENT _ -> ()
            | Line.SHOW(imgFile, ltwho) ->
                if not(System.IO.File.Exists(imgFile)) then
                    raise <| new HotKeys.UserError(sprintf "The file to SHOW does not exist:\n%s" imgFile)
                let bmp = new System.Drawing.Bitmap(imgFile)
                let img = Graphics.BMPtoImage(bmp)
                let b = new System.Windows.Controls.Border(Background=new System.Windows.Media.ImageBrush(img.Source), Width=float bmp.Width, Height=float bmp.Height)
                let window = new Window(Title="Z-Tracker SHOW", Owner=Application.Current.MainWindow, Content=b)
                match ltwho with
                | Some(l,t,w,h) ->
                    window.Left <- l
                    window.Top <- t
                    window.Width <- w
                    window.Height <- h
                | _ -> 
                    window.SizeToContent <- SizeToContent.WidthAndHeight
                let cur = i
                let update() = 
                    if not(window.Left < MINIMIZED_THRESHOLD) then // minimized
                        data.[cur] <- Line.SHOW(imgFile, Some(window.Left,window.Top,window.ActualWidth,window.ActualHeight))
                        WriteShowRunCustomFile(ShowRunCustomFilename, data)
                window.SizeChanged.Add(fun _ -> update(); refocusMainWindow())
                window.LocationChanged.Add(fun _ -> update(); refocusMainWindow())
                window.Show()
                didAnything <- true
            | Line.RUN(comm,args) ->
                if not(System.IO.File.Exists(comm)) then
                    if (comm.ToLowerInvariant().StartsWith("http://") || comm.ToLowerInvariant().StartsWith("https://")) && System.Uri.IsWellFormedUriString(comm, System.UriKind.Absolute) then
                        System.Diagnostics.Process.Start(comm) |> ignore    // open a URL in default browser
                        didAnything <- true
                    else
                        raise <| new HotKeys.UserError(sprintf "The file to RUN does not exist:\n%s" comm)
                else
                    let dir = System.IO.Path.GetDirectoryName(comm)
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(
                            WorkingDirectory = dir,
                            FileName = comm,
                            Arguments = args,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                        )) |> ignore
                    didAnything <- true
        if not didAnything then
            displayInfoOrError("info", sprintf "There were no custom SHOW or RUN commands to process.\nYou can add some by editing this file:\n%s" ShowRunCustomFilename)
    with
        | :? HotKeys.UserError as ue -> 
            displayInfoOrError("error", prefixErrorMessageWithContext(ue.Message))
        | e -> 
            displayInfoOrError("error", prefixErrorMessageWithContext(e.ToString()))

