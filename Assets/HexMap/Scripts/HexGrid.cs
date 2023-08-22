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
	void Start () {
		hexMesh.Triangulate(cells);
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
			HexFogView.cellList[8100].fogItem.targetStatus = FogGridStatus.Unlocked;
			HexFogView.cellList[-900].fogItem.targetStatus = FogGridStatus.Unlocking;
			HexFogView.cellList[9000].fogItem.targetStatus = FogGridStatus.Unlocking;
			HexFogView.cellList[-1800].fogItem.targetStatus = FogGridStatus.Unlocking;
		}

		if (GUI.Button(new Rect(140, 10, 120, 80), "后退一步"))
		{
			HexFogView.cellList[8100].fogItem.targetStatus = FogGridStatus.Unlocking;
		}
		
		if (GUI.Button(new Rect(270, 10, 120, 80), "重置"))
		{
			HexFogView.IsNeedClear = true;
			List<int> unlocked = new List<int>() { 17100,26100};
			List<int> unlocking = new List<int>() {18000,27000,36000,8100,35100,7200,16200,25200};

			foreach (var (id,hexCell) in HexFogView.cellList)
			{
				hexCell.fogItem.srcStatus = FogGridStatus.Lock;
				if (unlocked.Contains(id))
				{
					hexCell.fogItem.srcStatus = FogGridStatus.Unlocked;
				}
				if (unlocking.Contains(id))
				{
					hexCell.fogItem.srcStatus = FogGridStatus.Unlocking;
				}
				hexCell.fogItem.targetStatus = FogGridStatus.None;
			}
		}
	}
	
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
		cell.gameObject.name = $"{i}_{cell.coordinates}_{cell.coordinates.GetHexId()}";

		Text label = Instantiate<Text>(cellLabelPrefab);
		label.rectTransform.SetParent(gridCanvas.transform, false);
		label.rectTransform.anchoredPosition =
			new Vector2(position.x, position.z);
		label.text = cell.coordinates.ToStringOnSeparateLines();
	}
}