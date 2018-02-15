using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Forms;
using GitCommands;
using GitUIPluginInterfaces;
using ResourceManager;
using System.Linq;

namespace Notifications
{
    public class NotificationsPlugin : GitPluginBase, IGitPluginForRepository
    {
        public NotificationsPlugin()
        {
            Repos = new Dictionary<string, GitRepository>();

            SetNameAndDescription("Windows notifications");
            Translate();
        }

        public Dictionary<string, GitRepository> Repos { get; private set; }

        private IDisposable cancellationToken;
        private IGitUICommands currentGitUiCommands;
        private Notifier _Notifier;


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

            GitRepositoryManager.SetGitPath(@"C:\Program Files\Git\bin\git.exe");

            _Notifier = new Notifier(gitUiCommands);

            InitRepos();

            currentGitUiCommands = gitUiCommands;
            currentGitUiCommands.PostSettings += OnPostSettings;

            RecreateObservable();
        }

        private void InitRepos()
        {
            foreach (var gr in Repos.Values)
            {
                gr.OnStateChange -= GitRepository_OnStateChange;
            }

            Repos.Clear();

            string repos = Repositories.ValueOrDefault(Settings);
            foreach (string repo in repos.Split(';'))
            {
                var gr = new GitRepository(repo);

                gr.OnStateChange += GitRepository_OnStateChange;

                Repos.Add(repo, gr);
            }

            _Notifier.Init(Repos.Values.ToList());
        }

        private void GitRepository_OnStateChange(GitRepository sender, RepositoryStatus state)
        {
            _Notifier.UpdateRepo(sender);
            
            // Update UI if it's current repo
            if (currentGitUiCommands.GitModule.WorkingDir == sender.RepoPath)
            {
                currentGitUiCommands.RepoChangedNotifier.Notify();
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
                                  try
                                  {
                                      // Loop on each repository to check status
                                      foreach (var repo in Repos.Values)
                                      {
                                          repo.CheckStatus();
                                      }
                                  }
                                  catch (Exception e)
                                  {
                                      string msg = e.ToString();
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
