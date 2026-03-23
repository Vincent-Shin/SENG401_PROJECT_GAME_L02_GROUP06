using System.Collections;
using TMPro;
using UnityEngine;

public class ResumeActivityInteraction : MonoBehaviour
{
    private enum InteractionMode
    {
        InstantClaim,
        StockMinigame
    }

    public static bool IsAnyMinigameOpen { get; private set; }
    public static bool IsGameplayInputBlocked { get; private set; }

    [Header("World UI")]
    [SerializeField] private GameObject questionMark;
    [SerializeField] private GameObject activityPanel;
    [SerializeField] private TMP_Text activityTitleText;
    [SerializeField] private TMP_Text activityBodyText;
    [SerializeField] private TMP_Text activityInstructionText;
    [SerializeField] private TMP_Text hintText;

    [Header("Activity Content")]
    [TextArea(2, 6)]
    [SerializeField] private string activityTitle = "Mini Project";

    [TextArea(4, 12)]
    [SerializeField] private string activityDescription = "Press ENTER to gain resume score.";

    [SerializeField] private string activityId = "";
    [SerializeField] private string activityType = "project";
    [SerializeField] private int scoreDelta = 10;
    [SerializeField] private bool oneTimeOnly = true;

    [Header("Entry And Result Panels")]
    [SerializeField] private InteractionMode interactionMode = InteractionMode.InstantClaim;
    [TextArea(3, 8)]
    [SerializeField] private string entryInstruction =
        "ETF first. Next opens Slime Coin.\n" +
        "Hit the target before the loss limit.";
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultBodyText;
    [SerializeField] private TMP_Text resultHintText;
    [SerializeField] private CandlestickSpawnTest stockController;

    [Header("Stock Result Copy")]
    [SerializeField] private string stockWinTitle = "Profit Target Reached";
    [TextArea(3, 8)]
    [SerializeField] private string stockWinRewardBody =
        "You hit the profit target before the market could file a restraining order.\n\n" +
        "The terminal reluctantly admits you made money on purpose.\n" +
        "First-win reward unlocked: Resume +{score}.";
    [TextArea(3, 8)]
    [SerializeField] private string stockWinRepeatBody =
        "You won again.\n\n" +
        "The terminal is furious, HR is suspicious, and the first-win reward was already claimed earlier.\n" +
        "Bragging rights: granted. Extra score: denied.";
    [TextArea(3, 8)]
    [SerializeField] private string stockWinErrorBody =
        "You hit the profit target, but the resume reward tripped over office bureaucracy.\n\n" +
        "{error}";
    [SerializeField] private string stockWinHint = "Press ENTER to close.";
    [SerializeField] private string stockFailTitle = "Risk Management Died First";
    [TextArea(3, 8)]
    [SerializeField] private string stockFailBody =
        "You crossed the loss limit before locking in the target.\n\n" +
        "The market ate your plan, your confidence, and part of your imaginary internship fund.\n" +
        "No resume reward this run.";
    [SerializeField] private string stockFailHint = "Press ENTER to try again later.";

    private bool playerInRange;
    private bool isSubmitting;
    private bool isCompleted;
    private bool stockRunResolved;
    private bool stockRewardApplied;

    private bool UsesStockMinigame =>
        interactionMode == InteractionMode.StockMinigame ||
        string.Equals(activityType, "life_experience", System.StringComparison.OrdinalIgnoreCase) ||
        string.Equals(activityType, "work_experience", System.StringComparison.OrdinalIgnoreCase) ||
        gameObject.name.ToLower().Contains("work_experience") ||
        gameObject.name.ToLower().Contains("life_experience");

