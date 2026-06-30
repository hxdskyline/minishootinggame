using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyShooter2D : MonoBehaviour
{
    public bool autoFire = true;
    public bool useBurstFire = true;
    public bool burstOnce;
    public Transform target;
    public Sprite projectileSprite;
    public float range = 8f;
    public float burstInterval = 0.3f;
    public int burstCount = 3;
    public float burstCooldown = 3f;
    public float fireInterval = 1.2f;
    public float projectileSpeed = 6f;
    public float visiblePadding = 0.35f;
    public float spawnDelay = 2f;

    private EnemyStatus2D status;
    private float spawnTime;
    private float nextFireTime;
    private float nextSfxTime;
    private int shotsInBurst;
    private float cooldownEndTime;

    public bool IsInCooldown => Time.time < cooldownEndTime;

    private void Awake()
    {
        spawnTime = Time.time;
        status = GetComponent<EnemyStatus2D>();
    }

    private void Update()
    {
        if (!autoFire || target == null || Time.time - spawnTime < spawnDelay || Time.time < nextFireTime || !IsInsideCameraView()) return;

        Vector2 direction = target.position - transform.position;
        if (direction.sqrMagnitude > range * range) return;

        float intervalMultiplier = status != null ? status.AttackIntervalMultiplier : 1f;
        FireDirected(direction.normalized);

        if (!useBurstFire)
        {
            nextFireTime = Time.time + fireInterval * intervalMultiplier;
            return;
        }

        shotsInBurst++;
        if (shotsInBurst >= burstCount)
        {
            shotsInBurst = 0;
            if (burstOnce)
            {
                autoFire = false;
                cooldownEndTime = Time.time + 999f;
                return;
            }
            cooldownEndTime = Time.time + burstCooldown;
            nextFireTime = cooldownEndTime;
        }
        else
        {
            nextFireTime = Time.time + burstInterval * intervalMultiplier;
        }
    }

    private bool IsInsideCameraView()
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic) return true;

        Vector3 cameraPosition = camera.transform.position;
        float verticalExtent = camera.orthographicSize;
        float horizontalExtent = verticalExtent * camera.aspect;
        Vector3 position = transform.position;

        return position.x >= cameraPosition.x - horizontalExtent - visiblePadding
            && position.x <= cameraPosition.x + horizontalExtent + visiblePadding
            && position.y >= cameraPosition.y - verticalExtent - visiblePadding
            && position.y <= cameraPosition.y + verticalExtent + visiblePadding;
    }

    public void FireDirected(Vector2 direction)
    {
        if (Time.time >= nextSfxTime)
        {
            nextSfxTime = Time.time + 0.2f;
            SoundHelper.PlayOneShot("EnemyBullet3_595", 0.3f);
        }

        GameObject projectile = new GameObject("Enemy Projectile");
        projectile.transform.position = transform.position + (Vector3)(direction * 0.65f);
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        projectile.transform.localScale = Vector3.one * 0.2f;

        var renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite;
        renderer.sortingOrder = 20;

        var collider = projectile.AddComponent<CircleCollider2D>();
        collider.radius = 0.13f;
        collider.isTrigger = true;

        var body = projectile.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var shot = projectile.AddComponent<Projectile2D>();
        shot.direction = direction;
        shot.speed = projectileSpeed;
        shot.owner = gameObject;
        shot.hitPlayers = true;
        shot.hitEnemies = false;
    }
}
