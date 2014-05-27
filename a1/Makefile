.PHONY: all clean test

# mono program loader
MONO = mono

# c# compiler
CSC = mono

# gppg and gplex utilities
GPPG = ../gppg-distro-1.5.0/binaries/gppg.exe
GPLEX = ../gplex-distro-1.2.1/binaries/gplex.exe

all: submission.zip

clean:
	rm -f CbLexer.cs CbParser.cs cbc.exe submission.zip tokens.txt

CbLexer.cs: CbLexer.lex
	$(MONO) $(GPLEX) CbLexer.lex

CbParser.cs: CbParser.y
	$(MONO) $(GPPG) /gplex /conflicts CbParser.y > CbParser.cs

cbc.exe: cbc.cs CbParser.cs CbLexer.cs
	$(MONO) $(CSC) cbc.cs

submission.zip: cbc.cs CbParser.cs CbLexer.cs TestProgram.cs
	zip submission cbc.cs CbParser.cs CbLexer.cs TestProgram.cs

test: cbc.exe TestProgram.cs
	./cbc.exe -tokens -debug TestProgram.cs
