using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;

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

    public enum Direction
    {
        Left,
        Right,
        Up,
        Down,
        None
    }

    public Direction cubeDirection = Direction.None;

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
                Debug.Log("Nearby colliders " + nearbyColliders[i].transform.name);
                if (nearbyColliders[i].transform.GetComponent<CubeVisualController>() || nearbyColliders[i].transform.parent?.GetComponent<RootController>() || nearbyColliders[i].transform.name == "Floor")
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
        if (earlierSpawnedObject)
        {
            Vector3 direction = (earlierSpawnedObject.transform.position - sphere.GetComponent<SphereController>().visualObject.transform.position).normalized;
            if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x) && Mathf.Abs(direction.y) > Mathf.Abs(direction.z))
            {
                if (direction.y > 0)
                {
                    Debug.Log("Direction is pointing up.");
                    cubeDirection = Direction.Up;
                }
                else
                {
                    Debug.Log("Direction is pointing down.");
                    cubeDirection = Direction.Down;
                }
            }
            else if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y) && Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
            {
                if (direction.x > 0)
                {
                    Debug.Log("Direction is pointing right.");
                    cubeDirection = Direction.Right;
                }
                else
                {
                    Debug.Log("Direction is pointing left.");
                    cubeDirection = Direction.Left;
                }
            }
            else
            {
                Debug.Log("Direction is not primarily pointing up, down, left, or right.");
            }
        }
        List<Vector3> earlierVertexWorldPositions = new List<Vector3>();
        if (earlierSpawnedObject)
        {
            // Down right 0
            earlierVertexWorldPositions.Add(earlierSpawnedObject.GetComponent<SphereController>().visualObject.transform.TransformPoint(earlierSpawnedObject.transform.GetComponent<SphereController>().meshFilter.mesh.vertices[1]));

            // Up right 1
            earlierVertexWorldPositions.Add(earlierSpawnedObject.GetComponent<SphereController>().visualObject.transform.TransformPoint(earlierSpawnedObject.transform.GetComponent<SphereController>().meshFilter.mesh.vertices[2]));

            // Down left 2
            earlierVertexWorldPositions.Add(earlierSpawnedObject.GetComponent<SphereController>().visualObject.transform.TransformPoint(earlierSpawnedObject.transform.GetComponent<SphereController>().meshFilter.mesh.vertices[0]));

            // Up left 3
            earlierVertexWorldPositions.Add(earlierSpawnedObject.GetComponent<SphereController>().visualObject.transform.TransformPoint(earlierSpawnedObject.transform.GetComponent<SphereController>().meshFilter.mesh.vertices[3]));
        }

        GenerateCube(sphere, earlierVertexWorldPositions, cubeDirection);

        sphere.GetComponent<SphereController>().meshCollider.sharedMesh = sphere.GetComponent<SphereController>().meshFilter.mesh;
        earlierSpawnedHitPoint = hitPoint;
        earlierSpawnedObject = sphere;
    }

    private void GenerateCube(GameObject sphere, List<Vector3> earlierVertexWorldPositions, Direction direction)
    {
        MeshFilter meshFilter = sphere.GetComponent<SphereController>().meshFilter;
        Mesh mesh = new Mesh();
        meshFilter.mesh = mesh;

        Vector3 earlierDownRight = Vector3.zero;
        Vector3 earlierUpRight = Vector3.zero;

        Vector3 earlierDownLeft = Vector3.zero;
        Vector3 earlierUpLeft = Vector3.zero;
        if (earlierVertexWorldPositions.Count > 0)
        {
            earlierDownLeft = sphere.transform.GetComponent<SphereController>().visualObject.transform.InverseTransformPoint(earlierVertexWorldPositions[0]);
            earlierUpLeft = sphere.transform.GetComponent<SphereController>().visualObject.transform.InverseTransformPoint(earlierVertexWorldPositions[1]);
            earlierDownRight = sphere.transform.GetComponent<SphereController>().visualObject.transform.InverseTransformPoint(earlierVertexWorldPositions[2]);
            earlierUpRight = sphere.transform.GetComponent<SphereController>().visualObject.transform.InverseTransformPoint(earlierVertexWorldPositions[3]);
        }

        Vector3[] vertices = new Vector3[8];

        if (direction == Direction.None)
        {
            vertices = new Vector3[]
            {
                // Front
                new Vector3(-0.5f, -0.5f, -0.5f), // Down left
                new Vector3(0.5f, -0.5f, -0.5f), // Down right
                new Vector3(0.5f, 0.5f, -0.5f), // Up Right
                new Vector3(-0.5f, 0.5f, -0.5f), // Up left

                // Back
                new Vector3(-0.5f, -0.5f, 0.5f), // Down left
                new Vector3(0.5f, -0.5f, 0.5f), // Down right
                new Vector3(0.5f, 0.5f, 0.5f), // Up right
                new Vector3(-0.5f, 0.5f, 0.5f), // Up left
            };
        }
        if (direction == Direction.Left)
        {
            vertices = new Vector3[]
            {
                // Front
            new Vector3(earlierDownLeft.x, earlierDownLeft.y, -0.5f), // Down left
            new Vector3(0.5f, -0.5f, -0.5f), // Down right
            new Vector3(0.5f, 0.5f, -0.5f), // Up Right
            new Vector3(earlierUpLeft.x, earlierUpLeft.y, -0.5f), // Up left

            // Back
            new Vector3(earlierDownLeft.x, earlierDownLeft.y, 0.5f), // Down left
            new Vector3(0.5f, -0.5f, 0.5f), // Down right
            new Vector3(0.5f, 0.5f, 0.5f), // Up right
            new Vector3(earlierUpLeft.x, earlierUpLeft.y, 0.5f), // Up left
            };
        }
        if (direction == Direction.Right)
        {
            vertices = new Vector3[]
            {
                // Front
            new Vector3(-0.5f, -0.5f, -0.5f), // Down left
            new Vector3(earlierDownRight.x, earlierDownRight.y, -0.5f), // Down right
            new Vector3(earlierUpRight.x, earlierUpRight.y, -0.5f), // Up Right
            new Vector3(-0.5f, 0.5f, -0.5f), // Up left

            // Back
            new Vector3(-0.5f, -0.5f, 0.5f), // Down left
            new Vector3(earlierDownRight.x, earlierDownRight.y, 0.5f), // Down right
            new Vector3(earlierUpRight.x, earlierUpRight.y, 0.5f), // Up right
            new Vector3(-0.5f, 0.5f, 0.5f), // Up left
            };
        }
        if (direction == Direction.Up)
        {
            vertices = new Vector3[]
            {
                 // Front
                new Vector3(-0.5f, -0.5f, -0.5f), // Down left 0
                new Vector3(0.5f, -0.5f, -0.5f), // Down right 1
                new Vector3(earlierDownLeft.x, earlierDownLeft.y, -0.5f), // Up Right 2
                new Vector3(earlierDownRight.x, earlierDownRight.y, -0.5f), // Up left 3

                // Back
                new Vector3(-0.5f, -0.5f, 0.5f), // Down left
                new Vector3(0.5f, -0.5f, 0.5f), // Down right
                new Vector3(earlierDownLeft.x, earlierDownLeft.y, 0.5f), // Up right
                new Vector3(earlierDownRight.x, earlierDownRight.y, 0.5f), // Up left
            };
        }
        if (direction == Direction.Down)
        {
            vertices = new Vector3[]
            {
               // Front
            new Vector3(earlierUpRight.x, earlierUpRight.y, -0.5f), // Down left
            new Vector3(earlierUpLeft.x, earlierUpLeft.y, -0.5f), // Down right
            new Vector3(0.5f, 0.5f, -0.5f), // Up Right
            new Vector3(-0.5f, 0.5f, -0.5f), // Up left

            // Back
            new Vector3(earlierUpRight.x, earlierUpRight.y, 0.5f), // Down left
            new Vector3(earlierUpLeft.x, earlierUpLeft.y, 0.5f), // Down right
            new Vector3(0.5f, 0.5f, 0.5f), // Up right
            new Vector3(-0.5f, 0.5f, 0.5f), // Up left
            };
        }

        int[] triangles = new int[]
        {
            0, 2, 1, 0, 3, 2,    // Front
            1, 2, 6, 1, 6, 5,    // Right
            5, 6, 7, 5, 7, 4,    // Back
            4, 7, 3, 4, 3, 0,    // Left
            3, 7, 6, 3, 6, 2,    // Top
            4, 0, 1, 4, 1, 5     // Bottom
        };

        Vector3[] normals = new Vector3[]
        {
            Vector3.forward,
            Vector3.forward,
            Vector3.forward,
            Vector3.forward,
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back,
        };

        Vector2[] uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uv;
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
                //earlierSpawnedHitPoint = hit.point;
                //earlierSpawnedObject = root;
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
        cubeDirection = Direction.None;
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

    public void Reset()
    {
        SceneManager.LoadScene(0);
    }

    public void HandleValueChange(string name, decimal value)
    {
        Debug.Log(value);
        switch (name)
        {
            case "mass":
                sphereMass = (float)value;
                break;
            case "breakForce":
                breakForce = (float)value;
                break;
            case "breakTorque":
                breakTorque = (float)value;
                break;
            default:
                break;
        }
    }

    public void ChangeToggleState(string name, bool state)
    {
        switch (name)
        {
            case "solid":
                createSolidObjects = state;
                break;
            default:
                break;
        }
    }
}
