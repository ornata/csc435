.PHONY: all clean test

# mono program loader
MONO = mono

# c# compiler
CSC = mono-csc

# gppg and gplex utilities
GPPG = ../gppg-distro-1.5.0/binaries/gppg.exe
GPLEX = ../gplex-distro-1.2.1/binaries/gplex.exe

# sources without generated files
SOURCES_NOGEN = cbc.cs CbSymTab.cs CbTLVisitor.cs CbType.cs CbTopLevel.cs \
CbVisitor.cs CbPrVisitor.cs CbAST.cs ShiftReduceParser.cs CbTypeCheckVisitor.cs

# generated source files
SOURCES_GEN = CbLexer.cs CbParser.cs

# All sources
SOURCES = $(SOURCES_NOGEN) $(SOURCES_GEN)

all: cbc.exe submission.zip

clean:
	rm -f submission.zip cbc.exe $(SOURCES_GEN) CbParser.conflicts tokens.txt *.mdb

CbLexer.cs: CbLexer.lex
	$(MONO) $(GPLEX) $^

CbParser.cs: CbParser.y
	$(MONO) $(GPPG) /gplex /conflicts $^ > $@

cbc.exe:  $(SOURCES)
	$(CSC) -debug $^

submission.zip: $(SOURCES_NOGEN) CbLexer.lex CbParser.y README.txt
	zip submission $^

test: cbc.exe CbExample.cs
	$(MONO) --debug cbc.exe -ns CbExample.cs
