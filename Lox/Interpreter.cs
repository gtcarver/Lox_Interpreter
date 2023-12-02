// Gabriel Carver
// class to intepret syntax trees, computing values of expressions and statements

class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<object>
{
    private EnvironmentClass environment = new EnvironmentClass();

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

    public Object VisitLogicalExpr(Expr.Logical expr) 
    {
        Object left = Evaluate(expr.left);

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

    public object VisitExpressionStmt(Stmt.Expression stmt)
    {
        Evaluate(stmt.expression);
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

    public object VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.statements, new EnvironmentClass(environment));
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
        environment.Assign(expr.name, value);
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
        return environment.Get(expr.name);
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