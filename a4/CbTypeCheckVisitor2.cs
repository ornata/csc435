/*  CbTypeCheckVisitor2.cs

    Second stage of full type-checking on AST

    We now visit and type-check all the parts of the AST which were not
    checked in the first stage of full type-checking.
    
    Author: Nigel Horspool
    
    Dates: 2012-2014
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace FrontEnd {


public class TypeCheckVisitor2: Visitor {
    NameSpace ns;
    CbClass currentClass;
    CbMethod currentMethod;
    SymTab sy;
    int loopNesting;
    CbMethod writeMethod, writeLineMethod;

    // constructor
    public TypeCheckVisitor2( ) {
        ns = NameSpace.TopLevelNames;  // get the top-level namespace
        currentMethod = null;
        sy = new SymTab();
        loopNesting = 0;
        NameSpace sys = ns.LookUp("System") as NameSpace;
        CbClass c = sys.LookUp("Console") as CbClass;
        writeMethod = c.Members["Write"] as CbMethod;
        writeLineMethod = c.Members["WriteLine"] as CbMethod;
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
            sy.Enter();
            // visit each statement or local declaration
            for(int i=0; i<node.NumChildren; i++) {
                node[i].Accept(this, data);
            }
            sy.Exit();
            break;
        case NodeType.ActualList:
            for(int i=0; i<node.NumChildren; i++) {
                node[i].Accept(this, data);
            }
            break;
        }
    }

	public override void Visit( AST_nonleaf node, object data ) {
        switch(node.Tag) {
        case NodeType.Program:
            node[1].Accept(this, data);  // visit class declarations
            break;
        case NodeType.Class:
            AST_leaf classNameId = node[0] as AST_leaf;
            string className = classNameId.Sval;
            currentClass = ns.LookUp(className) as CbClass;
            Debug.Assert(currentClass != null);
            performParentCheck(currentClass,node);  // check Object is ultimate ancestor
            // now check the class's members, passing the class defn
            AST_kary memberList = node[2] as AST_kary;
            for(int i=0; i<memberList.NumChildren; i++) {
                memberList[i].Accept(this,data);
            }
            currentClass = null;
            break;
        case NodeType.Const:
            node[2].Accept(this,data);  // get type of value
            if (!isAssignmentCompatible(node[0].Type,node[2].Type))
                Start.SemanticError(node.LineNumber, "invalid initialization for const");
            break;
        case NodeType.Field:
            break;
        case NodeType.Method:   
            // get the method's type description
            string methname = ((AST_leaf)(node[1])).Sval;
            currentMethod = currentClass.Members[methname] as CbMethod;
            sy.Empty();
            // add each formal parameter to the symbol table
            AST_kary formals = (AST_kary)node[2];
            for(int i=0; i<formals.NumChildren; i++) {
                AST_nonleaf formal = (AST_nonleaf)formals[i];
                string name = ((AST_leaf)formal[1]).Sval;
                SymTabEntry se = sy.Binding(name, formal[1].LineNumber);
                se.Type = formal[0].Type;
            }
            sy.Enter();
            // now type-check the method body
            node[3].Accept(this,data);
            // finally check that static/virtual/override are used correctly
            checkOverride(node);
            currentMethod = null;
            break;
        case NodeType.LocalDecl:
            node[0].Accept(this,data);  // get type
            AST_kary locals = node[1] as AST_kary;
            for(int i=0; i<locals.NumChildren; i++) {
                AST_leaf local = locals[i] as AST_leaf;
                string name = local.Sval;
                SymTabEntry en = sy.Binding(name, local.LineNumber);
                en.Type = node[0].Type;
            }
            break;
        case NodeType.Assign:
            node[0].Accept(this,data);
            node[1].Accept(this,data);
            if (node[0].Kind != CbKind.Variable && node[0].Type != CbType.Error)
                Start.SemanticError(node.LineNumber, "target of assignment is not a variable");
            if (!isAssignmentCompatible(node[0].Type, node[1].Type))
                Start.SemanticError(node.LineNumber, "invalid types in assignment statement");
            break;
        case NodeType.If:
            node[0].Accept(this,data);
            if (node[0].Type != CbType.Bool && node[0].Type != CbType.Error)
                Start.SemanticError(node.LineNumber, "if statement condition must have bool type");
            node[1].Accept(this,data);
            node[2].Accept(this,data);
            break;
        case NodeType.While:
            node[0].Accept(this,data);
            if (node[0].Type != CbType.Bool && node[0].Type != CbType.Error)
                Start.SemanticError(node.LineNumber, "while statement condition must have bool type");
            loopNesting++;
            node[1].Accept(this,data);
            loopNesting--;
            break;
        case NodeType.Break:
            if (loopNesting == 0)
                Start.SemanticError(node.LineNumber, "break statement is not used inside a loop");
            break;
        case NodeType.Return:
            if (node[0] == null) {
                if (currentMethod.ResultType != CbType.Void)
                    Start.SemanticError(node.LineNumber, "missing return value for method");
                break;
            }
            node[0].Accept(this,data);
            if (currentMethod.ResultType == CbType.Void) {
                Start.SemanticError(node.LineNumber, "method has void type");
                break;
            }
            if (!isAssignmentCompatible(currentMethod.ResultType, node[0].Type))
                Start.SemanticError(node.LineNumber, "return expression has invalid type");
            break;
        case NodeType.Call:
            node[0].Accept(this,data); // method name (could be a dotted expression)
            node[1].Accept(this,data); // actual parameters
            CbMethodType m = node[0].Type as CbMethodType;
            if (m != null) {
                node.Type = m.Method.ResultType;
                // now check that the arguments have appropriate types
                int nc = node[1].NumChildren;
                if (m.Method.ArgType.Count != nc)
                    Start.SemanticError(node.LineNumber, "wrong number of arguments in method call");
                else {
                    if (m.Method == writeMethod || m.Method == writeLineMethod)
                    {
                        if (nc != 1)
                            Start.SemanticError(node.LineNumber, "only one argument is supported for Write/WriteLine");
                        else
                        {
                            CbType t = node[1][0].Type;
                            if (t != CbType.Int && t != CbType.Char && t != CbType.String)
                                Start.SemanticError(node.LineNumber, "only int/char/string argument supported");
                        }
                    }
                    else
                    {
                        for (int i = 0; i < nc; i++)
                            if (!isAssignmentCompatible(m.Method.ArgType[i], node[1][i].Type))
                                Start.SemanticError(node.LineNumber, "method argument has invalid type");
                    }
                }
            } else {
                if (node[0].Type != CbType.Error)
                    Start.SemanticError(node.LineNumber, "not a method");
                node.Type = CbType.Error;
            }
            break;
        case NodeType.Dot:
            node[0].Accept(this,data);
            string rhs = ((AST_leaf)node[1]).Sval;
            CbClass lhstype = node[0].Type as CbClass;
            if (lhstype != null) {
                // rhs needs to be a member of the lhs class or an ancestor
                CbClass c = lhstype;
                node.Type = CbType.Error;
                while (c != null) {
                    CbMember mem;
                    if (c.Members.TryGetValue(rhs,out mem)) {
                        node.Type = mem.Type;
                        if (mem is CbField)
                            node.Kind = CbKind.Variable;
                        if (node[0].Kind == CbKind.ClassName) {
                            // mem has to be a static member
                            if (!mem.IsStatic) {
                                Start.SemanticError(node[1].LineNumber, "static member required");
                            }
                        } else {
                            // mem has to be an instance member
                            if (mem.IsStatic) {
                                Start.SemanticError(node[1].LineNumber,
                                    "member cannot be accessed via a reference, use classname instead");
                            }
                        }
                        break;
                    }
                    c = c.Parent;
                }
                if (node.Type == CbType.Error)
                    Start.SemanticError(node[1].LineNumber,
                        "member {0} not found in class {1}", rhs, lhstype.Name);
                break;
            }
            if (rhs == "Length") {
                // lhs has to be an array or a string
                if (node[0].Type != CbType.String && !(node[0].Type is CFArray))
                    Start.SemanticError(node[1].LineNumber, "member Length not found");
                node.Type = CbType.Int;
                break;
            }
            CbNameSpaceContext lhsns = node[0].Type as CbNameSpaceContext;
            if (lhsns != null) {
                lhstype = lhsns.Space.LookUp(rhs) as CbClass;
                if (lhstype != null) {
                    node.Type = lhstype;
                    node.Kind = CbKind.ClassName;
                    break;
                }
            }
            Start.SemanticError(node[1].LineNumber, "member {0} does not exist", rhs);
            node.Type = CbType.Error;
            break;
        case NodeType.Cast:
            if (typeSyntaxOK(node[0]))
            {
                node[0].Accept(this, data);
                node[1].Accept(this, data);
                if (!isCastable(node[0].Type, node[1].Type) ||
                    (node[0].Tag == NodeType.Ident && node[0].Kind != CbKind.ClassName))
                {
                    Start.SemanticError(node[1].LineNumber, "invalid cast");
                    node.Type = CbType.Error;
                }
                else
                    node.Type = node[0].Type;
            }
            else
            {
                Start.SemanticError(node[1].LineNumber, "invalid syntax for cast");
                node.Type = CbType.Error;
            }
            break;
        case NodeType.NewArray:
            node[0].Accept(this,data);
            node[1].Accept(this,data);
            if (!isIntegerType(node[1].Type))
                Start.SemanticError(node[1].LineNumber, "array size is not a int");
            node.Type = CbType.Array(node[0].Type);
            break;
        case NodeType.NewClass:
            node[0].Accept(this,data);
            if (!(node[0].Type is CbClass)) {
                Start.SemanticError(node[0].LineNumber, "class type expected");
                node.Type = CbType.Error;
            } else
                node.Type = node[0].Type;
            break;
        case NodeType.PlusPlus:
        case NodeType.MinusMinus:
            node[0].Accept(this,data);
            if (node[0].Kind != CbKind.Variable)
                Start.SemanticError(node[0].LineNumber, "operand must be a variable");
            if (!isIntegerType(node[0].Type))
                Start.SemanticError(node[0].LineNumber, "integer value expected");
            node.Type = node[0].Type;
            break;
        case NodeType.UnaryPlus:
        case NodeType.UnaryMinus:
            node[0].Accept(this,data);
            if (!isIntegerType(node[0].Type))
                Start.SemanticError(node[0].LineNumber, "integer value expected");
            node.Type = CbType.Int;
            break;
        case NodeType.Index:
            node[0].Accept(this,data);
            if (node[0].Type is CFArray) {
                node.Type = ((CFArray)(node[0].Type)).ElementType;
            } else if (node[0].Type == CbType.String) {
                node.Type = CbType.Char;
            } else {
                if (node[0].Type != CbType.Error)
                    Start.SemanticError(node[0].LineNumber, "array value expected");
                node.Type = CbType.Error;
            }
            node[1].Accept(this,data);
            if (!isIntegerType(node[1].Type))
                Start.SemanticError(node[0].LineNumber, "array index should be an int");
            node.Kind = CbKind.Variable;  // Bug fix, 15-July-14
            break;
        case NodeType.Add:
        case NodeType.Sub:
        case NodeType.Mul:
        case NodeType.Div:
        case NodeType.Mod:
            node[0].Accept(this,data);
            node[1].Accept(this,data);
            if (isIntegerType(node[0].Type) && isIntegerType(node[1].Type))
                node.Type = CbType.Int;
            else if (node.Tag == NodeType.Add && node[0].Type == CbType.String
                    && node[1].Type == CbType.String)
                node.Type = CbType.String;
            else {
                Start.SemanticError(node[0].LineNumber,
                    "{0} operator does not accept these argument types", node.Tag);
                node.Type = CbType.Error;
            }
            break;
        case NodeType.Equals:
        case NodeType.NotEquals:
            node[0].Accept(this,data);
            node[1].Accept(this,data);
            node.Type = CbType.Bool;           
            // operands need to be integral or classes with ancestor relationship
            if (isIntegerType(node[0].Type) && isIntegerType(node[1].Type))
                break;  // OK!
            if ((node[0].Type is CbClass || node[0].Type == CbType.Null) &&
                    (node[1].Type is CbClass || node[1].Type == CbType.Null)) {
                if (node[0].Type == CbType.Null || node[1].Type == CbType.Null)
                    break;
                CbClass t1 = node[0].Type as CbClass;
                CbClass t2 = node[1].Type as CbClass;
                if (isAncestor(t1,t2) || isAncestor(t2,t1))
                    break;
            }
            Start.SemanticError(node[0].LineNumber,
                "{0} operator does not accept these argument types", node.Tag);
            break;
        case NodeType.LessThan:
        case NodeType.GreaterThan:
        case NodeType.LessOrEqual:
        case NodeType.GreaterOrEqual:
            node[0].Accept(this,data);
            node[1].Accept(this,data);
            node.Type = CbType.Bool;
            // both operands need to be integral
            if (!isIntegerType(node[0].Type) || !isIntegerType(node[1].Type))
                Start.SemanticError(node[0].LineNumber,
                    "{0} operator does not accept these argument types", node.Tag);
            break;
        case NodeType.And:
        case NodeType.Or:
            node[0].Accept(this,data);
            node[1].Accept(this,data);
            node.Type = CbType.Bool;
            // both operands need to be bool
            if (node[0].Type != CbType.Bool || node[1].Type != CbType.Bool)
            if (!isIntegerType(node[0].Type) || !isIntegerType(node[1].Type))
                Start.SemanticError(node[0].LineNumber,
                    "{0} operator does not accept these argument types", node.Tag);
            break;
        case NodeType.Array:
            // processing a local declaration of an array       
	        // visit the child to get the element type
	        node[0].Accept(this,data);
	        // now create an array type from that element type
	        CbType at = CbType.Array(node[0].Type);
	        node.Type = at;
            break;
        default:
            throw new Exception("Unexpected tag: "+node.Tag);  
        }
    }

	public override void Visit(AST_leaf node, object data) {
        switch(node.Tag) {
        case NodeType.Ident:
            string name = node.Sval;
            SymTabEntry local = sy.LookUp(name);
            if (local != null) {
                node.Type = local.Type;
                node.Kind = CbKind.Variable;
                return;
            }
            CbMember mem;
            CbClass c = currentClass;
            while (c != null)
            {
                if (c.Members.TryGetValue(name, out mem))
                {
                    node.Type = mem.Type;
                    if (mem is CbField)
                        node.Kind = CbKind.Variable;
                    return;
                }
                c = c.Parent;
            }
            CbClass t = ns.LookUp(name) as CbClass;
            if (t != null) {
                node.Type = t;
                node.Kind = CbKind.ClassName;
                break;
            }
            NameSpace lhsns = ns.LookUp(name) as NameSpace;
            if (lhsns != null) {
                node.Type = new CbNameSpaceContext(lhsns);
                break;
            }
            node.Type = CbType.Error;;
            Start.SemanticError(node.LineNumber, "{0} is unknown", name);
            break;
        case NodeType.Break:
            if (loopNesting <= 0)
                Start.SemanticError(node.LineNumber, "break can only be used inside a loop");
            break;
        case NodeType.Null:
            node.Type = CbType.Null;
            break;
        case NodeType.IntConst:
            node.Type = CbType.Int;
            break;
        case NodeType.StringConst:
            node.Type = CbType.String;
            break;
        case NodeType.CharConst:
            node.Type = CbType.Char;
            break;
        case NodeType.Empty:
            break;
        case NodeType.IntType:
            node.Type = CbType.Int;
            break;
        case NodeType.CharType:
            node.Type = CbType.Char;
            break;
        case NodeType.StringType:
            node.Type = CbType.String;
            break;
        default:
            throw new Exception("Unexpected tag: "+node.Tag);
        }
    }

    private void performParentCheck(CbClass c, AST n) {
        IList<CbClass> path = new List<CbClass>();
        while(!path.Contains(c)) {
            CbClass p = c.Parent;
            if (p == null) {  // insert missing parent
                c.Parent = CbType.Object;
                return;
            }
            if (p == CbType.Object)
                return;
            path.Add(c);
            c = p;
        }
        Start.SemanticError(n.LineNumber,
            "circularity in class hierarchy involving {0}", c.Name);
        // overwrite parent so that subsequent checking does not
        // get caught in infinite searches
        c.Parent = CbType.Object;
    }
    
    private bool isAssignmentCompatible(CbType dest, CbType src) {
        if (dest == CbType.Error || src == CbType.Error) return true;
        if (dest == src) return true;
        if (dest == CbType.Int) return isIntegerType(src);
        CbClass d = dest as CbClass;
        CbClass s = src as CbClass;
        if (d != null) {
            if (src == CbType.Null) return true;
            if (s == null) return false;
            if (isAncestor(d,s)) return true;
        }
        return false;
    }

    private bool typeSyntaxOK(AST n)
    {
        switch (n.Tag)
        {
            case NodeType.Dot:
                // LHS must be namespace, RHS must be classname -- both identifiers
                return (n[0].Tag == NodeType.Ident && n[1].Tag == NodeType.Ident);
            case NodeType.Array:
                return typeSyntaxOK(n[0]);
            case NodeType.Ident:
            case NodeType.IntType:
            case NodeType.CharType:
            case NodeType.StringType:
                break;
            default:
                return false;
        }
        return true;
    }

    private bool isCastable(CbType dest, CbType src) {
        if (isIntegerType(dest) && isIntegerType(src)) return true;
        if (dest == CbType.Error || src == CbType.Error) return true;
        CbClass d = dest as CbClass;
        CbClass s = src as CbClass;
        if (isAncestor(d,s)) return true;
        if (isAncestor(s,d)) return true;
        return false;
    }
    
    private bool isIntegerType(CbType t) {
        return t == CbType.Int || t == CbType.Char || t == CbType.Error;
    }
    
    // tests if T1 == T2 or T1 is an ancestor of T2 in hierarchy
    private bool isAncestor( CbClass T1, CbClass T2 ) {
        while(T1 != T2) {
            T2 = T2.Parent;
            if (T2 == null) return false;
        }
        return true;
    }
    
    private void checkOverride(AST_nonleaf node) {
        string name = currentMethod.Name;
        // search for a member in any ancestor with same name
        CbClass cl = currentClass.Parent;
        CbMember mem;
        for( ; ; ) {
            if (cl == null) {
                if (node[4].Tag == NodeType.Override)
                    Start.SemanticError(node.LineNumber,
                        "{0}: no method in a parent class found to override", name);
                return;
            }
            if (cl.Members.TryGetValue(name,out mem))
                break;
            cl = cl.Parent;
        }
        CbMethod meth2 = mem as CbMethod;
        if (meth2 == null) {
            Start.SemanticError(node.LineNumber,
                "{0}: only overriding of methods is permitted", name);
            return;
        }
        bool ok = true;
        if (currentMethod.ResultType != meth2.ResultType) ok = false;
        if (currentMethod.ArgType.Count != meth2.ArgType.Count)
            ok = false;
        else {
            for(int i=0; i<meth2.ArgType.Count; i++) {
                if (currentMethod.ArgType[i] != meth2.ArgType[i]) {
                    ok = false;
                    break;
                }
            }
        }
        if (!ok) {
            Start.SemanticError(node.LineNumber,
                "signature of method {0} does not match method it is overriding", name);
        } else
        if (currentMethod.IsStatic || meth2.IsStatic) {
            Start.SemanticError(node.LineNumber, "static methods cannot override or be overridden");
        } else
        if (node[4].Tag == NodeType.Virtual)
            Start.SemanticError(node.LineNumber, "method {0} should have override attribute", name);
    }
}

}
