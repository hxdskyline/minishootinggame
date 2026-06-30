using System.Collections.Generic;
using UnityEngine;

public class DroneBeamWeapon2D : MonoBehaviour
{
    public int damage = 1;
    public float duration = 3f;
    public float hitCooldown = 0.35f;
    public float scanAngle = 45f;
    public float length = 20f;
    public float width = 0.35f;

    private LineRenderer line;
    private float endTime;
    private float baseAngle;
    private readonly Dictionary<int, float> lastHitTimes = new Dictionary<int, float>();

    private void Awake()
    {
        endTime = Time.time + duration;
        baseAngle = FindCenterAxisEnemyAngle();
    }

    private void Start()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = width;
        line.endWidth = width * 0.4f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(0.25f, 0.9f, 1f, 0.85f);
        line.endColor = new Color(0.25f, 0.9f, 1f, 0.05f);
        line.sortingOrder = 25;
    }

    private void Update()
    {
        if (Time.time >= endTime)
        {
            Destroy(gameObject);
            return;
        }

        float sweep = Mathf.Sin(Time.time * 2.4f) * scanAngle;
        Vector2 direction = DirectionFromUpAngle(baseAngle + sweep);
        Vector3 end = transform.position + (Vector3)(direction * length);
        line.SetPosition(0, transform.position);
        line.SetPosition(1, end);

        DamageAlongBeam(direction);
    }

    private void DamageAlongBeam(Vector2 direction)
    {
        Vector2 origin = transform.position;
        float checkRadius = width * 0.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin + direction * (length * 0.5f), checkRadius + length * 0.5f);

        for (int i = 0; i < hits.Length; i++)
        {
            Health2D health = hits[i].GetComponent<Health2D>();
            if (health == null || hits[i].GetComponent<PlayerController2D>() != null) continue;

            int id = hits[i].GetInstanceID();
            if (lastHitTimes.TryGetValue(id, out float last) && Time.time - last < hitCooldown) continue;

            Vector2 toTarget = (Vector2)hits[i].transform.position - origin;
            float projectedDist = Vector2.Dot(toTarget, direction);
            if (projectedDist < 0f || projectedDist > length) continue;

            float perpDist = (toTarget - direction * projectedDist).magnitude;
            if (perpDist > checkRadius + 0.3f) continue;

            lastHitTimes[id] = Time.time;

            EnemyStatus2D status = hits[i].GetComponent<EnemyStatus2D>();
            int finalDamage = damage;
            if (status != null)
                finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * status.DamageTakenMultiplier));

            health.TakeDamage(finalDamage, health.transform.position);
            float enemyScale = hits[i].transform.localScale.x;
            EnemyDeathEffect2D.Spawn(hits[i].transform.position, enemyScale * 0.1f);
        }
    }

    private Vector2 DirectionFromUpAngle(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)).normalized;
    }

    private float FindCenterAxisEnemyAngle()
    {
        Health2D[] healths = Object.FindObjectsOfType<Health2D>();
        Transform best = null;
        float bestAbsX = float.MaxValue;

        for (int i = 0; i < healths.Length; i++)
        {
            Health2D health = healths[i];
            if (health == null || health.currentHealth <= 0 || health.GetComponent<PlayerController2D>() != null) continue;

            float absX = Mathf.Abs(health.transform.position.x);
            if (absX < bestAbsX)
            {
                bestAbsX = absX;
                best = health.transform;
            }
        }

        if (best == null) return 0f;

        Vector2 toEnemy = (Vector2)best.position - (Vector2)transform.position;
        float angle = Mathf.Atan2(toEnemy.x, toEnemy.y) * Mathf.Rad2Deg;
        return Mathf.Clamp(angle, -scanAngle, scanAngle);
    }
}
