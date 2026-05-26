using System.Globalization;

namespace Genesis.Core.Tests;

public class DynamicTests
{
    #region Construction

    [Fact]
    public void Constructor_WithValidString_ShouldInitializeValue()
    {
        var dynamic = new Dynamic("hello");

        Assert.Equal("hello", dynamic.ToString());
    }

    [Fact]
    public void Constructor_WithEmptyString_ShouldInitializeEmptyValue()
    {
        var dynamic = new Dynamic(string.Empty);

        Assert.Equal(string.Empty, dynamic.ToString());
    }

    [Fact]
    public void Constructor_WithNull_ShouldThrowArgumentNullException()
    {
        var act = () => new Dynamic(null!);

        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Empty_StaticProperty_ShouldReturnEmptyDynamic()
    {
        var empty = Dynamic.Empty;

        Assert.Equal(string.Empty, empty.ToString());
    }

    #endregion

    #region Implicit Conversions - To Dynamic

    [Fact]
    public void ImplicitFrom_String_ShouldConvertCorrectly()
    {
        Dynamic d = "hello";

        Assert.Equal("hello", d.ToString());
    }

    [Fact]
    public void ImplicitFrom_Int_ShouldConvertCorrectly()
    {
        Dynamic d = 42;

        Assert.Equal("42", d.ToString());
    }

    [Fact]
    public void ImplicitFrom_NegativeInt_ShouldConvertCorrectly()
    {
        Dynamic d = -17;

        Assert.Equal("-17", d.ToString());
    }

    [Fact]
    public void ImplicitFrom_Double_ShouldConvertCorrectly()
    {
        Dynamic d = 3.14;

        Assert.Equal("3.14", d.ToString());
    }

    [Fact]
    public void ImplicitFrom_BoolTrue_ShouldConvertCorrectly()
    {
        Dynamic d = true;

        Assert.Equal("True", d.ToString());
    }

    [Fact]
    public void ImplicitFrom_BoolFalse_ShouldConvertCorrectly()
    {
        Dynamic d = false;

        Assert.Equal("False", d.ToString());
    }

    [Fact]
    public void ImplicitFrom_Guid_ShouldConvertCorrectly()
    {
        var guid = Guid.NewGuid();
        Dynamic d = guid;

        Assert.Equal(guid.ToString(), d.ToString());
    }

    #endregion

    #region Explicit Conversions - From Dynamic

    [Fact]
    public void ImplicitTo_String_ShouldReturnOriginalValue()
    {
        Dynamic d = "hello";
        string result = d;

        Assert.Equal("hello", result);
    }

    [Fact]
    public void ExplicitTo_Int_ValidValue_ShouldParseCorrectly()
    {
        Dynamic d = "42";
        int result = (int)d;

        Assert.Equal(42, result);
    }

    [Fact]
    public void ExplicitTo_Int_InvalidValue_ShouldReturnZero()
    {
        Dynamic d = "not_a_number";
        int result = (int)d;

        Assert.Equal(0, result);
    }

    [Fact]
    public void ExplicitTo_Int_EmptyValue_ShouldReturnZero()
    {
        Dynamic d = Dynamic.Empty;
        int result = (int)d;

        Assert.Equal(0, result);
    }

    [Fact]
    public void ExplicitTo_Double_ValidValue_ShouldParseCorrectly()
    {
        Dynamic d = "3.14";
        double result = (double)d;

        Assert.Equal(3.14, result);
    }

    [Fact]
    public void ExplicitTo_Double_InvalidValue_ShouldReturnZero()
    {
        Dynamic d = "not_a_number";
        double result = (double)d;

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void ExplicitTo_Bool_TrueString_ShouldReturnTrue()
    {
        Dynamic d = "True";
        bool result = (bool)d;

        Assert.True(result);
    }

    [Fact]
    public void ExplicitTo_Bool_FalseString_ShouldReturnFalse()
    {
        Dynamic d = "False";
        bool result = (bool)d;

        Assert.False(result);
    }

    [Fact]
    public void ExplicitTo_Bool_InvalidValue_ShouldReturnFalse()
    {
        Dynamic d = "not_a_bool";
        bool result = (bool)d;

        Assert.False(result);
    }

    [Fact]
    public void ExplicitTo_Guid_ValidValue_ShouldParseCorrectly()
    {
        var guid = Guid.NewGuid();
        Dynamic d = guid.ToString();
        Guid result = (Guid)d;

        Assert.Equal(guid, result);
    }

    [Fact]
    public void ExplicitTo_Guid_InvalidValue_ShouldReturnEmptyGuid()
    {
        Dynamic d = "not-a-guid";
        Guid result = (Guid)d;

        Assert.Equal(Guid.Empty, result);
    }

    #endregion

    #region Change

    [Fact]
    public void Change_WithIntValue_ShouldUpdateInternalValue()
    {
        var dynamic = new Dynamic("initial");
        dynamic.Change(99);

        Assert.Equal("99", dynamic.ToString());
    }

    [Fact]
    public void Change_WithNegativeIntValue_ShouldUpdateInternalValue()
    {
        var dynamic = new Dynamic("initial");
        dynamic.Change(-5);

        Assert.Equal("-5", dynamic.ToString());
    }

    [Fact]
    public void Change_WithZero_ShouldUpdateInternalValue()
    {
        var dynamic = new Dynamic("initial");
        dynamic.Change(0);

        Assert.Equal("0", dynamic.ToString());
    }

    #endregion

    #region ToUpper

    [Fact]
    public void ToUpper_ShouldReturnUppercaseString()
    {
        var dynamic = new Dynamic("hello world");

        Assert.Equal("HELLO WORLD", dynamic.ToUpper());
    }

    [Fact]
    public void ToUpper_AlreadyUppercase_ShouldReturnSameString()
    {
        var dynamic = new Dynamic("HELLO");

        Assert.Equal("HELLO", dynamic.ToUpper());
    }

    [Fact]
    public void ToUpper_EmptyValue_ShouldReturnEmptyString()
    {
        var dynamic = Dynamic.Empty;

        Assert.Equal(string.Empty, dynamic.ToUpper());
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnInternalValue()
    {
        var dynamic = new Dynamic("test value");

        Assert.Equal("test value", dynamic.ToString());
    }

    [Fact]
    public void ToString_CalledMultipleTimes_ShouldReturnSameValue()
    {
        var dynamic = new Dynamic("consistent");

        Assert.Equal("consistent", dynamic.ToString());
        Assert.Equal("consistent", dynamic.ToString());
        Assert.Equal("consistent", dynamic.ToString());
    }

    #endregion

    #region Culture Invariance

    [Fact]
    public void ImplicitFrom_Int_ShouldBeCultureInvariant()
    {
        // Ensure number formatting is invariant regardless of thread culture
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE"); // uses comma for decimal
            Dynamic d = 42;

            Assert.Equal("42", d.ToString());
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Fact]
    public void ImplicitFrom_Double_ShouldBeCultureInvariant()
    {
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            Dynamic d = 3.14;

            Assert.Equal("3.14", d.ToString());
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Fact]
    public void ToUpper_ShouldBeCultureInvariant()
    {
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR"); // Turkish I issue
            var dynamic = new Dynamic("hello");

            Assert.Equal("HELLO", dynamic.ToUpper());
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    #endregion
}
