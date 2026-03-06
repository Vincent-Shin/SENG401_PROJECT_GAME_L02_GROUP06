using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuUI;
    [SerializeField] private GameObject[] panelsToHideOnPause;

    private bool isOpen = false;
    private bool[] panelActiveStates;

    void Start()
    {
        if (menuUI != null)
            menuUI.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        panelActiveStates = new bool[panelsToHideOnPause.Length];
    }

    void Update()
    {
        if ((ResumeLogic.Instance != null && ResumeLogic.Instance.IsGameplayLocked) ||
            CertificateMinigameInteraction.IsGameplayInputBlocked ||
            ResumeTailoredMinigameInteraction.IsGameplayInputBlocked ||
            ResumeSwipeMinigameInteraction.IsGameplayInputBlocked)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }


        if (isOpen && Input.GetKeyDown(KeyCode.X))
        {
            ResumeGame();
        }
    }

    public void ToggleMenu()
    {
        isOpen = !isOpen;
        if (menuUI != null)
            menuUI.SetActive(isOpen);

        if (isOpen)
        {
            CacheAndHidePanels();
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (menuUI != null)
                menuUI.transform.SetAsLastSibling();
        }
        else
        {
            ResumeGame();
        }
    }

    public void ResumeGame()
    {
        isOpen = false;
        if (menuUI != null)
            menuUI.SetActive(false);
        RestorePanels();
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    void CacheAndHidePanels()
    {
        for (int i = 0; i < panelsToHideOnPause.Length; i++)
        {
            GameObject panel = panelsToHideOnPause[i];
            if (panel == null)
                continue;

            panelActiveStates[i] = panel.activeSelf;
            panel.SetActive(false);
        }
    }

    void RestorePanels()
    {
        for (int i = 0; i < panelsToHideOnPause.Length; i++)
        {
            GameObject panel = panelsToHideOnPause[i];
            if (panel == null)
                continue;

            panel.SetActive(panelActiveStates[i]);
        }
    }
}
