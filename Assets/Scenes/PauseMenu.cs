using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuUI;

    private bool isOpen = false;

    void Start()
    {
        menuUI.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
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
        menuUI.SetActive(isOpen);

        if (isOpen)
        {
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            ResumeGame();
        }
    }

    public void ResumeGame()
    {
        isOpen = false;
        menuUI.SetActive(false);
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}