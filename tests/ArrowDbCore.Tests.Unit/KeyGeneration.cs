using System.Runtime.CompilerServices;

namespace ArrowDbCore.Tests.Unit;

public class KeyGeneration {
    [InlineArray(128)]
    private struct Buffer {
        private char _first;
    }

    [Fact]
    public void GenerateTypedKey_Primitive() {
        var buffer = new Buffer();
        var key = ArrowDb.GenerateTypedKey<int>("1", buffer);
        Assert.Equal("Int32:1", key);
    }

    [Fact]
    public void GenerateTypedKey_String() {
        var buffer = new Buffer();
        var key = ArrowDb.GenerateTypedKey<string>("1", buffer);
        Assert.Equal("String:1", key);
    }

    [Fact]
    public void GenerateTypedKey_Person() {
        var buffer = new Buffer();
        var key = ArrowDb.GenerateTypedKey<Buffer>("1", buffer);
        Assert.Equal("Buffer:1", key);
    }
}