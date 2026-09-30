    parser grammar construccion_semana2Parser;

options { tokenVocab=construccion_semana2Lexer; }

// ============================================================================
// REGLAS DEL PARSER (SINTAXIS)
// ============================================================================

/*
 * Regla principal del programa.
 * El programa puede contener importaciones (use) y clases (object).
 */
program
    : (otraclase | clase_decl)* EOF
    ;

// ---------------------------------------------------------------------------
// ESTRUCTURA DE CLASES
// ---------------------------------------------------------------------------

/*
 * Importación de clases externas.
 * Ejemplo: use MiOtraClase;
 */
otraclase
    : USE name SEMI_COLON
    ;

/*
 * Declaración de una clase (object).
 * Ejemplo: object MiClase { ... }
 */
clase_decl
    : OBJECT name O_BRACES classBody C_BRACES
    ;

classBody
    : classMember*
    ;

/*
 * Miembros posibles dentro de una clase:
 *  - Declaraciones de variables
 *  - Funciones normales
 *  - Función de entrada (entry func)
 */
classMember
    : declare_stmt
    | func_decl
    | entry_func_decl
    ;

// ---------------------------------------------------------------------------
// FUNCIONES
// ---------------------------------------------------------------------------

/*
 * Declaración estándar de función.
 * Ejemplo: func miFuncion(param1:i, param2:i):i { ... }
 */
func_decl
    : FUNC name O_PAREN param_list? C_PAREN COLON data_type block
    ;

/*
 * Declaración de la función de entrada principal (entry func).
 * Ejemplo: entry func Main():i { ... }
 */
entry_func_decl
    : ENTRY func_decl
    ;

/*
 * Lista de parámetros de función, separados por coma.
 */
param_list
    : param (COMMA param)*
    ;

/*
 * Declaración individual de parámetro.
 * Sintaxis: nombre : tipo
 */
param
    : name COLON data_type
    ;

// ---------------------------------------------------------------------------
// BLOQUES Y SENTENCIAS
// ---------------------------------------------------------------------------

/*
 * Un bloque de código es un conjunto de sentencias dentro de llaves { }.
 */
block
    : O_BRACES sentencia* C_BRACES
    ;

/*
 * Tipos de sentencias válidas dentro de un bloque.
 */
sentencia
    : declare_stmt
    | set_stmt
    | return_stmt
    | stmt_control
    | llamadaFuncion SEMI_COLON
    | accesoMiembro SEMI_COLON
    ;

// ---------------------------------------------------------------------------
// DECLARACIONES Y ASIGNACIONES
// ---------------------------------------------------------------------------

/*
 * Declaración de variables.
 * Ejemplos:
 *   declare x:i;
 *   declare y:f = 5;
 *   declare miObj:MiClase;
 *   declare miObj:MiClase = MiClase();
 */
declare_stmt
    : DECLARE name COLON (data_type | name?) (EQUAL inicializador)? SEMI_COLON 
    ;

/*
 * Reasignación de variables o estructuras.
 * Ejemplo:
 *   set x = x + 1;
 *   set miObj = MiClase();
 */
set_stmt
    : SET setObjetivo EQUAL expression SEMI_COLON
    ;

setObjetivo
    : name
    | accesoArreglo
    | accesoMiembro
    ;

/*
 * Retorno de valor desde una función.
 * Ejemplo:
 *   gives resultado;
 *   gives;
 */
return_stmt
    : GIVES expression? SEMI_COLON
    ;

// ---------------------------------------------------------------------------
// ESTRUCTURAS DE CONTROL (check, loop, repeat)
// ---------------------------------------------------------------------------

stmt_control
    : check_stmt
    | loop_stmt
    | repeat_stmt
    ;

/*
 * Condicional "check" con bloque opcional "otherwise".
 * Ejemplo:
 *   check (condicion) { ... } otherwise { ... }
 */
check_stmt
    : CHECK O_PAREN expression C_PAREN block otherwiseOpcional?
    ;

otherwiseOpcional
    : OTHERWISE block
    ;

/*
 * Bucle tipo "for".
 * Ejemplo:
 *   loop (declare i:i = 0; i < 10; set i = i + 1;) { ... }
 */
