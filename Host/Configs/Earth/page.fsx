#load "../_references.fsx"

namespace ScriptRoot

open Library
open WebSharper.UI.Next

module Pages =

    let all = 
        [ { Title = "Earth page"
            Route = "earth"
            Content = Doc.Empty } ]