/*
 * LambdaSharp (λ#)
 * Copyright (C) 2018-2019
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

using System.Collections.Generic;
using LambdaSharp.Compiler.Syntax.Declarations;

namespace LambdaSharp.Compiler.Validators {

    internal sealed class MacroHandlerValidator : AValidator {

        //--- Constructors ---
        public MacroHandlerValidator(IModuleValidatorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Validate(ModuleDeclaration moduleDeclaration, Dictionary<string, AItemDeclaration> declarations) {
            moduleDeclaration.InspectType<MacroDeclaration>(node => {
                if(node.Handler == null) {

                    // TODO: error
                } else if(declarations.TryGetValue(node.Handler.Value, out var referencedDeclaration)) {
                    if(!(referencedDeclaration is FunctionDeclaration)) {
                        Logger.Log(Error.HandlerMustBeAFunction, node.Handler);
                    }
                } else {
                    Logger.Log(Error.ReferenceDoesNotExist(node.Handler.Value), node);
                    node.ParentItemDeclaration?.TrackMissingDependency(node.Handler.Value, node);
                }
            });
        }
    }
}