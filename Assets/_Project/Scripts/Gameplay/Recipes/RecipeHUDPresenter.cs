using System.Collections.Generic;

public static class RecipeHUDPresenter
{
    private const string CompletedColor = "#6FE09D";
    private const string ActiveColor = "#FFD147";
    private const string PendingColor = "#9BA3B8";

    public static string BuildStepList(
        RecipeData recipe,
        int activeStepIndex,
        int completedThroughIndex
    )
    {
        if (recipe?.Steps == null || recipe.Steps.Count == 0)
        {
            return GameLocalization.Text("Sin pasos configurados", "No steps configured");
        }

        List<string> entries = new(recipe.Steps.Count);

        for (int index = 0; index < recipe.Steps.Count; index++)
        {
            RecipeStep step = recipe.Steps[index];
            string word = EscapeRichText(step?.ExpectedWord ?? "—");

            if (index <= completedThroughIndex)
            {
                entries.Add(
                    $"<color={CompletedColor}><b>{word}</b></color>"
                );
            }
            else if (index == activeStepIndex)
            {
                entries.Add(
                    $"<color={ActiveColor}><b>> {word}</b></color>"
                );
            }
            else
            {
                entries.Add($"<color={PendingColor}>○ {word}</color>");
            }
        }

        return string.Join("   ", entries);
    }

    public static string BuildProgress(
        RecipeData recipe,
        int activeStepIndex,
        int completedThroughIndex,
        bool recipeCompleted
    )
    {
        int total = recipe?.Steps?.Count ?? 0;

        if (total == 0)
        {
            return GameLocalization.Text("PROGRESO · 0 / 0", "PROGRESS · 0 / 0");
        }

        if (recipeCompleted)
        {
            return GameLocalization.Text($"RECETA · {total} / {total}", $"RECIPE · {total} / {total}");
        }

        if (activeStepIndex >= 0)
        {
            int current = Clamp(activeStepIndex + 1, 1, total);
            return GameLocalization.Text($"PASO · {current} / {total}", $"STEP · {current} / {total}");
        }

        int completed = Clamp(completedThroughIndex + 1, 0, total);
        return GameLocalization.Text($"PROGRESO · {completed} / {total}", $"PROGRESS · {completed} / {total}");
    }

    public static string EscapeRichText(string text)
    {
        return (text ?? string.Empty)
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }
}
