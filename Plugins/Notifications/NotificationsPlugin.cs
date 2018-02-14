using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Forms;
using GitCommands;
using GitUIPluginInterfaces;
using ResourceManager;

namespace Notifications
{
    public class NotificationsPlugin : GitPluginBase, IGitPluginForRepository
    {
        public NotificationsPlugin()
        {
            SetNameAndDescription("Windows notifications");
            Translate();
        }

        private IDisposable cancellationToken;
        private IGitUICommands currentGitUiCommands;
        private Notifier _Notifier;
        private List<string> _Repos = new List<string>();

        private NumberSetting<int> CheckUpdateInterval = new NumberSetting<int>("Check update every (seconds) - set to 0 to disable", 0);
        private BoolSetting ShowNotification = new BoolSetting("Show window desktop notification", false);
        private StringSetting Repositories = new StringSetting("Repositories to check separated by ;", "");

        public override IEnumerable<ISetting> GetSettings()
        {
            //return all settings or introduce implementation based on reflection on GitPluginBase level
            yield return CheckUpdateInterval;
            yield return ShowNotification;
            yield return Repositories;
        }

        public override void Register(IGitUICommands gitUiCommands)
        {
            base.Register(gitUiCommands);

            currentGitUiCommands = gitUiCommands;
            currentGitUiCommands.PostSettings += OnPostSettings;

            _Notifier = new Notifier(currentGitUiCommands);

            InitRepos();

            RecreateObservable();
        }

        private void InitRepos()
        {
            _Repos.Clear();

            string repos = Repositories.ValueOrDefault(Settings);
            foreach (string repo in repos.Split(';'))
            {
                _Repos.Add(repo);
                _Notifier.AddRepo(repo);
            }
        }

        private void OnPostSettings(object sender, GitUIPostActionEventArgs e)
        {
            RecreateObservable();
        }

        private void RecreateObservable()
        {
            CancelBackgroundOperation();

            int fetchInterval = CheckUpdateInterval.ValueOrDefault(Settings);

            var gitModule = currentGitUiCommands.GitModule;
            if (fetchInterval > 0 && gitModule.IsValidGitWorkingDir())
            {
                cancellationToken =
                    Observable.Timer(TimeSpan.FromSeconds(Math.Max(5, fetchInterval)))
                              .SelectMany(i =>
                              {
                                  // if git not runing - start fetch immediately
                                  if (!gitModule.IsRunningGitProcess())
                                      return Observable.Return(i);

                                  // in other case - every 5 seconds check if git still runnnig
                                  return Observable
                                      .Interval(TimeSpan.FromSeconds(5))
                                      .SkipWhile(ii => gitModule.IsRunningGitProcess())
                                      .FirstAsync()
                                  ;
                              })
                              .Repeat()
                              .ObserveOn(ThreadPoolScheduler.Instance)
                              .Subscribe(i =>
                              {
                                  var gitCmd = "fetch --all";

                                  // Loop on each repository
                                  foreach (string repo in _Repos)
                                  {
                                      // Create git module to fetch this repo
                                      GitModule module = new GitModule(repo);
                                      string res = module.RunGitCmd(gitCmd);

                                      // Conditions informing repo has changed
                                      if (res.Contains("From"))
                                      {
                                          _Notifier.RepoUpdated(repo);

                                          if (ShowNotification.ValueOrDefault(Settings))
                                          {
                                              _Notifier.ShowNotif(repo);
                                          }

                                          // Update UI if it's current repo
                                          if (gitModule.WorkingDir == module.WorkingDir)
                                          {
                                              currentGitUiCommands.RepoChangedNotifier.Notify();
                                          }
                                      }
                                  }
                              }
                        );
            }
        }

        private void CancelBackgroundOperation()
        {
            if (cancellationToken != null)
            {
                cancellationToken.Dispose();
                cancellationToken = null;
            }
        }

        public override void Unregister(IGitUICommands gitUiCommands)
        {
            CancelBackgroundOperation();

            if (currentGitUiCommands != null)
            {
                currentGitUiCommands.PostSettings -= OnPostSettings;
                currentGitUiCommands = null;
            }

            if (_Notifier != null)
            {
                _Notifier.Dispose();
            }

            base.Unregister(gitUiCommands);
        }

        public override bool Execute(GitUIBaseEventArgs gitUiArgs)
        {
            gitUiArgs.GitUICommands.StartSettingsDialog(this);
            return false;
        }
    }
}
