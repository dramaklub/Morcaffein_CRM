using PdfSharp.Fonts;

namespace CRM_Morcaffein.winforms.Services.Reports
{
    /// <summary>
    /// PDFsharp 6.x font resolver that pulls TTFs from the Windows fonts folder.
    /// Maps the two fonts the report uses (Segoe UI for body, Courier New as a
    /// fallback for MigraDoc internals) plus a small set of common Windows fonts,
    /// so any font the document requests resolves without a custom bundled TTF.
    ///
    /// PDFsharp 6 removed the "just use system fonts" behavior that existed in 4.x.
    /// Without a resolver registered via GlobalFontSettings.FontResolver, even
    /// MigraDoc's own error-path rendering throws "font cannot be resolved."
    /// </summary>
    public class WindowsFontResolver : IFontResolver
    {
        // Map (family name, bold) -> the TTF filename in C:\Windows\Fonts\.
        // Segoe UI ships as four separate files: regular, bold, italic, bold-italic.
        private static readonly Dictionary<string, string> FontFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Segoe UI|false"] = "segoeui.ttf",
            ["Segoe UI|true"] = "segoeuib.ttf",
            ["Courier New|false"] = "cour.ttf",
            ["Courier New|true"] = "courbd.ttf",
            ["Arial|false"] = "arial.ttf",
            ["Arial|true"] = "arialbd.ttf",
            ["Times New Roman|false"] = "times.ttf",
            ["Times New Roman|true"] = "timesbd.ttf",
        };

        // FaceName is what PDFsharp asks us for — we get back the family + bold
        // flag from ResolveTypeface and hand it back on the second call.
        private static readonly Dictionary<string, string> FaceToFamily = new(StringComparer.OrdinalIgnoreCase);

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // Fold bold-italic into bold for simplicity — report never uses italics.
            var key = $"{familyName}|{isBold}";

            if (!FontFiles.ContainsKey(key))
            {
                // Fallback: if the requested family isn't in our map, try Segoe UI.
                key = $"Segoe UI|{isBold}";
            }

            var faceName = key;   // use the composite key as the "face name"
            FaceToFamily[faceName] = key;

            return new FontResolverInfo(faceName);
        }

        public byte[]? GetFont(string faceName)
        {
            if (!FaceToFamily.TryGetValue(faceName, out var key)) return null;
            if (!FontFiles.TryGetValue(key, out var filename)) return null;

            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                filename);

            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }
}