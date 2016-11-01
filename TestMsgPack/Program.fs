// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

open MsgPack
open MsgPack.Serialization

[<EntryPoint>]
let main argv = 
    let array = [| [|0;1|] ; [|10;20|] |]
    let serializer = MessagePackSerializer.Get(SerializationContext())
    let stream = new System.IO.MemoryStream()
    serializer.Pack(stream,array)
    printfn "%A" <| stream.ToArray()
    printfn "%A" <| SerializationContext().ExtTypeCodeMapping
    //let result = serializer.Unpack(stream)
    //printfn "%A" result
    0 // return an integer exit code

