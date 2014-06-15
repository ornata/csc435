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
            CbConst c = new CbConst(name, node[0] == null ? CbType.Void : node[0].Type);

            if (CurrentClass.AddMember(c) == false) {
                Console.WriteLine("Error: Multiple member declarations of {0} in class {1} " +
                                  "(subsequent definition at {2}",
                                  c.Name,
                                  CurrentClass.Name,
                                  node.LineNumber);
            }
        }
        else if (node.Tag == NodeType.Field) {
            CbType type = node[0] == null ? CbType.Void : node[0].Type;
            AST_kary identList = node[1] as AST_kary;
            for (int i = 0; i < identList.NumChildren; i++) {
                string name = (identList[i] as AST_leaf).Sval;
                CbField f = new CbField(name, type);

                if (CurrentClass.AddMember(f) == false) {
                    Console.WriteLine("Error: Multiple member declarations of {0} in class {1} " +
                                      "(subsequent definition at {2}",
                                      f.Name,
                                      CurrentClass.Name,
                                      node.LineNumber);
                }
            }
        }
        else if (node.Tag == NodeType.Method) {
            string name = (node[1] as AST_leaf).Sval;

            bool isStatic = node[4].Tag == NodeType.Static;

            CbType rt = node[0] == null ? CbType.Void : node[0].Type;

            AST_kary formals = node[2] as AST_kary;

            IList<CbType> argType = new List<CbType>();
            for (int i = 0; i < formals.NumChildren; i++) {
                argType.Add(formals[i] == null ? CbType.Void :formals[i].Type);
            }

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

        if (node.Tag == NodeType.Class) {
            CurrentClass = null;
        }
    }
}

}
