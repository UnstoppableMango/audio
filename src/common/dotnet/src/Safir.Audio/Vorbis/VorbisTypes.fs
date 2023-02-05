namespace Safir.Audio.Vorbis

type VorbisValue =
    | VendorLength = 20
    | VendorString = 21
    | UserCommentListLength = 22
    | UserCommentLength = 23
    | UserComment = 24

type VorbisComment =
    | Title of string
    | Version of string
    | Album of string
    | TrackNumber of string
    | Artist of string
    | Performer of string
    | Copyright of string
    | License of string
    | Organization of string
    | Description of string
    | Genre of string
    | Date of string
    | Location of string
    | Contact of string
    | Isrc of string
    | Other of Name: string * Value: string

type VorbisCommentHeader =
    { Vendor: string
      UserComments: VorbisComment list }
