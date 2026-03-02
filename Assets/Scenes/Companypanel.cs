using UnityEngine;
using TMPro;
using System.Collections;

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

    private bool playerInRange = false;
    private bool isSubmitting = false;
    void Update()
    {
        if (playerInRange && !isSubmitting && !IsResultPanelOpen() && Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(Apply());
        }
    }

    void Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (companyTitleText != null)
            companyTitleText.richText = true;

        if (companyBodyText != null)
            companyBodyText.richText = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        questionMark.SetActive(false);
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
        hintText.text = "Press ENTER to Apply";
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;

        companyPanel.SetActive(false);
        if (!isSubmitting && resultPanel != null)
            resultPanel.SetActive(false);
        questionMark.SetActive(true);
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
            response => backendResponse = response);

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
                ShowResultPanel(
                    "Requirements Missing",
                    "Complete these first:\n- " + string.Join("\n- ", backendResponse.missing_activities),
                    "Continue");
            }
            else
            {
                hintText.text = backendResponse.error;
            }
            yield break;
        }

        if (backendResponse.result == "success")
        {
            int scoreDelta = backendResponse.application != null ? backendResponse.application.score_delta : 0;
            ShowResultPanel(
                "Application Successful",
                "You successfully applied to " + companyName + ".\n\nCongratulations, you made it through and gained +" + scoreDelta + " resume score.",
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
                ShowResultPanel(
                    "Application Declined",
                    "Declined. You need resume " + backendResponse.minimum_score + "+.\n\nAttempts left: " + attemptsLeft,
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
            ShowResultPanel(
                "Application Declined",
                "Declined.\n\n1 attempt has been used.\nAttempts left: " + attemptsLeft,
                "Continue");
        }
    }

    public void ContinueAfterApplyResult()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        companyPanel.SetActive(false);
        hintText.text = "Press ENTER to Apply";

        if (questionMark != null)
            questionMark.SetActive(true);
    }

    void ShowResultPanel(string title, string body, string buttonLabel)
    {
        if (companyPanel != null)
            companyPanel.SetActive(false);

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();
        }

        if (resultTitleText != null)
            resultTitleText.text = title;

        if (resultBodyText != null)
            resultBodyText.text = body;

        if (resultButtonText != null)
            resultButtonText.text = buttonLabel;
    }

    bool IsResultPanelOpen()
    {
        return resultPanel != null && resultPanel.activeSelf;
    }

    void CloseCompanyPanels()
    {
        if (companyPanel != null)
            companyPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (questionMark != null)
            questionMark.SetActive(false);
    }
}