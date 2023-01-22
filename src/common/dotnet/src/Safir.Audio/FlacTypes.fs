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
type MetadataBlockHeader =
    { LastBlock: bool
      BlockType: BlockType
      Length: int }

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockStreamInfo =
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
type MetadataBlockPadding = MetadataBlockPadding of int

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockApplication =
    { ApplicationId: uint32
      ApplicationData: ReadOnlySpan<byte> }

// TODO: How to define placeholder seek points?
[<Struct; IsReadOnly>]
type SeekPoint =
    { SampleNumber: uint64
      StreamOffset: uint64
      FrameSamples: uint16 }

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockSeekTable =
    { Count: int
      SeekPoints: ReadOnlySpan<byte> }

type MetadataBlockVorbisComment = VorbisCommentHeader

[<Struct; IsReadOnly; IsByRefLike>]
type CueSheetTrackIndex = { Offset: uint64; IndexPoint: uint16 }

[<Struct; IsReadOnly; IsByRefLike>]
type CueSheetTrack =
    { Offset: uint64
      Number: uint16
      Isrc: ReadOnlySpan<byte>
      IsAudio: bool
      PreEmphasis: bool
      IndexPoints: uint16
      TrackIndexPoints: ReadOnlySpan<byte> }

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockCueSheet =
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
type MetadataBlockPicture =
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
type MetadataBlockData =
    | StreamInfo of streamInfo: MetadataBlockStreamInfo
    | Padding of padding: MetadataBlockPadding
    | Application of application: MetadataBlockApplication
    | SeekTable of seekTable: MetadataBlockSeekTable
    | VorbisComment of vorbisComment: MetadataBlockVorbisComment
    | CueSheet of cueSheet: MetadataBlockCueSheet
    | Picture of picture: MetadataBlockPicture
    | Skipped of ReadOnlySpan<byte>

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlock =
    { Header: MetadataBlockHeader
      Data: MetadataBlockData }

type FlacStream = { Metadata: List<Object> }
