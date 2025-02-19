using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;

public class Camera
{
	private Vector3 _position;
	private Vector3 _rotation;
	private Vector3 _target;
	private bool _isLookingAtTarget;
	private float _smoothLookSpeed = 5f;
	private readonly GraphicsDevice _graphicsDevice;

	public Vector3 Position
	{
		get => _position;
		set => _position = value;
	}

	public Vector3 Rotation
	{
		get => _rotation;
		set => _rotation = value;
	}

	public float AspectRatio { get; set; }
	public float FieldOfView { get; set; }
	public float NearPlane { get; set; }
	public float FarPlane { get; set; }

	public Camera(GraphicsDevice graphicsDevice)
	{
		_position = new Vector3(20, 20, 20);
		_rotation = new Vector3(0, 0, 0);
		AspectRatio = graphicsDevice.Viewport.AspectRatio;
		FieldOfView = MathHelper.PiOver4;
		NearPlane = 0.1f;
		FarPlane = 100f;
		_isLookingAtTarget = false;
		_graphicsDevice = graphicsDevice;
	}

	public void LookAt(Vector3 target)
	{
		_target = target;
		_isLookingAtTarget = true;
	}

	public void Update(GameTime gameTime)
	{
		if (_isLookingAtTarget)
		{
			// Плавне обертання до цілі
			Vector3 directionToTarget = _target - _position;
			directionToTarget.Normalize();

			// Обчислення кутів повороту до цілі
			float targetYaw = (float)Math.Atan2(directionToTarget.X, directionToTarget.Z);
			float targetPitch = (float)Math.Asin(directionToTarget.Y);

			// Плавне обертання
			float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_rotation.Y = MathHelper.Lerp(_rotation.Y, targetYaw, deltaTime * _smoothLookSpeed);
			_rotation.X = MathHelper.Lerp(_rotation.X, targetPitch, deltaTime * _smoothLookSpeed);

			// Перевірка, чи досягнуто цілі
			float distanceToTarget = Vector2.Distance(
				new Vector2(_rotation.X, _rotation.Y),
				new Vector2(targetPitch, targetYaw));

			if (distanceToTarget < 0.01f)
			{
				_isLookingAtTarget = false;
			}
		}
	}

	public Matrix GetViewMatrix()
	{
		Vector3 forward = Vector3.Transform(Vector3.Forward,
			Matrix.CreateRotationX(_rotation.X) *
			Matrix.CreateRotationY(_rotation.Y) *
			Matrix.CreateRotationZ(_rotation.Z));

		Vector3 up = Vector3.Transform(Vector3.Up,
			Matrix.CreateRotationX(_rotation.X) *
			Matrix.CreateRotationY(_rotation.Y) *
			Matrix.CreateRotationZ(_rotation.Z));

		return Matrix.CreateLookAt(
			_position,
			_position + forward,
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
}