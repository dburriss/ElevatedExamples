using LanguageExt;
using System;

namespace FunctionalHelpers
{
    public static class F
    {
        public static IfValue<TInput> If<TInput>(Func<TInput, bool> predicate)
        {
            return new IfValue<TInput>(predicate);
        }
    }

    public class IfValue<TInput>
    {
        private readonly Func<TInput, bool> test;
        public IfValue(Func<TInput, bool> predicate)
        {
            test = predicate;
        }

        public ThenValue<TInput, TResult> IsTrue<TResult>(TInput input)
        {
            if (test(input))
            {
                return new ThenValue<TInput, TResult>(test, input, true);
            }
            else
            {
                return new ThenValue<TInput, TResult>(test, input, false);
            }
        }

        public ThenValue<TInput, TResult> IsFalse<TResult>(TInput input)
        {
            if (!test(input))
            {
                return new ThenValue<TInput, TResult>(test, input, true);
            }
            else
            {
                return new ThenValue<TInput, TResult>(test, input, false);
            }
        }
    }
    

    public class IfValue<TInput,TResult>
    {
        Option<TResult> value;
        readonly TInput input;
        readonly Func<TInput, bool> test;

        public IfValue(TInput input, Option<TResult> value, Func<TInput, bool> predicate)
        {
            this.input = input;
            test = predicate;
        }

        public ThenValue<TInput, TResult> IsTrue(TInput input)
        {
            if (test(input))
            {
                return new ThenValue<TInput, TResult>(test, input, true);
            }
            else
            {
                return new ThenValue<TInput, TResult>(test, input, false);
            }
        }

        public ThenValue<TInput, TResult> IsFalse(TInput input)
        {
            if (!test(input))
            {
                return new ThenValue<TInput, TResult>(test, input, true);
            }
            else
            {
                return new ThenValue<TInput, TResult>(test, input, false);
            }
        }

        public IfValue<TInput, TResult> Then(Func<TResult> doThis)
        {
            return value.Match(
                Some: x => new IfValue<TInput, TResult>(input, x, test),
                None: () => new IfValue<TInput, TResult>(input, Option<TResult>.None, test)
                );
        }

        //todo: bind, apply, match
    }

    public class ThenValue<TInput, TResult>
    {
        private readonly Func<TInput, bool> test;
        private readonly TInput input;
        private readonly bool isSuccess;

        public ThenValue(Func<TInput, bool> test, TInput input, bool isSuccess)
        {
            this.test = test;
            this.input = input;
            this.isSuccess = isSuccess;
        }

        public IfValue<TInput, TResult> Then(Func<TInput, TResult> doThis)
        {
            if (isSuccess)
            {
                var result = Option<TResult>.Some(doThis(input));
                return new IfValue<TInput, TResult>(input, result, test);
            }
            else
            {
                return new IfValue<TInput, TResult>(input, Option<TResult>.None, test);
            }
        }
    }
}
