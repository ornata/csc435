/*  CbLLVMVisitor1.cs

    First stage of LLVM intermediate code generation
    Everything except generating code for the methods is done -- this
    covers:
    * defining llvm structures for the data layout of class instances,
    * constructing VTables for virtual method dispatch,
    * defining class constants
    
    Author: Nigel Horspool
    
    Dates: 2012-2014
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace FrontEnd {


public class LLVMVisitor1: Visitor {
    NameSpace ns;
    CbClass currentClass;
    LLVM llvm;

    // constructor
    public LLVMVisitor1( LLVM llvm ) {
        ns = NameSpace.TopLevelNames;  // get the top-level namespace
        currentClass = null;
        this.llvm = llvm;
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
        case NodeType.Block:
        case NodeType.ActualList:
            break;
        }
    }

	public override void Visit( AST_nonleaf node, object data ) {
        switch(node.Tag) {
        case NodeType.Program:
            node[1].Accept(this, data);  // visit class declarations
            break;
        case NodeType.Class:
            string className = ((AST_leaf)node[0]).Sval;
            currentClass = ns.LookUp(className) as CbClass;
            llvm.OutputClassDefinition(currentClass);
            // now visit the class's members -- only consts matter
            AST_kary memberList = node[2] as AST_kary;
            for(int i=0; i<memberList.NumChildren; i++) {
                memberList[i].Accept(this,data);
            }
            currentClass = null;
            break;
        case NodeType.Const:
            AST_leaf k = (AST_leaf)(node[2]);
            string constName = ((AST_leaf)node[1]).Sval;
            CbConst cm = currentClass.Members[constName] as CbConst;
            llvm.OutputConstDefn(cm, k);
            break;
        case NodeType.Field:
        case NodeType.Method:
            // we are ignoring these in pass 1
            break;
        default:
            throw new Exception("Unexpected tag: "+node.Tag);  
        }
    }

	public override void Visit(AST_leaf node, object data) {
        throw new Exception("Unexpected tag: "+node.Tag);
    }

}

}
