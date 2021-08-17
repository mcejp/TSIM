using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

using PeterO.Cbor;

namespace TSIM {

public class Serialization {
    public static IDictionary<int, TrainControlStateSummary> DeserializeTrainControlState(byte[] bytes) {
        var dict = JsonSerializer.Deserialize<IDictionary<int, TrainControlStateSummary>>(bytes);
        Trace.Assert(dict != null);
        // Console.WriteLine(JsonSerializer.Serialize(dict));
        return dict;
    }

    public static byte[] GlueFullSimSnapshot(byte[] unitsSnapshot, byte[] trainControlSnapshot, CBORObject simInfoSnapshot) {
        // MEGA HACK FOR MVP
        var cbor = CBORObject.NewMap()
            .Add("units", unitsSnapshot)
            .Add("trainControl", trainControlSnapshot)
            .Add("simInfo", simInfoSnapshot)
            ;
        return cbor.EncodeToBytes();
    }

    public static byte[] SerializeTrainControlStateToJsonUtf8Bytes(IDictionary<int, TrainControlStateSummary> summaryMap) {
        return JsonSerializer.SerializeToUtf8Bytes(summaryMap);
    }

    public static (byte[] unitsSnapshot, byte[] trainControlSnapshot, CBORObject simInfoSnapshot) UnglueFullSimSnapshot(byte[] fullSimSnapshot) {
        // MEGA HACK FOR MVP
        var cbor = CBORObject.DecodeFromBytes(fullSimSnapshot);

        return (cbor["units"].ToObject<byte[]>(),
                cbor["trainControl"].ToObject<byte[]>(),
                cbor["simInfo"]
                );
    }

    // public static void DeserializeInfoSnapshot(Simulation sim, CBORObject simInfoSnapshot)
    // {
    //     sim.SimTimeElapsed = TimeSpan.FromSeconds(simInfoSnapshot["timeElapsed"].AsDouble());
    // }

    public static CBORObject MakeSimInfoSnapshot(Simulation sim, float? perfValue)
    {
        var simInfoSnapshot = CBORObject.NewMap()
            .Add("simTimeElapsedSec", sim.SimTimeElapsed.TotalSeconds)
            .Add("perf", perfValue)
            // .Add("timeStepSec", sim.)
            ;
        return simInfoSnapshot;
    }
}

}
