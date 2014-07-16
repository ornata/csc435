/*  CbVisitor.cs

    Defines an Abstract Visitor class for the CFlat AST
    
    Author: Nigel Horspool
    
    Date: October 2012
*/

using System;
using System.IO;
using System.Collections.Generic;

namespace FrontEnd {

public abstract class Visitor {
	public virtual void Visit(AST_kary n, object data) {
		Console.WriteLine("Internal compiler error! This method should have been overridden");
	}

	public virtual void Visit(AST_leaf n, object data) {
		Console.WriteLine("Internal compiler error! This method should have been overridden");
	}

	public virtual void Visit(AST_nonleaf n, object data) {
		Console.WriteLine("Internal compiler error! This method should have been overridden");
	}	
}

}
