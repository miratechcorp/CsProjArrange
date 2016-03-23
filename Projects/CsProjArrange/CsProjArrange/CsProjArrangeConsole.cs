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
using System.Collections.Generic;
using Mono.Options;

namespace CsProjArrange
{
    /// <summary>
    /// Parse console arguments and run the CsProjArrange.Arrange method.
    /// </summary>
    public class CsProjArrangeConsole
    {
        /// <summary>
        /// Run the program using the supplied command line arguments.
        /// </summary>
        /// <param name="args"></param>
        public void Run(string[] args)
        {
            bool help = false;
            string inputFile = null;
            string outputFile = null;
            IEnumerable<string> stickyElementNames = null;
            IEnumerable<string> keepOrderElementNames = null;
            IEnumerable<string> sortAttributes = null;
            CsProjArrange.ArrangeOptions options = CsProjArrange.ArrangeOptions.All;

            OptionSet os = new OptionSet(){
                { "?|help", "Display this usage message.", x => help = x != null },
                { "i|input=", "Set the input file name. Standard input is the default.", x => inputFile = x },
                { "o|output=", "Set the output file name. Standard output is the default.", x => outputFile = x },
                { "s|sticky=", "Comma separated list of element names which should be stuck to the top.", x => stickyElementNames = x.Split(',') },
                { "k|keeporder=", "Comma separated list of element names where children should not be sorted.", x => keepOrderElementNames = x.Split(',') },
                { "a|attributes=", "Comma separated list of attributes to sort on.", x => sortAttributes = x.Split(',') },
                { "p|options=", "Specify options", x => Enum.TryParse<CsProjArrange.ArrangeOptions>(x, out options) },
            };
            List<string> extra;
            try {
                extra = os.Parse(args);
            } catch (OptionException e) {
                Console.Write(System.AppDomain.CurrentDomain.FriendlyName + ": ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `" + System.AppDomain.CurrentDomain.FriendlyName + " --help` for more information.");
                return;
            }

            if (
                help
                ||
                extra.Count > 0
                ) {
                Help(os);
                return;
            }

            try
            {
                var csProjArrange = CreateCsProjArrange(stickyElementNames, keepOrderElementNames, sortAttributes, options);
                csProjArrange.Arrange(inputFile, outputFile ?? inputFile);
            }
            catch (Exception e) {
                Console.Error.WriteLine("Encountered an error: {0}", e.Message);
            }
        }

        private static CsProjArrange CreateCsProjArrange(IEnumerable<string> stickyElementNames, IEnumerable<string> keepOrderElementNames,
            IEnumerable<string> sortAttributes, CsProjArrange.ArrangeOptions options)
        {
            CsProjArrangeStrategy strategy = new CsProjArrangeStrategy(stickyElementNames, keepOrderElementNames, sortAttributes, options);
            CsProjArrange csProjArrange = new CsProjArrange(strategy);
            return csProjArrange;
        }

        /// <summary>
        /// Display the help text for this command line program.
        /// </summary>
        /// <param name="os"></param>
        private void Help(OptionSet os)
        {
            Console.WriteLine("Usage: " + System.AppDomain.CurrentDomain.FriendlyName + " [-?|--help] [-iINPUT|--input=INPUT] [-oOUTPUT|--output=OUTPUT] [-sSTICKY|--sticky=STICKY] [-kKEEPORDER|--keeporder=KEEPORDER] [-aSORTATTRIBUTES|--attributes=SORTATTRIBUTES]");
            Console.WriteLine();
            Console.WriteLine("Option:");
            os.WriteOptionDescriptions(Console.Out);
        }
    }
}