using LanguageExt;
using static LanguageExt.Prelude;
using System;
using Xunit;
using SharedTesting;

namespace LanguageExtExamples
{
    internal class MyType
    {
        public int Nr { get; private set; }
        public MyType(int nr) => Nr = nr;
        public MyType With(int nr) => new MyType(nr);
    }

    internal class MyTypeDescriptor
    {
        public string Description { get; private set; }
        public MyTypeDescriptor(int nr) => Description = nr.ToString();
        public MyTypeDescriptor(string description) => Description = description;
        public MyType With(int nr) => new MyType(nr);
        public static explicit operator MyTypeDescriptor(MyType m) => new MyTypeDescriptor(m.Nr);
    }

    public class UnitTest1
    {
        //adapters
        static Func<OptionalResult<MyType>, Result<MyType>> ErrorIfNone = r =>
            r.Match(
                Some: x => x,
                None: () => new Result<MyType>(new Exception("Not found")),
                Fail: e => new Result<MyType>(e)
            );
        
        //Business ops
        static Func<int, OptionalResult<MyType>> Get = i => new MyType(i);
        static Func<OptionalResult<MyType>, int, Result<MyType>> Set = (t, i) => t.Apply(ErrorIfNone).Map(x => new MyType(i));
        static Func<Result<MyType>, Result<MyTypeDescriptor>> Convert = t => t.Map(x => new MyTypeDescriptor(x.Nr));

        //test helper

        [Fact]
        public void A1SetTo2Is2()
        {
            //flip so int is in correct place to curry, then curry the function with value of 2
            Func<int, Func<OptionalResult<MyType>, Result<MyType>>> currySet = curry(flip(Set));
            var set2 = currySet(2);

            var result =
                Get(1)
                .Apply(set2)
                .Apply(Convert)
                .ExtractUnsafe();

            Assert.Equal("2", result.Description);
        }

        [Fact]
        public void NoneSetTo2IsException()
        {
            Func<int, Func<OptionalResult<MyType>, Result<MyType>>> currySet = curry(flip(Set));
            var set2 = currySet(2);
            var result =
                new OptionalResult<MyType>(None)
                .Apply(set2)
                .Apply(Convert);

            var ex = result.ExtractExceptionUnsafe<MyTypeDescriptor, Exception>();

            Assert.True(result.IsFaulted);
            Assert.Equal("Not found", ex.Message);
        }

        [Fact]
        public void ErrorSetTo2IsException()
        {
            Func<int, Func<OptionalResult<MyType>, Result<MyType>>> currySet = curry(flip(Set));

            var set2 = currySet(2);
            var result =
                new OptionalResult<MyType>(new Exception("Something went wrong"))
                .Apply(set2)
                .Apply(Convert);

            var ex = result.ExtractExceptionUnsafe<MyTypeDescriptor, Exception>();

            Assert.True(result.IsFaulted);
            Assert.Equal("Something went wrong", ex.Message);
        }
    }
}
