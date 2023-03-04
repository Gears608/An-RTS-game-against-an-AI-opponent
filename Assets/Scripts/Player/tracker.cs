using UnityEngine;

public class tracker : MonoBehaviour
{
    [SerializeField] //serialized to allow assignment in the editor
    private Transform trackerObject; // initializes the variable which holds the tracker which the camera must follow
    private float speed = 20f; // initalizes the variable which holds the speed at which the camera should move

    void LateUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, trackerObject.position, speed * Time.deltaTime);  //moves the camera to the position of the tracker over time
    }
}
