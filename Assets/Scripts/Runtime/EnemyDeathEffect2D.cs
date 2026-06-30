using System.Collections;
using UnityEngine;

public class EnemyDeathEffect2D : MonoBehaviour
{
    public float frameInterval = 0.02f;

    private void Start()
    {
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        for (int i = 1; i <= 7; i++)
        {
            string spriteName = "baozha" + i;
            Sprite frame = LoadSprite(spriteName);
            if (frame != null)
            {
                var sr = GetComponent<SpriteRenderer>();
                sr.sprite = frame;
            }
            yield return new WaitForSecondsRealtime(frameInterval);
        }
        Destroy(gameObject);
    }

    public static void Spawn(Vector3 position, float enemyScale = 0.6f, float sizeMultiplier = 1f)
    {
        var go = new GameObject("Death Effect");
        go.transform.position = position;

        Sprite firstFrame = LoadSprite("baozha1");
        float scale = 0.5f;
        if (firstFrame != null)
        {
            float ppu = firstFrame.pixelsPerUnit > 0f ? firstFrame.pixelsPerUnit : 100f;
            float nativeWidth = firstFrame.rect.width / ppu;
            scale = enemyScale / nativeWidth * sizeMultiplier;
        }
        else
        {
            scale = 0.5f * sizeMultiplier;
        }

        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 30;
        sr.color = new Color(1f, 1f, 1f, 0.6f);
        sr.sprite = firstFrame;
        go.AddComponent<EnemyDeathEffect2D>();
    }

    private static Sprite LoadSprite(string name)
    {
        Sprite sprite = Resources.Load<Sprite>("Effect/" + name);
        if (sprite != null) return sprite;

        Texture2D tex = Resources.Load<Texture2D>("Effect/" + name);
        if (tex != null)
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets(name + " t:Sprite", new[] { "Assets/Art/Effect" });
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        guids = UnityEditor.AssetDatabase.FindAssets(name + " t:Texture2D", new[] { "Assets/Art/Effect" });
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            Texture2D editorTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (editorTex != null)
                return Sprite.Create(editorTex, new Rect(0, 0, editorTex.width, editorTex.height), new Vector2(0.5f, 0.5f));
        }
#endif
        return null;
    }
}
