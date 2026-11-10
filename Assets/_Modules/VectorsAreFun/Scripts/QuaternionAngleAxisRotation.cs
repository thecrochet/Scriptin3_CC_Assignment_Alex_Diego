using UnityEngine;

public class QuaternionAngleAxisRotation : MonoBehaviour
{
    public float DegreeRotationPerSecond = 22.5f;

    public void Update()
    {
        Turn();
    }

    private void Turn()
    {
        // We rotate around ourselves, using our up axis as the rotation axis
        // Simple! Or is it?
        // Notice the *= ?
        // Quaternions cannot be added together, but you can multiply them together to have the same effect as adding vectors together!
        transform.rotation *= Quaternion.AngleAxis(DegreeRotationPerSecond * Time.deltaTime, transform.up);
    }
}
