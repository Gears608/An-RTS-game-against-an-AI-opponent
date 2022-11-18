using UnityEngine;

public class cameraController : MonoBehaviour
{
    public float cameraSpeed = 20f;
    public float cameraBoarderWidth = 10f;
    public float scrollSpeed = 10f;
    public float minimumZ = 0f;
    public float maximumZ = 100f;

    void Update()
    {
        Vector2 movement = new Vector2(0,0);

        if (Input.GetKey("w") || Input.mousePosition.y >= Screen.height - cameraBoarderWidth)
        {
            movement.y += cameraSpeed;
        }

        if (Input.GetKey("s") || Input.mousePosition.y <= cameraBoarderWidth)
        {
            movement.y -= cameraSpeed;
        }

        if (Input.GetKey("a") || Input.mousePosition.x <= cameraBoarderWidth)
        {
            movement.x -= cameraSpeed;
        }

        if (Input.GetKey("d") || Input.mousePosition.x >= Screen.width - cameraBoarderWidth)
        {
            movement.x += cameraSpeed;
        }

        transform.Translate(movement * Time.deltaTime);

        Vector3 currentPosition = transform.position;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentPosition.z += scroll * scrollSpeed * 1000f * Time.deltaTime;
        currentPosition.z = Mathf.Clamp(currentPosition.z, -maximumZ, -minimumZ);
        transform.position = (currentPosition);
    }
}
