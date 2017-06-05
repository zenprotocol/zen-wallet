module ContractExamples.MaybeWorkflow
open System

    /// Forked from FSharpX -- topynate
    /// The maybe monad.
    /// This monad is my own and uses an 'T option. Others generally make their own Maybe<'T> type from Option<'T>.
    /// The builder approach is from Matthew Podwysocki's excellent Creating Extended Builders series http://codebetter.com/blogs/matthew.podwysocki/archive/2010/01/18/much-ado-about-monads-creating-extended-builders.aspx.
type MaybeBuilder() =
    member this.Return(x) = Some x

    member this.ReturnFrom(m: 'T option) = m

    member this.Bind(m, f) = Option.bind f m

    member this.Zero() = this.Return ()

    member this.Combine(m, f) = Option.bind f m

    member this.Delay(f: unit -> _) = f

    member this.Run(f) = f()

    member this.TryWith(m, h) =
        try this.ReturnFrom(m())
        with e -> h e

    member this.TryFinally(m, compensation) =
        try this.ReturnFrom(m())
        finally compensation()

    member this.Using(res:#IDisposable, body) =
        let body' = fun () -> body res
        this.TryFinally(body', fun () ->
            match res with
            | null -> ()
            | disp -> disp.Dispose())

    member this.While(guard, f) =
        if not (guard()) then Some () else
        do f() |> ignore
        this.While(guard, f)

    member this.For(sequence:seq<_>, body) =
        this.Using(sequence.GetEnumerator(),
                             fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> body enum.Current)))
let maybe = MaybeBuilder()