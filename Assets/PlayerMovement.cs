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

    private bool canDash = true;
    private bool isDashing;
    private float dashingPower = 15f; // Adjust dash power as needed
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

    public int Respawn;

    private void Update()
    {
        if (isDashing) return;

        // Horizontal movement via arrow keys
        if (Input.GetKey(KeyCode.RightArrow))
            horizontal = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow))
            horizontal = -1f;
        else
            horizontal = 0f;

        // Update facing based on horizontal input only if not wall jumping
        if (!isWallJumping && horizontal != 0)
        {
            isFacingRight = horizontal > 0;
        }

        // Jump on X key
        if (Input.GetKeyDown(KeyCode.X) && IsGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
        }

        // Short jump if X released early
        if (Input.GetKeyUp(KeyCode.X) && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }

        // Restart scene on R key if needed
        if (Input.GetKeyDown(KeyCode.R) && canDash)
        {
            SceneManager.LoadScene(Respawn);
        }

        // Dash on Z key
        if (Input.GetKeyDown(KeyCode.Z) && canDash)
        {
            Vector2 dashDir = GetDashDirection();

            // If wall sliding and dash direction is purely vertical, reset horizontal to face direction
            if (isWallSliding && Mathf.Abs(dashDir.x) < 0.1f && Mathf.Abs(dashDir.y) > 0.1f)
            {
                dashDir.x = isFacingRight ? 1f : -1f;
                dashDir.y = 0f;
                dashDir.Normalize();
            }

            StartCoroutine(Dash(dashDir));
        }

        WallSlide();
        WallJump();

        // Always flip to face movement direction unless wall jumping (flip handled there)
        if (!isWallJumping)
            Flip();
    }

    private Vector2 GetDashDirection()
    {
        float dashX = 0f;
        float dashY = 0f;

        if (Input.GetKey(KeyCode.RightArrow))
            dashX += 1f;
        if (Input.GetKey(KeyCode.LeftArrow))
            dashX -= 1f;
        if (Input.GetKey(KeyCode.UpArrow))
            dashY += 1f;
        if (Input.GetKey(KeyCode.DownArrow))
            dashY -= 1f;

        Vector2 dashDirection = new Vector2(dashX, dashY);

        if (dashDirection == Vector2.zero)
        {
            // No arrow pressed: dash in facing direction horizontally
            dashDirection = isFacingRight ? Vector2.right : Vector2.left;
        }

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

            // Face direction of wall jump
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
        {
            FlipCharacterScale();
        }
        else if (!isFacingRight && localScale.x > 0)
        {
            FlipCharacterScale();
        }
    }

    private void FlipCharacterScale()
    {
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    private IEnumerator Dash(Vector2 direction)
    {
        canDash = false;
        isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        rb.velocity = direction * dashingPower;

        if (tr != null) tr.emitting = true;
        yield return new WaitForSeconds(dashingTime);
        if (tr != null) tr.emitting = false;

        rb.gravityScale = originalGravity;
        isDashing = false;

        // Allow dash immediately again
        canDash = true;
    }
}






















