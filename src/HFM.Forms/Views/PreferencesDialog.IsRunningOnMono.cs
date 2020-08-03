﻿
using System.ComponentModel;

using HFM.Forms.Models;

namespace HFM.Forms
{
    public partial class PreferencesDialog
    {
        private void WebGenerationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Core.Application.IsRunningOnMono && Enabled)
            {
                HandleWebGenerationPropertyEnabledForMono(e.PropertyName);
                HandleWebGenerationPropertyChangedForMono(e.PropertyName);
            }
        }

        private void HandleWebGenerationPropertyEnabledForMono(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(WebGenerationModel.Enabled):
                    webGenerationOnScheduleRadioButton.Enabled = _presenter.Model.WebGenerationModel.Enabled;
                    webGenerationIntervalLabel.Enabled = _presenter.Model.WebGenerationModel.Enabled;
                    webGenerationAfterClientRetrievalRadioButton.Enabled = _presenter.Model.WebGenerationModel.Enabled;
                    webGenerationPathTextBox.Enabled = _presenter.Model.WebGenerationModel.Enabled;
                    webGenerationCopyHtmlCheckBox.Enabled = _presenter.Model.WebGenerationModel.Enabled;
                    webGenerationCopyXmlCheckBox.Enabled = _presenter.Model.WebGenerationModel.Enabled;
                    webGenerationCopyLogCheckBox.Enabled = _presenter.Model.WebGenerationModel.Enabled;
                    webGenerationTestConnectionLinkLabel.Enabled = _presenter.Model.WebGenerationModel.Enabled;
                    webDeploymentTypeRadioPanel.Enabled = _presenter.Model.WebGenerationModel.Enabled;
                    break;
                case nameof(WebGenerationModel.IntervalEnabled):
                    webGenerationIntervalTextBox.Enabled = _presenter.Model.WebGenerationModel.IntervalEnabled;
                    break;
                case nameof(WebGenerationModel.FtpModeEnabled):
                    webGenerationServerTextBox.Enabled = _presenter.Model.WebGenerationModel.FtpModeEnabled;
                    webGenerationServerLabel.Enabled = _presenter.Model.WebGenerationModel.FtpModeEnabled;
                    webGenerationPortTextBox.Enabled = _presenter.Model.WebGenerationModel.FtpModeEnabled;
                    webGenerationPortLabel.Enabled = _presenter.Model.WebGenerationModel.FtpModeEnabled;
                    webGenerationUsernameTextBox.Enabled = _presenter.Model.WebGenerationModel.FtpModeEnabled;
                    webGenerationUsernameLabel.Enabled = _presenter.Model.WebGenerationModel.FtpModeEnabled;
                    webGenerationPasswordTextBox.Enabled = _presenter.Model.WebGenerationModel.FtpModeEnabled;
                    webGenerationPasswordLabel.Enabled = _presenter.Model.WebGenerationModel.FtpModeEnabled;
                    webGenerationFtpModeRadioPanel.Enabled = _presenter.Model.WebGenerationModel.FtpModeEnabled;
                    break;
                case nameof(WebGenerationModel.BrowsePathEnabled):
                    webGenerationBrowsePathButton.Enabled = _presenter.Model.WebGenerationModel.BrowsePathEnabled;
                    break;
                case nameof(WebGenerationModel.LimitLogSizeEnabled):
                    webGenerationLimitLogSizeCheckBox.Enabled = _presenter.Model.WebGenerationModel.LimitLogSizeEnabled;
                    break;
                case nameof(WebGenerationModel.LimitLogSizeLengthEnabled):
                    webGenerationLimitLogSizeLengthUpDown.Enabled = _presenter.Model.WebGenerationModel.LimitLogSizeLengthEnabled;
                    break;
            }
        }

        private void HandleWebGenerationPropertyChangedForMono(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(WebGenerationModel.Path):
                    webGenerationPathTextBox.Text = _presenter.Model.WebGenerationModel.Path;
                    break;
            }
        }

        private void OptionsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Core.Application.IsRunningOnMono && Enabled)
            {
                HandleOptionsPropertyEnabledForMono(e.PropertyName);
                HandleOptionsPropertyChangedForMono(e.PropertyName);
            }
        }

        private void HandleOptionsPropertyEnabledForMono(string propertyName)
        {

        }

        private void HandleOptionsPropertyChangedForMono(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(OptionsModel.LogFileViewer):
                    optionsLogFileViewerTextBox.Text = _presenter.Model.OptionsModel.LogFileViewer;
                    break;
                case nameof(OptionsModel.FileExplorer):
                    optionsFileExplorerTextBox.Text = _presenter.Model.OptionsModel.FileExplorer;
                    break;
            }
        }

        private void ClientsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Core.Application.IsRunningOnMono && Enabled)
            {
                HandleClientsPropertyEnabledForMono(e.PropertyName);
                HandleClientsPropertyChangedForMono(e.PropertyName);
            }
        }

        private void HandleClientsPropertyEnabledForMono(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(ClientsModel.DefaultConfigFileEnabled):
                    clientsDefaultConfigFileTextBox.Enabled = _presenter.Model.ClientsModel.DefaultConfigFileEnabled;
                    clientsBrowseConfigFileButton.Enabled = _presenter.Model.ClientsModel.DefaultConfigFileEnabled;
                    break;
                case nameof(ClientsModel.RetrievalEnabled):
                    clientsRetrievalIntervalTextBox.Enabled = _presenter.Model.ClientsModel.RetrievalEnabled;
                    break;
            }
        }

        private void HandleClientsPropertyChangedForMono(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(ClientsModel.DefaultConfigFile):
                    clientsDefaultConfigFileTextBox.Text = _presenter.Model.ClientsModel.DefaultConfigFile;
                    break;
            }
        }

        private void ReportingPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Core.Application.IsRunningOnMono && Enabled)
            {
                HandleReportingPropertyEnabledForMono(e.PropertyName);
            }
        }

        private void HandleReportingPropertyEnabledForMono(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(ReportingModel.ReportingEnabled):
                    chkEmailSecure.Enabled = _presenter.Model.ReportingModel.ReportingEnabled;
                    SendTestEmailLinkLabel.Enabled = _presenter.Model.ReportingModel.ReportingEnabled;
                    txtToEmailAddress.Enabled = _presenter.Model.ReportingModel.ReportingEnabled;
                    txtFromEmailAddress.Enabled = _presenter.Model.ReportingModel.ReportingEnabled;
                    txtSmtpServer.Enabled = _presenter.Model.ReportingModel.ReportingEnabled;
                    txtSmtpServerPort.Enabled = _presenter.Model.ReportingModel.ReportingEnabled;
                    txtSmtpUsername.Enabled = _presenter.Model.ReportingModel.ReportingEnabled;
                    txtSmtpPassword.Enabled = _presenter.Model.ReportingModel.ReportingEnabled;
                    grpReportSelections.Enabled = _presenter.Model.ReportingModel.ReportingEnabled;
                    break;
            }
        }

        private void WebProxyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Core.Application.IsRunningOnMono && Enabled)
            {
                HandleWebProxyPropertyEnabledForMono(e.PropertyName);
            }
        }

        private void HandleWebProxyPropertyEnabledForMono(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(WebProxyModel.UseProxy):
                    txtProxyServer.Enabled = _presenter.Model.WebProxyModel.UseProxy;
                    txtProxyPort.Enabled = _presenter.Model.WebProxyModel.UseProxy;
                    chkUseProxyAuth.Enabled = _presenter.Model.WebProxyModel.UseProxy;
                    break;
                case nameof(WebProxyModel.ProxyAuthEnabled):
                    txtProxyUser.Enabled = _presenter.Model.WebProxyModel.ProxyAuthEnabled;
                    txtProxyPass.Enabled = _presenter.Model.WebProxyModel.ProxyAuthEnabled;
                    break;
            }
        }

        private void WebVisualStylesPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Core.Application.IsRunningOnMono && Enabled)
            {
                HandleWebVisualStylesPropertyChangedForMono(e.PropertyName);
            }
        }

        private void HandleWebVisualStylesPropertyChangedForMono(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(WebVisualStylesModel.WebOverview):
                    txtOverview.Text = _presenter.Model.WebVisualStylesModel.WebOverview;
                    break;
                case nameof(WebVisualStylesModel.WebSummary):
                    txtSummary.Text = _presenter.Model.WebVisualStylesModel.WebSummary;
                    break;
                case nameof(WebVisualStylesModel.WebSlot):
                    txtInstance.Text = _presenter.Model.WebVisualStylesModel.WebSlot;
                    break;
            }
        }
    }
}
