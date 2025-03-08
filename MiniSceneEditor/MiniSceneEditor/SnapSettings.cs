namespace MiniSceneEditor;

public class SnapSettings
{
	public bool EnablePositionSnap { get; set; } = false;
	public bool EnableRotationSnap { get; set; } = false;
	public bool EnableScaleSnap { get; set; } = false;

	public float PositionSnapValue { get; set; } = 1.0f;
	public float RotationSnapValue { get; set; } = 15.0f; // в градусах
	public float ScaleSnapValue { get; set; } = 0.5f;

	public bool EnableRelativeSnap { get; set; } = false;
	public bool SnapToGrid { get; set; } = true;
	public bool SnapToObjects { get; set; } = false;
}