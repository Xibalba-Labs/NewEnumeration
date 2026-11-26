using System;
using System.Data;
using Xibalbalabs.Enumeration.Dapper;
using Xibalbalabs.Enumeration.Tests.TestFixtures;
using Xunit;

namespace Xibalbalabs.Enumeration.Tests.Dapper;

public sealed class EnumerationTypeHandlerTests
{
    [Fact]
    public void Parse_ConvertsRawValueToMatchingInstance()
    {
        var handler = new EnumerationTypeHandler<Colour, int>();

        var result = handler.Parse(2);

        Assert.Same(Colour.Green, result);
    }

    [Fact]
    public void Parse_ConvertsRawDbTypeToMatchingInstance()
    {
        var handler = new EnumerationTypeHandler<Colour, int>();

        var result = handler.Parse((long)3);

        Assert.Same(Colour.Blue, result);
    }

    [Fact]
    public void Parse_ReturnsNull_ForDbNull()
    {
        var handler = new EnumerationTypeHandler<Colour, int>();

        var result = handler.Parse(DBNull.Value);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_Throws_ForUndefinedValue()
    {
        var handler = new EnumerationTypeHandler<Colour, int>();

        Assert.Throws<ArgumentException>(() => handler.Parse(999));
    }

    [Fact]
    public void SetValue_AssignsUnderlyingValue()
    {
        var handler = new EnumerationTypeHandler<Colour, int>();
        var parameter = new FakeDbDataParameter();

        handler.SetValue(parameter, Colour.Red);

        Assert.Equal(1, parameter.Value);
    }

    [Fact]
    public void SetValue_AssignsDbNull_ForNull()
    {
        var handler = new EnumerationTypeHandler<Colour, int>();
        var parameter = new FakeDbDataParameter();

        handler.SetValue(parameter, null);

        Assert.Equal(DBNull.Value, parameter.Value);
    }

#pragma warning disable CS8766, CS8767 // System.Data's IDataParameter nullability annotations are inconsistent between get/set; this is a test-only fake.
    private sealed class FakeDbDataParameter : IDbDataParameter
    {
        public DbType DbType { get; set; }
        public ParameterDirection Direction { get; set; }
        public bool IsNullable => true;
        public string ParameterName { get; set; } = string.Empty;
        public string SourceColumn { get; set; } = string.Empty;
        public DataRowVersion SourceVersion { get; set; }
        public object? Value { get; set; }
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public int Size { get; set; }
    }
#pragma warning restore CS8766, CS8767
}
