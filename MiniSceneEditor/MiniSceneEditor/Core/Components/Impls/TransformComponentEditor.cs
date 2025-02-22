using ImGuiNET;
using MiniSceneEditor.Core.Components.Abstractions;
using MiniSceneEditor.Core.Utils;

namespace MiniSceneEditor.Core.Components.Impls;
public class TransformComponentEditor : ComponentEditor<TransformComponent>
{
	public TransformComponentEditor(TransformComponent component) : base(component) { }

	public override void OnInspectorGUI()
	{
		var position = Component.Position.ToNumerics();
		if (ImGui.DragFloat3("Position", ref position, 0.1f))
		{
			Component.Position = position.ToXNA();
		}

		var rotation = Component.Rotation.ToNumerics();
		if (ImGui.DragFloat3("Rotation", ref rotation, 0.1f))
		{
			Component.Rotation = rotation.ToXNA();
		}
	}
}
