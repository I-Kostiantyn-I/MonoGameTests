using Microsoft.Core;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor;

//public class PickingSystem
//{
//	private GraphicsDevice _graphicsDevice;
//	private EditorCamera _camera;

//	public PickingSystem(GraphicsDevice graphicsDevice, EditorCamera camera)
//	{
//		_graphicsDevice = graphicsDevice;
//		_camera = camera;
//	}

//	public SceneObject PickObject(Vector2 screenPosition, IEnumerable<SceneObject> objects)
//	{
//		Ray pickRay = GetPickRay(screenPosition);
//		float nearestDistance = float.MaxValue;
//		SceneObject nearestObject = null;

//		foreach (var obj in objects)
//		{
//			if (TestRayIntersection(pickRay, obj, out float distance))
//			{
//				if (distance < nearestDistance)
//				{
//					nearestDistance = distance;
//					nearestObject = obj;
//				}
//			}
//		}

//		return nearestObject;
//	}

//	private Ray GetPickRay(Vector2 screenPosition)
//	{
//		Vector3 nearPoint = _graphicsDevice.Viewport.Unproject(
//			new Vector3(screenPosition, 0),
//			_camera.ProjectionMatrix,
//			_camera.ViewMatrix,
//			Matrix.Identity);

//		Vector3 farPoint = _graphicsDevice.Viewport.Unproject(
//			new Vector3(screenPosition, 1),
//			_camera.ProjectionMatrix,
//			_camera.ViewMatrix,
//			Matrix.Identity);

//		Vector3 direction = Vector3.Normalize(farPoint - nearPoint);
//		return new Ray(nearPoint, direction);
//	}
//}
