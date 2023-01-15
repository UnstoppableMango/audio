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

type VorbisCommentHeader =
    { VendorString: string
      UserComments: VorbisComment list }
