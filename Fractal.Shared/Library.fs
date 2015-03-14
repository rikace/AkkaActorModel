module Fractal.Shared

open System
open System.Drawing
open System.Threading
open System.Threading.Tasks
open System.IO
open MandelbrotSet
open Akka.FSharp


type RenderedTile = {
    Bytes: byte array
    X: int
    Y: int
}

type RenderTile = {
    X: int
    Y: int
    Height: int
    Width: int
}

type BitmapConverter() = 
    
    static member toByteArray (imageIn:Bitmap) =
        use mem = new MemoryStream()
        imageIn.Save(mem, System.Drawing.Imaging.ImageFormat.Png)        
        mem.ToArray()

    static member toBitmap(byteArray:byte array) =
        use mem = new MemoryStream(byteArray) 
        let image = Image.FromStream(mem)
        (image :?> Bitmap)



let tileRenderer (mailbox: Actor<_>) render =

    logInfof mailbox "%A rendering %d , %d" mailbox.Self render.X render.Y

    let res = Mandelbrot.Set(render.X, render.Y,render.Width,render.Height, 4000, 4000, 0.5, -2.5, 1.5, -1.5)
    let bytes = BitmapConverter.toByteArray(res)
    mailbox.Sender() <! {Bytes = bytes; X = render.X; Y = render.Y}
