using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Occtoo.Formatter.Commercetools.Services;

public interface IValidationService
{
    bool Validate<T>(T instance);
    bool ValidateAll<T>(IEnumerable<T> instances);
}

public class ValidationService : IValidationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(IServiceProvider serviceProvider, ILogger<ValidationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public bool Validate<T>(T instance)
    {
        var validator = _serviceProvider.GetService<IValidator<T>>();

        if (validator == null)
        {
            _logger.LogError("Validator for {object} was not found", typeof(T));
            return false;
        }

        var validationResult = validator.Validate(instance);
        if (validationResult.Errors.Any())
        {
            _logger.LogError("Validation errors were found for {object}, {errors}", typeof(T), string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
        }

        return validationResult.IsValid;
    }

    public bool ValidateAll<T>(IEnumerable<T> instances)
    {
        return instances.All(Validate);
    }
}
