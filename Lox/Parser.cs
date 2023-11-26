// Gabriel Carver
// Parser class

using System;
using System.Collections.Generic;


class Parser
{
    public class ParseError : Exception {}

    private readonly List<Token> tokens;
    private int current = 0; // token index

    public Parser(List<Token> tokens)
    {
        this.tokens = tokens;
    }

    /*
        This is the grammar we're working with (as of ch. 6):
        expression     → equality ;
        equality       → comparison ( ( "!=" | "==" ) comparison )* ;
        comparison     → term ( ( ">" | ">=" | "<" | "<=" ) term )* ;
        term           → factor ( ( "-" | "+" ) factor )* ;
        factor         → unary ( ( "/" | "*" ) unary )* ;
        unary          → ( "!" | "-" ) unary | primary ;
        primary        → NUMBER | STRING | "true" | "false" | "nil" | "(" expression ")" ;
    
        Will be expanded later.
    */

    // first production rule
    private Expr Expression()
    {
        return Equality(); // simple for now, will be expanded in later chapters
    }

    // equality production rule
    private Expr Equality()
    {
        Expr expr = Comparison();

        while (Match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL))
        {
            Token operatorToken = Previous();
            Expr right = Comparison();
            expr = new Expr.Binary(expr, operatorToken, right);
        }

        return expr;
    }

    // comparison production rule
    private Expr Comparison()
    {
        Expr expr = Term();

        while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
        {
            Token op = Previous();
            Expr right = Term();
            expr = new Expr.Binary(expr, op, right);
        }

        return expr;
    }

    // term production rule
    private Expr Term()
    {
        Expr expr = Factor();

        while (Match(TokenType.MINUS, TokenType.PLUS))
        {
            Token op = Previous();
            Expr right = Factor();
            expr = new Expr.Binary(expr, op, right);
        }

        return expr;
    }

    // factor production rule
    private Expr Factor()
    {
        Expr expr = Unary();

        while (Match(TokenType.SLASH, TokenType.STAR))
        {
            Token op = Previous();
            Expr right = Unary();
            expr = new Expr.Binary(expr, op, right);
        }

        return expr;
    }

    // unary production rule
    private Expr Unary()
    {
        if (Match(TokenType.BANG, TokenType.MINUS))
        {
            Token op = Previous();
            Expr right = Unary();
            return new Expr.Unary(op, right);
        }

        return Primary();
    }

    // primary production rule
    private Expr Primary()
    {
        if (Match(TokenType.FALSE)) return new Expr.Literal(false);
        if (Match(TokenType.TRUE)) return new Expr.Literal(true);
        if (Match(TokenType.NIL)) return new Expr.Literal(null);

        if (Match(TokenType.NUMBER, TokenType.STRING))
        {
            return new Expr.Literal(Previous().Literal);
        }

        if (Match(TokenType.LEFT_PAREN))
        {
            Expr expr = Expression();
            Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
            return new Expr.Grouping(expr);
        }

        throw Error(Peek(), "Expect expression.");
    }

    // returns true if the current token has any of the given types
    // I actually didn't know about params in c# before this, pretty cool equivalency
    // of Java's "..."
    private bool Match(params TokenType[] types)
    {
        foreach (TokenType type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }

    // consumes next token if its the correct type, otherwise throw an error
    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();

        throw Error(Peek(), message);
    }

    // returns true if the current token is of the given type
    private bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Peek().Type == type;
    }

    // consumes current token and returns it
    private Token Advance()
    {
        if (!IsAtEnd()) current++;
        return Previous();
    }

    // returns true if there are no tokens left
    private bool IsAtEnd()
    {
        return Peek().Type == TokenType.EOF;
    }

    // returns current token without consuming it
    private Token Peek()
    {
        return tokens[current];
    }

    // returns the most recently consumed token
    private Token Previous()
    {
        return tokens[current - 1];
    }

    // error handler for parsing
    private ParseError Error(Token token, string message)
    {
        Lox.Error(token, message);
        return new ParseError();
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Previous().Type == TokenType.SEMICOLON) return;

            switch (Peek().Type)
            {
                case TokenType.CLASS:
                case TokenType.FUN:
                case TokenType.VAR:
                case TokenType.FOR:
                case TokenType.IF:
                case TokenType.WHILE:
                case TokenType.PRINT:
                case TokenType.RETURN:
                    return;
            }

            Advance();
        }
    }

    public Expr Parse()
    {
        try
        {
            return Expression();
        }
        catch (ParseError error)
        {
            return null;
        }
    }
}

