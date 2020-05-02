﻿
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

using HFM.Preferences.Properties;

namespace HFM.Preferences.Data
{
   internal static class MigrateFromUserSettings
   {
      internal static PreferenceData Execute()
      {
         // upgrade first
         Settings.Default.Upgrade();

         if (String.IsNullOrEmpty(Settings.Default.ApplicationVersion))
         {
            // no previous settings
            return null;
         }

         var data = new PreferenceData();
         data.ApplicationVersion = Settings.Default.ApplicationVersion;

         var location = new Point();
         var size = new Size();
         List<string> columns = null;
         GetFormStateValues(ref location, ref size, ref columns);
         data.MainWindow.Location = location;
         data.MainWindow.Size = size;
         data.MainWindowGrid.Columns = columns;
         data.MainWindowGrid.SortColumn = Settings.Default.FormSortColumn;
         data.MainWindowGrid.SortOrder = GetFormSortOrder();
         data.MainWindowState.SplitterLocation = Settings.Default.FormSplitLocation;
         data.MainWindowState.LogWindowHeight = Settings.Default.FormLogWindowHeight;
         data.MainWindowState.LogWindowVisible = Settings.Default.FormLogVisible;
         data.MainWindowState.QueueWindowVisible = Settings.Default.QueueViewerVisible;
         data.MainWindowProperties.MinimizeTo = GetFormShowStyle();
         data.MainWindowProperties.EnableStats = Settings.Default.ShowUserStats;
         data.MainWindowProperties.StatsType = Settings.Default.ShowTeamStats ? "Team" : null;
         data.MainWindowGridProperties.TimeFormatting = GetTimeStyle();
         data.MainWindowGridProperties.UnitTotals = GetCompletedCountDisplay();
         data.MainWindowGridProperties.DisplayVersions = Settings.Default.ShowVersions;
         data.MainWindowGridProperties.OfflineClientsLast = Settings.Default.OfflineLast;
         data.MainWindowGridProperties.DisplayEtaAsDate = Settings.Default.EtaDate;

         location = new Point();
         size = new Size();
         GetBenchmarksFormStateValues(ref location, ref size);
         data.BenchmarksWindow.Location = location;
         data.BenchmarksWindow.Size = size;
         data.BenchmarksGraphing.GraphColors = GetGraphColorsList();
         data.BenchmarksGraphing.GraphLayout = GetBenchmarksGraphLayoutType();
         data.BenchmarksGraphing.ClientsPerGraph = Settings.Default.BenchmarksClientsPerGraph;

         location = new Point();
         size = new Size();
         GetMessagesFormStateValues(ref location, ref size);
         data.MessagesWindow.Location = location;
         data.MessagesWindow.Size = size;

         data.WebGenerationTask.Enabled = Settings.Default.GenerateWeb;
         data.WebGenerationTask.Interval = ToInt32(Settings.Default.GenerateInterval);
         data.WebGenerationTask.AfterClientRetrieval = Settings.Default.WebGenAfterRefresh;
         data.WebDeployment.DeploymentType = GetWebGenType();
         data.WebDeployment.DeploymentRoot = Settings.Default.WebRoot;
         data.WebDeployment.FtpServer.Address = Settings.Default.WebGenServer;
         data.WebDeployment.FtpServer.Port = Settings.Default.WebGenPort;
         data.WebDeployment.FtpServer.Username = Settings.Default.WebGenUsername;
         data.WebDeployment.FtpServer.Password = Settings.Default.WebGenPassword;
         data.WebDeployment.CopyLog = Settings.Default.WebGenCopyFAHlog;
         data.WebDeployment.FtpMode = GetFtpMode();
         data.WebDeployment.CopyHtml = Settings.Default.WebGenCopyHtml;
         data.WebDeployment.CopyXml = Settings.Default.WebGenCopyXml;
         data.WebDeployment.LogSizeLimitEnabled = Settings.Default.WebGenLimitLogSize;
         data.WebDeployment.LogSizeLimitedTo = Settings.Default.WebGenLimitLogSizeLength;
         data.WebRendering.StyleSheet = Settings.Default.CSSFile;
         data.WebRendering.OverviewTransform = Settings.Default.WebOverview;
         data.WebRendering.SummaryTransform = Settings.Default.WebSummary;
         data.WebRendering.SlotTransform = Settings.Default.WebSlot;

         data.Startup.RunMinimized = Settings.Default.RunMinimized;
         data.Startup.CheckForUpdate = Settings.Default.StartupCheckForUpdate;
         data.Startup.DefaultConfigFileEnabled = Settings.Default.UseDefaultConfigFile;
         data.Startup.DefaultConfigFilePath = Settings.Default.DefaultConfigFile;

         data.LogWindowProperties.ApplyColor = Settings.Default.ColorLogFile;
         data.LogWindowProperties.FollowLog = Settings.Default.FollowLogFile;

         data.ApplicationSettings.CacheFolder = Settings.Default.CacheFolder;
         data.ApplicationSettings.AutoSaveConfig = Settings.Default.AutoSaveConfig;
         data.ApplicationSettings.DecimalPlaces = Settings.Default.DecimalPlaces;
         data.ApplicationSettings.MessageLevel = Settings.Default.MessageLevel;
         data.ApplicationSettings.ProjectDownloadUrl = Settings.Default.ProjectDownloadUrl;
         data.ApplicationSettings.PpdCalculation = GetPpdCalculation();
         data.ApplicationSettings.BonusCalculation = GetBonusCalculation();
         data.ApplicationSettings.LogFileViewer = Settings.Default.LogFileViewer;
         data.ApplicationSettings.FileExplorer = Settings.Default.FileExplorer;
         data.ApplicationSettings.DuplicateProjectCheck = Settings.Default.DuplicateProjectCheck;

         data.ClientRetrievalTask.Enabled = Settings.Default.SyncOnSchedule;
         data.ClientRetrievalTask.Interval = ToInt32(Settings.Default.SyncTimeMinutes);
         data.ClientRetrievalTask.ProcessingMode = Settings.Default.SyncOnLoad ? "Serial" : null;

         data.Email.Enabled = Settings.Default.EmailReportingEnabled;
         data.Email.SecureConnection = Settings.Default.EmailReportingServerSecure;
         data.Email.ToAddress = Settings.Default.EmailReportingToAddress;
         data.Email.FromAddress = Settings.Default.EmailReportingFromAddress;
         data.Email.SmtpServer.Address = Settings.Default.EmailReportingServerAddress;
         data.Email.SmtpServer.Port = Settings.Default.EmailReportingServerPort;
         data.Email.SmtpServer.Username = Settings.Default.EmailReportingServerUsername;
         data.Email.SmtpServer.Password = Settings.Default.EmailReportingServerPassword;

         // data.Reporting

         data.UserSettings.EocUserId = Settings.Default.EOCUserID;
         data.UserSettings.StanfordId = Settings.Default.StanfordID;
         data.UserSettings.TeamId = Settings.Default.TeamID;

         data.WebProxy.Enabled = Settings.Default.UseProxy;
         data.WebProxy.Server.Address = Settings.Default.ProxyServer;
         data.WebProxy.Server.Port = Settings.Default.ProxyPort;
         data.WebProxy.CredentialsEnabled = Settings.Default.UseProxyAuth;
         data.WebProxy.Server.Username = Settings.Default.ProxyUser;
         data.WebProxy.Server.Password = Settings.Default.ProxyPass;

         location = new Point();
         size = new Size();
         columns = null;
         GetHistoryFormStateValues(ref location, ref size, ref columns);
         data.HistoryWindow.Location = location;
         data.HistoryWindow.Size = size;
         data.HistoryWindowGrid.Columns = columns;
         data.HistoryWindowGrid.SortColumn = Settings.Default.HistorySortColumnName;
         data.HistoryWindowGrid.SortOrder = Settings.Default.HistorySortOrder;
         data.HistoryWindowProperties.BonusCalculation = GetHistoryWindowBonusCalculation();
         data.HistoryWindowProperties.MaximumResults = Settings.Default.ShowEntriesValue;

         return data;
      }

