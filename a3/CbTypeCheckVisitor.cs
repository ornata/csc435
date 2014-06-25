namespace FrontEnd {

using System;

public class TypeCheckVisitor : Visitor {
    private SymTab Symbols = new SymTab();
    
    private CbType CurrentReturnType;

    public override void Visit(AST_kary node, object data) {
        for (int i = 0; i < node.NumChildren; i++) {
            if (node[i] != null) {
                node[i].Accept(this, data);
            }
        }
    }

    public override void Visit(AST_nonleaf node, object data) {
        if (node.Tag == NodeType.Method) {
            Console.WriteLine("Entering method");

            Symbols.Enter();

            CurrentReturnType = node[0] == null ? CbType.Void
                                                : node[0].Type;

            // Visit list of formals
            node[2].Accept(this, data);

            // Visit method body
            node[3].Accept(this, data);

            Symbols.Exit();
        }
        else
        {
            for (int i = 0; i < node.NumChildren; i++) {
                if (node[i] != null) {
                    node[i].Accept(this, data);
                }
            }
        }
    }

    public override void Visit(AST_leaf node, object data) {
    }
}

}
