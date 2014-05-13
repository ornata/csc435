.PHONY: all clean test

all: lab1.exe

clean:
	rm -f lab1.exe

test: lab1.exe lab1.tst
	./lab1.exe lab1.tst

lab1.exe : lab1.cs
	gmcs lab1.cs
