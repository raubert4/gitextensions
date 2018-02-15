using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notifications
{
    public enum RepositoryStatus
    {
        Unknown = 3,
        Error = 2,
        NeedUpdate = 1,
        NeedUpdate_Modified = 5,
        UpToDate = 0,
        UpToDate_Modified = 4
    }
}
