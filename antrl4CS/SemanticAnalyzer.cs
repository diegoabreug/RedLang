using System;
using System.Collections.Generic;
using System.Linq;
using antrl4CS.Node;
using antrl4CS.Symbols;

namespace antrl4CS
{
    public class CompilerException : Exception
    {
        public CompilerException(string message) : base(message) { }
    }

    public class SemanticAnalyzer
    {
        // Scopes/symbols
        private readonly SymbolTable _globalScope = new();
        private SymbolTable _currentScope;
        private ClassSymbol? _currentClass;
        private FunctionSymbol? _currentFunction;

        // Entry tracking
        private ClassSymbol? _entryClass;
        private FunctionSymbol? _entryMethod;

        // Numeric ranking
        private static readonly Dictionary<string, int> NumericRank = new()
        {
            ["i"] = 1,
            ["f"] = 2
        };

        public SemanticAnalyzer()
        {
            _currentScope = _globalScope;
            RegisterBuiltIns();
        }

        // -------------------------
        // Public API
        // -------------------------
        public void Analyze(ProgramNode program)
        {
            if (program == null) throw new ArgumentNullException(nameof(program));

            // 1) Register classes in global scope
            foreach (var cls in program.ClassNodes)
            {
                if (_globalScope.Lookup(cls.Name) != null)
                    throw new CompilerException($"Class '{cls.Name}' is already defined.");

                var cs = new ClassSymbol
                {
                    Name = cls.Name,
                    ClassScope = new SymbolTable(_globalScope)
                };
                _globalScope.Add(cs);
            }

            // 2) Register class members (fields and method signatures)
            foreach (var cls in program.ClassNodes)
                RegisterClassMembers(cls);

            // 3) Analyze bodies (functions)
            foreach (var cls in program.ClassNodes)
                AnalyzeClass(cls);

            // Final checks: entry method present
            if (_entryClass == null || _entryMethod == null)
                throw new CompilerException("no entry method found, can't execute program.");

            // Entry class must contain only the entry method
            string entryVarErrMsg = _entryClass.Fields.Count > 0 ? $"{_entryClass.Fields.Count} fields" : "";
            string entryFuncErrMsg = _entryClass.Methods.Count > 1 ? $"{_entryClass.Methods.Count} methods" : "";
            if (!string.IsNullOrEmpty(entryVarErrMsg) || !string.IsNullOrEmpty(entryFuncErrMsg))
            {
                string connector = !string.IsNullOrEmpty(entryVarErrMsg) && !string.IsNullOrEmpty(entryFuncErrMsg) ? " and " : "";
                throw new CompilerException($"the entry class \"{_entryClass.Name}\" contains the entry method but has {entryFuncErrMsg}{connector}{entryVarErrMsg}. The entry class can only contain the entry method.");
            }
        }

        // -------------------------
        // Built-ins
        // -------------------------
        private void RegisterBuiltIns()
        {
            // len(arr) -> i
            var lenFn = new FunctionSymbol
            {
                Name = "len",
                ReturnType = new TypeInfo { BaseName = "i" },
                LocalScope = new SymbolTable(_globalScope)
            };
            lenFn.Parameters.Add(new VariableSymbol { Name = "arr", Type = new TypeInfo { BaseName = "any", IsArray = true } });
            _globalScope.Add(lenFn);

            // convertToInt, convertToFloat, convertToBool
            var cti = new FunctionSymbol
            {
                Name = "convertToInt",
                ReturnType = new TypeInfo { BaseName = "i" },
                LocalScope = new SymbolTable(_globalScope)
            };
            cti.Parameters.Add(new VariableSymbol { Name = "value", Type = new TypeInfo { BaseName = "any" } });
            _globalScope.Add(cti);

            var ctf = new FunctionSymbol
            {
                Name = "convertToFloat",
                ReturnType = new TypeInfo { BaseName = "f" },
                LocalScope = new SymbolTable(_globalScope)
            };
            ctf.Parameters.Add(new VariableSymbol { Name = "value", Type = new TypeInfo { BaseName = "any" } });
            _globalScope.Add(ctf);

            var ctb = new FunctionSymbol
            {
                Name = "convertToBool",
                ReturnType = new TypeInfo { BaseName = "b" },
                LocalScope = new SymbolTable(_globalScope)
            };
            ctb.Parameters.Add(new VariableSymbol { Name = "value", Type = new TypeInfo { BaseName = "any" } });
            _globalScope.Add(ctb);
        }

