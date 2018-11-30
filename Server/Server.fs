namespace MockServer

open System
open System.IO
open System.Text
open System.Collections.Generic
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Extensions
open Microsoft.Extensions.Primitives
open MockItem

module Server =
    let HeadersCopy = new HashSet<String>([
            "content-type"
        ])

    let FromHttpQuery (query: IQueryCollection) : Query =
        query.Keys
        |> Seq.map (fun q -> { name = q; values = (query.Item q |> Seq.toList)})
        |> Seq.toList

    let LoadItem (dir: string) (itemHash: byte []) : Async<Option<MockItem>> =
        let fname = sprintf "%s.resp" (BitConverter.ToString itemHash)
        let fpath = Path.Join (dir.AsSpan (), fname.AsSpan ()) in
        async {
            if not (File.Exists fpath) then
                return None
            else
                use s = File.OpenRead fpath in
                use mem = new MemoryStream () in
                do! s.CopyToAsync mem |> Async.AwaitTask
                let item = mem.ToArray () |> MockItem.Serialize.Deserialize
                // printf "item.content = %s" (Encoding.UTF8.GetString item.content)
                return Some item
        }

    let ServeMockItem (context : HttpContext) =
        let request = context.Request in
        let itemHash = BuildItemHash request.Path.Value (FromHttpQuery request.Query) in
        printfn "%s -> %s" (UriHelper.GetDisplayUrl request) (BitConverter.ToString itemHash)
        async {
            let! item = LoadItem "MockItems" itemHash in
            match item with
            | None ->
                context.Response.StatusCode <- StatusCodes.Status404NotFound;
                do! context.Response.WriteAsync "not found" |> Async.AwaitTask
            | Some item ->
                context.Response.StatusCode <- item.status;
                item.headers |>
                Seq.iter (fun h ->
                    if HeadersCopy.Contains (h.name.ToLower ())
                    then do
                       context.Response.Headers.Item h.name <- (new StringValues [| h.value |]))
                // printfn "item.content = `%s`" (Encoding.ASCII.GetString item.content);
                // printfn "item.content.length = %d" item.content.Length;
                // printfn "item.content.AsString.length = %d" (Encoding.UTF8.GetString item.content).Length;
                do! context.Response.Body.WriteAsync (item.content, 0, item.content.Length) |> Async.AwaitTask
        } |> Async.StartImmediateAsTask :> Threading.Tasks.Task
