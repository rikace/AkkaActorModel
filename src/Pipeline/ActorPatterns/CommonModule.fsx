namespace Common

[<AutoOpenAttribute>]
module HelperModule =

    open System
    open System.Net
    open System.IO
    open System.Threading

    type Agent<'T> = MailboxProcessor<'T>

//    let (<->) (m:'a Agent) msg = m.PostAndReply(fun replyChannel -> msg replyChannel)
//    let (<-!) (m:'a Agent) msg = m.PostAndAsyncReply(fun replyChannel -> msg replyChannel)
//    let (<--) (m:'a Agent) msg = m.Post msg
    let (<--) (m:Agent<_>) msg = m.Post msg
    let (<->) (m:Agent<_>) msg = m.PostAndReply(fun replyChannel -> msg replyChannel)
    let (<-!) (m: Agent<_>) msg = m.PostAndAsyncReply(fun replyChannel -> msg replyChannel)


    type Result<'TSuccess,'TFailure> = 
    | Success of 'TSuccess
    | Failure of 'TFailure

    let bind switchFunction twoTrackInput = 
        match twoTrackInput with
        | Success s -> switchFunction s
        | Failure f -> Failure f

    let (<||>) first second = async { 
        let! results = Async.Parallel([|first; second|]) 
        return (results.[0], results.[1]) }

[<AutoOpen>]
module TimeMeasurement =

    /// Stops the runtime for a given function
    let stopTime f = 
        let sw = new System.Diagnostics.Stopwatch()
        sw.Start()
        let result = f()
        sw.Stop()
        result,float sw.ElapsedMilliseconds

    /// Stops the average runtime for a given function and applies it the given count
    let stopAverageTime count f = 
        let sw = new System.Diagnostics.Stopwatch()
        let list = [1..count]
        sw.Start()
        let results = List.map (fun _ -> f()) list
        sw.Stop()
        results,float sw.ElapsedMilliseconds / float count

    /// Stops the average runtime for a given function and applies it the given count
    /// Afterwards it reports it with the given description
    let stopAndReportAvarageTime count desc f =
        let results,time = stopAverageTime count f
        printfn "%s %Ams" desc time
        results,time

    /// Stops the average runtime for the given functions
    /// Afterwards it reports it with the given descriptions
    let compareTwoRuntimes count desc1 f1 desc2 f2 =
        let _,time1 = stopAndReportAvarageTime count desc1 f1
        let _,time2 = stopAndReportAvarageTime count desc2 f2

        printfn "  Ratio:  %A" (time1 / time2)
 
    let benchmark f = 
        let m_gen0Start = System.GC.CollectionCount(0)
        let m_gen1Start = System.GC.CollectionCount(1)
        let m_gen2Start = System.GC.CollectionCount(2)
        let sw = System.Diagnostics.Stopwatch.StartNew()
        
        try
            f()
        finally  
            let m_gen0Start' = System.GC.CollectionCount(0) - m_gen0Start
            let m_gen1Start' = System.GC.CollectionCount(1) - m_gen1Start
            let m_gen2Start' = System.GC.CollectionCount(2) - m_gen2Start
                              
            let result = System.String.Format("Time {0,7:N0}ms\tGC(G0={1,4}, G1={2,4}, G2={3,4})",
                                sw.Elapsed.TotalMilliseconds, m_gen0Start', m_gen1Start', m_gen2Start')
            printfn "%s" result 

    type OperationTimer(label) =
        let label = defaultArg label ""
        let m_gen0Start = System.GC.CollectionCount(0)
        let m_gen1Start = System.GC.CollectionCount(1)
        let m_gen2Start = System.GC.CollectionCount(2)
        let sw = System.Diagnostics.Stopwatch.StartNew()
        
        static member PrepareForOperation() =
            System.GC.Collect()
            System.GC.WaitForPendingFinalizers()
            System.GC.Collect()

        interface System.IDisposable with
            member x.Dispose() = 
                    let m_gen0Start' = System.GC.CollectionCount(0) - m_gen0Start
                    let m_gen1Start' = System.GC.CollectionCount(1) - m_gen1Start
                    let m_gen2Start' = System.GC.CollectionCount(2) - m_gen2Start
                              
                    let result = System.String.Format("{0}\tTime {1,7:N0}ms\tGC(G0={2,4}, G1={3,4}, G2={4,4})",
                                     label, sw.Elapsed.TotalMilliseconds, m_gen0Start', m_gen1Start', m_gen2Start')
                    printfn "%s" result
                

[<AutoOpen>]
module Async =
    let inline AwaitPlainTask (task: System.Threading.Tasks.Task) = 
        // rethrow exception from preceding task if it fauled
        let continuation (t : System.Threading.Tasks.Task) : unit =
            match t.IsFaulted with
            | true -> raise t.Exception
            | arg -> ()
        task.ContinueWith continuation |> Async.AwaitTask
 
    let inline StartAsPlainTask (work : Async<unit>) = System.Threading.Tasks.Task.Factory.StartNew(fun () -> work |> Async.RunSynchronously)
 
    let inline Parallel2 (job1, job2) = async {
            let! task1 = Async.StartChild job1
            let! task2 = Async.StartChild job2
            let! res1 = task1
            let! res2 = task2
            return (res1, res2) }

    let inline Parallel3 (job1, job2, job3) = async {
            let! task1 = Async.StartChild job1
            let! task2 = Async.StartChild job2
            let! task3 = Async.StartChild job3
            let! res1 = task1
            let! res2 = task2
            let! res3 = task3
            return (res1, res2, res3) }
    
[<AutoOpen>]
 module Task =
    let getData(uri:string) =
        Async.StartAsTask <|
        async { let request = System.Net.WebRequest.Create uri
                use! response = request.AsyncGetResponse()
                return [    use stream = response.GetResponseStream()
                            use reader = new System.IO.StreamReader(stream)
                            while not reader.EndOfStream
                                do yield reader.ReadLine() ] }
[<AutoOpen>]
module ArrayUtils =

    open System
    open System.Drawing
    open System.Runtime.InteropServices

    //-----------------------------------------------------------------------------
    // Implements a fast unsafe conversion from 2D array to a bitmap 
      
    /// Converts array to a bitmap using the provided conversion function,
    /// which converts value from the array to a color value
    let toBitmap f (arr:_[,]) =
      // Create bitmap & lock it in the memory, so that we can access it directly
      let bmp = new Bitmap(arr.GetLength(0), arr.GetLength(1))
      let rect = new Rectangle(0, 0, bmp.Width, bmp.Height)
      let bmpData = 
        bmp.LockBits
          (rect, Imaging.ImageLockMode.ReadWrite, 
           Imaging.PixelFormat.Format32bppArgb)
       
      // Using pointer arithmethic to copy all the bits
      let ptr0 = bmpData.Scan0 
      let stride = bmpData.Stride
      for i = 0 to bmp.Width - 1 do
        for j = 0 to bmp.Height - 1 do
          let offset = i*4 + stride*j
          let clr = (f(arr.[i,j]) : Color).ToArgb()
          Marshal.WriteInt32(ptr0, offset, clr)
  
      bmp.UnlockBits(bmpData)
      bmp


module StartPhotoViewer = 
    let start() = System.Diagnostics.Process.Start(@"C:\Git\AkkaActorModel\src\Pipeline\PhotoViewer\bin\Release\PhotoViewer.exe")

    