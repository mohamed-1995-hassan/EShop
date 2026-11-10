using EShop.Application.Interfaces;
using EShop.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EShop.Infrastructure.Tests.Email;

/// <summary>
/// Unit tests for FakeEmailService
/// Verifies email logging and test helper functionality
/// </summary>
public class FakeEmailServiceTests
{
    private readonly FakeEmailService _emailService;
    private readonly Mock<ILogger<FakeEmailService>> _loggerMock;

    public FakeEmailServiceTests()
    {
        _loggerMock = new Mock<ILogger<FakeEmailService>>();
        var configMock = new Mock<IOptions<EmailConfiguration>>();
        configMock.Setup(x => x.Value).Returns(new EmailConfiguration
        {
            DefaultFromEmail = "noreply@test.com",
            DefaultFromName = "Test",
            EnableTracking = false
        });
        
        _emailService = new FakeEmailService(configMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldLogEmailDetails()
    {
        // Arrange
        var to = "test@example.com";
        var subject = "Test Subject";
        var body = "Test Body";

        // Act
        await _emailService.SendEmailAsync(to, subject, body);

        // Assert - Verify logger was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendAsync_WithEmailMessage_ShouldTrackSentEmail()
    {
        // Arrange
        var message = new EmailMessage
        {
            To = "recipient@example.com",
            Subject = "Test Subject",
            HtmlBody = "<p>Test Body</p>",
            TextBody = "Test Body"
        };

        // Act
        var result = await _emailService.SendAsync(message);

        // Assert
        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrEmpty();
        result.MessageId.Should().StartWith("fake-");
        _emailService.SentEmails.Should().ContainSingle();
    }

    [Fact]
    public async Task SendBulkAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        var messages = new List<EmailMessage>
        {
            new EmailMessage
            {
                To = "user1@example.com",
                Subject = "Subject 1",
                TextBody = "Body 1"
            },
            new EmailMessage
            {
                To = "user2@example.com",
                Subject = "Subject 2",
                TextBody = "Body 2"
            },
            new EmailMessage
            {
                To = "user3@example.com",
                Subject = "Subject 3",
                TextBody = "Body 3"
            }
        };

        // Act
        var result = await _emailService.SendBulkAsync(messages);

        // Assert
        result.SuccessCount.Should().Be(3);
        result.FailedCount.Should().Be(0);
        result.TotalCount.Should().Be(3);
        result.Results.Should().HaveCount(3);
    }

    [Fact]
    public async Task SendTemplatedEmailAsync_ShouldSucceed()
    {
        // Arrange
        var to = "user@example.com";
        var templateId = "welcome";
        var variables = new Dictionary<string, string>
        {
            { "UserName", "John" },
            { "CompanyName", "EShop" }
        };

        // Act
        var result = await _emailService.SendTemplatedEmailAsync(templateId, to, variables);

        // Assert
        result.Success.Should().BeTrue();
        result.MessageId.Should().StartWith("fake-template-");
    }

    [Fact]
    public void ClearSentEmails_ShouldClearHistory()
    {
        // Arrange
        var message = new EmailMessage
        {
            To = "test@example.com",
            Subject = "Subject",
            TextBody = "Body"
        };
        _emailService.SendAsync(message).Wait();
        _emailService.SentEmails.Should().NotBeEmpty();

        // Act
        _emailService.ClearSentEmails();

        // Assert
        _emailService.SentEmails.Should().BeEmpty();
    }

    [Fact]
    public async Task SentEmails_ShouldContainAllSentMessages()
    {
        // Arrange & Act
        await _emailService.SendAsync(new EmailMessage
        {
            To = "user1@example.com",
            Subject = "Email 1",
            TextBody = "Body 1"
        });
        await _emailService.SendAsync(new EmailMessage
        {
            To = "user2@example.com",
            Subject = "Email 2",
            TextBody = "Body 2"
        });

        // Assert
        _emailService.SentEmails.Should().HaveCount(2);
        _emailService.SentEmails.Should().Contain(e => e.To == "user1@example.com");
        _emailService.SentEmails.Should().Contain(e => e.To == "user2@example.com");
    }

    [Fact]
    public async Task GetDeliveryStatusAsync_ShouldReturnDeliveredStatus()
    {
        // Arrange
        var messageId = "test-message-id";

        // Act
        var status = await _emailService.GetDeliveryStatusAsync(messageId);

        // Assert
        status.Should().NotBeNull();
        status!.MessageId.Should().Be(messageId);
        status.State.Should().Be(DeliveryState.Delivered);
        status.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAsync_WithAttachments_ShouldSucceed()
    {
        // Arrange
        var message = new EmailMessage
        {
            To = "recipient@example.com",
            Subject = "Email with attachment",
            TextBody = "Please see attached"
        };
        message.Attachments.Add(EmailAttachment.FromBytes(
            "document.pdf",
            new byte[] { 1, 2, 3, 4, 5 },
            "application/pdf"));

        // Act
        var result = await _emailService.SendAsync(message);

        // Assert
        result.Success.Should().BeTrue();
        _emailService.SentEmails.Should().ContainSingle();
        _emailService.SentEmails.First().Attachments.Should().ContainSingle();
    }

    [Fact]
    public async Task SendAsync_WithCcAndBcc_ShouldSucceed()
    {
        // Arrange
        var message = new EmailMessage
        {
            To = "recipient@example.com",
            Subject = "Email with CC/BCC",
            TextBody = "Test body"
        };
        message.Cc.Add("cc1@example.com");
        message.Cc.Add("cc2@example.com");
        message.Bcc.Add("bcc@example.com");

        // Act
        var result = await _emailService.SendAsync(message);

        // Assert
        result.Success.Should().BeTrue();
        var sentEmail = _emailService.SentEmails.First();
        sentEmail.Cc.Should().HaveCount(2);
        sentEmail.Bcc.Should().ContainSingle();
    }
}
