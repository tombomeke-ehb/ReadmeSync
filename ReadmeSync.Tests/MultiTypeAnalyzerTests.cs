using System.Linq;
using Xunit;
using ReadmeSync.Models;
using ReadmeSync.Services;

namespace ReadmeSync.Tests
{
    public class MultiTypeAnalyzerTests
    {
        private readonly CodeAnalyzer _analyzer = new();

        private System.Collections.Generic.IReadOnlyList<CodeFileInfo> Analyze(string language, string code) =>
            _analyzer.AnalyzeAll(code, LanguagePatterns.For(language), language);

        [Fact]
        public void CSharp_MultipleTypesInOneFile_AreAllReturnedWithTheirOwnMembers()
        {
            string code = @"
namespace Shop.Orders;

/// <summary>
/// An order placed by a customer.
/// </summary>
public class Order : IEntity
{
    public decimal Total() => 0;
    // TODO: Add discounts
}

/// <summary>Stores orders.</summary>
public interface IOrderRepository
{
    Order? Find(int id);
    void Save(Order order);
}

public enum OrderStatus { Open, Paid }
";
            var types = Analyze("csharp", code);

            Assert.Equal(new[] { "Order", "IOrderRepository", "OrderStatus" }, types.Select(t => t.Class));
            Assert.All(types, t => Assert.Equal("Shop.Orders", t.Namespace));

            Assert.Equal("An order placed by a customer.", types[0].Summary);
            Assert.Equal("IEntity", types[0].Inheritance);
            Assert.Equal(new[] { "Total" }, types[0].Methods);
            Assert.Equal(new[] { "Add discounts" }, types[0].Todos);

            Assert.Equal("Stores orders.", types[1].Summary);
            Assert.Equal(new[] { "Find", "Save" }, types[1].Methods);
            Assert.Empty(types[1].Todos);

            Assert.Equal("enum", types[2].TypeKeyword);
            Assert.Equal("", types[2].Summary);
            Assert.Empty(types[2].Methods);
        }

        [Fact]
        public void CSharp_NullableAndGenericReturnTypes_AreDetected()
        {
            string code = @"
namespace App
{
    public class Finder
    {
        public string? FindRoot(string start, out string? reason) { reason = null; return null; }
        public async Task<List<int>> LoadAsync() => new();
        public T Get<T>(int id) => default!;
        public Finder(int x) { }
        public int Count => Compute(1);
        private void Hidden() { }
    }
}";
            var type = Assert.Single(Analyze("csharp", code));

            Assert.Equal(new[] { "FindRoot", "LoadAsync", "Get" }, type.Methods);
        }

        [Fact]
        public void CSharp_KeywordsInsideStringsAndComments_AreIgnored()
        {
            string code = @"
namespace App
{
    // This class pattern is tricky: class Fake { }
    public class Real
    {
        private string _sample = @""
namespace Inner { public class NotReal { public void Nope() { } } }
// TODO: not a real todo
"";
        public void Run() { var text = ""public void AlsoNope() { }""; }
    }
}";
            var type = Assert.Single(Analyze("csharp", code));

            Assert.Equal("Real", type.Class);
            Assert.Equal(new[] { "Run" }, type.Methods);
            Assert.Empty(type.Todos);
        }

        [Fact]
        public void CSharp_RecordWithPrimaryConstructor_KeepsOnlyBaseTypes()
        {
            string code = @"
namespace App;
public record Point(int X, int Y) : Shape(X), IComparable<Point>;
public class Service(ILogger logger) : BaseService(logger) where T : class { }
";
            var types = Analyze("csharp", code);

            Assert.Equal("Shape, IComparable<Point>", types[0].Inheritance);
            Assert.Equal("BaseService", types[1].Inheritance);
        }

        [Fact]
        public void CSharp_PrivateNestedType_IsNotReported()
        {
            string code = @"
namespace App
{
    public class Outer
    {
        public void Before() { }

        private sealed class Helper
        {
            public void Internal() { }
            // TODO: tidy helper
        }

        public class Nested
        {
            public void Inside() { }
        }

        public void After() { }
    }
}";
            var types = Analyze("csharp", code);

            Assert.Equal(new[] { "Outer", "Nested" }, types.Select(t => t.Class));
            Assert.Equal(new[] { "Before", "After" }, types[0].Methods);
            Assert.Equal(new[] { "tidy helper" }, types[0].Todos);
            Assert.Equal(new[] { "Inside" }, types[1].Methods);
        }