loop_stmt
    : LOOP O_PAREN loopInit SEMI_COLON expression SEMI_COLON accionLoop C_PAREN block
    ;

/*
 * Inicialización del bucle "loop",
 * que puede ser una declaración o una asignación sin punto y coma.
 */
loopInit
    : decl_head (EQUAL expression)?
    | set_stmt_no_sc
    ;

/*
 * Acción final de cada iteración del bucle.
 */
accionLoop
    : set_stmt_no_sc
    ;

/*
 * Bucle tipo "repeat" (while-like).
 * Ejemplo:
 *   repeat (condicion) { ... }
 */
repeat_stmt
    : REPEAT O_PAREN expression C_PAREN block
    ;

// ---------------------------------------------------------------------------
// AUXILIARES SIN PUNTO Y COMA (para estructuras loop)
// ---------------------------------------------------------------------------

decl_head
    : DECLARE name COLON (data_type | name?)
    ;

set_stmt_no_sc
    : SET setObjetivo EQUAL expression
    ;

// ---------------------------------------------------------------------------
// TIPOS DE DATOS E INICIALIZADORES
// ---------------------------------------------------------------------------

/*
 * Definición de tipo de dato: primitivo, clase, arreglo o nullable (?).
 */
data_type
    : type_base array_specifier? QUESTION?
    ;

/*
 * Tipos base soportados:
 * - i (entero)
 * - f (decimal)
 * - b (booleano)
 * - s (cadena)
 * - name (nombre de clase o tipo de usuario)
 */
type_base
    : TYPE_I | TYPE_F | TYPE_B | TYPE_S | name
    ;

/*
 * Especificación del tamaño de un arreglo.
 * Ejemplo: [3]
 */
array_specifier
    : O_BRACKETS expression C_BRACKETS
    ;

/*
 * Valor de inicialización de una variable.
 */
inicializador
    : expression
    ;

// ---------------------------------------------------------------------------
// EXPRESIONES Y OPERADORES
// ---------------------------------------------------------------------------

/*
 * Expresiones con operadores lógicos, relacionales y aritméticos.
 * Mantiene precedencia simplificada (sintaxis centrada, no semántica).
 */
expression
    : expression OR expression          # LogicalOr
    | expression AND expression         # LogicalAnd
    | NOT expression                    # LogicalNot
    | expression comparador expression  # Relational
    | expression (PLUS | MINUS) expression # AddSub
    | expression (MULTIPLY | DIVIDE | MODULO) expression # MulDiv
    | MINUS expression                  # UnaryMinus
    | factor                            # Atom
    ;

comparador
    : EQ | NEQ | GTE | LTE | GTHAN | LTHAN
    ;

factor
    : name
    | literal
    | accesoArreglo
    | accesoMiembro
    | llamadaFuncion
    | O_PAREN expression C_PAREN
    ;

// ---------------------------------------------------------------------------
// LITERALES Y ARREGLOS
// ---------------------------------------------------------------------------

literal
    : BOOL
    | FLOAT
    | INT
    | STRING
    | NULL
    | array_literal
    ;

/*
 * Literal de arreglo: [1, 2, 3]
 */
array_literal 
    : O_BRACKETS (listaArgumentos)? C_BRACKETS
    ;

// ---------------------------------------------------------------------------
// ACCESO A ARREGLOS, MIEMBROS Y LLAMADAS A FUNCIONES
// ---------------------------------------------------------------------------

accesoArreglo
    : name O_BRACKETS expression C_BRACKETS
    ;

accesoMiembro
    : name DOT name
    | name DOT llamadaFuncion
    ;

/*
 * Llamada a función: admite funciones nativas o definidas por el usuario.
 */
llamadaFuncion
    : (ASK | SHOW | LEN | FILE_OP | CONVERT_OP) O_PAREN listaArgumentos? C_PAREN
    | name O_PAREN listaArgumentos? C_PAREN
    ;

listaArgumentos
    : expression (COMMA expression)*
    ;

/*
 * Identificadores de nombres (variables, funciones, clases).
 */
name
    : ID
    ;