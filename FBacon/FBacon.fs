namespace FBacon

module FBacon =
    let GetBaconStrips () : unit =
        (new NLua.Lua()).DoFile("bacon.lua")
        |> ignore
        
