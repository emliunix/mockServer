// Learn more about F# at http://fsharp.org

open System
open System.Threading

let MockDirectory = "MockItems"

let ProcessUrl (url: string) =
    async {
        do! Async.SwitchToThreadPool ()
        let! item = Client.Download (new Uri(url)) in
        do! Client.SaveToDirectory MockDirectory item
        printfn "%d %s %d %s" item.status (BitConverter.ToString item.hash) item.content.Length item.originUrl
    } |> Async.RunSynchronously

let List () = 
    async {
        let! items = Client.ListItems MockDirectory in
        items |>
        Seq.iter (fun item ->
            printfn "%d %s %s" item.status (BitConverter.ToString item.hash) item.originUrl
        )
    } |> Async.Start

[<EntryPoint>]
let main argv =
    let exit = ref false in
        while not !exit do
            match Console.ReadLine () with
            | null -> exit := true
            | "" -> () // skip
            | (line: string) -> ProcessUrl line
    0 // return an integer exit code
