using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
//using System.Text.Json.Serialization;

namespace TSIM {

public class Serialization {
    /*public class TrainControlStateSummaryJsonConverter : JsonConverter<TrainControlStateSummary>
    {
        public override TrainControlStateSummary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                throw new JsonException();
            }

            var summary = new TrainControlStateSummary();

            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject) {
                    return summary;
                }

                if (reader.TokenType != JsonTokenType.PropertyName) {
                    throw new JsonException();
                }

                string propertyName = reader.GetString();
                reader.Read();

                if (propertyName == "schedulerState") {
                    // summary.SchedulerState = JsonSerializer.Deserialize<string>(ref reader, options);
                    summary.SchedulerState = reader.GetString();
                }
                else if (propertyName == "segmentsToFollow") {
                    if (reader.TokenType != JsonTokenType.StartArray) {
                        throw new JsonException();
                    }

                    while (reader.Read()) {
                        if (reader.TokenType == JsonTokenType.EndArray) {
                            break;
                        }

                        if (reader.TokenType != JsonTokenType.StartObject) {
                            throw new JsonException();
                        }

                        var entry = "";

                        while (reader.Read()) {
                            if (reader.TokenType == JsonTokenType.EndObject) {
                                return summary;
                            }

                            if (reader.TokenType != JsonTokenType.PropertyName) {
                                throw new JsonException();
                            }

                            propertyName = reader.GetString();
                            reader.Read();

                            if (propertyName == "segmentId") {

                            }
                        }
                    }
                }
                else {
                    // FIXME: probably wrong! since we already called Read()
                    reader.Skip();
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, TrainControlStateSummary summary, JsonSerializerOptions options) {
            writer.WriteStartObject();

            writer.WriteString("schedulerState", summary.SchedulerState);

            writer.WritePropertyName("segmentsToFollow");
            if (summary.SegmentsToFollow == null) {
                writer.WriteNullValue();
            }
            else {
                writer.WriteStartArray();
                foreach (var segment in summary.SegmentsToFollow) {
                    writer.WriteStartObject();
                    writer.WriteNumber("segmentId", segment.SegmentId);
                    writer.WriteString("entryEp", segment.EntryEp.ToString);
                    writer.WriteNumber("goalT", segment.GoalT);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }*/

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
