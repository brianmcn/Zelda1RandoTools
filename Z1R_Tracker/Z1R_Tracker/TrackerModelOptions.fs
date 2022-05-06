module TrackerModelOptions

open System.Text.Json
open System.Text.Json.Serialization

type IntentionalApplicationShutdown(msg) =
    inherit System.Exception(msg)

type Bool(init) =
    let mutable v = init
    member this.Value with get() = v and set(x) = v <- x
module Overworld =
    let mutable DrawRoutes = Bool(true)
    let mutable RoutesCanScreenScroll = Bool(false)
    let mutable HighlightNearby = Bool(true)
    let mutable ShowMagnifier = Bool(true)
    let mutable MirrorOverworld = Bool(false)
    let mutable ShopsFirst = Bool(true)
module VoiceReminders =
    let mutable DungeonFeedback = Bool(true)
    let mutable SwordHearts = Bool(true)
    let mutable CoastItem = Bool(true)
    let mutable RecorderPBSpotsAndBoomstickBook = Bool(false)
    let mutable HaveKeyLadder = Bool(true)
    let mutable Blockers = Bool(true)
    let mutable DoorRepair = Bool(true)
module VisualReminders =
    let mutable DungeonFeedback = Bool(true)
    let mutable SwordHearts = Bool(true)
    let mutable CoastItem = Bool(true)
    let mutable RecorderPBSpotsAndBoomstickBook = Bool(false)
    let mutable HaveKeyLadder = Bool(true)
    let mutable Blockers = Bool(true)
    let mutable DoorRepair = Bool(true)
let mutable AnimateTileChanges = Bool(true)
let mutable SaveOnCompletion = Bool(false)
let mutable SnoopSeedAndFlags = Bool(false)
let mutable DisplaySeedAndFlags = Bool(true)
let mutable ListenForSpeech = Bool(false)
let mutable RequirePTTForSpeech = Bool(false)
let mutable PlaySoundWhenUseSpeech = Bool(true)
let mutable BOARDInsteadOfLEVEL = Bool(false)
let mutable IsSecondQuestDungeons = Bool(false)
let mutable ShowBasementInfo = Bool(true)
let mutable DoDoorInference = Bool(false)
let mutable BookForHelpfulHints = Bool(false)
let mutable ShowBroadcastWindow = Bool(false)
let mutable BroadcastWindowSize = 3
let mutable BroadcastWindowIncludesOverworldMagnifier = Bool(false)
let mutable SmallerAppWindow = Bool(false)
let mutable SmallerAppWindowScaleFactor = 2.0/3.0
let mutable IsMuted = false
let mutable Volume = 30
let mutable MainWindowLT = ""
let mutable BroadcastWindowLT = ""
let mutable HotKeyWindowLTWH = ""
let mutable OverlayLocatorWindowLTWH = ""

type ReadWrite() =
    member val DrawRoutes = true with get,set
    member val RoutesCanScreenScroll = false with get,set
    member val HighlightNearby = true with get,set
    member val ShowMagnifier = true with get,set
    member val MirrorOverworld = false with get,set
    member val ShopsFirst = false with get,set

    member val Voice_DungeonFeedback = true with get,set
    member val Voice_SwordHearts = true with get,set
    member val Voice_CoastItem = true with get,set
    member val Voice_RecorderPBSpotsAndBoomstickBook = true with get,set
    member val Voice_HaveKeyLadder = true with get,set
    member val Voice_Blockers = true with get,set
    member val Voice_DoorRepair = true with get,set

    member val Visual_DungeonFeedback = true with get,set
    member val Visual_SwordHearts = true with get,set
    member val Visual_CoastItem = true with get,set
    member val Visual_RecorderPBSpotsAndBoomstickBook = true with get,set
    member val Visual_HaveKeyLadder = true with get,set
    member val Visual_Blockers = true with get,set
    member val Visual_DoorRepair = true with get,set
        
    member val AnimateTileChanges = true with get,set
    member val SaveOnCompletion = false with get,set
    member val SnoopSeedAndFlags = false with get,set
    member val DisplaySeedAndFlags = true with get,set
    member val ListenForSpeech = false with get,set
    member val RequirePTTForSpeech = false with get,set
    member val PlaySoundWhenUseSpeech = true with get,set
    member val BOARDInsteadOfLEVEL = false with get,set
    member val IsSecondQuestDungeons = false with get,set
    member val ShowBasementInfo = true with get,set
    member val DoDoorInference = false with get,set
    member val BookForHelpfulHints = false with get,set
    member val ShowBroadcastWindow = false with get,set
    member val BroadcastWindowSize = 3 with get,set
    member val BroadcastWindowIncludesOverworldMagnifier = false with get,set
    member val SmallerAppWindow = false with get,set
    member val SmallerAppWindowScaleFactor = 2.0/3.0 with get,set

    member val IsMuted = false with get, set
    member val Volume = 30 with get, set
    member val MainWindowLT = "" with get,set
    member val BroadcastWindowLT = "" with get,set
    member val HotKeyWindowLTWH = "" with get, set
    member val OverlayLocatorWindowLTWH = "" with get, set

