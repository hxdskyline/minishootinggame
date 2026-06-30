using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChaser2D : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 1.2f;
    public float maxMoveSpeed = 4.5f;
    public float acceleration = 0.8f;
    public int contactDamage = 1;

    private Rigidbody2D body;
    private EnemyStatus2D status;
    private bool lockedDirection;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        status = GetComponent<EnemyStatus2D>();
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            body.velocity = Vector2.zero;
            return;
        }

        float topThirdY = GetTopThirdY();
        if (transform.position.y < topThirdY && !lockedDirection)
        {
            lockedDirection = true;
        }

        if (lockedDirection)
        {
            return;
        }

        moveSpeed = Mathf.Min(maxMoveSpeed, moveSpeed + acceleration * Time.fixedDeltaTime);
        Vector2 direction = target.position - transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            float multiplier = status != null ? status.MoveMultiplier : 1f;
            Vector2 velocity = direction.normalized * moveSpeed * multiplier;
            body.velocity = velocity;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            body.rotation = angle;
        }
    }

    private static float GetTopThirdY()
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic) return 2f;
        float top = camera.transform.position.y + camera.orthographicSize;
        float height = camera.orthographicSize * 2f;
        return top - height / 3f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Health2D health = collision.gameObject.GetComponent<Health2D>();
        if (health == null || collision.gameObject.GetComponent<PlayerController2D>() == null) return;

        health.TakeDamage(contactDamage);
        Destroy(gameObject);
    }
}
