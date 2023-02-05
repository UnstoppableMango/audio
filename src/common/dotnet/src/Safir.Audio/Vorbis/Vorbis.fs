module Safir.Audio.Vorbis.Vorbis

open System
open System.Buffers.Binary
open Safir.Audio

let private throw m : unit = invalidOp m

let private (|InvariantEqual|_|) (str: string) arg =
    if String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0 then
        Some()
    else
        None

let readComment (f: ReadOnlySpan<byte>) =
    let length = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(0, 4))
    let rest = f.Slice(4)

    let midIndex = rest.IndexOf(byte '=')

    if midIndex = -1 then throw "Invalid vorbis comment"

    let name = rest.Slice(0, midIndex)
    let value = rest.Slice(midIndex + 1, (int length) - midIndex - 1)

    readerEx "TODO"
    // { Length = length
    //   Name = name
    //   Value = value }

let readCommentHeader (f: ReadOnlySpan<byte>) (length: int) =
    if int64 length >= ((pown 2L 32) - 1L) then
        throw "Invalid vorbis comment header block length"

    // Read as uint32 because that's what the spec defines,
    // but cast to an int so we can slice with it
    let vendorLength = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(0, 4))
    let vendorLengthInt = vendorLength |> int
    let vendorString = f.Slice(4, vendorLengthInt)

    let listLength = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(vendorLengthInt + 4, 4))
    let listLengthInt = listLength |> int

    let listStart = vendorLengthInt + 8
    let mutable offset = listStart

    // TODO: Can we slice the span with comments without reading all of them?
    // for i = 0 to listLengthInt - 1 do
    //     let comment = readComment (f.Slice(offset))
    //     offset <- offset + 4 + (int comment.Length)

    let comments = f.Slice(listStart, offset - listStart)

    // TODO: Supposedly flac doesn't include the framing bit
    // https://xiph.org/flac/format.html#METADATA_BLOCK_VORBIS_COMMENT
    if f[offset + 1] &&& 0x01uy = 0x00uy then
        throw "Missing framing bit"

    readerEx "TODO"
    // { VendorLength = vendorLength
    //   VendorString = vendorString
    //   UserCommentListLength = listLength
    //   UserComments = comments }

let toComment (comment: string) =
    let split = comment.Split("=")

    match split with
    | [| name; value |] ->
        match value with
        | InvariantEqual "TITLE" -> Title value
        | InvariantEqual "VERSION" -> VorbisComment.Version value
        | InvariantEqual "ALBUM" -> Album value
        | InvariantEqual "TRACKNUMBER" -> TrackNumber value
        | InvariantEqual "ARTIST" -> Artist value
        | InvariantEqual "PERFORMER" -> Performer value
        | InvariantEqual "COPYRIGHT" -> Copyright value
        | InvariantEqual "LICENSE" -> License value
        | InvariantEqual "ORGANIZATION" -> Organization value
        | InvariantEqual "DESCRIPTION" -> Description value
        | InvariantEqual "GENRE" -> Genre value
        | InvariantEqual "DATE" -> Date value
        | InvariantEqual "LOCATION" -> Location value
        | InvariantEqual "CONTACT" -> Contact value
        | InvariantEqual "ISRC" -> Isrc value
        | _ -> Other(name, value)
        |> Some
    | _ -> None
