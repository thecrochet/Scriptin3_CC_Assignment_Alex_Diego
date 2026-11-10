using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

[RequireComponent (typeof(Rigidbody))]
public class VectorFollow : MonoBehaviour
{
    public Transform FollowTarget;

    public float Force = 1;
    public float SpinDistance = 1;
    public float MaxVelocity = 10;

    // Try changing the force mode to see how it impacts the reactivity and speed!
    public ForceMode ForceMode = ForceMode.Force;

    private Vector3 totalAvoidanceForce = Vector3.zero;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    // Update the follow movement
    private void Update()
    {
        Follow();
    }

    private void Follow()
    {
        // Destination - CurrentPosition = direction towards destination. It gives us a vector that points from currentPosition towards destination
        Vector3 distanceVectorToTarget = FollowTarget.position - transform.position;

        // The direction towards the target
        // You can normalize vectors by placing them under parentheses like this
        Vector3 directionVector = distanceVectorToTarget.normalized;

        // The current distance to target. Magnitude = Vector length
        float distanceToTarget = distanceVectorToTarget.magnitude;

        // The cross product of the directionVector and Vector3.up gives the left side
        // This is essentially the way tornado spins due to the coriolis effect of the spinning earth!
        // More info about the maths behind: https://www.youtube.com/watch?v=eu6i7WJeinw
        // https://www.mathsisfun.com/algebra/vectors-cross-product.html
        // And for those curious about the coriolis effect: https://www.nesdis.noaa.gov/about/k-12-education/atmosphere/what-the-coriolis-effect
        Vector3 sideSpin = Vector3.Cross(directionVector, Vector3.up).normalized;

        // Make the spin force inward by adding it to the direction vector, then normalizing it
        // Basically the average between sideSpin and directionVector
        sideSpin = (sideSpin + directionVector).normalized;

        // Spinning all the time with side spin will actually make everything grow distant from each other.
        // Try it by setting directionToFollow to sideSpin. Even the inward direction isn't enough to keep all close together.
        // To fix this, directionToFollow will lerp between SideSpin and DirectionVector, between spinning around target or going straight to target basically.
        // The closer to the object, the more spinning, the further to the object, the more going straight to target
        // The lerp is done based on distanceToTarget * SpinDistance. I noticed that scaling the distance is interesting to increase/decrease the spin influence. Higher spin distance means much less spin.
        Vector3 directionToFollow = Vector3.Lerp(sideSpin, directionVector, distanceToTarget * SpinDistance);

        // Drawing rays so that the spin and direction are visible
        Debug.DrawRay(transform.position, sideSpin * 0.1f, Color.aquamarine);
        Debug.DrawRay(transform.position, directionToFollow * 0.1f, Color.blue);

        // We add the directionToFollow to the totalAvoidanceForce (Basically average them) then multiply by acceleration
        // For more info on ForceMode: https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Rigidbody.AddForce.html
        _rigidbody.AddForce((directionToFollow + totalAvoidanceForce).normalized * Force, ForceMode);

        // Clamp the maximum velocity so that the points stays close to target
        _rigidbody.linearVelocity = Vector3.ClampMagnitude(_rigidbody.linearVelocity, MaxVelocity);

        // Reset the avoidance velocity
        totalAvoidanceForce = Vector3.zero;
    }

    private void OnTriggerStay(Collider other)
    {
        // Basically, everytime we overlap another target, we add the direction to the avoidanceForce, which gives us the best direction to avoid all surroundings
        // Here, we want to avoid the other collider, so we do currentPosition - otherPosition to get a vector that points AWAY otherPosition
        totalAvoidanceForce += (transform.position - other.transform.position).normalized;
    }
}
