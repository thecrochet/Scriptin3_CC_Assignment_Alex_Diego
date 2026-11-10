using System;
using UnityEngine;
using UnityEngine.UIElements;

public class VectorReflection : MonoBehaviour
{
    Ray ray;
    RaycastHit hitInfo = new RaycastHit();

    // Update is called once per frame
    void Update()
    {
        ShootLaser();
    }

    private void ShootLaser()
    {
        ray = new Ray(transform.position, transform.up);

        if(Physics.Raycast(ray, out hitInfo, 100f))
        {
            Debug.DrawRay(hitInfo.point, hitInfo.normal, Color.blue);

            Vector3 distanceToHit = hitInfo.point - transform.position;

            Vector3 reflectionVector = Vector3.Reflect(distanceToHit, hitInfo.normal);

            Debug.DrawRay(transform.position, distanceToHit, Color.red);

            Debug.DrawRay(hitInfo.point, reflectionVector, Color.green);
        }
    }
}
