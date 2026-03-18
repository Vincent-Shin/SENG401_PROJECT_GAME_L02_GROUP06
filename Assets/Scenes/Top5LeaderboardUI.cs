using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class Top5LeaderboardUI : MonoBehaviour
{
    [System.Serializable]
    public class PlayerData
    {
        public string username;
        public int score;
    }

    [System.Serializable]
    public class PlayerList
    {
        public List<PlayerData> players;
    }

    public string url = "http://127.0.0.1:8000/leaderboard/top5";
    public TMP_Text leaderboardText;

    void Start()
    {
        StartCoroutine(GetLeaderboard());
    }

    IEnumerator GetLeaderboard()
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Leaderboard request failed: " + request.error);
            leaderboardText.text = "No leaderboard data";
            yield break;
        }

        string json = request.downloadHandler.text;
        string wrappedJson = "{\"players\":" + json + "}";
        PlayerList data = JsonUtility.FromJson<PlayerList>(wrappedJson);

        leaderboardText.text = FormatLeaderboard(data.players);
    }

    string FormatLeaderboard(List<PlayerData> players)
    {
        string output = "";

        for (int i = 0; i < players.Count; i++)
        {
            output += (i + 1) + ". " +
                      players[i].username +
                      " | Score: " + players[i].score + "\n";
        }

        return output;
    }
}