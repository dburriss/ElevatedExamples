# Elevated Examples

I contain examples in C# and F# of functional programming. I wrote some example code in F# and then tried to use the same patterns to write the C# code. The C# code makes use of [LanguageExt](https://github.com/louthy/language-ext), a functional helper library for C#.

## Background

Each test case has a workflow that goes something like *Get data* -> *Mutate data* -> *Validate* -> *Log* -> *Map to different type*. The idea is that there are different [elevated types](https://fsharpforfunandprofit.com/posts/elevated-world/) that need to be mapped between to allow for chaining. This is meant to be very practical with only as much theory as needed to understand the code.

- [F# Code](https://github.com/dburriss/ElevatedExamples/blob/master/FExamples/Tests.fs)
- [C# Code](https://github.com/dburriss/ElevatedExamples/blob/master/LanguageExtExamples/Tests.cs)

### Why use C# for functional programming(FP)

First of all FP is not an all or nothing choice. [Michael Feathers](http://michaelfeathers.typepad.com/michael_feathers_blog/2012/03/tell-above-and-ask-below-hybridizing-oo-and-functional-design.html) wrote about using a hybrid approach back in 2012 and others have even further back. Some would even argue that Actor model coupled with FP may be closer to what Alan Kay initially envisaged anyway.

For me the main benefit is the minimization of moving parts, and so an increase in predictability of the system. This is done in a few ways. The first one is purity of functions. By minimizing the functions that mutate state, and pushing those to the outside, your system becomes both predictable and testable.

Next an emphasis on [honest argument](http://devonburriss.me/honest-arguments/) and [return types](http://devonburriss.me/honest-return-types/) makes functions very explicit about what they do. OOP values encapsulation but often that encapsulation means information on intent and consequence is lost. This makes it hard to reason and predict the behavior of a system. A return result of `Result<MyType option,string>` tells me this function could throw an error, so it is probably an IO type of function. It also tells me the data is optional so it might return no data. Finally the error case will give me a string describing the error. That is explicit, and I can code accordingly without knowing the internal details. With a C# method that throws exceptions I need to know about the internal details to handle the failure modes. That is actually poor encapsulation.

Related to this but not mentioned explicitly is the idea of immutability. Getting a new instance back rather than mutating state avoids many weird unintended side-effects. Unfortunately this is fairly painful in C# currently.

```csharp
class MyType
{
    public int Nr { get; private set; }
    public MyType(int nr) => Nr = nr;
    public MyType With(int nr) => new MyType(nr);
}
```

## Examples

- [x] Example of an immutable C# type
- [ ] Example of an immutable C# list
- [x] `flip` function parameter order to allow currying
- [x] `curry` (partial application) functions so only a single parameter needed so the can be chained together
- [x] Composing a workflow with `Apply` or  pipe `|>` (Note I don't mean composing as F# `>>` but as a concept)
- [x] Usage of `Tap` (or `tee`) to change a `unit` returning function into a pass-through function
- [ ] mapError throwing an exception and LangExt Try

## Overview

Although not necessary, I defined some function types in F# that formed the definition of the main functions chained together in my use cases. These were useful even when defining the C# implementations.

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

As you can see from the definition of `SetFn`, the `int` parameter comes after the elevated `Result` type which would be passed through when chaining. So to use partial application we need to flip the parameter order.

## A note on Elevated types (Return)

Return functions elevate normal values to the elevated world.

So the first thing to point out is that Elevated Types is not an official thing. Scott introduces the idea on [fsharpforfunandprofit.com](https://fsharpforfunandprofit.com/posts/elevated-world/) to avoid using functional programming terms that can initially be quite overwhelming.

Basically they are types that have some sort of state. The 2 we deal with here are `Option` and `Result`.

- `Option` represents something where data might not be present. Return functions for `Option` are `Some` and `None`.
- `Result` represents a return type that might be data but instead could also be an error. In F# to lift a value you use `Ok` and `Error`. *Note: in the examples I will often shorten `Result<MyType,string>` to `Result<MyType>` to keep things concise.*

Here is an example in F# of a function that takes in `Result<MyType option,string>` and returns `Result<MyType,string>`

```fsharp
//Result<MyType option,string> -> Result<MyType,string>
let errorIfNone r =
    match r with
    | Ok (Some x) -> Ok x
    | Ok None -> Error "Not found"
    | Error s -> Error s
```

So it checks if there is no data and converts that `None` case to an `Error`

A final note on elevated types in general. It is best to stay in the elevated world as much as possible within your application. If you keep dropping back to normal values to work with them you will experience a lot of friction. Instead of getting values out and working with them it is better to use the techniques outlined in this example to use functions to manipulate the wrapped values within the elevated world. This will not always be possible but it is more often than you initially would think. It does require a change in mindset. Instead of get a piece of data and issuing imperative commands that manipulate it you use functions to declare what you would like to happen and then hand those functions off appropriately.

[See here for further reading on return](https://fsharpforfunandprofit.com/posts/elevated-world/#return)

## Using Map

`map` allows you to apply a function to the normal value inside the elevated type. In the example below we define a function `f` that take a `MyType` and transforms it into `MyTypeDescriptor`. 

```fsharp
//Result<MyType> -> Result<MyTypeDescriptor>
let convert : ConvertFn = fun r ->
    //MyType -> MyTypeDescriptor
    let f (x:MyType) = x.Nr |> toString |> MyTypeDescriptor.create
    Result.map f r
```

So the function signature is `MyType` -> `MyTypeDescriptor`. Calling `Result.map` with this function and a data value with type `Result<MyType>` will return a value with type `Result<MyTypeDescriptor>`. Thus we have mapped a value from one type to another while staying in the same elevated world.

> Do you see `Map` is the same as LINQ `Select`?

[See here for further reading on map](https://fsharpforfunandprofit.com/posts/elevated-world/#map)

## Using Apply

`apply` is a little different. Apply is used when you have an elevated value and an elevated function. Applying the function which is in terms of elevated types yields an elevated value out. So in the snippet:

```csharp
...
.Apply(Validate)
.Apply(Convert)
```

`Validate` has the signature `Result<MyType> -> Result<MyType>` and as we saw earlier `Convert` has `Result<MyType> -> Result<MyTypeDescriptor>`.

**`Validate`**
input: `Result<MyType>`
output: `Result<MyType>`

**`Convert`**
input: `Result<MyType>`
output: `Result<MyTypeDescriptor>`

As you can see, the *output* of `Validate` matches the *input* of `Convert`. So `Convert` is a function in elevated `Result` that will take the output value of `Validate` and output the result of that function when applied. All this in the elevated world of `Result`.

[See here for further reading on apply](https://fsharpforfunandprofit.com/posts/elevated-world/#apply)

## Using Bind

`bind` allows us to cross between worlds, moving from normal world to elevated. Where `map` used a function that operated in the normal world like in the `Convert` example of `MyType -> MyTypeDescriptor`, `bind` uses a function that crosses worlds eg. `MyType -> Result<MyType>`.

```fsharp
//MyType -> Result<MyType>
let validateMyTypeIsPositiveR x = if validateMyTypeIsPositive x then Ok x else Error "Number should not be negative"

//Result<MyType> -> Result<MyType>
let validate r = Result.bind validateMyTypeIsPositiveR r
```

So here `validateMyTypeIsPositiveR` is a function that lifts a normal type to an elevated one

> Do you see `Bind` is the same as LINQ `SelectMany`?

[See here for further reading on bind](https://fsharpforfunandprofit.com/posts/elevated-world-2/#bind)

## Curry, Adapters, Tee/Tap, and Error handling

Often the input and output of function calls don't line up and you need to do some extra work to get types to match up.

### Partial Application and Currying

If functions have more than 1 input you can use partial application to apply values to the function and get a new function back with that value baked in. Earlier we had the example of the set function. The set function has the signature `Result<MyType option> -> int -> Result<MyType>`. Partial application works from the first parameter so we first call `flip` on `set`. Note that partial application works automatically with F# if not all parameters are supplied.

```fsharp
//set:Result<MyType option> -> int -> Result<MyType>
let flippedSet = flip set //int -> Result<MyType option> -> Result<MyType>
let set2 = flippedSet 2//Result<MyType option> -> Result<MyType> with the 2 now baked in
```

In the C# example things are a little busier because C# does not support it so we use some functions from the LanguageExt library to help

```csharp
//flip so int is in correct place to curry, then curry the function with value of 2
Func<int, Func<OptionalResult<MyType>, Result<MyType>>> currySet = curry(flip(Set));
var set2 = currySet(2);
```

### Adapters

In the examples above I chose to use `Set`, one of the main workflow parts to map from `Result<MyType option>` to change to `Result<MyType>`. That probably isn't the best but luckily internally it just uses an adapter function. So I can reuse that. A workflow that does not use set could then look something like this:

```fsharp
//adapters
let errorIfNone r =
    match r with
    | Ok (Some x) -> Ok x
    | Ok None -> Error "Not found"
    | Error s -> Error s

//workflow
let r = get(1) |> errorIfNone |> validate |> convert |> tapLog
```

`errorIfNone` is an adapter from the optional result to the non-optional result.

### Tee/Tap

When chaining these workflows having a function that returns `unit` (think of it as a functional `void` except it is a value) isn't very useful. `void` functions are usually something that changes state. You can continue to chain calls together though by defining a function that does what needs to be done and then just returns the input parameter. That function is often called `tee` or `tap`.

```fsharp
let tap f x = //or tee
    f x |> ignore
    x

//normal logging function
let logObj a = printf "%A" a

//pass-through logging
let tapLog a = tap logObj a
```

As you can see we can use `tap` as an adapter that gives us a function that can be chained.

### Exception handling

TODO

## Lists

TODO