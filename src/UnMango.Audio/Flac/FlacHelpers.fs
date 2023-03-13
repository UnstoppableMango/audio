[<AutoOpen>]
module UnMMango.Audio.Flac.FlacHelpers

let internal flacEx m = raise (FlacStreamReaderException(m))
