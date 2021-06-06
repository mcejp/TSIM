using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace TSIM
{
    public class LoggingManager : IDisposable, ISignalSink
    {
        private int _nextPin = 1;

        private readonly Dictionary<int, Stopwatch> _lastEmit = new Dictionary<int, Stopwatch>();

        private readonly CsvLog _log;

        public LoggingManager(string logPath)
        {
            _log = new CsvLog(logPath);
        }

        public void Dispose()
        {
            _log.Dispose();
        }

        public object GetEntityHandle(Type type, in int id)
        {
            return type.Name + "/" + id;
        }

        public int GetSignalPin(object eh, string pinName)
        {
            var pinId = _nextPin++;
            _log.DefineLogTarget(pinId, (string) eh + "/" + pinName);
            return pinId;
        }

        // Message signal pins don't have their string emissions de-duplicated
        public int GetMessageSignalPin(object eh, string pinName)
        {
            var pinId = _nextPin++;
            _log.DefineMessageLogTarget(pinId, (string) eh + "/" + pinName);
            return pinId;
        }

        public void Feed(in int logTarget, float value)
        {
            _log.Feed(logTarget, value);
        }

        public void Feed(in int logTarget, string text)
        {
            if (!_lastEmit.ContainsKey(logTarget) || _lastEmit[logTarget].ElapsedMilliseconds > 2000)
            {
                // Console.WriteLine($"{logTarget}: {text}");
                _lastEmit[logTarget] = Stopwatch.StartNew();
            }

            _log.Feed(logTarget, text);
        }

        public void FeedNullable(in int logTarget, float? value)
        {
            _log.FeedNullable(logTarget, value);
        }

        public void SetClassPolicy(Type type, ClassPolicy cp)
        {
        }

        public void SetSimulatedTime(double timeSeconds)
        {
            _log.SetSimulatedTime(timeSeconds);
        }

        public class ClassPolicy
        {
            public ClassPolicy(bool acceptByDefault, int[]? acceptId)
            {
            }
        }
    }

    // TODO: this shall be factored out so that the user can choose between logging to CSV, SQL, etc.
    internal class CsvLog : IDisposable
    {
        private double _simTime;

        private readonly StreamWriter _output;
        private readonly StringBuilder _sb = new StringBuilder();
//        private readonly Dictionary<int, string> _targetNames = new Dictionary<int, string>();
        private readonly Stopwatch _lastFlush = new Stopwatch();

        private readonly TimeSpan _flushPeriod = TimeSpan.FromSeconds(10);
        private const int FlushThreshold = 1024 * 1024;

        private const char Delimiter = ',';

        public CsvLog(string fileName)
        {
            _output = new StreamWriter(fileName);
            _output.WriteLine("time,target,value");

            _lastFlush.Restart();
        }

        public void DefineLogTarget(int logTarget, string name)
        {
            _sb.Append("DEF").Append(Delimiter).Append(logTarget).Append(Delimiter).Append(name).AppendLine();
//            _targetNames.Add(logTarget, name);
        }

        public void DefineMessageLogTarget(int logTarget, string name)
        {
            _sb.Append("DEFM").Append(Delimiter).Append(logTarget).Append(Delimiter).Append(name).AppendLine();
//            _targetNames.Add(logTarget, name);
        }

        public void Dispose()
        {
            Flush();
            _output.Dispose();
        }

        public void Feed(int logTarget, float value)
        {
            _sb.Append(_simTime).Append(Delimiter).Append(logTarget).Append(Delimiter).Append(value).AppendLine();
        }

        public void Feed(int logTarget, string value)
        {
            _sb.Append(_simTime); _sb.Append(Delimiter).Append(logTarget).Append(Delimiter).Append('"').Append(value).AppendLine("\"");
        }

        public void FeedNullable(int logTarget, float? value)
        {
            _sb.Append(_simTime).Append(Delimiter).Append(logTarget).Append(Delimiter).Append(value != null ? value.ToString() : "nan").AppendLine();
        }

        private void Flush()
        {
//            Console.WriteLine($"CsvLog: Flushing {_sb.Length} bytes");

            _output.Write(_sb.ToString());
            _output.Flush();
            _sb.Clear();
            _lastFlush.Restart();
        }

        public void SetSimulatedTime(double timeSeconds)
        {
            if (_sb.Length > FlushThreshold || _lastFlush.Elapsed >= _flushPeriod)
            {
                Flush();
            }

            _simTime = timeSeconds;
        }
    }
}
