#r "WindowsBase.dll"
#r "PresentationCore.dll"
#r "PresentationFramework.dll"
#r "System.Xaml.dll"
#r "UIAutomationTypes.dll"

(*  every time the mouse moves, I want to have the first letter of the sentence
    move to follow it, and then after a short delay have the second letter move 
    to follow it, and then after a short delay the next letter, and so on.  
    Since this little loop-over-each-character runs asynchronously, we don’t jam up the UI *)
open System.Windows 
open System.Windows.Controls 
open System.Windows.Media 

type MyWindow() as this = 
    inherit Window()   

    let WIDTH = 20.0
    let canvas = new Canvas(Width=800.0, Height=400.0, Background = Brushes.White) 
    let chars = 
        "Triangle .Net UG is awsome!"
        |> Seq.map (fun c -> 
            new TextBlock(Width=WIDTH, Height=30.0, FontSize=20.0, Text=string c, 
                          Foreground=Brushes.Black, Background=Brushes.White))
        |> Seq.toArray 
    do
        let title = sprintf "Mouse Move Sample - x = %d"
        this.Content <- canvas 
        this.Topmost <- true
        this.Title <- title 0
        this.SizeToContent <- SizeToContent.WidthAndHeight  
        for tb in chars do                                     
            canvas.Children.Add(tb) |> ignore 

        this.MouseMove // Event-Combinators
        |> Event.map (fun ea -> ea.GetPosition(this))
        |> Event.filter (fun p -> p.X < 300.0)
        |> Event.add (fun p -> 
            async {
                this.Title <- title (int p.X)
                for i in 0..chars.Length-1 do
                    do! Async.Sleep(90)
                    Canvas.SetTop(chars.[i], p.Y)
                    Canvas.SetLeft(chars.[i], p.X + float i*WIDTH)
            } |> Async.StartImmediate 
            (*  Async.StartImmediate to start an asynchronous 
                computation on the current thread. Often, 
                an asynchronous operation needs to update UI, 
                which should always be done on the UI thread. 
                When your asynchronous operation needs to begin 
                by updating UI, Async.StartImmediate is a better choice *)
        ) 

[<System.STAThread()>] 
do  
    let app =  new Application() 
    app.Run(new MyWindow()) |> ignore 