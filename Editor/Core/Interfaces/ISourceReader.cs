using DataToScriptableObject;

namespace DataToScriptableObject.Editor
{
    public interface ISourceReader
    {
        SourceValidationResult ValidateQuick();
        void ValidateFull(System.Action<SourceValidationResult> onComplete);
        TableSchema[] ExtractSchemas();
    }
}
