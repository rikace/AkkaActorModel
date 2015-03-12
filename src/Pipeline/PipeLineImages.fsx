#load "..\CommonModule.fsx"
#load "6.AsyncBoundedQueue.fsx"
#r "..\..\Lib\CommonHelpers.dll"
namespace Pipeline

open System
open System.IO
open System.Drawing
open System.Threading
open AsyncBoundedQueue
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


type PipeLineImages(capacityBoundedQueue:int, ?cancellationToken:CancellationTokenSource) =
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
        image.Save(Path.Combine(destinationPath, info.Name))
    
    let loadedImages = new AsyncBoundedQueue<ImageInfo>(capacityBoundedQueue, cts)
    let scaledImages = new AsyncBoundedQueue<ImageInfo>(capacityBoundedQueue, cts)    
    let filteredImages = new AsyncBoundedQueue<ImageInfo>(capacityBoundedQueue, cts)    

    // STEP 1 Load images from disk and put them a queue
    let loadImages = async {
        let dirPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(__SOURCE_DIRECTORY__ ),"Data\\Images")
        //while true do 
        let images =  Directory.GetFiles(dirPath, "*.jpg") 
        for img in images do
            if not cts.IsCancellationRequested then 
                let info = loadImage img dirPath 
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
          
    // STEP 4 Display the result in a UI
    let displayPipelinedImages =
        let destinationPath = @"C:\Demo\ConcurrecnyFSharp\DemoConcurrency_V4\ImagesProcessed"
        async {
            while not cts.IsCancellationRequested do
                try 
                    let! info = filteredImages.AsyncDequeue()      
                    printfn "display %s"  info.Name 
                    displayImage info destinationPath  
                with 
                | ex-> printfn "Error %s" ex.Message; ()}

    member x.Step1_loadImages = loadImages // STEP 1
    member x.Step2_scalePipelinedImages = scalePipelinedImages // STEP 2
    member x.Step3_filterPipelinedImages = filterPipelinedImages // STEP 3
    member x.Step4_displayPipelinedImages = displayPipelinedImages  // STEP 4

