module Json

open FSharp.Data

type RawResultJson = JsonProvider<"""
{
    "data": [
        {
            "identifier": "AAPL",
            "value": 208.22
        }
    ]
}""">

type AuditPathResponseJson = JsonProvider<"""
{
    "auditPath": [
        "abcd123"
    ]
}""">
