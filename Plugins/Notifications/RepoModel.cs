using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notifications
{
    public class RepoModel
    {
        public enum RepoStateEnum { OnDate, Pending };

        public string Repo { get; set; }
        public RepoStateEnum State { get; set; }
    }
}
