using GitCommands;
using GitUI;
using GitUIPluginInterfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Notifications
{
    public class Notifier : IDisposable
    {
        private NotifyIcon _NotifyIcon = null;
        private IGitUICommands _GitUiCommands = null;

        private List<string> _reposUpdated = new List<string>();

        public Notifier(IGitUICommands gitUICommands)
        {
            _GitUiCommands = gitUICommands;

            _NotifyIcon = new NotifyIcon();
            _NotifyIcon.BalloonTipClicked += _notifyIcon_BalloonTipClicked;
            _NotifyIcon.Visible = true;

            UpdateIcon();
            UpdateMenu();
        }

        private void OnMenu(object sender, EventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag != null && mi.Tag.ToString() == "Clear")
            {
                _reposUpdated.Clear();

                UpdateMenu();
                UpdateIcon();
            }
        }

        private void _notifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
        }
        
        public void RepoUpdated(string repoPath)
        {
            if (!_reposUpdated.Contains(repoPath))
            {
                _reposUpdated.Add(repoPath);

                // Shows a notification with specified message and title
                _NotifyIcon.ShowBalloonTip(3000, "Repository updated", $"Repo {repoPath} has changed !", ToolTipIcon.Info);
            }

            UpdateIcon();
            UpdateMenu();
        }

        private void UpdateIcon()
        {
            if (_reposUpdated.Count > 0)
            {
                _NotifyIcon.Icon = Notifications.Properties.Resources.git_extensions_logo_final_red;
            }
            else
            {
                _NotifyIcon.Icon = Notifications.Properties.Resources.git_extensions_logo_final_green;
            }
        }

        private void UpdateMenu()
        {
            if (_reposUpdated.Count > 0)
            {
                List<MenuItem> menus = new List<MenuItem>();
                foreach (var repo in _reposUpdated)
                {
                    var menu = new MenuItem($"Repo {repo} has changed !", OnMenu)
                    {
                        Tag = repo
                    };
                    menus.Add(menu);
                }

                var menuClear = new MenuItem($"Clear", OnMenu)
                {
                    Tag = "Clear"
                };
                menus.Add(menuClear);


                _NotifyIcon.ContextMenu = new ContextMenu(menus.ToArray());
            }
            else
            {
                MenuItem[] menus = { new MenuItem("No change") };
                _NotifyIcon.ContextMenu = new ContextMenu(menus);
            }
        }

        public void Dispose()
        {
            if (_NotifyIcon != null)
            {
                _NotifyIcon.Visible = false;
                _NotifyIcon.BalloonTipClicked -= _notifyIcon_BalloonTipClicked;
                _NotifyIcon.Dispose();
                _NotifyIcon = null;
            }

            _GitUiCommands = null;
        }
    }
}
