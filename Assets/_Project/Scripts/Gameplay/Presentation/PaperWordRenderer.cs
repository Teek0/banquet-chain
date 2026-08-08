using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PaperWordRenderer : MonoBehaviour
{
    [SerializeField] private PaperAlphabetGlyphSet alphabet;
    [SerializeField] private TMP_Text fallbackLabel;
    [SerializeField, Min(1f)] private float maximumGlyphHeight = 64f;
    [SerializeField, Min(0f)] private float spacing = 4f;
    [SerializeField] private Color correctTint = new(0.42f, 0.9f, 0.58f);

    private readonly List<Image> glyphImages = new();

    public bool IsConfigured => alphabet != null && alphabet.IsConfigured;
    public PaperAlphabetGlyphSet Alphabet => alphabet;

    public void Configure(PaperAlphabetGlyphSet glyphSet, TMP_Text label)
    {
        alphabet = glyphSet;
        fallbackLabel = label;
    }

    private void Awake()
    {
        if (fallbackLabel == null)
        {
            fallbackLabel = GetComponent<TMP_Text>();
        }
    }

    public bool RenderWord(string word, int correctPrefixLength)
    {
        return RenderWord(word, correctPrefixLength, correctPrefixLength);
    }

    public bool RenderWord(string word, int correctPrefixLength, int typedLength)
    {
        string displayedWord = word ?? string.Empty;

        if (!IsConfigured || !CanRender(displayedWord))
        {
            SetFallbackEnabled(true);
            SetVisibleGlyphCount(0);
            return false;
        }

        SetFallbackEnabled(false);
        EnsureGlyphCount(displayedWord.Length);
        SetVisibleGlyphCount(displayedWord.Length);

        int safePrefix = Mathf.Clamp(
            correctPrefixLength,
            0,
            displayedWord.Length
        );

        for (int index = 0; index < displayedWord.Length; index++)
        {
            bool isTyped = index < typedLength;
            bool isError = isTyped && index >= safePrefix;
            alphabet.TryGetGlyph(
                displayedWord[index],
                isError,
                out Sprite glyph
            );
            Image image = glyphImages[index];
            image.sprite = glyph;
            image.color = index < safePrefix ? correctTint : Color.white;
        }

        LayoutGlyphs(displayedWord.Length);
        return true;
    }

    private bool CanRender(string word)
    {
        foreach (char character in word)
        {
            if (!alphabet.TryGetGlyph(character, false, out _))
            {
                return false;
            }
        }

        return word.Length > 0;
    }

    private void EnsureGlyphCount(int count)
    {
        while (glyphImages.Count < count)
        {
            GameObject glyphObject = new(
                $"PaperGlyph_{glyphImages.Count:00}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            glyphObject.layer = gameObject.layer;
            RectTransform glyphTransform = glyphObject
                .GetComponent<RectTransform>();
            glyphTransform.SetParent(transform, false);
            glyphTransform.anchorMin = new Vector2(0.5f, 0.5f);
            glyphTransform.anchorMax = new Vector2(0.5f, 0.5f);
            glyphTransform.pivot = new Vector2(0.5f, 0.5f);

            Image image = glyphObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            glyphImages.Add(image);
        }
    }

    private void LayoutGlyphs(int count)
    {
        RectTransform container = (RectTransform)transform;
        float availableWidth = Mathf.Max(1f, container.rect.width);
        float availableHeight = Mathf.Max(1f, container.rect.height);
        float sumAspectRatios = 0f;

        for (int index = 0; index < count; index++)
        {
            Sprite sprite = glyphImages[index].sprite;
            sumAspectRatios += sprite.rect.width / sprite.rect.height;
        }

        float totalSpacing = spacing * Mathf.Max(0, count - 1);
        float glyphHeight = Mathf.Min(maximumGlyphHeight, availableHeight);
        float naturalWidth = (sumAspectRatios * glyphHeight) + totalSpacing;

        if (naturalWidth > availableWidth && sumAspectRatios > 0f)
        {
            glyphHeight = Mathf.Max(
                1f,
                (availableWidth - totalSpacing) / sumAspectRatios
            );
        }

        float totalWidth = (sumAspectRatios * glyphHeight) + totalSpacing;
        float cursor = -totalWidth * 0.5f;

        for (int index = 0; index < count; index++)
        {
            RectTransform glyphTransform = glyphImages[index].rectTransform;
            Sprite sprite = glyphImages[index].sprite;
            float width = glyphHeight * sprite.rect.width / sprite.rect.height;
            glyphTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                width
            );
            glyphTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                glyphHeight
            );
            glyphTransform.anchoredPosition = new Vector2(
                cursor + (width * 0.5f),
                0f
            );
            cursor += width + spacing;
        }
    }

    private void SetVisibleGlyphCount(int count)
    {
        for (int index = 0; index < glyphImages.Count; index++)
        {
            glyphImages[index].gameObject.SetActive(index < count);
        }
    }

    private void SetFallbackEnabled(bool enabled)
    {
        if (fallbackLabel != null)
        {
            fallbackLabel.enabled = enabled;
        }
    }
}
