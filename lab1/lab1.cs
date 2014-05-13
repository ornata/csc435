// CSc 435 - 2012 : Lab 1

using System;
using System.IO;

public class ScannerException : System.Exception
{
  // This code copied from here: (only the names have changed)
  // http://msdn.microsoft.com/en-us/library/ms173163.aspx
  public ScannerException() : base() { }
  public ScannerException(string message) : base(message) { }
  public ScannerException(string message, System.Exception inner) : base(message, inner) { }

  // A constructor is needed for serialization when an 
  // exception propagates from a remoting server to the client.  
  protected ScannerException(System.Runtime.Serialization.SerializationInfo info,
    System.Runtime.Serialization.StreamingContext context) { }
}

public enum Token
{
  EOF,     // end of file
  ERR,     // unknown character

  ADD,     // [+]
  DIV,     // [/]    or could this be the beginning of a comment //...
  ASSIGN,  // [=]
  EQ,      // [==]
  ADDA,    // [+=]

  KWDIF,   // "if"
  KWDGOTO, // "goto"
  ID,      // identifiers: [a-zA-Z][a-zA-Z0-9_]*

  INT,     // integers: [1-9][0-9]* | [0] | [0][x][0-9a-fA-F] | ...
  FLT,     // floats: ( [0] | [1-9][0-9]*[.][0-9]* )  
};

class Scanner
{
  string buffer = null;
  int pos = -1;  // absolute position in buffer
  int line = -1; // line number in text file
  int cpos = -1; // position on line
  // some information about the current token that may be
  // requested by the user 
  string token_text = "";
  int token_line = -1;
  int token_cpos = -1;
  
  // a few properties for 
  public int TokenLine {get{return token_line;}}
  public int TokenPos  {get{return token_cpos;}}
  public string Text   {get{return token_text;}}

  public Scanner(string filename) {
    StreamReader stream = new StreamReader(filename);
    buffer = stream.ReadToEnd();
    stream.Close();
    pos = 0;
    line = 0;
    cpos = 0;
  }

  // a possible whitespace skipper that stops on the newline character, beware
  // of the '\r\n' combination in dos
  private void SkipWhiteSpace() {
    if (pos < 0) return;
    while (pos < buffer.Length) {
      char c = PeekChar();
      if (c == '/' && LookAhead(1) == '/') {
        SkipToEOL();
        if (pos < buffer.Length)
          c = PeekChar(); // reset c as position has moved
        else
          return;
      }

      if (!Char.IsWhiteSpace(c))
        return;
      Advance();
    }
  }

  // ignore everything until the end of the line
  private void SkipToEOL() {
    if (pos < 0) return;
    while (pos < buffer.Length) {
      if (PeekChar() == '\n')
        return;
      Advance();
    }
  }
  // advance the position in the buffer by 1
  // also, keep track of line and character position 
  private void Advance() {
    if (PeekChar() == '\n') {++line; cpos=-1;}
    ++pos; ++cpos;
  }

  // get current character and advance file position
  private char PopChar() {
    char c = PeekChar();
    Advance();
    return c;
  }

  // get current character, do not advance
  private char PeekChar() {return buffer[pos];}

  // get character at position + offset
  private char LookAhead(uint offset) {
    return ((pos+offset) < buffer.Length) ? buffer[(int)(pos+offset)] : '\0';
  }

  public bool HasNext() {return pos < buffer.Length;}

  public Token Next() {
    if (buffer == null) return Token.EOF;
    // ignore leading whitespace
    SkipWhiteSpace();
    // save some token info for the user
    token_line = line;
    token_cpos = cpos;
    // SkipWhiteSpace may have eaten everything
    if (!HasNext()) {
      token_text = "";
      return Token.EOF;
    }
    // token starts here in the buffer, for saving text
    int start = pos;
    // get the first character
    char c = PopChar();

    Token tok = Token.ERR; // or unknown token
    switch(c) {
      case '+':
          if (HasNext() && PeekChar() == '=') {
              PopChar(); // eat the '=' too
              tok = Token.ADDA;
          } else {
              tok = Token.ADD;
          }
      break;
      case '/': tok = Token.DIV; break;
      case '=':
          if (HasNext() && PeekChar() == '=') {
              PopChar(); // eat the '=' too
              tok = Token.EQ;
          } else {
              tok = Token.ASSIGN;
          }
      break;
      default : 
      string tokentext = "";
      if (char.IsLetter(c)) {
          // read identifier
          tokentext += c;
          while (HasNext()) {
              c = PeekChar();
              if (char.IsLetterOrDigit(c) || c == '_') {
                  PopChar();
                  tokentext += c;
              } else {
                  break;
              }
          }

          // check for keywords and fall back to identifier
          if (tokentext == "if") {
               tok = Token.KWDIF;
          } else if (tokentext == "goto") {
               tok = Token.KWDGOTO;
          } else {
               tok = Token.ID;
          }
      } else if (char.IsDigit(c)) {
          // read number
          bool isFloating = false;
          int numericalBase = 10;
          tokentext += c;
          while (HasNext()) {
              c = PeekChar();
              if (c == '.') {
                  PopChar();
                  tokentext += c;
                  if (isFloating) {
                      tok = Token.ERR;
                      break;
                  } else {
                      isFloating = true;
                  }
              } else if (tokentext.Length == 1
                  && tokentext[0] == '0'
                  && (c == 'x' || c == 'X')) {
                  PopChar();
                  tokentext += c;
                  numericalBase = 16;
              } else if (tokentext.Length == 1
                  && tokentext[0] == '0'
                  && c == '0') {
                  PopChar(); 
                  tokentext += c;
                  numericalBase = 8;
              } else if (numericalBase == 16
                  && ((c >= 'a' && c <= 'f')
                      || (c >= 'A' && c <= 'F')
                      || (c >= '0' && c <= '9'))) {
                  PopChar();
                  tokentext += c;
              } else if (numericalBase == 8
                  && (c >= '0' && c <= '7')) {
                  PopChar();
                  tokentext += c;
              } else if (numericalBase == 10
                  && (c >= '0' && c <= '9')) {
                  PopChar();
                  tokentext += c;
              } else if (isFloating) {
                  tok = Token.FLT;
                  break;
              } else {
                  tok = Token.INT;
                  break;
              }
          }
      }
      break;
    }
    // this information is necessary for identifiers or integers
    // but is a bit expensive for keywords, and known symbols
    token_text = buffer.Substring(start,pos-start);
    return tok;
  }
}

class Test
{
  public static void Main(string[] args)
  {
    if (args.Length != 1) {
      Console.WriteLine("Usage: ./lab1 [filename]");
      return;
    }
    Scanner lex = new Scanner(args[0]);
    while (lex.HasNext()) {
      Token t = lex.Next();
      int l = lex.TokenLine;
      int c = lex.TokenPos;
      string s = lex.Text;
      if (s == null) s = "";
      Console.WriteLine("TOKEN[{0}:{1}]: {2} : \"{3}\" : ",l,c,t,s);
    }
  }
}

