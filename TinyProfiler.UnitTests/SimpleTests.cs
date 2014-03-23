using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace TinyProfiler.UnitTests
{
    [TestFixture]
    public class SimpleTests
    {
        private IProfilerFactory _factory;
        private readonly ILogger _logger = DebugLogger.Instance;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _factory = new ProfilerFactory(_logger);
        }

       

        [Test]
        public void BasicUsage()
        {
            using (var profiler = _factory.StartProfiling("BasicUsage"))
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
        public void OperationWith3Steps()
        {
            using (var profiler = _factory.StartProfiling("Operation"))
            {
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
        public void BasicUsage_Depth2()
        {
            using (var profiler = _factory.StartProfiling("BasicUsage"))
            {
                _logger.Info("Some long initial step...");
                Thread.Sleep(1000);

                for (int i = 1; i <= 3; i++)
                {
                    using (var step = profiler.StartStep("step{0}", i))
                    {
                        _logger.Info("Start of step {0}", i);
                        Thread.Sleep(150);
                        using (var innerStep = step.StartStep("inner step"))
                        {
                            _logger.Info("Inner step");
                            Thread.Sleep(30);
                        }
                        _logger.Info("End of step {0}", i);
                    }
                }
            }
        }
    }
}
