using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider))]
public class PaintableWall : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private MeshRenderer _renderer;
    private Mesh _mesh;
    private MaterialPropertyBlock _block;
    private readonly Color[] _colors = new Color[WallFaceMeshUtility.FaceCount];

    public int WallId { get; private set; } = -1;

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _block = new MaterialPropertyBlock();
    }

    public void Initialize(int wallId, Material material)
    {
        WallId = wallId;
        if (_mesh != null) Destroy(_mesh);
        _mesh = WallFaceMeshUtility.Create();
        GetComponent<MeshFilter>().sharedMesh = _mesh;
        Material baseMaterial = material != null ? material : _renderer.sharedMaterial;
        var slots = new Material[WallFaceMeshUtility.FaceCount];
        Color initialColor = baseMaterial.HasProperty(BaseColorId) ? baseMaterial.GetColor(BaseColorId) : Color.white;
        for (int i = 0; i < slots.Length; i++) slots[i] = baseMaterial;
        _renderer.sharedMaterials = slots;
        for (int i = 0; i < slots.Length; i++) ApplyColor(i, initialColor);
    }

    public int FaceFromWorldNormal(Vector3 normal)
    {
        return WallFaceMeshUtility.FaceFromLocalNormal(transform.localToWorldMatrix.transpose.MultiplyVector(normal));
    }

    public Color GetColor(int face)
    {
        return _colors[face];
    }

    public void ApplyColor(int face, Color color)
    {
        if (face < 0 || face >= _colors.Length) return;
        color.a = 1f;
        _colors[face] = color;
        _renderer.GetPropertyBlock(_block, face);
        _block.SetColor(BaseColorId, color);
        _block.SetColor(ColorId, color);
        _renderer.SetPropertyBlock(_block, face);
    }

    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
    }
}
