namespace Safir.Audio

open System
open System.Buffers.Binary
open System.Runtime.CompilerServices

[<Struct; IsByRefLike>]
type ParseResult<'a> =
    { Remaining: ReadOnlySpan<byte>
      Result: 'a option }

[<Struct; IsByRefLike>]
type ParseResultCs<'T>(remaining: ReadOnlySpan<byte>, result: 'T) =
    member _.Remaining = remaining
    member _.Result = result

module Parse =
    let map p r =
        { Remaining = r.Remaining
          Result = Option.map p r.Result }

    let private pad (span: ReadOnlySpan<byte>) (requested: int) =
        if span.Length >= requested then
            span
        else
            let a = requested - span.Length |> Array.zeroCreate<byte>
            let r = span.ToArray() |> Array.append a
            ReadOnlySpan<byte>(r)

    let uint16be (span: ReadOnlySpan<byte>) =
        let l = 2
        let b = pad span l
        let r = BinaryPrimitives.ReadUInt16BigEndian(b)

        { Remaining = span.Slice(l)
          Result = Some r }

    let uint64be (span: ReadOnlySpan<byte>) =
        let l = 8
        let b = pad span l
        let r = BinaryPrimitives.ReadUInt64BigEndian(b)

        { Remaining = span.Slice(l)
          Result = Some r }
