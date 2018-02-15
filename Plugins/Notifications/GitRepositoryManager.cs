using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Notifications
{
    public struct ExecuteResult
    {
        public Process process;
        public string processError;
        public string processOutput;
    }

    public class GitRepositoryManager
    {
        #region Variables

        public delegate void SvnErrorAddedHandler(string path, string error);
        public static event SvnErrorAddedHandler ErrorAdded;

        private static string _GitPath = string.Empty;

        private static Process backgroundProcess;

        #endregion

        #region Properties

        #endregion

        #region Constructor

        #endregion

        #region Methods

        public static void SetGitPath(string path)
        {
            _GitPath = path;
        }

        public static ExecuteResult ExecuteProcess(string workingPath, string arguments, bool waitForExit, bool lowPriority)
        {
            if (string.IsNullOrEmpty(_GitPath))
            {
                var resEr = new ExecuteResult() { processError = "Error - Git path not defined" };
                OnErrorAdded("", resEr.processError);
                return resEr;
            }
            if (!File.Exists(_GitPath))
            {
                var resEr = new ExecuteResult() { processError = "Error - Git path wrong" };
                OnErrorAdded(_GitPath, resEr.processError);
                return resEr;
            }

            SetEnvironmentVariable();
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = _GitPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.ASCII,
                WorkingDirectory = workingPath
            };

            ExecuteResult er = new ExecuteResult();
            er.process = Process.Start(psi);

            if (waitForExit) backgroundProcess = er.process;

            if (lowPriority)
            {
                try
                {
                    er.process.PriorityClass = ProcessPriorityClass.Idle;
                }
                catch	// Exception may occur if process finishing or already finished
                {
                }
            }

            if (waitForExit)
            {
                ArrayList lines = new ArrayList();
                string line;

                // Read output stream
                while ((line = er.process.StandardOutput.ReadLine()) != null)
                    lines.Add(line);

                er.processOutput = String.Join("\n", (string[])lines.ToArray(typeof(string)));
                lines.Clear();

                // Read error stream
                while ((line = er.process.StandardError.ReadLine()) != null)
                    lines.Add(line);

                er.processError = String.Join("\n", (string[])lines.ToArray(typeof(string)));
                lines.Clear();

                er.process.WaitForExit();

                if (er.process.ExitCode != 0 && er.processError.Length > 0)
                    OnErrorAdded(workingPath, er.processError);

                //if ((uint)er.process.ExitCode == 0xc0000142)		// STATUS_DLL_INIT_FAILED - Occurs when Windows shutdown in progress
                //{
                //    Application.Exit();

                //    if (Thread.CurrentThread == MainForm.statusThread)
                //        Thread.CurrentThread.Abort();
                //}

                backgroundProcess = null;
            }
            else
            {
                er.processOutput = "";
                er.processError = "";
            }

            return er;
        }

        private static void SetEnvironmentVariable()
        {
            Environment.SetEnvironmentVariable("HOME", Environment.GetEnvironmentVariable("USERPROFILE"));
            Environment.SetEnvironmentVariable("TERM", "msys");
        }

        #endregion

        #region Events

        public static void OnErrorAdded(string path, string error)
        {
            var handler = ErrorAdded;
            if (handler != null) handler(path, error);
        }

        #endregion
    }
}
