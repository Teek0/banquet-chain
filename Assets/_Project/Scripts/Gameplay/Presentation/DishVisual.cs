using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DishVisualState
{
    Empty,
    Building,
    Ready,
    Serving,
    Completed
}

public sealed class DishVisual : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private RecipeRunner recipeRunner;

    [Header("Visuals")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Graphic plateGraphic;
    [SerializeField] private TMP_Text stateLabel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private List<Graphic> progressLayers = new();
    [SerializeField] private Color emptyColor = new(0.22f, 0.17f, 0.14f, 1f);
    [SerializeField] private Color buildingColor = new(0.76f, 0.46f, 0.16f, 1f);
    [SerializeField] private Color readyColor = new(0.94f, 0.72f, 0.25f, 1f);
    [SerializeField] private Color completedColor = new(0.27f, 0.65f, 0.38f, 1f);

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float buildPulseDuration = 0.32f;
    [SerializeField, Min(0.05f)] private float servingDuration = 0.6f;
    [SerializeField, Min(0f)] private float transitionSpeed = 18f;

    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Quaternion baseLocalRotation;
    private bool hasCapturedBaseTransform;
    private bool isSubscribed;
    private bool completionRequested;
    private float buildPulseRemaining;
    private float servingTimeRemaining;
    private int totalTransformations;
    private int completedTransformations;
    private int visibleLayerCount;

    public DishVisualState State { get; private set; } = DishVisualState.Empty;
    public int TransformationCount => completedTransformations;
    public int TotalTransformations => totalTransformations;
    public int VisibleLayerCount => visibleLayerCount;
    public bool IsServing => State == DishVisualState.Serving;

    private void Awake()
    {
        ResolveReferences();
        CaptureBaseTransform();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CaptureBaseTransform();
        Subscribe();
        BeginRecipe(recipeRunner != null ? recipeRunner.CurrentRecipe : null);
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        AdvanceVisual(Time.deltaTime);
    }

    public void BeginRecipe(RecipeData recipe)
    {
        totalTransformations = CountTransformingSteps(recipe);
        completedTransformations = 0;
        visibleLayerCount = 0;
        buildPulseRemaining = 0f;
        servingTimeRemaining = 0f;
        completionRequested = false;
        State = DishVisualState.Empty;
        UpdateLayers();
        RefreshStateLabel();
        ApplyVisuals(0f, true);
    }

    public void ApplyStep(RecipeStep step)
    {
        if (step == null)
        {
            return;
        }

        if (IsServingStep(step))
        {
            BeginServing();
            return;
        }

        if (!step.TransformsDish)
        {
            return;
        }

        completedTransformations = Mathf.Min(
            completedTransformations + 1,
            Mathf.Max(1, totalTransformations)
        );
        visibleLayerCount = CalculateVisibleLayerCount();
        buildPulseRemaining = Mathf.Max(0.05f, buildPulseDuration);
        State = totalTransformations > 0
            && completedTransformations >= totalTransformations
                ? DishVisualState.Ready
                : DishVisualState.Building;
        UpdateLayers();
        RefreshStateLabel();
    }

    public void CompleteRecipe()
    {
        completionRequested = true;

        if (State != DishVisualState.Serving)
        {
            SetCompleted();
        }
    }

    public void AdvanceVisual(float deltaTime)
    {
        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        buildPulseRemaining = Mathf.Max(
            0f,
            buildPulseRemaining - safeDeltaTime
        );

        if (State == DishVisualState.Serving)
        {
            servingTimeRemaining = Mathf.Max(
                0f,
                servingTimeRemaining - safeDeltaTime
            );

            if (servingTimeRemaining <= 0f)
            {
                if (completionRequested)
                {
                    SetCompleted();
                }
                else
                {
                    State = DishVisualState.Ready;
                    RefreshStateLabel();
                }
            }
        }

        ApplyVisuals(safeDeltaTime, false);
    }

    private void ResolveReferences()
    {
        if (recipeRunner == null)
        {
            recipeRunner = FindFirstObjectByType<RecipeRunner>();
        }

        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        if (plateGraphic == null)
        {
            plateGraphic = GetComponent<Graphic>();
        }

        if (stateLabel == null)
        {
            stateLabel = GetComponentInChildren<TMP_Text>(true);
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void CaptureBaseTransform()
    {
        if (hasCapturedBaseTransform || visualRoot == null)
        {
            return;
        }

        baseLocalPosition = visualRoot.localPosition;
        baseLocalScale = visualRoot.localScale;
        baseLocalRotation = visualRoot.localRotation;
        hasCapturedBaseTransform = true;
    }

    private void Subscribe()
    {
        if (isSubscribed || recipeRunner == null)
        {
            return;
        }

        recipeRunner.RecipeStarted += BeginRecipe;
        recipeRunner.StepCompleted += HandleStepCompleted;
        recipeRunner.RecipeCompleted += HandleRecipeCompleted;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || recipeRunner == null)
        {
            return;
        }

        recipeRunner.RecipeStarted -= BeginRecipe;
        recipeRunner.StepCompleted -= HandleStepCompleted;
        recipeRunner.RecipeCompleted -= HandleRecipeCompleted;
        isSubscribed = false;
    }

    private void HandleStepCompleted(RecipeStep step, int _)
    {
        ApplyStep(step);
    }

    private void HandleRecipeCompleted(RecipeData _)
    {
        CompleteRecipe();
    }

    private void BeginServing()
    {
        completionRequested = false;
        buildPulseRemaining = 0f;
        servingTimeRemaining = Mathf.Max(0.05f, servingDuration);
        State = DishVisualState.Serving;
        RefreshStateLabel();
    }

    private void SetCompleted()
    {
        completionRequested = false;
        servingTimeRemaining = 0f;
        visibleLayerCount = progressLayers?.Count ?? 0;
        State = DishVisualState.Completed;
        UpdateLayers();
        RefreshStateLabel();
    }

    private int CalculateVisibleLayerCount()
    {
        int layerCount = progressLayers?.Count ?? 0;

        if (layerCount == 0 || completedTransformations == 0)
        {
            return 0;
        }

        if (totalTransformations <= 0)
        {
            return Mathf.Min(completedTransformations, layerCount);
        }

        float progress = (float)completedTransformations / totalTransformations;
        return Mathf.Clamp(Mathf.CeilToInt(progress * layerCount), 1, layerCount);
    }

    private void UpdateLayers()
    {
        if (progressLayers == null)
        {
            return;
        }

        for (int index = 0; index < progressLayers.Count; index++)
        {
            Graphic layer = progressLayers[index];

            if (layer != null)
            {
                layer.gameObject.SetActive(index < visibleLayerCount);
            }
        }
    }

    private void RefreshStateLabel()
    {
        if (stateLabel == null)
        {
            return;
        }

        stateLabel.text = State switch
        {
            DishVisualState.Building => totalTransformations > 0
                ? $"PREPARANDO · {completedTransformations}/{totalTransformations}"
                : "PREPARANDO",
            DishVisualState.Ready => "PLATO LISTO",
            DishVisualState.Serving => "ENTREGANDO...",
            DishVisualState.Completed => "PLATO SERVIDO",
            _ => "PLATO VACÍO"
        };
    }

    private void ApplyVisuals(float deltaTime, bool immediate)
    {
        if (!hasCapturedBaseTransform || visualRoot == null)
        {
            return;
        }

        Vector3 desiredPosition = baseLocalPosition;
        Vector3 desiredScale = baseLocalScale;
        Quaternion desiredRotation = baseLocalRotation;
        float desiredAlpha = 1f;

        if (State == DishVisualState.Serving)
        {
            float duration = Mathf.Max(0.05f, servingDuration);
            float phase = 1f - servingTimeRemaining / duration;
            float easedPhase = Mathf.SmoothStep(0f, 1f, phase);
            desiredPosition += new Vector3(
                easedPhase * 150f,
                Mathf.Sin(phase * Mathf.PI) * 38f,
                0f
            );
            desiredScale *= 1f + Mathf.Sin(phase * Mathf.PI) * 0.1f;
            desiredRotation *= Quaternion.Euler(0f, 0f, easedPhase * -10f);
            desiredAlpha = Mathf.Lerp(1f, 0.35f, easedPhase);
        }
        else if (State == DishVisualState.Completed)
        {
            desiredScale *= 1.08f;
        }
        else if (buildPulseRemaining > 0f)
        {
            float duration = Mathf.Max(0.05f, buildPulseDuration);
            float phase = 1f - buildPulseRemaining / duration;
            desiredScale *= 1f + Mathf.Sin(phase * Mathf.PI) * 0.12f;
            desiredPosition += Vector3.up * Mathf.Sin(phase * Mathf.PI) * 8f;
        }

        float blend = immediate
            ? 1f
            : 1f - Mathf.Exp(-Mathf.Max(0f, transitionSpeed) * deltaTime);
        visualRoot.localPosition = Vector3.Lerp(
            visualRoot.localPosition,
            desiredPosition,
            blend
        );
        visualRoot.localScale = Vector3.Lerp(
            visualRoot.localScale,
            desiredScale,
            blend
        );
        visualRoot.localRotation = Quaternion.Slerp(
            visualRoot.localRotation,
            desiredRotation,
            blend
        );

        if (canvasGroup != null)
        {
            canvasGroup.alpha = immediate
                ? desiredAlpha
                : Mathf.Lerp(canvasGroup.alpha, desiredAlpha, blend);
        }

        if (plateGraphic != null)
        {
            Color desiredColor = State switch
            {
                DishVisualState.Building => buildingColor,
                DishVisualState.Ready => readyColor,
                DishVisualState.Serving => readyColor,
                DishVisualState.Completed => completedColor,
                _ => emptyColor
            };
            plateGraphic.color = immediate
                ? desiredColor
                : Color.Lerp(plateGraphic.color, desiredColor, blend);
        }
    }

    private static int CountTransformingSteps(RecipeData recipe)
    {
        if (recipe?.Steps == null)
        {
            return 0;
        }

        int count = 0;

        foreach (RecipeStep step in recipe.Steps)
        {
            if (step?.TransformsDish == true)
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsServingStep(RecipeStep step)
    {
        return step.ReactionType == KitchenReactionType.Serving
            || string.Equals(
                step.ExpectedWord?.Trim(),
                "servir",
                StringComparison.OrdinalIgnoreCase
            );
    }
}
