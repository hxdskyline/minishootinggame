using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;

    private void LateUpdate()
    {
        if (target == null) return;

        transform.position = new Vector3(target.position.x, target.position.y, -10f);
    }
}
