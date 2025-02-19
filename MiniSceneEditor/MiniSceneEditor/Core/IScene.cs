using Microsoft.Xna.Framework;
using System.Collections.Generic;

public interface IScene
{
	// Основні системні об'єкти (обов'язкові)
	SceneObject MainCamera { get; }
	SceneObject DirectionalLight { get; }

	// Управління об'єктами
	uint RegisterObject(SceneObject obj);
	bool UnregisterObject(uint id);
	SceneObject GetObject(uint id);

	// Отримання ієрархії
	IEnumerable<SceneObject> GetRootObjects();

	// Базові операції
	void Initialize();
	void Update(GameTime gameTime);
	void Draw(Matrix view, Matrix projection, GameTime gameTime = null);

	// Серіалізація
	void SaveToFile(string path);
	void LoadFromFile(string path);
}