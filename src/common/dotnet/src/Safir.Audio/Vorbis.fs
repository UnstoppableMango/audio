module Safir.Audio.Vorbis

open System
open System.Buffers.Binary
open System.Text

let private throw m : unit = invalidOp m

let readVorbisComment (f: ReadOnlySpan<byte>) (length: int) =
    let midIndex = f.IndexOf(byte '=')

    if midIndex = -1 then throw "Invalid vorbis comment"

    let name = f.Slice(0, midIndex)
    let value = f.Slice(midIndex + 1, length - midIndex - 1)

    { Name = name.ToArray()
      Value = value.ToArray() }

let readVorbisCommentHeader (f: ReadOnlySpan<byte>) (length: int) =
    if int64 length >= ((pown 2L 32) - 1L) then
        throw "Invalid vorbis comment header block length"

    // Read as uint32 because that's what the spec defines,
    // but cast to an int so we can slice with it
    let vendorLength = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(0, 4)) |> int
    let vendorString = f.Slice(4, vendorLength).ToArray()

    let listStart = vendorLength + 4
    let listLength = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(listStart, 4)) |> int

    let mutable comments = Array.zeroCreate listLength
    let mutable offset = listStart + 4

    for i = 0 to listLength - 1 do
        let length = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(offset, 4)) |> int
        offset <- offset + 4

        comments[i] <- readVorbisComment (f.Slice(offset)) length
        offset <- offset + length

    // TODO: Supposedly flac doesn't include the framing bit
    // https://xiph.org/flac/format.html#METADATA_BLOCK_VORBIS_COMMENT
    if f[offset + 1] &&& 0x01uy = 0x00uy then
        throw "Missing framing bit"

    { VendorString = vendorString
      UserComments = comments }
