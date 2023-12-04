// Gabriel Carver
// Class for variable resolution

using System;
using System.Collections.Generic;

class Resolver : Expr.IVisitor<object>, Stmt.IVisitor<object>
{
    private readonly Interpreter interpreter;
    private readonly List<Dictionary<string, bool>> scopes = new List<Dictionary<string, bool>>();
    private FunctionType currentFunction = FunctionType.NONE;

    public Resolver(Interpreter interpreter)
    {
        this.interpreter = interpreter;
    }

    private enum FunctionType 
    {
        NONE,
        FUNCTION,
        INITIALIZER,
        METHOD
    }

    private enum ClassType 
    {
        NONE,
        CLASS,
        SUBCLASS
    }

  private ClassType currentClass = ClassType.NONE;

    public object VisitBlockStmt(Stmt.Block stmt) 
    {
        BeginScope();
        Resolve(stmt.statements);
        EndScope();
        return null;
    }

    public object VisitClassStmt(Stmt.Class stmt) 
    {
        ClassType enclosingClass = currentClass;
        currentClass = ClassType.CLASS;

        Declare(stmt.name);
        Define(stmt.name);

        if (stmt.superclass != null && stmt.name.Lexeme.Equals(stmt.superclass.name.Lexeme)) 
        {
            Lox.Error(stmt.superclass.name, "A class can't inherit from itself.");
        }

        if (stmt.superclass != null) 
        {
            currentClass = ClassType.SUBCLASS;
            Resolve(stmt.superclass);
        }

        if (stmt.superclass != null) 
        {
            BeginScope();
            scopes[scopes.Count - 1]["super"] = true;
        }

        BeginScope();
        scopes[scopes.Count - 1]["this"] = true;

        foreach (Stmt.Function method in stmt.methods) 
        {
            FunctionType declaration = FunctionType.METHOD;
            if (method.name.Lexeme.Equals("init")) 
            {
                declaration = FunctionType.INITIALIZER;
            }

            ResolveFunction(method, declaration); 
        }

        EndScope();

        if (stmt.superclass != null) EndScope();

        currentClass = enclosingClass;
        return null;
    }

    public object VisitExpressionStmt(Stmt.Expression stmt) 
    {
        Resolve(stmt.expression);
        return null;
    }

    // resolving function declarations
    public object VisitFunctionStmt(Stmt.Function stmt) 
    {
        Declare(stmt.name);
        Define(stmt.name);

        ResolveFunction(stmt, FunctionType.FUNCTION);

        return null;
    }

    public object VisitIfStmt(Stmt.If stmt) 
    {
        Resolve(stmt.condition);
        Resolve(stmt.thenBranch);
        if (stmt.elseBranch != null) Resolve(stmt.elseBranch);
        return null;
    }

    public object VisitPrintStmt(Stmt.Print stmt) 
    {
        Resolve(stmt.expression);
        return null;
    }

    public object VisitReturnStmt(Stmt.Return stmt) 
    {
        if (currentFunction == FunctionType.NONE) 
        {
            Lox.Error(stmt.keyword, "Can't return from top-level code.");
        }

        if (stmt.value != null)
        {
            if (currentFunction == FunctionType.INITIALIZER) 
            {
                Lox.Error(stmt.keyword, "Can't return a value from an initializer.");
            }

            Resolve(stmt.value);
        }

        return null;
    }

    // resolving variable declarations
    public object VisitVarStmt(Stmt.Var stmt) 
    {
        Declare(stmt.name);
        if (stmt.initializer != null) Resolve(stmt.initializer);
        
        Define(stmt.name);
        return null;
   }

    public object VisitWhileStmt(Stmt.While stmt) 
    {
        Resolve(stmt.condition);
        Resolve(stmt.body);
        return null;
    }

    // resolving variable expressions
    public object VisitVariableExpr(Expr.Variable expr) 
    {
        if (!(scopes.Count == 0) && scopes[scopes.Count - 1].TryGetValue(expr.name.Lexeme, out bool val) == false)
        {
            Lox.Error(expr.name, "Can't read local variable in its own initializer.");
        }

