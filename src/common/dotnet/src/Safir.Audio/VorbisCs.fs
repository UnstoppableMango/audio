module Safir.Audio.VorbisCs

let toComment =
    function
    | Title v -> TitleComment(v) :> VorbisCommentCs
    | Version v -> VersionComment(v)
    | Album v -> AlbumComment(v)
    | TrackNumber v -> TrackNumberComment(v)
    | Artist v -> ArtistComment(v)
    | Performer v -> PerformerComment(v)
    | Copyright v -> CopyrightComment(v)
    | License v -> LicenseComment(v)
    | Organization v -> OrganizationComment(v)
    | Description v -> DescriptionComment(v)
    | Genre v -> GenreComment(v)
    | Date v -> DateComment(v)
    | Location v -> LocationComment(v)
    | Contact v -> ContactComment(v)
    | Isrc v -> IsrcComment(v)
    | Other (n, v) -> OtherComment(n, v)
