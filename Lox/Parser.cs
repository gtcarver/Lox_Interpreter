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
        This is the grammar we're working with (as of ch. 9):

        program        → declaration* EOF ;
        declaration    → varDecl | statement ;
        varDecl        → "var" IDENTIFIER ( "=" expression )? ";" ;
        statement      → exprStmt | forStmt | ifStmt | printStmt | whileStmt | block ;
        exprStmt       → expression ";" ;
        forStmt        → "for" "(" ( varDecl | exprStmt | ";" ) expression? ";" expression? ")" statement ;
        ifStmt         → "if" "(" expression ")" statement ( "else" statement )? ;
        printStmt      → "print" expression ";" ;
        whileStmt      → "while" "(" expression ")" statement ;
        block          → "{" declaration* "}" ;
        expression     → assignment ;
        assignment     → IDENTIFIER "=" assignment | logic_or ;
        logic_or       → logic_and ( "or" logic_and )* ;
        logic_and      → equality ( "and" equality )* ;
        equality       → comparison ( ( "!=" | "==" ) comparison )* ;
        comparison     → term ( ( ">" | ">=" | "<" | "<=" ) term )* ;
        term           → factor ( ( "-" | "+" ) factor )* ;
        factor         → unary ( ( "/" | "*" ) unary )* ;
        unary          → ( "!" | "-" ) unary | primary ;
        primary        → NUMBER | STRING | "true" | "false" | "nil" | "(" expression ")" | IDENTIFIER ;
    
        Will be expanded later.
    */

    // declaration production rule
    private Stmt Declaration()
    {
        try
        {
            if (Match(TokenType.VAR)) return VarDeclaration();

            return Statement();
        }
        catch (Parser.ParseError error)
        {
            Synchronize();
            return null;
        }
    }

    // variable declaration production rule
    private Stmt VarDeclaration()
    {
        Token name = Consume(TokenType.IDENTIFIER, "Expect variable name.");

        Expr initializer = null;
        if (Match(TokenType.EQUAL))
        {
            initializer = Expression();
        }

        Consume(TokenType.SEMICOLON, "Expect ';' after variable declaration.");
        return new Stmt.Var(name, initializer);
    }

    // statement production rule
    private Stmt Statement()
    {
        if (Match(TokenType.FOR)) return ForStatement();
        if (Match(TokenType.IF)) return IfStatement();
        if (Match(TokenType.PRINT)) return PrintStatement();
        if (Match(TokenType.WHILE)) return WhileStatement();
        if (Match(TokenType.LEFT_BRACE)) return new Stmt.Block(Block());

        return ExpressionStatement();
    }

    // for statement production rule
    private Stmt ForStatement() 
    {
        Consume(TokenType.LEFT_PAREN, "Expect '(' after 'for'.");

        // initializer
        Stmt initializer;
        if (Match(TokenType.SEMICOLON)) 
        {
            initializer = null;
        } 
        else if (Match(TokenType.VAR)) 
        {
            initializer = VarDeclaration();
        }
        else 
        {
            initializer = ExpressionStatement();
        }

        // condition
        Expr condition = null;
        if (!Check(TokenType.SEMICOLON)) 
        {
            condition = Expression();
        }
        Consume(TokenType.SEMICOLON, "Expect ';' after loop condition.");

        // increment
        Expr increment = null;
        if (!Check(TokenType.RIGHT_PAREN)) 
        {
            increment = Expression();
        }
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after for clauses.");

        // body
        Stmt body = Statement();

        
        if (increment != null)
        {
            List<Stmt> bodyVars = new List<Stmt>
                {
                    body,
                    new Stmt.Expression(increment)
                };
            body = new Stmt.Block(bodyVars);
        }   
        
        condition ??= new Expr.Literal(true); // if no condition, make always true (infinite loop)
        body = new Stmt.While(condition, body);

        if (initializer != null) 
        {
            List<Stmt> moreBodyVars = new List<Stmt>
            {
                initializer,
                body
            };
            body = new Stmt.Block(moreBodyVars);
        }

        return body;
    }

    // if statement production rule
    private Stmt IfStatement()
    {
        Consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
        Expr condition = Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after if condition.");

        Stmt thenBranch = Statement();
        Stmt elseBranch = null;
        if (Match(TokenType.ELSE))
        {
            elseBranch = Statement();
        }

        return new Stmt.If(condition, thenBranch, elseBranch);
    }

    // expression statement production rule
    private Stmt ExpressionStatement()
    {
        Expr expr = Expression();
        Consume(TokenType.SEMICOLON, "Expect ';' after expression.");
        return new Stmt.Expression(expr);
    }

    // print statement production rule
    private Stmt PrintStatement()
    {
        Expr value = Expression();
        Consume(TokenType.SEMICOLON, "Expect ';' after value.");
        return new Stmt.Print(value);
    }

    // while statement production rule
    private Stmt WhileStatement() 
    {
        Consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
        Expr condition = Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after condition.");
        Stmt body = Statement();
        return new Stmt.While(condition, body);
    }

    // block statement production rule
    private List<Stmt> Block()
    {
        List<Stmt> statements = new List<Stmt>();

        while (!Check(TokenType.RIGHT_BRACE) && !IsAtEnd())
        {
            statements.Add(Declaration());
        }

        Consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
        return statements;
    }

    // expression production rule
    private Expr Expression()
    {
        return Assignment();
    }

    // assignment production rule
    private Expr Assignment()
    {
        Expr expr = Or();

        if (Match(TokenType.EQUAL))
        {
            Token equals = Previous();
            Expr value = Assignment();

            if (expr is Expr.Variable variable)
            {
                Token name = variable.name;
                return new Expr.Assign(name, value);
            }

            Error(equals, "Invalid assignment target.");
        }

        return expr;
    }

    // logical or production rule
    private Expr Or() 
    {
        Expr expr = And();

        while (Match(TokenType.OR)) 
        {
            Token operatorToken = Previous();
            Expr right = And();
            expr = new Expr.Logical(expr, operatorToken, right);
        }

        return expr;
    }

    // logical and production rule
    private Expr And() 
    {
        Expr expr = Equality();

        while (Match(TokenType.AND)) 
        {
            Token operatorToken = Previous();
            Expr right = Equality();
            expr = new Expr.Logical(expr, operatorToken, right);
        }

        return expr;
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

        if (Match(TokenType.IDENTIFIER))
        {
            return new Expr.Variable(Previous());
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

    
    public List<Stmt> Parse()
    {
        List<Stmt> statements = new List<Stmt>();
        while (!IsAtEnd())
        {
            statements.Add(Declaration());
        }

        return statements;
    }
}

