using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Recipe_New",
    menuName = "Banquet Chain/Recipe",
    order = 0
)]
public sealed class RecipeData : ScriptableObject
{
    [SerializeField] private string displayName = string.Empty;
    [SerializeField] private string displayNameEnglish = string.Empty;
    [SerializeField] private Sprite icon;
    [SerializeField, TextArea(2, 4)] private string catOrder = string.Empty;
    [SerializeField, TextArea(2, 4)] private string catOrderEnglish = string.Empty;
    [SerializeField] private List<RecipeStep> steps = new();

    public string DisplayName => GameLocalization.Text(displayName, displayNameEnglish);
    public Sprite Icon => icon;
    public string CatOrder => GameLocalization.Text(catOrder, catOrderEnglish);
    public IReadOnlyList<RecipeStep> Steps => steps;

    public List<string> GetValidationMessages()
    {
        List<string> messages = new();

        if (steps == null || steps.Count == 0)
        {
            messages.Add("La receta no contiene pasos.");
            return messages;
        }

        for (int index = 0; index < steps.Count; index++)
        {
            RecipeStep step = steps[index];
            string location = $"Paso {index + 1}";

            if (step == null)
            {
                messages.Add($"{location}: el paso no está configurado.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(step.ExpectedWord))
            {
                messages.Add($"{location}: la palabra esperada está vacía.");
            }

            if (string.IsNullOrWhiteSpace(step.ActorId))
            {
                messages.Add($"{location}: falta el identificador del actor.");
            }

            if (step.DurationBeforeNextStep < 0f)
            {
                messages.Add($"{location}: la duración no puede ser negativa.");
            }
        }

        RecipeStep lastStep = steps[^1];

        if (lastStep == null
            || lastStep.ReactionType != KitchenReactionType.Serving)
        {
            messages.Add("El último paso debería ser de servicio.");
        }

        return messages;
    }

    private void OnValidate()
    {
        foreach (string message in GetValidationMessages())
        {
            Debug.LogWarning($"RecipeData '{name}': {message}", this);
        }
    }
}

public enum GameLanguage
{
    Spanish,
    English
}

// Kept in the Recipes assembly so recipe data stays independent from UI.
public static class GameLocalization
{
    public static GameLanguage CurrentLanguage { get; set; } = GameLanguage.Spanish;

    public static bool IsEnglish => CurrentLanguage == GameLanguage.English;

    public static string Text(string spanish, string english)
        => IsEnglish && !string.IsNullOrWhiteSpace(english) ? english : spanish;
}
