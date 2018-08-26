module Merkle

open Infrastructure
open Consensus
open Hash
open Util

let hashLeaf (identifier, value:decimal) =
    [
        getBytes identifier
        [| (byte ';') |]
        BigEndianBitConverter.uint32ToBytes (1000u * uint32 value) 
    ]
    |> Array.concat
    |> Hash.compute
