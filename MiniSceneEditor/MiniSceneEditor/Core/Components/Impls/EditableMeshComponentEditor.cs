using ImGuiNET;
using MiniSceneEditor.Core.Components.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor.Core.Components.Impls;

public class EditableMeshComponentEditor : ComponentEditor<EditableMeshComponent>
{
	public EditableMeshComponentEditor(EditableMeshComponent component) : base(component) { }

	public override void OnInspectorGUI()
	{
		// Режими редагування
		string[] modes = Enum.GetNames(typeof(EditMode));
		int currentMode = (int)Component.CurrentEditMode;

		if (ImGui.Combo("Edit Mode", ref currentMode, modes, modes.Length))
		{
			Component.CurrentEditMode = (EditMode)currentMode;
		}

		// Інструменти редагування залежно від режиму
		switch (Component.CurrentEditMode)
		{
			case EditMode.Vertex:
				DrawVertexTools();
				break;
			case EditMode.Edge:
				DrawEdgeTools();
				break;
			case EditMode.Face:
				DrawFaceTools();
				break;
		}
	}

	private void DrawVertexTools() { /* ... */ }
	private void DrawEdgeTools() { /* ... */ }
	private void DrawFaceTools() { /* ... */ }
}