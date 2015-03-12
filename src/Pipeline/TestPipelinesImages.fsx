#load "..\CommonModule.fsx"
#load "2.PipeLineImages.fsx"
open Pipeline
open Microsoft.FSharp.Control
open Common

let cts = new System.Threading.CancellationTokenSource()

let pipeLineImagesTest = PipeLineImages(4, cts)

let processImagesPipeline() =
    Async.Start(pipeLineImagesTest.Step1_loadImages, cts.Token) // STEP 1
    Async.Start(pipeLineImagesTest.Step2_scalePipelinedImages, cts.Token) // STEP 2
    Async.Start(pipeLineImagesTest.Step3_filterPipelinedImages, cts.Token) // STEP 3
    Async.Start(pipeLineImagesTest.Step4_displayPipelinedImages, cts.Token) // STEP 4

StartPhotoViewer.start()

processImagesPipeline() // Pipeline-Agent

cts.Cancel()

