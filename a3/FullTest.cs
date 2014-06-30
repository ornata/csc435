using System;

class Foo {
    public const int theAnswer = 42;
    public const string hiThere = "hello";
    public const char itsAnX = 'x';

    public int a;
    public string b;
    public char c;

    public static void Main() {
        Foo f;
        f = null;
        f = new Bar();
        int r;
        r = null; // should not compile
        r = f.Umm(3,4);
        string s;
        s = null; // should not compile

        int L;
        L = "hello".Length;

        // string literals should only have the Length member
        L = "hello".Substring; // should error with message saying Length is the only property allowed on literals
        L = "hello".Wength; // should error with general "member not found"
        L = hiThere.Substring; // should error with message saying assigned type doesn't match

        // should not be able to access non-existent string member
        hiThere.Wength = 3; // should error

        // should not be able to assign to string length
        hiThere.Length = 3; // should error

        int[] arr;
        arr = new int[25];
        L = arr.Length;

        int[] arr2;
        arr2 = arr;

        // should not be able to access non-existent array member
        L = arr.Wength; // should error

        // should not be able to assign to array length
        arr.Length = 10; // should error

        Object o;
        o = null; // should compile
        o = arr; // should compile
        o = f; // should compile
        o = L; // shouldn't compile
        o = hiThere; // should compile (?)

        // should not be able to assign to a const
        theAnswer = 62; // should error

        return 5; // should error (returning non-void from void function)

        return; // should not error (void return from void function)
    }

    public virtual int Ummm( int a, int b ) {
        System.Console.WriteLine("This is Foo");

        return; // should error (void return from non-void function)

        return "blah"; // should error (incorrect type return)

        return a+b;
    }

}


class Bar : Foo {
    public int x;

    public override int Umm( int aa, int bb ) {
        System.Console.WriteLine("This is Bar");
        return a-b;
    }
}

// class with direct cyclic inheritance dependency
class Loop : Loop {
}

// class with 1-level cyclic inheritance dependency
class Loop1 : Loop2 {
}

class Loop2 : Loop1 {
}
