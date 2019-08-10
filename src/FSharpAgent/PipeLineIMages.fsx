#if INTERACTIVE
#r "../../packages/Akka/lib/netstandard1.6/Akka.dll"
#r "../../packages/Akka.FSharp/lib/netstandard2.0/Akka.FSharp.dll"
#r "./bin/Debug/netcoreapp2.2/Common.dll"

#endif

module AgentPipelines

open Microsoft.FSharp.Control
open System.Collections.Generic
open System.Threading

[<AutoOpenAttribute>]
module HelperModule =

    open System
    open System.Net
    open System.IO
    open System.Threading

    type Agent<'T> = MailboxProcessor<'T>

    let (<--) (m:Agent<_>) msg = m.Post msg
    let (<->) (m:Agent<_>) msg = m.PostAndReply(fun replyChannel -> msg replyChannel)
    let (<-!) (m: Agent<_>) msg = m.PostAndAsyncReply(fun replyChannel -> msg replyChannel)

[<AutoOpen>]
module ArrayUtils =

    open System
    open System.Drawing
    open System.Runtime.InteropServices

    //-----------------------------------------------------------------------------
    // Implements a fast unsafe conversion from 2D array to a bitmap

    /// Converts array to a bitmap using the provided conversion function,
    /// which converts value from the array to a color value
    let toBitmap f (arr:_[,]) =
      // Create bitmap & lock it in the memory, so that we can access it directly
      let bmp = new Bitmap(arr.GetLength(0), arr.GetLength(1))
      let rect = new Rectangle(0, 0, bmp.Width, bmp.Height)
      let bmpData =
        bmp.LockBits
          (rect, Imaging.ImageLockMode.ReadWrite,
           Imaging.PixelFormat.Format32bppArgb)

      // Using pointer arithmethic to copy all the bits
      let ptr0 = bmpData.Scan0
      let stride = bmpData.Stride
      for i = 0 to bmp.Width - 1 do
        for j = 0 to bmp.Height - 1 do
          let offset = i*4 + stride*j
          let clr = (f(arr.[i,j]) : Color).ToArgb()
          Marshal.WriteInt32(ptr0, offset, clr)

      bmp.UnlockBits(bmpData)
      bmp

module StartPhotoViewer =
    let start() = System.Diagnostics.Process.Start(@"C:\Git\AkkaActorModel\src\Pipeline\PhotoViewer\bin\Release\PhotoViewer.exe")



