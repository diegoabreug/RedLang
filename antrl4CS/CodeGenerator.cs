using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp.Interop;
using antrl4CS.Node;

namespace antrl4CS
{
    public class CodeGenerator : IDisposable
    {
        private bool _disposed;
        private readonly LLVMModuleRef _module;
        private readonly LLVMBuilderRef _builder;
        private readonly LLVMContextRef _context;

        // Tablas de Símbolos
        private readonly Dictionary<string, LLVMTypeRef> _classStructs = new();
        private readonly Dictionary<string, LLVMValueRef> _namedValues = new();
        private readonly Dictionary<string, LLVMTypeRef> _variableTypes = new();
        private readonly Dictionary<string, LLVMTypeRef> _functionTypes = new();

        // Funciones Runtime
        private LLVMValueRef _printfFunc;
        private LLVMValueRef _scanfFunc;
        private LLVMValueRef _mallocFunc;

        public CodeGenerator(string moduleName = "RedLangModule")
        {
            _context = LLVMContextRef.Create();
            _module = _context.CreateModuleWithName(moduleName);
            _builder = _context.CreateBuilder();

            DeclareRuntimeFunctions();
        }

        private void DeclareRuntimeFunctions()
        {
            var charPtr = LLVMTypeRef.CreatePointer(_context.Int8Type, 0);

            // printf
            var printfType = LLVMTypeRef.CreateFunction(_context.Int32Type, new[] { charPtr }, true);
            _printfFunc = _module.AddFunction("printf", printfType);
            _functionTypes["printf"] = printfType;

            // scanf
            var scanfType = LLVMTypeRef.CreateFunction(_context.Int32Type, new[] { charPtr }, true);
            _scanfFunc = _module.AddFunction("scanf", scanfType);
            _functionTypes["scanf"] = scanfType;

            // malloc
            var mallocType = LLVMTypeRef.CreateFunction(charPtr, new[] { _context.Int64Type });
            _mallocFunc = _module.AddFunction("malloc", mallocType);
            _functionTypes["malloc"] = mallocType;
        }

        public string Generate(ProgramNode program)
        {
            // 1. Structs
            foreach (var cls in program.ClassNodes)
                _classStructs[cls.Name] = _context.CreateNamedStruct(cls.Name);

            // 2. Cuerpos de Structs
            foreach (var cls in program.ClassNodes) DefineClassBody(cls);

            // 3. Prototipos (Dos pasadas: Declarar antes de implementar)
            foreach (var cls in program.ClassNodes)
            {
                foreach (var method in cls.Members.OfType<FunctionNode>())
                    RegisterFunctionPrototype(method);
            }

            // 4. Implementación de Funciones
            foreach (var cls in program.ClassNodes)
            {
                foreach (var method in cls.Members.OfType<FunctionNode>())
                    GenerateMethodBody(method);
            }

            _module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
            return _module.ToString();
        }

        private void DefineClassBody(ClassNode classNode)
        {
            if (!_classStructs.TryGetValue(classNode.Name, out var structType)) return;
            var fields = classNode.Members.OfType<VariableDeclNode>().Select(f => MapType(f.Type!)).ToArray();
            structType.StructSetBody(fields, false);
        }

        private void RegisterFunctionPrototype(FunctionNode methodNode)
        {
            var funcName = methodNode.Name;

            // --- FIX ENTRY POINT ---
            if (funcName == "Main") funcName = "main";
            // -----------------------

            var returnType = MapType(methodNode.ReturnType ?? new TypeInfo { BaseName = "void" });
            var paramTypes = methodNode.Parameters.Select(p => MapType(p.Type!)).ToArray();

            var functionType = LLVMTypeRef.CreateFunction(returnType, paramTypes);
            var function = _module.AddFunction(funcName, functionType);

            _functionTypes[funcName] = functionType;
        }

