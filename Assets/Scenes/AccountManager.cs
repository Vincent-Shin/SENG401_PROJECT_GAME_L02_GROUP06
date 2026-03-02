using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections;

[System.Serializable]
public class AccountData
{
    public string username;
    public long createdAt;
}

public class AccountManager : MonoBehaviour
{
    public TMP_InputField nameInput;
    public GameObject introCanvas;
    public GameObject gameplayRoot;
    public GameObject dialoguepanel;
    public TMP_Text feedbackText;

    private const int MAX_ACCOUNTS = 3;
    private const string SAVE_KEY = "ACCOUNTS";

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (nameInput != null)
        {
            nameInput.text = string.Empty;
            nameInput.ActivateInputField();
            nameInput.Select();
        }
    }

    public static void RemoveSavedAccount(string username)
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
            return;

        string json = PlayerPrefs.GetString(SAVE_KEY);
        AccountList wrapper = JsonUtility.FromJson<AccountList>(json);
        List<AccountData> accounts = wrapper != null && wrapper.accounts != null
            ? wrapper.accounts
            : new List<AccountData>();

        accounts.RemoveAll(account => account.username == username);

        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(new AccountList(accounts)));
        PlayerPrefs.Save();
    }

    public void OnContinuePressed()
    {
        string username = nameInput.text.Trim();

        if (!IsValidUsername(username))
        {
            SetFeedback("Invalid name. Max 8 letters/numbers only.");
            return;
        }

        StartCoroutine(LoadPlayerAndEnterGame(username));
    }

    bool IsValidUsername(string name)
    {
        if (name.Length == 0 || name.Length > 8)
            return false;

        return Regex.IsMatch(name, "^[a-zA-Z0-9]+$");
    }

    void SaveAccount(string username)
    {
        List<AccountData> accounts = LoadAccounts();

        AccountData newAccount = new AccountData
        {
            username = username,
            createdAt = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        accounts.Add(newAccount);

        if (accounts.Count > MAX_ACCOUNTS)
        {
            accounts.Sort((a, b) => a.createdAt.CompareTo(b.createdAt));
            accounts.RemoveAt(0);
        }

        string json = JsonUtility.ToJson(new AccountList(accounts));
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    List<AccountData> LoadAccounts()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
            return new List<AccountData>();

        string json = PlayerPrefs.GetString(SAVE_KEY);
        AccountList wrapper = JsonUtility.FromJson<AccountList>(json);

        return wrapper.accounts ?? new List<AccountData>();
    }

    IEnumerator LoadPlayerAndEnterGame(string username)
    {
        if (ResumeLogic.Instance == null)
        {
            SetFeedback("ResumeLogic is missing from the scene.");
            yield break;
        }

        SetFeedback("Loading account...");

        bool completed = false;
        bool success = false;
        string errorMessage = null;

        yield return ResumeLogic.Instance.LoadOrCreatePlayer(username, (requestSuccess, requestError) =>
        {
            completed = true;
            success = requestSuccess;
            errorMessage = requestError;
        });

        if (!completed || !success)
        {
            SetFeedback(string.IsNullOrEmpty(errorMessage) ? "Failed to load account." : errorMessage);
            yield break;
        }

        SaveAccount(username);
        SetFeedback(string.Empty);

        introCanvas.SetActive(false);
        gameplayRoot.SetActive(true);
        dialoguepanel.SetActive(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        yield return ResumeLogic.Instance.UpdateStage("main_game");
    }

    void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;

        if (!string.IsNullOrEmpty(message))
            Debug.Log(message);
    }
}

[System.Serializable]
public class AccountList
{
    public List<AccountData> accounts;

    public AccountList(List<AccountData> list)
    {
        accounts = list;
    }
}
