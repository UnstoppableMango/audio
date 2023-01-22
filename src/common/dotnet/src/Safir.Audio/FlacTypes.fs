namespace Safir.Audio

open System
open System.Runtime.CompilerServices

type BlockType =
    | StreamInfo = 0
    | Padding = 1
    | Application = 2
    | SeekTable = 3
    | VorbisComment = 4
    | CueSheet = 5
    | Picture = 6
    | Invalid = 127

[<Struct; IsReadOnly>]
type MetadataBlockHeaderValue =
    { LastBlock: bool
      BlockType: BlockType
      Length: int }

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockStreamInfoValue =
    { MinBlockSize: uint16
      MaxBlockSize: uint16
      MinFrameSize: uint32
      MaxFrameSize: uint32
      SampleRate: uint32
      Channels: uint16
      BitsPerSample: uint16
      TotalSamples: uint64
      Md5Signature: ReadOnlySpan<byte> }

[<Struct; IsReadOnly>]
type MetadataBlockPaddingValue = MetadataBlockPaddingValue of int

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockApplicationValue =
    { ApplicationId: uint32
      ApplicationData: ReadOnlySpan<byte> }

// TODO: How to define placeholder seek points?
[<Struct; IsReadOnly>]
type SeekPointValue =
    { SampleNumber: uint64
      StreamOffset: uint64
      FrameSamples: uint16 }

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockSeekTableValue =
    { Count: int
      SeekPoints: ReadOnlySpan<byte> }

type MetadataBlockVorbisCommentValue = VorbisCommentHeaderValue

[<Struct; IsReadOnly; IsByRefLike>]
type CueSheetTrackIndexValue = { Offset: uint64; IndexPoint: uint16 }

[<Struct; IsReadOnly; IsByRefLike>]
type CueSheetTrackValue =
    { Offset: uint64
      Number: uint16
      Isrc: ReadOnlySpan<byte>
      IsAudio: bool
      PreEmphasis: bool
      IndexPoints: uint16
      TrackIndexPoints: ReadOnlySpan<byte> }

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockCueSheetValue =
    { CatalogNumber: ReadOnlySpan<byte>
      LeadInSamples: uint64
      IsCompactDisc: bool
      TotalTracks: uint16
      Tracks: ReadOnlySpan<byte> }

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

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockPictureValue =
    { Type: PictureType
      MimeLength: uint32
      MimeType: ReadOnlySpan<byte>
      DescriptionLength: uint32
      Description: ReadOnlySpan<byte>
      Width: uint32
      Height: uint32
      Depth: uint32
      Colors: uint32
      DataLength: uint32
      Data: ReadOnlySpan<byte> }

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockDataValue =
    | StreamInfo of streamInfo: MetadataBlockStreamInfoValue
    | Padding of padding: MetadataBlockPaddingValue
    | Application of application: MetadataBlockApplicationValue
    | SeekTable of seekTable: MetadataBlockSeekTableValue
    | VorbisComment of vorbisComment: MetadataBlockVorbisCommentValue
    | CueSheet of cueSheet: MetadataBlockCueSheetValue
    | Picture of picture: MetadataBlockPictureValue
    | Skipped of ReadOnlySpan<byte>

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockValue =
    { Header: MetadataBlockHeaderValue
      Data: MetadataBlockDataValue }

type FlacStream = { Metadata: List<Object> }
