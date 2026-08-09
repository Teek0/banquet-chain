using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CatVisualState
{
    Hungry,
    Requesting,
    Waiting,
    Receiving,
    Relaxed,
    Satisfied,
    Sleeping
}

public sealed class CatController : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private RecipeRunner recipeRunner;

    [Header("Visuals")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private GameObject awakeVisual;
    [SerializeField] private GameObject sleepingVisual;
    [SerializeField] private Graphic bodyGraphic;
    [SerializeField] private TMP_Text stateLabel;
    [SerializeField] private TMP_Text requestLabel;
    [SerializeField] private Animator animator;
    [SerializeField] private string visualStateParameter = "VisualState";
    [SerializeField] private Color hungryColor = new(0.43f, 0.34f, 0.48f, 1f);
    [SerializeField] private Color requestingColor = new(0.78f, 0.48f, 0.22f, 1f);
    [SerializeField] private Color relaxedColor = new(0.46f, 0.62f, 0.72f, 1f);
    [SerializeField] private Color satisfiedColor = new(0.38f, 0.72f, 0.48f, 1f);

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float receivingDuration = 0.6f;
    [SerializeField, Min(0f)] private float transitionSpeed = 14f;
    [SerializeField, Min(0f)] private float satisfiedBounceHeight = 0.75f;

    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Quaternion baseLocalRotation;
    private bool hasCapturedBaseTransform;
    private bool isSubscribed;
    private float receivingTimeRemaining;
    private float animationClock;
    private CatVisualState afterReceivingState = CatVisualState.Relaxed;
    private CatVisualState renderedState = (CatVisualState)(-1);

    public CatVisualState State { get; private set; } = CatVisualState.Hungry;
    public int Satisfaction { get; private set; }
    public string CurrentRequest { get; private set; } = string.Empty;
    public bool IsReceiving => State == CatVisualState.Receiving;

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
        ApplyVisuals(0f, true);
        RefreshLabels(true);
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        AdvanceVisual(Time.deltaTime);
    }

    public void BeginBanquet()
    {
        Satisfaction = 0;
        CurrentRequest = string.Empty;
        receivingTimeRemaining = 0f;
        afterReceivingState = CatVisualState.Relaxed;
        animationClock = 0f;
        State = CatVisualState.Hungry;
        RefreshLabels(true);
        ApplyVisuals(0f, true);
    }

    public void ShowRequest(RecipeData recipe)
    {
        CurrentRequest = recipe?.CatOrder ?? string.Empty;
        receivingTimeRemaining = 0f;
        State = CatVisualState.Requesting;
        RefreshLabels(true);
    }

    public void SetWaitingForKitchen()
    {
        if (State == CatVisualState.Receiving
            || State == CatVisualState.Satisfied)
        {
            return;
        }

        State = CatVisualState.Waiting;
        RefreshLabels();
    }

    public void PlayReceiving()
    {
        receivingTimeRemaining = Mathf.Max(0.05f, receivingDuration);
        afterReceivingState = Satisfaction >= 3
            ? CatVisualState.Satisfied
            : CatVisualState.Relaxed;
        State = CatVisualState.Receiving;
        RefreshLabels();
    }

    public void RegisterServedDish(int completedDishes, bool isFinalDish)
    {
        Satisfaction = Mathf.Clamp(completedDishes, 0, 3);
        afterReceivingState = isFinalDish || Satisfaction >= 3
            ? CatVisualState.Satisfied
            : CatVisualState.Relaxed;

        if (State != CatVisualState.Receiving
            || receivingTimeRemaining <= 0f)
        {
            State = afterReceivingState;
        }

        RefreshLabels(true);
    }

    public void PlayFinalPurr()
    {
        Satisfaction = 3;
        receivingTimeRemaining = 0f;
        afterReceivingState = CatVisualState.Satisfied;
        State = CatVisualState.Satisfied;
        RefreshLabels(true);
    }

    public void PlaySleeping()
    {
        Satisfaction = 3;
        receivingTimeRemaining = 0f;
        State = CatVisualState.Sleeping;
        RefreshLabels(true);
        ApplyVisuals(0f, true);
    }

    public void AdvanceVisual(float deltaTime)
    {
        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        animationClock += safeDeltaTime;

        if (State == CatVisualState.Receiving)
        {
            receivingTimeRemaining = Mathf.Max(
                0f,
                receivingTimeRemaining - safeDeltaTime
            );

            if (receivingTimeRemaining <= 0f)
            {
                State = afterReceivingState;
                RefreshLabels();
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

        if (bodyGraphic == null)
        {
            bodyGraphic = GetComponent<Graphic>();
        }

        if (awakeVisual == null && visualRoot != null
            && visualRoot.gameObject != gameObject)
        {
            awakeVisual = visualRoot.gameObject;
        }

        if (animator == null && visualRoot != null)
        {
            animator = visualRoot.GetComponentInChildren<Animator>(true);
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

        recipeRunner.RecipeStarted += ShowRequest;
        recipeRunner.StepStarted += HandleStepStarted;
        recipeRunner.StepCompleted += HandleStepCompleted;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || recipeRunner == null)
        {
            return;
        }

        recipeRunner.RecipeStarted -= ShowRequest;
        recipeRunner.StepStarted -= HandleStepStarted;
        recipeRunner.StepCompleted -= HandleStepCompleted;
        isSubscribed = false;
    }

    private void HandleStepStarted(RecipeStep _, int __)
    {
        SetWaitingForKitchen();
    }

    private void HandleStepCompleted(RecipeStep step, int _)
    {
        if (step != null
            && (step.ReactionType == KitchenReactionType.Serving
                || string.Equals(
                    step.ExpectedWord?.Trim(),
                    "servir",
                    System.StringComparison.OrdinalIgnoreCase
                )))
        {
            PlayReceiving();
        }
    }

    private void RefreshLabels(bool force = false)
    {
        if (!force && renderedState == State)
        {
            return;
        }

        if (stateLabel != null)
        {
            stateLabel.text = State switch
            {
                CatVisualState.Requesting => "PIDE",
                CatVisualState.Waiting => "OBSERVANDO",
                CatVisualState.Receiving => "PROBANDO...",
                CatVisualState.Relaxed => $"MÁS TRANQUILO · {Satisfaction}/3",
                CatVisualState.Satisfied => "SATISFECHO · RONRONEANDO",
                CatVisualState.Sleeping => "DURMIENDO",
                _ => "HAMBRIENTO"
            };
        }

        if (requestLabel != null)
        {
            requestLabel.text = string.IsNullOrWhiteSpace(CurrentRequest)
                ? "ESPERANDO EL BANQUETE"
                : CurrentRequest;
        }

        SetAnimatorState();
        renderedState = State;
    }

    private void ApplyVisuals(float deltaTime, bool immediate)
    {
        bool isSleeping = State == CatVisualState.Sleeping;

        if (awakeVisual != null && awakeVisual != gameObject
            && awakeVisual.activeSelf == isSleeping)
        {
            awakeVisual.SetActive(!isSleeping);
        }

        if (sleepingVisual != null && sleepingVisual != gameObject
            && sleepingVisual.activeSelf != isSleeping)
        {
            sleepingVisual.SetActive(isSleeping);
        }

        if (renderedState != State)
        {
            SetAnimatorState();
        }

        if (isSleeping)
        {
            return;
        }

        if (!hasCapturedBaseTransform || visualRoot == null)
        {
            return;
        }

        float scaleBoost = 0f;
        float rotation = 0f;
        float bounce = 0f;

        switch (State)
        {
            case CatVisualState.Hungry:
                rotation = Mathf.Sin(animationClock * 4f) * 2f;
                break;
            case CatVisualState.Requesting:
                scaleBoost = 0.04f + Mathf.Sin(animationClock * 5f) * 0.02f;
                break;
            case CatVisualState.Waiting:
                scaleBoost = Mathf.Sin(animationClock * 2.5f) * 0.015f;
                break;
            case CatVisualState.Receiving:
                bounce = Mathf.Abs(Mathf.Sin(animationClock * 8f)) * 9f;
                rotation = Mathf.Sin(animationClock * 10f) * 4f;
                scaleBoost = 0.06f;
                break;
            case CatVisualState.Relaxed:
                rotation = -3f;
                scaleBoost = 0.035f;
                break;
            case CatVisualState.Satisfied:
                scaleBoost = 0.07f + Mathf.Sin(animationClock * 3f) * 0.025f;
                bounce = Mathf.Abs(Mathf.Sin(animationClock * 2f))
                    * satisfiedBounceHeight;
                break;
        }

        Vector3 desiredPosition = baseLocalPosition + Vector3.up * bounce;
        Vector3 desiredScale = baseLocalScale * (1f + scaleBoost);
        Quaternion desiredRotation = baseLocalRotation
            * Quaternion.Euler(0f, 0f, rotation);
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
            Color desiredColor = State switch
            {
                CatVisualState.Requesting => requestingColor,
                CatVisualState.Receiving => requestingColor,
                CatVisualState.Relaxed => relaxedColor,
                CatVisualState.Satisfied => satisfiedColor,
                _ => hungryColor
            };
            bodyGraphic.color = immediate
                ? desiredColor
                : Color.Lerp(bodyGraphic.color, desiredColor, blend);
        }
    }

    private void SetAnimatorState()
    {
        if (animator == null || string.IsNullOrWhiteSpace(visualStateParameter))
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Int
                && parameter.name == visualStateParameter)
            {
                animator.SetInteger(visualStateParameter, (int)State);
                return;
            }
        }
    }
}
