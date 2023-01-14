module Safir.Audio.Vorbis

open System
open System.Buffers.Binary
open System.Text

let private (|InvariantEqual|_|) (str: string) arg =
    if String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0 then
        Some()
    else
        None

let pVorbisComment (f: ReadOnlySpan<byte>) (length: int) =
    let s = Encoding.UTF8.GetString(f.Slice(0, length))
    let p = s.Split("=")

    if Array.length p = 2 then
        let name = p[0]
        let value = p[1]

        let res =
            match name with
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

        { Remaining = f.Slice(length)
          Result = Some res }
    else
        { Remaining = f; Result = None }

let pVorbisCommentHeader (f: ReadOnlySpan<byte>) (length: int) =
    if int64 length < ((pown 2L 32) - 1L) then
        // Read as uint32 because that's what the spec defines,
        // but cast to an int so we can slice with it
        let vl = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(0, 4)) |> int
        let vs = Encoding.UTF8.GetString(f.Slice(4, vl))

        let ls = vl + 4
        let ll = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(ls, 4))

        let mutable uc = List.empty
        let mutable o = ls + 4

        for i = 1 to int ll do
            let l = BinaryPrimitives.ReadUInt32LittleEndian(f.Slice(o, 4)) |> int
            let c = pVorbisComment (f.Slice(o + 4)) l

            match c.Result with
            | Some x -> uc <- x :: uc
            | None -> ()

            o <- o + 4 + l

        if f[o + 1] &&& 0x01uy = 0x01uy then
            { Remaining = f.Slice(length)
              Result =
                { VendorString = vs
                  UserComments = uc |> List.rev }
                |> Some }
        else
            { Remaining = f; Result = None }
    else
        { Remaining = f; Result = None }
