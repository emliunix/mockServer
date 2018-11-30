module MockItem

open System.Text
open System.Security.Cryptography
open Newtonsoft.Json

type Header = { name: string; value: string }
type Headers = Header list

type MockItem = {
    originUrl: string;
    hash: byte [];
    status: int;
    headers: Headers;
    content: byte [];
}

type QueryItem = {name: string; values: string list}
type Query = QueryItem list

let FormatQuery (query : Query) : string =
    query
    |> Seq.sortBy (fun q -> q.name)
    |> Seq.fold (fun acc q -> acc + (sprintf "%s %s" q.name (String.concat "," q.values))) ""

let BuildItemHash path query =
    use md5 = MD5.Create() in
        path + (FormatQuery query)
        |> Encoding.ASCII.GetBytes
        |> md5.ComputeHash

module Serialize =
    let Serialize (item: MockItem) =
        JsonConvert.SerializeObject item |>
        Encoding.UTF8.GetBytes

    let Deserialize (v: byte[]) : MockItem =
        v |>
        Encoding.UTF8.GetString |>
        JsonConvert.DeserializeObject<MockItem>
