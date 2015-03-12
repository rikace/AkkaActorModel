namespace EventNet

open System
open System.IO
open System.Net

module Event =

    type Delegate = delegate of obj * System.EventArgs -> unit

    type MyEvent() =
        let myEvent = new Event<Delegate, System.EventArgs>()

        [<CLIEventAttribute>]
        member this.Event = myEvent.Publish
        
        member this.Raise(args) = myEvent.Trigger(this, args)
