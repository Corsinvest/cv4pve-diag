/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Corsinvest.ProxmoxVE.Diagnostic.Api;

/// <summary>
/// Reads the Context and Gravity of an ignore rule. A name ("Node", "Info") is matched exactly.
/// A number is the format of the files written by older versions, where 0 was the default value
/// and meant "any": it is read as null.
/// </summary>
internal sealed class DiagnosticIgnoreRuleEnumConverter<TEnum> : JsonConverter<TEnum?> where TEnum : struct, Enum
{
    public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            return Enum.TryParse<TEnum>(text, true, out var value) && Enum.IsDefined(value)
                    ? value
                    : throw new JsonException($"'{text}' is not a valid {typeof(TEnum).Name}. Values: {string.Join(", ", Enum.GetNames<TEnum>())}");
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var number))
        {
            return number == 0
                    ? null
                    : (TEnum)Enum.ToObject(typeof(TEnum), number);
        }

        throw new JsonException($"Invalid value for {typeof(TEnum).Name}");
    }

    public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
    {
        if (value == null) { writer.WriteNullValue(); }
        else { writer.WriteStringValue(value.Value.ToString()); }
    }
}
