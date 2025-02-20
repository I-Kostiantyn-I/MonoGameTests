using Microsoft.Xna.Framework;
using System;
using System.IO;

public class EditorLogger
{
	private static string _logPath = "editor_log.txt";
	private static bool _initialized;
	private readonly string componentName;

	public EditorLogger(string componentName)
	{
		Initialize();
		this.componentName = componentName;
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
		var logMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {componentName} {message}";
		File.AppendAllText(_logPath, logMessage + "\n");
		System.Diagnostics.Debug.WriteLine(logMessage);
	}

	public void LogVector3(string name, Vector3 vector)
	{
		Log($"{name}: X={vector.X:F3}, Y={vector.Y:F3}, Z={vector.Z:F3}");
	}
}