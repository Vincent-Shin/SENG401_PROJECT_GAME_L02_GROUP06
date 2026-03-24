using TMPro;
using UnityEngine;

public class IntroLeaderboardToggle : MonoBehaviour
{
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private TMP_Text toggleButtonText;
    [SerializeField] private bool startOpen;

    void Start()
    {
        ApplyState(startOpen);
    }

    public void ToggleLeaderboard()
    {
        ApplyState(leaderboardPanel == null || !leaderboardPanel.activeSelf);
    }

    void ApplyState(bool isOpen)
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(isOpen);

        if (toggleButtonText != null)
            toggleButtonText.text = isOpen ? "Hide Leaderboard" : "Show Leaderboard";
    }
}
