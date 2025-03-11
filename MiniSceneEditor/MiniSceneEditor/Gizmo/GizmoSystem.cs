using ImGuiNET;
using Microsoft.Core;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MiniSceneEditor.Camera;
using MiniSceneEditor.Commands;
using System.Collections.Generic;

namespace MiniSceneEditor.Gizmo;
public class GizmoSystem
{
	private GraphicsDevice _graphicsDevice;
	private readonly CommandManager _commandManager;
	private readonly SnapSystem _snapSystem;
	private BasicEffect _effect;
	private Dictionary<GizmoType, IGizmo> _gizmos;
	private IGizmo _currentGizmo;
	private SceneObject _targetObject;
	private bool _isVisible = true;

	private bool _showDebug = true;

	public IGizmo CurrentGizmo => _currentGizmo;

	public enum GizmoType
	{
		Translate,
		Rotate,
		Scale,
		MeshEdit
	}

	public GizmoSystem(GraphicsDevice graphicsDevice, CommandManager commandManager, SnapSystem snapSystem)
	{
		_graphicsDevice = graphicsDevice;
		_commandManager = commandManager;
		_snapSystem = snapSystem;
		_effect = new BasicEffect(graphicsDevice);
		_gizmos = new Dictionary<GizmoType, IGizmo>();

		InitializeGizmos();

		SetCurrentGizmo(GizmoType.Scale);
	}

	public void SetCurrentGizmo(GizmoType type)
	{
		if (_gizmos.TryGetValue(type, out var gizmo))
		{
			_currentGizmo = gizmo;
		}
	}

	public void SetTarget(SceneObject target)
	{
		_targetObject = target;
	}

	private void InitializeGizmos()
	{
		_gizmos[GizmoType.Translate] = new TranslationGizmo(_graphicsDevice, _commandManager, _snapSystem);
		_gizmos[GizmoType.Rotate] = new RotationGizmo(_graphicsDevice, _commandManager, _snapSystem);
		_gizmos[GizmoType.Scale] = new ScaleGizmo(_graphicsDevice, _commandManager, _snapSystem);
		//_gizmos[GizmoType.MeshEdit] = new MeshEditGizmo(_graphicsDevice, _commandManager, _snapSystem);
	}

	public void Draw(CameraMatricesState camera)
	{
		if (!_isVisible || _targetObject == null || _currentGizmo == null)
		{
			System.Diagnostics.Debug.WriteLine("Gizmo not drawing because:");
			if (!_isVisible) System.Diagnostics.Debug.WriteLine("- Not visible");
			if (_targetObject == null) System.Diagnostics.Debug.WriteLine("- No target");
			if (_currentGizmo == null) System.Diagnostics.Debug.WriteLine("- No current gizmo");
			return;
		}

		_effect.View = camera.ViewMatrix;
		_effect.Projection = camera.ProjectionMatrix;

		_currentGizmo.Draw(_effect, _targetObject.Transform);
	}

	public void HandleInput(InputState inputState, CameraMatricesState camera)
	{
		if (_currentGizmo != null)
		{
			// Перемикання режимів гізмо
			if (inputState.IsKeyPressed(Keys.T)) SetCurrentGizmo(GizmoType.Translate);
			if (inputState.IsKeyPressed(Keys.R)) SetCurrentGizmo(GizmoType.Rotate);
			if (inputState.IsKeyPressed(Keys.E)) SetCurrentGizmo(GizmoType.Scale);

			_currentGizmo.HandleInput(inputState, camera, _targetObject.Transform);
		}
	}

	public void DrawDebugInfo()
	{
		if (!_showDebug) return;

		if (ImGui.Begin("Gizmo Debug"))
		{
			ImGui.Text($"Current Gizmo: {_currentGizmo?.GetType().Name ?? "None"}");
			ImGui.Text($"Target Object: {_targetObject?.Name ?? "None"}");

			if (_targetObject != null)
			{
				var transform = _targetObject.Transform;
				ImGui.Text("Transform before Gizmo operation:");
				ImGui.Text($"Position: {transform.Position}");
				ImGui.Text($"Rotation: {transform.Rotation}");
				ImGui.Text($"Scale: {transform.Scale}");
			}
		}
		ImGui.End();
	}
}