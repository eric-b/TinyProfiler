using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TinyProfiler.UnitTests
{
    /// <summary>
    /// <para>Implementation of <see cref="ILogger"/> based on <see cref="System.Diagnostics.Debug.WriteLine"/>.</para>
    /// </summary>
    public sealed class DebugLogger : ILogger
    {
        private static readonly ILogger _logger = new DebugLogger();

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static ILogger Instance
        {
            get
            {
                return _logger;
            }
        }

        void ILogger.Info(string messageFormat, params object[] args)
        {
            Debug.WriteLine(string.Format(messageFormat, args));
        }

        void ILogger.Debug(string messageFormat, params object[] args)
        {
            System.Diagnostics.Debug.WriteLine(string.Format(messageFormat, args));
        }
    }
}
