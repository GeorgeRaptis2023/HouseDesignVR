using UnityEngine;

public class SpawnWalls : MonoBehaviour
{
    [SerializeField, Tooltip("Base material for independently paintable room walls.")]
    private Material _wallPaintMaterial;

    private readonly System.Collections.Generic.List<PaintableWall> _paintableWalls = new System.Collections.Generic.List<PaintableWall>(4);
    public System.Collections.Generic.IReadOnlyList<PaintableWall> PaintableWalls => _paintableWalls;
    public event System.Action WallsReady;

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

        RegisterWall(SpawnCuboid(new Vector3(0, 1f, 5), new Vector3(10, 5, 0.5f), Quaternion.identity), 0);
        RegisterWall(SpawnCuboid(new Vector3(0, 1f, -5), new Vector3(10, 5, 0.5f), Quaternion.identity), 1);
        RegisterWall(SpawnCuboid(new Vector3(5, 1f, 0), new Vector3(0.5f, 5, 10), Quaternion.identity), 2);
        RegisterWall(SpawnCuboid(new Vector3(-5, 1f, 0), new Vector3(0.5f, 5, 10), Quaternion.identity), 3);
        SpawnCuboid(new Vector3(0, 3.75f, 0), new Vector3(10.5f, 0.5f, 10.5f), Quaternion.identity, "invisible");
        WallsReady?.Invoke();
    }

    private void RegisterWall(GameObject wallObject, int wallId)
    {
        wallObject.name = "PaintableWall_" + wallId;
        var wall = wallObject.AddComponent<PaintableWall>();
        wall.Initialize(wallId, _wallPaintMaterial);
        _paintableWalls.Add(wall);
    }
}