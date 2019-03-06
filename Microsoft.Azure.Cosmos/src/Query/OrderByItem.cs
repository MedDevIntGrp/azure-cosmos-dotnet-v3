﻿//-----------------------------------------------------------------------
// <copyright file="OrderByItem.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.Azure.Cosmos.Query
{
    using System;
    using Newtonsoft.Json;
    using Microsoft.Azure.Cosmos.CosmosElements;
    using Newtonsoft.Json.Linq;
    using System.Text;

    /// <summary>
    /// Used to represent an order by item for a cross partition ORDER BY query.
    /// </summary>
    /// <example>{"item": 5}</example>
    [JsonConverter(typeof(OrderByItemConverter))]
    internal struct OrderByItem
    {
        private const string ItemName = "item";
        private static readonly JsonSerializerSettings NoDateParseHandlingJsonSerializerSettings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None
        };

        private readonly CosmosObject cosmosObject;

        public OrderByItem(CosmosElement cosmosElement)
        {
            if (cosmosElement == null)
            {
                throw new ArgumentNullException($"{nameof(cosmosElement)} must not be null.");
            }

            if (!(cosmosElement is CosmosObject cosmosObject))
            {
                throw new ArgumentException($"{nameof(cosmosElement)} must not be an object.");
            }

            this.cosmosObject = cosmosObject;
        }

        public bool IsDefined
        {
            get
            {
                return this.cosmosObject.ContainsKey(ItemName);
            }
        }

        public object Item
        {
            get
            {
                if (!this.cosmosObject.TryGetValue(ItemName, out CosmosElement cosmosElement))
                {
                    throw new InvalidOperationException($"Underlying object does not have an 'item' field.");
                }

                return ToObject(cosmosElement);
            }
        }

        public CosmosElementType Type
        {
            get
            {
                if (!this.cosmosObject.TryGetValue(ItemName, out CosmosElement cosmosElement))
                {
                    throw new InvalidOperationException($"Underlying object does not have an 'item' field.");
                }

                return cosmosElement.Type;
            }
        }

        private static object ToObject(CosmosElement cosmosElement)
        {
            if (cosmosElement == null)
            {
                throw new ArgumentNullException($"{nameof(cosmosElement)} must not be null.");
            }

            object obj;
            switch (cosmosElement.Type)
            {
                case CosmosElementType.String:
                    obj = (cosmosElement as CosmosString).Value;
                    break;

                case CosmosElementType.Number:
                    obj = (cosmosElement as CosmosNumber).GetValueAsDouble();
                    break;

                case CosmosElementType.Boolean:
                    obj = (cosmosElement as CosmosBoolean).Value;
                    break;

                case CosmosElementType.Null:
                    obj = null;
                    break;

                default:
                    throw new ArgumentException($"Unknown {nameof(CosmosElementType)}: {cosmosElement.Type}");
            }

            return obj;
        }

        /// <summary>
        /// Custom converter to serialize and deserialize the payload.
        /// </summary>
        private sealed class OrderByItemConverter : JsonConverter
        {
            /// <summary>
            /// Gets whether or not the object can be converted.
            /// </summary>
            /// <param name="objectType">The type of the object.</param>
            /// <returns>Whether or not the object can be converted.</returns>
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(object);
            }

            /// <summary>
            /// Reads a payload from a json reader.
            /// </summary>
            /// <param name="reader">The reader.</param>
            /// <param name="objectType">The object type.</param>
            /// <param name="existingValue">The existing value.</param>
            /// <param name="serializer">The serialized</param>
            /// <returns>The deserialized JSON.</returns>
            public override object ReadJson(
                JsonReader reader, 
                Type objectType, 
                object existingValue, 
                JsonSerializer serializer)
            {
                JToken jToken = JToken.Load(reader);
                // TODO: In the future we can go from jToken to CosmosElement if we have the eager implemenation.
                CosmosElement cosmosElement = LazyCosmosElementFactory.CreateFromBuffer(Encoding.UTF8.GetBytes(jToken.ToString()));
                return new OrderByItem(cosmosElement);
            }

            /// <summary>
            /// Writes the json to a writer.
            /// </summary>
            /// <param name="writer">The writer to write to.</param>
            /// <param name="value">The value to serialize.</param>
            /// <param name="serializer">The serializer to use.</param>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                OrderByItem orderByItem = (OrderByItem)value;
                if (orderByItem.IsDefined)
                {
                    writer.WritePropertyName(ItemName);
                    writer.WriteValue(orderByItem.Item);
                }

                writer.WriteEndObject();
            }
        }
    }
}
