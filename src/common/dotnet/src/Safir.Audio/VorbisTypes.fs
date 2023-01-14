namespace Safir.Audio

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
    | Other of name: string * value: string

type VorbisCommentHeader =
    { VendorString: string
      UserComments: VorbisComment list }
