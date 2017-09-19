//namespace Analytics
namespace Registration

type HotkeyRegistration(hotkey:string, commandName:string, usageCount:int) =

    let mutable _usageCount: int = usageCount
    let mutable _commandName: string = commandName
    let mutable _hotKey: string = hotkey

        


    member this.Hotkey
        with public get() = _hotKey
        and public set value =
            match value with
                | _ when value = _hotKey -> ()
                | _ -> 
                    _hotKey <- value

    member this.CommandName
        with public get() = _commandName
        and public set value =
            match value with
                | _ when value = _commandName -> ()
                | _ -> 
                    _commandName <- value

    member this.UsageCount
        with public get() = _usageCount
        and public set value =
            match value with
                | _ when value = _usageCount -> ()
                | _ -> 
                    _usageCount <- value
    