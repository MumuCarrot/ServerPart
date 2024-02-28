class Program 
{
    static void Main() 
    {
        Task.Run(() =>
        {
            foreach (var number in It())
            {
                Console.WriteLine(number); // Выводим числа из последовательности
            }
        });
    }

    static IEnumerable<int> It() 
    {
        int i = 0;
        while (true) 
        {
            Thread.Sleep(1000);
            yield return ++i;
        }
    }
}