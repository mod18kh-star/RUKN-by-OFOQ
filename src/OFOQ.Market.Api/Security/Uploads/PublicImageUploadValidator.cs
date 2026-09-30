using System.Buffers.Binary;

namespace OFOQ.Market.Api.Security.Uploads;

public static class PublicImageUploadValidator
{
    private const uint MaxDimension = 8192;
    private const ulong MaxPixelCount = 32_000_000;

    public static bool TryValidate(
        string fileName,
        string? declaredContentType,
        ReadOnlySpan<byte> content,
        out string safeExtension,
        out string safeContentType)
    {
        safeExtension = string.Empty;
        safeContentType = string.Empty;

        var extension =
            Path.GetExtension(fileName)
                .Trim()
                .ToLowerInvariant();

        return extension switch
        {
            ".png" =>
                TryValidatePng(
                    declaredContentType,
                    content,
                    out safeExtension,
                    out safeContentType),

            ".jpg" or ".jpeg" =>
                TryValidateJpeg(
                    declaredContentType,
                    content,
                    out safeExtension,
                    out safeContentType),

            ".webp" =>
                TryValidateWebP(
                    declaredContentType,
                    content,
                    out safeExtension,
                    out safeContentType),

            _ =>
                false
        };
    }

    private static bool TryValidatePng(
        string? declaredContentType,
        ReadOnlySpan<byte> content,
        out string safeExtension,
        out string safeContentType)
    {
        safeExtension = string.Empty;
        safeContentType = string.Empty;

        if (!DeclaredTypeMatches(
                declaredContentType,
                "image/png"))
        {
            return false;
        }

        if (content.Length < 24 ||
            !content[..8].SequenceEqual(
                new byte[]
                {
                    137, 80, 78, 71, 13, 10, 26, 10
                }) ||
            !content.Slice(12, 4).SequenceEqual(
                "IHDR"u8))
        {
            return false;
        }

        var width =
            BinaryPrimitives.ReadUInt32BigEndian(
                content.Slice(16, 4));

        var height =
            BinaryPrimitives.ReadUInt32BigEndian(
                content.Slice(20, 4));

        if (!DimensionsAreSafe(
                width,
                height))
        {
            return false;
        }

        safeExtension = ".png";
        safeContentType = "image/png";

        return true;
    }

    private static bool TryValidateJpeg(
        string? declaredContentType,
        ReadOnlySpan<byte> content,
        out string safeExtension,
        out string safeContentType)
    {
        safeExtension = string.Empty;
        safeContentType = string.Empty;

        if (!DeclaredTypeMatches(
                declaredContentType,
                "image/jpeg"))
        {
            return false;
        }

        if (content.Length < 16 ||
            content[0] != 0xFF ||
            content[1] != 0xD8 ||
            content[2] != 0xFF ||
            content[^2] != 0xFF ||
            content[^1] != 0xD9 ||
            !TryReadJpegDimensions(
                content,
                out var width,
                out var height) ||
            !DimensionsAreSafe(
                width,
                height))
        {
            return false;
        }

        safeExtension = ".jpg";
        safeContentType = "image/jpeg";

        return true;
    }

    private static bool TryValidateWebP(
        string? declaredContentType,
        ReadOnlySpan<byte> content,
        out string safeExtension,
        out string safeContentType)
    {
        safeExtension = string.Empty;
        safeContentType = string.Empty;

        if (!DeclaredTypeMatches(
                declaredContentType,
                "image/webp"))
        {
            return false;
        }

        if (content.Length < 25 ||
            !content[..4].SequenceEqual(
                "RIFF"u8) ||
            !content.Slice(8, 4).SequenceEqual(
                "WEBP"u8) ||
            !TryReadWebPDimensions(
                content,
                out var width,
                out var height) ||
            !DimensionsAreSafe(
                width,
                height))
        {
            return false;
        }

        safeExtension = ".webp";
        safeContentType = "image/webp";

        return true;
    }

