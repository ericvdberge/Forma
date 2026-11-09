namespace Forma.Application.Services.Media;

/// <summary>The bytes of a stored image, ready to send. Dispose it to close <see cref="Content"/>.</summary>
public sealed record MediaFile(Stream Content, string ContentType, long Length) : IDisposable
{
    public void Dispose() => Content.Dispose();
}
