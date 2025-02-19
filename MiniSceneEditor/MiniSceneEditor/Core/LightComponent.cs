using Microsoft.Xna.Framework;

public class LightComponent : IComponent
{
	public SceneObject Owner { get; set; }

	// Основні параметри світла
	public Vector3 Color { get; set; } = Vector3.One; // Білий колір за замовчуванням
	public float Intensity { get; set; } = 1.0f;
	public bool CastShadows { get; set; } = true;

	// Специфічні параметри для спрямованого світла
	public Vector3 Direction
	{
		get
		{
			// Обчислюємо напрямок на основі повороту об'єкта
			return Vector3.Transform(
				Vector3.Forward,
				Matrix.CreateRotationX(Owner.Transform.Rotation.X) *
				Matrix.CreateRotationY(Owner.Transform.Rotation.Y) *
				Matrix.CreateRotationZ(Owner.Transform.Rotation.Z));
		}
	}

	// Параметри для тіней (якщо вони підтримуються)
	public float ShadowStrength { get; set; } = 1.0f;
	public float ShadowBias { get; set; } = 0.005f;

	// Допоміжні параметри для налаштування освітлення
	public Vector3 Ambient { get; set; } = new Vector3(0.1f); // Фонове освітлення
	public float FalloffStart { get; set; } = 0.0f; // Для м'якого переходу тіней

	public void Initialize()
	{
		// Встановлення початкових значень
	}

	public void OnDestroy()
	{
		// Очищення ресурсів, якщо потрібно
	}

	// Отримання даних для шейдера
	public LightData GetLightData()
	{
		return new LightData
		{
			Direction = Direction,
			Color = Color,
			Intensity = Intensity,
			Ambient = Ambient
		};
	}
}

// Структура для передачі даних у шейдер
public struct LightData
{
	public Vector3 Direction;
	public Vector3 Color;
	public float Intensity;
	public Vector3 Ambient;
}