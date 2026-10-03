using SmsProNikita.Models;

namespace SmsProNikita.Tests;

public sealed class SendMessageRequestTests
{
    [Fact]
    public void ValidRequestPassesValidation()
    {
        CreateValid().Validate();
    }

    [Fact]
    public void NumericSenderOfFourteenDigitsIsValid()
    {
        var request = CreateValid();
        request.Sender = "99655512345678";

        request.Validate();
    }

    [Fact]
    public void PhoneWithPlusIsValid()
    {
        var request = CreateValid();
        request.Phones = ["+996550123456"];

        request.Validate();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("АВС123")]
    [InlineData("abc-123")]
    [InlineData("abcdefghijklm")]
    public void InvalidIdIsRejected(string id)
    {
        var request = CreateValid();
        request.Id = id;

        var exception = Assert.Throws<ArgumentException>(request.Validate);
        Assert.Equal("Id", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("СМС-СЕРВИС")]
    [InlineData("sender_1")]
    [InlineData("too-long-sender")]
    [InlineData("9965551234567")]
    public void InvalidSenderIsRejected(string sender)
    {
        var request = CreateValid();
        request.Sender = sender;

        var exception = Assert.Throws<ArgumentException>(request.Validate);
        Assert.Equal("Sender", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("755141192")]
    [InlineData("+99655012345")]
    [InlineData("9965501234567")]
    [InlineData("+79995501234")]
    public void InvalidPhoneIsRejected(string phone)
    {
        var request = CreateValid();
        request.Phones = [phone];

        var exception = Assert.Throws<ArgumentException>(request.Validate);
        Assert.Equal("Phones", exception.ParamName);
    }

    [Fact]
    public void EmptyTextIsRejected()
    {
        var request = CreateValid();
        request.Text = string.Empty;

        var exception = Assert.Throws<ArgumentException>(request.Validate);
        Assert.Equal("Text", exception.ParamName);
    }

    [Fact]
    public void TextLongerThanEightHundredCharactersIsRejected()
    {
        var request = CreateValid();
        request.Text = new string('a', 801);

        var exception = Assert.Throws<ArgumentException>(request.Validate);
        Assert.Equal("Text", exception.ParamName);
    }

    [Fact]
    public void TextOfExactlyEightHundredCharactersIsValid()
    {
        var request = CreateValid();
        request.Text = new string('a', 800);

        request.Validate();
    }

    [Fact]
    public void EmptyPhoneListIsRejected()
    {
        var request = CreateValid();
        request.Phones = [];

        var exception = Assert.Throws<ArgumentException>(request.Validate);
        Assert.Equal("Phones", exception.ParamName);
    }

    [Fact]
    public void MoreThanFiftyRecipientsAreRejected()
    {
        var request = CreateValid();
        request.Phones = Enumerable.Range(0, 51).Select(i => $"9965501{i:D5}").ToArray();

        var exception = Assert.Throws<ArgumentException>(request.Validate);
        Assert.Equal("Phones", exception.ParamName);
    }

    [Fact]
    public void FiftyRecipientsAreValid()
    {
        var request = CreateValid();
        request.Phones = Enumerable.Range(0, 50).Select(i => $"9965501{i:D5}").ToArray();

        request.Validate();
    }

    private static SendMessageRequest CreateValid() => new()
    {
        Id = "A88726",
        Sender = "My-company",
        Text = "Any SMS message text",
        Phones = ["996550123456"],
    };
}
