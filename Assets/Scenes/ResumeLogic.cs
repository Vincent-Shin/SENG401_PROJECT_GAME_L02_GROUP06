using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class PlayerStateDto
{
    public int id;
    public string username;
    public int score;
    public string current_stage;
    public int failed_applications;
    public int apply_attempts_left;
    public int mentor_count;
    public int networking_count;
    public string[] completed_activity_ids;
    public bool completed_project;
    public bool completed_certificate;
    public bool completed_resume_tailored;
    public bool completed_networking;
    public bool completed_work_experience;
    public string[] successful_company_tiers;
    public float resume_multiplier;
    public bool is_game_over;
    public bool is_employed;
    public string employed_company_tier;
}

[System.Serializable]
public class LoadOrCreatePlayerResponse
{
    public bool created;
    public PlayerStateDto player;
    public string error;
}

[System.Serializable]
public class ApplyResponseDto
{
    public int id;
    public int player_id;
    public string company_tier;
    public int company_id;
    public float market_percent;
    public float market_multiplier;
    public int score_snapshot;
    public float resume_multiplier;
    public float interview_probability;
    public int score_delta;
    public int failure_count_after;
    public string message;
    public string status;
}

[System.Serializable]
public class ApplyResponse
{
    public bool application_submitted;
    public string result;
    public string reason;
    public int minimum_score;
    public string error;
    public string[] missing_activities;
    public PlayerStateDto player;
    public ApplyResponseDto application;
}

[System.Serializable]
public class LoadOrCreatePlayerRequest
{
    public string username;
}

[System.Serializable]
public class ApplyRequest
{
    public int player_id;
    public string company_tier;
    public float market_percent;
    public float market_multiplier;
    public string message;
}

[System.Serializable]
public class UpdateStageRequest
{
    public int player_id;
    public string stage;
}

[System.Serializable]
public class AddBonusRequest
{
    public int player_id;
    public int mentor_delta;
    public int networking_delta;
}

[System.Serializable]
public class CompleteActivityRequest
{
    public int player_id;
    public string activity_id;
    public string activity_type;
    public int score_delta;
    public bool one_time_only;
}

[System.Serializable]
public class PlayerStateResponse
{
    public bool updated;
    public bool reset;
    public bool already_completed;
    public PlayerStateDto player;
    public string error;
}

[System.Serializable]
public class DeletePlayerResponse
{
    public bool deleted;
    public string username;
    public string error;
}

public class ResumeLogic : MonoBehaviour
{
    public static ResumeLogic Instance { get; private set; }

    [Header("Backend")]
    [SerializeField] private string backendBaseUrl = "https://seng401-project-game-l02-group06-test.onrender.com";

