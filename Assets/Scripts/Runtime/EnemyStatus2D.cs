using UnityEngine;

public class EnemyStatus2D : MonoBehaviour
{
    public bool isBoss;

    private float moveMultiplier = 1f;
    private float attackIntervalMultiplier = 1f;
    private float damageTakenMultiplier = 1f;
    private float slowEndTime;

    public float MoveMultiplier
    {
        get
        {
            Refresh();
            return moveMultiplier;
        }
    }

    public float AttackIntervalMultiplier
    {
        get
        {
            Refresh();
            return attackIntervalMultiplier;
        }
    }

    public float DamageTakenMultiplier
    {
        get
        {
            Refresh();
            return damageTakenMultiplier;
        }
    }

    public void ApplyIceSlow(float moveSlowPercent, float attackSlowPercent, float duration, float vulnerabilityPercent)
    {
        if (isBoss) return;

        moveMultiplier = Mathf.Clamp01(1f - moveSlowPercent);
        attackIntervalMultiplier = 1f + Mathf.Max(0f, attackSlowPercent);
        damageTakenMultiplier = 1f + Mathf.Max(0f, vulnerabilityPercent);
        slowEndTime = Time.time + duration;
    }

    private void Refresh()
    {
        if (slowEndTime > 0f && Time.time >= slowEndTime)
        {
            moveMultiplier = 1f;
            attackIntervalMultiplier = 1f;
            damageTakenMultiplier = 1f;
            slowEndTime = 0f;
        }
    }
}
