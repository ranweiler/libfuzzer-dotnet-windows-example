using SharpFuzz;

namespace Example;

public class Program {
    public static void Main(string[] args) {
        Fuzzer.LibFuzzer.Run(data =>
        {
            var reader = new InputReader();
            reader.Process(data);
        });
    }
}
