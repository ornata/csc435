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
			    { $$ = AST.Kary(NodeType.ClassList, LineNumber, $1); }
        |       ClassList ClassDecl
			    { $1.AddChild($2);  $$ = $1; }
        ;

ClassDecl:      Kwd_class Identifier  '{'  DeclList  '}'
                { $$ = AST.NonLeaf(NodeType.Class, $2.LineNumber, $2, null, $4); }
        |       Kwd_class Identifier  ':' Identifier  '{'  DeclList  '}'
                { $$ = AST.NonLeaf(NodeType.Class, $2.LineNumber, $2, $4, $6); }
        ;

DeclList:       /* empty */
                { $$ = AST.Kary(NodeType.MemberList, LineNumber); }
        |       DeclList ConstDecl
                { $1.AddChild($2);  $$ = $1; }
        |       DeclList FieldDecl
                { $1.AddChild($2);  $$ = $1; }
        |       DeclList MethodDecl
                { $1.AddChild($2);  $$ = $1; }   
        ;

ConstDecl:      Kwd_public Kwd_const Type Identifier '=' InitVal ';'
                { $$ = AST.NonLeaf(NodeType.Const, $4.LineNumber, $3, $4, $6); }
        ;

InitVal:        AnInteger   { $$ = $1; }
        |       AChar       { $$ = $1; }
        |       AString     { $$ = $1; }
        ;

FieldDecl:      Kwd_public Type IdentList ';'
                { $$ = AST.NonLeaf(NodeType.Field, $2.LineNumber, $2, $3); }
        ;

IdentList:      IdentList ',' Identifier
                { $1.AddChild($3);  $$ = $1; }
        |       Identifier
                { $$ = AST.Kary(NodeType.IdList, LineNumber, $1); }
        ;

MethodDecl:     Kwd_public MethodAttr MethodType Identifier '(' OptFormals ')' Block
                { $$ = AST.NonLeaf(NodeType.Method, $4.LineNumber, $3, $4, $6, $8, $2); }
        ;

MethodAttr:     Kwd_static      { $$ = AST.Leaf(NodeType.Static, LineNumber); }
        |       Kwd_virtual     { $$ = AST.Leaf(NodeType.Virtual, LineNumber); }
        |       Kwd_override    { $$ = AST.Leaf(NodeType.Override, LineNumber); }
        ;

MethodType:     Kwd_void        { $$ = null; }
        |       Type            { $$ = $1; }
        ;

OptFormals:     /* empty */
                { $$ = AST.Kary(NodeType.FormalList, LineNumber); }
        |       FormalPars
                { $$ = $1; }
        ;

FormalPars:     FormalDecl
                { $$ = AST.Kary(NodeType.FormalList, LineNumber, $1); }
        |       FormalPars ',' FormalDecl
                { $1.AddChild($3);  $$ = $1; }
        ;

FormalDecl:     Type Identifier
                { $$ = AST.NonLeaf(NodeType.Formal, $2.LineNumber, $1, $2); }
        ;

Type:           TypeName
                { $$ = $1; }
        |       TypeName '[' ']'
                { $$ = AST.NonLeaf(NodeType.Array, $1.LineNumber, $1); }
        ;

TypeName:       Identifier          { $$ = $1; }
        |       BuiltInType         { $$ = $1; }
        ;

BuiltInType:    Kwd_int         { $$ = AST.Leaf(NodeType.IntType, LineNumber); }
        |       Kwd_string      { $$ = AST.Leaf(NodeType.StringType, LineNumber); }
        |       Kwd_char        { $$ = AST.Leaf(NodeType.CharType, LineNumber); }
        ;

