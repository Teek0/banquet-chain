using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum KitchenActorVisualState
{
    Idle,
    Targeted,
    Anticipating,
    Acting,
    Celebrating
}

public sealed class KitchenActor : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string actorId = string.Empty;
    [SerializeField] private string displayName = string.Empty;

    [Header("Visuals")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Graphic bodyGraphic;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Color idleColor = new(0.19f, 0.14f, 0.16f, 1f);
    [SerializeField] private Color targetColor = new(0.93f, 0.68f, 0.25f, 1f);
    [SerializeField] private Color celebrationColor = new(0.35f, 0.82f, 0.52f, 1f);

    [Header("Reaction Timing")]
    [SerializeField, Min(0.05f)] private float actionDuration = 0.42f;
    [SerializeField, Min(0.05f)] private float celebrationDuration = 1.05f;
    [SerializeField, Min(0f)] private float transitionSpeed = 18f;

    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Quaternion baseLocalRotation;
    private bool hasCapturedBaseTransform;
    private bool isTargeted;
    private float anticipationProgress;
    private float actionTimeRemaining;
    private float celebrationTimeRemaining;
    private KitchenReactionType lastReactionType;
    private KitchenActorVisualState lastRenderedState = (KitchenActorVisualState)(-1);

    public string ActorId => actorId;
    public float AnticipationProgress => anticipationProgress;
    public bool HasActiveReaction => actionTimeRemaining > 0f
        || celebrationTimeRemaining > 0f;
    public KitchenActorVisualState State
    {
        get
        {
            if (celebrationTimeRemaining > 0f)
            {
                return KitchenActorVisualState.Celebrating;
            }

            if (actionTimeRemaining > 0f)
            {
                return KitchenActorVisualState.Acting;
            }

            if (isTargeted && anticipationProgress > 0f)
            {
                return KitchenActorVisualState.Anticipating;
            }

            return isTargeted
                ? KitchenActorVisualState.Targeted
                : KitchenActorVisualState.Idle;
        }
    }

    private void Awake()
    {
        ResolveReferences();
        CaptureBaseTransform();
        RefreshLabel(true);
        ApplyVisuals(0f, true);
    }

    private void OnEnable()
    {
        ResolveReferences();
        CaptureBaseTransform();
        ApplyVisuals(0f, true);
    }

    private void Update()
    {
        AdvanceReaction(Time.deltaTime);
    }

    public void ConfigureIdentity(string id, string actorDisplayName)
    {
        actorId = id?.Trim() ?? string.Empty;
        displayName = actorDisplayName?.Trim() ?? string.Empty;
        RefreshLabel(true);
    }

    public void SetTargeted(bool targeted)
    {
        isTargeted = targeted;

        if (!targeted)
        {
            anticipationProgress = 0f;
        }

        RefreshLabel();
    }

    public void SetAnticipation(float normalizedProgress)
    {
        anticipationProgress = isTargeted
            ? Mathf.Clamp01(normalizedProgress)
            : 0f;
        RefreshLabel();
    }

    public void PlayAction(KitchenReactionType reactionType)
    {
        lastReactionType = reactionType;
        actionTimeRemaining = Mathf.Max(0.05f, actionDuration);
        celebrationTimeRemaining = 0f;
        anticipationProgress = 0f;
        RefreshLabel();
    }

    public void PlayCelebration()
    {
        celebrationTimeRemaining = Mathf.Max(0.05f, celebrationDuration);
        actionTimeRemaining = 0f;
        anticipationProgress = 0f;
        isTargeted = false;
        RefreshLabel();
    }

    public void ResetActor()
    {
        isTargeted = false;
        anticipationProgress = 0f;
        actionTimeRemaining = 0f;
        celebrationTimeRemaining = 0f;
        RefreshLabel(true);
        ApplyVisuals(0f, true);
    }

    public void AdvanceReaction(float deltaTime)
    {
        float safeDeltaTime = Mathf.Max(0f, deltaTime);

        if (celebrationTimeRemaining > 0f)
        {
            celebrationTimeRemaining = Mathf.Max(
                0f,
                celebrationTimeRemaining - safeDeltaTime
            );
        }
        else if (actionTimeRemaining > 0f)
        {
            actionTimeRemaining = Mathf.Max(
                0f,
                actionTimeRemaining - safeDeltaTime
            );
        }

        RefreshLabel();
        ApplyVisuals(safeDeltaTime, false);
    }

    private void ResolveReferences()
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        if (bodyGraphic == null)
        {
            bodyGraphic = GetComponent<Graphic>();
        }

        if (nameLabel == null)
        {
            nameLabel = GetComponentInChildren<TMP_Text>(true);
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

    private void ApplyVisuals(float deltaTime, bool immediate)
    {
        if (!hasCapturedBaseTransform || visualRoot == null)
        {
            return;
        }

        KitchenActorVisualState state = State;
        float scaleBoost = 0f;
        float tilt = 0f;
        float bounce = 0f;

        if (state == KitchenActorVisualState.Targeted)
        {
            scaleBoost = 0.025f;
        }
        else if (state == KitchenActorVisualState.Anticipating)
        {
            scaleBoost = 0.025f + anticipationProgress * 0.075f;
            tilt = Mathf.Lerp(-3.5f, 3.5f, anticipationProgress);
        }
        else if (state == KitchenActorVisualState.Acting)
        {
            float duration = Mathf.Max(0.05f, actionDuration);
            float phase = 1f - actionTimeRemaining / duration;
            scaleBoost = Mathf.Sin(phase * Mathf.PI) * 0.16f;
            tilt = GetActionTilt() * Mathf.Sin(phase * Mathf.PI * 2f);
            bounce = Mathf.Sin(phase * Mathf.PI) * 12f;
        }
        else if (state == KitchenActorVisualState.Celebrating)
        {
            float duration = Mathf.Max(0.05f, celebrationDuration);
            float phase = 1f - celebrationTimeRemaining / duration;
            scaleBoost = 0.06f + Mathf.Sin(phase * Mathf.PI * 6f) * 0.035f;
            tilt = Mathf.Sin(phase * Mathf.PI * 8f) * 4f;
            bounce = Mathf.Abs(Mathf.Sin(phase * Mathf.PI * 4f)) * 16f;
        }

        Vector3 desiredPosition = baseLocalPosition + Vector3.up * bounce;
        Vector3 desiredScale = baseLocalScale * (1f + scaleBoost);
        Quaternion desiredRotation = baseLocalRotation
            * Quaternion.Euler(0f, 0f, tilt);
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

        if (bodyGraphic != null)
        {
            Color desiredColor = state switch
            {
                KitchenActorVisualState.Celebrating => celebrationColor,
                KitchenActorVisualState.Targeted => targetColor,
                KitchenActorVisualState.Anticipating => Color.Lerp(
                    targetColor,
                    Color.white,
                    anticipationProgress * 0.32f
                ),
                KitchenActorVisualState.Acting => Color.Lerp(
                    targetColor,
                    Color.white,
                    0.2f
                ),
                _ => idleColor
            };
            bodyGraphic.color = immediate
                ? desiredColor
                : Color.Lerp(bodyGraphic.color, desiredColor, blend);
        }

        lastRenderedState = state;
    }

    private float GetActionTilt()
    {
        return lastReactionType switch
        {
            KitchenReactionType.Ingredient => 5f,
            KitchenReactionType.Preparation => -7f,
            KitchenReactionType.Cooking => 8f,
            KitchenReactionType.Serving => -5f,
            KitchenReactionType.Collaboration => 10f,
            _ => 5f
        };
    }

    private void RefreshLabel(bool force = false)
    {
        if (nameLabel == null)
        {
            return;
        }

        KitchenActorVisualState state = State;

        if (!force && state == lastRenderedState)
        {
            return;
        }

        string label = string.IsNullOrWhiteSpace(displayName)
            ? actorId
            : displayName;
        nameLabel.text = state switch
        {
            KitchenActorVisualState.Targeted => $"▶ {label}",
            KitchenActorVisualState.Anticipating => $"▶ {label}",
            KitchenActorVisualState.Acting => $"✦ {label}",
            KitchenActorVisualState.Celebrating => $"★ {label}",
            _ => label
        };
    }
}
