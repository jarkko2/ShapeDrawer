using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereController : MonoBehaviour
{
    [SerializeField] private float connectionDistance = 1.0f;
    public GameObject visualObject;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public Rigidbody rigidBody;
    public List<GameObject> FindNeighbors()
    {
        List<GameObject> neighbors = new List<GameObject>();
        Collider[] nearbyColliders = Physics.OverlapSphere(visualObject.transform.position, connectionDistance);
        foreach (Collider collider in nearbyColliders)
        {
            Debug.Log(collider.name);
            if (collider.transform != visualObject.transform)
            {
                if (collider.transform.GetComponent<CubeVisualController>())
                {
                    neighbors.Add(collider.transform.parent.gameObject);
                }
            }
        }
        return neighbors;
    }
}
