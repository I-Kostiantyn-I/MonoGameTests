using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using Microsoft.Core;
using MiniSceneEditor.Core.Components.Impls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor.Physics;

public class PhysicsSystem : IDisposable
{
	private Simulation _simulation;
	private BufferPool _bufferPool;
	private Scene _scene;
	private List<RigidbodyComponent> _rigidbodies;
	private ThreadDispatcher _threadDispatcher;

	// Налаштування симуляції
	private float _fixedTimeStep = 1.0f / 60.0f;
	private float _accumulator;
	private bool _isPaused;
	private Vector3 _gravity = new Vector3(0, -9.81f, 0);
	private CallbacksHolder _callbacks; // Зберігаємо посилання на колбеки

	private readonly PhysicsSettings _settings;

	private readonly EditorLogger _log;

	// Публічні властивості для доступу до налаштувань
	public float FixedTimeStep
	{
		get => _fixedTimeStep;
		set
		{
			_fixedTimeStep = MathHelper.Clamp(value, 0.001f, 0.1f);
			_log.Log($"Fixed time step set to: {_fixedTimeStep}");
		}
	}

	public Vector3 Gravity
	{
		get => _gravity;
		set
		{
			_gravity = value;
			_callbacks.Gravity = _gravity;
			_log.Log($"Gravity set to: {_gravity}");
		}
	}

	public PhysicsSystem(Scene scene, PhysicsSettings? settings = null)
	{
		_settings = settings ?? new PhysicsSettings();
		_scene = scene;
		_rigidbodies = new List<RigidbodyComponent>();
		_log = new EditorLogger(nameof(PhysicsSystem));

		// Ініціалізація BepuPhysics
		_bufferPool = new BufferPool();
		_threadDispatcher = new ThreadDispatcher(Environment.ProcessorCount);

		_callbacks = new CallbacksHolder
		{
			Gravity = _gravity,
			LinearDamping = _settings.LinearDamping,
			AngularDamping = _settings.AngularDamping
		};

		_simulation = Simulation.Create(
			_bufferPool,
			_callbacks,
			_callbacks,
			new SolveDescription(_settings.VelocityIterations, _settings.SubstepCount));
	}

	public void RegisterRigidbody(RigidbodyComponent rigidbody)
	{
		if (!_rigidbodies.Contains(rigidbody))
		{
			_rigidbodies.Add(rigidbody);
			rigidbody.InitializePhysics(_simulation);
			_log.Log($"Registered rigidbody for object: {rigidbody.Owner.Name}");
		}
	}

	public void UnregisterRigidbody(RigidbodyComponent rigidbody)
	{
		if (_rigidbodies.Contains(rigidbody))
		{
			_rigidbodies.Remove(rigidbody);
			rigidbody.OnDestroy();
			_log.Log($"Unregistered rigidbody for object: {rigidbody.Owner.Name}");
		}
	}

	public void Update(Microsoft.Xna.Framework.GameTime gameTime)
	{
		if (_isPaused)
			return;

		float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
		_accumulator += deltaTime;

		// Фіксований крок для фізики
		while (_accumulator >= _fixedTimeStep)
		{
			_simulation.Timestep(_fixedTimeStep);
			_accumulator -= _fixedTimeStep;

			// Оновлюємо позиції об'єктів
			foreach (var rb in _rigidbodies)
			{
				rb.UpdateTransform();
			}
		}
	}

	public void SetPaused(bool paused)
	{
		_isPaused = paused;
		_log.Log($"Physics simulation {(paused ? "paused" : "resumed")}");
	}

	public void Dispose()
	{
		_simulation.Dispose();
		_bufferPool.Clear();
		_threadDispatcher.Dispose();
		_log.Log("Physics system disposed");
	}

	public void SetGravity(Vector3 gravity)
	{
		_gravity = gravity;
		// В Bepu gravity встановлюється через опис симуляції при створенні
		// Якщо потрібно змінити гравітацію під час виконання, потрібно застосувати її до кожного тіла
		foreach (var rb in _rigidbodies)
		{
			rb.UpdateGravity(_gravity);
		}
		_log.Log($"Gravity set to: {gravity}");
	}

	public void SetTimeStep(float timeStep)
	{
		_fixedTimeStep = timeStep;
		_log.Log($"Fixed time step set to: {timeStep}");
	}

	public void Reset()
	{
		_accumulator = 0;
		foreach (var rb in _rigidbodies)
		{
			rb.ResetToInitialState();
		}
		_log.Log("Physics simulation reset");
	}
}
