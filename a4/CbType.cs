/*  CbType.cs

    Classes and types to describe the datatypes used in a Cb program
    
    Author: Nigel Horspool
    
    Dates: 2012-2014
    
    [26 June] Additions/Changes:
    *   enum value: CbBasicType.Null
    *   static member: CbType.Null
    *   class: CbMethodType
    *   and some recoding of CbMethod to provide a Type property
        
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace FrontEnd {


public enum CbBasicType {
    Void, Int, Bool, Char, Null, Error
}

public abstract class CbType {
    static CbBasic vt = new CbBasic(CbBasicType.Void);
    static CbBasic it = new CbBasic(CbBasicType.Int);
    static CbBasic bt = new CbBasic(CbBasicType.Bool);
    static CbBasic ct = new CbBasic(CbBasicType.Char);
    static CbClass ot = new CbClass("Object",null);
    static CbClass st = new CbClass("String",null);
    static CbBasic nt = new CbBasic(CbBasicType.Null);
    static CbBasic et = new CbBasic(CbBasicType.Error);
    static IDictionary<CbType,CFArray> arrayTypes = new Dictionary<CbType,CFArray>();

    // Properties which return unique descriptions of the basic types
    public static CbBasic Void{ get{ return vt; } }
    public static CbBasic Int{ get{ return it; } }
    public static CbBasic Bool{ get{ return bt; } }
    public static CbBasic Char{ get{ return ct; } }
    public static CbBasic Null{ get{ return nt; } }
    public static CbClass String{ get{ return st; } }
    public static CbClass Object{ get{ return ot; } }
    public static CbBasic Error{ get{ return et; } }

    // Static method which returns a unique descriptor for an array type
    public static CFArray Array( CbType elt ) {
        if (arrayTypes.ContainsKey(elt))
            return arrayTypes[elt];  // using existing descriptor
        // create a new descriptor
        return (arrayTypes[elt] = new CFArray(elt));
    }

    public abstract void Print(TextWriter p);

    // This initialization method is needed to create the type descriptors
    // for classes and methods assumed to be available to Cb programs, and
    // to enter them into the System namespace.
    // This method should be called before any typechecking is performed.
    public static void Initialize() {
        NameSpace system = (NameSpace)NameSpace.TopLevelNames.LookUp("System");
        system.AddMember(CbType.Object);
        
        // Provide String class with String.Substring and String.Length members
        CbClass st = CbType.String;
        CbMethod sub = new CbMethod("Substring", false, st, new List<CbType> { CbType.Int, CbType.Int });
        st.AddMember(sub);
        CbField len = new CbField("Length", CbType.Int);
        st.AddMember(len);
        system.AddMember(st);

        CbClass con = new CbClass("Console", null);
        con.AddMember(new CbMethod("WriteLine", true, CbType.Void, new List<CbType> { CbType.Object }));
        con.AddMember(new CbMethod("Write", true, CbType.Void, new List<CbType> { CbType.Object }));
        con.AddMember(new CbMethod("ReadLine", true, CbType.String, new List<CbType> { }));
        system.AddMember(con);
        
        CbClass i32 = new CbClass("Int32", null);
        i32.AddMember(new CbMethod("Parse", true, CbType.Int, new List<CbType> {st}));
        system.AddMember(i32);
    }
}

public class CFArray: CbType {
    public CbType ElementType{ get; set; }

    // Do not call directly -- use CbType.Array(elt) instead
    public CFArray( CbType elt ) {
        ElementType = elt;
    }

    public override string ToString() {
        return System.String.Format("{0}[]", ElementType);
    }
    
    public override void Print(TextWriter p) {
        p.Write(this.ToString());
    }
}

// A class has members which can be constants, fields, and methods.
// Since Cb does not have overloading of member names for user-defined classes,
// a dictionary can be used to look up the unique member with a particular name.
public class CbClass: CbType {
    public IDictionary<string, CbMember> Members{ get; set; }
    public string Name{ get; set; }     // name of this class
    public CbClass Parent{ get; set; }  // parent class
    public int LastIndex{ get; set; }   // NEW FOR ASS4

    // if no parent class has been specified, use null
    public CbClass( string name, CbClass parent ) {
        Name = name;
        Members = new Dictionary<string,CbMember>();
        Parent = parent==null? CbType.Object : parent;
    }

    public override string ToString() {
        return System.String.Format("class {0}", Name);
    }

    public bool AddMember( CbMember mem ) {
        if (Members.ContainsKey(mem.Name)) return false;  // FAIL -- we don't allow overloading
        mem.Owner = this;
        Members[mem.Name] = mem;
        return true;
    }

    public override void Print(TextWriter p) {
        Print(p, "");
    }

    public void Print(TextWriter p, string prefix) {
        p.Write("\nclass {0}{1} : {2}", prefix, Name, Parent==null? "null" : Parent.Name);
        p.WriteLine(" {");

        // output the constants
        foreach( CbMember cm in Members.Values ) {
            CbConst cc = cm as CbConst;
            if (cc == null) continue;
            p.Write("    ");
            cc.Print(p);
        }

        // output the fields
        foreach( CbMember cm in Members.Values ) {
            CbField cf = cm as CbField;
            if (cf == null) continue;
            p.Write("    ");
            cf.Print(p);
        }

        // output the methods
        foreach( CbMember cm in Members.Values ) {
            CbMethod ct = cm as CbMethod;
            if (ct == null) continue;
            p.Write("    ");
            ct.Print(p);
        }

        p.WriteLine("}\n");
    }
}

// A wrapper for a CbMethod value
public class CbMethodType : CbType {
    public CbMethod Method{ get; protected set; } 
    
    public CbMethodType( CbMethod method ) {
        Method = method;
    }
    
    public override string ToString() {
        return Method.ToString();
    }

    public override void Print(TextWriter p) {
        p.Write(this.ToString());
    }
}

// A wrapper for a NameSpace
public class CbNameSpaceContext : CbType {
    public NameSpace Space{ get; protected set; } 
    
    public CbNameSpaceContext( NameSpace space ) {
        Space = space;
    }
    
    public override string ToString() {
        return "namespace " + Space.Name;
    }

    public override void Print(TextWriter p) {
        p.Write(this.ToString());
    }
}

public class CbBasic: CbType {

    public CbBasicType Type{ get; protected set; }

    public CbBasic( CbBasicType t ) {
        Type = t;
    }

    public override string ToString() {
        return Type.ToString().ToLower();
    }

    public override void Print(TextWriter p) {
        p.WriteLine(this.ToString());
    }
}

// Members of a class can be constants, fields or methods
public abstract class CbMember {
    public String Name{ get; set; }
    public CbClass Owner { get; set; }  // class owning this field
    public bool IsStatic{ get; set; }   // static
    public virtual CbType Type{ get; set; }  // type of the const, field or method

    public abstract void Print(TextWriter p);
}

public class CbConst: CbMember {
    public CbConst( string nm, CbType t ) {
        Name = nm;  Type = t; IsStatic = true;
    }

    public override void Print(TextWriter p) {
        p.WriteLine("{0}:{1}", Name, Type);
    }
}

public class CbField: CbMember {
    public int Index { get; set; }  // NEW FOR ASS4

    public CbField( string nm, CbType t ) {
        Name = nm;  Type = t;
        IsStatic = false; // Cb does not have static fields
    }

    public override void Print(TextWriter p) {
        p.WriteLine("{0}:{1}", Name, Type);
    }
}

public class CbMethod: CbMember {
    public CbType ResultType{ get; set; }
    public IList<CbType> ArgType { get; set; }
    public override CbType Type{
        get { return new CbMethodType(this); }
        set { throw new Exception("internal error"); } }
    public int VTableIndex { get; set; }   // NEW FOR ASS4

    public CbMethod( string nm, bool isStatic, CbType rt, IList<CbType> argType ) {
       Name = nm;  IsStatic = isStatic;
       ResultType = rt; ArgType = argType;
    }

    public override void Print(TextWriter p) {
        p.Write("{2}{0} {1}", ResultType, Name, IsStatic? "static " : "");
        if (ArgType != null) {
            p.Write("(");
            string s = "";
            foreach( CbType at in ArgType ) {
                p.Write("{0}{1}", s, at.ToString());
                s = ",";
            }
            p.WriteLine(")");
        } else
            p.WriteLine();
    }
}

} // end of namespace FrontEnd

