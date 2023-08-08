using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereController : MonoBehaviour
{
    [SerializeField] private float connectionDistance = 1.0f;
    public List<GameObject> FindNeighbors()
    {
        List<GameObject> neighbors = new List<GameObject>();
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, connectionDistance);
        foreach (Collider collider in nearbyColliders)
        {
            if (collider.transform != transform)
            {
                if (collider.transform.GetComponent<SphereController>())
                {
                    neighbors.Add(collider.gameObject);
                }
            }
        }
        return neighbors;
    }
}
