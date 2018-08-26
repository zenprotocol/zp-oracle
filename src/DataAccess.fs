module DataAccess

open MongoDB.Bson
open MongoDB.Driver
open MongoDB.FSharp
open System

[<Literal>]
let ConnectionString = "mongodb://localhost"

[<Literal>]
let DbName = "oracle"

[<Literal>]
let CollectionName = "data"

type Data = { 
    id : BsonObjectId
    date: DateTime
    data : (string * decimal) array
} 

let client = MongoClient(ConnectionString)
let db = client.GetDatabase(DbName)
let collection = db.GetCollection<Data>(CollectionName)

let insert data =
   {
        id = MongoDB.Bson.BsonObjectId(MongoDB.Bson.ObjectId.GenerateNewId())
        date = DateTime.Today
        data = data
   }
   |> collection.InsertOne
   data

let find date = 
    let records = collection.Find(fun x -> x.date = date).ToEnumerable()
    
    if Seq.length records = 1 then
        (Seq.head records).data
        |> Some
    else
        None