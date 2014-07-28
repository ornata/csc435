.PHONY: all clean test

# mono program loader
MONO = mono

# c# compiler
CSC = mono-csc

# gppg and gplex utilities
GPPG = ../gppg-distro-1.5.0/binaries/gppg.exe
GPLEX = ../gplex-distro-1.2.1/binaries/gplex.exe

# sources without generated files
SOURCES_NOGEN = \
	CbAST.cs cbc.cs CbPrVisitor.cs CbSymTab.cs CbTLVisitor.cs \
	CbTopLevel.cs CbType.cs CbTypeCheckVisitor1.cs \
	CbTypeCheckVisitor2.cs CbVisitor.cs LLVM.cs LLVM-Arrays.cs \
	LLVM-ConstantHandling.cs LLVM-CreateClassDefn.cs \
	LLVM-Definitions.cs LLVM-SSAMethods.cs \
	LLVM-UtilityMethods.cs LLVMVisitor1.cs LLVMVisitor2.cs \
	LLVM-WriteMethods.cs ShiftReduceParser.cs

# generated source files
SOURCES_GEN = CbLexer.cs CbParser.cs

# All sources
SOURCES = $(SOURCES_NOGEN) $(SOURCES_GEN)

all: cbc.exe submission.zip

clean:
	rm -f submission.zip Fibs *.exe $(SOURCES_GEN) CbParser.conflicts *.mdb

CbLexer.cs: CbLexer.lex
	$(MONO) $(GPLEX) $^

CbParser.cs: CbParser.y
	$(MONO) $(GPPG) /gplex /conflicts $^ > $@

cbc.exe:  $(SOURCES)
	$(CSC) -debug $^ /out:cbc.exe

submission.zip: $(SOURCES_NOGEN) CbLexer.lex CbParser.y README.txt
	zip submission $^

Fibs.ll: cbc.exe Fibs.cs
	$(MONO) --debug cbc.exe Fibs.cs
	
Fibs.s: Fibs.ll
	llc Fibs.ll
    
Fibs: Fibs.s
	gcc Fibs.s -o Fibs
    
test: Fibs
	./Fibs
