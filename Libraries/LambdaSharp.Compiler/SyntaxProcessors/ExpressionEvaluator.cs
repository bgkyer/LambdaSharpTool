﻿/*
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
using System.Linq;
using System.Text.RegularExpressions;
using LambdaSharp.Compiler.Syntax;
using LambdaSharp.Compiler.Syntax.Declarations;
using LambdaSharp.Compiler.Syntax.Expressions;

namespace LambdaSharp.Compiler.SyntaxProcessors {

    internal sealed class ExpressionEvaluator : ASyntaxProcessor {

        //--- Class Fields ---
        private static readonly Regex SubFormatStringParameterRegex = new Regex(@"\$\{(?!\!)[^\}]+\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex SubFormatStringEscapeRegex = new Regex(@"\$\{\![^\}]*\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        #region Errors/Warnings
        private static readonly Warning IfExpressionAlwaysTrue = new Warning(0, "!If expression is always True");
        private static readonly Warning IfExpressionAlwaysFalse = new Warning(0, "!If expression is always False");
        private static readonly Func<string, Warning> SubFormatParameterIsNotUsed = parameter => new Warning(0, $"parameter '{parameter}' in never used in !Sub format string");
        private static readonly Func<string, Error> SubFunctionParametersCannotUseAttributeNotation = parameter => new Error(0, $"cannot use attribute notation on local parameter '{parameter}'");
        #endregion

        //--- Constructors ---
        public ExpressionEvaluator(ISyntaxProcessorDependencyProvider provider) : base(provider) { }

        //--- Methods ---
        public void Normalize() {
            var substitutions = new Dictionary<ISyntaxNode, ISyntaxNode>();

            // normalize !Sub expressions by extracting embedded !Ref/!GetAtt expressions
            InspectType<SubFunctionExpression>(node => {

                // normalize the format string
                var newExpression = NormalizeSubFunctionExpression(node);
                if(!object.ReferenceEquals(node, newExpression)) {
                    substitutions[node] = newExpression;
                }
            });
            if(substitutions.Any()) {
                Substitute(node => {
                    if(substitutions.TryGetValue(node, out var newNode)) {
                        return newNode;
                    }
                    return node;
                });
            }
        }

        public void Evaluate() {
            var substitutions = new Dictionary<ISyntaxNode, ISyntaxNode>();
            while(true) {
                substitutions.Clear();

                // find expressions that can be substituted
                InspectType<AExpression>(node => Evaluate(node, substitutions));

                // stop when no more substitutions can be found
                if(!substitutions.Any()) {
                    break;
                }

                // apply substitions
                Substitute(node => {
                    if(substitutions.TryGetValue(node, out var newNode)) {
                        return newNode;
                    }
                    return node;
                });
            }

            // inline !Ref/!GetAtt into !Sub expression
            substitutions.Clear();
            InspectType<SubFunctionExpression>(node => {
                var newExpression = InlineSubFunctionParameters(node);
                if(!object.ReferenceEquals(node, newExpression)) {
                        substitutions[node] = newExpression;
                }
            });
            if(substitutions.Any()) {
                Substitute(node => {
                    if(substitutions.TryGetValue(node, out var newNode)) {
                        return newNode;
                    }
                    return node;
                });
            }
        }

        private void Evaluate(AExpression expression, Dictionary<ISyntaxNode, ISyntaxNode> substitutions) {

            // TODO: add missing expression types
            switch(expression) {
            case LiteralExpression _:

                // nothing to do
                break;
            case ConditionReferenceExpression conditionReferenceExpression:
                Substitute(EvaluateExpression(conditionReferenceExpression));
                break;
            case ReferenceFunctionExpression itemReferenceExpression:
                Substitute(EvaluateExpression(itemReferenceExpression));
                break;
            case IfFunctionExpression ifExpression:
                Substitute(EvaluateExpression(ifExpression));
                break;
            case ConditionAndExpression andExpression:
                Substitute(EvaluateExpression(andExpression));
                break;
            case ConditionOrExpression orExpression:
                Substitute(EvaluateExpression(orExpression));
                break;
            case ConditionIsDefinedExpression existsExpression:
                Substitute(EvaluateExpression(existsExpression));
                break;
            case ConditionEqualsExpression equalsExpression:
                Substitute(EvaluateExpression(equalsExpression));
                break;
            case ConditionNotExpression notExpression:
                Substitute(EvaluateExpression(notExpression));
                break;
            case JoinFunctionExpression joinExpression:
                Substitute(EvaluateExpression(joinExpression));
                break;
            case SelectFunctionExpression selectExpression:
                Substitute(EvaluateExpression(selectExpression));
                break;
            case SubFunctionExpression subExpression:
                Substitute(EvaluateExpression(subExpression));
                break;
            }

            // local functions
            void Substitute(AExpression newExpression) {
                if(!object.ReferenceEquals(expression, newExpression)) {
                        substitutions[expression] = newExpression;
                }
            }
        }

        private AExpression EvaluateExpression(ConditionReferenceExpression expression) {
            if(
                Provider.TryGetItem(expression.ReferenceName.Value, out var declaration)
                && (declaration is ConditionDeclaration conditionDeclaration)
                && (conditionDeclaration.Value is LiteralExpression conditionLiteral)
                && bool.TryParse(conditionLiteral.Value, out var conditionBool)
            ) {
                return BooleanLiteral(expression, conditionBool);
            }
            return expression;
        }

        private AExpression EvaluateExpression(ReferenceFunctionExpression expression) {

            // check if the reference is final and needs no further resolution
            if(expression.Final) {
                return expression;
            }

            // check if an alternative expression is defined for the reference
            if(Provider.TryGetReferenceExpression(expression.ReferenceName.Value, out var referenceExpression)) {

                // TODO: make sure we're not doing an infinite substitution!
                return referenceExpression;
            }
            return expression;
        }

        private AExpression EvaluateExpression(IfFunctionExpression expression) {

            // check if the condition is a constant
            if(
                (expression.Condition is LiteralExpression ifConditionLiteralExpression)
                && ifConditionLiteralExpression.IsBool
            ) {
                if(ifConditionLiteralExpression.AsBool() ?? false) {

                    // unless the condition literal expression was the result of the !IsDefined function, warn about it
                    if(!ifConditionLiteralExpression.FromExistsExpression) {
                        Logger.Log(IfExpressionAlwaysTrue, expression);
                    }
                    return expression.IfTrue;
                } else {

                    // unless the condition literal expression was the result of the !IsDefined function, warn about it
                    if(!ifConditionLiteralExpression.FromExistsExpression) {
                        Logger.Log(IfExpressionAlwaysFalse, expression);
                    }
                    return expression.IfFalse;
                }
            }
            return expression;
        }

        private AExpression EvaluateExpression(ConditionAndExpression expression) {

            // check if either branch is `false`
            if(
                (expression.LeftValue is LiteralExpression andLeftLiteralExpression)
                && (andLeftLiteralExpression.AsBool() == false)
            ) {
                return andLeftLiteralExpression;
            } else if(
                (expression.RightValue is LiteralExpression andRightLiteralExpression)
                && (andRightLiteralExpression.AsBool() == false)
            ) {
                return andRightLiteralExpression;
            }
            return expression;
        }

        private AExpression EvaluateExpression(ConditionOrExpression expression) {

            // check if either branch is `true`
            if(
                (expression.LeftValue is LiteralExpression orLeftLiteralExpression)
                && (orLeftLiteralExpression.AsBool() == true)
            ) {
                return orLeftLiteralExpression;
            } else if(
                (expression.RightValue is LiteralExpression orRightLiteralExpression)
                && (orRightLiteralExpression.AsBool() == true)
            ) {
                return orRightLiteralExpression;
            }
            return expression;
        }

        private AExpression EvaluateExpression(ConditionIsDefinedExpression expression) {

            // this expression becomes 'true' when an item declaration exists, otherwise 'false'
            return BooleanLiteral(expression, Provider.TryGetItem(expression.ReferenceName.Value, out var _), fromExistsExpression: true);
        }

        private AExpression EvaluateExpression(ConditionEqualsExpression expression) {

            // check if two literal expressions are being compared
            if(
                (expression.LeftValue is LiteralExpression equalsLeftLiteralExpression)
                && (expression.RightValue is LiteralExpression equalsRightLiteralExpression)
            ) {
                return BooleanLiteral(expression, equalsLeftLiteralExpression.Value == equalsRightLiteralExpression.Value);
            }
            return expression;
        }

        private AExpression EvaluateExpression(ConditionNotExpression expression) {

            // check if the literal expression can be negated
            if(
                (expression.Value is LiteralExpression notLiteralExpression)
                && notLiteralExpression.IsBool
            ) {
                return BooleanLiteral(expression, !(notLiteralExpression.AsBool() ?? false));
            }
            return expression;
        }

        private AExpression EvaluateExpression(JoinFunctionExpression expression) {

            // check if adjacent values can be concatenated
            if(
                (expression.Delimiter is LiteralExpression delimiterLiteral)
                && (expression.Values is ListExpression valuesList)
            ) {

                // check for trivial cases
                if(valuesList.Count == 0) {
                    return Fn.Literal("", expression.SourceLocation);
                } else if(valuesList.Count == 1) {
                    return valuesList[0];
                }

                // iterate over all items and concatenate adjacent literal expressions
                var processed = new List<AExpression>();
                foreach(var value in valuesList) {
                    if(
                        (value is LiteralExpression valueLiteral)
                        && processed.Any()
                        && (processed.Last() is LiteralExpression lastProcessedLiteral)
                    ) {

                        // replace previous item with concatenation
                        processed[processed.Count - 1] = Fn.Literal(lastProcessedLiteral.Value + delimiterLiteral.Value + valueLiteral.Value, lastProcessedLiteral.SourceLocation);
                    } else {

                        // append item to processed list
                        processed.Add(value);
                    }
                }

                // check if a new expression needs to be returned
                if(processed.Count != valuesList.Count) {
                    return new JoinFunctionExpression {
                        Delimiter = expression.Delimiter,
                        Values = new ListExpression(processed) {
                            SourceLocation = expression.Values.SourceLocation
                        },
                        SourceLocation = expression.SourceLocation
                    };
                }
            }
            return expression;
        }

        private AExpression EvaluateExpression(SelectFunctionExpression expression) {

            // check if item can be selected from list
            if(
                (expression.Index is LiteralExpression indexLiteral)
                && (expression.Values is ListExpression valuesList)
                && int.TryParse(indexLiteral.Value, out var index)
                && (index >= 0)
                && (index < valuesList.Count)
            ) {
                return valuesList[index];
            }
            return expression;
        }

        private AExpression EvaluateExpression(SubFunctionExpression expression) {

            // check if every parameter is used in the !Sub format string
            var parameterKeys = new HashSet<string>(expression.Parameters.Select(kv => kv.Key.Value));
            var parameterReferences = SubFormatStringParameterRegex.Matches(expression.FormatString.Value).Select(match => {
                var matchText = match.ToString();
                return matchText.Substring(2, matchText.Length - 3).Trim();
            }).ToList();
            foreach(var parameterReference in parameterReferences) {
                parameterKeys.Remove(parameterReference);
            }
            foreach(var leftOverParameterKey in parameterKeys) {
                Logger.Log(SubFormatParameterIsNotUsed(leftOverParameterKey), expression.Parameters.First(kv => kv.Key.Value == leftOverParameterKey).Key);
            }

            // inline parameters with literal values
            var inlinedParameterKeys = new HashSet<string>();
            var newSubFormatString = SubFormatStringParameterRegex.Replace(expression.FormatString.Value, match => {

                // check if match is a local parameter reference
                var matchText = match.ToString();
                var matchReference = matchText.Substring(2, matchText.Length - 3).Trim();
                var matchParameter = expression.Parameters.FirstOrDefault(parameter => parameter.Key.Value == matchReference);

                // check if parmaeter is a literal value
                if(matchParameter?.Value is LiteralExpression matchParameterLiteralExpression) {
                    inlinedParameterKeys.Add(matchParameter.Key.Value);

                    // make literal value safe for inlining
                    var literalValue = SubFormatStringParameterRegex.Replace(matchParameterLiteralExpression.Value, literalMatch => {
                        var literalMatchText = literalMatch.ToString();
                        var literalMatchInnerText = literalMatchText.Substring(2, literalMatchText.Length - 3);

                        // NOTE (2020-06-27, bjorg): !Sub replaces "${!Text}" with "${Text}"
                        return "${!" + literalMatchInnerText + "}";
                    });
                    return literalValue;
                }

                // keep original
                return matchText;
            });

            // check if any parameters were inlined
            var newParameters = expression.Parameters;
            if(inlinedParameterKeys.Any()) {

                // keep only parameters that were not inlined
                newParameters = new ObjectExpression {
                    SourceLocation = expression.Parameters.SourceLocation
                };
                foreach(var parameter in expression.Parameters.Where(parameter => !inlinedParameterKeys.Contains(parameter.Key.Value))) {
                    newParameters[parameter.Key] = parameter.Value;
                }
            }

            // check if all parameters were inlined
            if(!newParameters.Any()) {

                // unescape "${!Text}" to "${Text}
                newSubFormatString = SubFormatStringEscapeRegex.Replace(newSubFormatString, escapedMatch => {
                    var escapedMatchText = escapedMatch.ToString();
                    var escapedMatchReference = escapedMatchText.Substring(3, escapedMatchText.Length - 4);
                    return "${" + escapedMatchReference + "}";
                });

                // return string literal as final outcome
                return Fn.Literal(newSubFormatString, expression.FormatString.SourceLocation);
            }

            // check if nothing has changed
            if(
                object.ReferenceEquals(newParameters, expression.Parameters)
                && (newSubFormatString == expression.FormatString.Value)
            ) {
                return expression;
            }

            // create a new !Sub expression
            return new SubFunctionExpression {
                FormatString = Fn.Literal(newSubFormatString, expression.FormatString.SourceLocation),
                Parameters = newParameters,
                SourceLocation = expression.SourceLocation
            };
        }

        private AExpression InlineSubFunctionParameters(SubFunctionExpression expression) {

            // inline !Ref/!GetAtt expressions
            var inlinedParameterKeys = new HashSet<string>();
            var newSubFormatString = SubFormatStringParameterRegex.Replace(expression.FormatString.Value, match => {

                // check if match is a local parameter reference
                var matchText = match.ToString();
                var matchReference = matchText.Substring(2, matchText.Length - 3).Trim();
                var matchParameter = expression.Parameters.FirstOrDefault(parameter => parameter.Key.Value == matchReference);

                // check if parmaeter is a !Ref/!GetAtt expression
                switch(matchParameter?.Value) {
                case ReferenceFunctionExpression referenceFunctionExpression:
                    inlinedParameterKeys.Add(matchParameter.Key.Value);

                    // convert to !Ref inline notation
                    return "${" + referenceFunctionExpression.ReferenceName.Value + "}";
                case GetAttFunctionExpression getAttFunctionExpression:
                    if(getAttFunctionExpression.AttributeName is LiteralExpression attributeLiteralExpression) {
                        inlinedParameterKeys.Add(matchParameter.Key.Value);

                        // convert to !GetAtt inline notation
                        return "${" + getAttFunctionExpression.ReferenceName.Value + "." + attributeLiteralExpression.Value + "}";
                    }
                    break;
                }

                // keep original
                return matchText;
            });

            // check if any parameters were inlined
            var newParameters = expression.Parameters;
            if(inlinedParameterKeys.Any()) {

                // keep only parameters that were not inlined
                newParameters = new ObjectExpression {
                    SourceLocation = expression.Parameters.SourceLocation
                };
                foreach(var parameter in expression.Parameters.Where(parameter => !inlinedParameterKeys.Contains(parameter.Key.Value))) {
                    newParameters[parameter.Key] = parameter.Value;
                }
            }

            // check if nothing has changed
            if(
                object.ReferenceEquals(newParameters, expression.Parameters)
                && (newSubFormatString == expression.FormatString.Value)
            ) {
                return expression;
            }

            // create a new !Sub expression
            return new SubFunctionExpression {
                FormatString = Fn.Literal(newSubFormatString, expression.FormatString.SourceLocation),
                Parameters = newParameters,
                SourceLocation = expression.SourceLocation
            };
        }

        private AExpression NormalizeSubFunctionExpression(SubFunctionExpression expression) {

            // NOTE (2019-12-07, bjorg): convert all nested !Ref and !GetAtt expressions into
            //  explit expressions using local !Sub parameters; this allows us track these
            //  references as dependencies, as well as allowing us later to analyze
            //  and resolve these references without having to parse the !Sub format string anymore;
            //  during the optimization phase, the !Ref and !GetAtt expressions are inlined again
            //  where possible.

            // replace as many ${VAR} occurrences as possible in the format string
            var newParameters = new List<ObjectExpression.KeyValuePair>();
            var replaceFormatString = EvaluateSubFormatString(
                expression.FormatString.Value,
                expression.Parameters,
                expression.SourceLocation,
                (reference, attribute, startLineOffset, endLineOffset, startColumnOffset, endColumnOffset) => {

                    // compute source location based on line/column offsets
                    var sourceLocation = new SourceLocation(
                        expression.FormatString.SourceLocation.FilePath,
                        expression.FormatString.SourceLocation.LineNumberStart + startLineOffset,
                        expression.FormatString.SourceLocation.LineNumberStart + endLineOffset,
                        expression.FormatString.SourceLocation.ColumnNumberStart + startColumnOffset,
                        expression.FormatString.SourceLocation.ColumnNumberStart + endColumnOffset
                    );

                    // check if embedded expression is a !Ref or !GetAtt expression
                    AExpression argExpression;
                    if(attribute == null) {

                        // create explicit !Ref expression
                        argExpression = new ReferenceFunctionExpression {
                            SourceLocation = sourceLocation,
                            ReferenceName = Fn.Literal(reference)
                        };
                    } else {

                        // create explicit !GetAtt expression
                        argExpression = new GetAttFunctionExpression {
                            SourceLocation = sourceLocation,
                            ReferenceName = Fn.Literal(reference),
                            AttributeName = Fn.Literal(attribute)
                        };
                    }

                    // move the resolved expression into !Sub parameters
                    var argName = $"P{expression.Parameters.Count + newParameters.Count}";
                    newParameters.Add(new ObjectExpression.KeyValuePair(Fn.Literal(argName), argExpression));

                    // substitute found value as new argument
                    return "${" + argName + "}";
                }
            );
            if(newParameters.Any()) {
                return new SubFunctionExpression {
                    FormatString = Fn.Literal(replaceFormatString, expression.FormatString.SourceLocation),
                    Parameters = new ObjectExpression(expression.Parameters.Union(newParameters)) {
                        SourceLocation = expression.Parameters.SourceLocation
                    },
                    SourceLocation = expression.SourceLocation
                };
            }
            return expression;
        }

        private string EvaluateSubFormatString(string subFormatString, ObjectExpression parameters, SourceLocation sourceLocation, Func<string, string?, int, int, int, int, string> replace) {
            return SubFormatStringParameterRegex.Replace(subFormatString, match => {

                // parse matched expression into Reference.Attribute
                var matchText = match.ToString();
                var namePair = matchText
                    .Substring(2, matchText.Length - 3)
                    .Trim()
                    .Split('.', 2);
                var reference = namePair[0].Trim();
                var attribute = (namePair.Length == 2) ? namePair[1].Trim() : null;

                // check if reference is to a local !Sub parameter
                if(parameters.ContainsKey(reference)) {
                    if(attribute != null) {

                        // local references cannot have an attribute suffix
                        Logger.Log(SubFunctionParametersCannotUseAttributeNotation(reference), sourceLocation);
                    }

                    // keep matched expression as-is
                    return matchText;
                }

                // compute matched expression position
                var startLineOffset = subFormatString.Take(match.Index).Count(c => c == '\n');
                var endLineOffset = subFormatString.Take(match.Index + matchText.Length).Count(c => c == '\n');
                var startColumnOffset = subFormatString.Take(match.Index).Reverse().TakeWhile(c => c != '\n').Count();
                var endColumnOffset = subFormatString.Take(match.Index + matchText.Length).Reverse().TakeWhile(c => c != '\n').Count();

                // invoke callback
                return replace(reference, attribute, startLineOffset, endLineOffset, startColumnOffset, endColumnOffset) ?? matchText;
            });
        }

        private LiteralExpression BooleanLiteral(AExpression expression, bool value, bool fromExistsExpression = false)
            => new LiteralExpression(value.ToString(), LiteralType.Bool, fromExistsExpression) {
                SourceLocation = expression.SourceLocation
            };
    }
}