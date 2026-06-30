using UnityEngine;

public class EnemyLifeTracker : MonoBehaviour
{
    public StageFlowController stage;
    public float weight = 1f;

    private void OnDestroy()
    {
        if (stage != null) stage.OnEnemyRemoved(weight);
    }
}
