using UnityEngine;

public static class WallFaceMeshUtility
{
    public const int FaceCount = 6;
    private static readonly Vector3[] Normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };

    public static Mesh Create()
    {
        var vertices = new Vector3[24];
        var normals = new Vector3[24];
        var uvs = new Vector2[24];
        var mesh = new Mesh { name = "Six-face wall mesh" };
        for (int face = 0; face < FaceCount; face++)
        {
            Vector3 n = Normals[face];
            Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.Cross(Vector3.up, n);
            Vector3 v = Vector3.Cross(n, u);
            int start = face * 4;
            vertices[start] = (n - u - v) * 0.5f;
            vertices[start + 1] = (n + u - v) * 0.5f;
            vertices[start + 2] = (n + u + v) * 0.5f;
            vertices[start + 3] = (n - u + v) * 0.5f;
            for (int i = 0; i < 4; i++) normals[start + i] = n;
            uvs[start] = Vector2.zero;
            uvs[start + 1] = Vector2.right;
            uvs[start + 2] = Vector2.one;
            uvs[start + 3] = Vector2.up;
        }
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.subMeshCount = FaceCount;
        for (int face = 0; face < FaceCount; face++)
        {
            int i = face * 4;
            mesh.SetTriangles(new[] { i, i + 1, i + 2, i, i + 2, i + 3 }, face);
        }
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    public static int FaceFromLocalNormal(Vector3 normal)
    {
        Vector3 abs = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
        if (abs.x >= abs.y && abs.x >= abs.z) return normal.x >= 0f ? 0 : 1;
        if (abs.y >= abs.z) return normal.y >= 0f ? 2 : 3;
        return normal.z >= 0f ? 4 : 5;
    }
}
