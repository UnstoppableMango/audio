namespace Safir.Audio

type VorbisComment = { Name: byte []; Value: byte [] }

type VorbisCommentCs(comment: VorbisComment) =
    member this.Name = comment.Name
    member this.Value = comment.Value

type VorbisCommentHeader =
    { VendorString: byte []
      UserComments: VorbisComment array }
