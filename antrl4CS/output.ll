; ModuleID = 'RedLangModule'
source_filename = "RedLangModule"

@printfmt = private unnamed_addr constant [6 x i8] c"%lld\0A\00", align 1
@str = private unnamed_addr constant [46 x i8] c"La variable x es mas grande que la variable y\00", align 1
@printfmt.1 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@str.2 = private unnamed_addr constant [46 x i8] c"La variable y es mas grande que la variable x\00", align 1
@printfmt.3 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@str.4 = private unnamed_addr constant [11 x i8] c"While loop\00", align 1
@printfmt.5 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@str.6 = private unnamed_addr constant [6 x i8] c"Suma:\00", align 1
@printfmt.7 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@printfmt.8 = private unnamed_addr constant [6 x i8] c"%lld\0A\00", align 1
@str.9 = private unnamed_addr constant [7 x i8] c"Resta:\00", align 1
@printfmt.10 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@printfmt.11 = private unnamed_addr constant [6 x i8] c"%lld\0A\00", align 1
@str.12 = private unnamed_addr constant [17 x i8] c"Multiplicaci\C3\B3n:\00", align 1
@printfmt.13 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@printfmt.14 = private unnamed_addr constant [6 x i8] c"%lld\0A\00", align 1
@str.15 = private unnamed_addr constant [11 x i8] c"Divisi\C3\B3n:\00", align 1
@printfmt.16 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@printfmt.17 = private unnamed_addr constant [6 x i8] c"%lld\0A\00", align 1
@str.18 = private unnamed_addr constant [9 x i8] c"M\C3\B3dulo:\00", align 1
@printfmt.19 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@printfmt.20 = private unnamed_addr constant [6 x i8] c"%lld\0A\00", align 1
@str.21 = private unnamed_addr constant [17 x i8] c"Ingresa un dato:\00", align 1
@printfmt.22 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@scanfmt = private unnamed_addr constant [3 x i8] c"%s\00", align 1
@str.23 = private unnamed_addr constant [12 x i8] c"Ingresaste:\00", align 1
@printfmt.24 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@printfmt.25 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@str.26 = private unnamed_addr constant [38 x i8] c"Calculo terminado. El valor final es:\00", align 1
@printfmt.27 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@printfmt.28 = private unnamed_addr constant [6 x i8] c"%lld\0A\00", align 1
@scanfmt.29 = private unnamed_addr constant [5 x i8] c"%lld\00", align 1

declare i32 @printf(ptr, ...)

declare i32 @scanf(ptr, ...)

declare ptr @malloc(i64)

