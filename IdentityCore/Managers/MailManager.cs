using System.Net;

using IdentityCore.Configuration;
using IdentityCore.DAL.PostgreSQL.Models;
using IdentityCore.DAL.PostgreSQL.Models.enums;
using IdentityCore.Managers.Interfaces;

using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;

namespace IdentityCore.Managers;

public class MailManager : IMailManager
{
    #region Fields

    private readonly AmazonSimpleEmailServiceV2Client _sesClient = new(
        MailConfig.Values.AwsAccessKeyId,
        MailConfig.Values.AwsSecretAccessKey,
        MailConfig.Values.RegionEndpoint);

    #endregion

    #region Email Operations

    public async Task<string> SendEmailAsync(
        string toEmailAddress,
        TokenType tokenType,
        string confirmationLink,
        User user = null,
        RedisUserUpdate userUpdate = null)
    {
        var template = GenerateConfirmationContent(tokenType, confirmationLink, user, userUpdate);
        var request = new SendEmailRequest
        {
            FromEmailAddress = MailConfig.Values.Mail,
            Destination = new Destination { ToAddresses = [toEmailAddress] },
            Content = new EmailContent
            {
                Template = new Template
                {
                    TemplateName = tokenType.ToString(),
                    TemplateData = template
                }
            }
        };

        return await ExecuteSesRequestAsync(() => _sesClient.SendEmailAsync(request));
    }

    #endregion

    #region Template

    public async Task<string> CreateTemplate(string templateName, string subject, string htmlContent)
    {
        var request = new CreateEmailTemplateRequest
        {
            TemplateName = templateName,
            TemplateContent = new EmailTemplateContent
            {
                Subject = subject,
                Html = htmlContent
            }
        };

        return await ExecuteSesRequestAsync(() => _sesClient.CreateEmailTemplateAsync(request));
    }

    public async Task<string> DeleteTemplate(string templateName)
    {
        var request = new DeleteEmailTemplateRequest
        {
            TemplateName = templateName
        };

        return await ExecuteSesRequestAsync(() => _sesClient.DeleteEmailTemplateAsync(request));
    }

    #endregion

    #region Private Methods

    private static async Task<string> ExecuteSesRequestAsync<T>(Func<Task<T>> action)
        where T : AmazonWebServiceResponse
    {
        try
        {
            var response = await action();
            return response.HttpStatusCode == HttpStatusCode.OK ? string.Empty : response.HttpStatusCode.ToString();
        }
        catch (AccountSuspendedException)
        {
            return "The account's ability to send email has been permanently restricted.";
        }
        catch (MailFromDomainNotVerifiedException)
        {
            return "The sending domain is not verified.";
        }
        catch (MessageRejectedException)
        {
            return "The message content is invalid.";
        }
        catch (SendingPausedException)
        {
            return "The account's ability to send email is currently paused.";
        }
        catch (TooManyRequestsException)
        {
            return "Too many requests were made. Please try again later.";
        }
#if DEBUG
        catch (AmazonSimpleEmailServiceV2Exception ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized || ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return "Invalid security token.";
        }
        catch (Exception ex)
        {
            return $"An error occurred: {ex.Message}";
        }
#endif
    }

    private static string GenerateConfirmationContent(
        TokenType tokenType,
        string confirmationLink,
        User user,
        RedisUserUpdate userUpdate = null)
    {
        return tokenType switch
        {
            TokenType.RegistrationConfirmation =>
                $"{{\"username\":\"{user.Username}\", " +
                $"\"confirmationLink\":\"{confirmationLink}\"}}",

            TokenType.PasswordChange when userUpdate is not null =>
                $"{{\"username\":\"{user.Username}\", " +
                $"\"confirmationLink\":\"{confirmationLink}\"}}",

            TokenType.UsernameChange when userUpdate is not null =>
                $"{{\"newUsername\":\"{userUpdate.Username}\", " +
                $"\"oldUsername\":\"{user.Username}\", " +
                $"\"confirmationLink\":\"{confirmationLink}\"}}",

            TokenType.EmailChangeNew when userUpdate is not null =>
                $"{{\"username\":\"{user.Username}\", " +
                $"\"newEmail\":\"{userUpdate.Email}\", " +
                $"\"confirmationLink\":\"{confirmationLink}\"}}",

            TokenType.EmailChangeOld when userUpdate is not null =>
                $"{{\"username\":\"{user.Username}\", " +
                $"\"newEmail\":\"{userUpdate.Email}\", " +
                $"\"oldEmail\":\"{user.Email}\", " +
                $"\"confirmationLink\":\"{confirmationLink}\"}}",

            _ => string.Empty
        };
    }

    #endregion
}