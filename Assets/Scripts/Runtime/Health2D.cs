using System;
using UnityEngine;

public class Health2D : MonoBehaviour
{
    public int maxHealth = 3;
    public int currentHealth = 3;
    public event Action<Health2D> Died;

    private static float lastPlayerDamageSfxTime;
    private static float lastEnemyDamageSfxTime;

    public void TakeDamage(int amount, Vector3? hitPosition = null)
    {
        currentHealth -= amount;
        if (GetComponent<PlayerController2D>() != null)
        {
            if (Time.time >= lastPlayerDamageSfxTime + 0.3f)
            {
                lastPlayerDamageSfxTime = Time.time;
                SoundHelper.PlayOneShot("ImpactPrimordial2_260", 0.5f);
            }
        }
        else
        {
            if (Time.time >= lastEnemyDamageSfxTime + 0.15f)
            {
                lastEnemyDamageSfxTime = Time.time;
                SoundHelper.PlayOneShot("ImpactJar2_569", 0.3f);
            }

            if (hitPosition.HasValue)
            {
                SpawnHitEffect(hitPosition.Value);
            }
        }

        if (currentHealth <= 0)
        {
            Died?.Invoke(this);
            Destroy(gameObject);
        }
    }

    private static void SpawnHitEffect(Vector3 position)
    {
        var go = new GameObject("Hit Effect");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.05f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 28;
        sr.color = new Color(1f, 0.7f, 0.2f, 0.7f);
        var effect = go.AddComponent<HitFlash2D>();
    }

    private class HitFlash2D : MonoBehaviour
    {
        private void Start()
        {
            Sprite sprite = Resources.Load<Sprite>("Effect/baozha1");
            if (sprite == null)
            {
                Texture2D tex = Resources.Load<Texture2D>("Effect/baozha1");
                if (tex != null)
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
#if UNITY_EDITOR
            if (sprite == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("baozha1 t:Sprite", new[] { "Assets/Art/Effect" });
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
#endif
            GetComponent<SpriteRenderer>().sprite = sprite;
            Destroy(gameObject, 0.3f);
        }
    }
}