    [Header("Optional HUD")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text attemptsLeftText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text backendStatusText;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button pauseBackButton;
    [SerializeField] private Button quitGameButton;

    [Header("Auto Bind Scene UI")]
    [SerializeField] private bool autoBindSceneTexts = true;
    [SerializeField] private string scoreTextObjectName = "resume score";
    [SerializeField] private string pauseMenuPanelObjectName = "menu";
    [SerializeField] private string pauseBackButtonObjectName = "Back";
    [SerializeField] private string quitGameButtonObjectName = "QuitGameButton";
    [SerializeField] private string gameOverPanelObjectName = "GameOverPanel";
    [SerializeField] private string gameOverTextObjectName = "GameOverText";
    [SerializeField] private string winPanelObjectName = "WinPanel";
    [SerializeField] private string winTextObjectName = "WinText";

    [Header("Optional Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverText;

    [Header("Optional Win UI")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TMP_Text winText;
    [SerializeField] private bool overrideWinTextFromCode;

    [Header("Debug Testing")]
    [SerializeField] private bool applyDebugStartingScoreOnLoad;
    [SerializeField] private int debugStartingScore = 42;
    [SerializeField] private int backendWarmupRetries = 6;
    [SerializeField] private float backendWarmupDelaySeconds = 2f;
    [SerializeField] private int requestTimeoutSeconds = 30;

    public PlayerStateDto CurrentPlayer { get; private set; }
    public string LastLoadedUsername { get; private set; }

    public bool HasLoadedPlayer => CurrentPlayer != null;
    public bool IsGameplayLocked => isReturningToIntro || (CurrentPlayer != null && (CurrentPlayer.is_game_over || ShouldShowWinPanel()));
    private bool isReturningToIntro;
    private bool winPanelDeferred;
    private bool isPauseMenuOpen;
    private bool backendReady;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (SceneManager.GetActiveScene().name == "IntroScene")
            {
                Instance.enabled = false;
                Destroy(Instance.gameObject);
                Instance = null;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        RegisterMenuButtons();
        UpdateUi();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        SanitizeSceneReferences();

        if (CurrentPlayer == null || isReturningToIntro)
            return;

        bool canQuitToHome = SceneManager.GetActiveScene().name == "MainGameScene" &&
                             !IsGameplayLocked &&
                             !ResumeActivityInteraction.IsAnyMinigameOpen &&
                             !ProjectPipelineChaseMinigameInteraction.IsAnyMinigameOpen &&
                             !CertificateMinigameInteraction.IsGameplayInputBlocked &&
                             !ResumeTailoredMinigameInteraction.IsGameplayInputBlocked &&
                             !ResumeSwipeMinigameInteraction.IsGameplayInputBlocked &&
                             !NetworkingMemoryMinigameInteraction.IsGameplayInputBlocked;

        if (canQuitToHome && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
            return;
        }

        if (CurrentPlayer.is_game_over && IsGameOverPanelVisible() && Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(ReturnToIntroAfterGameOver());
        }
        else if (IsBigTechWin(CurrentPlayer) && IsWinPanelVisible() && Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(ReturnToIntroAfterWin());
        }
    }

    public IEnumerator LoadOrCreatePlayer(string username, System.Action<bool, string> onComplete = null)
    {
        username = (username ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(username))
        {
            onComplete?.Invoke(false, "Username is required.");
            yield break;
        }

        LoadOrCreatePlayerRequest payload = new LoadOrCreatePlayerRequest
        {
            username = username
        };

        bool requestSucceeded = false;
        string requestError = null;
        LoadOrCreatePlayerResponse loadResponse = null;

        yield return SendJsonRequest(
            "/player/load-or-create",
            UnityWebRequest.kHttpVerbPOST,
            JsonUtility.ToJson(payload),
            json =>
            {
                loadResponse = JsonUtility.FromJson<LoadOrCreatePlayerResponse>(json);
                if (loadResponse == null || loadResponse.player == null)
                {
                    SetBackendStatus("Failed to load account.");
                    requestError = "Invalid backend response.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(loadResponse.player.username))
                    loadResponse.player.username = username;

                CurrentPlayer = loadResponse.player;
                LastLoadedUsername = CurrentPlayer.username;
                requestSucceeded = true;
            },
            error =>
            {
                SetBackendStatus(error);
                requestError = error;
            });

        if (!requestSucceeded)
        {
            onComplete?.Invoke(false, requestError);
            yield break;
        }

        yield return HandlePlayerLoaded(loadResponse.created, onComplete);
    }

    public IEnumerator ApplyToCompany(
        string companyTier,
        float marketPercent,
        float marketMultiplier,
        string message,
        System.Action<ApplyResponse> onComplete = null,
        bool deferWinPopup = false)
    {
        if (!HasLoadedPlayer)
        {
            ApplyResponse errorResponse = new ApplyResponse
            {
                error = "No player loaded."
            };
            onComplete?.Invoke(errorResponse);
            yield break;
        }

        ApplyRequest payload = new ApplyRequest
        {
            player_id = CurrentPlayer.id,
            company_tier = companyTier,
            market_percent = marketPercent,
            market_multiplier = marketMultiplier,
            message = message
        };

        string payloadJson = JsonUtility.ToJson(payload);
        string usernameSnapshot = GetRecoveryUsername();
        ApplyResponse finalResponse = null;

        for (int attempt = 0; attempt < 2; attempt++)
        {
            bool shouldRetry = false;

            yield return SendJsonRequest(
                "/apply",
                UnityWebRequest.kHttpVerbPOST,
                payloadJson,
                json =>
                {
                    ApplyResponse response = JsonUtility.FromJson<ApplyResponse>(json);
                    if (response != null && response.player != null)
                    {
                        CurrentPlayer = response.player;
                        if (deferWinPopup && IsBigTechWin(CurrentPlayer))
                            winPanelDeferred = true;
                        else if (!IsBigTechWin(CurrentPlayer))
                            winPanelDeferred = false;
                        UpdateUi();
                    }

                    if (response != null && !string.IsNullOrEmpty(response.error))
                    {
                        SetBackendStatus(response.error);
                        if (attempt == 0 && IsTransientApplyError(response.error))
                            shouldRetry = true;
                    }
                    else if (response != null)
                    {
                        SetBackendStatus("Apply result: " + response.result);
                    }

                    finalResponse = response;
                },
                error =>
                {
                    ApplyResponse errorResponse = JsonUtility.FromJson<ApplyResponse>(error);
                    if (errorResponse != null &&
                        (!string.IsNullOrEmpty(errorResponse.error) ||
                         !string.IsNullOrEmpty(errorResponse.reason) ||
                         errorResponse.player != null))
                    {
                        if (errorResponse.player != null)
                        {
                            CurrentPlayer = errorResponse.player;
                            UpdateUi();
                        }

                        if (!string.IsNullOrEmpty(errorResponse.error))
                        {
                            SetBackendStatus(errorResponse.error);
                            if (attempt == 0 && IsTransientApplyError(errorResponse.error))
                                shouldRetry = true;
                        }

                        finalResponse = errorResponse;
                        return;
                    }

                    SetBackendStatus(error);
                    if (attempt == 0 && IsTransientApplyError(error))
                        shouldRetry = true;
                    finalResponse = new ApplyResponse { error = error };
                });

            if (!shouldRetry)
                break;

            SetBackendStatus("Retrying application...");
            bool reloadCompleted = false;
            bool reloadSucceeded = false;
            string reloadError = null;

            yield return LoadOrCreatePlayer(usernameSnapshot, (success, error) =>
            {
                reloadCompleted = true;
                reloadSucceeded = success;
                reloadError = error;
            });

            if (!reloadCompleted || !reloadSucceeded)
            {
                finalResponse = new ApplyResponse
                {
                    error = string.IsNullOrEmpty(reloadError) ? "Failed to reload account before retry." : reloadError
                };
                break;
            }

            payload.player_id = CurrentPlayer.id;
            payloadJson = JsonUtility.ToJson(payload);
            yield return new WaitForSeconds(0.5f);
        }

        onComplete?.Invoke(finalResponse);
    }

    public IEnumerator UpdateScore(int score, System.Action<bool, string> onComplete = null)
    {
        if (!HasLoadedPlayer)
        {
            onComplete?.Invoke(false, "No player loaded.");
            yield break;
        }

        yield return SendPlayerStateRequest(
            "/player/update-score",
            "{\"player_id\":" + CurrentPlayer.id + ",\"score\":" + Mathf.Max(0, score) + "}",
            onComplete);
    }

    public IEnumerator ResetRun(System.Action<bool, string> onComplete = null)
    {
        if (!HasLoadedPlayer)
        {
            onComplete?.Invoke(false, "No player loaded.");
            yield break;
        }

        yield return SendPlayerStateRequest(
            "/player/reset-run",
            "{\"player_id\":" + CurrentPlayer.id + "}",
            onComplete);
    }

    public IEnumerator DeleteCurrentPlayerAndReturnToIntro(System.Action<bool, string> onComplete = null)
    {
        if (!HasLoadedPlayer)
        {
            onComplete?.Invoke(false, "No player loaded.");
            yield break;
        }

        isReturningToIntro = true;

        int playerId = CurrentPlayer.id;
        string username = CurrentPlayer.username;

        yield return SendJsonRequest(
            "/player/delete",
            UnityWebRequest.kHttpVerbPOST,
            "{\"player_id\":" + playerId + "}",
            json =>
            {
                DeletePlayerResponse response = JsonUtility.FromJson<DeletePlayerResponse>(json);
                if (response == null || !response.deleted)
                {
                    onComplete?.Invoke(false, "Failed to delete player.");
                    return;
                }

                AccountManager.RemoveSavedAccount(username);
                CurrentPlayer = null;

                if (gameOverText != null)
                    gameOverText.text = string.Empty;

                if (gameOverPanel != null)
                    gameOverPanel.SetActive(false);

                if (winPanel != null)
                    winPanel.SetActive(false);

                ClearSceneUiReferences();
                UpdateUi();
                Time.timeScale = 1f;
                SceneManager.LoadScene("IntroScene");
                isReturningToIntro = false;
                onComplete?.Invoke(true, null);
            },
            error =>
            {
                isReturningToIntro = false;
                Time.timeScale = 1f;
                onComplete?.Invoke(false, error);
            });
    }

    public void ClearLoadedPlayerForIntro()
    {
        CurrentPlayer = null;
        LastLoadedUsername = null;
        winPanelDeferred = false;
        isPauseMenuOpen = false;

        if (gameOverText != null)
            gameOverText.text = string.Empty;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);

        if (winText != null && overrideWinTextFromCode)
            winText.text = string.Empty;

        UpdateUi();
    }

    public IEnumerator UpdateStage(string stage, System.Action<bool, string> onComplete = null)
    {
        if (!HasLoadedPlayer)
        {
            onComplete?.Invoke(false, "No player loaded.");
            yield break;
        }

        UpdateStageRequest payload = new UpdateStageRequest
        {
            player_id = CurrentPlayer.id,
            stage = stage
        };

        yield return SendPlayerStateRequest(
            "/player/update-stage",
            JsonUtility.ToJson(payload),
            onComplete);
    }

    public IEnumerator AddBonus(int mentorDelta, int networkingDelta, System.Action<bool, string> onComplete = null)
    {
        if (!HasLoadedPlayer)
        {
            onComplete?.Invoke(false, "No player loaded.");
            yield break;
        }

        AddBonusRequest payload = new AddBonusRequest
        {
            player_id = CurrentPlayer.id,
            mentor_delta = mentorDelta,
            networking_delta = networkingDelta
        };

        yield return SendPlayerStateRequest(
            "/player/add-bonus",
            JsonUtility.ToJson(payload),
            onComplete);
    }

    public IEnumerator CompleteActivity(
        string activityId,
        string activityType,
        int scoreDelta,
        bool oneTimeOnly,
        System.Action<bool, bool, string> onComplete = null)
    {
        if (!HasLoadedPlayer)
        {
            onComplete?.Invoke(false, false, "No player loaded.");
            yield break;
        }

        CompleteActivityRequest payload = new CompleteActivityRequest
        {
            player_id = CurrentPlayer.id,
            activity_id = activityId,
            activity_type = activityType,
            score_delta = scoreDelta,
            one_time_only = oneTimeOnly
        };
        string payloadJson = JsonUtility.ToJson(payload);
        string usernameSnapshot = GetRecoveryUsername();

        bool finalSuccess = false;
        bool finalAlreadyCompleted = false;
        string finalError = null;

        for (int attempt = 0; attempt < 2; attempt++)
        {
            bool shouldRetry = false;
            bool requestFinished = false;

            yield return SendJsonRequest(
                "/player/complete-activity",
                UnityWebRequest.kHttpVerbPOST,
                payloadJson,
                json =>
                {
                    PlayerStateResponse response = JsonUtility.FromJson<PlayerStateResponse>(json);
                    requestFinished = true;

                    if (response == null || response.player == null)
                    {
                        finalError = "Invalid backend response.";
                        return;
                    }

                    CurrentPlayer = response.player;
                    UpdateUi();

                    finalSuccess = response.updated;
                    finalAlreadyCompleted = response.already_completed;
                    finalError = null;
                },
                error =>
                {
                    requestFinished = true;
                    finalSuccess = false;
                    finalAlreadyCompleted = false;
                    finalError = error;
                    if (attempt == 0 && IsTransientApplyError(error))
                        shouldRetry = true;
                });

            if (requestFinished && (finalSuccess || finalAlreadyCompleted || !shouldRetry))
                break;

            SetBackendStatus("Retrying activity save...");
            bool reloadCompleted = false;
            bool reloadSucceeded = false;
            string reloadError = null;

            yield return LoadOrCreatePlayer(usernameSnapshot, (success, error) =>
            {
                reloadCompleted = true;
                reloadSucceeded = success;
                reloadError = error;
            });

            if (!reloadCompleted || !reloadSucceeded)
            {
                finalError = string.IsNullOrEmpty(reloadError) ? "Failed to reload account before retry." : reloadError;
                break;
            }

            payload.player_id = CurrentPlayer.id;
            payloadJson = JsonUtility.ToJson(payload);
            yield return new WaitForSeconds(0.5f);
        }

        onComplete?.Invoke(finalSuccess, finalAlreadyCompleted, finalError);
    }

    private IEnumerator SendJsonRequest(
        string path,
        string method,
        string jsonBody,
        System.Action<string> onSuccess,
        System.Action<string> onError)
    {
        string url = backendBaseUrl.TrimEnd('/') + path;
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody ?? "{}");

        if (path != "/health")
        {
            bool warmupComplete = false;
            bool warmupSucceeded = false;

            yield return EnsureBackendReady(success =>
            {
                warmupComplete = true;
                warmupSucceeded = success;
            });

            if (!warmupComplete || !warmupSucceeded)
            {
                onError?.Invoke("Backend is waking up. Please try again.");
                yield break;
            }
        }

        string lastError = null;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, method))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = Mathf.Max(5, requestTimeoutSeconds);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    backendReady = true;
                    onSuccess?.Invoke(request.downloadHandler.text);
                    yield break;
                }

                string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                lastError = string.IsNullOrEmpty(responseText) ? "Backend request failed: " + request.error : responseText;

                if (attempt >= 2 || !IsTransientApplyError(lastError))
                    break;

                backendReady = false;
                Input.ResetInputAxes();
                PlayerController.Instance?.ForceStopMovement();
                yield return new WaitForSeconds(backendWarmupDelaySeconds);
                bool warmupComplete = false;
                bool warmupSucceeded = false;
                yield return EnsureBackendReady(success =>
                {
                    warmupComplete = true;
                    warmupSucceeded = success;
                });

                if (!warmupComplete || !warmupSucceeded)
                    break;
            }            
        }

        onError?.Invoke(lastError ?? "Backend request failed.");
    }

    private IEnumerator SendPlayerStateRequest(
        string path,
        string jsonBody,
        System.Action<bool, string> onComplete)
    {
        yield return SendJsonRequest(
            path,
            UnityWebRequest.kHttpVerbPOST,
            jsonBody,
            json =>
            {
                PlayerStateResponse response = JsonUtility.FromJson<PlayerStateResponse>(json);
                if (response == null || response.player == null)
                {
                    onComplete?.Invoke(false, "Invalid backend response.");
                    return;
                }

                CurrentPlayer = response.player;
                UpdateUi();
                onComplete?.Invoke(true, null);
            },
            error => onComplete?.Invoke(false, error));
    }

    private bool IsTransientApplyError(string errorText)
    {
        if (string.IsNullOrWhiteSpace(errorText))
            return false;

        string normalized = errorText.ToLowerInvariant();
        return normalized.Contains("player not found") ||
               normalized.Contains("database operation failed") ||
               normalized.Contains("backend request failed") ||
               normalized.Contains("timed out") ||
               normalized.Contains("connection error") ||
               normalized.Contains("network error") ||
               normalized.Contains("temporarily unavailable") ||
               normalized.Contains("backend is waking up");
    }

    private string GetRecoveryUsername()
    {
        if (CurrentPlayer != null && !string.IsNullOrWhiteSpace(CurrentPlayer.username))
            return CurrentPlayer.username.Trim();

        return (LastLoadedUsername ?? string.Empty).Trim();
    }

    private void UpdateUi()
    {
        SanitizeSceneReferences();

        if (autoBindSceneTexts && NeedsSceneUiRebind())
            AutoBindSceneTexts();

        if (CurrentPlayer == null)
        {
            if (scoreText != null)
                scoreText.text = "Resume: --\n[ ] Resume activity\n[ ] Project\n[ ] Certificate\n[ ] Life experience\n[ ] Highest company: none";
            if (attemptsLeftText != null)
                attemptsLeftText.text = "Attempts: --";
            if (playerNameText != null)
                playerNameText.text = "Player: --";
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            if (winPanel != null)
                winPanel.SetActive(false);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 1f;
            return;
        }

        if (scoreText != null)
            scoreText.text = BuildResumeProgressText(CurrentPlayer);

        if (attemptsLeftText != null)
            attemptsLeftText.text = "Attempts: " + CurrentPlayer.apply_attempts_left;

        if (playerNameText != null)
            playerNameText.text = "Player: " + CurrentPlayer.username;

        bool hasBigTechWin = IsBigTechWin(CurrentPlayer);
        bool shouldShowWinPanel = ShouldShowWinPanel();
        bool shouldShowGameOverPanel = CurrentPlayer.is_game_over && !shouldShowWinPanel;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(shouldShowGameOverPanel);
            if (shouldShowGameOverPanel)
                gameOverPanel.transform.SetAsLastSibling();
        }

        if (gameOverText != null)
            gameOverText.text = CurrentPlayer.is_game_over
                ? "You used all 3 failed applications.\n\nPress Enter to start over again."
                : string.Empty;

        if (winPanel != null)
        {
            winPanel.SetActive(shouldShowWinPanel && !shouldShowGameOverPanel);
            if (shouldShowWinPanel)
                winPanel.transform.SetAsLastSibling();
        }

        if (winText != null && overrideWinTextFromCode)
            winText.text = shouldShowWinPanel
                ? "Congratulations! You got into Big Tech.\n\nPress Enter to return to Intro."
                : string.Empty;

        bool showQuitUi = SceneManager.GetActiveScene().name == "MainGameScene" &&
                          !shouldShowGameOverPanel &&
                          !shouldShowWinPanel;

        if (!showQuitUi)
            isPauseMenuOpen = false;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(showQuitUi && isPauseMenuOpen);

        bool shouldFreeze = (shouldShowGameOverPanel && IsGameOverPanelVisible()) ||
                            (shouldShowWinPanel && IsWinPanelVisible()) ||
                            (showQuitUi && isPauseMenuOpen);

        bool inMainGameScene = SceneManager.GetActiveScene().name == "MainGameScene";
        if (inMainGameScene)
        {
            bool menuWantsCursor = showQuitUi && isPauseMenuOpen;
            Cursor.visible = menuWantsCursor;
            Cursor.lockState = menuWantsCursor ? CursorLockMode.None : CursorLockMode.Locked;
        }

        Time.timeScale = shouldFreeze ? 0f : 1f;
    }

    private void SetBackendStatus(string message)
    {
        SanitizeSceneReferences();

        if (backendStatusText != null)
            backendStatusText.text = message;
    }

    private IEnumerator HandlePlayerLoaded(bool created, System.Action<bool, string> onComplete)
    {
        UpdateUi();
        SetBackendStatus(created ? "Account created." : "Account loaded.");

        if (applyDebugStartingScoreOnLoad)
        {
            bool scoreUpdated = false;
            string scoreError = null;

            yield return UpdateScore(debugStartingScore, (success, error) =>
            {
                scoreUpdated = success;
                scoreError = error;
            });

            if (!scoreUpdated)
            {
                onComplete?.Invoke(false, scoreError ?? "Failed to apply debug starting score.");
                yield break;
            }

            SetBackendStatus("Account loaded with debug score " + debugStartingScore + ".");
        }

        onComplete?.Invoke(true, null);
    }

    private IEnumerator ReturnToIntroAfterGameOver()
    {
        if (isReturningToIntro)
            yield break;

        SetBackendStatus("Returning to intro...");
        yield return DeleteCurrentPlayerAndReturnToIntro();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Safety reset to prevent stale pause/input-lock state when switching scenes.
        Time.timeScale = 1f;
        isPauseMenuOpen = false;
        Input.ResetInputAxes();

        SanitizeSceneReferences();

        if (autoBindSceneTexts)
            AutoBindSceneTexts();

        StartCoroutine(RebindSceneUiNextFrame());

        if (scene.name == "MainGameScene")
        {
            // Main gameplay scene must always start unpaused.
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            if (winPanel != null)
                winPanel.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        UpdateUi();
        PlayerController.Instance?.ForceStopMovement();
        StartCoroutine(ResetInputStateAfterSceneLoad());
    }

    private void ClearSceneUiReferences()
    {
        scoreText = null;
        attemptsLeftText = null;
        playerNameText = null;
        backendStatusText = null;
        pauseMenuPanel = null;
        pauseBackButton = null;
        quitGameButton = null;
        gameOverPanel = null;
        gameOverText = null;
        winPanel = null;
        winText = null;
    }

    private void SanitizeSceneReferences()
    {
        if (!scoreText)
            scoreText = null;

        if (!attemptsLeftText)
            attemptsLeftText = null;

        if (!playerNameText)
            playerNameText = null;

        if (!backendStatusText)
            backendStatusText = null;

        if (!pauseMenuPanel)
            pauseMenuPanel = null;

        if (!pauseBackButton)
            pauseBackButton = null;

        if (!quitGameButton)
            quitGameButton = null;

        if (!gameOverPanel)
            gameOverPanel = null;

        if (!gameOverText)
            gameOverText = null;

        if (!winPanel)
            winPanel = null;

        if (!winText)
            winText = null;
    }

    private void AutoBindSceneTexts()
    {
        if (scoreText == null)
            scoreText = FindTextByObjectName(scoreTextObjectName);

        if (pauseMenuPanel == null)
            pauseMenuPanel = FindGameObjectByName(pauseMenuPanelObjectName);

        if (pauseMenuPanel == null)
            pauseMenuPanel = FindGameObjectByName("PauseMenuPanel");

        if (pauseMenuPanel == null)
            pauseMenuPanel = FindGameObjectByName("menu");

        if (pauseBackButton == null)
            pauseBackButton = FindButtonByObjectName(pauseBackButtonObjectName);

        if (quitGameButton == null)
            quitGameButton = FindButtonByObjectName(quitGameButtonObjectName);

        if (gameOverPanel == null)
            gameOverPanel = FindGameObjectByName(gameOverPanelObjectName);

        if (gameOverText == null)
            gameOverText = FindTextByObjectName(gameOverTextObjectName);

        if (winPanel == null)
            winPanel = FindGameObjectByName(winPanelObjectName);

        if (winText == null)
            winText = FindTextByObjectName(winTextObjectName);

        RegisterMenuButtons();
    }

    private bool NeedsSceneUiRebind()
    {
        if (scoreText == null)
            return true;

        string sceneName = SceneManager.GetActiveScene().name;
        bool inMainGameScene = sceneName == "MainGameScene";

        if (inMainGameScene)
        {
            if (pauseMenuPanel == null || pauseBackButton == null || quitGameButton == null)
                return true;
        }

        if (gameOverPanel == null || gameOverText == null || winPanel == null || winText == null)
            return true;

        return false;
    }

    private IEnumerator RebindSceneUiNextFrame()
    {
        yield return null;

        SanitizeSceneReferences();

        if (autoBindSceneTexts)
            AutoBindSceneTexts();

        UpdateUi();
    }

    private IEnumerator ResetInputStateAfterSceneLoad()
    {
        yield return null;
        Input.ResetInputAxes();
        PlayerController.Instance?.ForceStopMovement();
        yield return null;
        Input.ResetInputAxes();
        PlayerController.Instance?.ForceStopMovement();
    }

    public void TogglePauseMenu()
    {
        isPauseMenuOpen = !isPauseMenuOpen;
        ApplyPauseCursorState(isPauseMenuOpen);
        UpdateUi();
    }

    public void ClosePauseMenu()
    {
        isPauseMenuOpen = false;
        ApplyPauseCursorState(false);
        UpdateUi();
    }

    private TMP_Text FindTextByObjectName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform transform = allTransforms[i];
            if (transform == null)
                continue;

            if (transform.hideFlags != HideFlags.None)
                continue;

            if (!transform.gameObject.scene.IsValid())
                continue;

            if (transform.name != objectName)
                continue;

            TMP_Text directText = transform.GetComponent<TMP_Text>();
            if (directText != null)
                return directText;

            TMP_Text childText = transform.GetComponentInChildren<TMP_Text>(true);
            if (childText != null)
                return childText;
        }

        return null;
    }

    private GameObject FindGameObjectByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform transform = allTransforms[i];
            if (transform == null)
                continue;

            if (transform.hideFlags != HideFlags.None)
                continue;

            if (!transform.gameObject.scene.IsValid())
                continue;

            if (transform.name != objectName)
                continue;

            return transform.gameObject;
        }

        return null;
    }

    private Button FindButtonByObjectName(string objectName)
    {
        GameObject buttonObject = FindGameObjectByName(objectName);
        return buttonObject != null ? buttonObject.GetComponent<Button>() : null;
    }

    private void RegisterMenuButtons()
    {
        if (pauseBackButton != null)
        {
            pauseBackButton.onClick.RemoveListener(HandleBackButtonPressed);
            pauseBackButton.onClick.AddListener(HandleBackButtonPressed);
        }

        if (quitGameButton == null)
            return;

        quitGameButton.onClick.RemoveListener(HandleQuitButtonPressed);
        quitGameButton.onClick.AddListener(HandleQuitButtonPressed);
    }

    private void HandleBackButtonPressed()
    {
        if (SceneManager.GetActiveScene().name != "MainGameScene")
            return;

        ClosePauseMenu();
    }

    private void HandleQuitButtonPressed()
    {
        if (CurrentPlayer == null || isReturningToIntro)
            return;

        if (SceneManager.GetActiveScene().name != "MainGameScene")
            return;

        if (ResumeActivityInteraction.IsAnyMinigameOpen ||
            ProjectPipelineChaseMinigameInteraction.IsAnyMinigameOpen ||
            CertificateMinigameInteraction.IsGameplayInputBlocked ||
            ResumeTailoredMinigameInteraction.IsGameplayInputBlocked ||
            ResumeSwipeMinigameInteraction.IsGameplayInputBlocked ||
            NetworkingMemoryMinigameInteraction.IsGameplayInputBlocked)
            return;

        isPauseMenuOpen = false;
        ApplyPauseCursorState(false);
        StartCoroutine(DeleteCurrentPlayerAndReturnToIntro());
    }

    private void ApplyPauseCursorState(bool showCursor)
    {
        if (SceneManager.GetActiveScene().name != "MainGameScene")
            return;

        Cursor.visible = showCursor;
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private string BuildResumeProgressText(PlayerStateDto player)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("Resume: ").Append(player.score).Append('\n');
        builder.Append(FormatProgressLineSafe(player.completed_resume_tailored, "Resume activity")).Append('\n');
        builder.Append(FormatProgressLineSafe(player.completed_project, "Project")).Append('\n');
        builder.Append(FormatProgressLineSafe(player.completed_certificate, "Certificate")).Append('\n');
        builder.Append(FormatProgressLineSafe(player.completed_work_experience, "Life experience")).Append('\n');
        builder.Append("Highest company: ").Append(GetHighestVerifiedCompanyLabel(player));
        return builder.ToString();
    }

    private string FormatProgressLineSafe(bool completed, string label)
    {
        return (completed ? "[OK] " : "[...] ") + label;
    }

    private string FormatProgressLine(bool completed, string label)
    {
        return (completed ? "[✓] " : "[…] ") + label;
    }

    private string GetHighestVerifiedCompanyLabel(PlayerStateDto player)
    {
        if (player == null || player.successful_company_tiers == null || player.successful_company_tiers.Length == 0)
            return "none";

        bool hasStartup = false;
        bool hasMidTier = false;
        bool hasBigTech = false;
        for (int i = 0; i < player.successful_company_tiers.Length; i++)
        {
            string tier = player.successful_company_tiers[i];
            if (tier == "startup") hasStartup = true;
            if (tier == "mid_tier") hasMidTier = true;
            if (tier == "big_tech") hasBigTech = true;
        }

        if (hasBigTech) return "Big Tech";
        if (hasMidTier) return "Mid-tier";
        if (hasStartup) return "Startup";
        return "none";
    }

    private bool IsBigTechWin(PlayerStateDto player)
    {
        return player != null &&
               player.is_employed &&
               player.employed_company_tier == "big_tech";
    }

    private bool IsWinPanelVisible()
    {
        return winPanel != null && winPanel.activeInHierarchy;
    }

    private bool IsGameOverPanelVisible()
    {
        return gameOverPanel != null && gameOverPanel.activeInHierarchy;
    }

    private bool ShouldShowWinPanel()
    {
        return IsBigTechWin(CurrentPlayer) && !winPanelDeferred;
    }

    public void RevealDeferredWinPanelIfNeeded()
    {
        if (CurrentPlayer == null)
            return;

        if (!IsBigTechWin(CurrentPlayer))
            return;

        if (!winPanelDeferred)
            return;

        winPanelDeferred = false;
        UpdateUi();
    }

    private IEnumerator ReturnToIntroAfterWin()
    {
        if (isReturningToIntro)
            yield break;

        isReturningToIntro = true;
        SetBackendStatus("Returning to intro...");

        CurrentPlayer = null;
        if (winPanel != null)
            winPanel.SetActive(false);
        if (winText != null)
            winText.text = string.Empty;

        UpdateUi();
        Time.timeScale = 1f;
        SceneManager.LoadScene("IntroScene");
        isReturningToIntro = false;
    }

    private IEnumerator EnsureBackendReady(System.Action<bool> onComplete)
    {
        if (backendReady)
        {
            onComplete?.Invoke(true);
            yield break;
        }

        string healthUrl = backendBaseUrl.TrimEnd('/') + "/health";
        for (int attempt = 0; attempt < Mathf.Max(1, backendWarmupRetries); attempt++)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(healthUrl))
            {
                request.timeout = Mathf.Max(5, requestTimeoutSeconds);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    backendReady = true;
                    onComplete?.Invoke(true);
                    yield break;
                }
            }

            yield return new WaitForSeconds(backendWarmupDelaySeconds);
        }

        backendReady = false;
        onComplete?.Invoke(false);
    }

    private IEnumerator ReturnToIntroWithoutDeletingPlayer()
    {
        if (isReturningToIntro)
            yield break;

        isReturningToIntro = true;
        isPauseMenuOpen = false;
        SetBackendStatus("Returning to intro...");

        if (winPanel != null)
            winPanel.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        Time.timeScale = 1f;
        SceneManager.LoadScene("IntroScene");
        yield return null;
        isReturningToIntro = false;
    }
}
