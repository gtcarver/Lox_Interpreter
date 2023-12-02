using System.Collections.Generic;

class EnvironmentClass
{
    readonly EnvironmentClass enclosing;

    // using a dictionary in place of java's 'Map'
    private readonly Dictionary<string, object> values = new Dictionary<string, object>();

    // Constructor for the top-level environment (global scope)
    public EnvironmentClass()
    {
        enclosing = null;
    }

    // Constructor for environments with an enclosing environment
    public EnvironmentClass(EnvironmentClass enclosing)
    {
        this.enclosing = enclosing;
    }

    public void Define(string name, object value)
    {
        values[name] = value;
    }

    public object Get(Token name)
    {
        if (values.ContainsKey(name.Lexeme))
        {
            return values[name.Lexeme];
        }

        // variable not found, check enclosing environment for variable
        if (enclosing != null) return enclosing.Get(name);

        throw new RuntimeError(name, "Undefined variable '" + name.Lexeme + "'.");
    }

    public void Assign(Token name, object value)
    {
        if (values.ContainsKey(name.Lexeme))
        {
            values[name.Lexeme] = value;
            return;
        }

        // variable not found, check enclosing environment for variable
        if (enclosing != null) 
        {
            enclosing.Assign(name, value);
            return;
        }

        throw new RuntimeError(name, "Undefined variable '" + name.Lexeme + "'.");
    }

}