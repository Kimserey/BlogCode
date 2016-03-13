namespace Host

open System
open System.IO
open System.Text
open Microsoft.FSharp.Compiler.Interactive.Shell

module FsiExec =

    type FsiEvaluationSession with
        member x.ReferenceDlls filenames = 
            filenames
            |> Seq.map (fun name -> Path.Combine (AppDomain.CurrentDomain.BaseDirectory, name + ".dll"))
            |> Seq.map (sprintf "#r @\"%s\"")
            |> Seq.iter x.EvalInteraction

    let evaluateFsx<'TExpected> filename evaluate =
        let sbOut = new StringBuilder()
        let sbErr = new StringBuilder()
        use inStream = new StringReader("")
        use outStream = new StringWriter(sbOut)
        use errStream = new StringWriter(sbErr)
        
        use fsiSession = 
            FsiEvaluationSession.Create(
                FsiEvaluationSession.GetDefaultConfiguration(), 
                [||], 
                inStream, 
                outStream, 
                errStream)

        fsiSession.ReferenceDlls [
            "WebSharper.Core"
            "WebSharper.UI.Next"
            "WebSharper.Main"
            "WebSharper.Web"
            "WebSharper.Sitelets"
            "Libray"
        ]

        fsiSession.EvalScript(filename)

        match fsiSession.EvalExpression evaluate with
        | Some value -> value.ReflectionValue |> unbox<'TExpected>
        | None -> failwith ("Failed to evaluate expression " + evaluate)