        [Fact]
        public void Summary_OfAnotherMember_IsNotBorrowed()
        {
            string code = @"
namespace App
{
    public class First
    {
        /// <summary>Does a thing.</summary>
        public void Thing() { }
    }

    public class Second { }
}";
            var types = Analyze("csharp", code);

            Assert.Equal("", types[0].Summary);
            Assert.Equal("", types[1].Summary);
        }

        [Fact]
        public void Todos_SupportFixmeAndHack_WithLineNumbers()
        {
            string code = "namespace App\n{\n    public class Job\n    {\n        // TODO: first\n        // FIXME: broken on Mondays\n        /* HACK: remove after v2 */\n        // and // TODO comments are described here\n        // TODO\n    }\n}";

            var type = Assert.Single(Analyze("csharp", code));

            Assert.Equal(new[] { "first", "FIXME: broken on Mondays", "HACK: remove after v2" }, type.Todos);
            Assert.Equal(new[] { "TODO", "FIXME", "HACK" }, type.TodoItems.Select(t => t.Tag));
            Assert.Equal(new[] { 5, 6, 7 }, type.TodoItems.Select(t => t.Line));
            Assert.Equal(3, type.Line);
        }

        [Fact]
        public void Java_DocTagsAreDropped_AndInterfaceMethodsFound()
        {
            string code = @"
package com.shop;

import java.util.List;

/**
 * Finds products.
 *
 * @author someone
 * @since 1.0
 */
public interface ProductFinder {
    List<Product> findAll();
    Product findById(long id);
}

record Product(long id, String name) implements Comparable<Product> { }
";
            var types = Analyze("java", code);

            Assert.Equal("Finds products.", types[0].Summary);
            Assert.Equal(new[] { "findAll", "findById" }, types[0].Methods);
            Assert.Equal("record", types[1].TypeKeyword);
            Assert.Equal("implements Comparable<Product>", types[1].Inheritance);
        }

        [Fact]
        public void Kotlin_ClassesAndFunctions_AreExtracted()
        {
            string code = @"
package com.shop.cart

/**
 * A shopping cart.
 */
class Cart(private val owner: String) : Basket(owner), Printable {
    fun add(item: Item) { }
    override fun print() { }
    private fun recalc() { }
    // TODO: support coupons
}

data class Item(val name: String, val price: Double)

object Registry {
    suspend fun load(): List<Item> = emptyList()
}
";
            var types = Analyze("kotlin", code);

            Assert.Equal(new[] { "Cart", "Item", "Registry" }, types.Select(t => t.Class));
            Assert.Equal("com.shop.cart", types[0].Namespace);
            Assert.Equal("A shopping cart.", types[0].Summary);
            Assert.Equal("Basket, Printable", types[0].Inheritance);
            Assert.Equal(new[] { "add", "print" }, types[0].Methods);
            Assert.Equal(new[] { "support coupons" }, types[0].Todos);
            Assert.Equal("data class", types[1].TypeKeyword);
            Assert.Empty(types[1].Methods);
            Assert.Equal("object", types[2].TypeKeyword);
            Assert.Equal(new[] { "load" }, types[2].Methods);
        }

        [Fact]
        public void Go_StructsInterfacesAndReceivers_AreExtracted()
        {
            string code = @"package server

import ""fmt""

// Server handles incoming requests.
// It is safe for concurrent use.
type Server struct {
	addr string
}

// Handler processes one request.
type Handler interface {
	Handle(req string) error
	name() string
}

func (s *Server) Start() error { return nil }
func (s *Server) stop() {}
func (s Server) Addr() string { return fmt.Sprintf(""%s"", s.addr) }

// TODO: add TLS support
";
            var types = Analyze("go", code);

            Assert.Equal(new[] { "Server", "Handler" }, types.Select(t => t.Class));
            Assert.Equal("server", types[0].Namespace);
            Assert.Equal("struct", types[0].TypeKeyword);
            Assert.Equal("Server handles incoming requests. It is safe for concurrent use.", types[0].Summary);
            Assert.Equal(new[] { "Start", "Addr" }, types[0].Methods);
            Assert.Equal("interface", types[1].TypeKeyword);
            Assert.Equal(new[] { "Handle" }, types[1].Methods);
            Assert.Contains("add TLS support", types.SelectMany(t => t.Todos));
        }

