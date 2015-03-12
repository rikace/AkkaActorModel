(*  Observable.add 
        simply subscribes to the event. Typically this method 
        is used at the end of a series of pipe-forward operations 
        and is a cleaner way to subscribe to events than calling 
        AddHandler on the event object
        
    Observable.filter 
        function creates an event that’s triggered when the source 
        event produces a value that matches the given predicate

    Observable.merge
        Observable.merge takes two input events and produces a 
        single output event, which will be fired whenever either 
        of its input events is raised.

    Observable.map 
        allows you to convert an event with a given argument 
        type into another.      *)

open System
open System.Drawing
open System.Windows.Forms
open System.Threading
open System.IO
open System.Windows.Forms

let fillellipseform = new Form(Text="Draw with Obseravbles", Visible=true, TopMost=true, Width=500, Height=500)  
fillellipseform.BackColor<-Color.Gray

let exitbutton=new Button(Top=0,Left=0)
exitbutton.Height <- 30
exitbutton.Width <- 100
exitbutton.Text<-"X"  
exitbutton.BackColor<-Color.Ivory
fillellipseform.Controls.Add(exitbutton)  

let crgraphics=fillellipseform.CreateGraphics()  

let (observableEventLeft, observableEventRight) =   
    fillellipseform.MouseDown  
    |> Observable.merge fillellipseform.MouseMove
    |> Observable.filter(fun ev -> ev.Button = MouseButtons.Left)
    |> Observable.partition(fun ev -> ev.X > (fillellipseform.Width /2))

let obsEvent1Disposable = observableEventLeft |> Observable.subscribe(fun move->crgraphics.FillEllipse(Brushes.Green,new Rectangle(move.X,move.Y,10,10)))                                                                                                                 
let obsEvent2Disposable = observableEventRight |> Observable.subscribe(fun move->crgraphics.FillEllipse(Brushes.Red,new Rectangle(move.X,move.Y,10,10)))                                                                                                                 

exitbutton.Click.Add(fun _-> obsEvent1Disposable.Dispose()
                             obsEvent2Disposable.Dispose())

//~~~~~~~~~~~~~~~~ OBSERVABLE CPU USAGE   ~~~~~~~~~~~~~~~~~~~~~~

#load "..\Utilities\ChartBase.fsx"

let cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total", true)

let creatTimer interval f = 
    let timer = new System.Timers.Timer()
    timer.Interval <- interval    
    timer.Start()
    timer.Elapsed |> Observable.map f

let chart = ChartBase.ChartHelper.createCPUChart()

let disposable = creatTimer 250. (fun _ -> cpuCounter.NextValue()) 
                 |> Observable.subscribe(fun x -> chart.Points.Add(float(x)) |> ignore)

disposable.Dispose()
