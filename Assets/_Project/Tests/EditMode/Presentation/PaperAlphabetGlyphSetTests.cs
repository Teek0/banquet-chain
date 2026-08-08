using NUnit.Framework;

public sealed class PaperAlphabetGlyphSetTests
{
    [Test]
    public void GlyphIndex_SupportsSpanishAlphabetAndAccentedVowels()
    {
        Assert.That(PaperAlphabetGlyphSet.GetGlyphIndex('A'), Is.EqualTo(0));
        Assert.That(PaperAlphabetGlyphSet.GetGlyphIndex('ñ'), Is.EqualTo(14));
        Assert.That(PaperAlphabetGlyphSet.GetGlyphIndex('ó'), Is.EqualTo(15));
        Assert.That(PaperAlphabetGlyphSet.GetGlyphIndex('ü'), Is.EqualTo(21));
        Assert.That(PaperAlphabetGlyphSet.GetGlyphIndex('Z'), Is.EqualTo(26));
        Assert.That(PaperAlphabetGlyphSet.GetGlyphIndex('-'), Is.EqualTo(-1));
    }
}
