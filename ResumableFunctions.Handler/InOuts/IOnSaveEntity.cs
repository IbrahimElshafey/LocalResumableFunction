namespace ResumableFunctions.Handler.InOuts;

internal interface IOnSaveEntity
{
    void OnSave();
}

internal interface ILoadUnMapped
{
    void LoadUnmappedProps(params object[] args);
}
