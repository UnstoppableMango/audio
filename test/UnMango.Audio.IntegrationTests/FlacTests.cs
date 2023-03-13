using UnMango.Audio.Flac;

namespace UnMango.Audio.IntegrationTests;

[Trait("Category", "Unit")]
public class FlacTests
{
    private const string FileName = "NEFFEX-Flirt";

    [Fact]
    public void MagicNumber_FlacFile()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        var res = FlacCs.ReadMagic(ref reader);

        Assert.Equal("fLaC", res);
    }

    [Fact]
    public void MagicNumber_Mp3File()
    {
        Assert.Throws<FlacStreamReaderException>(() => {
            ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.mp3");
            var reader = new FlacStreamReader(file);

            FlacCs.ReadMagic(ref reader);
        });
    }

    [Fact]
    public void ReadMetadataBlockHeader_StreamInfo()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file[4..], FlacStreamState.StreamInfoHeader);

        var res = FlacCs.ReadMetadataBlockHeader(ref reader);

        // Assert.Equal(34, res.Length);
        // Assert.False(res.LastBlock);
        Assert.Equal(BlockType.StreamInfo, res.BlockType);
    }

    [Fact]
    public void ReadMetadataBlockHeader_Padding()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file[80_524..], FlacStreamState.AfterBlockData());

        var res = FlacCs.ReadMetadataBlockHeader(ref reader);

        // Assert.Equal(16384, res.Length);
        // Assert.True(res.LastBlock);
        Assert.Equal(BlockType.Padding, res.BlockType);
    }

    [Fact(Skip = "No test files with application meta yet")]
    public void ReadMetadataBlockHeader_Application()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file[69_420..]);

        var application = FlacCs.ReadMetadataBlockApplication(ref reader);

        // Assert.Equal(69u, application.Id);
        // Assert.Equal(420, application.Data.Length);
    }

    [Fact]
    public void ReadMetadataBlockHeader_SeekTable()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file[42..], FlacStreamState.AfterBlockData());

        var res = FlacCs.ReadMetadataBlockHeader(ref reader);

        // Assert.Equal(288, res.Length);
        // Assert.False(res.LastBlock);
        Assert.Equal(BlockType.SeekTable, res.BlockType);
    }

    [Fact]
    public void ReadMetadataBlockHeader_VorbisComment()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file[334..], FlacStreamState.AfterBlockData());

        var res = FlacCs.ReadMetadataBlockHeader(ref reader);

        // Assert.Equal(294, res.Length);
        // Assert.False(res.LastBlock);
        Assert.Equal(BlockType.VorbisComment, res.BlockType);
    }

    [Fact]
    public void ReadMetadataBlockHeader_Picture()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file[632..], FlacStreamState.AfterBlockData());

        var res = FlacCs.ReadMetadataBlockHeader(ref reader);

        // Assert.Equal(79_888, res.Length);
        // Assert.False(res.LastBlock);
        Assert.Equal(BlockType.Picture, res.BlockType);
    }

    [Fact]
    public void ReadMetadataBlockStreamInfo()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file[8..], FlacStreamState.AfterBlockHeader(false, 34, BlockType.StreamInfo));

        var streamInfo = FlacCs.ReadMetadataBlockStreamInfo(ref reader);

        Assert.Equal(4096, streamInfo.MinBlockSize);
        Assert.Equal(4096, streamInfo.MaxBlockSize);
        Assert.Equal(1781, streamInfo.MinFrameSize);
        Assert.Equal(14163, streamInfo.MaxFrameSize);
        Assert.Equal(44100, streamInfo.SampleRate);
        Assert.Equal(2, streamInfo.Channels);
        Assert.Equal(16, streamInfo.BitsPerSample);
        Assert.Equal(7028438u, streamInfo.TotalSamples);
        Assert.Equal("3c16b5b7186537d6823c7be62fe8c661", streamInfo.Md5Signature, ignoreCase: true);
    }

    [Fact]
    public void ReadMetadataBlockPadding()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file[80_528..], FlacStreamState.AfterBlockHeader(true, 16_384, BlockType.Padding));

        var res = FlacCs.ReadMetadataBlockPadding(ref reader);

        Assert.Equal(16_384, res.Length);
    }

    [Fact]
    public void ReadMetadataBlockSeekTable()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file[46..], FlacStreamState.AfterBlockHeader(false, 288, BlockType.SeekTable));

        var res = FlacCs.ReadMetadataBlockSeekTable(ref reader);
        var points = res.Points.ToList();

        Assert.Equal(16, points.Count);

        var first = points[0];
        Assert.Equal(0, first.SampleNumber);
        Assert.Equal(0, first.StreamOffset);
        Assert.Equal(4096, first.FrameSamples);

        var second = points[1];
        Assert.Equal(438_272, second.SampleNumber);
        Assert.Equal(532_963, second.StreamOffset);
        Assert.Equal(4096, second.FrameSamples);

        var third = points[2];
        Assert.Equal(880_640, third.SampleNumber);
        Assert.Equal(1_732_918, third.StreamOffset);
        Assert.Equal(4096, third.FrameSamples);

        var last = points.Last();
        Assert.Equal(6_610_944, last.SampleNumber);
        Assert.Equal(1_8894_059, last.StreamOffset);
        Assert.Equal(4096, last.FrameSamples);
    }

    [Fact]
    public void ReadMetadataBlockVorbisComment()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file[338..], FlacStreamState.AfterBlockHeader(false, 294, BlockType.VorbisComment));

        var vorbisComment = FlacCs.ReadMetadataBlockVorbisComment(ref reader);
        var comments = vorbisComment.UserComments.ToList();

        Assert.Equal("reference libFLAC 1.3.2 20170101", vorbisComment.Vendor);
        Assert.Equal(14, comments.Count);

        var title = comments[0];
        Assert.Equal("TITLE", title.Name);
        Assert.Equal("Flirt", title.Value);

        var artist = comments[1];
        Assert.Equal("ARTIST", artist.Name);
        Assert.Equal("NEFFEX", artist.Value);

        var album = comments[2];
        Assert.Equal("ALBUM", album.Name);
        Assert.Equal("Flirt", album.Value);

        var albumArtist = comments[3];
        Assert.Equal("ALBUMARTIST", albumArtist.Name);
        Assert.Equal("NEFFEX", albumArtist.Value);

        var trackNumber = comments[4];
        Assert.Equal("TRACKNUMBER", trackNumber.Name);
        Assert.Equal("1", trackNumber.Value);

        var discNumber = comments[5];
        Assert.Equal("DISCNUMBER", discNumber.Name);
        Assert.Equal("1", discNumber.Value);

        var genre1 = comments[6];
        Assert.Equal("GENRE", genre1.Name);
        Assert.Equal("Electro", genre1.Value);

        var genre2 = comments[7];
        Assert.Equal("GENRE", genre2.Name);
        Assert.Equal("Dance", genre2.Value);

        var genre3 = comments[8];
        Assert.Equal("GENRE", genre3.Name);
        Assert.Equal("Pop", genre3.Value);

        var date = comments[9];
        Assert.Equal("DATE", date.Name);
        Assert.Equal("2017-10-11", date.Value);

        var lengthComment = comments[10];
        Assert.Equal("LENGTH", lengthComment.Name);
        Assert.Equal("159000", lengthComment.Value);

        var publisher = comments[11];
        Assert.Equal("PUBLISHER", publisher.Name);
        Assert.Equal("Burning Boat", publisher.Value);

        var isrc = comments[12];
        Assert.Equal("ISRC", isrc.Name);
        Assert.Equal("TCADH1750644", isrc.Value);

        var barcode = comments[13];
        Assert.Equal("BARCODE", barcode.Name);
        Assert.Equal("859723487007", barcode.Value);
    }

    [Fact]
    public void ReadMetadataBlockPicture()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file[636..], FlacStreamState.AfterBlockHeader(false, 79_888, BlockType.Picture));

        var picture = FlacCs.ReadMetadataBlockPicture(ref reader);

        Assert.Equal(PictureType.FrontCover, picture.Type);
        Assert.Equal("image/jpeg", picture.MimeType);
        Assert.Equal(0, picture.Description.Length);
        Assert.Equal(800, picture.Width);
        Assert.Equal(800, picture.Height);
        Assert.Equal(24, picture.Depth);
        Assert.Equal(0, picture.Colors);
        Assert.Equal(79_846, picture.DataLength);
        Assert.Equal(79_846, picture.Data.Length);
    }

    [Fact]
    public void ReadStream_FlacFile()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        var res = FlacCs.ReadStream(ref reader);

        Assert.NotEmpty(res.Metadata);

        var metadata = res.Metadata.ToList();
        Assert.Equal(5, metadata.Count);

        Assert.IsType<MetadataBlockStreamInfoCs>(metadata[0].Data);
        Assert.IsType<MetadataBlockSeekTableCs>(metadata[1].Data);
        Assert.IsType<MetadataBlockVorbisCommentCs>(metadata[2].Data);
        Assert.IsType<MetadataBlockPictureCs>(metadata[3].Data);
        Assert.IsType<MetadataBlockPaddingCs>(metadata[4].Data);
    }
}
