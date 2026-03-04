using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CertificateMinigameInteraction : MonoBehaviour
{
    private struct QuestionData
    {
        public string prompt;
        public string[] answers;
        public int correctIndex;

        public QuestionData(string prompt, string[] answers, int correctIndex)
        {
            this.prompt = prompt;
            this.answers = answers;
            this.correctIndex = correctIndex;
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
    [SerializeField] private TMP_Text entryTipText;
    [SerializeField] private TMP_Text entryHintText;

    [Header("Game Panel")]
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private TMP_Text gameTitleText;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text streakText;
    [SerializeField] private TMP_Text poolText;
    [SerializeField] private TMP_Text rewardText;

    [Header("Game Answer Texts")]
    [SerializeField] private TMP_Text answer1Text;
    [SerializeField] private TMP_Text answer2Text;
    [SerializeField] private TMP_Text answer3Text;
    [SerializeField] private TMP_Text answer4Text;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultBodyText;
    [SerializeField] private TMP_Text resultHintText;

    [Header("Config")]
    [SerializeField] private string activityId = "";
    [SerializeField] private int scoreReward = 5;
    [SerializeField] private bool oneTimeOnly = true;

    private readonly List<QuestionData> questionPool = new List<QuestionData>();
    private readonly List<int> randomizedQuestionOrder = new List<int>();

    private bool playerInRange;
    private bool isPlaying;
    private bool isProcessingAnswer;
    private bool hasCompletedReward;
    private int currentQuestionOrderIndex;
    private int currentStreak;

    private void Awake()
    {
        BuildQuestionPool();
    }

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(activityId))
            activityId = gameObject.name.ToLower().Replace(" ", "_");

        if (entryPanel != null)
            entryPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange || ResumeLogic.Instance == null)
            return;

        if (ResumeLogic.Instance.IsGameplayLocked && !IsAnyMinigameOpen)
            return;

        if (IsAnyMinigameOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAllPanels();
            return;
        }

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
            StartMinigameRun();
            return;
        }

        if (!isPlaying || isProcessingAnswer)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            SubmitAnswer(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SubmitAnswer(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SubmitAnswer(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            SubmitAnswer(3);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;
        hasCompletedReward = HasAlreadyCompletedReward();

        if (questionMark != null)
            questionMark.SetActive(false);

        OpenEntryPanel();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;
        isPlaying = false;
        isProcessingAnswer = false;
        IsAnyMinigameOpen = false;
        IsGameplayInputBlocked = false;

        if (entryPanel != null)
            entryPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        IsGameplayInputBlocked = false;
        if (questionMark != null)
            questionMark.SetActive(true);
    }

    public void SelectAnswer1() => SubmitAnswer(0);
    public void SelectAnswer2() => SubmitAnswer(1);
    public void SelectAnswer3() => SubmitAnswer(2);
    public void SelectAnswer4() => SubmitAnswer(3);

    public void StartFromButton()
    {
        if (playerInRange && !isPlaying)
            StartMinigameRun();
    }

    public void CloseResultPanel()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        isPlaying = false;
        isProcessingAnswer = false;
        IsAnyMinigameOpen = false;
        IsGameplayInputBlocked = false;

        if (questionMark != null)
            questionMark.SetActive(true);
    }

    private void OpenEntryPanel()
    {
        IsAnyMinigameOpen = true;
        IsGameplayInputBlocked = false;
        isPlaying = false;
        isProcessingAnswer = false;

        if (entryPanel != null)
        {
            entryPanel.SetActive(true);
            entryPanel.transform.SetAsLastSibling();
        }

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (entryTitleText != null)
            entryTitleText.text = "Certified Behavioral Survivor";

        if (entryDescriptionText != null)
        {
            entryDescriptionText.text =
                "Survive 15 awkward workplace questions.\n" +
                "Get 3 correct answers in a row to win.\n" +
                "Use up the pool without a streak and you fail.";
        }

        if (entryTipText != null)
        {
            entryTipText.text = hasCompletedReward
                ? "Replay is allowed, but the reward is already claimed."
                : "Wrong answers reset your streak. Reward: Resume +" + scoreReward + ".";
        }

        if (entryHintText != null)
            entryHintText.text = "Press ENTER to play";
    }

    private void StartMinigameRun()
    {
        if (questionPool.Count == 0)
            return;

        if (entryPanel != null)
            entryPanel.SetActive(false);

        if (gamePanel != null)
        {
            gamePanel.SetActive(true);
            gamePanel.transform.SetAsLastSibling();
        }

        IsGameplayInputBlocked = true;
        isPlaying = true;
        isProcessingAnswer = false;
        currentStreak = 0;
        currentQuestionOrderIndex = 0;

        randomizedQuestionOrder.Clear();
        for (int i = 0; i < questionPool.Count; i++)
            randomizedQuestionOrder.Add(i);

        ShuffleQuestionOrder();
        ShowCurrentQuestion();
    }

    private void SubmitAnswer(int answerIndex)
    {
        if (!isPlaying || isProcessingAnswer || currentQuestionOrderIndex >= randomizedQuestionOrder.Count)
            return;

        isProcessingAnswer = true;

        QuestionData question = questionPool[randomizedQuestionOrder[currentQuestionOrderIndex]];
        bool isCorrect = answerIndex == question.correctIndex;

        currentQuestionOrderIndex++;
        currentStreak = isCorrect ? currentStreak + 1 : 0;

        if (currentStreak >= 3)
        {
            StartCoroutine(FinishSuccess());
            return;
        }

        if (currentQuestionOrderIndex >= randomizedQuestionOrder.Count)
        {
            ShowFailurePanel();
            return;
        }

        ShowCurrentQuestion();
        if (hintText != null)
            hintText.text = isCorrect ? "Correct. Keep the streak alive." : "Wrong. Streak reset to 0.";

        isProcessingAnswer = false;
    }

    private IEnumerator FinishSuccess()
    {
        bool awardedScoreThisWin = false;
        string errorMessage = null;

        if (!hasCompletedReward)
        {
            bool updated = false;
            bool alreadyCompleted = false;

            yield return ResumeLogic.Instance.CompleteActivity(
                activityId,
                "certificate",
                scoreReward,
                oneTimeOnly,
                (success, wasAlreadyCompleted, error) =>
                {
                    updated = success;
                    alreadyCompleted = wasAlreadyCompleted;
                    errorMessage = error;
                });

            if (!updated && !alreadyCompleted)
            {
                ShowResultPanel(
                    "Certificate Error",
                    (string.IsNullOrEmpty(errorMessage) ? "Something broke while saving your certificate reward. Try again in a moment." : errorMessage) + "\n\nPress ENTER to exit.",
                    "Save failed.");
                isPlaying = false;
                isProcessingAnswer = false;
                yield break;
            }

            hasCompletedReward = true;
            awardedScoreThisWin = !alreadyCompleted;
        }

        string body = awardedScoreThisWin
            ? "Congratulations. You somehow survived a full set of awkward workplace behavior questions without collapsing under corporate pressure.\n\nThank you for sacrificing your time and dignity for the Certified Behavioral Survivor test.\n\nResume +" + scoreReward + " has been added.\n\nPress ENTER to exit."
            : "Congratulations, you cleared the Certified Behavioral Survivor test again.\n\nVery impressive, but you already claimed this reward before, so no extra resume score this time.\n\nPress ENTER to exit.";

        ShowResultPanel("Certified Behavioral Survivor", body, "Completed. Score already added to resume.");
        isPlaying = false;
        isProcessingAnswer = false;
    }

    private void ShowFailurePanel()
    {
        ShowResultPanel(
            "Social Survival Not Found",
            "You ran out of questions before reaching a 3-answer streak.\n\nThat was a worrying display of workplace survival instinct. You may be a little under-leveled in professional small talk, conflict handling, or basic corporate self-preservation.\n\nTake the test again and try not to emotionally fumble the office politics this time.\n\nPress ENTER to exit.",
            "Failed. You can take the test again.");
        isPlaying = false;
        isProcessingAnswer = false;
    }

    private void ShowCurrentQuestion()
    {
        QuestionData question = questionPool[randomizedQuestionOrder[currentQuestionOrderIndex]];

        if (gameTitleText != null)
            gameTitleText.text = "Certified Behavioral Survivor";

        if (questionText != null)
            questionText.text = question.prompt;

        if (streakText != null)
            streakText.text = "Streak: " + currentStreak + " / 3";

        if (poolText != null)
            poolText.text = "Pool Left: " + (questionPool.Count - currentQuestionOrderIndex);

        if (rewardText != null)
            rewardText.text = hasCompletedReward ? "Reward already claimed." : "Reward: Resume +" + scoreReward;

        SetAnswerTexts(
            "1. " + question.answers[0],
            "2. " + question.answers[1],
            "3. " + question.answers[2],
            "4. " + question.answers[3]);

        if (hintText != null)
            hintText.text = "Answer with keys 1, 2, 3, or 4.";
    }

    private void ShowResultPanel(string title, string body, string hint)
    {
        IsAnyMinigameOpen = true;
        IsGameplayInputBlocked = true;

        if (entryPanel != null)
            entryPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();
        }

        if (resultTitleText != null)
            resultTitleText.text = title;

        if (resultBodyText != null)
            resultBodyText.text = body;

        if (resultHintText != null)
            resultHintText.text = hint;
    }

    private void CloseAllPanels()
    {
        isPlaying = false;
        isProcessingAnswer = false;
        IsAnyMinigameOpen = false;
        IsGameplayInputBlocked = false;

        if (entryPanel != null)
            entryPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (questionMark != null)
            questionMark.SetActive(true);
    }

    private void SetAnswerTexts(string answer1, string answer2, string answer3, string answer4)
    {
        if (answer1Text != null) answer1Text.text = answer1;
        if (answer2Text != null) answer2Text.text = answer2;
        if (answer3Text != null) answer3Text.text = answer3;
        if (answer4Text != null) answer4Text.text = answer4;
    }

    private bool HasAlreadyCompletedReward()
    {
        if (ResumeLogic.Instance == null || ResumeLogic.Instance.CurrentPlayer == null || ResumeLogic.Instance.CurrentPlayer.completed_activity_ids == null)
            return false;

        string normalizedId = activityId.ToLower().Replace(" ", "_");
        for (int i = 0; i < ResumeLogic.Instance.CurrentPlayer.completed_activity_ids.Length; i++)
        {
            if (ResumeLogic.Instance.CurrentPlayer.completed_activity_ids[i] == normalizedId)
                return true;
        }

        return false;
    }

    private void ShuffleQuestionOrder()
    {
        for (int i = randomizedQuestionOrder.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (randomizedQuestionOrder[i], randomizedQuestionOrder[j]) = (randomizedQuestionOrder[j], randomizedQuestionOrder[i]);
        }
    }

    private void BuildQuestionPool()
    {
        questionPool.Clear();

        questionPool.Add(new QuestionData(
            "A teammate keeps giving you their unfinished work right before the deadline. What do you do?",
            new[]
            {
                "Do all of it silently forever.",
                "Help if needed, but raise the pattern early and set clearer boundaries.",
                "Publicly embarrass them in the team chat.",
                "Refuse immediately and let the whole project burn."
            },
            1));

        questionPool.Add(new QuestionData(
            "You are asked to fix a bug caused by another developer, and they are blaming you for it. What do you do?",
            new[]
            {
                "Start a blame war and bring screenshots.",
                "Focus on resolving it first, then discuss responsibility professionally.",
                "Refuse to touch it because it is not your bug.",
                "Tell the manager the other developer is incompetent."
            },
            1));

        questionPool.Add(new QuestionData(
            "Your manager says the company is struggling and asks whether you would accept a salary cut. What is the best response?",
            new[]
            {
                "Accept immediately without asking anything.",
                "Ask for context, scope, and duration before deciding professionally.",
                "Rage quit on the spot.",
                "Say yes, then secretly do no work."
            },
            1));

        questionPool.Add(new QuestionData(
            "Two coworkers clearly dislike each other, and the tension is slowing the team down. What do you do?",
            new[]
            {
                "Pick a side for entertainment.",
                "Stay professional and involve a manager if it affects delivery.",
                "Spread the drama to the rest of the office.",
                "Ignore it forever even if deadlines slip."
            },
            1));

        questionPool.Add(new QuestionData(
            "You hear a coworker speaking badly about another teammate behind their back. What is the best response?",
            new[]
            {
                "Join in so you do not look awkward.",
                "Do not participate and steer the conversation away from gossip.",
                "Repeat it to more people.",
                "Confront everyone loudly in public."
            },
            1));

        questionPool.Add(new QuestionData(
            "A teammate keeps saying, 'I'll do it later,' disappears until the deadline, then returns asking for a status update. What do you do?",
            new[]
            {
                "Do all their work and thank them for the leadership.",
                "Clarify responsibilities, document ownership, and raise it early if it continues.",
                "Remove them from every group chat without warning.",
                "Wait until demo day and expose them dramatically."
            },
            1));

        questionPool.Add(new QuestionData(
            "Your manager gives you feedback you disagree with. What do you do?",
            new[]
            {
                "Reject it immediately because you know better.",
                "Ask clarifying questions and respond respectfully with evidence if needed.",
                "Argue emotionally until the meeting ends.",
                "Complain to coworkers afterward and do nothing."
            },
            1));

        questionPool.Add(new QuestionData(
            "You are overloaded, but your teammate asks you to take on one more task because they are 'too busy.' What do you do?",
            new[]
            {
                "Accept everything and burn out quietly.",
                "Be honest about your capacity and discuss priorities before agreeing.",
                "Say yes, then ignore the task.",
                "Tell them their problems are not your concern."
            },
            1));

        questionPool.Add(new QuestionData(
            "You caused a bug in production, but nobody has noticed yet. What do you do?",
            new[]
            {
                "Stay quiet and hope another issue distracts everyone.",
                "Report it quickly, explain the impact, and help fix it before it gets worse.",
                "Rewrite git history and pretend reality is optional.",
                "Blame the last person who touched the file."
            },
            1));

        questionPool.Add(new QuestionData(
            "A senior developer dismisses your idea in a rude way during a meeting. What is the best response?",
            new[]
            {
                "Attack them back immediately.",
                "Stay calm and follow up professionally after the meeting if needed.",
                "Never speak in meetings again.",
                "Try to undermine them later."
            },
            1));

        questionPool.Add(new QuestionData(
            "A coworker takes credit for work you mostly did. What do you do?",
            new[]
            {
                "Correct them in every public channel immediately.",
                "Address it directly and make your contributions visible professionally.",
                "Accept it because conflict is scary.",
                "Wait until review season and explode."
            },
            1));

        questionPool.Add(new QuestionData(
            "A new teammate keeps asking questions already answered in the documentation. What is the best approach?",
            new[]
            {
                "Send 'read the docs' with no context every time.",
                "Help briefly, point them to the right docs, and encourage them to try first.",
                "Stop replying until they evolve.",
                "Tell everyone they are not going to make it."
            },
            1));

        questionPool.Add(new QuestionData(
            "A teammate messages you after work hours expecting an immediate reply for non-urgent issues. What do you do?",
            new[]
            {
                "Answer every time and resent them privately.",
                "Set respectful boundaries and respond during normal hours unless it is urgent.",
                "Block them without explanation.",
                "Start doing the same thing back to them."
            },
            1));

        questionPool.Add(new QuestionData(
            "Your team shipped a feature, and a serious issue appears in production. Nobody knows who caused it. What is the best response?",
            new[]
            {
                "Find someone to blame fast.",
                "Focus on diagnosing and fixing it first, then review the process without blame.",
                "Stay silent and wait for someone else to handle it.",
                "Delete messages so you are not involved."
            },
            1));

        questionPool.Add(new QuestionData(
            "In an interview, you are asked: 'Tell me about a difficult coworker.' What is the strongest answer style?",
            new[]
            {
                "Roast them in detail.",
                "Describe the challenge professionally, focus on your response, and show what you learned.",
                "Say all coworkers are terrible.",
                "Refuse to answer because it is a trap."
            },
            1));
    }
}
