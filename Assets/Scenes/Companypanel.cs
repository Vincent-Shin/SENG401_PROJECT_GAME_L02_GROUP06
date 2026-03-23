using UnityEngine;
using TMPro;
using System.Collections;
using System.Text;

public class CompanyInteraction : MonoBehaviour
{
    public GameObject questionMark;
    public GameObject companyPanel;
    public GameObject resultPanel;
    public TMP_Text companyTitleText;
    public TMP_Text companyBodyText;
    public TMP_Text hintText;
    public TMP_Text resultTitleText;
    public TMP_Text resultBodyText;
    public TMP_Text resultButtonText;

    [TextArea(2, 6)]
    public string companyName;

    [TextArea(8, 20)]
    public string requirementText;
    public string companyTier = "startup";

    [Header("Result Body Text")]
    [TextArea(4, 10)]
    public string successResultBody = "Congratulations. You made it through and gained resume score from this company.";

    [TextArea(4, 10)]
    public string requirementBlockedResultBody = "You are still missing something important for this company. Fix your resume or finish the required tasks first.";

    [TextArea(4, 10)]
    public string declinedResultBody = "They looked at your application, made a face, and sent a rejection back.";

    private bool playerInRange = false;
    private bool isSubmitting = false;
    private bool showingResult = false;
    private string postResultHintText = "Press ENTER to Apply";
    private int lastKnownPlayerId = -1;

    void Update()
    {
        int currentPlayerId = ResumeLogic.Instance != null && ResumeLogic.Instance.CurrentPlayer != null
            ? ResumeLogic.Instance.CurrentPlayer.id
            : -1;

        if (currentPlayerId != lastKnownPlayerId)
        {
            lastKnownPlayerId = currentPlayerId;
            playerInRange = false;
            showingResult = false;
            if (resultPanel != null && resultPanel != companyPanel)
                resultPanel.SetActive(false);
            RefreshCompanyAvailability();
        }

        if ((ResumeLogic.Instance != null && ResumeLogic.Instance.IsGameplayLocked) ||
            ResumeActivityInteraction.IsGameplayInputBlocked ||
            CertificateMinigameInteraction.IsGameplayInputBlocked ||
            ResumeTailoredMinigameInteraction.IsGameplayInputBlocked ||
            ResumeSwipeMinigameInteraction.IsGameplayInputBlocked ||
            ProjectPipelineChaseMinigameInteraction.IsAnyMinigameOpen)
            return;

        if (showingResult && !isSubmitting && Input.GetKeyDown(KeyCode.Return))
        {
            ContinueAfterApplyResult();
            return;
        }

        if (playerInRange && !isSubmitting && !showingResult && Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(Apply());
        }
    }

    void Start()
    {
        if (resultPanel != null && resultPanel != companyPanel)
            resultPanel.SetActive(false);

        if (companyTitleText != null)
            companyTitleText.richText = true;

        if (companyBodyText != null)
            companyBodyText.richText = true;

        if (resultTitleText != null)
            resultTitleText.richText = true;

        if (resultBodyText != null)
            resultBodyText.richText = true;

        RefreshCompanyAvailability();
    }

    void OnEnable()
    {
        RefreshCompanyAvailability();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (ResumeLogic.Instance != null && ResumeLogic.Instance.IsGameplayLocked) return;

        if (HasCompletedThisCompanyTier())
        {
            if (questionMark != null)
                questionMark.SetActive(false);
            if (companyPanel != null)
                companyPanel.SetActive(false);
            return;
        }

        playerInRange = true;
        showingResult = false;

        if (questionMark != null)
            questionMark.SetActive(false);
        if (companyPanel != null)
            companyPanel.SetActive(true);

        if (companyTitleText != null)
        {
            companyTitleText.richText = true;
            companyTitleText.text = companyName;
        }

        if (companyBodyText != null)
        {
            companyBodyText.richText = true;
            companyBodyText.text = requirementText;
        }
        postResultHintText = "Press ENTER to Apply";
        hintText.text = "Press ENTER to Apply";
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;

        if (companyPanel != null)
            companyPanel.SetActive(false);
        showingResult = false;
        if (!isSubmitting && resultPanel != null && resultPanel != companyPanel)
            resultPanel.SetActive(false);
        if (questionMark != null)
            questionMark.SetActive(!HasCompletedThisCompanyTier());

        // Big Tech win flow: close result by leaving object, then show global WinPanel.
        if (companyTier == "big_tech" && ResumeLogic.Instance != null)
            ResumeLogic.Instance.RevealDeferredWinPanelIfNeeded();
    }

