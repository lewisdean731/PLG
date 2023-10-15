using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData generateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail, bool useFlatShading)
    {
        int meshSimplificationIncrement = TerrainMetrics.lods[levelOfDetail];
        int lodDifference = TerrainMetrics.highestLod - meshSimplificationIncrement;

        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys); // AnimationCurve goes all funny when accessed by multiple threads; give each thread its own one here
        int borderedSize = heightMap.GetLength(0) - (2 * TerrainMetrics.highestLod) + (2 * meshSimplificationIncrement);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;
        float topLeftX = (meshSizeUnsimplified - 1) / -2f; // negative value to get left most position
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1; // ensures number of verts will be correct at different LOD levels

        MeshData meshData = new MeshData(verticesPerLine, useFlatShading);
        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;
        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                if(isBorderVertex)
                {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for(int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndicesMap[x, y];
                float vertHeight = heightCurve.Evaluate(heightMap[x + lodDifference, y + lodDifference]) * heightMultiplier;
                Vector2 percent = new Vector2((x) / (float)(meshSizeUnsimplified), (y) / (float)(meshSizeUnsimplified));
                Vector2 percentUV = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);
                //Vector2 percent = new Vector2((x - meshSimplificationIncrement)/(float)meshSize, (y - meshSimplificationIncrement)/(float)meshSize);
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, vertHeight, topLeftZ - percent.y * meshSizeUnsimplified);

                meshData.addVertex(vertexPosition, percent, vertexIndex);

                if(x < borderedSize - 1 && y < borderedSize - 1) // ignore right and bottom edge vertices
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];
                    // top left, bottom right, bottom left
                    meshData.AddTriangle(a,d,c);
                    // bottom right, top left, top right
                    meshData.AddTriangle(d,a,b);
                }

                vertexIndex += 1;
            }
        }

        meshData.finalise();

        return meshData;
    }
}

public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    Vector3[] bakedNormals;

    Vector3[] borderVertices;
    int[] borderTriangles;

    int triangleIndex;
    int borderTriangleIndex;

    bool useFlatShading;
    public MeshData(int verticesPerLine, bool useFlatShading)
    {
        this.useFlatShading = useFlatShading;
        int meshWidth = verticesPerLine;
        int meshHeight = verticesPerLine;
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6]; // *6 because each square is made up of 2 triangles of 3 verts each

        borderVertices = new Vector3[verticesPerLine * 4 + 4]; // * per side + corners
        borderTriangles = new int[24 * verticesPerLine]; // 24 == (2 tris * number of sides) 
    }

    public void addVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if(vertexIndex < 0)
        {
            borderVertices[-vertexIndex - 1] = vertexPosition;

        }
        else
        {
            vertices[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    Vector3[] calculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;
        for(int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int VertexIndexA = triangles[normalTriangleIndex];
            int VertexIndexB = triangles[normalTriangleIndex + 1];
            int VertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = surfaceNormalFromIndices(VertexIndexA, VertexIndexB, VertexIndexC);
            vertexNormals[VertexIndexA] += triangleNormal;
            vertexNormals[VertexIndexB] += triangleNormal;
            vertexNormals[VertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int VertexIndexA = borderTriangles[normalTriangleIndex];
            int VertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int VertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = surfaceNormalFromIndices(VertexIndexA, VertexIndexB, VertexIndexC);
            if(VertexIndexA >= 0)
            {
                vertexNormals[VertexIndexA] += triangleNormal;
            }
            if (VertexIndexB >= 0)
            {
                vertexNormals[VertexIndexB] += triangleNormal;
            }
            if (VertexIndexC >= 0)
            {
                vertexNormals[VertexIndexC] += triangleNormal;
            }
        }

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    Vector3 surfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA -1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void finalise()
    {
        if (useFlatShading)
        {
            flatShading();
        }
        else
        {
            bakeNormals();
        }
    }

     void bakeNormals()
    {
        bakedNormals = calculateNormals();
    }

    void flatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUVs = new Vector2[triangles.Length];
        for(int i = 0; i < triangles.Length; i++)
        {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUVs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uvs = flatShadedUVs;
    }

    public Mesh createMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        if (useFlatShading)
        {
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.normals = bakedNormals;
        }

        return mesh;
    }
}
