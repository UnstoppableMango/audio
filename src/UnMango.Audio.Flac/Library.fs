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
type MetadataBlockStreamInfo =
    { MinBlockSize: int
      MaxBlockSize: int
      MinFrameSize: int
      MaxFrameSize: int
      SampleRate: int
      Channels: int
      BitsPerSample: int
      TotalSamples: int64
      Md5Signature: string }

[<Struct>]
type MetadataBlockPadding = MetadataBlockPadding of int

[<Struct>]
type MetadataBlockApplication =
    { ApplicationId: int
      ApplicationData: byte array }

[<Struct>]
type SeekPoint =
    | Placeholder
    | SeekPoint of
        {| SampleNumber: int64
           StreamOffset: int64
           FrameSamples: int |}

[<Struct>]
type MetadataBlockSeekTable = { Points: SeekPoint list }

[<Struct>]
type MetadataBlockVorbisComment = MetadataBlockVorbisComment of int // VorbisCommentHeader

[<Struct>]
type CueSheetTrackIndex = { Offset: int64; IndexPoint: int }

[<Struct>]
type CueSheetTrack =
    { Offset: int64
      Number: int16
      Isrc: string option
      IsAudio: bool
      PreEmphasis: bool
      IndexPoints: CueSheetTrackIndex list }

// TODO: Discriminate between CD-DA cue-sheets
[<Struct>]
type MetadataBlockCueSheet =
    { CatalogNumber: string
      LeadInSamples: int64
      IsCompactDisc: bool
      TotalTracks: int
      Tracks: CueSheetTrack list }

type PictureType =
    | Other = 0
    | StandardFileIcon = 1
    | OtherFileIcon = 2
    | FrontCover = 3
    | BackCover = 4
    | LeafletPage = 5
    | Media = 6
    | LeadArtist = 7
    | Artist = 8
    | Conductor = 9
    | Band = 10
    | Composer = 11
    | Lyricist = 12
    | RecordingLocation = 13
    | DuringRecording = 14
    | DuringPerformance = 15
    | VideoScreenCapture = 16
    | ABrightColouredFish = 17
    | Illustration = 18
    | ArtistLogoType = 19
    | PublisherLogoType = 20

[<Struct>]
type MetadataBlockPicture =
    { Type: PictureType
      MimeType: string
      Description: string
      Width: int
      Height: int
      Depth: int
      Colors: int
      DataLength: int
      Data: byte array }

[<Struct>]
type MetadataBlockData =
    | StreamInfo of streamInfo: MetadataBlockStreamInfo
    | Padding of padding: MetadataBlockPadding
    | Application of application: MetadataBlockApplication
    | SeekTable of seekTable: MetadataBlockSeekTable
    | VorbisComment of vorbisComment: MetadataBlockVorbisComment
    | CueSheet of cueSheet: MetadataBlockCueSheet
    | Picture of picture: MetadataBlockPicture
    | Skipped of byte array

[<Struct>]
type FlacValue =
    | Magic
    | MetadataBlockHeader of header: MetadataBlockHeader
    | MetadataBlockData of data: MetadataBlockData

[<Struct>]
type State =
    | Magic
    | Todo

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
