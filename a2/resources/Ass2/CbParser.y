/* CbParser.y */

// The grammar has one shift-reduce conflict for the if-then-else ambiguity.
// As long as the parser shifts in the conflict state, the language will be
// parsed correctly.
//
// Because the cast operation syntax is hard to express as a LALR(1) grammar,
// the grammar rules below accept some syntax for expressions which is nonsense.
// There are two kinds of nonsense:
//  1.  Any expression can be used instead of a typename in a cast. For example:
//             (x+1)y
//  2.  An index can be omitted from an array access. For example,
//              a = ARR[]+2;
// A later semantic pass over the AST must check for both kinds of nonsense and
// produce error messages if discovered.
//
// The grammar tricks used to support casts were obtained from here:
//      http://msdn.microsoft.com/en-us/library/aa245175(v=vs.60).aspx
//
// Author:  Nigel Horspool
// Date:    June 2014

%namespace  FrontEnd
%tokentype  Tokens
%output=CbParser.cs
%YYSTYPE    AST     // set datatype of $$, $1, $2... attributes

// All tokens which can be used as operators in expressions
// they are ordered by precedence level (lowest first)
%right      '='
%left       OROR
%left       ANDAND
%nonassoc   EQEQ NOTEQ
%nonassoc   '>' GTEQ '<' LTEQ
%left       '+' '-'
%left       '*' '/' '%'

// All other named tokens (i.e. the single character tokens are omitted)
// The order in which they are listed here does not matter.

// Keywords
%token      Kwd_break Kwd_char Kwd_class Kwd_const Kwd_else Kwd_if Kwd_int
%token      Kwd_new Kwd_null Kwd_override Kwd_public Kwd_return
%token      Kwd_static Kwd_string Kwd_using Kwd_virtual Kwd_void Kwd_while

// Other tokens
%token      PLUSPLUS MINUSMINUS Ident CharConst IntConst StringConst


%%

/* *************************************************************************
   *                                                                       *
   *         PRODUCTION RULES AND ASSOCIATED SEMANTIC ACTIONS              *
   *                                                                       *
   ************************************************************************* */

Program:        UsingList ClassList
                { Tree = AST.NonLeaf(NodeType.Program, $1.LineNumber, $1, $2); }
        ;

UsingList:      /* empty */
			    { $$ = AST.Kary(NodeType.UsingList, LineNumber); }
        |       UsingList Kwd_using Identifier ';'
			    { $1.AddChild($3);  $$ = $1; }
        ;

ClassList:      ClassDecl
			    { $$ = AST.Kary(NodeType.ClassList, LineNumber); }
        |       ClassList ClassDecl
			    { $1.AddChild($2);  $$ = $1; }
        ;

ClassDecl:      Kwd_class Identifier  '{'  DeclList  '}'
                { $$ = AST.NonLeaf(NodeType.Class, $2.LineNumber, $2, null, $4); }
        |       Kwd_class Identifier  ':' Identifier  '{'  DeclList  '}'
                { $$ = AST.NonLeaf(NodeType.Class, $2.LineNumber, $2, $4, $6); }
        ;

DeclList:       /* empty */
        |       DeclList ConstDecl
        |       DeclList FieldDecl
        |       DeclList MethodDecl     
        ;

ConstDecl:      Kwd_public Kwd_const Type Identifier '=' InitVal ';'
        ;

InitVal:        IntConst
        |       CharConst
        |       StringConst
        ;

FieldDecl:      Kwd_public Type IdentList ';'
        ;

IdentList:      IdentList ',' Identifier
        |       Identifier
        ;

MethodDecl:     Kwd_public MethodAttr MethodType Identifier '(' OptFormals ')' Block
        ;

MethodAttr:     Kwd_static
        |       Kwd_virtual
        |       Kwd_override
        ;

MethodType:     Kwd_void
        |       Type
        ;

OptFormals:     /* empty */
        |       FormalPars
        ;

FormalPars:     FormalDecl
        |       FormalPars ',' FormalDecl
        ;

FormalDecl:     Type Identifier
        ;

Type:           TypeName
        |       TypeName '[' ']'
        ;

TypeName:       Identifier
        |       BuiltInType
        ;

BuiltInType:    Kwd_int
        |       Kwd_string
        |       Kwd_char
        ;

Statement:      Designator '=' Expr ';'
        |       Designator '(' OptActuals ')' ';'
        |       Designator PLUSPLUS ';'
        |       Designator MINUSMINUS ';'
        |       Kwd_if '(' Expr ')' Statement Kwd_else Statement
        |       Kwd_if '(' Expr ')' Statement
        |       Kwd_while '(' Expr ')' Statement
        |       Kwd_break ';'
        |       Kwd_return ';'
        |       Kwd_return Expr ';'
        |       Block
        |       ';'
        ;

OptActuals:     /* empty */
        |       ActPars
        ;

ActPars:        ActPars ',' Expr
        |       Expr
        ;

Block:          '{' DeclsAndStmts '}'
        ;

LocalDecl:      TypeName IdentList ';'
        |       Identifier '[' ']' IdentList ';'
        |       BuiltInType '[' ']' IdentList ';'
        ;

DeclsAndStmts:   /* empty */
        |       DeclsAndStmts Statement
        |       DeclsAndStmts LocalDecl
        ;

Expr:           Expr OROR Expr
        |       Expr ANDAND Expr
        |       Expr EQEQ Expr
        |       Expr NOTEQ Expr
        |       Expr LTEQ Expr
        |       Expr '<' Expr
        |       Expr GTEQ Expr
        |       Expr '>' Expr
        |       Expr '+' Expr
        |       Expr '-' Expr
        |       Expr '*' Expr
        |       Expr '/' Expr
        |       Expr '%' Expr
        |       UnaryExpr
        ;

UnaryExpr:      '-' Expr
        |       '+' Expr
        |       UnaryExprNotUMinus
        ;

UnaryExprNotUMinus:
                Designator
        |       Designator '(' OptActuals ')'
        |       Kwd_null
        |       IntConst
        |       CharConst
        |       StringConst
        |       StringConst '.' Identifier // Identifier must be "Length"
        |       Kwd_new Identifier '(' ')'
        |       Kwd_new TypeName '[' Expr ']'
        |       '(' Expr ')'
        |       '(' Expr ')' UnaryExprNotUMinus                 // cast
        |       '(' BuiltInType ')' UnaryExprNotUMinus          // cast
        |       '(' BuiltInType '[' ']' ')' UnaryExprNotUMinus  // cast

        ;

Designator:     Identifier Qualifiers
        ;

Qualifiers:     '.' Identifier Qualifiers
        |       '[' Expr ']' Qualifiers
        |       '[' ']' Qualifiers   // needed for cast syntax
        |       /* empty */
        ;

Identifier:     Ident   { $$ = AST.Leaf(NodeType.Ident, LineNumber, lexer.yytext); }
        ;
%%

// returns the AST constructed for the Cb program
public AST Tree { get; private set; }

private Scanner lexer;

// returns the lexer's current line number
public int LineNumber {
    get{ return lexer.LineNumber == 0? 1 : lexer.LineNumber; }
}

// Use this function for reporting non-fatal errors discovered
// while parsing and building the AST.
// An example usage is:
//    yyerror( "Identifier {0} has not been declared", idname );
public void yyerror( string format, params Object[] args ) {
    Console.Write("{0}: ", LineNumber);
    Console.WriteLine(format, args);
}

// The parser needs a suitable constructor
public Parser( Scanner src ) : base(null) {
    lexer = src;
    Scanner = src;
}


