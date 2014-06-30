/*  CbPrVisitor.cs

    Defines a Print Visitor class for the CFlat AST
    
    Author: Nigel Horspool
    
    Dates: 2012-2014
*/

using System;
using System.IO;
using System.Collections.Generic;

namespace FrontEnd {


// Traverses the AST to output a textual representation
// If the Type field of a node is not null, that datatype is included in the output.
public class PrVisitor: Visitor {
	private TextWriter f;    // where to output the tree
	private int indent = 0;  // current indentation level

	// constructor where the output destination can be specified
	public PrVisitor( TextWriter outputStream ) {
		f = outputStream;
		indent = 0;
	}

	// constructor where the tree gets written to standard output
	public PrVisitor() {
		f = Console.Out;
		indent = 0;
	}

    private string indentString( int indent ) {
        return " ".PadRight(2*indent);
    }

    private void printTag( AST node, int cn ) {
        f.Write("{0}{1}: {2}  [line {3}]", indentString(indent), cn, node.Tag, node.LineNumber);
        if (node.Type != null)
            f.Write(", type {0}", node.Type);
    }

	public override void Visit(AST_kary node, object data) {
	    int childNum = (int)data;
        printTag(node, childNum);
        f.WriteLine();
        int arity = node.NumChildren;
        indent++;
        for( int i = 0; i < arity; i++ ) {
        	AST ch = node[i];
            if (ch != null)
                ch.Accept(this, i);
            else
                f.WriteLine("{0}{1}: -- missing child --", indentString(indent), i);
        }
        indent--;
    }

	public override void Visit(AST_leaf node, object data) {
	    int childNum = (int)data;
        printTag(node, childNum);
        switch(node.Tag) {
        case NodeType.Ident:
        case NodeType.StringConst:
        case NodeType.CharConst:
            f.WriteLine(" \"{0}\"", node.Sval);  break;
        case NodeType.IntConst:
            f.WriteLine(" {0}", node.Ival);  break;
        default:
            f.WriteLine();  break;
        }
    }

	public override void Visit( AST_nonleaf node, object data ) {
	    int childNum = (int)data;
        printTag(node, childNum);
        f.WriteLine();
        int arity = node.NumChildren;
        indent++;
        for( int i = 0; i < arity; i++ ) {
        	AST ch = node[i];
            if (ch != null)
                ch.Accept(this, i);
            else
                f.WriteLine("{0}{1}: -- missing child --", indentString(indent), i);
        }
        if (arity == 0)
            f.WriteLine("{0}-- missing children --", indentString(indent));
        indent--;
    }

}

}
