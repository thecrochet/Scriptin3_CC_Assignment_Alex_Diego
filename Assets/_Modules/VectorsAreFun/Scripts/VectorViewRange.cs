using UnityEngine;

public class VectorViewRange : MonoBehaviour
{
    // The maximum view range. Since it is a cone, it is a radius and the entire range is double
    public float MaxViewDegreeAngle = 45f;

    // The target we are trying to detect
    public Transform ViewTarget;

    // The spotlight and detector material used to update detection state
    public Light SpotLight;
    public Renderer DetectorLight;

    // Colors used for detection states
    public Color NotDetectedColor;
    public Color DetectedColor;

    private void Update()
    {
        CheckViewRange();
    }

    private void CheckViewRange()
    {
        // Distance from the view location to the view target. Don't forget, TargetPosition - CurrentPosition
        Vector3 distanceToViewTarget = ViewTarget.position - transform.position;

        // The dot product can be useful for finding how close 2 vectors are from each others.
        // If vectors are facing the same direction, the result is 1
        // If vectors are facing opposite directions, the result is -1
        // If vectors are facing perpenticular directions, the result is 0
        // If vectors have a 45 degree angle between each other, the result IS NOT 0.5, despite common thinking. Use Acos(dot) * Mathf.Rad2Deg to extract the actual angle from the dot product.
        float dotProduct = Vector3.Dot(distanceToViewTarget.normalized, transform.forward);

        // Extract the angle from the dot product
        // Acos of the dot product = RadianAngle
        // RadianAngle * Mathf.Rad2Deg = DegreeAngle (0-360)
        // DegreeAngle * Mathf.Deg2Rad = RadianAngle
        float dotDegreeAngle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

        // If the angle of both vectors is below MaxViewDegreeAngle, then we are within range
        if (dotDegreeAngle <= MaxViewDegreeAngle)
        {
            //  /!\
            // ALERT!
            SpotLight.color = DetectedColor;
            DetectorLight.material.color = DetectedColor * 10; //Multiplying by 10 here to boost color over the HDR range and make it glow
        }
        else
        {
            //Nothing detected, carry on
            SpotLight.color = NotDetectedColor.linear;
            DetectorLight.material.color = NotDetectedColor * 10; //Multiplying by 10 here to boost color over the HDR range and make it glow
        }

        // Match the spotlight range to the view range
        // The spot angle is double the MaxViewDegreeAngle since MaxViewDegreeAngle is a radius for for SpotLight.spotAngle, it is the actual length of the cone instead
        SpotLight.innerSpotAngle = Mathf.Max(0, MaxViewDegreeAngle * 2 - 5);
        SpotLight.spotAngle = MaxViewDegreeAngle * 2;
    }
}
