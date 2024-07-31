using System.Net;

using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;

using IdentityCore.Configuration;

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
        string subject,
        string htmlContent)
    {
        var request = new SendEmailRequest
        {
            FromEmailAddress = fromEmailAddress,
            Destination = new Destination { ToAddresses = [toEmailAddress] },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = subject },
                    Body = new Body
                    {
                        Html = new Content { Data = htmlContent }
                    }
                }
            }
        };

        try
        {
            var response = await _sesClient.SendEmailAsync(request);
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
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized || ex.StatusCode == HttpStatusCode.Forbidden)
            {
                return "Invalid security token.";
            }
        }
        catch (Exception ex)
        {
            return $"An error occurred while sending the email: {ex.Message}";
        }
#endif

        return "Unknown error";
    }
}