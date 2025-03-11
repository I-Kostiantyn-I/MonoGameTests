using BepuPhysics.Collidables;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using BepuVector3 = System.Numerics.Vector3;
using XnaBoundingSphere = Microsoft.Xna.Framework.BoundingSphere;
using XnaRay = Microsoft.Xna.Framework.Ray;
using XnaColor = Microsoft.Xna.Framework.Color;
using BepuPhysics;
using BepuUtilities;
using Microsoft.Xna.Framework.Graphics;
using System;
using MiniSceneEditor.Core.Components.Abstractions;

namespace MiniSceneEditor.Core.Components.Impls;

public class BepuSphereCollider : ICollider
{
	private float _radius;
	private Sphere _sphereShape;
	private XnaBoundingSphere _boundingSphere;
	private TypedIndex _collidableReference;

	public BepuSphereCollider(float radius)
	{
		_radius = radius;
		_sphereShape = new Sphere(radius);

		XnaVector3 extent = new XnaVector3(_radius);
		_boundingSphere = new XnaBoundingSphere(XnaVector3.Zero, radius);
	}

	public Microsoft.Xna.Framework.BoundingBox GetBoundingBox()
	{
		return new Microsoft.Xna.Framework.BoundingBox(
			new XnaVector3(-_radius),
			new XnaVector3(_radius)
		);
	}

	public TypedIndex GetCollidableReference(Simulation simulation)
	{
		// Додаємо форму до симуляції, якщо ще не додана
		if (_collidableReference.Type == 0)
		{
			_collidableReference = simulation.Shapes.Add(_sphereShape);
		}
		return _collidableReference;
	}

	public BodyInertia ComputeInertia(float mass)
	{
		float inverseMass = 1f / mass;
		// Для сфери інерція розраховується за формулою I = (2/5) * mass * radius^2
		float inverseI = (5 * inverseMass) / (2 * _radius * _radius);

		return new BodyInertia
		{
			InverseMass = inverseMass,
			InverseInertiaTensor = new Symmetric3x3
			{
				XX = inverseI,
				YY = inverseI,
				ZZ = inverseI,
				YX = 0,
				ZX = 0,
				ZY = 0
			}
		};
	}

	public bool Intersects(XnaRay ray, out float distance)
	{
		float? intersection = ray.Intersects(_boundingSphere);
		if (intersection.HasValue)
		{
			distance = intersection.Value;
			return true;
		}

		distance = float.MaxValue;
		return false;
	}

	public void DrawDebug(BasicEffect effect, XnaColor color)
	{
		// Малюємо сферу за допомогою кілець
		const int segments = 32;
		for (int axis = 0; axis < 3; axis++)
		{
			var vertices = new VertexPositionColor[segments + 1];

			for (int i = 0; i <= segments; i++)
			{
				float angle = i * MathHelper.TwoPi / segments;
				XnaVector3 position = XnaVector3.Zero;

				switch (axis)
				{
					case 0: // XY plane
						position = new XnaVector3(
							_radius * (float)Math.Cos(angle),
							_radius * (float)Math.Sin(angle),
							0);
						break;
					case 1: // XZ plane
						position = new XnaVector3(
							_radius * (float)Math.Cos(angle),
							0,
							_radius * (float)Math.Sin(angle));
						break;
					case 2: // YZ plane
						position = new XnaVector3(
							0,
							_radius * (float)Math.Cos(angle),
							_radius * (float)Math.Sin(angle));
						break;
				}

				vertices[i] = new VertexPositionColor(position, color);
			}

			foreach (var pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				effect.GraphicsDevice.DrawUserPrimitives(
					PrimitiveType.LineStrip,
					vertices,
					0,
					segments);
			}
		}
	}

	public IShape GetPhysicsShape()
	{
		return _sphereShape;
	}

	public void Dispose()
	{
		if (_collidableReference.Type != 0)
		{
			// TODO: Очищення ресурсів
		}
	}
}
