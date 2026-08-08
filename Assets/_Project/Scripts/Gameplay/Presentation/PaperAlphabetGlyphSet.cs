using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PaperAlphabetGlyphSet",
    menuName = "Banquet Chain/UI/Paper Alphabet Glyph Set"
)]
public sealed class PaperAlphabetGlyphSet : ScriptableObject
{
    public const string SupportedCharacters = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ";

    [SerializeField] private Sprite[] grayGlyphs = Array.Empty<Sprite>();
    [SerializeField] private Sprite[] redGlyphs = Array.Empty<Sprite>();

    public int GlyphCount => SupportedCharacters.Length;
    public bool IsConfigured => HasCompleteSet(grayGlyphs)
        && HasCompleteSet(redGlyphs);

    public bool TryGetGlyph(
        char character,
        bool useRedVariant,
        out Sprite glyph
    )
    {
        int index = GetGlyphIndex(character);
        Sprite[] source = useRedVariant ? redGlyphs : grayGlyphs;

        if (index < 0 || source == null || index >= source.Length)
        {
            glyph = null;
            return false;
        }

        glyph = source[index];
        return glyph != null;
    }

    public static int GetGlyphIndex(char character)
    {
        char normalized = char.ToUpperInvariant(character) switch
        {
            'Á' or 'À' or 'Ä' or 'Â' => 'A',
            'É' or 'È' or 'Ë' or 'Ê' => 'E',
            'Í' or 'Ì' or 'Ï' or 'Î' => 'I',
            'Ó' or 'Ò' or 'Ö' or 'Ô' => 'O',
            'Ú' or 'Ù' or 'Ü' or 'Û' => 'U',
            char value => value
        };

        return SupportedCharacters.IndexOf(normalized);
    }

    private static bool HasCompleteSet(Sprite[] glyphs)
    {
        if (glyphs == null || glyphs.Length != SupportedCharacters.Length)
        {
            return false;
        }

        foreach (Sprite glyph in glyphs)
        {
            if (glyph == null)
            {
                return false;
            }
        }

        return true;
    }
}
