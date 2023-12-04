// Gabriel Carver
// class to intepret syntax trees, computing values of expressions and statements

using System;
using System.Collections.Generic;

class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<object>
{
    public static readonly EnvironmentClass globals = new EnvironmentClass();

    private EnvironmentClass environment = globals;
    private readonly Dictionary<Expr, int> locals = new Dictionary<Expr, int>();

    public Interpreter()
    {
        globals.Define("clock", new Clock());
    }

    public void Interpret(List<Stmt> statements)
    {
        try
        {
            foreach (Stmt statement in statements)
            {
                Execute(statement);
            }
        }
        catch (RuntimeError error)
        {
            Lox.RuntimeError(error);
        }
    }

    public object VisitLiteralExpr(Expr.Literal expr)
    {
        return expr.value; 
    }

    public object VisitLogicalExpr(Expr.Logical expr) 
    {
        object left = Evaluate(expr.left);

        if (expr.operatorToken.Type == TokenType.OR) 
        {
            if (IsTruthy(left)) return left;
        } 
        else 
        {
            if (!IsTruthy(left)) return left;
        }

        return Evaluate(expr.right);
    }

    public object VisitSetExpr(Expr.Set expr) 
    {
        object obj = Evaluate(expr.exprObject);

        if (!(obj is LoxInstance)) 
        { 
            throw new RuntimeError(expr.name, "Only instances have fields.");
        }

        object value = Evaluate(expr.value);
        ((LoxInstance)obj).Set(expr.name, value);
        return value;
    }

    public object VisitSuperExpr(Expr.Super expr) 
    {
        int distance = locals[expr];
        LoxClass superclass = (LoxClass)environment.GetAt(distance, "super");

        LoxInstance obj = (LoxInstance)environment.GetAt(distance - 1, "this");

        LoxFunction method = superclass.FindMethod(expr.method.Lexeme);

        if (method == null) 
        {
            throw new RuntimeError(expr.method, "Undefined property '" + expr.method.Lexeme + "'.");
        }

        return method.Bind(obj);
    }

    public object VisitThisExpr(Expr.This expr) 
    {
        return LookUpVariable(expr.keyword, expr);
    }

    public object VisitGroupingExpr(Expr.Grouping expr)
    {
        return Evaluate(expr.expression);
    }

    private object Evaluate(Expr expr)
    {
        return expr.Accept(this);
    }

    private void Execute(Stmt stmt)
    {
        stmt.Accept(this);
    }

    public void Resolve(Expr expr, int depth)
    {
        locals[expr] = depth;
    }

    public object VisitExpressionStmt(Stmt.Expression stmt)
    {
        Evaluate(stmt.expression);
        return null;
    }

    public object VisitFunctionStmt(Stmt.Function stmt) 
    {
        LoxFunction function = new LoxFunction(stmt, environment, false);
        environment.Define(stmt.name.Lexeme, function);
        return null;
    }

    public object VisitIfStmt(Stmt.If stmt)
    {
        if (IsTruthy(Evaluate(stmt.condition)))
        {
            Execute(stmt.thenBranch);
        }
        else if (stmt.elseBranch != null)
        {
            Execute(stmt.elseBranch);
        }
        return null;
    }

    public object VisitPrintStmt(Stmt.Print stmt)
    {
        Object value = Evaluate(stmt.expression);
        Console.WriteLine(Stringify(value));
        return null;
    }

    public object VisitReturnStmt(Stmt.Return stmt) 
    {
        object value = null;
        if (stmt.value != null) value = Evaluate(stmt.value);

        throw new Return(value);
    }