        // -------------------------
        // Registration phase
        // -------------------------
        private void RegisterClassMembers(ClassNode classNode)
        {
            var clsSym = _globalScope.Lookup(classNode.Name) as ClassSymbol
                         ?? throw new CompilerException($"internal error: class symbol '{classNode.Name}' not found during registration.");

            _currentClass = clsSym;
            _currentScope = clsSym.ClassScope;

            foreach (var member in classNode.Members)
            {
                if (member is VariableDeclNode vdecl)
                {
                    RegisterField(vdecl);
                }
                else if (member is FunctionNode fdecl)
                {
                    RegisterMethod(fdecl);
                }
                else
                {
                    // unknown member -> skip or warn
                    Console.WriteLine($"Warning: unknown class member '{member.GetType().Name}' in class {classNode.Name}");
                }
            }

            // restore
            _currentClass = null;
            _currentScope = _globalScope;
        }

        private void RegisterField(VariableDeclNode vdecl)
        {
            if (_currentClass == null) throw new CompilerException("internal error: RegisterField outside class.");

            if (string.IsNullOrEmpty(vdecl.Name))
                throw new CompilerException("field with empty name.");

            if (vdecl.Type == null)
                vdecl.Type = new TypeInfo { BaseName = "any" };

            var vsym = new VariableSymbol
            {
                Name = vdecl.Name,
                Type = vdecl.Type,
                IsField = true
            };

            if (_currentClass.Fields.ContainsKey(vsym.Name))
                throw new CompilerException($"Field '{vsym.Name}' already defined in class '{_currentClass.Name}'.");

            _currentClass.Fields.Add(vsym.Name, vsym);
            _currentClass.ClassScope.Add(vsym);
        }

        private void RegisterMethod(FunctionNode fdecl)
        {
            if (_currentClass == null) throw new CompilerException("internal error: RegisterMethod outside class.");

            if (_currentClass.Methods.ContainsKey(fdecl.Name))
                throw new CompilerException($"Method '{fdecl.Name}' already defined in class '{_currentClass.Name}'.");

            var fn = new FunctionSymbol
            {
                Name = fdecl.Name,
                ReturnType = fdecl.ReturnType ?? new TypeInfo { BaseName = "i" },
                IsEntry = fdecl.IsEntry,
                LocalScope = new SymbolTable(_currentScope),
                ParentClass = _currentClass
            };

            // parameters
            foreach (var p in fdecl.Parameters)
            {
                var paramSym = new VariableSymbol
                {
                    Name = p.Name,
                    Type = p.Type ?? new TypeInfo { BaseName = "any" }
                };
                fn.Parameters.Add(paramSym);
                fn.LocalScope.Add(paramSym);
            }

            _currentClass.Methods.Add(fn.Name, fn);
            _currentClass.ClassScope.Add(fn);

            if (fn.IsEntry)
            {
                if (_entryMethod != null)
                    throw new CompilerException("multiple entry methods found");
                _entryMethod = fn;
                _entryClass = _currentClass;
            }
        }

        // -------------------------
        // Analysis phase
        // -------------------------
        private void AnalyzeClass(ClassNode classNode)
        {
            var clsSym = _globalScope.Lookup(classNode.Name) as ClassSymbol
                         ?? throw new CompilerException($"internal error: class symbol '{classNode.Name}' not found during analysis.");

            _currentClass = clsSym;
            _currentScope = clsSym.ClassScope;

            // analyze each method body
            foreach (var methodPair in clsSym.Methods)
            {
                var methodSym = methodPair.Value;
                var astMethod = classNode.Members.OfType<FunctionNode>().FirstOrDefault(f => f.Name == methodSym.Name);
                if (astMethod == null)
                {
                    Console.WriteLine($"Warning: AST method node for '{methodSym.Name}' not found.");
                    continue;
                }

                // set current function/scope
                _currentFunction = methodSym;
                _currentScope = methodSym.LocalScope;

                // analyze body statements
                AnalyzeStatements(astMethod.Body);

                // restore
                _currentFunction = null;
                _currentScope = clsSym.ClassScope;
            }

            _currentScope = _globalScope;
            _currentClass = null;
        }

