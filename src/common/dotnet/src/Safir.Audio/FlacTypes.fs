namespace Safir.Audio

type BlockType =
    | StreamInfo = 0
    | Padding = 1
    | Application = 2
    | SeekTable = 3
    | VorbisComment = 4
    | CueSheet = 5
    | Picture = 6
    | Invalid = 127

type MetadataBlockHeader =
    { LastBlock: bool
      BlockType: BlockType
      Length: int }

type IMetadataBlockData =
    interface
    end

type MetadataBlockStreamInfo =
    { MinBlockSize: uint16
      MaxBlockSize: uint16
      MinFrameSize: uint
      MaxFrameSize: uint
      SampleRate: uint
      Channels: uint16
      BitsPerSample: uint16
      TotalSamples: uint64
      Md5Signature: string }

    interface IMetadataBlockData

type MetadataBlockPadding =
    | MetadataBlockPadding of int

    interface IMetadataBlockData

type MetadataBlockApplication =
    { ApplicationId: int
      ApplicationData: byte [] }

    interface IMetadataBlockData

// TODO: How to define placeholder seek points?
type SeekPoint =
    { SampleNumber: uint64
      StreamOffset: uint64
      FrameSamples: uint16 }

type MetadataBlockSeekTable =
    | MetadataBlockSeekTable of SeekPoint list

    interface IMetadataBlockData

[<AbstractClass>]
type VorbisCommentCs(name: string, value: string) =
    member this.Name = name
    member this.Value = value

type TitleComment(value: string) =
    inherit VorbisCommentCs("TITLE", value)

type VersionComment(value: string) =
    inherit VorbisCommentCs("VERSION", value)

type AlbumComment(value: string) =
    inherit VorbisCommentCs("ALBUM", value)

type TrackNumberComment(value: string) =
    inherit VorbisCommentCs("TRACKNUMBER", value)

type ArtistComment(value: string) =
    inherit VorbisCommentCs("ARTIST", value)

type PerformerComment(value: string) =
    inherit VorbisCommentCs("PERFORMER", value)

type CopyrightComment(value: string) =
    inherit VorbisCommentCs("COPYRIGHT", value)

type LicenseComment(value: string) =
    inherit VorbisCommentCs("LICENSE", value)

type OrganizationComment(value: string) =
    inherit VorbisCommentCs("ORGANIZATION", value)

type DescriptionComment(value: string) =
    inherit VorbisCommentCs("DESCRIPTION", value)

type GenreComment(value: string) =
    inherit VorbisCommentCs("GENRE", value)

type DateComment(value: string) =
    inherit VorbisCommentCs("DATE", value)

type LocationComment(value: string) =
    inherit VorbisCommentCs("LOCATION", value)

type ContactComment(value: string) =
    inherit VorbisCommentCs("Contact", value)

type IsrcComment(value: string) =
    inherit VorbisCommentCs("ISRC", value)

type OtherComment(name: string, value: string) =
    inherit VorbisCommentCs(name, value)

type MetadataBlockVorbisComment<'a> =
    { VendorString: string
      UserComments: 'a list }

    interface IMetadataBlockData

type CueSheetTrackIndex =
    { Offset: uint64
      IndexPoint: uint16 }

type CueSheetTrack =
    { Offset: uint64
      Number: uint16
      Isrc: string option
      IsAudio: bool
      PreEmphasis: bool
      IndexPoints: uint16
      TrackIndexPoints: CueSheetTrackIndex list }

type MetadataBlockCueSheet =
    { CatalogNumber: string
      LeadInSamples: uint64
      IsCompactDisc: bool
      TotalTracks: uint16
      Tracks: CueSheetTrack list }

    interface IMetadataBlockData

type MetadataBlockData =
    | StreamInfo of MetadataBlockStreamInfo
    | Padding of MetadataBlockPadding
    | Application of MetadataBlockApplication
    | SeekTable of MetadataBlockSeekTable
    | VorbisComment of MetadataBlockVorbisComment<VorbisComment>
