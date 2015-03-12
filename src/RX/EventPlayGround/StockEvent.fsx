#load "..\Utilities\AsyncHelpers.fs"
#load "..\Utilities\show-wpf40.fsx"
open System
open System.Drawing
open System.Windows.Forms
open System.Threading
open System.IO
open System.Windows.Forms
open AsyncHelpers
    
type StockInfo = 
    { Date : DateTime
      Open : float
      High : float
      Low : float
      Close : float
      Volume : int
      AdjClose : float }

let currentDirectory = __SOURCE_DIRECTORY__
let csvFile = File.ReadLines(Path.Combine(currentDirectory, "..\Data\\table.csv"))

let csv = 
    csvFile
    |> Seq.toList
    |> List.tail
    |> List.map (fun c -> c.Split(','))
    |> List.map 
           (fun c -> 
           {    Date = DateTime.Parse(c.[0]); Open = float (c.[1])
                High = float (c.[2]); Low = float (c.[3])
                Close = float (c.[4]); Volume = int (c.[5]) 
                AdjClose = float (c.[6]) })

let stockInfoEvent = new Event<StockInfo>()

stockInfoEvent.Publish  |> Observable.filter(fun f -> f.Close <= f.Open)
                        |> Observable.map(fun f -> sprintf "Closing value %f was bigger the Open Value %f" f.Close f.Open)
                        |> Observable.subscribe(fun f -> showAC f)

let gate = System.Threading.Semaphore.Gate(2)

csv |> List.iter(fun f -> async {   use! holder = gate.Aquire()
                                    do! Async.Sleep  100
                                    stockInfoEvent.Trigger f } |> Async.StartImmediate)


