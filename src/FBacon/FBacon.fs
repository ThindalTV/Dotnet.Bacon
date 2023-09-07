namespace FBacon

module FBacon =
    let GetBaconStrips (lua : string) : unit =
        let bs = System.Text.Encoding.UTF8.GetBytes lua
        (new NLua.Lua()).DoString(bs, "bacon.lua")
        |> ignore
