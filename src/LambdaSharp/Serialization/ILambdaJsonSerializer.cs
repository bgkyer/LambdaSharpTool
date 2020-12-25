/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2020
 * lambdasharp.net
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;
using Amazon.Lambda.Core;

namespace LambdaSharp.Serialization {

    public interface ILambdaJsonSerializer : ILambdaSerializer {

        //--- Methods ---

        /// <summary>
        /// The <see cref="Deserialize(Stream, Type)"/> method deserializes the JSON object from a <c>Stream</c>.
        /// </summary>
        /// <param name="stream">Stream to deserialize.</param>
        /// <param name="type">The type to instantiate.</param>
        /// <returns>Deserialized instance.</returns>
        object Deserialize(Stream stream, Type type);

        /// <summary>
        /// The <see cref="Deserialize(String, Type)"/> method deserializes the JSON object from a <c>string</c>.
        /// </summary>
        /// <param name="json">String to deserialize.</param>
        /// <param name="type">The type to instantiate.</param>
        /// <returns>Deserialized instance.</returns>
        object Deserialize(string json, Type type) => Deserialize(json.ToStream(), type);
    }
}