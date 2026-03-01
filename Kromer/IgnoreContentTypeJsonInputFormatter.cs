using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Kromer;

public class IgnoreContentTypeJsonInputFormatter : TextInputFormatter
{
    private readonly JsonSerializerOptions _options;

    public IgnoreContentTypeJsonInputFormatter(JsonSerializerOptions options)
    {
        _options = options;

        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
        SupportedEncodings.Add(Encoding.ASCII);

        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("*/*"));
    }

    protected override bool CanReadType(Type type) => true;

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context,
        Encoding encoding)
    {
        using var reader = new StreamReader(context.HttpContext.Request.Body, encoding);

        var body = await reader.ReadToEndAsync();

        var result = JsonSerializer.Deserialize(body, context.ModelType, _options);
        return await InputFormatterResult.SuccessAsync(result);
    }
}