module Analysis

open System.Collections.Generic
open BclListUtil
open Registration


    let Analyze (hotkeyRegistrations:IEnumerable<HotkeyRegistration>)  =

        let groupGeneralize (commandName:string, group:HotkeyRegistration list) =
            let groupedUsage = group |> List.sumBy (fun x->x.UsageCount)
            let groupedHotkeyName = String.concat " || "  (group |> List.map (fun x-> x.Hotkey))
            HotkeyRegistration(groupedHotkeyName, commandName, groupedUsage)

        let commandNameSplitter (commandName:string) : string=
            (commandName.Split [|'.';'_'|]).[0]

        hotkeyRegistrations
        |> Microsoft.FSharp.Collections.List.ofSeq 
        |> List.groupBy (fun x->x.CommandName)
        |> List.map groupGeneralize
        |> List.sortBy (fun x-> ((commandNameSplitter x.CommandName), -x.UsageCount))
        |> (fun arr -> new System.Collections.Generic.List<HotkeyRegistration>(arr))
        
        