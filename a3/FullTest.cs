using System;

class Foo {
    public const int theAnswer = 42;
    public const string hiThere = "hello";
    public const char itsAnX = 'x';

    public int a;
    public string b;
    public char c;

    public virtual void NotMain() {
        Foo f;
        f = this; // should compile

        // this is immutable.
        this = this; // should not compile
        this = f; // should not compile

        Bar b;
        b = this; // should not compile (type mismatch, needs downcast)

        string s;
        s = hiThere;
    }

    public static void SomeStaticFun() {
    }

    public static void Main() {
        SomeStaticFun(); // should compile
        NotMain(); // should not compile (calling member function in static function)

        Foo f;
        f = null;
        f = this; // should not compile (this used in static)
        f = new Bar();
        int r;
        r = null; // should not compile
        r = a; // should not compile (a is not static)

        r = f.Umm(3,4); // should not compile (Foo has no Umm method)
        r = f.Ummm(3,4); // should compile
        r = f.Ummm(34); // should not compile (wrong number of arguments)
        r = f.Ummm("asdf",4); // should not compile (wrong types for arguments)
        f.SomeStaticFun(); // should not compile (static function called as a member)
        Foo.SomeStaticFun(); // should compile

        string s;
        s = null; // should not compile
        int x;
        x = 0;
        int y;
        y = 1;
        string str;
        str = "hello";
        string str2;
        str2 = "world";
        string str3;
        str3 = str + str2; // should work
        str3 = str + x; // shouldn't work

        int[] array;
        int[] array2;
        array = new int[7]; // should work
        array2 = new System[2]; // shouldn't work

        if (x <= y) {
            x = 2;
        }

        if(x < y && x > 2) {
            str = "goodbye";
        }

        if (x && y) { // shouldn't compile
            y = 22;
        }

        if (x > str) { // shouldn't compile
            y = 7;
        }

        int uplus;
        uplus = +x;
        int uminus;
        uminus = -y;
        str2 = -str3; // shouldn't compile

        x++; // should be fine
        str++; // shouldn't work

        y = new System(); // should not compile

        while (x < 10) { // should compile
            x++;
        }

        while (str) { // shouldn't compile
            x++;
        }

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

    // should not compile: Bar.Umm does not override Foo.Ummm
    public override int Umm( int aa, int bb ) {
        System.Console.WriteLine("This is Bar");

        return a-b; // should error (a,b are inherited, but a is int and b is string)
        return a-c; // should not error (a,b are inherited. a is int, c is char)

        return c; // should not error (c is inherited)

        return aa * bb; // should not error (both are locals)
    }

    // should compile (Bar.Ummm overrides Foo.Ummm)
    public override int Ummm( int aa, int bb ) {
        return aa / bb;
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
