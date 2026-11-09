using EShop.Domain.Common;
using EShop.Domain.Exceptions;

namespace EShop.Domain.Aggregates.ReviewAggregate;

/// <summary>
/// Product Review Aggregate - Customer reviews with moderation
/// 
/// BUSINESS RULES:
/// - Rating 1-5 stars only
/// - Customer must have purchased product to review
/// - One review per customer per product
/// - Moderation workflow for approval
/// - Helpful votes tracked
/// - Review can be edited within 30 days
/// </summary>
public class ProductReview : AggregateRoot<Guid>
{
    public Guid ProductId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? OrderId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public int Rating { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public bool IsVerifiedPurchase { get; private set; }
    public ReviewStatus Status { get; private set; }
    public string? ModerationNotes { get; private set; }
    public int HelpfulVotes { get; private set; }
    public int UnhelpfulVotes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    private readonly List<ReviewImage> _images = new();
    public IReadOnlyCollection<ReviewImage> Images => _images.AsReadOnly();

    private readonly List<ReviewVote> _votes = new();
    public IReadOnlyCollection<ReviewVote> Votes => _votes.AsReadOnly();

    private readonly List<ReviewResponse> _responses = new();
    public IReadOnlyCollection<ReviewResponse> Responses => _responses.AsReadOnly();

    public bool CanEdit => DateTime.UtcNow <= CreatedAt.AddDays(30) && 
                           Status != ReviewStatus.Rejected;
    public decimal HelpfulPercentage => 
        (HelpfulVotes + UnhelpfulVotes) == 0 ? 0 : 
        (decimal)HelpfulVotes / (HelpfulVotes + UnhelpfulVotes) * 100;

    private ProductReview() { }

    public static ProductReview Create(
        Guid productId,
        Guid customerId,
        string customerName,
        int rating,
        string title,
        string content,
        Guid? orderId = null)
    {
        if (productId == Guid.Empty)
            throw new DomainException("Product ID is required");
        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID is required");
        ValidateRating(rating);
        ValidateTitle(title);
        ValidateContent(content);

        return new ProductReview
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CustomerId = customerId,
            CustomerName = customerName,
            Rating = rating,
            Title = title,
            Content = content,
            OrderId = orderId,
            IsVerifiedPurchase = orderId.HasValue,
            Status = ReviewStatus.PendingModeration,
            HelpfulVotes = 0,
            UnhelpfulVotes = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    #region Edit Operations

    public void Update(int rating, string title, string content)
    {
        if (!CanEdit)
            throw new DomainException("Review can no longer be edited");

        ValidateRating(rating);
        ValidateTitle(title);
        ValidateContent(content);

        Rating = rating;
        Title = title;
        Content = content;
        UpdatedAt = DateTime.UtcNow;

        // Re-submit for moderation if previously approved
        if (Status == ReviewStatus.Approved)
        {
            Status = ReviewStatus.PendingModeration;
        }
    }

    public void AddImage(string imageUrl, string? caption = null)
    {
        if (_images.Count >= 5)
            throw new DomainException("Maximum 5 images allowed per review");

        _images.Add(new ReviewImage(Guid.NewGuid(), imageUrl, caption));
    }

    public void RemoveImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new DomainException("Image not found");

        _images.Remove(image);
    }

    #endregion

    #region Moderation

    public void Approve(string? notes = null)
    {
        if (Status == ReviewStatus.Approved)
            throw new DomainException("Review already approved");

        Status = ReviewStatus.Approved;
        ModerationNotes = notes;
        ApprovedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Rejection reason is required");

        Status = ReviewStatus.Rejected;
        ModerationNotes = reason;
    }

    public void Flag(string reason)
    {
        if (Status == ReviewStatus.Flagged)
            throw new DomainException("Review already flagged");

        Status = ReviewStatus.Flagged;
        ModerationNotes = $"Flagged: {reason}";
    }

    #endregion

    #region Voting

    public void AddVote(Guid customerId, bool isHelpful)
    {
        if (customerId == CustomerId)
            throw new DomainException("Cannot vote on your own review");

        var existingVote = _votes.FirstOrDefault(v => v.CustomerId == customerId);
        if (existingVote != null)
        {
            // Update existing vote
            if (existingVote.IsHelpful != isHelpful)
            {
                if (existingVote.IsHelpful)
                {
                    HelpfulVotes--;
                    UnhelpfulVotes++;
                }
                else
                {
                    HelpfulVotes++;
                    UnhelpfulVotes--;
                }
                existingVote.ChangeVote(isHelpful);
            }
        }
        else
        {
            _votes.Add(new ReviewVote(Guid.NewGuid(), customerId, isHelpful));
            if (isHelpful)
                HelpfulVotes++;
            else
                UnhelpfulVotes++;
        }
    }

    public void RemoveVote(Guid customerId)
    {
        var vote = _votes.FirstOrDefault(v => v.CustomerId == customerId)
            ?? throw new DomainException("Vote not found");

        if (vote.IsHelpful)
            HelpfulVotes--;
        else
            UnhelpfulVotes--;

        _votes.Remove(vote);
    }

    #endregion

    #region Responses

    public ReviewResponse AddSellerResponse(string response, string responderName)
    {
        if (string.IsNullOrWhiteSpace(response))
            throw new DomainException("Response content is required");

        if (_responses.Any(r => r.Type == ResponseType.Seller))
            throw new DomainException("Seller has already responded");

        var reviewResponse = new ReviewResponse(
            Guid.NewGuid(), 
            response, 
            responderName, 
            ResponseType.Seller);

        _responses.Add(reviewResponse);
        return reviewResponse;
    }

    public ReviewResponse AddCustomerResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            throw new DomainException("Response content is required");

        var reviewResponse = new ReviewResponse(
            Guid.NewGuid(), 
            response, 
            CustomerName, 
            ResponseType.Customer);

        _responses.Add(reviewResponse);
        return reviewResponse;
    }

