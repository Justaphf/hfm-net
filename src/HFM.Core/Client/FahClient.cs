using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;

using HFM.Client;
using HFM.Core.Data;
using HFM.Core.Logging;
using HFM.Core.Services;
using HFM.Core.WorkUnits;
using HFM.Log;
using HFM.Preferences;
using HFM.Proteins;

namespace HFM.Core.Client;

public interface IFahClient : IClient
{
    FahClientConnection Connection { get; }
}

public enum NvidiaSmiQueryStatus
{
    Success,
    UnsupportedOS,
    InvalidServerName,
    SshNotEnabled,
    SshNotConfigured,
    ConnectionFailure,
    AuthenticationFailure,
    QueryFailure,

    UnknownError
}

public class FahClient : Client, IFahClient, IFahClientCommand
{
    protected override void OnSettingsChanged(ClientSettings oldSettings, ClientSettings newSettings)
    {
        Debug.Assert(newSettings.ClientType == ClientType.FahClient);

        if (oldSettings != null)
        {
            if (oldSettings.Server != newSettings.Server ||
                oldSettings.Port != newSettings.Port ||
                oldSettings.Password != newSettings.Password ||
                oldSettings.Disabled != newSettings.Disabled)
            {
                // close existing connection and allow retrieval to open a new connection
                Connection?.Close();
            }
            else
            {
                RefreshSlots();
            }
        }
    }

    public IProteinBenchmarkRepository Benchmarks { get; }
    public IProteinService ProteinService { get; }
    public IWorkUnitRepository WorkUnitRepository { get; }
    public FahClientMessages Messages { get; protected set; }
    public FahClientConnection Connection { get; protected set; }

    public FahClient(ILogger logger,
                     IPreferences preferences,
                     IProteinBenchmarkRepository benchmarks,
                     IProteinService proteinService,
                     IWorkUnitRepository workUnitRepository)
        : base(logger, preferences)
    {
        Benchmarks = benchmarks;
        ProteinService = proteinService;
        WorkUnitRepository = workUnitRepository;
        Messages = new FahClientMessages(Logger, Preferences);
        _messageActions = new List<FahClientMessageAction>
        {
            new DelegateFahClientMessageAction(FahClientMessageType.SlotInfo, RefreshSlots),
            new DelegateFahClientMessageAction(FahClientMessageType.Info, RefreshClientPlatform),
            new ExecuteRetrieveMessageAction(Messages, async () => await Retrieve().ConfigureAwait(false))
        };
    }

    private readonly List<FahClientMessageAction> _messageActions;

    protected virtual async Task OnMessageRead(FahClientMessage message)
    {
        if (IsCancellationRequested) return;

        Logger.Debug(String.Format(Logging.Logger.NameFormat, Settings.Name, $"{message.Identifier} - Length: {message.MessageText.Length}"));

        bool updated = await Messages.UpdateMessageAsync(message, this).ConfigureAwait(false);
        if (updated)
        {
            _messageActions.ForEach(x => x.Execute(message.Identifier.MessageType));
        }
    }

    private List<IClientData> _clientData = new();

    public void RefreshSlots()
    {
        var slots = new List<IClientData>();
        OnRefreshSlots(slots);
        Interlocked.Exchange(ref _clientData, slots);

        OnClientDataChanged();
    }

    /// <summary>
    /// The user name of the Debian/Ubunut Linux user to use for nvidia-smi monitoring
    /// </summary>
    private const string DEBIAN_CLIENT_USERNAME = "hfmclient";

    /// <summary>
    /// The desired home directory of the <see cref="DEBIAN_CLIENT_USERNAME"/> user
    /// </summary>
    private const string DEBIAN_CLIENT_HOME_DIR = "/home/HFM.NET";

    /// <summary>
    /// This is the exact command line to use when querying nvidia-smi to obtain GPU statistics
    /// <br/>Works with PowerShell (Windows) and sh/bash (Debian/Ubunut flavors of linux)
    /// <br/>NOTE: May work for all flavors of OS as there isn't anything OS syntax specific in this command
    /// </summary>
    private const string NVIDIA_SMI_QUERY_STRING = "nvidia-smi --query-gpu=pci.bus,fan.speed,temperature.gpu," +
        "pcie.link.gen.current,pcie.link.width.current,pcie.link.gen.max,pcie.link.width.max,pstate,power.draw," +
        "power.limit,power.default_limit,clocks.current.graphics,clocks.current.memory --format=csv,noheader,nounits";

