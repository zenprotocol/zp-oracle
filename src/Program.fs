module Main

open System
open FSharp.Configuration
open FSharp.Data
open Infrastructure
open Result
open Consensus
open Serialization
open FsBech32
open Json
open Http
open Util
open Server
open Api.Types

type Config = YamlConfig<"config.yaml">
    
[<EntryPoint>]
let main _ = 
    let intrinioUserName = getEnv "intrinio_user_name"
    let intrinioPassword = getEnv "intrinio_password"
    let zenPassword = getEnv "zen_account_password" 
    
    let basicAuthHeader = 
        sprintf "%s:%s" intrinioUserName intrinioPassword
        |> getBytes
        |> Convert.ToBase64String
        
    let config = new Config()
    
    config.Load("config.yaml")
    
    initHttpServer config.api

    while true do
        printfn "fetching..."

        Exception.resultWrap<String> (fun _ -> 
            Http.RequestString (
                "https://api.intrinio.com/data_point",
                httpMethod = "GET",
                query   = [ 
                    "identifier", String.Join(",", config.tickers)
                    "item", "close_price"
                ],
                headers = [ 
                    "Authorization", basicAuthHeader 
                ]
            )) "error getting provider data"
        >>= (fun raw -> 
            printfn "parsing..."

            Exception.resultWrap<RawResultJson.Root> (
                fun _ -> RawResultJson.Parse(raw)) "error parsing provider data")
        <@> fun x -> x.Data
        <@> Array.map (fun x -> x.Identifier, x.Value)
        <@> (fun data ->
            data
            |> Array.map Merkle.hashLeaf
            |> Array.toList
            |> MerkleTree.computeRoot
            |> Hash.bytes
            |> Zen.Types.Data.data.Hash
            |> Data.serialize
            |> Base16.encode
            |> (fun messageBody ->
                (new ContractExecuteRequestJson.Root(
                    config.contract, 
                    "Add", 
                    messageBody,
                    new ContractExecuteRequestJson.Options(false, "m/44'/258'/0'/3/0"),
                    Array.empty,
                    zenPassword
                )))
            |> (fun json ->
                printfn "making a commitment on the blockchain..."
    
                Exception.resultWrap<HttpResponse> (fun _ -> 
                    (sprintf "http://%s/wallet/contract/execute" config.nodeApi)
                    |> json.JsonValue.Request) "error communicating with zen-node"
                )
            >>= (fun response ->
                if response.StatusCode <> 200 then 
                    sprintf "could not execute contract: %A" response.Body
                    |> Error
                else
                    Ok data)
            <@> DataAccess.insert
        )
        |> Result.mapError (printfn "%A")
        |> ignore
            
        printfn "waiting..."
        Threading.Thread.Sleep (1000 * 60 * 60 * config.interval)
    0