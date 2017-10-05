module ContractUtilities.ResultWorkflow

open System

    /// Forked from FSharpX -- topynate
    /// The maybe monad.
    /// This monad is my own and uses an 'T option. Others generally make their own Maybe<'T> type from Option<'T>.
    /// The builder approach is from Matthew Podwysocki's excellent Creating Extended Builders series http://codebetter.com/blogs/matthew.podwysocki/archive/2010/01/18/much-ado-about-monads-creating-extended-builders.aspx.


type ResultBuilder() =
    member this.Return(x) = Ok x

    member this.ReturnFrom(m: Result<'Err, 'Value>) = m

    member this.Bind(m, f) = Result.bind f m

    member this.Bind((e, opt): 'Err * 'T option, f) = Result.bind f (match opt with 
                                                                       | Some x -> Ok x 
                                                                       | None -> Error e)

    member this.Zero() = this.Return ()

    member this.Combine(m, f) = Result.bind f m

    member this.Delay(f: unit -> _) = f

    member this.Run(f) = f()

    //member this.TryWith(m, h) =
    //    try this.ReturnFrom(m())
    //    with e -> h e

    //member this.TryFinally(m, compensation) =
    //    try this.ReturnFrom(m())
    //    finally compensation()

    //member this.Using(res:#IDisposable, body) =
    //    let body' = fun () -> body res
    //    this.TryFinally(body', fun () ->
    //        match res with
    //        | null -> ()
    //        | disp -> disp.Dispose())

    //member this.While(guard, f) =
    //    if not (guard()) then Some () else
    //    do f() |> ignore
    //    this.While(guard, f)

    //member this.For(sequence:seq<_>, body) =
        //this.Using(sequence.GetEnumerator(),
                             //fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> body enum.Current)))
let result = ResultBuilder()