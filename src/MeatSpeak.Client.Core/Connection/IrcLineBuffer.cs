using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using MeatSpeak.Protocol;

namespace MeatSpeak.Client.Core.Connection;

public sealed class IrcLineBuffer
{
    private readonly PipeReader _reader;

    public IrcLineBuffer(Stream stream)
    {
        _reader = PipeReader.Create(stream, new StreamPipeReaderOptions(bufferSize: IrcConstants.MaxLineLengthWithTags));
    }

    public async IAsyncEnumerable<IrcMessage> ReadLinesAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            ReadResult result;
            try
            {
                result = await _reader.ReadAsync(ct);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (IOException)
            {
                yield break;
            }

            var buffer = result.Buffer;

            while (TryReadLine(ref buffer, out var line))
            {
                if (line.Length == 0) continue;

                var bytes = line.ToArray();
                if (IrcLine.TryParse(bytes, out var parts))
                {
                    yield return parts.ToMessage();
                }
            }

            _reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
                yield break;
        }
    }

    private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        var reader = new SequenceReader<byte>(buffer);
        if (reader.TryReadTo(out ReadOnlySequence<byte> slice, (byte)'\n'))
        {
            // Trim trailing \r if present
            if (slice.Length > 0)
            {
                var lastByte = slice.Slice(slice.Length - 1, 1).FirstSpan[0];
                if (lastByte == (byte)'\r')
                    slice = slice.Slice(0, slice.Length - 1);
            }

            line = slice;
            buffer = buffer.Slice(reader.Position);
            return true;
        }

        line = default;
        return false;
    }

    // For unit testing with raw bytes
    public static IrcMessage? ParseLine(ReadOnlySpan<byte> data)
    {
        if (IrcLine.TryParse(data, out var parts))
            return parts.ToMessage();
        return null;
    }
}
