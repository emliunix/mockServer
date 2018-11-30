namespace MockServer

open System
open System.IO
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Extensions
open Microsoft.Extensions.Primitives
open MockItem

module Server =
    let hello = "world"

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
                let item = mem.GetBuffer () |> MockItem.Serialize.Deserialize
                return Some item
        }

    let ServeMockItem (context : HttpContext) =
        let request = context.Request in
        let itemHash = BuildItemHash request.Path.Value (FromHttpQuery request.Query) in
            printfn "%s -> %s" (UriHelper.GetDisplayUrl request) (BitConverter.ToString itemHash) |> ignore;
        async {
            let! item = LoadItem "MockItems" itemHash in
            match item with
            | None -> context.Response.StatusCode <- StatusCodes.Status404NotFound;
                      return context.Response.WriteAsync "not found"
            | Some item -> context.Response.StatusCode <- item.status;
                           item.headers |> Seq.iter (fun h ->
                               if h.name = "Content-Length" then () else
                               context.Response.Headers.Item h.name <- (new StringValues [| h.value |]));
                           return (context.Response.Body.WriteAsync (new ReadOnlyMemory<byte>(item.content))).AsTask ()
        } |> Async.RunSynchronously
