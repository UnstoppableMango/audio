namespace UnMango.Audio

module internal Option =
    let apply fOpt xOpt =
        match fOpt, xOpt with
        | Some f, Some x -> Some(f x)
        | _ -> None

module internal List =
    let traverseOptionA f list =
        let (<*>) = Option.apply
        let retn = Option.Some

        let cons h t = h :: t

        let initState = retn []

        let folder h t =
            retn cons
            <*> (f h)
            <*> t

        List.foldBack folder list initState

    let traverseOptionM f list =
        let (>>=) x f = Option.bind f x
        let retn = Option.Some

        let cons h t = h :: t

        let initState = retn []

        let folder head tail =
            f head
            >>= (fun h ->
                tail
                >>= (fun t -> retn (cons h t)))

        List.foldBack folder list initState

    let sequenceOptionA x = traverseOptionA id x

    let sequenceOptionM x = traverseOptionM id x
