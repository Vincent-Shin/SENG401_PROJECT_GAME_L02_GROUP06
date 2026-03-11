using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class ResumeTailoredMinigameInteraction : MonoBehaviour
{
    private const string InstructionPrefix = "<size=120%><b>Instruction:</b></size>\n";

    private struct QuestionData
    {
        public string prompt;
        public string[] answers;
        public int correctIndex;
        public string explanation;

        public QuestionData(string prompt, string[] answers, int correctIndex, string explanation)
        {
            this.prompt = prompt;
            this.answers = answers;
            this.correctIndex = correctIndex;
            this.explanation = explanation;
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
    [SerializeField] private TMP_Text entryTipText;
    [SerializeField] private TMP_Text entryHintText;

    [Header("Entry Copy")]
    [SerializeField] private string entryTitle = "Resume Tailored Challenge";
    [TextArea(2, 8)]
    [SerializeField] private string entryDescription =
        "This is where job descriptions pretend to be specific and your resume pretends to be versatile. Tailor wisely.";
    [TextArea(2, 8)]
    [SerializeField] private string requirementText =
        "Match the best skill-to-job fit, keep the streak alive, and avoid giving HR a reason to ghost you.";
    [TextArea(2, 8)]
    [SerializeField] private string replayRequirementText =
        "You can replay this minigame, but no additional points will be awarded.";
    [SerializeField] private string enterHint = "Press ENTER to play.";
    [SerializeField] private string replayHint = "Press ENTER to replay.";

    [Header("Game Panel")]
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private TMP_Text gameTitleText;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text streakText;
    [SerializeField] private TMP_Text poolText;
    [SerializeField] private TMP_Text rewardText;

    [Header("Answer Texts")]
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
    [SerializeField] private string activityId = "resume_tailored_game";
    [SerializeField] private string activityType = "resume_activity";
    [SerializeField] private int scoreReward = 5;
    [SerializeField] private int streakToWin = 5;
    [SerializeField] private bool oneTimeOnly = true;

    private readonly List<QuestionData> questionPool = new List<QuestionData>();
    private readonly List<int> randomizedQuestionOrder = new List<int>();

    private bool playerInRange;
    private bool isPlaying;
    private bool isProcessingAnswer;
    private bool hasCompletedReward;
    private int currentQuestionOrderIndex;
    private int currentStreak;
    private readonly int[] answerDisplayOrder = { 0, 1, 2, 3 };
    private int displayedCorrectIndex;

    private void Awake()
    {
        IsAnyMinigameOpen = false;
        IsGameplayInputBlocked = false;
        BuildQuestionPool();
    }

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(activityId))
            activityId = "resume_tailored_game";

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

        if (CertificateMinigameInteraction.IsAnyMinigameOpen ||
            ResumeSwipeMinigameInteraction.IsAnyMinigameOpen ||
            ProjectPipelineChaseMinigameInteraction.IsAnyMinigameOpen)
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
        CloseAllPanels();
        if (questionMark != null)
            questionMark.SetActive(true);
    }

    public void SelectAnswer1() => SubmitAnswer(0);
    public void SelectAnswer2() => SubmitAnswer(1);
    public void SelectAnswer3() => SubmitAnswer(2);
    public void SelectAnswer4() => SubmitAnswer(3);

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
            entryTitleText.text = entryTitle;

        if (entryDescriptionText != null)
            entryDescriptionText.text = entryDescription;

        if (entryTipText != null)
        {
            entryTipText.text = hasCompletedReward
                ? InstructionPrefix + replayRequirementText
                : InstructionPrefix + requirementText + "\nFirst clear bonus: Resume +" + Mathf.Max(0, scoreReward) + ".";
        }

        if (entryHintText != null)
            entryHintText.text = hasCompletedReward ? replayHint : enterHint;
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

        bool isCorrect = answerIndex == displayedCorrectIndex;

        currentQuestionOrderIndex++;
        currentStreak = isCorrect ? currentStreak + 1 : 0;

        if (currentStreak >= streakToWin)
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
            hintText.text = "Pick best fit with keys 1, 2, 3, or 4.";

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
                activityType,
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
                    "Resume Tailored Error",
                    (string.IsNullOrEmpty(errorMessage) ? "Failed to save reward. Try again." : errorMessage) + "\n\nPress ENTER to exit.",
                    "Save failed.");
                isPlaying = false;
                isProcessingAnswer = false;
                yield break;
            }

            hasCompletedReward = true;
            awardedScoreThisWin = !alreadyCompleted;
        }

        string body = awardedScoreThisWin
            ? "Clean targeting. You tailored your resume like a professional, not like someone mass-spamming Apply All.\n\nRecruiters did not laugh. Resume +" + scoreReward + " added.\n\nPress ENTER to exit."
            : "You passed Resume Tailored again.\n\nNice flex, but this reward was already claimed earlier, so no extra score this run.\n\nPress ENTER to exit.";

        ShowResultPanel("Resume Tailored Complete", body, "Completed. Score already added to resume.");
        isPlaying = false;
        isProcessingAnswer = false;
    }

    private void ShowFailurePanel()
    {
        ShowResultPanel(
            "Application Spray Detected",
            "You burned through the job pool without building a " + streakToWin + "-answer streak.\n\nCurrent strategy: vibes + random clicking.\nSuggested patch: read skills first, then choose role.\n\nPress ENTER to exit.",
            "Failed. You can retry.");
        isPlaying = false;
        isProcessingAnswer = false;
    }

    private void ShowCurrentQuestion()
    {
        QuestionData question = questionPool[randomizedQuestionOrder[currentQuestionOrderIndex]];
        ShuffleAnswerOrder();
        displayedCorrectIndex = GetDisplayedCorrectIndex(question.correctIndex);

        if (gameTitleText != null)
            gameTitleText.text = "Resume Tailored Challenge";
        if (questionText != null)
            questionText.text = question.prompt;
        if (streakText != null)
            streakText.text = "Streak: " + currentStreak + " / " + streakToWin;
        if (poolText != null)
            poolText.text = "Pool Left: " + (questionPool.Count - currentQuestionOrderIndex);
        if (rewardText != null)
            rewardText.text = hasCompletedReward ? "Reward already claimed." : "Reward: Resume +" + scoreReward;

        SetAnswerTexts(
            "1. " + question.answers[answerDisplayOrder[0]],
            "2. " + question.answers[answerDisplayOrder[1]],
            "3. " + question.answers[answerDisplayOrder[2]],
            "4. " + question.answers[answerDisplayOrder[3]]);

        if (hintText != null)
            hintText.text = "Pick best fit with keys 1, 2, 3, or 4.";
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

    private void ShuffleAnswerOrder()
    {
        for (int i = answerDisplayOrder.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (answerDisplayOrder[i], answerDisplayOrder[j]) = (answerDisplayOrder[j], answerDisplayOrder[i]);
        }
    }

    private int GetDisplayedCorrectIndex(int originalCorrectIndex)
    {
        for (int i = 0; i < answerDisplayOrder.Length; i++)
        {
            if (answerDisplayOrder[i] == originalCorrectIndex)
                return i;
        }

        return 0;
    }

    private void BuildQuestionPool()
    {
        questionPool.Clear();

        AddQuestion(
            "Resume skills: Python, SQL, Airflow, ETL",
            "Data Engineer - Python SQL ETL - posted today",
            "Graphic Designer - Figma Illustrator - posted today",
            "HR Coordinator - ATS onboarding - posted today",
            0,
            "Data engineering aligns directly with ETL + Python + SQL.");

        AddQuestion(
            "Resume skills: React, TypeScript, REST API, Jest",
            "Frontend Developer - React TypeScript - posted today",
            "Network Technician - Cisco routing - posted today",
            "Payroll Specialist - Excel compliance - posted today",
            0,
            "Frontend stack fits the React role best.");

        AddQuestion(
            "Resume skills: Linux, Docker, Kubernetes, CI/CD",
            "DevOps Engineer - K8s pipelines - posted today",
            "UX Writer - content design - posted today",
            "Sales Associate - CRM - posted today",
            0,
            "Container + orchestration + CI/CD are core DevOps skills.");

        AddQuestion(
            "Resume skills: Figma, User Research, Wireframing, Prototyping",
            "Backend Developer - Java Spring - posted today",
            "Product Designer - UX research Figma - posted today",
            "Procurement Clerk - vendor docs - posted today",
            1,
            "UX research/prototyping maps to Product Designer.");

        AddQuestion(
            "Resume skills: Java, Spring Boot, Microservices, PostgreSQL",
            "Backend Engineer - Java Spring - posted today",
            "Motion Designer - After Effects - posted today",
            "Recruiter - sourcing - posted today",
            0,
            "Backend Java/Spring role is the strongest fit.");

        AddQuestion(
            "Resume skills: Python, Pandas, Scikit-learn, A/B testing",
            "ML Analyst - modeling experimentation - posted today",
            "IT Support - printer hardware - posted today",
            "Office Admin - scheduling - posted today",
            0,
            "Modeling + A/B testing aligns with ML analyst work.");

        AddQuestion(
            "Resume skills: SQL, Tableau, Power BI, stakeholder reporting",
            "Business Intelligence Analyst - posted today",
            "Mobile Game Artist - posted today",
            "Front Desk Assistant - posted today",
            0,
            "Dashboards/reporting point to BI Analyst.");

        AddQuestion(
            "Resume skills: C#, Unity, gameplay scripting, Git",
            "Unity Gameplay Programmer - posted today",
            "Data Entry Clerk - posted today",
            "Warehouse Picker - posted today",
            0,
            "Unity/C# skills fit gameplay programming.");

        AddQuestion(
            "Resume skills: Python, Selenium, test automation, API testing",
            "QA Automation Engineer - posted today",
            "Social Media Intern - posted today",
            "Event Host - posted today",
            0,
            "Automation + API test stack is QA Automation.");

        AddQuestion(
            "Resume skills: Incident response, SIEM, threat hunting, SOC workflow",
            "SOC Analyst - posted today",
            "Brand Illustrator - posted today",
            "Receptionist - posted today",
            0,
            "Security operations profile matches SOC Analyst.");

        AddQuestion(
            "Resume skills: SQL, dbt, Snowflake, data modeling",
            "Analytics Engineer - posted today",
            "Android UI Animator - posted today",
            "Talent Sourcer - posted today",
            0,
            "dbt + modeling is analytics engineering.");

        AddQuestion(
            "Resume skills: Node.js, Express, MongoDB, JWT auth",
            "Full-stack Developer (Node-heavy) - posted today",
            "Legal Assistant - posted today",
            "Barista - posted today",
            0,
            "Node auth/backend stack fits full-stack Node role.");

        AddQuestion(
            "Resume skills: Azure, Terraform, IaC, monitoring",
            "Cloud Infrastructure Engineer - posted today",
            "Photographer - posted today",
            "Recruiter Assistant - posted today",
            0,
            "Cloud IaC skills map to infrastructure engineering.");

        AddQuestion(
            "Resume skills: User interviews, journey mapping, service blueprint",
            "UX Researcher - posted today",
            "Java Compiler Engineer - posted today",
            "Tax Clerk - posted today",
            0,
            "Research/service methods align with UX research.");

        AddQuestion(
            "Resume skills: C++, multithreading, low-latency systems",
            "Systems Engineer (performance critical) - posted today",
            "Digital Marketer - posted today",
            "HR Generalist - posted today",
            0,
            "Low-latency C++ profile is systems/performance.");

        AddQuestion(
            "Resume skills: Agile facilitation, sprint planning, backlog grooming",
            "Scrum Master / Agile Coach - posted today",
            "3D Character Artist - posted today",
            "Data Labeler - posted today",
            0,
            "Agile process ownership fits Scrum roles.");

        AddQuestion(
            "Resume skills: Python, NLP basics, prompt evaluation, annotation QA",
            "AI Data Quality Analyst - posted today",
            "Civil CAD Drafter - posted today",
            "Hotel Concierge - posted today",
            0,
            "NLP/prompt evaluation fits AI data quality.");

        AddQuestion(
            "Resume skills: Kotlin, Android SDK, MVVM, REST",
            "Android Developer - posted today",
            "BI Analyst - posted today",
            "Procurement Officer - posted today",
            0,
            "Android stack clearly fits Android role.");

        AddQuestion(
            "Resume skills: Swift, iOS, SwiftUI, Xcode CI",
            "iOS Developer - posted today",
            "Graphic Artist - posted today",
            "Recruiting Coordinator - posted today",
            0,
            "iOS native profile fits iOS developer.");

        AddQuestion(
            "Resume skills: SQL, financial modeling, dashboarding, KPI storytelling",
            "FP&A Data Analyst - posted today",
            "Unity VFX Artist - posted today",
            "Warehouse Supervisor - posted today",
            0,
            "Finance analytics skills map to FP&A analyst.");

        AddQuestion(
            "Resume skills: UX writing, content strategy, microcopy testing",
            "UX Content Designer - posted today",
            "Network Security Engineer - posted today",
            "Payroll Admin - posted today",
            0,
            "Content strategy/microcopy aligns with UX content.");

        AddQuestion(
            "Resume skills: Python, geospatial analysis, GIS, PostGIS",
            "Geospatial Data Analyst - posted today",
            "Frontend React Intern - posted today",
            "Receptionist - posted today",
            0,
            "GIS/PostGIS is a direct geospatial match.");

        AddQuestion(
            "Resume skills: SQL, churn analysis, cohort analysis, experiment readouts",
            "Product Analyst - posted today",
            "Character Rigging Artist - posted today",
            "Event Coordinator - posted today",
            0,
            "Cohort/churn + experiments match product analytics.");

        AddQuestion(
            "Resume skills: Endpoint hardening, vulnerability scanning, network basics",
            "Junior Cybersecurity Analyst - posted today",
            "Brand Copywriter - posted today",
            "HR Recruiter - posted today",
            0,
            "Security fundamentals fit junior security analyst.");

        AddQuestion(
            "Resume skills: Python, SQL, stakeholder communication, dashboard automation",
            "Operations Analyst (data-heavy) - posted today",
            "Graphic Artist - posted today",
            "Facilities Assistant - posted today",
            0,
            "Ops analytics + automation aligns with operations analyst.");

        AddQuestion(
            "Resume skills: React Native, TypeScript, mobile debugging",
            "Cross-platform Mobile Developer - posted today",
            "Data Engineer (Spark/Scala) - posted today",
            "Compensation Specialist - posted today",
            0,
            "React Native points to cross-platform mobile role.");

        AddQuestion(
            "Resume skills: Kafka, stream processing, event-driven architecture",
            "Streaming Data Engineer - posted today",
            "UX Research Intern - posted today",
            "Office Admin - posted today",
            0,
            "Event streaming skills map to streaming data engineering.");

        AddQuestion(
            "Resume skills: Python, SQL, healthcare data privacy, reporting",
            "Healthcare Data Analyst - posted today",
            "Motion Graphics Designer - posted today",
            "Front Desk Executive - posted today",
            0,
            "Healthcare analytics + compliance awareness fits data analyst.");

        AddQuestion(
            "Resume skills: API docs, Swagger/OpenAPI, developer onboarding docs",
            "Technical Writer (Developer Docs) - posted today",
            "SOC Analyst - posted today",
            "HR Operations - posted today",
            0,
            "API documentation work aligns with developer technical writing.");

        AddQuestion(
            "Resume skills: Python, SQL, basic ML, non-technical communication",
            "Data Analyst (growth team) - posted today",
            "2D Concept Artist - posted today",
            "Store Cashier - posted today",
            0,
            "Balanced analytics + communication fits growth data analyst.");
    }

    private void AddQuestion(
        string skills,
        string jobA,
        string jobB,
        string jobC,
        int correctIndex,
        string explanation)
    {
        questionPool.Add(new QuestionData(
            skills,
            new[]
            {
                jobA,
                jobB,
                jobC,
                "No suitable match - tailor resume first"
            },
            correctIndex,
            explanation));
    }
}
