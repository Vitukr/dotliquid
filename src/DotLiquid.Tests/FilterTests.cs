using System.Collections;
using System.Globalization;
using DotLiquid.Exceptions;
using NUnit.Framework;

namespace DotLiquid.Tests
{
    [TestFixture]
    public class FilterTests
    {
        #region Classes used in tests

        private static class MoneyFilter
        {
            public static string Money(object input)
            {
                return string.Format(" {0:d}$ ", input);
            }

            public static string MoneyWithUnderscore(object input)
            {
                return string.Format(" {0:d}$ ", input);
            }
        }

        private static class CanadianMoneyFilter
        {
            public static string Money(object input)
            {
                return string.Format(" {0:d}$ CAD ", input);
            }
        }

        private static class FiltersWithArguments
        {
            public static string Adjust(int input, int offset = 10)
            {
                return string.Format("[{0:d}]", input + offset);
            }

            public static string AddSub(int input, int plus, int minus = 20)
            {
                return string.Format("[{0:d}]", input + plus - minus);
            }
        }

        private static class FiltersWithMulitpleMethodSignatures
        {
            public static string Concat(string one, string two)
            {
                return string.Concat(one, two);
            }

            public static string Concat(string one, string two, string three)
            {
                return string.Concat(one, two, three);
            }
        }

        private static class FiltersWithMultipleMethodSignaturesAndContextParam
        {
            public static string ConcatWithContext(Context context, string one, string two)
            {
                return string.Concat(one, two);
            }

            public static string ConcatWithContext(Context context, string one, string two, string three)
            {
                return string.Concat(one, two, three);
            }
        }

        private static class ContextFilters
        {
            public static string BankStatement(Context context, object input)
            {
                return string.Format(" " + context["name"] + " has {0:d}$ ", input);
            }
        }

        #endregion

        private Context _context;

        [OneTimeSetUp]
        public void SetUp()
        {
            _context = new Context(CultureInfo.InvariantCulture);
        }

        /*[Test]
        public void TestNonExistentFilter()
        {
            _context["var"] = 1000;
            Assert.Throws<FilterNotFoundException>(() => new Variable("var | syzzy").Render(_context));
        }*/

        [Test]
        public void TestLocalFilter()
        {
            _context["var"] = 1000;
            _context.AddFilters(typeof(MoneyFilter));
            Assert.AreEqual(" 1000$ ", new Variable("var | money").Render(_context));
        }

        [Test]
        public void TestUnderscoreInFilterName()
        {
            _context["var"] = 1000;
            _context.AddFilters(typeof(MoneyFilter));
            Assert.AreEqual(" 1000$ ", new Variable("var | money_with_underscore").Render(_context));
        }

        [Test]
        public void TestFilterWithNumericArgument()
        {
            _context["var"] = 1000;
            _context.AddFilters(typeof(FiltersWithArguments));
            Assert.AreEqual("[1005]", new Variable("var | adjust: 5").Render(_context));
        }

        [Test]
        public void TestFilterWithNegativeArgument()
        {
            _context["var"] = 1000;
            _context.AddFilters(typeof(FiltersWithArguments));
            Assert.AreEqual("[995]", new Variable("var | adjust: -5").Render(_context));
        }

        [Test]
        public void TestFilterWithDefaultArgument()
        {
            _context["var"] = 1000;
            _context.AddFilters(typeof(FiltersWithArguments));
            Assert.AreEqual("[1010]", new Variable("var | adjust").Render(_context));
        }

        [Test]
        public void TestFilterWithTwoArguments()
        {
            _context["var"] = 1000;
            _context.AddFilters(typeof(FiltersWithArguments));
            Assert.AreEqual("[1150]", new Variable("var | add_sub: 200, 50").Render(_context));
        }

        [Test]
        public void TestFilterWithMultipleMethodSignatures()
        {
            Template.RegisterFilter(typeof(FiltersWithMulitpleMethodSignatures));

            Assert.AreEqual("AB", Template.Parse("{{'A' | concat : 'B'}}").Render());
            Assert.AreEqual("ABC", Template.Parse("{{'A' | concat : 'B', 'C'}}").Render());
        }

        [Test]
        public void TestFilterWithMultipleMethodSignaturesAndContextParam()
        {
            Template.RegisterFilter(typeof(FiltersWithMultipleMethodSignaturesAndContextParam));

            Assert.AreEqual("AB", Template.Parse("{{'A' | concat_with_context : 'B'}}").Render());
            Assert.AreEqual("ABC", Template.Parse("{{'A' | concat_with_context : 'B', 'C'}}").Render());
        }

