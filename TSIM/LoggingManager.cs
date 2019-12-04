using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TSIM
{
    public class LoggingManager : ISignalSink
    {
        private readonly Dictionary<int, Stopwatch> _lastEmit = new Dictionary<int, Stopwatch>();

        public object GetEntityHandle(Type type, in int id)
        {
            return this;
        }

        public int GetSignalPin(object eh, string pinName)
        {
            return -1;
        }

        public void Feed(in int logTarget, float value)
        {
        }

        public void Feed(in int logTarget, string text)
        {
            if (!_lastEmit.ContainsKey(logTarget) || _lastEmit[logTarget].ElapsedMilliseconds > 2000)
            {
                Console.WriteLine($"{logTarget}: {text}");
                _lastEmit[logTarget] = Stopwatch.StartNew();
            }
        }

        public void FeedNullable(in int logTarget, float? value)
        {
        }

        public void SetClassPolicy(Type type, ClassPolicy cp)
        {
        }

        public class ClassPolicy
        {
            public ClassPolicy(bool acceptByDefault, int[]? acceptId)
            {
            }
        }
    }
}
