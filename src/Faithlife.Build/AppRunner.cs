using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SimpleExec;

namespace Faithlife.Build
{
	/// <summary>
	/// Runs command-line apps.
	/// </summary>
	/// <remarks>
	/// <para>Consider calling these methods directly via <c>using static Faithlife.Build.AppRunner;</c>.</para>
	/// </remarks>
	public static class AppRunner
	{
		/// <summary>
		/// Runs the specified command-line app.
		/// </summary>
		/// <param name="path">The path of the command-line app.</param>
		/// <param name="args">The arguments to send to the command-line app.</param>
		public static void RunApp(string path, params string?[] args) =>
			RunApp(path, (args ?? throw new ArgumentNullException(nameof(args))).AsEnumerable());

		/// <summary>
		/// Runs the specified command-line app.
		/// </summary>
		/// <param name="path">The path of the command-line app.</param>
		/// <param name="args">The arguments to send to the command-line app.</param>
		public static void RunApp(string path, IEnumerable<string?> args) =>
			RunApp(path, new AppRunnerSettings { Arguments = args ?? throw new ArgumentNullException(nameof(args)) });

		/// <summary>
		/// Runs the specified command-line app.
		/// </summary>
		/// <param name="path">The path of the command-line app.</param>
		/// <param name="settings">The settings to use when running the app.</param>
		public static int RunApp(string path, AppRunnerSettings settings) =>
			DoRunApp(path, settings);

		/// <summary>
		/// Runs the specified .NET Framework command-line app.
		/// </summary>
		/// <param name="path">The path of the command-line app.</param>
		/// <param name="args">The arguments to send to the command-line app.</param>
		/// <remarks>On Linux and macOS, Mono is used to run the app.</remarks>
		[Obsolete("Use AppRunnerSettings.IsFrameworkApp.")]
		public static void RunDotNetFrameworkApp(string path, params string?[] args) =>
			RunDotNetFrameworkApp(path, (args ?? throw new ArgumentNullException(nameof(args))).AsEnumerable());

		/// <summary>
		/// Runs the specified .NET Framework command-line app.
		/// </summary>
		/// <param name="path">The path of the command-line app.</param>
		/// <param name="args">The arguments to send to the command-line app.</param>
		/// <remarks>On Linux and macOS, Mono is used to run the app.</remarks>
		[Obsolete("Use AppRunnerSettings.IsFrameworkApp.")]
		public static void RunDotNetFrameworkApp(string path, IEnumerable<string?> args) =>
			RunDotNetFrameworkApp(path, new AppRunnerSettings { Arguments = args ?? throw new ArgumentNullException(nameof(args)), IsFrameworkApp = true });

		/// <summary>
		/// Runs the specified .NET Framework command-line app.
		/// </summary>
		/// <param name="path">The path of the command-line app.</param>
		/// <param name="settings">The settings to use when running the app.</param>
		/// <remarks>On Linux and macOS, Mono is used to run the app.</remarks>
		[Obsolete("Use AppRunnerSettings.IsFrameworkApp.")]
		public static int RunDotNetFrameworkApp(string path, AppRunnerSettings settings)
		{
			var clone = (settings ?? throw new ArgumentNullException(nameof(settings))).Clone();
			clone.IsFrameworkApp = true;
			return DoRunApp(path, clone);
		}

		/// <summary>
		/// Runs the specified command-line app, via <c>cmd /c</c> on Windows.
		/// </summary>
		/// <param name="path">The path of the command-line app.</param>
		/// <param name="args">The arguments to send to the command-line app.</param>
		[Obsolete("Use AppRunnerSettings.UseCmdOnWindows.")]
		public static void RunCmd(string path, params string?[] args) =>
			RunCmd(path, (args ?? throw new ArgumentNullException(nameof(args))).AsEnumerable());

		/// <summary>
		/// Runs the specified command-line app, via <c>cmd /c</c> on Windows.
		/// </summary>
		/// <param name="path">The path of the command-line app.</param>
		/// <param name="args">The arguments to send to the command-line app.</param>
		[Obsolete("Use AppRunnerSettings.UseCmdOnWindows.")]
		public static void RunCmd(string path, IEnumerable<string?> args) =>
			DoRunApp(path, new AppRunnerSettings { Arguments = args ?? throw new ArgumentNullException(nameof(args)), UseCmdOnWindows = true });

		/// <summary>
		/// Runs the specified command-line app, via <c>cmd /c</c> on Windows.
		/// </summary>
		/// <param name="path">The path of the command-line app.</param>
		/// <param name="settings">The settings to use when running the app.</param>
		[Obsolete("Use AppRunnerSettings.UseCmdOnWindows.")]
		public static int RunCmd(string path, AppRunnerSettings settings)
		{
			var clone = (settings ?? throw new ArgumentNullException(nameof(settings))).Clone();
			clone.UseCmdOnWindows = true;
			return DoRunApp(path, clone);
		}

		private static int DoRunApp(string path, AppRunnerSettings settings)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));

			var arguments = settings.Arguments?.WhereNotNull() ?? Enumerable.Empty<string>();
			string commandPath;
			string argsString;
			if (settings.UseCmdOnWindows && BuildEnvironment.IsWindows())
			{
				commandPath = "cmd.exe";
				argsString = "/S /C \"" + ArgumentEscaper.EscapeAndConcatenate(arguments.Prepend(path)) + "\"";
			}
			else if (settings.IsFrameworkApp && BuildEnvironment.IsUnix())
			{
				commandPath = "mono";
				argsString = ArgumentEscaper.EscapeAndConcatenate(arguments.Prepend(path));
			}
			else
			{
				commandPath = path;
				argsString = ArgumentEscaper.EscapeAndConcatenate(arguments);
			}

			var exitCode = 0;
			try
			{
				Command.Run(name: commandPath, args: argsString, workingDirectory: settings.WorkingDirectory, noEcho: settings.NoEcho);
			}
			catch (NonZeroExitCodeException exception)
			{
				exitCode = exception.ExitCode;
			}

			var isExitCodeSuccess = settings.IsExitCodeSuccess ?? (x => x == 0);
			if (!isExitCodeSuccess(exitCode))
				throw new BuildException($"{Path.GetFileName(path)} failed with exit code {exitCode}.");

			return exitCode;
		}
	}
}
