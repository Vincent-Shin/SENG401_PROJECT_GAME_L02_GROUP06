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
        public int completion_seconds;
    }

    [System.Serializable]
    public class PlayerList
    {
        public List<PlayerData> players;
    }

    public string url = "https://seng401-project-game-l02-group06-test.onrender.com/leaderboard/top3";
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

        leaderboardText.text = FormatLeaderboard(data != null ? data.players : null);
    }

    string FormatLeaderboard(List<PlayerData> players)
    {
        if (players == null || players.Count == 0)
            return "No Big Tech winners yet.";

        string output = "";

        int count = Mathf.Min(3, players.Count);

        for (int i = 0; i < count; i++)
        {
            output += (i + 1) + ". " +
                      players[i].username +
                      " | Score: " + players[i].score +
                      " | Time: " + FormatTime(players[i].completion_seconds) + "\n";
        }

        return output;
    }

    string FormatTime(int totalSeconds)
    {
        int safeSeconds = Mathf.Max(0, totalSeconds);
        int minutes = safeSeconds / 60;
        int seconds = safeSeconds % 60;
        return minutes.ToString("00") + ":" + seconds.ToString("00");
    }
}
