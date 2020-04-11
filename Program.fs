//Gave up running in fsi after adding these & needing more
//NOTE: 11.0.2 ver
// #r @"C:\Users\Dan\.nuget\packages\newtonsoft.json\11.0.2\lib\netstandard2.0\Newtonsoft.Json.dll"
// #r @"C:\Users\Dan\.nuget\packages\fscodec\2.0.1\lib\netstandard2.0\FsCodec.dll"
// #r @"C:\Users\Dan\.nuget\packages\fscodec.newtonsoftjson\2.0.1\lib\netstandard2.0\FsCodec.NewtonsoftJson.dll"

open System
open Newtonsoft.Json
open Newtonsoft
open FsCodec
open FsCodec.NewtonsoftJson

let inline des<'t> x = Serdes.Deserialize<'t> x

module Events =
//TODO: check Guid types either JsonIsomorphism or tagged string (UMX)
// using Newtonsoft wich already handles guids fine...
    type ProductSkuAdded = {
        Id: Guid
        Name: string
        Price: decimal option
        Sku: string
        TimeStamp: DateTime
    }

    type ProductPriceAdjusted = {
        Id: Guid
        Sku: string
        Price: decimal
        Timestamp: DateTime
    }

    type Event = 
        | ProductSkuAdded of ProductSkuAdded
        | ProductPriceAdjusted of ProductPriceAdjusted
        | UnhandledEvent
        interface TypeShape.UnionContract.IUnionContract

    let codec = FsCodec.NewtonsoftJson.Codec.Create<Event>() 
    let decode = codec.TryDecode 

let prodAddEvent = """
{"id":"37a17c49-8d31-4cf3-860f-4fa916a6b815",
"name": "firstproduct",
"sku": "012-3456",
"timestamp": "2020-04-09 19:21:45"
}"""
//1586456505000

let prodPriceAdjustedEvent = """
{"id":"b5ddc517-aeb5-4fb0-a659-488fd4f1dcc7",
"sku": "012-3456",
"Price": 99.99,
"timestamp": "2020-04-09 19:21:46"
}"""

let utf8 (s : string) = System.Text.Encoding.UTF8.GetBytes(s)
let events = [
    FsCodec.Core.TimelineEvent.Create(0L, "ProductSkuAdded", utf8 prodAddEvent)
    FsCodec.Core.TimelineEvent.Create(1L, "ProductPriceAdjusted", utf8 prodPriceAdjustedEvent)
    FsCodec.Core.TimelineEvent.Create(2L, "UnhandledEvent", utf8 "")
]

open Events
let handler =
    function
    | Some(ProductSkuAdded(e)) -> printfn "handing ProductSkuAdded: %A" e
    | Some(ProductPriceAdjusted(e)) -> printfn "handing ProductPriceAdjusted: %A" e
    | Some(_) -> printfn "unhandled event"
    | None -> printfn "unknown event"

[<EntryPoint>]
let main argv =
    let e = prodAddEvent |> des<Events.ProductSkuAdded>
    printfn "Deserialized raw json to domain event. Options & casing handled:\n%A" e
    printfn "Decoded events:"
    events
    //TODO: decoding error handling - pass in ILogger
    |> List.map Events.decode
    |> List.iter handler
    0