        ResolveLocal(expr, expr.name);
        return null;
    }

    // resolving assignment expressions
    public object VisitAssignExpr(Expr.Assign expr) 
    {
        Resolve(expr.value);
        ResolveLocal(expr, expr.name);
        return null;
    }

    public object VisitBinaryExpr(Expr.Binary expr) 
    {
        Resolve(expr.left);
        Resolve(expr.right);
        return null;
    }

    public object VisitCallExpr(Expr.Call expr) 
    {
        Resolve(expr.callee);

        foreach (Expr argument in expr.arguments) 
        {
            Resolve(argument);
        }

        return null;
    }

    public object VisitGetExpr(Expr.Get expr) 
    {
        Resolve(expr.exprObject);
        return null;
    }

    public object VisitGroupingExpr(Expr.Grouping expr) 
    {
        Resolve(expr.expression);
        return null;
    }

    public object VisitLiteralExpr(Expr.Literal expr) 
    {
        return null;
    }

    public object VisitLogicalExpr(Expr.Logical expr) 
    {
        Resolve(expr.left);
        Resolve(expr.right);
        return null;
    }

    public object VisitSetExpr(Expr.Set expr) 
    {
        Resolve(expr.value);
        Resolve(expr.exprObject);
        return null;
    }

    public object VisitSuperExpr(Expr.Super expr) 
    {
        if (currentClass == ClassType.NONE) 
        {
            Lox.Error(expr.keyword, "Can't use 'super' outside of a class.");
        } 
        else if (currentClass != ClassType.SUBCLASS) 
        {
            Lox.Error(expr.keyword, "Can't use 'super' in a class with no superclass.");
        }

        ResolveLocal(expr, expr.keyword);
        return null;
    }

    public object VisitThisExpr(Expr.This expr) 
    {
        if (currentClass == ClassType.NONE) 
        {
            Lox.Error(expr.keyword, "Can't use 'this' outside of a class.");
            return null;
        }

        ResolveLocal(expr, expr.keyword);
        return null;
    }


    public object VisitUnaryExpr(Expr.Unary expr) 
    {
        Resolve(expr.right);
        return null;
    }

    // resolves a list of statements
    public void Resolve(List<Stmt> statements) 
    {
        foreach (Stmt statement in statements) 
        {
            Resolve(statement);
        }
    }

    // resolves individual statement
    private void Resolve(Stmt stmt) 
    {
        stmt.Accept(this);
    }

    // resolves individual expression
    private void Resolve(Expr expr) 
    {
        expr.Accept(this);
    }

    // resolves the body of a given function
    private void ResolveFunction(Stmt.Function function, FunctionType type) 
    {
        FunctionType enclosingFunction = currentFunction;
        currentFunction = type;

        BeginScope();
        foreach (Token param in function.funParams) 
        {
            Declare(param);
            Define(param);
        }

        Resolve(function.body);
        EndScope();
        currentFunction = enclosingFunction;
    }

    // new block scope created
    private void BeginScope()
    {
        scopes.Add(new Dictionary<string, bool>());
    }

    // exiting block scope
    private void EndScope()
    {
        scopes.RemoveAt(scopes.Count - 1);
    }

    // declaring a variable
    private void Declare(Token name)
    {
        if (scopes.Count == 0) return;

        Dictionary<string, bool> scope = scopes[scopes.Count - 1];
        
        if (scope.ContainsKey(name.Lexeme))
        {
            Lox.Error(name, "Already a variable with this name in this scope.");
        }
        
        scope[name.Lexeme] = false; 
    }

    // defining declared variable
    private void Define(Token name) 
    {
        if (scopes.Count == 0) return;
        scopes[scopes.Count - 1][name.Lexeme] = true; // variable fully initialized, ready for use
    }

    // resolving local variable
    private void ResolveLocal(Expr expr, Token name)
    {
        for (int i = scopes.Count - 1; i >= 0; i--)
        {
            Dictionary<string, bool> scope = scopes[i];

            if (scope.ContainsKey(name.Lexeme))
            {
                interpreter.Resolve(expr, scopes.Count - i - 1);
                return;
            }
        }  
    } 

}
