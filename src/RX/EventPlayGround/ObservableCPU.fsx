#r "PresentationCore.dll"
#r "PresentationFramework.dll"
#r "WindowsBase.dll"
#r "System.Xaml.dll"
#r @"..\bin\Debug\FSharpx.TypeProviders.Xaml.dll"

open System.Windows
open System
open System.Windows
open System.Windows.Controls
open System.Windows.Data
open System.Windows.Documents
open System.Windows.Media
open System.Windows.Media.Animation
open System.Windows.Media.Media3D
open System.Windows.Shapes
open System.Threading
open FSharpx

type MainWindow = XAML< "..\MainWindow.xaml" >

type Cylinder() =
    let mutable height = 2.0
    let mutable longitude = 48
    let mutable radius = 1.0
    let mutable changed = true
    let mutable mesh = System.Windows.Media.Media3D.MeshGeometry3D()

    let createMesh() =
            mesh <- new MeshGeometry3D()

            let negY = new Vector3D(0., -1., 0.)
            let posY = new Vector3D(0., 1., 0.)
            let texCoordTop = new Point(0., 0.)
            let texCoordBottom = new Point(0., 1.)
            let lonDeltaTheta = 2.0 * Math.PI / (double)longitude
            let y0 = height / 2.
            let y1 = -y0
            let mutable lonTheta = Math.PI
            let mutable indices = 0

            for  lon in [0..longitude-1] do
                let u0 = (double)lon / (double)longitude
                let x0 = radius * Math.Cos(lonTheta)
                let z0 = radius * Math.Sin(lonTheta)

                if lon = longitude - 1 then
                    lonTheta <- Math.PI
                else
                    lonTheta <- lonTheta - lonDeltaTheta
                
                let u1 = (double)(lon + 1) / (double)longitude
                let x1 = radius * Math.Cos(lonTheta)
                let z1 = radius * Math.Sin(lonTheta)
                let p0 = new Point3D(x0, y1, z0)
                let p1 = new Point3D(x0, y0, z0)
                let p2 = new Point3D(x1, y1, z1)
                let p3 = new Point3D(x1, y0, z1)
                let norm0 = new Vector3D(x0, 0., z0)
                let norm1 = new Vector3D(x1, 0., z1)
                norm0.Normalize();
                norm1.Normalize();
                mesh.Positions.Add(p0)
                mesh.Positions.Add(p1)
                mesh.Positions.Add(p2)
                mesh.Positions.Add(p3)
                mesh.Normals.Add(norm0)
                mesh.Normals.Add(norm0)
                mesh.Normals.Add(norm1)
                mesh.Normals.Add(norm1)
                mesh.TextureCoordinates.Add(new Point(u0, 1.))
                mesh.TextureCoordinates.Add(new Point(u0, 0.))
                mesh.TextureCoordinates.Add(new Point(u1, 1.))
                mesh.TextureCoordinates.Add(new Point(u1, 0.))                
                
                mesh.TriangleIndices.Add(indices)  // 0
                indices <- indices + 1
                mesh.TriangleIndices.Add(indices)  // 1
                indices <- indices + 1
                mesh.TriangleIndices.Add(indices)  // 2
                indices <- indices + 1
                mesh.TriangleIndices.Add(indices)  // 3
                indices <- indices - 1
                mesh.TriangleIndices.Add(indices)  // 2
                indices <- indices - 1
                mesh.TriangleIndices.Add(indices)  // 1
                indices <- indices + 2

                mesh.Positions.Add(p0)
                mesh.Positions.Add(p2)
                mesh.Positions.Add(new Point3D(0., y1, 0.))
                mesh.Normals.Add(negY)
                mesh.Normals.Add(negY)
                mesh.Normals.Add(negY)
                mesh.TextureCoordinates.Add(texCoordBottom)
                mesh.TextureCoordinates.Add(texCoordBottom)
                mesh.TextureCoordinates.Add(texCoordBottom)               
                indices <- indices + 1
                mesh.TriangleIndices.Add(indices)
                indices <- indices + 1
                mesh.TriangleIndices.Add(indices)
                indices <- indices + 1
                mesh.TriangleIndices.Add(indices)
                indices <- indices + 1

                mesh.Positions.Add(p3)
                mesh.Positions.Add(p1)
                mesh.Positions.Add(new Point3D(0., y0, 0.))
                mesh.Normals.Add(posY)
                mesh.Normals.Add(posY)
                mesh.Normals.Add(posY)
                mesh.TextureCoordinates.Add(texCoordTop)
                mesh.TextureCoordinates.Add(texCoordTop)
                mesh.TextureCoordinates.Add(texCoordTop)                
                mesh.TriangleIndices.Add(indices)
                indices <- indices + 1
                mesh.TriangleIndices.Add(indices)
                indices <- indices + 1
                mesh.TriangleIndices.Add(indices)
                indices <- indices + 1
          

    member x.Longitude with get() = longitude
                       and set(v) = longitude <- v        
                                    changed <- true 
                                     
    member x.Height with get() = height
                    and set(v) = height <- v        
                                 changed <- true 
                       
    member x.Radius with get() = radius
                    and set(v) = radius <- v        
                                 changed <- true 

    member x.Mesh with get() = 
                        if changed then
                            createMesh() 
                            changed <- false 
                        mesh

