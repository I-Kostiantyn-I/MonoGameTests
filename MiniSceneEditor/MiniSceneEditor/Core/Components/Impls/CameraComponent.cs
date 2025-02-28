using Microsoft.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MiniSceneEditor.Core.Components.Abstractions;
using System;
using System.Collections.Generic;

namespace MiniSceneEditor.Core.Components.Impls;

public class CameraComponent : IComponent, IRenderable
{
	public SceneObject Owner { get; set; }

	// Основні параметри камери
	private float _fieldOfView = MathHelper.PiOver4;
	private float _nearPlane = 0.1f;
	private float _farPlane = 1000f;
	private float _aspectRatio = 1.0f;

	private GraphicsDevice _graphicsDevice;
	private BasicEffect _debugEffect;
	private VertexBuffer _cameraVertexBuffer;
	private float _visualSize = 0.5f; // Розмір візуального представлення камери
	private bool _isInitialized;

	public void SetGraphicsDevice(GraphicsDevice graphicsDevice)
	{
		_graphicsDevice = graphicsDevice;
		_debugEffect = new BasicEffect(graphicsDevice)
		{
			VertexColorEnabled = true,
			LightingEnabled = false
		};

		CreateCameraVisual();
		_isInitialized = true;
	}

	// Властивості з валідацією
	public float FieldOfView
	{
		get => _fieldOfView;
		set => _fieldOfView = MathHelper.Clamp(value, 0.1f, MathHelper.Pi);
	}

	public float NearPlane
	{
		get => _nearPlane;
		set => _nearPlane = Math.Max(0.001f, value);
	}

	public float FarPlane
	{
		get => _farPlane;
		set => _farPlane = Math.Max(_nearPlane + 0.1f, value);
	}

	public float AspectRatio
	{
		get => _aspectRatio;
		set => _aspectRatio = Math.Max(0.1f, value);
	}

	public void Initialize()
	{
		AspectRatio = 16.0f / 9.0f;
	}

	public void UpdateAspectRatio(float newAspectRatio)
	{
		AspectRatio = newAspectRatio;
	}

