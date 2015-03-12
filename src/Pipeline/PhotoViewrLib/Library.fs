
namespace PhotoViewerLib

module GetImages =
    open System
    open System.Net
    open System.IO
    
    type TypeDownload =
    | Web  
    | FileSys  

    type Downloader(destination,typeDownload:TypeDownload ) = 
        
        let files = seq{ for i in [1..11] do yield sprintf "bugghina%d.jpg" i }

        let getImageWeb name = async {
                let req = WebRequest.Create("http://teknology360.com/photos/" + name)        
                let! resp = req.AsyncGetResponse()
                use streamSource = resp.GetResponseStream()
                let! data = streamSource.AsyncRead(int resp.ContentLength)
                let fileDestination = Path.Combine(destination, name)
                if File.Exists fileDestination then File.Delete fileDestination
                use streamDestination = new FileStream(fileDestination, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 0x1000, FileOptions.Asynchronous)
                do! streamDestination.AsyncWrite(data, 0, data.Length)  
                streamSource.Dispose(); streamDestination.Dispose() }
                
        let getImageFileSys name = async {         
                let fileSource = Path.Combine(@"..\..\..\DemoConcurrency\Data\Images", name)     
                let fileDestination = Path.Combine(destination, name)
                if File.Exists fileSource then 
                    if File.Exists fileDestination then File.Delete fileDestination    
                    use streamSource= new FileStream(fileSource, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0x100, FileOptions.Asynchronous)
                    use streamDestination = new FileStream(fileDestination, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 0x100, FileOptions.Asynchronous)
                    let! data = streamSource.AsyncRead(int streamSource.Length)
                    do! streamDestination.AsyncWrite(data, 0, data.Length) 
                    streamSource.Dispose(); streamDestination.Dispose() }

        let getImage fileName = 
            match typeDownload with
            | Web -> getImageWeb fileName
            | FileSys -> getImageFileSys fileName

        let parallelAgents n f = 
            MailboxProcessor.Start(fun inbox ->
                let workers = 
                    Array.init n (fun i -> MailboxProcessor.Start(f))
                let rec loop i = async {                    
                    let! msg = inbox.Receive()
                    workers.[i].Post(msg)
                    return! loop ((i+1) % n)    }
                loop 0  )

        let agent = 
            parallelAgents (System.Environment.ProcessorCount - 1) (fun inbox ->
                let rec loop() = async {                    
                    let! msg = inbox.Receive()
                    do! getImage msg
                    return! loop()  }
                loop()  )
    
        member this.DownloadAllAgent() = 
                    files
                    |> Seq.iter agent.Post

        member this.DownloadAllAsync() = 
            files
            |> Seq.map getImage
            |> Async.Parallel 
            |> Async.Ignore
            |> Async.Start

        member this.DownloadAllSync() = 
            let downloadSync name = 
                let req = WebRequest.Create("http://teknology360.com/photos/" + name)        
                let resp = req.GetResponse()
                use stream = resp.GetResponseStream()
                use streamDestination = new FileStream(Path.Combine(destination, name), FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 0x1000)
                stream.CopyTo(streamDestination)
            files
            |> Seq.iter downloadSync
        

module GetImagesTest = 
    
    let op = GetImages.Downloader(@"c:\temp", GetImages.TypeDownload.FileSys)

    op.DownloadAllAgent()