using ImGuiNET;
using MiniSceneEditor.Core.Components.Abstractions;

namespace MiniSceneEditor.Core.Components.Impls;

public class RigidbodyComponentEditor : ComponentEditor<RigidbodyComponent>
{
	public RigidbodyComponentEditor(RigidbodyComponent component) : base(component)
	{
	}

	public override void OnInspectorGUI()
	{
		var component = Component;

		// Основні параметри
		var isStatic = component.IsStatic;
		if (ImGui.Checkbox("Is Static", ref isStatic))
		{
			component.IsStatic = isStatic;
		}

		if (!isStatic)
		{
			var mass = component.Mass;
			if (ImGui.DragFloat("Mass", ref mass, 0.1f, 0.001f, 1000f))
			{
				component.Mass = mass;
			}

			var useGravity = component.UseGravity;
			if (ImGui.Checkbox("Use Gravity", ref useGravity))
			{
				component.UseGravity = useGravity;
			}

			// Демпфінг
			var linearDamping = component.LinearDamping;
			if (ImGui.DragFloat("Linear Damping", ref linearDamping, 0.01f, 0f, 1f))
			{
				component.LinearDamping = linearDamping;
			}

			var angularDamping = component.AngularDamping;
			if (ImGui.DragFloat("Angular Damping", ref angularDamping, 0.01f, 0f, 1f))
			{
				component.AngularDamping = angularDamping;
			}
		}

		// Тригер
		var isTrigger = component.IsTrigger;
		if (ImGui.Checkbox("Is Trigger", ref isTrigger))
		{
			component.IsTrigger = isTrigger;
		}
	}
}