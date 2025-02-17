using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor
{
	public class SceneObject
	{
		public string Name { get; set; }
		public Vector3 Position { get; set; }
		public List<SceneObject> Children { get; } = new List<SceneObject>();
		public bool IsSelected { get; set; }
		public bool IsExpanded { get; set; }
		public bool IsRenaming { get; set; }

		public SceneObject(string name, Vector3 position)
		{
			Name = name;
			Position = position;
			Children = new List<SceneObject>();
			IsSelected = false;
			IsExpanded = false;
			IsRenaming = false;
		}
	}
}
