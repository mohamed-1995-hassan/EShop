using EShop.Domain.Common;
using EShop.Domain.Exceptions;
using EShop.Domain.ValueObjects;

namespace EShop.Domain.Aggregates.AuctionAggregate;

/// <summary>
/// Auction Aggregate - Bidding system for products
/// 
/// BUSINESS RULES:
/// - Auctions have start/end times
/// - Minimum bid increments enforced
/// - Reserve price (optional hidden minimum)
/// - Buy Now price (optional instant purchase)
/// - Proxy/automatic bidding support
/// - Anti-sniping extension (extends time if bid in final minutes)
/// - Winner notification and payment processing
/// </summary>
public class Auction : AggregateRoot<Guid>
{
    public string AuctionNumber { get; private set; } = string.Empty;
    public Guid ProductId { get; private set; }
    public Guid SellerId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public AuctionStatus Status { get; private set; }
    public AuctionType Type { get; private set; }
    public Money StartingPrice { get; private set; } = null!;
    public Money? ReservePrice { get; private set; }
    public Money? BuyNowPrice { get; private set; }
    public Money CurrentBid { get; private set; } = null!;
    public Money MinimumBidIncrement { get; private set; } = null!;
    public int BidCount { get; private set; }
    public Guid? CurrentWinnerId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public DateTime? ActualEndTime { get; private set; }
    public int ExtensionCount { get; private set; }
    public int MaxExtensions { get; private set; }
    public int ExtensionMinutes { get; private set; }
    public int AntiSnipingMinutes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<Bid> _bids = new();
    public IReadOnlyCollection<Bid> Bids => _bids.AsReadOnly();

    private readonly List<AuctionWatcher> _watchers = new();
    public IReadOnlyCollection<AuctionWatcher> Watchers => _watchers.AsReadOnly();

    public bool IsActive => Status == AuctionStatus.Active && DateTime.UtcNow >= StartTime && DateTime.UtcNow < EffectiveEndTime;
    public bool HasReserve => ReservePrice != null;
    public bool ReserveMet => !HasReserve || CurrentBid.Amount >= ReservePrice!.Amount;
    public bool HasBuyNow => BuyNowPrice != null && Status == AuctionStatus.Active;
    public DateTime EffectiveEndTime => ActualEndTime ?? EndTime;
    public TimeSpan TimeRemaining => IsActive ? EffectiveEndTime - DateTime.UtcNow : TimeSpan.Zero;

    private Auction() { }

    public static Auction Create(
        Guid productId,
        Guid sellerId,
        string title,
        string description,
        Money startingPrice,
        DateTime startTime,
        DateTime endTime,
        Money? reservePrice = null,
        Money? buyNowPrice = null,
        AuctionType type = AuctionType.Standard,
        string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Auction title is required");
        if (startTime >= endTime)
            throw new DomainException("End time must be after start time");
        if (startTime < DateTime.UtcNow.AddMinutes(-5))
            throw new DomainException("Start time cannot be in the past");
        if (reservePrice != null && reservePrice.Amount <= startingPrice.Amount)
            throw new DomainException("Reserve price must be higher than starting price");
        if (buyNowPrice != null && buyNowPrice.Amount <= startingPrice.Amount)
            throw new DomainException("Buy Now price must be higher than starting price");

        return new Auction
        {
            Id = Guid.NewGuid(),
            AuctionNumber = GenerateAuctionNumber(),
            ProductId = productId,
            SellerId = sellerId,
            Title = title,
            Description = description,
            ImageUrl = imageUrl,
            Status = startTime <= DateTime.UtcNow ? AuctionStatus.Active : AuctionStatus.Scheduled,
            Type = type,
            StartingPrice = startingPrice,
            ReservePrice = reservePrice,
            BuyNowPrice = buyNowPrice,
            CurrentBid = startingPrice,
            MinimumBidIncrement = CalculateMinBidIncrement(startingPrice),
            BidCount = 0,
            StartTime = startTime,
            EndTime = endTime,
            ExtensionCount = 0,
            MaxExtensions = 5,
            ExtensionMinutes = 5,
            AntiSnipingMinutes = 2,
            CreatedAt = DateTime.UtcNow
        };
    }

    #region Bidding

    public Bid PlaceBid(Guid bidderId, Money bidAmount, Money? maxAutoBid = null)
    {
        ValidateBidding(bidderId, bidAmount);

        var bid = new Bid(
            Guid.NewGuid(),
            bidderId,
            bidAmount,
            maxAutoBid,
            BidType.Manual);

        ProcessBid(bid);
        return bid;
    }