    #endregion

    #region Validation

    private static void ValidateRating(int rating)
    {
        if (rating < 1 || rating > 5)
            throw new DomainException("Rating must be between 1 and 5");
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Review title is required");
        if (title.Length > 100)
            throw new DomainException("Title cannot exceed 100 characters");
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Review content is required");
        if (content.Length < 20)
            throw new DomainException("Review must be at least 20 characters");
        if (content.Length > 5000)
            throw new DomainException("Review cannot exceed 5000 characters");
    }

    #endregion
}

public class ReviewImage : Entity<Guid>
{
    public string ImageUrl { get; private set; }
    public string? Caption { get; private set; }

    internal ReviewImage(Guid id, string imageUrl, string? caption)
    {
        Id = id;
        ImageUrl = imageUrl;
        Caption = caption;
    }

    private ReviewImage() { } // EF Core
}

public class ReviewVote : Entity<Guid>
{
    public Guid CustomerId { get; private set; }
    public bool IsHelpful { get; private set; }
    public DateTime VotedAt { get; private set; }

    internal ReviewVote(Guid id, Guid customerId, bool isHelpful)
    {
        Id = id;
        CustomerId = customerId;
        IsHelpful = isHelpful;
        VotedAt = DateTime.UtcNow;
    }

    private ReviewVote() { } // EF Core

    internal void ChangeVote(bool isHelpful)
    {
        IsHelpful = isHelpful;
        VotedAt = DateTime.UtcNow;
    }
}

public class ReviewResponse : Entity<Guid>
{
    public string Content { get; private set; }
    public string ResponderName { get; private set; }
    public ResponseType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }

    internal ReviewResponse(Guid id, string content, string responderName, ResponseType type)
    {
        Id = id;
        Content = content;
        ResponderName = responderName;
        Type = type;
        CreatedAt = DateTime.UtcNow;
    }

    private ReviewResponse() { } // EF Core
}

public enum ReviewStatus
{
    PendingModeration = 1,
    Approved = 2,
    Rejected = 3,
    Flagged = 4
}

public enum ResponseType
{
    Seller = 1,
    Customer = 2
}
