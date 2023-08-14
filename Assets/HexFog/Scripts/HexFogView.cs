using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Elex.HexFog
{
    public class HexFogParam
    {
        public List<List<Vector3>> FogData = new List<List<Vector3>>();
        public List<List<float>> FogDir = new List<List<float>>();
    }

    //迷雾格子状态
    public enum FogGridStatus
    {
        None = -1, //未知，给目标状态使用
        Lock = 0, //初始状态，全黑，临时blue
        Unlocking = 1, //探索状态，迷雾叠加地表，临时Green
        Unlocked = 2, //拥有的状态，临时红色
    }

    public class FogItem
    {
        public string key;

        public float currentTime=0.0f;
        public float targetTime=0.0f;
        private FogGridStatus _targetStatus;
        public bool isDirty = false;

        public FogGridStatus targetStatus
        {
            get { return _targetStatus; }
            set
            {
                if (value == FogGridStatus.None)
                {
                    currentTime = 0;
                    targetTime = 0;
                }
                else
                {
                    if (_targetStatus == FogGridStatus.None)
                    {
                        currentTime = Time.realtimeSinceStartup;
                        targetTime = currentTime + HexFogView.Instance.disovleTime;
                    }
                    else
                    {
                        lastProgress = 0;
                        srcStatus = _targetStatus;
                        float previousProgress = GetProgress(); // 获取上一次的进度
                        float remainingTime = (1-previousProgress) * HexFogView.Instance.disovleTime; // 计算剩余的时间
                        currentTime = Time.realtimeSinceStartup-remainingTime;
                        targetTime =  currentTime + HexFogView.Instance.disovleTime; // 使用剩余的时间来计算目标时间
                    }
                }
                _targetStatus = value;
                isDirty = true;
            }
        }


        public FogGridStatus srcStatus = FogGridStatus.Lock;
        private float lastProgress = 0f; // 存储上一次的进度

        public float GetProgress()
        {
            if (targetTime <= currentTime)
            {
                return 0f;
            }

            float elapsedTime = Time.realtimeSinceStartup - currentTime;
            float totalTime = targetTime - currentTime;
            float progress = elapsedTime / totalTime;
            progress = Mathf.Clamp01(progress);

            //检查进度是否有显著变化
            if (Mathf.Abs(progress - lastProgress) < 0.01f)
            {
                return lastProgress; // 如果变化不显著，则返回上一次的进度
            }

            lastProgress = progress; // 更新上一次的进度
            return progress;
        }
    }


    //组织每次绘制的数据
    class HexFogDrawData
    {
        public List<Vector3> posList = new List<Vector3>();
        public List<Matrix4x4> matrixList = new List<Matrix4x4>();
        public List<float> dissolveList = new List<float>();
        public List<Vector4> srcColorList = new List<Vector4>();
        public List<Vector4> destColorList = new List<Vector4>();

        public void SortByColor(List<Vector4> _sortColor)
        {
            // 使用LINQ创建一个排序后的索引列表
            var sortedIndices = _sortColor
                .Select((color, index) => new { color, index })
                .OrderBy(item => (Color)item.color == Color.green ? 0 : 1) // 绿色在前，红色在后
                .Select(item => item.index)
                .ToList();

            // 使用索引列表调整其他列表的顺序
            posList = sortedIndices.Select(index => posList[index]).ToList();
            matrixList = sortedIndices.Select(index => matrixList[index]).ToList();
            dissolveList = sortedIndices.Select(index => dissolveList[index]).ToList();
            srcColorList = sortedIndices.Select(index => srcColorList[index]).ToList();
            destColorList = sortedIndices.Select(index => destColorList[index]).ToList();
        }
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

        //迷雾shader参数
        int dissolveId = Shader.PropertyToID("_Dissolve");
        int destColorId = Shader.PropertyToID("_DestColor");
        int srcColorId = Shader.PropertyToID("_SrcColor");

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

        public static List<HexCell> cellList = new List<HexCell>();
        public static bool IsNeedClear = false;

        private static HexFogView _instance;

        public static HexFogView Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<HexFogView>();
                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject(nameof(HexFogView));
                        _instance = singleton.AddComponent<HexFogView>();
                        DontDestroyOnLoad(singleton);
                    }
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

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
            SetViewMatrix();
        }

        public static void RegisterCell(HexCell cell)
        {
            cellList.Add(cell);
        }

        public static void ClearCell()
        {
            cellList.Clear();
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

        private Color GetColorByStatus(FogGridStatus fogGridStatus)
        {
            switch (fogGridStatus)
            {
                case FogGridStatus.Unlocked: return FogColor0;
                case FogGridStatus.Unlocking: return FogColor1;
                case FogGridStatus.Lock: return FogColor2;
                default: return Color.white;
            }
        }

        public void Update()
        {
            if (cellList == null || cellList.Count == 0)
            {
                return;
            }

            if (!IsAnyCellDirty())
            {
                return;
            }

            HexFogDrawData hexFogDrawData = ProcessCells();

            if (hexFogDrawData.posList.Count > 0)
            {
                hexFogDrawData.matrixList = Convert2Matrix(hexFogDrawData.posList.ToArray()).ToList();

                if (IsNeedClear)
                {
                    IsNeedClear = false;
                    DrawHexFog2(hexFogDrawData, true);
                }
                else
                {
                    DrawHexFog2(hexFogDrawData, true);
                }
            }
            else
            {
                ClearTarget();
            }
        }

        private bool IsAnyCellDirty()
        {
            foreach (var hexCell in cellList)
            {
                if (hexCell.fogItem.isDirty)
                {
                    return true;
                }
            }

            return false;
        }

        private HexFogDrawData ProcessCells()
        {
            HexFogDrawData hexFogDrawData = new HexFogDrawData();
            foreach (var hexCell in cellList)
            {
                ProcessHexCell(hexCell, hexFogDrawData);
            }

            return hexFogDrawData;
        }

        private void ProcessHexCell(HexCell hexCell, HexFogDrawData hexFogDrawData)
        {
            var targetStatus = hexCell.fogItem.targetStatus;
            var srcStatus = hexCell.fogItem.srcStatus;
            Color srcColor = GetColorByStatus(srcStatus);
            Vector3 pos = hexCell.GetPos();

            if (targetStatus == FogGridStatus.None)
            {
                //没有目标状态，立即刷新，使用srcColor
                hexCell.fogItem.isDirty = false;
                if (srcStatus == FogGridStatus.Unlocked || srcStatus == FogGridStatus.Unlocking)
                {
                    AddHexFogData(hexFogDrawData, pos, 0.0f, srcColor, Color.white, srcColor == FogColor0);
                }
            }
            else
            {
                //存在目标状态，需要有动画效果
                ProcessTargetStatus(hexCell, hexFogDrawData, srcColor, pos);
            }
        }

        private void ProcessTargetStatus(HexCell hexCell, HexFogDrawData hexFogDrawData, Color srcColor, Vector3 pos)
        {
            var targetStatus = hexCell.fogItem.targetStatus;
            //每帧获取进度
            float process = hexCell.fogItem.GetProgress();
            if (process >= 0.99f)
            {
                hexCell.fogItem.srcStatus = targetStatus;
                hexCell.fogItem.targetStatus = FogGridStatus.None;
                process = 1.0f;
                hexCell.fogItem.isDirty = false;
            }

            Color destColor = GetColorByStatus(targetStatus);
            if (targetStatus == FogGridStatus.Unlocked || targetStatus == FogGridStatus.Unlocking)
            {
                AddHexFogData(hexFogDrawData, pos, process, srcColor, destColor,
                    srcColor == FogColor0 || destColor == FogColor0);
            }
        }

        private void AddHexFogData(HexFogDrawData hexFogDrawData, Vector3 pos, float process, Color srcColor,
            Color destColor, bool isRed)
        {
            if (Math.Abs(pos.x - 17.32f) < 0.1f)
            {
                LogError($"{process}");
                //
            }
            //LogError($"{pos},{process},{srcColor},{destColor}");
            
            //对颜色进行排序，先渲染绿色（第0层），再渲染红色（第1层）
            if (isRed)
            {
                hexFogDrawData.posList.Add(pos);
                hexFogDrawData.dissolveList.Add(process);
                hexFogDrawData.srcColorList.Add(srcColor);
                hexFogDrawData.destColorList.Add(destColor);
            }
            else
            {
                hexFogDrawData.posList.Insert(0, pos);
                hexFogDrawData.dissolveList.Insert(0, process);
                hexFogDrawData.srcColorList.Insert(0, srcColor);
                hexFogDrawData.destColorList.Insert(0, destColor);
            }
        }


        void DrawHexFog2(HexFogDrawData hexFogDrawData, bool clear)
        {
            if (fogRT == null)
            {
                LogError("fog rt is null");
                return;
            }

            var matrices = hexFogDrawData.matrixList.ToArray();
            var dissolveBuffer = hexFogDrawData.dissolveList.ToArray();
            var colorBuffer = hexFogDrawData.srcColorList.ToArray();
            var destColorBuffer = hexFogDrawData.destColorList.ToArray();
            m_propertyBlock ??= new MaterialPropertyBlock();
            m_propertyBlock.Clear();

            m_propertyBlock.SetFloatArray(dissolveId, dissolveBuffer);
            m_propertyBlock.SetVectorArray(destColorId, destColorBuffer);
            m_propertyBlock.SetVectorArray(srcColorId, colorBuffer);
            DrawHexMesh(matrices, clear, m_propertyBlock);
        }

        #region 工具

        private void LogError(string content)
        {
            if (logEnable)
            {
                Debug.LogError(content);
            }
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

        private void ClearTarget()
        {
            Graphics.SetRenderTarget(fogRT);
            m_cbuffer.ClearRenderTarget(true, true, FogColor2);
            Graphics.ExecuteCommandBuffer(m_cbuffer);
            m_cbuffer.Clear();
        }

        void FogBlur(CommandBuffer cmd)
        {
            if (forBlurRt == null)
            {
                forBlurRt = new RenderTexture(fogRT.descriptor);
            }

            blurMaterial.SetFloat("_BlurRadius", blurRadius);
            cmd.Blit(fogRT, forBlurRt, blurMaterial);
            cmd.Blit(forBlurRt, fogRT, blurMaterial);
        }

        #endregion
    }
}