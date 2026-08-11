using Microsoft.Extensions.Logging.Abstractions;

namespace Acra.Test;

public sealed class AcraApiClientTest
{
	AcraMock mock = null!;
	AcraApiClient client = null!;

	[Before(HookType.Test)]
	public async Task Setup()
	{
		mock = new AcraMock();
		await mock.InitializeAsync();
		client = new AcraApiClient(NullLogger<AcraApiClient>.Instance, new AcraConfig(), new HttpClient { BaseAddress = mock.Host });
	}

	[After(HookType.Test)]
	public async Task Teardown() => await mock.DisposeAsync();

	[Test]
	public async Task SearchByUen_WhenFound_ReturnsEntity()
	{
		var entity = new AcraEntity
		{
			Uen = "201912345A",
			EntityName = "ACME PTE LTD",
			IssuanceAgencyDesc = "ACRA",
			UenStatusDesc = "Registered",
			EntityTypeDesc = "Local Company",
			UenIssueDate = "2019-05-01",
			RegStreetName = "ORCHARD ROAD",
			RegPostalCode = "238888",
		};
		mock.MockSearchByUen(entity.Uen, entity);

		var result = await client.SearchByUen(entity.Uen);

		await Assert.That(result).IsNotNull();
		await Assert.That(result!.Uen).IsEqualTo(entity.Uen);
		await Assert.That(result.EntityName).IsEqualTo(entity.EntityName);
		await Assert.That(result.IssuanceAgencyDesc).IsEqualTo(entity.IssuanceAgencyDesc);
		await Assert.That(result.UenStatusDesc).IsEqualTo(entity.UenStatusDesc);
		await Assert.That(result.EntityTypeDesc).IsEqualTo(entity.EntityTypeDesc);
		await Assert.That(result.UenIssueDate).IsEqualTo(entity.UenIssueDate);
		await Assert.That(result.RegStreetName).IsEqualTo(entity.RegStreetName);
		await Assert.That(result.RegPostalCode).IsEqualTo(entity.RegPostalCode);
	}

	[Test]
	public async Task SearchByUen_WhenNotFound_ReturnsNull()
	{
		mock.MockSearchByUen("999999999Z", null);

		var result = await client.SearchByUen("999999999Z");

		await Assert.That(result).IsNull();
	}

	[Test]
	public async Task SearchByName_WhenMatches_ReturnsRecordsAndTotal()
	{
		AcraEntity[] entities =
		[
			new() { Uen = "201900001A", EntityName = "ACME HOLDINGS PTE LTD" },
			new() { Uen = "201900002B", EntityName = "ACME TRADING PTE LTD" },
		];
		mock.MockSearchByName("ACME", entities, total: 50);

		var result = await client.SearchByName("ACME");

		await Assert.That(result.Records.Count).IsEqualTo(2);
		await Assert.That(result.Total).IsEqualTo(50);
		await Assert.That(result.Records[0].Uen).IsEqualTo(entities[0].Uen);
		await Assert.That(result.Records[1].Uen).IsEqualTo(entities[1].Uen);
	}

	[Test]
	public async Task SearchByName_WhenNoMatches_ReturnsEmptyResult()
	{
		mock.MockSearchByName("NONEXISTENT", []);

		var result = await client.SearchByName("NONEXISTENT");

		await Assert.That(result.Records.Count).IsEqualTo(0);
		await Assert.That(result.Total).IsEqualTo(0);
	}

	[Test]
	public async Task SearchByName_RespectsLimitAndOffset()
	{
		AcraEntity[] entities = [new() { Uen = "201900003C", EntityName = "PAGED PTE LTD" }];
		mock.MockSearchByName("PAGED", entities, limit: 20, offset: 5);

		var result = await client.SearchByName("PAGED", limit: 20, offset: 5);

		await Assert.That(result.Records.Count).IsEqualTo(1);
	}

	[Test]
	public async Task SearchByName_WithStatusFilter_FiltersToMatchingStatusClientSide()
	{
		AcraEntity[] entities =
		[
			new() { Uen = "201900004D", EntityName = "ACTIVE PTE LTD", UenStatusDesc = "Registered" },
			new() { Uen = "201900005E", EntityName = "ACTIVE HOLDINGS PTE LTD", UenStatusDesc = "Deregistered" },
		];
		mock.MockSearchByName("ACTIVE", entities, total: 2);

		var result = await client.SearchByName("ACTIVE", status: "Registered");

		await Assert.That(result.Records.Count).IsEqualTo(1);
		await Assert.That(result.Records[0].Uen).IsEqualTo(entities[0].Uen);
	}

	[Test]
	public async Task SearchByUen_WhenApiReturnsError_ThrowsAcraApiException()
	{
		mock.MockError("resource_id not found");

		await Assert.That(async () => await client.SearchByUen("ANY"))
			.Throws<AcraApiException>()
			.WithMessage("resource_id not found");
	}

	[Test]
	public async Task SearchByName_SendsCorrectlyShapedQuery()
	{
		mock.MockSearchByName("SHAPED", [], limit: 20, offset: 5);

		await client.SearchByName("SHAPED", limit: 20, offset: 5);

		var query = Uri.UnescapeDataString(mock.RecordedQueries.Last());
		await Assert.That(query).Contains($"resource_id={AcraConfig.DefaultResourceId}");
		await Assert.That(query).Contains("\"entity_name\":\"SHAPED\"");
		await Assert.That(query).Contains("limit=20");
		await Assert.That(query).Contains("offset=5");
	}
}
