using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ShooterEnemyPositioner2D : MonoBehaviour
{
    public float targetY = 3.2f;
    public float moveSpeed = 1.8f;
    public float driftSpeed = 2f;
    public bool neverDrift;
    public bool sideExit;
    public float holdAfterBurst = 0.8f;
    public float exitDelay = 5f;

    private Rigidbody2D body;
    private EnemyStatus2D status;
    private EnemyShooter2D shooter;
    private bool reachedPosition;
    private float driftStartTime;
    private bool wasInCooldown;
    private float sideExitDirection;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        status = GetComponent<EnemyStatus2D>();
        shooter = GetComponent<EnemyShooter2D>();
    }

    private void FixedUpdate()
    {
        bool inCooldown = shooter != null && shooter.IsInCooldown;

        if (inCooldown && reachedPosition && !neverDrift)
        {
            if (!wasInCooldown)
            {
                driftStartTime = Time.time + holdAfterBurst;
                if (sideExit)
                {
                    Camera cam = Camera.main;
                    float camLeft = cam != null ? cam.transform.position.x - cam.orthographicSize * cam.aspect : -8f;
                    float camRight = cam != null ? cam.transform.position.x + cam.orthographicSize * cam.aspect : 8f;
                    float distLeft = transform.position.x - camLeft;
                    float distRight = camRight - transform.position.x;
                    sideExitDirection = distLeft < distRight ? -1f : 1f;
                }
            }

            if (Time.time >= driftStartTime)
            {
                float multiplier = status != null ? status.MoveMultiplier : 1f;

                if (sideExit && Time.time >= driftStartTime + exitDelay)
                {
                    body.velocity = new Vector2(sideExitDirection * driftSpeed * multiplier, 0f);
                }
                else
                {
                    body.velocity = Vector2.down * (driftSpeed * multiplier);
                }
            }
            else
            {
                body.velocity = Vector2.zero;
            }

            wasInCooldown = inCooldown;
            return;
        }

        wasInCooldown = inCooldown;

        if (neverDrift && reachedPosition)
        {
            if (transform.position.y < targetY - 0.3f)
                body.velocity = Vector2.up * moveSpeed;
            else
                body.velocity = Vector2.zero;
            return;
        }

        if (!reachedPosition && transform.position.y > targetY)
        {
            float multiplier = status != null ? status.MoveMultiplier : 1f;
            body.velocity = Vector2.down * (moveSpeed * multiplier);
        }
        else
        {
            reachedPosition = true;
            body.velocity = Vector2.zero;
        }
    }
}
