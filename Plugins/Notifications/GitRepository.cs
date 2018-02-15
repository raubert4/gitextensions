using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Notifications
{
    public class GitRepository
    {
        #region Variables

        public delegate void StateChangeEvent(GitRepository sender, RepositoryStatus state);
        public event StateChangeEvent OnStateChange;

        private const string CST_RegexUpToDate = @"^\s=\s\[up to date\]\s.+?\s->\s.+$";
        private Regex regexUpToDate;

        #endregion

        #region Properties

        public string RepoPath { get; set; }
        public RepositoryStatus State { get; set; }

        #endregion

        #region Constructor

        public GitRepository(string repo)
        {
            regexUpToDate = new Regex(CST_RegexUpToDate, RegexOptions.Compiled);

            RepoPath = repo;

            CheckStatus();
        }

        #endregion

        #region Methods

        public void CheckStatus()
        {
            var s = GetStatus();

            if (State != s)
            {
                State = s;

                if (OnStateChange != null)
                {
                    OnStateChange(this, s);
                }
            }
        }

        private RepositoryStatus GetStatus()
        {
            string path = RepoPath;
            if (!Directory.Exists(path) && !File.Exists(path))
            {
                GitRepositoryManager.OnErrorAdded(path, "File or folder don't exist!");
                return RepositoryStatus.Error;
            }

            try
            {
                ExecuteResult er = GitRepositoryManager.ExecuteProcess(path, "fetch --all --dry-run -v", true, true);
                if (er.processError.Contains("Could not fetch"))
                {
                    return RepositoryStatus.Error;
                }
                if (er.processError.StartsWith("Error - "))
                {
                    return RepositoryStatus.Error;
                }

                bool needUpdate = this.IsNeedUpdate(er.processError);

                string arguments = String.Format("status -u \"{0}\"", path);
                er = GitRepositoryManager.ExecuteProcess(path, arguments, true, true);

                if (er.processOutput.Contains("have diverged"))
                {
                    return RepositoryStatus.NeedUpdate_Modified;
                }
                if (er.processOutput.Contains("branch is behind"))
                {
                    needUpdate = true;
                }

                if (er.processOutput.Contains("branch is ahead of") || er.processOutput.Contains("Changed but not updated") || er.processOutput.Contains("Changes not staged for commit")
                    || er.processOutput.Contains("Changes to be committed"))
                {
                    return needUpdate ? RepositoryStatus.NeedUpdate_Modified : RepositoryStatus.UpToDate_Modified;
                }
                else
                if (!IsModified(er.processOutput))
                {
                    return needUpdate ? RepositoryStatus.NeedUpdate : RepositoryStatus.UpToDate;
                }

                return RepositoryStatus.Unknown;
            }
            catch (Exception e)
            {
                GitRepositoryManager.OnErrorAdded(path, e.Message);
                return RepositoryStatus.Error;
            }
        }

        private bool IsNeedUpdate(string str)
        {
            using (var sr = new StringReader(str))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("From")) continue;
                    if (!regexUpToDate.IsMatch(line)) return true;
                }
            }
            return false;
        }

        private bool IsModified(string response)
        {
            return !(response.Contains("othing added to commit") || response.Contains("othing to commit"));
        }

        #endregion

        #region Events

        #endregion
    }
}