    private static bool TryReadJpegDimensions(
        ReadOnlySpan<byte> content,
        out uint width,
        out uint height)
    {
        width = 0;
        height = 0;

        var position = 2;

        while (position + 4 <= content.Length)
        {
            while (position < content.Length &&
                   content[position] != 0xFF)
            {
                position++;
            }

            while (position < content.Length &&
                   content[position] == 0xFF)
            {
                position++;
            }

            if (position >= content.Length)
            {
                return false;
            }

            var marker =
                content[position++];

            if (marker is 0xD8 or 0xD9)
            {
                continue;
            }

            if (marker == 0x01 ||
                marker is >= 0xD0 and <= 0xD7)
            {
                continue;
            }

            if (position + 2 > content.Length)
            {
                return false;
            }

            var segmentLength =
                BinaryPrimitives.ReadUInt16BigEndian(
                    content.Slice(
                        position,
                        2));

            if (segmentLength < 2 ||
                position + segmentLength >
                    content.Length)
            {
                return false;
            }

            if (IsJpegStartOfFrame(
                    marker))
            {
                if (segmentLength < 7)
                {
                    return false;
                }

                height =
                    BinaryPrimitives.ReadUInt16BigEndian(
                        content.Slice(
                            position + 3,
                            2));

                width =
                    BinaryPrimitives.ReadUInt16BigEndian(
                        content.Slice(
                            position + 5,
                            2));

                return width > 0 &&
                    height > 0;
            }

            if (marker == 0xDA)
            {
                return false;
            }

            position +=
                segmentLength;
        }

        return false;
    }

    private static bool TryReadWebPDimensions(
        ReadOnlySpan<byte> content,
        out uint width,
        out uint height)
    {
        width = 0;
        height = 0;

        var chunk =
            content.Slice(
                12,
                4);

        if (chunk.SequenceEqual(
                "VP8X"u8))
        {
            if (content.Length < 30)
            {
                return false;
            }

            width =
                1u +
                ReadUInt24LittleEndian(
                    content.Slice(
                        24,
                        3));

            height =
                1u +
                ReadUInt24LittleEndian(
                    content.Slice(
                        27,
                        3));

            return true;
        }

        if (chunk.SequenceEqual(
                "VP8 "u8))
        {
            if (content.Length < 30 ||
                content[23] != 0x9D ||
                content[24] != 0x01 ||
                content[25] != 0x2A)
            {
                return false;
            }

            width =
                (uint)(
                    BinaryPrimitives.ReadUInt16LittleEndian(
                        content.Slice(
                            26,
                            2)) &
                    0x3FFF);

            height =
                (uint)(
                    BinaryPrimitives.ReadUInt16LittleEndian(
                        content.Slice(
                            28,
                            2)) &
                    0x3FFF);

            return width > 0 &&
                height > 0;
        }

        if (chunk.SequenceEqual(
                "VP8L"u8))
        {
            if (content.Length < 25 ||
                content[20] != 0x2F)
            {
                return false;
            }

            var b1 = content[21];
            var b2 = content[22];
            var b3 = content[23];
            var b4 = content[24];

            width =
                1u +
                (uint)(
                    b1 |
                    ((b2 & 0x3F) << 8));

            height =
                1u +
                (uint)(
                    ((b2 & 0xC0) >> 6) |
                    (b3 << 2) |
                    ((b4 & 0x0F) << 10));

            return true;
        }

        return false;
    }

    private static bool IsJpegStartOfFrame(
        byte marker)
    {
        return marker is
            0xC0 or
            0xC1 or
            0xC2 or
            0xC3 or
            0xC5 or
            0xC6 or
            0xC7 or
            0xC9 or
            0xCA or
            0xCB or
            0xCD or
            0xCE or
            0xCF;
    }

    private static uint ReadUInt24LittleEndian(
        ReadOnlySpan<byte> bytes)
    {
        return
            (uint)bytes[0] |
            ((uint)bytes[1] << 8) |
            ((uint)bytes[2] << 16);
    }

    private static bool DimensionsAreSafe(
        uint width,
        uint height)
    {
        if (width is 0 or > MaxDimension ||
            height is 0 or > MaxDimension)
        {
            return false;
        }

        return
            (ulong)width *
            height <=
            MaxPixelCount;
    }

    private static bool DeclaredTypeMatches(
        string? declaredContentType,
        string expected)
    {
        if (string.IsNullOrWhiteSpace(
                declaredContentType))
        {
            return true;
        }

        return string.Equals(
            declaredContentType.Trim(),
            expected,
            StringComparison.OrdinalIgnoreCase);
    }
}