using AcmePay.Application.Features.Payments.Authorize;
using AcmePay.UnitTests.TestDoubles;
using Xunit;

namespace AcmePay.UnitTests.Payments;

public sealed class AuthorizePaymentCommandValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ShouldUseInjectedClockForExpiryYearBoundaries()
    {
        var clock = new FakeClock(new DateTimeOffset(2031, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var validator = new AuthorizePaymentCommandValidator(clock);

        var tooOld = PaymentTestData.CreateAuthorizeCommand(expiryYear: 2030);
        var valid = PaymentTestData.CreateAuthorizeCommand(expiryYear: 2031);
        var tooFar = PaymentTestData.CreateAuthorizeCommand(expiryYear: 2052);

        var tooOldResult = await validator.ValidateAsync(tooOld);
        var validResult = await validator.ValidateAsync(valid);
        var tooFarResult = await validator.ValidateAsync(tooFar);

        Assert.False(tooOldResult.IsValid);
        Assert.Contains(tooOldResult.Errors, x => x.PropertyName == nameof(AuthorizePaymentCommand.ExpiryYear));
        Assert.True(validResult.IsValid);
        Assert.False(tooFarResult.IsValid);
        Assert.Contains(tooFarResult.Errors, x => x.PropertyName == nameof(AuthorizePaymentCommand.ExpiryYear));
    }
}
