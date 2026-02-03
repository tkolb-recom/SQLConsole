using System.ComponentModel.DataAnnotations;
using System.IO;
using Recom.SQLConsole.Properties;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Recom.SQLConsole.UI;

public partial class UploadItem : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(UploadItem), nameof(ValidateUniquity))]
    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.RequiredField))]
    private ReleaseConfigViewModel? _release;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.RequiredField))]
    [CustomValidation(typeof(UploadItem), nameof(ValidateFileExists))]
    [CustomValidation(typeof(UploadItem), nameof(ValidateFileIsExecutable))]
    private string? _filePath;

    private static Func<ReleaseConfigViewModel?, ValidationResult>? _validateRelease;
    private static Func<string?, ValidationResult>? _validateScript;

    public bool Validate(Func<ReleaseConfigViewModel?, ValidationResult>? validateRelease = null,
        Func<string?, ValidationResult>? validateScript = null)
    {
        _validateRelease = validateRelease;
        _validateScript = validateScript;

        this.ClearErrors();
        this.ValidateAllProperties();

        _validateRelease = null;
        _validateScript = null;

        return this.HasErrors;
    }

    public static ValidationResult ValidateUniquity(ReleaseConfigViewModel? value, ValidationContext validationContext)
    {
        return _validateRelease?.Invoke(value) ?? ValidationResult.Success!;
    }

    public static ValidationResult ValidateFileExists(string? value, ValidationContext validationContext)
    {
        return File.Exists(value)
                   ? ValidationResult.Success!
                   : new ValidationResult(ValidationMessages.FileDoesNotExist);
    }

    public static ValidationResult ValidateFileIsExecutable(string? value, ValidationContext validationContext)
    {
        return _validateScript?.Invoke(value) ?? ValidationResult.Success!;
    }
}