/*  CbTypeCheckVisitor1.cs

    First stage of full type-checking on AST

    In this traversal, we fill in the type details of class members.
    We do not visit (and therefore do not typecheck) the values of
    const declarations or the bodies of methods.
    
    Author: Nigel Horspool
    
    Dates: 2012-2014
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace FrontEnd {


public class TypeCheckVisitor1: Visitor {
    NameSpace ns;

    // constructor
    public TypeCheckVisitor1( ) {
        ns = NameSpace.TopLevelNames;  // get the top-level namespace
    }

	public override void Visit(AST_kary node, object data) {
        switch(node.Tag) {
        case NodeType.ClassList:
            // visit each class declared in the program
            for(int i=0; i<node.NumChildren; i++) {
                node[i].Accept(this, data);
            }
            break;
        case NodeType.MemberList:
            // visit each member of the current class
            for(int i=0; i<node.NumChildren; i++) {
                node[i].Accept(this, data);
            }
            break;
        default:
            throw new Exception("Unexpected tag: "+node.Tag);
        }
    }

	public override void Visit( AST_nonleaf node, object data ) {
	    CbClass classTypeDefn = data as CbClass;
        switch(node.Tag) {
        case NodeType.Program:
            node[1].Accept(this, null);  // visit class declarations
            break;
        case NodeType.Class:
            // find the (incomplete) type definition for the class
            AST_leaf classNameId = node[0] as AST_leaf;
            string className = classNameId.Sval;
            classTypeDefn = ns.LookUp(className) as CbClass;
            Debug.Assert(classTypeDefn != null);
            // now check the class's members, passing the class defn
            AST_kary memberList = node[2] as AST_kary;
            for(int i=0; i<memberList.NumChildren; i++) {
                memberList[i].Accept(this,classTypeDefn);
            }
            break;
        case NodeType.Const:
	        Debug.Assert(data != null && data is CbClass);
            // add const name to current class
            string cname = ((AST_leaf)(node[1])).Sval;
            // obtain the const definition description
            CbConst cdef = classTypeDefn.Members[cname] as CbConst;
            // visit the first subtree (the type)
            node[0].Accept(this,classTypeDefn);
            // after visiting it, it has a type
            cdef.Type = node[0].Type;
            node[1].Type = node[0].Type;
            // done ... we will type check the const's value later
            break;
        case NodeType.Field:
	        Debug.Assert(data != null && data is CbClass);
            // visit the first subtree (the type)
            node[0].Accept(this,classTypeDefn);
            CbType t = node[0].Type;
            // access each field in the list
            AST_kary fields = (AST_kary)(node[1]);
            for(int i=0; i<fields.NumChildren; i++) {
                string fieldname = ((AST_leaf)fields[i]).Sval;
                CbField fdef = classTypeDefn.Members[fieldname] as CbField;
                fdef.Type = t;
                // annote the field name with its type
                fields[i].Type = t;
            }
            break;
        case NodeType.Method:
	        Debug.Assert(data != null && data is CbClass);
	        CbType rt;
            if (node[0] == null)
                rt = CbType.Void;
            else {
                // visit the first subtree (the result type)
                node[0].Accept(this,classTypeDefn);
                rt = node[0].Type;
            }      
            // add method name to current class
            string methname = ((AST_leaf)(node[1])).Sval;
            CbMethod mdef = classTypeDefn.Members[methname] as CbMethod;
            mdef.ResultType = rt;
            // initialize the list of argument types for the method
            mdef.ArgType = new List<CbType>();
            // access each formal parameter to get its type
            AST_kary formals = (AST_kary)node[2];
            for(int i=0; i<formals.NumChildren; i++) {
                AST_nonleaf formal = (AST_nonleaf)formals[i];
                // visit the formal's type
                formal[0].Accept(this,data);
                // add this type to the list of method's argument types
                mdef.ArgType.Add(formal[0].Type);
                // annotate the formal parameter name with its type
                formal[1].Type = formal[0].Type;
            }
            break;
        case NodeType.Array:
	        Debug.Assert(data != null && data is CbClass);       
	        // visit the child to get the element type
	        node[0].Accept(this,classTypeDefn);
	        // now create an array type from that element type
	        CbType at = CbType.Array(node[0].Type);
	        node.Type = at;
	        break; 
        default:
            throw new Exception("Unexpected tag: "+node.Tag);
        }
    }

	public override void Visit(AST_leaf node, object data) {
	    CbClass classTypeDefn = data as CbClass;
        switch(node.Tag) {
        case NodeType.IntType:
            node.Type = CbType.Int;
            break;
        case NodeType.StringType:
            node.Type = CbType.String;
            break;
        case NodeType.CharType:
            node.Type = CbType.Char;
            break;
        case NodeType.VoidType:
            node.Type = CbType.Void;
            break;
        case NodeType.Ident:
            // this needs to be the name of a class, but we need to
            // check that it hasn't been used locally as a name of
            // a member in the current class 
            string name = node.Sval;
            CbMember mem = null;
            CbType ctype = CbType.Error;
            if (classTypeDefn.Members.TryGetValue(name,out mem)) {
                // uh, oh  we found a name clash
                Start.SemanticError(node.LineNumber, "{0} is declared as a class member but is used as a type", name);
                // leave the type as error
            } else {
                CbClass t = ns.LookUp(name) as CbClass;
                if (t == null) {
                    Start.SemanticError(node.LineNumber, "type {0} is unknown", name);
                } else {
                    ctype = t;
                }
            }
            node.Type = ctype;
            break;
        default:
            throw new Exception("Unexpected tag: "+node.Tag);
        }
    }

 
}

}
