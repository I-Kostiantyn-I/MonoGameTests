using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

public class EditorCamera
{
	private GraphicsDevice _graphicsDevice;
	private Vector3 _position;
	private Vector3 _rotation;
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
	private Vector3 _initialPosition;
	private float _initialYaw;
	private float _initialPitch;
	private float _targetYaw;
	private float _targetPitch;
	private float _focusDistance = 10.0f;
	private float _smoothLookSpeed = 5.0f;
	private float _speedMultiplier = 2.0f;
	private float _speedDivider = 0.5f;
	private EditorLogger _log;

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
		get
		{
			_rotation = new Vector3(_pitch, _yaw, 0);
			return _rotation;
		}
		set => _rotation = value;
	}

	public void SetRotation(Vector3 rotationDegrees)
	{
		_pitch = MathHelper.ToRadians(MathHelper.Clamp(rotationDegrees.X, -89f, 89f));
		_yaw = NormalizeDegrees(MathHelper.ToRadians(rotationDegrees.Y));
		// Z ігноруємо, оскільки камера не має крену
	}

	public EditorCamera(GraphicsDevice graphicsDevice)
	{
		_graphicsDevice = graphicsDevice;
		_position = new Vector3(0, 5, 10);
		_yaw = 0;
		_pitch = 0;
		_log = new EditorLogger(nameof(EditorCamera));
	}

	public void Update(GameTime gameTime)
	{
		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

		if (_isLookingAtTarget)
		{
			UpdateFocusing(deltaTime);
		}
		else
		{
			HandleKeyboardInput(deltaTime);
			HandleMouseInput();
		}
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
		return Matrix.CreatePerspectiveFieldOfView(
			MathHelper.PiOver4,
			_graphicsDevice.Viewport.AspectRatio,
			0.1f,
			1000f);
	}

	public void FocusOn(Vector3 targetPosition, bool withTransition)
	{
		_log.Log($"\nStarting focus on target: {targetPosition}");
		_log.Log($"Current camera position: {_position}");
		_log.Log($"Current angles - Yaw: {MathHelper.ToDegrees(_yaw):F2}°, Pitch: {MathHelper.ToDegrees(_pitch):F2}°");

		// Зберігаємо цільову позицію
		_targetPosition = targetPosition;

		// Розраховуємо позицію камери позаду об'єкта
		Vector3 targetCameraPosition = CalculateCameraPosition(targetPosition);

		if (!withTransition)
		{
			// Миттєве переміщення
			_position = targetCameraPosition;

			// Розрахунок напрямку погляду
			Vector3 direction = _targetPosition - _position;
			direction.Normalize();

			// Встановлення кутів
			_yaw = (float)Math.Atan2(direction.X, direction.Z);
			_pitch = (float)Math.Atan2(-direction.Y,
				Math.Sqrt(direction.X * direction.X + direction.Z * direction.Z));

			_log.Log($"New position: {_position}");
			_log.Log($"New angles - Yaw: {MathHelper.ToDegrees(_yaw):F2}°, Pitch: {MathHelper.ToDegrees(_pitch):F2}°");
		}
		else
		{
			_initialPosition = _position;
			_targetPosition = targetPosition;
			_isLookingAtTarget = true;
		}
	}

	private void LookAt(Vector3 targetPosition)
	{
		Vector3 direction = targetPosition - _position;
		direction.Normalize();

		_yaw = NormalizeDegrees((float)Math.Atan2(direction.X, direction.Z));
		_pitch = (float)Math.Atan2(-direction.Y,
			Math.Sqrt(direction.X * direction.X + direction.Z * direction.Z));
		_pitch = MathHelper.Clamp(_pitch, -MathHelper.PiOver2 + 0.1f, MathHelper.PiOver2 - 0.1f);

		_log.Log($"Look at position: {targetPosition}, Resulting angles: Yaw={MathHelper.ToDegrees(_yaw):F2}°, Pitch={MathHelper.ToDegrees(_pitch):F2}°");
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

	private void HandleKeyboardInput(float deltaTime)
	{
		var keyboard = Keyboard.GetState();
		var moveSpeed = _moveSpeed;

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

	private void HandleMouseInput()
	{
		var mouse = Mouse.GetState();

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

	private Vector3 CalculateCameraPosition(Vector3 targetPosition)
	{
		// Розміщуємо камеру позаду об'єкта
		return new Vector3(
			targetPosition.X,
			targetPosition.Y + 5.0f, // Трохи вище об'єкта
			targetPosition.Z + DEFAULT_DISTANCE // Позаду об'єкта
		);
	}

	private void UpdateFocusing(float deltaTime)
	{
		if (!_isLookingAtTarget)
			return;

		// Розраховуємо цільову позицію
		Vector3 targetCameraPosition = CalculateCameraPosition(_targetPosition);

		// Плавне переміщення
		_position = Vector3.Lerp(_position, targetCameraPosition, deltaTime * _smoothLookSpeed);

		// Розрахунок напрямку погляду
		Vector3 direction = _targetPosition - _position;
		direction.Normalize();

		// Плавне обертання
		float targetYaw = (float)Math.Atan2(direction.X, direction.Z);
		float targetPitch = (float)Math.Atan2(-direction.Y,
			Math.Sqrt(direction.X * direction.X + direction.Z * direction.Z));

		_yaw = MathHelper.Lerp(_yaw, targetYaw, deltaTime * _smoothLookSpeed);
		_pitch = MathHelper.Lerp(_pitch, targetPitch, deltaTime * _smoothLookSpeed);

		_log.Log($"Focusing - Position: {_position}");
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

	private float NormalizeAngle(float angle)
	{
		angle = angle % MathHelper.TwoPi;
		if (angle < 0)
			angle += MathHelper.TwoPi;
		return angle;
	}

	private float NormalizeDegrees(float radians)
	{
		// Нормалізація кута до діапазону [0, 2π]
		float angle = radians % MathHelper.TwoPi;
		if (angle < 0)
			angle += MathHelper.TwoPi;
		return angle;
	}
}











//using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Xna.Framework;
//using System;

//public class Camera
//{
//	private Vector3 _position;
//	private Vector3 _rotation;
//	private Vector3 _target;
//	private bool _isLookingAtTarget;
//	private float _smoothLookSpeed = 5f;
//	private readonly GraphicsDevice _graphicsDevice;

//	private Vector3 _targetPosition;
//	private Vector3 _targetRotation;
//	private const float CAMERA_DISTANCE = 10f; // Відстань від об'єкта при фокусуванні

//	// Параметри руху
//	private float _moveSpeed = 10.0f;
//	private float _rotationSpeed = 0.01f;
//	private float _speedMultiplier = 2.0f; // Для Shift
//	private float _speedDivider = 0.5f; // Для Alt
//	private Vector2 _lastMousePosition;
//	private bool _isRotating;

//	public Vector3 Position
//	{
//		get => _position;
//		set => _position = value;
//	}

//	public Vector3 Rotation
//	{
//		get => _rotation;
//		set => _rotation = value;
//	}

//	public float AspectRatio { get; set; }
//	public float FieldOfView { get; set; }
//	public float NearPlane { get; set; }
//	public float FarPlane { get; set; }

//	public Camera(GraphicsDevice graphicsDevice)
//	{
//		_position = new Vector3(20, 20, 20);
//		_rotation = new Vector3(0, 0, 0);
//		AspectRatio = graphicsDevice.Viewport.AspectRatio;
//		FieldOfView = MathHelper.PiOver4;
//		NearPlane = 0.1f;
//		FarPlane = 100f;
//		_isLookingAtTarget = false;
//		_graphicsDevice = graphicsDevice;
//	}

//	public void LookAt(Vector3 target)
//	{
//		_target = target;
//		_isLookingAtTarget = true;
//	}

//	public void Update(GameTime gameTime)
//	{
//		if (_isLookingAtTarget)
//		{
//			float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

//			// Плавне переміщення до цільової позиції
//			Position = Vector3.Lerp(Position, _targetPosition, deltaTime * _smoothLookSpeed);
//			Rotation = Vector3.Lerp(Rotation, _targetRotation, deltaTime * _smoothLookSpeed);

//			// Перевіряємо, чи досягли цілі
//			if (Vector3.Distance(Position, _targetPosition) < 0.01f &&
//				Vector3.Distance(Rotation, _targetRotation) < 0.01f)
//			{
//				_isLookingAtTarget = false;
//			}
//		}



//		//if (_isLookingAtTarget)
//		//{
//		//	// Плавне обертання до цілі
//		//	Vector3 directionToTarget = _target - _position;
//		//	directionToTarget.Normalize();

//		//	// Обчислення кутів повороту до цілі
//		//	float targetYaw = (float)Math.Atan2(directionToTarget.X, directionToTarget.Z);
//		//	float targetPitch = (float)Math.Asin(directionToTarget.Y);

//		//	// Плавне обертання
//		//	float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
//		//	_rotation.Y = MathHelper.Lerp(_rotation.Y, targetYaw, deltaTime * _smoothLookSpeed);
//		//	_rotation.X = MathHelper.Lerp(_rotation.X, targetPitch, deltaTime * _smoothLookSpeed);

//		//	// Перевірка, чи досягнуто цілі
//		//	float distanceToTarget = Vector2.Distance(
//		//		new Vector2(_rotation.X, _rotation.Y),
//		//		new Vector2(targetPitch, targetYaw));

//		//	if (distanceToTarget < 0.01f)
//		//	{
//		//		_isLookingAtTarget = false;
//		//	}
//		//}
//	}

//	public Matrix GetViewMatrix()
//	{
//		Vector3 forward = Vector3.Transform(Vector3.Forward,
//			Matrix.CreateRotationX(_rotation.X) *
//			Matrix.CreateRotationY(_rotation.Y) *
//			Matrix.CreateRotationZ(_rotation.Z));

//		Vector3 up = Vector3.Transform(Vector3.Up,
//			Matrix.CreateRotationX(_rotation.X) *
//			Matrix.CreateRotationY(_rotation.Y) *
//			Matrix.CreateRotationZ(_rotation.Z));

//		return Matrix.CreateLookAt(
//			_position,
//			_position + forward,
//			up);
//	}

//	public Matrix GetProjectionMatrix()
//	{
//		return Matrix.CreatePerspectiveFieldOfView(
//			FieldOfView,
//			AspectRatio,
//			NearPlane,
//			FarPlane);
//	}

//	public void FocusOn(Vector3 targetPosition)
//	{
//		// Обчислюємо нову позицію камери
//		Vector3 direction = Vector3.Normalize(Position - targetPosition);
//		Vector3 newPosition = targetPosition + direction * CAMERA_DISTANCE;

//		// Обчислюємо кути повороту для погляду на ціль
//		Vector3 toTarget = targetPosition - newPosition;
//		float yaw = (float)Math.Atan2(toTarget.X, toTarget.Z);
//		float pitch = (float)Math.Atan2(toTarget.Y, Math.Sqrt(toTarget.X * toTarget.X + toTarget.Z * toTarget.Z));

//		_targetPosition = newPosition;
//		_targetRotation = new Vector3(pitch, yaw, 0);
//		_isLookingAtTarget = true;
//	}
//}