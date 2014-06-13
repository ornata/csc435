using System;
using System.IO;
using System.Collections.Generic;

namespace FrontEnd {

public class TopLevelVisitor : Visitor {
	public override void Visit(AST_kary node, object data) {
		Console.WriteLine("blah1");
    }

	public override void Visit(AST_leaf node, object data) {
		Console.WriteLine("blah2");
    }

	public override void Visit(AST_nonleaf node, object data) {
		Console.WriteLine("blah3");
    }
}

}
