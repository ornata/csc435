.PHONY: all clean test

# mono program loader
MONO = mono

# c# compiler
CSC = mono-csc

# gppg and gplex utilities
GPPG = ../gppg-distro-1.5.0/binaries/gppg.exe
GPLEX = ../gplex-distro-1.2.1/binaries/gplex.exe

all: cbc.exe submission.zip

clean:
	rm -f CbLexer.cs CbParser.cs cbc.exe submission.zip tokens.txt CbLexer.lst CbParser.lst

CbLexer.cs: CbLexer.lex
	$(MONO) $(GPLEX) CbLexer.lex

CbParser.cs: CbParser.y
	$(MONO) $(GPPG) /gplex /conflicts CbParser.y > CbParser.cs

cbc.exe: cbc.cs CbParser.cs CbLexer.cs ShiftReduceParser.cs
	$(CSC) cbc.cs CbParser.cs CbLexer.cs ShiftReduceParser.cs

submission.zip: cbc.cs CbParser.cs CbLexer.cs TestProgram.cs
	zip submission cbc.cs CbParser.cs CbLexer.cs TestProgram.cs

tokens.txt: cbc.exe TestProgram.cs
	$(MONO) ./cbc.exe -tokens -debug TestProgram.cs

# run once with tokens output and once without
test: cbc.exe TestProgram.cs
	$(MONO) ./cbc.exe -tokens -debug TestProgram.cs
	cat tokens.txt
	$(MONO) ./cbc.exe -debug TestProgram.cs
