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
    movement = Vector2.zero;

    if (Input.GetKey(KeyCode.Keypad1)) // Left
        movement.x = -1;

    if (Input.GetKey(KeyCode.Keypad3)) // Right
        movement.x = 1;

    if (Input.GetKey(KeyCode.Keypad5)) // Up
        movement.y = 1;

    if (Input.GetKey(KeyCode.Keypad2)) // Down
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