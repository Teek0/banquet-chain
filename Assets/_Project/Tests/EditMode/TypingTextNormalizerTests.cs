using NUnit.Framework;

public sealed class TypingTextNormalizerTests
{
    [TestCase("Azúcar", "azucar")]
    [TestCase("freír", "freir")]
    [TestCase("LIMÓN", "limon")]
    [TestCase("mezclar", "mezclar")]
    [TestCase("PIÑÓN", "pinon")]
    [TestCase("vergüenza", "verguenza")]
    [TestCase("¡Sartén!", "sarten")]
    [TestCase("a z-u.c/a:r", "azucar")]
    public void NormalizeForComparison_ReturnsComparableLetters(
        string source,
        string expected
    )
    {
        Assert.That(
            TypingTextNormalizer.NormalizeForComparison(source),
            Is.EqualTo(expected)
        );
    }

    [Test]
    public void AreEquivalent_DoesNotModifyDisplayedOriginal()
    {
        const string displayedWord = "Azúcar";

        Assert.That(
            TypingTextNormalizer.AreEquivalent(displayedWord, "AZUCAR"),
            Is.True
        );
        Assert.That(displayedWord, Is.EqualTo("Azúcar"));
    }
}
