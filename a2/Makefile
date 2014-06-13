.PHONY: all clean test

# mono program loader
MONO = mono

# c# compiler
CSC = mono-csc

# gppg and gplex utilities
GPPG = ../gppg-distro-1.5.0/binaries/gppg.exe
GPLEX = ../gplex-distro-1.2.1/binaries/gplex.exe

all: cbc submission.zip

clean:
	rm -f submission.zip cbc.exe CbLexer.cs CbParser.cs CbParser.conflicts

CbLexer.cs: CbLexer.lex
	$(MONO) $(GPLEX) CbLexer.lex

CbParser.cs: CbParser.y
	$(MONO) $(GPPG) /gplex /conflicts CbParser.y > CbParser.cs

cbc.exe: cbc.cs ShiftReduceParser.cs CbLexer.cs CbParser.cs CbAST.cs CbType.cs CbTopLevel.cs CbVisitor.cs CbPrVisitor.cs CbTopLevelVisitor.cs
	$(CSC) $^

submission.zip: cbc.cs CbLexer.lex CbParser.y CbAST.cs CbVisitor.cs CbPrVisitor.cs CbType.cs CbTopLevel.cs CbTopLevelVisitor.cs ShiftReduceParser.cs README.txt
	zip submission $^
