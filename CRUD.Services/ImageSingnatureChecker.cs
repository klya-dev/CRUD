namespace CRUD.Services;

/// <inheritdoc cref="IImageSingnatureChecker"/>
public class ImageSingnatureChecker : IImageSingnatureChecker
{
    public (bool IsValid, string FileExtension) IsFileValid(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var buffer = new byte[_maxSignatureLength];

        // Поток не закрываем, т.к он ещё нужен
        // Поток поддерживает Seek
        if (stream.CanSeek)
            stream.Position = 0; // Сбрасываем позицию потока к началу для повторного чтения, иначе с каждым вызовом метода с тем же Stream сигнатура не будет совпадать, т.к позиция изменилась
        else
            return (false, null!);

        var bytesRead = stream.Read(buffer, 0, _maxSignatureLength);
        if (bytesRead == 0)
            return (false, null!);

        var headerSpan = new ReadOnlySpan<byte>(buffer, 0, bytesRead);

        foreach (var kvp in _fileSignatures)
        {
            var extension = kvp.Key;
            var sigs = kvp.Value;
            foreach (var sig in sigs)
                if (headerSpan.Length >= sig.Length && headerSpan.Slice(0, sig.Length).SequenceEqual(sig))
                    return (true, extension);
        }

        return (false, null!);
    }

    private static readonly IReadOnlyDictionary<string, byte[][]> _fileSignatures = new Dictionary<string, byte[][]>()
    {
        // https://en.wikipedia.org/wiki/List_of_file_signatures
        { "png", new byte[][] { [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A] } },
        { "jpeg", new byte[][]
            {
                [0xFF, 0xD8, 0xFF, 0xE0],
                [0xFF, 0xD8, 0xFF, 0xE2],
                [0xFF, 0xD8, 0xFF, 0xE3],
                [0xFF, 0xD8, 0xFF, 0xEE],
                [0xFF, 0xD8, 0xFF, 0xDB],
            }
        },
        { "jpeg2000", new byte[][] { [0x00, 0x00, 0x00, 0x0C, 0x6A, 0x50, 0x20, 0x20, 0x0D, 0x0A, 0x87, 0x0A] } },
        { "jpg", new byte[][]
            {
                [0xFF, 0xD8, 0xFF, 0xE0],
                [0xFF, 0xD8, 0xFF, 0xE1],
                [0xFF, 0xD8, 0xFF, 0xE8],
                [0xFF, 0xD8, 0xFF, 0xEE],
                [0xFF, 0xD8, 0xFF, 0xDB],
            }
        },
    };

    private static readonly byte[][] _allSignatures = _fileSignatures.Values.SelectMany(x => x).ToArray();
    private static readonly int _maxSignatureLength = _allSignatures.Max(sig => sig.Length);
}