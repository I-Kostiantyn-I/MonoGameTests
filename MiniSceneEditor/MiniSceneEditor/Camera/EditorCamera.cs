using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MiniSceneEditor;
using System;

public class EditorCamera
{
	private GraphicsDevice _graphicsDevice;
	private float _zoomSpeed = 0.1f;
	private Vector3 _position;
	private float _yaw;   // Горизонтальний поворот
	private float _pitch; // Вертикальний поворот
	private float _moveSpeed = 10.0f;
	private float _rotationSpeed = 0.01f;
	private Vector2 _lastMousePosition;
	private bool _isRotating;
	private const float DEFAULT_DISTANCE = 15.0f; // Стандартна відстань для огляду об'єкта


	// Параметри для фокусування
	private bool _isLookingAtTarget;
	private Vector3 _targetPosition;
	private float _smoothLookSpeed = 5.0f;
	private float _speedMultiplier = 2.0f;
	private float _speedDivider = 0.5f;
	private EditorLogger _log;

	public EditorCamera(GraphicsDevice graphicsDevice)
	{
		_graphicsDevice = graphicsDevice;
		_position = new Vector3(0, 5, 10);
		_yaw = 0;
		_pitch = 0;
		_moveSpeed = 10.0f;
		_log = new EditorLogger(nameof(EditorCamera));
	}

	public Vector3 Position
	{
		get => _position;
		set => _position = value;
	}

	public Vector3 RotationDegrees
	{
		get
		{
			return new Vector3(
				MathHelper.ToDegrees(NormalizeDegrees(_pitch)),
				MathHelper.ToDegrees(NormalizeDegrees(_yaw)),
				0);
		}
	}

	// Властивість для отримання поточного повороту в радіанах
	public Vector3 RotationRadians
	{
		get => new Vector3(_pitch, _yaw, 0);
		set
		{
			_pitch = value.X;
			_yaw = value.Y;
			// Z ігноруємо
		}
	}

	public void SetRotation(Vector3 rotationDegrees)
	{
		_pitch = MathHelper.ToRadians(MathHelper.Clamp(rotationDegrees.X, -89f, 89f));
		_yaw = NormalizeDegrees(MathHelper.ToRadians(rotationDegrees.Y));
		// Z ігноруємо, оскільки камера не має крену
	}

	public Matrix GetViewMatrix()
	{
		var rotation = Matrix.CreateRotationX(_pitch) * Matrix.CreateRotationY(_yaw);
		var forward = Vector3.Transform(Vector3.Forward, rotation);
		var up = Vector3.Transform(Vector3.Up, rotation);

		return Matrix.CreateLookAt(_position, _position + forward, up);
	}

	public Matrix GetProjectionMatrix()
	{
		float aspectRatio = _graphicsDevice.Viewport.AspectRatio;
		return Matrix.CreatePerspectiveFieldOfView(
			MathHelper.ToRadians(45f), // FOV
			aspectRatio,
			0.1f,  // Near plane
			1000f  // Far plane
		);
	}

	public void FocusOn(Vector3 targetPosition, bool withTransition)
	{
		_log.Log($"\nStarting focus on target: {targetPosition}");
		_log.Log($"Current camera position: {_position}");

		if (!withTransition)
		{

			_position = targetPosition - GetForwardVector() * 10f;

			//// Відступаємо на 15 одиниць по Z від цілі
			//_position = targetPosition + new Vector3(0, 0, 15);

			//// Розраховуємо вектор напрямку до цілі
			//Vector3 lookDir = targetPosition - _position;
			//lookDir.Normalize();

			//// Розраховуємо кути
			//_yaw = 0;  // Оскільки ми дивимось прямо, кут повороту 0
			//_pitch = -(float)Math.Asin(lookDir.Y); // Кут нахилу

			//_log.Log($"New position: {_position}");
			//_log.Log($"Look direction: {lookDir}");
			//_log.Log($"Angles - Yaw: {MathHelper.ToDegrees(_yaw):F2}°, Pitch: {MathHelper.ToDegrees(_pitch):F2}°");
		}
	}

	public void Update(GameTime gameTime, InputState inputState)
	{
		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

		if (_isLookingAtTarget)
		{
			UpdateFocusing(deltaTime);
		}
		else
		{
			HandleKeyboardInput(deltaTime, inputState.CurrentKeyboard);
			HandleMouseInput(inputState.CurrentMouse);
		}
	}

