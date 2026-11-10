using UnityEngine;

public class VectorRandomCircleMove : MonoBehaviour
{
    public float MoveSpeed = 1f;
    public float Radius = 0.5f;

    public Transform CenterPoint;

    private Vector3 CurrentTarget;

    private void Awake()
    {
        GetRandomCircleTarget();
    }

    private void Update()
    {
        RandomCircleMove();
    }

    private void RandomCircleMove()
    {
        // The distance to our target
        Vector3 distanceToTarget = CurrentTarget - transform.position;

        // The movement we want to complete this frame
        Vector3 currentMove = distanceToTarget.normalized * MoveSpeed * Time.deltaTime;

        transform.position += currentMove;

        // If our currentMove is larger than distanceToTarget, that means that our movement will overshoot target
        // We should pick a new random target
        if(currentMove.sqrMagnitude > distanceToTarget.sqrMagnitude)
        {
            GetRandomCircleTarget();
        }

        // Draw a ray at the target location
        Debug.DrawRay(CurrentTarget, Vector3.up, Color.orangeRed);
    }

    private void GetRandomCircleTarget()
    {
        Vector2 randomCirclePosition = Random.insideUnitCircle * Radius;

        CurrentTarget = CenterPoint.position + new Vector3(randomCirclePosition.x, 0, randomCirclePosition.y);
    }
}
