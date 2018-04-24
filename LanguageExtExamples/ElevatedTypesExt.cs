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
    }

}