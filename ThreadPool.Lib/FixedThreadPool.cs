using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ThreadPool.State;

namespace ThreadPool.Lib
{
    public class FixedThreadPool : IThreadPool, ITaskProvider, IDisposable
    {
        private const int HightPriorityLimit = 3;

        private int _workCount;
        private bool _started = true;

        private int _hightPriorityStarted = 0;

        private ConcurrentQueue<ITask> _mainQueue = new ConcurrentQueue<ITask>();

        private ConcurrentQueue<ITask> _highQueue = new ConcurrentQueue<ITask>();
        private ConcurrentQueue<ITask> _normalQueue = new ConcurrentQueue<ITask>();
        private ConcurrentQueue<ITask> _lowQueue = new ConcurrentQueue<ITask>();

        private Worker[] _workers;
        private Thread _taskPlanner;

        public FixedThreadPool(int workCount)
        {
            if (workCount < 1)
                throw new ArgumentOutOfRangeException(nameof(workCount), "Value must be more then zero.");

            _workCount = workCount;

            _taskPlanner = new Thread(PlanTasks);
            _taskPlanner.IsBackground = true;
            _taskPlanner.Start();

            StartWorkers();
        }

        private void StartWorkers()
        {
            _workers = new Worker[_workCount];

            for (var i = 0; i < _workers.Length; i++)
            {
                _workers[i] = new Worker(this);
            }
        }

        public bool Execute(ITask task, State.Priority priority)
        {
            if (!_started)
                return false;

            switch (priority)
            {
                case Priority.Low: _lowQueue.Enqueue(task); break;
                case Priority.Normal: _normalQueue.Enqueue(task); break;
                case Priority.High: _highQueue.Enqueue(task); break;
            }

            return true;
        }

        public void Stop()
        {
            if (!_started) return;

            _taskPlanner.Abort();

            _started = false;
            foreach (var worker in _workers)
                worker.Stop();
        }

        protected void PlanTasks()
        {
            while (_started)
            {
                if (IsMainQueueFull() || NoTasks())
                {
                    Thread.Sleep(100);
                    continue;
                };

                if (TryPlanHighPriorityTask()) continue;
                if (TryPlanNormalPriorityTask()) continue;
                TryPlanLowPriorityTask();
            }
        }

        private bool NoTasks()
        {
            return _lowQueue.Count + _normalQueue.Count + _highQueue.Count == 0;
        }

        private bool IsMainQueueFull()
        {
            return _mainQueue.Count >= 2 * _workCount;
        }

        private bool TryPlanHighPriorityTask()
        {
            ITask task;

            if (!_highQueue.TryDequeue(out task)) return false;

            if (_hightPriorityStarted == HightPriorityLimit && _normalQueue.Count > 0)
            {
                ITask normalTask;
                _normalQueue.TryDequeue(out normalTask);
                _mainQueue.Enqueue(normalTask);
                _hightPriorityStarted = 0;
            }

            if (_hightPriorityStarted < HightPriorityLimit)
                _hightPriorityStarted++;

            _mainQueue.Enqueue(task);

            return true;
        }

        private bool TryPlanNormalPriorityTask()
        {
            ITask task;

            if (!_normalQueue.TryDequeue(out task)) return false;

            _mainQueue.Enqueue(task);
            return true;
        }

        private void TryPlanLowPriorityTask()
        {
            ITask task;

            if (_mainQueue.Count == 0 &&
                _highQueue.Count == 0 &&
                _normalQueue.Count == 0 &&
                _lowQueue.TryDequeue(out task))
            {
                _mainQueue.Enqueue(task);
            }
        }

        public ITask GetNext()
        {
            ITask task;

            return _mainQueue.TryDequeue(out task) ? task : null;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
