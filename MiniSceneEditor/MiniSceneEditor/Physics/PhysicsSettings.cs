using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor.Physics;

public struct PhysicsSettings
{
	public PhysicsSettings()
	{
	}

	public int VelocityIterations { get; set; } = 6;
	public int SubstepCount { get; set; } = 2;
	public float LinearDamping { get; set; } = 0.1f;
	public float AngularDamping { get; set; } = 0.1f;
}


//var physicsSettings = new PhysicsSystem.Settings
//{
//	VelocityIterations = 8,  // Більше ітерацій для точності
//	SubstepCount = 3,        // Більше підкроків для стабільності
//	LinearDamping = 0.05f,   // Менше затухання для більш плавного руху
//	AngularDamping = 0.05f
//};
