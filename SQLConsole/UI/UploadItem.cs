using System.ComponentModel.DataAnnotations;
using System.IO;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Recom.SQLConsole.UI;

public partial class UploadItem : ObservableValidator
{
    [ObservableProperty]
    [Required]
    private ReleaseConfigViewModel? _release;

    [ObservableProperty]
    [Required]
    [CustomValidation(typeof(UploadItem), nameof(ValidateFileExists))]
    [CustomValidation(typeof(UploadItem), nameof(ValidateFileIsExecutable))]
    private string? _filePath;

    private static Func<string?, ValidationResult>? _runScript = null;

    public bool Validate(Func<string?, ValidationResult> runScript)
    {
        _runScript = runScript;

        this.ClearErrors();
        this.ValidateAllProperties();

        _runScript = null;

        return this.HasErrors;
    }

    public static ValidationResult ValidateFileExists(string? value, ValidationContext validationContext)
    {
        return File.Exists(value)
                   ? ValidationResult.Success!
                   : new ValidationResult("Datei existiert nicht.");
    }

    public static ValidationResult ValidateFileIsExecutable(string? value, ValidationContext validationContext)
    {
        return _runScript?.Invoke(value) ?? ValidationResult.Success!;
    }
}