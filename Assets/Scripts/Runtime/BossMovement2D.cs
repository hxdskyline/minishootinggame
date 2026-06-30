using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossMovement2D : MonoBehaviour
{
    public Transform target;
    public StageFlowController stage;
    public float anchorY = 3.8f;
    public float leftX = -4.5f;
    public float rightX = 4.5f;
    public float moveSpeed = 2.2f;

    private enum Phase
    {
        EnteringTop,
        TopBarrage1,
        TopPause1,
        TopBarrage2,
        TopPause2,
        TopRapidBarrage1,
        TopRapidInterval,
        TopRapidBarrage2,
        TopPause3,
        SpawnChasers,
        ChaserSupportFire,
        DiveToPlayerY,
        BottomContinuous1,
        BottomPause1,
        BottomContinuous2,
        BottomPause2,
        ReturnToTop
    }

    private Rigidbody2D body;
    private EnemyShooter2D shooter;
    private Phase phase;
    private float phaseTimer;
    private float nextSupportFireTime;
    private int burstCounter;
    private float nextBurstShotTime;
    private int horizontalDirection = 1;
    private float diveTargetX;
    private readonly List<Health2D> summonedChasers = new List<Health2D>();

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        shooter = GetComponent<EnemyShooter2D>();
    }

    private void Start()
    {
        phase = Phase.EnteringTop;
    }

    private void FixedUpdate()
    {
        if (phase == Phase.EnteringTop)
        {
            MoveToward(0f, anchorY);
            if (Vector2.Distance(transform.position, new Vector2(0f, anchorY)) <= 0.15f)
            {
                EnterPhase(Phase.TopBarrage1);
            }
            return;
        }

        if (phase == Phase.ReturnToTop)
        {
            MoveToward(0f, anchorY);
            if (Vector2.Distance(transform.position, new Vector2(0f, anchorY)) <= 0.15f)
            {
                EnterPhase(Phase.TopBarrage1);
            }
            return;
        }

        if (phase == Phase.DiveToPlayerY)
        {
            float targetY = GetDiveTargetY();
            MoveToward(diveTargetX, targetY);
            if (Mathf.Abs(transform.position.y - targetY) <= 0.15f)
            {
                EnterPhase(Phase.BottomContinuous1);
            }
            return;
        }
    }

    private void Update()
    {
        switch (phase)
        {
            case Phase.TopBarrage1:
                FireSpread();
                EnterPhase(Phase.TopPause1);
                break;

            case Phase.TopPause1:
                PatrolHorizontal();
                if (Time.time >= phaseTimer) EnterPhase(Phase.TopBarrage2);
                break;

            case Phase.TopBarrage2:
                FireSpread();
                EnterPhase(Phase.TopPause2);
                break;

            case Phase.TopPause2:
                PatrolHorizontal();
                if (Time.time >= phaseTimer) EnterPhase(Phase.TopRapidBarrage1);
                break;

            case Phase.TopRapidBarrage1:
                FireSpread();
                EnterPhase(Phase.TopRapidInterval);
                break;

            case Phase.TopRapidInterval:
                PatrolHorizontal();
                if (Time.time >= phaseTimer) EnterPhase(Phase.TopRapidBarrage2);
                break;

            case Phase.TopRapidBarrage2:
                FireSpread();
                EnterPhase(Phase.TopPause3);
                break;

            case Phase.TopPause3:
                PatrolHorizontal();
                if (Time.time >= phaseTimer) EnterPhase(Phase.SpawnChasers);
                break;

            case Phase.SpawnChasers:
                SpawnTwoChasers();
                EnterPhase(Phase.ChaserSupportFire);
                break;

            case Phase.ChaserSupportFire:
                UpdateChaserSupport();
                break;

            case Phase.BottomContinuous1:
                UpdateContinuousBurst(10);
                break;

            case Phase.BottomPause1:
                if (Time.time >= phaseTimer) EnterPhase(Phase.BottomContinuous2);
                break;

            case Phase.BottomContinuous2:
                UpdateContinuousBurst(10);
                break;

            case Phase.BottomPause2:
                if (Time.time >= phaseTimer) EnterPhase(Phase.ReturnToTop);
                break;
        }

        ClearDeadChasers();
    }

    private void EnterPhase(Phase newPhase)
    {
        phase = newPhase;
        burstCounter = 0;
        nextBurstShotTime = 0f;

        switch (newPhase)
        {
            case Phase.TopPause1:
            case Phase.TopPause2:
                phaseTimer = Time.time + 3f;
                break;
            case Phase.TopRapidInterval:
                phaseTimer = Time.time + 0.5f;
                break;
            case Phase.TopPause3:
                phaseTimer = Time.time + 2f;
                break;
            case Phase.BottomPause1:
                phaseTimer = Time.time + 3f;
                break;
            case Phase.BottomPause2:
                phaseTimer = Time.time + 2f;
                break;
            case Phase.ChaserSupportFire:
                nextSupportFireTime = Time.time + 2f;
                break;
            case Phase.DiveToPlayerY:
                diveTargetX = Random.Range(leftX + 1f, rightX - 1f);
                break;
        }
    }

    private void FireSpread()
    {
        if (shooter == null || target == null) return;

        Vector2 baseDir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        float[] angles = { -50f, -25f, 0f, 25f, 50f };
        for (int i = 0; i < angles.Length; i++)
        {
            float radians = angles[i] * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            Vector2 dir = new Vector2(
                baseDir.x * cos - baseDir.y * sin,
                baseDir.x * sin + baseDir.y * cos
            ).normalized;
            shooter.FireDirected(dir);
        }
    }

    private void PatrolHorizontal()
    {
        Vector2 pos = transform.position;
        if (pos.x >= rightX) horizontalDirection = -1;
        if (pos.x <= leftX) horizontalDirection = 1;
        float step = horizontalDirection * moveSpeed * 0.5f * Time.deltaTime;
        body.MovePosition(new Vector2(pos.x + step, pos.y));
    }

    private void SpawnTwoChasers()
    {
        if (stage == null) return;

        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? transform.position.x - 1.5f : transform.position.x + 1.5f;
            Vector2 pos = new Vector2(x, transform.position.y - 0.5f);
            GameObject enemy = stage.CreateBossChaser(pos);
            if (enemy != null)
            {
                Health2D health = enemy.GetComponent<Health2D>();
                if (health != null) summonedChasers.Add(health);
            }
        }
    }

    private void UpdateChaserSupport()
    {
        bool anyAlive = false;
        for (int i = summonedChasers.Count - 1; i >= 0; i--)
        {
            if (summonedChasers[i] != null && summonedChasers[i].currentHealth > 0)
            {
                anyAlive = true;
                break;
            }
        }

        if (!anyAlive)
        {
            summonedChasers.Clear();
            EnterPhase(Phase.DiveToPlayerY);
            return;
        }

        PatrolHorizontal();
        if (Time.time >= nextSupportFireTime && shooter != null && target != null)
        {
            nextSupportFireTime = Time.time + 2f;
            Vector2 dir1 = ((Vector2)target.position - (Vector2)transform.position).normalized;
            Vector2 dir2 = new Vector2(dir1.x + 0.15f, dir1.y).normalized;
            shooter.FireDirected(dir1);
            shooter.FireDirected(dir2);
        }
    }

    private void UpdateContinuousBurst(int count)
    {
        if (shooter == null || target == null) return;

        if (burstCounter < count && Time.time >= nextBurstShotTime)
        {
            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            shooter.FireDirected(dir);
            burstCounter++;
            nextBurstShotTime = Time.time + shooter.fireInterval;
        }
        else if (burstCounter >= count)
        {
            Phase nextPhase = phase == Phase.BottomContinuous1
                ? Phase.BottomPause1
                : Phase.BottomPause2;
            EnterPhase(nextPhase);
        }
    }

    private void ClearDeadChasers()
    {
        for (int i = summonedChasers.Count - 1; i >= 0; i--)
        {
            if (summonedChasers[i] == null || summonedChasers[i].currentHealth <= 0)
            {
                summonedChasers.RemoveAt(i);
            }
        }
    }

    private void MoveToward(float targetX, float targetY)
    {
        Vector2 delta = new Vector2(targetX - transform.position.x, targetY - transform.position.y);
        if (delta.sqrMagnitude <= 0.0225f)
        {
            return;
        }
        Vector2 step = delta.normalized * (moveSpeed * Time.fixedDeltaTime);
        if (step.sqrMagnitude > delta.sqrMagnitude)
            step = delta;
        body.MovePosition((Vector2)transform.position + step);
    }

    private void MoveVertical(float destinationY)
    {
        float deltaY = destinationY - transform.position.y;
        if (Mathf.Abs(deltaY) <= 0.1f) return;
        float step = Mathf.Sign(deltaY) * moveSpeed * Time.fixedDeltaTime;
        if (Mathf.Abs(step) > Mathf.Abs(deltaY)) step = deltaY;
        body.MovePosition((Vector2)transform.position + Vector2.up * step);
    }

    private float GetDiveTargetY()
    {
        float y = target != null ? target.position.y : 0f;
        return Mathf.Clamp(y, -1.5f, anchorY - 0.6f);
    }
}
