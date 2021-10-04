module SpeechRecognition

open System
open System.Windows.Controls

let speechRecognizer = new System.Speech.Recognition.SpeechRecognitionEngine()

type SpeechRecognitionInstance(kind:TrackerModel.DungeonTrackerInstanceKind) =
    let wakePhrase = "tracker set"
    let mapStatePhrases = 
        match kind with
        | TrackerModel.DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS ->
            dict [|
                "level"             , 0   // 0 1 2 3 4 5 6 7
                "level nine"        , 8
                "any road"          , 12  // 9 10 11 12
                "sword three"       , 13
                "sword two"         , 14
                "sword one"         , 15
                "arrow shop"        , 16
                "bomb shop"         , 17
                "book shop"         , 18
                "candle shop"       , 19
                "blue ring shop"    , 20
                "meat shop"         , 21
                "key shop"          , 22
                "shield shop"       , 23
                "arm owes"          , 24
                "hint shop"         , 25
                "take any"          , 26
                "potion shop"       , 27
                "money"             , 28
                |]
        | TrackerModel.DungeonTrackerInstanceKind.DEFAULT ->
            dict [|
                "level one"         , 0
                "level two"         , 1
                "level three"       , 2
                "level four"        , 3
                "level five"        , 4
                "level six"         , 5
                "level seven"       , 6
                "level eight"       , 7
                "level nine"        , 8
                "any road"          , 12  // 9 10 11 12
                "sword three"       , 13
                "sword two"         , 14
                "sword one"         , 15
                "arrow shop"        , 16
                "bomb shop"         , 17
                "book shop"         , 18
                "candle shop"       , 19
                "blue ring shop"    , 20
                "meat shop"         , 21
                "key shop"          , 22
                "shield shop"       , 23
                "arm owes" (*armos*), 24
                "hint shop"         , 25
                "take any"          , 26
                "potion shop"       , 27
                "money"             , 28
                |]
    do
        speechRecognizer.LoadGrammar(new System.Speech.Recognition.Grammar(let gb = new System.Speech.Recognition.GrammarBuilder(wakePhrase) in gb.Append(new System.Speech.Recognition.Choices(mapStatePhrases.Keys |> Seq.toArray)); gb))
    member _this.ConvertSpokenPhraseToMapCell(phrase:string) =
        let phrase = phrase.Substring(wakePhrase.Length+1)
        let newState = mapStatePhrases.[phrase]
        if newState = 12 then // any road
            if   TrackerModel.mapSquareChoiceDomain.CanAddUse( 9) then Some 9
            elif TrackerModel.mapSquareChoiceDomain.CanAddUse(10) then Some 10
            elif TrackerModel.mapSquareChoiceDomain.CanAddUse(11) then Some 11
            elif TrackerModel.mapSquareChoiceDomain.CanAddUse(12) then Some 12
            else None
        else
            if kind = TrackerModel.DungeonTrackerInstanceKind.HIDE_DUNGEON_NUMBERS && newState = 0 then
                if   TrackerModel.mapSquareChoiceDomain.CanAddUse(0) then Some 0
                elif TrackerModel.mapSquareChoiceDomain.CanAddUse(1) then Some 1
                elif TrackerModel.mapSquareChoiceDomain.CanAddUse(2) then Some 2
                elif TrackerModel.mapSquareChoiceDomain.CanAddUse(3) then Some 3
                elif TrackerModel.mapSquareChoiceDomain.CanAddUse(4) then Some 4
                elif TrackerModel.mapSquareChoiceDomain.CanAddUse(5) then Some 5
                elif TrackerModel.mapSquareChoiceDomain.CanAddUse(6) then Some 6
                elif TrackerModel.mapSquareChoiceDomain.CanAddUse(7) then Some 7
                else None
            else
                if TrackerModel.mapSquareChoiceDomain.CanAddUse(newState) then
                    Some newState
                else
                    None
    member _this.AttachSpeechRecognizedToApp(appMainCanvas:Canvas, whenRecognizedFunc) =
        speechRecognizer.SpeechRecognized.Add(fun r ->
            if TrackerModel.Options.ListenForSpeech.Value then 
                if not(TrackerModel.Options.RequirePTTForSpeech.Value) ||                                               // if PTT not required, or
                        Gamepad.IsLeftShoulderButtonDown() ||                                                           // if PTT is currently held, or
                        (DateTime.Now - Gamepad.LeftShoulderButtonMostRecentRelease) < TimeSpan.FromSeconds(1.0) then   // if PTT was just recently released
                    //printfn "conf: %3.3f  %s" r.Result.Confidence r.Result.Text                                         // then we want to use speech
                    let threshold =
                        if TrackerModel.Options.RequirePTTForSpeech.Value then
                            0.90f   // empirical tests suggest this confidence threshold is good enough to avoid false positives with PTT
                        else
                            0.94f   // empirical tests suggest this confidence threshold is good to avoid false positives
                    if r.Result.Confidence > threshold then  
                        appMainCanvas.Dispatcher.Invoke(fun () -> whenRecognizedFunc r.Result.Text)
            )



