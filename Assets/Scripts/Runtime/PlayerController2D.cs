using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float fireRate = 0.48f;
    public float projectileSpeed = 13f;
    public Sprite projectileSprite;
    public Sprite idleSprite;
    public Sprite rightTransitionSprite;
    public Sprite rightTiltSprite;
    public Sprite leftTransitionSprite;
    public Sprite leftTiltSprite;

    public int cannonDamageLevel;
    public int cannonTrajectoryLevel;
    public int cannonFireRateLevel;
    public int iceDamageLevel;
    public int iceCountLevel;
    public int iceSlowLevel;
    public int airBombCountLevel;
    public int airBombExplosionLevel;
    public int airBombSplitLevel;
    public int droneDamageLevel;
    public int droneDurationLevel;
    public int droneWidthLevel;
    public int lifeRestoreLevel;

    public bool iceUnlocked;
    public bool airBombUnlocked;
    public bool droneUnlocked;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Vector2 movement;
    private float nextFireTime;
    private float nextIceTime;
    private float nextAirBombTime;
    private float nextDroneTime;
    private float tiltStartTime;
    private float stationaryTime;
    private int tiltDirection;
    private int iceVolleyId;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (movement.sqrMagnitude > 1f) movement.Normalize();

        stationaryTime = movement.sqrMagnitude <= 0.001f ? stationaryTime + Time.deltaTime : 0f;

        UpdateSpriteState();
        transform.rotation = Quaternion.identity;

        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + GetCannonInterval();
            FireCannon();
        }

        if (iceUnlocked && Time.time >= nextIceTime)
        {
            nextIceTime = Time.time + 3.5f;
            FireIceMissiles();
        }

        if (airBombUnlocked && Time.time >= nextAirBombTime)
        {
            nextAirBombTime = Time.time + 2f;
            FireAirBombs();
        }

        if (droneUnlocked && Time.time >= nextDroneTime)
        {
            nextDroneTime = Time.time + GetDroneCooldown();
            SpawnDrones();
        }
    }

    private void FixedUpdate()
    {
        body.velocity = movement * moveSpeed;
        ClampToCameraBounds();
    }

    private void ClampToCameraBounds()
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic) return;

        float verticalExtent = camera.orthographicSize;
        float horizontalExtent = verticalExtent * camera.aspect;
        Vector3 cameraPosition = camera.transform.position;
        Vector3 position = transform.position;
        float padding = 0.45f;

        position.x = Mathf.Clamp(position.x, cameraPosition.x - horizontalExtent + padding, cameraPosition.x + horizontalExtent - padding);
        position.y = Mathf.Clamp(position.y, cameraPosition.y - verticalExtent + padding, cameraPosition.y + verticalExtent - padding);
        transform.position = position;
    }

    public int GetUpgradeLevel(PlayerUpgradeType type)
    {
        switch (type)
        {
            case PlayerUpgradeType.CannonDamage: return cannonDamageLevel;
            case PlayerUpgradeType.CannonTrajectory: return cannonTrajectoryLevel;
            case PlayerUpgradeType.CannonFireRate: return cannonFireRateLevel;
            case PlayerUpgradeType.IceDamage: return iceDamageLevel;
            case PlayerUpgradeType.IceCount: return iceCountLevel;
            case PlayerUpgradeType.IceSlow: return iceSlowLevel;
            case PlayerUpgradeType.AirBombCount: return airBombCountLevel;
            case PlayerUpgradeType.AirBombExplosion: return airBombExplosionLevel;
            case PlayerUpgradeType.AirBombSplit: return airBombSplitLevel;
            case PlayerUpgradeType.DroneDamage: return droneDamageLevel;
            case PlayerUpgradeType.DroneDuration: return droneDurationLevel;
            case PlayerUpgradeType.DroneWidth: return droneWidthLevel;
            case PlayerUpgradeType.LifeRestore: return lifeRestoreLevel;
            default: return 0;
        }
    }

    public void ApplyUpgrade(PlayerUpgradeType type)
    {
        bool wasLocked = false;

        switch (type)
        {
            case PlayerUpgradeType.IceDamage:
            case PlayerUpgradeType.IceCount:
            case PlayerUpgradeType.IceSlow:
                if (!iceUnlocked) { iceUnlocked = true; wasLocked = true; }
                break;
            case PlayerUpgradeType.AirBombCount:
            case PlayerUpgradeType.AirBombExplosion:
            case PlayerUpgradeType.AirBombSplit:
                if (!airBombUnlocked) { airBombUnlocked = true; wasLocked = true; }
                break;
            case PlayerUpgradeType.DroneDamage:
            case PlayerUpgradeType.DroneDuration:
            case PlayerUpgradeType.DroneWidth:
                if (!droneUnlocked) { droneUnlocked = true; wasLocked = true; }
                break;
        }

        if (wasLocked) return;

        switch (type)
        {
            case PlayerUpgradeType.CannonDamage: cannonDamageLevel = Mathf.Min(3, cannonDamageLevel + 1); break;
            case PlayerUpgradeType.CannonTrajectory: cannonTrajectoryLevel = Mathf.Min(3, cannonTrajectoryLevel + 1); break;
            case PlayerUpgradeType.CannonFireRate: cannonFireRateLevel = Mathf.Min(3, cannonFireRateLevel + 1); break;
            case PlayerUpgradeType.IceDamage: iceDamageLevel = Mathf.Min(3, iceDamageLevel + 1); break;
            case PlayerUpgradeType.IceCount: iceCountLevel = Mathf.Min(3, iceCountLevel + 1); break;
            case PlayerUpgradeType.IceSlow: iceSlowLevel = Mathf.Min(3, iceSlowLevel + 1); break;
            case PlayerUpgradeType.AirBombCount: airBombCountLevel = Mathf.Min(3, airBombCountLevel + 1); break;
            case PlayerUpgradeType.AirBombExplosion: airBombExplosionLevel = Mathf.Min(3, airBombExplosionLevel + 1); break;
            case PlayerUpgradeType.AirBombSplit: airBombSplitLevel = Mathf.Min(3, airBombSplitLevel + 1); break;
            case PlayerUpgradeType.DroneDamage: droneDamageLevel = Mathf.Min(3, droneDamageLevel + 1); break;
            case PlayerUpgradeType.DroneDuration: droneDurationLevel = Mathf.Min(3, droneDurationLevel + 1); break;
            case PlayerUpgradeType.DroneWidth: droneWidthLevel = Mathf.Min(3, droneWidthLevel + 1); break;
            case PlayerUpgradeType.LifeRestore:
                var health = GetComponent<Health2D>();
                if (health != null && lifeRestoreLevel == 0)
                {
                    health.currentHealth = Mathf.Min(health.maxHealth, health.currentHealth + 2);
                }
                lifeRestoreLevel = 1;
                break;
        }
    }

    private void UpdateSpriteState()
    {
        if (spriteRenderer == null) return;

        int direction = movement.x > 0.1f ? 1 : movement.x < -0.1f ? -1 : 0;
        if (direction != tiltDirection)
        {
            tiltDirection = direction;
            tiltStartTime = Time.time;
        }

        if (tiltDirection == 0)
        {
            if (idleSprite != null) spriteRenderer.sprite = idleSprite;
            return;
        }

        bool transition = Time.time - tiltStartTime < 0.08f;
        if (tiltDirection > 0)
        {
            spriteRenderer.sprite = transition && rightTransitionSprite != null ? rightTransitionSprite : rightTiltSprite != null ? rightTiltSprite : spriteRenderer.sprite;
        }
        else
        {
            spriteRenderer.sprite = transition && leftTransitionSprite != null ? leftTransitionSprite : leftTiltSprite != null ? leftTiltSprite : spriteRenderer.sprite;
        }
    }

    private float GetCannonInterval()
    {
        return fireRate / (1f + cannonFireRateLevel);
    }

    private int GetCannonDamage()
    {
        return 1 + cannonDamageLevel;
    }

    private float nextCannonSfxTime;

    private void FireCannon()
    {
        if (Time.time >= nextCannonSfxTime)
        {
            nextCannonSfxTime = Time.time + 0.15f;
            SoundHelper.PlayOneShot("EnemyBullet1_417", 0.35f);
        }

        int damage = GetCannonDamage();
        if (cannonTrajectoryLevel == 0)
        {
            SpawnProjectile(Vector2.up, Vector3.zero, damage);
            return;
        }

        if (cannonTrajectoryLevel == 1)
        {
            SpawnProjectile(Vector2.up, Vector3.zero, damage);
            SpawnProjectile(DirectionFromUpAngle(-30f), Vector3.left * 0.18f, damage);
            SpawnProjectile(DirectionFromUpAngle(30f), Vector3.right * 0.18f, damage);
            return;
        }

        if (cannonTrajectoryLevel == 2)
        {
            SpawnProjectile(Vector2.up, Vector3.zero, damage);
            SpawnProjectile(DirectionFromUpAngle(-15f), Vector3.left * 0.12f, damage);
            SpawnProjectile(DirectionFromUpAngle(15f), Vector3.right * 0.12f, damage);
            SpawnProjectile(DirectionFromUpAngle(-35f), Vector3.left * 0.28f, damage);
            SpawnProjectile(DirectionFromUpAngle(35f), Vector3.right * 0.28f, damage);
            return;
        }

        SpawnProjectile(Vector2.up, Vector3.zero, damage);
        SpawnProjectile(DirectionFromUpAngle(-15f), Vector3.left * 0.12f, damage);
        SpawnProjectile(DirectionFromUpAngle(15f), Vector3.right * 0.12f, damage);
        SpawnProjectile(DirectionFromUpAngle(-35f), Vector3.left * 0.28f, damage);
        SpawnProjectile(DirectionFromUpAngle(35f), Vector3.right * 0.28f, damage);
        SpawnProjectile(DirectionFromUpAngle(-50f), Vector3.left * 0.42f, damage);
        SpawnProjectile(DirectionFromUpAngle(50f), Vector3.right * 0.42f, damage);
    }

    private void FireIceMissiles()
    {
        SoundHelper.PlayOneShot("EnemyBullet2_541", 0.4f);

        int count = 2 + iceCountLevel * 2;
        int damage = Mathf.Max(1, Mathf.RoundToInt(GetIceDamage() * 0.8f));
        bool stationaryBonus = stationaryTime >= 2f;
        if (stationaryBonus)
        {
            damage = GetStationaryIceDamage();
        }

        iceVolleyId++;
        for (int i = 0; i < count; i++)
        {
            float spreadX = count == 1 ? 0f : Mathf.Lerp(-2.2f, 2.2f, i / (float)(count - 1));
            float spreadY = -0.6f - Random.Range(0.3f, 1.5f);
            Vector2 spreadTarget = (Vector2)transform.position + new Vector2(spreadX, spreadY);

            GameObject missile = new GameObject("Ice Missile");
            missile.transform.position = transform.position;
            missile.transform.rotation = Quaternion.identity;
            missile.transform.localScale = Vector3.one * (stationaryBonus ? 0.9f : 0.55f);

            var renderer = missile.AddComponent<SpriteRenderer>();
            renderer.sprite = projectileSprite;
            renderer.color = new Color(0.45f, 0.85f, 1f, 1f);
            renderer.sortingOrder = 20;

            var collider = missile.AddComponent<CircleCollider2D>();
            collider.radius = 0.13f;
            collider.isTrigger = true;

            var body2D = missile.AddComponent<Rigidbody2D>();
            body2D.gravityScale = 0f;
            body2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var component = missile.AddComponent<IceMissile2D>();
            component.spreadTarget = spreadTarget;
            component.target = FindEnemyTarget(i);
            component.damage = damage;
            component.volleyId = iceVolleyId;
            ApplyIceSlowValues(component);
        }
    }

    private int GetIceDamage()
    {
        if (iceDamageLevel == 1) return 2;
        if (iceDamageLevel == 2) return 3;
        if (iceDamageLevel == 3) return 4;
        return 1;
    }

    private int GetStationaryIceDamage()
    {
        if (iceDamageLevel == 1) return 2;
        if (iceDamageLevel == 2) return 4;
        if (iceDamageLevel == 3) return 5;
        return 1;
    }

    private void ApplyIceSlowValues(IceMissile2D missile)
    {
        if (iceSlowLevel == 0)
        {
            missile.moveSlowPercent = 0.3f;
            missile.attackSlowPercent = 0.2f;
            missile.slowDuration = 2f;
            missile.vulnerabilityPercent = 0f;
        }
        else if (iceSlowLevel == 1)
        {
            missile.moveSlowPercent = 0.4f;
            missile.attackSlowPercent = 0.3f;
            missile.slowDuration = 2.5f;
            missile.vulnerabilityPercent = 0.2f;
        }
        else if (iceSlowLevel == 2)
        {
            missile.moveSlowPercent = 0.5f;
            missile.attackSlowPercent = 0.4f;
            missile.slowDuration = 3f;
            missile.vulnerabilityPercent = 0.4f;
        }
        else
        {
            missile.moveSlowPercent = 0.6f;
            missile.attackSlowPercent = 0.5f;
            missile.slowDuration = 3.5f;
            missile.vulnerabilityPercent = 0.6f;
        }
    }

    private Transform FindEnemyTarget(int offset)
    {
        Health2D[] healths = Object.FindObjectsOfType<Health2D>();
        int seen = 0;
        Transform fallback = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < healths.Length; i++)
        {
            Health2D health = healths[i];
            if (health == null || health.currentHealth <= 0 || health.GetComponent<PlayerController2D>() != null) continue;

            if (seen == offset) return health.transform;
            seen++;

            float distance = ((Vector2)health.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                fallback = health.transform;
            }
        }

        return fallback;
    }

    private void FireAirBombs()
    {
        SoundHelper.PlayOneShot("EnemyBullet2_541", 0.4f);

        int count = 1 + airBombCountLevel;
        int damage = Mathf.Max(1, Mathf.RoundToInt(GetCannonDamage() * 1.5f));
        for (int i = 0; i < count; i++)
        {
            float offset = count == 1 ? 0f : Mathf.Lerp(-0.45f, 0.45f, i / (float)(count - 1));
            SpawnAirBomb(Vector3.right * offset, damage, false, Vector2.up);
        }
    }

    private void SpawnAirBomb(Vector3 offset, int damage, bool small, Vector2 direction)
    {
        GameObject bomb = new GameObject(small ? "Small Air Bomb" : "Air Bomb");
        bomb.transform.position = transform.position + offset + Vector3.up * 0.75f;
        bomb.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        bomb.transform.localScale = Vector3.one * (small ? 0.55f : 0.85f);

        var renderer = bomb.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite;
        renderer.color = new Color(1f, 0.45f, 0.15f, 1f);
        renderer.sortingOrder = 20;

        var collider = bomb.AddComponent<CircleCollider2D>();
        collider.radius = 0.22f;
        collider.isTrigger = true;

        var body2D = bomb.AddComponent<Rigidbody2D>();
        body2D.gravityScale = 0f;
        body2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var component = bomb.AddComponent<AirBombProjectile2D>();
        component.direction = direction;
        component.speed = 6f;
        component.contactDamage = damage;
        component.explosionLevel = airBombExplosionLevel;
        component.splitLevel = airBombSplitLevel;
        component.sprite = projectileSprite;
        component.owner = gameObject;
    }

    private float GetDroneCooldown()
    {
        return 10f;
    }

    private void SpawnDrones()
    {
        SoundHelper.PlayOneShot("EnemyBullet2_541", 0.4f);

        float duration = 3f + droneDurationLevel;
        float scanAngle = droneDurationLevel == 0 ? 0f : droneDurationLevel == 1 ? 15f : droneDurationLevel == 2 ? 45f : 90f;
        int damage = droneDamageLevel == 0 ? 2 : droneDamageLevel == 1 ? 4 : droneDamageLevel == 2 ? 6 : 8;
        float beamWidth = 0.35f + droneDamageLevel * 0.17f;

        GameObject drone = new GameObject("Pulse Drone");
        drone.transform.position = transform.position;

        var renderer = drone.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite;
        renderer.color = new Color(0.25f, 1f, 1f, 1f);
        renderer.sortingOrder = 19;
        drone.transform.localScale = Vector3.one * 0.5f;

        var component = drone.AddComponent<DroneBeamWeapon2D>();
        component.damage = damage;
        component.duration = duration;
        component.scanAngle = scanAngle;
        component.width = beamWidth;
    }

    private Vector2 DirectionFromUpAngle(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)).normalized;
    }

    private void SpawnProjectile(Vector2 direction, Vector3 offset, int damage)
    {
        Vector3 spawnPosition = transform.position + offset + (Vector3)(direction.normalized * 0.7f);

        GameObject projectile = new GameObject("Player Projectile");
        projectile.transform.position = spawnPosition;
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        projectile.transform.localScale = Vector3.one * (0.35f + damage * 0.15f);

        var renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite;
        renderer.sortingOrder = 20;

        var collider = projectile.AddComponent<CircleCollider2D>();
        collider.radius = 0.14f;
        collider.isTrigger = true;

        var projectileBody = projectile.AddComponent<Rigidbody2D>();
        projectileBody.gravityScale = 0f;
        projectileBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var projectileComponent = projectile.AddComponent<Projectile2D>();
        projectileComponent.direction = direction;
        projectileComponent.speed = projectileSpeed;
        projectileComponent.damage = damage;
        projectileComponent.owner = gameObject;
        projectileComponent.hitPlayers = false;
        projectileComponent.hitEnemies = true;
    }
}
