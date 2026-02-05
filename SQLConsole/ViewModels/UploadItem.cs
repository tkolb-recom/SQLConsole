using System.ComponentModel.DataAnnotations;
using System.IO;
using Recom.SQLConsole.Properties;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Recom.SQLConsole.ViewModels;

public partial class UploadItem : ObservableValidator
{
    [CustomValidation(typeof(UploadItem), nameof(ValidateUploadIsExecutable))]
    public Guid Id { get; private init; } = Guid.NewGuid();

    public string GeneralError => string.Join("\n", this.GetErrors().Select(x => x.ErrorMessage));

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(UploadItem), nameof(ValidateUniquity))]
    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.RequiredField))]
    private ReleaseConfigViewModel? _release;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.RequiredField))]
    [CustomValidation(typeof(UploadItem), nameof(ValidateFileExists))]
    private string? _filePath;

    private static Func<ReleaseConfigViewModel?, ValidationResult>? _validateRelease;
    private static Func<Guid, ValidationResult>? _validateUpload;

    public bool Validate(Func<ReleaseConfigViewModel?, ValidationResult>? validateRelease = null,
        Func<Guid, ValidationResult>? validateUpload = null)
    {
        _validateRelease = validateRelease;
        _validateUpload = validateUpload;

        this.ClearErrors();
        this.ValidateAllProperties();

        _validateRelease = null;
        _validateUpload = null;

        this.OnPropertyChanged(nameof(this.GeneralError));

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

    public static ValidationResult ValidateUploadIsExecutable(Guid id, ValidationContext validationContext)
    {
        return _validateUpload?.Invoke(id) ?? ValidationResult.Success!;
    }
}