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
		static readonly string[] _Dependencies = new string[] { 
			Assembly.GetExecutingAssembly().Location,
			"nunit.framework.dll",
		};

		public static bool Execute(App app, string fileName, out object result)
		{
			result = "";

			var process = new Process();

			var dllFile = Path.ChangeExtension(fileName, ".dll");

			if (IsRunningOnMono())
			{
#if LINUX
				process.StartInfo.FileName = "mono";
				process.StartInfo.Arguments = $"{ Path.Combine(_CompilerPath, "fsc.exe") } -o { dllFile } -a {fileName}{DEPENCENCY_OPTION + string.Join(DEPENCENCY_OPTION, _Dependencies)}";
#else
				process.StartInfo.FileName = "fsharpc";
				process.StartInfo.Arguments = $"-o { dllFile } -a {fileName}{DEPENCENCY_OPTION + string.Join(DEPENCENCY_OPTION, _Dependencies)}";
#endif
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
				return false;
			}

			try
			{
				var assembly = Assembly.LoadFrom(dllFile);
				var module = assembly.GetModules()[0];
				var type = module.GetTypes()[0];
				var method = type.GetMethod("run");

				var args = new object[] { app };

				result = method.Invoke(null, args);
			}
			catch (Exception e)
			{
				Console.WriteLine("error executing script");
				Console.WriteLine(e);
				Console.WriteLine(e.Message);
				return false;
			}

			File.Delete(dllFile);

			return true;
		}

		static bool IsRunningOnMono()
		{
			return Type.GetType("Mono.Runtime") != null;
		}
	}
}
