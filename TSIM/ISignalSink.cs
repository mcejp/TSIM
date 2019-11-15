using System;

namespace TSIM
{
    public interface ISignalSink
    {
        object GetEntityHandle(Type type, in int id);
        int GetSignalPin(object eh, string pinName);

        void Feed(in int logTarget, float value);
        void FeedNullable(in int logTarget, float? value);
        void Feed(in int logTarget, string text);
    }
}
