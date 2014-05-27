%namespace FrontEnd

%tokentype Tokens
%x STRING
%x COMMENT
%x SHORTCOMMENT

%{
  public StringBuilder str = new StringBuilder("");

  public int lineNum = 1;
  
  public int nesting = 0;

  public int LineNumber { get{ return lineNum; } }

  public override void yyerror( string msg, params object[] args ) {
    Console.WriteLine("{0}: ", lineNum);
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

ID [a-zA-Z][a-zA-Z0-9_]*
NUMBER [0-9]+
WS [ \t\n\r]
OPERATOR [><+\-*/%=]
SEMI \;
COLON :
%%

// Handle whitespace
{WS}+ {}

// Handle comments
"/*" { BEGIN(COMMENT); nesting = 0; }
<COMMENT>"/*" { nesting++; }
<COMMENT>[^*\n]* {} // consume anything that isn't a *
<COMMENT>"*"+[^*/\n]* {} // Found a * not followed by a /
<COMMENT>\n { lineNum = lineNum+1; } //keep track of line number
<COMMENT>"*"+"/" { nesting = nesting-1; if(nesting <= 0){ BEGIN(INITIAL); } }

"//" {BEGIN(SHORTCOMMENT);}
<SHORTCOMMENT>"\n" {BEGIN(INITIAL);}

// Handle integers and floats
{NUMBER} { return (int)Tokens.IntConst; }
{NUMBER}"."{NUMBER} { return (int)Tokens.FloatConst; }

// Handle keywords
if { return (int)Tokens.Kwd_if; }

while { return (int)Tokens.Kwd_while; }

else { return (int)Tokens.Kwd_else; }

break { return (int)Tokens.Kwd_break; }

return { return (int)Tokens.Kwd_return; }

class { return (int)Tokens.Kwd_class; }

public { return (int)Tokens.Kwd_public; }

static { return (int)Tokens.Kwd_static; }

void { return (int)Tokens.Kwd_void; }

virtual { return (int)Tokens.Kwd_virtual; }

override { return (int)Tokens.Kwd_override; }

using { return (int)Tokens.Kwd_using; }

const {return (int)Tokens.Kwd_const; }

int { return (int)Tokens.Kwd_int; }

new { return (int)Tokens.Kwd_new; }

string { return (int)Tokens.Kwd_string; }

char { return (int)Tokens.Kwd_char; }

null { return (int)Tokens.Kwd_null; }

// Plusplus, minus minus
"++" { return (int)Tokens.PLUSPLUS; }

"--" { return (int)Tokens.MINUSMINUS; }

// Identifiers
{ID} { return (int)Tokens.Ident; }

{OPERATOR} { return (int)(yytext[0]); } 

// Strings
\" { BEGIN(STRING);}

<STRING>\" { Console.WriteLine(str);
  yylval.strVal = str.ToString();
             str = new StringBuilder("");
             BEGIN(INITIAL);
             return (int)Tokens.StringConst; }

<STRING>\n { yyerror("String constant not terminated by quote."); }
<STRING>\\n { str.Append("\n"); }
<STRING>\\t { str.Append("\t"); }
<STRING>\\r { str.Append("\r"); }
<STRING>\\b { str.Append("\b"); }
<STRING>\\f { str.Append("\f"); }
<STRING>\\(.|\n) { str.Append(yytext[0]); }
<STRING>[^\\\n\"]+ { str.Append(yytext); }


"&&"|"||"|"." { return (int)(yytext[0]); }

"<="|">="|"=="|"!=" { return (int)(yytext[0]); }

// Braces
"(" {return (int)(yytext[0]);}
")" {return (int)(yytext[0]);}

"[" {return (int)(yytext[0]);}
"]" {return (int)(yytext[0]);}

"{" {return (int)(yytext[0]);}
"}" {return (int)(yytext[0]);} 

{SEMI}|{COLON} { return (int)(yytext[0]); }

<<EOF>> { return 0; }

. { yyerror("Illegal character ({0})", yytext); }

%%