[<AutoOpenAttribute>]
module AsyncQueue =

    // represent a queue operation
    type Instruction<'T> =
        | Enqueue of 'T * (unit -> unit)
        | Dequeue of ('T -> unit)

    type AsyncBoundedQueue<'T> (capacity: int, ?cancellationToken:CancellationTokenSource) =
        let waitingConsumers, elts, waitingProducers = Queue(), Queue<'T>(), Queue()
        let cancellationToken = defaultArg cancellationToken (new CancellationTokenSource())

(*  The following balance function shuffles as many elements through the queue
    as possible by dequeuing if there are elements queued and consumers waiting
    for them and enqueuing if there is capacity spare and producers waiting *)
        let rec balance() =
            if elts.Count > 0 && waitingConsumers.Count > 0 then
                elts.Dequeue() |> waitingConsumers.Dequeue()
                balance()
            elif elts.Count < capacity && waitingProducers.Count > 0 then
                let x, reply = waitingProducers.Dequeue()
                reply()
                elts.Enqueue x
                balance()

(*  This agent sits in an infinite loop waiting to receive enqueue and dequeue instructions,
    each of which are queued internally before the internal queues are rebalanced *)
        let agent = MailboxProcessor.Start((fun inbox ->
                let rec loop() = async {
                        let! msg = inbox.Receive()
                        match msg with
                        | Enqueue(x, reply) -> waitingProducers.Enqueue (x, reply)
                        | Dequeue reply -> waitingConsumers.Enqueue reply
                        balance()
                        return! loop() }
                loop()), cancellationToken.Token)

        member __.AsyncEnqueue x =
              agent <-! (fun reply -> Enqueue(x, reply.Reply))
        member __.AsyncDequeue() =
              agent <-! (fun reply -> Dequeue reply.Reply)

        interface System.IDisposable with
              member __.Dispose() =
                cancellationToken.Cancel()
                (agent :> System.IDisposable).Dispose()




open System
open System.IO
open System.Drawing
open ImageProcessing

type ColorFilters =
    | Red
    | Green
    | Blue
    | Gray
    with member x.GetFilter(c:Color) =
                    let correct value =
                        let value' = Math.Max(int(value), 0)
                        Math.Min(255, value')
                    match x with
                    | Red -> Color.FromArgb( correct(c.R), correct(c.G - 255uy), correct(c.B - 255uy))
                    | Green ->  Color.FromArgb(correct(c.R - 255uy), correct(c.G), correct(c.B - 255uy))
                    | Blue -> Color.FromArgb(correct(c.R - 255uy), correct(c.G - 255uy), correct(c.B))
                    | Gray -> let gray = correct(byte(0.299 * float(c.R) + 0.587 * float(c.G) + 0.114 * float(c.B)))
                              Color.FromArgb(gray, gray, gray)
         member x.GetFilterColorName() =
                    match x with
                    | Red -> "Red"
                    | Green -> "Green"
                    | Blue -> "Blue"
                    | Gray -> "Gray"

type ImageInfo = { Name:string; Path:string; mutable Image:Bitmap}

type PipeLineImages(source:string, destination:string, capacityBoundedQueue:int, ?cancellationToken:CancellationTokenSource) =
    let cts = defaultArg cancellationToken (new CancellationTokenSource())

    let disposeOnException f (obj:#IDisposable) =
        try f obj
        with _ ->
            obj.Dispose()
            reraise()

    // string -> string -> ImageInfo
    let loadImage imageName sourceDir =
        let info =
            new Bitmap(Path.Combine(sourceDir, imageName)) |> disposeOnException (fun bitmap ->
                bitmap.Tag <- imageName
                let imagePath =Path.Combine(sourceDir, imageName)
                let info = { Name=imageName; Path= imagePath; Image=bitmap }
                info )
        info

     // ImageInfo -> ImageInfo
    let scaleImage (info:ImageInfo) =
        let scale = 200
        let image' = info.Image
        info.Image <- null
        let isLandscape = (image'.Width > image'.Height)
        let newWidth = if isLandscape then scale else scale * image'.Width / image'.Height
        let newHeight = if not isLandscape then scale else scale * image'.Height / image'.Width
        let bitmap = new System.Drawing.Bitmap(image', newWidth, newHeight)
        try
            ImageHandler.Resize(bitmap, newWidth, newHeight) |> disposeOnException (fun bitmap2 ->
                            bitmap2.Tag <- info.Name
                            { info with Image=bitmap2 })
        finally
            bitmap.Dispose()
            image'.Dispose()


    let tweaker (c1:Color) (c2:Color) =
     Color.FromArgb(int c1.R, int c2.G, int c2.B)

    let tweak (bitmap:Bitmap)=
        let w,h = bitmap.Width, bitmap.Height
        for x in 20 .. (w-1) do
            for y in 0 .. (h-1) do
                let color1 = bitmap.GetPixel(x,y)
                let color2 = bitmap.GetPixel(x - 20,y)
                let tweaked = tweaker color1 color2
                bitmap.SetPixel(x - 20 ,y,tweaked)
        bitmap


    // ImageInfo -> ImageInfo
    let apply3DEffect (info:ImageInfo) =
        let apply3dEffect' (bitmap:Bitmap) =
            let w,h = bitmap.Width, bitmap.Height
            for x in 20 .. (w-1) do
                for y in 0 .. (h-1) do
                    let c1 = bitmap.GetPixel(x,y)
                    let c2 = bitmap.GetPixel(x - 20,y)
                    let color3D = Color.FromArgb(int 0,int c1.R, int c2.G, int c2.B)
                    bitmap.SetPixel(x - 20 ,y,color3D)
            bitmap

        let image = info.Image
        let bmp = new System.Drawing.Bitmap(image)
        let bitmap3d = tweak bmp
        bitmap3d.Tag  <- info.Name
        info.Image <- null
        {info with Image=bitmap3d; Name = sprintf "%s_3D.jpg" (Path.GetFileNameWithoutExtension(info.Name)) }

    // ImageInfo -> ColorFilter -> ImageInfo
    let filterImage (info:ImageInfo) (filter:ColorFilters) =
        let image = info.Image
        let image' =    match filter with
                        | Red -> ImageHandler.SetColorFilter(image, ImageHandler.ColorFilterTypes.Red)
                        | Green ->ImageHandler.SetColorFilter(image, ImageHandler.ColorFilterTypes.Green)
                        | Blue->ImageHandler.SetColorFilter(image, ImageHandler.ColorFilterTypes.Blue)
                        | Gray ->ImageHandler.SetColorFilter(image, ImageHandler.ColorFilterTypes.Gray)
        image' |> disposeOnException(fun bitmap ->
                                            bitmap.Tag <- info.Name
                                            {info with Image=bitmap; Name= sprintf "%s%s.jpg" (Path.GetFileNameWithoutExtension(info.Name)) (filter.GetFilterColorName()) })

    // ImageInfo -> string -> unit
    let displayImage (info:ImageInfo) destinationPath =
        use image = info.Image
        printfn "Saving file %s" (Path.Combine(destinationPath, info.Name))
        image.Save(Path.Combine(destinationPath, info.Name))

    let loadedImages = new AsyncBoundedQueue<ImageInfo>(capacityBoundedQueue, cts)
    let scaledImages = new AsyncBoundedQueue<ImageInfo>(capacityBoundedQueue, cts)
    let filteredImages = new AsyncBoundedQueue<ImageInfo>(capacityBoundedQueue, cts)

    // STEP 1 Load images from disk and put them a queue
    let loadImages = async {
        //while true do
        let images =  Directory.GetFiles(source, "*.jpg")
        for img in images do
            if not cts.IsCancellationRequested then
                let info = loadImage img source
                do! loadedImages.AsyncEnqueue(info) }

    // STEP 2 Scale picture frame
    let scalePipelinedImages = async {
        while not cts.IsCancellationRequested do
            let! info = loadedImages.AsyncDequeue()
            let info = scaleImage info
            do! scaledImages.AsyncEnqueue(info) }

    // STEP 3 Apply filters to images
    let filterPipelinedImages = async {
        while not cts.IsCancellationRequested do
            let! info = scaledImages.AsyncDequeue()
            for filter in [ColorFilters.Blue; ColorFilters.Green;
                            ColorFilters.Red; ColorFilters.Gray ] do
                let info = filterImage info filter
                do! filteredImages.AsyncEnqueue(info) }

    // STEP 3.2 Apply 3D Effect
    let effect3DPipelinedImages = async {
        while not cts.IsCancellationRequested do
            let! info = scaledImages.AsyncDequeue()
            let info3D = apply3DEffect info
            do! filteredImages.AsyncEnqueue(info3D) }

    // STEP 4 Display the result in a UI
    let displayPipelinedImages =
        async {
            while not cts.IsCancellationRequested do
                try
                    let! info = filteredImages.AsyncDequeue()
                    printfn "display %s"  info.Name
                    displayImage info destination
                with
                | ex-> printfn "Error %s" ex.Message; ()}

    member x.Step1_loadImages = loadImages // STEP 1
    member x.Step2_scalePipelinedImages = scalePipelinedImages // STEP 2
    member x.Step3_filterPipelinedImages = effect3DPipelinedImages //filterPipelinedImages // STEP 3 // effect3DPipelinedImages
    member x.Step4_displayPipelinedImages = displayPipelinedImages  // STEP 4



let cts = new System.Threading.CancellationTokenSource()

let source = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(__SOURCE_DIRECTORY__ ),"src\\Data\\Images")
let destination =  System.IO.Path.Combine(System.IO.Path.GetDirectoryName(__SOURCE_DIRECTORY__ ),"src\\Data\\ImagesProcessed")

let pipeLineImagesTest = PipeLineImages(source, destination, 4, cts)

let processImagesPipeline() =
    Async.Start(pipeLineImagesTest.Step1_loadImages, cts.Token) // STEP 1
    Async.Start(pipeLineImagesTest.Step2_scalePipelinedImages, cts.Token) // STEP 2
    Async.Start(pipeLineImagesTest.Step3_filterPipelinedImages, cts.Token) // STEP 3
    Async.Start(pipeLineImagesTest.Step4_displayPipelinedImages, cts.Token) // STEP 4

StartPhotoViewer.start()

processImagesPipeline() // Pipeline-Agent

cts.Cancel()


//
//let rec ping (target1: Agent<_>) (target2: Agent<_>) = Agent<string>.Start(fun inbox ->
//  async {
//    let target = ref target1
//    for x=1 to 10 do
//      (!target).Post("Ping", inbox.Post)
//      let! msg = inbox.Receive()
//      if msg = "Bail!" then
//        target := target2
//      System.Console.WriteLine msg
//    (!target).Post("Stop", inbox.Post)
//  })
//
//
//  (*******************************  MAPPING AGENTS **********************************)
//let (-->) agent1 agent2 = agent1 agent2
//
//let MapToAgent f (target:Agent<_>) = Agent<_>.Start(fun inbox ->
//            let rec loop () = async {
//                let! msg = inbox.Receive()
//                target.Post (f msg)
//                return! loop () }
//            loop ()
