using GitCommands;
using GitUI;
using GitUIPluginInterfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private IGitUICommands _CurrentGitUiCommands;

        private List<GitRepository> _Repos = new List<GitRepository>();


        public Notifier(IGitUICommands currentGitUiCommands)
        {
            _CurrentGitUiCommands = currentGitUiCommands;

            _NotifyIcon = new NotifyIcon();
            _NotifyIcon.BalloonTipClicked += _notifyIcon_BalloonTipClicked;
            _NotifyIcon.Visible = true;
        }

        private void OnMenu(object sender, EventArgs e)
        {
            if (sender is ToolStripItem mir && mir.Tag is GitRepository rm)
            {
                if (_CurrentGitUiCommands.GitModule.WorkingDir != rm.RepoPath)
                {
                    Process.Start(@"E:\tmp\CSharp\Git\gitextensions\GitExtensions\bin\Debug\GitExtensions.exe", $"browse {rm.RepoPath}");
                }
            }
        }

        private void _notifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
        }
        
        public void Init(List<GitRepository> repos)
        {
            _Repos = repos;

            UpdateIcon();
            UpdateMenu();
        }

        public void UpdateRepo(GitRepository repo)
        {
            foreach (ToolStripItem menu in _NotifyIcon.ContextMenuStrip.Items)
            {
                if (menu.Tag is GitRepository gr && gr.RepoPath == repo.RepoPath)
                {
                    menu.Image = CastStatusToIcon(repo.State).ToBitmap();
                    menu.Text = $"{repo.RepoPath} - {CastStatusToLabel(repo.State)}";
                }
            }

            UpdateIcon();
        }

        public void ShowNotif(string repo)
        {
            // Shows a notification with specified message and title
            _NotifyIcon.ShowBalloonTip(3000, "Repository updated", $"Repo {repo} has changed !", ToolTipIcon.Info);

        }

        private void UpdateIcon()
        {
            RepositoryStatus status = RepositoryStatus.UpToDate;

            if (_Repos.Where(r => r.State == RepositoryStatus.Error).Count() > 0)
            {
                status = RepositoryStatus.Error;
            }
            else if (_Repos.Where(r => r.State == RepositoryStatus.NeedUpdate_Modified).Count() > 0)
            {
                status = RepositoryStatus.NeedUpdate_Modified;
            }
            else if (_Repos.Where(r => r.State == RepositoryStatus.NeedUpdate).Count() > 0)
            {
                status = RepositoryStatus.NeedUpdate;
            }
            else if (_Repos.Where(r => r.State == RepositoryStatus.UpToDate_Modified).Count() > 0)
            {
                status = RepositoryStatus.UpToDate_Modified;
            }
            if (_Repos.Where(r => r.State == RepositoryStatus.Unknown).Count() > 0)
            {
                status = RepositoryStatus.Unknown;
            }

            _NotifyIcon.Icon = CastStatusToIcon(status);
        }

        private void UpdateMenu()
        {
            ContextMenuStrip menus = new ContextMenuStrip();
            
            foreach (var repo in _Repos)
            {
                var menu = menus.Items.Add($"{repo.RepoPath} - {CastStatusToLabel(repo.State)}");
                menu.Tag = repo;
                menu.Image = CastStatusToIcon(repo.State).ToBitmap();

                menu.Click += OnMenu;
            }
                
            //var clearItem = menus.Items.Add("Clear");
            //clearItem.Tag = "Clear";
            //clearItem.Click += OnMenu;

            _NotifyIcon.ContextMenuStrip = menus;
        }

        private Icon CastStatusToIcon(RepositoryStatus status)
        {
            switch (status)
            {
                case RepositoryStatus.Error:
                    return Notifications.Properties.Resources.git_extensions_logo_final_red;
                case RepositoryStatus.Unknown:
                    return Notifications.Properties.Resources.git_extensions_logo_final_lightblue;
                case RepositoryStatus.NeedUpdate:
                    return Notifications.Properties.Resources.git_extensions_logo_final_yellow;
                case RepositoryStatus.NeedUpdate_Modified:
                    return Notifications.Properties.Resources.git_extensions_logo_final_yellow_upd;
                case RepositoryStatus.UpToDate:
                    return Notifications.Properties.Resources.git_extensions_logo_final_green;
                case RepositoryStatus.UpToDate_Modified:
                    return Notifications.Properties.Resources.git_extensions_logo_final_green_upd;
                default:
                    return Notifications.Properties.Resources.git_extensions_logo_final_red;
            }
        }

        private string CastStatusToLabel(RepositoryStatus status)
        {
            switch (status)
            {
                case RepositoryStatus.NeedUpdate:
                    return "Need udpate";
                case RepositoryStatus.NeedUpdate_Modified:
                    return "Need udpate, modified locally";
                case RepositoryStatus.UpToDate:
                    return "Up to date";
                case RepositoryStatus.UpToDate_Modified:
                    return "Up to date, modified locally";
                default:
                    return status.ToString();
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
        }
    }
}
