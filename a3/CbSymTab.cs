/* CbSymTab.cs

   Implements a symbol table, useful for processing the local declarations
   in the body of a method.
   
   This does NOT continue searches amongst the fields of the class containing
   the method if noi local declaration for a name is found. That must be
   handled by the caller.

    Author: Nigel Horspool
    
    Dates: 2012-2014
*/


/*
NOTE:
The implementation provided here will work ... but it becomes exceedingly
inefficient when the number of symbols grows -- linear search is used.
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace FrontEnd {

public class SymTabEntry {
    public int DeclLineNo { get; private set; }  // declared on this line
    public CbType Type{ get; set; }              // declared type

    public string Name{ get; private set; }

    public SymTabEntry( string nm, int ln ) {
        Name = nm;  DeclLineNo = ln;
    }
}


public class SymTab {
    private IList<SymTabEntry> table;  // a simple list data stucture
    
    public int ScopeLevel { get; private set; }

    public SymTab() {
        table = new List<SymTabEntry>();
        Empty();
    }

    public void Empty() {
        // resets the symbol table to be empty
        table.Clear();
        table.Add(null);    // add a scope marker for top scope
        ScopeLevel = 0;
    }

    public SymTabEntry Binding( string name, int ln ) {
        // check for duplicate definition which hides a prior definition in this method
        int last = table.Count;
        while(last > 0) {
            last--;
            SymTabEntry syt = table[last];
            if (syt == null) continue;  // ignore the scope marker
            if (syt.Name == name) {
                Start.SemanticError(ln, "declaration of {0} hides an earlier declaration", name);
                break;
            }
        }
        // add a new entry to the symbol table
        SymTabEntry result = new SymTabEntry(name,ln);
        table.Add(result);
        return result;
    }

    public SymTabEntry LookUp( string name ) {
        // Search symbol table for this name -- need the latest occurrence
        int last = table.Count;
        while(last > 0) {
            last--;
            SymTabEntry syt = table[last];
            if (syt != null && syt.Name == name) return syt;
        }
        return null;
    }

    // Start a new scope
    public void Enter() {
        table.Add(null);  // we use null as the scope marker
        ScopeLevel++;
    }

    // Exit the most recent scope
    // Also make sure that the number of Exits does not exceed
    // the number of Enters.
    public void Exit() {
        ScopeLevel--;
        Debug.Assert(ScopeLevel >= 0);
        int last = table.Count;
        while(last > 0) {
            last--;
            SymTabEntry syt = table[last];
            table.RemoveAt(last);
            if (syt == null) break; // hit the scope marker
        }
    }
}

} // end of namespace FrontEnd 
