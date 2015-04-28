namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Basic Akka")>]
[<assembly: AssemblyProductAttribute("ActorModelAkka")>]
[<assembly: AssemblyDescriptionAttribute("Actor Model in F# and Akka.Net")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
