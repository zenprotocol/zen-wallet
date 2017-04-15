using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Zen
{
	public static class ScriptRunner
	{
		static string _CompilerPath = "/usr/lib/mono/4.5/"; //TODO
		const string DEPENCENCY_OPTION = " -r ";
		static readonly string[] _Dependencies = new string[] { Process.GetCurrentProcess().MainModule.ModuleName };

		public static void Execute(App app, string fileName)
		{
			var process = new Process();

			var dllFile = Path.ChangeExtension(fileName, ".dll");

			if (IsRunningOnMono())
			{
				process.StartInfo.FileName = "mono";
				process.StartInfo.Arguments = $"{ Path.Combine(_CompilerPath, "fsc.exe") } -o { dllFile } -a {fileName}{DEPENCENCY_OPTION + string.Join(DEPENCENCY_OPTION, _Dependencies)}";
			}
			else
			{
				//TODO
			}

			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;

			process.OutputDataReceived += (sender, args1) =>
			{
				Console.WriteLine(args1.Data);
			};

			process.Start();
			process.BeginOutputReadLine();
			process.WaitForExit();

			if (process.ExitCode != 0)
			{
				Console.WriteLine("error compiling script");
				return;
			}

			try
			{
				var assembly = Assembly.LoadFrom(dllFile);
				var module = assembly.GetModules()[0];
				var type = module.GetTypes()[0];
				var method = type.GetMethod("run");

				var args = new object[] { app };

				var result = method.Invoke(null, args);
			}
			catch
			{
				Console.WriteLine("error executing script");
			}

			File.Delete(dllFile);
		}

		static bool IsRunningOnMono()
		{
			return Type.GetType("Mono.Runtime") != null;
		}
	}
}
