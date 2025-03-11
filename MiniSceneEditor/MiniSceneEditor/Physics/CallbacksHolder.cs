using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics;
using BepuUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using BepuPhysics.Collidables;

namespace MiniSceneEditor.Physics;
/*AngularIntegrationMode може бути:

Implicit (за замовчуванням) - більш стабільний, але менш точний
Explicit - більш точний, але може бути менш стабільним
Вибір залежить від ваших потреб:

Implicit краще для загального використання
Explicit краще для ситуацій, де потрібна висока точність обертання*/


public unsafe struct CallbacksHolder : INarrowPhaseCallbacks, IPoseIntegratorCallbacks
{
	public Vector3 Gravity;
	public float LinearDamping;
	public float AngularDamping;

	public void Initialize(Simulation simulation) { }

	public void PrepareForIntegration(float dt) { }

	// IPoseIntegratorCallbacks
	public readonly bool AllowSubstepsForUnconstrainedBodies => false;
	public readonly bool IntegrateVelocityForKinematics => false;
	public readonly int MinimumIterationCount => 1;
	public readonly int MaximumIterationCount => 1;
	public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.ConserveMomentum;

	public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
		BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
	{
		// Застосовуємо гравітацію та демпфінг
		var gravityDt = Vector3Wide.Broadcast(Gravity * dt[0]);
		velocity.Linear += gravityDt;

		var linearDampingDt = new Vector<float>(MathF.Pow(1 - LinearDamping, dt[0]));
		var angularDampingDt = new Vector<float>(MathF.Pow(1 - AngularDamping, dt[0]));

		Vector3Wide.Scale(velocity.Linear, linearDampingDt, out velocity.Linear);
		Vector3Wide.Scale(velocity.Angular, angularDampingDt, out velocity.Angular);
	}

	// INarrowPhaseCallbacks
	public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b)
	{
		return true;
	}

	public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
	{
		return true;
	}

	public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
	{
		return true;
	}

	bool INarrowPhaseCallbacks.ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties material)
	{
		// Створюємо налаштування пружності
		var springSettings = new SpringSettings(30f, 1f); // frequency: 30Hz, damping ratio: 1

		// Створюємо властивості матеріалу
		material = new PairMaterialProperties(
			frictionCoefficient: 1f,           // коефіцієнт тертя
			maximumRecoveryVelocity: 2f,       // максимальна швидкість відновлення
			springSettings: springSettings      // налаштування пружності
		);

		return true;
	}

	public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
	{
		return true;
	}

	public void Dispose() { }
}