    internal readonly record struct GPUStatistic(int BusId, double FanSpeed_Pct, double GPUTemp_C,
        PcieVersionType PcieVersionCurrent, PcieLanesType PcieLanesCurrent, PcieVersionType PcieVersionMax, PcieLanesType PcieLanesMax,
        string PowerState, double CurrentPower_Watt, double PowerLimitCurrent_Watt, double PowerLimitDefault_Watt,
        double GraphicsClock_MHz, double MemoryClock_MHz);

    public static void CreateNewSshRsaKeyPair(ClientSettings settings, out string publicKey)
    {
        using var keygen = new SshKeyGenerator.SshKeyGenerator(4096);
        publicKey = keygen.ToRfcPublicKey($"{DEBIAN_CLIENT_USERNAME}@HFM.NET");

        // REVISIT: Properly save the private key to HFM.NET key store for this server
        // TODO: This at least needs to trigger a persistence update
        settings.SshPrivateKey = keygen.ToPrivateKey();

        // DEBUG: Save to files as if they were generated as regular 
        string privateKeyFile = $@"PRIVATE_KEY_FILE_BASE_PATH\id_rsa_hfmclient.{settings.Server}";
        string publicKeyFile = $"{privateKeyFile}.pub";
        File.WriteAllText(privateKeyFile, settings.SshPrivateKey);
        File.WriteAllText(publicKeyFile, publicKey);
    }

    private NvidiaSmiQueryStatus TryRetrieveNvidiaSmiStatistics(out IReadOnlyDictionary<int, GPUStatistic> gpuStats)
    {
        gpuStats = null;

        string os = Platform?.OperatingSystem;
        if (String.IsNullOrWhiteSpace(os)) return NvidiaSmiQueryStatus.UnsupportedOS;
        // REVISIT: This almost certainly isn't robust enough to catch all flavors of operating systems (but it works on my setup for now)
        bool isWindows = os.Contains("windows", StringComparison.InvariantCultureIgnoreCase);
        bool isLinux = os.Contains("linux", StringComparison.InvariantCultureIgnoreCase);

        // nvidia-smi accessible via SSH on Linux, and PowerShell on local Windows machines
        // NOTE: Remote Windows can also be accessed via PowerShell, but it requires some non-standard configuration first
        // REVISIT: Haven't looked at mac since I don't have the hardware for any testing
        if (!isWindows && !isLinux) return NvidiaSmiQueryStatus.UnsupportedOS;

        string server = Settings?.Server;
        if (String.IsNullOrWhiteSpace(server)) return NvidiaSmiQueryStatus.InvalidServerName;

        return isWindows ?
            _TryRetrieveNvidiaSmiStatistics_Windows(out gpuStats) :
            _TryRetrieveNvidiaSmiStatistics_Linux(out gpuStats);
    }