        // -------------------------
        // Statement / expression analysis
        // -------------------------
        private void AnalyzeStatements(IEnumerable<AstNode> statements)
        {
            foreach (var s in statements)
                AnalyzeStatement(s);
        }

        private void AnalyzeStatement(AstNode node)
        {
            switch (node)
            {
                case VariableDeclNode vdecl:
                    // local variable declaration in current scope
                    if (string.IsNullOrEmpty(vdecl.Name)) throw new CompilerException("variable with empty name");

                    var varSym = new VariableSymbol
                    {
                        Name = vdecl.Name,
                        Type = vdecl.Type ?? new TypeInfo { BaseName = "any" }
                    };

                    // duplicate check within scope
                    if (_currentScope.Lookup(varSym.Name) != null)
                        throw new CompilerException($"variable '{varSym.Name}' is already declared in this scope.");

                    _currentScope.Add(varSym);

                    // initializer check
                    if (vdecl.Initializer != null)
                    {
                        var exprType = AnalyzeExpression(vdecl.Initializer);
                        if (!AreTypesCompatible(varSym.Type, exprType))
                            throw new CompilerException($"cannot assign expression of type '{TypeToString(exprType)}' to variable '{varSym.Name}' of type '{TypeToString(varSym.Type)}'.");
                    }
                    break;

                case SetStatementNode set:
                    // target resolution: identifier, array access, or member access
                    AnalyzeSet(set);
                    break;

                case ReturnNode ret:
                    AnalyzeReturn(ret);
                    break;

                case StatementNode st when st.Kind == "call" && st.Payload is CallNode call:
                    // standalone call
                    AnalyzeCall(call);
                    break;

                case StatementNode st when st.Kind == "member" && st.Payload is AstNode memb:
                    // member access as statement (maybe getter or method call lost arguments)
                    AnalyzeExpression(memb);
                    break;

                case CheckNode check:
                    var condType = AnalyzeExpression(check.Condition);
                    if (!IsBooleanType(condType))
                        throw new CompilerException($"check condition must be boolean, got '{TypeToString(condType)}'.");

                    // then
                    AnalyzeStatements(check.ThenBlock);

                    if (check.ElseBlock != null)
                        AnalyzeStatements(check.ElseBlock);
                    break;

                case LoopNode loop:
                    // init already registered by earlier registration (loopInit returns a decl or set)
                    if (loop.Init != null)
                        AnalyzeStatement(loop.Init);

                    var condT = AnalyzeExpression(loop.Condition);
                    if (!IsBooleanType(condT))
                        throw new CompilerException($"loop condition must be boolean, got '{TypeToString(condT)}'.");

                    // action (set)
                    if (loop.Action != null)
                        AnalyzeStatement(loop.Action);

                    AnalyzeStatements(loop.Body);
                    break;

                case RepeatNode repeat:
                    var rCond = AnalyzeExpression(repeat.Condition);
                    if (!IsBooleanType(rCond))
                        throw new CompilerException($"repeat condition must be boolean, got '{TypeToString(rCond)}'.");
                    AnalyzeStatements(repeat.Body);
                    break;

                case CallNode callNode:
                    AnalyzeCall(callNode);
                    break;

                case ArrayAccessNode aA:
                    // expression statement of array access (rare) -> validate
                    AnalyzeExpression(aA);
                    break;

                case MemberAccessNode mA:
                    AnalyzeExpression(mA);
                    break;

                case StatementNode unknown:
                    // Could be other wrapped statement, ignore or warn
                    // Console.WriteLine($"Warning: Unhandled statement kind '{unknown.Kind}'");
                    break;

                default:
                    throw new CompilerException($"Unhandled statement type: {node.GetType().Name}");
            }
        }

