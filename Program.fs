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
        Timestamp: DateTime
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
    let (|Decode|_|) = codec.TryDecode 

module ProductProjection =
    open Events

    type Product = {
        Sku: string
        Name: string
        Price: decimal option
        LastUpdated: DateTime option
        Version: int64
    }
    let private empty = {
        Sku = ""
        Name = ""
        Price = None
        LastUpdated = None
        Version = int64 0
    }

    let applyEvent model (event: Event) =
        match event with
        | ProductSkuAdded e -> 
            { model with 
                Sku = e.Sku
                Name = e.Name
                Price = e.Price
                LastUpdated = Some e.Timestamp
                Version = model.Version + int64 1 }
        | ProductPriceAdjusted e ->
            { model with 
                Price = Some e.Price
                LastUpdated = Some e.Timestamp
                Version = model.Version + int64 1 }
        | UnhandledEvent -> model

    let fromHistory = List.fold applyEvent     

    let initialState = Map.empty<string, Product>

    let get state streamId = 
        state
        |> Map.tryFind streamId 
        |> Option.defaultValue empty

    let evolve state (streamId, events) = 
        let currentProduct = get state streamId
        state |> Map.add streamId (fromHistory currentProduct events)        

    let evolveSingle state (streamId, event) = 
        let currentProduct = get state streamId
        state |> Map.add streamId (applyEvent currentProduct event)        

let prodAddEvent = """
{"id":"37a17c49-8d31-4cf3-860f-4fa916a6b815",
"name": "firstproduct",
"sku": "012-3456",
"timestamp": "2020-04-09 19:21:45"
}"""

let prodPriceAdjustedEvent = """
{"id":"b5ddc517-aeb5-4fb0-a659-488fd4f1dcc7",
"sku": "012-3456",
"Price": 99.99,
"timestamp": "2020-04-09 19:21:46"
}"""

let prodAddEvent2 = """
{"id":"84050f10-cb42-4223-8aed-e38c97929242",
"name": "secondproduct",
"sku": "023-4567",
"timestamp": "2020-04-17 19:21:45"
}"""

let utf8 (s : string) = System.Text.Encoding.UTF8.GetBytes(s)
let events = [
    StreamName.parse "Product-5e29e373-d092-4bc0-94d9-3cf8e48ed5fb", FsCodec.Core.TimelineEvent.Create(0L, "ProductSkuAdded", utf8 prodAddEvent)
    StreamName.parse "Product-5e29e373-d092-4bc0-94d9-3cf8e48ed5fb", FsCodec.Core.TimelineEvent.Create(1L, "ProductPriceAdjusted", utf8 prodPriceAdjustedEvent)
    StreamName.parse "Product-5e29e373-d092-4bc0-94d9-3cf8e48ed5fb", FsCodec.Core.TimelineEvent.Create(2L, "UnhandledEvent", utf8 "")
    StreamName.parse "Product-c7718eda-e628-4633-a082-62ccd29c7c76", FsCodec.Core.TimelineEvent.Create(0L, "ProductSkuAdded", utf8 prodAddEvent2)
]

[<EntryPoint>]
let main argv =
    let e = prodAddEvent |> des<Events.ProductSkuAdded>
    printfn "Deserialized raw json to domain event. Options & casing handled:\n%A" e
    printfn "Decoded events:"
    let decoded = 
        events
        //TODO: decoding error handling - pass in ILogger
        |> List.choose (fun (stream, e) ->
            match stream, e with
            | StreamName.CategoryAndId(_, id), (Events.Decode e) -> Some(id, e)
            | _ ->
                //simulate logging
                printfn "Failed to deserialize StreamName=%s Index=%d Event=%A CausationId=%s CorrelationId=%s"
                    (StreamName.toString stream) e.Index e.EventType e.CausationId e.CorrelationId
                None)
    let finalState = decoded |> List.fold ProductProjection.evolveSingle ProductProjection.initialState 
    printfn "Final state: %A" finalState
    0