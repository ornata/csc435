/*  CbSemanticCheckVisitor.cs

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


public class SemanticCheckVisitor: Visitor {
    NameSpace ns;  // namespace for all top-level names and names opened with 'using' clauses
    CbClass currentClass;  // current class being checked (null if there isn't one)
    CbMethod currentMethod;  // current method being checked (null if there isn't one)
    SymTab sy;   // one instance of SymTab used for all method body checking
    int loopNesting;  // current depth of nesting of while loops

    // constructor
    public SemanticCheckVisitor( ) {
        ns = NameSpace.TopLevelNames;  // get the top-level namespace
        currentMethod = null;
        sy = new SymTab();
        loopNesting = 0;
    }
    
    // Note: the data parameter for the Visit methods is never used
    // It is always null (or whatever is passed on the initial call)

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
            performParentCheck(currentClass,node.LineNumber);  // check Object is ultimate ancestor
            // now check the class's members
            AST_kary memberList = node[2] as AST_kary;
            for(int i=0; i<memberList.NumChildren; i++) {
                memberList[i].Accept(this,data);
            }
            currentClass = null;
            break;
        case NodeType.Const:
            node[2].Accept(this,data);  // get type of value
            if (!isAssignmentCompatible(node[0].Type,node[2].Type))
                Start.SemanticError(node.LineNumber, "Invalid initialization for const.");
            break;
        case NodeType.Field:
            break;
        case NodeType.Method:   
            // get the method's type description
            string methname = ((AST_leaf)(node[1])).Sval;
            currentMethod = currentClass.Members[methname] as CbMethod;
            sy.Empty();
            // add secret "this" argument
            SymTabEntry thisBinding = sy.Binding("this", node[2].LineNumber);
            thisBinding.Type = currentClass;
            thisBinding.Kind = CbKind.Constant;
            // add each formal parameter to the symbol table
            AST_kary formals = (AST_kary)node[2];
            for(int i=0; i<formals.NumChildren; i++) {
                AST_nonleaf formal = (AST_nonleaf)formals[i];
                string name = ((AST_leaf)formal[1]).Sval;
                SymTabEntry newBinding = sy.Binding(name, formal[1].LineNumber);
                newBinding.Type = formal[0].Type;
                newBinding.Kind = CbKind.Variable;
            }
            sy.Enter();
            // now type-check the method body
            node[3].Accept(this,data);
            // finally check that static/virtual/override are used correctly
            checkOverride(node);
            currentMethod = null;
            break;
        case NodeType.LocalDecl:
            node[0].Accept(this,data);  // get type for the locals
            AST_kary locals = node[1] as AST_kary;
            for(int i=0; i<locals.NumChildren; i++) {
                AST_leaf local = locals[i] as AST_leaf;
                string name = local.Sval;
                SymTabEntry en = sy.Binding(name, local.LineNumber);
                en.Type = node[0].Type;
                en.Kind = CbKind.Variable;
            }
            break;
        case NodeType.Assign:
            node[0].Accept(this,data);
            node[1].Accept(this,data);
            if (node[0].Kind != CbKind.Variable)
                Start.SemanticError(node.LineNumber, "Target of assignment is not a variable.");
            if (!isAssignmentCompatible(node[0].Type, node[1].Type))
                Start.SemanticError(node.LineNumber, "Invalid types in assignment statement.");
            break;
        case NodeType.If:
            node[0].Accept(this,data);
            /* TODO ... check type */
            node[1].Accept(this,data);
            node[2].Accept(this,data);
            break;
        case NodeType.While:
            node[0].Accept(this,data);

            if (node[0].Type != CbType.Bool) {
                Start.SemanticError(node[0].LineNumber, "While loop test condition must be boolean.");
                node.Type = CbType.Error;
            }

            loopNesting++;
            node[1].Accept(this,data);

            loopNesting--;

            break;

        case NodeType.Return:
            CbType typeToReturn = null;
            if (node[0] != null) {
                // visit the returned expression if there is one
                node[0].Accept(this,data);
                typeToReturn = node[0].Type;
            }

            if (currentMethod.ResultType != CbType.Void) {
                // in this case, we are in a non-void-returning method
                if (typeToReturn == null) {
                    // returned void in a non-void returning method
                    Start.SemanticError(node.LineNumber, "Missing return value for method.");
                } else if (!isAssignmentCompatible(currentMethod.ResultType, typeToReturn)) {
                    Start.SemanticError(node.LineNumber, "Incompatible return type for method.");
                }
            } else {
                // in this case, we are in a void-returning method
                if (typeToReturn != null) {
                    // tried to return a value from a void function
                    Start.SemanticError(node.LineNumber, "Void methods cannot return values.");
                }
            }

            break;
        case NodeType.Call:
            node[0].Accept(this,data); // method name (could be a dotted expression)
            node[1].Accept(this,data); // actual parameters
            
            if (!(node[0].Type is CbMethodType)) {
                Start.SemanticError(node[0].LineNumber, "Calls must be made on methods.");
                node.Type = CbType.Error;
            } else {
                CbMethodType methodT = node[0].Type as CbMethodType;
                CbMethod method = methodT.Method;

                AST_kary actualAST = node[1] as AST_kary;

                if (actualAST.NumChildren != method.ArgType.Count) {
                    Start.SemanticError(node[1].LineNumber, "Incorrect number of arguments to method call."); 
                    node.Type = CbType.Error;
                } else {
                    bool foundTypeError = false;

                    for (int i = 0; i < actualAST.NumChildren && !foundTypeError; i++) {
                        if (!isAssignmentCompatible(method.ArgType[i], actualAST[i].Type)) {
                            foundTypeError = true;
                        }
                    }

                    if (foundTypeError) {
                        Start.SemanticError(node[1].LineNumber, "Incompatible argument types in method call.");
                        node.Type = CbType.Error;
                    } else {
                        node.Type = method.ResultType;
                    }
                }
            }

            break;
        case NodeType.Dot:
            node[0].Accept(this,data);
            string rhs = ((AST_leaf)node[1]).Sval;

            // check if we're accessing a class
            CbClass lhstype = node[0].Type as CbClass;
            if (lhstype != null) {
                // rhs needs to be a member of the lhs class
                CbMember mem;
                if (lhstype.Members.TryGetValue(rhs,out mem)) {
                    node.Type = mem.Type;

                    if (mem is CbField)
                        node.Kind = CbKind.Variable;
                    else if (mem is CbConst)
                        node.Kind = CbKind.Constant;

                    if (node[0].Kind == CbKind.ClassName) {
                        // mem has to be a static member
                        if (!mem.IsStatic)
                            Start.SemanticError(node[1].LineNumber, "Static member required.");
                    } else {
                        // mem has to be an instance member
                        if (mem.IsStatic)
                            Start.SemanticError(node[1].LineNumber,
                                "Member cannot be accessed via a reference, use classname instead.");

                        if (lhstype == CbType.String || lhstype is CFArray) {
                            // "Length" field of String is immutable
                            if (rhs == "Length") {
                                node.Kind = CbKind.Constant;
                            }
                            
                            // "Length" is the only valid field for literals
                            if (node[0].Kind == CbKind.Literal && rhs != "Length") {
                                Start.SemanticError(node[1].LineNumber,
                                    "The only property for string literals is \"Length\"");
                                node.Type = CbType.Error;
                            }
                        }
                    }
                } else {
                    Start.SemanticError(node[1].LineNumber,
                        "member {0} not found in class {1}", rhs, lhstype.Name);
                    node.Type = CbType.Error;
                }
                break;
            }

            // check if we're accessing a namespace
            CbNameSpaceContext lhsns = node[0].Type as CbNameSpaceContext;
            if (lhsns != null) {
                lhstype = lhsns.Space.LookUp(rhs) as CbClass;
                if (lhstype != null) {
                    node.Type = lhstype;
                    node.Kind = CbKind.ClassName;
                    break;
                }
            }
            // couldn't find it
            Start.SemanticError(node[1].LineNumber, "Member {0} does not exist.", rhs);
            node.Type = CbType.Error;
            break;

        case NodeType.Cast:
            checkTypeSyntax(node[0]);
            node[0].Accept(this,data);
            node[1].Accept(this,data);

            if (!isCastable(node[0].Type, node[1].Type))
                Start.SemanticError(node[1].LineNumber, "Invalid cast.");
            node.Type = node[0].Type;

            break;

        case NodeType.NewArray:
            node[0].Accept(this,data);
            node[1].Accept(this,data);
            bool foundError = false;

            if (node[0].Type != CbType.Int &&
                node[0].Type != CbType.Char &&
                node[0].Type != CbType.String &&
                !(node[0].Type is CbClass)) {
                Start.SemanticError(node[0].LineNumber, "Array type must be 'int', 'char', 'string', or a class.");
                node.Type = CbType.Error;
                foundError = true;
            }

            if (node[1].Type != CbType.Int &&
                node[1].Type != CbType.Char) {
                Start.SemanticError(node[1].LineNumber, "Array length must be of type 'int' or 'char.");
                node.Type = CbType.Error;
                foundError = true;
            }

            if (foundError == false) {
                node.Type = CbType.Array(node[0].Type);
            }

            break;

        case NodeType.NewClass:
            node[0].Accept(this,data);
            if (node[0].Type is CbClass) {
                node.Type = node[0].Type;
            }

            else {
                Start.SemanticError(node[0].LineNumber, "Cannot use 'new' to create non-class types.");
                node.Type = CbType.Error;
            }

            break;

        case NodeType.PlusPlus:
        case NodeType.MinusMinus:
            node[0].Accept(this,data);
            /* TODO ... check types and operand must be a variable */
            if (isIntegerType(node[0].Type) && node[0].Kind == CbKind.Variable) {
                node.Type = node[0].Type;
            }

            else {
                Start.SemanticError(node[0].LineNumber, "Post increment/decrement may only be applied to integer-value variables.");
                node.Type = CbType.Error;
            }

            break;

        case NodeType.UnaryPlus:
        case NodeType.UnaryMinus:
            node[0].Accept(this,data);

            if (isIntegerType(node[0].Type)) {
                node.Type = CbType.Int;
            }

            else {
                Start.SemanticError(node[0].LineNumber, "Unary operations are only valid for integer and char types.");
                node.Type = CbType.Error;
            }

            break;

        case NodeType.Array:
            node[0].Accept(this,data);
            node.Type = CbType.Array(node[0].Type);

            break;

        case NodeType.Index:
            node[0].Accept(this,data);
            node[1].Accept(this,data);

            if (node[1].Type != CbType.Char && node[1].Type != CbType.Int) {
                Start.SemanticError(node[0].LineNumber, "Cannot index string or array using an object whose type is not 'int' or 'char'.");
                node.Type = CbType.Error;
            }

            else {
                if (node[0].Type == CbType.String){
                    node.Type = CbType.Char;
                }

                else if(node[0].Type is CFArray) {
                    node.Type = (node[0].Type as CFArray).ElementType; // doesn't work... :(
                }

                else {
                    Start.SemanticError(node[0].LineNumber, "Cannot index an object whose type is not 'string' or 'array'.");
                    node.Type = CbType.Error;
                }
            }
                
            break;

        case NodeType.Add:
        case NodeType.Sub:
        case NodeType.Mul:
        case NodeType.Div:
        case NodeType.Mod:
            node[0].Accept(this,data);
            node[1].Accept(this,data);

            // Addition operator doubles as string concatenation
            if(node.Tag == NodeType.Add){
                if (node[0].Type == CbType.String && node[1].Type == CbType.String) {
                    node.Type = CbType.String;
                    break;
                }
            }
                
            // Otherwise, just handle an arithmetic expression
            if (node[0].Type == CbType.Int && node[1].Type == CbType.Char) {
                node.Type = CbType.Int;
            }

            else if (node[0].Type == CbType.Char && node[1].Type == CbType.Int) {
                node.Type = CbType.Int;
            }

            else if (node[0].Type == CbType.Int && node[0].Type == node[1].Type) {
                node.Type = CbType.Int;
            }

            else if (node[0].Type == CbType.Char && node[0].Type == node[1].Type) {
                node.Type = CbType.Char;
            }

            else {
                if (node.Tag == NodeType.Add) {
                    Start.SemanticError(node[0].LineNumber, "Invalid arithmetic expression; must be between 'int' and 'char' types or 'string' types.");
                } else {
                    Start.SemanticError(node[0].LineNumber, "Invalid arithmetic expression; must be between 'int' or 'char' types.");
                }
                node.Type = CbType.Error;
            }

            break;

        case NodeType.Equals:
        case NodeType.NotEquals:
            node[0].Accept(this,data);
            node[1].Accept(this,data);

            if (isAssignmentCompatible(node[0].Type, node[1].Type) ||
                isAssignmentCompatible(node[1].Type, node[0].Type) ||
                node[0].Type == CbType.Null && node[1].Type == CbType.Null) {
                node.Type = CbType.Bool;
            }

            else {
                Start.SemanticError(node[0].LineNumber, "Comparison between two non-assignment compatible values.");
                node.Type = CbType.Error;
            }

            break;

        case NodeType.LessThan:
        case NodeType.GreaterThan:
        case NodeType.LessOrEqual:
        case NodeType.GreaterOrEqual:
            node[0].Accept(this,data);
            node[1].Accept(this,data);

            if (isIntegerType(node[0].Type) && isIntegerType(node[1].Type)) {
                node.Type = CbType.Bool;
            }

            else {
                Start.SemanticError(node[0].LineNumber, "Ordered comparisons must be between objects of type 'char' and type 'int'.");
                node.Type = CbType.Error;
            }
               
            break;

        case NodeType.And:
        case NodeType.Or:
            node[0].Accept(this,data);
            node[1].Accept(this,data);

                if (node[0].Type == CbType.Bool && node[1].Type == CbType.Bool) {
                    node.Type = CbType.Bool;
                }

                else {
                    Start.SemanticError(node[0].LineNumber, "Boolean operations can only be performed on boolean operands");
                    node.Type = CbType.Error;
                }

            break;

        default:
            throw new Exception(String.Format("Line {0} Unexpected tag: {1}",
                                node.LineNumber, node.Tag));
        }
    }

    public override void Visit(AST_leaf node, object data) {
        switch(node.Tag) {
        case NodeType.Ident:
            string name = node.Sval;

            // look through local declarations
            SymTabEntry local = sy.LookUp(name);
            if (local != null) {
                node.Type = local.Type;
                node.Kind = local.Kind;
                break;
            }

            // look through this class and all its parents
            CbMember mem;
            bool foundMember = false;
            for (CbClass curr = currentClass; curr != null && !foundMember; curr = curr.Parent) {
                if (curr.Members.TryGetValue(name,out mem)) {
                    node.Type = mem.Type;

                    if (mem is CbField) {
                        node.Kind = CbKind.Variable;
                    }

                    foundMember = true;
                }
            }

            if (foundMember) {
                break;
            }

            // look through the top-level namespace for a class
            CbClass t = ns.LookUp(name) as CbClass;
            if (t != null) {
                node.Type = t;
                node.Kind = CbKind.ClassName;
                break;
            }

            // look through the top-level namespace for a namespace
            NameSpace lhsns = ns.LookUp(name) as NameSpace;
            if (lhsns != null) {
                node.Type = new CbNameSpaceContext(lhsns);
                break;
            }

            // couldn't find identifier
            node.Type = CbType.Error;
            Start.SemanticError(node.LineNumber, "{0} is unknown", name);
            break;
        case NodeType.Break:
            if (loopNesting <= 0)
                Start.SemanticError(node.LineNumber, "break can only be used inside a loop");
            break;
        case NodeType.Null:
            node.Type = CbType.Null;
            node.Kind = CbKind.Literal;
            break;
        case NodeType.IntConst:
            node.Type = CbType.Int;
            node.Kind = CbKind.Literal;
            break;
        case NodeType.StringConst:
            node.Type = CbType.String;
            node.Kind = CbKind.Literal;
            break;
        case NodeType.CharConst:
            node.Type = CbType.Char;
            node.Kind = CbKind.Literal;
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

    private void performParentCheck(CbClass c, int lineNumber) {
        ISet<CbClass> classes = new HashSet<CbClass>();
        CbClass curr = c;
        while (curr != null) {
            classes.Add(curr);

            if (classes.Contains(curr.Parent)) {
                Start.SemanticError(lineNumber, "cyclic dependency in class hierarchy for " + c.Name);
                // attempt to recover from the error by cutting the cycle
                curr.Parent = CbType.Object;
                return;
            }

            curr = curr.Parent;
        }
    }
    
    private bool isAssignmentCompatible(CbType dest, CbType src) {
        if (dest == CbType.Error || src == CbType.Error) return true;
        if (dest == src) return true;
        if (dest == CbType.Int) return isIntegerType(src);
        CbClass d = dest as CbClass;
        CbClass s = src as CbClass;
        if (d != null) {
            // Strings cannot be null
            if (src == CbType.Null && dest != CbType.String) return true;

            if (s == null) return false;
            if (isAncestor(d,s)) return true;
        }
        return false;
    }

    
    private void checkTypeSyntax(AST n) {
        /* TODO
           code to check whether n is the subtree that has appropriate AST
           structure for a Cb type. It could be a builtin type (int, char,
           string), a class, or an array whose elements have a valid type.
        */
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
    
    // returns true if type t can be used where an integer is needed
    private bool isIntegerType(CbType t) {
        return t == CbType.Int || t == CbType.Char || t == CbType.Error;
    }
    
    // tests if T1 == T2 or T1 is an ancestor of T2 in hierarchy
    private bool isAncestor( CbClass T1, CbClass T2 ) {
        while (T1 != T2) {
            T2 = T2.Parent;
            if (T2 == null) return false;
        }
        return true;
    }

    private void checkOverride(AST_nonleaf node) {
        string name = currentMethod.Name;
        // search for a member in any ancestor with same name
        /* TODO
           code to check whether any ancestor class contains a member with
           the same name. If so, it has to be a method with the identical
           signature.
           If there is a method with the same signature, then neither method
           is allowed to be static. (Not part of Cb language.)
           Otherwise, currentMethod must be flagged as override (not virtual).
        */
    }
}

}
