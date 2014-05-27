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
%union { public string strVal; }

// All tokens which can be used as operators in expressions
// they are ordered by precedence level (lowest first)
%right      '='
%left       OROR
%left       ANDAND
%nonassoc   EQEQ NOTEQ
%nonassoc   '>' GTEQ '<' LTEQ
%left       '+' '-'
%left       '*' '/' '%'
%left       UMINUS

%token <strVal> StringConst

// All other named tokens (i.e. the single character tokens are omitted)
// The order in which they are listed here does not matter.
%token      Kwd_break Kwd_class Kwd_const Kwd_else Kwd_if Kwd_int Kwd_char
%token      Kwd_new Kwd_public Kwd_return Kwd_static Kwd_string
%token      Kwd_override Kwd_virtual Kwd_null
%token      Kwd_using Kwd_void Kwd_while
%token      PLUSPLUS MINUSMINUS Ident FloatConst IntConst

%start Program

%%

/* *************************************************************************
   *                                                                       *
   *         PRODUCTION RULES AND ASSOCIATED SEMANTIC ACTIONS              *
   *                                                                       *
 ************************************************************************* */

/* Program
**********************
*
* {"using" ident ";"} ClassDecl {ClassDecl}
* 
********************************************/

Program:        UsingList ClassList
        ;

UsingList:      Kwd_using Ident ';' UsingList
        |       /*empty*/
        ;

ClassList:		ClassDecl ClassList
        |		ClassDecl
        ;

/* ClassDecl
***************************
*
* "class" ident [":" ident] "{" {MemberDecl} "}"
*
********************************************/

ClassDecl:		Kwd_class OptColon '{' DeclList '}'
	    ;

OptColon: 		':' Ident 
        |  		/* empty */ 
        ;

DeclList:       MemberDecl DeclList
        |       /* empty */
        ;
        
/* MemberDecl
***************************
*
* ConstDecl | FieldDecl | MethodDecl
*
********************************************/

MemberDecl:     ConstDecl
        |       FieldDecl
        |       MethodDecl
        ;

/* ConstDecl
***************************
*
* "public" "static" "const" Type ident "=" (number|stringConst) ";"
*
********************************************/

ConstDecl:      Kwd_public Kwd_static Kwd_const Type Ident '=' InitVal ';'
        ;

InitVal:        Number
        |       StringConst
        ;

/* FieldDecl
***************************
*
* "public" Type ident {"," ident} ";"
*
********************************************/

FieldDecl:      Kwd_public Type Ident IdentList ';'
        ;

IdentList:      ',' Ident IdentList
        |       /*empty*/
        ;

/* MethodDecl
***************************
*
* "public" ("static" | "virtual" | "override") ("void" | Type) ident
* "("{FormalPars}")" Block
*
********************************************/

MethodDecl:     Kwd_public MethodScope MethodType Ident '(' OptFormals ')' Block
        ;

MethodScope:    Kwd_static
        |       Kwd_virtual
        |       Kwd_override
        ;

MethodType:     Kwd_void
        |       Type
        ;

OptFormals:     /* empty */
        |       FormalPars
        ;


/* LocalDecl
***************************
*
* Type ident {"," ident} ";"
*
********************************************/

LocalDecl:      Type Ident IdentList ';'
        ;

/* FormalPars
***************************
*
* FormalDecl {"," FormalDecl}
*
********************************************/

FormalPars:     FormalDecl FormalDeclList
          ;
        
FormalDeclList: ',' FormalPars FormalDeclList
	  |		/* empty */
	  ;

/* FormalDecl
***************************
*
* Type ident
*
********************************************/

FormalDecl:     Type Ident
        ;

/* Type
***************************
*
* (ident|"int"|"string"|"char")["[""]"]
*
********************************************/

Type:			TypeName OptBraces
        ;

TypeName:       Ident
        |       Kwd_int
        |       Kwd_string
        |       Kwd_char
        ;

OptBraces:     /*empty*/
        |      '[' ']'
        ;

/* Statement
***************************
*
*   Designator "=" Expr ";"
* | "if" "(" Condition ")" Statement ["else" Statement]
* | "while" "(" Condition ")" Statement
* | "break" ";"
* | "return" [Expr] ";"
* | Designator "(" ActualPars ")" ";"
* | Designator ("++" | "--") ";"
* | Block
* | ";"
*
********************************************/

Statement:      Designator '=' Expr ';'
        |       Kwd_if '(' Condition ')' Statement OptElsePart
        |       Kwd_while '(' Condition ')' Statement
        |       Kwd_break ';'
        |       Kwd_return OptExpr ';'
        |       Designator '(' ActPars ')' ';'
        |       Designator PLUSPLUS ';'
        |       Designator MINUSMINUS ';'
        |       Block
        |       ';'
        ;


OptActuals:     /* empty */
        |       ActPars
        ;

OptElsePart:    Kwd_else Statement
        |       /* empty */
        ;

