using BepuPhysics.Collidables;
using BepuPhysics;
using Microsoft.Core;
using System;
using System.Numerics;
using MiniSceneEditor.Core.Utils;
using MiniSceneEditor.Core.Components.Abstractions;
using BepuUtilities;

namespace MiniSceneEditor.Core.Components.Impls;

public class RigidbodyComponent : IComponent
{
	public SceneObject Owner { get; set; }
	private BodyHandle _bodyHandle;
	private BodyReference? _bodyReference;
	private ColliderComponent _colliderComponent;
	private EditorLogger _log;

	// Фізичні властивості
	public float Mass { get; set; } = 1.0f;
	public bool IsStatic { get; set; }
	public bool UseGravity { get; set; } = true;
	public Vector3 Velocity { get; set; }
	public float LinearDamping { get; set; } = 0.01f;
	public float AngularDamping { get; set; } = 0.01f;
	public bool IsTrigger { get; set; }

	private Vector3 _initialPosition;
	private Vector3 _initialRotation;

	public RigidbodyComponent()
	{
		_log = new EditorLogger(nameof(RigidbodyComponent));
	}

	public void Initialize()
	{
		_colliderComponent = Owner.GetComponent<ColliderComponent>();
		if (_colliderComponent == null)
		{
			throw new InvalidOperationException("RigidbodyComponent requires ColliderComponent");
		}

		// Зберігаємо початковий стан
		_initialPosition = Owner.Transform.Position.ToNumerics();
		_initialRotation = Owner.Transform.Rotation.ToNumerics();
	}

	public void InitializePhysics(Simulation simulation)
	{
		var collider = _colliderComponent.GetCollider();

		if (IsStatic)
		{
			var pose = new RigidPose(Owner.Transform.Position.ToNumerics());
			var velocity = new BodyVelocity();
			var inertia = new BodyInertia { InverseMass = 0 }; // Для статичного тіла маса нескінченна

			var description = BodyDescription.CreateDynamic(
				pose,
				velocity,
				inertia,
				new CollidableDescription(collider.GetCollidableReference(simulation)),
				new BodyActivityDescription(0.01f));

			_bodyHandle = simulation.Bodies.Add(description);
		}
		else
		{
			var pose = new RigidPose(Owner.Transform.Position.ToNumerics());
			var velocity = new BodyVelocity();

			// Створюємо інерцію для динамічного тіла
			float inverseMass = 1f / Mass;
			var inertia = new BodyInertia
			{
				InverseMass = inverseMass,
				// Для простого випадку можна використати діагональну матрицю інерції
				InverseInertiaTensor = new Symmetric3x3
				{
					XX = inverseMass,
					YY = inverseMass,
					ZZ = inverseMass,
					YX = 0,
					ZX = 0,
					ZY = 0
				}
			};

			var description = BodyDescription.CreateDynamic(
				pose,
				velocity,
				inertia,
				new CollidableDescription(collider.GetCollidableReference(simulation)),
				new BodyActivityDescription(LinearDamping));

			_bodyHandle = simulation.Bodies.Add(description);
		}

		_bodyReference = simulation.Bodies.GetBodyReference(_bodyHandle);
		_log.Log($"Initialized physics for {Owner.Name}");
	}

	public void UpdateTransform()
	{
		if (_bodyReference.HasValue)
		{
			Owner.Transform.Position = _bodyReference.Value.Pose.Position.ToXNA();
			var rotation = _bodyReference.Value.Pose.Orientation.ToXNA();
			Owner.Transform.Rotation = new Vector3(
				MathF.Asin(2.0f * (rotation.X * rotation.Z - rotation.W * rotation.Y)),
				MathF.Atan2(2.0f * (rotation.X * rotation.W + rotation.Y * rotation.Z),
						   1.0f - 2.0f * (rotation.Z * rotation.Z + rotation.W * rotation.W)),
				MathF.Atan2(2.0f * (rotation.X * rotation.Y + rotation.Z * rotation.W),
						   1.0f - 2.0f * (rotation.Y * rotation.Y + rotation.Z * rotation.Z))
			);
		}
	}

	public void UpdateGravity(Vector3 gravity)
	{
		if (_bodyReference.HasValue && !IsStatic)
		{
			// Конвертуємо в System.Numerics.Vector3 для Bepu
			var force = gravity * Mass;

			// Застосовуємо імпульс у центрі маси (offset = 0)
			BodyReference.ApplyImpulse(
				force,                              // лінійний імпульс
				Vector3.Zero,                   // точка прикладання (центр маси)
				ref _bodyReference.Value.LocalInertia,  // інерція тіла
				ref _bodyReference.Value.Pose,          // поза тіла
				ref _bodyReference.Value.Velocity       // швидкість тіла
			);
		}
	}

	public void ResetToInitialState()
	{
		if (_bodyReference.HasValue)
		{
			_bodyReference.Value.Pose.Position = _initialPosition;
			_bodyReference.Value.Pose.Orientation = Quaternion.CreateFromYawPitchRoll(
				_initialRotation.Y,
				_initialRotation.X,
				_initialRotation.Z);

			_bodyReference.Value.Velocity.Linear = System.Numerics.Vector3.Zero;
			_bodyReference.Value.Velocity.Angular = System.Numerics.Vector3.Zero;

			UpdateTransform();
		}
	}

	public void OnDestroy()
	{
		// Очищення фізичного тіла буде виконуватися в PhysicsSystem
	}
}