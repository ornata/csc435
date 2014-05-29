using System;

class List {
    public List next;
    public virtual void Print() {}
}

class Other: List {
    public char c;
    public override void Print() {
        Console.Write(' ');
        Console.Write(c);
        if (next != null) next.Print();
    }
}

class Digit: List {
    public int d;
    public override void Print() {
        Console.Write(' ');
        Console.Write(d);
        if (next != null) next.Print();
    }
}

// empty class test
class EmptyClass
{
}

class Algorithm
{
    // note: slightly obfuscated in order to test keywords
    public static int Find(int[] haystack, int needle)
    {
        int i;
        i = 0;

        while (i < haystack.Length)
        {
            if (haystack[i] == needle)
            {
                break; // test of return of break
            }

            if (i > haystack.Length)
            {
                Console.WriteLine("Testing operator >");
            }

            i++;
        }

        if (i == haystack.Length)
        {
            i = 0;
            i--; // reandom use of -- for testing
        }

        ; // random empty statement for testing

        return i; // test of return with value
    }

    public static void SayHello()
    {
        Console.WriteLine("Hello!");
        return; // test of return with no value
    }

    /* intentionally commented out for the sake of testing
       // public static void 
       blah blah blash dsfsdf
    */
    
    /* nested comment test
        /*
        */
        sfthis should cause an error if nested comments don't work
        /*
        */
    */

    public static void LogicTest()
    {
        if (1 == 1 && 2 == 2 || 5 == 3)
        {
            Console.WriteLine("Logic!");
        }
    }

    public static void UnaryTest()
    {
        if (+1 == -(-1))
        {
            Console.WriteLine("Unary test!");
        }
    }

    public static void NewArrayTest()
    {
        int[] a;
        a = new int[50];
        a = null;
    }

    public static void CastTest()
    {
        int x;
        char c;
        x = 3;
        c = (int) x;
    }

    public static void PrecedenceTest()
    {
        int x;
        x = 2 + 5 * 3 * (2 - +3) + 3 % 5 / 3;
    }

    public static void LeadingZeroTest()
    {
        int x;
        x = 0003;
    }
}

class Lists {
    public const string PromptText = "enter some text =>";

    public static void Main() {
        // testing ambiguity of "ident["
        List[] listArray;
        listArray = new List[10];
        listArray[0] = new List();

        List ccc;
        string s;
        Console.WriteLine(PromptText);
        s = Console.ReadLine();
        ccc = null;
        int i;
        i = 0;
        while(i < s.Length) {
            char ch;
            ch = s[i];  i++;
            List elem;
            if (ch >= '0' && ch <= '9') {
                Digit elemD;
                elemD = new Digit();  elemD.d = ch - '0';
                elem = elemD;
            } else {
                Other elemO;
                elemO = new Other();  elemO.c = ch;
                elem = elemO;
            }
            elem.next = ccc;
            ccc = elem;
        }
        Console.WriteLine("\nReversed text =");
        ccc.Print();
        Console.WriteLine("\n\r\t\"\'"); // test of a bunch of escape characters
    }
}
