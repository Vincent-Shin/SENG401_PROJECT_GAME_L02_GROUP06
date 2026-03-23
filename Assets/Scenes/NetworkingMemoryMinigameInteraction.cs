using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class NetworkingMemoryMinigameInteraction : MonoBehaviour
{
    private const string InstructionPrefix = "<size=120%><b>Instruction:</b></size>\n";

    [System.Serializable]
    private class HintCard
    {
        public string answer;
        [TextArea(2, 6)]
        public string hint;

        public HintCard(string answer, string hint)
        {
            this.answer = answer;
            this.hint = hint;
        }
    }

    public static bool IsAnyMinigameOpen { get; private set; }
    public static bool IsGameplayInputBlocked { get; private set; }

    [Header("World")]
    [SerializeField] private GameObject questionMark;

    [Header("Entry Panel")]
    [SerializeField] private GameObject entryPanel;
    [SerializeField] private TMP_Text entryTitleText;
    [SerializeField] private TMP_Text entryDescriptionText;
    [FormerlySerializedAs("entryTipText")]
    [SerializeField] private TMP_Text entryInstructionText;
    [SerializeField] private TMP_Text entryHintText;

    [Header("Entry Copy")]
    [SerializeField] private string entryTitle = "Networking Memory Test";
    [TextArea(3, 10)]
    [SerializeField] private string entryDescription =
        "You've been networking a lot lately.\n" +
        "Met dozens of people, shook hands, exchanged business cards...\n\n" +
        "Unfortunately, your brain decided to remember exactly none of it.\n\n" +
        "Somewhere in your head you remember clues about their name and the company they work for.\n" +
        "Now you must reconstruct the information before it becomes socially awkward.";
    [TextArea(3, 10)]
    [SerializeField] private string requirementText =
        "Each round shows one hint about a name and one hint about a company.\n" +
        "Type the correct Name and Company.\n\n" +
        "Both must be correct to succeed.\n\n" +
        "Good luck remembering who you talked to.";
    [SerializeField] private string enterHint = "Press ENTER to play.";

    [Header("Gameplay Panel")]
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private TMP_Text gameplayTitleText;
    [SerializeField] private TMP_Text nameHintText;
    [SerializeField] private TMP_Text companyHintText;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_InputField companyInputField;
    [SerializeField] private TMP_Text gameplayHintText;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultBodyText;
    [SerializeField] private TMP_Text resultHintText;

    [Header("Result Copy")]
    [SerializeField] private string winTitle = "Networking Success";
    [TextArea(3, 10)]
    [SerializeField] private string winBody =
        "You somehow remembered both the name and the company correctly.\n" +
        "The person nods approvingly, impressed that you remembered them.\n\n" +
        "They say:\n" +
        "\"Nice to see someone with actual memory at these events.\"\n\n" +
        "Your networking reputation increases.";
    [SerializeField] private string winHint = "People appreciate when you remember their name.";
    [SerializeField] private string loseTitle = "Networking Failure";
    [TextArea(3, 10)]
    [SerializeField] private string loseBody =
        "You confidently guessed wrong and turned a normal conversation into a small social disaster.\n" +
        "They stare at you for a second, politely, the way people look at corrupted spreadsheets.\n\n" +
        "Your networking reputation survives, but only because nobody expects much from mixers anyway.";
    [SerializeField] private string loseHint = "Names and companies both matter. Social recovery is expensive.";

    [Header("Reward")]
    [SerializeField] private string activityId = "networking_game";
    [SerializeField] private string activityType = "networking";
    [SerializeField] private int scoreReward = 5;
    [SerializeField] private bool oneTimeOnly = true;

    [Header("Cards")]
    [SerializeField] private HintCard[] nameCards =
    {
        new HintCard("Ronnie", "Starts with R\n6 letters\nYour favorite teacher in SENG401, but the difficulty of the tests changes depending on his mood."),
        new HintCard("Trump", "Starts with T\n5 letters\nA politician who treats social media like a multiplayer game.\nOne post can move the price of Bitcoin."),
        new HintCard("Elon", "Starts with E\n4 letters\nExtremely rich tech billionaire who launches rockets when he gets bored."),
        new HintCard("Gordon", "Starts with G\n6 letters\nFamous chef known for cooking shows and extremely creative curse words."),
        new HintCard("Cristiano", "Starts with C\n9 letters\nFootball legend with the celebration \"Siuuu\".\nSome people call him GOAT."),
        new HintCard("Rahul", "Starts with R\n5 letters\nA very common Indian name.\nYou'll probably see this name many times on the \"Top Tech Employers\" employee list."),
        new HintCard("Kevin", "Starts with K\n5 letters\nRhymes with \"seven\".\nA classic office name you'll hear everywhere."),
        new HintCard("Alice", "Starts with A\n5 letters\nA standard office name that sounds like someone who already invited you to a meeting.")
    };
    [SerializeField] private HintCard[] companyCards =
    {
        new HintCard("Amazon", "Starts with A\n6 letters\nOnline shopping giant where packages appear at your door before you even remember ordering them."),
        new HintCard("Google", "Starts with G\n6 letters\nSearch engine giant that probably knows your question before you finish typing it."),
        new HintCard("Microsoft", "Starts with M\n9 letters\nThe company behind Windows, Excel, and more meetings than any human deserves."),
        new HintCard("Nvidia", "Starts with N\n6 letters\nChip company currently powering every second AI startup pitch deck."),
        new HintCard("Tesla", "Starts with T\n5 letters\nElectric car company run by the same billionaire who also treats space as a side quest."),
        new HintCard("Netflix", "Starts with N\n7 letters\nStreaming company that keeps asking if you're still watching while deadlines approach."),
        new HintCard("Apple", "Starts with A\n5 letters\nTech company famous for premium devices and making wallets feel lighter."),
        new HintCard("OpenAI", "Starts with O\n6 letters\nAI company that somehow became everyone's coworker, tutor, and debugging partner.")
    };

    private bool playerInRange;
    private bool isPlaying;
    private bool isSubmitting;
    private bool hasCompletedReward;
    private HintCard activeNameCard;
    private HintCard activeCompanyCard;

    private void Awake()
    {
        IsAnyMinigameOpen = false;
        IsGameplayInputBlocked = false;
    }

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(activityId))
            activityId = "networking_game";

        SetActiveSafe(entryPanel, false);
        SetActiveSafe(gameplayPanel, false);
        SetActiveSafe(resultPanel, false);
    }

    private void Update()
    {
        if (!playerInRange || ResumeLogic.Instance == null)
            return;

        if (CertificateMinigameInteraction.IsAnyMinigameOpen ||
            ResumeActivityInteraction.IsGameplayInputBlocked ||
            ResumeTailoredMinigameInteraction.IsAnyMinigameOpen ||
            ResumeSwipeMinigameInteraction.IsAnyMinigameOpen ||
            ProjectPipelineChaseMinigameInteraction.IsAnyMinigameOpen)
            return;

        if (ResumeLogic.Instance.IsGameplayLocked && !IsAnyMinigameOpen)
            return;

        if (resultPanel != null && resultPanel.activeSelf && Input.GetKeyDown(KeyCode.Return))
        {
            CloseResultPanel();
            return;
        }

        if (!IsAnyMinigameOpen && Input.GetKeyDown(KeyCode.Return))
        {
            OpenEntryPanel();
            return;
        }

        if (entryPanel != null && entryPanel.activeSelf && Input.GetKeyDown(KeyCode.Return))
        {
            StartNetworkingRound();
            return;
        }

        if (!isPlaying || isSubmitting)
            return;

        if (Input.GetKeyDown(KeyCode.Return))
            SubmitFromUi();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;
        hasCompletedReward = HasAlreadyCompletedReward();

        SetActiveSafe(questionMark, false);
        OpenEntryPanel();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;
        CloseAllPanels();
        SetActiveSafe(questionMark, true);
    }

    private void OpenEntryPanel()
    {
        if (!playerInRange)
            return;

        hasCompletedReward = HasAlreadyCompletedReward();
        SetPanelOpenState(true, false);
        SetActiveSafe(gameplayPanel, false);
        SetActiveSafe(resultPanel, false);
        SetActiveSafe(entryPanel, true);

        if (entryTitleText != null)
            entryTitleText.text = entryTitle;
        if (entryDescriptionText != null)
            entryDescriptionText.text = entryDescription;
        if (entryInstructionText != null)
            entryInstructionText.text = InstructionPrefix + requirementText + "\nFirst clear bonus: Resume +" + Mathf.Max(0, scoreReward) + ".";
        if (entryHintText != null)
            entryHintText.text = enterHint;

        SetCursorState(false);
    }

    private void StartNetworkingRound()
    {
        if (nameCards == null || nameCards.Length == 0 || companyCards == null || companyCards.Length == 0)
        {
            if (entryHintText != null)
                entryHintText.text = "Networking cards are missing.";
            return;
        }

        activeNameCard = nameCards[Random.Range(0, nameCards.Length)];
        activeCompanyCard = companyCards[Random.Range(0, companyCards.Length)];
        isPlaying = true;
        isSubmitting = false;

        SetActiveSafe(entryPanel, false);
        SetActiveSafe(resultPanel, false);
        SetActiveSafe(gameplayPanel, true);
        SetPanelOpenState(true, true);
        SetCursorState(true);

        if (gameplayTitleText != null)
            gameplayTitleText.text = "<color=#6B4712><size=125%><b>Find the network</b></size></color>";
        if (nameHintText != null)
            nameHintText.text = "<b>Name hint:</b>\n" + activeNameCard.hint;
        if (companyHintText != null)
            companyHintText.text = "<b>Company hint:</b>\n" + activeCompanyCard.hint;
        if (gameplayHintText != null)
            gameplayHintText.text = "Press ENTER to submit.";

        if (nameInputField != null)
        {
            nameInputField.text = string.Empty;
            nameInputField.ActivateInputField();
        }

        if (companyInputField != null)
            companyInputField.text = string.Empty;
    }

    private void SubmitFromUi()
    {
        if (!isPlaying || isSubmitting)
            return;

        string enteredName = NormalizeAnswer(nameInputField != null ? nameInputField.text : string.Empty);
        string enteredCompany = NormalizeAnswer(companyInputField != null ? companyInputField.text : string.Empty);
        bool nameCorrect = enteredName == NormalizeAnswer(activeNameCard != null ? activeNameCard.answer : string.Empty);
        bool companyCorrect = enteredCompany == NormalizeAnswer(activeCompanyCard != null ? activeCompanyCard.answer : string.Empty);

        StartCoroutine(ResolveRound(nameCorrect && companyCorrect));
    }

    private IEnumerator ResolveRound(bool success)
    {
        isSubmitting = true;
        isPlaying = false;

        SetActiveSafe(gameplayPanel, false);

        if (!success)
        {
            ShowResult(false, false);
            isSubmitting = false;
            yield break;
        }

        if (ResumeLogic.Instance == null || !ResumeLogic.Instance.HasLoadedPlayer)
        {
            ShowResult(false, false);
            if (resultHintText != null)
                resultHintText.text = "Load an account first.";
            isSubmitting = false;
            yield break;
        }

        bool updated = false;
        bool alreadyCompleted = false;
        string errorMessage = null;
        yield return ResumeLogic.Instance.CompleteActivity(
            activityId,
            activityType,
            Mathf.Max(0, scoreReward),
            oneTimeOnly,
            (requestUpdated, wasAlreadyCompleted, error) =>
            {
                updated = requestUpdated;
                alreadyCompleted = wasAlreadyCompleted;
                errorMessage = error;
            });

        if (!updated && !alreadyCompleted)
        {
            ShowResult(false, false);
            if (resultHintText != null)
                resultHintText.text = string.IsNullOrWhiteSpace(errorMessage)
                    ? "Failed to update networking score."
                    : errorMessage;
            isSubmitting = false;
            yield break;
        }

        hasCompletedReward = hasCompletedReward || updated || alreadyCompleted;
        ShowResult(true, updated);
        isSubmitting = false;
    }

    private void ShowResult(bool success, bool awardedReward)
    {
        SetActiveSafe(entryPanel, false);
        SetActiveSafe(gameplayPanel, false);
        SetActiveSafe(resultPanel, true);
        SetPanelOpenState(true, true);
        SetCursorState(true);

        if (resultTitleText != null)
            resultTitleText.text = success ? winTitle : loseTitle;

        if (resultBodyText != null)
        {
            if (!success)
            {
                resultBodyText.text = loseBody;
            }
            else if (awardedReward)
            {
                resultBodyText.text = winBody + "\n\nResume +" + Mathf.Max(0, scoreReward) + " awarded.";
            }
            else
            {
                resultBodyText.text = winBody + "\n\nReplay detected. No additional resume points awarded.";
            }
        }

        if (resultHintText != null)
            resultHintText.text = success ? winHint : loseHint;
    }

    private void CloseResultPanel()
    {
        SetActiveSafe(resultPanel, false);

        if (playerInRange)
        {
            OpenEntryPanel();
            return;
        }

        CloseAllPanels();
    }

    private void CloseAllPanels()
    {
        isPlaying = false;
        isSubmitting = false;
        SetActiveSafe(entryPanel, false);
        SetActiveSafe(gameplayPanel, false);
        SetActiveSafe(resultPanel, false);
        SetPanelOpenState(false, false);
        SetCursorState(false);
    }

    private bool HasAlreadyCompletedReward()
    {
        if (ResumeLogic.Instance == null || ResumeLogic.Instance.CurrentPlayer == null || ResumeLogic.Instance.CurrentPlayer.completed_activity_ids == null)
            return false;

        string normalizedId = NormalizeAnswer(activityId);
        string[] completedIds = ResumeLogic.Instance.CurrentPlayer.completed_activity_ids;
        for (int i = 0; i < completedIds.Length; i++)
        {
            if (NormalizeAnswer(completedIds[i]) == normalizedId)
                return true;
        }

        return false;
    }

    private static void SetActiveSafe(GameObject obj, bool value)
    {
        if (obj != null)
            obj.SetActive(value);
    }

    private static string NormalizeAnswer(string value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static void SetPanelOpenState(bool isOpen, bool blockGameplayInput)
    {
        IsAnyMinigameOpen = isOpen;
        IsGameplayInputBlocked = blockGameplayInput;
    }

    private static void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
