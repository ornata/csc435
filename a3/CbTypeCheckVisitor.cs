namespace FrontEnd {

using System;
using System.Collections.Generic;

public class TypeCheckVisitor : Visitor {
    private NameSpace CurrentNameSpace;

    public TypeCheckVisitor(NameSpace currentNameSpace) {
        CurrentNameSpace = currentNameSpace;
    }

    public override void Visit(AST_kary node, object data) {
        for (int i = 0; i < node.NumChildren; i++) {
            if (node[i] != null) {
                node[i].Accept(this, data);
            }
        }
    }

    public override void Visit(AST_nonleaf node, object data) {
        if (node.Tag == NodeType.Class) {
            CbClass clazz = (CbClass) node.Type;

            for (int i = 0; i < node.NumChildren; i++) {
                if (node[i] != null) {
                    node[i].Accept(this, clazz);
                }
            }
        }
        else if (node.Tag == NodeType.Const) {
            CbClass clazz = (CbClass) data;
            
            string name = (node[1] as AST_leaf).Sval;

            CbConst conzt = clazz.Members[name] as CbConst;

            // get the type of the const
            if (node[0] == null) {
                conzt.Type = CbType.Void;
            } else {
                node[0].Accept(this, clazz);
                conzt.Type = node[0].Type;
            }
        }
        else if (node.Tag == NodeType.Field) {
            CbClass clazz = (CbClass) data;

            // get the type of the field
            CbType fieldtype;
            if (node[0] == null) {
                fieldtype = CbType.Void;
            } else {
                node[0].Accept(this, clazz);
                fieldtype = node[0].Type;
            }

            // apply the type to all ids declared with it
            AST_kary idList = node[1] as AST_kary;
            for (int i = 0; i < idList.NumChildren; i++) {
                string name = (idList[i] as AST_leaf).Sval;
                CbField field = clazz.Members[name] as CbField;
                field.Type = fieldtype;
            }
        }
        else if (node.Tag == NodeType.Method) {
            CbClass clazz = (CbClass) data;

            string name = (node[1] as AST_leaf).Sval;

            CbMethod method = clazz.Members[name] as CbMethod;

            // get the method's return type
            if (node[0] == null) {
                method.ResultType = CbType.Void;
            } else {
                node[0].Accept(this, clazz);
                method.ResultType = node[0].Type;
            }

            // Visit list of formals
            node[2].Accept(this, clazz);
            
            // add formals to CbMethod information
            List<CbType> formalTypes = new List<CbType>();
            for (int i = 0; i < node[2].NumChildren; i++) {
                formalTypes.Add(node[2][i].Type);
            }
            method.ArgType = formalTypes;

            // Visit method body
            node[3].Accept(this, clazz);
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
        if (node.Tag == NodeType.IntType) {
            node.Type = CbType.Int;
        }
        else if (node.Tag == NodeType.CharType) {
            node.Type = CbType.Char;
        }
        else if (node.Tag == NodeType.StringType) {
            node.Type = CbType.String;
        }
        else if (node.Tag == NodeType.VoidType) {
            node.Type = CbType.Void;
        }
        else if (node.Tag == NodeType.Ident) {
            object ns_obj = CurrentNameSpace.LookUp(node.Sval);
            node.Type = ns_obj as CbClass;
        }
    }
}

}
