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
using System.Linq;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.Syntax.Declarations {

    /// <summary>
    /// The <see cref="IScopedDeclaration"/> interface indicates a resources that
    /// can be scoped to a Lambda function environment.
    /// </summary>
    public interface IScopedDeclaration {

        //--- Properties ---
        string FullName { get; }
        LiteralExpression? Type { get; }
        LiteralExpression? Description { get; }
        SyntaxNodeCollection<LiteralExpression>? Scope { get; }
        IEnumerable<string>? ScopeValues => Scope?.Select(item => item.Value).ToList();
        bool HasSecretType { get; }
        AExpression? ReferenceExpression { get; }
        bool IsPublic => Scope?.Any(item => item.Value == "public") ?? false;
    }
}