    IEnumerator Apply()
    {
        float marketPercent = 0f;
        float marketMultiplier = 1f;

        if (MarketPhaseController.Instance != null)
        {
            marketPercent = MarketPhaseController.Instance.CurrentPercent;
            marketMultiplier = MarketPhaseController.Instance.CurrentMultiplier;
        }

        if (ResumeLogic.Instance == null)
        {
            hintText.text = "ResumeLogic missing.";
            yield break;
        }

        if (!ResumeLogic.Instance.HasLoadedPlayer)
        {
            hintText.text = "Load account first.";
            yield break;
        }

        isSubmitting = true;
        hintText.text = "Submitting application...";

        ApplyResponse backendResponse = null;
        yield return ResumeLogic.Instance.ApplyToCompany(
            companyTier,
            marketPercent,
            marketMultiplier,
            "Applied to " + companyName,
            response => backendResponse = response,
            companyTier == "big_tech");

        isSubmitting = false;

        if (backendResponse == null)
        {
            hintText.text = "No backend response.";
            yield break;
        }

        if (!string.IsNullOrEmpty(backendResponse.error))
        {
            if (backendResponse.reason == "missing_required_activities" && backendResponse.missing_activities != null && backendResponse.missing_activities.Length > 0)
            {
                if (hintText != null)
                    hintText.text = "Requirements missing.";
                ShowResultPanel(
                    companyName,
                    BuildRequirementBlockedResultBody(backendResponse),
                    "Continue");
            }
            else if (backendResponse.reason == "already_applied_to_tier")
            {
                postResultHintText = string.Empty;
                MarkCompanyAsCompleted();
            }
            else
            {
                hintText.text = backendResponse.error;
            }
            yield break;
        }

        if (backendResponse.result == "success")
        {
            MarkCompanyAsCompleted();
            ShowResultPanel(
                companyName,
                successResultBody,
                "Continue");
        }
        else if (backendResponse.reason == "resume_below_requirement")
        {
            int attemptsLeft = backendResponse.player != null ? backendResponse.player.apply_attempts_left : 0;
            bool gameOver = backendResponse.player != null && backendResponse.player.is_game_over;
            if (gameOver)
            {
                CloseCompanyPanels();
                hintText.text = "Game Over";
            }
            else
            {
                if (hintText != null)
                    hintText.text = "Requirements not met.";
                ShowResultPanel(
                    companyName,
                    BuildRequirementBlockedResultBody(backendResponse),
                    "Continue");
            }
        }
        else if (backendResponse.player != null && backendResponse.player.is_game_over)
        {
            CloseCompanyPanels();
            hintText.text = "Game Over";
        }
        else
        {
            int attemptsLeft = backendResponse.player != null ? backendResponse.player.apply_attempts_left : 0;
            if (hintText != null)
                hintText.text = "Attempts left: " + attemptsLeft;
            ShowResultPanel(
                companyName,
                declinedResultBody,
                "Continue");
        }
    }

    public void ContinueAfterApplyResult()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        showingResult = false;
        bool completedCompany = HasCompletedThisCompanyTier();

        if (companyPanel != null)
            companyPanel.SetActive(false);
        if (hintText != null)
            hintText.text = postResultHintText;

        if (questionMark != null)
            questionMark.SetActive(!completedCompany);

