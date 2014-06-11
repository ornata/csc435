/* cbc.cs

    This will become the main module for invoking each step
    of the compilation process.
    Currently, cbc only lexes and parses the input.

    Datea: 2012-2014
*/

using System;
using System.IO;
using System.Collections.Generic;
using FrontEnd;


public class Start {
    
    static AST DoParse( string filename, bool printTokens ) {
        AST result = null;
        FileStream src = File.OpenRead(filename);
        Scanner sc = new Scanner(src);
        sc.Initialize();
        if (printTokens)
            sc.TrackTokens("tokens.txt");

        Parser parser = new Parser(sc);
    
        if (parser.Parse())
            result = parser.Tree;

        sc.CleanUp();
        return result;
    }
    
    static void Usage() {
        string[] usage = {
            "Usage:",
            "    cbc [options] filename",
            "where 'filename' must have the suffix '.cb' or '.cs' and the options are:",
            "    -ast      print the AST after construction",
            "    -tc       print the AST after type checking",
            "    -ns       recursively print the contents of the top-level namespace",
            "    -tokens   print the token sequence after scanning"
        };
        foreach(string s in usage) {
            Console.WriteLine("{0}", s);
        }
        System.Environment.Exit(1);  // terminate!
    }
    
    public static void Main( string[] args ) {
        string filename = null;
        bool printAST = false;
        bool printASTtc = false;
        bool printTokens = false;
        bool printNS = false;

        foreach( string arg in args ) {
            if (arg.StartsWith("-")) {
                switch(arg) {
                case "-ast":
                    printAST = true;  break;
                case "-ns":
                    printNS = true;  break;
                case "-tokens":
                    printTokens = true;  break;
                case "-tc":
                    printASTtc = true;  break;
                default:
                    Console.WriteLine("Unknown option {0}, ignored", arg);
                    break;
                }
            } else {
                if (filename != null)
                    Usage();
                filename = arg;
            }
        }
        // require exactly one input file to be provided
        if (filename == null)
            Usage();
        // require a filename suffix of .cb or .cs
        if (!filename.EndsWith(".cb") && !filename.EndsWith(".cs"))
            Usage();

        AST tree = DoParse(filename,printTokens);

        if (tree == null) {
            Console.WriteLine("\n-- no tree constructed");
            return;
        }

        if (printAST) {
        	PrVisitor printVisitor = new PrVisitor();
            tree.Accept(printVisitor,0);
        }

        CbType.Initialize();  // initialize some predefined types and top-level namespace

        // Create and invoke your top-level namespace visitor here
        
        if (printNS)
            NameSpace.Print();

/*
        int numErrors;
        
        // perform typechecking ...

        if (printASTtc) {
        	PrVisitor printVisitor = new PrVisitor();
            tree.Accept(printVisitor);    // print AST with datatype annotations
        }

		// generate intermediate code

        if (numErrors > 0) {
            Console.WriteLine("\n{0} errors reported, compilation halted", numErrors);
            return;
        }
*/

    }

}