Statement:      Designator '=' Expr ';'
                { $$ = AST.NonLeaf(NodeType.Assign, $1.LineNumber, $1, $3); }
        |       Designator '(' OptActuals ')' ';'
                { $$ = AST.NonLeaf(NodeType.Call, $1.LineNumber, $1, $3); }
        |       Designator PLUSPLUS ';'
                { $$ = AST.NonLeaf(NodeType.PlusPlus, $1.LineNumber, $1); }
        |       Designator MINUSMINUS ';'
                { $$ = AST.NonLeaf(NodeType.MinusMinus, $1.LineNumber, $1); }
        |       Kwd_if '(' Expr ')' Statement Kwd_else Statement
                { $$ = AST.NonLeaf(NodeType.If, $3.LineNumber, $3, $5, $7); }
        |       Kwd_if '(' Expr ')' Statement
                { var empty = AST.Leaf(NodeType.Empty, LineNumber);
                  $$ = AST.NonLeaf(NodeType.If, $3.LineNumber, $3, $5, empty); }
        |       Kwd_while '(' Expr ')' Statement
                 { $$ = AST.NonLeaf(NodeType.While, $3.LineNumber, $3, $5); }
        |       Kwd_break ';'
                { $$ = AST.Leaf(NodeType.Break, LineNumber); }
        |       Kwd_return ';'
                { $$ = AST.NonLeaf(NodeType.Return, LineNumber, null); }
        |       Kwd_return Expr ';'
                { $$ = AST.NonLeaf(NodeType.Return, $2.LineNumber, $2); }
        |       Block
                { $$ = $1; }
        |       ';'
                { $$ = AST.Leaf(NodeType.Empty, LineNumber); }
        ;

OptActuals:     /* empty */
                { $$ = AST.Kary(NodeType.ActualList, LineNumber); }
        |       ActPars
                { $$ = $1; }
        ;

ActPars:        ActPars ',' Expr
                { $1.AddChild($3);  $$ = $1; }
        |       Expr
                { $$ = AST.Kary(NodeType.ActualList, LineNumber, $1); }
        ;

Block:          '{' DeclsAndStmts '}'
                { $$ = $2; }
        ;

LocalDecl:      TypeName IdentList ';'
                { $$ = AST.NonLeaf(NodeType.LocalDecl, $1.LineNumber, $1, $2); }
        |       Identifier '[' ']' IdentList ';'
                { var type = AST.NonLeaf(NodeType.Array, $1.LineNumber, $1);
                  $$ = AST.NonLeaf(NodeType.LocalDecl, $1.LineNumber, type, $4); }
        |       BuiltInType '[' ']' IdentList ';'
                { var type = AST.NonLeaf(NodeType.Array, $1.LineNumber, $1);
                  $$ = AST.NonLeaf(NodeType.LocalDecl, $1.LineNumber, type, $4); }
        ;

DeclsAndStmts:   /* empty */
                { $$ = AST.Kary(NodeType.Block, LineNumber); }
        |       DeclsAndStmts Statement
                { $1.AddChild($2);  $$ = $1; }
        |       DeclsAndStmts LocalDecl
                { $1.AddChild($2);  $$ = $1; }
        ;

Expr:           Expr OROR Expr      { $$ = AST.NonLeaf(NodeType.Or, $1.LineNumber, $1, $3); }
        |       Expr ANDAND Expr    { $$ = AST.NonLeaf(NodeType.And, $1.LineNumber, $1, $3); }
        |       Expr EQEQ Expr      { $$ = AST.NonLeaf(NodeType.Equals, $1.LineNumber, $1, $3); }
        |       Expr NOTEQ Expr     { $$ = AST.NonLeaf(NodeType.NotEquals, $1.LineNumber, $1, $3); }
        |       Expr LTEQ Expr      { $$ = AST.NonLeaf(NodeType.LessOrEqual, $1.LineNumber, $1, $3); }
        |       Expr '<' Expr       { $$ = AST.NonLeaf(NodeType.LessThan, $1.LineNumber, $1, $3); }
        |       Expr GTEQ Expr      { $$ = AST.NonLeaf(NodeType.GreaterOrEqual, $1.LineNumber, $1, $3); }
        |       Expr '>' Expr       { $$ = AST.NonLeaf(NodeType.GreaterThan, $1.LineNumber, $1, $3); }
        |       Expr '+' Expr       { $$ = AST.NonLeaf(NodeType.Add, $1.LineNumber, $1, $3); }
        |       Expr '-' Expr       { $$ = AST.NonLeaf(NodeType.Sub, $1.LineNumber, $1, $3); }
        |       Expr '*' Expr       { $$ = AST.NonLeaf(NodeType.Mul, $1.LineNumber, $1, $3); }
        |       Expr '/' Expr       { $$ = AST.NonLeaf(NodeType.Div, $1.LineNumber, $1, $3); }
        |       Expr '%' Expr       { $$ = AST.NonLeaf(NodeType.Mod, $1.LineNumber, $1, $3); }
        |       UnaryExpr           { $$ = $1; }
        ;