        /*/// <summary>
        /// ATM the trailing value is silently ignored. Should raise an exception?
        /// </summary>
        [Test]
        public void TestFilterWithTwoArgumentsNoComma()
        {
            _context["var"] = 1000;
            _context.AddFilters(typeof(FiltersWithArguments));
            Assert.AreEqual("[1150]", string.Join(string.Empty, new Variable("var | add_sub: 200 50").Render(_context));
        }*/

        [Test]
        public void TestSecondFilterOverwritesFirst()
        {
            _context["var"] = 1000;
            _context.AddFilters(typeof(MoneyFilter));
            _context.AddFilters(typeof(CanadianMoneyFilter));
            Assert.AreEqual(" 1000$ CAD ", new Variable("var | money").Render(_context));
        }

        [Test]
        public void TestSize()
        {
            _context["var"] = "abcd";
            _context.AddFilters(typeof(MoneyFilter));
            Assert.AreEqual(4, new Variable("var | size").Render(_context));
        }

        [Test]
        public void TestJoin()
        {
            _context["var"] = new[] { 1, 2, 3, 4 };
            Assert.AreEqual("1 2 3 4", new Variable("var | join").Render(_context));
        }

        [Test]
        public void TestSort()
        {
            _context["value"] = 3;
            _context["numbers"] = new[] { 2, 1, 4, 3 };
            _context["words"] = new[] { "expected", "as", "alphabetic" };
            _context["arrays"] = new[] { new[] { "flattened" }, new[] { "are" } };

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, new Variable("numbers | sort").Render(_context) as IEnumerable);
            CollectionAssert.AreEqual(new[] { "alphabetic", "as", "expected" }, new Variable("words | sort").Render(_context) as IEnumerable);
            CollectionAssert.AreEqual(new[] { 3 }, new Variable("value | sort").Render(_context) as IEnumerable);
            CollectionAssert.AreEqual(new[] { "are", "flattened" }, new Variable("arrays | sort").Render(_context) as IEnumerable);
        }

        [Test]
        public void TestSplit()
        {
            _context["var"] = "a~b";
            Assert.AreEqual(new[] { "a", "b" }, new Variable("var | split:'~'").Render(_context));
        }

        [Test]
        public void TestStripHtml()
        {
            _context["var"] = "<b>bla blub</a>";
            Assert.AreEqual("bla blub", new Variable("var | strip_html").Render(_context));
        }

        [Test]
        public void Capitalize()
        {
            _context["var"] = "blub";
            Assert.AreEqual("Blub", new Variable("var | capitalize").Render(_context));
        }

        [Test]
        public void Slice()
        {
            _context["var"] = "blub";
            Assert.AreEqual("b", new Variable("var | slice: 0, 1").Render(_context));
            Assert.AreEqual("bl", new Variable("var | slice: 0, 2").Render(_context));
            Assert.AreEqual("l", new Variable("var | slice: 1").Render(_context));
            Assert.AreEqual("", new Variable("var | slice: 4, 1").Render(_context));
            Assert.AreEqual("ub", new Variable("var | slice: -2, 2").Render(_context));
            Assert.AreEqual(null, new Variable("var | slice: 5, 1").Render(_context));
        }

        [Test]
        public void TestLocalGlobal()
        {
            Template.RegisterFilter(typeof(MoneyFilter));

            Assert.AreEqual(" 1000$ ", Template.Parse("{{1000 | money}}").Render());
            Assert.AreEqual(" 1000$ CAD ", Template.Parse("{{1000 | money}}").Render(new RenderParameters(CultureInfo.InvariantCulture) { Filters = new[] { typeof(CanadianMoneyFilter) } }));
            Assert.AreEqual(" 1000$ CAD ", Template.Parse("{{1000 | money}}").Render(new RenderParameters(CultureInfo.InvariantCulture) { Filters = new[] { typeof(CanadianMoneyFilter) } }));
        }

        [Test]
        public void TestContextFilter()
        {
            _context["var"] = 1000;
            _context["name"] = "King Kong";
            _context.AddFilters(typeof(ContextFilters));
            Assert.AreEqual(" King Kong has 1000$ ", new Variable("var | bank_statement").Render(_context));
        }
    }
}
