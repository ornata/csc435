using System;
using System.IO;
using System.Collections.Generic;

namespace FrontEnd {

public class TopLevelVisitor : Visitor {
    private bool InUsingList = false;

    public override void Visit(AST_kary node, object data) {
        if (node.Tag == NodeType.UsingList) {
            InUsingList = true;
        }

        Console.WriteLine("blah1");
        for (int i = 0; i < node.NumChildren; i++) {
            if (node[i] != null) {
                node[i].Accept(this, i);
            }
        }

        if (node.Tag == NodeType.UsingList) {
            InUsingList = false;
        }
    }

    public override void Visit(AST_leaf node, object data) {
        if (InUsingList) {
            if (node.Sval == "System") {
                NameSpace top = NameSpace.TopLevelNames;
                NameSpace sys = (NameSpace) top.LookUp("System");

                CbClass obj = (CbClass) sys.LookUp("Object");
                top.AddMember(obj);

                CbClass st = (CbClass) sys.LookUp("String");
                top.AddMember(st);
                
                CbClass console = (CbClass) sys.LookUp("Console");
                top.AddMember(console);

                CbClass i32 = (CbClass) sys.LookUp("Int32");
                top.AddMember(i32);
            }
        }
    }

    public override void Visit(AST_nonleaf node, object data) {
        Console.WriteLine("blah3");
        for (int i = 0; i < node.NumChildren; i++) {
            if (node[i] != null) {
                node[i].Accept(this, i);
            }
        }
    }
}

}