UnaryExpr:      '-' Expr            { $$ = AST.NonLeaf(NodeType.UnaryMinus, $2.LineNumber, $2); }
        |       '+' Expr            { $$ = AST.NonLeaf(NodeType.UnaryPlus, $2.LineNumber, $2); }
        |       UnaryExprNotUMinus  { $$ = $1; }
        ;

UnaryExprNotUMinus:
                Designator
                { $$ = $1; }
        |       Designator '(' OptActuals ')'
                { $$ = AST.NonLeaf(NodeType.Call, $1.LineNumber, $1, $3); }
        |       Kwd_null
                { $$ = AST.Leaf(NodeType.Null, LineNumber); }
        |       AnInteger
                { $$ = $1; }
        |       AChar
                { $$ = $1; }
        |       AString
                { $$ = $1; }
        |       AString '.' Identifier // Identifier must be "Length"
                { $$ = AST.NonLeaf(NodeType.Dot, $1.LineNumber, $1, $3); }
        |       Kwd_new Identifier '(' ')'
                { $$ = AST.NonLeaf(NodeType.NewClass, $2.LineNumber, $2); }
        |       Kwd_new TypeName '[' Expr ']'
                { $$ = AST.NonLeaf(NodeType.NewArray, $2.LineNumber, $2, $4); }
        |       '(' Expr ')'
                { $$ = $2; }
        |       '(' Expr ')' UnaryExprNotUMinus                 // cast
                { $$ = AST.NonLeaf(NodeType.Cast, $2.LineNumber, $2, $4); }
        |       '(' BuiltInType ')' UnaryExprNotUMinus          // cast
                { $$ = AST.NonLeaf(NodeType.Cast, $2.LineNumber, $2, $4); }
        |       '(' BuiltInType '[' ']' ')' UnaryExprNotUMinus  // cast
                { var type = AST.NonLeaf(NodeType.Array, $2.LineNumber, $2);
                  $$ = AST.NonLeaf(NodeType.Cast, $2.LineNumber, type, $6); }
        ;

Designator:     Identifier Qualifiers
                { $$ = repNull($2,$1); }
        ;

Qualifiers:     '.' Identifier Qualifiers
                { var t = AST.NonLeaf(NodeType.Dot, LineNumber, null, $2);
                  $$ = repNull($3,t); }
        |       '[' Expr ']' Qualifiers
                { var t = AST.NonLeaf(NodeType.Index, LineNumber, null, $2);
                  $$ = repNull($4,t); }
        |       '[' ']' Qualifiers   // needed for cast syntax
                { var t = AST.NonLeaf(NodeType.Array, LineNumber, null);
                  $$ = repNull($3,t); }
        |       /* empty */
                { $$ = null; }
        ;

Identifier:     Ident   { $$ = AST.Leaf(NodeType.Ident, LineNumber, lexer.yytext); }
        ;

AnInteger:      IntConst
				{ int val = 0;
				  if (!int.TryParse(lexer.yytext, out val))
				      throw new Exception("Bad integer constant: "+lexer.yytext);
				  $$ = AST.Leaf(NodeType.IntConst, LineNumber, val);
				}
        ;

AChar:          CharConst   { $$ = AST.Leaf(NodeType.CharConst, LineNumber, lexer.yytext); }
        ;

AString:        StringConst { $$ = AST.Leaf(NodeType.StringConst, LineNumber, lexer.yytext); }
        ;

%%


// used to reorganize AST structure for a Designator
private AST repNull( AST tree, AST replacement ) {
    if (tree == null) return replacement;
    AST_nonleaf np = tree as AST_nonleaf;
    for( ; ; ) {
        if (np == null)
            throw new Exception("error restructuring Qualifiers");
        if (np[0] == null)
            break;
        np = np[0] as AST_nonleaf;
    }
    np[0] = replacement;
    return tree;
}

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


