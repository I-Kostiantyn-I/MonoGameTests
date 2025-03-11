using ImGuiNET;
using MiniSceneEditor.Core.Components.Abstractions;
using MiniSceneEditor.Core.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor.Core.Components.Impls;

public class ColliderComponentEditor : ComponentEditor<ColliderComponent>
{
	public ColliderComponentEditor(ColliderComponent component) : base(component)
	{
	}

	public override void OnInspectorGUI()
	{
		var component = Component;

		// Вибір типу колайдера
		var currentType = (int)component.Type;
		var types = Enum.GetNames(typeof(ColliderType));
		if (ImGui.Combo("Collider Type", ref currentType, types, types.Length))
		{
			component.SetType((ColliderType)currentType);
		}

		// Параметри в залежності від типу
		switch (component.Type)
		{
			case ColliderType.Box:
				var size = component.Size.ToNumerics();
				if (ImGui.DragFloat3("Size", ref size, 0.1f))
				{
					component.Size = size.ToXNA();
					component.UpdateCollider();
				}
				break;

			case ColliderType.Sphere:
				var radius = component.Radius;
				if (ImGui.DragFloat("Radius", ref radius, 0.1f))
				{
					component.Radius = radius;
					component.UpdateCollider();
				}
				break;
		}

		// Відображення дебаг візуалізації
		bool showDebug = component.ShowDebug;
		if (ImGui.Checkbox("Show Debug", ref showDebug))
		{
			component.SetDebugVisible(showDebug);
		}
	}
}