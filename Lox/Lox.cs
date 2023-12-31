﻿// Gabriel Carver
// Main file to be ran for Lox Interpreter

using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

class Lox
{
    private static Interpreter interpreter = new Interpreter();
    private static bool hadError = false;
    static bool hadRuntimeError = false;

    static void Main(string[] args)
    {
        try
        {
            if (args.Length > 1)
            {
                Console.WriteLine("Usage: clox [script]"); // more than 1 argument passed, exit
                Environment.Exit(64);
            }
            else if (args.Length == 1)
            {
                RunFile(args[0]); // one argument passed, attempt to find and run the file
            }
            else
            {
                RunPrompt(); // otherwise (no arguments) go to RunPrompt
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error: {ex.Message}"); // error encountered, print error msg
        }
    }

    // function to run a specified lox file
    static void RunFile(string path)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(path); // read in everything from given path
            Run(Encoding.Default.GetString(bytes)); // run everything that's read in
            // Indicate an error in the exit code.
            if (hadError) Environment.Exit(65);
            if (hadRuntimeError) Environment.Exit(70);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error reading file: {ex.Message}"); // encountered an error, print error msg
        }
    }

    // function to run individual lox commands given by user
    static void RunPrompt()
    {
        try
        {
            using (StreamReader reader = new StreamReader(Console.OpenStandardInput()))
            {
                while (true) // continuously get input
                {
                    Console.Write("> ");
                    string line = reader.ReadLine(); // read line from user
                    if (line == null) break;
                    Run(line); // run the line fetched from input
                    hadError = false;
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error reading from console: {ex.Message}"); // encountered an error, print error msg
        }
    }

    // function to run a given lox command/commands
    static void Run(string source)
    {
        var scanner = new Scanner(source);
        // get list of tokens using ScanTokens from Scanner.cs
        var tokens = scanner.ScanTokens();

        Parser parser = new Parser(tokens);
        List<Stmt> statements = parser.Parse();

        // Stop if there was a syntax error.
        if (hadError) return;

        Resolver resolver = new Resolver(interpreter);
        resolver.Resolve(statements);

        // Stop if there was a resolution error.
        if (hadError) return;
        
        interpreter.Interpret(statements);
    }

    // function to call when an error is generated
    public static void Error(int line, string message)
    {
        Report(line, "", message);
    }

    // function for reporting the error to the user
    private static void Report(int line, string where, string message)
    {
        Console.Error.WriteLine($"[line {line}] Error{where}: {message}");
        hadError = true;
    }

    // Error thrown by parser
    public static void Error(Token token, string message)
    {
        if (token.Type == TokenType.EOF)
        {
            Report(token.Line, " at end", message);
        }
        else
        {
            Report(token.Line, " at '" + token.Lexeme + "'", message);
        }
    }

    public static void RuntimeError(RuntimeError error)
    {
        Console.Error.WriteLine(error.Message + $"\n[line {error.Token.Line}]");
        hadRuntimeError = true;
    }
}