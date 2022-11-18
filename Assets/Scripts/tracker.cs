using UnityEngine;

public class tracker : MonoBehaviour
{
    public Transform trackedObject;
    public float updateSpeed = 3f;
    public Vector2 trackingOffset;
    private Vector3 offset;

    private void Start()
    {
        offset = (Vector3)trackingOffset;
        offset.z = transform.position.z - trackedObject.position.z;
    }

    void LateUpdate()
    {
        Vector3 newPos = transform.position;
        newPos.x = trackedObject.position.x;
        newPos.z = trackedObject.position.z;
        newPos.y = trackedObject.position.y;
        transform.position = Vector3.Lerp(transform.position, newPos, updateSpeed * Time.deltaTime);
    }
}
