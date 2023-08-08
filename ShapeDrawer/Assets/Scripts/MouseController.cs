using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class MouseController : MonoBehaviour
{
    [SerializeField] private GameObject spherePrefab;
    [SerializeField] private GameObject rootPrefab;
    [SerializeField] private GameObject cursor;
    public bool drawing = false;
    public Vector3 earlierSpawnedHitPoint = Vector3.zero;
    public GameObject earlierSpawnedObject = null;
    public GameObject currentRoot;

    public List<GameObject> drawedShape = new List<GameObject>();
    [SerializeField] private float drawingOffset = 0.5f;

    [SerializeField] private int solverIterations = 10;
    [SerializeField] private float sphereMass = 1;

    [SerializeField] private float breakForce = 100;
    [SerializeField] private float breakTorque = 100;

    [SerializeField] private bool createSolidObjects = false;

    void Update()
    {
        if (Input.GetMouseButton(0) && !drawing)
        {
            drawing = true;

            if (createSolidObjects)
            {
                CreateRootAtMousePosition();
            }
        }
        if (Input.GetMouseButton(0) && drawing)
        {
            if (!createSolidObjects)
            {
                CreateSphereAtMousePosition(true);
            }
            else
            {
                CreateSphereAtMousePosition(false);
            }
            
        }
        if (!Input.GetMouseButton(0) && drawing)
        {
            
            drawing = false;
            EnablePhysicsOnRigidbody();
            ConnectDrawedShapes();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    private void CreateSphereAtMousePosition(bool useConfigurableJoint)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(hit.point, drawingOffset);

            for (int i = 0; i < nearbyColliders.Length; i++)
            {
                if (nearbyColliders[i].transform.GetComponent<SphereController>() || nearbyColliders[i].transform.parent?.GetComponent<RootController>())
                {
                    return;
                }
            }

            cursor.transform.position = hit.point + hit.normal * 1.0f;

            float distance;
            if (earlierSpawnedObject != null)
            {
                distance = Vector3.Distance(earlierSpawnedObject.transform.position, cursor.transform.position);
            }
            else
            {
                distance = drawingOffset;
            }
            
            while (distance >= drawingOffset)
            {
                Vector3 direction = earlierSpawnedObject == null ? hit.normal : hit.point + hit.normal * 1.0f - earlierSpawnedObject.transform.position;
                Vector3 position = earlierSpawnedObject == null ? hit.point + direction * 1.0f : earlierSpawnedObject.transform.position + direction.normalized * drawingOffset;
                CreateSphere(position, useConfigurableJoint, hit.point);
                distance = Vector3.Distance(earlierSpawnedObject.transform.position, cursor.transform.position);
            }
        }
    }

    private void CreateSphere(Vector3 position, bool useConfigurableJoint, Vector3 hitPoint)
    {
        GameObject sphere = Instantiate(spherePrefab, position, Quaternion.identity);
        if (useConfigurableJoint)
        {
            drawedShape.Add(sphere);
        }
        else
        {
            sphere.transform.SetParent(currentRoot.transform);
            Destroy(sphere.GetComponent<SphereController>().rigidBody);
        }

        earlierSpawnedHitPoint = hitPoint;
        earlierSpawnedObject = sphere;
    }

    private void CreateRootAtMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(hit.point, drawingOffset);

            for (int i = 0; i < nearbyColliders.Length; i++)
            {
                if (nearbyColliders[i].transform.GetComponent<SphereController>())
                {
                    return;
                }
            }

            if (currentRoot == null)
            {
                Vector3 position = hit.point + hit.normal * 1.0f;
                GameObject root = Instantiate(rootPrefab, position, Quaternion.identity);
                earlierSpawnedHitPoint = hit.point;
                earlierSpawnedObject = root;
                currentRoot = root;
            }
        }
    }

    private void ConnectDrawedShapes()
    {
        for (int i = 0; i < drawedShape.Count; i++)
        {
            Rigidbody drawedShapeRigidbody = drawedShape[i].GetComponent<SphereController>().rigidBody;
            drawedShapeRigidbody.isKinematic = false;
            drawedShapeRigidbody.solverIterations = solverIterations;
            drawedShapeRigidbody.mass = sphereMass;

            List<GameObject> neighbors = drawedShape[i].GetComponent<SphereController>().FindNeighbors();
            Debug.Log("Found " + neighbors.Count);
            for (int neighborIndex = 0; neighborIndex < neighbors.Count; neighborIndex++)
            {
                CreateAndConnectConfigurableJoint(neighbors[neighborIndex], drawedShape[i]);
                CreateAndConnectConfigurableJoint(drawedShape[i], neighbors[neighborIndex]);
            }
        }
        earlierSpawnedObject = null;
        earlierSpawnedHitPoint = Vector3.zero;
        drawedShape.Clear();
        
    }

    private void CreateAndConnectConfigurableJoint(GameObject gameObject, GameObject toConnect)
    {
        ConfigurableJoint configurableJoint = gameObject.GetComponent<SphereController>().visualObject.AddComponent<ConfigurableJoint>();
        configurableJoint.connectedBody = toConnect.GetComponent<SphereController>().rigidBody;
        configurableJoint.angularXMotion = ConfigurableJointMotion.Locked;
        configurableJoint.angularYMotion = ConfigurableJointMotion.Locked;
        configurableJoint.angularZMotion = ConfigurableJointMotion.Locked;
        configurableJoint.xMotion = ConfigurableJointMotion.Locked;
        configurableJoint.yMotion = ConfigurableJointMotion.Locked;
        configurableJoint.zMotion = ConfigurableJointMotion.Locked;
        configurableJoint.breakForce = breakForce;
        configurableJoint.breakTorque = breakTorque;
    }

    private void EnablePhysicsOnRigidbody()
    {
        if (currentRoot)
        {
            currentRoot.GetComponent<Rigidbody>().isKinematic = false;
            currentRoot = null;
        } 
    }
}