      private static int ToInt32(string value)
      {
         int result;
         if (Int32.TryParse(value, out result))
         {
            return result;
         }
         return 0;
      }

      private static void GetFormStateValues(ref Point location, ref Size size, ref List<string> columns)
      {
         try
         {
            location = Settings.Default.FormLocation;
            size = Settings.Default.FormSize;
            columns = new List<string>(Settings.Default.FormColumns.OfType<string>());
         }
         catch (NullReferenceException)
         { }
      }

      private static ListSortDirection GetFormSortOrder()
      {
         ListSortDirection order = ListSortDirection.Ascending;
         try
         {
            order = Settings.Default.FormSortOrder;
         }
         catch (NullReferenceException)
         { }

         return order;
      }

      private static string GetTimeStyle()
      {
         switch (Settings.Default.TimeStyle)
         {
            case "Formatted":
               return "Format1";
            default:
               return null;
         }
      }

      private static string GetCompletedCountDisplay()
      {
         switch (Settings.Default.CompletedCountDisplay)
         {
            case "ClientRunTotal":
               return "ClientStart";
            default:
               return null;
         }
      }

      private static string GetFormShowStyle()
      {
         switch (Settings.Default.FormShowStyle)
         {
            case "TaskBar":
            case "Both":
               return Settings.Default.FormShowStyle;
            default:
               return null;
         }
      }

