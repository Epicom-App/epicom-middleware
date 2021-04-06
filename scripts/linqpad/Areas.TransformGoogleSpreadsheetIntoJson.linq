<Query Kind="Program">
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Runtime.Serialization</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

List<AreaModel> columns = new ();

// OBJECTID, RS, GEN, BEZ, BL, BL_ID, county, Website Link

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
	var docId = Util.GetPassword("FL_Areas_Google_DocumentID");
	var gId = Util.GetPassword("FL_Areas_Google_GID");
	var url = $"https://docs.google.com/spreadsheets/d/{docId}/export?format=tsv&id={docId}&gid={gId}";

	int numberOfColumns = 8;

	using HttpClient httpClient = new ();	
	
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

		columns.Add(new AreaModel
		{
			AreaId = int.Parse(data[Columns.OBJECTID]),
			AreaCode = data[Columns.RS],
			Name = $"{data[Columns.GEN]}, {data[Columns.BEZ]}",
			StateId = int.Parse(data[Columns.BL_ID]),
			InformationUrl = data[Columns.WebsiteLink],
		});
	}

	System.Text.Json.JsonSerializer.Serialize(
		columns,
		new JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		}).Dump();
}















