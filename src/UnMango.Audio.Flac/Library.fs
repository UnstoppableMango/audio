namespace UnMango.Audio.Flac

open System.Buffers
open System.IO.Pipelines

type BlockType =
    | StreamInfo = 0
    | Padding = 1
    | Application = 2
    | SeekTable = 3
    | VorbisComment = 4
    | CueSheet = 5
    | Picture = 6
    | Invalid = 127

[<Struct>]
type MetadataBlockHeader =
    { LastBlock: bool
      BlockType: BlockType
      Length: uint }

[<Struct>]
type FlacValue = MetadataBlockHeader of MetadataBlockHeader

type ParseResult = (struct (ReadOnlySequence<byte> * FlacValue))
type Parse = ReadOnlySequence<byte> -> ParseResult
type Handle = FlacValue -> Async<unit>

module Flac =
    let parse (buffer: ReadOnlySequence<byte>) : ParseResult =
        buffer,
        MetadataBlockHeader
            { Length = 0u
              BlockType = BlockType.Invalid
              LastBlock = false }

type Flac =
    static member Read(reader: PipeReader, handle: Handle, cancellationToken) = async {
        let mutable completed = false

        while not completed do
            let! result = reader.ReadAsync(cancellationToken) |> _.AsTask() |> Async.AwaitTask
            let struct (buffer, value) = Flac.parse result.Buffer
            do! handle value
            reader.AdvanceTo(buffer.Start, buffer.End)
            completed <- result.IsCompleted
    }
