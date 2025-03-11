using BepuPhysics.Collidables;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using BepuVector3 = System.Numerics.Vector3;
using XnaBoundingBox = Microsoft.Xna.Framework.BoundingBox;
using XnaRay = Microsoft.Xna.Framework.Ray;
using XnaColor = Microsoft.Xna.Framework.Color;
using MiniSceneEditor.Core.Components.Abstractions;
using BepuPhysics;
using Microsoft.Xna.Framework.Graphics;



namespace MiniSceneEditor.Core.Components.Impls;

public class BepuBoxCollider : ICollider
{
	private XnaVector3 _size;
	private Box _boxShape;
	private XnaBoundingBox _boundingBox;
	private TypedIndex _collidableReference;

	public BepuBoxCollider(XnaVector3 size)
	{
		_size = size;
		_boxShape = new Box(size.X, size.Y, size.Z);
		_boundingBox = new XnaBoundingBox(
			-size / 2,  // Min
			size / 2    // Max
		);
	}

	public TypedIndex GetCollidableReference(Simulation simulation)
	{
		// Додаємо форму до симуляції, якщо ще не додана
		if (_collidableReference.Type == 0) // 0 означає невалідний індекс
		{
			_collidableReference = simulation.Shapes.Add(_boxShape);
		}
		return _collidableReference;
	}


	public BodyInertia ComputeInertia(float mass)
	{
		float inverseMass = 1f / mass;
		float width = _size.X;
		float height = _size.Y;
		float depth = _size.Z;

		float inverseIx = (12 * inverseMass) / (height * height + depth * depth);
		float inverseIy = (12 * inverseMass) / (width * width + depth * depth);
		float inverseIz = (12 * inverseMass) / (width * width + height * height);

		return new BodyInertia
		{
			InverseMass = inverseMass,
			InverseInertiaTensor = new BepuUtilities.Symmetric3x3
			{
				XX = inverseIx,
				YY = inverseIy,
				ZZ = inverseIz,
				YX = 0,
				ZX = 0,
				ZY = 0
			}
		};
	}

	public bool Intersects(XnaRay ray, out float distance)
	{
		float? intersection = ray.Intersects(_boundingBox);
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
		// Малювання каркасного куба
		var min = _boundingBox.Min;
		var max = _boundingBox.Max;

		// Створюємо вершини куба
		var vertices = new[]
		{
            // Передня грань
            new VertexPositionColor(new XnaVector3(min.X, min.Y, max.Z), color),
			new VertexPositionColor(new XnaVector3(max.X, min.Y, max.Z), color),
			new VertexPositionColor(new XnaVector3(max.X, max.Y, max.Z), color),
			new VertexPositionColor(new XnaVector3(min.X, max.Y, max.Z), color),
			new VertexPositionColor(new XnaVector3(min.X, min.Y, max.Z), color),

            // Задня грань
            new VertexPositionColor(new XnaVector3(min.X, min.Y, min.Z), color),
			new VertexPositionColor(new XnaVector3(max.X, min.Y, min.Z), color),
			new VertexPositionColor(new XnaVector3(max.X, max.Y, min.Z), color),
			new VertexPositionColor(new XnaVector3(min.X, max.Y, min.Z), color),
			new VertexPositionColor(new XnaVector3(min.X, min.Y, min.Z), color),

            // З'єднувальні лінії
            new VertexPositionColor(new XnaVector3(max.X, min.Y, min.Z), color),
			new VertexPositionColor(new XnaVector3(max.X, min.Y, max.Z), color),

			new VertexPositionColor(new XnaVector3(max.X, max.Y, min.Z), color),
			new VertexPositionColor(new XnaVector3(max.X, max.Y, max.Z), color),

			new VertexPositionColor(new XnaVector3(min.X, max.Y, min.Z), color),
			new VertexPositionColor(new XnaVector3(min.X, max.Y, max.Z), color),
		};

		foreach (var pass in effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			effect.GraphicsDevice.DrawUserPrimitives(
				PrimitiveType.LineStrip,
				vertices,
				0,
				vertices.Length - 1);
		}
	}

	public IShape GetPhysicsShape()
	{
		return _boxShape;
	}

	public void Dispose()
	{
		if (_collidableReference.Type != 0)
		{
			// TODO: Очищення ресурсів
		}
	}
}