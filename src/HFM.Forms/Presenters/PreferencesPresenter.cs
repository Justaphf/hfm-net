﻿
using System;
using System.Windows.Forms;

using HFM.Core.Logging;
using HFM.Forms.Models;

namespace HFM.Forms
{
    public class PreferencesPresenter : IDisposable
    {
        public ILogger Logger { get; }
        public PreferencesModel Model { get; }
        public MessageBoxPresenter MessageBox { get; }

        public PreferencesPresenter(ILogger logger, PreferencesModel model, MessageBoxPresenter messageBox)
        {
            Logger = logger ?? NullLogger.Instance;
            Model = model;
            MessageBox = messageBox ?? NullMessageBoxPresenter.Instance;
        }

        public IWin32Dialog Dialog { get; protected set; }

        public virtual DialogResult ShowDialog(IWin32Window owner)
        {
            Dialog = new PreferencesDialog(this);
            return Dialog.ShowDialog(owner);
        }

        public void OKClicked()
        {
            if (Model.ValidateAcceptance())
            {
                Model.Update();
                Dialog.DialogResult = DialogResult.OK;
                Dialog.Close();
            }
        }

        public void Dispose()
        {
            Dialog?.Dispose();
        }
    }
}