	private void HandleKeyboardInput(float deltaTime, KeyboardState keyboard)
	{
		// Базова швидкість руху (можливо, потрібно збільшити це значення)
		var moveSpeed = _moveSpeed;// * 60.0f; // Множимо на 60 для компенсації deltaTime

		// Модифікатори швидкості
		if (keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift))
			moveSpeed *= _speedMultiplier;
		if (keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt))
			moveSpeed *= _speedDivider;

		// Розраховуємо напрямки руху відносно повороту камери
		var forward = Vector3.Transform(Vector3.Forward, Matrix.CreateRotationY(_yaw));
		var right = Vector3.Transform(Vector3.Right, Matrix.CreateRotationY(_yaw));

		Vector3 moveVector = Vector3.Zero;

		if (keyboard.IsKeyDown(Keys.W))
			moveVector += forward;
		if (keyboard.IsKeyDown(Keys.S))
			moveVector -= forward;
		if (keyboard.IsKeyDown(Keys.A))
			moveVector -= right;
		if (keyboard.IsKeyDown(Keys.D))
			moveVector += right;

		if (keyboard.IsKeyDown(Keys.Space))
			moveVector += Vector3.Up;
		if (keyboard.IsKeyDown(Keys.LeftControl))
			moveVector -= Vector3.Up;

		if (moveVector != Vector3.Zero)
		{
			moveVector.Normalize();
			_position += moveVector * moveSpeed * deltaTime;
			_log.Log($"Position: {_position}");
		}
	}

	private void HandleMouseInput(MouseState mouse)
	{
		if (ImGui.GetIO().WantCaptureMouse)
			return;

		if (mouse.RightButton == ButtonState.Pressed)
		{
			if (!_isRotating)
			{
				_lastMousePosition = new Vector2(mouse.X, mouse.Y);
				_isRotating = true;
			}
			else
			{
				float deltaX = (mouse.X - _lastMousePosition.X) * _rotationSpeed;
				float deltaY = (mouse.Y - _lastMousePosition.Y) * _rotationSpeed;

				_yaw = NormalizeDegrees(_yaw - deltaX);
				_pitch -= deltaY;
				_pitch = MathHelper.Clamp(_pitch, -MathHelper.PiOver2 + 0.1f, MathHelper.PiOver2 - 0.1f);

				_log.Log($"Rotation: Yaw={MathHelper.ToDegrees(_yaw):F2}°, Pitch={MathHelper.ToDegrees(_pitch):F2}°");

				_lastMousePosition = new Vector2(mouse.X, mouse.Y);
			}
		}
		else
		{
			_isRotating = false;
		}
	}

	private bool IsUserInput()
	{
		var keyboard = Keyboard.GetState();
		var mouse = Mouse.GetState();

		return keyboard.IsKeyDown(Keys.W) ||
			   keyboard.IsKeyDown(Keys.S) ||
			   keyboard.IsKeyDown(Keys.A) ||
			   keyboard.IsKeyDown(Keys.D) ||
			   keyboard.IsKeyDown(Keys.Space) ||
			   keyboard.IsKeyDown(Keys.LeftControl) ||
			   mouse.RightButton == ButtonState.Pressed;
	}

	private void DrawCameraSettings()
	{
		if (ImGui.Begin("Camera Settings"))
		{
			float moveSpeed = _moveSpeed;
			if (ImGui.DragFloat("Move Speed", ref moveSpeed, 0.01f, 0.01f, 1.0f))
			{
				//SetMoveSpeed(moveSpeed);
			}

			float rotationSpeed = _rotationSpeed;
			if (ImGui.DragFloat("Rotation Speed", ref rotationSpeed, 0.001f, 0.001f, 0.1f))
			{
				//SetRotationSpeed(rotationSpeed);
			}

			float zoomSpeed = _zoomSpeed;
			if (ImGui.DragFloat("Zoom Speed", ref zoomSpeed, 0.01f, 0.01f, 1.0f))
			{
				//SetZoomSpeed(zoomSpeed);
			}
		}
		ImGui.End();
	}

	private void UpdateFocusing(float deltaTime)
	{
		if (!_isLookingAtTarget)
			return;

		// Розрахунок напрямку до цілі
		Vector3 direction = _targetPosition - _position;
		direction.Normalize();

		// Розрахунок цільових кутів
		float targetYaw = (float)Math.Atan2(direction.X, direction.Z);
		float targetPitch = (float)Math.Atan2(-direction.Y,
			Math.Sqrt(direction.X * direction.X + direction.Z * direction.Z));

		// Плавне обертання
		_yaw = MathHelper.Lerp(_yaw, targetYaw, deltaTime * _smoothLookSpeed);
		_pitch = MathHelper.Lerp(_pitch, targetPitch, deltaTime * _smoothLookSpeed);

		// Плавне переміщення до цільової позиції
		Vector3 targetCameraPosition = _targetPosition - direction * DEFAULT_DISTANCE;
		_position = Vector3.Lerp(_position, targetCameraPosition, deltaTime * _smoothLookSpeed);

		_log.Log($"Focusing - Position: {_position}");
		_log.Log($"Target Position: {_targetPosition}");
		_log.Log($"Direction: {direction}");
		_log.Log($"Current angles - Yaw: {MathHelper.ToDegrees(_yaw):F2}°, Pitch: {MathHelper.ToDegrees(_pitch):F2}°");

		// Перевірка завершення
		if (Vector3.Distance(_position, targetCameraPosition) < 0.01f)
		{
			_isLookingAtTarget = false;
			_log.Log("Focus completed");
		}

		if (IsUserInput())
		{
			_isLookingAtTarget = false;
			_log.Log("Focus interrupted by user input");
		}
	}

	private float NormalizeDegrees(float radians)
	{
		// Нормалізація кута до діапазону [0, 2π]
		float angle = radians % MathHelper.TwoPi;
		if (angle < 0)
			angle += MathHelper.TwoPi;
		return angle;
	}

	private Vector3 GetForwardVector()
	{
		return Vector3.Transform(Vector3.Forward,
			Matrix.CreateRotationX(_pitch) * Matrix.CreateRotationY(_yaw));
	}
}