        private void GenerateMethodBody(FunctionNode methodNode)
        {
            var funcName = methodNode.Name;
            // --- FIX ENTRY POINT ---
            if (funcName == "Main") funcName = "main";
            // -----------------------

            var function = _module.GetNamedFunction(funcName);

            var entryBlock = _context.AppendBasicBlock(function, "entry");
            _builder.PositionAtEnd(entryBlock);

            _namedValues.Clear();
            _variableTypes.Clear();

            // Parámetros
            for (int i = 0; i < methodNode.Parameters.Count; i++)
            {
                var pNode = methodNode.Parameters[i];
                var pVal = function.GetParam((uint)i);
                pVal.Name = pNode.Name;

                var alloca = _builder.BuildAlloca(MapType(pNode.Type!), pNode.Name);
                _builder.BuildStore(pVal, alloca);

                _namedValues[pNode.Name] = alloca;
                _variableTypes[pNode.Name] = MapType(pNode.Type!);
            }

            // Cuerpo
            if (methodNode.Body != null)
            {
                foreach (var stmt in methodNode.Body)
                {
                    GenerateStatement(stmt);
                    if (stmt is ReturnNode) break;
                }
            }

            // Return seguro
            var returnType = function.TypeOf.ElementType.ReturnType;
            if (_builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
            {
                if (returnType.Kind == LLVMTypeKind.LLVMVoidTypeKind) _builder.BuildRetVoid();
                else _builder.BuildRet(LLVMValueRef.CreateConstInt(_context.Int64Type, 0, false));
            }
        }

        private void GenerateStatement(AstNode stmt)
        {
            switch (stmt)
            {
                // --- FIX PARSER (Desempaquetado) ---
                case StatementNode sNode:
                    if (sNode.Payload != null) GenerateStatement(sNode.Payload);
                    break;

                case VariableDeclNode v: GenerateVariableDecl(v); break;
                case SetStatementNode s: GenerateSet(s); break;
                case CallNode c: GenerateCallExpression(c); break;
                case ReturnNode r: GenerateReturn(r); break;
                case CheckNode check: GenerateCheck(check); break;
                case RepeatNode rep: GenerateRepeat(rep); break;
                case LoopNode loop: GenerateLoop(loop); break;

                default:
                    Console.WriteLine($"[WARNING] Nodo no manejado: {stmt.GetType().Name}");
                    break;
            }
        }

        private LLVMValueRef GenerateCallExpression(CallNode c)
        {
            string cleanName = c.FunctionName;
            if (cleanName.Contains(".")) cleanName = cleanName.Split('.').Last();

            // 1. ASK / SCANF (Con FIX de Memoria para Strings)
            if (cleanName.Equals("ask", StringComparison.OrdinalIgnoreCase))
            {
                if (c.Arguments.Count > 0 && c.Arguments[0] is IdentifierNode idArg && _namedValues.TryGetValue(idArg.Name, out var alloca))
                {
                    var type = _variableTypes[idArg.Name];

                    // --- NUEVA LÓGICA ANTI-CRASH PARA STRINGS ---
                    if (type.Kind == LLVMTypeKind.LLVMPointerTypeKind)
                    {
                        // 1. Reservar memoria (Buffer de 256 bytes)
                        var size = LLVMValueRef.CreateConstInt(_context.Int64Type, 256, false);
                        var buffer = _builder.BuildCall2(_functionTypes["malloc"], _mallocFunc, new[] { size }, "input_buffer");

                        // 2. Guardar la dirección del buffer en la variable
                        _builder.BuildStore(buffer, alloca);

                        // 3. Leer en el buffer
                        var fmtStr = _builder.BuildGlobalStringPtr("%s", "scanfmt");
                        return _builder.BuildCall2(_functionTypes["scanf"], _scanfFunc, new[] { fmtStr, buffer }, "call_scanf");
                    }
                    // --------------------------------------------

                    // Lógica normal para números
                    string fmt = "%lld";
                    if (type.Kind == LLVMTypeKind.LLVMDoubleTypeKind) fmt = "%lf";

                    var fmtPtr = _builder.BuildGlobalStringPtr(fmt, "scanfmt");
                    return _builder.BuildCall2(_functionTypes["scanf"], _scanfFunc, new[] { fmtPtr, alloca }, "call_scanf");
                }
                return LLVMValueRef.CreateConstInt(_context.Int32Type, 0, false);
            }

            // 2. SHOW / PRINTF
            if (cleanName.Equals("show", StringComparison.OrdinalIgnoreCase))
            {
                var val = GenerateExpression(c.Arguments[0]);
                string fmt = "%lld\n";
                if (val.TypeOf.Kind == LLVMTypeKind.LLVMDoubleTypeKind) fmt = "%f\n";
                else if (val.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind) fmt = "%s\n";
                else if (val.TypeOf.IntWidth == 1) fmt = "%d\n";

                var fmtPtr = _builder.BuildGlobalStringPtr(fmt, "printfmt");
                if (_functionTypes.TryGetValue("printf", out var printfType))
                {
                    return _builder.BuildCall2(printfType, _printfFunc, new[] { fmtPtr, val }, "call_show");
                }
            }

            // 3. Constructor
            if (_classStructs.ContainsKey(cleanName))
            {
                var size = LLVMValueRef.CreateConstInt(_context.Int64Type, 8, false);
                return _builder.BuildCall2(_functionTypes["malloc"], _mallocFunc, new[] { size }, "new_obj");
            }

            // 4. Funciones de Usuario
            var func = _module.GetNamedFunction(cleanName);
            if (func.Handle != IntPtr.Zero && _functionTypes.TryGetValue(cleanName, out var funcType))
            {
                var args = c.Arguments.Select(GenerateExpression).ToArray();
                return _builder.BuildCall2(funcType, func, args, "call_func");
            }

            return LLVMValueRef.CreateConstInt(_context.Int64Type, 0, false);
        }

        // --- RESTO DE FUNCIONES DE CONTROL DE FLUJO ---
        private void GenerateCheck(CheckNode node)
        {
            var function = _builder.InsertBlock.Parent;
            var condVal = GenerateExpression(node.Condition);
            if (condVal.TypeOf.IntWidth != 1)
                condVal = _builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, condVal, LLVMValueRef.CreateConstInt(condVal.TypeOf, 0, false), "booltmp");

            var thenBB = _context.AppendBasicBlock(function, "then");
            var elseBB = _context.AppendBasicBlock(function, "else");
            var mergeBB = _context.AppendBasicBlock(function, "ifcont");

            _builder.BuildCondBr(condVal, thenBB, elseBB);

            _builder.PositionAtEnd(thenBB);
            foreach (var s in node.ThenBlock) GenerateStatement(s);
            if (_builder.InsertBlock.Terminator.Handle == IntPtr.Zero) _builder.BuildBr(mergeBB);

            _builder.PositionAtEnd(elseBB);
            if (node.ElseBlock != null) foreach (var s in node.ElseBlock) GenerateStatement(s);
            if (_builder.InsertBlock.Terminator.Handle == IntPtr.Zero) _builder.BuildBr(mergeBB);

            _builder.PositionAtEnd(mergeBB);
        }

