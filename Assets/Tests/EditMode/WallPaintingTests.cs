using NUnit.Framework;
using UnityEngine;

public class WallPaintingTests
{
    private static System.Type Type(string name)
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name);
            if (type != null) return type;
        }
        throw new System.Exception("Missing type " + name);
    }

    private static Color Wheel(Vector2 point, float value, bool expectedValid = true)
    {
        object[] args = { point, value, Color.clear };
        bool valid = (bool)Type("VRColorWheel").GetMethod("TryGetWheelColor").Invoke(null, args);
        Assert.AreEqual(expectedValid, valid);
        return (Color)args[2];
    }

    [Test]
    public void WheelSupportsHueWhiteGrayBlackAndRejectsOutside()
    {
        Assert.AreEqual(Color.red, Wheel(Vector2.right, 1));
        Assert.AreEqual(Color.white, Wheel(Vector2.zero, 1));
        Assert.AreEqual(new Color(0.5f, 0.5f, 0.5f, 1), Wheel(Vector2.zero, 0.5f));
        Assert.AreEqual(Color.black, Wheel(Vector2.up, 0));
        Wheel(new Vector2(1.1f, 0), 1, false);
        Wheel(new Vector2(float.NaN, 0), 1, false);
        var below = Wheel(new Vector2(0.999f, -0.001f), 1);
        Assert.Greater(below.r, 0.99f);
    }

    [Test]
    public void CubeHasSixOutwardFacesAndUnitBounds()
    {
        var mesh = (Mesh)Type("WallFaceMeshUtility").GetMethod("Create").Invoke(null, null);
        try
        {
            Assert.AreEqual(6, mesh.subMeshCount);
            Assert.AreEqual(24, mesh.vertexCount);
            Assert.AreEqual(Vector3.one, mesh.bounds.size);
            for (int face = 0; face < 6; face++)
            {
                int[] tris = mesh.GetTriangles(face);
                Assert.AreEqual(6, tris.Length);
                Vector3 a = mesh.vertices[tris[0]], b = mesh.vertices[tris[1]], c = mesh.vertices[tris[2]];
                Assert.Greater(Vector3.Dot(Vector3.Cross(b - a, c - a), mesh.normals[tris[0]]), 0.99f);
                Assert.AreEqual(face, (int)Type("WallFaceMeshUtility").GetMethod("FaceFromLocalNormal").Invoke(null, new object[] { mesh.normals[tris[0]] }));
            }
        }
        finally { Object.DestroyImmediate(mesh); }
    }

    [Test]
    public void PaintingOneFaceDoesNotChangeOthersOrSharedMaterial()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", Color.white);
        Mesh mesh = null;
        try
        {
            go.transform.rotation = Quaternion.Euler(20, 50, 10);
            go.transform.localScale = new Vector3(10, 5, 0.5f);
            var wall = go.AddComponent(Type("PaintableWall"));
            Type("PaintableWall").GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(wall, null);
            Type("PaintableWall").GetMethod("Initialize").Invoke(wall, new object[] { 0, material });
            mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Type("PaintableWall").GetMethod("ApplyColor").Invoke(wall, new object[] { 4, Color.blue });
            for (int face = 0; face < 6; face++)
            {
                var block = new MaterialPropertyBlock();
                go.GetComponent<MeshRenderer>().GetPropertyBlock(block, face);
                Assert.AreEqual(face == 4 ? Color.blue : Color.white, block.GetColor("_BaseColor"));
            }
            Assert.AreEqual(Color.white, material.GetColor("_BaseColor"));
            Vector3 worldNormal = go.transform.worldToLocalMatrix.transpose.MultiplyVector(Vector3.forward).normalized;
            Assert.AreEqual(4, Type("PaintableWall").GetMethod("FaceFromWorldNormal").Invoke(wall, new object[] { worldNormal }));
            Assert.AreEqual(Vector3.one, go.GetComponent<BoxCollider>().size);
        }
        finally
        {
            // OnDestroy owns its runtime mesh; detach it for deterministic EditMode cleanup.
            if (mesh != null) Type("PaintableWall").GetField("_mesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(go.GetComponent(Type("PaintableWall")), null);
            Object.DestroyImmediate(go);
            if (mesh != null) Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(material);
        }
    }

    [Test]
    public void NetworkColorsRejectInvalidChannelsAndTransparency()
    {
        var method = Type("NetworkWallPaintState").GetMethod("IsValidColor");
        Assert.IsTrue((bool)method.Invoke(null, new object[] { Color.cyan }));
        Assert.IsFalse((bool)method.Invoke(null, new object[] { new Color(float.NaN, 0, 0, 1) }));
        Assert.IsFalse((bool)method.Invoke(null, new object[] { new Color(2, 0, 0, 1) }));
        Assert.IsFalse((bool)method.Invoke(null, new object[] { new Color(0, 0, 0, 0.5f) }));
    }
}
