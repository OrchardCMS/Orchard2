namespace OrchardCore.Environment.Extensions.Features
{
    /// <summary>
    /// Provides validation for whether features are allowed to be disabled on a per tenant basis.
    /// </summary>
    public interface IFeatureValidationService
    {
        bool IsFeatureValid(string id);
    }
}
