using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TestHexFog : MonoBehaviour
{
    [Header("六边形遮罩的mesh")] public Mesh hexMesh;

    [Header("迷雾的材质球")] public Material fogMaterial;

    [Header("迷雾RT")] public RenderTexture fogRT;

    [Header("迷雾RT的Size")] public Vector2Int fogRTSize = new Vector2Int(256, 256);


    [Header("地表")] public Renderer planeRender;

    [Header("迷雾溶解时间")] public float disovleTime = 0.1f;
    
    
    //地表shader参数
    private static readonly int baseMap = Shader.PropertyToID("_BaseMap");

    #region 视野矩阵

    public Vector3 viewTransform = new Vector3(0, 20, 0);
    private static readonly Quaternion viewQuaternion = Quaternion.Euler(90, 0, 0);
    private static readonly Vector3 viewScale = new Vector3(1, 1, -1);

    private static readonly Color FogColor0 = Color.black;
    private static readonly Color FogColor1 = Color.green;
    private static readonly Color FogColor2 = Color.blue;

    #endregion

    #region 正交投影

    public float fogWidth;
    public float fogHeight;
    private float viewLeft = -50.0f;
    private float viewRight = 50.0f;
    private float viewBottom = -50.0f;

    private float viewTop = 50.0f;
    // public float viewNear = 0f;
    // public float viewFar = 100.0f;

    #endregion

    private const string HexFogCbufferName = "DrawFog";

    public Vector2 m_visiblaRange;
    public float m_hexSize;
    private Vector3 m_projCenter;
    private Matrix4x4 m_viewMatrix;
    private CommandBuffer m_cbuffer;

    public MaterialPropertyBlock m_propertyBlock;

    private Vector3[] hexPos = new Vector3[]
    {
        new Vector3(0,0,0)
        // new Vector3(17.32f, 0.00f, 30.00f),
        // new Vector3(34.64f, 0.00f, 30.00f),
        // new Vector3(51.96f, 0.0f, 30.00f),
        // new Vector3(69.28f, 0.00f, 30.00f),
        // new Vector3(69.28f, 0.00f, 0.00f),
        //
        //
        // new Vector3(86.60f, 0.00f, 0.00f),
        // new Vector3(8.66f, 0.00f, 75.00f),
        // new Vector3(25.98f, 0.00f, 75.00f),
        // new Vector3(34.64f, 0.00f, 60.00f),
        // new Vector3(25.98f, 0.00f, 45.00f),
        // new Vector3(43.30f, 0.00f, 45.00f),
    };

    void Start()
    {
        if (m_cbuffer == null)
        {
            m_cbuffer = new CommandBuffer();
        }

        if (fogRT == null)
        {
            fogRT = new RenderTexture(fogRTSize.x, fogRTSize.y, 0, RenderTextureFormat.ARGB32);
        }

        planeRender.sharedMaterial.SetTexture(baseMap, fogRT);
        m_cbuffer.name = HexFogCbufferName;
       

        viewRight = fogWidth * .5f;
        viewLeft = -viewRight;
        viewTop = fogHeight * .5f;
        viewBottom = -viewTop;
    }

    private Matrix4x4[] Convert2Matrix(Vector3[] positions)
    {
        if (positions.Length > 0)
        {
            Matrix4x4[] mx = new Matrix4x4[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                mx[i] = Matrix4x4.TRS(positions[i], Quaternion.identity, Vector3.one * m_hexSize);
            }

            return mx;
        }

        return null;
    }

    public void DrawHexImmediately(Vector3[] positions, bool open)
    {
        if (fogRT == null)
        {
            Debug.LogError("fog rt is null");
            return;
        }
        m_viewMatrix = Matrix4x4.TRS(viewTransform, viewQuaternion, viewScale);

        var matrixArray = Convert2Matrix(positions);
        float dissolve = 1;
        if (open)
        {
            dissolve = 0;
        }

        if (m_propertyBlock == null)
        {
            m_propertyBlock = new MaterialPropertyBlock();
            m_propertyBlock.Clear();
        }

        var buffer = new float[matrixArray.Length];
        for (int i = 0; i < matrixArray.Length; i++)
        {
            buffer[i] = dissolve;
        }

        m_propertyBlock.SetFloatArray("_Dissolve", buffer);
        DrawHexMesh(matrixArray, true, m_propertyBlock);
    }

    #region 渐变效果

    public bool StopdrawHexAsync;

    public void DrawHexAync2(Vector3[] positions, float[] maskdirection, bool open)
    {
        m_viewMatrix = Matrix4x4.TRS(viewTransform, viewQuaternion, viewScale);
        StartCoroutine(DrawHexAsync(positions, maskdirection,open));
    }

    IEnumerator DrawHexAsync(Vector3[] positions, float[] maskdirection,bool open)
    {
        if (fogRT == null)
        {
            Debug.LogError("fog rt is null");
            yield break;
        }

        var matrices = Convert2Matrix(positions);

        var dissolve = 1.0f;
        if (open)
        {
            dissolve = 0f;
        }
       
        var stop = false;
        var buffer = new float[matrices.Length];
        var colorbuffer = new Vector4[matrices.Length];
        if (m_propertyBlock == null)
        {
            m_propertyBlock = new MaterialPropertyBlock();
        }

        m_propertyBlock.Clear();

        if (open)
        {
            while (dissolve < 1.1)
            {
                if (StopdrawHexAsync)
                {
                    dissolve = 1f;
                    stop = true;
                }

                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = dissolve;
                    colorbuffer[i] = Color.green;
                }

                m_propertyBlock.SetFloatArray("_Dissolve", buffer);
                m_propertyBlock.SetVectorArray("_BaseColor", colorbuffer);

                DrawHexMesh(matrices, true, m_propertyBlock);
                if (stop)
                {
                    yield break;
                }
                dissolve += 0.1f;
                //Debug.LogError(dissolve);
                yield return new WaitForSeconds(disovleTime);
            }
        }
        else
        {
            while (dissolve >0)
            {
                if (StopdrawHexAsync)
                {
                    dissolve = 0.0f;
                    stop = true;
                }

                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = dissolve;
                    colorbuffer[i] = Color.green;
                }

                m_propertyBlock.SetFloatArray("_Dissolve", buffer);
                m_propertyBlock.SetVectorArray("_BaseColor", colorbuffer);

                DrawHexMesh(matrices, true, m_propertyBlock);
                if (stop)
                {
                    yield break;
                }
                dissolve -= 0.1f;
                //Debug.LogError(dissolve);
                yield return new WaitForSeconds(disovleTime);
            }
        }
    }

    #endregion

    void DrawHexMesh(Matrix4x4[] matrices, bool clear = false, MaterialPropertyBlock properties = null)
    {
        Graphics.SetRenderTarget(fogRT);
        if (clear)
        {
            m_cbuffer.ClearRenderTarget(true, true, FogColor2);
        }

        m_cbuffer.SetViewProjectionMatrices(m_viewMatrix.inverse, GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(viewLeft, viewRight, viewBottom, viewTop, 0.03f, 100), false));
        if (properties != null)
        {
            m_cbuffer.DrawMeshInstanced(hexMesh, 0, fogMaterial, 0, matrices, matrices.Length, properties);
        }
        else
        {
            m_cbuffer.DrawMeshInstanced(hexMesh, 0, fogMaterial, 0, matrices, matrices.Length);
        }

        Graphics.ExecuteCommandBuffer(m_cbuffer);
        m_cbuffer.Clear();
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 120, 80), "Blit"))
        {
            //Debug.LogError(m_cam.worldToCameraMatrix);
            Debug.LogError(m_viewMatrix.inverse);
            DrawHexImmediately(hexPos,true);
        }

        if (GUI.Button(new Rect(130, 10, 120, 80), "渐变开启迷雾"))
        {
            List<float> dir = new List<float>();
            for (int i = 0; i < hexPos.Length; i++)
            {
                dir.Add(3.14f);
            }

            DrawHexAync2(hexPos, dir.ToArray(),false);
        }
        
        if (GUI.Button(new Rect(250, 10, 120, 80), "渐变结束迷雾"))
        {
            List<float> dir = new List<float>();
            for (int i = 0; i < hexPos.Length; i++)
            {
                dir.Add(3.14f);
            }

            DrawHexAync2(hexPos, dir.ToArray(),true);
        }
    }
}