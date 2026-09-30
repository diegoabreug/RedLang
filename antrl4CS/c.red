declare nombre:s;
show("Bienvenido Ingrese su nombre: ");
ask(nombre);
show("Gracias por probar mi compilador ");
show(nombre);

show("--- Declaraciones iniciales ---");
declare age: i = 25;
show("Edad inicial:");
show(age);

declare price: f = 19.99;
show("Precio inicial:");
show(price);

declare name: s = "RedLang";
show("Nombre inicial:");
show(name);

declare isActive: b = true;
show("Estado inicial (isActive):");
show(isActive);

declare maybeNumber: i?;
declare scores: array[i];

show("--- Asignaciones ---");
set age = 30;
show("Nueva edad:");
show(age);

set scores[0] = 100;
show("Puntaje en scores[0]:");
show(scores[0]);

set scores[1] = age + 5;
show("Puntaje en scores[1] (edad + 5):");
show(scores[1]);

show("--- Expresiones ---");
declare x: f = 10;
declare y: f = 3;
declare result: f = (x * 2.5) + (y / 2.0);
show("Resultado de (x * 2.5) + (y / 2.0):");
show(result);

declare flag: b = (x >= y) and (not (x == 0.0));
show("Resultado de flag (x >= y) and (not (x == 0.0)):");
show(flag);

declare isEqual: b = (x != y) or false;
show("Resultado de isEqual (x != y) or false:");
show(isEqual);

show("--- Estructura condicional (check) ---");
check (age >= 18) {
    show("Eres mayor de edad");
} otherwise {
    show("Eres menor de edad");
}

show("--- Bucle while (repeat) ---");
declare counter: i = 3;
repeat (counter > 0) {
    show("Cuenta regresiva: ");
    show(counter);
    set counter = counter - 1;
}

show("--- Bucle for (loop) ---");
loop (declare j: i = 0; j < 3; set j = j + 1) {
    show("Índice: ");
    show(j);
}

func add(a: i, c: i): i {
    give a + c;
}

func isPositive(n: i): b {
    check (n > 0) {
        give true;
    } otherwise {
        give false;
    }
}

show("--- Llamadas a funciones ---");
declare total: i = add(7, 8);
show("Suma (resultado de add(7, 8)):");
show(total);

declare positive: b = isPositive(-5);
show("¿Es positivo? (resultado de isPositive(-5)):");
show(positive);

show("--- Valores nulos ---");
declare empty: s? = null;
check (empty == null) {
    show("La variable 'empty' es null");
}

show("--- Operaciones con arrays ---");
declare first: i = scores[0];
show("Valor de 'first' (tomado de scores[0]):");
show(first);

set scores[2] = add(scores[0], scores[1]);
show("Valor de scores[2] (resultado de add(scores[0], scores[1])):");
show(scores[2]);

show("--- Recorriendo un Arreglo de Números ---");

declare numberList: array[i];
set numberList[0] = 11;
set numberList[1] = 22;
set numberList[2] = 33;
set numberList[3] = 44;
declare listSize: i = 4;

show("Recorriendo con 'repeat' (while):");
declare i_while: i = 0;
repeat (i_while < listSize) {
    show(numberList[i_while]);
    set i_while = i_while + 1;
}

show("Recorriendo con 'loop' (for):");
loop (declare i_for: i = 0; i_for < listSize; set i_for = i_for + 1) {
    show(numberList[i_for]);
}


show("--- Expresión compleja ---");
declare complex: b = ((5 + 3 * 2) >= 10) and (not (true == false or null == null));
show("Resultado de la expresión compleja:");
show(complex);

func factorial(num:i):i {
    check (num == 1) {
        give num;
    }
    otherwise
    {
        give num * factorial(num - 1);
    }
}

show(factorial(6));