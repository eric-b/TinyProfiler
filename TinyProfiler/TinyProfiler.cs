using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TinyProfiler
{
    #region Profiler API interfaces
    /// <summary>
    /// Profiler
    /// </summary>
    public interface IProfiler : IDisposable
    {
        /// <summary>
        /// Create a child profiler.
        /// </summary>
        /// <param name="name">Name of activity to profile.</param>
        /// <returns>Child profiler</returns>
        IProfiler StartStep(string name);

        /// <summary>
        /// Create a child profiler.
        /// </summary>
        /// <param name="nameFormat">Name of activity to profile.</param>
        /// <param name="args">Arguments (used with string.Format() and <paramref name="nameFormat"/>).</param>
        /// <returns>Child profiler</returns>
        IProfiler StartStep(string nameFormat, params object[] args);

        /// <summary>
        /// Discard this profiler.
        /// </summary>
        void Discard();
    }

    /// <summary>
    /// Root profiler factory.
    /// </summary>
    public interface IProfilerFactory
    {
        /// <summary>
        /// Create a root profiler.
        /// </summary>
        /// <param name="name">Name of activity to profile.</param>
        /// <returns>Root profiler</returns>
        IProfiler StartProfiling(string name);

        /// <summary>
        /// Create a root profiler.
        /// </summary>
        /// <param name="nameFormat">Name of activity to profile.</param>
        /// <param name="args">Arguments (used with string.Format() and <paramref name="nameFormat"/>).</param>
        /// <returns>Root profiler</returns>
        IProfiler StartProfiling(string nameFormat, params object[] args);
    }
    #endregion

    #region Optional implementation provided
    public interface ILogger
    {
        void Info(string messageFormat, params object[] args);

        void Debug(string messageFormat, params object[] args);
    }

    interface INotifyDisposed
    {
        void Disposed();
    }

    sealed class EmptyProfiler : IProfiler
    {
        private static readonly EmptyProfiler _instance = new EmptyProfiler();

        public static IProfiler Instance { get { return _instance; } }

        private EmptyProfiler()
        {

        }

        public IProfiler StartStep(string name)
        {
            return this;
        }

        public IProfiler StartStep(string nameFormat, params object[] args)
        {
            return this;
        }

        public void Discard()
        {

        }

        public void Dispose()
        {

        }
    }

    /// <summary>
    /// Default implementation of <see cref="IProfiler"/> 
    /// based on <see cref="Stopwatch"/> and <see cref="ILogger"/>.
    /// </summary>
    public class Profiler : IProfiler
    {
        #region Fields
        /// <summary>
        /// Null if child profiler.
        /// </summary>
        private readonly ILogger _logger = null;

        private static readonly char[] _trimLineBreak = { '\r', '\n', ' ' };

        private readonly string _name;

        private readonly List<Profiler> _children = new List<Profiler>(0);

        private readonly object _childrenSync = new object();

        /// <summary>
        /// Same instance between root and children.
        /// </summary>
        private readonly Stopwatch _watch;

        /// <summary>
        /// Only for child (start of parent _watch).
        /// </summary>
        private readonly long _watchCount = 0;

        /// <summary>
        /// Total elapsed time counted at dispose time (root and children).
        /// </summary>
        private long _watchStop;

        /// <summary>
        /// Object to notify when disposed. 
        /// Can be null.
        /// Always null if child profiler.
        /// </summary>
        private readonly INotifyDisposed _notifyDisposed = null;

        private volatile bool _disposed;
        #endregion

        #region Ctors
        /// <summary>
        /// Constructor for root profiler.
        /// </summary>
        /// <param name="logger"></param>
        internal Profiler(INotifyDisposed notifyDisposed, ILogger logger, string name)
        {
            _name = name;
            _logger = logger;
            _notifyDisposed = notifyDisposed;
            _watch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Constructor for child profiler.
        /// </summary>
        protected Profiler(string name, Stopwatch watch)
        {
            _name = name;
            _watch = watch;
            _watchCount = watch.ElapsedMilliseconds;
        }
        #endregion

        private void WriteTimes(System.IO.TextWriter writer, int depth)
        {
            const string format = "-{0} :\t{1} ms{2}";
            const string format2 = "-{0} :\t{1} ms (+{2})";
            if (depth == 1)
                writer.WriteLine(format.PadLeft(format.Length + depth * 2), _name, _watchStop, _watchCount != 0 ? " (+" + _watchCount + ")" : string.Empty);
            else
                writer.WriteLine(format2.PadLeft(format2.Length + depth * 2), _name, _watchStop, _watchCount);
            Profiler[] children;
            lock (_childrenSync)
            {
                children = _children.ToArray();
            }
            foreach (var child in children)
                child.WriteTimes(writer, depth + 1);
        }

        #region IProfiler
        IProfiler IProfiler.StartStep(string name)
        {
            if (_disposed)
            {
                if (_logger != null)
                    _logger.Debug("WARN: bad use of an instance of Profiler[{0}]: object disposed.", _name);
                return EmptyProfiler.Instance;
            }
            var child = new Profiler(name, _watch);
            lock (_childrenSync)
            {
                _children.Add(child);
            }
            return child;
        }

        IProfiler IProfiler.StartStep(string nameFormat, params object[] args)
        {
            if (_disposed)
            {
                if (_logger != null)
                    _logger.Debug("WARN: bad use of an instance of Profiler[{0}]: object disposed.", _name);
                return EmptyProfiler.Instance;
            }
            var child = new Profiler(string.Format(nameFormat, args), _watch);
            lock (_childrenSync)
            {
                _children.Add(child);
            }
            return child;
        }

        public void Discard()
        {
            if (_disposed)
                return;
            _disposed = true;
            lock (_childrenSync)
            {
                foreach (var c in _children)
                    c.Discard();
                _children.Clear();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_disposed)
                    return;
                _disposed = true;
                _watchStop = _watch.ElapsedMilliseconds - _watchCount;
                if (_logger != null)
                {
                    // Write times...
                    if (_notifyDisposed != null)
                        _notifyDisposed.Disposed();
                    Profiler[] children;
                    lock (_childrenSync)
                    {
                        children = _children.ToArray();
                    }
                    using (var writer = new System.IO.StringWriter())
                    {
                        writer.WriteLine("{0} :\t{1} ms", _name, _watchStop);
                        foreach (var child in children)
                            child.WriteTimes(writer, 1);

                        _watch.Stop();
                        var measureOutputTime = _watch.ElapsedMilliseconds - _watchStop;
                        if (measureOutputTime != 0)
                            writer.WriteLine("Measure overhead: {0} ms", measureOutputTime);

                        _logger.Info(writer.ToString().TrimEnd(_trimLineBreak));
                    }
                }
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }
        #endregion

    }

    /// <summary>
    /// <para>Default implementation of <see cref="IProfilerFactory"/> based on <see cref="Profiler"/> and <see cref="ILogger"/>.</para>
    /// </summary>
    public sealed class ProfilerFactory : IProfilerFactory
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Where to write measures.</param>
        public ProfilerFactory(ILogger logger)
        {
            if (logger == null)
                throw new ArgumentNullException("logger");
            _logger = logger;
        }

        public IProfiler StartProfiling(string name)
        {
            return new Profiler(null, _logger, name);
        }

        public IProfiler StartProfiling(string nameFormat, params object[] args)
        {
            return new Profiler(null, _logger, string.Format(nameFormat, args));
        }
    }

    /// <summary>
    /// <para>Default implementation of <see cref="IProfilerFactory"/> based on <see cref="Profiler"/> and <see cref="ILogger"/>,
    /// with a tweak with current thread: if the current thread has a root profiler attached to it,
    /// the factory will return it.</para>
    /// <para>When a new profiler is created, it is associated to the current thread.</para>
    /// </summary>
    public sealed class StaticThreadProfilerFactory : IProfilerFactory, INotifyDisposed
    {
        #region Fields
        private readonly ILogger _logger;

        [ThreadStatic]
        private static IProfiler _threadRootProfiler = null;

        [ThreadStatic]
        private static bool _threadRootProfilerSet = false;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Where to write measures.</param>
        public StaticThreadProfilerFactory(ILogger logger)
        {
            if (logger == null)
                throw new ArgumentNullException("logger");
            _logger = logger;
        }

        #region IProfilerFactory
        public IProfiler StartProfiling(string name)
        {
            if (_threadRootProfilerSet)
            {
                try
                {
                    return _threadRootProfiler.StartStep(name);
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex.ToString());
                }
            }

            _threadRootProfiler = new Profiler(this, _logger, name);
            _threadRootProfilerSet = true;
            return _threadRootProfiler;
        }

        public IProfiler StartProfiling(string nameFormat, params object[] args)
        {
            if (_threadRootProfilerSet)
            {
                try
                {
                    return _threadRootProfiler.StartStep(nameFormat, args);
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex.ToString());
                }
            }

            _threadRootProfiler = new Profiler(this, _logger, string.Format(nameFormat, args));
            _threadRootProfilerSet = true;
            return _threadRootProfiler;
        }
        #endregion

        void INotifyDisposed.Disposed()
        {
            _threadRootProfilerSet = false;
            _threadRootProfiler = null;
        }
    }
    #endregion
}
