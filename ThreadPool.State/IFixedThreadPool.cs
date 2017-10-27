using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreadPool.State
{
    public interface IThreadPool
    {
        bool Execute(ITask task, Priority priority);
        void Stop();
    }

    public enum Priority
    {
        Low = 20,
        Normal = 0,        
        High = 10
    }
}
