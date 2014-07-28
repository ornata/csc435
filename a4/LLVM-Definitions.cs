/* LLVM-Definitions.cs
 * 
 * Predefined functions and strings which need to be included in the generated
 * LLVM output file.
 * 
 * Author: Nigel Horspool
 * Date: July 2014
 */
 
using System;
using System.IO;
using System.Collections.Generic;


namespace FrontEnd
{
    public partial class LLVM
    {


    // predefined code to emit
    static string[] predefined = {
        // in the following strings, {0} is replaced with pointer/int size (32 or 64)
        // and {1} is replaced with the alignment needed for pointers (4 or 8).
    	"declare i{0} @strlen(i8*) #1",
    	"declare i8* @malloc(i{0}) #1",
    	"declare i8* @strcpy(i8*, i8*) #1",
    	"declare i8* @strcat(i8*, i8*) #1",
        "declare i8* @strncpy(i8*, i8*, i{0}) #1",
    	"declare i{0} @printf(i8*, ...) #1",
    	"declare i8* @gets(i8*) #1",
        "declare i{0} @atoi(i8*) #1",
    	"declare void @llvm.memset.p0i8.i{0}(i8*, i8, i{0}, i32, i1)",
    	"",
    	"define i8* @String.Concat(i8* %s1, i8* %s2) #0 {{",
    	"entry:",
    	"  %0 = call i{0} @strlen(i8* %s1)",
    	"  %1 = call i{0} @strlen(i8* %s2)",
    	"  %2 = add i{0} %0, %1",
    	"  %3 = add i{0} %2, 1",
    	"  %4 = call i8* @malloc(i{0} %3)",
    	"  %5 = call i8* @strcpy(i8* %4, i8* %s1)",
    	"  %6 = call i8* @strcat(i8* %5, i8* %s2)",
    	"  ret i8* %6",
    	"}}",
        "",
    	"@.fmts = private unnamed_addr constant [3 x i8] c\"%s\\00\", align 1",
    	"@.fmti = private unnamed_addr constant [3 x i8] c\"%d\\00\", align 1",
    	"@.fmtc = private unnamed_addr constant [3 x i8] c\"%c\\00\", align 1",
    	"@.fmtsn = private unnamed_addr constant [4 x i8] c\"%s\\0A\\00\", align 1",
    	"@.fmtin = private unnamed_addr constant [4 x i8] c\"%d\\0A\\00\", align 1",
    	"@.fmtcn = private unnamed_addr constant [4 x i8] c\"%c\\0A\\00\", align 1",
        "",
    	"define void @Console.WriteString(i8* %s1) #0 {{",
    	"entry:",
    	"  %0 = bitcast [3 x i8]* @.fmts to i8*",
    	"  %1 = call i{0} (i8*, ...)* @printf(i8* %0, i8* %s1)",
    	"  ret void",
    	"}}",
    	"",
    	"define void @Console.WriteInt(i32 %i) #0 {{",
    	"entry:",
    	"  %0 = bitcast [3 x i8]* @.fmti to i8*",
    	"  %1 = call i{0} (i8*, ...)* @printf(i8* %0, i32 %i)",
    	"  ret void",
    	"}}",
    	"",
    	"define void @Console.WriteChar(i8 %c) #0 {{",
    	"entry:",
    	"  %0 = bitcast [3 x i8]* @.fmtc to i8*",
    	"  %1 = call i{0} (i8*, ...)* @printf(i8* %0, i8 %c)",
    	"  ret void",
    	"}}",
    	"",
    	"define void @Console.WriteLineString(i8* %s1) #0 {{",
    	"entry:",
    	"  %0 = bitcast [4 x i8]* @.fmtsn to i8*",
    	"  %1 = call i{0} (i8*, ...)* @printf(i8* %0, i8* %s1)",
    	"  ret void",
    	"}}",
    	"",
    	"define void @Console.WriteLineInt(i32 %i) #0 {{",
    	"entry:",
    	"  %0 = bitcast [4 x i8]* @.fmtin to i8*",
    	"  %1 = call i{0} (i8*, ...)* @printf(i8* %0, i32 %i)",
    	"  ret void",
    	"}}",
    	"",
    	"define void @Console.WriteLineChar(i8 %c) #0 {{",
    	"entry:",
    	"  %0 = bitcast [4 x i8]* @.fmtcn to i8*",
    	"  %1 = call i{0} (i8*, ...)* @printf(i8* %0, i8 %c)",
    	"  ret void",
    	"}}",
        "",
        "define i8* @String.Substring(i8* %s, i32 %start, i32 %end) #0 {{",
        "entry:",
        "  %s.addr = alloca i8*, align {1}",
        "  %start.addr = alloca i32, align 4",
        "  %end.addr = alloca i32, align 4",
        "  %n = alloca i32, align 4",
        "  %r = alloca i8*, align {1}",
        "  store i8* %s, i8** %s.addr, align {1}",
        "  store i32 %start, i32* %start.addr, align 4",
        "  store i32 %end, i32* %end.addr, align 4",
        "  %0 = load i32* %end.addr, align 4",
        "  %1 = load i32* %start.addr, align 4",
        "  %sub = sub nsw i32 %0, %1",
        "  %add = add nsw i32 %sub, 1",
        "  store i32 %add, i32* %n, align 4",
        "  %2 = load i32* %n, align 4",
        "#if8  %m = zext i32 %2 to i64",
        "#if8  %call = call i8* @malloc(i64 %m)",
        "#if4  %call = call i8* @malloc(i{0} %2)",
        "  store i8* %call, i8** %r, align {1}",
        "  %3 = load i8** %r, align {1}",
        "  %4 = load i8** %s.addr, align {1}",
        "  %5 = load i32* %start.addr, align {1}",
        "  %add.ptr = getelementptr inbounds i8* %4, i32 %5",
        "  %6 = load i32* %n, align 4",
        "#if8  %7 = zext i32 %6 to i64",
        "#if8  %call1 = call i8* @strncpy(i8* %3, i8* %add.ptr, i64 %7)",
        "#if4  %call1 = call i8* @strncpy(i8* %3, i8* %add.ptr, i{0} %6)",
        "  ret i8* %call1",
        "}}",
        "",
        "define i8* @String.Substring2(i8* %s, i32 %start) #0 {{",
        "entry:",
        "  %s.addr = alloca i8*, align {1}",
        "  %start.addr = alloca i32, align 4",
        "  store i8* %s, i8** %s.addr, align {1}",
        "  store i32 %start, i32* %start.addr, align 4",
        "  %0 = load i8** %s.addr, align {1}",
        "  %1 = load i32* %start.addr, align 4",
        "  %add.ptr = getelementptr inbounds i8* %0, i32 %1",
        "  ret i8* %add.ptr",
        "}}",
        "",
        "define i32 @String.Length(i8* %s) #0 {{",
        "entry:",
        "  %s.addr = alloca i8*, align {1}",
        "  store i8* %s, i8** %s.addr, align {1}",
        "  %0 = load i8** %s.addr, align {1}",
        "  %call = call i{0} @strlen(i8* %0)",
        "#if4  ret i32 %call",
        "#if8  %1 = trunc i64 %call to i32",
        "#if8  ret i32 %1",
        "}}",
        "",
        "define i32 @Int32.Parse(i8* %s) #0 {{",
        "entry:",
        "  %s.addr = alloca i8*, align {1}",
        "  store i8* %s, i8** %s.addr, align {1}",
        "  %0 = load i8** %s.addr, align {1}",
        "  %call = call i{0} @atoi(i8* %0)",
        "#if4  ret i32 %call",
        "#if8  %1 = trunc i64 %call to i32",
        "#if8  ret i32 %1",
        "}}",
        "define i8* @Console.ReadLine() #0 {{",
        "entry:",
        "  %buff = alloca [80 x i8], align 1",
        "  %s = alloca i8*, align {1}",
        "  %arraydecay = getelementptr inbounds [80 x i8]* %buff, i32 0, i32 0",
        "  %call = call i8* @gets(i8* %arraydecay)",
        "  %cmp = icmp ne i8* %call, null",
        "  br i1 %cmp, label %if.then, label %if.end",
        "if.then:",
        "  %call1 = call i{0} @strlen(i8* %call)",
        "  %add = add i{0} %call1, 1",
        "  %call2 = call i8* @malloc(i{0} %add)",
        "  %call3 = call i8* @strcpy(i8* %call2, i8* %call)",
        "  br label %if.end",
        "if.end:",
        "  %0 = phi i8* [%call, %entry], [%call3, %if.then]",
        "  ret i8* %0",
        "}}",
    	""
    };

