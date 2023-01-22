namespace Safir.Audio

open System
open System.Runtime.CompilerServices

[<Struct; IsReadOnly; IsByRefLike>]
type VorbisCommentValue =
    { Length: uint32
      Name: ReadOnlySpan<byte>
      Value: ReadOnlySpan<byte> }

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

[<Struct; IsReadOnly; IsByRefLike>]
type VorbisCommentHeaderValue =
    { VendorLength: uint32
      VendorString: ReadOnlySpan<byte>
      UserCommentListLength: uint32
      UserComments: ReadOnlySpan<byte> }

type VorbisCommentHeader =
    { Vendor: string
      UserComments: VorbisComment list }
