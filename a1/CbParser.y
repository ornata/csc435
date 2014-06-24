/* CbParser.y */

// The grammar shown in this file is INCOMPLETE!!
// It does not support class inheritance, it does not permit
// classes to contain methods (other than Main).
// Other language features may be missing too.
%using System.IO;
%namespace  FrontEnd
%tokentype  Tokens
%visibility public

// Define yylval so we can recover type info
%union { 
    public string strVal;
    public int    intVal;
    public char   charVal;
}

// solves if-then-else ambiguity
%nonassoc IFX
%nonassoc Kwd_else

%token <strVal> StringConst
%token <intVal> IntConst
%token <charVal> CharConst

// All other named tokens (i.e. the single character tokens are omitted)
// The order in which they are listed here does not matter.
%token      Kwd_break Kwd_class Kwd_const Kwd_else Kwd_if Kwd_int Kwd_char
%token      Kwd_new Kwd_public Kwd_return Kwd_static Kwd_string
%token      Kwd_override Kwd_virtual Kwd_null
%token      Kwd_using Kwd_void Kwd_while
%token      PLUSPLUS MINUSMINUS Ident
%token      OROR ANDAND EQEQ NOTEQ GTEQ LTEQ
%token      ARRAYDECL

%start Program

%%

/* *************************************************************************
   *                                                                       *
   *         PRODUCTION RULES AND ASSOCIATED SEMANTIC ACTIONS              *
   *                                                                       *
 ************************************************************************* */

Program:        UsingList ClassList
        ;

UsingList:      Kwd_using Ident ';' UsingList
        |       /*empty*/
        ;

ClassList:      ClassDecl ClassList
        |       ClassDecl
        ;

ClassDecl:      Kwd_class Ident OptBase '{' MemberDeclList '}'
        ;

OptBase:        ':' Ident
        |       /* empty */ 
        ;

MemberDeclList: MemberDecl MemberDeclList
        |       /* empty */
        ;

MemberDecl:     ConstDecl
        |       FieldDecl
        |       MethodDecl
        ;

ConstDecl:      Kwd_public Kwd_const Type Ident '=' InitVal ';'
        ;

InitVal:        IntConst
        |       CharConst
        |       StringConst
        ;

FieldDecl:      Kwd_public Type IdentList ';'
        ;

IdentList:      IdentList ',' Ident
        |       Ident
        ;

MethodDecl:     Kwd_public MethodScope MethodType Ident '(' OptFormals ')' Block
        ;

LocalDecl:      Type IdentList ';'
        ;

MethodScope:    Kwd_static
        |       Kwd_virtual
        |       Kwd_override
        ;

MethodType:     Kwd_void
        |       Type
        ;

OptFormals:     FormalPars
        |       /* empty */
        ;

FormalPars:     FormalDecl
        |       FormalPars ',' FormalDecl
        ;

FormalDecl:     Type Ident
        ;

Type:           TypeName ARRAYDECL
        |       TypeName
        ;

TypeName:       Ident
        |       PrimitiveType
        ;

NonClassType:   PrimitiveType ARRAYDECL
        |       PrimitiveType
        ;

PrimitiveType:  Kwd_int
        |       Kwd_string
        |       Kwd_char
        ;

Statement:      Designator '=' Expr ';'
        |       Kwd_if '(' Condition ')' Statement %prec IFX // solves if-then-else ambiguity
        |       Kwd_if '(' Condition ')' Statement Kwd_else Statement
        |       Kwd_while '(' Condition ')' Statement
        |       Kwd_break ';'
        |       Kwd_return ';'
        |       Kwd_return Expr ';'
        |       Designator '(' OptActuals ')' ';'
        |       Designator PLUSPLUS ';'
        |       Designator MINUSMINUS ';'
        |       Block
        |       ';'
        ;

OptActuals:     ActPars
        |       /* empty */
        ;

Block:          '{' DeclsAndStmts '}'
        ;

DeclsAndStmts:  DeclsAndStmts Statement
        |       DeclsAndStmts LocalDecl
        |       /* empty */
        ;

ActPars:        ActPars ',' Expr
        |       Expr
        ;

Condition:      CondTermList
        ;

CondTermList:   CondTermList OROR CondTerm
        |       CondTerm
        ;

CondTerm:       CondFactList
        ;

CondFactList:   CondFactList ANDAND CondFact
        |       CondFact
        ;

CondFact:       EqFact EqOp EqFact
        |       EqFact
        ;

EqFact:         Expr RelOp Expr
        |       Expr
        ;

Expr:           Expr AddOp Term
        |       Term
        ;

Term:           Term MulOp Factor
        |       Factor
        ;

Factor:         '+' Factor
        |       '-' Factor
        |       FactorNotPlusMinus
        ;

FactorNotPlusMinus:
                Designator
        |       Designator '(' OptActuals ')'
        |       IntConst
        |       CharConst
        |       StringConst
        |       StringConst '.' Ident
        |       Kwd_new Ident '[' Expr ']'
        |       Kwd_new PrimitiveType '[' Expr ']'
        |       Kwd_new Ident '(' ')'
        |       Kwd_null
        |       '(' Expr ')' FactorNotPlusMinus
        |       '(' NonClassType ')' Factor
        |       '(' Ident ARRAYDECL ')' FactorNotPlusMinus
        |       '(' Expr ')'
        ;

Designator:     Ident
        |       Designator '.' Ident
        |       Designator '[' Expr ']'
        ;

EqOp:           EQEQ
        |       NOTEQ
        ;

RelOp:          '>'
        |       GTEQ
        |       '<'
        |       LTEQ
        ;

AddOp:          '+'
        |       '-'
        ;

MulOp:          '*'
        |       '/'
        |       '%'
        ;

%%

public Parser(string filename, Scanner sc) : base(sc){}




