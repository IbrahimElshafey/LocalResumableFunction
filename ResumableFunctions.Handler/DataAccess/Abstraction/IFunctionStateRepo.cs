namespace ResumableFunctions.Handler.DataAccess.Abstraction;

public interface IGenericRepo<Entity>
{
    bool MarkAsRemoved(Entity entity);
    bool Add(Entity entity);
}
public interface IFunctionStateRepo
{

}