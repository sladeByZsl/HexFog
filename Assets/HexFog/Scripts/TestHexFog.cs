using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

//[ExecuteAlways]
public class TestHexFog : MonoBehaviour
{
    [Header("六边形遮罩的mesh")]
    public Mesh maskMesh;
    
    [Header("迷雾的材质球")] 
    public Material fogMaterial;
    
    [Header("迷雾RT")] 
    public RenderTexture fogRT;

    [Header("迷雾RT的Size")]
    public Vector2Int fogRTSize = new Vector2Int(256, 256);
    
    [Header("0层颜色")]
    public static readonly Color FogColor0=Color.black;
    [Header("1层颜色")]
    public static readonly Color FogColor1=Color.green;
    [Header("2层颜色")]
    public static readonly Color FogColor2=Color.blue;
    
    [Header("地表")]
    public Renderer planeRender;
    //地表shader参数
    private static readonly int baseMap = Shader.PropertyToID("_BaseMap");

    #region 视野矩阵
    public Vector3 viewTransform = new Vector3(0, 20, 0);
    private static readonly Quaternion viewQuaternion = Quaternion.Euler(90, 0, 0);
    private static readonly Vector3 viewScale = new Vector3(1, 1, -1);
    #endregion

    #region 正交投影
    public float viewLeft = -50.0f;
    public float viewRight = 50.0f;
    public float viewBottom = -50.0f;
    public float viewTop = 50.0f;
    public float viewNear = 0f;
    public float viewFar = 100.0f;
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
        m_viewMatrix = Matrix4x4.TRS(viewTransform, viewQuaternion, viewScale);
    }

    private Matrix4x4[] Convert2Matrix(Vector3[] positions)
    {
        if (positions.Length > 0)
        {
            Matrix4x4[] mx = new Matrix4x4[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                mx[i] = Matrix4x4.TRS(positions[i], Quaternion.identity, Vector3.one*m_hexSize );
            }
            return mx;
        }
        return null;
    }

    private void DrawHexImmediately(Vector3[] positions)
    {
        if (fogRT == null)
        {
            Debug.LogError("fog rt is null");
            return;
        }

        var matrixArray = Convert2Matrix(positions);
        DrawHexMesh(matrixArray,true);
    }

    #region 渐变效果
    public bool StopdrawHexAsync;
    public void DrawHexAync2(Vector3[] positions, float[] maskdirection, bool clear = false)
    {
        StartCoroutine(DrawHexAsync(positions, maskdirection));
    }

    IEnumerator DrawHexAsync(Vector3[] positions, float[] maskdirection)
    {
        if (fogRT == null)
        {
            Debug.LogError("fog rt is null");
            yield break;
        }

        var matrixes = Convert2Matrix(positions);
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

    

    #endregion
   
    void DrawHexMesh(Matrix4x4[] matries, bool clear = false, MaterialPropertyBlock properties = null)
    {
        Graphics.SetRenderTarget(fogRT);
        if (clear)
        {
            m_cbuffer.ClearRenderTarget(true, true,FogColor2);
        }

        m_cbuffer.SetViewProjectionMatrices(m_viewMatrix.inverse, GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(viewLeft, viewRight, viewBottom, viewTop, viewNear, viewFar), false));
        if (properties != null)
        {
            m_cbuffer.DrawMeshInstanced(maskMesh, 0, fogMaterial, 0, matries, matries.Length, properties);
        }
        else
        {
            //matries = new Matrix4x4[1];
            //matries[0] = Matrix4x4.TRS(  new Vector3(17.32f, 0.00f, 30.00f),quaternion.identity, Vector3.one);
            //m_cbuffer.DrawMeshInstancedProcedural(m_maskMesh,0,m_mat,0,matries.Length);
            m_cbuffer.DrawMeshInstanced(maskMesh, 0, fogMaterial, 0, matries, matries.Length);
        }

        //m_cbuffer.Clear();
        Graphics.ExecuteCommandBuffer(m_cbuffer);
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 120, 80), "Blit"))
        {
            //Debug.LogError(m_cam.worldToCameraMatrix);
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