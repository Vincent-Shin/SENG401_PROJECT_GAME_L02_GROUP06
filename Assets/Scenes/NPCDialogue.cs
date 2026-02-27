using UnityEngine;
using TMPro;
using System.Collections;

public class NPCDialogue : MonoBehaviour
{
    public string[] firstDialogue;
    public string[] secondDialogue;

    public GameObject dialoguePanel;
    public TMP_Text dialogueText;

    public float autoLineDuration = 2.5f;

    private bool playerInRange = false;
    private bool isTalking = false;
    private bool hasTalkedBefore = false;

    private int currentIndex = 0;

    void Update()
    {
        if (playerInRange && !hasTalkedBefore && isTalking && Input.GetKeyDown(KeyCode.Space))
        {
            NextLineManual();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (!isTalking)
        {
            if (!hasTalkedBefore)
                StartFirstDialogue();
            else
                StartCoroutine(PlaySecondDialogueAuto());
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        EndDialogue();
    }

    void StartFirstDialogue()
    {
        isTalking = true;
        dialoguePanel.SetActive(true);

        currentIndex = 0;
        dialogueText.text = firstDialogue[currentIndex];
    }

    void NextLineManual()
    {
        currentIndex++;

        if (currentIndex < firstDialogue.Length)
        {
            dialogueText.text = firstDialogue[currentIndex];
        }
        else
        {
            EndDialogue();
            hasTalkedBefore = true;
        }
    }

    IEnumerator PlaySecondDialogueAuto()
    {
        isTalking = true;
        dialoguePanel.SetActive(true);

        for (int i = 0; i < secondDialogue.Length; i++)
        {
            dialogueText.text = secondDialogue[i];
            yield return new WaitForSeconds(autoLineDuration);
        }

        EndDialogue();
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        isTalking = false;
    }
}