using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Geco.Common;
using Geco.Common.Inflector;

using Microsoft.Extensions.Configuration;

using static System.ConsoleColor;
using static Geco.Common.Util.ColorConsole;

namespace Geco.Database
{
	[Options(typeof(DatabasePublishOptions))]
	public class DatabasePublish : BaseGenerator
	{
		private readonly IConfigurationRoot configurationRoot;
		public DatabasePublishOptions Options { get; }

		public DatabasePublish(DatabasePublishOptions options, IInflector inf, IConfigurationRoot configurationRoot)
			: base(inf)
		{
			this.configurationRoot = configurationRoot;
			Options = options;
		}

		protected override void Generate()
		{
			var visualStudioPath = FindVisualStudio();
			if (visualStudioPath == null)
			{
				WriteLine($"Error: Cannot find {("msbuild.exe", Yellow)} path!", Red);
				return;
			}
			var msbuildPath = Path.Combine(visualStudioPath, @"MSBuild\Current\Bin");

			var psi = new ProcessStartInfo($@"{msbuildPath}\msbuild.exe", $"\"{Options.ProjectName}.sqlproj\" /P:Configuration=Release ")
			{
				WorkingDirectory = BaseOutputPath
			};
			var pb = Process.Start(psi);
			pb?.WaitForExit();

			if (pb?.ExitCode != 0)
			{
				WriteLine($"Error: Build failed for {($"{Options.ProjectName}.sqlproj", Yellow)} project!", Red);
				return;
			}

			var connectionString = configurationRoot.GetConnectionString(Options.ConnectionName);
			var cnn = new SqlConnectionStringBuilder(connectionString);
			var args = new StringBuilder();
			var dacpacFile = $@"bin\Release\{Options.ProjectName}.dacpac";

			args.Append("/Action:Publish")
				.Append($" /SourceFile:\"{dacpacFile}\"")
				.Append($" /TargetServerName:\"{cnn["Server"]}\"")
				.Append($" /TargetDatabaseName:\"{cnn.InitialCatalog}\"")
				.Append($" /Profile:\"bin\\Release\\{Options.PublishProfile}\"")
				.Append($" /p:BlockOnPossibleDataLoss={Options.BlockOnPossibleDataLoss}", Options.BlockOnPossibleDataLoss != null);

			var sqlPackagePath = FindSqlPackage(visualStudioPath);
			if (sqlPackagePath == null)
			{
				WriteLine($"Error: Cannot find {("SqlPackage.exe", Yellow)} path!", Red);
				return;
			}
			WriteLine($"Running: {("SqlPackage.Exe", Yellow)} {(args, Yellow)}", White);
			var sqlpsi = new ProcessStartInfo($@"{sqlPackagePath}\SqlPackage.exe", args.ToString())
			{
				WorkingDirectory = BaseOutputPath
			};

			var ps = Process.Start(sqlpsi);
			ps?.WaitForExit();
		}

		private static string FindVisualStudio()
		{
			var pi = new ProcessStartInfo(@"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe", "-products * -requires Microsoft.Component.MSBuild -prerelease -latest -utf8 -format text");
			pi.RedirectStandardOutput = true;
			var p = Process.Start(pi);
			var output = p.StandardOutput.ReadToEnd();
			p.WaitForExit();
			var installationPath = Regex.Match(output, @"^installationPath:\s*(.*)\r$", RegexOptions.Multiline);

			if (installationPath.Success)
				return installationPath.Groups[1].Value;

			return null;
		}

		public static string FindSqlPackage(string visualStudioPath)
		{
			if (ExistsHere(@"Common7\IDE\Extensions\Microsoft\SQLDB\DAC\150", out var foundPath))
				return foundPath;
			if (ExistsHere(@"Common7\IDE\Extensions\Microsoft\SQLDB\DAC\140", out foundPath))
				return foundPath;
			if (ExistsHere(@"Common7\IDE\Extensions\Microsoft\SQLDB\DAC\130", out foundPath))
				return foundPath;

			if (ExistsHere(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft SQL Server\140\DAC\bin"), out foundPath))
				return foundPath;

			// C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\Microsoft\SQLDB\DAC
			// VS 2022 Community
			string vs2022 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\Microsoft\SQLDB\DAC");
			if (File.Exists(Path.Combine(vs2022, "SqlPackage.exe")))
				return vs2022;

			return null;

			bool ExistsHere(string path, out string foundPath)
			{
				return File.Exists(Path.Combine(foundPath = Path.Combine(visualStudioPath, path), "SqlPackage.exe"));
			}
		}
	}
}