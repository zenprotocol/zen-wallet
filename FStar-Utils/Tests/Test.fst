module Test
//TODO: Tests with implicits

///////////////////
// Simple functions
///////////////////

// function
let id x = x

// function
let f x y = x + y

//function
let eq (#a:eqtype) (x:a) y = x = y

// namespace
let ns = FStar.Mul.op_Star

// operator
let (@@@) = (+)


///////////////////////
// Function application
///////////////////////

// function application
let f_at x = f x

// application at multiple arguments
let mult_app x y = ns x y

///////////////
// Conditionals
///////////////

// simple conditional
let cond_simple x y = if true then x else y

// conditional with app in if branch
let cond_app_if x y = if eq true false then x else y

// conditional with apps in each branch
let cond_apps x y z = if eq x y then f x z else f y z

/////////////
// Projectors
/////////////

// simple projector
let proj_simple ( x: option 'a {Some? x} ) = Some?.v x

// projector with app
let proj_app ( x: option 'a {Some? x} ) = Some?.v (id x)

///////////////
// Constructors
///////////////

// simple constructor
let cons_simple x = Some x

// constructor with app
let cons_app x = Some (id x)

// constructor with several args
let cons_args x y = Cons x y

// constructor with several args, each with apps
let cons_args_apps x y = Cons (id x) (id y)

//////////
// Matches
//////////

// simple match
let match_simple x y = match x with | true -> x | false -> y

// match with destructuring
let match_destruct x = match x with | Some x -> true | None -> false

// match with constructor
let match_cons x = match Some x with | Some x -> false

// match with hole
let match_hole x = match x with | _ -> true

// match with apps in 2 branches
let match_apps2 x = match x with | Some _ -> id (id x) | None -> id (id x)

// match with apps in 3 branches
let match_apps3 x = match x with | 1 -> id (id x) | 2 -> id (id (id x)) | _ -> id (id (id (id x)))

// match with combined cases
let match_comb x = match x with | Some _ | None -> x

//matching with a function
let match_function = function
    | 0 -> true
    | _ -> false

////////
// Types
////////

type vector (a:Type) : nat -> Type =
  | VNil : vector a 0
  | VCons: hd:a
        -> #l:nat
        -> tl:vector a l
        -> vector a (l+1)

///////////
// Literals
///////////

let l1 = []

let ls = [1;2;3]

//////////
// Vectors
//////////

let v1 = VCons 1 (VCons 2 (VCons 3 VNil))

////////////
// Do-Syntax
////////////

let bind x f = f x

let bind_simple x f =
    do x <-- f x;
    f x

let bind_match x =
    do x <-- begin match x with
             | 0 -> true
             | _ -> false
             end;
    not x

///////////
// Let Open
///////////

let u64_add x y = let open FStar.UInt64 in
    x +%^ y


///////////
// Holes
///////////

let hole_test = Ctr _ true



////////////////
// Record Access
////////////////

let record_access = r.field

let record_access2 = r.field1.field2


