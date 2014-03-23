using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace TinyProfiler.UnitTests
{
    [TestFixture]
    public class StaticThreadFactoryTest
    {
        private IProfiler _profiler;
        private readonly ILogger _logger = DebugLogger.Instance;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _logger.Info("Test SetUp");
            _profiler = new StaticThreadProfilerFactory(_logger).StartProfiling("StaticThreadFactoryTest #{0}", Thread.CurrentThread.ManagedThreadId);
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            _logger.Info("Test TearDown");
            _profiler.Dispose();
        }

        [Test]
        public void BasicUsageWithNewFactory()
        {
            using (var profiler = new StaticThreadProfilerFactory(_logger).StartProfiling("BasicUsage"))
            {
                _logger.Info("Some long initial step...");
                Thread.Sleep(1000);

                for (int i = 1; i <= 3; i++)
                {
                    using (var step = profiler.StartStep("step{0}", i))
                    {
                        _logger.Info("Start of step {0}", i);
                        Thread.Sleep(150);
                        _logger.Info("End of step {0}", i);
                    }
                }
            }
        }

        [Test]
        public void BasicUsageWithGlobalProfiler()
        {
            using (var profiler = _profiler.StartStep("BasicUsage"))
            {
                _logger.Info("Some long initial step...");
                Thread.Sleep(1000);

                for (int i = 1; i <= 3; i++)
                {
                    using (var step = profiler.StartStep("step{0}", i))
                    {
                        _logger.Info("Start of step {0}", i);
                        Thread.Sleep(150);
                        _logger.Info("End of step {0}", i);
                    }
                }
            }
        }

        
     
    }
}
