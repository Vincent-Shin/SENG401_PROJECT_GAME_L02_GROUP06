using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IntroDialogue : MonoBehaviour
{
    public string[] lines;
    public TMP_Text dialogueText;
    public GameObject dialoguePanel;


    private int currentIndex = -1;

    void Start()
    {
        dialoguePanel.SetActive(true);
        currentIndex = 0;
        dialogueText.text = lines[currentIndex];
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NextLine();
        }
    }
    public void NextLine()
    {
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
}