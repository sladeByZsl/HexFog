using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Elex.HexFog
{
    public class HexFogParam
    {
        public List<List<Vector3>> FogData=new List<List<Vector3>>();
        public List<List<float>> FogDir=new List<List<float>>();
    }

    //迷雾格子状态
    enum FogGridStatus
    {
        None=-1,//未知，给目标状态使用
        Lock=0,//初始状态，全黑，临时blue
        Unlocking=1,//探索状态，迷雾叠加地表，临时Green
        Unlocked=2,//拥有的状态，临时红色
    }

    class FogItem
    {
        public string key;
        public FogGridStatus targetStatus=FogGridStatus.None;
        public FogGridStatus srcStatus=FogGridStatus.Lock;
    }

    //组织每次绘制的数据
    class HexFogDrawData
    {
       public List<Matrix4x4> matrixList = new List<Matrix4x4>();
       public  List<float> dissolveList = new List<float>();
       public  List<Vector4> colorList = new List<Vector4>();
    }

    public class HexFogView : MonoBehaviour
    {
        [Header("六边形遮罩的mesh")] public Mesh hexMesh;
        [Header("迷雾的材质球")] public Material fogMaterial;
        [Header("迷雾RT")] public RenderTexture fogRT;
        [Header("迷雾RT的Size")] public Vector2Int fogRTSize = new Vector2Int(256, 256);
        [Header("地表")] public Renderer planeRender;
        [Header("迷雾溶解时间")] public float disovleTime = 0.1f;
        [Header("模糊的材质球")] public Material blurMaterial;
        [Header("模糊半径")] public float blurRadius;
        [Header("LogEnable")] public bool logEnable = true;

        //地表shader参数
        private static readonly int baseMap = Shader.PropertyToID("_FogMaskMap");

        #region 视野矩阵

        private Vector3 viewTransform = new Vector3(0, 20, 0);
        private static readonly Quaternion viewQuaternion = Quaternion.Euler(90, 0, 0);
        private static readonly Vector3 viewScale = new Vector3(1, 1, -1);

        private static readonly Color FogColor0 = Color.red;
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
        public RenderTexture forBlurRt;
        private MaterialPropertyBlock m_propertyBlock;

        //迷雾全部的数据
        private Dictionary<string, FogItem> fogItemDic = new Dictionary<string, FogItem>();

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

            //设置迷雾相机尺寸
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
                    mx[i] = Matrix4x4.TRS(positions[i], Quaternion.Euler(0, -90, 0), Vector3.one * m_hexSize);
                }

                return mx;
            }

            return null;
        }

        #region 立刻绘制

        public void DrawHexFogImmediately(HexFogParam hexFogParam, bool open)
        {
            if (fogRT == null)
            {
                Debug.LogError("fog rt is null");
                return;
            }
            fogItemDic.Clear();
            SetViewMatrix();
            float dissolve = open ? 0 : 1;

            List<Matrix4x4> matrixList = new List<Matrix4x4>();
            List<float> dissolveList = new List<float>();
            List<Vector4> colorList = new List<Vector4>();
            for (int i=0;i<hexFogParam.FogData.Count;i++)
            {
                var positions = hexFogParam.FogData[i].ToArray();
                var matrices = Convert2Matrix(positions);
                var color = GetColorForKey(i);
                matrixList.AddRange(matrices);
                for (int j = 0; j < matrices.Length; j++)
                {
                    dissolveList.Add(dissolve);
                    colorList.Add((Vector4) color);
                }
            }
            m_propertyBlock ??= new MaterialPropertyBlock();
            m_propertyBlock.Clear();
            m_propertyBlock.SetFloatArray("_Dissolve", dissolveList.ToArray());
            m_propertyBlock.SetVectorArray("_BaseColor", colorList.ToArray());
            DrawHexMesh(matrixList.ToArray(), true, m_propertyBlock);
        }

        private Color GetColorForKey(int key)
        {
            // You can define a mapping of keys to colors here
            // For example:
            switch (key)
            {
                case 0: return FogColor0;
                case 1: return FogColor1;
                case 2: return FogColor2;
                // Add more cases as needed
                default: return Color.white; // Default color
            }
        }

        #endregion

        private HexFogDrawData GetDrawData(HexFogParam hexFogParam, int paramType, List<float> dissolveParam)
        {
            HexFogDrawData drawData = new HexFogDrawData();

            if (paramType == -1) // 获取全部数据
            {
                for (int i = 0; i < hexFogParam.FogData.Count; i++)
                {
                    ProcessFogData(hexFogParam.FogData[i], i, dissolveParam[i], drawData);
                }
            }
            else if (paramType >= 0 && paramType < hexFogParam.FogData.Count) // 获取指定索引的数据
            {
                ProcessFogData(hexFogParam.FogData[paramType], paramType, dissolveParam[paramType], drawData);
            }

            m_propertyBlock ??= new MaterialPropertyBlock();
            m_propertyBlock.Clear();

            return drawData;
        }

        private void ProcessFogData(List<Vector3> positionsList, int index, float dissolveParam, HexFogDrawData drawData)
        {
            var positions = positionsList.ToArray();
            var matrices = Convert2Matrix(positions);
            var color = GetColorForKey(index);
            drawData.matrixList.AddRange(matrices);
            for (int j = 0; j < matrices.Length; j++)
            {
                drawData.dissolveList.Add(dissolveParam);
                drawData.colorList.Add((Vector4)color);
            }
        }


        #region 渐变绘制
        
        public async UniTask StartDrawHexFogAsync(HexFogParam hexFogParam)
        {
            SetViewMatrix();
            HexFogDrawData drawData = GetDrawData(hexFogParam,-1,new List<float>(){1,1});
            var handler = DrawHexFogAsync(drawData).Start();
            handler.OnComplete(stopped => OnCoroutineComplete(stopped, handler));
            
            // await UniTask.Delay(TimeSpan.FromSeconds(1), ignoreTimeScale: false);
            //
            // drawData = GetDrawData(hexFogParam,-1,new List<float>(){1,0});
            // handler = DrawHexFogAsync(drawData).Start();
            // handler.OnComplete(stopped => OnCoroutineComplete(stopped, handler));
        }
        
        IEnumerator DrawHexFogAsync(HexFogDrawData hexFogDrawData)
        {
            if (fogRT == null)
            {
                Debug.LogError("fog rt is null");
                yield break;
            }
            var matrices = hexFogDrawData.matrixList.ToArray();
            var dissolveBuffer = hexFogDrawData.dissolveList.ToArray();
            var dissolveBufferUse = hexFogDrawData.dissolveList.ToArray();
            var colorBuffer = hexFogDrawData.colorList.ToArray();
            if (m_propertyBlock == null)
            {
                m_propertyBlock = new MaterialPropertyBlock();
            }
            m_propertyBlock.Clear();

            float dissolveTime = 1.0f;
            while (dissolveTime>0)
            {
                dissolveTime -= 0.1f;
                for (int i = 0; i < dissolveBuffer.Length; i++)
                {
                    bool open = dissolveBuffer.Length >= i && dissolveBuffer[i] == 0f;
                    float dissolveStep = open ? 0.1f : -0.1f;
                    dissolveBufferUse[i] += dissolveStep;
                    dissolveBufferUse[i] = Mathf.Clamp(dissolveBufferUse[i], 0f, 1f); // 确保溶解值在0到1之间
                }
                m_propertyBlock.SetFloatArray("_Dissolve", dissolveBufferUse);
                m_propertyBlock.SetVectorArray("_BaseColor", colorBuffer);
                DrawHexMesh(matrices, true, m_propertyBlock);
                if (StopdrawHexAsync)
                {
                    yield break;
                }
                yield return new WaitForSeconds(disovleTime);
            }
        }
        
        private List<CoroutineHandler> runningCoroutines = new List<CoroutineHandler>();
        private bool StopdrawHexAsync;

        public void StartDrawHexFogAsync(Vector3[] positions, float[] maskDir, bool open)
        {
            SetViewMatrix();
            var handler = DrawHexAsync(positions, maskDir, open).Start();
            handler.OnComplete(stopped => OnCoroutineComplete(stopped, handler));
            runningCoroutines.Add(handler);
        }

        private void OnCoroutineComplete(bool stopped, CoroutineHandler handler)
        {
            Log(stopped ? "Coroutine was stopped." : "Coroutine completed successfully.");
            runningCoroutines.Remove(handler);
        }

        public void StopAllFogCoroutines()
        {
            foreach (var handler in runningCoroutines)
            {
                handler.Stop();
            }

            runningCoroutines.Clear();
        }

        IEnumerator DrawHexAsync(Vector3[] positions, float[] maskdirection, bool open)
        {
            if (fogRT == null)
            {
                Debug.LogError("fog rt is null");
                yield break;
            }

            var matrices = Convert2Matrix(positions);
            var buffer = new float[matrices.Length];
            var colorbuffer = new Vector4[matrices.Length];
            if (m_propertyBlock == null)
            {
                m_propertyBlock = new MaterialPropertyBlock();
            }
            m_propertyBlock.Clear();

            float dissolve = open ? 0f : 1.0f;
            float dissolveStep = open ? 0.1f : -0.1f;
            float dissolveLimit = open ? 1.1f : 0f;

            while ((open && dissolve < dissolveLimit) || (!open && dissolve > dissolveLimit))
            {
                if (StopdrawHexAsync)
                {
                    dissolve = open ? 1f : 0.0f;
                }

                UpdateBuffers(buffer, colorbuffer, dissolve, Color.green);
                m_propertyBlock.SetFloatArray("_Dissolve", buffer);
                m_propertyBlock.SetVectorArray("_BaseColor", colorbuffer);

                DrawHexMesh(matrices, true, m_propertyBlock);
                if (StopdrawHexAsync)
                {
                    yield break;
                }

                dissolve += dissolveStep;
                yield return new WaitForSeconds(disovleTime);
            }
        }

        private void UpdateBuffers(float[] buffer, Vector4[] colorbuffer, float value, Color color)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = value;
                colorbuffer[i] = color;
            }
        }

        #endregion

        #region 工具

        private void Log(string content)
        {
            if (logEnable)
            {
                Debug.Log(content);
            }
        }
        
        public static string Vector3ToKey(Vector3 vector)
        {
            return string.Format("{0}_{1}_{2}", vector.x, vector.y, vector.z);
            // 或使用插值字符串
            // return $"{vector.x}_{vector.y}_{vector.z}";
        }
        public static int Vector3ToHash(Vector3 vector)
        {
            int hash = 17;
            hash = hash * 23 + vector.x.GetHashCode();
            hash = hash * 23 + vector.y.GetHashCode();
            hash = hash * 23 + vector.z.GetHashCode();
            return hash;
        }

        //设置迷雾相机的矩阵
        private void SetViewMatrix()
        {
            viewTransform = planeRender.transform.position;
            viewTransform.y += 10; //迷雾相机一直比迷雾高
            m_viewMatrix = Matrix4x4.TRS(viewTransform, viewQuaternion, viewScale);
        }

        private void DrawHexMesh(Matrix4x4[] matrices, bool clear = false, MaterialPropertyBlock properties = null)
        {
            Graphics.SetRenderTarget(fogRT);
            if (clear)
            {
                m_cbuffer.ClearRenderTarget(true, true, FogColor2);
            }

            m_cbuffer.SetViewProjectionMatrices(m_viewMatrix.inverse,
                GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(viewLeft, viewRight, viewBottom, viewTop, 0.03f, 100),
                    false));
            if (properties != null)
            {
                m_cbuffer.DrawMeshInstanced(hexMesh, 0, fogMaterial, 0, matrices, matrices.Length, properties);
            }
            else
            {
                m_cbuffer.DrawMeshInstanced(hexMesh, 0, fogMaterial, 0, matrices, matrices.Length);
            }

            //FogBlur(m_cbuffer);
            Graphics.ExecuteCommandBuffer(m_cbuffer);
            m_cbuffer.Clear();
        }


        void FogBlur(CommandBuffer cmd)
        {
          
            if (forBlurRt == null)
            {
                forBlurRt = new RenderTexture(fogRT.descriptor);
            }
            blurMaterial.SetFloat("_BlurRadius",blurRadius);
            cmd.Blit(fogRT, forBlurRt, blurMaterial);
            cmd.Blit(forBlurRt, fogRT, blurMaterial);
        }

        #endregion
    }
}