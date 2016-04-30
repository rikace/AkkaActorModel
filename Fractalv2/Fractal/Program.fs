open System
open System.IO
open System.Windows.Forms
open System
open System.Drawing
open System.Threading
open System.Threading.Tasks
open System.IO
open Akka
open Akka.Configuration
open Akka.FSharp
open Akka.Actor
open Akka.Routing
open MandelbrotSet

type public AkkaFractalForm() as form = 
    inherit Form()
    let picBox = new PictureBox(Dock = DockStyle.Fill)
    let panel = new Panel()
    
    let initControls = 
        form.Width <- 574
        form.Height <- 454
        form.Visible <- true
        form.Text <- "F# Akka Fractal"
        form.AutoScaleDimensions <- new System.Drawing.SizeF(float32 8, float32 16)
        form.Margin <- new System.Windows.Forms.Padding(2, 2, 2, 2)
        form.AutoScaleMode <- System.Windows.Forms.AutoScaleMode.Font
        form.ClientSize <- new System.Drawing.Size(558, 415)
        form.WindowState <- System.Windows.Forms.FormWindowState.Maximized
        form.AutoScroll <- true
        picBox.Location <- new System.Drawing.Point(2, 2)
        picBox.Margin <- new System.Windows.Forms.Padding(2, 2, 2, 2)
        picBox.Size <- new System.Drawing.Size(744, 511)
        picBox.SizeMode <- System.Windows.Forms.PictureBoxSizeMode.AutoSize
        picBox.Dock <- System.Windows.Forms.DockStyle.Fill
        panel.AutoScroll <- true
        panel.Anchor <- AnchorStyles.Top ||| AnchorStyles.Left
        //  panel.Dock <- System.Windows.Forms.DockStyle.Fill
        panel.Location <- new System.Drawing.Point(0, 0)
        panel.Margin <- new System.Windows.Forms.Padding(2, 2, 2, 2)
        panel.ClientSize <- new System.Drawing.Size(8000, 8000)
        panel.VerticalScroll.Visible <- true
        panel.HorizontalScroll.Visible <- true
        panel.ResumeLayout(false)
        panel.PerformLayout()
    
    do 
        form.SuspendLayout()
        initControls
        panel.Controls.Add(picBox)
        form.Controls.Add(panel)
        form.Load.AddHandler(new System.EventHandler(fun sender e -> form.eventForm_Loading (sender, e)))
        form.ResumeLayout(false)
        form.PerformLayout()
        form.OnLoad(EventArgs.Empty)
    
    member form.eventForm_Loading (sender : obj, e : EventArgs) = 
        let config = Configuration.parse """
                akka {  
                    log-config-on-start = on
                    stdout-loglevel = DEBUG
                    loglevel = DEBUG
                    actor {
                        provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
                    }
                    remote {
                        helios.tcp {
                            transport-class = "Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote"
		                    transport-protocol = tcp
		                    port = 0
		                    hostname = localhost
                        }
                    }
                }
                """
        let system = System.create "fractal" (config)
        let w = 8000
        let h = 8000
        let img = new Bitmap(w, h)
        picBox.Image <- img
        picBox.Invalidate()
        let split = 80
        let ys = h / split
        let xs = w / split
        let g = Graphics.FromImage(img)
        
        let toBitmap (byteArray:byte array) =
            use mem = new MemoryStream(byteArray) 
            let image = Image.FromStream(mem)
            (image :?> Bitmap)

        let renderer (bytes:byte array) x y = 
            let image = toBitmap bytes
            g.DrawImageUnscaled(image, x, y)
            picBox.Invalidate()
        
        let displayTile = 
            spawnOpt system "display-tile" (fun mailbox -> 
                let rec loop() = 
                    actor { 
                        let! (bytes, x, y) = mailbox.Receive()
                        renderer bytes x y
                        return! loop()
                    }
                loop()) [ SpawnOption.Dispatcher "akka.actor.synchronized-dispatcher" ]
        
               
        let deployment = Deploy(RemoteScope(Address.Parse "akka.tcp://worker@localhost:8090/user/render"))
        let router = RoundRobinPool 16
        
        let actor = 
            spawne system "render" 
            <| <@ actorOf2(fun mailbox msg ->
                    match msg with
                    | x, y, w, h ->
                        logInfof mailbox "%A rendering %d , %d" mailbox.Self x y
                      
                        let res = Mandelbrot.Set(x, y, w, h, 4000, 4000, 0.5, -2.5, 1.5, -1.5)
                       
                        use mem = new MemoryStream()
                        res.Save(mem, System.Drawing.Imaging.ImageFormat.Png)                        
                        mailbox.Sender() <! (mem.ToArray(), x, y)
                        mem.Close()

                    | _ -> mailbox.Unhandled msg) @> 
            <| [ SpawnOption.Deploy deployment; SpawnOption.Router router ]


        MessageBox.Show(form, "Click ok to Start") |> ignore

        for y = 0 to split do
            let yy = ys * y
            for x = 0 to split do
                let xx = xs * x
                g.DrawRectangle(Pens.Red, xx, yy, xs - 1, ys - 1)
                actor.Tell((yy, xx, ys, xs), displayTile)

module Main = 
    [<STAThread>]
    do Application.EnableVisualStyles()
       Application.SetCompatibleTextRenderingDefault(false)
       Application.Run(new AkkaFractalForm() :> Form)