      private static void GetBenchmarksFormStateValues(ref Point location, ref Size size)
      {
         try
         {
            location = Settings.Default.BenchmarksFormLocation;
            size = Settings.Default.BenchmarksFormSize;
         }
         catch (NullReferenceException)
         { }
      }

      private static List<Color> GetGraphColorsList()
      {
         var graphColors = new List<Color>();
         foreach (string color in Settings.Default.GraphColors)
         {
            Color realColor = Color.FromName(color);
            if (realColor.IsEmpty == false)
            {
               graphColors.Add(realColor);
            }
         }

         return graphColors;
      }

      private static string GetBenchmarksGraphLayoutType()
      {
         switch (Settings.Default.BenchmarksGraphLayoutType)
         {
            case "ClientsPerGraph":
               return Settings.Default.BenchmarksGraphLayoutType;
            default:
               return null;
         }
      }

      private static string GetWebGenType()
      {
         switch (Settings.Default.WebGenType)
         {
            case "Ftp":
               return Settings.Default.WebGenType;
            default:
               return null;
         }
      }

      private static void GetMessagesFormStateValues(ref Point location, ref Size size)
      {
         try
         {
            location = Settings.Default.MessagesFormLocation;
            size = Settings.Default.MessagesFormSize;
         }
         catch (NullReferenceException)
         { }
      }

      private static string GetFtpMode()
      {
         switch (Settings.Default.WebGenFtpMode)
         {
            case "Active":
               return Settings.Default.WebGenFtpMode;
            default:
               return null;
         }
      }

      private static string GetPpdCalculation()
      {
         switch (Settings.Default.PpdCalculation)
         {
            case "LastFrame":
            case "LastThreeFrames":
            case "AllFrames":
            case "EffectiveRate":
               return Settings.Default.PpdCalculation;
            default:
               return "LastThreeFrames";
         }
      }

      private static string GetBonusCalculation()
      {
         switch (Settings.Default.CalculateBonus)
         {
            case "FrameTime":
            case "None":
               return Settings.Default.CalculateBonus;
            default:
               return null;
         }
      }

      private static string GetHistoryWindowBonusCalculation()
      {
         switch (Settings.Default.HistoryProductionView)
         {
            case 1:
               return "FrameTime";
            case 2:
               return "None";
            default:
               return null;
         }
      }

      private static void GetHistoryFormStateValues(ref Point location, ref Size size, ref List<string> columns)
      {
         try
         {
            location = Settings.Default.HistoryFormLocation;
            size = Settings.Default.HistoryFormSize;
            columns = new List<string>(Settings.Default.HistoryFormColumns.OfType<string>());
         }
         catch (NullReferenceException)
         { }
      }
   }
}
