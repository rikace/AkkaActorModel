open System
open System.Threading

type RequestGate(n : int) =
    let semaphore = new Semaphore(initialCount = n, maximumCount = n)
    member x.AsyncAcquire(?timeout) =
        async {let! ok = Async.AwaitWaitHandle(semaphore,
                                               ?millisecondsTimeout = timeout)
               if ok then
                   return
                     {new System.IDisposable with
                         member x.Dispose() =
                             semaphore.Release() |> ignore}
               else
                   return! failwith "couldn't acquire a semaphore" }


let using (ie : #System.IDisposable) f =
    try f(ie)
    finally ie.Dispose()
//val using : ie:'a -> f:('a -> 'b) -> 'b when 'a :> System.IDisposable


