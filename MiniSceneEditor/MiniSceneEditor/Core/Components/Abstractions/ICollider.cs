using BepuPhysics.Collidables;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using System;

namespace MiniSceneEditor.Core.Components.Abstractions;

public interface ICollider : IDisposable
{
	bool Intersects(Microsoft.Xna.Framework.Ray ray, out float distance);
	void DrawDebug(BasicEffect effect, Microsoft.Xna.Framework.Color color);
	IShape GetPhysicsShape();
	TypedIndex GetCollidableReference(Simulation simulation);
	BodyInertia ComputeInertia(float mass);
}