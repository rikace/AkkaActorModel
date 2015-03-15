#load "CommonModule.fsx"
#load "PipeLineImages.fsx"

open Pipeline
open Microsoft.FSharp.Control
open Common

let cts = new System.Threading.CancellationTokenSource()

let source = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(__SOURCE_DIRECTORY__ ),"..\\Data\\Images")
let destination =  System.IO.Path.Combine(System.IO.Path.GetDirectoryName(__SOURCE_DIRECTORY__ ),"..\\Data\\ImagesProcessed")

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
//            loop () )