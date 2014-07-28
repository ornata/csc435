/* cbc.cs

    This is the main module for invoking each step of the compilation process.
    Currently, cbc performs these actions:
    * lexes and parses the input
    * builds the AST
    * builds a symbol-table of top-level names: for namespaces and classes
    * performs type and semantic checking
    * generates LLVM code

    Author: Nigel Horspool
    
    Dates: 2012-2014
*/

using System;
using System.IO;
using System.Collections.Generic;
using FrontEnd;


public class Start {

    public static int SemanticErrorCnt = 0;  // count of semantic errors
    public static int WarningMsgCnt = 0;     // count of warning messages
    
    // reports a semantic error
    // if no line number can be associated with the message, 0 should be used
    public static void SemanticError( int lineNum, string message, params object[] args ) {
        if (lineNum > 0)
            Console.Write("{0}: Error: ", lineNum);
        else
            Console.Write("*** Error: ");
        Console.WriteLine(String.Format(message, args));
        SemanticErrorCnt++;
    }  

    // outputs a warning message
    public static void WarningMessage( int lineNum, string message, params object[] args ) {
        if (lineNum > 0)
            Console.Write("{0}: Warning: ", lineNum);
        else
            Console.Write("*** Warning: ");
        Console.WriteLine(String.Format(message, args));
        WarningMsgCnt++;
    }

    static AST DoParse( string filename, bool printTokens ) {
        AST result = null;
        try {
            using (FileStream src = File.OpenRead(filename)) {
                Scanner sc = new Scanner(src);
                sc.Initialize();
                if (printTokens)
                    sc.TrackTokens("tokens.txt");
        
                Parser parser = new Parser(sc);
            
                if (parser.Parse())
                    result = parser.Tree;
        
                sc.CleanUp();
            }
        } catch( Exception e ) {
            Console.WriteLine(e.Message);
        }
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
            "    -tokens   print the token sequence after scanning",
            "    -nocode   do not generate LLVM code"
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
        bool noCode = false;
        string target = "x86_64-unknown-linux-gnu";  // default for CSC linux server

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
                case "-nocode":
                    noCode = true;  break;
                default:
                    if (arg.StartsWith("-target="))
                    {
                        target = arg.Substring(8).Trim();
                        break;
                    }
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

        TLVisitor tlv = new TLVisitor();
        tree.Accept(tlv, NameSpace.TopLevelNames);
        
        TypeCheckVisitor1 tcv1 = new TypeCheckVisitor1();
        tree.Accept(tcv1, null);

        TypeCheckVisitor2 tcv2 = new TypeCheckVisitor2();
        tree.Accept(tcv2, null);

        // allow inspection of all the type annotations
        if (printASTtc) {
        	PrVisitor printVisitor = new PrVisitor();
            tree.Accept(printVisitor,0);    // print AST with the datatype annotations
        }


        if (printNS)
            NameSpace.Print();

        if (SemanticErrorCnt > 0) {
            Console.WriteLine("\n{0} errors reported, no code generated\n", SemanticErrorCnt);
            return;
        }

        if (noCode)
        {
            Console.WriteLine("\nLLVM output suppressed, compilation ended");
            return;
        }

		// generate intermediate representation (IR) code in LLVM's text formal
		string llfilename = filename.Substring(0,filename.Length-2)+"ll";
		LLVM llvm = new LLVM(llfilename, target);

        LLVMVisitor1 lv1 = new LLVMVisitor1(llvm);
        tree.Accept(lv1, null);
        
        // second pass of LLVM IR code generation
        LLVMVisitor2 lv2 = new LLVMVisitor2(llvm);
        tree.Accept(lv2, null);
        
        // close tle llvm file
        llvm.Dispose();
		
		// ideally no semantic errors are detected while creating IR code, but in case
        if (SemanticErrorCnt > 0) {
            Console.WriteLine("\n{0} errors reported\n", SemanticErrorCnt);
            return;
        }

        // There could be warning messages from any previous stage
        if (WarningMsgCnt > 0) {
            Console.WriteLine("\n{0} warning messages generated\n", WarningMsgCnt);
        }
    }

}