    private NvidiaSmiQueryStatus _TryRetrieveNvidiaSmiStatistics_Linux(out IReadOnlyDictionary<int, GPUStatistic> gpuStats)
    {
        gpuStats = null;
        if (Settings?.EnableSsh != true) return NvidiaSmiQueryStatus.SshNotEnabled;

        if (!ClientSettings.DoesLinuxUserNameLookValid(Settings.SshUserName) ||
            !ClientSettings.DoesSshRsaPrivateKeyLookValid(Settings.SshPrivateKey) ||
            !ClientSettings.DoesPortLookValid(Settings.Port))
            return NvidiaSmiQueryStatus.SshNotConfigured;

        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Settings.SshPrivateKey));
            using var sshKeyFile = new PrivateKeyFile(stream);
            using var client = new SshClient(Settings.Server, Settings.SshPort, Settings.SshUserName, sshKeyFile);
            client.Connect();
            using var cmd = client.CreateCommand(NVIDIA_SMI_QUERY_STRING);
            var result = cmd.Execute();
            if (cmd.ExitStatus != 0 || !String.IsNullOrWhiteSpace(cmd.Error)) return NvidiaSmiQueryStatus.QueryFailure;

            gpuStats = result.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(x => new GPUStatistic(
                    //Int32.TryParse(x[0], NumberStyles.AllowHexSpecifier, null, out var busId) ? busId : -1,
                    // REVISIT: Need a non-throwing method for Hex conversion, but there doesn't appear to be a built-in method
                    Convert.ToInt32(x[0], 16),
                    Double.TryParse(x[1], out var fanSpeed) ? fanSpeed : 0.0d,
                    Double.TryParse(x[2], out var temp) ? temp : 0.0d,
                    Int32.TryParse(x[3], out int tempInt) ? (PcieVersionType)tempInt : PcieVersionType.Unknown,
                    Int32.TryParse(x[4], out tempInt) ? (PcieLanesType)tempInt : PcieLanesType.Unknown,
                    Int32.TryParse(x[5], out tempInt) ? (PcieVersionType)tempInt : PcieVersionType.Unknown,
                    Int32.TryParse(x[6], out tempInt) ? (PcieLanesType)tempInt : PcieLanesType.Unknown,
                    x[7],
                    Double.TryParse(x[8], out temp) ? temp : 0.0d,
                    Double.TryParse(x[9], out temp) ? temp : 0.0d,
                    Double.TryParse(x[10], out temp) ? temp : 0.0d,
                    Double.TryParse(x[11], out temp) ? temp : 0.0d,
                    Double.TryParse(x[12], out temp) ? temp : 0.0d))
                .ToDictionary(x => x.BusId);

            return NvidiaSmiQueryStatus.Success;
        }
        catch (SshConnectionException e)
        {
            Trace.Fail(e.Message, e.ToString());
            return NvidiaSmiQueryStatus.ConnectionFailure;
        }
        catch (SshAuthenticationException e)
        {
            Trace.Fail(e.Message, e.ToString());
            return NvidiaSmiQueryStatus.AuthenticationFailure;
        }
        catch (Exception e)
        {
            Trace.Fail(e.Message, e.ToString());
            return NvidiaSmiQueryStatus.UnknownError;
        }
    }

    private NvidiaSmiQueryStatus _TryRetrieveNvidiaSmiStatistics_Windows(out IReadOnlyDictionary<int, GPUStatistic> gpuStats)
    {
        gpuStats = null;
        // TODO: Get working for remote Windows machines (this only works on the local machine)
        string server = Settings.Server;
        try
        {
            using var shell = PowerShell.Create();
            shell.Runspace = null;
            shell.AddScript(NVIDIA_SMI_QUERY_STRING);
            var results = shell.Invoke();
            gpuStats = results.Select(x => x.ToString())
                .Select(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(x => new GPUStatistic(
                    //Int32.TryParse(x[0], NumberStyles.AllowHexSpecifier, null, out var busId) ? busId : -1,
                    // REVISIT: Need a non-throwing method for Hex conversion, but there doesn't appear to be a built-in method
                    Convert.ToInt32(x[0], 16),
                    Double.TryParse(x[1], out var fanSpeed) ? fanSpeed : 0.0d,
                    Double.TryParse(x[2], out var temp) ? temp : 0.0d,
                    Int32.TryParse(x[3], out int tempInt) ? (PcieVersionType)tempInt : PcieVersionType.Unknown,
                    Int32.TryParse(x[4], out tempInt) ? (PcieLanesType)tempInt : PcieLanesType.Unknown,
                    Int32.TryParse(x[5], out tempInt) ? (PcieVersionType)tempInt : PcieVersionType.Unknown,
                    Int32.TryParse(x[6], out tempInt) ? (PcieLanesType)tempInt : PcieLanesType.Unknown,
                    x[7],
                    Double.TryParse(x[8], out temp) ? temp : 0.0d,
                    Double.TryParse(x[9], out temp) ? temp : 0.0d,
                    Double.TryParse(x[10], out temp) ? temp : 0.0d,
                    Double.TryParse(x[11], out temp) ? temp : 0.0d,
                    Double.TryParse(x[12], out temp) ? temp : 0.0d))
                .ToDictionary(x => x.BusId);

            return NvidiaSmiQueryStatus.Success;
        }
        catch (Exception e)
        {
            Trace.Fail(e.Message, e.ToString());
            return NvidiaSmiQueryStatus.UnknownError;
        }
    }

    protected virtual void OnRefreshSlots(ICollection<IClientData> collection)
    {
        var slotCollection = Messages?.SlotCollection;
        if (slotCollection is { Count: > 0 })
        {
            foreach (var slot in slotCollection)
            {
                var slotDescription = SlotDescription.Parse(slot.Description);
                var status = (SlotStatus)Enum.Parse(typeof(SlotStatus), slot.Status, true);
                var slotID = slot.ID.GetValueOrDefault();
                var clientData = new FahClientData(Preferences, this, status, slotID)
                {
                    Description = slotDescription
                };
                collection.Add(clientData);
            }
        }
    }

    protected override IReadOnlyCollection<IClientData> OnGetClientDataCollection() =>
        _clientData.Count > 0
            ? _clientData
            : base.OnGetClientDataCollection();

    private void RefreshClientPlatform()
    {
        var info = Messages.Info;
        if (info is not null)
        {
            Platform = new ClientPlatform(info.Client.Version, info.System.OS);
        }
    }

    protected override void OnClose()
    {
        base.OnClose();

        if (Connected)
        {
            try
            {
                Connection.Close();
            }
            catch (Exception ex)
            {
                Logger.Error(String.Format(Logging.Logger.NameFormat, Settings.Name, ex.Message), ex);
            }
        }

        // reset messages
        Messages.Clear();
        // refresh (clear) the slots
        RefreshSlots();
    }

    public override bool Connected => Connection is { Connected: true };

    protected override async Task OnConnect()
    {
        await CreateAndOpenConnection().ConfigureAwait(false);

        _ = Task.Run(async () => await ReadMessagesFromConnection().ConfigureAwait(false))
            .ContinueWith(_ => Close(),
                CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }

    private async Task CreateAndOpenConnection()
    {
        Connection = new FahClientConnection(Settings.Server, Settings.Port);
        await Connection.OpenAsync().ConfigureAwait(false);
        if (!String.IsNullOrWhiteSpace(Settings.Password))
        {
            await Connection.CreateCommand("auth " + Settings.Password).ExecuteAsync().ConfigureAwait(false);
        }
        if (Connected)
        {
            await SetupClientToSendMessageUpdatesAsync().ConfigureAwait(false);
        }
    }

    internal async Task SetupClientToSendMessageUpdatesAsync()
    {
        var heartbeatCommandText = String.Format(CultureInfo.InvariantCulture, "updates add 0 {0} $heartbeat", FahClientMessages.HeartbeatInterval);

        await Connection.CreateCommand("updates clear").ExecuteAsync().ConfigureAwait(false);
        await Connection.CreateCommand("log-updates restart").ExecuteAsync().ConfigureAwait(false);
        await Connection.CreateCommand(heartbeatCommandText).ExecuteAsync().ConfigureAwait(false);
        await Connection.CreateCommand("updates add 1 1 $info").ExecuteAsync().ConfigureAwait(false);
        await Connection.CreateCommand("updates add 2 1 $(options -a)").ExecuteAsync().ConfigureAwait(false);
        await Connection.CreateCommand("updates add 3 1 $slot-info").ExecuteAsync().ConfigureAwait(false);
        // get an initial queue reading
        await Connection.CreateCommand("queue-info").ExecuteAsync().ConfigureAwait(false);
    }

    private async Task ReadMessagesFromConnection()
    {
        var reader = Connection.CreateReader();
        try
        {
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                await OnMessageRead(reader.Message).ConfigureAwait(false);
            }
        }
        catch (ObjectDisposedException ex)
        {
            Logger.Debug(String.Format(Logging.Logger.NameFormat, Settings.Name, ex.Message), ex);
        }
        catch (Exception ex)
        {
            Logger.Error(String.Format(Logging.Logger.NameFormat, Settings.Name, ex.Message), ex);
        }
    }

    protected override async Task OnRetrieve()
    {
        if (Messages.IsHeartbeatOverdue())
        {
            Close();
        }

        await Process().ConfigureAwait(false);
    }

    private async Task Process()
    {
        var sw = Stopwatch.StartNew();

        var workUnitsBuilder = new WorkUnitCollectionBuilder(Logger, Settings, Messages, LastRetrieveTime);
        var workUnitQueueBuilder = new WorkUnitQueueItemCollectionBuilder(
            Messages.UnitCollection, Messages.Info?.System);

        var statResult = TryRetrieveNvidiaSmiStatistics(out var gpuStats);
        foreach (var clientData in ClientDataCollection.Cast<FahClientData>())
        {
            var previousWorkUnitModel = clientData.WorkUnitModel;
            var workUnits = workUnitsBuilder.BuildForSlot(clientData.SlotID, clientData.Description, previousWorkUnitModel.WorkUnit);
            var workUnitModels = new WorkUnitModelCollection(workUnits.Select(x => BuildWorkUnitModel(clientData, x)));

            await PopulateClientData(clientData, workUnits, workUnitModels, workUnitQueueBuilder).ConfigureAwait(false);
            foreach (var m in workUnitModels)
            {
                await UpdateWorkUnitRepository(m).ConfigureAwait(false);
            }

            clientData.WorkUnitModel.ShowProductionTrace(Logger, clientData.Name, clientData.Status,
                Preferences.Get<PPDCalculation>(Preference.PPDCalculation),
                Preferences.Get<BonusCalculation>(Preference.BonusCalculation));

            string statusMessage = String.Format(CultureInfo.CurrentCulture, "Slot Status: {0}", clientData.Status);
            Logger.Info(String.Format(Logging.Logger.NameFormat, clientData.Name, statusMessage));

            if (statResult == NvidiaSmiQueryStatus.Success &&
                gpuStats != null &&
                clientData.SlotType == SlotType.GPU &&
                clientData.GPUBus.HasValue &&
                gpuStats.TryGetValue(clientData.GPUBus.Value, out var stat))
            {
                // NOTE: Convert from percent to ratio in order to use the Percent number format for display purposes
                clientData.GPUFanSpeed = stat.FanSpeed_Pct / 100.0d;
                clientData.GPUCoreTemp_C = stat.GPUTemp_C;
                clientData.GPUPcieVersionCurrent = stat.PcieVersionCurrent;
                clientData.GPUPcieLanesCurrent = stat.PcieLanesCurrent;
                clientData.GPUPcieVersionMax = stat.PcieVersionMax;
                clientData.GPUPcieLanesMax = stat.PcieLanesMax;
                clientData.GPUPowerState = stat.PowerState;
                clientData.GPUPowerDrawCurrent_Watts = stat.CurrentPower_Watt;
                clientData.GPUPowerLimitCurrent_Watts = stat.PowerLimitCurrent_Watt;
                clientData.GPUPowerLimitDefault_Watts = stat.PowerLimitDefault_Watt;
                clientData.GPUGraphicsClock_MHz = stat.GraphicsClock_MHz;
                clientData.GPUMemoryClock_MHz = stat.MemoryClock_MHz;
            }
            else
            {
                clientData.GPUFanSpeed = null;
                clientData.GPUCoreTemp_C = null;
                clientData.GPUPcieVersionCurrent = PcieVersionType.Unknown;
                clientData.GPUPcieLanesCurrent = PcieLanesType.Unknown;
                clientData.GPUPcieVersionMax = PcieVersionType.Unknown;
                clientData.GPUPcieLanesMax = PcieLanesType.Unknown;
                clientData.GPUPowerState = null;
                clientData.GPUPowerDrawCurrent_Watts = null;
                clientData.GPUPowerLimitCurrent_Watts = null;
                clientData.GPUPowerLimitDefault_Watts = null;
                clientData.GPUGraphicsClock_MHz = null;
                clientData.GPUMemoryClock_MHz = null;
            }
        }

        string message = String.Format(CultureInfo.CurrentCulture, "Retrieval finished: {0}", sw.GetExecTime());
        Logger.Info(String.Format(Logging.Logger.NameFormat, Settings.Name, message));
    }

    private IReadOnlyCollection<LogLine> EnumerateLogLines(int slotID, WorkUnitCollection workUnits)
    {
        IEnumerable<LogLine> logLines = workUnits.Current?.LogLines;

        if (logLines is null)
        {
            var slotRun = Messages.GetSlotRun(slotID);
            if (slotRun != null)
            {
                logLines = LogLineEnumerable.Create(slotRun);
            }
        }

        if (logLines is null)
        {
            var clientRun = Messages.ClientRun;
            if (clientRun != null)
            {
                logLines = LogLineEnumerable.Create(clientRun);
            }
        }

        return logLines is null ? Array.Empty<LogLine>() : logLines.ToList();
    }

    private WorkUnitModel BuildWorkUnitModel(IClientData clientData, WorkUnit workUnit)
    {
        Debug.Assert(clientData != null);
        Debug.Assert(workUnit != null);

        var protein = ProteinService?.GetOrRefresh(workUnit.ProjectID) ?? new Protein();
        return new WorkUnitModel(clientData, workUnit, Benchmarks)
        {
            CurrentProtein = protein
        };
    }

    private async Task PopulateClientData(FahClientData clientData,
                                          WorkUnitCollection workUnits,
                                          WorkUnitModelCollection workUnitModels,
                                          WorkUnitQueueItemCollectionBuilder workUnitQueueBuilder)
    {
        Debug.Assert(clientData != null);
        Debug.Assert(workUnits != null);
        Debug.Assert(workUnitModels != null);
        Debug.Assert(workUnitQueueBuilder != null);

        if (clientData.SlotType == SlotType.CPU)
        {
            clientData.Description.Processor = Messages.Info?.System?.CPU;
        }
        clientData.WorkUnitQueue = workUnitQueueBuilder.BuildForSlot(clientData.SlotID);
        clientData.CurrentLogLines = EnumerateLogLines(clientData.SlotID, workUnits);

        if (WorkUnitRepository is not null && Messages.ClientRun is not null)
        {
            var r = WorkUnitRepository;
            var slotIdentifier = clientData.SlotIdentifier;
            var clientStartTime = Messages.ClientRun.Data.StartTime;

            clientData.TotalRunCompletedUnits = (int)await r.CountCompletedAsync(slotIdentifier, clientStartTime).ConfigureAwait(false);
            clientData.TotalCompletedUnits = (int)await r.CountCompletedAsync(slotIdentifier, null).ConfigureAwait(false);
            clientData.TotalRunFailedUnits = (int)await r.CountFailedAsync(slotIdentifier, clientStartTime).ConfigureAwait(false);
            clientData.TotalFailedUnits = (int)await r.CountFailedAsync(slotIdentifier, null).ConfigureAwait(false);
        }

        // Update the WorkUnitModel if we have a current unit index
        if (workUnits.CurrentID != WorkUnitCollection.NoID && workUnitModels.ContainsID(workUnits.CurrentID))
        {
            clientData.WorkUnitModel = workUnitModels[workUnits.CurrentID];
            // Update the project details from the API so that we have the Project Cause available
            // NOTE: We don't actually care if it succeeded of failed, just that the refresh attempt happened
            await ProjectDetailsServiceBase.Default.TryGetWithRefreshAsync(clientData.WorkUnitModel.WorkUnit.ProjectID).ConfigureAwait(false);
        }
    }

    private async Task UpdateWorkUnitRepository(WorkUnitModel workUnitModel)
    {
        if (WorkUnitRepository is not null)
        {
            try
            {
                if (await WorkUnitRepository.UpdateAsync(workUnitModel).ConfigureAwait(false) > 0)
                {
                    if (Logger.IsDebugEnabled)
                    {
                        string message = $"Updated {workUnitModel.WorkUnit.ToProjectString()} in database.";
                        Logger.Debug(String.Format(Logging.Logger.NameFormat, workUnitModel.ClientData.SlotIdentifier.Name, message));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, ex);
            }
        }
    }

    public void Fold(int? slotID)
    {
        if (!Connected)
        {
            return;
        }
        string command = slotID.HasValue ? "unpause " + slotID.Value : "unpause";
        Connection.CreateCommand(command).Execute();
    }

    public void Pause(int? slotID)
    {
        if (!Connected)
        {
            return;
        }
        string command = slotID.HasValue ? "pause " + slotID.Value : "pause";
        Connection.CreateCommand(command).Execute();
    }

    public void Finish(int? slotID)
    {
        if (!Connected)
        {
            return;
        }
        string command = slotID.HasValue ? "finish " + slotID.Value : "finish";
        Connection.CreateCommand(command).Execute();
    }
}
