using UnityEngine;

public class DestroyWhenOffscreen2D : MonoBehaviour
{
    public float topY = 12f;
    public float bottomY = -12f;
    public float leftX = -12f;
    public float rightX = 12f;

    private void Update()
    {
        Vector3 position = transform.position;
        if (position.y > topY || position.y < bottomY || position.x < leftX || position.x > rightX)
        {
            Destroy(gameObject);
        }
    }
}