let mutable private cachedSettingJson = null

let private writeImpl(filename) =
    let data = ReadWrite()
    data.DrawRoutes <- Overworld.DrawRoutes.Value
    data.RoutesCanScreenScroll <- Overworld.RoutesCanScreenScroll.Value
    data.HighlightNearby <- Overworld.HighlightNearby.Value
    data.ShowMagnifier <- Overworld.ShowMagnifier.Value
    data.MirrorOverworld <- Overworld.MirrorOverworld.Value
    data.ShopsFirst <- Overworld.ShopsFirst.Value

    data.Voice_DungeonFeedback <- VoiceReminders.DungeonFeedback.Value
    data.Voice_SwordHearts <-     VoiceReminders.SwordHearts.Value
    data.Voice_CoastItem <-       VoiceReminders.CoastItem.Value
    data.Voice_RecorderPBSpotsAndBoomstickBook <- VoiceReminders.RecorderPBSpotsAndBoomstickBook.Value
    data.Voice_HaveKeyLadder <-   VoiceReminders.HaveKeyLadder.Value
    data.Voice_Blockers <-        VoiceReminders.Blockers.Value
    data.Voice_DoorRepair <-      VoiceReminders.DoorRepair.Value

    data.Visual_DungeonFeedback <- VisualReminders.DungeonFeedback.Value
    data.Visual_SwordHearts <-     VisualReminders.SwordHearts.Value
    data.Visual_CoastItem <-       VisualReminders.CoastItem.Value
    data.Visual_RecorderPBSpotsAndBoomstickBook <- VisualReminders.RecorderPBSpotsAndBoomstickBook.Value
    data.Visual_HaveKeyLadder <-   VisualReminders.HaveKeyLadder.Value
    data.Visual_Blockers <-        VisualReminders.Blockers.Value
    data.Visual_DoorRepair <-      VisualReminders.DoorRepair.Value

    data.AnimateTileChanges <- AnimateTileChanges.Value
    data.SaveOnCompletion <- SaveOnCompletion.Value
    data.SnoopSeedAndFlags <- SnoopSeedAndFlags.Value
    data.DisplaySeedAndFlags <- DisplaySeedAndFlags.Value
    data.ListenForSpeech <- ListenForSpeech.Value
    data.RequirePTTForSpeech <- RequirePTTForSpeech.Value
    data.PlaySoundWhenUseSpeech <- PlaySoundWhenUseSpeech.Value
    data.BOARDInsteadOfLEVEL <- BOARDInsteadOfLEVEL.Value
    data.IsSecondQuestDungeons <- IsSecondQuestDungeons.Value
    data.ShowBasementInfo <- ShowBasementInfo.Value
    data.DoDoorInference <- DoDoorInference.Value
    data.BookForHelpfulHints <- BookForHelpfulHints.Value
    data.ShowBroadcastWindow <- ShowBroadcastWindow.Value
    data.BroadcastWindowSize <- BroadcastWindowSize
    data.BroadcastWindowIncludesOverworldMagnifier <- BroadcastWindowIncludesOverworldMagnifier.Value
    data.SmallerAppWindow <- SmallerAppWindow.Value
    data.SmallerAppWindowScaleFactor <- SmallerAppWindowScaleFactor
    data.IsMuted <- IsMuted
    data.Volume <- Volume
    data.MainWindowLT <- MainWindowLT
    data.BroadcastWindowLT <- BroadcastWindowLT
    data.HotKeyWindowLTWH <- HotKeyWindowLTWH
    data.OverlayLocatorWindowLTWH <- OverlayLocatorWindowLTWH

    let json = JsonSerializer.Serialize<ReadWrite>(data, new JsonSerializerOptions(WriteIndented=true))
    if json <> cachedSettingJson then
        cachedSettingJson <- json
        System.IO.File.WriteAllText(filename, cachedSettingJson)

let private write(filename) =
    try
        writeImpl(filename)
    with e ->
        printfn "failed to write settings file '%s':" filename
        printfn "%s" (e.ToString())
        printfn ""

