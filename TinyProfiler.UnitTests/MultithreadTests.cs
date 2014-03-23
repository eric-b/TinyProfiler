using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace TinyProfiler.UnitTests
{
    [TestFixture]
    public class MultithreadTests
    {
        private IProfilerFactory _factory;
        private readonly ILogger _logger = DebugLogger.Instance;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _factory = new ProfilerFactory(_logger);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        public void Multithreads(int count)
        {
            var waitHandles = new WaitHandle[count];
            var threadPoolItemProfilers = new IProfiler[count];
            using (var profiler = new StaticThreadProfilerFactory(_logger).StartProfiling("Multithreads({0})", count))
            {
                for (int i = 0; i < count; i++)
                {
                    var signal = new ManualResetEvent(false);
                    waitHandles[i] = signal;
                    var tpItemProfiler = profiler.StartStep("Thread pool item {0}", i);
                    threadPoolItemProfilers[i] = tpItemProfiler;
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        using (var t = tpItemProfiler.StartStep("Thread {0}", Thread.CurrentThread.ManagedThreadId))
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                _logger.Debug("T{0} - Wait 1 second...", Thread.CurrentThread.ManagedThreadId);
                                Thread.Sleep(1000);
                            }
                        }
                        signal.Set();
                    });
                    ThreadPool.RegisterWaitForSingleObject(signal, (o, t) => ((IProfiler)o).Dispose(), tpItemProfiler, Timeout.Infinite, true);
                }
                WaitHandle.WaitAll(waitHandles);
            }
        }
    }
}
