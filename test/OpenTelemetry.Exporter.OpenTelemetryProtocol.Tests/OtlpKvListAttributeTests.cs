// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation.Serializer;
using OtlpCommon = OpenTelemetry.Proto.Common.V1;

namespace OpenTelemetry.Exporter.OpenTelemetryProtocol.Tests;

public class OtlpKvListAttributeTests
{
    [Fact]
    public void EmptyKvList()
    {
        var kvList = new List<KeyValuePair<string, object?>>();
        var kvp = new KeyValuePair<string, object?>("key", kvList);

        Assert.True(TryTransformTag(kvp, out var attribute));
        Assert.Equal("key", attribute.Key);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.KvlistValue, attribute.Value.ValueCase);
        Assert.Empty(attribute.Value.KvlistValue.Values);
    }

    [Fact]
    public void KvListWithSingleStringEntry()
    {
        var kvList = new List<KeyValuePair<string, object?>>
        {
            new("innerKey", "innerValue"),
        };
        var kvp = new KeyValuePair<string, object?>("key", kvList);

        Assert.True(TryTransformTag(kvp, out var attribute));
        Assert.Equal("key", attribute.Key);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.KvlistValue, attribute.Value.ValueCase);

        var values = attribute.Value.KvlistValue.Values;
        Assert.Single(values);
        Assert.Equal("innerKey", values[0].Key);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.StringValue, values[0].Value.ValueCase);
        Assert.Equal("innerValue", values[0].Value.StringValue);
    }

    [Fact]
    public void KvListWithMixedValueTypes()
    {
        var kvList = new List<KeyValuePair<string, object?>>
        {
            new("stringKey", "stringValue"),
            new("intKey", 42L),
            new("boolKey", true),
            new("doubleKey", 3.14),
        };
        var kvp = new KeyValuePair<string, object?>("key", kvList);

        Assert.True(TryTransformTag(kvp, out var attribute));
        Assert.Equal("key", attribute.Key);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.KvlistValue, attribute.Value.ValueCase);

        var values = attribute.Value.KvlistValue.Values;
        Assert.Equal(kvList.Count, values.Count);

        Assert.Equal("stringKey", values[0].Key);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.StringValue, values[0].Value.ValueCase);
        Assert.Equal("stringValue", values[0].Value.StringValue);

        Assert.Equal("intKey", values[1].Key);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.IntValue, values[1].Value.ValueCase);
        Assert.Equal(42L, values[1].Value.IntValue);

        Assert.Equal("boolKey", values[2].Key);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.BoolValue, values[2].Value.ValueCase);
        Assert.True(values[2].Value.BoolValue);

        Assert.Equal("doubleKey", values[3].Key);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.DoubleValue, values[3].Value.ValueCase);
        Assert.Equal(3.14, values[3].Value.DoubleValue);
    }

    [Fact]
    public void KvListWithNullEntryValue()
    {
        var kvList = new List<KeyValuePair<string, object?>>
        {
            new("nullKey", null),
        };
        var kvp = new KeyValuePair<string, object?>("key", kvList);

        Assert.True(TryTransformTag(kvp, out var attribute));
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.KvlistValue, attribute.Value.ValueCase);

        var values = attribute.Value.KvlistValue.Values;
        Assert.Single(values);
        Assert.Equal("nullKey", values[0].Key);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.None, values[0].Value.ValueCase);
    }

    [Fact]
    public void NestedKvList()
    {
        var innerKvList = new List<KeyValuePair<string, object?>>
        {
            new("nestedKey", "nestedValue"),
        };
        var outerKvList = new List<KeyValuePair<string, object?>>
        {
            new("inner", innerKvList),
        };
        var kvp = new KeyValuePair<string, object?>("key", outerKvList);

        Assert.True(TryTransformTag(kvp, out var attribute));
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.KvlistValue, attribute.Value.ValueCase);

        var outerValues = attribute.Value.KvlistValue.Values;
        Assert.Single(outerValues);
        Assert.Equal("inner", outerValues[0].Key);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.KvlistValue, outerValues[0].Value.ValueCase);

        var innerValues = outerValues[0].Value.KvlistValue.Values;
        Assert.Single(innerValues);
        Assert.Equal("nestedKey", innerValues[0].Key);
        Assert.Equal("nestedValue", innerValues[0].Value.StringValue);
    }

    [Fact]
    public void KvListWithManyEntries()
    {
        var kvList = new List<KeyValuePair<string, object?>>();
        for (int i = 0; i < 50; i++)
        {
            kvList.Add(new($"key{i}", (long)i));
        }

        var kvp = new KeyValuePair<string, object?>("key", kvList);

        Assert.True(TryTransformTag(kvp, out var attribute));
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.KvlistValue, attribute.Value.ValueCase);

        var values = attribute.Value.KvlistValue.Values;
        Assert.Equal(50, values.Count);

        for (int i = 0; i < 50; i++)
        {
            Assert.Equal($"key{i}", values[i].Key);
            Assert.Equal((long)i, values[i].Value.IntValue);
        }
    }

    [Fact]
    public void DictionaryAsKvList()
    {
        var dict = new Dictionary<string, object?>
        {
            ["alpha"] = "a",
            ["beta"] = 2L,
        };
        var kvp = new KeyValuePair<string, object?>("key", dict);

        Assert.True(TryTransformTag(kvp, out var attribute));
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.KvlistValue, attribute.Value.ValueCase);
        Assert.Equal(2, attribute.Value.KvlistValue.Values.Count);
    }

    [Fact]
    public void KvListEnumerationFailureFallsBackToTruncated()
    {
        var kvp = new KeyValuePair<string, object?>("key", FaultyKvList());

        Assert.False(TryTransformTag(kvp, out var attribute));
        Assert.NotNull(attribute);
        Assert.Equal("key", attribute!.Key);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.StringValue, attribute.Value.ValueCase);
        Assert.Equal("TRUNCATED", attribute.Value.StringValue);
    }

    [Fact]
    public void KvListNestedEnumerationFailsAfterSomeEntriesStillFallsBackToTruncated()
    {
        var kvList = new List<KeyValuePair<string, object?>>
        {
            new("key", "value"),
            new("faulty", FaultyKvList()),
            new("intKey", 1),
        };

        var kvp = new KeyValuePair<string, object?>("list", kvList);

        Assert.True(TryTransformTag(kvp, out var attribute));
        Assert.NotNull(attribute);
        Assert.Equal("list", attribute.Key);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.KvlistValue, attribute.Value.ValueCase);

        var list = attribute.Value;
        Assert.NotNull(list.KvlistValue);

        Assert.Equal(kvList.Count, list.KvlistValue.Values.Count);

        Assert.Equal("key", list.KvlistValue.Values[0].Key);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.StringValue, list.KvlistValue.Values[0].Value.ValueCase);
        Assert.Equal("value", list.KvlistValue.Values[0].Value.StringValue);

        Assert.Equal("faulty", list.KvlistValue.Values[1].Key);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.StringValue, list.KvlistValue.Values[1].Value.ValueCase);
        Assert.Equal("TRUNCATED", list.KvlistValue.Values[1].Value.StringValue);

        Assert.Equal("intKey", list.KvlistValue.Values[2].Key);
        Assert.Equal(OtlpCommon.AnyValue.ValueOneofCase.IntValue, list.KvlistValue.Values[2].Value.ValueCase);
        Assert.Equal(1, list.KvlistValue.Values[2].Value.IntValue);

        Assert.Equal(1, 1);
    }

    private static bool TryTransformTag(KeyValuePair<string, object?> tag, [NotNullWhen(true)] out OtlpCommon.KeyValue? attribute)
    {
        ProtobufOtlpTagWriter.OtlpTagWriterState otlpTagWriterState = new ProtobufOtlpTagWriter.OtlpTagWriterState
        {
            Buffer = new byte[4096],
            WritePosition = 0,
        };

        if (ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, tag))
        {
            using var stream = new MemoryStream(otlpTagWriterState.Buffer, 0, otlpTagWriterState.WritePosition);
            var keyValue = OtlpCommon.KeyValue.Parser.ParseFrom(stream);
            Assert.NotNull(keyValue);
            attribute = keyValue;
            return true;
        }

        // On failure (e.g., TRUNCATED fallback), still try to deserialize what was written.
        if (otlpTagWriterState.WritePosition > 0)
        {
            using var stream = new MemoryStream(otlpTagWriterState.Buffer, 0, otlpTagWriterState.WritePosition);
            attribute = OtlpCommon.KeyValue.Parser.ParseFrom(stream);
            return false;
        }

        attribute = null;
        return false;
    }

    private static IEnumerable<KeyValuePair<string, object?>> FaultyKvList()
    {
        yield return new KeyValuePair<string, object?>("key1", "value1");
        throw new InvalidOperationException("simulated failure");
    }
}
