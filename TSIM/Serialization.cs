using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace TSIM {

public class Serialization {
    public static IDictionary<int, TrainControlStateSummary> DeserializeTrainControlState(byte[] bytes) {
        var dict = JsonSerializer.Deserialize<IDictionary<int, TrainControlStateSummary>>(bytes);
        Trace.Assert(dict != null);
        // Console.WriteLine(JsonSerializer.Serialize(dict));
        return dict;
    }

    public static byte[] GlueFullSimSnapshot(byte[] unitsSnapshot, byte[] trainControlSnapshot) {
        // MEGA HACK FOR MVP
        var a = Encoding.Default.GetString(unitsSnapshot).Replace('\n', ' ');
        var b = Encoding.Default.GetString(trainControlSnapshot).Replace('\n', ' ');

        return Encoding.Default.GetBytes(a + "\n" + b);
    }

    public static byte[] SerializeTrainControlStateToJsonUtf8Bytes(IDictionary<int, TrainControlStateSummary> summaryMap) {
        return JsonSerializer.SerializeToUtf8Bytes(summaryMap);
    }

    public static (byte[] unitsSnapshot, byte[] trainControlSnapshot) UnglueFullSimSnapshot(byte[] fullSimSnapshot) {
        // MEGA HACK FOR MVP
        var lst = Encoding.Default.GetString(fullSimSnapshot).Split('\n');
        var a = lst[0];
        var b = lst[1];

        return (Encoding.Default.GetBytes(a), Encoding.Default.GetBytes(b));
    }
}

}
