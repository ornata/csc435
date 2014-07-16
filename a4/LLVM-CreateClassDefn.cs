/* LLVM-CreateClassDefn.cs
 * 
 * Methods which generate LLVM code for a class definition
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

        // Generates the LLVM type declarations needed for heap instances of
        // a Cb class, and constructs the VTable for that class.
        // Each class instance holds a reference to its name (a string) in the
        // first field, then a reference to its vtable, followed by fields for
        // each field of the class (including its parent class's fiulds)
        public void OutputClassDefinition(CbClass t)
        {
            IList<CbMethod> vtEntries = new List<CbMethod>();
            IList<CbField> fields = new List<CbField>();
            addMembers(vtEntries, fields, t);

            // Define the type of this class's VTable
            ll.Write("  %{0}..VTableType = type ", t.Name);
            char sep = '{';
            foreach( var entry in vtEntries ) {
                ll.Write("{0} {1}*", sep, GetTypeDescr(entry));
                sep = ',';
            }
            if (sep == ',')
                ll.WriteLine(" {0}", rightBrace);
            else
                ll.WriteLine("{0} i64* {1}", leftBrace, rightBrace);
            // Now declare the VTable itself
            ll.Write("  @{0}..VTable = global %{0}..VTableType ", t.Name);
            sep = '{';
            foreach( var entry in vtEntries ) {
                ll.Write("{0} {1}* @{2}.{3}",
                    sep, GetTypeDescr(entry), entry.Owner.Name, entry.Name);
                sep = ',';
            }
            if (sep == ',')
                ll.WriteLine(" {0}, align {1}", rightBrace, ptrAlign);
            else
                ll.WriteLine("  {0} i64* null {1}, align {2}", leftBrace, rightBrace, ptrAlign);

            // instances begin with class name pointer and vtable pointer
            ll.Write("  %{0} = type {1} i8*, %{0}..VTableType*", t.Name, leftBrace);
            int index = 2;
            foreach( CbField mm in fields ) {
                mm.Index = index++;
                ll.Write(", " + GetTypeDescr(mm.Type));
            }
            // Note: there is a fictitious int array with 0 elements at the end.
            // The byte offset of that field gives us the size of the instance. 
            ll.WriteLine(", [0 x i32] {0}", rightBrace);
            t.LastIndex = index;
            ll.WriteLine("  @{0}..Name = private unnamed_addr constant [{1} x i8] c\"{2}\\00\", align 1",
                t.Name, t.Name.Length+1, t.Name);
        }

        // creates list of (virtual) methods to go into a class's VTable and fields
        // to go into a class instance;
        // each method is annotated with its index into the VTable
        private void addMembers(IList<CbMethod> list, IList<CbField> fields, CbClass t) {
            if (t == null) return;
            addMembers(list, fields, t.Parent);  // parent's methods & fields go in first!
            foreach( var m in t.Members.Values ) {
                CbField field = m as CbField;
                if (field != null) {
                    fields.Add(field);
                    continue;
                }
                CbMethod meth = m as CbMethod;
                if (meth == null || meth.IsStatic) continue;
                bool isOverride = false;
                for( int i=0; i < list.Count; i++ ) {
                    CbMethod c = list[i];
                    if (c.Name == meth.Name) {
                        // it's an override
                        meth.VTableIndex = i;
                        list[i] = meth;
                        isOverride = true;
                        break;
                    }
                }
                if (!isOverride) {
                    meth.VTableIndex = list.Count;
                    list.Add(meth);
                }
            }
        }

        public LLVMValue CreateThisPointer( CbClass c ) {
            return new LLVMValue(GetTypeDescr(c), "%this", false);
        }
    }
}
