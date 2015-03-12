#load "..\Utilities\AsyncHelpers.fs"
//#load "..\Utilities\show-wpf40.fsx"
open System
open System.Drawing
open System.Windows.Forms
open System.Threading
open System.IO
open System.Windows.Forms
open AsyncHelpers


////////////////////////  DRAW LINES

let form_Click (sender:obj) (e:EventArgs) =  MessageBox.Show("Clicked me!") |> ignore
let handler = new EventHandler(form_Click)
let form' = new Form(Text="Test Event", TopMost=true, Visible=true)


let pen = new System.Drawing.Pen(System.Drawing.Color.Red)
let graphics = System.Drawing.BufferedGraphicsManager.Current.Allocate(form'.CreateGraphics(), 
                                new System.Drawing.Rectangle( 0, 0, form'.Width, form'.Height ))
let (overEvent, underEvent) =
   form'.MouseMove
     |> Event.merge form'.MouseDown
     |> Event.filter (fun args -> args.Button = MouseButtons.Left)
     |> Event.map (fun args -> (args.X, args.Y))
     |> Event.partition (fun (x, y) -> x > 100 && y > 100)

// Partition Event
overEvent |> Event.add (fun (x, y) -> form'.Text <- sprintf "Over (%d, %d)" x y)
underEvent |> Event.add (fun (x, y) -> form'.Text <- sprintf "Under (%d, %d)" x y)

form'.MouseClick
    |> Event.pairwise
    |> Event.add ( fun (evArgs1, evArgs2) ->
        graphics.Graphics.DrawLine(pen, evArgs1.X, evArgs1.Y, evArgs2.X, evArgs2.Y)
        form'.Refresh())

form'.Paint
    |> Event.add(fun evArgs -> graphics.Render(evArgs.Graphics))

