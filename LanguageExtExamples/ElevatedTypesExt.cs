using System;

using LanguageExt;

namespace LanguageExtExamples
{
    public static class LanguageExtExtensions
    {
        public static Result<TResult> Bind<TInput, TResult>(this Result<TInput> result,
                                                            Func<TInput, Result<TResult>> func)
        {
            return result.Match(
                Succ: func,
                Fail: ex => new Result<TResult>(ex)
            );
        }

        public static Option<TResult> Bind<TInput, TResult>(this Option<TInput> option,
                                                            Func<TInput, Option<TResult>> func)
        {
            return option.Match(
                Some: func,
                None: () => Option<TResult>.None
            );
        }
    }
}