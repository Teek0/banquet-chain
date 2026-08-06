using System.Globalization;
using System.Text;

public static class TypingTextNormalizer
{
    public static string NormalizeForComparison(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string decomposed = text
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);
        StringBuilder normalized = new(decomposed.Length);

        foreach (char character in decomposed)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(
                character
            );

            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetter(character))
            {
                normalized.Append(character);
            }
        }

        return normalized
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }

    public static bool AreEquivalent(string left, string right)
    {
        return NormalizeForComparison(left)
            == NormalizeForComparison(right);
    }
}
