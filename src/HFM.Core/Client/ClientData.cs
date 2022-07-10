using HFM.Core.WorkUnits;
using HFM.Log;
using HFM.Preferences;
using HFM.Proteins;
using System.ComponentModel;

namespace HFM.Core.Client;

public enum PcieVersionType
{
    [Description("")]
    Unknown = 0,
    [Description("1.0")]
    v1 = 1,
    [Description("2.0")]
    v2 = 2,
    [Description("3.0")]
    v3 = 3,
    [Description("4.0")]
    v4 = 4,
    [Description("5.0")]
    v5 = 5,
    [Description("6.0")]
    v6 = 6,
    [Description("7.0")]
    v7 = 7,
}

public enum PcieLanesType
{
    [Description("")]
    Unknown = 0,
    [Description("x1")]
    x1 = 1,
    [Description("x2")]
    x2 = 2,
    [Description("x4")]
    x4 = 4,
    [Description("x8")]
    x8 = 8,
    [Description("x16")]
    x16 = 16,
}

public interface IClientData
{
    SlotStatus Status { get; }
    int PercentComplete { get; }
    string Name { get; }
    string SlotTypeString { get; }
    string Processor { get; }
    TimeSpan TPF { get; }
    double PPD { get; }
    double UPD { get; }
    TimeSpan ETA { get; }
    DateTime ETADate { get; }
    string Core { get; }
    string ProjectRunCloneGen { get; }
    string ProjectCause { get; }
    double Credit { get; }
    int Completed { get; }
    int Failed { get; }
    string Username { get; }
    DateTime Assigned { get; }
    DateTime PreferredDeadline { get; }

    IProjectInfo ProjectInfo { get; }
    IProductionProvider ProductionProvider { get; }
    IReadOnlyCollection<LogLine> CurrentLogLines { get; }
    ValidationRuleErrors Errors { get; }

    string FoldingID { get; }
    int Team { get; }
    ClientSettings Settings { get; }
    ClientPlatform Platform { get; }
    Protein CurrentProtein { get; }

    int? GPUBus { get; }
    double? GPUFanSpeed { get; }
    double? GPUCoreTemp_C { get; }
    string GPUPcieCurrent { get; }
    string GPUPcieMax { get; }
    string GPUPowerState { get; }
    double? GPUPowerDrawCurrent_Watts { get; }
    double? GPUPowerLimitCurrent_Watts { get; }
    double? GPUPowerLimitDefault_Watts { get; }
    double? GPUPowerLimitRatio { get; }
    double? GPUGraphicsClock_MHz { get; }
    double? GPUMemoryClock_MHz { get; }

    SlotIdentifier SlotIdentifier { get; }
    ProteinBenchmarkIdentifier BenchmarkIdentifier { get; }
}

public class ClientData : IClientData
{
    public virtual SlotStatus Status { get; set; }
    public virtual int PercentComplete { get; set; }
    public virtual string Name { get; set; }
    public virtual string SlotTypeString { get; set; }
    public virtual string Processor { get; set; }
    public virtual TimeSpan TPF { get; set; }
    public virtual double PPD { get; set; }
    public virtual double UPD { get; set; }
    public virtual TimeSpan ETA { get; set; }
    public virtual DateTime ETADate { get; set; }
    public virtual string Core { get; set; }
    public virtual string ProjectRunCloneGen { get; set; }
    public virtual string ProjectCause { get; set; }
    public virtual double Credit { get; set; }
    public virtual int Completed { get; set; }
    public virtual int Failed { get; set; }
    public virtual string Username { get; set; }
    public virtual DateTime Assigned { get; set; }
    public virtual DateTime PreferredDeadline { get; set; }

    public virtual IProjectInfo ProjectInfo { get; set; }
    public virtual IProductionProvider ProductionProvider { get; set; }
    public virtual IReadOnlyCollection<LogLine> CurrentLogLines { get; set; } = new List<LogLine>();
    public ValidationRuleErrors Errors { get; } = new();

    public virtual string FoldingID { get; set; }
    public virtual int Team { get; set; }
    public virtual ClientSettings Settings { get; set; }
    public virtual ClientPlatform Platform { get; set; }
    public virtual Protein CurrentProtein { get; set; }

    public virtual int? GPUBus => null;

    public virtual double? GPUFanSpeed { get; set; }

    public virtual double? GPUCoreTemp_C { get; set; }

    public virtual string GPUPcieCurrent => "";

    public virtual string GPUPcieMax => "";

    public virtual string GPUPowerState { get; set; }

    public virtual double? GPUPowerDrawCurrent_Watts { get; set; }

    public virtual double? GPUPowerLimitCurrent_Watts { get; set; }

    public virtual double? GPUPowerLimitDefault_Watts { get; set; }

    public virtual double? GPUPowerLimitRatio => GPUPowerLimitCurrent_Watts / GPUPowerLimitDefault_Watts;

    public virtual double? GPUGraphicsClock_MHz { get; set; }

    public virtual double? GPUMemoryClock_MHz { get; set; }


    public virtual SlotIdentifier SlotIdentifier { get; set; }
    public virtual ProteinBenchmarkIdentifier BenchmarkIdentifier { get; set; }

    public static ClientData Offline(ClientSettings settings) =>
        new()
        {
            Status = SlotStatus.Offline,
            Name = settings?.Name,
            Settings = settings
        };

    public static void ValidateRules(ICollection<IClientData> collection, IPreferences preferences)
    {
        var rules = new IClientDataValidationRule[]
        {
            new ClientUsernameValidationRule(preferences),
            new ClientProjectIsDuplicateValidationRule(ClientProjectIsDuplicateValidationRule.FindDuplicateProjects(collection))
        };

        foreach (var c in collection)
        {
            foreach (var rule in rules)
            {
                rule.Validate(c);
            }
        }
    }
}
