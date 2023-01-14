module Safir.Audio.FlacCs

let ParseMagic f =
    let res = Flac.pMagic f
    ParseResultCs(res.Remaining, res.Result |> Option.defaultValue null)

let ParseMetadataBlockHeader f =
    let res = Flac.pMetadataBlockHeader f
    ParseResultCs(res.Remaining, res.Result |> Option.get)

let ParseMetadataBlockStreamInfo f l =
    let res = Flac.pMetadataBlockStreamInfo f l
    ParseResultCs(res.Remaining, res.Result |> Option.get)

let ParseMetadataBlockPadding f l =
    let res = Flac.pMetadataBlockPadding f l
    ParseResultCs(res.Remaining, res.Result |> Option.get)

let ParseMetadataBlockApplication f l =
    let res = Flac.pMetadataBlockApplication f l
    ParseResultCs(res.Remaining, res.Result |> Option.get)

let ParseMetadataBlockSeekTable f l =
    let res = Flac.pMetadataBlockSeekTable f l
    ParseResultCs(res.Remaining, res.Result |> Option.get)

let private toCsComment =
    function
    | Title x -> TitleComment x :> VorbisCommentCs
    | Version x -> VersionComment x
    | Album x -> AlbumComment x
    | TrackNumber x -> TrackNumberComment x
    | Artist x -> ArtistComment x
    | Performer x -> PerformerComment x
    | Copyright x -> CopyrightComment x
    | License x -> LicenseComment x
    | Organization x -> OrganizationComment x
    | Description x -> DescriptionComment x
    | Genre x -> GenreComment x
    | Date x -> DateComment x
    | Location x -> LocationComment x
    | Contact x -> ContactComment x
    | Isrc x -> IsrcComment x
    | Other (n, v) -> OtherComment(n, v)

let ParseMetadataBlockVorbisComment f l =
    let res = Flac.pMetadataBlockVorbisComment f l

    ParseResultCs(
        res.Remaining,
        res.Result
        |> Option.map (fun x ->
            { VendorString = x.VendorString
              UserComments = x.UserComments |> List.map toCsComment })
        |> Option.get
    )
