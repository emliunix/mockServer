module Client

open System
open System.IO
open System.Net
open Microsoft.AspNetCore.WebUtilities
open MockItem

let FromHttpHeaders (header: WebHeaderCollection) : Headers =
    header.AllKeys |>
    Seq.map (fun h -> { name = h; value = header.Item h}) |>
    Seq.toList

let AsyncReadStream (stream: Stream) = async {
    let count = ref 0
    let buffer = Array.create 1024 (byte 0) in
    use mem = new MemoryStream () in
    do! stream.CopyToAsync mem |> Async.AwaitTask
    return mem.ToArray ()
}

let QueryFromUri (uri: Uri) : Query = 
    let query = QueryHelpers.ParseQuery uri.Query in
    query.Keys |>
    Seq.map (fun q -> {name = q; values = query.Item q |> Seq.toList }) |>
    Seq.toList

let Download (uri : Uri) =
    async {
        let req = WebRequest.Create uri in
        use! resp = req.AsyncGetResponse () in
        use stream = resp.GetResponseStream() in
        let! data = AsyncReadStream stream in
        let query = QueryFromUri uri in
        return {
            originUrl = uri.ToString ();
            hash = BuildItemHash uri.AbsolutePath query;
            status = int (resp :?> HttpWebResponse).StatusCode;
            headers = resp.Headers |> FromHttpHeaders;
            content = data;
        }
    }

let SaveToDirectory (dir: string) (item: MockItem) =
    async {
        if not (Directory.Exists dir) then Directory.CreateDirectory dir |> ignore else ()
        let fname = sprintf "%s.resp" (BitConverter.ToString item.hash) in
        let fpath = Path.Join (dir.AsSpan (), fname.AsSpan ()) in
        use f = File.Open(fpath, FileMode.Create) in
        use mem = new MemoryStream (MockItem.Serialize.Serialize item) in
        do! mem.CopyToAsync f |> Async.AwaitTask
    }

let ListItems (dir: string) : Async<MockItem array> = async {
    match (Directory.Exists dir) with
    | false -> return [| |]
    | true ->
        let tasks = Directory.GetFiles dir |>
                    Seq.map (fun f -> async {
                        use s = File.OpenRead f in
                        let! data = AsyncReadStream s in
                        let item = Serialize.Deserialize data in
                        return item
                    })
        in
        return! Async.Parallel tasks
}
