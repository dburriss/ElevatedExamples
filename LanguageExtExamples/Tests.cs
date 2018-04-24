using LanguageExt;
using static LanguageExt.Prelude;
using System;
using Xunit;
using SharedTesting;
using System.Collections.Generic;
using static FunctionalHelpers.F;

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


        [Fact]
        public void StatefulForEachWithFoldOnEither()
        {
            Func<IEnumerable<int>, Seq<Result<int>>> onlyEven = ints => {

                var evens = ints.Map(i => { if (i % 2 == 0) { return new Result<int>(i); } else { return new Result<int>(new Exception()); } });
                return Seq(evens);
            };
            Func<Seq<Result<int>>, Seq<Either<Exception, int>>> toEither = s => s.Map(x => x.Match(
                                                                            Succ: a => Right<Exception, int>(a), 
                                                                            Fail: ex => Left<Exception, int>(ex)
                                                                            ));
            var results = onlyEven(new[] { 1, 2, 3, 4 }).Apply(toEither);
            var sumEven = results.FoldT(0, (state, item) => state + item);
            Assert.Equal(6, sumEven);
        }

        [Fact]
        public void StatefulForEachWithFoldOnResult()
        {
            Func<IEnumerable<int>, Seq<Result<int>>> onlyEven = ints => {

                var evens = ints.Map(i => { if (i % 2 == 0) { return new Result<int>(i); } else { return new Result<int>(new Exception()); } });
                return Seq(evens);
            };
            Func<int, Result<int>, int> addIfSuccessful = (s, x) => x.Match(
                                                            Succ: a => s + a,
                                                            Fail: ex => s
                                                            );
            var results = onlyEven(new[] { 1, 2, 3, 4 });
            var sumEven = results.Fold(0, (state, item) => addIfSuccessful(state, item));
            Assert.Equal(6, sumEven);
        }

        [Fact]
        public void StatefulForEachExistsOnOdd()
        {
            Func<IEnumerable<int>, Seq<Result<int>>> onlyEven = ints => {

                var evens = ints.Map(i => { if (i % 2 == 0) { return new Result<int>(i); } else { return new Result<int>(new Exception()); } });
                return Seq(evens);
            };
            Func<int, Result<int>, int> addIfSuccessful = (s, x) => x.Match(
                                                            Succ: a => s + a,
                                                            Fail: ex => s
                                                            );
            Func<Result<int>, bool> carryOn = x => !x.IsFaulted;
            var results = onlyEven(new[] { 1, 2, 3, 4 });
            var sumEven = results.FoldWhile(0, (state, item) => addIfSuccessful(state, item), carryOn);
            Assert.Equal(0, sumEven);
        }



        [Fact]
        public void Match_WhenNotAllScenariosMatched_Throws()
        {
            var value = new MyType(5);

            var r = If<MyType>(x => x.Nr % 2 == 0)
                    .IsTrue<int>(value).Then(x => 0).Then(() => 0)
                    .IsFalse(value).Then(x => 1);
        }


        [Fact]
        public void Try_LikeYouMeanIt_GivesResults()
        {
            Func<int, MyType> make = i => new MyType(i);
            var result = Try(make).Apply(Try(1));
            result.Match(
                   Succ: x => Assert.Equal(1, x.Nr),
                   Fail: ex => throw ex
            );
        }
    }
}
