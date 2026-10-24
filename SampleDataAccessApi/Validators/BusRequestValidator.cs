using NuGet.SampleSharedModels.Interfaces;
using NuGet.SampleSharedModels.Results;
using SampleDataAccessApi.Models;

namespace SampleDataAccessApi.Validators;

using System.Text.Json;
using FluentValidation;
using NuGet.SampleSharedModels.Enums.EnumsServiceBus;

public class BusRequestValidator : AbstractValidator<BusRequest>
{
    public BusRequestValidator()
    {
        // 1. MessageId MUSS vorhanden sein (Core für Idempotenz)
        RuleFor(x => x.MessageId)
            .NotEmpty()
            .WithMessage("MessageId is required for Idempotency tracking.");

        // 2. MessageAction MUSS definiert sein (für den Composite Key)
        RuleFor(x => x.MessageAction)
            .IsInEnum()
            .WithMessage("MessageAction must be a valid enum value (Add, Update, Delete).");

        // 3. MessageStatus MUSS definiert sein und ein gültiger Wert sein
        RuleFor(x => x.MessageStatus)
            .IsInEnum()
            .WithMessage("MessageStatus must be a valid enum value (Pending, Success, Fail).");
            
        // 4. MessageData: Optional, aber wenn vorhanden, muss es JSON sein (optional, aber gut für Audit)
        RuleFor(x => x.MessageData)
            .Must(data => string.IsNullOrEmpty(data) || IsJson(data))
            .When(x => !string.IsNullOrEmpty(x.MessageData))
            .WithMessage("MessageData must be valid JSON.");

        // 5. MovieId: Optional, aber wenn angegeben, muss sie positiv sein
        // 1. Must have a value (not null)
        // 1. Must not be null
        RuleFor(x => x.MovieId)
            .NotNull()
            .WithMessage("MovieId is required.");

        // 2. Must not be Guid.Empty either
        RuleFor(x => x.MovieId)
            .NotEmpty()
            .WithMessage("MovieId cannot be empty.");

        // 3. Custom check (equivalent to NotEmpty, shown for reference)
        RuleFor(x => x.MovieId)
            .Must(id => id.HasValue && id.Value != Guid.Empty)
            .WithMessage("MovieId must be a valid non-empty GUID.");
            
        // 6. ErrorMessage: Optional, aber wenn Fail, dann sollte sie vorhanden sein (Business Rule)
        RuleFor(x => x.ErrorMessage)
            .NotEmpty()
            .When(x => x.MessageStatus == MessageStatus.Fail)
            .WithMessage("ErrorMessage is required when Status is Fail.");
    }

    private static bool IsJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}