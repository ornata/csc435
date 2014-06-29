/* Cb scanner */

%namespace  FrontEnd
%option     out:CbLexer.cs

using System.Collections.Generic;

%x CCOMMENT

DIGIT       [0-9]
LETTER      [a-zA-Z]
IDCHAR      [a-zA-Z0-9_]
WS          [ \t\n\r]
cChar       [^\'\\\n\r]
sChar       [^\"\\\n\r]
opChar      [\-+<>*/%:=;,.\[\]{}()]

%{

    IDictionary<string,Tokens> keywords;
    int commentNesting = 0;
    StreamWriter tokensListing = null;

    public int LineNumber { get{ return yyline; } }

    Tokens t;

    public override void yyerror( string errmsg, params object[] args ) {
        System.Console.Write("{0}: ", yyline);
        System.Console.WriteLine(errmsg, args);
    }

    public void TrackTokens( string tfile ) {
        try {
            tokensListing = new StreamWriter(tfile);
        } catch(Exception) {
             Console.WriteLine("* Unable to create output file: {0}", tfile);
        }
    }

    public void CleanUp() {
        if (tokensListing != null) {
            tokensListing.Close();
            tokensListing = null;
        }
    }

    // initialize the keyword table
    public void Initialize() {
        keywords = new Dictionary<string,Tokens>();
        keywords["break"] = Tokens.Kwd_break;
        keywords["char"] = Tokens.Kwd_char;
        keywords["class"] = Tokens.Kwd_class;
        keywords["const"] = Tokens.Kwd_const;
        keywords["else"] = Tokens.Kwd_else;
        keywords["if"] = Tokens.Kwd_if;
        keywords["int"] = Tokens.Kwd_int;
        keywords["new"] = Tokens.Kwd_new;
        keywords["null"] = Tokens.Kwd_null;
        keywords["override"] = Tokens.Kwd_override;
        keywords["public"] = Tokens.Kwd_public;
        keywords["return"] = Tokens.Kwd_return;
        keywords["static"] = Tokens.Kwd_static;
        keywords["string"] = Tokens.Kwd_string;
        keywords["using"] = Tokens.Kwd_using;
        keywords["virtual"] = Tokens.Kwd_virtual;
        keywords["void"] = Tokens.Kwd_void;
        keywords["while"] = Tokens.Kwd_while;
    }

    // lookup an identifier to see if it actually a keyword
    private Tokens LookupIdent( string id ) {
        Tokens result;
        if (keywords.TryGetValue(id,out result))
            return result;
        return Tokens.Ident;
    }

    // Output tracing information to a file for each token
    private void track( Tokens t ) {
        if (tokensListing == null) return;
        if ((int)t <= 127)
            tokensListing.Write("Token \"{0}\"", (char)t);
        else
            tokensListing.Write("Token.{0}", t);
        if (t == Tokens.Ident)
            tokensListing.WriteLine(", text=\"{0}\"", yytext);
        else if (t == Tokens.IntConst || t == Tokens.StringConst || t == Tokens.CharConst)
            tokensListing.WriteLine(", text={0}", yytext);
        else
            tokensListing.WriteLine();
    }
%}

%%

{LETTER}({IDCHAR})* { t = LookupIdent(yytext); track(t);  return (int)t; }

{DIGIT}+            { t = Tokens.IntConst; track(t);  return (int)t; }

'({cChar}|\\.)'     { t = Tokens.CharConst; track(t);  return (int)t; }

\"({sChar}|\\.)*\"  { t = Tokens.StringConst; track(t);  return (int)t; }

"&&"            { t = Tokens.ANDAND; track(t);  return (int)t; }
"||"            { t = Tokens.OROR; track(t);  return (int)t; }
"++"            { t = Tokens.PLUSPLUS; track(t);  return (int)t; }
"--"            { t = Tokens.MINUSMINUS; track(t);  return (int)t; }
"=="            { t = Tokens.EQEQ; track(t);  return (int)t; }
"!="            { t = Tokens.NOTEQ; track(t);  return (int)t; }
">="            { t = Tokens.GTEQ; track(t);  return (int)t; }
"<="            { t = Tokens.LTEQ; track(t);  return (int)t; }

{opChar}        { t = (Tokens)yytext[0]; track(t);  return (int)t; }

"//".*          {  /* do nothing */  }
{WS}+           {  /* do nothing */ }

"/*"            { commentNesting++; BEGIN(CCOMMENT); }

.               { Console.WriteLine("{0}: bad input character: {1}", yyline, yytext); }

/* Matching rules while a comment is active */
<CCOMMENT>"*/"      { if (--commentNesting == 0)  BEGIN(INITIAL);  }
<CCOMMENT>"/*"      { commentNesting++; }
<CCOMMENT>.         { /* do nothing */  }

%%
