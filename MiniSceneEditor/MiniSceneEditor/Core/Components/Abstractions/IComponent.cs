namespace MiniSceneEditor.Core.Components.Abstractions;

public interface IComponent
{
	SceneObject Owner { get; set; }
	void Initialize();
	void OnDestroy();
}
