using UnityEngine;

public static class RecoveredSpriteLoader
{
    public static Sprite LoadSprite(string assetName, Sprite fallback = null)
    {
        Sprite sprite = Resources.Load<Sprite>("Sprites/" + assetName);
        if (sprite != null) return sprite;

        Texture2D tex = Resources.Load<Texture2D>("Sprites/" + assetName);
        if (tex != null)
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), Mathf.Max(tex.width, tex.height));

        sprite = Resources.Load<Sprite>("Textures/" + assetName);
        if (sprite != null) return sprite;

        tex = Resources.Load<Texture2D>("Textures/" + assetName);
        if (tex != null)
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), Mathf.Max(tex.width, tex.height));

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets(assetName + " t:Sprite", new[] { "Assets/Art/Sprites", "Assets/Art/Textures" });
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
        }

        guids = UnityEditor.AssetDatabase.FindAssets(assetName + " t:Texture2D", new[] { "Assets/Art/Sprites", "Assets/Art/Textures" });
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), Mathf.Max(texture.width, texture.height));
            }
        }
#endif

        return fallback;
    }
}