define i64 @main() {
entry:
  %z = alloca ptr, align 8
  store i64 0, ptr %z, align 4
  %x = alloca i64, align 8
  store i64 5, ptr %x, align 4
  %y = alloca i64, align 8
  store i64 3, ptr %y, align 4
  %j = alloca i64, align 8
  store i64 0, ptr %j, align 4
  br label %forcond

forcond:                                          ; preds = %forbody, %entry
  %j1 = load i64, ptr %j, align 4
  %lt = icmp slt i64 %j1, 10
  br i1 %lt, label %forbody, label %forafter

forbody:                                          ; preds = %forcond
  %j2 = load i64, ptr %j, align 4
  %y3 = load i64, ptr %y, align 4
  %add = add i64 %j2, %y3
  %call_show = call i32 (ptr, ...) @printf(ptr @printfmt, i64 %add)
  %j4 = load i64, ptr %j, align 4
  %add5 = add i64 %j4, 1
  store i64 %add5, ptr %j, align 4
  br label %forcond

forafter:                                         ; preds = %forcond
  %x6 = load i64, ptr %x, align 4
  %y7 = load i64, ptr %y, align 4
  %gt = icmp sgt i64 %x6, %y7
  br i1 %gt, label %then, label %else

then:                                             ; preds = %forafter
  %call_show8 = call i32 (ptr, ...) @printf(ptr @printfmt.1, ptr @str)
  br label %ifcont

else:                                             ; preds = %forafter
  %call_show9 = call i32 (ptr, ...) @printf(ptr @printfmt.3, ptr @str.2)
  br label %ifcont

ifcont:                                           ; preds = %else, %then
  br label %cond

cond:                                             ; preds = %loop, %ifcont
  %x10 = load i64, ptr %x, align 4
  %lt11 = icmp slt i64 %x10, 7
  br i1 %lt11, label %loop, label %afterloop

loop:                                             ; preds = %cond
  %call_show12 = call i32 (ptr, ...) @printf(ptr @printfmt.5, ptr @str.4)
  %x13 = load i64, ptr %x, align 4
  %add14 = add i64 %x13, 1
  store i64 %add14, ptr %x, align 4
  br label %cond

afterloop:                                        ; preds = %cond
  %call_show15 = call i32 (ptr, ...) @printf(ptr @printfmt.7, ptr @str.6)
  %x16 = load i64, ptr %x, align 4
  %y17 = load i64, ptr %y, align 4
  %add18 = add i64 %x16, %y17
  %call_show19 = call i32 (ptr, ...) @printf(ptr @printfmt.8, i64 %add18)
  %call_show20 = call i32 (ptr, ...) @printf(ptr @printfmt.10, ptr @str.9)
  %x21 = load i64, ptr %x, align 4
  %y22 = load i64, ptr %y, align 4
  %sub = sub i64 %x21, %y22
  %call_show23 = call i32 (ptr, ...) @printf(ptr @printfmt.11, i64 %sub)
  %call_show24 = call i32 (ptr, ...) @printf(ptr @printfmt.13, ptr @str.12)
  %x25 = load i64, ptr %x, align 4
  %y26 = load i64, ptr %y, align 4
  %mul = mul i64 %x25, %y26
  %call_show27 = call i32 (ptr, ...) @printf(ptr @printfmt.14, i64 %mul)
  %call_show28 = call i32 (ptr, ...) @printf(ptr @printfmt.16, ptr @str.15)
  %x29 = load i64, ptr %x, align 4
  %y30 = load i64, ptr %y, align 4
  %div = sdiv i64 %x29, %y30
  %call_show31 = call i32 (ptr, ...) @printf(ptr @printfmt.17, i64 %div)
  %call_show32 = call i32 (ptr, ...) @printf(ptr @printfmt.19, ptr @str.18)
  %x33 = load i64, ptr %x, align 4
  %y34 = load i64, ptr %y, align 4
  %mod = srem i64 %x33, %y34
  %call_show35 = call i32 (ptr, ...) @printf(ptr @printfmt.20, i64 %mod)
  %call_show36 = call i32 (ptr, ...) @printf(ptr @printfmt.22, ptr @str.21)
  %input_buffer = call ptr @malloc(i64 256)
  store ptr %input_buffer, ptr %z, align 8
  %call_scanf = call i32 (ptr, ...) @scanf(ptr @scanfmt, ptr %input_buffer)
  %call_show37 = call i32 (ptr, ...) @printf(ptr @printfmt.24, ptr @str.23)
  %z38 = load ptr, ptr %z, align 8
  %call_show39 = call i32 (ptr, ...) @printf(ptr @printfmt.25, ptr %z38)
  %new_obj = call ptr @malloc(i64 8)
  %obj = alloca ptr, align 8
  store ptr %new_obj, ptr %obj, align 8
  %x40 = load i64, ptr %x, align 4
  %y41 = load i64, ptr %y, align 4
  %call_func = call i64 @suma(i64 %x40, i64 %y41)
  %test = alloca i64, align 8
  store i64 %call_func, ptr %test, align 4
  %x42 = load i64, ptr %x, align 4
  %y43 = load i64, ptr %y, align 4
  %call_func44 = call i64 @suma(i64 %x42, i64 %y43)
  store i64 %call_func44, ptr %x, align 4
  %call_show45 = call i32 (ptr, ...) @printf(ptr @printfmt.27, ptr @str.26)
  %x46 = load i64, ptr %x, align 4
  %call_show47 = call i32 (ptr, ...) @printf(ptr @printfmt.28, i64 %x46)
  %call_scanf48 = call i32 (ptr, ...) @scanf(ptr @scanfmt.29, ptr %x)
  ret i64 0
}

define i64 @suma(i64 %a, i64 %c) {
entry:
  %a1 = alloca i64, align 8
  store i64 %a, ptr %a1, align 4
  %c2 = alloca i64, align 8
  store i64 %c, ptr %c2, align 4
  %a3 = load i64, ptr %a1, align 4
  %c4 = load i64, ptr %c2, align 4
  %add = add i64 %a3, %c4
  ret i64 %add
}

define i64 @factorial(i64 %num) {
entry:
  %num1 = alloca i64, align 8
  store i64 %num, ptr %num1, align 4
  %num2 = load i64, ptr %num1, align 4
  %num3 = load i64, ptr %num1, align 4
  %sub = sub i64 %num3, 1
  %call_func = call i64 @factorial(i64 %sub)
  %mul = mul i64 %num2, %call_func
  ret i64 %mul
}
