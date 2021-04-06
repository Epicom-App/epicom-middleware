<Query Kind="Program">
  <Reference Relative="Dll\FL.Ebolapp.FunctionApps.Fetch.dll">C:\repos\Freunde-Liberias-EBOLAPP-Backend\scripts\linqpad\Dll\FL.Ebolapp.FunctionApps.Fetch.dll</Reference>
  <Reference Relative="Dll\FL.Ebolapp.FunctionsApp.Fetch.Domain.dll">C:\repos\Freunde-Liberias-EBOLAPP-Backend\scripts\linqpad\Dll\FL.Ebolapp.FunctionsApp.Fetch.Domain.dll</Reference>
  <Reference Relative="Dll\Fl.Ebolapp.Shared.Infrastructure.Azure.Blob.dll">C:\repos\Freunde-Liberias-EBOLAPP-Backend\scripts\linqpad\Dll\Fl.Ebolapp.Shared.Infrastructure.Azure.Blob.dll</Reference>
  <Reference Relative="Dll\Fl.Ebolapp.Shared.Infrastructure.Azure.dll">C:\repos\Freunde-Liberias-EBOLAPP-Backend\scripts\linqpad\Dll\Fl.Ebolapp.Shared.Infrastructure.Azure.dll</Reference>
  <Reference Relative="Dll\FL.Ebolapp.Shared.Infrastructure.dll">C:\repos\Freunde-Liberias-EBOLAPP-Backend\scripts\linqpad\Dll\FL.Ebolapp.Shared.Infrastructure.dll</Reference>
  <Reference Relative="Dll\FL.Ebolapp.Shared.Infrastructure.Extensions.dll">C:\repos\Freunde-Liberias-EBOLAPP-Backend\scripts\linqpad\Dll\FL.Ebolapp.Shared.Infrastructure.Extensions.dll</Reference>
  <NuGetReference>Azure.Storage.Blobs</NuGetReference>
  <Namespace>Azure.Storage.Blobs</Namespace>
  <Namespace>Azure.Storage.Blobs.Models</Namespace>
  <Namespace>FL.Ebolapp.FunctionsApp.Fetch.Domain.Entities</Namespace>
  <Namespace>FL.Ebolapp.Shared.Infrastructure</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main()
{
	var env = "dev"; // dev | prod
	var l18n = new[] { "de", "en" };

	var connectionStringConfig = Util.GetPassword($"FL.Ebolapp.Blob.Config.{env}");
	var connectionStringStatic = Util.GetPassword($"FL.Ebolapp.Blob.Static.{env}");

	// Covid-19-Fälle der letzten 7 Tage/100.000 Einwohner
	// Cases in the last 7 days/100,000 inhabitants

	var clientConfig = new BlobContainerClient(connectionStringConfig, "config");
	var clientStatic = new BlobContainerClient(connectionStringConfig, "static");
	
	var maxAge = env == "dev" ? TimeSpan.FromMinutes(1) : TimeSpan.FromDays(1);
	
	var legend = await GetLegendConfig(clientConfig);
	
	foreach (var ln in l18n)
	{
		var name = await Util.ReadLineAsync($"Enter value for key [{legend.Name}] for property [{nameof(legend.Name)}] for language [{ln}].", legend.Name);
		var legendLn = new LegendConfig
		{
			Name = name,
			Items = legend.Items
		};
		
		legendLn.Dump();

		await UploadStaticLegend(clientStatic, legendLn, ln, (int)maxAge.TotalSeconds);
	}
}

public async Task UploadStaticLegend(BlobContainerClient client, LegendConfig legend, string language, int maxAgeInSeconds = 0)
{
	var blob = client.GetBlobClient($"{language}/legend.json");
	var contentString = JsonSerializer.Serialize(legend, new JsonSerializerOptions
	{
		WriteIndented = true,
		Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	});
	contentString.Dump();
	
	var contentStream = new MemoryStream();
	var streamWriter = new StreamWriter(contentStream);
	await streamWriter.WriteAsync(contentString);
	await streamWriter.FlushAsync();
	contentStream.Seek(0L, SeekOrigin.Begin);
	
	var response = await blob.UploadAsync(contentStream, new BlobUploadOptions
	{
		HttpHeaders = new BlobHttpHeaders
		{
			ContentType = MimeType.Json,
			CacheControl = $"max-age={maxAgeInSeconds}",
		}
	});
}

public async Task<LegendConfig> GetLegendConfig(BlobContainerClient client)
{
	var blobLegend = client.GetBlobClient("legend.json");
	if (!await blobLegend.ExistsAsync())
	{
		throw new Exception($"Blob [{blobLegend.Uri}] not found!");
	}
	
	using var legendStream = new MemoryStream();
	await blobLegend.DownloadToAsync(legendStream);
	legendStream.Seek(0L, SeekOrigin.Begin);
	using var legendReader = new StreamReader(legendStream);
	var legendString = await legendReader.ReadToEndAsync();
	legendString.Dump();
	return JsonSerializer.Deserialize<LegendConfig>(legendString);
}











