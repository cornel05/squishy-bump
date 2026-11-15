using UnityEngine;

namespace SquishyBump.Hazards
{
    /// <summary>Flat, upward-facing ring (or disc when inner = 0) in the XZ plane, for procedurally built floors.</summary>
    public static class AnnulusMesh
    {
        public static Mesh Build(float inner, float outer, int segments)
        {
            var vertices = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            for (var i = 0; i <= segments; i++)
            {
                var a = i * Mathf.PI * 2f / segments;
                var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                vertices[i * 2] = d * inner;
                vertices[i * 2 + 1] = d * outer;
                uvs[i * 2] = new Vector2(i / (float)segments, 0f);
                uvs[i * 2 + 1] = new Vector2(i / (float)segments, 1f);
            }
            for (var i = 0; i < segments; i++)
            {
                int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
                // Normal = (b − a) × (c − a) points +y.
                triangles[i * 6 + 0] = a;
                triangles[i * 6 + 1] = b;
                triangles[i * 6 + 2] = c;
                triangles[i * 6 + 3] = b;
                triangles[i * 6 + 4] = d;
                triangles[i * 6 + 5] = c;
            }
            var mesh = new Mesh { name = $"Annulus {inner}-{outer}", vertices = vertices, uv = uvs, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
