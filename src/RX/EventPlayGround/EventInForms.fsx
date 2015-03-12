#load "..\Utilities\AsyncHelpers.fs"
//#load "..\Utilities\show-wpf40.fsx"
open System
open System.Drawing
open System.Windows.Forms
open System.Threading
open System.IO
open System.Windows.Forms
open AsyncHelpers

/////////////// Async AwaitObservable
type Semaphore = 
    | Red
    | Yellow
    | Green
    | None

let formSemaphore = new System.Windows.Forms.Form(ClientSize = Size(200, 100), Visible = true)

let display = 
    function 
    | None -> formSemaphore.BackColor <- Color.Black
    | Red -> formSemaphore.BackColor <- Color.Red
    | Yellow -> formSemaphore.BackColor <- Color.Yellow
    | _ -> formSemaphore.BackColor <- Color.Green

let rec changeLightColor() = 
    async { 
        for i in [ Red; Green; Yellow ] do
            let! md = Async.AwaitObservable(formSemaphore.MouseDown)
            if md.Button = MouseButtons.Left then display (i)
        do! changeLightColor()
    }

//let rec changeLightColor2() = 
//    async { 
//        for i in [ Red; Green; Yellow ] do
//            let! md = Async.AwaitObservable
//                          (formSemaphore.MouseDown, formSemaphore.MouseUp)
//            match md with
//            | Choice1Of2(m) -> display (i)
//            | Choice2Of2(m) -> display (None)
//        do! changeLightColor2()
//    }
//
//let rec changeLightColor3() = 
//    async { 
//        let ignoreEvent e = Event.map ignore e
//        let mergedEvents = 
//            Event.merge (ignoreEvent formSemaphore.MouseDown) 
//                (ignoreEvent formSemaphore.MouseLeave)
//        for i in [ Red; Green; Yellow ] do
//            let! _ = Async.AwaitEvent(mergedEvents)
//            display (i)
//        do! changeLightColor3()
//    }
//
//let formEvent1, formEvent2 = 
//    formSemaphore.MouseMove
//    |> Event.filter (fun m -> m.Button = MouseButtons.Left)
//    |> Event.partition (fun m -> m.X > 60)
//
//let rec changeLightColor4() = 
//    async { 
//        for i in [ Red; Green; Yellow ] do
//            let! md = Async.AwaitObservable(formEvent1, formEvent2)
//            match md with
//            | Choice1Of2(m) -> display (Red)
//            | Choice2Of2(m) -> display (None)
//        do! changeLightColor4()
//    }

let startSemaphore = changeLightColor() |> Async.StartImmediate

//////////////////////// Event Lable Sample


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

//regsiter(lbl.MouseDown)

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
