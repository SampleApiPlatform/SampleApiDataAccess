
using NuGet.SampleSharedModels.Enums.EnumsServiceBus;
using NuGet.SampleSharedModels.Interfaces;
using NuGet.SampleSharedModels.Results;
using SampleDataAccessApi.Models;

namespace SampleDataAccessApi.Validators;

public class BusRequestValidator : IValidator<BusRequest>
{


     public ValidationResult Validate(BusRequest busRequest)
    {
        var validationResult = new ValidationResult();
        if (!string.IsNullOrEmpty(busRequest.ErrorMessage) && busRequest.MessageStatus != MessageStatus.Fail)
        {
            validationResult.Errors.Add("The busRequest has an error but the MessageStatus is not MessageStatus.Fail");
        }
        
        if (busRequest.MessageAction == MessageAction.None)
        {
            validationResult.Errors.Add("Invalid MessageAction = MessageAction.None. Valid Values: Add, Update, Delete");
        }

        if (busRequest.MessageStatus == MessageStatus.Success && busRequest.MovieId == null)
        {
            validationResult.Errors.Add("The MovieId related to this BusRequest is missing.");
        }

        if (busRequest.MessageStatus == MessageStatus.None)
        {
            validationResult.Errors.Add("The BusRequest has an invalid MessageStatus.");
        }

        if (busRequest.MessageData == null)
        {
            validationResult.Errors.Add("The BusRequest does not have any MessageData.");
        }
        
        if (validationResult.Errors.Count == 0)
        {
            validationResult.IsValid = true;
        }
        else
        {
            validationResult.IsValid = false;
        }

        return validationResult;

    }

    
}
