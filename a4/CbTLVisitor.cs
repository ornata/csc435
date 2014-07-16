/*  CbTLVisitor.cs

    Defines a Top-Level Sybol Table Visitor class for the CFlat AST
    
    Author: Nigel Horspool
    
    Dates: 2012-2014
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace FrontEnd {


// Traverses the AST to add top-level names into the top-level
// symbol table.
// Incomplete type descriptions of new classes are createdn too,
// these descriptions specify only the parent class and the names
// of members (but each is associated with a minimal field, method
// or const type description as appropriate).
public class TLVisitor: Visitor {
    private Dictionary<string, AST> pendingClassDefns = null;

    // constructor
    public TLVisitor( ) {
    }

	public override void Visit(AST_kary node, object data) {
	    Dictionary<string, AST> savedList;
        switch(node.Tag) {
        case NodeType.UsingList:
	        Debug.Assert(data != null && data is NameSpace);
            // add members of each namespace in list to top-level namespace
            for(int i=0; i<node.NumChildren; i++) {
                openNameSpace(node[i], (NameSpace)data);
            }
            break;
        case NodeType.ClassList:
	        Debug.Assert(data != null && data is NameSpace);
	        savedList = pendingClassDefns;
	        pendingClassDefns = new Dictionary<string, AST>();
            // add each class to the current namespace, by continuing traversal
            for(int i=0; i<node.NumChildren; i++) {
                node[i].Accept(this, data);
            }
            if (pendingClassDefns.Count > 0) {
                foreach( var pair in pendingClassDefns ) {
                    Start.SemanticError(pair.Value.LineNumber,
                        "unknown parent class {0}", pair.Key);
                }
            }
            pendingClassDefns = savedList;
            break;
        case NodeType.MemberList:
	        Debug.Assert(data != null && data is CbClass);
            // add each member to the current class, by continuing traversal
            for(int i=0; i<node.NumChildren; i++) {
                node[i].Accept(this, data);
            }
            break;
        default:
            throw new Exception("Unexpected tag: "+node.Tag);
        }
    }

	public override void Visit( AST_nonleaf node, object data ) {
        switch(node.Tag) {
        case NodeType.Program:
            Debug.Assert(data != null && data is NameSpace);
            node[0].Accept(this, data);  // visit the using list
            node[1].Accept(this, data);  // visit class declarations
            break;
        case NodeType.Class:
            Debug.Assert(data != null && data is NameSpace);
            NameSpace ns = (NameSpace)data;
            // add class name to current namespace,
            //  then continue traversal to add members to this class
            AST_leaf classNameId = node[0] as AST_leaf;
            string className = classNameId.Sval;
            AST_leaf parentClassId = node[1] as AST_leaf;
            string parentName = parentClassId == null? null : parentClassId.Sval;
            AST_kary memberList = node[2] as AST_kary;
            object ctd = ns.LookUp(className);
            //Debug.Assert(ctd is CbClass);
            CbClass classTypeDefn = (CbClass)ctd;
            CbClass parentTypeDefn = null;
            if (parentName != null) {
                object ptd = ns.LookUp(parentName);
                //Debug.Assert(ptd is CbClass);
                parentTypeDefn = (CbClass)ptd;
                if (parentTypeDefn == null) {
                    pendingClassDefns[parentName] = node;
                    parentTypeDefn = new CbClass(parentName, null);
                }
            }
            if (classTypeDefn != null) {
                if (!pendingClassDefns.ContainsKey(className)) {
                    Start.SemanticError(node.LineNumber,
                        "duplication definition for class {0}", className);
                } else {
                    classTypeDefn.Parent = parentTypeDefn;
                    pendingClassDefns.Remove(className);
                }
            } else {
                classTypeDefn = new CbClass(className, parentTypeDefn);
                ns.AddMember(classTypeDefn);
                if (pendingClassDefns.ContainsKey(className)) {
                    pendingClassDefns.Remove(className);
                }
            }
            // now add the class's members
            for(int i=0; i<memberList.NumChildren; i++) {
                memberList[i].Accept(this,classTypeDefn);
            }
            break;
        case NodeType.Const:
	        Debug.Assert(data != null && data is CbClass);
	        CbClass c1 = (CbClass)data;
            // add const name to current class
            AST_leaf cid = (AST_leaf)(node[1]);
            CbConst cdef = new CbConst(cid.Sval,null);
            c1.AddMember(cdef);
            break;
        case NodeType.Field:
	        Debug.Assert(data != null && data is CbClass);
	        CbClass c2 = (CbClass)data;
            // add a bunch of field names to current class
            AST_kary fields = (AST_kary)(node[1]);
            for(int i=0; i<fields.NumChildren; i++) {
                AST_leaf id = fields[i] as AST_leaf;
                CbField fdef = new CbField(id.Sval,null);
                c2.AddMember(fdef);
            }
            break;
        case NodeType.Method:
	        Debug.Assert(data != null && data is CbClass);
	        CbClass c3 = (CbClass)data;
            // add method name to current class
            AST_leaf mid = (AST_leaf)(node[1]);
            AST attr = node[4];
            CbMethod mdef = new CbMethod(mid.Sval, attr.Tag==NodeType.Static, null, null);
            c3.AddMember(mdef);
            break;
        default:
            throw new Exception("Unexpected tag: "+node.Tag);
        }
    }

	public override void Visit(AST_leaf node, object data) {
	    throw new Exception("TLVisitor traversal should not have reached a leaf node");
    }

    private void openNameSpace( AST ns2open, NameSpace currentNS ) {
        string nsName = ((AST_leaf)ns2open).Sval;
        object r = currentNS.LookUp(nsName);
        if (r == null) {
            Start.SemanticError(ns2open.LineNumber, "namespace {0} not found", nsName);
            return;
        }
        NameSpace c = r as NameSpace;
        if (r == null) {
            Start.SemanticError(ns2open.LineNumber, "{1} is not a namespce", nsName);
            return;
        }
        foreach(object def in c.Members) {
            Debug.Assert(def is CbClass);
            currentNS.AddMember((CbClass)def);
        }
    }
 
}

}