	public void Render(Matrix view, Matrix projection)
	{
		if (!_isInitialized)
		{
			return;
		}

		var worldMatrix = Owner.Transform.GetWorldMatrix();

		// Зберігаємо поточний стан
		var currentBlendState = _graphicsDevice.BlendState;
		var currentDepthStencilState = _graphicsDevice.DepthStencilState;
		var currentRasterizerState = _graphicsDevice.RasterizerState;

		try
		{
			// Встановлюємо необхідний стан
			_graphicsDevice.BlendState = BlendState.AlphaBlend; // Змінимо на AlphaBlend
			_graphicsDevice.DepthStencilState = DepthStencilState.Default;
			_graphicsDevice.RasterizerState = new RasterizerState
			{
				CullMode = CullMode.None,
				FillMode = FillMode.WireFrame // Спробуємо wireframe режим
			};

			_debugEffect.World = worldMatrix;
			_debugEffect.View = view;
			_debugEffect.Projection = projection;
			_debugEffect.Alpha = 1.0f; // Встановимо повну непрозорість

			foreach (EffectPass pass in _debugEffect.CurrentTechnique.Passes)
			{
				pass.Apply();
				_graphicsDevice.SetVertexBuffer(_cameraVertexBuffer);

				// Збільшимо розмір точок для кращої видимості
				_graphicsDevice.DrawPrimitives(
					PrimitiveType.LineList,
					0,
					9);
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error during render: {ex.Message}");
		}
		finally
		{
			// Відновлюємо стан
			_graphicsDevice.BlendState = currentBlendState;
			_graphicsDevice.DepthStencilState = currentDepthStencilState;
			_graphicsDevice.RasterizerState = currentRasterizerState;
		}
	}

	public void OnDestroy()
	{
		// Очищення ресурсів, якщо потрібно
		_debugEffect?.Dispose();
		_cameraVertexBuffer?.Dispose();
	}

	public Matrix GetViewMatrix()
	{
		// Створення матриці виду на основі Transform власника
		var transform = Owner.Transform;

		// Отримуємо напрямок погляду та вектор "вгору"
		Vector3 forward = Vector3.Transform(Vector3.Forward,
			Matrix.CreateRotationX(transform.Rotation.X) *
			Matrix.CreateRotationY(transform.Rotation.Y) *
			Matrix.CreateRotationZ(transform.Rotation.Z));

		Vector3 up = Vector3.Transform(Vector3.Up,
			Matrix.CreateRotationX(transform.Rotation.X) *
			Matrix.CreateRotationY(transform.Rotation.Y) *
			Matrix.CreateRotationZ(transform.Rotation.Z));

		return Matrix.CreateLookAt(
			transform.Position,
			transform.Position + forward,
			up);
	}

	public Matrix GetProjectionMatrix()
	{
		return Matrix.CreatePerspectiveFieldOfView(
			FieldOfView,
			AspectRatio,
			NearPlane,
			FarPlane);
	}

	// Допоміжні методи для роботи з камерою
	public Ray GetRayFromScreen(Point screenPoint)
	{
		// Перетворення координат екрану в промінь у світовому просторі
		Vector3 nearPoint = new Vector3(screenPoint.X, screenPoint.Y, 0);
		Vector3 farPoint = new Vector3(screenPoint.X, screenPoint.Y, 1);

		var viewProjection = GetViewMatrix() * GetProjectionMatrix();
		var inverse = Matrix.Invert(viewProjection);

		Vector3 nearWorld = Vector3.Transform(nearPoint, inverse);
		Vector3 farWorld = Vector3.Transform(farPoint, inverse);

		Vector3 direction = farWorld - nearWorld;
		direction.Normalize();

		return new Ray(nearWorld, direction);
	}

	public bool IsInViewFrustum(BoundingSphere sphere)
	{
		// Перевірка чи об'єкт знаходиться в полі зору камери
		var viewProjection = GetViewMatrix() * GetProjectionMatrix();
		return new BoundingFrustum(viewProjection).Intersects(sphere);
	}

	private void CreateCameraVisual()
	{
		var vertices = new List<VertexPositionColor>();
		float size = 2.0f; // Збільшимо розмір

		// Основа піраміди (близька площина)
		Vector3 topLeft = new Vector3(-size, size, 0);
		Vector3 topRight = new Vector3(size, size, 0);
		Vector3 bottomLeft = new Vector3(-size, -size, 0);
		Vector3 bottomRight = new Vector3(size, -size, 0);
		Vector3 cameraPos = new Vector3(0, 0, -size * 2);

		// Яскравіший колір
		Color mainColor = new Color(255, 0, 255); // Яскравий пурпурний
		Color directionColor = new Color(255, 255, 0); // Яскравий жовтий

		vertices.AddRange(new[]
		{
        // Передня грань (прямокутник)
        new VertexPositionColor(topLeft, mainColor),
		new VertexPositionColor(topRight, mainColor),

		new VertexPositionColor(topRight, mainColor),
		new VertexPositionColor(bottomRight, mainColor),

		new VertexPositionColor(bottomRight, mainColor),
		new VertexPositionColor(bottomLeft, mainColor),

		new VertexPositionColor(bottomLeft, mainColor),
		new VertexPositionColor(topLeft, mainColor),

        // Лінії до камери
        new VertexPositionColor(topLeft, mainColor),
		new VertexPositionColor(cameraPos, mainColor),

		new VertexPositionColor(topRight, mainColor),
		new VertexPositionColor(cameraPos, mainColor),

		new VertexPositionColor(bottomRight, mainColor),
		new VertexPositionColor(cameraPos, mainColor),

		new VertexPositionColor(bottomLeft, mainColor),
		new VertexPositionColor(cameraPos, mainColor),

        // Напрямок погляду (довша стрілка)
        new VertexPositionColor(cameraPos, directionColor),
		new VertexPositionColor(new Vector3(0, 0, size * 6), directionColor),
	});

		_cameraVertexBuffer = new VertexBuffer(
			_graphicsDevice,
			typeof(VertexPositionColor),
			vertices.Count,
			BufferUsage.WriteOnly);

		_cameraVertexBuffer.SetData(vertices.ToArray());
	}
}