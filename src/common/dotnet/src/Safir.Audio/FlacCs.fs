module Safir.Audio.FlacCs

let private upcastBlockData =
    function
    | StreamInfo x -> x :> IMetadataBlockData
    | Padding x -> x
    | Application x -> x
    | SeekTable x -> x
    | VorbisComment x -> x

let ParseMagic f =
    Flac.pMagic f |> Option.defaultValue null

let ParseMetadataBlockHeader f =
    Flac.pMetadataBlockHeader f |> Option.get

let ParseMetadataBlockStreamInfo f =
    Flac.pMetadataBlockStreamInfo f |> Option.get

let ParseMetadataBlockPadding f l =
    Flac.pMetadataBlockPadding f l |> Option.get

let ParseMetadataBlockApplication f l =
    Flac.pMetadataBlockApplication f l |> Option.get

let ParseMetadataBlockSeekTable f l =
    Flac.pMetadataBlockSeekTable f l |> Option.get

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
    Flac.pMetadataBlockVorbisComment f l
    |> Option.map (fun x ->
        { VendorString = x.VendorString
          UserComments = x.UserComments |> List.map toCsComment })
    |> Option.get

let private toCsMetadataBlock block : MetadataBlockCs =
    { Header = block.Header
      Data = upcastBlockData block.Data }

let private toCsFlacStream stream : FlacStreamCs =
    { Metadata = stream.Metadata |> Seq.map toCsMetadataBlock }

let ParseFlacStream f =
    Flac.pFlacStream f |> Option.map toCsFlacStream |> Option.get
