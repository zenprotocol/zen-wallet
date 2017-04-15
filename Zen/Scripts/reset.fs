module Test
open Zen
let run (app: App) = 
    app.ResetDB()
    app.AddGenesisBlock()
    app.Reconnect()
    0