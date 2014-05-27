using FrontEnd;
using System;

class cbc
{
    public bool DebugEnabled = false;
    public bool TokensEnabled = false;
    public string InputFilename;

	public void Run()
	{
		Scanner sc = new Scanner();
		Parser p = new Parser(InputFilename, sc);

		if (p.Parse())
		{
			System.Console.WriteLine("Parse successful!");
		}
		else
		{
			System.Console.WriteLine("Parse unsuccessful.");
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
                    System.Console.WriteLine("No arguments may follow the first filename.");
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
