using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProjectMainTerminalInteraction : MonoBehaviour
{
    private const string ActiveGameIdKey = "project_active_game_id";

    [Header("Identity")]
    [SerializeField] private string gameId = "project_game";
    [SerializeField] private bool clearSavedStateOnStart = false;

    [Header("Scene Flow")]
    [SerializeField] private string projectMinigameSceneName = "ProjectMinigameScene";

    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("World UI")]
    [SerializeField] private GameObject questionMark;
    [SerializeField] private GameObject entryPanel;
    [SerializeField] private TMP_Text entryTitleText;
    [SerializeField] private TMP_Text entryBodyText;
    [SerializeField] private TMP_Text entryInstructionText;
    [SerializeField] private TMP_Text entryHintText;
    [SerializeField] private Image entryPreviewImage;
    [SerializeField] private bool hidePreviewWhenEmpty = true;

    [Header("Entry Copy")]
    [SerializeField] private string entryTitle = "Software Development Simulator";
    [SerializeField] private string replayTitle = "Software Development Simulator (Replay Mode)";
    [SerializeField] private Sprite previewSprite;
    [TextArea(2, 8)]
    [SerializeField] private string entryDescription =
        "Collect the sacred artifacts of software development while avoiding real-world hazards like meetings, " +
        "boss check-ins, and clients requesting \"one tiny change.\" Fail the process and your project instantly becomes legacy code.";
    [TextArea(2, 8)]
    [SerializeField] private string requirementText =
        "Collect development resources and avoid skeleton problems.";
    [TextArea(2, 8)]
    [SerializeField] private string replayRequirementText =
        "You can replay this minigame, but no additional points will be awarded.";
    [SerializeField] private int firstWinPoints = 10;
    [SerializeField] private string enterHint = "Press ENTER to play.";
    [SerializeField] private string replayHint = "Press ENTER to replay.";

    private bool playerInRange;

    private void Start()
    {
        if (clearSavedStateOnStart)
            ClearSavedStateForThisGame();

        SetActiveSafe(entryPanel, false);
        SetActiveSafe(questionMark, true);
        ApplyEntryCopyByState();
    }

    private void Update()
    {
        if (!playerInRange)
            return;
        if (HasPendingResult())
            return;

        if (Input.GetKeyDown(KeyCode.Return))
            EnterMinigameScene();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;
        if (HasPendingResult())
        {
            SetActiveSafe(questionMark, false);
            SetActiveSafe(entryPanel, false);
            return;
        }

        SetActiveSafe(questionMark, false);
        SetActiveSafe(entryPanel, true);
        ApplyEntryCopyByState();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;
        SetActiveSafe(entryPanel, false);
        SetActiveSafe(questionMark, true);
    }

    private void EnterMinigameScene()
    {
        if (player != null)
        {
            PlayerPrefs.SetFloat("project_return_x", player.position.x);
            PlayerPrefs.SetFloat("project_return_y", player.position.y);
        }

        SetInt("played_once", 1);
        PlayerPrefs.SetString(ActiveGameIdKey, gameId);
        PlayerPrefs.SetInt("project_should_return_to_terminal", 1);
        PlayerPrefs.SetString("project_return_scene_name", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();

        SceneManager.LoadScene(projectMinigameSceneName);
    }

    private void ApplyEntryCopyByState()
    {
        bool wonAtLeastOnce = GetInt("first_win_claimed", 0) == 1;
        string title = wonAtLeastOnce ? replayTitle : entryTitle;
        string hint = wonAtLeastOnce ? replayHint : enterHint;

        string description = entryDescription;
        string instruction;
        if (wonAtLeastOnce)
        {
            instruction = replayRequirementText;
        }
        else
        {
            instruction = requirementText + "\nFirst clear bonus: +" + Mathf.Max(0, firstWinPoints) + " points.";
        }

        if (entryTitleText != null)
            entryTitleText.text = title;
        if (entryBodyText != null)
            entryBodyText.text = description;
        if (entryInstructionText != null)
            entryInstructionText.text = instruction;
        else if (entryBodyText != null)
            entryBodyText.text = description + "\n\nInstruction:\n" + instruction;
        if (entryHintText != null)
            entryHintText.text = hint;
        if (entryPreviewImage != null)
        {
            entryPreviewImage.sprite = previewSprite;
            if (hidePreviewWhenEmpty)
                entryPreviewImage.enabled = previewSprite != null;
        }
    }

    [ContextMenu("Clear Saved State For This Game")]
    public void ClearSavedStateForThisGame()
    {
        PlayerPrefs.DeleteKey(Key("played_once"));
        PlayerPrefs.DeleteKey(Key("last_result_code"));
        PlayerPrefs.DeleteKey(Key("first_win_claimed"));
        PlayerPrefs.DeleteKey(Key("result_pending"));
        PlayerPrefs.DeleteKey(Key("result_code"));
        PlayerPrefs.DeleteKey(Key("result_title"));
        PlayerPrefs.DeleteKey(Key("result_body"));
        PlayerPrefs.DeleteKey(Key("result_hint"));
        PlayerPrefs.DeleteKey(Key("result_awarded_points"));
        PlayerPrefs.Save();
    }

    private bool HasPendingResult()
    {
        return GetInt("result_pending", 0) == 1;
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