let private read(filename) =
    try
        cachedSettingJson <- System.IO.File.ReadAllText(filename)
        let data = JsonSerializer.Deserialize<ReadWrite>(cachedSettingJson, new JsonSerializerOptions(AllowTrailingCommas=true))
        Overworld.DrawRoutes.Value <- data.DrawRoutes
        Overworld.RoutesCanScreenScroll.Value <- data.RoutesCanScreenScroll
        Overworld.HighlightNearby.Value <- data.HighlightNearby
        Overworld.ShowMagnifier.Value <- data.ShowMagnifier
        Overworld.MirrorOverworld.Value <- data.MirrorOverworld
        Overworld.ShopsFirst.Value <- data.ShopsFirst    

        VoiceReminders.DungeonFeedback.Value <- data.Voice_DungeonFeedback
        VoiceReminders.SwordHearts.Value <-     data.Voice_SwordHearts
        VoiceReminders.CoastItem.Value <-       data.Voice_CoastItem
        VoiceReminders.RecorderPBSpotsAndBoomstickBook.Value <- data.Voice_RecorderPBSpotsAndBoomstickBook
        VoiceReminders.HaveKeyLadder.Value <-   data.Voice_HaveKeyLadder
        VoiceReminders.Blockers.Value <-        data.Voice_Blockers
        VoiceReminders.DoorRepair.Value <-      data.Voice_DoorRepair

        VisualReminders.DungeonFeedback.Value <- data.Visual_DungeonFeedback
        VisualReminders.SwordHearts.Value <-     data.Visual_SwordHearts
        VisualReminders.CoastItem.Value <-       data.Visual_CoastItem
        VisualReminders.RecorderPBSpotsAndBoomstickBook.Value <- data.Visual_RecorderPBSpotsAndBoomstickBook
        VisualReminders.HaveKeyLadder.Value <-   data.Visual_HaveKeyLadder
        VisualReminders.Blockers.Value <-        data.Visual_Blockers
        VisualReminders.DoorRepair.Value <-      data.Visual_DoorRepair

        AnimateTileChanges.Value <- data.AnimateTileChanges
        SaveOnCompletion.Value <- data.SaveOnCompletion
        SnoopSeedAndFlags.Value <- data.SnoopSeedAndFlags
        DisplaySeedAndFlags.Value <- data.DisplaySeedAndFlags
        ListenForSpeech.Value <- data.ListenForSpeech
        RequirePTTForSpeech.Value <- data.RequirePTTForSpeech
        PlaySoundWhenUseSpeech.Value <- data.PlaySoundWhenUseSpeech
        BOARDInsteadOfLEVEL.Value <- data.BOARDInsteadOfLEVEL
        IsSecondQuestDungeons.Value <- data.IsSecondQuestDungeons
        ShowBasementInfo.Value <- data.ShowBasementInfo
        DoDoorInference.Value <- data.DoDoorInference
        BookForHelpfulHints.Value <- data.BookForHelpfulHints
        ShowBroadcastWindow.Value <- data.ShowBroadcastWindow
        BroadcastWindowSize <- max 1 (min 3 data.BroadcastWindowSize)
        BroadcastWindowIncludesOverworldMagnifier.Value <- data.BroadcastWindowIncludesOverworldMagnifier
        SmallerAppWindow.Value <- data.SmallerAppWindow
        SmallerAppWindowScaleFactor <- data.SmallerAppWindowScaleFactor
        IsMuted <- data.IsMuted
        Volume <- max 0 (min 100 data.Volume)
        MainWindowLT <- data.MainWindowLT
        BroadcastWindowLT <- data.BroadcastWindowLT
        HotKeyWindowLTWH <- data.HotKeyWindowLTWH
        OverlayLocatorWindowLTWH <- data.OverlayLocatorWindowLTWH
    with e ->
        cachedSettingJson <- null
        printfn "Unable to read settings file '%s':" filename 
        if System.IO.File.Exists(filename) then
            printfn "That file does exist on disk, but could not be read.  Here is the error detail:"
            printfn "%s" (e.ToString())
            printfn ""
            printfn "If you were intentionally hand-editing the .json settings file, you may have made a mistake that must be corrected."
            printfn ""
            printfn "The application will shut down now. If you want to discard the broken settings file and start %s fresh with some default settings, then simply delete the file:" OverworldData.ProgramNameString
            printfn ""
            printfn "%s" filename
            printfn ""
            printfn "from disk before starting the application again."
            printfn ""
            raise <| IntentionalApplicationShutdown "Intentional application shutdown: failed to read settings file data."
        else
            printfn "Perhaps this is your first time using '%s' on this machine?" OverworldData.ProgramNameString
            printfn "A default settings file will be created for you..."
            try
                writeImpl(filename)
                printfn "... The settings file has been successfully created."
            with e ->
                printfn "... Failed to write settings file.  Perhaps there is an issue with the file location?"
                printfn "'Filename'='%s'" filename
                printfn "Here is more information about the problem:"
                printfn "%s" (e.ToString())
                printfn ""

let mutable private settingsFile = null
    
let readSettings() =
    if settingsFile = null then
        settingsFile <- System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Z1R_Tracker_settings.json")
    read(settingsFile)
let writeSettings() =
    if settingsFile = null then
        settingsFile <- System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Z1R_Tracker_settings.json")
    write(settingsFile)


