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
    }

    public class UnitTest1
    {
        static string LogSink = "";
        //helpers
        static T1 Tap<T1>(Action<T1> f, T1 x) { f(x); return x; }

        static Func<MyType, bool> ValidateMyTypeIsPositive = t => t.Nr > 0;
        static Action<MyType> LogMyType = t => LogSink = $"{t.Nr}";
        static Func<Result<MyType>, Func<MyType, Result<MyType>>, Result<MyType>> resultBind = (r, f) => r.Bind(f);

        //adapters
        static Func<OptionalResult<MyType>, Result<MyType>> ErrorIfNone = r =>
            r.Match(
                Some: x => x,
                None: () => new Result<MyType>(new Exception("Not found")),
                Fail: e => new Result<MyType>(e)
            );

        static Func<MyType, MyType> TapLog = t => Tap(LogMyType, t);
        static Func<MyType, Result<MyType>> TapLogR = t => TapLog(t);

        //validateMyTypeIsPositiveR: MyType -> Result<MyType>
        static Func<MyType, Result<MyType>> ValidateMyTypeIsPositiveR = t => 
        {
            if (ValidateMyTypeIsPositive(t)) { return t; }
            else { return new Result<MyType>(new Exception("Number should not be negative")); }
        };

        //Business ops
        static Func<int, OptionalResult<MyType>> Get = i => new MyType(i);
        static Func<OptionalResult<MyType>, int, Result<MyType>> Set = (t, i) => t.Apply(ErrorIfNone).Map(x => x.With(i));
        static Func<Result<MyType>, Result<MyType>> Validate = t => t.Bind(ValidateMyTypeIsPositiveR);
        static Func<Result<MyType>, Result<MyType>> Log = t => resultBind(t, TapLogR);

        static Func<Result<MyType>, Result<MyTypeDescriptor>> Convert = t => t.Map(x => new MyTypeDescriptor(x.Nr));

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

        [Fact]
        public void LoggingJustPassesThrough()
        {
            Func<int, Func<OptionalResult<MyType>, Result<MyType>>> currySet = curry(flip(Set));

            var set2 = currySet(2);
            var result =
                Get(1)
                .Apply(set2)
                .Apply(Log)
                .Apply(Convert);

            Assert.False(result.IsFaulted);
            Assert.Equal("2", LogSink);
        }

        [Fact]
        public void ValidatePositiveIsSuccessful()
        {
            Func<int, Func<OptionalResult<MyType>, Result<MyType>>> currySet = curry(flip(Set));

            var set2 = currySet(2);
            var result =
                Get(1)
                .Apply(set2)
                .Apply(Validate)
                .Apply(Convert);

            Assert.False(result.IsFaulted);
        }

        [Fact]
        public void ValidateNegativeIsFail()
        {
            Func<int, Func<OptionalResult<MyType>, Result<MyType>>> currySet = curry(flip(Set));

            var setMinus1 = currySet(-1);
            var result =
                Get(1)
                .Apply(setMinus1)
                .Apply(Validate)
                .Apply(Convert);

            Assert.True(result.IsFaulted);
            var ex = result.ExtractExceptionUnsafe<MyTypeDescriptor, Exception>();
            Assert.Equal("Number should not be negative", ex.Message);
        }
    }
}
