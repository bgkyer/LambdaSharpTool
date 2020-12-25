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
using System.Linq;
using System.Reflection;
using Amazon.Lambda.Core;
using LambdaSharp.Exceptions;

namespace LambdaSharp.Serialization {

    public static class LambdaSerializerSettings {

        //--- Class Properties ---
        public static ILambdaJsonSerializer LambdaSharpSerializer { get; set; } = new LambdaSystemTextJsonSerializer();
        public static ILambdaJsonSerializer AssemblySerializer { get; set; }

        //--- Methods ---
        internal static void InitializeAssemblySerializer(Assembly assembly) {
            if(assembly is null) {
                throw new ArgumentNullException(nameof(assembly));
            }
            if(AssemblySerializer is null) {

                // instantiate the assembly serializer or default LambdaJsonSerializer
                var serializerAttribute = assembly
                    .GetCustomAttributes(typeof(LambdaSerializerAttribute), false)
                    .OfType<LambdaSerializerAttribute>()
                    .FirstOrDefault();
                AssemblySerializer = (serializerAttribute != null)
                    ? (ILambdaJsonSerializer)(Activator.CreateInstance(serializerAttribute.SerializerType) ?? throw new ShouldNeverHappenException())

                    // TODO (2020-12-25, bjorg): default to NativeSerializer when Newtonsoft.Json references have been removed
                    : new LambdaJsonSerializer();
            }
        }
    }
}