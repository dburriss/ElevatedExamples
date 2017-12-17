using LanguageExt;
using System;

namespace SharedTesting
{
    public static class ElevatedTypesUnsafeHelpers
    {
        public static T ExtractUnsafe<T>(this Option<T> result)
        {
            T value = default(T);

            var r = result.Match<Option<T>>(
                    Some: x => value = x,
                    None: () => throw new InvalidOperationException("No value available as result is None.")
                );

            return value;
        }

        public static T ExtractUnsafe<T>(this Option<T> result, T defaultValue)
        {
            T value = default(T);

            var r = result.Match<Option<T>>(
                    Some: x => value = x,
                    None: () => value = defaultValue
                );

            return value;
        }

        public static T ExtractUnsafe<T>(this Result<T> result)
        {
            T value = default(T);
            Exception exception = null;

            var r = result.Match<Result<T>>(
                    Succ: x => value = x,
                    Fail: ex =>
                    {
                        exception = ex;
                        return new Result<T>(ex);
                    });

            if (exception != null)
            {
                throw exception;
            }

            return value;
        }

        public static TException ExtractExceptionUnsafe<T, TException>(this Result<T> result) where TException : Exception
        {
            T value = default(T);
            Exception exception = null;

            var r = result.Match<Result<T>>(
                    Succ: x => value = x,
                    Fail: ex =>
                    {
                        exception = ex;
                        return new Result<T>(ex);
                    });

            if (exception == null)
            {
                throw new NullReferenceException($"Expected an exception but instead found null. IsFaulted: {result.IsFaulted}");
            }

            return exception as TException;
        }

        public static T ExtractUnsafe<T>(this OptionalResult<T> result)
        {
            T value = default(T);
            Exception exception = null;

            var r = result.Match<Result<T>>(
                    Some: x => value = x,
                    None: () => throw new InvalidOperationException("No value available as result is None."),
                    Fail: ex =>
                    {
                        exception = ex;
                        return new Result<T>(ex);
                    });

            if (exception != null)
            {
                throw exception;
            }

            return value;
        }

        public static T ExtractUnsafe<T>(this OptionalResult<T> result, T defaultValue)
        {
            T value = default(T);
            Exception exception = null;

            var r = result.Match<Result<T>>(
                    Some: x => value = x,
                    None: () => value = defaultValue,
                    Fail: ex =>
                    {
                        exception = ex;
                        return new Result<T>(ex);
                    });

            if (exception != null)
            {
                throw exception;
            }

            return value;
        }

        public static TException ExtractExceptionUnsafe<T, TException>(this OptionalResult<T> result) where TException : Exception
        {
            T value = default(T);
            Exception exception = null;

            var r = result.Match<Result<T>>(
                    Some: x => value = x,
                    None: () => throw new NullReferenceException($"Expected an exception but instead found null. IsFaulted: {result.IsFaulted}"),
                    Fail: ex =>
                    {
                        exception = ex;
                        return new Result<T>(ex);
                    });

            if (exception == null)
            {
                throw new NullReferenceException($"Expected an exception but instead found null. IsFaulted: {result.IsFaulted}");
            }

            return exception as TException;
        }
    }
}
