using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SquadChaser2D : MonoBehaviour
{
    public int squadIndex;
    public float followDelay = 0.18f;
    public float descendSpeed = 2.5f;
    public float sAmplitude = 1.8f;
    public float sDuration = 2.8f;
    public int contactDamage = 1;

    private static readonly Dictionary<int, List<Vector2>> squadPaths = new Dictionary<int, List<Vector2>>();
    private static readonly Dictionary<int, int> squadAliveCount = new Dictionary<int, int>();
    private static readonly Dictionary<int, Vector3> squadLastDeathPos = new Dictionary<int, Vector3>();
    private static readonly HashSet<int> cascadingSquads = new HashSet<int>();
    private static int nextSquadId;
    private static readonly float recordInterval = 0.03f;

    public static event System.Action<Vector3> SquadDefeated;

    private Rigidbody2D body;
    private int squadId;
    private float startX;
    private float startTime;
    private float nextRecordTime;
    private enum State { SCurve, Exiting }
    private State state;
    private Vector2 exitDirection;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        startX = transform.position.x;
        startTime = Time.time;

        var health = GetComponent<Health2D>();
        if (health != null) health.Died += OnSquadMemberDied;
    }

    public static int BeginSquad()
    {
        int id = nextSquadId++;
        squadPaths[id] = new List<Vector2>();
        squadAliveCount[id] = 5;
        return id;
    }

    public static void EndSquad(int squadId)
    {
        squadPaths.Remove(squadId);
        squadAliveCount.Remove(squadId);
        squadLastDeathPos.Remove(squadId);
        cascadingSquads.Remove(squadId);
    }

    public static void ResetAllStatics()
    {
        squadPaths.Clear();
        squadAliveCount.Clear();
        squadLastDeathPos.Clear();
        cascadingSquads.Clear();
        nextSquadId = 0;
    }

    public void AssignSquad(int id)
    {
        squadId = id;
    }

    private void FixedUpdate()
    {
        if (squadIndex == 0)
        {
            UpdateLeader();
        }
        else
        {
            UpdateFollower();
        }
    }

    private void UpdateLeader()
    {
        if (state == State.SCurve)
        {
            float elapsed = Time.time - startTime;
            if (elapsed >= sDuration)
            {
                BeginExit();
                return;
            }

            float t = elapsed / sDuration;
            float y = transform.position.y - descendSpeed * Time.fixedDeltaTime;
            float x = startX + Mathf.Sin(t * Mathf.PI * 2f) * sAmplitude * (1f - t);

            Vector2 pos = new Vector2(x, y);
            body.MovePosition(pos);
            RecordPosition(pos);
        }
        else if (state == State.Exiting)
        {
            Vector2 pos = (Vector2)transform.position + exitDirection * (descendSpeed * 1.5f * Time.fixedDeltaTime);
            body.MovePosition(pos);
            RecordPosition(pos);
        }
    }

    private void UpdateFollower()
    {
        if (!squadPaths.TryGetValue(squadId, out List<Vector2> path) || path.Count == 0) return;

        float totalDelay = squadIndex * followDelay;
        int stepsBehind = Mathf.RoundToInt(totalDelay / recordInterval);
        int index = path.Count - 1 - stepsBehind;
        if (index < 0) index = 0;

        body.MovePosition(path[index]);

        if (state == State.SCurve)
        {
            float elapsed = Time.time - startTime;
            if (elapsed >= sDuration + totalDelay)
            {
                state = State.Exiting;
            }
        }
    }

    private void RecordPosition(Vector2 pos)
    {
        if (Time.time < nextRecordTime) return;
        nextRecordTime = Time.time + recordInterval;

        if (squadPaths.TryGetValue(squadId, out List<Vector2> path))
        {
            const int maxRecords = 200;
            if (path.Count >= maxRecords) path.RemoveAt(0);
            path.Add(pos);
        }
    }

    private void BeginExit()
    {
        state = State.Exiting;

        float leftDist = transform.position.x - GetCameraLeft();
        float rightDist = GetCameraRight() - transform.position.x;
        float topDist = GetCameraTop() - transform.position.y;

        if (topDist < leftDist && topDist < rightDist)
            exitDirection = Vector2.up;
        else if (leftDist < rightDist)
            exitDirection = Vector2.left;
        else
            exitDirection = Vector2.right;
    }

    private static float GetCameraLeft()
    {
        Camera cam = Camera.main;
        return cam != null ? cam.transform.position.x - cam.orthographicSize * cam.aspect : -8f;
    }

    private static float GetCameraRight()
    {
        Camera cam = Camera.main;
        return cam != null ? cam.transform.position.x + cam.orthographicSize * cam.aspect : 8f;
    }

    private static float GetCameraTop()
    {
        Camera cam = Camera.main;
        return cam != null ? cam.transform.position.y + cam.orthographicSize : 6f;
    }

    private void OnSquadMemberDied(Health2D health)
    {
        if (!squadAliveCount.TryGetValue(squadId, out int count)) return;

        count--;
        squadAliveCount[squadId] = count;
        squadLastDeathPos[squadId] = transform.position;

        if (count == 4 && !cascadingSquads.Contains(squadId))
        {
            int capturedId = squadId;
            GameObject host = new GameObject("Cascade Host");
            host.AddComponent<CascadeHost>().StartCascade(capturedId);
        }
        else if (count <= 0 && !cascadingSquads.Contains(squadId))
        {
            int capturedId = squadId;
            SquadDefeated?.Invoke(squadLastDeathPos[squadId]);
            GameObject host = new GameObject("Cleanup Host");
            host.AddComponent<CleanupHost>().Init(capturedId);
        }
    }

    private void OnDestroy()
    {
        var health = GetComponent<Health2D>();
        if (health != null) health.Died -= OnSquadMemberDied;
    }

    private class CleanupHost : MonoBehaviour
    {
        private int squadId;
        public void Init(int id) { squadId = id; Invoke(nameof(DelayedCleanup), 1f); }
        private void DelayedCleanup() { EndSquad(squadId); Destroy(gameObject); }
    }

    private class CascadeHost : MonoBehaviour
    {
        public void StartCascade(int squadId)
        {
            cascadingSquads.Add(squadId);
            StartCoroutine(CascadeKillRoutine(squadId));
        }

        private IEnumerator CascadeKillRoutine(int squadId)
        {
            yield return new WaitForSeconds(0.2f);

            SquadChaser2D[] all = FindObjectsOfType<SquadChaser2D>();
            var others = new List<SquadChaser2D>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].squadId == squadId)
                    others.Add(all[i]);
            }

            others.Sort((a, b) => a.squadIndex.CompareTo(b.squadIndex));

            for (int i = 0; i < others.Count; i++)
            {
                if (others[i] != null)
                {
                    float scale = others[i].transform.localScale.x;
                    EnemyDeathEffect2D.Spawn(others[i].transform.position, scale);
                    Destroy(others[i].gameObject);
                    yield return new WaitForSeconds(0.2f);
                }
            }

            if (squadAliveCount.TryGetValue(squadId, out int remaining) && remaining <= 0)
            {
                Vector3 pos = squadLastDeathPos.ContainsKey(squadId) ? squadLastDeathPos[squadId] : transform.position;
                SquadDefeated?.Invoke(pos);
                GameObject host = new GameObject("Cleanup Host");
                host.AddComponent<CleanupHost>().Init(squadId);
            }

            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Health2D health = collision.gameObject.GetComponent<Health2D>();
        if (health == null || collision.gameObject.GetComponent<PlayerController2D>() == null) return;

        health.TakeDamage(contactDamage);
        float scale = transform.localScale.x;
        EnemyDeathEffect2D.Spawn(transform.position, scale);
        Destroy(gameObject);
    }
}
