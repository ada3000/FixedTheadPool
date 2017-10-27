using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ThreadPool.State;

namespace ThreadPool.Lib
{
    class Worker
    {
        public event Action<Worker> OnTaskDone = (w) => { };
        public event Action<Worker, ITask, Exception> OnTaskError = (w, t, e) => { };

        private ITaskProvider _taskProvider;
        private Thread _worker;
        private bool _started = true;

        public Worker(ITaskProvider taskProvider)
        {
            _taskProvider = taskProvider;

            _worker = new Thread(DoWork);
            _worker.IsBackground = true;
            _worker.Start();
        }

        public void Stop()
        {
            if (!_started) return;

            _started = false;
            _worker = null;
        }

        private void DoWork()
        {
            while (_started)
            {
                ITask task = _taskProvider.GetNext();
                if (task == null)
                {
                    Thread.Sleep(10);
                    continue;
                }

                try
                {
                    task.Execute();
                    OnTaskDone(this);
                }
                catch (Exception ex)
                {
                    OnTaskError(this, task, ex);
                }
            }
        }
    }
}