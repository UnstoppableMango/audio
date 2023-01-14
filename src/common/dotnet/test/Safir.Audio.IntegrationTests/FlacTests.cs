namespace Safir.Audio.IntegrationTests;

[Trait("Category", "Unit")]
public class FlacTests
{
    private const string FileName = "NEFFEX-Flirt";

    [Fact]
    public void MagicNumber_FlacFile()
    {
        var file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ParseMagic(file);

        Assert.Equal("fLaC", res);
    }

    [Fact]
    public void MagicNumber_Mp3File()
    {
        var file = File.ReadAllBytes($"{FileName}.mp3");

        var res = FlacCs.ParseMagic(file);

        Assert.Null(res);
    }

    [Fact]
    public void ParseMetadataBlockHeader_StreamInfo()
    {
        var file = File.ReadAllBytes($"{FileName}.flac");
        var span = new Span<byte>(file)[4..];

        var res = FlacCs.ParseMetadataBlockHeader(span);

        Assert.Equal(34, res.Length);
        Assert.False(res.LastBlock);
        Assert.Equal(BlockType.StreamInfo, res.BlockType);
    }

    [Fact]
    public void ParseMetadataBlockHeader_Padding()
    {
        var file = File.ReadAllBytes($"{FileName}.flac");
        var span = new Span<byte>(file)[80_524..];

        var res = FlacCs.ParseMetadataBlockHeader(span);

        Assert.Equal(16384, res.Length);
        Assert.True(res.LastBlock);
        Assert.Equal(BlockType.Padding, res.BlockType);
    }

    [Fact]
    public void ParseMetadataBlockHeader_VorbisComment()
    {
        var file = File.ReadAllBytes($"{FileName}.flac");
        var span = new Span<byte>(file)[334..];

        var res = FlacCs.ParseMetadataBlockHeader(span);

        Assert.Equal(294, res.Length);
        Assert.False(res.LastBlock);
        Assert.Equal(BlockType.VorbisComment, res.BlockType);
    }

    [Fact]
    public void ParseMetadataBlockHeader_Picture()
    {
        var file = File.ReadAllBytes($"{FileName}.flac");
        var span = new Span<byte>(file)[632..];

        var res = FlacCs.ParseMetadataBlockHeader(span);

        Assert.Equal(79_888, res.Length);
        Assert.False(res.LastBlock);
        Assert.Equal(BlockType.Picture, res.BlockType);
    }

    [Fact]
    public void ParseMetadataBlockStreamInfo()
    {
        var file = File.ReadAllBytes($"{FileName}.flac");
        var span = new Span<byte>(file)[8..];

        var streamInfo = FlacCs.ParseMetadataBlockStreamInfo(span);

        Assert.Equal(4096u, streamInfo.MinBlockSize);
        Assert.Equal(4096u, streamInfo.MaxBlockSize);
        Assert.Equal(1781u, streamInfo.MinFrameSize);
        Assert.Equal(14163u, streamInfo.MaxFrameSize);
        Assert.Equal(44100u, streamInfo.SampleRate);
        Assert.Equal(2u, streamInfo.Channels);
        Assert.Equal(16u, streamInfo.BitsPerSample);
        Assert.Equal(7028438u, streamInfo.TotalSamples);
        Assert.Equal("3c16b5b7186537d6823c7be62fe8c661", streamInfo.Md5Signature);
    }

    [Fact]
    public void ParseMetadataBlockPadding()
    {
        var file = File.ReadAllBytes($"{FileName}.flac");
        var span = new Span<byte>(file)[80_528..];

        var res = FlacCs.ParseMetadataBlockPadding(span, 16384);

        Assert.NotNull(res);
        Assert.Equal(16384, res.Item);
    }

    [Fact(Skip = "No test files with application meta")]
    public void ParseMetadataBlockHeader_Application()
    {
        var file = File.ReadAllBytes($"{FileName}.flac");
        var span = new Span<byte>(file)[69_420..];

        var application = FlacCs.ParseMetadataBlockApplication(span, 69);

        Assert.Equal(69, application.ApplicationId);
        Assert.Equal(420, application.ApplicationData.Length);
    }

