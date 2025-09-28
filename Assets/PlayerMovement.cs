using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    private float horizontal;
    private float speed = 10f;
    private float jumpingPower = 17f;
    private bool isFacingRight = true;
    private bool isWallSliding;
    private float wallSlidingSpeed = 2f;

    private bool isDashing;
    private float dashingPower = 15f;
    private float dashingTime = 0.2f;

    private bool isWallJumping;
    private float wallJumpingDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    private float wallJumpingDuration = 0.4f;
    private Vector2 wallJumpingPower = new Vector2(22f, 20f);

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer tr;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;

    [SerializeField] private int respawnSceneIndex; // scene index for respawn
    [SerializeField] private int maxDashes = 3;     // ?? how many dashes allowed before respawn

    private int currentDashes; // counter for remaining dashes

    private void Start()
    {
        currentDashes = maxDashes; // reset at start
    }

    private void Update()
    {
        if (isDashing) return;

        // Horizontal movement
        if (Input.GetKey(KeyCode.RightArrow))
            horizontal = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow))
            horizontal = -1f;
        else
            horizontal = 0f;

        if (!isWallJumping && horizontal != 0)
            isFacingRight = horizontal > 0;

        // Jump
        if (Input.GetKeyDown(KeyCode.X) && IsGrounded())
            rb.velocity = new Vector2(rb.velocity.x, jumpingPower);

        if (Input.GetKeyUp(KeyCode.X) && rb.velocity.y > 0f)
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);

        // Restart manually
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(respawnSceneIndex);

        // Dash
        if (Input.GetKeyDown(KeyCode.Z) && currentDashes > 0)
        {
            Vector2 dashDir = GetDashDirection();

            if (isWallSliding && Mathf.Abs(dashDir.x) < 0.1f && Mathf.Abs(dashDir.y) > 0.1f)
            {
                dashDir.x = isFacingRight ? 1f : -1f;
                dashDir.y = 0f;
                dashDir.Normalize();
            }

            StartCoroutine(Dash(dashDir));

            // reduce dash count
            currentDashes--;

            // if no dashes left, respawn
            if (currentDashes <= 0)
            {
                SceneManager.LoadScene(respawnSceneIndex);
            }
        }

        WallSlide();
        WallJump();

        if (!isWallJumping)
            Flip();
    }

    private Vector2 GetDashDirection()
    {
        float dashX = 0f;
        float dashY = 0f;

        if (Input.GetKey(KeyCode.RightArrow)) dashX += 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) dashX -= 1f;
        if (Input.GetKey(KeyCode.UpArrow)) dashY += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) dashY -= 1f;

        Vector2 dashDirection = new Vector2(dashX, dashY);
        if (dashDirection == Vector2.zero)
            dashDirection = isFacingRight ? Vector2.right : Vector2.left;

        return dashDirection.normalized;
    }

    private void FixedUpdate()
    {
        if (isDashing) return;

        if (!isWallJumping)
            rb.velocity = new Vector2(horizontal * speed, rb.velocity.y);
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private bool IsWalled()
    {
        return Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
    }

    private void WallSlide()
    {
        if (IsWalled() && !IsGrounded())
        {
            bool movingAwayFromWall = (horizontal > 0f && !isFacingRight) || (horizontal < 0f && isFacingRight);
            if (!movingAwayFromWall)
            {
                isWallSliding = true;
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
            }
            else
            {
                isWallSliding = false;
            }
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;
            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.X) && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rb.velocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
            wallJumpingCounter = 0f;

            if (wallJumpingDirection > 0 && !isFacingRight)
            {
                isFacingRight = true;
                FlipCharacterScale();
            }
            else if (wallJumpingDirection < 0 && isFacingRight)
            {
                isFacingRight = false;
                FlipCharacterScale();
            }

            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    private void Flip()
    {
        Vector3 localScale = transform.localScale;
        if (isFacingRight && localScale.x < 0)
            FlipCharacterScale();
        else if (!isFacingRight && localScale.x > 0)
            FlipCharacterScale();
    }

    private void FlipCharacterScale()
    {
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    private IEnumerator Dash(Vector2 direction)
    {
        isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        rb.velocity = direction * dashingPower;

        if (tr != null) tr.emitting = true;
        yield return new WaitForSeconds(dashingTime);
        if (tr != null) tr.emitting = false;

        rb.gravityScale = originalGravity;
        isDashing = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Banana"))
        {
            // Increase dash capacity AND refill
            maxDashes++;
            currentDashes++;

            Destroy(collision.gameObject);
        }
    }
}
























