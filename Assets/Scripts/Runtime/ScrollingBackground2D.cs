using UnityEngine;

public class ScrollingBackground2D : MonoBehaviour
{
    public float scrollSpeed = 2f;
    public float tileHeight = 16f;
    public float resetBelowY = -16f;
    public float resetOffsetY = 32f;

    private void Update()
    {
        transform.position += Vector3.down * (scrollSpeed * Time.deltaTime);

        if (transform.position.y <= resetBelowY)
        {
            transform.position += Vector3.up * resetOffsetY;
        }
    }
}
