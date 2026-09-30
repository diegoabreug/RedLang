# RedLang

A compiler for **RedLang**, a small teaching language, written in C# with
[ANTLR 4](https://www.antlr.org/) for parsing and
[LLVMSharp](https://github.com/dotnet/LLVMSharp) for code generation.

RedLang source files (`.red`) go through the full compiler pipeline:

1. **Lexing & parsing** — ANTLR 4 grammar in `antrl4CS/Definition/`
2. **AST construction** — `AstBuilderVisitor.cs`, node types in `antrl4CS/Node/`
3. **Semantic analysis** — scopes, symbols and type checks in `SemanticAnalyzer.cs` and `antrl4CS/Symbols/`
4. **Code generation** — LLVM IR emitted by `CodeGenerator.cs` (written to `output.ll`) and compiled to a native executable

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Windows x64 (uses the `libLLVM.runtime.win-x64` package)
- Java, only if you want to regenerate the parser from the grammar (`antrl4CS/ANTLR4/antlr-4.13.2-complete.jar`)

## Build & run

```bash
dotnet build antrl4CS
```

Compile a single file:

```bash
dotnet run --project antrl4CS -- path/to/program.red
```

Or pass a directory to compile every `.red` file in it. With no argument, the
current directory is scanned.

## Project layout

```
antrl4CS/
├── Definition/      ANTLR grammar (lexer + parser)
├── Generated/       ANTLR-generated lexer/parser/visitors
├── Node/            AST node classes
├── Symbols/         Symbol table and symbol types
├── Tests/           Sample RedLang programs
├── AstBuilderVisitor.cs
├── SemanticAnalyzer.cs
├── CodeGenerator.cs
└── Program.cs       Entry point
```

## Credits

Originally built as a team project at INTEC by:

- Diego Abreu ([@diegoabreug](https://github.com/diegoabreug))
- Sebastián Tavares ([@SebastianTavares](https://github.com/SebastianTavares))
- Raimond ([@Raimond123](https://github.com/Raimond123))
- Student 1120667

Original repository: [Raimond123/RedLang](https://github.com/Raimond123/RedLang)

Maintained by [@diegoabreug](https://github.com/diegoabreug).
