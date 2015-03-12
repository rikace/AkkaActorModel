#load "..\Utilities\AsyncHelpers.fs"
//#load "..\Utilities\show-wpf40.fsx"
open System
open System.Drawing
open System.Windows.Forms
open System.Threading
open System.IO
open System.Windows.Forms
open AsyncHelpers

let fnt = new Font("Calibri", 24.0f)
let lbl = new System.Windows.Forms.Label(Dock = DockStyle.Fill, 
                                         TextAlign = ContentAlignment.MiddleCenter, 
                                         Font = fnt)

let form = new System.Windows.Forms.Form(ClientSize = Size(200, 100), Visible = true)
do form.Controls.Add(lbl)

let regsiter(ev) =  
    ev   
    |> Event.map (fun _ -> DateTime.Now) // Create events carrying the current time
    |> Event.scan (fun (_, dt : DateTime) ndt -> // Remembers the last time click was accepted
           if ((ndt - dt).TotalSeconds > 2.0) then // When the time is more than a second...
               (4, ndt)
           else (1, dt)) (0, DateTime.Now) // .. we return 1 and the new current time
    |> Event.map fst
    |> Event.scan (+) 0 // Sum the yielded numbers 
    |> Event.map (sprintf "Clicks: %d") // Format the output as a string 
    |> Event.add lbl.set_Text // Display the result...    

regsiter(lbl.MouseDown)

//  asynchronous loop
let rec loop (count) = 
    async { 
        // Wait for the next click
        let! ev = Async.AwaitEvent(lbl.MouseDown)
        lbl.Text <- sprintf "Clicks: %d" count
        do! Async.Sleep(1000)        
        return! loop (count + 1)
    }
let start = Async.StartImmediate(loop (1))
form.Show()

/////////////////////////////////////////////////////////////////////// 


let process' = new System.Diagnostics.ProcessStartInfo("ping.exe", "-t -n 3 127.0.0.1")
process'.UseShellExecute <- false
process'.RedirectStandardOutput <- true

let mProcess = new System.Diagnostics.Process()
mProcess.StartInfo <- process'
mProcess.EnableRaisingEvents <- true

mProcess.OutputDataReceived 
    |> Event.map (fun p -> p.Data) 
    |> Event.add(fun s -> printfn "Data from Process: %s" s)

mProcess.Start();
mProcess.BeginOutputReadLine()



//////////  TIMER -> EVENT -> OBSERVABLE //////////////

type Log ={ label:int; time:DateTime }

let createTimerAndObservable timerInterval =
    // setup a timer
    let timer = new System.Timers.Timer(float timerInterval)
    timer.AutoReset <- true

    // events are automatically IObservable
    let observable = timer.Elapsed  

    // return an async task
    let task = async {
        timer.Start()
        do! Async.Sleep 5000
        timer.Stop()
        }

    // return a async task and the observable
    (task,observable)

let areSimultaneous (earlierEvent,laterEvent) =
    let {label=_;time=t1} = earlierEvent
    let {label=_;time=t2} = laterEvent
    t2.Subtract(t1).Milliseconds < 50


// create the event streams and raw observables
let timer3, timerEventStream3 = createTimerAndObservable 300
let timer5, timerEventStream5 = createTimerAndObservable 500

// convert the time events into FizzBuzz events with the appropriate id
let eventStream3  = timerEventStream3  
                    |> Observable.map (fun _ -> {label=3; time=DateTime.Now})

let eventStream5  = timerEventStream5  
                    |> Observable.map (fun _ -> {label=5; time=DateTime.Now})

// combine the two streams
let combinedStream = Observable.merge eventStream3 eventStream5
 
// make pairs of events
let pairwiseStream = combinedStream |> Observable.pairwise
 
// split the stream based on whether the pairs are simultaneous
let simultaneousStream, nonSimultaneousStream = 
   pairwiseStream |> Observable.partition areSimultaneous

// split the non-simultaneous stream based on the id
let fizzStream, buzzStream  =
    nonSimultaneousStream  
    // convert pair of events to the first event
    |> Observable.map (fun (ev1,_) -> ev1)
    // split on whether the event id is three
    |> Observable.partition (fun {label=id} -> id=3)

//print events from the combinedStream
combinedStream 
|> Observable.subscribe (fun {label=id;time=t} -> 
                              printf "[%i] %i.%03i " id t.Second t.Millisecond)
 
//print events from the simultaneous stream
simultaneousStream 
|> Observable.subscribe (fun _ -> printfn "FizzBuzz")

//print events from the nonSimultaneous streams
fizzStream 
|> Observable.subscribe (fun _ -> printfn "Fizz")

buzzStream 
|> Observable.subscribe (fun _ -> printfn "Buzz")

// run the two timers at the same time
[timer3;timer5]
|> Async.Parallel
|> Async.RunSynchronously