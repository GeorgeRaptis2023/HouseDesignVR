using UnityEngine;

public class SpawnWalls : MonoBehaviour
{
    public GameObject SpawnCuboid(Vector3 position, Vector3 scale, Quaternion rotation, string objectTag = "")
    {
        GameObject cuboid = GameObject.CreatePrimitive(PrimitiveType.Cube);

        cuboid.transform.position = position;
        cuboid.transform.localScale = scale;
        cuboid.transform.rotation = rotation;

        if (!string.IsNullOrEmpty(objectTag))
        {
            cuboid.tag = objectTag;
            if (objectTag == "invisible")
            {
                int layer = LayerMask.NameToLayer("PCInvisible");
                if (layer >= 0)
                {
                    cuboid.layer = layer;
                }
                else
                {
                    Debug.LogError("PCInvisible layer is missing; the PC camera cannot see through this cuboid.", cuboid);
                }
            }
        }

        return cuboid;
    }

    void Start()
    {
        // old house
        // SpawnCuboid(new Vector3(0, 1f, 5), new Vector3(10, 5, 0.5f), Quaternion.identity); 
        // SpawnCuboid(new Vector3(0, 1f, -5), new Vector3(10, 5, 0.5f), Quaternion.identity);
        // SpawnCuboid(new Vector3(5, 1f, 0), new Vector3(0.5f, 5, 10), Quaternion.identity);
        // SpawnCuboid(new Vector3(-5, 1f, 0), new Vector3(0.5f, 5, 10), Quaternion.identity); 
        // SpawnCuboid(new Vector3(0, 3.75f, 0), new Vector3(10.5f, 0.5f, 10.5f), Quaternion.identity, "invisible");
    
        // new house
// Outer walls
    SpawnCuboid(new Vector3(7.63f, 1.5f, 0f), new Vector3(0.2f, 3f, 15.05f), Quaternion.identity);                  // Wall_1
    SpawnCuboid(new Vector3(-7.63f, 1.5f, 0f), new Vector3(0.2f, 3f, 15.05f), Quaternion.identity);                 // Wall_2
    SpawnCuboid(new Vector3(-0.07f, 1.5f, 7.57f), new Vector3(0.2f, 3f, 15.804f), Quaternion.Euler(0f, 90f, 0f));   // Wall_3
    SpawnCuboid(new Vector3(-0.07f, 1.5f, -7.52f), new Vector3(0.2f, 3f, 15.804f), Quaternion.Euler(0f, 90f, 0f));  // Wall_4

    // Interior walls
    SpawnCuboid(new Vector3(-5.08f, 1.5f, -4.43f), new Vector3(0.2f, 3f, 5.145f), Quaternion.Euler(0f, 90f, 0f));   // hall_bathroom
    SpawnCuboid(new Vector3(-5.11f, 1.5f, 0f), new Vector3(0.2f, 3f, 4.913f), Quaternion.Euler(0f, 90f, 0f));       // bedroom_bathroom (1)
    SpawnCuboid(new Vector3(-2.53f, 1.5f, -0.63f), new Vector3(0.2f, 3f, 1.925f), Quaternion.identity);             // bathroom_door
    SpawnCuboid(new Vector3(-2.53f, 1.5f, -3.8f), new Vector3(0.2f, 3f, 1.732f), Quaternion.identity);              // bathroom_door (1)
    SpawnCuboid(new Vector3(-0.23f, 0.75f, -0.55f), new Vector3(0.4f, 1.5f, 4.838f), Quaternion.identity);          // low_wall
    SpawnCuboid(new Vector3(-1.33f, 1.5f, 1.97f), new Vector3(0.2f, 3f, 2.613f), Quaternion.Euler(0f, 90f, 0f));    // middle_thingy
    SpawnCuboid(new Vector3(-1.54f, 1.5f, 4.78f), new Vector3(0.2f, 3f, 5.48f), Quaternion.identity);               // bedroom_livingroom

    // Ceiling (converted from your 1.5-scale Plane to a 15.5m cuboid on the PCInvisible layer)
    SpawnCuboid(new Vector3(0f, 3.1f, 0f), new Vector3(15.5f, 0.2f, 15.5f), Quaternion.identity, "invisible");      // ceiling
    }
}