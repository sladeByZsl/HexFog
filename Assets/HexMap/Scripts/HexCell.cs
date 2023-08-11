using System.Collections;
using Elex.HexFog;
using UnityEngine;

public class HexCell : MonoBehaviour {

	public HexCoordinates coordinates;

	public Color color;
	
	private void Start()
	{
		HexFogView.RegisterCell(this);
	}
	public float Visibility { get; private set; }
	private bool isVisible = false;
	private void OnMouseDown()
	{
		ToggleVisibility();
	}

	/// <summary>
	/// ...or when you drag the clicked mouse over it
	/// </summary>
	private void OnMouseEnter()
	{
		ToggleVisibility();
	}

	/// <summary>
	/// Toggle the visibility and lerp to the new value from the current one
	/// Interupts itself if still in a animation
	/// </summary>
	private void ToggleVisibility()
	{
		if (!Input.GetMouseButton(0))
			return;

		isVisible = !isVisible;
		StopAllCoroutines();
		StartCoroutine(AnimateVisibility(isVisible ? 1.0f : 0.0f));
	}

	/// <summary>
	/// Visibility toggle animation
	/// Pretty basic animation coroutine, the animation takes 1 second
	/// </summary>
	/// <param name="targetVal">Visibility value to end up with</param>
	/// <returns>Yield</returns>
	private IEnumerator AnimateVisibility(float targetVal)
	{
		float startingTime = Time.time;
		float startingVal = Visibility;
		float lerpVal = 0.0f;
		while(lerpVal < 1.0f)
		{
			lerpVal = (Time.time - startingTime) / 1.0f;
			Visibility = Mathf.Lerp(startingVal, targetVal, lerpVal);
			yield return null;
		}
		Visibility = targetVal;
	}
}