        [Fact]
        public void TypeScript_MethodsAreDetected_ButControlFlowAndPrivateMembersAreNot()
        {
            string code = @"
import type { Foo } from './foo';

/** Talks to the API. */
export class ApiClient extends BaseClient implements Disposable {
    constructor(private url: string) { super(); }

    public async fetchAll(): Promise<Item[]> {
        if (this.url) {
            for (const x of []) { }
        }
        return [];
    }

    static create(url: string): ApiClient { return new ApiClient(url); }

    private secret(): void { }

    dispose(): void { }
}

export interface Disposable {
    dispose(): void;
    name?: string;
}

export type Item = { id: number };
";
            var types = Analyze("typescript", code);

            Assert.Equal(new[] { "ApiClient", "Disposable", "Item" }, types.Select(t => t.Class));
            Assert.Equal("Talks to the API.", types[0].Summary);
            Assert.Equal("BaseClient, Disposable", types[0].Inheritance);
            Assert.Equal(new[] { "fetchAll", "create", "dispose" }, types[0].Methods);
            Assert.Equal(new[] { "dispose" }, types[1].Methods);
            Assert.Equal("type", types[2].TypeKeyword);
        }

        [Fact]
        public void JavaScript_FunctionCallsAreNotMethods()
        {
            string code = @"
export class Cart {
    add(item) {
        this.items.push(item);
        notify(item);
        if (item.price > 100) { log('big'); }
    }

    async checkout() { }
}

export function helper(a, b) {
    return a + b;
}
";
            var types = Analyze("javascript", code);

            Assert.Equal(new[] { "Cart", "helper" }, types.Select(t => t.Class));
            Assert.Equal(new[] { "add", "checkout" }, types[0].Methods);
            Assert.Equal("function", types[1].TypeKeyword);
        }

        [Fact]
        public void Python_MethodsBelongToTheirClass_AndPrivateOnesAreSkipped()
        {
            string code = "class First(Base):\n    '''First class.'''\n\n    def __init__(self):\n        pass\n\n    def run(self):\n        pass\n\n    def _internal(self):\n        pass\n\n\ndef module_function():\n    pass\n\n\nclass Second:\n    \"\"\"Second class.\"\"\"\n\n    async def fetch(self):\n        pass\n    # TODO: retry\n";

            var types = Analyze("python", code);

            Assert.Equal(new[] { "First", "Second" }, types.Select(t => t.Class));
            Assert.Equal("First class.", types[0].Summary);
            Assert.Equal("Base", types[0].Inheritance);
            Assert.Equal(new[] { "run" }, types[0].Methods);
            Assert.Equal("Second class.", types[1].Summary);
            Assert.Equal(new[] { "fetch" }, types[1].Methods);
            Assert.Equal(new[] { "retry" }, types[1].Todos);
        }

        [Fact]
        public void Php_ClassConstantAndMagicMethods_AreIgnored()
        {
            string code = @"<?php
namespace App\Models;

/**
 * A user account.
 */
final class User extends Model implements Authenticatable, JsonSerializable
{
    public function __construct() { $x = Other::class; }
    public static function find(int $id) { }
    public function save() { }
    protected function touch() { }
}
";
            var type = Assert.Single(Analyze("php", code));

            Assert.Equal("User", type.Class);
            Assert.Equal("A user account.", type.Summary);
            Assert.Equal("Model, Authenticatable, JsonSerializable", type.Inheritance);
            Assert.Equal(new[] { "find", "save" }, type.Methods);
        }

        [Theory]
        [InlineData("cs", "csharp")]
        [InlineData("C#", "csharp")]
        [InlineData("TS", "typescript")]
        [InlineData("golang", "go")]
        [InlineData("kt", "kotlin")]
        [InlineData("cobol", null)]
        public void Normalize_MapsAliases(string input, string? expected)
        {
            Assert.Equal(expected, LanguagePatterns.Normalize(input));
        }

        [Theory]
        [InlineData("Program.cs", "csharp")]
        [InlineData("App.tsx", "typescript")]
        [InlineData("types.d.ts", null)]
        [InlineData("bundle.min.js", null)]
        [InlineData("index.mjs", "javascript")]
        [InlineData("main.go", "go")]
        [InlineData("notes.txt", null)]
        public void FromFileName_DetectsLanguage(string fileName, string? expected)
        {
            Assert.Equal(expected, LanguagePatterns.FromFileName(fileName));
        }
    }
}
