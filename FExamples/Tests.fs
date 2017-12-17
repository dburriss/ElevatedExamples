module Tests

open Xunit

type MyType = { Nr : int }
module MyType =
    let create nr = { Nr = nr }
    
type MyTypeDescriptor = { Description : string }
module MyTypeDescriptor =
    let create desc = { Description = desc }

//defining function types. Not needed but useful to think about types before implementation.
type GetMyTypeFn = int -> Result<MyType option,string>
type SetFn = Result<MyType option,string> -> int -> Result<MyType,string>
//type ValidatePositiveFn = Result<MyType,string> -> Result<MyType,string>
//type LogFn = Result<MyType,string> -> Result<MyType,string>
type ConvertFn = Result<MyType,string> -> Result<MyTypeDescriptor,string>

//helpers
let toString s = s.ToString()
let flip f a b = f b a

//adapters
let errorIfNone r =
    match r with
    | Ok (Some x) -> Ok x
    | Ok None -> Error "Not found"
    | Error s -> Error s

//impl
let get : GetMyTypeFn = fun i ->
    MyType.create i
    |> Some
    |> Ok

let set : SetFn = fun t i ->
    let t' = errorIfNone t
    t' |> Result.map (fun _ -> MyType.create i)

let convert : ConvertFn = fun t ->
    let f (x:MyType) = x.Nr |> toString |> MyTypeDescriptor.create
    t |> Result.map f

//tests
[<Fact>]
let ``1 set to 2 is 2`` () =
    let setTo2 = (set |> flip) 2 
    let r = get(1) |> setTo2 |> convert
    match r with
    | Ok x -> Assert.Equal("2", x.Description)
    | Error s -> failwith s

[<Fact>]
let ``None set to 2 is Error Not Found`` () =
    let setTo2 = (set |> flip) 2 
    let r = Result<MyType option,string>.Ok None |> setTo2 |> convert
    match r with
    | Ok x -> Assert.False(true, "Was Ok when should be Error")
    | Error s -> Assert.Equal("Not found", s);

[<Fact>]
let ``Error set to 2 is Error`` () =
    let setTo2 = (set |> flip) 2 
    let r = Result<MyType option,string>.Error "Something went wrong" |> setTo2 |> convert
    match r with
    | Ok x -> Assert.False(true, "Was Ok when should be Error")
    | Error s -> Assert.Equal("Something went wrong", s);
    