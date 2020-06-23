﻿
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using HFM.Preferences;

namespace HFM.Forms.Models
{
    public class PreferencesModel : INotifyPropertyChanged
    {
        public PreferencesModel(IPreferenceSet preferences, IAutoRun autoRunConfiguration)
        {
            Preferences = preferences;
            ScheduledTasksModel = new ScheduledTasksModel(preferences);
            StartupAndExternalModel = new StartupAndExternalModel(preferences, autoRunConfiguration);
            OptionsModel = new OptionsModel(preferences);
            ReportingModel = new ReportingModel(preferences);
            WebSettingsModel = new WebSettingsModel(preferences);
            WebVisualStylesModel = new WebVisualStylesModel(preferences);
        }

        public IPreferenceSet Preferences { get; }
        public ScheduledTasksModel ScheduledTasksModel { get; set; }
        public StartupAndExternalModel StartupAndExternalModel { get; set; }
        public OptionsModel OptionsModel { get; set; }
        public ReportingModel ReportingModel { get; set; }
        public WebSettingsModel WebSettingsModel { get; set; }
        public WebVisualStylesModel WebVisualStylesModel { get; set; }

        public string Error
        {
            get
            {
                return null;
            }
        }

        public bool HasError => !String.IsNullOrWhiteSpace(Error);

        public bool ValidateAcceptance()
        {
            OnPropertyChanged(String.Empty);
            return !HasError;
        }

        public void Update()
        {
            ScheduledTasksModel.Update(Preferences);
            StartupAndExternalModel.Update();
            OptionsModel.Update(Preferences);
            ReportingModel.Update(Preferences);
            WebSettingsModel.Update(Preferences);
            WebVisualStylesModel.Update(Preferences);
            Preferences.Save();
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