    public object VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.statements, new EnvironmentClass(environment));
        return null;
    }

    public object VisitClassStmt(Stmt.Class stmt) 
    {
        object superclass = null;
        if (stmt.superclass != null) 
        {
            superclass = Evaluate(stmt.superclass);
            if (!(superclass is LoxClass)) 
            {
                throw new RuntimeError(stmt.superclass.name, "Superclass must be a class.");
            }
        }

        environment.Define(stmt.name.Lexeme, null);

        if (stmt.superclass != null) 
        {
            environment = new EnvironmentClass(environment);
            environment.Define("super", superclass);
        }

        Dictionary<string, LoxFunction> methods = new Dictionary<string, LoxFunction>();
        foreach (Stmt.Function method in stmt.methods)
        {
            LoxFunction function = new LoxFunction(method, environment, method.name.Lexeme.Equals("init"));
            methods[method.name.Lexeme] = function;
        }

        LoxClass klass = new LoxClass(stmt.name.Lexeme, (LoxClass)superclass, methods);

        if (superclass != null) environment = environment.enclosing;

        environment.Assign(stmt.name, klass);
        return null;
    }

    public void ExecuteBlock(List<Stmt> statements, EnvironmentClass environment) 
    {
        EnvironmentClass previous = this.environment;
        try 
        {
            this.environment = environment;

            foreach (Stmt statement in statements) 
            {
                Execute(statement);
            }
        } 

        finally 
        {
            this.environment = previous;
        }
    }

    public object VisitVarStmt(Stmt.Var stmt)
    {
        object value = null;
        if (stmt.initializer != null)
        {
            value = Evaluate(stmt.initializer);
        }

        environment.Define(stmt.name.Lexeme, value);
        return null;
    }

    public object VisitWhileStmt(Stmt.While stmt) 
    {
        while (IsTruthy(Evaluate(stmt.condition))) 
        {
            Execute(stmt.body);
        }

        return null;
    }

    public object VisitAssignExpr(Expr.Assign expr)
    {
        object value = Evaluate(expr.value);
        
        if (locals.TryGetValue(expr, out int distance))
        {
            environment.AssignAt(distance, expr.name, value);
        }
        else
        {
            globals.Assign(expr.name, value);
        }

        return value;
    }

    public object VisitBinaryExpr(Expr.Binary expr)
    {
        object left = Evaluate(expr.left);
        object right = Evaluate(expr.right);

        switch (expr.operatorToken.Type)
        {
            case TokenType.GREATER:
                CheckNumberOperands(expr.operatorToken, left, right);
                return (double)left > (double)right;
            case TokenType.GREATER_EQUAL:
                CheckNumberOperands(expr.operatorToken, left, right);
                return (double)left >= (double)right;
            case TokenType.LESS:
                CheckNumberOperands(expr.operatorToken, left, right);
                return (double)left < (double)right;
            case TokenType.LESS_EQUAL:
                CheckNumberOperands(expr.operatorToken, left, right);
                return (double)left <= (double)right;
            case TokenType.MINUS:
                CheckNumberOperands(expr.operatorToken, left, right);
                return (double)left - (double)right;
            case TokenType.PLUS:
                if (left is double && right is double)
                {
                    return (double)left + (double)right;
                }

                if (left is string && right is string)
                {
                    return (string)left + (string)right;
                }
                throw new RuntimeError(expr.operatorToken, "Operands must be two numbers or two strings.");
            case TokenType.SLASH:
                CheckNumberOperands(expr.operatorToken, left, right);
                return (double)left / (double)right;
            case TokenType.STAR:
                CheckNumberOperands(expr.operatorToken, left, right);
                return (double)left * (double)right;
            case TokenType.BANG_EQUAL: 
                return !IsEqual(left, right);
            case TokenType.EQUAL_EQUAL: 
                return IsEqual(left, right);
        }

        // Unreachable.
        return null;
    }

    public object VisitCallExpr(Expr.Call expr)
    {
        object callee = Evaluate(expr.callee);

        List<Object> arguments = new List<object>();

        foreach (Expr argument in expr.arguments) 
        { 
            arguments.Add(Evaluate(argument));
        }

        if (callee is not ILoxCallable) 
        {
            throw new RuntimeError(expr.paren, "Can only call functions and classes.");
        }

        ILoxCallable function = (ILoxCallable)callee;
        if (arguments.Count != function.Arity()) 
        {
            throw new RuntimeError(expr.paren, "Expected " +
            function.Arity() + " arguments but got " +
            arguments.Count + ".");
        }

        return function.Call(this, arguments);
    }

    public object VisitGetExpr(Expr.Get expr) 
    {
        object obj = Evaluate(expr.exprObject);
        if (obj is LoxInstance) return ((LoxInstance) obj).Get(expr.name);

        throw new RuntimeError(expr.name, "Only instances have properties.");
    }

    public object VisitUnaryExpr(Expr.Unary expr)
    {
        object right = Evaluate(expr.right);

        switch (expr.operatorToken.Type)
        {
            case TokenType.MINUS:
                CheckNumberOperand(expr.operatorToken, right);
                return -(double)right;
            case TokenType.BANG:
                return !IsTruthy(right);
        }

        // Unreachable.
        return null;
    }

    public object VisitVariableExpr(Expr.Variable expr)
    {
        return LookUpVariable(expr.name, expr);
    }

    private object LookUpVariable(Token name, Expr expr)
    {
        int distance;
        if (locals.ContainsKey(expr))
        {
            distance = locals[expr];
            return environment.GetAt(distance, name.Lexeme);
        }
        else
        {
            return globals.Get(name);
        }
    }

    private void CheckNumberOperand(Token operatorToken, object operand)
    {
        if (operand is double)
            return;

        throw new RuntimeError(operatorToken, "Operand must be a number.");
    }

    private void CheckNumberOperands(Token operatorToken, object left, object right)
    {
        if (left is double && right is double) return;

        throw new RuntimeError(operatorToken, "Operands must be numbers.");
    }

    private static bool IsTruthy(object value)
    {
        if (value == null)
        {
            return false;
        }
        else if (value is bool boolValue)
        {
            return boolValue;
        }
        else
        {
            return true;
        }
    }

    private static bool IsEqual(object a, object b)
    {
        if (a == null && b == null)
            return true;

        if (a == null)
            return false;

        return a.Equals(b);
    }

    private string Stringify(object obj)
    {
        if (obj == null) return "nil";

        if (obj is double)
        {
            string text = obj.ToString();
            if (text.EndsWith(".0"))
            {
                text = text.Substring(0, text.Length - 2);
            }
            return text;
        }

        return obj.ToString();
    }
}