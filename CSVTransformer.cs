//#define enable_dump

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;

namespace CSVTransformer
{
    class CSVTransformer
    {
        string InputFile;
        string InputFormat;
        Format IFmt;
        string OutputFile;
        string OutputFormat;
        Format OFmt;
        bool Verbose = true;
        bool ForceAddSeparators = false;

        enum Format
        {
            SEMICOLON,     // ;
            COMMA,      // ,
            TAB,        // 
            SPACE       //
        }
    
        static void Main(string[] args)
        {
            new CSVTransformer(args);
        }        

        CSVTransformer(string[] args)
        {
            try
            {
                Verbose &= !CheckOpt(args, "-q");
                ForceAddSeparators = CheckOpt(args, "-f");
                Info();
                InputFile = TryGetArg(args, 0);
                InputFormat = TryGetArg(args, 1);
                OutputFile = TryGetArg(args, 2);
                OutputFormat = TryGetArg(args, 3);
                IFmt = TryGetFormat(InputFormat);
                OFmt = TryGetFormat(OutputFormat);
                ApplyTransform();
            } catch (Exception Ex)
            {
                Ln("Error: " + Ex.Message);
            }
        }

        char toChar(Format f)
        {
            char s = ',';
            switch (f)
            {
                case Format.COMMA:
                    s = ',';
                    break;
                case Format.TAB:
                    s = '\t';
                    break;
                case Format.SPACE:
                    s = ' ';
                    break;
                case Format.SEMICOLON:
                    s = ';';
                    break;
            }
            return s;
        }

        void ApplyTransform()
        {
            Ln($"transform: {InputFile} : {InputFormat} -> {OutputFile} : {OutputFormat}");
            var inp = GetFileLines(InputFile);
            var outp = new List<string>();
            char splitSymbol = toChar(IFmt);            
            var nbc = 0;
            foreach ( var s in inp )
            {
                /* split selon les symbols non neutralisés par les doubles quotes
                 * les doubles quotes sont neutralisées par double quote
                 */
                var t = ExtractColumns(s, splitSymbol);
                if (nbc == 0)
                    nbc = t.Count;
#if enable_dump
                foreach (var x in t)
                    L(x);
                Ln();
#endif
                outp.Add(CollapseColumns(nbc, t, toChar(OFmt)));
            }
            WriteFile(OutputFile, outp);
            Ln("done.");
        }

        string CollapseColumns(int nbc,List<string> t,char separator)
        {
            var seps = separator + "";
            for (int i=0;i<t.Count;i++)
            {
                var s = t[i];
                if (!s.StartsWith("\""))
                {
                    if (s.Contains(seps) || ForceAddSeparators)
                    {
                        t[i] = "\"" + s + "\"";
                    }
                }
            }
            while (t.Count < nbc)
                t.Add(seps);
            return string.Join(seps, t);
        }  

        List<string> ExtractColumns(string s, char separator)
        {
            var r = new List<string>();
            var sb = new StringBuilder();
            var t = s.ToCharArray();
            var k = t.Length;
            bool inQuotedStr = false;
            for (int i = 0; i < k; i++)
            {
                var c = t[i];
                var isQ = c == '"';
                var hasNext = i < (k - 1);
                var nextIsQ = (hasNext && t[i + 1] == '"') ? true : false;
                var isQuotedStrDelimiter = isQ && !nextIsQ;
                if (!inQuotedStr)
                {
                    if (isQuotedStrDelimiter)
                        inQuotedStr = true;
                    if (c == separator)
                    {
                        r.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                        sb.Append(c);
                }
                else
                {
                    if (isQuotedStrDelimiter)
                        inQuotedStr = false;
                    sb.Append(c);
                }
            }
            r.Add(sb.ToString());
            return r;
        }

        #region arguments

        bool CheckOpt(string[] args,string opt)
        {
            var r = false;
            foreach (var a in args)
                if (a.ToLower() == opt)
                    return true;
            return r;
        }

        Format TryGetFormat(string[] args, int n)
        {
            return TryGetFormat(TryGetArg(args, n));
        }

        Format TryGetFormat(string s)
        {
            s = s.ToUpper();
            Format f;
            if (!Enum.TryParse<Format>(s, out f))
                UsageAndAbort($"unknown format: {s}");
            return f;
        }

        string TryGetArg(string[] args,int n)
        {
            if (n < args.Length)
                return args[n];
            else
                UsageAndAbort("wrong number of arguments");
            return null;
        }

        #endregion

        #region traces & exit

        void Info()
        {
            Ln("CSV Transformer " + this.GetType().Assembly.GetName().Version);
            Ln("(c) 2018 Franck Gaspoz http://franckgaspoz.fr");
            Ln();
        }

        void Usage()
        {
            Ln("command line syntax: inputFile inputFileFormat outputFile outputFileFormat [opts]");
            Ln("inputFileFormat,outputFileFormat: comma | semicolon | tab | space");
            Ln("opts:");
            Ln("-q : supress all outputs except errors");
            Ln("-f : force add of separators");
            Ln();
        }

        void UsageAndAbort(string s)
        {
            Usage();
            Abort(s);
        }

        void Abort(string s)
        {
            Ln(s);
            Environment.Exit(0);
        }

        void Ln(string s = "")
        {
            if (Verbose)
                Console.WriteLine(s);
        }

        void L(string s)
        {
            if (Verbose)
                Console.Write(s);
        }

        #endregion

        #region fichiers

        List<string> GetFileLines(string Path)
        {
            List<string> Res = new List<string>();
            using (StreamReader sr = new StreamReader(Path))
            {
                while (!sr.EndOfStream)
                {
                    Res.Add(sr.ReadLine());
                }
            }
            return Res;
        }

        void WriteFile(string Path, List<string> Lines)
        {
            using (StreamWriter sw = new StreamWriter(Path))
            {
                foreach (String S in Lines)
                    sw.WriteLine(S);
                sw.Flush();
            }
        }

        #endregion
    }
}
