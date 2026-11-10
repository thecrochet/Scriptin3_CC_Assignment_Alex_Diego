using System.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

public class VectorRotateAroundCenter : MonoBehaviour
{
    public float DegreeRotationPerSecond = 10f;

    public Transform CenterPoint;

    private Vector2 currentRotatingDirection = Vector2.up;

    private void Update()
    {
        RotatePosition();
    }

    private void RotatePosition()
    {
        // To get radian angle, multiply degree angle with Mathf.Deg2Rad
        float radianAngle = DegreeRotationPerSecond * Mathf.Deg2Rad;

        // Rotate the currentRotatingDirection vector using the radian angle, just juuust a little bit this frame
        currentRotatingDirection = Rotate2DVector(currentRotatingDirection, radianAngle * Time.deltaTime);

        // Apply the 2D currentRotatingDirection vector onto the XZ plane
        // That means we will spin around the Y center on the horizontal plane
        transform.position = CenterPoint.position + new Vector3(currentRotatingDirection.x, 0, currentRotatingDirection.y);

        Debug.DrawRay(CenterPoint.position, new Vector3(currentRotatingDirection.x, 0, currentRotatingDirection.y));
    }

    // Read this for more info: https://www.wolfire.com/blog/2010/07/Linear-algebra-for-game-developers-part-3/
    // If you don't want to do this manually, you can do something similar using Quaternion.LookRotation
    private Vector2 Rotate2DVector(Vector2 vector, float radianAngle)
    {
        Vector2 rotatedVector;
        rotatedVector.x = vector.x * Mathf.Cos(radianAngle) - vector.y * Mathf.Sin(radianAngle);
        rotatedVector.y = vector.x * Mathf.Sin(radianAngle) + vector.y * Mathf.Cos(radianAngle);
        return rotatedVector;
    }
}