        private void GenerateRepeat(RepeatNode node)
        {
            var function = _builder.InsertBlock.Parent;
            var condBB = _context.AppendBasicBlock(function, "cond");
            var loopBB = _context.AppendBasicBlock(function, "loop");
            var afterBB = _context.AppendBasicBlock(function, "afterloop");

            _builder.BuildBr(condBB);
            _builder.PositionAtEnd(condBB);
            var condVal = GenerateExpression(node.Condition);
            if (condVal.TypeOf.IntWidth != 1) condVal = _builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, condVal, LLVMValueRef.CreateConstInt(condVal.TypeOf, 0, false), "loopcond");
            _builder.BuildCondBr(condVal, loopBB, afterBB);

            _builder.PositionAtEnd(loopBB);
            foreach (var s in node.Body) GenerateStatement(s);
            if (_builder.InsertBlock.Terminator.Handle == IntPtr.Zero) _builder.BuildBr(condBB);

            _builder.PositionAtEnd(afterBB);
        }

        private void GenerateLoop(LoopNode node)
        {
            if (node.Init != null) GenerateStatement(node.Init);
            var function = _builder.InsertBlock.Parent;
            var condBB = _context.AppendBasicBlock(function, "forcond");
            var bodyBB = _context.AppendBasicBlock(function, "forbody");
            var afterBB = _context.AppendBasicBlock(function, "forafter");

            _builder.BuildBr(condBB);
            _builder.PositionAtEnd(condBB);
            var condVal = GenerateExpression(node.Condition);
            if (condVal.TypeOf.IntWidth != 1) condVal = _builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, condVal, LLVMValueRef.CreateConstInt(condVal.TypeOf, 0, false), "forcond");
            _builder.BuildCondBr(condVal, bodyBB, afterBB);

            _builder.PositionAtEnd(bodyBB);
            foreach (var s in node.Body) GenerateStatement(s);
            if (node.Action != null) GenerateStatement(node.Action);
            if (_builder.InsertBlock.Terminator.Handle == IntPtr.Zero) _builder.BuildBr(condBB);

