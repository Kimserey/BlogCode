#load "../_references.fsx"

namespace ScriptRoot

open Library
open WebSharper.UI.Next

module Pages =

    let all = 
        [ { Title = "Moon page"
            Route = "moon"
            Content = Doc.Empty } ]