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
    [SerializeField] private Color pendingTint = new(0.65f, 0.65f, 0.65f);

    [Header("Typing caret")]
    [SerializeField] private RectTransform caretTransform;
    [SerializeField] private Graphic caretGraphic;
    [SerializeField, Min(0.1f)] private float caretBlinkInterval = 0.45f;
    [SerializeField, Min(1f)] private float caretWidth = 3f;
    [SerializeField, Range(0.1f, 1f)] private float caretHeightRatio = 0.82f;

    private readonly List<Image> glyphImages = new();
    private bool caretRequested;
    private float nextCaretBlink;
    private bool caretBlinkVisible;

    public bool IsConfigured => alphabet != null && alphabet.IsConfigured;
    public PaperAlphabetGlyphSet Alphabet => alphabet;

    public void Configure(PaperAlphabetGlyphSet glyphSet, TMP_Text label)
    {
        alphabet = glyphSet;
        fallbackLabel = label;
    }

    public void ConfigureCaret(RectTransform caret, Graphic graphic)
    {
        caretTransform = caret;
        caretGraphic = graphic;
        caretRequested = false;
        caretBlinkVisible = false;
        SetCaretVisible(false);
    }

    private void Awake()
    {
        if (fallbackLabel == null)
        {
            fallbackLabel = GetComponent<TMP_Text>();
        }

        SetCaretVisible(false);
    }

    private void Update()
    {
        if (!caretRequested || caretGraphic == null)
        {
            SetCaretVisible(false);
            return;
        }

        if (Time.unscaledTime < nextCaretBlink)
        {
            return;
        }

        caretBlinkVisible = !caretBlinkVisible;
        SetCaretVisible(caretBlinkVisible);
        nextCaretBlink = Time.unscaledTime + caretBlinkInterval;
    }

    public void SetCaretActive(bool active)
    {
        if (caretRequested == active)
        {
            return;
        }

        caretRequested = active;
        caretBlinkVisible = active;
        nextCaretBlink = Time.unscaledTime + caretBlinkInterval;
        SetCaretVisible(active);
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
            SetCaretActive(false);
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
            image.color = index < safePrefix
                ? correctTint
                : isError ? Color.white : pendingTint;
        }

        LayoutGlyphs(displayedWord.Length, typedLength);
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

        return true;
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

    private void LayoutGlyphs(int count, int typedLength)
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

        PositionCaret(count, typedLength, glyphHeight);
    }

    private void PositionCaret(int count, int typedLength, float glyphHeight)
    {
        if (caretTransform == null)
        {
            return;
        }

        int safeTypedLength = Mathf.Clamp(typedLength, 0, count);
        float x = 0f;

        if (count > 0 && safeTypedLength == 0)
        {
            RectTransform first = glyphImages[0].rectTransform;
            x = first.anchoredPosition.x - first.rect.width * 0.5f - spacing * 0.5f;
        }
        else if (count > 0)
        {
            RectTransform previous = glyphImages[safeTypedLength - 1].rectTransform;
            x = previous.anchoredPosition.x
                + previous.rect.width * 0.5f
                + spacing * 0.5f;
        }

        float height = Mathf.Max(4f, glyphHeight * caretHeightRatio);
        caretTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            caretWidth
        );
        caretTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            height
        );
        caretTransform.anchoredPosition = new Vector2(x, 0f);
        caretTransform.SetAsLastSibling();
    }

    private void SetCaretVisible(bool visible)
    {
        if (caretGraphic != null)
        {
            caretGraphic.enabled = visible;
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
