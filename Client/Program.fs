// Learn more about F# at http://fsharp.org

open System

let MockDirectory = "MockItems"

let ProcessUrl (url: string) =
    async {
        let! item = Client.Download (new Uri(url)) in
        do! Client.SaveToDirectory MockDirectory item
        printfn "%d %s %s" item.status (BitConverter.ToString item.hash) item.originUrl
    } |> Async.RunSynchronously |> ignore

let List () = 
    async {
        let! items = Client.ListItems MockDirectory in
        items |>
        Seq.iter (fun item ->
            printfn "%d %s %s" item.status (BitConverter.ToString item.hash) item.originUrl
        )
    } |> Async.RunSynchronously

[<EntryPoint>]
let main argv =
    let exit = ref false in
        while not !exit do
            match Console.ReadLine () with
            | null -> exit := true
            | "" -> () // skip
            | (line: string) -> ProcessUrl line
    0 // return an integer exit code
