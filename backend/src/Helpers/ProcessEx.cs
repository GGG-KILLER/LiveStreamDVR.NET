using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;

namespace LiveStreamDVR.Api.Helpers;

public sealed class ProcessEx
{
    private readonly Process _process;

    public event EventHandler<string>? OnStandardOutputLine;
    public event EventHandler<string>? OnStandardErrorLine;
    public event EventHandler OnExit { add => _process.Exited += value; remove => _process.Exited -= value; }

    public ProcessEx() : this(new Process())
    {
    }

    public ProcessEx(Process process)
    {
        _process = process;
        _process.StartInfo.RedirectStandardOutput = true;
        _process.StartInfo.RedirectStandardError = true;
    }

    public Process Process => _process;

    public async ValueTask<bool> StartAndWaitAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!_process.Start())
                return false;

            await Task.WhenAll([
                _process.WaitForExitAsync(cancellationToken),
                ReadLinesAsync(_process.StandardOutput.BaseStream, OnStandardOutputLine, cancellationToken),
                ReadLinesAsync(_process.StandardError.BaseStream, OnStandardErrorLine, cancellationToken)
            ]);

            return true;
        }
        catch (Exception)
        {
            if (!_process.HasExited)
            {
                _process.Kill(true);
            }

            throw;
        }
    }

    private async Task ReadLinesAsync(Stream stream, EventHandler<string>? onLine, CancellationToken cancellationToken = default)
    {
        var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));
        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken);
            var buffer = result.Buffer;

            if (tryReadLine(ref buffer, out var line) || result.IsCompleted)
            {
                var decodedLine = Encoding.UTF8.GetString(line);
                onLine?.Invoke(this, decodedLine);
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }
        await reader.CompleteAsync();

        static bool tryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
        {
            // Look for a EOL in the buffer.
            SequencePosition? position = buffer.PositionOf((byte)'\n');

            if (position == null)
            {
                line = default;
                return false;
            }

            // Skip the line + the \n.
            line = buffer.Slice(0, position.Value);
            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
            return true;
        }
    }
}
