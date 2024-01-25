[<AutoOpen>]
module UnMango.Audio.Flac.FlacHelpers

let internal flacEx m = raise (FlacStreamReaderException(m))
