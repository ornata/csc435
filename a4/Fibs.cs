// Sample program for testing use of arrays
//
// It uses recursion instead of while loops
// It uses k = k+1  instead of  k++
//
// Program to generate first 20 Fibonacci numbers.

using System;

class Fibs {
    public int[] nums;
    public const int max = 20;

    public virtual void NextFib( int k ) {
        nums[k] = nums[k-1] + nums[k-2];
        k = k + 1;
        //Console.Write("* k = ");
        //Console.WriteLine(k);
        if (k < nums.Length) NextFib(k);
    }

    public virtual void Print( int k ) {
        Console.Write(nums[k]);
        k = k + 1;
        if (k < nums.Length) {
            Console.Write(", ");
            Print(k);
        } else {
            Console.WriteLine(".");
        }
    }

    public virtual void Run() {
        nums = new int[max];
        //Console.Write("* nums.Length= ");
        //Console.WriteLine(nums.Length);
        nums[0] = 1;
        nums[1] = 1;
        NextFib(2);
        Print(0);
    }

    public static void UnaryMinusTest(int x) {
        Console.Write("Should be -");
        Console.Write(x);
        Console.Write(": ");
        x = -x;
        Console.WriteLine(x);
    }

    public static void PlusPlusMinusMinusTest(int x) {
        Console.Write("x: ");
        Console.WriteLine(x);
        Console.Write("x++: ");
        x++;
        Console.WriteLine(x);
        Console.Write("x--: ");
        x--;
        x--;
        Console.WriteLine(x);
    }

    public static void AndAndOrOrTest(int t, int f) {
        if (t && t) {
            Console.WriteLine("t && t Should print");
        }

        if (t && f) {
            Console.WriteLine("t && f Should not print"); 
        }
        
        if (f && t) {
            Console.WriteLine("f && t Should not print"); 
        }

        if (f && f) {
            Console.WriteLine("f && f Should not print"); 
        }

        if (t || t) {
            Console.WriteLine("t || t Should print");
        }

        if (t || f) {
            Console.WriteLine("t || f Should print"); 
        }
        
        if (f || t) {
            Console.WriteLine("f || t Should print"); 
        }

        if (f || f) {
            Console.WriteLine("f || f Should not print"); 
        }
    }

    public static void WhileTest(int k) {
        int i;
        i = 0;
        while (i < k) {
            int j;
            j = 0;
            while (j < k) {
                Console.Write(i);
                Console.Write(",");
                Console.WriteLine(j);
                j++;
            }
            i++;
        }

        i = 0;
        while (i < k) {
            if (i < 7 && i > 2) {
                        Console.WriteLine(i);
            } else {
                Console.WriteLine(k);
            }
            i++;
        }
    }

    public static void Main() {
        UnaryMinusTest(3);
        PlusPlusMinusMinusTest(3);
                AndAndOrOrTest(1,0);
        WhileTest(10);
        Fibs f;
        f = new Fibs();
        f.Run();
    }
}

