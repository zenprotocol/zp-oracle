module Util

open System
open System.Text

let getBytes value = 
    ASCIIEncoding.ASCII.GetBytes (value:string)
    
let getEnv key = 
    let value = Environment.GetEnvironmentVariable(key)
    if String.IsNullOrEmpty value then
        failwith <| sprintf "missing environment variable: %s" key
    else
        value

let parseDate date =
    try
        DateTime.ParseExact(date, "yyyy-MM-dd", Globalization.CultureInfo.InvariantCulture)
        |> Some
    with _ ->
        None