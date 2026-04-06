using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class ResumeSwipeMinigameInteraction : MonoBehaviour
{
    private const string InstructionPrefix = "<size=120%><b>Instruction:</b></size>\n";

    private struct JobCard
    {
        public string jobTitle;
        public string companyName;
        public string locationPolicy;
        public string salaryRange;
        public string requiredSkills;
        public string responsibilities;
        public string applyProcess;
        public string shortHint;
        public string failExplanation;
        public bool isScam;

        public JobCard(
            string jobTitle,
            string companyName,
            string locationPolicy,
            string salaryRange,
            string requiredSkills,
            string responsibilities,
            string applyProcess,
            string shortHint,
            string failExplanation,
            bool isScam)
        {
            this.jobTitle = jobTitle;
            this.companyName = companyName;
            this.locationPolicy = locationPolicy;
            this.salaryRange = salaryRange;
            this.requiredSkills = requiredSkills;
            this.responsibilities = responsibilities;
            this.applyProcess = applyProcess;
            this.shortHint = shortHint;
            this.failExplanation = failExplanation;
            this.isScam = isScam;
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
    [SerializeField] private Image entryPreviewImage;
    [SerializeField] private Sprite entryPreviewSprite;

    [Header("Entry Copy")]
    [SerializeField] private string entryTitle = "Resume Tailored Swipe";
    [TextArea(2, 8)]
    [SerializeField] private string entryDescription =
        "Recruitment has collapsed into swipe culture. Judge each listing before a scam, a fake fit, or bad instincts judge you first.";
    [TextArea(2, 8)]
    [SerializeField] private string requirementText =
        "Swipe right only on real, suitable jobs and avoid scam cards. One bad right swipe can end the run instantly.";
    [TextArea(2, 8)]
    [SerializeField] private string replayRequirementText =
        "You can replay this minigame, but no additional points will be awarded.";
    [SerializeField] private string enterHint = "Press ENTER to play.";
    [SerializeField] private string replayHint = "Press ENTER to replay.";

    [Header("Game Panel")]
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private RectTransform swipeCardTransform;
    [SerializeField] private TMP_Text gameTitleText;
    [SerializeField] private TMP_Text cardTitleText;
    [SerializeField] private TMP_Text cardBodyText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text poolText;
    [SerializeField] private TMP_Text rewardText;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultBodyText;
    [SerializeField] private TMP_Text resultHintText;

    [Header("Config")]
    [SerializeField] private string activityId = "resume_swipe_game";
    [SerializeField] private string activityType = "resume_activity";
    [SerializeField] private int scoreReward = 8;
    [SerializeField] private int rightSwipesToWin = 5;
    [SerializeField] private bool oneTimeOnly = true;
    [SerializeField] private float swipeDistance = 1200f;
    [SerializeField] private float swipeDuration = 0.18f;
    [SerializeField] private float swipeRotationDegrees = 12f;

    private readonly List<JobCard> cardPool = new List<JobCard>();
    private readonly List<int> randomizedCardOrder = new List<int>();

    private bool playerInRange;
    private bool isPlaying;
    private bool isProcessing;
    private bool hasCompletedReward;
    private int currentCardOrderIndex;
    private int confirmedRealRightSwipes;
    private Vector2 cardStartAnchoredPosition;
    private Quaternion cardStartRotation;

    private void Awake()
    {
        IsAnyMinigameOpen = false;
        IsGameplayInputBlocked = false;
        BuildCardPool();
    }

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(activityId))
            activityId = "resume_swipe_game";

        if (entryPanel != null) entryPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        if (swipeCardTransform != null)
        {
            cardStartAnchoredPosition = swipeCardTransform.anchoredPosition;
            cardStartRotation = swipeCardTransform.localRotation;
        }
    }

    private void Update()
    {
        if (!playerInRange || ResumeLogic.Instance == null)
            return;

        if (CertificateMinigameInteraction.IsAnyMinigameOpen ||
            ResumeActivityInteraction.IsGameplayInputBlocked ||
            ResumeTailoredMinigameInteraction.IsAnyMinigameOpen ||
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

        if (!isPlaying || isProcessing)
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            ProcessSwipe(false);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            ProcessSwipe(true);
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

    public void SwipeLeftButton() => ProcessSwipe(false);
    public void SwipeRightButton() => ProcessSwipe(true);

    public void CloseResultPanel()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        isPlaying = false;
        isProcessing = false;
        IsAnyMinigameOpen = false;
        IsGameplayInputBlocked = false;

        ResetCardVisualImmediate();
        Input.ResetInputAxes();
        PlayerController.Instance?.ForceStopMovement();

        if (questionMark != null)
            questionMark.SetActive(true);
    }

    private void OpenEntryPanel()
    {
        IsAnyMinigameOpen = true;
        IsGameplayInputBlocked = false;
        isPlaying = false;
        isProcessing = false;

        if (entryPanel != null)
        {
            entryPanel.SetActive(true);
            entryPanel.transform.SetAsLastSibling();
        }

        if (gamePanel != null) gamePanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

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

        if (entryPreviewImage != null)
        {
            entryPreviewImage.sprite = entryPreviewSprite;
            entryPreviewImage.enabled = entryPreviewSprite != null;
        }
    }

    private void StartMinigameRun()
    {
        if (cardPool.Count == 0)
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
        isProcessing = false;
        currentCardOrderIndex = 0;
        confirmedRealRightSwipes = 0;

        randomizedCardOrder.Clear();
        for (int i = 0; i < cardPool.Count; i++)
            randomizedCardOrder.Add(i);

        ShuffleCardOrder();
        ResetCardVisualImmediate();
        ShowCurrentCard();
    }

    private void ProcessSwipe(bool swipeRight)
    {
        if (!isPlaying || isProcessing || currentCardOrderIndex >= randomizedCardOrder.Count)
            return;

        StartCoroutine(AnimateAndResolveSwipe(swipeRight));
    }

    private IEnumerator AnimateAndResolveSwipe(bool swipeRight)
    {
        isProcessing = true;
        yield return AnimateCardOut(swipeRight);

        JobCard card = cardPool[randomizedCardOrder[currentCardOrderIndex]];
        currentCardOrderIndex++;

        if (swipeRight && card.isScam)
        {
            ShowFailurePanel(card);
            yield break;
        }

        if (swipeRight && !card.isScam)
            confirmedRealRightSwipes++;

        if (confirmedRealRightSwipes >= rightSwipesToWin)
        {
            StartCoroutine(FinishSuccess());
            yield break;
        }

        if (currentCardOrderIndex >= randomizedCardOrder.Count)
        {
            ShowPoolExhaustedPanel();
            yield break;
        }

        ResetCardVisualImmediate();
        ShowCurrentCard();
        if (hintText != null)
            hintText.text = swipeRight ? "RIGHT recorded." : "LEFT recorded.";

        isProcessing = false;
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
                    "Swipe Save Error",
                    (string.IsNullOrEmpty(errorMessage) ? "Failed to save reward. Try again." : errorMessage) + "\n\nPress ENTER to exit.",
                    "Save failed.");
                isPlaying = false;
                isProcessing = false;
                yield break;
            }

            hasCompletedReward = true;
            awardedScoreThisWin = !alreadyCompleted;
        }

        string body = awardedScoreThisWin
            ? "You filtered fake listings and locked in solid job fits.\n\nResume +" + scoreReward + " added.\n\nPress ENTER to exit."
            : "You cleared Resume Tailored Swipe again.\n\nNo extra score this run because reward was already claimed.\n\nPress ENTER to exit.";

        ShowResultPanel("Swipe Round Complete", body, "Completed. Score already added to resume.");
        isPlaying = false;
        isProcessing = false;
    }

    private void ShowFailurePanel(JobCard failedCard)
    {
        string reason = "You swiped RIGHT on a scam listing.\n\n" + failedCard.failExplanation;
        ShowResultPanel(
            "Offer Not Legit",
            reason + "\n\nRetry and protect your resume from fake jobs.\n\nPress ENTER to exit.",
            "Failed. You can retry.");
        isPlaying = false;
        isProcessing = false;
    }

    private void ShowPoolExhaustedPanel()
    {
        ShowResultPanel(
            "Pool Exhausted",
            "You ran out of cards before reaching " + rightSwipesToWin + " correct RIGHT swipes.\n\nPress ENTER to exit.",
            "Failed. You can retry.");
        isPlaying = false;
        isProcessing = false;
    }

    private void ShowCurrentCard()
    {
        JobCard card = cardPool[randomizedCardOrder[currentCardOrderIndex]];

        if (gameTitleText != null)
            gameTitleText.text = "Resume Tailored Swipe";

        if (cardTitleText != null)
            cardTitleText.text = string.Empty;

        if (cardBodyText != null)
        {
            cardBodyText.text =
                "Job Title: " + card.jobTitle + "\n" +
                "Company Name: " + card.companyName + "\n" +
                "Location / Remote policy: " + card.locationPolicy + "\n" +
                "Salary range: " + card.salaryRange + "\n" +
                "Required skills: " + card.requiredSkills + "\n" +
                "Responsibilities: " + card.responsibilities + "\n" +
                "Apply process: " + card.applyProcess;
        }

        if (progressText != null)
            progressText.text = "Correct Right: " + confirmedRealRightSwipes + " / " + rightSwipesToWin;

        if (poolText != null)
            poolText.text = "Pool Left: " + (cardPool.Count - currentCardOrderIndex);

        if (rewardText != null)
            rewardText.text = hasCompletedReward ? "Reward already claimed." : "Reward: Resume +" + scoreReward;

        if (hintText != null)
            hintText.text = "A/Left = Skip    D/Right = Accept";
    }

    private IEnumerator AnimateCardOut(bool swipeRight)
    {
        if (swipeCardTransform == null)
            yield break;

        Vector2 fromPos = swipeCardTransform.anchoredPosition;
        Quaternion fromRot = swipeCardTransform.localRotation;

        Vector2 toPos = fromPos + new Vector2((swipeRight ? 1f : -1f) * swipeDistance, 0f);
        Quaternion toRot = Quaternion.Euler(0f, 0f, swipeRight ? -swipeRotationDegrees : swipeRotationDegrees);

        float t = 0f;
        while (t < swipeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / Mathf.Max(0.001f, swipeDuration));
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            swipeCardTransform.anchoredPosition = Vector2.LerpUnclamped(fromPos, toPos, eased);
            swipeCardTransform.localRotation = Quaternion.SlerpUnclamped(fromRot, toRot, eased);
            yield return null;
        }
    }

    private void ResetCardVisualImmediate()
    {
        if (swipeCardTransform == null)
            return;

        swipeCardTransform.anchoredPosition = cardStartAnchoredPosition;
        swipeCardTransform.localRotation = cardStartRotation;
    }

    private void ShowResultPanel(string title, string body, string hint)
    {
        IsAnyMinigameOpen = true;
        IsGameplayInputBlocked = true;

        if (entryPanel != null) entryPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(false);

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();
        }

        if (resultTitleText != null) resultTitleText.text = title;
        if (resultBodyText != null) resultBodyText.text = body;
        if (resultHintText != null) resultHintText.text = hint;
    }

    private void CloseAllPanels()
    {
        isPlaying = false;
        isProcessing = false;
        IsAnyMinigameOpen = false;
        IsGameplayInputBlocked = false;

        if (entryPanel != null) entryPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        ResetCardVisualImmediate();
        Input.ResetInputAxes();
        PlayerController.Instance?.ForceStopMovement();
    }

    private void OnDisable()
    {
        isPlaying = false;
        isProcessing = false;
        IsAnyMinigameOpen = false;
        IsGameplayInputBlocked = false;
        Input.ResetInputAxes();
        PlayerController.Instance?.ForceStopMovement();
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

    private void ShuffleCardOrder()
    {
        for (int i = randomizedCardOrder.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (randomizedCardOrder[i], randomizedCardOrder[j]) = (randomizedCardOrder[j], randomizedCardOrder[i]);
        }
    }

    private void BuildCardPool()
    {
        cardPool.Clear();

        // REAL (10)
        AddCard("Junior Backend Developer", "NorthForge Systems", "Toronto (Hybrid)", "$68k - $74k", "Python, SQL, REST API", "Maintain internal services, ship small features, and prevent accidental production disasters.", "Screening call + technical round + manager chat", "Structured process + realistic salary.", "Legit role with clear stack and normal hiring pipeline.", false);
        AddCard("Frontend Developer Intern", "MintUI Studio", "Vancouver (Onsite)", "$22/hour", "HTML, CSS, JavaScript, React basics", "Support UI implementation and debug real layout issues from designers.", "Portfolio check + short task", "Specific skills + normal intern pay.", "Legit junior internship with concrete responsibilities.", false);
        AddCard("Junior Game Developer", "Pixel Harbor", "Montreal (Hybrid)", "$70k", "Unity, C#, Git", "Implement gameplay systems and fix bugs before release.", "Tech interview + pair programming", "Clear skill match for Unity dev.", "Legit dev job with practical interview flow.", false);
        AddCard("Remote Customer Support Agent", "ClearView Health", "Remote (Canada)", "$21/hour", "Communication, ticket tools, troubleshooting", "Handle support tickets and escalate issues with clear reports.", "Interview + simulation task", "Reasonable support role and process.", "Legit operations role; no fee or suspicious requests.", false);
        AddCard("QA Game Tester", "Arcade Loop", "Montreal (Onsite)", "$20/hour", "Bug reporting, test cases, patience", "Play test builds and report reproducible defects.", "Practical test session", "Normal QA expectations.", "Legit tester position with realistic pay and process.", false);
        AddCard("Product Tester", "StableBuild Labs", "Remote", "$30/hour", "Usability testing, detail focus", "Test flows and submit structured feedback.", "Screening + paid trial", "Paid trial is normal in testing gigs.", "Legit contract testing role.", false);
        AddCard("Warehouse Assistant", "PaperTrail Logistics", "Edmonton (Onsite)", "$19/hour", "Inventory tools, teamwork, basic Excel", "Track inventory movement and assist shipments.", "Onsite interview", "Simple role, clear onsite process.", "Legit physical operations role.", false);
        AddCard("Restaurant Server", "Noodle & Byte", "Calgary (Onsite)", "$16/hour + tips", "Customer service, POS, multitasking", "Serve customers and coordinate orders during rush.", "Shift trial + manager interview", "Hospitality role with realistic pay.", "Legit service-industry role.", false);
        AddCard("AI Prompt Writer", "PromptWorks", "Remote", "$35/hour", "Prompt design, editing, domain context", "Design and iterate prompts for internal tools.", "Writing test + video call", "Clear deliverables and test step.", "Legit AI content role with normal evaluation.", false);
        AddCard("Junior Data Analyst", "BrightMetrics", "Toronto (Hybrid)", "$64k - $70k", "SQL, dashboards, reporting", "Prepare KPI dashboards and help data quality checks.", "Recruiter call + SQL task", "Realistic analyst stack and salary.", "Legit analytics position.", false);

        // SCAM (20)
        AddCard("Senior Software Engineer (Entry Level)", "FastHire 24/7", "Remote", "$180k guaranteed", "Passion, grind, weekend loyalty", "Build global systems from zero with no support.", "No interview, instant offer link", "Red flag: title and level contradict.", "Contradictory role and guaranteed high salary for entry-level.", true);
        AddCard("Crypto Technical Assistant", "Moon Salary DAO", "Remote", "Not listed", "Wallet transfer, blind trust", "Move crypto funds between anonymous wallets.", "Send $200 verification deposit first", "Red flag: deposit before onboarding.", "Any upfront payment request before hiring is scam behavior.", true);
        AddCard("AI Engineer (Training Provided)", "FutureNova", "Remote", "$200k", "No coding needed", "Attend motivational calls; AI does everything else.", "Immediate hire after setup payment", "Red flag: huge pay + no skill needed.", "Unrealistic compensation and pay-to-start scheme.", true);
        AddCard("Remote Data Entry VIP", "Unknown Recruit LLC", "Remote", "$500/day", "Copy-paste stamina", "Work 20 minutes/day and earn absurd pay.", "No interview, start now", "Red flag: trivial work, extreme pay.", "Compensation is far above market for task complexity.", true);
        AddCard("Global Tech Visionary Rockstar", "StealthGrowth999", "Unknown", "Paid in future success", "Ninja coding mindset", "Change the world under total ambiguity.", "No recruiter call required", "Red flag: no real company details.", "Vague company identity and no formal process.", true);
        AddCard("Social Media Like Manager", "Influencer Vault", "Remote", "$400/day", "Scrolling, vibes, engagement", "Like posts all day for easy money.", "Instant hire via DM", "Red flag: DM-only hiring flow.", "Legit companies do not run hiring fully through random DMs.", true);
        AddCard("Entry Level Software Engineer", "NoWebsite Corp", "Remote", "$55k", "15+ years experience", "Own all engineering domains alone.", "No technical interview", "Red flag: impossible requirements.", "Experience requirements are contradictory and hiring flow is fake.", true);
        AddCard("Elite Coding Wizard", "Exposure Labs", "Remote", "Unlimited coffee + exposure", "Everything, everywhere, all at once", "Maintain platform solo after prior mass exits.", "Offer first, details later", "Red flag: unpaid compensation style.", "No salary clarity and manipulative 'exposure' payment.", true);
        AddCard("Secret Shopper", "TrustMe Careers", "Remote", "$300/day", "Shopping, gift cards", "Buy items and return gift cards for verification.", "Chat-only onboarding", "Red flag: gift card loop.", "Gift-card reimbursement schemes are common fraud patterns.", true);
        AddCard("Remote Personal Assistant", "QuickContract", "Remote", "$3500/month", "Finance handling, trust", "Receive and reroute payments for unknown executives.", "No interview, banking task first", "Red flag: money transfer role.", "Payment rerouting jobs are often laundering-related scams.", true);
        AddCard("Online Package Receiver", "Inbox Talent Group", "Remote", "$3000/month", "Home storage, shipping labels", "Receive and reship international packages.", "One-message approval", "Red flag: reshipping job.", "Reshipping roles are linked to stolen-goods fraud chains.", true);
        AddCard("Work-From-Home Opportunity", "Undefined Studio", "Unknown", "Not listed", "None listed", "Easy money online with no fixed duties.", "Pay $100 to unlock description", "Red flag: pay to see details.", "Charging a registration fee for job details is scam behavior.", true);
        AddCard("Software Developer", "Payroll Verify Inc", "Remote", "Not listed", "General coding", "Build systems after payroll verification.", "Send banking login before interview", "Red flag: asks for login credentials.", "No legitimate recruiter asks for banking login credentials.", true);
        AddCard("Remote Data Entry", "DataQuick Pro", "Remote", "$45/hour", "Install proprietary software", "Install unknown executable before contract.", "Install first, interview later", "Red flag: unvetted software first.", "Malware-style onboarding flow before any hiring validation.", true);
        AddCard("Global Tech Ambassador", "Yacht Protocol", "Worldwide", "$150k + free yacht", "Posting, travel, charisma", "Promote product with no public demo.", "Auto contract sent in 5 minutes", "Red flag: luxury bait offer.", "Luxury perks and instant contract are typical bait patterns.", true);
        AddCard("Junior Developer", "Company Name Hidden", "Remote", "Not listed", "General software knowledge", "Employer identity hidden until payment.", "Pay setup fee to reveal employer", "Red flag: hidden employer + fee.", "Legit hiring discloses company and never requires reveal fees.", true);
        AddCard("Confidential Analyst", "DarkSignal Group", "Remote", "Not listed", "Confidentiality compliance", "Analyze unknown data before role disclosure.", "NDA processing fee required", "Red flag: fee for NDA.", "Legitimate NDAs are signed, not purchased.", true);
        AddCard("Cloud Engineer", "SkyRoot Verify", "Remote", "$130k", "Cloud, infra, ownership", "Manage production cloud immediately.", "Share ID selfie + card PIN for payroll validation", "Red flag: ID + PIN request.", "Requesting card PIN or sensitive identity combo is fraud.", true);
        AddCard("Data Scientist", "PromptKingdom", "Remote", "Not listed", "None mandatory", "Unlock interview by loyalty payment.", "Pay to receive coding test", "Red flag: pay to test.", "Charging candidates for coding tests is not legitimate hiring.", true);
        AddCard("Analyst Trainee", "FastCash Talent", "Remote", "Not listed", "Beginner friendly", "Guaranteed placement in 24h.", "Bring cash to interview to secure laptop", "Red flag: cash at interview.", "Cash requirement at interview is direct scam behavior.", true);
    }

    private void AddCard(
        string jobTitle,
        string companyName,
        string locationPolicy,
        string salaryRange,
        string requiredSkills,
        string responsibilities,
        string applyProcess,
        string shortHint,
        string failExplanation,
        bool isScam)
    {
        cardPool.Add(new JobCard(
            jobTitle,
            companyName,
            locationPolicy,
            salaryRange,
            requiredSkills,
            responsibilities,
            applyProcess,
            shortHint,
            failExplanation,
            isScam));
    }
}
