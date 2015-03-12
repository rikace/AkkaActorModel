namespace AkkaFractalShared

open System
open System.Drawing
open System.Threading
open System.Threading.Tasks
open System.IO
open Akka
open Akka.FSharp
open Akka.Actor
open MandelbrotSet

type BitmapConverter() = 
    
    static member toByteArray (imageIn:Bitmap) =
        use mem = new MemoryStream()
        imageIn.Save(mem, System.Drawing.Imaging.ImageFormat.Png)        
        mem.ToArray()

    static member toBitmap(byteArray:byte array) =
        use mem = new MemoryStream(byteArray) 
        let image = Image.FromStream(mem)
        (image :?> Bitmap)


type RenderedTile(bytes:byte array, x:int, y:int) =

    member this.Bytes = bytes
    member this.X = x
    member this.Y = y

type RenderTile(x:int, y:int, height:int, width:int) =
    member this.X = x
    member this.Y = y
    member this.Height = height
    member this.Width = width

//type RenderTile = { X: int; Y: int; Width: int; Height: int }
//type RenderedTile = { X: int; Y: int; Bytes: byte array }

//let tileRenderer (mailbox: Actor<_>) render =
//    logDebugf mailbox "%A rendering %d , %d" mailbox.Self render.X render.Y
//    let res = Mandelbrot.Set(render.X, render.Y,render.Width,render.Height, 4000, 4000, 0.5, -2.5, 1.5, -1.5)
//    let bytes = BitmapConverter.toByteArray(res)
//    mailbox.Sender() <! { Bytes = bytes; X = render.X; Y = render.Y }



type TileRenderActor() =
    inherit  Actor()

    override this.OnReceive(message) =
        match message with
        | :? RenderTile as render -> 
           
            printfn "%A rendering %d , %d" this.Self render.X render.Y

            let res = Mandelbrot.Set(render.X, render.Y,render.Width,render.Height, 4000, 4000, 0.5, -2.5, 1.5, -1.5)
            let bytes = BitmapConverter.toByteArray(res)
            this.Sender <! (RenderedTile(bytes, render.X, render.Y))
        | _ -> ()


