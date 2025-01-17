﻿using UID;
using Mosaik.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

//using MessagePack;

namespace Catalyst
{
    public class ImmutableDocument
    {
        public Language Language { get; set; }
        public string Value { get; set; }
        public TokenData[][] TokensData { get; set; }
        public long[] SpanBounds { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public UID128 UID { get; set; }
        public string[] Labels { get; set; }
        public Dictionary<long, EntityType[]> EntityData { get; set; }
        public Dictionary<long, Dictionary<string, string>> TokenMetadata { get; set; }

        public int Length { get { return Value.Length; } }

        private static long GetTokenIndex(int spanIndex, int tokenIndex)
        {
            return (long)spanIndex << 32 | (long)(uint)tokenIndex;
        }

        public ImmutableDocument(Language language, string value, TokenData[][] tokensData, long[] spanBounds, Dictionary<string, string> metadata, UID128 uID, string[] labels, Dictionary<long, EntityType[]> entityData, Dictionary<long, Dictionary<string, string>> tokenMetadata)
        {
            Language = language;
            Value = value;
            TokensData = tokensData;
            SpanBounds = spanBounds;
            Metadata = metadata;
            UID = uID;
            Labels = labels;
            EntityData = entityData;
            TokenMetadata = tokenMetadata;
        }

        public Document ToMutable()
        {
            return Document.FromImmutable(this);
        }

        public void WriteAsJson(JsonTextWriter jw)
        {
            jw.WriteStartObject();

            jw.WritePropertyName(nameof(Language)); jw.WriteValue(Languages.EnumToCode(Language));
            jw.WritePropertyName(nameof(Length)); jw.WriteValue(Length);
            jw.WritePropertyName(nameof(Value)); jw.WriteValue(Value);

            if (UID.IsNotNull())
            {
                jw.WritePropertyName(nameof(UID)); jw.WriteValue(UID.ToString());
            }

            if (Metadata is object && Metadata.Any())
            {
                jw.WritePropertyName(nameof(Metadata));
                jw.WriteStartObject();
                foreach (var kv in Metadata)
                {
                    jw.WritePropertyName(kv.Key); jw.WriteValue(kv.Value);
                }
                jw.WriteEndObject();
            }

            if (Labels is object && Labels.Length > 0)
            {
                jw.WritePropertyName(nameof(Labels));
                jw.WriteStartArray();
                foreach (var l in Labels)
                {
                    jw.WriteValue(l);
                }
                jw.WriteEndArray();
            }

            jw.WritePropertyName(nameof(TokensData));
            jw.WriteStartArray();
            for (int i = 0; i < TokensData.Length; i++)
            {
                var spanData = TokensData[i];
                jw.WriteStartArray();
                for (int j = 0; j < spanData.Length; j++)
                {
                    var tk = spanData[j];
                    long ix = GetTokenIndex(i, j);

                    jw.WriteStartObject();
                    jw.WritePropertyName(nameof(TokenData.Bounds));
                    jw.WriteStartArray();
                    jw.WriteValue(tk.LowerBound);
                    jw.WriteValue(tk.UpperBound);
                    jw.WriteEndArray();

                    if (tk.Tag != PartOfSpeech.NONE)
                    {
                        jw.WritePropertyName(nameof(TokenData.Tag)); jw.WriteValue(tk.Tag.ToString());
                    }

                    if (tk.Head >= 0)
                    {
                        jw.WritePropertyName(nameof(TokenData.Head)); jw.WriteValue(tk.Head);
                    }

                    if (tk.Frequency != 0)
                    {
                        jw.WritePropertyName(nameof(TokenData.Frequency)); jw.WriteValue(tk.Frequency);
                    }

                    if (!string.IsNullOrEmpty(tk.Replacement))
                    {
                        jw.WritePropertyName(nameof(TokenData.Replacement)); jw.WriteValue(tk.Replacement);
                    }

                    if (TokenMetadata is object)
                    {
                        if (TokenMetadata.TryGetValue(ix, out var tokenMetadata))
                        {
                            if (!(tokenMetadata is null) && tokenMetadata.Any())
                            {
                                jw.WritePropertyName(nameof(Metadata));
                                jw.WriteStartObject();
                                foreach (var kv in tokenMetadata)
                                {
                                    jw.WritePropertyName(kv.Key); jw.WriteValue(kv.Value);
                                }
                                jw.WriteEndObject();
                            }
                        }
                    }

                    if (EntityData is object)
                    {
                        if (EntityData.TryGetValue(ix, out var entities))
                        {
                            if (!(entities is null) && entities.Any())
                            {
                                jw.WritePropertyName(nameof(EntityType));
                                jw.WriteStartArray();
                                for (int k = 0; k < entities.Length; k++)
                                {
                                    jw.WriteStartObject();
                                    jw.WritePropertyName(nameof(EntityType.Type)); jw.WriteValue(entities[k].Type);
                                    jw.WritePropertyName(nameof(EntityType.Tag)); jw.WriteValue(entities[k].Tag.ToString());

                                    if (entities[k].TargetUID.IsNotNull())
                                    {
                                        jw.WritePropertyName(nameof(EntityType.TargetUID)); jw.WriteValue(entities[k].TargetUID.ToString());
                                    }

                                    if (!(entities[k].Metadata is null) && entities[k].Metadata.Any())
                                    {
                                        jw.WritePropertyName(nameof(EntityType.Metadata));
                                        jw.WriteStartObject();
                                        foreach (var kv in entities[k].Metadata)
                                        {
                                            jw.WritePropertyName(kv.Key); jw.WriteValue(kv.Value);
                                        }
                                        jw.WriteEndObject();
                                    }

                                    jw.WriteEndObject();
                                }
                                jw.WriteEndArray();
                            }
                        }
                    }
                    jw.WriteEndObject();
                }
                jw.WriteEndArray();
            }
            jw.WriteEndArray();

            jw.WriteEndObject();
        }
    }
}