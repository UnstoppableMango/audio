[<AutoOpen>]
module Safir.Audio.Flac.FlacHelpers

let internal flacEx m = raise (FlacStreamReaderException(m))
