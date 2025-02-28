using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor.Camera;

public struct CameraMatricesState
{
	public Matrix ViewMatrix { get; set; }
	public Matrix ProjectionMatrix { get; set; }

	public CameraMatricesState(Matrix view, Matrix projection)
	{
		ViewMatrix = view;
		ProjectionMatrix = projection;
	}
}
