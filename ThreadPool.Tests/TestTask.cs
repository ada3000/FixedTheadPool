using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ThreadPool.State;

namespace ThreadPool.Tests
{
    class TestTask : ITask
    {
        public int TimeoutMSec { get; private set; }
        public string Name { get; private set; }
        public Priority Priority { get; private set; }

        public Action<TestTask> OnStart = (t) => { };
        public Action<TestTask> OnDone = (t) => { };

        public bool IsStarted { get; private set; } = false;
        public bool IsStopped { get; private set; } = false;

        public TestTask(string name, Priority priority = Priority.Normal, int timeoutMSec = 1000)
        {
            Name = name;
            Priority = priority;
            TimeoutMSec = timeoutMSec;
        }

        public void Execute()
        {
            IsStarted = true;
            OnStart(this);

            Thread.Sleep(TimeoutMSec);

            IsStopped = true;
            OnDone(this);
        }

        public override string ToString()
        {
            return $"{Priority}-{Name}-{TimeoutMSec}";
        }
    }
}
