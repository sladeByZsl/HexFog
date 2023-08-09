using System.Collections.Generic;
using UnityEngine;
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

	public TestHexFog TestHexFog;

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
		if (Input.GetMouseButtonDown(0)) {
			Debug.LogError("开启迷雾");
			Vector3 cellPos = Vector3.zero;
			float dir = 3.14f;
			HandleInput(true,out cellPos);
			List<Vector3> cellPosList = new List<Vector3>(){cellPos};
			List<float> dirList = new List<float>() { dir};
			TestHexFog.DrawHexAync2(cellPosList.ToArray(),dirList.ToArray(),false);
		}
		if (Input.GetMouseButtonDown(1)) {
			//关闭
			Debug.LogError("关闭迷雾");
			Vector3 cellPos = Vector3.zero;
			float dir = 3.14f;
			HandleInput(false,out cellPos);
			List<Vector3> cellPosList = new List<Vector3>(){cellPos};
			List<float> dirList = new List<float>() { dir};
			TestHexFog.DrawHexAync2(cellPosList.ToArray(),dirList.ToArray(),true);
		}
	}

	void HandleInput (bool select,out Vector3 cellPos) {
		cellPos = Vector3.zero;
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(inputRay, out hit)) {
			TouchCell(hit.point,select,out cellPos);
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