module UIHelpers

type NotTooFrequently(timespan) =
    let mutable recentThunk = None
    do
        let timer = new System.Windows.Threading.DispatcherTimer()
        timer.Interval <- timespan
        timer.Tick.Add(fun _ -> 
            if recentThunk.IsSome then
                recentThunk.Value()
                recentThunk <- None
            )
        timer.Start()
    member this.SendThunk(thunk) =
        recentThunk <- Some thunk
