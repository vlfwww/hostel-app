using System.ComponentModel.DataAnnotations;

namespace HotelApi.Common;

public sealed class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is null || !ShouldValidate(argument))
            {
                continue;
            }

            var results = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(
                argument,
                new ValidationContext(argument),
                results,
                validateAllProperties: true);

            if (valid)
            {
                continue;
            }

            var errors = results
                .SelectMany(result =>
                {
                    var members = result.MemberNames.Any() ? result.MemberNames : [string.Empty];
                    return members.Select(member => (member, result.ErrorMessage ?? "Некорректное значение"));
                })
                .GroupBy(item => item.member)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(item => item.Item2).Distinct().ToArray());

            return Results.ValidationProblem(
                errors,
                title: "Ошибка валидации",
                instance: context.HttpContext.Request.Path,
                extensions: new Dictionary<string, object?>
                {
                    ["traceId"] = context.HttpContext.TraceIdentifier
                });
        }

        return await next(context);
    }

    private static bool ShouldValidate(object argument)
    {
        if (argument is CancellationToken or HttpContext or IValidatableObject)
        {
            return argument is IValidatableObject;
        }

        var type = argument.GetType();
        if (type.Namespace != "HotelApi.Dtos")
        {
            return false;
        }

        if (type.GetCustomAttributes(typeof(ValidationAttribute), inherit: true).Length > 0)
        {
            return true;
        }

        return type.GetProperties().Any(property =>
            property.GetCustomAttributes(typeof(ValidationAttribute), inherit: true).Length > 0);
    }
}
