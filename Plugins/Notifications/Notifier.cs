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

        private Dictionary<string, RepoModel> _repos = new Dictionary<string, RepoModel>();

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
            if (sender is ToolStripItem mi && mi.Tag != null && mi.Tag.ToString() == "Clear")
            {
                foreach(var repo in _repos.Values)
                {
                    repo.State = RepoModel.RepoStateEnum.OnDate;
                }

                UpdateMenu();
                UpdateIcon();
            }
        }

        private void _notifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
        }

        public void AddRepo(string repo)
        {
            if (!_repos.ContainsKey(repo))
            {
                RepoModel rm = new RepoModel() { Repo = repo };
                rm.State = RepoModel.RepoStateEnum.OnDate;
                _repos.Add(repo, rm);

                UpdateIcon();
                UpdateMenu();
            }
        }

        public void RepoUpdated(string repoPath)
        {
            if (_repos.ContainsKey(repoPath))
            {
                _repos[repoPath].State = RepoModel.RepoStateEnum.Pending;
                
                UpdateIcon();
                UpdateMenu();
            }
        }

        public void ShowNotif(string repo)
        {
            // Shows a notification with specified message and title
            _NotifyIcon.ShowBalloonTip(3000, "Repository updated", $"Repo {repo} has changed !", ToolTipIcon.Info);

        }

        private void UpdateIcon()
        {
            if (_repos.Values.Where(r => r.State == RepoModel.RepoStateEnum.Pending).Count() > 0)
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
            ContextMenuStrip menus = new ContextMenuStrip();
            
            foreach (var repo in _repos.Values)
            {
                var menu = menus.Items.Add(repo.Repo);
                menu.Tag = repo;
                if (repo.State == RepoModel.RepoStateEnum.OnDate)
                {
                    menu.Image = Notifications.Properties.Resources.git_extensions_logo_final_green.ToBitmap();
                }
                else
                {
                    menu.Image = Notifications.Properties.Resources.git_extensions_logo_final_red.ToBitmap();
                }
                menu.Click += OnMenu;
            }
                
            var clearItem = menus.Items.Add("Clear");
            clearItem.Tag = "Clear";
            clearItem.Click += OnMenu;

            _NotifyIcon.ContextMenuStrip = menus;
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
