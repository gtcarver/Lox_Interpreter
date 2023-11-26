// Gabriel Carver
// Scanner class

using System;
using System.Collections.Generic;

public class Scanner
{
    // dictionary used for identifying different keywords
    private static readonly Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>
    {
        { "and", TokenType.AND },
        { "class", TokenType.CLASS },
        { "else", TokenType.ELSE },
        { "false", TokenType.FALSE },
        { "for", TokenType.FOR },
        { "fun", TokenType.FUN },
        { "if", TokenType.IF },
        { "nil", TokenType.NIL },
        { "or", TokenType.OR },
        { "print", TokenType.PRINT },
        { "return", TokenType.RETURN },
        { "super", TokenType.SUPER },
        { "this", TokenType.THIS },
        { "true", TokenType.TRUE },
        { "var", TokenType.VAR },
        { "while", TokenType.WHILE }
    };

    private readonly string source;
    private readonly List<Token> tokens = new List<Token>();
    private int start = 0;
    private int current = 0;
    private int line = 1;
    public Scanner(string source)
    {
        this.source = source;
    }

    public List<Token> ScanTokens() 
    {
        // loop runs until EOF
        while (!IsAtEnd()) {
            // We are at the beginning of the next lexeme.
            start = current;
            ScanToken();
        }
        // add an EOF token to token list
        tokens.Add(new Token(TokenType.EOF, "", null, line));
        return tokens;
    }

    // function to locate the next token
    private void ScanToken()
    {
        char c = Advance();
        switch (c)
        {
            case '(': 
                AddToken(TokenType.LEFT_PAREN); 
                break;
            case ')': 
                AddToken(TokenType.RIGHT_PAREN); 
                break;
            case '{': 
                AddToken(TokenType.LEFT_BRACE); 
                break;
            case '}': 
                AddToken(TokenType.RIGHT_BRACE); 
                break;
            case ',': 
                AddToken(TokenType.COMMA); 
                break;
            case '.': 
                AddToken(TokenType.DOT); 
                break;
            case '-': 
                AddToken(TokenType.MINUS); 
                break;
            case '+': 
                AddToken(TokenType.PLUS); 
                break;
            case ';': 
                AddToken(TokenType.SEMICOLON); 
                break;
            case '*': 
                AddToken(TokenType.STAR); 
                break;
            case '!':
                AddToken(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG);
                break;
            case '=':
                AddToken(Match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL);
                break;
            case '<':
                AddToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS);
                break;
            case '>':
                AddToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER);
                break;
            case '/':
                if (Match('/'))
                {
                    // two forward slashes found -> lox comment located
                    // A comment goes until the end of the line.
                    while (Peek() != '\n' && !IsAtEnd()) Advance();
                }
                else
                {
                    // otherwise add forward slash as its own token
                    AddToken(TokenType.SLASH);
                }
                break;
            case ' ':
            case '\r':
            case '\t':
                // Ignore whitespace.
                break;
            case '\n':
                line++; // newline, so increment line counter
                break;
            case '"': String(); break; // string found, call string helper method
            default:
                if (IsDigit(c))
                {
                    Number();
                }
                else if (IsAlpha(c))
                {
                    Identifier();
                }
                else
                {
                    // unkown character, so raise an error
                    Lox.Error(line, "Unexpected character.");
                }
            break;
        }
    }

    // function to add identifier (or known keyword) to token list
    private void Identifier()
    {
        while (IsAlphaNumeric(Peek())) Advance();

        string text = source.Substring(start, current - start);
        if (keywords.TryGetValue(text, out TokenType type))
        {
            // The keyword exists in the dictionary.
            AddToken(type);
        }
        else
        {
            // It's not a keyword; treat it as an identifier.
            AddToken(TokenType.IDENTIFIER);
        }
            }

    // function to find a number in its entirety
    private void Number()
    {
        while (IsDigit(Peek())) Advance();

        // Look for a fractional part.
        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            // Consume the "."
            Advance();

            while (IsDigit(Peek())) Advance();
        }

        AddToken(TokenType.NUMBER, double.Parse(source.Substring(start, current - start)));
    }

    // function to find a string in its entirety
    private void String()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n') line++;
            Advance();
        }

        if (IsAtEnd())
        {
            // end of string was never found
            Lox.Error(line, "Unterminated string.");
            return;
        }

        // The closing ".
        Advance();

        // Trim the surrounding quotes.
        string value = source.Substring(start + 1, current - start - 2);
        AddToken(TokenType.STRING, value);
    }

    // function to determine if next char is a specific value
    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (source[current] != expected) return false;

        current++;
        return true;
    }

    // lookahead without consuming the next character
    private char Peek()
    {
        if (IsAtEnd()) return '\0';
        return source[current];
    }

    // lookahead twice without consuming characters
    private char PeekNext()
    {
        if (current + 1 >= source.Length) return '\0';
        return source[current + 1];
    }

    // checks whether given char is an alphabetic char or an underscore
    private bool IsAlpha(char c)
    {
        return (c >= 'a' && c <= 'z') ||
            (c >= 'A' && c <= 'Z') ||
                c == '_';
    }

    // checks whether given char is alphanumeric
    private bool IsAlphaNumeric(char c)
    {
        return IsAlpha(c) || IsDigit(c);
    }

    // checks if given char is a digit
    private bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    // function to advance to the next character
    private char Advance()
    {
        return source[current++];
    }

    // adds token to token list without a specific literal
    private void AddToken(TokenType type)
    {
        AddToken(type, null);
    }

    // adds token and its literal value to token list
    private void AddToken(TokenType type, object literal)
    {
        string text = source.Substring(start, current - start);
        tokens.Add(new Token(type, text, literal, line));
    }

    // determines whether scanner has reached EOF
    private bool IsAtEnd() 
    {
        return current >= source.Length;
    }
}