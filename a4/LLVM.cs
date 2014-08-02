/* LLVM.cs
 * 
 * Utility code to help with outputting intermediate code in the
 * LLVM text format (as a '.ll' file).
 * 
 * Author: Nigel Horspool
 * Date: July 2014
 */
 
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;


namespace FrontEnd
{
    // This datatype provides a <type, value> pair as used by LLVM
    // The IsReference flag distinguishes a value in memory from a value
    // held in a temporary. If IsReference is true, the temporary holds
    // a reference to a memory location.
    public class LLVMValue {
        public bool IsReference { get; set; }
        public string LLType{ get; set; }
        public string LLValue{ get; set; }

        public LLVMValue( string t, string v, bool isref ) {
            LLType = t; LLValue = v; IsReference = isref;
        }

        public override string ToString() { return LLType + " " + LLValue; }
    }

    public partial class LLVM
    {
        const char leftBrace = '{';    // used inside format strings to avoid
        const char rightBrace = '}';   // having to remember to escape the characters

        int ptrSize = 64;           // characteristics of the target platform
        int ptrAlign = 8;
        bool macOS = false;
        string targetTriple;

        TextWriter ll = null;  // where LLVM code is written

        // constructor -- the default target triple corresponds to the
        // CSc teaching server: linux.csc.uvic.ca
        public LLVM( string llFileName, string targetTriple="x86_64-unknown-linux-gnu" ) {
            this.targetTriple = targetTriple;
            try {
                ll = new StreamWriter(llFileName);
                switch(targetTriple) {
                case "i686-pc-mingw32":  // triple for 32-bit Windows system
                    ll.WriteLine(preamble32);  ptrSize = 32;  ptrAlign = 4;
                    break;
                case "x86_64-unknown-linux-gnu":  // 64-bit Linux system
                    ll.WriteLine(preamble64);  ptrSize = 64;  ptrAlign = 8;
                    break;
                case "x86_64-apple-macosx10.9.3":  // 64-bit Mac OS X system
                    ll.WriteLine(preambleMac64);
                    ptrSize = 64;  ptrAlign = 8;  macOS = true;
                    break;
                default:
                    Console.WriteLine("Unsupported triple: {0}", targetTriple);
                    break;
                }
                ll.WriteLine("target triple = \"{0}\"\n", targetTriple);
                WritePredefinedCode();
            } catch(Exception e) {
                Console.WriteLine("Unable to write to file {0}\n\n{1}\n",
                    llFileName, e.Message);
                System.Environment.Exit(1);
            }
        }

        // Must be called when the LLVM code generation is finished
        public void Dispose() {
            //Debug.Assert(suspensionCnt == 0);
            string s;
            switch(targetTriple) {
            case "i686-pc-mingw32": s = epilog32; break;
            case "x86_64-apple-macosx10.9.3": s = "\n"; break;
            default: s = epilog64; break;
            }
            ll.Write(s);
            try
            {
                ll.Close();
            }
            finally
            {
                ll.Dispose();
            }
            ll = null;
        }

        // For purposes of implementing Moessenboeck's SSA algorithm, we
        // can divert generated LLVM code into memory (as a big string).
        private Stack<TextWriter> savedStreams = null;
        private Stack<int> savedIndexNums = null;

        public void DivertOutput()
        {
            if (savedStreams == null)
                savedStreams = new Stack<TextWriter>();
            savedStreams.Push(ll);
            ll = new StringWriter();
        }

        public void DiscardOutput()
        {
            if (savedStreams == null)
                savedStreams = new Stack<TextWriter>();
            if (savedIndexNums == null)
                savedIndexNums = new Stack<int>();
            savedStreams.Push(ll);
            ll = TextWriter.Null;
            savedIndexNums.Push(nextUnnamedIndex);
        }


        // End the diversion to memory and return the diverted LLVM code
        // as a single very long string
        public string UndivertOutput()
        {
            if (savedStreams == null || savedStreams.Count == 0)
                throw new Exception("Undivert not paired with a divert");
            string result = ((StringWriter)ll).ToString();
            ll.Close();
            ll.Dispose();
            ll = savedStreams.Pop();
            return result;
        }

        public void ResumeOutput()
        {
            if (savedStreams == null || savedStreams.Count == 0)
                throw new Exception("Resume not paired with a discard");
            ll.Close();
            ll.Dispose();
            ll = savedStreams.Pop();
            nextUnnamedIndex = savedIndexNums.Pop();
        }

        // Diverted LLVM code can be reinserted using this method
        public void InsertCode(string code)
        {
            ll.WriteLine("\n; --- INSERTED CODE FOLLOWS ---");
            ll.WriteLine(code);
            ll.WriteLine("; --- END OF INSERTED CODE ---");
        }

        // The following methods convert a Cb type descriptor into its
        // LLVM type representation

        public string GetTypeDescr(CbBasic bt)
        {
            if (bt == CbType.Int) return "i32";
            if (bt == CbType.Char) return "i8";
            if (bt == CbType.Bool) return "i1";
            if (bt == CbType.Void) return "void";
            if (bt == CbType.Null) return "i8*";
            throw new Exception("unknown basic type");
        }

        public string GetTypeDescr(CFArray bt)
        {
            //return GetTypeDescr(bt.ElementType)+"*";
            CbType e = bt.ElementType;
            if (e == CbType.Char)
            	return "%.arrayChar*";
            if (e == CbType.Int)
            	return "%.arrayInt*";
            return "%.arrayPtr*";
        }

        public string GetTypeDescr(CbClass bt)
        {
            // a typename of the form %classname*
            // Note: it really *should* be %namespace.classname*
            return "%" + bt.Name + "*";
        }

        protected string GetTypeDescr(CbMethod bt)
        {
            // generates the full signature
            // E.g., i32 (%struct.ClassExample*)*
            StringBuilder sb = new StringBuilder();
            sb.Append(GetTypeDescr(bt.ResultType));
            string sep = " (";
            if (!bt.IsStatic)
            {   // the implicit first parameter is 'this'
                sb.Append(" (");
                sb.Append(GetTypeDescr(bt.Owner));
                sep = ", ";
            }
            foreach(var argType in bt.ArgType) {
                sb.Append(sep);
                sb.Append(GetTypeDescr(argType));
                sep = ", ";
            }
            if (sep == ", ")
                sb.Append(")");
            else
                sb.Append(" ()");
            return sb.ToString();
        }

        // The general version of the above methods
        public string GetTypeDescr(CbType t)
        {
            Debug.Assert(t != null);
            if (t is CbBasic) return GetTypeDescr((CbBasic)t);
            if (t is CFArray) return GetTypeDescr((CFArray)t);
            if (t is CbClass) return GetTypeDescr((CbClass)t);
            throw new Exception("invalid call to getTypeDescr");
        }

    }
}