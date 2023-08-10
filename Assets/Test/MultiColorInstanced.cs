using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiColorInstanced : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public int instanceCount = 10;
    public Vector3 startPosition = new Vector3(-5, 0, 0);
    public Vector3 offset = new Vector3(2, 0, 0);

    private Matrix4x4[] matrices;
    private MaterialPropertyBlock propertyBlock;
    private Vector4[] colors;
    void Start()
    {
        matrices = new Matrix4x4[instanceCount];
        propertyBlock = new MaterialPropertyBlock();
        colors = new Vector4[instanceCount];

        for (int i = 0; i < instanceCount; i++)
        {
            matrices[i] = Matrix4x4.TRS(startPosition + offset * i, Quaternion.identity, Vector3.one);
            colors[i] = new Vector4(Random.value, Random.value, Random.value, 1);
            //Debug.LogError(colors[i]);
        }
        propertyBlock.SetVectorArray("_BaseColor", colors);
    }

    // Update is called once per frame
    void Update()
    {
        Graphics.DrawMeshInstanced(mesh, 0, material, matrices, instanceCount, propertyBlock);
    }
}
