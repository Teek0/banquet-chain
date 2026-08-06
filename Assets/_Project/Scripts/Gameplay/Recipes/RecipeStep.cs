using System;
using UnityEngine;

public enum KitchenReactionType
{
    Ingredient,
    Preparation,
    Cooking,
    Serving
}

[Serializable]
public sealed class RecipeStep
{
    [SerializeField] private string expectedWord = string.Empty;
    [SerializeField] private Sprite icon;
    [SerializeField] private string actorId = string.Empty;
    [SerializeField] private KitchenReactionType reactionType;
    [SerializeField, Min(0f)] private float durationBeforeNextStep = 0.4f;
    [SerializeField] private bool transformsDish;

    public string ExpectedWord => expectedWord;
    public Sprite Icon => icon;
    public string ActorId => actorId;
    public KitchenReactionType ReactionType => reactionType;
    public float DurationBeforeNextStep => durationBeforeNextStep;
    public bool TransformsDish => transformsDish;
}
