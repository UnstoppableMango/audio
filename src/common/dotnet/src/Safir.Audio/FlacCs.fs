module Safir.Audio.FlacCs

let ReadMagic (r: byref<FlacStreamReader>) = Flac.readMagic &r

let ReadMetadataBlockHeader (r: byref<FlacStreamReader>) = Flac.readMetadataBlockHeader &r

let ReadMetadataBlockStreamInfo (r: byref<FlacStreamReader>) = Flac.readMetadataBlockStreamInfo &r

let ReadMetadataBlockPadding (r: byref<FlacStreamReader>) =
    let (MetadataBlockPadding length) = Flac.readMetadataBlockPadding &r
    { Length = length }

let ReadMetadataBlockApplication (r: byref<FlacStreamReader>) = Flac.readMetadataBlockApplication &r

let ReadMetadataBlockSeekTable (r: byref<FlacStreamReader>) =
    let result = Flac.readMetadataBlockSeekTable &r

    { Points =
        result.Points
        |> List.map (function
            | Placeholder ->
                { IsPlaceHolder = true
                  FrameSamples = 0
                  SampleNumber = 0
                  StreamOffset = 0 }
            | SeekPoint x ->
                { IsPlaceHolder = false
                  FrameSamples = x.FrameSamples
                  SampleNumber = x.SampleNumber
                  StreamOffset = x.StreamOffset })
        |> List.toSeq }

let ReadMetadataBlockVorbisComment (r: byref<FlacStreamReader>) =
    let (MetadataBlockVorbisComment result) = Flac.readMetadataBlockVorbisComment &r

    { Vendor = result.Vendor
      UserComments = result.UserComments |> List.map VorbisCs.toComment |> List.toSeq }

let ReadMetadataBlockPicture (r: byref<FlacStreamReader>) = Flac.readMetadataBlockPicture &r

// let ReadFlacStream f = Flac.readFlacStream f |> FlacStreamCs
