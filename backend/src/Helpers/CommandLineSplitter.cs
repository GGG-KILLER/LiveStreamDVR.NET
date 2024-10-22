using LinkDotNet.StringBuilder;

namespace LiveStreamDVR.Api.Helpers;

public static class CommandLineSplitter
{
    public static string[] SplitArguments(string arguments)
    {
        var args = new List<string>();
        for (var idx = 0; idx < arguments.Length; idx++)
        {
            if (char.IsWhiteSpace(arguments[idx]))
                continue;
            args.Add(readNextArgument(arguments, ref idx));
        }
        return [.. args];

        static string readNextArgument(string arguments, ref int idx)
        {
            var builder = new ValueStringBuilder(stackalloc char[256]);
            var inString = false;
            var delimiter = '\0';
            for (; idx < arguments.Length; idx++)
            {
                char c = arguments[idx];
                switch (c)
                {
                    case '"':
                    case '\'':
                        if (inString)
                        {
                            if (c == delimiter)
                            {
                                inString = false;
                                delimiter = '\0';
                                continue;
                            }
                            else
                            {
                                goto default;
                            }
                        }
                        else
                        {
                            inString = true;
                            delimiter = c;
                            continue;
                        }

                    case '\\':
                        if (delimiter == '\'')
                            goto default;
                        else
                        {
                            c = arguments[++idx];
                            switch (c)
                            {
                                case '0' when delimiter == '"': builder.Append('\0'); break;
                                case 'n' when delimiter == '"': builder.Append('\n'); break;
                                case 'r' when delimiter == '"': builder.Append('\r'); break;
                                case '"': builder.Append('"'); break;
                                case '\'': builder.Append('\''); break;
                                default: builder.Append(c); break;
                            };
                        }
                        break;

                    case char ch when !inString && char.IsWhiteSpace(ch):
                        return builder.ToString();

                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
