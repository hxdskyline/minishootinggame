using UnityEngine;

public class FireResidue2D : MonoBehaviour
{
    public int damage = 1;
    public float radius = 1.2f;
    public float tickInterval = 1f;
    public float lifetime = 3f;
    public GameObject owner;

    private float nextTickTime;

    public static void Spawn(Vector3 position, int damage, float radius, GameObject owner)
    {
        GameObject residue = new GameObject("Fire Residue");
        residue.transform.position = position;
        residue.transform.localScale = Vector3.one * radius;

        var renderer = residue.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSprite();
        renderer.color = new Color(1f, 0.35f, 0.05f, 0.35f);
        renderer.sortingOrder = 18;

        var collider = residue.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.isTrigger = true;

        var component = residue.AddComponent<FireResidue2D>();
        component.damage = damage;
        component.radius = radius;
        component.owner = owner;
    }

    private void Awake()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (Time.time < nextTickTime) return;
        nextTickTime = Time.time + tickInterval;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            Health2D health = hits[i].GetComponent<Health2D>();
            if (health == null || hits[i].GetComponent<PlayerController2D>() != null) continue;

            EnemyStatus2D status = hits[i].GetComponent<EnemyStatus2D>();
            int finalDamage = status == null ? damage : Mathf.Max(1, Mathf.RoundToInt(damage * status.DamageTakenMultiplier));
            health.TakeDamage(finalDamage, health.transform.position);
            float enemyScale = hits[i].transform.localScale.x;
            EnemyDeathEffect2D.Spawn(hits[i].transform.position, enemyScale * 0.1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Projectile2D projectile = other.GetComponent<Projectile2D>();
        if (projectile == null || !projectile.hitPlayers) return;

        Destroy(other.gameObject);
        Destroy(gameObject);
    }

    private static Sprite CreateSprite()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float alpha = Mathf.Clamp01(1f - distance);
                pixels[y * size + x] = new Color(1f, 0.35f, 0.05f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
