using UnityEngine;

public class VectorReflect : MonoBehaviour
{
    public Transform RayStart;
    public Transform RayEnd;

    public ArrowScaler ArrowPrefab;
    public Transform ArrowParent;

    private ArrowScaler _rayArrow;
    private ArrowScaler _reflectArrow;

    private RaycastHit _hitInfo;

    private void Awake()
    {
        InitializeVectors();
    }

    private void Update()
    {
        UpdateRays();
    }

    // Instantiate the arrows
    private void InitializeVectors()
    {
        _rayArrow = Instantiate(ArrowPrefab, ArrowParent);
        _reflectArrow = Instantiate(ArrowPrefab, ArrowParent);
    }

    //private void UpdateRa

    private void UpdateRays()
    {
        //Calculate the distance from RayStart to RayEnd
        Vector3 distanceToRayEnd = RayEnd.position - RayStart.position;

        //Create a ray using that distance
        Ray raycastRay = new Ray(RayStart.position, distanceToRayEnd.normalized);

        // Try raycasting in the direction of the ray
        if (Physics.Raycast(raycastRay, out _hitInfo))
        {
            // Display the normal of the surface we touched
            Debug.DrawRay(_hitInfo.point, _hitInfo.normal, Color.blue);
            // Display the reflection of the ray on the surface normal
            Debug.DrawRay(_hitInfo.point, Vector3.Reflect(raycastRay.direction, _hitInfo.normal), Color.cyan);

            // Update the arrows
            UpdateRayVector();
            UpdateReflectVector();
        }
    }

    private void UpdateRayVector()
    {
        // Calculate the distance from RayStart to HitPoint.Point (coming from the raycast)
        Vector3 distanceToHitPoint = _hitInfo.point - RayStart.position;

        // Set the arrow position to RayStart
        _rayArrow.transform.position = RayStart.position;

        // Point the arrow towards the hitpoint
        _rayArrow.ArrowVector = distanceToHitPoint;

        // Scale down the arrow for visual look
        _rayArrow.ArrowScale = 0.1f;

        // Multiplying the color above 1 brings the HDR color and bloom
        _rayArrow.ArrowColor = Color.orangeRed * 10;
    }

    private void UpdateReflectVector()
    {
        // Take in the ray vector and reflect it based on the surface normal we touched
        Vector3 reflectionVector = Vector3.Reflect(_rayArrow.ArrowVector, _hitInfo.normal);

        // Move the reflect arrow to hitInfo.point
        _reflectArrow.transform.position = _hitInfo.point;

        // Set the reflect arrow to point in the direction of the reflection
        _reflectArrow.ArrowVector = reflectionVector;

        // Scale down the arrow for visual look
        _reflectArrow.ArrowScale = 0.1f;

        // Multiplying the color above 1 brings the HDR color and bloom
        _reflectArrow.ArrowColor = Color.cyan * 10;
    }
}
