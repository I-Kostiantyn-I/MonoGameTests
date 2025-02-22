namespace MiniSceneEditor.Core.Components.Abstractions;

public abstract class ComponentEditor<T> : IComponentEditor where T : IComponent
{
	protected T Component { get; }

	protected ComponentEditor(T component)
	{
		Component = component;
	}

	public abstract void OnInspectorGUI();
}
