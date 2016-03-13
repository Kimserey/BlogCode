namespace Host

open System
open System.IO
open System.Text
open Microsoft.FSharp.Compiler.Interactive.Shell
open WebSharper
open WebSharper.Sitelets
open WebSharper.UI.Next

open Library

module Program =

    [<EntryPoint>]
    let main (args: string []) =
        match args with
        | [| instance |] ->

            let httpRoot = 
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "httproot")
            
            let path = 
                "Configs/" + instance + "/page.fsx"
            
            let expr =
                sprintf " ScriptRoot.pages.all, Library.Compiler.compileAndUnpack @\"%s\"" httpRoot
            
            let (pages, metadata) =
                FsiExec.evaluateFsx<Page list * WebSharper.Core.Metadata.Info> path expr
            
            let sitelet =
                Sitelet.Sum 
                    ([ yield Library.Home.Pages.page
                       yield! pages ]
                     |> List.map (fun p -> 
                            Sitelet.Content p.Route p.Route (fun _ -> 
                                Content.Page(
                                    Title = p.Title, 
                                    Head = [],
                                    Body = [ p.Content ]))))
            0
        | _ -> 
            failwith "Instance name expected in arguments."
            1