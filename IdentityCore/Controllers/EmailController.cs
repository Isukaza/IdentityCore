using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Helpers;
using IdentityCore.DAL.PostgreSQL.Roles;
using IdentityCore.Managers.Interfaces;
using IdentityCore.Models.Request;

namespace IdentityCore.Controllers;

[ApiController]
[Route("/[controller]")]
[Authorize(Roles = nameof(UserRole.SuperAdmin))]
public class EmailController : Controller
{
    #region C-tor and fields

    private readonly IMailManager _mailManager;

    public EmailController(IMailManager mailManager)
    {
        _mailManager = mailManager;
    }

    #endregion

    /// <summary>
    /// Adds a new email template to the AWS SES.
    /// </summary>
    /// <param name="request">An object containing the details of the email template to be created, including the template name, subject, and HTML content.</param>
    /// <returns>Returns the status of the operation.</returns>
    /// <response code="200">The template was successfully created.</response>
    /// <response code="400">The request is invalid. This can happen if the request body is empty or if the data provided is incorrect.</response>
    /// <response code="500">An error occurred during the creation of the template.</response>
    [HttpPost("add-email-template")]
    public async Task<IActionResult> AddEmailTemplate([FromBody] CreateEmailTemplateRequest request)
    {
        var result = await _mailManager.CreateTemplate(request.TemplateName, request.Subject, request.HtmlContent);
        return string.IsNullOrEmpty(result)
            ? await StatusCodes.Status200OK.ResultState("Operation was successfully completed")
            : await StatusCodes.Status500InternalServerError.ResultState(result);
    }

    /// <summary>
    /// Deletes an existing email template from the AWS SES.
    /// </summary>
    /// <param name="templateName">The name of the template to be deleted.</param>
    /// <returns>Returns the status of the operation.</returns>
    /// <response code="200">The template was successfully deleted.</response>
    /// <response code="400">The request is invalid. This can happen if the template name is not provided.</response>
    /// <response code="500">An error occurred during the deletion of the template.</response>
    [HttpPost("delete-email-template")]
    public async Task<IActionResult> DeleteEmailTemplate([Required] string templateName)
    {
        var result = await _mailManager.DeleteTemplate(templateName);
        return string.IsNullOrEmpty(result)
            ? await StatusCodes.Status200OK.ResultState("Operation was successfully completed")
            : await StatusCodes.Status500InternalServerError.ResultState(result);
    }
}