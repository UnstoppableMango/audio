namespace Safir.Audio

open System
open System.Runtime.CompilerServices

[<Struct; IsReadOnly; IsByRefLike>]
type VorbisCommentValue =
    { Length: uint32
      Name: ReadOnlySpan<byte>
      Value: ReadOnlySpan<byte> }

[<Struct; IsReadOnly; IsByRefLike>]
type VorbisCommentHeaderValue =
    { VendorLength: uint32
      VendorString: ReadOnlySpan<byte>
      UserCommentListLength: uint32
      UserComments: ReadOnlySpan<byte> }

[<Struct; IsReadOnly; IsByRefLike>]
type VorbisCommentValueCs(comment: VorbisCommentValue) =
    member this.Length = comment.Length
    member this.Name = comment.Name
    member this.Value = comment.Value
