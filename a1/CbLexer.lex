%namespace FrontEnd

%tokentype Tokens
%x STRING
%x BLOCKCOMMENT

%{
  public StringBuilder currentStringLiteral;

  private int currentLineNumber = 1;
  
  private int blockCommentNesting = 0;

  public int LineNumber { get{ return currentLineNumber; } }

  public override void yyerror( string msg, params object[] args ) {
    Console.WriteLine("{0}: ", LineNumber);
    if (args == null || args.Length == 0) {
      Console.WriteLine("{0}", msg);
    }
    else {
      Console.WriteLine(msg, args);
    }
  }

  public void yyerror( int lineNum, string msg, params object[] args ) {
    Console.WriteLine("{0}: {1}", msg, args);
  }

%}

space    [ \t]
id       [a-zA-Z][a-zA-Z0-9_]*
number   [0-9]+
opchar   [><+\-*/%=.,()\[\]{};:]

%%

{space}  {}
"\n\r"   {currentLineNumber++;}
\n       {currentLineNumber++;}

// Just read the whole line when a single-line comment comes in
"//".*                     {}

// When a block comment begins, enter the BLOCKCOMMENT state with nesting level 1.
"/*"                       {BEGIN(BLOCKCOMMENT);
                            blockCommentNesting = 1;}

<BLOCKCOMMENT>[^*/\n]*     {} // consume anything that isn't a * or a / or a newline
<BLOCKCOMMENT>\n           {currentLineNumber++;} // must keep track of line count in this state too.
<BLOCKCOMMENT>"/*"         {blockCommentNesting++;} // In the BLOCKCOMMENT state, seeing more instances of /* increases the nesting.
// at the end of every comment, decrease the nesting count and check if we need to go back to the initial state.
<BLOCKCOMMENT>"*/"         {blockCommentNesting--;
                            if (blockCommentNesting <= 0) {
                                BEGIN(INITIAL);
                            }}
// we've reached this point if we saw a * or / character but noticed it was neither /* nor */. just eat it.
<BLOCKCOMMENT>[*/]         {}

// Handle keywords
if       {return (int)Tokens.Kwd_if;}
while    {return (int)Tokens.Kwd_while;}
else     {return (int)Tokens.Kwd_else;}
break    {return (int)Tokens.Kwd_break;}
return   {return (int)Tokens.Kwd_return;}
class    {return (int)Tokens.Kwd_class;}
public   {return (int)Tokens.Kwd_public;}
static   {return (int)Tokens.Kwd_static;}
void     {return (int)Tokens.Kwd_void;}
virtual  {return (int)Tokens.Kwd_virtual;}
override {return (int)Tokens.Kwd_override;}
using    {return (int)Tokens.Kwd_using;}
const    {return (int)Tokens.Kwd_const;}
int      {return (int)Tokens.Kwd_int;}
new      {return (int)Tokens.Kwd_new;}
string   {return (int)Tokens.Kwd_string;}
char     {return (int)Tokens.Kwd_char;}
null     {return (int)Tokens.Kwd_null;}

{id}     {return (int)Tokens.Ident;}
{number} {return (int)Tokens.IntConst;}

// Plusplus, minusminus, and other multi-character operators.
"++" {return (int)Tokens.PLUSPLUS;}
"--" {return (int)Tokens.MINUSMINUS;}
"&&" {return (int)Tokens.ANDAND;}
"||" {return (int)Tokens.OROR;}
"<=" {return (int)Tokens.LTEQ;}
">=" {return (int)Tokens.GTEQ;}
"==" {return (int)Tokens.EQEQ;}
"!=" {return (int)Tokens.NOTEQ;}

"["[ \t\n\r]*"]" {return (int)Tokens.ARRAYDECL;
                  // have to maintain line count
                  if (yytext.Length > 0) {
                      currentLineNumber += yytext.Split('\n').Length - 1;
                  }}

{opchar} {return (int)(yytext[0]);}

// Literal character
\'.\' {yylval.charVal = yytext[1];
       return (int)Tokens.CharConst;}

// Upon seeing the start of a literal string, enter the STRING state, where the string's contents are accumulated.
\"                 {currentStringLiteral = new StringBuilder("");
                    BEGIN(STRING);}
// This rule handles seeing the ending quote character. It switches back to the initial state.
<STRING>\"         {yylval.strVal = currentStringLiteral.ToString();
                    BEGIN(INITIAL);
                    return (int)Tokens.StringConst;}
<STRING>\n         {yyerror("String constant not terminated by quote.");
                    currentLineNumber++;}
// handle escaped characters inside the string literal
// note: strangely, spec does not say that we should allow backslash to escape itself.
<STRING>\\n        {currentStringLiteral.Append("\n");}
<STRING>\\t        {currentStringLiteral.Append("\t");}
<STRING>\\r        {currentStringLiteral.Append("\r");}
<STRING>\\\"       {currentStringLiteral.Append("\"");}
<STRING>\\\'       {currentStringLiteral.Append("\'");}
<STRING>[^\\\n\"]+ {currentStringLiteral.Append(yytext);}

. { yyerror("Illegal character ({0})", yytext); }

%%
