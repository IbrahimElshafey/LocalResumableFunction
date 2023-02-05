using System.Linq.Expressions;

public abstract class Wait
{
    
}

public class Wait<Input, Output> : Wait
{
    public Wait<Input, Output> SetData(Expression<Action<Input,Output>> value)
    {
        throw new NotImplementedException();
    }

    public Wait<Input, Output> If(Expression<Func<Input, Output, bool>> value)
    {
        throw new NotImplementedException();
    }
}