using System.ComponentModel.DataAnnotations;

namespace HotelBlazor.Models;

public sealed class LoginDraft
{
    private string _userName = string.Empty;
    private string _password = string.Empty;

    [Required(ErrorMessage = "Укажите имя пользователя.")]
    [MaxLength(50, ErrorMessage = "Имя пользователя — не длиннее 50 символов.")]
    public string UserName
    {
        get => _userName;
        set => _userName = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "Укажите пароль.")]
    [MaxLength(100, ErrorMessage = "Пароль — не длиннее 100 символов.")]
    [NotWhitespace(ErrorMessage = "Пароль не может состоять только из пробелов.")]
    public string Password
    {
        get => _password;
        set => _password = value ?? string.Empty;
    }
}

public sealed class RegisterDraft
{
    private string _userName = string.Empty;
    private string _password = string.Empty;
    private string _fullName = string.Empty;
    private string? _phone;
    private string? _email;
    private string _passportNumber = string.Empty;

    [Required(ErrorMessage = "Укажите имя пользователя.")]
    [PresentLength(3, 50, TooShort = "Имя пользователя — не короче 3 символов.", TooLong = "Имя пользователя — не длиннее 50 символов.")]
    [RegularExpression(@"^[\p{L}0-9._-]*$", ErrorMessage = "Имя для входа: буквы, цифры и символы . _ -.")]
    public string UserName
    {
        get => _userName;
        set => _userName = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "Укажите пароль.")]
    [PresentLength(6, 100, TooShort = "Пароль — не короче 6 символов.", TooLong = "Пароль — не длиннее 100 символов.")]
    [NotWhitespace(ErrorMessage = "Пароль не может состоять только из пробелов.")]
    public string Password
    {
        get => _password;
        set => _password = value ?? string.Empty;
    }

    [Required(ErrorMessage = "Укажите ФИО.")]
    [PresentLength(2, 100, TooShort = "ФИО — не короче 2 символов.", TooLong = "ФИО — не длиннее 100 символов.")]
    public string FullName
    {
        get => _fullName;
        set => _fullName = value?.Trim() ?? string.Empty;
    }

    [MaxLength(30, ErrorMessage = "Телефон — не длиннее 30 символов.")]
    [RegularExpression(@"^\+?[0-9][0-9\s\-()]{5,28}$", ErrorMessage = "Телефон: цифры, пробелы, скобки и дефис, можно начать с +.")]
    public string? Phone
    {
        get => _phone;
        set => _phone = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
    [MaxLength(100, ErrorMessage = "Почта — не длиннее 100 символов.")]
    public string? Email
    {
        get => _email;
        set => _email = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [Required(ErrorMessage = "Укажите номер паспорта.")]
    [PresentLength(4, 20, TooShort = "Номер паспорта — не короче 4 символов.", TooLong = "Номер паспорта — не длиннее 20 символов.")]
    public string PassportNumber
    {
        get => _passportNumber;
        set => _passportNumber = value?.Trim() ?? string.Empty;
    }
}

public sealed class RoomDraft
{
    private string _number = string.Empty;
    private string _type = "Standard";

    [Required(ErrorMessage = "Укажите номер комнаты.")]
    [MaxLength(10, ErrorMessage = "Номер комнаты — не длиннее 10 символов.")]
    public string Number
    {
        get => _number;
        set => _number = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "Укажите тип номера.")]
    [MaxLength(50, ErrorMessage = "Тип номера — не длиннее 50 символов.")]
    public string Type
    {
        get => _type;
        set => _type = value?.Trim() ?? string.Empty;
    }

    [Range(0.01, 1_000_000, ErrorMessage = "Цена за ночь — от 0,01 до 1 000 000 BYN.")]
    public decimal PricePerNight { get; set; } = 100;

    [Range(1, 10, ErrorMessage = "Вместимость — от 1 до 10 гостей.")]
    public int Capacity { get; set; } = 2;

    public bool IsAvailable { get; set; } = true;
}

public sealed class GuestDraft
{
    private string _fullName = string.Empty;
    private string? _phone;
    private string? _email;
    private string _passportNumber = string.Empty;

