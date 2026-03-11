using System.Collections;
using TMPro;
using UnityEngine;

public class ProjectMainResultPanelController : MonoBehaviour
{
    private const string ActiveGameIdKey = "project_active_game_id";
    private const string PendingRewardFlagKey = "project_pending_reward_flag";
    private const string PendingRewardActivityIdKey = "project_pending_reward_activity_id";
    private const string PendingRewardActivityTypeKey = "project_pending_reward_activity_type";
    private const string PendingRewardPointsKey = "project_pending_reward_points";
    private const string PendingRewardOneTimeKey = "project_pending_reward_one_time";

    public static bool IsClaimPanelBlockingInput { get; private set; }

    [Header("Identity")]
    [SerializeField] private string gameId = "project_game";

    [Header("UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text hintText;

    [Header("Linked UI")]
    [SerializeField] private GameObject entryPanelToHide;
    [SerializeField] private GameObject questionMarkToHide;

    [Header("Behavior")]
    [SerializeField] private bool showOnlyInsideTrigger = true;
    [SerializeField] private KeyCode claimRewardKey = KeyCode.Return;

    [Header("Win Copy")]
    [SerializeField] private string winTitle = "Deployment Successful";
    [TextArea(2, 8)]
    [SerializeField] private string winBody =
        "The software reached production without collapsing. The boss is happy, the client is impressed, " +
        "and production will probably stay stable until someone says \"quick hotfix.\"";
    [TextArea(2, 4)]
    [SerializeField] private string winHint =
        "Testing before deployment is considered a risky but effective strategy.";
    [TextArea(2, 4)]
    [SerializeField] private string winClaimHint =
        "Press ENTER to receive your project reward.";

    [Header("Lose Copy")]
    [SerializeField] private string loseTitle = "Project Failed";
    [TextArea(2, 8)]
    [SerializeField] private string loseBody =
        "The project died somewhere between chaos, meetings, and \"small feature requests.\" " +
        "The system is now officially classified as legacy and will be maintained by future interns.";
    [TextArea(2, 4)]
    [SerializeField] private string loseHint =
        "There may be a correct order to building software. Few teams survive long enough to confirm it.";

    private bool playerInside;
    private bool showedThisVisit;
    private bool waitingForRewardClaim;
    private bool claimingReward;
    private string activeResultCode = "info";

    private void Start()
    {
        SetActiveSafe(resultPanel, false);
        IsClaimPanelBlockingInput = false;
    }

    private void OnDisable()
    {
        if (IsClaimPanelBlockingInput)
            IsClaimPanelBlockingInput = false;
    }

    private void Update()
    {
        if (!waitingForRewardClaim || !playerInside || claimingReward)
            return;

        if (Input.GetKeyDown(claimRewardKey))
            StartCoroutine(ClaimPendingReward());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        showedThisVisit = false;
        TryShowResultFromPrefs();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        SetActiveSafe(resultPanel, false);
        SetActiveSafe(questionMarkToHide, true);
        IsClaimPanelBlockingInput = false;

        if (showedThisVisit && (!waitingForRewardClaim || activeResultCode != "win"))
        {
            SetInt("result_pending", 0);
            PlayerPrefs.Save();
            showedThisVisit = false;
        }

        waitingForRewardClaim = false;
    }

    private void TryShowResultFromPrefs()
    {
        if (showOnlyInsideTrigger && !playerInside)
            return;
        if (GetInt("result_pending", 0) != 1)
            return;

        string code = GetString("result_code", "info");
        int awarded = GetInt("result_awarded_points", 0);
        activeResultCode = code;
        waitingForRewardClaim = code == "win" && PlayerPrefs.GetInt(PendingRewardFlagKey, 0) == 1;

        string title = code == "win" ? winTitle : loseTitle;
        string body = code == "win" ? winBody : loseBody;
        string hint = code == "win" ? (waitingForRewardClaim ? winClaimHint : winHint) : loseHint;

        if (code == "win" && awarded > 0)
            body += "\nReward: +" + awarded + " points.";

        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;
        if (hintText != null) hintText.text = hint;

        SetActiveSafe(entryPanelToHide, false);
        SetActiveSafe(questionMarkToHide, false);
        SetActiveSafe(resultPanel, true);
        IsClaimPanelBlockingInput = waitingForRewardClaim;
        showedThisVisit = true;
    }

    private IEnumerator ClaimPendingReward()
    {
        if (PlayerPrefs.GetInt(PendingRewardFlagKey, 0) != 1)
        {
            FinishClaimFlow(false);
            yield break;
        }

        claimingReward = true;

        float waitUntil = Time.unscaledTime + 10f;
        while ((ResumeLogic.Instance == null || !ResumeLogic.Instance.HasLoadedPlayer) && Time.unscaledTime < waitUntil)
            yield return null;

        if (ResumeLogic.Instance == null || !ResumeLogic.Instance.HasLoadedPlayer)
        {
            if (hintText != null)
                hintText.text = "Reward system is not ready yet. Try again in a moment.";
            claimingReward = false;
            yield break;
        }

        string activityId = PlayerPrefs.GetString(PendingRewardActivityIdKey, string.Empty);
        string activityType = PlayerPrefs.GetString(PendingRewardActivityTypeKey, "project");
        int scoreDelta = Mathf.Max(0, PlayerPrefs.GetInt(PendingRewardPointsKey, 0));
        bool oneTimeOnly = PlayerPrefs.GetInt(PendingRewardOneTimeKey, 1) == 1;

        bool success = false;
        bool alreadyCompleted = false;
        string errorMessage = null;
        yield return ResumeLogic.Instance.CompleteActivity(
            activityId,
            activityType,
            scoreDelta,
            oneTimeOnly,
            (updated, wasAlreadyCompleted, error) =>
            {
                success = updated;
                alreadyCompleted = wasAlreadyCompleted;
                errorMessage = error;
            });

        Debug.Log(
            "[ProjectResult] Claim reward. activityId=" + activityId +
            ", activityType=" + activityType +
            ", scoreDelta=" + scoreDelta +
            ", updated=" + success +
            ", alreadyCompleted=" + alreadyCompleted +
            ", error=" + (string.IsNullOrWhiteSpace(errorMessage) ? "<none>" : errorMessage),
            this);

        if (!success && !alreadyCompleted)
        {
            if (hintText != null)
                hintText.text = "Reward claim failed. Press ENTER to try again.";
            claimingReward = false;
            yield break;
        }

        string activeGameId = PlayerPrefs.GetString(ActiveGameIdKey, gameId);
        PlayerPrefs.SetInt(activeGameId + "_first_win_claimed", 1);
        PlayerPrefs.DeleteKey(PendingRewardFlagKey);
        PlayerPrefs.DeleteKey(PendingRewardActivityIdKey);
        PlayerPrefs.DeleteKey(PendingRewardActivityTypeKey);
        PlayerPrefs.DeleteKey(PendingRewardPointsKey);
        PlayerPrefs.DeleteKey(PendingRewardOneTimeKey);
        PlayerPrefs.SetInt(Key("result_pending"), 0);
        PlayerPrefs.Save();

        if (bodyText != null)
        {
            if (success)
                bodyText.text = winBody + "\nReward applied: +" + scoreDelta + " points.";
            else
                bodyText.text = winBody + "\nReward was already claimed earlier for this minigame.";
        }
        if (hintText != null)
            hintText.text = winHint;

        FinishClaimFlow(true);
        claimingReward = false;
    }

    private void FinishClaimFlow(bool hidePanel)
    {
        waitingForRewardClaim = false;
        IsClaimPanelBlockingInput = false;
        showedThisVisit = false;

        if (hidePanel)
        {
            SetActiveSafe(resultPanel, false);
            if (playerInside)
            {
                SetActiveSafe(questionMarkToHide, false);
                SetActiveSafe(entryPanelToHide, true);
            }
        }
    }

    private string Key(string suffix)
    {
        return gameId + "_" + suffix;
    }

    private int GetInt(string suffix, int defaultValue)
    {
        return PlayerPrefs.GetInt(Key(suffix), defaultValue);
    }

    private void SetInt(string suffix, int value)
    {
        PlayerPrefs.SetInt(Key(suffix), value);
    }

    private string GetString(string suffix, string defaultValue)
    {
        return PlayerPrefs.GetString(Key(suffix), defaultValue);
    }

    private static void SetActiveSafe(GameObject obj, bool value)
    {
        if (obj != null)
            obj.SetActive(value);
    }
}
