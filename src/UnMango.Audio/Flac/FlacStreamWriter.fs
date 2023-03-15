namespace UnMango.Audio.Flac

#nowarn "3391"

open System
open System.Buffers
open System.Buffers.Binary
open System.Text

type FlacStreamWriter(output: IBufferWriter<byte>) =
    member this.WriteUserCommentListLength(length: int) =
        let lengthSize = 4
        let span = output.GetSpan(lengthSize)
        BinaryPrimitives.WriteInt32LittleEndian(span, length)
        output.Advance(lengthSize)

    member this.WriteComment(key: string, value: string) =
        let comment = (key + "=" + value).AsSpan() // TODO: Interpolation is broke for some reason
        let lengthSize = 4
        let lengthSpan = output.GetSpan(lengthSize)
        BinaryPrimitives.WriteInt32LittleEndian(lengthSpan, comment.Length)
        output.Advance(lengthSize)
        0L < Encoding.UTF8.GetBytes(comment, output)
