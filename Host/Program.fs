namespace Host

open System
open System.IO
open System.Text
open Microsoft.FSharp.Compiler.Interactive.Shell
    
module Program =

    [<EntryPoint>]
    let main (args: string []) =
        printfn "hello"
        Console.ReadKey() |> ignore
        0