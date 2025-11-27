using Avalonia.Controls;
using Launcher.Views;
using System.Threading.Tasks;

namespace Launcher.Services
{
    public static class DialogManager
    {
        public static Task ShowMessageBox(Window owner, string title, string message)
        {
            return MessageBox.Show(owner, title, message);
        }

        public static Task<bool> ShowConfirmationDialog(Window owner, string title, string message)
        {
            return ConfirmationDialog.Show(owner, title, message);
        }

        public static Task<bool> ShowUpdateDialog(Window owner, string releaseName, string releaseNotes)
        {
            return UpdateDialog.Show(owner, releaseName, releaseNotes);
        }
    }
}