    [Fact]
    public void ParseMetadataBlockSeekTable()
    {
        var file = File.ReadAllBytes($"{FileName}.flac");
        var span = new Span<byte>(file)[46..];

        var res = FlacCs.ParseMetadataBlockSeekTable(span, 288);

        var list = res.Item; // TODO: Do we like this API?
        Assert.Equal(16, list.Length);

        var first = list[0];
        Assert.Equal(0UL, first.SampleNumber);
        Assert.Equal(0UL, first.StreamOffset);
        Assert.Equal(4096, first.FrameSamples);

        var second = list[1];
        Assert.Equal(438_272UL, second.SampleNumber);
        Assert.Equal(532_963UL, second.StreamOffset);
        Assert.Equal(4096, second.FrameSamples);

        var third = list[2];
        Assert.Equal(880_640UL, third.SampleNumber);
        Assert.Equal(1_732_918UL, third.StreamOffset);
        Assert.Equal(4096, third.FrameSamples);

        var last = list.Last();
        Assert.Equal(6_610_944UL, last.SampleNumber);
        Assert.Equal(1_8894_059UL, last.StreamOffset);
        Assert.Equal(4096, last.FrameSamples);
    }

    [Fact]
    public void ParseMetadataBlockVorbisComment()
    {
        const int length = 294;
        var file = File.ReadAllBytes($"{FileName}.flac");
        var span = new Span<byte>(file)[338..];

        var vorbisComment = FlacCs.ParseMetadataBlockVorbisComment(span, length);

        Assert.NotNull(vorbisComment);
        Assert.Equal(14, vorbisComment.UserComments.Length);

        var comments = vorbisComment.UserComments;
        var title = Assert.IsType<TitleComment>(comments[0]);
        Assert.Equal("Flirt", title.Value);

        var artist = Assert.IsType<ArtistComment>(comments[1]);
        Assert.Equal("NEFFEX", artist.Value);

        var album = Assert.IsType<AlbumComment>(comments[2]);
        Assert.Equal("Flirt", album.Value);

        var albumArtist = Assert.IsType<OtherComment>(comments[3]);
        Assert.Equal("ALBUMARTIST", albumArtist.Name);
        Assert.Equal("NEFFEX", albumArtist.Value);

        var trackNumber = Assert.IsType<TrackNumberComment>(comments[4]);
        Assert.Equal("1", trackNumber.Value);

        var discNumber = Assert.IsType<OtherComment>(comments[5]);
        Assert.Equal("DISCNUMBER", discNumber.Name);
        Assert.Equal("1", discNumber.Value);

        var genre1 = Assert.IsType<GenreComment>(comments[6]);
        Assert.Equal("Electro", genre1.Value);

        var genre2 = Assert.IsType<GenreComment>(comments[7]);
        Assert.Equal("Dance", genre2.Value);

        var genre3 = Assert.IsType<GenreComment>(comments[8]);
        Assert.Equal("Pop", genre3.Value);

        var date = Assert.IsType<DateComment>(comments[9]);
        Assert.Equal("2017-10-11", date.Value);

        var lengthComment = Assert.IsType<OtherComment>(comments[10]);
        Assert.Equal("LENGTH", lengthComment.Name);
        Assert.Equal("159000", lengthComment.Value);

        var publisher = Assert.IsType<OtherComment>(comments[11]);
        Assert.Equal("PUBLISHER", publisher.Name);
        Assert.Equal("Burning Boat", publisher.Value);

        var isrc = Assert.IsType<IsrcComment>(comments[12]);
        Assert.Equal("TCADH1750644", isrc.Value);

        var barcode = Assert.IsType<OtherComment>(comments[13]);
        Assert.Equal("BARCODE", barcode.Name);
        Assert.Equal("859723487007", barcode.Value);
    }
}
