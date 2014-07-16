/* LLVM-WriteMethods.cs
 * 
 * Utility code to help with outputting intermediate code in the
 * LLVM text format (as a '.ll' file).
 *
 * These are the simpler utility methods
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

        // generate code for the start of method m in class c
        // methodDecl references the AST node with tag MethodDecl where the method is described
        public void WriteMethodStart( CbClass c, CbMethod m, AST methodDecl ) {
            SSANumbering.Clear();
            if (m.Name == "Main" && m.IsStatic)
                ll.Write("\ndefine void @main ");
            else
                ll.Write("\ndefine {0} @{1}.{2} ",
                    GetTypeDescr(m.ResultType), c.Name, m.Name);
            char sep;
            if (m.IsStatic) {
                sep = '(';
            } else {
                // provide 'this' pointer as first argument
                ll.Write("({0} %this", GetTypeDescr(c));
                sep = ',';
            }
            if (methodDecl.Tag != NodeType.Method)
                throw new Exception("bad call to WriteMethodStart");
            AST_kary formals = methodDecl[2] as AST_kary;
            if (formals == null || formals.NumChildren != m.ArgType.Count)
                throw new Exception("bad AST structure");
            for( int i=0; i<m.ArgType.Count; i++ ) {
                AST_nonleaf formal = formals[i] as AST_nonleaf;
                AST_leaf idNode = formal[1] as AST_leaf;
                string id = idNode.Sval;
                ll.Write("{0} {1} %{2}", sep, GetTypeDescr(m.ArgType[i]), id);
                sep = ',';
            }
            if (sep == '(')
                ll.WriteLine("() {0} {{", macOS? "nounwind uwtable ssp" : "#0" );
            else
                ll.WriteLine(" ) {0} {{", macOS ? "nounwind uwtable ssp" : "#0");
            nextBBNumber = 0;
            nextUnnamedIndex = 0;
        }

        // Generate code for the end of a method, plus definitions for any
        // anonymous string constants needed which generating code for the
        // method body
        public void WriteMethodEnd(CbMethod currentMethod) {
            // make sure that all code paths return a result!
            if (currentMethod.ResultType != CbType.Void) {
                if (currentMethod.ResultType == CbType.Int)
                    ll.WriteLine("  ret i32 0");
                else if (currentMethod.ResultType == CbType.Char)
                    ll.WriteLine("  ret i8 0");
                else
                    ll.WriteLine("  ret {0} null", GetTypeDescr(currentMethod.ResultType));
            } else {
                ll.WriteLine("  ret void");
            }
            ll.WriteLine("}\n");
            if (stringConstantDefs.Count > 0)
            {
                ll.WriteLine();
                foreach (string s in stringConstantDefs)
                {
                    ll.WriteLine(s);
                }
                stringConstantDefs.Clear();
            }
        }

        // generate code to instantiate a class instance (aka the new operator);
        // the result is the name of the temporary (e.g. %37) which holds the
        // reference to the new instance on the heap and its type, as a pair
        public LLVMValue NewClassInstance( CbClass t ) {
            string instanceType = GetTypeDescr(t);
            ll.WriteLine("  %{0} = getelementptr inbounds {1} null, i32 0, i32 {2}",
                nextUnnamedIndex, instanceType, t.LastIndex);
            ll.WriteLine("  %{0} = ptrtoint [0 x i32]* %{1} to i32",
                nextUnnamedIndex+1, nextUnnamedIndex);
            ll.WriteLine("  %{0} = call i8* @malloc(i32 %{1})",
                nextUnnamedIndex+2, nextUnnamedIndex+1);
            // clear the allocated storage to 0
            ll.WriteLine("  call void @llvm.memset.p0i8.i32(i8* %{0}, i8 0, i32 %{1}, i32 0, i1 0)",
                nextUnnamedIndex+2, nextUnnamedIndex+1);
            // convert from i8* to the proper class instance pointer type
            ll.WriteLine("  %{0} = bitcast i8* %{1} to {2}",
                nextUnnamedIndex+3, nextUnnamedIndex+2, GetTypeDescr(t));
            string r = "%" + (nextUnnamedIndex+3);  // eventually the final result!!

            // store the class name in the first field of the instance
            ll.WriteLine("  ;  store the class name in field #0 of the instance");
            ll.WriteLine("  %{0} = getelementptr inbounds {1} %{2}, i32 0, i32 0",
                nextUnnamedIndex+4, instanceType, nextUnnamedIndex+3);
            ll.WriteLine("  store i8* getelementptr inbounds ([{0} x i8]* @{1}..Name, i32 0, i32 0), i8** %{2}, align {3}",
                t.Name.Length+1, t.Name, nextUnnamedIndex+4, ptrAlign);
            // store the VTable address in the second field
            ll.WriteLine("  ;  store the VTable address in field #1 of the instance");
            ll.WriteLine("  %{0} = getelementptr inbounds {1} %{2}, i32 0, i32 1",
                nextUnnamedIndex+5, instanceType, nextUnnamedIndex+3);
            ll.WriteLine("  store %{0}..VTableType* @{0}..VTable, %{0}..VTableType** %{1}, align {2}",
                t.Name, nextUnnamedIndex+5, ptrAlign);

            nextUnnamedIndex += 6;
            return new LLVMValue(instanceType,r,false);
        }
        
        // generate code to call a virtual method m in class c
        // thisPtr is a value such as "%3" specifying where the instance pointer is held,
        // and args is an array of LLVM values to use as arguments in the method call;
        // the result is the LLVM value which holds the result returned by m,
        // or null if m is void
        public LLVMValue CallVirtualMethod( CbMethod m, LLVMValue thisPtr, LLVMValue[] args ) {
            if (args.Length != m.ArgType.Count)
                throw new Exception("invalid call to CallVirtualMethod");
            CbClass c = m.Owner;
            LLVMValue result = null;
            // load VTable pointer
            ll.WriteLine("  %{0} = getelementptr inbounds {1}, i32 0, i32 1",
                nextUnnamedIndex, thisPtr);
            ll.WriteLine("  %{0} = load %{1}..VTableType** %{2}, align {3}",
                nextUnnamedIndex+1, c.Name, nextUnnamedIndex, ptrAlign);
            // load method address from VTable
            ll.WriteLine("  %{0} = getelementptr inbounds %{1}..VTableType* %{2}, i32 0, i32 {3}",
                nextUnnamedIndex+2, c.Name, nextUnnamedIndex+1, m.VTableIndex);
            ll.WriteLine("  %{0} = load {1}** %{2}, align {3}",
                nextUnnamedIndex+3, GetTypeDescr(m), nextUnnamedIndex+2, ptrAlign);
            int callReg = nextUnnamedIndex+3;
            nextUnnamedIndex += 4;
            string rt = GetTypeDescr(m.ResultType);
            if (m.ResultType != CbType.Void) {
                string rv = "%" + nextUnnamedIndex++;
                ll.Write("  {0} =", rv);
                result = new LLVMValue(rt,rv,false);
            }
            ll.Write("  call {0} %{1} ({2}", rt, callReg, thisPtr);
            for( int i=0; i<args.Length; i++ ) {
                ll.Write(", {0}", args[i]);
            }
            ll.WriteLine(")");
            return result;
        }

       // generate code to call a static method m in class c;
        // args is an array of LLVM values to use as arguments in the method call;
        // the result is the LLVM value & type which holds the result returned by m,
        // or null if m is void
        public LLVMValue CallStaticMethod( CbMethod m, LLVMValue[] args ) {
            if (args.Length != m.ArgType.Count)
                throw new Exception("invalid call to CallVirtualMethod");
            CbClass c = m.Owner;
            string rt = GetTypeDescr(m.ResultType);
            LLVMValue result = null;
            if (m.ResultType != CbType.Void) {
                string rv = nextTemporary();
                ll.Write("{0} = ", rv);
                result = new LLVMValue(rt,rv,false);
            }
            ll.Write("  call {0} @{1}.{2} ", rt, c.Name, m.Name);
            char sep = '(';
            for( int i=0; i<args.Length; i++ ) {
                ll.Write("{0} {1}", sep, args[i]);
                sep = ',';
            }
            if (sep == ',')
                ll.WriteLine(")");
            else
                ll.WriteLine("()");
            return result;
        }

        // Used to generate calls for System.Console methods: Write, WriteLine, ReadLine
        public LLVMValue CallBuiltInMethod( CbType resultType, string name, LLVMValue arg ) {
            LLVMValue result = null;
            string rt = GetTypeDescr(resultType);
            if (resultType != CbType.Void) {
                string rv = nextTemporary();
                ll.Write("{0} = ", rv);
                result = new LLVMValue(rt,rv,false);
            }
            if (arg == null)
                ll.WriteLine("  call {0} {1} ()", rt, name);
            else
                ll.WriteLine("  call {0} {1} ({2})", rt, name, arg);
            return result;  
        }

        public void AllocLocalVar( string name, CbType type ) {
            int align = 4;
            if (type == CbType.Char) align = 1;
            ll.WriteLine("  %{0}.addr = alloca {1}, align {2}",
                name, GetTypeDescr(type), align);
        }
        
        // Returns the name of the LLVM temporary being used for a
        // local variable -- SSA number ing is used
        public LLVMValue RefLocalVar( string name, CbType type ) {
            int num = 0;
            if (!SSANumbering.TryGetValue(name, out num) || num == 0)
            {
                SSANumbering[name] = 0;
                return new LLVMValue(GetTypeDescr(type), "%" + name, false);
            }
            return new LLVMValue(GetTypeDescr(type), "%" + name + "." + num, false);
        }

        // Generates access to a constant -- either the value (for int,char)
        // or a reference for a string value
        public LLVMValue AccessClassConstant( CbConst cnst ) {
            string fullName = String.Format("@{0}.{1}", cnst.Owner.Name, cnst.Name);
            string rv = nextTemporary();
            if (cnst.Type == CbType.Int)
            {
                ll.WriteLine("  {0} = load i32* {1}", rv, fullName);
                return new LLVMValue("i32", rv, false);
            }
            if (cnst.Type == CbType.Char)
            {
                ll.WriteLine("  {0} = load i8* {1}", rv, fullName);
                return new LLVMValue("i8", rv, false);
            }
            ll.WriteLine("  {0} = load i8** {1}", rv, fullName);
            return new LLVMValue("i8*", rv, false);
        }

        // Generates code to coerce int to char, char to int, or
        // any class type to any other class type
        public LLVMValue Coerce( LLVMValue src, CbType srcType, CbType destType ) {
            src = Dereference(src);
            if (srcType == destType)
                return src;
            string rv = nextTemporary();
            if (destType == CbType.Int) {
                // widen from char to int
                ll.WriteLine("  {0} = zext {1} to i32", rv, src);
                return new LLVMValue("i32", rv, false);   
            }
            if (destType == CbType.Char) {
                // narrow from int to to char
                ll.WriteLine("  {0} = trunc {1} to i8", rv, src);
                return new LLVMValue("i8", rv, false);
            }
            if (destType is CbClass) {
                string t = GetTypeDescr(destType);
                ll.WriteLine("  {0} = bitcast {1} to {2}", rv, src, t);
                return new LLVMValue(t, rv, false);
            }
            throw new Exception("bad call to llvm.Coerce");
        }


    }
}