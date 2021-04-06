<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Linq.dll</Reference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>System.Memory</NuGetReference>
  <NuGetReference>WindowsAzure.Storage</NuGetReference>
  <Namespace>Microsoft.WindowsAzure.Storage</Namespace>
  <Namespace>Microsoft.WindowsAzure.Storage.Blob</Namespace>
  <Namespace>Microsoft.WindowsAzure.Storage.Queue</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
</Query>

void Main()
{
	// set or find the paths
	var paths = new[]
	{
		$@"src\backend\Shared\FL.Ebolapp.Shared.Infrastructure\bin\Debug\netstandard2.1",
		$@"src\backend\Shared\Fl.Ebolapp.Shared.Infrastructure.Azure\bin\Debug\netstandard2.1",
		$@"src\backend\Shared\Fl.Ebolapp.Shared.Infrastructure.Azure.Blob\bin\Debug\netstandard2.1",
		$@"src\backend\Shared\FL.Ebolapp.Shared.Infrastructure.Extensions\bin\Debug\netstandard2.1",

		$@"src\backend\FunctionApps\FL.Ebolapp.FunctionApps.Fetch\bin\Debug\netcoreapp3.1",
		$@"src\backend\FunctionApps\FL.Ebolapp.FunctionsApp.Fetch.Domain\bin\Debug\netstandard2.1"
    };
	
	const string prefix = "FL.Ebolapp.";

	var currentDir = new DirectoryInfo(Path.GetDirectoryName(Util.CurrentQueryPath));
	var baseDir = new DirectoryInfo(currentDir.Parent.Parent.FullName);
	var targetDir = currentDir.FullName + @"\Dll\";
	if (Directory.Exists(targetDir))
	{
		Directory.Delete(targetDir, true);
	}

	Directory.CreateDirectory(targetDir);

	var counter = 0;
	foreach (var path in paths)
	{
		var di = new DirectoryInfo(Path.Combine(baseDir.FullName, path));
		if (!di.Exists)
		{
			continue;
		}

		var files = di.GetFiles($"{prefix}*.dll");

		foreach (var fi in files)
		{
			var targetFilePath = Path.Combine(targetDir, fi.Name);
			File.Copy(fi.FullName, targetFilePath, true);
			Console.WriteLine(targetFilePath);
			counter++;
		}

	}

	Console.WriteLine($"Copies {counter} files.");
}