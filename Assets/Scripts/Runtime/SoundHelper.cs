using UnityEngine;
using Object = UnityEngine.Object;

public static class SoundHelper
{
    private static SoundHelperRunner runner;

    private static void EnsureRunner()
    {
        if (runner != null) return;
        var go = new GameObject("SoundHelper Runner");
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<SoundHelperRunner>();
    }

    public static void PlayOneShot(string clipName, float volume = 1f)
    {
        AudioClip clip = LoadClip(clipName);
        if (clip == null) return;

        var go = new GameObject("SFX: " + clipName);
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.Play();
        EnsureRunner();
        runner.DelayedDestroy(go, clip.length + 0.1f);
    }

    public static AudioSource PlayMusic(string clipName, bool loop = true, float volume = 0.7f)
    {
        AudioClip clip = LoadClip(clipName);
        if (clip == null) return null;

        var go = new GameObject("Music: " + clipName);
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.volume = volume;
        source.Play();
        return source;
    }

    private static AudioClip LoadClip(string clipName)
    {
        AudioClip clip = Resources.Load<AudioClip>("Audio/" + clipName);
        if (clip != null) return clip;

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets(clipName + " t:AudioClip", new[] { "Assets/Audio" });
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
#endif
        return null;
    }

    private class SoundHelperRunner : MonoBehaviour
    {
        public void DelayedDestroy(GameObject go, float delay)
        {
            StartCoroutine(DestroyAfterDelay(go, delay));
        }

        private System.Collections.IEnumerator DestroyAfterDelay(GameObject go, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (go != null) Destroy(go);
        }
    }
}
