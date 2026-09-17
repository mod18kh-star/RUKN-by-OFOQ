namespace OFOQ.Market.Domain.Commerce.Reviews;
public readonly record struct ProductReviewId(Guid Value)
{
 public bool IsEmpty=>Value==Guid.Empty; public static ProductReviewId New()=>new(Guid.NewGuid()); public static ProductReviewId From(Guid value){if(value==Guid.Empty)throw new ArgumentException("Review ID cannot be empty.",nameof(value));return new(value);} public override string ToString()=>Value.ToString();
}