let loadWindow() = 
    let window = MainWindow()
    window.Root.Loaded.Add(fun t -> 
        let cylinderFactory = new Cylinder() //2.0, 48, 1.0, true)
        let materialGreen = new System.Windows.Media.Media3D.DiffuseMaterial(new SolidColorBrush(Colors.LimeGreen))
        let cylinder = new GeometryModel3D(cylinderFactory.Mesh, materialGreen)
        let mv3d = window.CpuPumpAnimation3dViewPort.Children.[1] :?> ModelVisual3D
        mv3d.Content <- cylinder
        window.Root.Topmost <- true
        window.CpuPumpAnimation3dViewPort.Visibility <- Visibility.Collapsed)
   
    let scaleTransform3D = new ScaleTransform3D(new Vector3D(1., 1., 1.))
    
    let animation (cpuInt, material) = 
        let vector3DAnimation = new DoubleAnimation()
        vector3DAnimation.From <- vector3DAnimation.To
        vector3DAnimation.To <- Nullable<float>(double (cpuInt * 0.01))
        vector3DAnimation.Duration <- new Duration(TimeSpan.FromSeconds(0.5))
        scaleTransform3D.CenterY <- -1.
        let clock3D = vector3DAnimation.CreateClock()
        scaleTransform3D.ApplyAnimationClock(ScaleTransform3D.ScaleYProperty, clock3D)
        let mv3d = window.CpuPumpAnimation3dViewPort.Children.[1] :?> ModelVisual3D
        mv3d.Transform <- scaleTransform3D
        (mv3d.Content :?> GeometryModel3D).Material <- material
        clock3D.Controller.Begin()
        window.CpuPumpAnimation3dViewPort.Visibility <- Visibility.Visible
    
    // CPU Performance Counter (sort of Event Stream)
    let performanceCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total")    
    
    let uiTimer = System.Windows.Threading.DispatcherTimer()
    uiTimer.Interval <- TimeSpan.FromSeconds(0.5)

    let redMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Red))
    let greenMaterial =  new DiffuseMaterial(new SolidColorBrush(Colors.Green))

    // OBSERVABLE CPU
    let disposable = 
            uiTimer.Tick 
            |> Observable.map (fun _ -> float (performanceCounter.NextValue()))
            |> Observable.filter( fun f -> f < 90.)         
            |> Observable.map(fun f->  if f > 5. then (f, redMaterial) else (f, greenMaterial)) 
            |> Observable.subscribe (fun f -> 
                    let toCpuUsage, material = f
                    window.CpuText.Text <- string (int (toCpuUsage))
                    animation (toCpuUsage, material))

    uiTimer.Start()

    window.btnDispose.Click.Add(fun _ -> disposable.Dispose())
    window.Root

    //disposable.Dispose()

[<STAThread>]
(new Application()).Run(loadWindow()) |> ignore


(*
let cpuIntensive() =
    let startDt = DateTime.Now;
    while (DateTime.Now - startDt).TotalSeconds >= 10. do
        ()

for i in [0..4] do
    let t = new Thread(new ThreadStart(fun _ -> cpuIntensive()))
    t.Start()
*)