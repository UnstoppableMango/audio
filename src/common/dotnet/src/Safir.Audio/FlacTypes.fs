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

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockHeaderValue =
    { LastBlock: bool
      BlockType: BlockType
      Length: int }

type MetadataBlockHeader = { BlockType: BlockType }

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

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockPaddingValue = MetadataBlockPaddingValue of int

type MetadataBlockPadding = MetadataBlockPadding of int

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockApplicationValue =
    { ApplicationId: uint32
      ApplicationData: ReadOnlySpan<byte> }

type MetadataBlockApplication =
    { ApplicationId: int
      ApplicationData: byte array }

[<Struct; IsReadOnly; IsByRefLike>]
type SeekPointValue =
    { SampleNumber: uint64
      StreamOffset: uint64
      FrameSamples: uint16 }

type DefinedSeekPoint =
    { SampleNumber: int64
      StreamOffset: int64
      FrameSamples: int }

type SeekPoint =
    | Placeholder
    | SeekPoint of DefinedSeekPoint

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockSeekTableValue =
    { Count: int
      SeekPoints: ReadOnlySpan<byte> }

type MetadataBlockSeekTable = { Points: SeekPoint list }

type MetadataBlockVorbisCommentValue = VorbisCommentHeaderValue

type MetadataBlockVorbisComment = MetadataBlockVorbisComment of VorbisCommentHeader

[<Struct; IsReadOnly; IsByRefLike>]
type CueSheetTrackIndexValue = { Offset: uint64; IndexPoint: uint16 }

type CueSheetTrackIndex = { Offset: int64; IndexPoint: int }

[<Struct; IsReadOnly; IsByRefLike>]
type CueSheetTrackValue =
    { Offset: uint64
      Number: uint16
      Isrc: ReadOnlySpan<byte>
      IsAudio: bool
      PreEmphasis: bool
      IndexPoints: uint16
      TrackIndexPoints: ReadOnlySpan<byte> }

type CueSheetTrack =
    { Offset: int64
      Number: int16
      Isrc: string option
      IsAudio: bool
      PreEmphasis: bool
      IndexPoints: CueSheetTrackIndex list }

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockCueSheetValue =
    { CatalogNumber: ReadOnlySpan<byte>
      LeadInSamples: uint64
      IsCompactDisc: bool
      TotalTracks: uint16
      Tracks: ReadOnlySpan<byte> }

// TODO: Discriminate between CD-DA cue-sheets
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

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockDataValue =
    | StreamInfo of StreamInfo: MetadataBlockStreamInfoValue
    | Padding of Padding: MetadataBlockPaddingValue
    | Application of Application: MetadataBlockApplicationValue
    | SeekTable of SeekTable: MetadataBlockSeekTableValue
    | VorbisComment of VorbisComment: MetadataBlockVorbisCommentValue
    | CueSheet of CueSheet: MetadataBlockCueSheetValue
    | Picture of Picture: MetadataBlockPictureValue
    | Skipped of ReadOnlySpan<byte>

type MetadataBlockData =
    | StreamInfo of MetadataBlockStreamInfo
    | Padding of MetadataBlockPadding
    | Application of MetadataBlockApplication
    | SeekTable of MetadataBlockSeekTable
    | VorbisComment of MetadataBlockVorbisComment
    | CueSheet of MetadataBlockCueSheet
    | Picture of MetadataBlockPicture
    | Skipped

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockValue =
    { Header: MetadataBlockHeaderValue
      Data: MetadataBlockDataValue }

type MetadataBlock =
    { Header: MetadataBlockHeader
      Data: MetadataBlockData }

type FlacStream = { Metadata: MetadataBlock list }
