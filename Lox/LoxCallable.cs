// Gabriel Carver
// LoxCallable interface

using System.Collections.Generic;

interface ILoxCallable
{
    int Arity();
    object Call(Interpreter interpreter, List<object> arguments);

}

// handling the clock function (native functions)
class Clock : ILoxCallable
{
    public int Arity()
    {
        return 0;
    }

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        var millisec = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        return millisec / 1000.0;
    }

    public override string ToString()
    {
        return "<native fn>";
    }


}