    [Required(ErrorMessage = "Укажите ФИО.")]
    [PresentLength(2, 100, TooShort = "ФИО — не короче 2 символов.", TooLong = "ФИО — не длиннее 100 символов.")]
    public string FullName
    {
        get => _fullName;
        set => _fullName = value?.Trim() ?? string.Empty;
    }

    [MaxLength(30, ErrorMessage = "Телефон — не длиннее 30 символов.")]
    [RegularExpression(@"^\+?[0-9][0-9\s\-()]{5,28}$", ErrorMessage = "Телефон: цифры, пробелы, скобки и дефис, можно начать с +.")]
    public string? Phone
    {
        get => _phone;
        set => _phone = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
    [MaxLength(100, ErrorMessage = "Почта — не длиннее 100 символов.")]
    public string? Email
    {
        get => _email;
        set => _email = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [Required(ErrorMessage = "Укажите номер паспорта.")]
    [PresentLength(4, 20, TooShort = "Номер паспорта — не короче 4 символов.", TooLong = "Номер паспорта — не длиннее 20 символов.")]
    public string PassportNumber
    {
        get => _passportNumber;
        set => _passportNumber = value?.Trim() ?? string.Empty;
    }
}

public sealed class BookingDraft : IValidatableObject
{
    [Required(ErrorMessage = "Укажите дату заезда.")]
    public DateOnly CheckIn { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(14));

    [Required(ErrorMessage = "Укажите дату выезда.")]
    public DateOnly CheckOut { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(17));

    public int? GuestId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CheckIn < DateOnly.FromDateTime(DateTime.Today))
        {
            yield return new ValidationResult("Дата заезда не может быть в прошлом.", [nameof(CheckIn)]);
        }

        if (CheckOut <= CheckIn)
        {
            yield return new ValidationResult("Дата выезда должна быть позже даты заезда.", [nameof(CheckOut)]);
        }

        var nights = CheckOut.DayNumber - CheckIn.DayNumber;
        if (nights > 60)
        {
            yield return new ValidationResult("Бронирование длиннее 60 ночей не принимается.", [nameof(CheckOut)]);
        }
    }
}

public sealed class RoomSearchDraft : IValidatableObject
{
    private string? _type;

    [MaxLength(50, ErrorMessage = "Тип — не длиннее 50 символов.")]
    public string? Type
    {
        get => _type;
        set => _type = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "Цена «от» — от 0 до 1 000 000 BYN.")]
    public decimal? MinPrice { get; set; }

    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "Цена «до» — от 0 до 1 000 000 BYN.")]
    public decimal? MaxPrice { get; set; }

    public string Available { get; set; } = string.Empty;

    public string Sort { get; set; } = "number";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPrice is decimal min && MaxPrice is decimal max && min > max)
        {
            yield return new ValidationResult("Цена «от» не может быть больше цены «до».", [nameof(MaxPrice)]);
        }

        if (Available is not ("" or "true" or "false"))
        {
            yield return new ValidationResult("Выберите, сдаётся номер или нет.", [nameof(Available)]);
        }

        if (Sort is not ("number" or "-number" or "price" or "-price"))
        {
            yield return new ValidationResult("Выберите порядок списка.", [nameof(Sort)]);
        }
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class PresentLengthAttribute : ValidationAttribute
{
    public PresentLengthAttribute(int minimum, int maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    public int Minimum { get; }

    public int Maximum { get; }

    public string? TooShort { get; set; }

    public string? TooLong { get; set; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var text = value as string ?? string.Empty;
        if (text.Length == 0)
        {
            return ValidationResult.Success;
        }

        if (text.Length < Minimum)
        {
            return new ValidationResult(TooShort ?? ErrorMessage, [validationContext.MemberName!]);
        }

        if (text.Length > Maximum)
        {
            return new ValidationResult(TooLong ?? ErrorMessage, [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class NotWhitespaceAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string text && text.Length > 0 && string.IsNullOrWhiteSpace(text))
        {
            return new ValidationResult(ErrorMessage, [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }
}
