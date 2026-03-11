using UnityEngine;

public class ProjectMainReturnHandler : MonoBehaviour
{
    [SerializeField] private Transform player;

    private void Start()
    {
        bool shouldReturnToTerminal = PlayerPrefs.GetInt("project_should_return_to_terminal", 0) == 1;

        if (shouldReturnToTerminal && player != null)
        {
            float x = PlayerPrefs.GetFloat("project_return_x", player.position.x);
            float y = PlayerPrefs.GetFloat("project_return_y", player.position.y);
            player.position = new Vector3(x, y, player.position.z);
        }

        if (shouldReturnToTerminal)
        {
            PlayerPrefs.SetInt("project_should_return_to_terminal", 0);
            PlayerPrefs.Save();
        }
    }
}