    // Preamble for target triple: "i686-pc-mingw32"
    const string preamble32 = "\ntarget datalayout = " +
            "\"e-p:32:32:32-i1:8:8-i8:8:8-i16:16:16-i32:32:32-i64:64:64-f32:" +
            "32:32-f64:64:64-f80:128:128-v64:64:64-v128:128:128-a0:0:64-f80:32:32-n8:16:32-S32\"\n";

    // Preamble for target triple: "x86_64-unknown-linux-gnu"
    // This is what is generated by clang on the linux teaching server: linux.csc.uvic.ca
    const string preamble64 = "\ntarget datalayout = " +
            "\"e-p:64:64:64-i1:8:8-i8:8:8-i16:16:16-i32:32:32-i64:64:64-f32:" +
            "32:32-f64:64:64-v64:64:64-v128:128:128-a0:0:64-s0:64:64-f80:128:128-n8:16:32:64-S128\"\n";

    // Preamble for target triple: "x86_64-apple-macosx10.9.3"
    const string preambleMac64 = "target datalayout = " +
            "\"e-p:64:64:64-i1:8:8-i8:8:8-i16:16:16-i32:32:32-i64:64:64-f32:" +
            "32:32-f64:64:64-v64:64:64-v128:128:128-a0:0:64-s0:64:64-f80:128:128-n8:16:32:64-S128\"\n";

