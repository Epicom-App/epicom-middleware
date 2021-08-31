<Query Kind="Program">
  <Reference Relative="Dll\FL.Ebolapp.FunctionApps.Fetch.dll">C:\repos\epicom-middleware\scripts\linqpad\Dll\FL.Ebolapp.FunctionApps.Fetch.dll</Reference>
  <Reference Relative="Dll\FL.Ebolapp.FunctionsApp.Fetch.Domain.dll">C:\repos\epicom-middleware\scripts\linqpad\Dll\FL.Ebolapp.FunctionsApp.Fetch.Domain.dll</Reference>
  <Reference Relative="Dll\Fl.Ebolapp.Shared.Infrastructure.Azure.Blob.dll">C:\repos\epicom-middleware\scripts\linqpad\Dll\Fl.Ebolapp.Shared.Infrastructure.Azure.Blob.dll</Reference>
  <Reference Relative="Dll\Fl.Ebolapp.Shared.Infrastructure.Azure.dll">C:\repos\epicom-middleware\scripts\linqpad\Dll\Fl.Ebolapp.Shared.Infrastructure.Azure.dll</Reference>
  <Reference Relative="Dll\FL.Ebolapp.Shared.Infrastructure.dll">C:\repos\epicom-middleware\scripts\linqpad\Dll\FL.Ebolapp.Shared.Infrastructure.dll</Reference>
  <Reference Relative="Dll\FL.Ebolapp.Shared.Infrastructure.Extensions.dll">C:\repos\epicom-middleware\scripts\linqpad\Dll\FL.Ebolapp.Shared.Infrastructure.Extensions.dll</Reference>
  <NuGetReference>Azure.Storage.Blobs</NuGetReference>
  <Namespace>Azure.Storage.Blobs</Namespace>
  <Namespace>Azure.Storage.Blobs.Models</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Runtime.Serialization</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

// OBJECTID, RS, GEN, BEZ, BL, BL_ID, county, Website Link
string env;

public static class Environments
{
	public const string Development = "dev";
	public const string Production = "prod";
}

public class AreaModel
{
	// BL_ID
	[JsonPropertyName("stateId")]
	public int StateId { get; set; }

	// GEN + BEZ
	[JsonPropertyName("stateName")]
	public string Name { get; set; }

	// Website Link
	[JsonPropertyName("informationUrl")]
	public string InformationUrl { get; set; }

	// OBJECTID
	[JsonPropertyName("areaId")]
	public int AreaId { get; set; }

	// RS
	[JsonPropertyName("areaCode")]
	public string AreaCode { get; set; }
}

public static class Columns
{
	public static int OBJECTID = 0;
	public static int RS = 1;
	public static int GEN = 2;
	public static int BEZ = 3;
	public static int BL = 4;
	public static int BL_ID = 5;
	public static int County = 6;
	public static int WebsiteLink = 7;
}

async Task Main()
{
	env = Environments.Development; // dev | prod

	var connectionStringConfig = Util.GetPassword($"FL.Ebolapp.Blob.Config.{env}");
	var clientConfig = new BlobContainerClient(connectionStringConfig, "config");

	var maxAge = env == Environments.Development ? TimeSpan.FromMinutes(1) : TimeSpan.FromDays(1);
	
	var docId = Util.GetPassword("FL_Areas_Google_DocumentID");
	var gId = Util.GetPassword("FL_Areas_Google_GID");
	var url = $"https://docs.google.com/spreadsheets/d/{docId}/export?format=tsv&id={docId}&gid={gId}";

	int numberOfColumns = 8;

	var data = await GetAreaData(numberOfColumns, url);
	await UploadAreas(clientConfig, data, maxAge);
}

public async Task<List<AreaModel>> GetAreaData(int numberOfColumns, string url)
{
	var result = new List<AreaModel>();
	using HttpClient httpClient = new();

	using HttpResponseMessage response = await httpClient.GetAsync(new Uri(url));
	using var file = new System.IO.StreamReader(await response.Content.ReadAsStreamAsync());

	int counter = 0;
	string row;

	while (!file.EndOfStream)
	{
		counter++;
		row = file.ReadLine();

		// skip header
		if (counter == 1)
		{
			continue;
		}

		var data = row.Split('\t');
		if (data.Length < numberOfColumns)
		{
			throw new Exception($"Found more columns! Supported number of columns is: [{numberOfColumns}]");
		}

		result.Add(new AreaModel
		{
			AreaId = int.Parse(data[Columns.OBJECTID]),
			AreaCode = data[Columns.RS],
			Name = $"{data[Columns.GEN]}, {data[Columns.BEZ]}",
			StateId = int.Parse(data[Columns.BL_ID]),
			InformationUrl = data[Columns.WebsiteLink],
		});
	}

	return result;
}

public async Task UploadAreas(BlobContainerClient client, List<AreaModel> data, TimeSpan maxAge)
{
	var blob = client.GetBlobClient("areas.json");
	var jsonString = System.Text.Json.JsonSerializer.Serialize(
		data,
		new JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		}
	);
	
	jsonString.Dump();

	Console.WriteLine($"Upload to [{env}] - [{blob.Name}] (y/n), default: n");
	var input = Console.ReadLine();
	if(!input.ToUpper().Equals("Y"))
	{
		return;
	}

	var contentStream = new MemoryStream();
	var streamWriter = new StreamWriter(contentStream);
	await streamWriter.WriteAsync(jsonString);
	await streamWriter.FlushAsync();
	contentStream.Seek(0L, SeekOrigin.Begin);

	var response = await blob.UploadAsync(contentStream, new BlobUploadOptions
	{
		HttpHeaders = new BlobHttpHeaders
		{
			ContentType = FL.Ebolapp.Shared.Infrastructure.MimeType.Json,
			CacheControl = $"max-age={(int)maxAge.TotalSeconds}",
		}
	});
	
	Console.WriteLine("Upload done.");
}













