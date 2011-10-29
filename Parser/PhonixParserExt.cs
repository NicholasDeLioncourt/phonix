using Antlr.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Phonix.Parse
{
#if debug
    using Antlr.Runtime.Debug;
    public partial class PhonixParser : DebugParser
#else
    public partial class PhonixParser : Parser
#endif
    {
        private Phonology _phono;
        private string _currentFile;
        private bool _parseError;
        private readonly Dictionary<string, IEnumerable<FeatureValue>> _featureValueGroups = 
            new Dictionary<string, IEnumerable<FeatureValue>>();

        public event Action<string> ParseBegin;
        public event Action<string> ParseEnd;

        public void Parse(Phonology phono)
        {
            if (ParseBegin == null)
            {
                ParseBegin += (s) => {};
            }
            if (ParseEnd == null)
            {
                ParseEnd += (s) => {};
            }

            try
            {
                ParseBegin(_currentFile);

                _phono = phono;
                parseRoot();

                if (_parseError)
                {
                    throw new ParseException(_currentFile);
                }
            }
            finally
            {
                ParseEnd(_currentFile);
            }
        }

        private void ParseImport(string importFile)
        {
            var importParser = FileParser(_currentFile, importFile);
            importParser.ParseBegin += (s) => this.ParseBegin(s);
            importParser.ParseEnd += (s) => this.ParseEnd(s);
            importParser.Parse(_phono);
        }

        // this is the public method for parsing a root file
        public static PhonixParser FileParser(string file)
        {
            return FileParser(file, file);
        }

        // this overload is used internally for parsing imports
        private static PhonixParser FileParser(string currentFile, string filename)
        {
            if (currentFile == null || filename == null)
            {
                throw new ArgumentNullException();
            }

            try
            {
                // first try opening the file directly
                var file = File.OpenText(filename);
                return GetParserForStream(filename, file);
            }
            catch (FileNotFoundException)
            {
                // look for a file in the same directory as the current file
                try
                {
                    var fullCurrentPath = Path.GetFullPath(currentFile);
                    var currentDir = Path.GetDirectoryName(fullCurrentPath);
                    var fullPath = Path.Combine(currentDir, filename);

                    var file = File.OpenText(fullPath);
                    return GetParserForStream(fullPath, file);
                }
                catch (FileNotFoundException)
                {
                    // look for an embedded resource. Exceptions thrown here are allowed to propagate.
                    var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filename);
                    if (stream == null)
                    {
                        throw new FileNotFoundException(filename);
                    }
                    return GetParserForStream(filename, new StreamReader(stream));
                }
            }
        }

        public static PhonixParser StringParser(string str)
        {
            StringReader reader = new StringReader(str);
            return GetParserForStream("<string>", reader);
        }

        private static PhonixParser GetParserForStream(string filename, TextReader stream)
        {
            var lexStream = new ANTLRReaderStream(stream);
            var lexer = new PhonixLexer(lexStream);
            var tokenStream = new CommonTokenStream();
            tokenStream.TokenSource = lexer;

#if debug
            var tracer = new PhonixDebugTracer(tokenStream);
            var parser = new PhonixParser(tokenStream, tracer);
#else
            var parser = new PhonixParser(tokenStream);
#endif

            parser._currentFile = filename;
            return parser;
        }

        private FeatureMatrix GetSingleSymbol(List<Symbol> symbols)
        {
            if (symbols.Count != 1)
            {
                string symbolStr = String.Join("", symbols.ConvertAll(s => s.ToString()).ToArray());
                throw new InvalidMultipleSymbolException(symbolStr);
            }
            return symbols[0].FeatureMatrix;
        }

        private class PhonixDebugTracer : Antlr.Runtime.Debug.Tracer
        {
            public PhonixDebugTracer(ITokenStream stream)
                : base(stream)
            {
            }
        }
    }
}
