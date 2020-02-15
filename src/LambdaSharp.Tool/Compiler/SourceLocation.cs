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

using System;

namespace LambdaSharp.Tool.Compiler {

    public class SourceLocation {

        //--- Class Fields ---
        public static readonly SourceLocation Empty = new SourceLocation("", 0, 0, 0, 0);

        //--- Constructors ---
        public SourceLocation(string filePath, int lineNumberStart, int columnNumberStart, int lineNumberEnd, int columnNumberEnd) {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            LineNumberStart = lineNumberStart;
            ColumnNumberStart = columnNumberStart;
            LineNumberEnd = lineNumberEnd;
            ColumnNumberEnd = columnNumberEnd;
        }

        //--- Properties ---
        public string FilePath { get; private set; }
        public int LineNumberStart { get; private set; }
        public int ColumnNumberStart { get; private set; }
        public int LineNumberEnd { get; private set; }
        public int ColumnNumberEnd { get; private set; }
    }
}