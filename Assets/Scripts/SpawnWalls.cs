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

        SpawnCuboid(new Vector3(0, 1f, 5), new Vector3(10, 5, 0.5f), Quaternion.identity); 
        SpawnCuboid(new Vector3(0, 1f, -5), new Vector3(10, 5, 0.5f), Quaternion.identity);
        SpawnCuboid(new Vector3(5, 1f, 0), new Vector3(0.5f, 5, 10), Quaternion.identity);
        SpawnCuboid(new Vector3(-5, 1f, 0), new Vector3(0.5f, 5, 10), Quaternion.identity); 
        SpawnCuboid(new Vector3(0, 3.75f, 0), new Vector3(10.5f, 0.5f, 10.5f), Quaternion.identity, "invisible");
    }
}