    public Bid PlaceProxyBid(Guid bidderId, Money maxBidAmount)
    {
        if (maxBidAmount.Amount <= CurrentBid.Amount)
            throw new DomainException("Maximum bid must be higher than current bid");

        // Calculate minimum bid to place
        var minBid = new Money(CurrentBid.Amount + MinimumBidIncrement.Amount, CurrentBid.Currency);

        var bid = new Bid(
            Guid.NewGuid(),
            bidderId,
            minBid,
            maxBidAmount,
            BidType.Proxy);

        ProcessBid(bid);
        return bid;
    }

    private void ValidateBidding(Guid bidderId, Money bidAmount)
    {
        if (!IsActive)
            throw new DomainException("Auction is not active");
        if (bidderId == SellerId)
            throw new DomainException("Seller cannot bid on own auction");
        if (bidAmount.Currency != CurrentBid.Currency)
            throw new DomainException("Bid currency must match auction currency");

        var minimumBid = _bids.Count == 0 
            ? StartingPrice.Amount 
            : CurrentBid.Amount + MinimumBidIncrement.Amount;

        if (bidAmount.Amount < minimumBid)
            throw new DomainException($"Bid must be at least {minimumBid} {bidAmount.Currency}");
    }

    private void ProcessBid(Bid newBid)
    {
        // Check for proxy bid war
        var currentProxyBid = _bids
            .Where(b => b.BidderId != newBid.BidderId && b.MaxAutoBid != null && !b.IsOutbid)
            .OrderByDescending(b => b.MaxAutoBid!.Amount)
            .FirstOrDefault();

        if (currentProxyBid != null && newBid.BidAmount.Amount <= currentProxyBid.MaxAutoBid!.Amount)
        {
            // Auto-outbid the new bidder
            var autoBidAmount = new Money(
                Math.Min(newBid.BidAmount.Amount + MinimumBidIncrement.Amount, currentProxyBid.MaxAutoBid.Amount),
                CurrentBid.Currency);

            var autoBid = new Bid(
                Guid.NewGuid(),
                currentProxyBid.BidderId,
                autoBidAmount,
                currentProxyBid.MaxAutoBid,
                BidType.Automatic);

            newBid.MarkAsOutbid();
            _bids.Add(newBid);
            _bids.Add(autoBid);
            
            CurrentBid = autoBidAmount;
            CurrentWinnerId = autoBid.BidderId;
            BidCount += 2;
        }
        else
        {
            // New bid wins
            foreach (var existingBid in _bids.Where(b => !b.IsOutbid))
            {
                existingBid.MarkAsOutbid();
            }

            _bids.Add(newBid);
            CurrentBid = newBid.BidAmount;
            CurrentWinnerId = newBid.BidderId;
            BidCount++;
        }

        // Anti-sniping: extend auction if bid in final minutes
        CheckAntiSniping();
        
        // Update minimum increment based on new price
        MinimumBidIncrement = CalculateMinBidIncrement(CurrentBid);
    }

    private void CheckAntiSniping()
    {
        var timeUntilEnd = EffectiveEndTime - DateTime.UtcNow;
        if (timeUntilEnd.TotalMinutes <= AntiSnipingMinutes && ExtensionCount < MaxExtensions)
        {
            ActualEndTime = DateTime.UtcNow.AddMinutes(ExtensionMinutes);
            ExtensionCount++;
        }
    }

    #endregion

    #region Buy Now

    public void BuyNow(Guid buyerId)
    {
        if (!HasBuyNow)
            throw new DomainException("Buy Now is not available");
        if (!IsActive)
            throw new DomainException("Auction is not active");
        if (buyerId == SellerId)
            throw new DomainException("Seller cannot buy own item");

        // Mark all existing bids as outbid
        foreach (var bid in _bids.Where(b => !b.IsOutbid))
        {
            bid.MarkAsOutbid();
        }

        var buyNowBid = new Bid(
            Guid.NewGuid(),
            buyerId,
            BuyNowPrice!,
            null,
            BidType.BuyNow);

        _bids.Add(buyNowBid);
        CurrentBid = BuyNowPrice!;
        CurrentWinnerId = buyerId;
        BidCount++;

        EndAuction(AuctionEndReason.BuyNow);
    }

    #endregion

    #region Auction Lifecycle

    public void Start()
    {
        if (Status != AuctionStatus.Scheduled)
            throw new DomainException("Only scheduled auctions can be started");

        Status = AuctionStatus.Active;
    }

    public void EndAuction(AuctionEndReason reason = AuctionEndReason.TimeExpired)
    {
        if (Status != AuctionStatus.Active)
            throw new DomainException("Only active auctions can be ended");

        ActualEndTime = DateTime.UtcNow;

        if (BidCount == 0)
        {
            Status = AuctionStatus.EndedNoBids;
        }
        else if (!ReserveMet)
        {
            Status = AuctionStatus.EndedReserveNotMet;
        }
        else
        {
            Status = AuctionStatus.EndedWithWinner;
        }
    }

