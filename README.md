# Elevated Examples

I contain examples in C# and F# of functional programming. I wrote some example code in F# and then tried to use the same patterns to write the C# code. The C# code makes use of [LanguageExt](https://github.com/louthy/language-ext), a functional helper library for C#.

## Background

Each test case has a workflow that goes something like *Get data* -> *Mutate data* -> *Validate* -> *Log* -> *Map to different type*. The idea is that there are different [elevated types](https://fsharpforfunandprofit.com/posts/elevated-world/) that need to be mapped between to allow for composition. This is meant to be very practical with only as much theory as needed to understand the code.

- [F# Code](https://github.com/dburriss/ElevatedExamples/blob/master/FExamples/Tests.fs)
- [C# Code](https://github.com/dburriss/ElevatedExamples/blob/master/LanguageExtExamples/Tests.cs)

## Examples

- [x] Example of an immutable C# type
- [ ] Example of an immutable C# list
- [x] `flip` function parameter order to allow currying
- [x] `curry` (partial application) functions so only a single parameter needed so the can be composed
- [x] Composing a workflow with `Apply` or  pipe `|>`
- [x] Usage of `Tap` (or `tee`) to change a `unit` returning function into a pass-through function
- [ ] mapError throwing an exception and LangExt Try

## Overview

Although not necessary, I defined some function types in F# that formed the definition of the main functions composed in my use cases. These were useful even when defining the C# implementations.

```fsharp
type GetMyTypeFn = int -> Result<MyType option,string>
type SetFn = Result<MyType option,string> -> int -> Result<MyType,string>
type ValidatePositiveFn = Result<MyType,string> -> Result<MyType,string>
type LogFn = Result<MyType,string> -> Result<MyType,string>
type ConvertFn = Result<MyType,string> -> Result<MyTypeDescriptor,string>
```

The final happy-path workflow in F# then looks like this:

```fsharp
let setTo2 = (set |> flip) 2
let r = get(1) |> setTo2 |> validate |> convert |> tapLog
```
and in C#:

```csharp
Func<int, Func<OptionalResult<MyType>, Result<MyType>>> currySet = curry(flip(Set));
var set2 = currySet(2);
var result =
    Get(1)
    .Apply(set2)
    .Apply(Validate)
    .Apply(Convert);
```

As you can see from the definition of `SetFn`, the `int` parameter comes after the elevated `Result` type which would be passed through when composing so to use partial application we need to flip the parameter order.

## A note on Elevated types (Return)

TODO

## Using Map

TODO

## Using Apply

TODO

## Using Bind

TODO

## Adapters, Tee, and Error handling

TODO