        private void AnalyzeSet(SetStatementNode set)
        {
            // Determine target type and source expression type
            var targetNode = set.Target;
            if (targetNode is IdentifierNode id)
            {
                var sym = ResolveVariableOrField(id.Name);
                if (sym == null)
                    throw new CompilerException($"'{id.Name}' is not declared.");

                var sourceType = AnalyzeExpression(set.Expression);
                if (!AreTypesCompatible(sym.Type, sourceType))
                    throw new CompilerException($"cannot assign '{TypeToString(sourceType)}' to '{TypeToString(sym.Type)}' (variable {id.Name}).");
                return;
            }
            else if (targetNode is ArrayAccessNode aa)
            {
                var arrayType = AnalyzeExpression(aa.Target);
                if (!arrayType.IsArray)
                    throw new CompilerException($"target is not an array: {TypeToString(arrayType)}");

                var indexType = AnalyzeExpression(aa.Index);
                if (!IsIntegerType(indexType))
                    throw new CompilerException($"array index must be integer, got '{TypeToString(indexType)}'.");

                var sourceType = AnalyzeExpression(set.Expression);
                var elemType = new TypeInfo { BaseName = arrayType.BaseName, IsArray = false, IsNullable = arrayType.IsNullable };
                if (!AreTypesCompatible(elemType, sourceType))
                    throw new CompilerException($"cannot assign '{TypeToString(sourceType)}' to array element of type '{TypeToString(elemType)}'.");

                return;
            }
            else if (targetNode is MemberAccessNode ma)
            {
                // Resolve target object type
                var targetType = AnalyzeExpression(ma.Target);
                if (targetType.BaseName == "any")
                {
                    // allow dynamic
                }
                else
                {
                    // find class symbol
                    var clsSym = _globalScope.Lookup(targetType.BaseName) as ClassSymbol;
                    if (clsSym == null)
                        throw new CompilerException($"'{targetType.BaseName}' is not a class.");

                    if (!clsSym.Fields.TryGetValue(ma.MemberName, out var fieldSym))
                        throw new CompilerException($"class '{clsSym.Name}' does not contain a field '{ma.MemberName}'.");

                    var sourceType = AnalyzeExpression(set.Expression);
                    if (!AreTypesCompatible(fieldSym.Type, sourceType))
                        throw new CompilerException($"cannot assign '{TypeToString(sourceType)}' to field '{ma.MemberName}' of type '{TypeToString(fieldSym.Type)}'.");
                }

                return;
            }

            throw new CompilerException($"unsupported set target of type {targetNode.GetType().Name}");
        }

        private void AnalyzeReturn(ReturnNode ret)
        {
            if (_currentFunction == null)
                throw new CompilerException("return used outside of function");

            var returnType = _currentFunction.ReturnType;
            if (ret.Expression == null)
            {
                // if function return type is not nullable/void then warn/error
                if (returnType.BaseName != "any" && !returnType.IsNullable)
                    Console.WriteLine($"Warning: function '{_currentFunction.Name}' may return nothing while declared return type is '{TypeToString(returnType)}'.");
                return;
            }

            var exprType = AnalyzeExpression(ret.Expression);
            if (!AreTypesCompatible(returnType, exprType))
                throw new CompilerException($"return type mismatch in function '{_currentFunction.Name}': cannot return '{TypeToString(exprType)}' as '{TypeToString(returnType)}'.");
        }

