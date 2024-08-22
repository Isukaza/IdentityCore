using System.ComponentModel.DataAnnotations;

namespace IdentityCore.Models.Request;

public record CreateEmailTemplateRequest
{
    [Required]
    public string TemplateName { get; init; }
    
    [Required]
    public string Subject { get; init; }
    
    [Required]
    public string HtmlContent { get; init; }
}