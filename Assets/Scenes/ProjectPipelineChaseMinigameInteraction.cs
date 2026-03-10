using UnityEngine;

// Compatibility shim:
// Old scripts still reference these static flags. This keeps them compiling
// after moving project gameplay to a separate scene controller.
public class ProjectPipelineChaseMinigameInteraction : MonoBehaviour
{
    public static bool IsAnyMinigameOpen => false;
    public static bool IsGameplayInputBlocked => false;
}