        // Analyze a call node (built-ins and user-defined functions)
        private TypeInfo AnalyzeCall(CallNode call)
        {
            // built-in handling by name
            if (call.IsBuiltIn)
            {
                var name = call.FunctionName;
                switch (name)
                {
                    case "len":
                        if (call.Arguments.Count != 1) throw new CompilerException("len() expects 1 argument.");
                        var argType = AnalyzeExpression(call.Arguments[0]);
                        if (!argType.IsArray) throw new CompilerException("len() expects an array argument.");
                        return new TypeInfo { BaseName = "i" };

                    case "convertToInt":
                        if (call.Arguments.Count != 1) throw new CompilerException("convertToInt() expects 1 argument.");
                        return new TypeInfo { BaseName = "i" };

                    case "convertToFloat":
                        if (call.Arguments.Count != 1) throw new CompilerException("convertToFloat() expects 1 argument.");
                        return new TypeInfo { BaseName = "f" };

                    case "convertToBool":
                        if (call.Arguments.Count != 1) throw new CompilerException("convertToBool() expects 1 argument.");
                        return new TypeInfo { BaseName = "b" };

                    default:
                        // unknown built-in (treat as any)
                        return new TypeInfo { BaseName = "any" };
                }
            }

            // --- NEW: treat ClassName() as constructor call returning instance of class ---
            var classSym = _globalScope.Lookup(call.FunctionName) as ClassSymbol;
            if (classSym != null)
            {
                // simple constructor support: require zero arguments for now
                if (call.Arguments.Count != 0)
                    throw new CompilerException($"constructor '{call.FunctionName}' expects 0 arguments but got {call.Arguments.Count}.");
                return new TypeInfo { BaseName = classSym.Name };
            }

            // user-defined function: try lookup in scope (local -> class -> global)
            var sym = ResolveFunction(call.FunctionName);
            if (sym == null)
                throw new CompilerException($"function '{call.FunctionName}' not found.");

            // check arity
            if (sym.Parameters.Count != call.Arguments.Count)
                throw new CompilerException($"function '{call.FunctionName}' expects {sym.Parameters.Count} arguments but got {call.Arguments.Count}.");

            // check param types
            for (int i = 0; i < call.Arguments.Count; i++)
            {
                var argType = AnalyzeExpression(call.Arguments[i]);
                var paramType = sym.Parameters[i].Type;
                if (!AreTypesCompatible(paramType, argType))
                    throw new CompilerException($"argument {i + 1} of function '{call.FunctionName}' expects '{TypeToString(paramType)}' but got '{TypeToString(argType)}'.");
            }

            return sym.ReturnType;
        }

