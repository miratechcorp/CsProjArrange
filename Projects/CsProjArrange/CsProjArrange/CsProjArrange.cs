// Copyright 2014 MIRATECH
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace CsProjArrange
{
    /// <summary>
    /// A class to arrange .csproj files.
    /// </summary>
    public class CsProjArrange
    {
        private static readonly XmlNodeType[] includeXmlNodeTypes = 
            new XmlNodeType[] {
                XmlNodeType.Comment,
                XmlNodeType.Element,
                XmlNodeType.EndElement,
                XmlNodeType.Text,
            };

        private readonly CsProjArrangeStrategy _csProjArrangeStrategy;

        public CsProjArrange(CsProjArrangeStrategy csProjArrangeStrategy)
        {
            _csProjArrangeStrategy = csProjArrangeStrategy;
        }

        [Flags]
        public enum ArrangeOptions
        {
            None = 0,
            All = -1,
            CombineRootElements = 1 << 0,
            KeepCommentWithNext = 1 << 1,
            SortRootElements = 1 << 2,
            SplitItemGroups = 1 << 3,
            NoRoot = All & ~CombineRootElements & ~SortRootElements,
            NoSortRootElements = ~SortRootElements,
        }

        /// <summary>
        /// Arrange the project file using the specified options.
        /// </summary>
        /// <param name="inputFile">The file path for the input .csproj file. If none is specified, it will use the standard input.</param>
        /// <param name="outputFile">The file path for the output .csproj file. If none is specified, it will output to standard output.</param>
        /// <param name="stickyElementNames">A list of element names which should be stuck to the top when sorting the nodes. Defaults to the values: Import, Task, PropertyGroup, ItemGroup, Target, Configuration, Platform, ProjectReference, Reference, Compile, Folder, Content, None, When, and Otherwise.</param>
        /// <param name="sortAttributes">A list of attributes which should be used to sort the elements after they have been sorted by name. Defaults to the values: Include.</param>
        /// <param name="options">Options for the arrange.</param>
        public void Arrange(string inputFile, string outputFile)
        {
            // Load the document.
            XDocument input;
            if (inputFile == null)
            {
                input = XDocument.Load(Console.OpenStandardInput());
            }
            else
            {
                input = XDocument.Load(inputFile);
            }

            _csProjArrangeStrategy.Arrange(input);

            BackupInputFile(inputFile, outputFile);

            WriteOutput(input, outputFile);
        }

        private static void WriteOutput(XDocument input, string outputFile)
        {
            if (outputFile == null)
            {
                // Write the output to standard output.
                var writerSettings = new XmlWriterSettings();
                writerSettings.Encoding = Encoding.UTF8;
                writerSettings.Indent = true;
                using (var writer = XmlWriter.Create(Console.OpenStandardOutput(), writerSettings))
                {
                    input.WriteTo(writer);
                }
            }
            else
            {
                // Write the output file.
                input.Save(outputFile);
            }
        }

        private void BackupInputFile(string inputFile, string outputFile)
        {
            // Backup input file if we are overwriting.
            if ((inputFile != null) && (inputFile == outputFile))
            {
                File.Copy(inputFile, inputFile + ".bak", true);
            }
        }
    }
}