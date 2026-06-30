using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile2D : MonoBehaviour
{
    public Vector2 direction = Vector2.right;
    public float speed = 13f;
    public int damage = 1;
    public float lifetime = 2.5f;
    public GameObject owner;
    public bool hitPlayers;
    public bool hitEnemies = true;
    public bool piercesEnemies;

    private Rigidbody2D body;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        body.velocity = direction.normalized * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.gameObject == owner) return;

        bool isPlayer = other.GetComponent<PlayerController2D>() != null;
        Health2D health = other.GetComponent<Health2D>();
        bool isEnemy = health != null && !isPlayer;
        if (isPlayer && !hitPlayers) return;
        if (isEnemy && !hitEnemies) return;
        if (isEnemy && hitPlayers && !hitEnemies) return;
        if (!isPlayer && !isEnemy) return;

        if (hitEnemies && transform.position.y > GetCameraTopY()) return;

        if (health != null)
        {
            int finalDamage = damage;
            if (isEnemy)
            {
                EnemyStatus2D status = other.GetComponent<EnemyStatus2D>();
                if (status != null)
                {
                    finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * status.DamageTakenMultiplier));
                }
            }

            health.TakeDamage(finalDamage, transform.position);
            if (isEnemy)
            {
                float enemyScale = other.transform.localScale.x;
                EnemyDeathEffect2D.Spawn(transform.position, enemyScale * 0.1f);
            }
            if (!piercesEnemies || !isEnemy)
            {
                Destroy(gameObject);
            }
        }
    }

    private static float GetCameraTopY()
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic) return 6f;
        return camera.transform.position.y + camera.orthographicSize;
    }
}
