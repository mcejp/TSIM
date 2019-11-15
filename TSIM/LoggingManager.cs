using System;

namespace TSIM
{
    public class LoggingManager : ISignalSink
    {
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
