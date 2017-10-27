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

        public FixedThreadPool(int workCount)
        {
            if (workCount < 1)
                throw new ArgumentOutOfRangeException(nameof(workCount), "Value must be more then zero.");

            _workCount = workCount;
            StartWorkers();
        }

        private void StartWorkers()
        {
            _workers = new Worker[_workCount];

            for(var i=0;i<_workers.Length;i++)
            {
                _workers[i] = new Worker(this);
                _workers[i].OnTaskDone += FixedThreadPool_OnTaskDone;
                _workers[i].OnTaskError += FixedThreadPool_OnTaskError;
            }
        }

        private void FixedThreadPool_OnTaskError(Worker sender, ITask task, Exception error)
        {
            PlanTasks();
        }

        private void FixedThreadPool_OnTaskDone(Worker sender)
        {
            PlanTasks();
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

            PlanTasks();

            return true;
        }

        public void Stop()
        {
            if (!_started) return;

            _started = false;
            foreach (var worker in _workers)
                worker.Stop();
        }

        protected void PlanTasks()
        {
            if (IsMainQueueFull()) return;

            if (TryPlanHighPriorityTask()) return;
            if (TryPlanNormalPriorityTask()) return;
            if (TryPlanLowPriorityTask()) return;
        }

        private bool IsMainQueueFull()
        {
            return _mainQueue.Count >= 2 * _workCount;
        }

        private bool TryPlanHighPriorityTask()
        {
            ITask task;

            if (_highQueue.TryDequeue(out task))
            {
                if (_hightPriorityStarted == HightPriorityLimit)
                {
                    if (_normalQueue.Count > 0)
                    {
                        ITask normalTask;
                        _normalQueue.TryDequeue(out normalTask);
                        _mainQueue.Enqueue(normalTask);
                        Interlocked.Exchange(ref _hightPriorityStarted, 0);
                    }
                }
                else
                    Interlocked.Increment(ref _hightPriorityStarted);

                _mainQueue.Enqueue(task);
                return true;
            }

            return false;
        }

        private bool TryPlanNormalPriorityTask()
        {
            ITask task;

            if (_normalQueue.TryDequeue(out task))
            {
                _mainQueue.Enqueue(task);
                return true;
            }

            return false;
        }

        private bool TryPlanLowPriorityTask()
        {
            ITask task;

            if (_mainQueue.Count == 0 &&
                _highQueue.Count == 0 &&
                _normalQueue.Count == 0 &&
                _lowQueue.TryDequeue(out task))
            {
                _mainQueue.Enqueue(task);
                return true;
            }

            return false;
        }

        public ITask GetNext()
        {
            ITask task;

            if (_mainQueue.TryDequeue(out task)) return task;

            return null;
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
