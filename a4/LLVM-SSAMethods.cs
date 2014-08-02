/* LLVM-SSAMethods.cs
 * 
 * Methods which support the SSA scheme in LLVM code
 * 
 * Author: Nigel Horspool
 * Date: July 2014
 */
 
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;


namespace FrontEnd
{
        
    public partial class LLVM
    {

        // shows which integer was appended to a name most recently
        // to make the name unique
        IDictionary<string, int> SSANumbering = new Dictionary<string,int>();

        // Creates a new unique name for a local variable
        public string CreateSSAName( string baseName ) {
            string result;
            int num = 0;
            SSANumbering.TryGetValue(baseName, out num);
            num++;
            result = baseName + "." + num;
            SSANumbering[baseName] = num;
            return result;
        }

        // Given identification of a Cb method's local variable in the symbol table,
        // return the LLVM temporary which holds its latest value
        public LLVMValue AccessLocalVariable( SymTabEntry local ) {
            return new LLVMValue(GetTypeDescr(local.Type), local.SSAName, false);
        }

        // Merges two symbol tables at a join point in the code
        // Each symbol table contains the latest version used for each variable
        // in the SSA naming scheme
        // Phi instructions are generated when two different names must be combined
        public SymTab Join(string pred1, SymTab tab1, string pred2, SymTab tab2)
        {
            IEnumerator<SymTabEntry> list1 = tab1.GetEnumerator();
            IEnumerator<SymTabEntry> list2 = tab2.GetEnumerator();
            SymTab result = new SymTab();
            for ( ; ; )
            {
                bool b1 = list1.MoveNext();
                bool b2 = list2.MoveNext();
                if (!b1 && !b2)
                    break;
                if (b1 && b2)
                {
                    SymTabEntry e1 = list1.Current;
                    SymTabEntry e2 = list2.Current;
                    if (e1 == null && e2 == null)
                    {   // we hit a scope marker
                        result.Enter();
                        continue;
                    }
                    Debug.Assert(e1 != null && e2 != null && e1.Name == e2.Name);
                    SymTabEntry ne = result.Binding(e1.Name, e1.DeclLineNo);
                    ne.Type = e1.Type;
                    if (e1.SSAName == e2.SSAName)
                    {
                        ne.SSAName = e1.SSAName;
                    }
                    else
                    {
                        string newName = "%" + CreateSSAName(e1.Name);
                        ll.WriteLine("  {0} = phi {1} [{2}, %{3}], [{4}, %{5}]",
                            newName, GetTypeDescr(e1.Type), e1.SSAName, pred1, e2.SSAName, pred2);
                        ne.SSAName = newName;
                    }
                }
                else
                    throw new Exception("Attempt to join inconsistent symbol tables");
            }
            return result;
        }


        public void InsertLoopCode(string code, SymTab syAtTop, SymTab syAtBot ) {
            // This is inefficient because we are making multiple passes over the code,
            // effectively once for each phi function at the top of the while loop
            IEnumerator<SymTabEntry> list1 = syAtTop.GetEnumerator();
            IEnumerator<SymTabEntry> list2 = syAtBot.GetEnumerator();
            for ( ; ; )
            {
                bool b1 = list1.MoveNext();
                bool b2 = list2.MoveNext();
                if (!b1 && !b2)
                    break;
                if (b1 && b2)
                {
                    SymTabEntry e1 = list1.Current;
                    SymTabEntry e2 = list2.Current;
                    if (e1 == e2 || e1.SSAName == e2.SSAName)
                        continue;
                    code = code.Replace(e1.SSAName,e2.SSAName);
                } else
                    throw new Exception("Inconsistent symbol tables");
            }
            this.InsertCode(code);
        }

    }
}
