using System.Net;
using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;

using IdentityCore.Configuration;
using IdentityCore.DAL.Models;

namespace IdentityCore.Managers;

public class MailManager
{
    private readonly AmazonSimpleEmailServiceV2Client _sesClient = new(
        Mail.Configs.AwsAccessKeyId,
        Mail.Configs.AwsSecretAccessKey,
        Mail.Configs.RegionEndpoint);

    public async Task<string> SendEmailAsync(
        string fromEmailAddress,
        string toEmailAddress,
        TokenType tokenType,
        string userName,
        string confirmationLink)
    {
        var request = new SendEmailRequest
        {
            FromEmailAddress = fromEmailAddress,
            Destination = new Destination { ToAddresses = [toEmailAddress] },
            Content = new EmailContent
            {
                Template = new Template
                {
                    TemplateName = tokenType.ToString(),
                    TemplateData = $"{{ \"username\":\"{userName}\", \"confirmationLink\":\"{confirmationLink}\"}}"
                }
            }
        };

        return await ExecuteSesRequestAsync(() => _sesClient.SendEmailAsync(request));
    }

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
        return "Unknown error";
    }
}