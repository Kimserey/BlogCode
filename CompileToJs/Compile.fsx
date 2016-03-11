#load "_references.fsx"

/// Most of the code is extracted from WebSharper.Warp:
/// https://github.com/intellifactory/websharper.warp

open System.Reflection
open WebSharper
open WebSharper.Sitelets
open System
open System.IO
open IntelliFactory.Core    

module PC = WebSharper.PathConventions
module FE = WebSharper.Compiler.FrontEnd

module Compiler =
    let compile asm =
        let localDir = Directory.GetCurrentDirectory()
        let websharperDir = Path.GetDirectoryName typeof<Sitelet<_>>.Assembly.Location
        let fsharpDir = Path.GetDirectoryName typeof<option<_>>.Assembly.Location

        let loadPaths =
            [
                localDir
                websharperDir
                fsharpDir
            ]
        let loader =
            let aR =
                AssemblyResolver.Create()
                    .SearchDirectories(loadPaths)
                    .WithBaseDirectory(fsharpDir)
            FE.Loader.Create aR stderr.WriteLine

        let refs =
            [
                for dll in Directory.EnumerateFiles(websharperDir, "*.dll") do
                    if Path.GetFileName(dll) <> "FSharp.Core.dll" then
                        yield dll
                let dontRef (n: string) =
                    [
                        "FSharp.Compiler.Interactive.Settings,"
                        "FSharp.Compiler.Service,"
                        "FSharp.Core,"
                        "FSharp.Data.TypeProviders,"
                        "Mono.Cecil"
                        "mscorlib,"
                        "System."
                        "System,"
                    ] |> List.exists n.StartsWith
                let rec loadRefs (asms: Assembly[]) (loaded: Map<string, Assembly>) =
                    let refs =
                        asms
                        |> Seq.collect (fun asm -> asm.GetReferencedAssemblies())
                        |> Seq.map (fun n -> n.FullName)
                        |> Seq.distinct
                        |> Seq.filter (fun n -> not (dontRef n || Map.containsKey n loaded))
                        |> Seq.choose (fun n ->
                            try Some (AppDomain.CurrentDomain.Load n)
                            with _ -> None)
                        |> Array.ofSeq
                    if Array.isEmpty refs then
                        loaded
                    else
                        (loaded, refs)
                        ||> Array.fold (fun loaded ref -> loaded |> Map.add ref.FullName ref)
                        |> loadRefs refs
                let asms =
                    AppDomain.CurrentDomain.GetAssemblies()
                    |> Array.filter (fun a -> not (dontRef a.FullName))
                yield! asms
                |> Array.map (fun asm -> asm.FullName, asm)
                |> Map.ofArray
                |> loadRefs asms
                |> Seq.choose (fun (KeyValue(_, asm)) ->
                    try Some asm.Location
                    with :? NotSupportedException ->
                        // The dynamic assembly does not support `.Location`.
                        // No problem, if it's from the dynamic assembly then
                        // it doesn't incur a dependency anyway.
                        None)
            ]
            |> Seq.distinctBy Path.GetFileName
            |> Seq.choose (fun x ->
                try
                    Some (loader.LoadFile x)
                with ex -> 
                    printfn "%s" ex.Message
                    None)
            |> Seq.toList
        let opts = { FE.Options.Default with References = refs }
        let compiler = FE.Prepare opts (eprintfn "%O")

        compiler.Compile(<@ () @>, context = asm)
        |> Option.map (fun asm -> asm, refs)



    let outputFiles root (refs: Compiler.Assembly list) =
        let pc = PC.PathUtility.FileSystem(root)
        let writeTextFile path contents =
            Directory.CreateDirectory (Path.GetDirectoryName path) |> ignore
            File.WriteAllText(path, contents)
        let writeBinaryFile path contents =
            Directory.CreateDirectory (Path.GetDirectoryName path) |> ignore
            File.WriteAllBytes(path, contents)
        let emit text path =
            match text with
            | Some text -> writeTextFile path text
            | None -> ()
        let script = PC.ResourceKind.Script
        let content = PC.ResourceKind.Content

        for a in refs do
            let aid = PC.AssemblyId.Create(a.FullName)
            emit a.ReadableJavaScript (pc.JavaScriptPath aid)
            emit a.CompressedJavaScript (pc.MinifiedJavaScriptPath aid)
            let writeText k fn c =
                let p = pc.EmbeddedPath(PC.EmbeddedResource.Create(k, aid, fn))
                writeTextFile p c
            let writeBinary k fn c =
                let p = pc.EmbeddedPath(PC.EmbeddedResource.Create(k, aid, fn))
                writeBinaryFile p c
            for r in a.GetScripts() do
                writeText script r.FileName r.Content
            for r in a.GetContents() do
                writeBinary content r.FileName (r.GetContentData())

    let private (+/) x y = Path.Combine(x, y)

    let outputFile root (asm: FE.CompiledAssembly) =
        let dir = root +/ "Scripts" +/ "WebSharper"
        Directory.CreateDirectory(dir) |> ignore
        File.WriteAllText(dir +/ "WebSharper.EntryPoint.js", asm.ReadableJavaScript)
        File.WriteAllText(dir +/ "WebSharper.EntryPoint.min.js", asm.CompressedJavaScript)

    /// Compiles the caller assembly to WebSharper and unpack \Contents and \Scripts in root folder.
    /// When compiling .fsx, must be called from the .fsx.
    let compileAndUnpack root =
        let perf = System.Diagnostics.Stopwatch.StartNew()
        printfn "Compiling FSX to WebSharper and extracting content files..."

        match compile (Assembly.GetCallingAssembly()) with
        | Some (asm, refs) ->
            printfn "Compiled to WebSharper in %.2f seconds." (perf.Elapsed.TotalSeconds)
            perf.Restart()

            outputFiles root refs
            outputFile root asm
            printfn "Extracted content files in %.2f seconds." (perf.Elapsed.TotalSeconds)

            printfn "Compiled to WebSharper and extracted content files with no error."
            asm.Info
        | None -> failwith "Failed to compile with WebSharper."