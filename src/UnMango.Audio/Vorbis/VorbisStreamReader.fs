namespace UnMango.Audio.Vorbis

open System
open System.Runtime.CompilerServices

[<Struct; IsByRefLike>]
type VorbisStreamReader =
    val private _buffer: ReadOnlySpan<byte>

    val mutable private _valueType: ValueOption<VorbisValue>
    val mutable private _consumed: int
    val mutable private _value: ReadOnlySpan<byte>