        // Expression analysis returns a TypeInfo describing expression type
        private TypeInfo AnalyzeExpression(AstNode expr)
        {
            switch (expr)
            {
                case IdentifierNode id:
                    {
                        var vsym = ResolveVariableOrField(id.Name);
                        if (vsym != null) return vsym.Type;
                        // maybe a class name (type)
                        var cs = _globalScope.Lookup(id.Name) as ClassSymbol;
                        if (cs != null) return new TypeInfo { BaseName = cs.Name };
                        throw new CompilerException($"identifier '{id.Name}' not found.");
                    }

                case LiteralNode lit:
                    {
                        switch (lit.Kind)
                        {
                            case "BOOL": return new TypeInfo { BaseName = "b" };
                            case "FLOAT": return new TypeInfo { BaseName = "f" };
                            case "INT": return new TypeInfo { BaseName = "i" };
                            case "STRING": return new TypeInfo { BaseName = "s" };
                            case "NULL": return new TypeInfo { BaseName = "null", IsNullable = true };
                        }
                        throw new CompilerException($"unknown literal kind '{lit.Kind}'.");
                    }

                case ArrayLiteralNode arrLit:
                    {
                        // infer element type: require at least one element to infer, otherwise unknown[]
                        if (arrLit.Elements.Count == 0)
                            return new TypeInfo { BaseName = "unknown", IsArray = true };

                        TypeInfo? common = null;
                        foreach (var e in arrLit.Elements)
                        {
                            var t = AnalyzeExpression(e);
                            if (common == null)
                                common = t;
                            else if (!AreTypesCompatible(common, t) && !AreTypesCompatible(t, common))
                                throw new CompilerException($"array literal contains incompatible element types: '{TypeToString(common)}' and '{TypeToString(t)}'.");
                        }

                        var baseName = common?.BaseName ?? "any";
                        var isNullable = common?.IsNullable ?? false;
                        return new TypeInfo { BaseName = baseName, IsArray = true, IsNullable = isNullable };
                    }

                case ArrayAccessNode aa:
                    {
                        var targetType = AnalyzeExpression(aa.Target);
                        if (!targetType.IsArray)
                            throw new CompilerException($"type '{TypeToString(targetType)}' is not an array.");

                        var indexType = AnalyzeExpression(aa.Index);
                        if (!IsIntegerType(indexType))
                            throw new CompilerException($"array index must be 'i', got '{TypeToString(indexType)}'.");

                        return new TypeInfo { BaseName = targetType.BaseName, IsNullable = targetType.IsNullable };
                    }

                case MemberAccessNode ma:
                    {
                        var targetType = AnalyzeExpression(ma.Target);
                        // if target is a class instance, lookup member
                        var clsSym = _globalScope.Lookup(targetType.BaseName) as ClassSymbol;
                        if (clsSym == null)
                        {
                            // allow dynamic or any
                            if (targetType.BaseName == "any") return new TypeInfo { BaseName = "any" };
                            throw new CompilerException($"type '{targetType.BaseName}' is not a class and has no member '{ma.MemberName}'.");
                        }

                        // field?
                        if (clsSym.Fields.TryGetValue(ma.MemberName, out var fieldSym))
                            return fieldSym.Type;

                        // method?
                        if (clsSym.Methods.TryGetValue(ma.MemberName, out var methodSym))
                            return methodSym.ReturnType;

                        throw new CompilerException($"class '{clsSym.Name}' does not contain member '{ma.MemberName}'.");
                    }

                case CallNode call:
                    return AnalyzeCall(call);

                case BinaryExpressionNode b:
                    {
                        var leftT = AnalyzeExpression(b.Left);
                        var rightT = AnalyzeExpression(b.Right);
                        switch (b.Op)
                        {
                            case "and":
                            case "or":
                                if (!IsBooleanType(leftT) || !IsBooleanType(rightT))
                                    throw new CompilerException($"logical operator '{b.Op}' requires boolean operands.");
                                return new TypeInfo { BaseName = "b" };

                            case "==":
                            case "!=":
                            case "<":
                            case ">":
                            case "<=":
                            case ">=":
                                // relational operators -> boolean, but check compatibility
                                if (!AreTypesCompatible(leftT, rightT) && !AreTypesCompatible(rightT, leftT))
                                    throw new CompilerException($"cannot compare incompatible types '{TypeToString(leftT)}' and '{TypeToString(rightT)}' with '{b.Op}'.");
                                return new TypeInfo { BaseName = "b" };

                            case "+":
                            case "-":
                            case "*":
                            case "/":
                            case "%":
                                // arithmetic -> numeric types
                                if (!IsNumericType(leftT.BaseName) || !IsNumericType(rightT.BaseName))
                                    throw new CompilerException($"arithmetic operator '{b.Op}' requires numeric operands.");
                                // result ranking
                                var leftRank = NumericRank.ContainsKey(leftT.BaseName) ? NumericRank[leftT.BaseName] : 0;
                                var rightRank = NumericRank.ContainsKey(rightT.BaseName) ? NumericRank[rightT.BaseName] : 0;
                                var result = leftRank >= rightRank ? leftT.BaseName : rightT.BaseName;
                                return new TypeInfo { BaseName = result };
                            default:
                                throw new CompilerException($"unknown binary operator '{b.Op}'.");
                        }
                    }

                case UnaryExpressionNode u:
                    {
                        var inner = AnalyzeExpression(u.Operand);
                        switch (u.Op)
                        {
                            case "-":
                                if (!IsNumericType(inner.BaseName))
                                    throw new CompilerException($"unary '-' requires numeric operand, got '{TypeToString(inner)}'.");
                                return inner;
                            case "not":
                                if (!IsBooleanType(inner))
                                    throw new CompilerException($"'not' requires boolean operand, got '{TypeToString(inner)}'.");
                                return new TypeInfo { BaseName = "b" };
                            default:
                                throw new CompilerException($"unknown unary operator '{u.Op}'.");
                        }
                    }

                default:
                    throw new CompilerException($"Unhandled expression node type {expr.GetType().Name}");
            }
        }

        // -------------------------
        // Resolution helpers
        // -------------------------
        private VariableSymbol? ResolveVariableOrField(string name)
        {
            // search current scope chain for variable symbol
            var s = _currentScope.Lookup(name);
            if (s is VariableSymbol vs) return vs;

            // if not found, maybe it's a class field on 'this' - check current class
            if (_currentClass != null && _currentClass.Fields.TryGetValue(name, out var f))
                return f;

            return null;
        }

        private FunctionSymbol? ResolveFunction(string name)
        {
            // check current scope
            var s = _currentScope.Lookup(name);
            if (s is FunctionSymbol fs) return fs;

            // check class methods if inside class
            if (_currentClass != null && _currentClass.Methods.TryGetValue(name, out var meth))
                return meth;

            // check global scope
            var g = _globalScope.Lookup(name);
            if (g is FunctionSymbol gfs) return gfs;

            return null;
        }

