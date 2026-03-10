using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

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
    [SerializeField] private string backendBaseUrl = "http://127.0.0.1:8000";

    [Header("Optional HUD")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text attemptsLeftText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text backendStatusText;

    [Header("Auto Bind Scene UI")]
    [SerializeField] private bool autoBindSceneTexts = true;
    [SerializeField] private string scoreTextObjectName = "resume score";
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
    [SerializeField] private int debugStartingScore = 50;

    public PlayerStateDto CurrentPlayer { get; private set; }

    public bool HasLoadedPlayer => CurrentPlayer != null;
    public bool IsGameplayLocked => isReturningToIntro || (CurrentPlayer != null && (CurrentPlayer.is_game_over || ShouldShowWinPanel()));
    private bool isReturningToIntro;
    private bool winPanelDeferred;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
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

                CurrentPlayer = loadResponse.player;
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

        yield return SendJsonRequest(
            "/apply",
            UnityWebRequest.kHttpVerbPOST,
            JsonUtility.ToJson(payload),
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
                    SetBackendStatus(response.error);
                else if (response != null)
                    SetBackendStatus("Apply result: " + response.result);

                onComplete?.Invoke(response);
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
                        SetBackendStatus(errorResponse.error);

                    onComplete?.Invoke(errorResponse);
                    return;
                }

                SetBackendStatus(error);
                onComplete?.Invoke(new ApplyResponse { error = error });
            });
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

        yield return SendJsonRequest(
            "/player/complete-activity",
            UnityWebRequest.kHttpVerbPOST,
            JsonUtility.ToJson(payload),
            json =>
            {
                PlayerStateResponse response = JsonUtility.FromJson<PlayerStateResponse>(json);
                if (response == null || response.player == null)
                {
                    onComplete?.Invoke(false, false, "Invalid backend response.");
                    return;
                }

                CurrentPlayer = response.player;
                UpdateUi();
                onComplete?.Invoke(response.updated, response.already_completed, null);
            },
            error => onComplete?.Invoke(false, false, error));
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

        using (UnityWebRequest request = new UnityWebRequest(url, method))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                onError?.Invoke(string.IsNullOrEmpty(responseText) ? "Backend request failed: " + request.error : responseText);
                yield break;
            }

            onSuccess?.Invoke(request.downloadHandler.text);
        }
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

    private void UpdateUi()
    {
        SanitizeSceneReferences();

        if (CurrentPlayer == null)
        {
            if (scoreText != null)
                scoreText.text = "Resume: --\nClick next button to see what your resume has now.";
            if (attemptsLeftText != null)
                attemptsLeftText.text = "Attempts: --";
            if (playerNameText != null)
                playerNameText.text = "Player: --";
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            if (winPanel != null)
                winPanel.SetActive(false);
            Time.timeScale = 1f;
            return;
        }

        if (scoreText != null)
            scoreText.text = "Resume: " + CurrentPlayer.score + "\nClick next button to see what your resume has now.";

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

        Time.timeScale = ((shouldShowGameOverPanel && IsGameOverPanelVisible()) || (shouldShowWinPanel && IsWinPanelVisible())) ? 0f : 1f;
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

        SanitizeSceneReferences();

        if (autoBindSceneTexts)
            AutoBindSceneTexts();

        if (scene.name == "MainGameScene")
        {
            // Main gameplay scene must always start unpaused.
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            if (winPanel != null)
                winPanel.SetActive(false);
        }

        UpdateUi();
    }

    private void ClearSceneUiReferences()
    {
        scoreText = null;
        attemptsLeftText = null;
        playerNameText = null;
        backendStatusText = null;
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

        if (gameOverPanel == null)
            gameOverPanel = FindGameObjectByName(gameOverPanelObjectName);

        if (gameOverText == null)
            gameOverText = FindTextByObjectName(gameOverTextObjectName);

        if (winPanel == null)
            winPanel = FindGameObjectByName(winPanelObjectName);

        if (winText == null)
            winText = FindTextByObjectName(winTextObjectName);
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
}
