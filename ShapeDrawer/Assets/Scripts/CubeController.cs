using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeController : MonoBehaviour
{
    [SerializeField] private float connectionDistance = 1.01f;
    public GameObject visualObject;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public Rigidbody rigidBody;

    public List<GameObject> FindNeighbors()
    {
        List<GameObject> neighbors = new List<GameObject>();

        // Get the bounds of the mesh
        Mesh mesh = meshFilter.mesh;
        Bounds bounds = mesh.bounds;

        // Calculate the size of the overlap box
        Vector3 size = new Vector3(
            bounds.size.x * transform.localScale.x * connectionDistance,
            bounds.size.y * transform.localScale.y * connectionDistance,
            bounds.size.z * transform.localScale.z * connectionDistance
        );

        // Create an array to store the colliders
        Collider[] colliders = new Collider[10];

        // Find all colliders within the overlap box
        int numColliders = Physics.OverlapBoxNonAlloc(
            new Vector3(transform.position.x, transform.position.y, transform.position.z),
            size / 2f,
            colliders,
            transform.rotation
        );

        for (int i = 0; i < numColliders; i++)
        {
            // Check if the collider belongs to a CubeController component
            CubeVisualController cubeController = colliders[i].gameObject.GetComponent<CubeVisualController>();
            if (cubeController == null) continue;

            // Check if the collider belongs to a different cube than the current one
            if (colliders[i].transform != visualObject.transform)
            {
                neighbors.Add(colliders[i].transform.parent.gameObject);
            }
        }

        return neighbors;
    }
}
