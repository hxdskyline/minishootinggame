using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AirBombProjectile2D : MonoBehaviour
{
    public Vector2 direction = Vector2.up;
    public float speed = 8f;
    public int contactDamage = 2;
    public float lifetime = 3f;
    public int explosionLevel;
    public int splitLevel;
    public bool smallBomb;
    public float smallExplosionMultiplier = 1f;
    public Sprite sprite;
    public GameObject owner;

    private readonly List<Health2D> hitTargets = new List<Health2D>();
    private Rigidbody2D body;
    private bool hasExploded;

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

        Health2D health = other.GetComponent<Health2D>();
        if (health == null || other.GetComponent<PlayerController2D>() != null || hitTargets.Contains(health)) return;

        hitTargets.Add(health);
        health.TakeDamage(ApplyVulnerability(other.gameObject, contactDamage), transform.position);
        float enemyScale = other.transform.localScale.x;
        EnemyDeathEffect2D.Spawn(transform.position, enemyScale * 0.1f);

        if (!smallBomb && splitLevel > 0)
        {
            Split();
        }

        if (explosionLevel > 0 && !hasExploded)
        {
            hasExploded = true;
            Explode();
        }
    }

    private void Split()
    {
        float[] angles = GetSplitAngles();
        float ratio = splitLevel == 1 ? 0.5f : splitLevel == 2 ? 0.6f : 0.7f;

        for (int i = 0; i < angles.Length; i++)
        {
            float rad = angles[i] * Mathf.Deg2Rad;
            Vector2 splitDir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)).normalized;
            SpawnSmallBomb(splitDir, Mathf.Max(1, Mathf.RoundToInt(contactDamage * ratio)));
        }
    }

    private float[] GetSplitAngles()
    {
        switch (splitLevel)
        {
            case 1: return new float[] { -45f, 45f };
            case 2: return new float[] { -50f, -25f, 25f, 50f };
            default: return new float[] { -55f, -35f, -15f, 15f, 35f, 55f };
        }
    }

    private void SpawnSmallBomb(Vector2 splitDirection, int splitDamage)
    {
        GameObject bomb = new GameObject("Split Bomb");
        bomb.transform.position = transform.position;
        bomb.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        bomb.transform.localScale = Vector3.one * 0.45f;

        var renderer = bomb.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(1f, 0.5f, 0.05f, 1f);
        renderer.sortingOrder = 20;

        var collider = bomb.AddComponent<CircleCollider2D>();
        collider.radius = 0.14f;
        collider.isTrigger = true;

        var body2D = bomb.AddComponent<Rigidbody2D>();
        body2D.gravityScale = 0f;
        body2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var component = bomb.AddComponent<AirBombProjectile2D>();
        component.direction = splitDirection;
        component.speed = speed * 1.1f;
        component.contactDamage = splitDamage;
        component.lifetime = 1.8f;
        component.explosionLevel = explosionLevel;
        component.smallBomb = true;
        component.smallExplosionMultiplier = 0.5f;
        component.sprite = sprite;
        component.owner = owner;
    }

    private void Explode()
    {
        float radius = explosionLevel == 1 ? 1f : explosionLevel == 2 ? 1.5f : 2f;
        float damageMultiplier = explosionLevel == 1 ? 0.8f : explosionLevel == 2 ? 1.2f : 1.7f;
        int explosionDamage = Mathf.Max(1, Mathf.RoundToInt(contactDamage * damageMultiplier * smallExplosionMultiplier));

        if (!ExplosionFlash2D.CanSpawnFlash()) return;

        var sharedHits = new HashSet<Health2D>();
        int flashCount = explosionLevel == 1 ? 6 : explosionLevel == 2 ? 10 : 14;
        for (int i = 0; i < flashCount; i++)
        {
            if (!ExplosionFlash2D.CanSpawnFlash()) break;
            float angle = i * Mathf.PI * 2f / flashCount + Random.Range(-0.3f, 0.3f);
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius * Random.Range(0.2f, 1f);
            SpawnFlashBomb(transform.position + offset, explosionDamage, sharedHits);
        }

        if (explosionLevel >= 3 && !smallBomb)
        {
            FireResidue2D.Spawn(transform.position, Mathf.Max(1, Mathf.RoundToInt(contactDamage * 0.5f)), radius, owner);
        }
    }

    private void SpawnFlashBomb(Vector3 position, int damage, HashSet<Health2D> sharedHits = null)
    {
        GameObject flash = new GameObject("Explosion Flash");
        flash.transform.position = position;
        flash.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        flash.transform.localScale = Vector3.one * 0.4f;

        var renderer = flash.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(1f, 0.5f, 0.05f, 0.8f);
        renderer.sortingOrder = 21;

        var collider = flash.AddComponent<CircleCollider2D>();
        collider.radius = 0.25f;
        collider.isTrigger = true;

        var flashComp = flash.AddComponent<ExplosionFlash2D>();
        flashComp.damage = damage;
        flashComp.lifetime = 3f;
        flashComp.owner = owner;
        flashComp.sharedHitTargets = sharedHits;
    }

    private int ApplyVulnerability(GameObject target, int baseDamage)
    {
        EnemyStatus2D status = target.GetComponent<EnemyStatus2D>();
        if (status == null) return baseDamage;
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * status.DamageTakenMultiplier));
    }
}