    public void Cancel(string reason)
    {
        if (Status == AuctionStatus.EndedWithWinner)
            throw new DomainException("Cannot cancel auction with winner");

        Status = AuctionStatus.Cancelled;
    }

    public void MarkAsPaid(Guid paymentId)
    {
        if (Status != AuctionStatus.EndedWithWinner)
            throw new DomainException("Auction must have a winner to be paid");

        Status = AuctionStatus.Paid;
    }

    public void MarkAsCompleted()
    {
        if (Status != AuctionStatus.Paid)
            throw new DomainException("Auction must be paid before completion");

        Status = AuctionStatus.Completed;
    }

    #endregion

    #region Watchers

    public void AddWatcher(Guid customerId)
    {
        if (_watchers.Any(w => w.CustomerId == customerId))
            return;

        _watchers.Add(new AuctionWatcher(Guid.NewGuid(), customerId));
    }

    public void RemoveWatcher(Guid customerId)
    {
        var watcher = _watchers.FirstOrDefault(w => w.CustomerId == customerId);
        if (watcher != null)
            _watchers.Remove(watcher);
    }

    #endregion

    #region Configuration

    public void UpdateBuyNowPrice(Money? newPrice)
    {
        if (BidCount > 0)
            throw new DomainException("Cannot change Buy Now price after bids received");

        BuyNowPrice = newPrice;
    }

    public void UpdateReservePrice(Money? newPrice)
    {
        if (BidCount > 0)
            throw new DomainException("Cannot change reserve price after bids received");

        ReservePrice = newPrice;
    }

    public void ExtendEndTime(DateTime newEndTime)
    {
        if (newEndTime <= EffectiveEndTime)
            throw new DomainException("New end time must be later than current end time");

        ActualEndTime = newEndTime;
    }

    #endregion

    private static Money CalculateMinBidIncrement(Money currentPrice)
    {
        var increment = currentPrice.Amount switch
        {
            < 10 => 0.5m,
            < 50 => 1m,
            < 100 => 2m,
            < 500 => 5m,
            < 1000 => 10m,
            < 5000 => 25m,
            _ => 50m
        };
        return new Money(increment, currentPrice.Currency);
    }

    private static string GenerateAuctionNumber()
    {
        return $"AUC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}

public class Bid : Entity<Guid>
{
    public Guid BidderId { get; private set; }
    public Money BidAmount { get; private set; }
    public Money? MaxAutoBid { get; private set; }
    public BidType Type { get; private set; }
    public bool IsOutbid { get; private set; }
    public bool IsWinning { get; private set; }
    public DateTime PlacedAt { get; private set; }

    internal Bid(Guid id, Guid bidderId, Money bidAmount, Money? maxAutoBid, BidType type)
    {
        Id = id;
        BidderId = bidderId;
        BidAmount = bidAmount;
        MaxAutoBid = maxAutoBid;
        Type = type;
        IsOutbid = false;
        IsWinning = true;
        PlacedAt = DateTime.UtcNow;
    }

    private Bid() { }

    internal void MarkAsOutbid()
    {
        IsOutbid = true;
        IsWinning = false;
    }
}

public class AuctionWatcher : Entity<Guid>
{
    public Guid CustomerId { get; private set; }
    public DateTime AddedAt { get; private set; }
    public bool NotifyOnBid { get; private set; }
    public bool NotifyOnEnd { get; private set; }

    internal AuctionWatcher(Guid id, Guid customerId)
    {
        Id = id;
        CustomerId = customerId;
        AddedAt = DateTime.UtcNow;
        NotifyOnBid = true;
        NotifyOnEnd = true;
    }

    private AuctionWatcher() { }
}

public enum AuctionStatus
{
    Draft = 1,
    Scheduled = 2,
    Active = 3,
    EndedNoBids = 4,
    EndedReserveNotMet = 5,
    EndedWithWinner = 6,
    Paid = 7,
    Completed = 8,
    Cancelled = 9
}

public enum AuctionType
{
    Standard = 1,       // Highest bid wins
    Dutch = 2,          // Price decreases until someone buys
    Sealed = 3,         // Bids are hidden
    Reserve = 4,        // Has minimum price
    Penny = 5           // Small increment auctions
}

public enum BidType
{
    Manual = 1,
    Proxy = 2,
    Automatic = 3,
    BuyNow = 4
}

public enum AuctionEndReason
{
    TimeExpired = 1,
    BuyNow = 2,
    SellerEnded = 3,
    AdminEnded = 4
}
