namespace Library

open WebSharper
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client

[<JavaScript>]
module Home =

    let page() =
        p [ text "Hello world" ]
