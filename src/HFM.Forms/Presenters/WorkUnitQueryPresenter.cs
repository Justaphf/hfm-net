﻿using System.Windows.Forms;

using HFM.Core.Data;
using HFM.Forms.Views;

namespace HFM.Forms.Presenters
{
    public class WorkUnitQueryPresenter : IDialogPresenter
    {
        public WorkUnitQuery Query { get; set; }

        public WorkUnitQueryPresenter(WorkUnitQuery query)
        {
            Query = query;
        }

        public IWin32Dialog Dialog { get; protected set; }

        public virtual DialogResult ShowDialog(IWin32Window owner)
        {
            Dialog = new WorkUnitQueryDialog(this);
            return Dialog.ShowDialog(owner);
        }

        public void Dispose()
        {
            Dialog?.Dispose();
        }

        public void OKClicked()
        {
            Dialog.DialogResult = DialogResult.OK;
            Dialog.Close();
        }

        public void CancelClicked()
        {
            Dialog.DialogResult = DialogResult.Cancel;
            Dialog.Close();
        }
    }
}
