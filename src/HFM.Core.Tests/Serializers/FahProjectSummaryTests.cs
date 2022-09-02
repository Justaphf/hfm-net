using System.Text.Json;

using HFM.Core.Services;

using NUnit.Framework;

namespace HFM.Core.Serializers;

[TestFixture]
public class FahProjectSummaryTests
{
    // NOTE: At the time of implementation, this list of project in projects.json was created from the full result
    //       set from querying https://api2.foldingathome.org/project on 2022-08-06
    private const string FullProjectListPath = @"TestFiles\FAHprojects.json";
    private static readonly FahProjectSummary _P18706_Expected = new()
    {
        ProjectId = 18706,
        DescriptionId = 617,
        Manager = "Rafal Wiewiora",
        Modified = new(2022, 06, 21, 18, 53, 25),
    };
    private static readonly FahProjectSummary _P18707_Expected = new()
    {
        ProjectId = 18707,
        DescriptionId = 617,
        Manager = "Rafal Wiewiora",
        Modified = new(2022, 06, 21, 18, 53, 25),
    };

    private static void CompareProjects(FahProjectSummary expected, FahProjectSummary actual)
    {
        Assert.AreEqual(expected.ProjectId, actual.ProjectId);
        Assert.AreEqual(expected.DescriptionId, actual.DescriptionId);
        Assert.AreEqual(expected.Manager, actual.Manager);
        Assert.AreEqual(expected.Modified, actual.Modified);
    }

    [Test]
    public void TestDeserializeProjectList()
    {
        if (!File.Exists(FullProjectListPath))
            Assert.Inconclusive($"Unable to find test data file {FullProjectListPath}");

        var options = new JsonSerializerOptions();
        options.Converters.Add(new FahApiDateTimeConverter());
        string json = File.ReadAllText(FullProjectListPath);
        var results = JsonSerializer.Deserialize<FahProjectSummary[]>(json, options);
        Assert.IsNotEmpty(results);

        var p18706 = results.FirstOrDefault(x => x.ProjectId == 18706);
        CompareProjects(_P18706_Expected, p18706!);

        var p18707 = results.FirstOrDefault(x => x.ProjectId == 18707);
        CompareProjects(_P18707_Expected, p18707!);
    }
}