        // -------------------------
        // Type helpers
        // -------------------------
        private static bool IsNumericType(string name) => name == "i" || name == "f";
        private static bool IsIntegerType(TypeInfo t) => t.BaseName == "i";
        private static bool IsBooleanType(TypeInfo t) => t.BaseName == "b";

        private static bool AreTypesCompatible(TypeInfo target, TypeInfo source)
        {
            // null literal -> only allowed if target nullable
            if (source.BaseName == "null") return target.IsNullable;

            // arrays
            if (target.IsArray != source.IsArray) return false;

            // same base
            if (target.BaseName == source.BaseName) return true;

            // numeric widening
            if (IsNumericType(target.BaseName) && IsNumericType(source.BaseName))
            {
                if (NumericRank.TryGetValue(source.BaseName, out var sRank) && NumericRank.TryGetValue(target.BaseName, out var tRank))
                    return sRank <= tRank;
            }

            // any accepts everything
            if (target.BaseName == "any") return true;

            return false;
        }

        private static string TypeToString(TypeInfo info)
        {
            if (info == null) return "null";
            var s = info.BaseName ?? "any";
            if (info.IsArray) s += "[]";
            if (info.IsNullable) s += "?";
            return s;
        }

        public void AnalyzeProject(List<ProgramNode> units)
        {
            if (units == null || units.Count == 0)
                throw new CompilerException("no compilation units to analyze.");

            // 1) 建立 “类名 -> 单元索引” 的提供者映射（一个类属于哪个文件/单元）
            var classProvider = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < units.Count; i++)
            {
                foreach (var cls in units[i].ClassNodes)
                {
                    if (classProvider.ContainsKey(cls.Name))
                        throw new CompilerException($"Class '{cls.Name}' is already defined in another file/unit.");
                    classProvider[cls.Name] = i;
                }
            }

            // 2) 建依赖图：unit i 依赖哪些 unit（通过 use 的名字解析到类提供者）
            var graph = new Dictionary<int, List<int>>();
            for (int i = 0; i < units.Count; i++)
                graph[i] = new List<int>();

            for (int i = 0; i < units.Count; i++)
            {
                foreach (var u in units[i].UseNodes)
                {
                    var depName = u.ClassName; // 你 PrintSummary 用的是 u.ClassName，所以这里也用它
                    if (string.IsNullOrWhiteSpace(depName)) continue;

                    if (!classProvider.TryGetValue(depName, out var depUnit))
                        throw new CompilerException($"use '{depName}' not found (no such class/module).");

                    graph[i].Add(depUnit);
                }
            }

            // 3) DFS 检测环（按 unit 维度）
            var visited = new bool[units.Count];
            var inStack = new bool[units.Count];
            var parent = new int[units.Count];
            Array.Fill(parent, -1);

            for (int i = 0; i < units.Count; i++)
            {
                if (!visited[i])
                    DfsDetectCycle(i);
            }

            void DfsDetectCycle(int v)
            {
                visited[v] = true;
                inStack[v] = true;

                foreach (var to in graph[v])
                {
                    if (!visited[to])
                    {
                        parent[to] = v;
                        DfsDetectCycle(to);
                    }
                    else if (inStack[to])
                    {
                        // 找到环：还原路径
                        var cycle = new List<int> { to };
                        int cur = v;
                        while (cur != -1 && cur != to)
                        {
                            cycle.Add(cur);
                            cur = parent[cur];
                        }
                        cycle.Add(to);
                        cycle.Reverse();

                        // 把 unit 索引转换成“类名”更好读：取该 unit 的第一个 class 名称作为标签
                        string Label(int idx) =>
                            units[idx].ClassNodes.FirstOrDefault()?.Name ?? $"unit#{idx}";

                        var cycleText = string.Join(" -> ", cycle.Select(Label));
                        throw new CompilerException($"circular dependency detected: {cycleText}");
                    }
                }

                inStack[v] = false;
            }

            // 4) 无环后：合并成 masterNode，再用你原来的 Analyze 走完整语义检查
            var master = new ProgramNode();
            foreach (var n in units)
            {
                master.UseNodes.AddRange(n.UseNodes);
                master.ClassNodes.AddRange(n.ClassNodes);
            }

            Analyze(master); // 调用你现有的 Analyze（注册类/成员/分析函数体/entry检查等）
        }

    }
}