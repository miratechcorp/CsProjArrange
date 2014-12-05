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
        #region Methods

        #region Public Methods

        /// <summary>
        /// Run the program using the supplied command line arguments.
        /// </summary>
        /// <param name="args"></param>
        public void Run(string[] args)
        {
            bool help = false;
            string inputFile = null;
            string outputFile = null;
            IList<string> stickyElementNames = null;
            IEnumerable<string> sortAttributes = null;
            CsProjArrange.ArrangeOptions options = CsProjArrange.ArrangeOptions.All;

            OptionSet os = new OptionSet(){
                { "?|help", "Display this usage message.", x => help = x != null },
                { "i|input=", "Set the input file name. Standard input is the default.", x => inputFile = x },
                { "o|output=", "Set the output file name. Standard output is the default.", x => outputFile = x },
                { "s|sticky=", "Comma separated list of elements names which should be stuck to the top.", x => stickyElementNames = x.Split(',') },
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

            try {
                new CsProjArrange().Arrange(inputFile, outputFile ?? inputFile, stickyElementNames, sortAttributes, options);
            } catch (Exception e) {
                Console.Error.WriteLine("Encountered an error: {0}", e.Message);
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Display the help text for this command line program.
        /// </summary>
        /// <param name="os"></param>
        private void Help(OptionSet os)
        {
            Console.WriteLine("Usage: " + System.AppDomain.CurrentDomain.FriendlyName + " [-?|--help] [-iINPUT|--input=INPUT] [-oOUTPUT|--output=OUTPUT] [-sSTICKY|--sticky=STICKY] [-aSORTATTRIBUTES|--attributes=SORTATTRIBUTES]");
            Console.WriteLine();
            Console.WriteLine("Option:");
            os.WriteOptionDescriptions(Console.Out);
        }

        #endregion Private Methods

        #endregion Methods
    }
}