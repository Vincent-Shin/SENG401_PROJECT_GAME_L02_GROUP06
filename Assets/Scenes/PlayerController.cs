using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer =GetComponent<SpriteRenderer>(); 
        rb.gravityScale = 0f; 
    }

    void Update()
{
    bool certBlocked = CertificateMinigameInteraction.IsAnyMinigameOpen &&
                       CertificateMinigameInteraction.IsGameplayInputBlocked;
    bool tailoredBlocked = ResumeTailoredMinigameInteraction.IsAnyMinigameOpen &&
                           ResumeTailoredMinigameInteraction.IsGameplayInputBlocked;
    bool swipeBlocked = ResumeSwipeMinigameInteraction.IsAnyMinigameOpen &&
                        ResumeSwipeMinigameInteraction.IsGameplayInputBlocked;
    bool pipelineBlocked = ProjectPipelineChaseMinigameInteraction.IsAnyMinigameOpen &&
                           ProjectPipelineChaseMinigameInteraction.IsGameplayInputBlocked;

    if (certBlocked ||
        tailoredBlocked ||
        swipeBlocked ||
        pipelineBlocked ||
        (ResumeLogic.Instance != null && ResumeLogic.Instance.IsGameplayLocked))
    {
        movement = Vector2.zero;
        return;
    }

    movement = Vector2.zero;

    if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) // Left
        movement.x = -1;

    if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) // Right
        movement.x = 1;

    if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) // Up
        movement.y = 1;

    if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) // Down
        movement.y = -1;

    movement = movement.normalized;
}
    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
        if(movement.x < 0)
        {
            spriteRenderer.flipX = true;
        }else if (movement.x > 0)
        {
            spriteRenderer.flipX = false;
        }
    }
}
