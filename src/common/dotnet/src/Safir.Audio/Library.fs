namespace Safir.Audio

module Option =
    let apply fOpt xOpt =
        match fOpt,xOpt with
        | Some f, Some x -> Some (f x)
        | _ -> None

    let (<!>) = Option.map
    let (<*>) = apply
