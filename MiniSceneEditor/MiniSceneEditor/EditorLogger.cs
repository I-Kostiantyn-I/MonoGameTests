using Microsoft.Xna.Framework;
using System;
using System.IO;

public class EditorLogger
{
	private static string _logPath = "editor_log.txt";
	private static bool _initialized;
	private readonly string _componentName;
	private readonly bool _isActive;

	public EditorLogger(string componentName, bool isActive = true)
	{
		Initialize();
		_componentName = componentName;
		_isActive = isActive;
	}

	private void Initialize()
	{
		if (!_initialized)
		{
			// Очищаємо файл при старті
			File.WriteAllText(_logPath, $"Editor Log Started: {DateTime.Now}\n");
			_initialized = true;
		}
	}

	public  void Log(string message)
	{
		if (!_isActive) return;

		var logMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {_componentName} {message}";
		File.AppendAllText(_logPath, logMessage + "\n");
		//System.Diagnostics.Debug.WriteLine(logMessage);
	}

	public void LogVector3(string name, Vector3 vector)
	{
		Log($"{name}: X={vector.X:F3}, Y={vector.Y:F3}, Z={vector.Z:F3}");
	}
}