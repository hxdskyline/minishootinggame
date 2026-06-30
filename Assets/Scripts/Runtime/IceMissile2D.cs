using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class IceMissile2D : MonoBehaviour
{
    public Transform target;
    public float homingSpeed = 7f;
    public int damage = 1;
    public float lifetime = 4f;
    public float moveSlowPercent = 0.3f;
    public float attackSlowPercent = 0.2f;
    public float slowDuration = 2f;
    public float vulnerabilityPercent;
    public int volleyId;

    public Vector2 spreadTarget;
    public float expandDuration = 0.45f;

    private static readonly Dictionary<int, List<Health2D>> volleyHits = new Dictionary<int, List<Health2D>>();

    public static void ResetAllStatics()
    {
        volleyHits.Clear();
    }
    private Rigidbody2D body;
    private Vector2 spawnPosition;
    private float expandProgress;
    private Vector2 previousPosition;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spawnPosition = transform.position;
        previousPosition = spawnPosition;
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        body.velocity = Vector2.zero;

        if (expandProgress < 1f)
        {
            expandProgress += Time.fixedDeltaTime / expandDuration;
            if (expandProgress >= 1f)
            {
                expandProgress = 1f;
                body.MovePosition(spreadTarget);
            }
            else
            {
                float t = EaseOutCubic(expandProgress);
                Vector2 position = Vector2.Lerp(spawnPosition, spreadTarget, t);
                body.MovePosition(position);
            }

            FaceMovement();
            previousPosition = transform.position;
            return;
        }

        if (target == null)
        {
            target = FindTarget();
        }

        Vector2 direction = target != null
            ? ((Vector2)target.position - (Vector2)transform.position).normalized
            : Vector2.up;

        body.velocity = direction * homingSpeed;
        body.rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private void FaceMovement()
    {
        Vector2 delta = (Vector2)transform.position - previousPosition;
        if (delta.sqrMagnitude > 0.0001f)
        {
            body.rotation = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        }
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private Transform FindTarget()
    {
        Health2D[] healths = Object.FindObjectsOfType<Health2D>();
        Transform best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < healths.Length; i++)
        {
            Health2D health = healths[i];
            if (health == null || health.currentHealth <= 0 || health.GetComponent<PlayerController2D>() != null) continue;

            float distance = ((Vector2)health.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = health.transform;
            }
        }

        return best;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Health2D health = other.GetComponent<Health2D>();
        if (health == null || other.GetComponent<PlayerController2D>() != null) return;

        int finalDamage = damage;
        if (volleyId != 0)
        {
            if (!volleyHits.TryGetValue(volleyId, out List<Health2D> hits))
            {
                hits = new List<Health2D>();
                volleyHits.Add(volleyId, hits);
            }
            else if (hits.Contains(health))
            {
                finalDamage = Mathf.Max(1, Mathf.CeilToInt(finalDamage * 0.5f));
            }

            if (!hits.Contains(health)) hits.Add(health);
        }

        EnemyStatus2D status = other.GetComponent<EnemyStatus2D>();
        if (status != null)
        {
            status.ApplyIceSlow(moveSlowPercent, attackSlowPercent, slowDuration, vulnerabilityPercent);
            finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * status.DamageTakenMultiplier));
        }

        health.TakeDamage(finalDamage, transform.position);
        float enemyScale = other.transform.localScale.x;
        EnemyDeathEffect2D.Spawn(other.transform.position, enemyScale * 0.1f);
        Destroy(gameObject);
    }
}
