using System.Collections.Generic;
using UnityEngine;

public class ExplosionFlash2D : MonoBehaviour
{
    public int damage = 1;
    public float lifetime = 3f;
    public GameObject owner;
    public HashSet<Health2D> sharedHitTargets;

    private static int activeFlashCount;
    private const int MaxActiveFlashes = 60;

    private float endTime;
    private bool damageDealt;
    private SpriteRenderer spriteRenderer;

    public static void ResetAllStatics()
    {
        activeFlashCount = 0;
    }

    public static bool CanSpawnFlash()
    {
        return activeFlashCount < MaxActiveFlashes;
    }

    private void Awake()
    {
        activeFlashCount++;
    }

    private void Start()
    {
        endTime = Time.time + lifetime;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (Time.time >= endTime)
        {
            Destroy(gameObject);
            return;
        }

        float t = 1f - (endTime - Time.time) / lifetime;
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = Mathf.Sin(Time.time * 20f) > 0f;
            Color c = spriteRenderer.color;
            c.a = 0.7f * (1f - t);
            spriteRenderer.color = c;
        }
    }

    private void OnDestroy()
    {
        activeFlashCount--;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (damageDealt) return;
        if (owner != null && other.gameObject == owner) return;

        Health2D health = other.GetComponent<Health2D>();
        if (health == null || other.GetComponent<PlayerController2D>() != null) return;

        if (sharedHitTargets != null)
        {
            if (sharedHitTargets.Contains(health)) return;
            sharedHitTargets.Add(health);
        }

        EnemyStatus2D status = other.GetComponent<EnemyStatus2D>();
        int finalDamage = damage;
        if (status != null)
            finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * status.DamageTakenMultiplier));

        health.TakeDamage(finalDamage, health.transform.position);
        float enemyScale = other.transform.localScale.x;
        EnemyDeathEffect2D.Spawn(other.transform.position, enemyScale * 0.1f);
        damageDealt = true;
    }
}
