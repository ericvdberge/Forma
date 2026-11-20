namespace Forma.Application.Documents;

/// <summary>
/// Checks and cleans the rich text of heading and text blocks against the studio's Tiptap allowlist:
/// <c>p, br, strong, em, s, a[href], ul, ol, li, span[data-muted]</c>, with <c>href</c> limited to
/// safe schemes. Implemented with Ganss.Xss's <c>HtmlSanitizer</c>.
/// </summary>
public interface IRichTextSanitizer
{
    /// <summary>
    /// True when sanitising would leave <paramref name="html"/> unchanged. Saving rejects anything else,
    /// so what is stored is exactly what the studio sent.
    /// </summary>
    bool IsClean(string html);

    /// <summary>Returns <paramref name="html"/> with everything outside the allowlist removed. Used when rendering.</summary>
    string Sanitize(string html);
}
