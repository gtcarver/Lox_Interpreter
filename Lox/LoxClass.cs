using System;
using System.Collections.Generic;

class LoxClass : ILoxCallable
{
    public string name;
    public readonly LoxClass superclass;
    private readonly Dictionary<string, LoxFunction> methods = new Dictionary<string, LoxFunction>();

    public LoxClass(string name, LoxClass superclass, Dictionary<string, LoxFunction> methods)
    {
        this.superclass = superclass;
        this.name = name;
        this.methods = methods;
    }

    public LoxFunction FindMethod(string name) 
    {
        if (methods.ContainsKey(name)) return methods[name];

        if (superclass != null) 
        {
            return superclass.FindMethod(name);
        }

        return null;
    }

    public override string ToString()
    {
        return name;
    }

    public object Call(Interpreter interpreter, List<object> arguments) 
    {
        LoxInstance instance = new LoxInstance(this);
        LoxFunction initializer = FindMethod("init");
        if (initializer != null) 
        { 
            initializer.Bind(instance).Call(interpreter, arguments);
        }

        return instance;
    }
  
    public int Arity() 
    {
        LoxFunction initializer = FindMethod("init");
        if (initializer == null) return 0;
        return initializer.Arity();
    }
}