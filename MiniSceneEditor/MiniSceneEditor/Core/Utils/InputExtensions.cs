using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor.Core.Utils;

public static class InputExtensions
{
	public static Vector3 ToVector3(this Vector2 vector2)
	{
		return new Vector3(vector2.X, vector2.Y, 0);
	}

	public static Vector2 ToVector2(this Vector3 vector3)
	{
		return new Vector2(vector3.X, vector3.Y);
	}
}
