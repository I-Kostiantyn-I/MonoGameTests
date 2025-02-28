using ImGuiNET;
using Microsoft.Core;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor;

public class SnapSystem
{
	private readonly SnapSettings _settings;
	private readonly Scene _scene;

	public SnapSettings Settings => _settings;

	public SnapSystem(Scene scene)
	{
		_settings = new SnapSettings();
		_scene = scene;
	}

	public Vector3 SnapPosition(Vector3 position, Vector3 originalPosition)
	{
		if (!_settings.EnablePositionSnap)
			return position;

		Vector3 snappedPosition = position;

		if (_settings.SnapToGrid)
		{
			snappedPosition = new Vector3(
				SnapToInterval(position.X, _settings.PositionSnapValue),
				SnapToInterval(position.Y, _settings.PositionSnapValue),
				SnapToInterval(position.Z, _settings.PositionSnapValue)
			);
		}

		if (_settings.SnapToObjects)
		{
			snappedPosition = SnapToNearestObject(snappedPosition);
		}

		if (_settings.EnableRelativeSnap)
		{
			Vector3 delta = position - originalPosition;
			delta = new Vector3(
				SnapToInterval(delta.X, _settings.PositionSnapValue),
				SnapToInterval(delta.Y, _settings.PositionSnapValue),
				SnapToInterval(delta.Z, _settings.PositionSnapValue)
			);
			snappedPosition = originalPosition + delta;
		}

		return snappedPosition;
	}

	public Vector3 SnapRotation(Vector3 rotation)
	{
		if (!_settings.EnableRotationSnap)
			return rotation;

		float snapAngle = MathHelper.ToRadians(_settings.RotationSnapValue);
		return new Vector3(
			SnapToInterval(rotation.X, snapAngle),
			SnapToInterval(rotation.Y, snapAngle),
			SnapToInterval(rotation.Z, snapAngle)
		);
	}

	public Vector3 SnapScale(Vector3 scale)
	{
		if (!_settings.EnableScaleSnap)
			return scale;

		return new Vector3(
			SnapToInterval(scale.X, _settings.ScaleSnapValue),
			SnapToInterval(scale.Y, _settings.ScaleSnapValue),
			SnapToInterval(scale.Z, _settings.ScaleSnapValue)
		);
	}

	private float SnapToInterval(float value, float interval)
	{
		return MathF.Round(value / interval) * interval;
	}

	private Vector3 SnapToNearestObject(Vector3 position)
	{
		float snapDistance = _settings.PositionSnapValue;
		Vector3 closestPoint = position;
		float minDistance = float.MaxValue;

		foreach (var obj in _scene.GetRootObjects())
		{
			Vector3 objPosition = obj.Transform.Position;
			float distance = Vector3.Distance(position, objPosition);

			if (distance < minDistance && distance < snapDistance)
			{
				minDistance = distance;
				closestPoint = objPosition;
			}
		}

		return closestPoint;
	}

	public void DrawGUI()
	{
		//if (ImGui.Begin("Snap Settings"))
		//{
		//	ImGui.Checkbox("Enable Position Snap", ref _settings.EnablePositionSnap);
		//	if (_settings.EnablePositionSnap)
		//	{
		//		ImGui.Indent();
		//		float positionSnap = _settings.PositionSnapValue;
		//		if (ImGui.DragFloat("Grid Size", ref positionSnap, 0.1f, 0.1f, 10.0f))
		//		{
		//			_settings.PositionSnapValue = positionSnap;
		//		}
		//		ImGui.Checkbox("Snap to Grid", ref _settings.SnapToGrid);
		//		ImGui.Checkbox("Snap to Objects", ref _settings.SnapToObjects);
		//		ImGui.Checkbox("Relative Snap", ref _settings.EnableRelativeSnap);
		//		ImGui.Unindent();
		//	}

		//	ImGui.Checkbox("Enable Rotation Snap", ref _settings.EnableRotationSnap);
		//	if (_settings.EnableRotationSnap)
		//	{
		//		ImGui.Indent();
		//		float rotationSnap = _settings.RotationSnapValue;
		//		if (ImGui.DragFloat("Angle", ref rotationSnap, 1.0f, 1.0f, 90.0f))
		//		{
		//			_settings.RotationSnapValue = rotationSnap;
		//		}
		//		ImGui.Unindent();
		//	}

		//	ImGui.Checkbox("Enable Scale Snap", ref _settings.EnableScaleSnap);
		//	if (_settings.EnableScaleSnap)
		//	{
		//		ImGui.Indent();
		//		float scaleSnap = _settings.ScaleSnapValue;
		//		if (ImGui.DragFloat("Scale Step", ref scaleSnap, 0.1f, 0.1f, 2.0f))
		//		{
		//			_settings.ScaleSnapValue = scaleSnap;
		//		}
		//		ImGui.Unindent();
		//	}
		//}
		//ImGui.End();
	}
}