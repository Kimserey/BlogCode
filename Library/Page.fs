namespace Library

open WebSharper.UI.Next

[<AutoOpen>]
module Pages =

    type Page = {
        Title: string
        Content: Doc
        Route: string
    }