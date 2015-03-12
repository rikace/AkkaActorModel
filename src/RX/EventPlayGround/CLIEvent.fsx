#light

open System

type Delegate = delegate of obj * System.EventArgs -> unit

type MyEvent() =
    let myEvent = new Event<Delegate, System.EventArgs>()

    [<CLIEventAttribute>]
    member this.Event = myEvent.Publish

    member this.Raise(args) = myEvent.Trigger(this, args)



////////////////////////

type EventValue(x:int) as this =
    inherit EventArgs()
    member this.Value = x

type EvDelegate = delegate of obj * EventValue -> unit

[<CLIEventAttribute>]
let valueChanged = new Event<EvDelegate, EventValue>()

let evInt = new Event<int>()

valueChanged.Publish.Add(fun x -> ())

evInt.Publish |> Observable.filter(fun n -> n % 2 = 0) 
              |> Observable.add(fun x -> printfn "number %d is even" x)
// using Observable we can write all the event processing  as a single pipeline using higher order funtions
evInt.Trigger 40
evInt.Trigger 4
evInt.Trigger 3

let even, odd = evInt.Publish |> Observable.partition(fun n -> n % 2 = 0)

even |> Observable.filter((<) 10) |> Observable.subscribe(fun x -> printfn "number %d is even and > 10" x)
even |> Observable.filter((>=) 10) |> Observable.subscribe(fun x -> printfn "number %d is even and <= 10" x)
odd |> Observable.subscribe(fun x -> printfn "number %d is odd" x)

Observable.merge even odd
|> Observable.scan (+) 0
|> Observable.add(fun n -> printfn "current sum %d" n)

evInt.Trigger 40
evInt.Trigger 4
evInt.Trigger 3


let even', odd' = valueChanged.Publish 
                |> Observable.map (fun n -> n.Value) 
                |> Observable.partition(fun n -> n % 2 = 0)
even' |> Observable.filter((<) 10) |> Observable.subscribe(fun x -> printfn "number %d is even and > 10" x)
even' |> Observable.filter((>=) 10) |> Observable.subscribe(fun x -> printfn "number %d is even and <= 10" x)
odd' |> Observable.subscribe(fun x -> printfn "number %d is odd" x)
Observable.merge even' odd'
|> Observable.scan (+) 0
|> Observable.add(fun n -> printfn "current sum %d" n)

valueChanged.Trigger(null, new EventValue(9))