using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public static PlayerController Instance { get; private set; }
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer =GetComponent<SpriteRenderer>(); 
        rb.gravityScale = 0f;
        ForceStopMovement();
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        ForceStopMovement();
    }

    private void OnDisable()
    {
        ForceStopMovement();
        if (Instance == this)
            Instance = null;
    }

    public void ForceStopMovement()
    {
        movement = Vector2.zero;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

void Update()
{
    bool activityBlocked = ResumeActivityInteraction.IsAnyMinigameOpen &&
                           ResumeActivityInteraction.IsGameplayInputBlocked;
    bool certBlocked = CertificateMinigameInteraction.IsAnyMinigameOpen &&
                       CertificateMinigameInteraction.IsGameplayInputBlocked;
    bool tailoredBlocked = ResumeTailoredMinigameInteraction.IsAnyMinigameOpen &&
                           ResumeTailoredMinigameInteraction.IsGameplayInputBlocked;
    bool swipeBlocked = ResumeSwipeMinigameInteraction.IsAnyMinigameOpen &&
                        ResumeSwipeMinigameInteraction.IsGameplayInputBlocked;
    bool networkingBlocked = NetworkingMemoryMinigameInteraction.IsAnyMinigameOpen &&
                             NetworkingMemoryMinigameInteraction.IsGameplayInputBlocked;
    bool pipelineBlocked = ProjectPipelineChaseMinigameInteraction.IsAnyMinigameOpen &&
                           ProjectPipelineChaseMinigameInteraction.IsGameplayInputBlocked;
    bool projectClaimBlocked = ProjectMainResultPanelController.IsClaimPanelBlockingInput;

    if (activityBlocked ||
        certBlocked ||
        tailoredBlocked ||
        swipeBlocked ||
        networkingBlocked ||
        pipelineBlocked ||
        projectClaimBlocked ||
        (ResumeLogic.Instance != null && ResumeLogic.Instance.IsGameplayLocked))
    {
        ForceStopMovement();
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
        if (rb == null)
            return;

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
