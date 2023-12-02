// Gabriel Carver
// Class for printing an abstract syntax tree of a given expression

using System;

class AstPrinter : Expr.IVisitor<string>
{
    public string Print(Expr expr)
    {
        return expr.Accept(this);
    }

    public string VisitBinaryExpr(Expr.Binary expr)
    {
        return Parenthesize(expr.operatorToken.Lexeme, expr.left, expr.right);
    }
    // get rid of
    public string VisitVariableExpr(Expr.Variable var)
    {
        return "";
    }
    // get rid of
    public string VisitAssignExpr(Expr.Assign var)
    {
        return "";
    }

    // get rid of
    public string VisitLogicalExpr(Expr.Logical var)
    {
        return "";
    }


    public string VisitGroupingExpr(Expr.Grouping expr)
    {
        return Parenthesize("group", expr.expression);
    }

    public string VisitLiteralExpr(Expr.Literal expr)
    {
        if (expr.value == null) return "nil";
        return expr.value.ToString() ?? "nil";
    }

    public string VisitUnaryExpr(Expr.Unary expr)
    {
        return Parenthesize(expr.operatorToken.Lexeme, expr.right);
    }

    private string Parenthesize(string name, params Expr[] exprs)
    {
        var builder = new System.Text.StringBuilder();

        builder.Append("(").Append(name);
        foreach (var expr in exprs)
        {
            builder.Append(" ");
            builder.Append(expr.Accept(this));
        }
        builder.Append(")");

        return builder.ToString();
    }

    //  static void Main(string[] args)
    // {
    //     Expr expression = new Expr.Binary(
    //         new Expr.Unary(
    //             new Token(TokenType.MINUS, "-", null, 1),
    //             new Expr.Literal(123)),
    //         new Token(TokenType.STAR, "*", null, 1),
    //         new Expr.Grouping(
    //             new Expr.Literal(45.67)));

    //     Console.WriteLine(new AstPrinter().Print(expression));
    // }
}