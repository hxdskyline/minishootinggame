using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnergyFragment2D : MonoBehaviour
{
    public Transform player;
    public StageFlowController stage;
    public float attractDistance = 2.4f;
    public float maxAttractSpeed = 14f;
    public float attractAcceleration = 8f;
    public float driftSpeed = 0.8f;
    public int energyValue = 1;

    private Rigidbody2D body;
    private float currentAttractSpeed;
    private bool isAttracted;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        Destroy(gameObject, 12f);
    }

    private void FixedUpdate()
    {
        if (player != null)
        {
            Vector2 toPlayer = player.position - transform.position;

            if (!isAttracted && toPlayer.sqrMagnitude <= attractDistance * attractDistance)
            {
                isAttracted = true;
            }

            if (isAttracted)
            {
                currentAttractSpeed = Mathf.Min(maxAttractSpeed, currentAttractSpeed + attractAcceleration * Time.fixedDeltaTime);
                body.velocity = toPlayer.normalized * currentAttractSpeed;
                return;
            }
        }

        body.velocity = Vector2.down * driftSpeed;
    }

    private static float lastCollectSfxTime;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController2D>() == null) return;

        if (stage != null)
        {
            stage.CollectEnergy(energyValue);
        }

        if (Time.time >= lastCollectSfxTime + 0.12f)
        {
            lastCollectSfxTime = Time.time;
            SoundHelper.PlayOneShot("ImpactShield_559", 0.45f);
        }

        Destroy(gameObject);
    }
}
