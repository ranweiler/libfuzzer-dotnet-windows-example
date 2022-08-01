namespace Example;

public class InputReader
{
    public void Process(ReadOnlySpan<byte> data) {
        if (data.Length < 4) {
            return;
        }

        var count = 0;

        if (data[0] == 'o') { count++; }
        if (data[1] == 'u') { count++; }
        if (data[2] == 'c') { count++; }
        if (data[3] == 'h') { count++; }

        // Simulate an out-of-bounds access.
        if (count >= 4) {
            var _ = data[data.Length];
        }
    }
}
