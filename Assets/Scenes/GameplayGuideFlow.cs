using UnityEngine;

public class GameplayGuideFlow : MonoBehaviour
{
    [Header("Guide Pages")]
    [SerializeField] private GameObject storyPage;
    [SerializeField] private GameObject controlsPage;

    [Header("Next Step")]
    [SerializeField] private GameObject backgroundPanel;
    [SerializeField] private IntroDialogue introDialogue;
    [SerializeField] private GameObject dialoguePanel;

    private bool isActive;
    private int pageIndex;

    private void Start()
    {
        SetGuideActive(false);
    }

    private void Update()
    {
        if (!isActive)
            return;

        if (!Input.GetKeyDown(KeyCode.Return))
            return;

        if (pageIndex == 0)
        {
            pageIndex = 1;
            if (storyPage != null) storyPage.SetActive(false);
            if (controlsPage != null) controlsPage.SetActive(true);
            return;
        }

        SetGuideActive(false);
        if (backgroundPanel != null)
            backgroundPanel.SetActive(true);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (introDialogue != null)
        {
            introDialogue.BeginDialogue();
        }
    }

    public void BeginGuide()
    {
        pageIndex = 0;
        SetGuideActive(true);
        if (storyPage != null) storyPage.SetActive(true);
        if (controlsPage != null) controlsPage.SetActive(false);
        if (backgroundPanel != null) backgroundPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    public void ReturnToGuideFromDialogue()
    {
        pageIndex = 0;
        SetGuideActive(true);
        if (storyPage != null) storyPage.SetActive(true);
        if (controlsPage != null) controlsPage.SetActive(false);
        if (backgroundPanel != null) backgroundPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    private void SetGuideActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            if (storyPage != null) storyPage.SetActive(false);
            if (controlsPage != null) controlsPage.SetActive(false);
        }
        gameObject.SetActive(active);
    }
}
