// Gabriel Carver
// class for handling return statements (necessary for leaving programs early throuhg return)

using System;

public class Return : Exception
{
    public object Value { get; }

    public Return(object value)
    {
        Value = value;
    }
}