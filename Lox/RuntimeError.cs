// Gabriel Carver
// class for handling runtime errors

using System;

public class RuntimeError : Exception
{
    public readonly Token Token;

    public RuntimeError(Token token, string message) : base(message)
    {
        Token = token;
    }
}