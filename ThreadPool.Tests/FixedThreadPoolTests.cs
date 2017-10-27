using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThreadPool.Lib;
using System.Threading;
using ThreadPool.State;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreadPool.Tests
{
    [TestClass]
    public class FixedThreadPoolTests
    {
        [TestMethod]
        public void TestStop()
        {
            var pool = new FixedThreadPool(5);

            TestTask task = new TestTask("1", State.Priority.Normal, 1000);

            bool addResult = pool.Execute(task, task.Priority);
            Assert.AreEqual(true, addResult);

            Thread.Sleep(2000);

            Assert.AreEqual(true, task.IsStarted);
            Assert.AreEqual(true, task.IsStopped);

            pool.Stop();
            addResult = pool.Execute(task, State.Priority.Normal);
            Assert.AreEqual(false, addResult);
        }

        [TestMethod]
        public void TestStartOrder()
        {
            int maxThreads = 3;
            string warmUp = new string('n', maxThreads * 2);

            string plannedModes = warmUp + "lhhnhhnl";
            string expectedModes = warmUp + "hhhnhnll";
            
            var pool = new FixedThreadPool(maxThreads);

            StringBuilder actualPriorites = new StringBuilder();

            for (int i = 0; i < plannedModes.Length; i++)
            {
                var task = new TestTask(i.ToString(), ToPriority(plannedModes[i]), 1000);
                task.OnStart = (t) => { lock (actualPriorites) actualPriorites.Append(t.Priority.ToString().ToLower()[0]); };

                pool.Execute(task, task.Priority);
            }

            while (expectedModes.Length > actualPriorites.Length)
                Thread.Sleep(100);

            var actualModesStr = actualPriorites.ToString();

            Assert.AreEqual(expectedModes, actualModesStr);
        }

        private Priority[] ToPriority(string value)
        {
            return value.Select(c => ToPriority(c)).ToArray();
        }

        private Priority ToPriority(char value)
        {
            switch (value)
            {
                case 'l': return Priority.Low;
                case 'n': return Priority.Normal;
                case 'h': return Priority.High;
            }

            throw new ArgumentOutOfRangeException();
        }
    }
}
