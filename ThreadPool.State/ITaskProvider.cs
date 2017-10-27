using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ThreadPool.State;

namespace ThreadPool.State
{
    public interface ITaskProvider
    {
        ITask GetNext();
    }
}
