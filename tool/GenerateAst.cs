// Gabriel Carver
// file for generating the abstract syntax tree

using System;
using System.IO;
using System.Collections.Generic;

class GenerateAst
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: generate_ast <output directory>");
            Environment.Exit(64);
        }

        string outputDir = args[0];

        List<string> exprTypes = new List<string>
        {
            "Assign   : Token name, Expr value",
            "Binary   : Expr left, Token operatorToken, Expr right",
            "Call     : Expr callee, Token paren, List<Expr> arguments",
            "Get      : Expr exprObject, Token name",
            "Grouping : Expr expression",
            "Literal  : object value",
            "Logical  : Expr left, Token operatorToken, Expr right",
            "Set      : Expr exprObject, Token name, Expr value",
            "Super    : Token keyword, Token method",
            "This     : Token keyword",
            "Unary    : Token operatorToken, Expr right",
            "Variable : Token name"
        };

        List<string> stmtTypes = new List<string>
        {
            "Block      : List<Stmt> statements",
             "Class      : Token name, Expr.Variable superclass," + " List<Stmt.Function> methods",
            "Expression : Expr expression",
            "Function   : Token name, List<Token> funParams," + " List<Stmt> body",
            "If         : Expr condition, Stmt thenBranch," + " Stmt elseBranch",
            "Print      : Expr expression",
            "Return     : Token keyword, Expr value",
            "Var        : Token name, Expr initializer",
            "While      : Expr condition, Stmt body"
        };

        DefineAst(outputDir, "Expr", exprTypes);
        DefineAst(outputDir, "Stmt", stmtTypes);
    }

    static void DefineAst(string outputDir, string baseName, List<string> types)
    {
        string path = Path.Combine(outputDir, baseName + ".cs");

        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("// Gabriel Carver"); // gotta have my comments
            writer.WriteLine("// File created from GenerateAst.cs");
            writer.WriteLine();
            writer.WriteLine("using System;");
            writer.WriteLine("using System.Collections.Generic;");
            writer.WriteLine();
            writer.WriteLine($"abstract class {baseName} ");
            writer.WriteLine("{");

            DefineVisitor(writer, baseName, types);

            foreach (string type in types)
            {
                string className = type.Split(":")[0].Trim();
                string fields = type.Split(":")[1].Trim();
                DefineType(writer, baseName, className, fields);
            }

            // The base accept() method.
            writer.WriteLine("    public abstract R Accept<R>(IVisitor<R> visitor);");
            writer.WriteLine("}");
        }
    }

    private static void DefineVisitor(StreamWriter writer, string baseName, List<string> types)
    {
        writer.WriteLine("    public interface IVisitor<R>");
        writer.WriteLine("    {");

        foreach (string type in types)
        {
            string typeName = type.Split(':')[0].Trim();
            writer.WriteLine($"        R Visit{typeName}{baseName}({typeName} {baseName.ToLower()});");
        }

        writer.WriteLine("    }");
        writer.WriteLine();
    }

    static void DefineType(StreamWriter writer, string baseName, string className, string fieldList)
    {
        writer.WriteLine($"    public class {className} : {baseName}");
        writer.WriteLine("    {");

        // Constructor.
        writer.WriteLine($"        public {className}({fieldList})");
        writer.WriteLine("        {");

        // Store parameters in fields.
        string[] fields = fieldList.Split(", ");
        foreach (string field in fields)
        {
            string name = field.Split(' ')[1];
            writer.WriteLine($"            this.{name} = {name};");
        }

        writer.WriteLine("        }");

        // Visitor pattern.
        writer.WriteLine();
        writer.WriteLine("        public override R Accept<R>(IVisitor<R> visitor)");
        writer.WriteLine("        {");
        writer.WriteLine($"          return visitor.Visit{className}{baseName}(this);");
        writer.WriteLine("        }");

        // Fields.
        writer.WriteLine();
        foreach (string field in fields)
        {
            writer.WriteLine($"        public readonly {field};");
        }

        writer.WriteLine("    }");
        writer.WriteLine();
    }
}
