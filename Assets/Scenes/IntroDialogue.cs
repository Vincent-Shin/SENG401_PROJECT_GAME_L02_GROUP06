using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IntroDialogue : MonoBehaviour
{
    public string[] lines;
    public TMP_Text dialogueText;
    public GameObject dialoguePanel;
    [SerializeField] private TMP_Text controlsHintText;
    [SerializeField] private GameplayGuideFlow guideFlow;
    [SerializeField] private bool autoStartOnEnable = true;


    private int currentIndex = -1;
    private bool hasStarted;

    void Start()
    {
        if (autoStartOnEnable)
            BeginDialogue();
        else if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
    void Update()
    {
        if (!hasStarted)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            NextLine();
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            SkipScene();
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            BackToGuide();
        }
    }

    public void BeginDialogue()
    {
        hasStarted = true;
        currentIndex = 0;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            dialoguePanel.transform.SetAsLastSibling();
        }

        if (dialogueText != null && lines != null && lines.Length > 0)
            dialogueText.text = lines[currentIndex];

        if (controlsHintText != null)
            controlsHintText.text = "\"SPACE\" to next dialogue\n\"S\" to skip the scene\n\"B\" to back the Guide panel";
    }

    public void NextLine()
    {
        if (!hasStarted)
            return;

        currentIndex++;

        if (currentIndex < lines.Length)
        {
            dialogueText.text = lines[currentIndex];
        }
        else
        {
            SceneManager.LoadScene("MainGameScene");
        }
    }

    public void StopDialogue()
    {
        hasStarted = false;
        currentIndex = -1;
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void SkipScene()
    {
        SceneManager.LoadScene("MainGameScene");
    }

    private void BackToGuide()
    {
        StopDialogue();
        if (guideFlow != null)
            guideFlow.ReturnToGuideFromDialogue();
    }
}