    const string epilog32 = "\nattributes #0 = { nounwind \"less-precise-fpmad\"=\"false\" " +
        "\"no-frame-pointer-elim\"=\"true\" \"no-frame-pointer-elim-non-leaf\" " +
        "\"no-infs-fp-math\"=\"false\" \"no-nans-fp-math\"=\"false\" " +
        "\"stack-protector-buffer-size\"=\"8\" \"unsafe-fp-math\"=\"false\" "+
        "\"use-soft-float\"=\"false\" }\n" +
        "attributes #1 = { \"less-precise-fpmad\"=\"false\" \"no-frame-pointer-elim\"=\"true\" " +
        "\"no-frame-pointer-elim-non-leaf\" \"no-infs-fp-math\"=\"false\" " +
        "\"no-nans-fp-math\"=\"false\" \"stack-protector-buffer-size\"=\"8\" " +
        "\"unsafe-fp-math\"=\"false\" \"use-soft-float\"=\"false\" }\n";

    const string epilog64 = "\nattributes #0 = { nounwind uwtable \"less-precise-fpmad\"=\"false\" " +
        "\"no-frame-pointer-elim\"=\"true\" \"no-frame-pointer-elim-non-leaf\" " +
        "\"no-infs-fp-math\"=\"false\" \"no-nans-fp-math\"=\"false\" " +
        "\"stack-protector-buffer-size\"=\"8\" \"unsafe-fp-math\"=\"false\" "+
        "\"use-soft-float\"=\"false\" }\n" +
        "attributes #1 = { \"less-precise-fpmad\"=\"false\" \"no-frame-pointer-elim\"=\"true\" " +
        "\"no-frame-pointer-elim-non-leaf\" \"no-infs-fp-math\"=\"false\" " +
        "\"no-nans-fp-math\"=\"false\" \"stack-protector-buffer-size\"=\"8\" " +
        "\"unsafe-fp-math\"=\"false\" \"use-soft-float\"=\"false\" }\n";

    public void WritePredefinedCode() {
        foreach(string s in predefined) {
            string ss = s;
            // a few lines have to be selectively included -- they are
            // tailored for use on the 8-byte and 4-byte platforms
            if (ss.StartsWith("#if8"))
            {
                if (ptrAlign == 4) continue;
                ss = ss.Substring(4);
            } else {
                if (ss.StartsWith("#if4"))
                {
                    if (ptrAlign == 8) continue;
                    ss = ss.Substring(4);
                }
            }
            if (macOS)
            {
                ss = ss.Replace(" #0", " ");  // causes errors with MacOS!
                ss = ss.Replace(" #1", " ");
            }
            ll.WriteLine(ss, ptrSize, ptrAlign);
        }
    }

    }
}