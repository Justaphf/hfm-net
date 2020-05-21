﻿
using harlam357.Windows.Forms;

namespace HFM.Forms
{
    public interface IViewFactory
    {
        IOpenFileDialogView GetOpenFileDialogView();

        ISaveFileDialogView GetSaveFileDialogView();

        IFolderBrowserView GetFolderBrowserView();

        IQueryView GetQueryDialog();

        IBenchmarksView GetBenchmarksForm();

        IPreferencesView GetPreferencesDialog();

        IProteinCalculatorView GetProteinCalculatorForm();

        void Release(object view);
    }
}
