using UnityEngine;

[RequireComponent(typeof(Unity.Netcode.NetworkObject))]
public class NetworkWallPaintState : Unity.Netcode.NetworkBehaviour
{
    public const int WallCount = 4;
    [SerializeField, Tooltip("Generator for the same deterministic room on each peer.")]
    private SpawnWalls _wallSpawner;
    [SerializeField, Tooltip("Maximum accepted controller-to-wall paint distance.")]
    private float _maxPaintDistance = 8f;

    private Unity.Netcode.NetworkList<Color> _faceColors;
    public static NetworkWallPaintState Instance { get; private set; }
    public int ColorCount => _faceColors.Count;
    public float MaxPaintDistance => _maxPaintDistance;

    private void Awake()
    {
        Instance = this;
        _faceColors = new Unity.Netcode.NetworkList<Color>();
    }

    public override void OnNetworkSpawn()
    {
        _wallSpawner.WallsReady += OnWallsReady;
        _faceColors.OnListChanged += OnColorsChanged;
        OnWallsReady();
    }

    private void OnWallsReady()
    {
        if (!IsSpawned || _wallSpawner.PaintableWalls.Count != WallCount) return;
        if (IsServer && _faceColors.Count == 0)
        {
            for (int wall = 0; wall < WallCount; wall++)
                for (int face = 0; face < WallFaceMeshUtility.FaceCount; face++)
                    _faceColors.Add(_wallSpawner.PaintableWalls[wall].GetColor(face));
        }
        ReplayColors();
    }

    private void OnColorsChanged(Unity.Netcode.NetworkListEvent<Color> change)
    {
        ReplayColors();
    }

    private void ReplayColors()
    {
        if (_wallSpawner.PaintableWalls.Count != WallCount) return;
        for (int i = 0; i < _faceColors.Count; i++)
            _wallSpawner.PaintableWalls[i / WallFaceMeshUtility.FaceCount].ApplyColor(i % WallFaceMeshUtility.FaceCount, _faceColors[i]);
    }

    public Color GetFaceColor(int wallId, int face)
    {
        return _faceColors[wallId * WallFaceMeshUtility.FaceCount + face];
    }

    public static bool IsValidColor(Color color)
    {
        return IsUnitChannel(color.r) && IsUnitChannel(color.g) && IsUnitChannel(color.b) && color.a == 1f;
    }

    private static bool IsUnitChannel(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;
    }

    public bool TryApplyPaint(int wallId, int face, Color color, Vector3 origin, Vector3 direction)
    {
        if (!IsServer || !IsSpawned || _faceColors.Count != WallCount * WallFaceMeshUtility.FaceCount ||
            wallId < 0 || wallId >= WallCount || face < 0 || face >= WallFaceMeshUtility.FaceCount || !IsValidColor(color)) return false;
        if (!IsFinite(origin) || !IsFinite(direction) || direction.sqrMagnitude < 0.9f || direction.sqrMagnitude > 1.1f) return false;
        // Recast on the server: clients cannot paint through intervening walls or arbitrary face IDs.
        if (!Physics.Raycast(origin, direction, out RaycastHit hit, _maxPaintDistance, ~0, QueryTriggerInteraction.Ignore)) return false;
        var target = hit.collider.GetComponent<PaintableWall>();
        if (target == null || target.WallId != wallId || target.FaceFromWorldNormal(hit.normal) != face) return false;
        _faceColors[wallId * WallFaceMeshUtility.FaceCount + face] = color;
        return true;
    }

    public static bool IsFinite(Vector3 vector)
    {
        return !float.IsNaN(vector.x) && !float.IsInfinity(vector.x) &&
            !float.IsNaN(vector.y) && !float.IsInfinity(vector.y) &&
            !float.IsNaN(vector.z) && !float.IsInfinity(vector.z);
    }

    public override void OnNetworkDespawn()
    {
        _wallSpawner.WallsReady -= OnWallsReady;
        _faceColors.OnListChanged -= OnColorsChanged;
    }

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_wallSpawner != null) _wallSpawner.WallsReady -= OnWallsReady;
        base.OnDestroy();
    }
}
