using System.Text.Json;

using HFM.Core.Services;

using NUnit.Framework;

namespace HFM.Core.Serializers;

[TestFixture]
public class FahProjectDetailsTests
{
    private const string FullProjectListPath = @"TestFiles\projects.json";
    private static readonly FahProject p18706_Expected = new()
    {
        ProjectId = 18706,
        DescriptionId = 617,
        Manager = "Rafal Wiewiora",
        Modified = new(2022, 06, 21, 18, 53, 25),
    };
    private static readonly FahProject p18707_Expected = new()
    {
        ProjectId = 18707,
        DescriptionId = 617,
        Manager = "Rafal Wiewiora",
        Modified = new(2022, 06, 21, 18, 53, 25),
    };

    // Pulled from https://api2.foldingathome.org/project/18707 on 2022-08-06
    // Which is the same as https://api2.foldingathome.org/project/description/617 (where 617 is the description id for shared descriptions)
    private const string TestProjectDetails_JsonString = /*lang=json,strict*/ @"{""manager"":""Rafal Wiewiora"",""cause"":""cancer"",""description"":""<p>From https://www.tandfonline.com/doi/full/10.1080/1744666X.2022.2050596: &quot;Dysregulation of JAK signaling is a feature of many diseases. Targeting the JAK pathway started out as a therapeutic option in the field of myeloproliferative disorders. The JAK2 Val617Phe mutation, which results in constitutive activation of the pathway, is found in over 50% of individuals with myelofibrosis and essential thrombocythaemia, and almost 100% of patients with polycythemia vera [<a href=\""https://www.tandfonline.com/doi/full/10.1080/1744666X.2022.2050596#\"">5</a>,<a href=\""https://www.tandfonline.com/doi/full/10.1080/1744666X.2022.2050596#\"">6</a>]. Ruxolitinib, a JAK1/JAK2 inhibitor, was the first JAK inhibitor approved for clinical use to treat myelofibrosis in 2011.&quot;</p>\n\n<p>Starting from an AlphaFold structure of the JAK2 mutant, In these projects were are generating data to aid virtual screening efforts for new (more selective) inhibitors, as well as conducting small molecule fragment screens to find potential starting points for design efforts (which are being shared with all of you in real time, and will be summarized regularly).&nbsp;</p>\n\n<p>This is a project run by&nbsp;Roivant&nbsp;Sciences (formerly Silicon Therapeutics) as was officially announced in this press release:&nbsp;<a href=\""https://foldingathome.org/2021/04/20/maximizing-the-impact-of-foldinghome-by-engaging-industry-collaborators/\"" target=\""_blank\"">https://foldingathome.org/2021/04/20/maximizing-the-impact-of-foldinghome-by-engaging-industry-collaborators/</a></p>\n\n<p>All data is being made publicly available as soon as it is received at&nbsp;https://console.cloud.google.com/storage/browser/stxfah-bucket</p>\n"",""thumb"":"""",""url"":""roivant.com"",""institution"":""Roivant Sciences (Silicon Therapeutics)"",""mthumb"":"""",""mdescription"":""<p>Senior Investigator in Computational Biophysics at Roivant Sciences / Silicon Therapeutics.</p>\n\n<p>Formerly graduate student in&nbsp;Chodera Lab at&nbsp;Memorial Sloan Kettering Cancer Center.</p>\n\n<p>Interested in studying conformational dynamics of proteins using molecular dynamics&nbsp;and experimental methods, to make rational&nbsp;drug design&nbsp;better, cheaper and faster.&nbsp;</p>\n"",""modified"":""2022-06-21 18:53:25"",""projects"":""18706,18707""}";
    private static readonly FahProjectDetails TestProjectDetails_Value = new()
    {
        Projects = "18706,18707",
        Manager = "Rafal Wiewiora",
        Cause = "cancer",
        Description = "<p>From https://www.tandfonline.com/doi/full/10.1080/1744666X.2022.2050596: &quot;Dysregulation of JAK signaling is a feature of many diseases. Targeting the JAK pathway started out as a therapeutic option in the field of myeloproliferative disorders. The JAK2 Val617Phe mutation, which results in constitutive activation of the pathway, is found in over 50% of individuals with myelofibrosis and essential thrombocythaemia, and almost 100% of patients with polycythemia vera [<a href=\"https://www.tandfonline.com/doi/full/10.1080/1744666X.2022.2050596#\">5</a>,<a href=\"https://www.tandfonline.com/doi/full/10.1080/1744666X.2022.2050596#\">6</a>]. Ruxolitinib, a JAK1/JAK2 inhibitor, was the first JAK inhibitor approved for clinical use to treat myelofibrosis in 2011.&quot;</p>\n\n<p>Starting from an AlphaFold structure of the JAK2 mutant, In these projects were are generating data to aid virtual screening efforts for new (more selective) inhibitors, as well as conducting small molecule fragment screens to find potential starting points for design efforts (which are being shared with all of you in real time, and will be summarized regularly).&nbsp;</p>\n\n<p>This is a project run by&nbsp;Roivant&nbsp;Sciences (formerly Silicon Therapeutics) as was officially announced in this press release:&nbsp;<a href=\"https://foldingathome.org/2021/04/20/maximizing-the-impact-of-foldinghome-by-engaging-industry-collaborators/\" target=\"_blank\">https://foldingathome.org/2021/04/20/maximizing-the-impact-of-foldinghome-by-engaging-industry-collaborators/</a></p>\n\n<p>All data is being made publicly available as soon as it is received at&nbsp;https://console.cloud.google.com/storage/browser/stxfah-bucket</p>\n",
        Modified = new(2022, 06, 21, 18, 53, 25)
    };

    [Test]
    public void TestDeserializeProjectList()
    {
        if (!File.Exists(FullProjectListPath))
            Assert.Inconclusive($"Unable to find test data file {FullProjectListPath}");

        var options = new JsonSerializerOptions();
        options.Converters.Add(new FahApiDateTimeConverter());
        string json = File.ReadAllText(FullProjectListPath);
        var results = JsonSerializer.Deserialize<FahProject[]>(json, options);
        Assert.IsNotEmpty(results);

        var p18706 = results.FirstOrDefault(x => x.ProjectId == 18706);
        CompareProjects(p18706_Expected, p18706!);

        var p18707 = results.FirstOrDefault(x => x.ProjectId == 18707);
        CompareProjects(p18707_Expected, p18707!);
    }

    [Test]
    public void TestDeserializeProjectDetails()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new FahApiDateTimeConverter());

        CompareProjectDetails(TestProjectDetails_Value,
            JsonSerializer.Deserialize<FahProjectDetails>(TestProjectDetails_JsonString, options));
    }

    private static void CompareProjects(FahProject expected, FahProject actual)
    {
        Assert.AreEqual(expected.ProjectId, actual.ProjectId);
        Assert.AreEqual(expected.DescriptionId, actual.DescriptionId);
        Assert.AreEqual(expected.Manager, actual.Manager);
        Assert.AreEqual(expected.Modified, actual.Modified);
    }

    private static void CompareProjectDetails(FahProjectDetails expected, FahProjectDetails actual)
    {
        Assert.AreEqual(expected.Projects, actual.Projects);
        Assert.AreEqual(expected.Manager, actual.Manager);
        Assert.AreEqual(expected.Cause, actual.Cause);
        Assert.AreEqual(expected.Modified, actual.Modified);
        Assert.AreEqual(expected.Description, actual.Description);
    }
}
