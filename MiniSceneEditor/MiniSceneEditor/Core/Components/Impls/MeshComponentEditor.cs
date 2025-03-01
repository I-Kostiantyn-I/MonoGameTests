using ImGuiNET;
using MiniSceneEditor.Core.Components.Abstractions;
using MiniSceneEditor.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor.Core.Components.Impls;

public class MeshComponentEditor : ComponentEditor<MeshComponent>
{
	public MeshComponentEditor(MeshComponent component) : base(component) { }

	public override void OnInspectorGUI()
	{
		var meshTypes = Enum.GetNames(typeof(MeshType));
		var currentType = (int)Component.MeshType;

		if (ImGui.Combo("Mesh Type", ref currentType, meshTypes, meshTypes.Length))
		{
			Component.MeshType = (MeshType)currentType;
		}

		switch (Component.MeshType)
		{
			case MeshType.Box:
				DrawBoxParameters();
				break;
			case MeshType.Cylinder:
				DrawCylinderParameters();
				break;
			case MeshType.Capsule:
				DrawCapsuleParameters();
				break;
		}
	}

	private void DrawBoxParameters()
	{
		var dimensions = Component._boxDimensions.ToNumerics();
		if (ImGui.DragFloat3("Dimensions", ref dimensions, 0.1f, 0.1f, 100f))
		{
			Component.SetBoxDimensions(dimensions.ToXNA());
		}
	}

	private void DrawCylinderParameters()
	{
		// Реалізація UI для параметрів циліндра
	}

	private void DrawCapsuleParameters()
	{
		// Реалізація UI для параметрів капсули
	}
}