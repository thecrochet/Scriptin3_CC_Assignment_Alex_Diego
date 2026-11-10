using System;
using UnityEngine;

public class VectorDotView : MonoBehaviour
{
    public Transform target;

    public float MaxViewAngle = 25f;

    private void Update()
    {
        ViewTargetting();
    }

    private void ViewTargetting()
    {
        Vector3 forwardView = transform.forward;
        Vector3 directionToTarget = (target.position - transform.position).normalized;

        Debug.DrawRay(transform.position, forwardView, Color.cyan);

        float dotProduct = Vector3.Dot(forwardView, directionToTarget);

        float degreeAngle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

        Color rayColor = Color.red;

        if(degreeAngle <= MaxViewAngle)
        {
            rayColor = Color.green;
        }

        Debug.DrawRay(transform.position, directionToTarget, rayColor);
    }
}
