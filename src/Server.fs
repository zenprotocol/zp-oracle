module Server

open FsNetMQ
open FSharp.Data.HttpRequestHeaders
open FSharp.Control.Reactive
open Infrastructure.Http
open Consensus
open FSharp.Data

module Actor = FsNetMQ.Actor 

let initHttpServer address =     
    Actor.create (fun shim ->
        use poller = Poller.create ()
        use observer = Poller.registerEndMessage poller shim
        
        use httpAgent = Server.create poller address
        
        use observer = 
            Server.observable httpAgent |>
            Observable.subscribeWithError (fun (request,reply) ->
                match request with
                | Get ("/auditpath", query) ->
                    match Map.tryFind "date" query, Map.tryFind "ticker" query with
                    | Some date, Some ticker ->
                        let data = 
                            date
                            |> Util.parseDate
                            |> Option.bind DataAccess.find
                            |> Option.map Array.toList
                            
                        let index = 
                            data
                            |> Option.map (List.map fst)
                            |> Option.map (List.findIndex ((=) ticker))
                            
                        let hashes = 
                            data
                            |> Option.map (List.map Merkle.hashLeaf)
                            
                        match Option.map2 MerkleTree.createAuditPath hashes index with
                        | Some data ->
                            data
                            |> List.toArray
                            |> Array.map Hash.toString
                            |> Array.map JsonValue.String
                            |> JsonValue.Array
                            |> JsonContent
                            |> reply StatusCode.OK
                        | None ->
                            reply StatusCode.BadRequest (TextContent "ticker not found or bad date format")
                    | _ ->
                        reply StatusCode.BadRequest (TextContent "missing timestamp and ticker")
                | _ -> 
                    reply StatusCode.NotFound NoContent                                                                     
            ) (fun error -> 
                printfn "error %A" error
                raise error    
            )
        
        Actor.signal shim
        Poller.run poller
    )
    |> ignore
