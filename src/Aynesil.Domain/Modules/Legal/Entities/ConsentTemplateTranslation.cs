namespace Aynesil.Domain.Modules.Legal.Entities;

/// <summary>
/// Maps to legal.consent_template_translation.
/// PK: (consent_template_id, locale). Stores the rendered title and body for KVKK/consent text.
/// Content must match the version that was shown to the guardian when the consent was recorded.
/// </summary>
public class ConsentTemplateTranslation
{
    public Guid ConsentTemplateId { get; private set; }
    public string Locale { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;

    private ConsentTemplateTranslation() { }

    public ConsentTemplateTranslation(Guid consentTemplateId, string locale, string title, string body)
    {
        if (string.IsNullOrWhiteSpace(locale)) throw new ArgumentException("Locale is required.",  nameof(locale));
        if (string.IsNullOrWhiteSpace(title))  throw new ArgumentException("Title is required.",   nameof(title));
        if (string.IsNullOrWhiteSpace(body))   throw new ArgumentException("Body is required.",    nameof(body));

        ConsentTemplateId = consentTemplateId;
        Locale            = locale.Trim().ToLowerInvariant();
        Title             = title.Trim();
        Body              = body.Trim();
    }

    public void Update(string title, string body)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(body))  throw new ArgumentException("Body is required.",  nameof(body));

        Title = title.Trim();
        Body  = body.Trim();
    }
}
