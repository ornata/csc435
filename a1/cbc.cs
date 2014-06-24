using FrontEnd;
using System.IO;
using System;

class cbc
{
    public bool DebugEnabled = false;
    public bool TokensEnabled = false;
    public string InputFilename;

    public void Run()
    {
        Scanner sc = new Scanner(new FileStream(InputFilename, FileMode.Open));

        if (TokensEnabled)
        {
            using (StreamWriter tokens = new StreamWriter("tokens.txt"))
            {
                int tok;
                for (tok = sc.yylex(); tok != (int) Tokens.EOF; tok = sc.yylex())
                {
                    string enumname = Enum.GetName(typeof(Tokens), tok);
                    if (enumname == null)
                    {
                        tokens.WriteLine("text = {0}", sc.yytext);
                    }
                    else
                    {
                        tokens.WriteLine("{0}, text = {1}",
                                         enumname, sc.yytext);
                    }
                }
            }
        }
        else
        {
            Parser p = new Parser(InputFilename, sc);

            if (p.Parse())
            {
                Console.WriteLine("{0} lines from file {1} were parsed successfully.",
                                         sc.LineNumber, InputFilename);
            }
            else
            {
                Console.WriteLine("Failed to parse {0}", InputFilename);
            }
        }
    }

    public static int Main(string[] args)
    {
        bool debugEnabled = false;
        bool tokensEnabled = false;
        string inputFilename = null;

        for (int arg = 0; arg < args.Length; arg++)
        {
            if (args[arg] == "-tokens")
            {
                tokensEnabled = true;
            }
            else if (args[arg] == "-debug")
            {
                debugEnabled = true;
            }
            else
            {
                if (arg + 1 == args.Length)
                {
                    inputFilename = args[arg];
                }
                else
                {
                    Console.WriteLine("No arguments may follow the first filename.");
                    return 1;
                }
            }
        }

        cbc cbcInstance = new cbc();
        cbcInstance.DebugEnabled = debugEnabled;
        cbcInstance.TokensEnabled = tokensEnabled;
        cbcInstance.InputFilename = inputFilename;

        cbcInstance.Run();

        return 0;
    }
}
