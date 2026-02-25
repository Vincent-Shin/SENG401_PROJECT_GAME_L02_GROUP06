using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

    private const int MAX_ACCOUNTS = 3;
    private const string SAVE_KEY = "ACCOUNTS";

    public void OnContinuePressed()
    {
        string username = nameInput.text.Trim();

        if (!IsValidUsername(username))
        {
            Debug.Log("Invalid name. Max 8 letters/numbers only.");
            return;
        }

        SaveAccount(username);

        introCanvas.SetActive(false);
        gameplayRoot.SetActive(true);
        dialoguepanel.SetActive(true);
        
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