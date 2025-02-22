using MiniSceneEditor.Core.Components.Abstractions;
using System;
using System.Collections.Generic;

namespace MiniSceneEditor.Core.Components;

// Фабрика для створення редакторів
public static class ComponentEditorFactory
{
	private static Dictionary<Type, Type> _editorTypes = new();

	public static void RegisterEditor<TComponent, TEditor>()
		where TComponent : IComponent
		where TEditor : ComponentEditor<TComponent>
	{
		_editorTypes[typeof(TComponent)] = typeof(TEditor);
	}

	public static IComponentEditor CreateEditor(IComponent component)
	{
		if (_editorTypes.TryGetValue(component.GetType(), out var editorType))
		{
			return (IComponentEditor)Activator.CreateInstance(editorType, component);
		}
		return null;
	}
}