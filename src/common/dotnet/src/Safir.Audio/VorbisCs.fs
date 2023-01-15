module Safir.Audio.VorbisCs

let internal toCsComment =
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
