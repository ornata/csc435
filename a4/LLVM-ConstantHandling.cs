/* LLVM-ConstantHandling.cs
 * 
 * Methods which handle constants in the Cb program
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
        
    public partial class LLVM
    {
        int stringConstNum = 0;     // used to number anonymous string constants


        public void OutputConstDefn( CbConst cnst, AST_leaf initVal ) {
            CbClass c = cnst.Owner;
            CbType cnstType = cnst.Type;
            CbType initType = initVal.Type;
            // Cb consts can only have int, char or string types
            if (cnstType == CbType.Int) {
                ll.WriteLine("@{0}.{1} = global i32 {2}, align 4",
                    c.Name, cnst.Name, GetIntVal(initVal).LLValue);
            } else if (cnstType == CbType.Char) {
                ll.WriteLine("@{0}.{1} = global i8 {2}, align 4",
                    c.Name, cnst.Name, GetIntVal(initVal).LLValue);
            } else { // it's a string
                byte[] bytes = convertString(initVal.Sval);
                string name = "@" + c.Name + "." + cnst.Name;
                CreateStringConstant(name, bytes);
            }
        }

        byte[] convertString( string s ) {
            List<byte> r = new List<byte>();
            int len = s.Length-1;
            Debug.Assert(len >= 1 && s[0] == '"' && s[len] == '"');
            int ix = 1;
            while(ix < len) {
                char c = s[ix++];
                if (c == '\\') {
                    Debug.Assert(ix < len);
                    c = s[ix++];
                    switch(c) {
                    case 'r':
                        r.Add((byte)13);
                        continue;
                    case 'n':
                        r.Add((byte)10);
                        continue;
                    case 't':
                        r.Add((byte)9);
                        continue;
                    }
                    // drop through for the \' and \" cases
                }
                r.Add((byte)c);
            }
            r.Add((byte)0);  // C string terminator
            return r.ToArray();
        }

        // string constant definitions are collected here until the current method
        // has been completed, and then they are output
        List<string> stringConstantDefs = new List<string>();

        public void CreateStringConstant(string name, byte[] chars)
        {
            string sname = "@.str" + stringConstNum;
            stringConstNum++;
            ll.WriteLine(createStringConstDefn(sname,chars));
            ll.WriteLine("{0} = global i8* getelementptr inbounds ([{1} x i8]* {2}, i32 0, i32 0), align {3}",
                name, chars.Length, sname, ptrAlign);
        }

        string createStringConstDefn(string name, byte[] chars)
        {
            string type = String.Format("[{0} x i8]", chars.Length);
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} = private unnamed_addr constant {1} c\"", name, type);
            foreach (byte b in chars)
            {
                if (b < (byte)20 || b == (byte)34 || b == (byte)92 || b >= (byte)127)
                    // use hex for special chars or doublequote or backslash
                    sb.AppendFormat("\\{0:X2}", b);
                else
                    sb.Append((char)b);
            }
            sb.Append("\", align 1");
            return sb.ToString();
        }

        // creates a new string constant; it returns the name and type
        public LLVMValue WriteStringConstant( byte[] chars ) {
            string name = "@.str"+stringConstNum;
            stringConstNum++;
            string ss = createStringConstDefn(name, chars);
            stringConstantDefs.Add(ss);
            string rv = nextTemporary();
            ll.WriteLine("  {0} = getelementptr inbounds [{1} x i8]* {2}, i32 0, i32 0",
                rv, chars.Length, name);
            return new LLVMValue("i8*", rv, false);
        }

        // creates a new string constant; it returns the name and type
        public LLVMValue WriteStringConstant( AST_leaf node ) {
            byte[] bb = convertString(node.Sval);
            return WriteStringConstant(bb);
        }

        // given a node which is either IntConst or CharConst, return its int value
        public LLVMValue GetIntVal( AST_leaf n ) {
            if (n.Tag == NodeType.IntConst)
                return new LLVMValue("i32", n.Ival.ToString(), false);
            Debug.Assert(n.Tag == NodeType.CharConst);
            string s = n.Sval;
            int len = s.Length;
            int val;
            Debug.Assert(len >= 3 && s[0] == '\'' && s[len-1] == '\'');
            if (len == 3)
                val = (int)s[1];
            else {
                Debug.Assert(len == 4 && s[1] == '\\');
                switch(s[2]) {
                case 'r':   val = 13; break;
                case 'n':   val = 10; break;
                case 't':   val = 9;  break;
                default:    val = (int)s[2]; break;
                }
            }
            return new LLVMValue("i8", val.ToString(), false);
        }
    }
}