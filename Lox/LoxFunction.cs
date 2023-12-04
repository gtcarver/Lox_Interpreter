class LoxFunction : ILoxCallable 
{

   private readonly Stmt.Function declaration;
   private readonly EnvironmentClass closure;

   private readonly bool isInitializer;

    public LoxFunction(Stmt.Function declaration, EnvironmentClass closure, bool isInitializer)
    {
        this.isInitializer = isInitializer;
        this.closure = closure;
        this.declaration = declaration;
    }

    public LoxFunction Bind(LoxInstance instance) 
    {
        EnvironmentClass environment = new EnvironmentClass(closure);
        environment.Define("this", instance);
        return new LoxFunction(declaration, environment, isInitializer);
    }

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        EnvironmentClass environment = new EnvironmentClass(closure);
        for (int i = 0; i < declaration.funParams.Count; i++)
        {
            environment.Define(declaration.funParams[i].Lexeme, arguments[i]);
        }

        try 
        {
            interpreter.ExecuteBlock(declaration.body, environment);
        } 
        catch (Return returnValue) 
        {
            if (isInitializer) return closure.GetAt(0, "this");

            return returnValue.Value;
        }

        return null; 
    }

    public int Arity() 
    {
        return declaration.funParams.Count;
    }

    public override string ToString() 
    {
        return "<fn " + declaration.name.Lexeme + ">";
    }

}