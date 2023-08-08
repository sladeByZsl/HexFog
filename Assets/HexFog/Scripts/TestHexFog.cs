using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

//[ExecuteAlways]
public class TestHexFog : MonoBehaviour
{
    //public Texture2D _hexMask;
    public Camera m_cam;
    public Mesh m_maskMesh;
    public Material m_mat;
    public RenderTexture m_fogRT;
    public Renderer Plan;

    public Vector2 m_visiblaRange;
    public float m_hexSize;
    private Vector3 m_projCenter;
    private Matrix4x4 m_viewMatrix;
    private CommandBuffer m_cbuffer;

    public MaterialPropertyBlock m_propertyBlock;

    private Vector3[] hexPos = new Vector3[]
    {
        new Vector3(17.32f, 0.00f, 30.00f),
        new Vector3(34.64f, 0.00f, 30.00f),
        new Vector3(51.96f, 0.0f, 30.00f),
        new Vector3(69.28f, 0.00f, 30.00f),
        new Vector3(69.28f, 0.00f, 0.00f),


        new Vector3(86.60f, 0.00f, 0.00f),
        new Vector3(8.66f, 0.00f, 75.00f),
        new Vector3(25.98f, 0.00f, 75.00f),
        new Vector3(34.64f, 0.00f, 60.00f),
        new Vector3(25.98f, 0.00f, 45.00f),
        new Vector3(43.30f, 0.00f, 45.00f),
    };

    // Start is called before the first frame update
    void Start()
    {
        if (m_cbuffer == null)
        {
            m_cbuffer = new CommandBuffer();
        }

        if (m_fogRT == null)
        {
            m_fogRT = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
        }

        Plan.sharedMaterial.SetTexture("_BaseMap", m_fogRT);
        m_cbuffer.name = "DrawFog";
        m_viewMatrix = Matrix4x4.TRS(new Vector3(0, 20, 0), Quaternion.Euler(90, 0, 0), new Vector3(1, 1, -1));
    }

    void Update()
    {
        // DrawHexImmediately();
    }

    public Matrix4x4[] ConverToMatrxs(Vector3[] Positions)
    {
        if (Positions.Length > 0)
        {
            Matrix4x4[] mx = new Matrix4x4[Positions.Length];

            for (int i = 0; i < Positions.Length; i++)
            {
                mx[i] = Matrix4x4.TRS(Positions[i], Quaternion.identity, Vector3.one*m_hexSize );
            }

            return mx;
        }

        return null;
    }

    public void DrawHexImmediately(Vector3[] positions)
    {
        if (m_fogRT == null)
        {
            Debug.LogError("fog rt is null");
            return;
        }

        var matrixes = ConverToMatrxs(positions);
        DrawHexMesh(matrixes,true);
    }


    public bool StopdrawHexAsync;

    public void DrawHexAync2(Vector3[] positions, float[] maskdirection, bool clear = false)
    {
        StartCoroutine(DrawHexAsync(positions, maskdirection));
    }


    IEnumerator DrawHexAsync(Vector3[] positions, float[] maskdirection)
    {
        if (m_fogRT == null)
        {
            Debug.LogError("fog rt is null");
            yield break;
        }

        var matrixes = ConverToMatrxs(positions);
        var _Dissolve = 1f;
        var stop = false;
        var buffer = new float[matrixes.Length];
        if (m_propertyBlock == null)
        {
            m_propertyBlock = new MaterialPropertyBlock();
        }

        m_propertyBlock.Clear();

        while (_Dissolve > -0.1)
        {
            if (StopdrawHexAsync)
            {
                _Dissolve = 1f;
                stop = true;
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = _Dissolve;
            }

            m_propertyBlock.SetFloatArray("_Dissolve", buffer);
            DrawHexMesh(matrixes, false, m_propertyBlock);
            if (stop)
            {
                yield break;
            }

            _Dissolve -= 0.1f;
            Debug.LogError(_Dissolve);
            yield return new WaitForSeconds(1f);
        }
    }


    void DrawHexMesh(Matrix4x4[] matries, bool clear = false, MaterialPropertyBlock properties = null)
    {
        Graphics.SetRenderTarget(m_fogRT);
        if (clear)
        {
            m_cbuffer.ClearRenderTarget(true, true, Color.blue);
        }

        m_cbuffer.SetViewProjectionMatrices(m_viewMatrix.inverse, GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-50, 50, -50, 50, 0, 100), false));
        if (properties != null)
        {
            m_cbuffer.DrawMeshInstanced(m_maskMesh, 0, m_mat, 0, matries, matries.Length, properties);
        }
        else
        {
            //matries = new Matrix4x4[1];
            //matries[0] = Matrix4x4.TRS(  new Vector3(17.32f, 0.00f, 30.00f),quaternion.identity, Vector3.one);
            //m_cbuffer.DrawMeshInstancedProcedural(m_maskMesh,0,m_mat,0,matries.Length);
            m_cbuffer.DrawMeshInstanced(m_maskMesh, 0, m_mat, 0, matries, matries.Length);
        }

        //m_cbuffer.Clear();
        Graphics.ExecuteCommandBuffer(m_cbuffer);
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 120, 80), "Blit"))
        {
            Debug.LogError(m_cam.worldToCameraMatrix);
            Debug.LogError(m_viewMatrix.inverse);
            DrawHexImmediately(hexPos);
        }

        if (GUI.Button(new Rect(130, 10, 120, 80), "Immediately"))
        {
            List<float> dir = new List<float>();
            for (int i=0;i<hexPos.Length;i++)
            {
                dir.Add(3.14f);
            }

            DrawHexAync2(hexPos,dir.ToArray());
            //DrawHexImmediately(new Vector3[] {hexPos[hexPos.Length]});
        }
    }
}