        if (companyTier == "big_tech" && ResumeLogic.Instance != null)
            ResumeLogic.Instance.RevealDeferredWinPanelIfNeeded();
    }

    void ShowResultPanel(string title, string body, string buttonLabel)
    {
        showingResult = true;

        GameObject activeResultPanel = resultPanel != null ? resultPanel : companyPanel;
        TMP_Text activeResultTitle = resultTitleText != null ? resultTitleText : companyTitleText;
        TMP_Text activeResultBody = resultBodyText != null ? resultBodyText : companyBodyText;

        if (activeResultPanel != companyPanel && (activeResultTitle == companyTitleText || activeResultBody == companyBodyText))
            activeResultPanel = companyPanel;

        if (companyPanel != null && activeResultPanel != companyPanel)
            companyPanel.SetActive(false);

        if (activeResultPanel != null)
        {
            activeResultPanel.SetActive(true);
            activeResultPanel.transform.SetAsLastSibling();
        }

        if (activeResultTitle != null)
        {
            activeResultTitle.richText = true;
            activeResultTitle.text = title;
        }

        if (activeResultBody != null)
        {
            activeResultBody.richText = true;
            activeResultBody.text = body;
        }

        if (resultButtonText != null)
            resultButtonText.text = buttonLabel;
    }

    void CloseCompanyPanels()
    {
        showingResult = false;

        if (companyPanel != null)
            companyPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (questionMark != null)
            questionMark.SetActive(false);
    }

    bool HasCompletedThisCompanyTier()
    {
        if (ResumeLogic.Instance == null || ResumeLogic.Instance.CurrentPlayer == null || ResumeLogic.Instance.CurrentPlayer.successful_company_tiers == null)
            return false;

        for (int i = 0; i < ResumeLogic.Instance.CurrentPlayer.successful_company_tiers.Length; i++)
        {
            if (ResumeLogic.Instance.CurrentPlayer.successful_company_tiers[i] == companyTier)
                return true;
        }

        return false;
    }

    void MarkCompanyAsCompleted()
    {
        playerInRange = false;
        postResultHintText = string.Empty;

        if (companyPanel != null)
            companyPanel.SetActive(false);

        if (questionMark != null)
            questionMark.SetActive(false);
    }

    void RefreshCompanyAvailability()
    {
        bool completed = HasCompletedThisCompanyTier();

        if (completed)
        {
            if (companyPanel != null)
                companyPanel.SetActive(false);

            if (resultPanel != null && !isSubmitting)
                resultPanel.SetActive(false);

            if (questionMark != null)
                questionMark.SetActive(false);
        }
        else if (!playerInRange && questionMark != null)
        {
            questionMark.SetActive(true);
        }
    }

    string BuildRequirementBlockedResultBody(ApplyResponse backendResponse)
    {
        StringBuilder body = new StringBuilder();
        body.Append("<color=#C36A1D><b>APPLICATION RESULT:\nREQUIREMENTS NOT MET</b></color>\n");
        body.Append("Your application cannot proceed at this time.\n\n");
        body.Append("<b>Required Minimum:</b>\n");

        switch ((companyTier ?? string.Empty).Trim().ToLower())
        {
            case "startup":
                body.Append("- Resume score: 50+\n");
                body.Append("- Resume mini-game completed\n");
                body.Append("- Must look employable before being underpaid\n");
                break;

            case "mid_tier":
                body.Append("- Resume score: 70+\n");
                body.Append("- Project mini-game completed\n");
                body.Append("- Must already survive one Startup company first\n");
                break;

            case "big_tech":
                body.Append("- Resume score: 85+\n");
                body.Append("- Life experience mini-game completed\n");
                body.Append("- Certificate mini-game completed\n");
                body.Append("- Must already succeed in Mid-tier first\n");
                break;
        }

        body.Append("\n<b>Current status:</b>\n");

        bool hasAnyMissingLine = false;
        if (backendResponse != null && backendResponse.reason == "resume_below_requirement")
        {
            int currentScore = backendResponse.player != null ? backendResponse.player.score : 0;
            body.Append("- Resume score is currently ").Append(currentScore).Append(".\n");
            hasAnyMissingLine = true;
        }

        if (backendResponse != null && backendResponse.missing_activities != null)
        {
            for (int i = 0; i < backendResponse.missing_activities.Length; i++)
            {
                string missing = backendResponse.missing_activities[i];
                if (string.IsNullOrWhiteSpace(missing))
                    continue;

                body.Append("- Missing: ").Append(missing).Append("\n");
                hasAnyMissingLine = true;
            }
        }

        if (!hasAnyMissingLine)
            body.Append("- Requirements are still not satisfied.\n");

        body.Append("\nFinish the missing items, then come back when your resume looks slightly more corporate-approved.");
        return body.ToString();
    }
}
