using System;
using System.Collections.Generic;
using Elex.HexFog;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour {

	public int width = 6;
	public int height = 6;

	public Color defaultColor = Color.white;
	public Color touchedColor = Color.magenta;

	public HexCell cellPrefab;
	public Text cellLabelPrefab;

	HexCell[] cells;

	Canvas gridCanvas;
	HexMesh hexMesh;

	[FormerlySerializedAs("TestHexFog")] public HexFogView hexFogView;

	private HexFogParam hexFogParam;

	void Awake () {
		gridCanvas = GetComponentInChildren<Canvas>();
		hexMesh = GetComponentInChildren<HexMesh>();

		cells = new HexCell[height * width];

		for (int z = 0, i = 0; z < height; z++) {
			for (int x = 0; x < width; x++) {
				CreateCell(x, z, i++);
			}
		}
	}

	private HexFogParam GetInitData()
	{
		HexFogParam data = new HexFogParam();

		List<Vector3> zeroLayer = new List<Vector3>();
		zeroLayer.Add(new Vector3(51.96f, 0, 0));

		List<Vector3> oneLayer = new List<Vector3>();
		oneLayer.Add( new Vector3(43.30f, 0, 15.00f));
		oneLayer.Add( new Vector3(60.62f,0,15.00f));
		data.FogData.Add(zeroLayer);
		data.FogData.Add(oneLayer);
		return data;
	}

	private HexFogParam GetFirstData()
	{
		HexFogParam data = new HexFogParam();
		List<Vector3> zeroLayer = new List<Vector3>();
		zeroLayer.Add( new Vector3(43.30f, 0, 15.00f));

		List<Vector3> oneLayer = new List<Vector3>();
		oneLayer.Add(new Vector3(51.96f, 0, 0));
		oneLayer.Add( new Vector3(60.62f,0,15.00f));
		oneLayer.Add(new Vector3(34.64f,0,30.00f));

		data.FogData.Add(zeroLayer);
		data.FogData.Add(oneLayer);

		return data;
	}
	
	private HexFogParam GetSecondData()
	{
		HexFogParam data = new HexFogParam();
		List<Vector3> zeroLayer = new List<Vector3>();
		zeroLayer.Add(new Vector3(51.96f, 0, 0));

		List<Vector3> oneLayer = new List<Vector3>();
		oneLayer.Add( new Vector3(43.30f, 0, 15.00f));
		oneLayer.Add( new Vector3(60.62f,0,15.00f));
		oneLayer.Add(new Vector3(34.64f,0,30.00f));

		data.FogData.Add(zeroLayer);
		data.FogData.Add(oneLayer);

		return data;
	}

	void Start () {
		hexMesh.Triangulate(cells);
		
		hexFogParam = GetInitData();
		hexFogView.DrawHexFogImmediately(hexFogParam,true);
	}

	void Update () {
		// if (Input.GetMouseButtonDown(0)) {
		// 	//开启
		// 	Debug.LogError("开启迷雾");
		// 	HandleInput(true);
		// }
		// if (Input.GetMouseButtonDown(1)) {
		// 	//关闭
		// 	Debug.LogError("关闭迷雾");
		// 	HandleInput(false);
		// }
	}

	private void OnGUI()
	{
		if (GUI.Button(new Rect(10, 10, 120, 80), "移动一步"))
		{
			HexFogParam param = GetFirstData();
			hexFogView.StartDrawHexFogAsync(param);
		}

		if (GUI.Button(new Rect(140, 10, 120, 80), "后退一步"))
		{
			HexFogParam param = GetSecondData();
			hexFogView.StartDrawHexFogAsync(param);
			// List<float> dir = new List<float>();
			// for (int i = 0; i < 6; i++)
			// {
			// 	dir.Add(3.14f);
			// }
			//hexFogView.StartDrawHexFogAsync(hexFogParam,dir,false);
		}
		
		if (GUI.Button(new Rect(270, 10, 120, 80), "重置"))
		{
			HexFogParam param = GetInitData();
			hexFogView.DrawHexFogImmediately(param,true);
			// List<float> dir = new List<float>();
			// for (int i = 0; i < 6; i++)
			// {
			// 	dir.Add(3.14f);
			// }
			//hexFogView.StartDrawHexFogAsync(hexFogParam,dir,false);
		}
		/*
		 *  new Vector3(51.96f, 0, 0),
            new Vector3(43.30f, 0, 15.00f),
            new Vector3(60.62f,0,15.00f),
            new Vector3(34.64f,0,30.00f),
            new Vector3(51.96f,0,30.00f),
            new Vector3(69.28f,0,30.00f),
		 */
		/*
		if (GUI.Button(new Rect(10, 10, 120, 80), "立即绘制区域"))
		{
			hexFogView.DrawHexFogImmediately(hexFogParam,true);
		}

		if (GUI.Button(new Rect(130, 10, 120, 80), "渐变开启迷雾"))
		{
			hexFogView.StartDrawHexFogAsync(hexFogParam,false);
			// List<float> dir = new List<float>();
			// for (int i = 0; i < 6; i++)
			// {
			// 	dir.Add(3.14f);
			// }
			//hexFogView.StartDrawHexFogAsync(hexFogParam,dir,false);
		}

		if (GUI.Button(new Rect(250, 10, 120, 80), "渐变结束迷雾"))
		{
			List<float> dir = new List<float>();
			for (int i = 0; i < 6; i++)
			{
				dir.Add(3.14f);
			}
			//hexFogView.StartDrawHexFogAsync(hexFogParam,dir,true);
		}*/
	}

	/*
	 * private void OnGUI()
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
	 * 
	 */

	void HandleInput (bool select) {
		Vector3 cellPos = Vector3.zero;
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		float dir = 3.14f;
		if (Physics.Raycast(inputRay, out hit)) {
			TouchCell(hit.point,select,out cellPos);
			List<Vector3> cellPosList = new List<Vector3>(){cellPos};
			List<float> dirList = new List<float>() { dir};
			//hexFogView.StartDrawHexFogAsync(cellPosList.ToArray(),dirList.ToArray(),select);
		}
	}

	void TouchCell (Vector3 position,bool select,out Vector3 cellPos) {
		cellPos = Vector3.zero;
		position = transform.InverseTransformPoint(position);
	
		HexCoordinates coordinates = HexCoordinates.FromPosition(position);
		int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
		HexCell cell = cells[index];
		cell.color = select ? touchedColor : defaultColor;

		cellPos = cell.transform.position;
		Debug.Log($"position:{cell.transform.position},index:{index},cor:{coordinates.ToString()}");
		hexMesh.Triangulate(cells);
	}

	void CreateCell (int x, int z, int i) {
		Vector3 position;
		position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
		position.y = 0f;
		position.z = z * (HexMetrics.outerRadius * 1.5f);

		HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
		cell.transform.SetParent(transform, false);
		cell.transform.localPosition = position;
		cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
		cell.color = defaultColor;
		cell.gameObject.name = $"{i}_{cell.coordinates}";

		Text label = Instantiate<Text>(cellLabelPrefab);
		label.rectTransform.SetParent(gridCanvas.transform, false);
		label.rectTransform.anchoredPosition =
			new Vector2(position.x, position.z);
		label.text = cell.coordinates.ToStringOnSeparateLines();
	}
}