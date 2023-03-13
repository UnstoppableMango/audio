namespace Safir.Audio

open System
open System.Buffers.Binary

[<AutoOpen>]
module internal Helpers =
    let readUInt32 (buffer: ReadOnlySpan<byte>) =
        if buffer.Length > 4 then
            invalidOp "Buffer is too large for a uin32"
        else if buffer.Length = 4 then
            BinaryPrimitives.ReadUInt32BigEndian(buffer)
        else
            let mutable sum = 0u
            let num = buffer.Length - 1

            for i = 0 to num do
                let s = num - i
                sum <- sum + (uint buffer[i] <<< 8 * s)

            sum
