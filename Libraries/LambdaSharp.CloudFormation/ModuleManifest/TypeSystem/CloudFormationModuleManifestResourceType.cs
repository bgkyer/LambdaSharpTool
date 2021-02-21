/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2021
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LambdaSharp.CloudFormation.TypeSystem;

namespace LambdaSharp.CloudFormation.ModuleManifest.TypeSystem {

    public class CloudFormationModuleManifestResourceType : IResourceType {

        //--- Types ---
        private class StringResourceProperty : IResourceProperty {

            //--- Constructors ---
            public StringResourceProperty(string attributeName, bool required) {
                Name = attributeName ?? throw new ArgumentNullException(nameof(attributeName));
                Required = required;
            }

            //--- Properties ---
            public string Name { get; }
            public ResourceCollectionType CollectionType => ResourceCollectionType.NoCollection;
            public ResourceItemType ItemType => ResourceItemType.String;
            public IResourceType ComplexType => throw new InvalidOperationException();
            public bool Required { get; }
        }

        //--- Fields ---
        private readonly ModuleManifest.CloudFormationModuleManifestResourceType _resourceType;
        private readonly Dictionary<string, IResourceProperty> _properties;
        private readonly Dictionary<string, IResourceAttribute> _attributes;

        //--- Constructors ---
        public CloudFormationModuleManifestResourceType(ModuleManifest.CloudFormationModuleManifestResourceType resourceType) {
            _resourceType = resourceType;
            _properties = new Dictionary<string, IResourceProperty>();
            foreach(var property in resourceType.Properties) {
                _properties[property.Name ?? throw new ArgumentException("missing property name")] = new CloudFormationModuleManifestResourceProperty(property);
            }
            _attributes = new Dictionary<string, IResourceAttribute>();
            foreach(var attribute in resourceType.Attributes) {
                _attributes[attribute.Name ?? throw new ArgumentException("missing attribute name")] = new ModuleManifestResourceAttribute(attribute);
            }
        }

        //--- Properties ---
        public string Name => _resourceType.Type ?? throw new InvalidOperationException();
        public string? Documentation => null;
        public IEnumerable<IResourceProperty> RequiredProperties => Properties.Where(property => property.Required);
        public IEnumerable<IResourceProperty> Properties => _properties.Values;
        public IEnumerable<IResourceAttribute> Attributes => _attributes.Values;

        //--- Methods ---
        public bool TryGetAttribute(string attributeName, [NotNullWhen(true)] out IResourceAttribute? attribute) => _attributes.TryGetValue(attributeName, out attribute);
        public bool TryGetProperty(string propertyName, [NotNullWhen(true)] out IResourceProperty? property) {

            // TODO: this doesn't feel right; seems like we're injecting the `ServiceToken` too early
            switch(propertyName) {
            case "ServiceToken":
                property = new StringResourceProperty(propertyName, required: true);
                return true;
            case "ResourceType":
                property = new StringResourceProperty(propertyName, required: false);
                return true;
            default:
                return _properties.TryGetValue(propertyName, out property);
            }
        }
    }
}