    private void Awake()
    {
        AutoBindStockMinigameReferences();
    }

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(activityId))
            activityId = gameObject.name.ToLower().Replace(" ", "_");

        isCompleted = IsActivityTypeAlreadyCompleted();

        SetActiveSafe(activityPanel, false);
        SetActiveSafe(gameplayPanel, false);
        SetActiveSafe(resultPanel, false);
        SetCursorState(false);
    }

    private void Update()
    {
        if ((ResumeLogic.Instance != null && ResumeLogic.Instance.IsGameplayLocked) ||
            CertificateMinigameInteraction.IsGameplayInputBlocked ||
            ResumeTailoredMinigameInteraction.IsGameplayInputBlocked ||
            ResumeSwipeMinigameInteraction.IsGameplayInputBlocked ||
            NetworkingMemoryMinigameInteraction.IsGameplayInputBlocked ||
            ProjectPipelineChaseMinigameInteraction.IsAnyMinigameOpen)
            return;

        if (UsesStockMinigame)
        {
            UpdateStockMinigame();
            return;
        }

        UpdateInstantClaimActivity();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        if (ResumeLogic.Instance != null && ResumeLogic.Instance.IsGameplayLocked)
            return;

        playerInRange = true;
        isCompleted = isCompleted || IsActivityTypeAlreadyCompleted();

        SetActiveSafe(questionMark, false);

        if (UsesStockMinigame)
        {
            OpenStockEntryPanel();
            return;
        }

        SetActiveSafe(activityPanel, true);
        RefreshPanelText();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;

        if (UsesStockMinigame)
        {
            CloseStockPanels();
            SetActiveSafe(questionMark, !isCompleted);
            return;
        }

        SetActiveSafe(activityPanel, false);
        SetActiveSafe(questionMark, !isCompleted);
    }

    private void UpdateInstantClaimActivity()
    {
        if (!playerInRange || isSubmitting || !Input.GetKeyDown(KeyCode.Return))
            return;

        if (oneTimeOnly && (isCompleted || IsActivityTypeAlreadyCompleted()))
        {
            isCompleted = true;
            SetHint("Already completed.");
            return;
        }

        StartCoroutine(GrantResumeScore());
    }

    private void UpdateStockMinigame()
    {
        if (!playerInRange)
            return;

        if (resultPanel != null && resultPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
                CloseStockResultPanel();
            return;
        }

        if (gameplayPanel != null && gameplayPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseStockPanels();
                SetActiveSafe(questionMark, true);
                return;
            }

            if (stockController == null || stockRunResolved)
                return;

            if (stockController.TryConsumeExitRequest(out bool won))
            {
                stockRunResolved = true;
                StartCoroutine(FinishStockRun(won));
            }

            return;
        }

        if (activityPanel != null && activityPanel.activeSelf && Input.GetKeyDown(KeyCode.Return))
        {
            StartStockGameplay();
            return;
        }

        if (!IsAnyMinigameOpen && Input.GetKeyDown(KeyCode.Return))
            OpenStockEntryPanel();
    }

    private void OpenStockEntryPanel()
    {
        AutoBindStockMinigameReferences();
        IsAnyMinigameOpen = true;
        IsGameplayInputBlocked = false;
        stockRunResolved = false;
        stockRewardApplied = false;

        SetActiveSafe(activityPanel, true);
        SetActiveSafe(gameplayPanel, false);
        SetActiveSafe(resultPanel, false);
        SetCursorState(false);

        if (activityTitleText != null)
            activityTitleText.text = activityTitle;

        if (activityBodyText != null)
            activityBodyText.text = BuildStockEntryBody();

        if (activityInstructionText != null)
            activityInstructionText.text = BuildStockEntryInstruction();

        SetHint("Press ENTER to play.");
    }

    private void StartStockGameplay()
    {
        AutoBindStockMinigameReferences();

        if (stockController == null || gameplayPanel == null)
        {
            SetHint("Stock minigame setup missing.");
            return;
        }

        stockRunResolved = false;
        stockRewardApplied = false;
        IsAnyMinigameOpen = true;
        IsGameplayInputBlocked = true;

        SetActiveSafe(activityPanel, false);
        SetActiveSafe(resultPanel, false);
        SetActiveSafe(gameplayPanel, true);
        gameplayPanel.transform.SetAsLastSibling();
        SetCursorState(true);
        stockController.RestartStockSession();
    }

    private IEnumerator FinishStockRun(bool won)
    {
        yield return null;

        string title = won ? stockWinTitle : stockFailTitle;
        string body;
        string hint;

        if (won)
        {
            bool rewardGranted = false;
            bool alreadyCompleted = false;
            string errorMessage = null;

            if (ResumeLogic.Instance == null)
            {
                errorMessage = "ResumeLogic missing.";
            }
            else if (!ResumeLogic.Instance.HasLoadedPlayer || ResumeLogic.Instance.CurrentPlayer == null)
            {
                errorMessage = "Load account first.";
            }
            else if (!oneTimeOnly || !IsActivityTypeAlreadyCompleted())
            {
                isSubmitting = true;
                yield return ResumeLogic.Instance.CompleteActivity(activityId, activityType, Mathf.Max(0, scoreDelta), oneTimeOnly, (success, wasAlreadyCompleted, error) =>
                {
                    rewardGranted = success;
                    alreadyCompleted = wasAlreadyCompleted;
                    errorMessage = error;
                });
                isSubmitting = false;
            }
            else
            {
                alreadyCompleted = true;
            }

            isCompleted = rewardGranted || alreadyCompleted || isCompleted;
            stockRewardApplied = rewardGranted;

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                body = FormatStockResultCopy(stockWinErrorBody, errorMessage);
                hint = stockWinHint;
            }
            else if (rewardGranted)
            {
                body = FormatStockResultCopy(stockWinRewardBody, Mathf.Max(0, scoreDelta).ToString());
                hint = stockWinHint;
            }
            else
            {
                body = stockWinRepeatBody;
                hint = stockWinHint;
            }
        }
        else
        {
            body = stockFailBody;
            hint = stockFailHint;
        }

        ShowStockResult(title, body, hint, won);
    }

    private string FormatStockResultCopy(string template, string value)
    {
        return (template ?? string.Empty)
            .Replace("{score}", value ?? string.Empty)
            .Replace("{error}", value ?? string.Empty);
    }

    private void ShowStockResult(string title, string body, string hint, bool won)
    {
        SetActiveSafe(gameplayPanel, false);
        SetActiveSafe(activityPanel, false);
        SetActiveSafe(resultPanel, true);
        if (resultPanel != null)
            resultPanel.transform.SetAsLastSibling();

        if (resultTitleText != null)
            resultTitleText.text = title;
        if (resultBodyText != null)
            resultBodyText.text = body;
        if (resultHintText != null)
            resultHintText.text = hint;

        IsAnyMinigameOpen = true;
        IsGameplayInputBlocked = true;
        SetActiveSafe(questionMark, false);
        SetCursorState(false);
    }

    private void CloseStockResultPanel()
    {
        SetActiveSafe(resultPanel, false);
        IsAnyMinigameOpen = false;
        IsGameplayInputBlocked = false;
        stockRunResolved = false;
        SetActiveSafe(questionMark, !isCompleted);
        SetCursorState(false);

        if (playerInRange)
            OpenStockEntryPanel();
    }

    private void CloseStockPanels()
    {
        SetActiveSafe(activityPanel, false);
        SetActiveSafe(gameplayPanel, false);
        SetActiveSafe(resultPanel, false);
        IsAnyMinigameOpen = false;
        IsGameplayInputBlocked = false;
        stockRunResolved = false;
        isSubmitting = false;
        SetCursorState(false);
    }

    private IEnumerator GrantResumeScore()
    {
        if (ResumeLogic.Instance == null)
        {
            SetHint("ResumeLogic missing.");
            yield break;
        }

        if (!ResumeLogic.Instance.HasLoadedPlayer || ResumeLogic.Instance.CurrentPlayer == null)
        {
            SetHint("Load account first.");
            yield break;
        }

        isSubmitting = true;
        SetHint("Updating resume...");

        bool updated = false;
        bool alreadyCompleted = false;
        string errorMessage = null;

        yield return ResumeLogic.Instance.CompleteActivity(activityId, activityType, Mathf.Max(0, scoreDelta), oneTimeOnly, (success, wasAlreadyCompleted, error) =>
        {
            updated = success;
            alreadyCompleted = wasAlreadyCompleted;
            errorMessage = error;
        });

        isSubmitting = false;

        if (alreadyCompleted)
        {
            isCompleted = true;
            RefreshPanelText();
            yield break;
        }

        if (!updated)
        {
            SetHint(string.IsNullOrEmpty(errorMessage) ? "Failed to update resume." : errorMessage);
            yield break;
        }

        isCompleted = true;
        RefreshPanelText();
    }

    private void RefreshPanelText()
    {
        if (activityTitleText != null)
            activityTitleText.text = activityTitle;

        if (activityBodyText != null)
        {
            activityBodyText.text = isCompleted
                ? activityDescription + "\n\nResume +" + scoreDelta + " already claimed."
                : activityDescription + "\n\nReward: Resume +" + scoreDelta;
        }

        SetHint(isCompleted ? "Completed." : "Press ENTER to claim.");
    }

    private void SetHint(string message)
    {
        if (hintText != null)
            hintText.text = message;
    }

    private string BuildStockEntryBody()
    {
        string baseDescription = string.IsNullOrWhiteSpace(activityDescription)
            ? "This terminal turns panic into a learning outcome."
            : activityDescription.Trim();

        string rewardLine = isCompleted && oneTimeOnly
            ? "Reward already claimed."
            : "First win: Resume +" + Mathf.Max(0, scoreDelta) + ".";

        return baseDescription + "\n" + rewardLine;
    }

    private string BuildStockEntryInstruction()
    {
        return string.IsNullOrWhiteSpace(entryInstruction)
            ? "ETF first. Next opens Slime Coin. Hit the target before the loss limit."
            : entryInstruction.Trim();
    }

    private void AutoBindStockMinigameReferences()
    {
        if (!UsesStockMinigame)
            return;

        if (stockController == null)
            stockController = FindObjectOfType<CandlestickSpawnTest>(true);

        if (activityInstructionText == null && activityPanel != null)
        {
            Transform instructionTransform = activityPanel.transform.Find("instructions");
            if (instructionTransform != null)
                activityInstructionText = instructionTransform.GetComponent<TMP_Text>();
        }

        if (gameplayPanel == null && stockController != null)
        {
            Transform candidate = stockController.transform;
            while (candidate != null)
            {
                string lowerName = candidate.name.ToLower();
                if (lowerName.Contains("stockscreem") || lowerName.Contains("stockscreen"))
                {
                    gameplayPanel = candidate.gameObject;
                    break;
                }
                candidate = candidate.parent;
            }
        }

        if (activityPanel == null)
            activityPanel = FindGameObjectByName("entry_panel_game");

        if (resultPanel == null)
            resultPanel = FindGameObjectByName("ApplyResultPanel");

        if (resultTitleText == null)
            resultTitleText = FindTextByName("ResultTitle");

        if (resultBodyText == null)
            resultBodyText = FindTextByName("ResultBody");

        if (resultHintText == null)
            resultHintText = FindTextByName("ResultHintText");
    }

    private GameObject FindGameObjectByName(string objectName)
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go.hideFlags != HideFlags.None)
                continue;
            if (go.scene.IsValid() && go.name == objectName)
                return go;
        }

        return null;
    }

    private TMP_Text FindTextByName(string objectName)
    {
        TMP_Text[] all = Resources.FindObjectsOfTypeAll<TMP_Text>();
        for (int i = 0; i < all.Length; i++)
        {
            TMP_Text text = all[i];
            if (text == null || text.hideFlags != HideFlags.None)
                continue;
            if (text.gameObject.scene.IsValid() && text.gameObject.name == objectName)
                return text;
        }

        return null;
    }

    private static void SetActiveSafe(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private static void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private bool IsActivityTypeAlreadyCompleted()
    {
        if (ResumeLogic.Instance == null || ResumeLogic.Instance.CurrentPlayer == null)
            return false;

        string normalizedType = (activityType ?? string.Empty).Trim().ToLower();
        PlayerStateDto player = ResumeLogic.Instance.CurrentPlayer;

        if (normalizedType == "project")
            return player.completed_project;
        if (normalizedType == "certificate")
            return player.completed_certificate;
        if (normalizedType == "resume" || normalizedType == "resume_tailored" || normalizedType == "resume_detect" || normalizedType == "resume_activity")
            return player.completed_resume_tailored;
        if (normalizedType == "networking")
            return player.completed_networking;
        if (normalizedType == "life_experience" || normalizedType == "work_experience")
            return player.completed_work_experience;

        return false;
    }
}
