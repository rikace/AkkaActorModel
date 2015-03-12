#load "..\Utilities\ImageInfo.fs" //Utility\
#load "..\Utilities\BitmapExtensions.fs" //Utility\
#load "..\4.Agent\5.BlockingQueueAgent.fsx"

open System
open System.IO
open System.Drawing
open System.Threading
open System.Windows.Forms
open ImagePipeline.Image
open System.Collections.Concurrent
open System.Collections.Generic
open System.Drawing
open ImagePipeline.Extensions


let GaussianNoiseAmount = 50.0 

type Agent<'T> = MailboxProcessor<'T>

type BlockingAgentMessage<'T> = 
  | Add of 'T * AsyncReplyChannel<unit> 
  | Get of AsyncReplyChannel<'T>

type BlockingQueueAgent<'T>(maxLength) =
  [<VolatileField>]
  let mutable count = 0
  let agent = Agent.Start(fun agent ->
    let queue = new Queue<_>()
    let pending = new Queue<_>()
    let rec emptyQueue() = 
      agent.Scan(fun msg ->
        match msg with 
        | Add(value, reply) -> Some <| async {  queue.Enqueue(value)
                                                count <- queue.Count
                                                reply.Reply()
                                                return! nonEmptyQueue() }
        | _ -> None )
    and nonEmptyQueue() = async {
      let! msg = agent.Receive()
      match msg with 
      | Add(value, reply) -> 
          if queue.Count < maxLength then 
            queue.Enqueue(value)
            count <- queue.Count
            reply.Reply()
          else 
            pending.Enqueue(value, reply) 
          return! nonEmptyQueue()
      | Get(reply) -> 
          let item = queue.Dequeue()
          while queue.Count < maxLength && pending.Count > 0 do
            let itm, caller = pending.Dequeue()
            queue.Enqueue(itm)
            caller.Reply()
          count <- queue.Count
          reply.Reply(item)
          if queue.Count = 0 then return! emptyQueue()
          else return! nonEmptyQueue() }
    emptyQueue() )

  member x.Count = count
  member x.AsyncAdd(v:'T, ?timeout) = 
    agent.PostAndAsyncReply((fun ch -> Add(v, ch)), ?timeout=timeout)
  member x.AsyncGet(?timeout) = 
    agent.PostAndAsyncReply(Get, ?timeout=timeout)

let disposeOnException f (obj:#IDisposable) =
    try f obj
    with _ -> 
      obj.Dispose()  
      reraise()

let loadImage fname sourceDir count clockOffset =
    let startTick = Environment.TickCount
    let info = 
        new Bitmap(Path.Combine(sourceDir, fname)) |> disposeOnException (fun bitmap ->
            bitmap.Tag <- fname
            let info = new ImageInfo(count, fname, bitmap, clockOffset)
            info.PhaseStartTick.[0] <- startTick - clockOffset 
            info )
    info.PhaseEndTick.[0] <- Environment.TickCount - clockOffset
    info 


let scaleImage (info:ImageInfo) =
    let startTick = Environment.TickCount
    let orig = info.OriginalImage
    info.OriginalImage <- null
    let scale = 200
    let isLandscape = (orig.Width > orig.Height)
    let newWidth = if isLandscape then scale else scale * orig.Width / orig.Height
    let newHeight = if not isLandscape then scale else scale * orig.Height / orig.Width
    let bitmap = new System.Drawing.Bitmap(orig, newWidth, newHeight)
    try
        bitmap.AddBorder(15) |> disposeOnException (fun bitmap2 ->
            bitmap2.Tag <- orig.Tag
            info.ThumbnailImage <- bitmap2
            info.PhaseStartTick.[1] <- startTick - info.ClockOffset )
    finally
        bitmap.Dispose()
        orig.Dispose()
    info.PhaseEndTick.[1] <- Environment.TickCount - info.ClockOffset
    info

let filterImage (info:ImageInfo) =
    let startTick = Environment.TickCount
    let sc = info.ThumbnailImage
    info.ThumbnailImage <- null
    sc.AddNoise(GaussianNoiseAmount) |> disposeOnException (fun bitmap ->
        bitmap.Tag <- sc.Tag
        info.FilteredImage <- bitmap
        info.PhaseStartTick.[2] <- startTick - info.ClockOffset )
    sc.Dispose()
    info.PhaseEndTick.[2] <- Environment.TickCount - info.ClockOffset
    info


let displayImage (info:ImageInfo) count displayFn duration =
    let startTick = Environment.TickCount
    info.ImageCount <- count
    info.PhaseStartTick.[3] <- startTick - info.ClockOffset
    info.PhaseEndTick.[3] <- 
      if  duration > 0 then startTick - info.ClockOffset + duration
      else Environment.TickCount - info.ClockOffset
    displayFn info

// ------------------------------------------------------------------------------
// A version using F# Mailbox Processor
// ------------------------------------------------------------------------------

let displayFn info = 
    printfn "%A" info 

let queueLength = 8

let loadedImages = new BlockingQueueAgent<ImageInfo>(queueLength)
let scaledImages = new BlockingQueueAgent<ImageInfo>(queueLength)    
let filteredImages = new BlockingQueueAgent<ImageInfo>(queueLength)    



// [PHASE 1] Load images from disk and put them a queue.
let loadImages = async {
    let clockOffset = Environment.TickCount
    let count = ref 0
    let dirPath = "..\\Data\\Images"
    while true do 
        let images =  Directory.GetFiles(dirPath, "*.jpg") 
        for img in images do
        let info = loadImage img dirPath (!count) clockOffset
        incr count 
        do! loadedImages.AsyncAdd(info) }

// [PHASE 2] Scale to thumbnail size and render picture frame.
let scalePipelinedImages = async {
    while true do 
        let! info = loadedImages.AsyncGet()
        let info = scaleImage info
        do! scaledImages.AsyncAdd(info) }

// [PHASE 3] Give images a speckled appearance by adding noise
let filterPipelinedImages = async {
    while true do 
        let! info = scaledImages.AsyncGet()
        let info = filterImage info
        do! filteredImages.AsyncAdd(info) }

// [PHASE 4]: Invoke the function (display the result in a UI)
let displayPipelinedImages = 
    let rec loop count duration = async {
        let! info = filteredImages.AsyncGet()
        let displayStart = Environment.TickCount
        info.QueueCount1 <- loadedImages.Count
        info.QueueCount2 <- scaledImages.Count
        info.QueueCount3 <- filteredImages.Count
        displayImage info (count + 1) displayFn duration
        let time = (Environment.TickCount - displayStart)
        return! loop (count + 1) time }
    loop 0 0

let cts = new System.Threading.CancellationTokenSource()

Async.Start(loadImages, cts.Token)
Async.Start(scalePipelinedImages, cts.Token)
Async.Start(filterPipelinedImages, cts.Token)

try 
    Async.RunSynchronously(displayPipelinedImages, cancellationToken = cts.Token)
with 
:? OperationCanceledException -> () 
