object Program
{
	entry func Main():i
	{
		declare z:s;
		declare x:i = 5;
		declare y:i = 3;

		loop (declare j:i = 0; j < 10; set j = j + 1)
		{
			show(j + y);
		}

		check (x > y)
		{
			show("La variable x es mas grande que la variable y");
		}
		otherwise
		{
			show("La variable y es mas grande que la variable x");
		}

		repeat (x < 7)
		{
			show("While loop");
			set x = x + 1;
		}

		show("Suma:");
		show(x + y);

		show("Resta:");
		show(x - y);

		show("Multiplicación:");
		show(x * y);

		show("División:");
		show(x / y);

		show("Módulo:");
		show(x % y);

		show("Ingresa un dato:");
		ask(z);

		show("Ingresaste:");
		show(z);
		
		declare obj:Math = Math();

		declare test:i = obj.suma(x, y);

		set x = obj.suma(x, y);
		show("Calculo terminado. El valor final es:");
		show(x);
		ask(x);

		gives 0;
	}
}

object Math
{
	func suma(a:i, c:i):i
	{
		gives a + c;
	}

	func factorial(num: i ) : i {
	
		gives num * factorial(num   - 1 )      ;
	}
}