using System.Text;
using Safir.Audio.Flac;

namespace Safir.Audio.IntegrationTests;

[Trait("Category", "Integration")]
public class FlacStreamReaderTests
{
    private const string FileName = "NEFFEX-Flirt";

    [Fact]
    public void Read_Start()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        Assert.Equal(StreamPosition.Start, reader.Position);
        Assert.Equal(0, reader.Value.Length);
    }

    [Fact]
    public void Read_Marker()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        reader.Read();

        Assert.Equal(StreamPosition.Marker, reader.Position);
        Assert.Equal("fLaC", Encoding.ASCII.GetString(reader.Value));
    }

    [Fact]
    public void Read_StreamInfo_LastMetadataBlockFlag()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 2)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.LastMetadataBlockFlag, reader.Position);
        Assert.False(reader.GetLastMetadataBlockFlag());
    }

    [Fact]
    public void Read_StreamInfo_BlockType()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 3)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.MetadataBlockType, reader.Position);
        Assert.Equal(BlockType.StreamInfo, reader.GetBlockType());
    }

    [Fact]
    public void Read_StreamInfo_DataBlockLength()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 4)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.DataBlockLength, reader.Position);
        Assert.Equal(34u, reader.GetDataBlockLength());
    }

    [Fact]
    public void Read_StreamInfo_MinimumBlockSize()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 5)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.MinimumBlockSize, reader.Position);
        Assert.Equal(4096, reader.GetMinimumBlockSize());
    }

    [Fact]
    public void Read_StreamInfo_MaximumBlockSize()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 6)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.MaximumBlockSize, reader.Position);
        Assert.Equal(4096, reader.GetMaximumBlockSize());
    }

    [Fact]
    public void Read_StreamInfo_MinimumFrameSize()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 7)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.MinimumFrameSize, reader.Position);
        Assert.Equal(1781u, reader.GetMinimumFrameSize());
    }

    [Fact]
    public void Read_StreamInfo_MaximumFrameSize()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 8)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.MaximumFrameSize, reader.Position);
        Assert.Equal(14163u, reader.GetMaximumFrameSize());
    }

    [Fact]
    public void Read_StreamInfo_SampleRate()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 9)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.StreamInfoSampleRate, reader.Position);
        Assert.Equal(44100u, reader.GetSampleRate());
    }

    [Fact]
    public void Read_StreamInfo_Channels()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 10)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.NumberOfChannels, reader.Position);
        Assert.Equal(2, reader.GetChannels());
    }

    [Fact]
    public void Read_StreamInfo_BitsPerSample()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 11)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.BitsPerSample, reader.Position);
        Assert.Equal(16, reader.GetBitsPerSample());
    }

    [Fact]
    public void Read_StreamInfo_TotalSamples()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 12)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.TotalSamples, reader.Position);
        Assert.Equal(7028438u, reader.GetTotalSamples());
    }

    [Fact]
    public void Read_StreamInfo_Md5Signature()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 13)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.Md5Signature, reader.Position);
        Assert.Equal("3c16b5b7186537d6823c7be62fe8c661", reader.GetMd5Signature(), ignoreCase: true);
    }

    [Fact]
    public void Read_SeekTable_LastMetadataBlockFlag()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 14)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.LastMetadataBlockFlag, reader.Position);
        Assert.False(reader.GetLastMetadataBlockFlag());
    }

    [Fact]
    public void Read_SeekTable_MetadataBlockType()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 15)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.MetadataBlockType, reader.Position);
        Assert.Equal(BlockType.SeekTable, reader.GetBlockType());
    }

    [Fact]
    public void Read_SeekTable_DataBlockLength()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 16)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.DataBlockLength, reader.Position);
        Assert.Equal(288u, reader.GetDataBlockLength());
    }

    [Fact]
    public void Read_SeekTable_SeekPoints()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 17)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.SeekPointSampleNumber, reader.Position);
        Assert.Equal(0u, reader.GetSeekPointSampleNumber());

        reader.Read();

        Assert.Equal(StreamPosition.SeekPointOffset, reader.Position);
        Assert.Equal(0u, reader.GetSeekPointOffset());

        reader.Read();

        Assert.Equal(StreamPosition.NumberOfSamples, reader.Position);
        Assert.Equal(4096u, reader.GetSeekPointNumberOfSamples());

        // 14 seek points until the end, 3 blocks each, +1 to advance into the final seek point
        foreach (var _ in Enumerable.Range(0, 14 * 3 + 1)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.SeekPointSampleNumber, reader.Position);
        Assert.Equal(6610944u, reader.GetSeekPointSampleNumber());

        reader.Read();

        Assert.Equal(StreamPosition.SeekPointOffset, reader.Position);
        Assert.Equal(18894059u, reader.GetSeekPointOffset());

        reader.Read();

        Assert.Equal(StreamPosition.NumberOfSamples, reader.Position);
        Assert.Equal(4096u, reader.GetSeekPointNumberOfSamples());
    }

    [Fact]
    public void Read_VorbisComment_LastMetadataBlockFlag()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 65)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.LastMetadataBlockFlag, reader.Position);
        Assert.False(reader.GetLastMetadataBlockFlag());
    }

    [Fact]
    public void Read_VorbisComment_MetadataBlockType()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 66)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.MetadataBlockType, reader.Position);
        Assert.Equal(BlockType.VorbisComment, reader.GetBlockType());
    }

    [Fact]
    public void Read_VorbisComment_DataBlockLength()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 67)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.DataBlockLength, reader.Position);
        Assert.Equal(294u, reader.GetDataBlockLength());
    }

    [Fact]
    public void Read_VorbisComment_VendorLength()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 68)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.VendorLength, reader.Position);
        Assert.Equal(32u, reader.GetVendorLength());
    }

    [Fact]
    public void Read_VorbisComment_VendorString()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 69)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.VendorString, reader.Position);
        Assert.Equal("reference libFLAC 1.3.2 20170101", reader.GetVendorString());
    }

    [Fact]
    public void Read_VorbisComment_UserCommentListLength()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 70)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.UserCommentListLength, reader.Position);
        Assert.Equal(14u, reader.GetUserCommentListLength());
    }

    [Fact]
    public void Read_VorbisComment_UserComments()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 71)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.UserCommentLength, reader.Position);
        Assert.Equal(11u, reader.GetUserCommentLength());

        reader.Read();

        Assert.Equal(StreamPosition.UserComment, reader.Position);
        Assert.Equal("TITLE=Flirt", reader.GetUserComment());

        // 12 comments until the end, 2 blocks each, +1 to advance into the final comment
        foreach (var _ in Enumerable.Range(0, 12 * 2 + 1)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.UserCommentLength, reader.Position);
        Assert.Equal(20u, reader.GetUserCommentLength());

        reader.Read();

        Assert.Equal(StreamPosition.UserComment, reader.Position);
        Assert.Equal("BARCODE=859723487007", reader.GetUserComment());
    }

    [Fact]
    public void Read_Picture_LastMetadataBlockFlag()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 99)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.LastMetadataBlockFlag, reader.Position);
        Assert.False(reader.GetLastMetadataBlockFlag());
    }

    [Fact]
    public void Read_Picture_MetadataBlockType()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 100)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.MetadataBlockType, reader.Position);
        Assert.Equal(BlockType.Picture, reader.GetBlockType());
    }

    [Fact]
    public void Read_Picture_DataBlockLength()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 101)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.DataBlockLength, reader.Position);
        Assert.Equal(79_888u, reader.GetDataBlockLength());
    }

    [Fact]
    public void Read_Picture_Type()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 102)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.PictureType, reader.Position);
        Assert.Equal(PictureType.FrontCover, reader.GetPictureType());
    }

    [Fact]
    public void Read_Picture_MimeTypeLength()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 103)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.MimeTypeLength, reader.Position);
        Assert.Equal(10u, reader.GetMimeTypeLength());
    }

    [Fact]
    public void Read_Picture_MimeType()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 104)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.MimeType, reader.Position);
        Assert.Equal("image/jpeg", reader.GetMimeType());
    }

    [Fact]
    public void Read_Picture_DescriptionLength()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 105)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.PictureDescriptionLength, reader.Position);
        Assert.Equal(0u, reader.GetPictureDescriptionLength());
    }

    [Fact]
    public void Read_Picture_Description()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 106)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.PictureDescription, reader.Position);
        Assert.Equal(string.Empty, reader.GetPictureDescription());
    }

    [Fact]
    public void Read_Picture_Width()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 107)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.PictureWidth, reader.Position);
        Assert.Equal(800u, reader.GetPictureWidth());
    }

    [Fact]
    public void Read_Picture_Height()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 108)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.PictureHeight, reader.Position);
        Assert.Equal(800u, reader.GetPictureHeight());
    }

    [Fact]
    public void Read_Picture_ColorDepth()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 109)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.PictureColorDepth, reader.Position);
        Assert.Equal(24u, reader.GetPictureColorDepth());
    }

    [Fact]
    public void Read_Picture_NumberOfColors()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 110)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.PictureNumberOfColors, reader.Position);
        Assert.Equal(0u, reader.GetPictureNumberOfColors());
    }

    [Fact]
    public void Read_Picture_DataLength()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 111)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.PictureDataLength, reader.Position);
        Assert.Equal(79_846u, reader.GetPictureDataLength());
    }

    [Fact]
    public void Read_Picture_Data()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 112)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.PictureData, reader.Position);
        Assert.Equal(79_846, reader.GetPictureData().Length);
    }

    [Fact]
    public void Read_Padding_LastMetadataBlockFlag()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 113)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.LastMetadataBlockFlag, reader.Position);
        Assert.True(reader.GetLastMetadataBlockFlag());
    }

    [Fact]
    public void Read_Padding_MetadataBlockType()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 114)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.MetadataBlockType, reader.Position);
        Assert.Equal(BlockType.Padding, reader.GetBlockType());
    }

    [Fact]
    public void Read_Padding_DataBlockLength()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 115)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.DataBlockLength, reader.Position);
        Assert.Equal(16_384u, reader.GetDataBlockLength());
    }

    [Fact]
    public void Read_Padding()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 116)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.Padding, reader.Position);
        Assert.Equal(16_384, reader.Value.Length);
    }

    [Fact]
    public void Read_End()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");
        var reader = new FlacStreamReader(file);

        foreach (var _ in Enumerable.Range(0, 117)) {
            reader.Read();
        }

        Assert.Equal(StreamPosition.End, reader.Position);
        Assert.Equal(0, reader.Value.Length);
    }
}
