namespace Library

open WebSharper
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client

module Home =

    [<JavaScript>]
    module Client =
        let page() =
            p [ text "Hello world" ]



    module Pages =

        let page = {
            Title = "Home"
            Route = "home"
            Content = client <@ Client.page() @>
        }