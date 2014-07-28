/* LLVM-Arrays.cs
 * 
 * Methods which generate LLVM code for arrays
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

        // Generates the LLVM type declarations needed for arrays.
        // Only three are needed, one each for element types which are
        // char, int and pointer (which covers strings, classes and arrays).
        //
        // Call this method before generating any code for fields or methods
        // which use arrays.
        public void OutputArrayDefinitions()
        {
			ll.WriteLine("%.arrayChar = type {0} i32, [0 x i8] {1}",
				leftBrace, rightBrace);
			ll.WriteLine("%.arrayInt = type {0} i32, [0 x i32] {1}",
				leftBrace, rightBrace);
			ll.WriteLine("%.arrayPtr = type {0} i32, [0 x i8*] {1}",
				leftBrace, rightBrace);
        }

		// Writes code to instantiate an array
		public LLVMValue WriteNewArray( CbType elemType, LLVMValue size ) {
			string e;
			int esize;
			string structPtr;
			size = ForceIntValue(size);
			if (elemType == CbType.Char) {
				e = "i8";  esize = 1;  structPtr = "%.arrayChar*";
			} else if (elemType == CbType.Int) {
				e = "i32";  esize = 4;  structPtr = "%.arrayInt*";
			} else {
				e = "i8*";  esize = ptrSize;  structPtr = "%.arrayPtr*";
			}
			string abytes;
			if (esize == 1)
				abytes = size.LLValue;
			else {
				abytes = nextTemporary();
				ll.WriteLine("  {0} = mul i32 {1}, {2}",
					abytes, size.LLValue, esize);
			}
			string nbytes = nextTemporary();
			ll.WriteLine("  {0} = add i32 {1}, 4", nbytes, abytes);
			if (ptrSize == 64) {
				string nn = nextTemporary();
				ll.WriteLine("  {0} = zext i32 {1} to i64", nn, nbytes);
				nbytes = nn;
			}
			string rv1 = nextTemporary();
			ll.WriteLine("  {0} = call i8* @malloc(i{1} {2})",
				rv1, ptrSize, nbytes);
			string rv2 = nextTemporary();
			ll.WriteLine("  {0} = bitcast i8* {1} to {2}", rv2, rv1, structPtr);
			string rv3 = nextTemporary();
			ll.WriteLine("  {0} = getelementptr inbounds {1} {2}, i32 0, i32 0",
				rv3, structPtr, rv2);
			ll.WriteLine("  store {0}, i32* {1}, align 4", size, rv3);
			return new LLVMValue(structPtr, rv2, false);
		}

		public LLVMValue ElementReference( CbType elemType,
				LLVMValue arrPtr, LLVMValue index ) {
			//string structPtr;
			string e;
			int esize;
			bool simple = true;
			if (elemType == CbType.Char) {
				e = "i8";  esize = 1;  //structPtr = "%.arrayChar*";
			} else if (elemType == CbType.Int) {
				e = "i32";  esize = 4;  //structPtr = "%.arrayInt*";
			} else {
				e = "i8*";  esize = ptrSize;  //structPtr = "%.arrayPtr*";
				simple = (elemType == CbType.String);
			}
			arrPtr = Dereference(arrPtr);
			string rv1 = nextTemporary();
			ll.WriteLine("  {0} = getelementptr inbounds {1}, i32 0, i32 1",
				rv1, arrPtr);
			index = ForceIntValue(index);
			string rv2 = nextTemporary();
			ll.WriteLine("  {0} = getelementptr inbounds [0 x {1}]* {2}, i32 0, {3}",
				rv2, e, rv1, index);
			if (simple)
				return new LLVMValue(e, rv2, true);
			// need to change LLVM datatype
			string t = GetTypeDescr(elemType);
			string rv3 = nextTemporary();
            ll.WriteLine("  {0} = bitcast i8* {1} to {2}", rv3, rv2, t);
            return new LLVMValue(t, rv3, true);
		}
		
		public LLVMValue ArrayLength( CbType eType, LLVMValue arrPtr ) {
		    arrPtr = Dereference(arrPtr);
			string rv1 = nextTemporary();
			ll.WriteLine("  {0} = getelementptr inbounds {1}, i32 0, i32 0",
				rv1, arrPtr);
			string rv2 = nextTemporary();
            ll.WriteLine("  {0} = load i32* {1}", rv2, rv1);
            return new LLVMValue("i32", rv2, false);
		}
    }
}
