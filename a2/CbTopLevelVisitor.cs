using System;
using System.IO;
using System.Collections.Generic;

namespace FrontEnd {

public class TopLevelVisitor : Visitor {
    private bool InUsingList = false;
    private CbClass CurrentClass = null;

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

    private static CbType gettype(AST ast) {
        // TODO
        return null;
    }

    public override void Visit(AST_nonleaf node, object data) {
        if (node.Tag == NodeType.Class) {
            NameSpace top = NameSpace.TopLevelNames;
            // create class descriptor
            CbClass parent = null;
            if (node[1] != null) {
                parent = (CbClass) top.LookUp((node[1] as AST_leaf).Sval);
            }

            CbClass clazz = new CbClass((node[0] as AST_leaf).Sval, parent);

            // add it to top-level namespace and check for conflicts
            if (top.AddMember(clazz) == false) {
                Console.WriteLine("Error: Multiple definitions for class {0} " +
                                  "(subsequent definition at {1}",
                                  clazz.Name, node.LineNumber);
            }

            CurrentClass = clazz;
        }
        else if (node.Tag == NodeType.Const) {
            string name = (node[1] as AST_leaf).Sval;
            CbType type = gettype(node[0]);
            CbConst c = new CbConst(name, type);

            if (CurrentClass.AddMember(c) == false) {
                Console.WriteLine("Error: Multiple member declarations of {0} in class {1} " +
                                  "(subsequent definition at {2}",
                                  c.Name,
                                  CurrentClass.Name,
                                  node.LineNumber);
            }
        }
        else if (node.Tag == NodeType.Field) {
            // TODO: iterate over all names in the IdentList
            string name = "temp";
            CbType type = gettype(node[0]);
            CbField f = new CbField(name, type);

            if (CurrentClass.AddMember(f) == false) {
                Console.WriteLine("Error: Multiple member declarations of {0} in class {1} " +
                                  "(subsequent definition at {2}",
                                  f.Name,
                                  CurrentClass.Name,
                                  node.LineNumber);
            }
        }
        else if (node.Tag == NodeType.Method) {
            // TODO: Get all info
            string name = "temp";
            bool isStatic = false;
            CbType rt = null;
            IList<CbType> argType = null;

            CbMethod m = new CbMethod(name, isStatic, rt, argType);

            if (CurrentClass.AddMember(m) == false) {
                Console.WriteLine("Error: Multiple member declarations of {0} in class {1} " +
                                  "(subsequent definition at {2}",
                                  m.Name,
                                  CurrentClass.Name,
                                  node.LineNumber);
            }
        }

        for (int i = 0; i < node.NumChildren; i++) {
            if (node[i] != null) {
                node[i].Accept(this, i);
            }
        }
    }
}

}
