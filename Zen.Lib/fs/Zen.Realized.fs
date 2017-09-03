#light "off"
module Zen.Realized

type lockCore = {version: uint32; lockData: byte[] list}


type contract = {code: byte[]; bounds: byte[]; hint: byte[]}

type extendedContract =                                         
    | Contract of contract
    | HighVContract of version : uint32 * data : byte[]

type extraData = byte[] list
