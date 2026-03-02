using System.Collections;
using TMPro;
using UnityEngine;

public class ResumeActivityInteraction : MonoBehaviour
{
    [Header("World UI")]
    [SerializeField] private GameObject questionMark;
    [SerializeField] private GameObject activityPanel;
    [SerializeField] private TMP_Text activityTitleText;
    [SerializeField] private TMP_Text activityBodyText;
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

    private bool playerInRange;
    private bool isSubmitting;
    private bool isCompleted;

    void Start()
    {
        if (string.IsNullOrWhiteSpace(activityId))
            activityId = gameObject.name.ToLower().Replace(" ", "_");

        if (activityPanel != null)
            activityPanel.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange || isSubmitting || !Input.GetKeyDown(KeyCode.Return))
            return;

        if (oneTimeOnly && isCompleted)
        {
            SetHint("Already completed.");
            return;
        }

        StartCoroutine(GrantResumeScore());
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;

        if (questionMark != null)
            questionMark.SetActive(false);

        if (activityPanel != null)
            activityPanel.SetActive(true);

        RefreshPanelText();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;

        if (activityPanel != null)
            activityPanel.SetActive(false);

        if (questionMark != null)
            questionMark.SetActive(!isCompleted);
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
}