            _builder.PositionAtEnd(afterBB);
        }

        private void GenerateReturn(ReturnNode node)
        {
            if (node.Expression != null) _builder.BuildRet(GenerateExpression(node.Expression));
            else _builder.BuildRetVoid();
        }

        private void GenerateVariableDecl(VariableDeclNode node)
        {
            var type = MapType(node.Type!);
            var initVal = node.Initializer != null ? GenerateExpression(node.Initializer) : LLVMValueRef.CreateConstInt(_context.Int64Type, 0, false);
            var alloca = _builder.BuildAlloca(type, node.Name);
            _builder.BuildStore(initVal, alloca);
            _namedValues[node.Name] = alloca;
            _variableTypes[node.Name] = type;
        }

        private void GenerateSet(SetStatementNode node)
        {
            if (node.Target is IdentifierNode id && _namedValues.TryGetValue(id.Name, out var alloca))
                _builder.BuildStore(GenerateExpression(node.Expression), alloca);
        }

        private LLVMValueRef GenerateExpression(AstNode expr)
        {
            switch (expr)
            {
                case LiteralNode l:
                    string txt = l.ValueText;
                    if (l.Kind == "INT") return long.TryParse(txt, out long i) ? LLVMValueRef.CreateConstInt(_context.Int64Type, (ulong)i, false) : LLVMValueRef.CreateConstInt(_context.Int64Type, 0, false);
                    if (l.Kind == "FLOAT") return double.TryParse(txt, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d) ? LLVMValueRef.CreateConstReal(_context.DoubleType, d) : LLVMValueRef.CreateConstReal(_context.DoubleType, 0);
                    if (l.Kind == "STRING") return _builder.BuildGlobalStringPtr(txt.Trim('"'), "str");
                    if (l.Kind == "BOOL") return LLVMValueRef.CreateConstInt(_context.Int1Type, (ulong)((txt == "true") ? 1 : 0), false);
                    break;
                case IdentifierNode id:
                    if (_namedValues.TryGetValue(id.Name, out var v)) return _builder.BuildLoad2(_variableTypes[id.Name], v, id.Name);
                    throw new Exception($"Var '{id.Name}' not found");
                case BinaryExpressionNode b:
                    var L = GenerateExpression(b.Left);
                    var R = GenerateExpression(b.Right);
                    switch (b.Op)
                    {
                        case "+": return _builder.BuildAdd(L, R, "add");
                        case "-": return _builder.BuildSub(L, R, "sub");
                        case "*": return _builder.BuildMul(L, R, "mul");
                        case "/": return _builder.BuildSDiv(L, R, "div");
                        case "%": return _builder.BuildSRem(L, R, "mod");
                        case ">": return _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, L, R, "gt");
                        case "<": return _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, L, R, "lt");
                        case "==": return _builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, L, R, "eq");
                    }
                    break;
                case CallNode c: return GenerateCallExpression(c);
                case MemberAccessNode m:
                    // Si es una llamada a método (tiene MethodCall), generar la llamada
                    if (m.MethodCall != null)
                    {
                        return GenerateCallExpression(m.MethodCall);
                    }
                    // Si es acceso a propiedad, aquí se manejaría (no implementado aún)
                    break;
            }
            return LLVMValueRef.CreateConstInt(_context.Int64Type, 0, false);
        }

        private LLVMTypeRef MapType(TypeInfo t)
        {
            if (t.IsArray) return LLVMTypeRef.CreatePointer(GetBaseType(t.BaseName), 0);
            return GetBaseType(t.BaseName);
        }

        private LLVMTypeRef GetBaseType(string s)
        {
            return s switch
            {
                "i" => _context.Int64Type,
                "f" => _context.DoubleType,
                "b" => _context.Int1Type,
                "s" => LLVMTypeRef.CreatePointer(_context.Int8Type, 0),
                "void" => _context.VoidType,
                _ => _classStructs.ContainsKey(s) ? LLVMTypeRef.CreatePointer(_classStructs[s], 0) : LLVMTypeRef.CreatePointer(_context.Int8Type, 0)
            };
        }

        public void WriteToFile(string path) { try { _module.PrintToFile(path); } catch { } }
        public void Dispose() { _builder.Dispose(); _module.Dispose(); _context.Dispose(); }
    }
}