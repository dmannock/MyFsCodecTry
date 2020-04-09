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

type ProductSkuAdded = {
    Id: Guid
    Name: string
    Price: decimal option
    SKU: string
    TimeStamp: DateTime
}

type ProductPriceAdjusted = {
    Id: Guid
    ProductId: Guid
    Price: decimal option
    TimeStamp: DateTime
}

let prodAddEvent = """{"id":"37a17c49-8d31-4cf3-860f-4fa916a6b815",
"name": "firstproduct",
"sku": "012-3456",
"timestamp": "2020-04-09 19:21:45"
}"""
//1586456505000

// FsCodec.NewtonsoftJson.Codec.Create<ProductSkuAdded>
let inline des<'t> x = Serdes.Deserialize<'t> x

[<EntryPoint>]
let main argv =
    let e = prodAddEvent |> des<ProductSkuAdded>
    printfn "Deserialized raw json to domain event: %A" e
    0
