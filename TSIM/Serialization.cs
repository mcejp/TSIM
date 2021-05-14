using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace TSIM {

public class Serialization {
    public static IDictionary<int, TrainControlStateSummary> DeserializeTrainControlState(byte[] bytes) {
        var dict = JsonSerializer.Deserialize<IDictionary<int, TrainControlStateSummary>>(bytes);
        Trace.Assert(dict != null);
        // Console.WriteLine(JsonSerializer.Serialize(dict));
        return dict;
    }

    public static byte[] SerializeTrainControlStateToJsonUtf8Bytes(IDictionary<int, TrainControlStateSummary> summaryMap) {
        return JsonSerializer.SerializeToUtf8Bytes(summaryMap);
    }
}

}
