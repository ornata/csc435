/*  CbTopLevel.cs

    Manages tables of names visible at top-level of a Cb program
    down to the level of individual classes.
    
    Author: Nigel Horspool
    
    Dates: 2012-2014
*/

using System;
using System.IO;
using System.Collections.Generic;

namespace FrontEnd {

    public class NameSpace {
        // name of this namespace
        public string Name { get; protected set; }
        
        // maps a name to either a NameSpace or a CbClass instance
        private IDictionary<string,object> nametable;
        
        public NameSpace( string name ) {
            Name = name;
            nametable = new Dictionary<string,object>();
        }
        
        public ICollection<string> Names {
            get{ return nametable.Keys; }
        }
        
        public ICollection<object> Members {
            get{ return nametable.Values; }
        }

        // lookup a name in the nametable
        // The result is a reference to a NameSpace or CbClass instance
        // if found, otherwise the result is null
        public object LookUp( string name ) {
            object result = null;
            nametable.TryGetValue(name, out result);
            return result;
        }
        
        protected bool addname( string name, object r ) {
            if (nametable.ContainsKey(name))
                return false;
            nametable[name] = r;
            return true;
        }

        // add a class to this namespace
        public bool AddMember( CbClass ct ) {
            return addname(ct.Name, ct);
        }
        
        // add a nested namespace to this namespace
        public bool AddMember( NameSpace ns ) {
            return addname(ns.Name, ns);
        }
        
        // dump a nested namespace
        public void Print( TextWriter outputStream, string prefix ) {
            string fullname = prefix + Name;
            string newPrefix = "";
            if (Name.Length == 0) {
                Console.WriteLine("\nNamespace <anonymous>:");
            } else {
                Console.WriteLine("\nNamespace {0}:", fullname);
                newPrefix = fullname + ".";
            }
            foreach( object m in nametable.Values ) {
                if (m is NameSpace)
                    ((NameSpace)m).Print(outputStream, newPrefix);
                else if (m is CbClass)
                    ((CbClass)m).Print(outputStream, newPrefix);
            }
        }

        // dump all namespaces from top-level down
        public static void Print( TextWriter outputStream ) {
            TopLevelNames.Print(outputStream, "");
        }

        // dump all namespaces from top-level down
        public static void Print() {
            TopLevelNames.Print(Console.Out, "");
        }

    // ************* static stuff *****************

        // Will contain a list of all classes & namespaces visible at top-level
        public static NameSpace TopLevelNames = new NameSpace("");

        // This is a static constructor, it initializes static fields
        static NameSpace() {
            // normally a C# compiler would obtain the names of top-level
            // namespaces from resource files, and then would obtain details
            // second level namespaces and classes if the C# program contains
            // the appropriate using declaration.
            NameSpace system = new NameSpace("System");
            TopLevelNames.AddMember(system);
        }

    }


}  // end of FrontEnd