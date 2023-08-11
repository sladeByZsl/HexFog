using Elex.HexFog;
using UnityEngine;

public class HexCell : MonoBehaviour {

	public HexCoordinates coordinates;

	public Color color;

	public FogItem fogItem;
	
	private void Start()
	{
		fogItem = new FogItem
		{
			key = coordinates.ToString(),
			targetStatus = FogGridStatus.None,
			srcStatus = FogGridStatus.Lock
		};
		HexFogView.RegisterCell(this);
	}

	public Vector3 GetPos()
	{
		return this.transform.position;
	}
}