/* Block
***************************
*
* "{" {LocalDecl | Statment} "}"
*
********************************************/

Block:          '{' DeclsAndStmts '}'
        ;

DeclsAndStmts:   /* empty */
        |       DeclsAndStmts Statement
        |       DeclsAndStmts LocalDecl
        ;

/* ActPars
***************************
*
* Expr { "," Expr}
*
********************************************/

ActPars:        Expr OptExpr
        ;

OptExpr:        /*empty*/
        |       ',' Expr
        ;

/* Condition
***************************
*
* CondTerm {"||" CondTerm}
*
********************************************/

Condition:      CondTerm CondTermList
        ;

CondTermList:   /* empty */
        |       OROR CondTerm CondTermList
        ;

/* CondTerm
***************************
*
* CondFact {"&&" CondFact}
*
********************************************/

CondTerm:       CondFact CondFactList
        ;

CondFactList:   /* empty */
        |       ANDAND CondFact CondFactList
        ;

/* CondFact
***************************
*
* EqFact EqOp EqFact
*
********************************************/

CondFact:       EqFact EqOp EqFact
        ;

/* EqFact
***************************
*
* Expr RelOp Expr
* | ["+"|"-"] Term {Addop Term}
*
********************************************/

EqFact:         Expr RelOp Expr
        |       '+' Term TermList
        |       '-' Term TermList
        ;

TermList:       AddOp Term TermList
        |       /* empty */
        ;

/* Term
***************************
*
* Factor {MulOp Factor}
*
********************************************/

Term:           Factor MulList
        ;

MulList:        /* empty */
        |       MulOp Factor MulList 
        ;

/* Factor
***************************
*
*   Designator ["(" [ActPars] ")"]
* | intConst
* | StringConst ["." ident]
* | "new" ident "[" Expr "]"
* | "new" ident "(" ")"
* | "null"
* | "(" Type ")" Factor
* | "(" Expr ")"
*
********************************************/

Factor:         Designator OptParams
        |       IntConst
        |       StringConst OptIdent
        |       Kwd_new Ident '[' Expr ']'
        |       Kwd_new Ident '(' ')'
        |       Kwd_null
        |       '(' Type ')' Factor
        |       '(' Expr ')'
        ;

OptParams:      '(' OptActuals ')'
        |       /* empty */
        ;

OptIdent:       '.' Ident
        |       /* empty */
        ;

/* Designator
***************************
*
* ident {"." ident | "[" Expr "]"}
*
********************************************/

Designator:     Ident Qualifiers
        ;

Qualifiers:     '.' Ident Qualifiers
        |       '[' Expr ']' Qualifiers
        |       /* empty */
        ;

/* EqOp
***************************
*
* "==" | "!="
*
********************************************/

EqOp:           EQEQ
        |       NOTEQ
        ;

/* RelOp
***************************
*
* ">" | ">=" | "<" | "<="
*
********************************************/

RelOp:          '>'
        |       '<'
        |       GTEQ
        |       LTEQ
        ;

/* AddOp
***************************
*
* "+" | "-"
*
********************************************/

AddOp:          '+'
        |       '-'
        ;

/* MulOp
***************************
*
* "*" | "/" | "%"
*
********************************************/

MulOp:          '*'
        |       '/'
        |       '%'
        ;

// Expr

/*
Expr:           Operation
        ;

Operation:      Item
        |       Operation ANDAND Item {$$ = $1 && $3;}
        |       Operation OROR Item {$$ = $1 || $3;}
        |       Operation '>' Item {$$ = $1 > $3;}
        |       Operation '<' Item {$$ = $1 < $3;}
        |       Operation GTEQ Item {$$ = $1 >= $3;}
        |       Operation LTEQ Item {$$ = $1 <= $3;}
        |       Operation '+' Item {$$ = $1 + $3;}
        |       Operation '-' Item {$$ = $1 - $3;}
        |       Operation '*' Item {$$ = $1 * $3;}
        |       Operation '/' Item {$$ = $1 / $3;}
        |       Operation '%' Item {$$ = $1 % $3;}
        |       '-' Operation %prec UMINUS {$$ = -$2;}
        ;

Item:           Atom
        |       Designator
        |       Designator '(' OptActuals ')'
        ;

Atom:           Number
        |       StringConst
        |       StringConst '.' Ident
        |       Kwd_new Ident '(' ')'
        |       Kwd_new Ident '[' Expr ']'
        |       '(' Expr ')' {$$ = $2;}
        ;

 */

// We can have integers or floats...

Number:         IntConst
        |       FloatConst
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
        |       '-' Expr %prec UMINUS
        |       Designator
        |       Designator '(' OptActuals ')'
        |       Number
        |       StringConst
        |       StringConst '.' Ident // Ident must be "Length"
        |       Kwd_new Ident '(' ')'
        |       Kwd_new Ident '[' Expr ']'
        |       '(' Expr ')'
        ;

%%

public Parser(string filename, Scanner sc) : base(sc){}




