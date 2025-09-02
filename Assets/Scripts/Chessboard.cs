using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chessboard : MonoBehaviour
{
    const int tileConstX = 8;
    const int tileConstY = 8;
    private GameObject[,] tiles;
    [SerializeField] private Material tileMaterial;
    
    void Awake(){
        GenerateTiles(1,tileConstX,tileConstY);
    }

    void GenerateTiles(float tileSize, int tileCountX, int tileCountY)
    {
        tiles = new GameObject[tileConstX, tileConstY];
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
            }
        }
    }

    GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObj = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObj.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObj.AddComponent<MeshFilter>().mesh = mesh;
        tileObj.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, 0, y * tileSize);
        vertices[1] = new Vector3(x * tileSize, 0, (y+1) * tileSize);
        vertices[2] = new Vector3((x+1) * tileSize, 0, y * tileSize);
        vertices[2] = new Vector3((x+1) * tileSize, 0, (y+1) * tileSize);

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };
        
        mesh.vertices = vertices;
        mesh.triangles = tris;

        tileObj.AddComponent<BoxCollider>();
        
        return tileObj;
    }
}
