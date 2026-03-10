using TMPro;
using UnityEngine;

public class ProjectMainResultPanelController : MonoBehaviour
{
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

    [Header("Win Copy")]
    [SerializeField] private string winTitle = "Deployment Successful";
    [TextArea(2, 8)]
    [SerializeField] private string winBody =
        "The software reached production without collapsing. The boss is happy, the client is impressed, " +
        "and production will probably stay stable until someone says \"quick hotfix.\"";
    [TextArea(2, 4)]
    [SerializeField] private string winHint =
        "Testing before deployment is considered a risky but effective strategy.";

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

    private void Start()
    {
        SetActiveSafe(resultPanel, false);
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

        if (showedThisVisit)
        {
            SetInt("result_pending", 0);
            PlayerPrefs.Save();
            showedThisVisit = false;
        }
    }

    private void TryShowResultFromPrefs()
    {
        if (showOnlyInsideTrigger && !playerInside)
            return;
        if (GetInt("result_pending", 0) != 1)
            return;

        string code = GetString("result_code", "info");
        int awarded = GetInt("result_awarded_points", 0);

        string title = code == "win" ? winTitle : loseTitle;
        string body = code == "win" ? winBody : loseBody;
        string hint = code == "win" ? winHint : loseHint;

        if (code == "win" && awarded > 0)
            body += "\nReward: +" + awarded + " points.";

        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;
        if (hintText != null) hintText.text = hint;

        SetActiveSafe(entryPanelToHide, false);
        SetActiveSafe(questionMarkToHide, false);
        SetActiveSafe(resultPanel, true);
        showedThisVisit = true;
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
