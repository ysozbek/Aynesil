namespace Aynesil.Domain.Modules.Legal.Entities;

/// <summary>
/// Maps to legal.contract_template_translation.
/// PK: (contract_template_id, locale). Stores the rendered title and body markup
/// for a specific locale. Content is immutable once the template version is archived.
/// </summary>
public class ContractTemplateTranslation
{
    public Guid ContractTemplateId { get; private set; }
    public string Locale { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;

    /// <summary>Template body — may contain merge-field placeholders such as {{student_name}}.</summary>
    public string Body { get; private set; } = string.Empty;

    private ContractTemplateTranslation() { }

    public ContractTemplateTranslation(Guid contractTemplateId, string locale, string title, string body)
    {
        if (string.IsNullOrWhiteSpace(locale))   throw new ArgumentException("Locale is required.",  nameof(locale));
        if (string.IsNullOrWhiteSpace(title))    throw new ArgumentException("Title is required.",   nameof(title));
        if (string.IsNullOrWhiteSpace(body))     throw new ArgumentException("Body is required.",    nameof(body));

        ContractTemplateId = contractTemplateId;
        Locale             = locale.Trim().ToLowerInvariant();
        Title              = title.Trim();
        Body               = body.Trim();
    }

    public void Update(string title, string body)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(body))  throw new ArgumentException("Body is required.",  nameof(body));

        Title = title.Trim();
        Body  = body.Trim();
    }
}
