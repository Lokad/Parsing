# Lokad.Parsing

**A type-safe .NET library for building tokenizers and parsers.** You can use Lokad.Parsing both for quick DSL prototypes and to implement robust, production-grade compilers. At Lokad, we have been using Lokad.Parsing to implement [Envision](https://docs.lokad.com), our in-house programming language, from 2015 to 2019 (we have upgraded to an F# parser since then).

You can see Lokad.Parsing in action in open source project Lokad.BrainScript, which parses the CNTK BrainScript language: [lexer](Lokad.Parsing/samples/Lokad.BrainScript/Token.cs) and [parser](Lokad.Parsing/samples/Lokad.BrainScript/Parser.cs).

The Lokad.Parsing API is designed to be used from C#, and relies heavily on reflection and annotations. A different API for F# is in the works.

NuGet package: [Lokad.Parsing](https://www.nuget.org/packages/Lokad.Parsing)

## Lexer features

Tokens are represented by an `enum`, and the lexer is built from annotations
on enumeration members. A very simple example would be:

```cs
using Lokad.Parsing.Lexer;

[Tokens]
enum Token
{
    [EndOfStream] EoS,
    [Error] Error,
    
    [Pattern("0|[1-9][0-9]*")] Number,
    [Any("+")] Add,
    [Any("-")] Sub,
    [Any("*")] Mul,
    [Any("/")] Div
}
```

The tokenizer runs through the input string, matching against the patterns associated with each token. If more than one token matches, the **longest match**  is kept. If two tokens are tied for the longest match, the one that appears first in the enumeration is returned.

To specify how each token matches the input text, attributes are used:

 - [`Pattern`](Lokad.Parsing/Lexer/PatternAttribute.cs) (and `PatternCi` for case-insensitive matching) recognize tokens with regular expressions, using the standard .NET [`Regex`](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions)  syntax.
 
   ```cs
       [Pattern("[a-zA-Z][a-zA-Z0-9]*")] Identifier,
   ```
   
 - [`Any`](Lokad.Parsing/Lexer/AnyAttribute.cs) (and `AnyCi` for case-insensitive matching) recognize tokens using a finite list of strings:
 
   ```cs
       [Any("if", "then", "else")] Keyword,
   ```
      
 - `Ci` is a special case of `AnyCi` which uses the enumeration member's name as the string to recognize. The two following lines are equivalent:

   ```cs
       [AnyCi("if")] If,
   
       [Ci] If,
   ```   
   
You should also define two mandatory special tokens:
 
 - The end-of-stream _special_ token `[EndOfStream]` is a zero-length token emitted at the very end of the input text. It is always the last token.
 
 - The error token `[Error]` is a single-character token emitted when no other token matches the next character. It is not a special token, merely a "default"
   case.

White-space (`[ \n\t\r]+`) is ignored by the tokenizer before and after tokens, but may still be recognized as part of tokens.

### Advanced feature : child tokens

For performance or clarity reasons, it is a bad idea to allow ambiguous token pairs that can match the same prefix. For example, a naïve "identifier" token likely matches every single keyword in a language. While the tokenizer will resolve this ambiguity deterministically, the resolution will often be the opposite of what the lexer author intended.

Lokad.Parsing solves this by allowing sub-tokens, or "child" tokens. These are not matched against the input text, but against the prefix recognized by their parent (and instead of a prefix match, they expect a complete match).

You can mark a token as the child of another by applying the [`From`](Lokad.Parsing/Lexer/FromAttribute.cs) attribute. For example:

```cs
    [PatternCi("[a-z][a-z0-9]*")] Identifier,
    
    // Keywords
    [From((int)Identifier), Any("if")] If,
    [From((int)Identifier), Any("else")] Else,
    [From((int)Identifier), Any("while")] While,
    [From((int)Identifier), Any("for")] For,
```

The syntax is a bit clunky, because C# does not let you define an attribute with a constructor argument that can be implicitly converted from the type of  `Token.Identifier`, so the example uses a manual cast instead. In practice, the recommended approach is to re-define a custom attribute for your specific token type:

```cs
class FAttribute : FromAttribute
{
    public FAttribute(Token t, bool isPrivate = false) : base(t, isPrivate) {}
}
```

This makes the enumeration code a bit more readable:

```cs
    [PatternCi("[a-z][a-z0-9]*")] Identifier,
    
    // Keywords
    [F(Identifier), Any("if")] If,
    [F(Identifier), Any("else")] Else,
    [F(Identifier), Any("while")] While,
    [F(Identifier), Any("for")] For,
```

Against the input `"if(A)"`, the tokenizer would not attempt to match tokens `If`, `Else`, `While` or `For` directly. It would attempt `Identifier`, which would match the initial prefix `"if"`, and only then it would attempt to match its child tokens, among which `If` would be an exact match. Therefore, the recognized token would be `If` instead of `Identifier`.

The `From` attribute takes an additional parameter `isPrivate` (which is false by default). It has no effect on the tokenization process, but it does change a few things for the parsing step, which will be detailed below.

### Advanced feature : comments

It is possible to define a "comment" regular expression in the `[Tokens]` attribute applied to the `enum`:

```cs
// Comments are C# style '//' until end-of-line or '/*' until '*/'.
[Tokens(Comments = "//[^\n]*|/\*.*?\*/")]
enum Tokens
{
```

Comments are treated as white-space and do not emit any tokens. You can even use the `Comments` regular expression to specify non-comment white-space characters, we just named it `Comments` because that made its purpose easier to understand.

### Advanced feature : significant white-space

Some languages give special meaning to line endings and indentation. The Lokad.Parsing tokenizer provides support for such languages by emitting dedicated end-of-line, indent and dedent _special_ tokens, marked with the following attributes:

```cs
[EndOfLine] EoL,
[Indent] Indent,
[Dedent] Dedent
```

You can choose to specify none of these, only an `EndOfLine` attribute, or all three.

An **end-of-line** is generated at the end of a syntax line, which is not the case for all new-line characters. The exact rules are: 

 1. A `\n` that is matched by a non-special token (such as a multi-line string literal) or a multi-line comment will never emit an end-of-line.
 2. The first `\n` (not eliminated by rule 1) after a non-special token will emit an `EndOfLine` token.
 3. If the input text does not end with a `\n`, the tokenizer will pretend that it does.

The tokenizer keeps an indentation stack. When it recognizes a non-special token after an `EndOfLine`, it measures the distance between that token and the `\n` immediately before it. A `' '` counts as one, a `'\t'` counts as two, and comments (if one were to place one there, for some reason) count as zero. This distance is compared to the indentation stack:

 1. If the top of the indentation stack is longer than the observed distance, pop from the stack, emit a `Dedent` token and repeat this step.
 2. If the top of the indentation stack is shorter than the observed distance, push the observed distance onto the stack and emit an `Indent` token.
 3. If the top of the indentation stack is equal to the observed distance, do nothing.
   
At the end of the script, between the final `EndOfLine` and the `EndOfStream` token, an additional `Dedent` token will be emitted for each element on the stack.

For example, given the following input:

```
if cond:
  print "Hello"
```

The tokens would be:

```
If     Colon
↓      ↓   
if cond:
   ↑    ↑
   Id   EoL
 
 Indent String  Dedent
 ↓      ↓       ↓
  print "Hello"
  ↑            ↑ ↑      
  Id         EoL EoS 
```

The tokenizer also provides two features that let users of the tokenized language temporarily disable significant white-space.

#### Escaping newlines

You can prevent a `\n` from generating an `EndOfLine` token by placing a backslash character `\` before it. This feature can be enabled by setting the appropriate flag in the `Tokens` attribute:

```cs
[Tokens(EscapeNewlines = true)]
enum Token
{
```

Any white-space or comments between the backslash and its corresponding `\n` are ignored. If the backslash is not followed by a `\n`, it will emit an error token instead.

Since the escaped `\n` does not generate an `EndOfLine` token, it also has no effect on the indentation stack and causes no `Indent` or `Dedent` tokens to be emitted.

#### Non-prefix and non-postfix tokens

It is possible to mark some tokens as "never appears at the end of a line" (non-postfix) or "never appears at the beginning of a line" (non-prefix). If a token pair `EndOfLine, Indent` appears immediately after a non-postfix token or immediately before a non-prefix token, then that token pair is removed, along with the top of the indentation stack.

For example, if operator `*` is marked as non-postfix, then the following script:

```
totalDistanceTraveled = durationOfTravel *
  averageTravelSpeed
```

... would be tokenized as `Id` `Equ` `Id` `Mul` `Id` `EndOfLine` `EndOfStream`, without of the `EndOfLine`, `Indent` and `Dedent` caused by the line break after the `*` sign.

To mark tokens as non-postfix or non-prefix, use the [`Infix`](Lokad.Parsing/Lexer/InfixAttribute.cs) attribute:

```cs
    [Any("*"), Infix] Mul,
```

## Parser features

Lokad.Parsing supports SLR grammars, defined as a set of rules that use tokens (from the lexing phase) as their non-terminals.

A parser is a class `P` that extends abstract class [`GrammarParser<TSelf, TTok, TResult>`](Lokad.Parsing/Parser/GrammarParser.cs). The meaning of the type parameters is:

 - `TSelf` is always the parser class `P`, this type parameter is used to extract the rules of the grammar, through reflection, from the parser class.
 - `TTok` is a token `enum` defined as explained in the lexer section above. The library will automatically extract the token definitions, through reflection (there's a trend, here).
 - `TResult` is the type of value returned by the start rule of the parser.
 
Rules are public instance methods of class `P` annotated with the `[Rule]` attribute. The name of the rules is only used for debugging, and has no consequence on the parser itself. Instead, the non-terminals are the **types** themselves. The components (terminals and non-terminals) derived by the rule are the function arguments.

Due to a limitation on generic attributes in C#, we recommend that you first define the following attributes for your token enumeration `Token`. They will make the grammar definition more readable.

```cs
class TAttribute : TerminalAttribute
{ TAttribute(params Token[] read) : base(read.Select(t => (int)t)) {} }

class OAttribute : TerminalAttribute
{ OAttribute(params Token[] read) : base(read.Select(t => (int)t), true) {} }

class LAttribute : ListAttribute
{
    LAttribute(int maxRank = -1) : base(maxRank) { }
    Token Sep { set => Separator = (int)value; }
    Token End { set => Terminator = (int)value; }
}
```
  
### A simple example

A parser library documentation would not be complete without implementing a parser for arithmetic expressions. Admit it, you saw it coming when you first  saw the example tokens above. These were: `Number`, `Add`, `Sub`, `Mul` and `Div`. Let us add `Open` for `'('` and `Close` for `')'` as well.

The EBNF representation of the grammar in this example is:

```
expr ::= term
       | expr Add term 
       | expr Sub term

term ::= atom
       | term Mul atom
       | term Div atom
    
atom ::= Number
       | Open expr Close
```

For each non-terminal in this grammar, we define a type. To keep the example short, instead of building an Abstract Syntax Tree, we will simply evaluate the expression, so the type of each non-terminal represents its value.

```cs
struct Expr { double Value; }
struct Term { double Value; }
struct Atom { double Value; }
```

All of these are value types to avoid any memory allocation. Lokad.Parsing is designed to avoid all memory allocations (beyond keeping one internal `Stack<T>` for each non-terminal type) and will not require any boxing.

The above grammar then translates to:

```cs
using Tk = Token;

class Parser : GrammarParser<Parser, Token, Expr>
{
    [Rule] // expr ::= term
    public Expr OfTerm([NT] Term t) => new Expr { Value = t.Value };
    
    [Rule] // expr ::= expr (Add | Sub) term
    public Expr Op(
        [NT]               Expr left,
        [T(Tk.Add,Tk.Sub)] Tk op,
        [NT]               Term right)
    => 
        new Expr { Value = op == Tk.Add 
            ? left.Value + right.Value 
            : left.Value - right.Value };
            
    [Rule] // term ::= atom
    public Term OfAtom([NT] Atom a) => new Term { Value = a.Value };
    
    [Rule] // term ::= term (Mul | Div) atom
    public Term Op( 
        [NT]               Term left,
        [T(Tk.Mul,Tk.Div)] Tk op,
        [NT]               Atom right)
    =>
        new Term { Value = op == Tk.Mul 
            ? left.Value * right.Value
            : left.Value / right.Value };
            
    [Rule] // atom ::= Number
    public Atom Number([T(Tk.Number)] string num) => 
        new Atom { Value = int.Parse(num) };
        
    [Rule] // atom ::= Open expr Close
    public Atom Parens(
        [T(Tk.Open)]  Tk a,
        [NT]          Expr e,
        [T(Tk.Close)] Tk b)
    =>
        new Atom { Value = e.Value };
}
```

Attribute `T` marks an argument as a terminal, and the value of the terminal will be passed as the argument when the function is called. The argument type can be:

 - `Token`, in which case the enumeration value is passed
 - `string`, in which case the substring matched by the token will be extracted from the original text and passed as argument
 - `Pos<string>`, which wraps the substring in a `Pos<>` object that also carries the exact location of the token in the original text (useful for error reporting).

The constructor of attribute `T` accepts one or more tokens which are to be recognized by that terminal.

Attribute `NT` marks an argument as a non-terminal, and the value returned by the parsing of that non-terminal will be passed as the argument when the function is called. Since non-terminals are types, the type of the argument determines the corresponding non-terminal.

### The token namer

A missing link in the previous example is the protected constructor of abstract base class `GrammarParser`, which expects an `ITokenNamer<Token>`. This interface is used internally by the parser to generate a `ParseException` when a parsing error occurs.

What is a parsing error ? Remember that the parser is an SLR automaton, meaning that it has a current state and moves to another state based on each token it reads. A state may not be able to handle certain tokens at all (for instance, the example grammar does not support `1 + + 2` because a `+` cannot be accepted immediately after another `+`) or may only be able to handle them if the internal stack of the automaton has the right contents (a `)` can be accepted after a number, but only if there is a matching `(` on the stack). When an unacceptable token is encountered, a parsing error occurs.

To make the error message more helpful, the parser collects all tokens that _could_ have been accepted in the current state, and the message will look like this:

> Syntax error, found '+' but expected number, identifier or ')'.

The token namer is responsible for turning the dry enumeration members `Add`, `Number`, `Identifier` and `Close` into the human-readable names `'+'`, `number`, `identifier` and `')'`.

See [`ITokenNamer.cs`](Lokad.Parsing/Error/ITokenNamer.cs) for more information.

### Advanced feature : optional terminals and non-terminals

Attribute `O` marks a terminal as optional, and attribute `NTO` marks a
non-terminal as optional. These help keep rules shorter. Consider the  
syntax for a  [method parameter in C#](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/classes#method-parameters):

```
fixed_parameter 
    : attributes? parameter_modifier? type identifier default_argument?
    ;
``` 

It is trivial to manually define a `attributes_opt` non-terminal that can be either empty or an `attributes`, and so on for the other optional components, but it is fastidious and should be automated instead. Using Lokad.Parsing optional values, the above rule would likely be implemented as:

```cs
[Rule]
FixedParameter FixedParam(
    [NTO]                      Attributes? attrs,
    [O(Tk.Ref,Tk.Out,Tk.This)] Tk? paramModifier,
    [NT]                       Type type,
    [T(Tk.Id)]                 string identifier,
    [NTO]                      DefaultArgument? dflt)
```

If an optional argument is not provided, the value of that argument will be the `default` for that type. For his reason, type `T?` is considered as being the same non-terminal as type `T`.

### Advanced feature : lists of non-terminals

Attribute `L` marks an argument as being a list of non-terminals. The argument must be of type `T[]`, where type `T` determines the repeated non-terminal.

For example, the `fixed_parameter` rule from the previous section could be rewritten to work without an `Attributes` non-terminal, which is just a list of  `Attribute` values:

```cs
[Rule]
FixedParameter FixedParam(
    [L]                        Attribute[] attrs,
    [O(Tk.Ref,Tk.Out,Tk.This)] Tk? paramModifier,
    [NT]                       Type type,
    [T(Tk.Id)]                 string identifier,
    [NTO]                      DefaultArgument? dflt)
```

The `L` attribute contains helpful fields for customizing the list further:

 - `L(Min=2)` indicates that the list must contain at least two elements. Note that there is no way to specify the maximum number of elements, and that setting a high minimum will significantly increase the size of the generated SLR automaton (after all, this is just a shorthand syntax for repeating the non-terminal `Min` times, followed by a `L(Min=0)`, and does not benefit from any special handling by the SLR automaton).
 - `L(Sep=Tk.Comma)` indicates that the list elements are separated by a specific token. If the list contains N elements, there will be N-1 separator tokens.
 - `L(End=Tk.Semicolon)` indicates that each list element is followed by a specific token, called terminator. If the list contains N elements, there will be N terminators.

The list is one of the rare cases of memory allocation in Lokad.Parsing (the other is terminals of type `string` or `Pos<string>`).
   
### Advanced feature : public child tokens

Consider the keyword `async` in C#. It is contextual, meaning that in some contexts it will be treated as a keyword, and in other contexts it will be treated as an identifier ; you _can_ write `bool async = true;` and this exact line probably appears in many projects written before the keyword was introduced. The same is also true for a lot of the LINQ keywords: `from`, `where`, etc.

To the lexer, `Token.Async` is different from the generic `Token.Id` identifier, because there are rules (such as method declarations) that need to treat `async` separately from normal identifiers. But for many rules, "identifier" covers `Token.Id`, `Token.Async`, `Token.From`, and all the others, and it can quickly become fastidious to write `[T(Tk.Id, Tk.Async, Tk.From, ...)]` every single time.

Remember the children token definition (using `From`) discussed in the lexer section above. It was possible to make a child public or private, which had no impact whatsoever on the lexer. The consequences happen in the parser, instead.

 - A **private** child token is considered an entirely independent entity from its parent token (the parser does not have any knowledge of the parent-child relationship).
   
 - A **public** child token will match any terminal that matches its parent.
 
With public child tokens, the parser will take care of the time-consuming addition of tokens to terminals. If `Tk.Async` is a public child of `Tk.Id`, then every `[T(Tk.Id)]` will be rewritten to include `Tk.Async` as well.

### Advanced feature : rule ranks

Dealing with infix operator precedence is a major source of pain in EBNF grammars.

One solution is to treat a sequence of infix operators as a soup of expressions separated by operations, and use a separate algorithm for turning that soup into a tree that respects precedence and associativity. See [this parser](tree/master/Lokad.Parsing/samples/Lokad.BrainScript/Parser.cs#L313)
for an example of this approach.

Another solution can be seen in the tiny example above: we created three non-terminals `expr`, `term` and `atom`, and we likely would have had to create more if there had been additional precedence levels (C# has around 14).

To avoid creating so many non-terminals (which in Lokad.Parsing means creating one type for each non-terminal), it is possible to attach a rank to each rule, and to use that to filter the rules which can be matched by each non-terminal.

If you tune your grammar correctly, ranks should represent complexity in the "turn a soup of expressions into a tree that respects precedence and associativity" sense. Very simple rules, like `atom`, have low ranks, while complex rules, like `expr`, have high ranks. A rule should only reference rules of lower ranks, except:

 - for associativity purposes, rules of equal rank
 - when properly parenthesized, rules of any rank

Let's revisit the simple example with ranks. This time, we define no `struct` at all and instead use `double` as the non-terminal type.

```cs
using Tk = Token;

class Parser : GrammarParser<Parser, Token, double>
{
    [Rule(Rank=2)] // expr ::= (expr|term|atom) (Add | Sub) (term|atom)
    public double Op(
        [NT(2)]            double left,
        [T(Tk.Add,Tk.Sub)] Tk op,
        [NT(1)]            double right)
    => 
        op == Tk.Add ? left + right : left - right;
                
    [Rule(Rank=1)] // term ::= (term|atom) (Mul | Div) atom
    public Term Op( 
        [NT(1)]            double left,
        [T(Tk.Mul,Tk.Div)] Tk op,
        [NT(0)]            double right)
    =>
        op == Tk.Mul ? left * right : left / right;
            
    [Rule] // atom ::= Number
    public Atom Number([T(Tk.Number)] string num) => double.Parse(num);
        
    [Rule] // atom ::= Open (expr|term|atom) Close
    public Atom Parens(
        [T(Tk.Open)]  Tk a,
        [NT]          double e,
        [T(Tk.Close)] Tk b) => e;
}
```

The code became slightly shorter.

Attribute `[Rule(Rank=N)]` indicates that the terminal generated by this rule is of rank `N`. We chose to assign rank 2 to `expr`, rank 1 to `term` and rank 0 to `atom` (by default, `[Rule]` assigns a rank of 0).

Attribute `[NT(N)]` indicates that the terminals matched should have rank `N` or **lower**. So, `[NT(2)]` matches `(expr|term|atom)`, `[NT(1)]` matches `(term|atom)` and `[NT(0)]` matches just `atom`. By default, `[NT]` matches all terminals regardless of rank.
